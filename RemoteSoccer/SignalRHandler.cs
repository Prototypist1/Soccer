using Common;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteSoccer
{
    class SignalRHandler
    {

        public static async Task<SignalRHandler> Create(Action<Positions> handlePossitions, Action<ObjectsCreated> handleObjectsCreated) {

            var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:53353/GameHub")
                .Build();

            // TODO more!
            connection.On(nameof(Positions), handlePossitions);
            connection.On(nameof(ObjectsCreated), handleObjectsCreated);

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

        public async void Send(CreateGame createGame)
        {
            await connection.InvokeAsync(nameof(CreateGame), createGame);
        }


        public async void Send(Guid game, CreatePlayer createPlayer)
        {
            await connection.InvokeAsync(nameof(CreatePlayer),game, createPlayer);
        }

        public async void Send(Guid game, PlayerInputs inputs)
        {
            await connection.InvokeAsync(nameof(PlayerInputs), inputs);   
        }
    }
}
