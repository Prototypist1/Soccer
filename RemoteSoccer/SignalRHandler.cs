using Common;
using Microsoft.AspNetCore.SignalR.Client;
using Prototypist.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteSoccer
{
    class SingleSignalRHandler {

        private static Task<SignalRHandler> instance;

        public static async Task<SignalRHandler> Get(Func<Exception,bool, Task> onClosed) {
            var getter = new Getter(onClosed);
            return await getter.GotIt(Interlocked.CompareExchange(ref instance, getter.task, null) == null, onClosed);
        }

        public static Task<SignalRHandler> GetOrNull()
        {
            return instance;
        }


        public static Task<SignalRHandler> GetOrBug()
        {
            var res = instance;

            if (res == null) {
                throw new Exception("bug! should not be null!");
            }

            return res;
        }


        protected class Getter
        {
            private TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
            internal Task<SignalRHandler> task;

            public async Task<SignalRHandler> GotIt(bool gotIt, Func<Exception, bool, Task> onClosed) {
                taskCompletionSource.SetResult(gotIt);
                var res = await task;
                    res.SetOnClosed(async ex => {
                    await onClosed(ex, ConnectionLost(task));
                });
                return res;
            }


            public Getter(Func<Exception,bool, Task> onClosed)
            {
                task = MakeTask(this, onClosed);
            }

            private async Task<SignalRHandler> MakeTask(Getter getter ,Func<Exception,bool, Task> onClosed) {
                var gotIt = await taskCompletionSource.Task;
                if (gotIt) {

                    return await Create(getter, async ex => {
                        await onClosed(ex, ConnectionLost(task));
                    });
                }
                else {
                    return await SingleSignalRHandler.instance;
                }
            }
        }

        public static bool ConnectionLost(Task<SignalRHandler> task) {

            if (Interlocked.CompareExchange(ref instance, null, task) == task) {

                return true;
            }
            return false;
        }

        private static async Task<SignalRHandler> Create(Getter getter, Func<Exception, Task> onClosed)
        {

            var connection = new HubConnectionBuilder()
                .WithUrl(@"http://localhost:50737/GameHub")
                //.WithUrl(@"https://soccerserver.azurewebsites.net/GameHub")
                .Build();

            var res = new SignalRHandler(connection);

            res.SetOnClosed(onClosed);

            try
            {
                await connection.StartAsync();
            }
            catch (Exception e) {
                var dontwait = onClosed(e);
            }
            return res;
        }

    }


    public class SignalRHandler
    {

        private readonly List<Action<GameCreated>> gameCreatedHandlers = new List<Action<GameCreated>>();
        private readonly List<Action<GameAlreadyExists>> gameAlreadyExistsHandlers = new List<Action<GameAlreadyExists>>();

        private readonly List<Action<GameJoined>> gameJoinedHandlers = new List<Action<GameJoined>>();
        private readonly List<Action<GameDoesNotExist>> gameDoesNotExistHandlers = new List<Action<GameDoesNotExist>>();

        private readonly HubConnection connection;

        public void SetOnClosed(Func<Exception, Task> onClosed)
        {
            connection.Closed += onClosed;
        }

        public SignalRHandler(HubConnection connection)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            connection.On<GameCreated>(nameof(GameCreated), HandleGameCreated);
            connection.On<GameAlreadyExists>(nameof(GameAlreadyExists), HandleGameAlreadyExists);
            connection.On<GameJoined>(nameof(GameJoined), HandleGameJoined);
            connection.On<GameDoesNotExist>(nameof(GameDoesNotExist), HandleGameDoesNotExist);

        }

        private void HandleGameDoesNotExist(GameDoesNotExist gameDoesNotExist)
        {
            foreach (var handler in gameDoesNotExistHandlers.ToArray())
            {
                handler(gameDoesNotExist);
            }
        }

        private void HandleGameJoined(GameJoined gameJoined)
        {
            foreach (var handler in gameJoinedHandlers.ToArray())
            {
                handler(gameJoined);
            }
        }

        private void HandleGameAlreadyExists(GameAlreadyExists gameAlreadyExists)
        {
            foreach (var handler in gameAlreadyExistsHandlers.ToArray())
            {
                handler(gameAlreadyExists);
            }
        }

        private void HandleGameCreated(GameCreated gameCreated)
        {
            foreach (var handler in gameCreatedHandlers.ToArray())
            {
                handler(gameCreated);
            }
        }

        public async Task<OrType<GameCreated, GameAlreadyExists>> Send(CreateGame createGame)
        {
            var taskCompletionSource = new TaskCompletionSource<OrType<GameCreated, GameAlreadyExists>>();

            (Action<GameCreated>, Action<GameAlreadyExists>) actions = (null, null);
            actions = (
               (GameCreated x) => {
                   if (x.Id == createGame.Id)
                   {
                       taskCompletionSource.SetResult(new OrType<GameCreated, GameAlreadyExists>(x));
                       gameCreatedHandlers.Remove(actions.Item1);
                       gameAlreadyExistsHandlers.Remove(actions.Item2);
                   }
               },
                (GameAlreadyExists x) => {
                    if (x.Id == createGame.Id)
                    {
                        taskCompletionSource.SetResult(new OrType<GameCreated, GameAlreadyExists>(x));
                        gameCreatedHandlers.Remove(actions.Item1);
                        gameAlreadyExistsHandlers.Remove(actions.Item2);
                    }
                }
            );

            gameCreatedHandlers.Add(actions.Item1);
            gameAlreadyExistsHandlers.Add(actions.Item2);

            await connection.InvokeAsync(nameof(CreateGame), createGame);

            return await taskCompletionSource.Task;
        }

        public async Task<OrType<GameJoined, GameDoesNotExist>> Send(JoinGame joinGame)
        {
            var taskCompletionSource = new TaskCompletionSource<OrType<GameJoined, GameDoesNotExist>>();

            (Action<GameJoined>, Action<GameDoesNotExist>) actions = (null, null);
            actions = (
               (GameJoined x) => {
                   if (x.Id == joinGame.Id)
                   {
                       taskCompletionSource.SetResult(new OrType<GameJoined, GameDoesNotExist>(x));
                       gameJoinedHandlers.Remove(actions.Item1);
                       gameDoesNotExistHandlers.Remove(actions.Item2);
                   }
               },
                (GameDoesNotExist x) => {
                    if (x.Id == joinGame.Id)
                    {
                        taskCompletionSource.SetResult(new OrType<GameJoined, GameDoesNotExist>(x));
                        gameJoinedHandlers.Remove(actions.Item1);
                        gameDoesNotExistHandlers.Remove(actions.Item2);
                    }
                }
            );

            gameJoinedHandlers.Add(actions.Item1);
            gameDoesNotExistHandlers.Add(actions.Item2);

            await connection.InvokeAsync(nameof(JoinGame), joinGame);

            return await taskCompletionSource.Task;
        }

        public async void Send(string game, CreatePlayer createPlayer, Action<Positions> handlePossitions, Action<ObjectsCreated> handleObjectsCreated, Action<ObjectsRemoved> handleObjectsRemoved)
        {
            connection.On(nameof(Positions), handlePossitions);
            connection.On(nameof(ObjectsCreated), handleObjectsCreated);
            connection.On(nameof(ObjectsRemoved), handleObjectsRemoved);

            await connection.InvokeAsync(nameof(CreatePlayer), game, createPlayer);
        }

        public async void Send(string game, PlayerInputs inputs)
        {
            await connection.InvokeAsync(nameof(PlayerInputs), game, inputs);
        }
    }

}
