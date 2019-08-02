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
    class SingleSignalRHandler
    {

        private static Task<SignalRHandler> instance;

        public static async Task<SignalRHandler> GetCreateOrThrow()
        {
            var getter = new Getter();
            return await getter.GotIt(Interlocked.CompareExchange(ref instance, getter.task, null) == null);
        }

        // I need to go around protection all of these
        public static async Task<SignalRHandler> GetOrThrow()
        {
            var res = instance;
            if (instance == null) {
                throw new Exception("oh no! we don't have a signal r handler!");
            }
            return await res;
        }

        public class Getter
        {
            private TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
            internal Task<SignalRHandler> task;

            public async Task<SignalRHandler> GotIt(bool gotIt)
            {
                taskCompletionSource.SetResult(gotIt);
                return await task;
            }


            public Getter()
            {
                task = MakeTask();
            }

            private async Task<SignalRHandler> MakeTask()
            {
                var gotIt = await taskCompletionSource.Task;
                if (gotIt)
                {

                    return await CreateOrThrow(this);
                }
                else
                {
                    return await SingleSignalRHandler.instance;
                }
            }
        }

        public static bool ConnectionLost(Task<SignalRHandler> task)
        {

            if (Interlocked.CompareExchange(ref instance, null, task) == task)
            {

                return true;
            }
            return false;
        }

        private static async Task<SignalRHandler> CreateOrThrow(Getter myGetter)
        {

            var connection = new HubConnectionBuilder()
                .WithUrl(@"http://localhost:50737/GameHub")
                //.WithUrl(@"https://soccerserver.azurewebsites.net/GameHub")
                .Build();

            var res = new SignalRHandler(connection, myGetter);

            await connection.StartAsync();

            return res;
        }


        public class SignalRHandler
        {

            private readonly List<Action<GameCreated>> gameCreatedHandlers = new List<Action<GameCreated>>();
            private readonly List<Action<GameAlreadyExists>> gameAlreadyExistsHandlers = new List<Action<GameAlreadyExists>>();

            private readonly List<Action<GameJoined>> gameJoinedHandlers = new List<Action<GameJoined>>();
            private readonly List<Action<GameDoesNotExist>> gameDoesNotExistHandlers = new List<Action<GameDoesNotExist>>();

            private readonly HubConnection connection;
            private readonly Getter getter;
            private Func<Exception, Task> onClosed;

            private Exception alreadyClosed = null;

            // accepts null
            // use null to release onclosed
            public void SetOnClosed(Func<Exception, Task> onClosed)
            {
                this.onClosed = onClosed;

                if (alreadyClosed != null &&
                    onClosed != null &&
                    ConnectionLost(getter.task))
                {
                    onClosed(alreadyClosed);
                }
            }

            public SignalRHandler(HubConnection connection, Getter getter)
            {
                this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
                this.getter = getter ?? throw new ArgumentNullException(nameof(getter));
                connection.Closed += OnClosed;
                connection.On<GameCreated>(nameof(GameCreated), HandleGameCreated);
                connection.On<GameAlreadyExists>(nameof(GameAlreadyExists), HandleGameAlreadyExists);
                connection.On<GameJoined>(nameof(GameJoined), HandleGameJoined);
                connection.On<GameDoesNotExist>(nameof(GameDoesNotExist), HandleGameDoesNotExist);
            }

            private async Task OnClosed(Exception arg)
            {
                if (Interlocked.CompareExchange(ref alreadyClosed, arg, null) == null)
                {
                    var myOnClosed = onClosed;
                    if (myOnClosed != null
                         && ConnectionLost(getter.task))
                    {
                        await myOnClosed(arg);
                    }

                }
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

            public async Task<OrType<GameCreated, GameAlreadyExists, Exception>> Send(CreateGame createGame)
            {
                var taskCompletionSource = new TaskCompletionSource<OrType<GameCreated, GameAlreadyExists, Exception>>();

                (Action<GameCreated>, Action<GameAlreadyExists>) actions = (null, null);
                actions = (
                   (GameCreated x) =>
                   {
                       if (x.Id == createGame.Id)
                       {
                           taskCompletionSource.SetResult(new OrType<GameCreated, GameAlreadyExists, Exception>(x));
                       }
                   },
                    (GameAlreadyExists x) =>
                    {
                        if (x.Id == createGame.Id)
                        {
                            taskCompletionSource.SetResult(new OrType<GameCreated, GameAlreadyExists, Exception>(x));
                        }
                    }
                );

                gameCreatedHandlers.Add(actions.Item1);
                gameAlreadyExistsHandlers.Add(actions.Item2);

                try
                {
                    await connection.InvokeAsync(nameof(CreateGame), createGame);
                }
                catch (TimeoutException e)
                {
                    taskCompletionSource.SetResult(new OrType<GameCreated, GameAlreadyExists, Exception>(e));
                }
                catch (InvalidOperationException e)
                {
                    taskCompletionSource.SetResult(new OrType<GameCreated, GameAlreadyExists, Exception>(e));
                }
                finally {
                    gameCreatedHandlers.Remove(actions.Item1);
                    gameAlreadyExistsHandlers.Remove(actions.Item2);
                }

                return await taskCompletionSource.Task;
            }

            public async Task<OrType<GameJoined, GameDoesNotExist, Exception>> Send(JoinGame joinGame)
            {
                var taskCompletionSource = new TaskCompletionSource<OrType<GameJoined, GameDoesNotExist, Exception>>();

                (Action<GameJoined>, Action<GameDoesNotExist>) actions = (null, null);
                actions = (
                   (GameJoined x) =>
                   {
                       if (x.Id == joinGame.Id)
                       {
                           taskCompletionSource.SetResult(new OrType<GameJoined, GameDoesNotExist, Exception>(x));
                           gameJoinedHandlers.Remove(actions.Item1);
                           gameDoesNotExistHandlers.Remove(actions.Item2);
                       }
                   },
                    (GameDoesNotExist x) =>
                    {
                        if (x.Id == joinGame.Id)
                        {
                            taskCompletionSource.SetResult(new OrType<GameJoined, GameDoesNotExist, Exception>(x));
                            gameJoinedHandlers.Remove(actions.Item1);
                            gameDoesNotExistHandlers.Remove(actions.Item2);
                        }
                    }
                );

                gameJoinedHandlers.Add(actions.Item1);
                gameDoesNotExistHandlers.Add(actions.Item2);

                try
                {
                    await connection.InvokeAsync(nameof(JoinGame), joinGame);
                }
                catch(TimeoutException e)
                {
                    taskCompletionSource.SetResult(new OrType<GameJoined, GameDoesNotExist, Exception>(e));
                    gameJoinedHandlers.Remove(actions.Item1);
                    gameDoesNotExistHandlers.Remove(actions.Item2);
                }
                catch (InvalidOperationException e)
                {
                    taskCompletionSource.SetResult(new OrType<GameJoined, GameDoesNotExist, Exception>(e));
                    gameJoinedHandlers.Remove(actions.Item1);
                    gameDoesNotExistHandlers.Remove(actions.Item2);
                }

                return await taskCompletionSource.Task;
            }

            public async void Send(string game, 
                CreatePlayer createPlayer, 
                Action<Positions> handlePossitions, 
                Action<ObjectsCreated> handleObjectsCreated, 
                Action<ObjectsRemoved> handleObjectsRemoved,
                Action<UpdateScore> handleUpdateScore)
            {
                connection.On(nameof(Positions), handlePossitions);
                connection.On(nameof(ObjectsCreated), handleObjectsCreated);
                connection.On(nameof(ObjectsRemoved), handleObjectsRemoved);
                connection.On(nameof(UpdateScore), handleUpdateScore);

                try
                {
                    await connection.InvokeAsync(nameof(CreatePlayer), game, createPlayer);
                }
                catch (TimeoutException)
                {
                }
                catch (InvalidOperationException)
                {
                }
            }

            public async void Send(string game, PlayerInputs inputs)
            {
                try
                {
                    await connection.InvokeAsync(nameof(PlayerInputs), game, inputs);
                }
                catch (TimeoutException)
                {
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

    }


}
