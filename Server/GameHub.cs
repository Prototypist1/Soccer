using Common;
using Microsoft.AspNetCore.SignalR;

namespace Server
{
    public class GameHub : Hub
    {
        public void PlayerInputs(PlayerInputs playerInputs)
        {
        }

        public void Positions(Positions positions)
        {
            Clients.All.SendAsync(nameof(Positions), positions);
        }
    }
}
