using Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RemoteSoccer
{
    internal class Game2
    {
        internal void OnDisconnect(Func<Exception, Task> onDisconnect)
        {
            throw new NotImplementedException();
        }

        internal void StreamInputs(IAsyncEnumerable<PlayerInputs> asyncEnumerable)
        {
            throw new NotImplementedException();
        }


        internal void CreatePlayer(AddPlayerEvent createPlayer)
        {
            throw new NotImplementedException();
        }

        internal void LeaveGame(RemovePlayerEvent removePlayerEvent)
        {
            throw new NotImplementedException();
        }

        internal void UpdatePlayer(UpdatePlayerEvent updatePlayerEvent)
        {
            throw new NotImplementedException();
        }
    }
}