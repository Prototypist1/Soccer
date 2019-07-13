using Common;
using Microsoft.AspNetCore.SignalR;
using Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server
{

    public static class GameHubState {

        public static readonly Dictionary<Guid, Game> games = new Dictionary<Guid, Game>();
    }

    public class GameHub : Hub
    {

        public void CreateGame(CreateGame createGame) {
            GameHubState.games[createGame.Id] = new Game();
        }

        public async Task CreatePlayer(Guid game, CreatePlayer createPlayer)
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

        public void PlayerInputs(Guid game, PlayerInputs playerInputs)
        {
           var positions = GameHubState.games[game].PlayerInputs(playerInputs);
            if (positions.Any()) {
                Clients.Group(game.ToString()).SendAsync(nameof(Positions), positions.Last());
            }
        }
    }
}
