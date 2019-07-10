using Common;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soccer
{
    class SignalRHandler
    {

        public static async Task<SignalRHandler> Create() {

            var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:53353/ChatHub")
                .Build();

            connection.On<Positions>(nameof(Positions), x =>
            {

            });

            connection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await connection.StartAsync();
            };


            await connection.StartAsync();

            return new SignalRHandler(connection);
        }

        HubConnection connection;
        public SignalRHandler(HubConnection connection)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        private async void Send(PlayerInputs inputs)
        {
            await connection.InvokeAsync(nameof(PlayerInputs), inputs);   
        }
    }
}
