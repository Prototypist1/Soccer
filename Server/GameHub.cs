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

        public async Task CreateGame(CreateGame createGame) {
            var myGame = new Game(x =>{
                state.connectionManager.Clients.Group(createGame.Id).SendAsync(nameof(UpdateScore), x);
            });
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
            catch (Exception e) {
                var db = 0;
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
