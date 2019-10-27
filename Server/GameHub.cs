using Common;
using Microsoft.AspNetCore.SignalR;
using Physics;
using Prototypist.TaskChain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
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

        public void ResetGame(ResetGame resetGame)
        {
            if (state.games.TryGetValue(resetGame.Id , out var value))
            {
                // this is why () should not be the method call syntax
                // .Invoke included for readablity
                getOnUpdateScore(resetGame.Id).Invoke(value.Reset());
            }
        }

        public IAsyncEnumerable<Positions> JoinChannel(JoinChannel joinChannel) {
            return state.games.GetOrThrow(joinChannel.Id).GetReader();
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
            }
            else
            {
                await Clients.Caller.SendAsync(nameof(GameJoined), new GameJoined(createOrJoinGame.Id));
            }
        }


        public async Task CreatePlayer(string game, CreatePlayer createPlayer)
        {
            // create the player
            // tell the other players
            await Clients.Group(game).SendAsync(nameof(ObjectsCreated), state.games[game].CreatePlayer(Context.ConnectionId, createPlayer));
            // tell the new player about everyone
            await Clients.Caller.SendAsync(nameof(ObjectsCreated), state.games[game].GetObjectsCreated());
            // add the player to the group
            await Groups.AddToGroupAsync(Context.ConnectionId, game);
        }


        public async Task ColorChanged(string game, ColorChanged colorChanged)
        {
            // update the players color
            state.games[game].ColorChanged(colorChanged);
            // tell the other players
            await Clients.Group(game).SendAsync(nameof(ColorChanged), colorChanged);
        }

        public async Task NameChanged(string game, NameChanged nameChanged)
        {
            // update the players color
            state.games[game].NameChanged(nameChanged);
            // tell the other players
            await Clients.Group(game).SendAsync(nameof(NameChanged), nameChanged);
        }

        public async Task PlayerInputs(string game, IAsyncEnumerable<PlayerInputs> playerInputs)
        {
            var game1 = state.games[game];

            await foreach (var item in playerInputs)
            {
                game1.PlayerInputs(item);
            }
        }

        public async Task LeaveGame(LeaveGame leaveGame) {
            await LeaveGame(Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await LeaveGame(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        private async Task LeaveGame(string connection)
        {
            try
            {
                if (state.connectionIdToGameName.TryRemove(connection, out var gameName))
                {
                    if (state.games.TryGetValue(gameName, out var game))
                    {
                        if (game.TryDisconnect(connection, out var objectRemoveds))
                        {

                            // tell the other players
                            await Clients.Group(gameName).SendAsync(nameof(ObjectsRemoved), new ObjectsRemoved(objectRemoveds.ToArray()));
                            var dontwait = Task.Run(async () =>
                            {
                                await Task.Delay(5000);
                                if (game.LastInputUTC.AddMinutes(5) < DateTime.Now)
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
        }
    }
}
