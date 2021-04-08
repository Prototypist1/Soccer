using Common;
using Physics2;
using System;
using System.Threading.Tasks;
using Windows.Gaming.Input;

namespace RemoteSoccer
{
    class ControllerInputes : IInputs
    {
        private readonly Guid id;

        //private readonly IReadonlyRef<bool> lockCurser;

        //private readonly Guid body;
        //private readonly Guid foot;
        public readonly Gamepad gamepad;
        private readonly Action changeColor;

        public ControllerInputes(
            //IReadonlyRef<bool> lockCurser,
            //Guid body,
            //Guid foot,
            Guid id,
            Gamepad gamepad,
            Action changeColor)
        {
            this.id = id;
            //this.lockCurser = lockCurser ?? throw new ArgumentNullException(nameof(lockCurser));
            //this.body = body;
            //this.foot = foot;
            this.gamepad = gamepad;
            this.changeColor = changeColor;
        }

        public Task Init()
        {
            return Task.CompletedTask;
        }

        bool lastA = false;

        //bool lastBoost = false;

        Guid boostPressed = Constants.NoMove;

        //double lastRightX, lastRightY;

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
            else
            {
                lastA = false;
            }

            if (boostPressed == Constants.NoMove && (snap.Buttons & GamepadButtons.RightShoulder) == GamepadButtons.RightShoulder)
            {
                boostPressed = Guid.NewGuid();
            }
            if (boostPressed != Constants.NoMove && (snap.Buttons & GamepadButtons.RightShoulder) == 0)
            {
                boostPressed = Constants.NoMove;
            }

            var throwing = snap.RightTrigger >0;

            var right = new Vector(snap.RightThumbstickX, -snap.RightThumbstickY);
            if (right.Length > 1)
            {
                right = right.NewUnitized();
            }

            var left = new Vector(snap.LeftThumbstickX, -snap.LeftThumbstickY);
            if (left.Length > 1)
            {
                left = left.NewUnitized();
            }

            return Task.FromResult(new PlayerInputs(right.x, right.y, left.x, left.y, id, ControlScheme.Controller, throwing, boostPressed));
            //}
            //else
            //{
            //    return Task.FromResult( new PlayerInputs(0, 0, 0, 0, foot, body,true, false));
            //}
        }
    }

}
