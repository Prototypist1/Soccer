using Common;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Gaming.Input;

namespace RemoteSoccer
{
    class ControllerInputes : IInputs
    {

        private readonly IReadonlyRef<bool> lockCurser;

        private readonly IGame game;
        double lastX = 0, lastY = 0;
        private readonly Guid body;
        private readonly Guid foot;


        public ControllerInputes(IReadonlyRef<bool> lockCurser, IGame game, Guid body, Guid foot)
        {
            this.lockCurser = lockCurser ?? throw new ArgumentNullException(nameof(lockCurser));
            this.game = game ?? throw new ArgumentNullException(nameof(game));
            this.body = body;
            this.foot = foot;
        }

        public Task Init()
        {
            return Task.CompletedTask;
        }

        public Task<PlayerInputs> Next()
        {
            var gamepad = Gamepad.Gamepads?.FirstOrDefault();
            if (lockCurser.Thing && gamepad != null)
            {
                var snap = gamepad.GetCurrentReading();
                    var footX = (snap.LeftThumbstickX - lastX);// * .75;
                    var footY = (snap.LeftThumbstickY - lastY);// * .75;

                    lastX = snap.LeftThumbstickX;
                    lastY = snap.LeftThumbstickY;
                return Task.FromResult(new PlayerInputs((Constants.footLen- Constants.PlayerRadius)* footX, -(Constants.footLen - Constants.PlayerRadius) * footY, snap.RightThumbstickX, -snap.RightThumbstickY, foot, body));
            }
            else
            {
                return Task.FromResult( new PlayerInputs(0, 0, 0, 0, foot, body));
            }
        }
    }
}
