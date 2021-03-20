using Common;
using System;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace RemoteSoccer
{
    public class GameInfo
    {
        public readonly string gameName;
        public readonly ControlScheme controlScheme;

        public GameInfo(string gameName, ControlScheme controlScheme)
        {
            this.gameName = gameName ?? throw new ArgumentNullException(nameof(gameName));
            this.controlScheme = controlScheme;
        }
    }
}
