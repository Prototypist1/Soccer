using Common;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Prototypist.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
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
            if (instance == null)
            {
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
                //.WithUrl(@"https://soccerserver.azurewebsites.net/GameHub", x=>
                //.WithUrl(@"http://Pyrite:5000/GameHub", x =>
                //.WithUrl(@"http://192.168.1.7:5000/GameHub", x =>
                .WithUrl(@"http://localhost:5000/GameHub", x =>
                {
                    // for some reason this seems to break azure signal r service
                    //x.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                    //x.SkipNegotiation = true;
                })
                .AddMessagePackProtocol()
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

            public void ClearCallBacks()
            {
                connection.Remove(nameof(ObjectsCreated));
                connection.Remove(nameof(ObjectsRemoved));
                connection.Remove(nameof(UpdateScore));
                connection.Remove(nameof(ColorChanged));
                connection.Remove(nameof(NameChanged));
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

            public async Task<OrType<GameCreated, GameJoined, Exception>> Send(CreateOrJoinGame createOrJoinGame)
            {
                var taskCompletionSource = new TaskCompletionSource<OrType<GameCreated, GameJoined, Exception>>();

                (Action<GameCreated>, Action<GameJoined>) actions = (null, null);
                actions = (
                   (GameCreated x) =>
                   {
                       if (x.Id == createOrJoinGame.Id)
                       {
                           taskCompletionSource.SetResult(new OrType<GameCreated, GameJoined, Exception>(x));
                       }
                   },
                    (GameJoined x) =>
                    {
                        if (x.Id == createOrJoinGame.Id)
                        {
                            taskCompletionSource.SetResult(new OrType<GameCreated, GameJoined, Exception>(x));
                        }
                    }
                );

                gameCreatedHandlers.Add(actions.Item1);
                gameJoinedHandlers.Add(actions.Item2);

                try
                {
                    await connection.InvokeAsync(nameof(CreateOrJoinGame), createOrJoinGame);
                    return await taskCompletionSource.Task;
                }
                catch (TimeoutException e)
                {
                    taskCompletionSource.SetResult(new OrType<GameCreated, GameJoined, Exception>(e));
                    return await taskCompletionSource.Task;
                }
                catch (InvalidOperationException e)
                {
                    taskCompletionSource.SetResult(new OrType<GameCreated, GameJoined, Exception>(e));
                    return await taskCompletionSource.Task;
                }
                finally
                {
                    gameCreatedHandlers.Remove(actions.Item1);
                    gameJoinedHandlers.Remove(actions.Item2);
                }

            }

            public void SetCallBacks(IGameView  gameView) {

                connection.On<ObjectsCreated>(nameof(ObjectsCreated), gameView.HandleObjectsCreated);
                connection.On< ObjectsRemoved>(nameof(ObjectsRemoved), gameView.HandleObjectsRemoved);
                connection.On< UpdateScore>(nameof(UpdateScore), gameView.HandleUpdateScore);
                connection.On< ColorChanged>(nameof(ColorChanged), gameView.HandleColorChanged);
                connection.On< NameChanged>(nameof(NameChanged), gameView.HandleNameChanged);
                connection.Closed += (x) => { 
                    return Task.CompletedTask; 
                };
            }

            public async void Send(string game,
                CreatePlayer createPlayer)
            {
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

            public async void Send(LeaveGame leave)
            {
                try
                {
                    await connection.InvokeAsync(nameof(LeaveGame), leave);
                }
                catch (TimeoutException)
                {
                }
                catch (InvalidOperationException)
                {
                }
                catch (Exception e) { 
                
                }
            }


            public async void Send(string game, IAsyncEnumerable<PlayerInputs> inputs)
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
                catch (Exception e) {
                
                }
            }

            public async void Send(ResetGame inputs)
            {
                try
                {
                    await connection.InvokeAsync(nameof(ResetGame), inputs);
                }
                catch (TimeoutException)
                {
                }
                catch (InvalidOperationException)
                {
                }
            }

            public async void Send(string game, ColorChanged colorChanged)
            {
                try
                {
                    await connection.InvokeAsync(nameof(ColorChanged), game, colorChanged);
                }
                catch (TimeoutException)
                {
                }
                catch (InvalidOperationException)
                {
                }
            }

            public async void Send(string game, NameChanged nameChanged)
            {
                try
                {
                    await connection.InvokeAsync(nameof(NameChanged), game, nameChanged);
                }
                catch (TimeoutException)
                {
                }
                catch (InvalidOperationException)
                {
                }
            }
            public IAsyncEnumerable<Positions> JoinChannel(JoinChannel joinChannel)
            {
                return connection.StreamAsync<Positions>(nameof(JoinChannel), joinChannel);
            }
        }
    }
}
