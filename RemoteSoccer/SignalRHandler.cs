using Common;
using Microsoft.AspNetCore.SignalR.Client;
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

        public static Task<SignalRHandler> Get() {
            var getter = new Getter();
            getter.GotIt(Interlocked.CompareExchange(ref instance, getter.task, null) == null);
            return instance;
        }

        private class Getter
        {
            private TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
            internal Task<SignalRHandler> task;

            public void GotIt(bool gotIt) {
                taskCompletionSource.SetResult(gotIt);
            }


            public Getter()
            {
                task = MakeTask();
            }

            private async Task<SignalRHandler> MakeTask() {
                var gotIt = await taskCompletionSource.Task;
                if (gotIt) {
                    return await SignalRHandler.Create();
                }
                else {
                    return await SingleSignalRHandler.instance;
                }
            }


        }
    }

    class SignalRHandler
    {

        public static async Task<SignalRHandler> Create() {

            var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:50737/GameHub")
                .Build();

            connection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await connection.StartAsync();
            };


            await connection.StartAsync();

            return new SignalRHandler(connection);
        }

        private readonly HubConnection connection;
        public SignalRHandler(HubConnection connection)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public async void Send(CreateGame createGame, Action<Positions> handlePossitions, Action<ObjectsCreated> handleObjectsCreated)
        {
            connection.On(nameof(Positions), handlePossitions);
            connection.On(nameof(ObjectsCreated), handleObjectsCreated);

            await connection.InvokeAsync(nameof(CreateGame), createGame);
        }


        public async void Send(Guid game, CreatePlayer createPlayer)
        {
            await connection.InvokeAsync(nameof(CreatePlayer),game, createPlayer);
        }

        public async void Send(Guid game, PlayerInputs inputs)
        {
            await connection.InvokeAsync(nameof(PlayerInputs), game, inputs);   
        }
    }
}
