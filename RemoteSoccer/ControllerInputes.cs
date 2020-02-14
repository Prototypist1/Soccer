using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Gaming.Input;

namespace RemoteSoccer
{
    class ControllerInputes : IInputs
    {

        private readonly IReadonlyRef<bool> lockCurser;

        private readonly Guid body;
        private readonly Guid foot;
        public readonly Gamepad gamepad;

        public ControllerInputes(IReadonlyRef<bool> lockCurser, Guid body, Guid foot, Gamepad gamepad)
        {
            this.lockCurser = lockCurser ?? throw new ArgumentNullException(nameof(lockCurser));
            this.body = body;
            this.foot = foot;
            this.gamepad = gamepad;
        }

        public Task Init()
        {
            return Task.CompletedTask;
        }

        public Task<PlayerInputs> Next()
        {
            
            if (lockCurser.Thing)
            {
                var snap = gamepad.GetCurrentReading();

                return Task.FromResult(new PlayerInputs(snap.LeftThumbstickX, -snap.LeftThumbstickY, snap.RightThumbstickX, -snap.RightThumbstickY, foot, body,true));
            }
            else
            {
                return Task.FromResult( new PlayerInputs(0, 0, 0, 0, foot, body,true));
            }
        }
    }

}
