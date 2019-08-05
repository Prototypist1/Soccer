using Common;
using Microsoft.AspNetCore.SignalR;
using Physics;
using Prototypist.TaskChain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server
{
    public class GameHubState {

        // I don't really like how I am storing this data
        public readonly ConcurrentIndexed<string, Game> games = new ConcurrentIndexed<string, Game>();
        public readonly ConcurrentIndexed<string, string> connectionIdToGameName = new ConcurrentIndexed<string, string>();
        public readonly IHubContext<GameHub> connectionManager;

        public GameHubState(IHubContext<GameHub> connectionManager)
        {
            this.connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }
    }

    public class GameHub : Hub
    {

        private readonly GameHubState state;


        public GameHub(GameHubState state)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
        }

        private Action<UpdateScore> getOnUpdateScore(string id) => x =>
        {
            state.connectionManager.Clients.Group(id).SendAsync(nameof(UpdateScore), x);
        };

        public async Task CreateGame(CreateGame createGame) {

            var myGame = new Game(getOnUpdateScore(createGame.Id));
            if (state.games.TryAdd(createGame.Id, myGame) && state.connectionIdToGameName.TryAdd(Context.ConnectionId, createGame.Id)) {
                await Clients.Caller.SendAsync(nameof(GameCreated), new GameCreated(createGame.Id));
                myGame.Start(async positions =>
                {
                    await state.connectionManager.Clients.Group(createGame.Id).SendAsync(nameof(Positions), positions);
                });
            }
            else {
                await Clients.Caller.SendAsync(nameof(GameAlreadyExists), new GameAlreadyExists(createGame.Id));
            }
        }

        public void ResetGame(ResetGame resetGame)
        {
            if (state.games.TryGetValue(resetGame.Id , out var value))
            {
                value.Reset(getOnUpdateScore(resetGame.Id));
            }
        }

        public async Task JoinGame(JoinGame joinGame)
        {
            if (state.games.ContainsKey(joinGame.Id) && state.connectionIdToGameName.TryAdd(Context.ConnectionId,joinGame.Id))
            {
                await Clients.Caller.SendAsync(nameof(GameJoined), new GameJoined(joinGame.Id));
            }
            else
            {
                await Clients.Caller.SendAsync(nameof(GameDoesNotExist), new GameDoesNotExist(joinGame.Id));
            }
        }

        public async Task CreateOrJoinGame(CreateOrJoinGame createOrJoinGame)
        {
            var myGame = new Game(getOnUpdateScore(createOrJoinGame.Id));
            var game = state.games.GetOrAdd(createOrJoinGame.Id, myGame);

            if (!state.connectionIdToGameName.TryAdd(Context.ConnectionId, createOrJoinGame.Id)) {
                // error!
                // I mean you are already in so who cares?
            }

            if(myGame == game)
            {
                await Clients.Caller.SendAsync(nameof(GameCreated), new GameCreated(createOrJoinGame.Id));
                myGame.Start(async positions =>
                {
                    await state.connectionManager.Clients.Group(createOrJoinGame.Id).SendAsync(nameof(Positions), positions);
                });
            }
            else
            {
                await Clients.Caller.SendAsync(nameof(GameJoined), new GameJoined(createOrJoinGame.Id));
            }
        }


        public async Task CreatePlayer(string game, CreatePlayer createPlayer)
        {
            // create the player
            var playerCreated = state.games[game].CreatePlayer(Context.ConnectionId,createPlayer);
            // tell the other players
            await Clients.Group(game).SendAsync(nameof(ObjectsCreated),new ObjectsCreated(playerCreated.ToArray()));
            // tell the new player about everyone
            await Clients.Caller.SendAsync(nameof(ObjectsCreated), new ObjectsCreated(state.games[game].GetObjectsCreated().ToArray()));
            // add the player to the group
            await Groups.AddToGroupAsync(Context.ConnectionId, game);
        }

        public void PlayerInputs(string game, PlayerInputs playerInputs)
        {
            state.games[game].PlayerInputs(playerInputs);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                if (state.connectionIdToGameName.TryRemove(Context.ConnectionId, out var gameName))
                {
                    if (state.games.TryGetValue(gameName, out var game))
                    {
                        if (game.TryDisconnect(Context.ConnectionId, out var objectRemoveds))
                        {

                            // tell the other players
                            await Clients.Group(gameName).SendAsync(nameof(ObjectsRemoved), new ObjectsRemoved(objectRemoveds.ToArray()));
                            var dontwait = Task.Run(async () =>
                            {
                                await Task.Delay(5000);
                                if (game.LastInput.AddMinutes(5) < DateTime.Now)
                                {
                                    state.games.TryRemove(gameName, out var _);
                                }
                            });
                        }
                    }
                }
            }
#pragma warning disable CS0168 // Variable is declared but never used
            catch (Exception e)
            {
#pragma warning restore CS0168 // Variable is declared but never used
#pragma warning disable CS0219 // Variable is assigned but its value is never used
                var db = 0;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
