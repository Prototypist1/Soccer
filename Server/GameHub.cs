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

        public readonly ConcurrentIndexed<string, Game> games = new ConcurrentIndexed<string, Game>();
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
            var myGame = new Game();
            var game = state.games.GetOrAdd(createGame.Id,myGame);
            if (ReferenceEquals(game, myGame)) {
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
            if (state.games.ContainsKey(joinGame.Id))
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
            var playerCreated = state.games[game].CreatePlayer(createPlayer);
            // tell the other players
            await Clients.Group(game.ToString()).SendAsync(nameof(ObjectsCreated),new ObjectsCreated(playerCreated.ToArray()));
            // tell the new player about everyone
            await Clients.Caller.SendAsync(nameof(ObjectsCreated), new ObjectsCreated(state.games[game].GetObjectsCreated().ToArray()));
            // add the player to the group
            await Groups.AddToGroupAsync(Context.ConnectionId, game.ToString());
        }

        public void PlayerInputs(string game, PlayerInputs playerInputs)
        {
            state.games[game].PlayerInputs(playerInputs);
        }
    }
}
