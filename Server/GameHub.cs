using Common;
using Microsoft.AspNetCore.SignalR;
using Physics;
using Prototypist.TaskChain;
using Prototypist.TaskChain.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server
{

    public static class GameHubState {

        public static readonly ConcurrentIndexed<string, Game> games = new ConcurrentIndexed<string, Game>();
    }

    public class GameHub : Hub
    {

        public async Task CreateGame(CreateGame createGame) {
            var myGame = new Game();
            var game = GameHubState.games.GetOrAdd(createGame.Id,myGame);
            if (ReferenceEquals(game, myGame)) {
                await Clients.Caller.SendAsync(nameof(GameCreated), new GameCreated(createGame.Id));
            }
            else {
                await Clients.Caller.SendAsync(nameof(GameAlreadyExists), new GameAlreadyExists(createGame.Id));
            }
        }

        public async Task JoinGame(JoinGame joinGame)
        {
            if (GameHubState.games.ContainsKey(joinGame.Id))
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
            var playerCreated = GameHubState.games[game].CreatePlayer(createPlayer);
            // tell the other players
            await Clients.Group(game.ToString()).SendAsync(nameof(ObjectsCreated),new ObjectsCreated(playerCreated.ToArray()));
            // tell the new player about everyone
            await Clients.Caller.SendAsync(nameof(ObjectsCreated), new ObjectsCreated(GameHubState.games[game].GetObjectsCreated().ToArray()));
            // add the player to the group
            await Groups.AddToGroupAsync(Context.ConnectionId, game.ToString());
        }

        public void PlayerInputs(string game, PlayerInputs playerInputs)
        {
           var positions = GameHubState.games[game].PlayerInputs(playerInputs);
            if (positions.Any()) {
                Clients.Group(game.ToString()).SendAsync(nameof(Positions), positions.Last());
            }
        }
    }
}
