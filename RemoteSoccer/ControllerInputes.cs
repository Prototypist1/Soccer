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

        //private readonly IReadonlyRef<bool> lockCurser;

        private readonly Guid body;
        private readonly Guid foot;
        public readonly Gamepad gamepad;
        private readonly Action changeColor;

        public ControllerInputes(
            //IReadonlyRef<bool> lockCurser,
            Guid body,
            Guid foot,
            Gamepad gamepad,
            Action changeColor)
        {
            //this.lockCurser = lockCurser ?? throw new ArgumentNullException(nameof(lockCurser));
            this.body = body;
            this.foot = foot;
            this.gamepad = gamepad;
            this.changeColor = changeColor;
        }

        public Task Init()
        {
            return Task.CompletedTask;
        }

        bool lastA = false;

        public Task<PlayerInputs> Next()
        {
            //if (lockCurser.Thing)
            //{
                var snap = gamepad.GetCurrentReading();

                if ((snap.Buttons & GamepadButtons.A) == GamepadButtons.A)
                {
                    if (!lastA)
                    {
                        changeColor();
                    }
                    lastA = true;
                }
                else {
                    lastA = false;
                }

                return Task.FromResult(new PlayerInputs(snap.RightThumbstickX, -snap.RightThumbstickY, snap.LeftThumbstickX, -snap.LeftThumbstickY, foot, body,true, (snap.Buttons & GamepadButtons.RightShoulder)== GamepadButtons.RightShoulder));
            //}
            //else
            //{
            //    return Task.FromResult( new PlayerInputs(0, 0, 0, 0, foot, body,true, false));
            //}
        }
    }

}
