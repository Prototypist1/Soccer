using Common;
using Microsoft.AspNetCore.SignalR;
using physics2;
using Prototypist.TaskChain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Server
{

    public class RemoteGame {
        public readonly Game2 game2 = new Game2();

        public TaskCompletionSource<bool> next = new TaskCompletionSource<bool>();
        
        internal async IAsyncEnumerable<GameStateUpdate> GetReader()
        {
            while (true)
            {
                await next.Task;
                next = new TaskCompletionSource<bool>();
                yield return game2.gameState.GetGameStateUpdate();
            }

        }

        private ConcurrentIndexed<Guid, ConcurrentLinkedList<PlayerInputs>> recieved = new ConcurrentIndexed<Guid, ConcurrentLinkedList<PlayerInputs>>();

        internal void PlayerInputs(PlayerInputs item)
        {
            recieved
                .GetOrAdd(item.Id, new ConcurrentLinkedList<PlayerInputs>())
                .Add(item);

            var sum = recieved.Sum(x => x.Value.Count);
            if (sum >= game2.gameState.players.Count) {
                var inputs = new Dictionary<Guid, PlayerInputs>();
                foreach (var pair in recieved)
                {
                    if (pair.Value.TryGetFirst(out var first)) {
                        // TODO TryGetFirst and RemoveStart should be combined
                        pair.Value.RemoveStart();
                        inputs[pair.Key] = first;
                    }
                }
                game2.ApplyInputs(inputs);
                try
                {
                    next.SetResult(true);
                }
                catch (System.InvalidOperationException) { 
                    // oh well, it already has a result
                }
            }
        }
    }


    public class GameHubState {

        // I don't really like how I am storing this data
        public readonly ConcurrentIndexed<string, RemoteGame> games = new ConcurrentIndexed<string, RemoteGame>();
        public readonly ConcurrentIndexed<string, string> connectionIdToGameName = new ConcurrentIndexed<string, string>();
        public readonly ConcurrentIndexed<string, Guid> connectionIdToPlayerId = new ConcurrentIndexed<string, Guid>();
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

        //public void ResetGame(ResetGame resetGame)
        //{
        //    if (state.games.TryGetValue(resetGame.Id , out var value))
        //    {
        //        // this is why () should not be the method call syntax
        //        // .Invoke included for readablity
        //        getOnUpdateScore(resetGame.Id).Invoke(value.Reset());
        //    }
        //}

        public IAsyncEnumerable<GameStateUpdate> JoinChannel(JoinChannel joinChannel) {
            return state.games.GetOrThrow(joinChannel.Id).GetReader();
        }

        public async Task CreateOrJoinGame(CreateOrJoinGame createOrJoinGame)
        {
            var myGame = new RemoteGame();
            var game = state.games.GetOrAdd(createOrJoinGame.Id, myGame);

            //if (myGame == game) {
            //    myGame.Init(getOnUpdateScore(createOrJoinGame.Id), createOrJoinGame.FieldDimensions);
            //}

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


        public void AddPlayerEvent(string game, AddPlayerEvent createPlayer)
        {
            state.connectionIdToPlayerId[Context.ConnectionId] = createPlayer.id;
            state.games.GetOrThrow(game).game2.gameState.Handle(createPlayer);
        }

        public async Task PlayerInputs(string game, IAsyncEnumerable<PlayerInputs> playerInputs)
        {
            var game1 = state.games[game];

            await foreach (var item in playerInputs)
            {
                game1.PlayerInputs(item);
            }
        }

        public void LeaveGame(LeaveGame leaveGame) {
            LeaveGame(Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            LeaveGame(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        private void LeaveGame(string connection)
        {
            try
            {
                if (state.connectionIdToGameName.TryRemove(connection, out var gameName))
                {
                    if (state.games.TryGetValue(gameName, out var game))
                    {
                        if (state.connectionIdToPlayerId.TryRemove(connection, out var guid))
                        {
                            game.game2.gameState.Handle(new RemovePlayerEvent(guid));
                        }
                    }
                }
            }
#pragma warning disable CS0168 // Variable is declared but never used
            catch (Exception e)
            {
#pragma warning restore CS0168 // Variable is declared but never used
            }
        }
    }
}
