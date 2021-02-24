using Common;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace RemoteSoccer
{

    class MouseKeyboardInputs : IInputs
    {
        private readonly IReadonlyRef<bool> lockCurser;
        double lastX = 0, lastY = 0;
        //private readonly Guid body;
        //private readonly Guid foot;
        private readonly Guid id;

        bool lastBoost = false;

        public MouseKeyboardInputs(IReadonlyRef<bool> lockCurser, Guid id)
        {
            this.lockCurser = lockCurser ?? throw new ArgumentNullException(nameof(lockCurser));
            this.id = id;
        }

        public async Task Init()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    var pointer = CoreWindow.GetForCurrentThread().PointerPosition;


                    // this is a gross hack
                    // and inputs will probably lost if the curser ever doesn't move 😢
                    CoreWindow.GetForCurrentThread().PointerMoved += MouseKeyboardInputs_PointerPressed;
                    CoreWindow.GetForCurrentThread().PointerReleased += MouseKeyboardInputs_PointerPressed;
                    CoreWindow.GetForCurrentThread().PointerPressed += MouseKeyboardInputs_PointerPressed;


                    lastX = pointer.X;
                    lastY = pointer.Y;
                });
        }

        bool throwing = false;
        Guid boostPressed = Constants.NoMove;

        //private void MouseKeyboardInputs_PointerReleased(CoreWindow sender, PointerEventArgs args)
        //{
        //    throwing = args.CurrentPoint.Properties.IsLeftButtonPressed;
        //    boostPressed = args.CurrentPoint.Properties.IsRightButtonPressed;
        //}
        private void MouseKeyboardInputs_PointerPressed(CoreWindow sender, PointerEventArgs args)
        {
            throwing = args.CurrentPoint.Properties.IsLeftButtonPressed;
            if (boostPressed == Constants.NoMove && args.CurrentPoint.Properties.IsRightButtonPressed) {
                boostPressed = Guid.NewGuid();
            }
            if (boostPressed != Constants.NoMove && !args.CurrentPoint.Properties.IsRightButtonPressed) {
                boostPressed = Constants.NoMove;
            }
        }

        public async Task<PlayerInputs> Next()
        {

            double bodyX = 0, bodyY = 0, footX = 0, footY = 0;
            PlayerInputs res = default;
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        () =>
                        {
                            var coreWindow = Window.Current.CoreWindow;

                            if (lockCurser.Thing)
                            {
                                if (coreWindow.GetKeyState(VirtualKey.R).HasFlag(CoreVirtualKeyStates.Down))
                                {
                                    // reset 
                                    throw new NotImplementedException();
                                }

                                bodyX =
                                    (coreWindow.GetKeyState(VirtualKey.A).HasFlag(CoreVirtualKeyStates.Down) ? -1.0 : 0.0) +
                                    (coreWindow.GetKeyState(VirtualKey.D).HasFlag(CoreVirtualKeyStates.Down) ? 1.0 : 0.0);
                                bodyY =
                                    (coreWindow.GetKeyState(VirtualKey.W).HasFlag(CoreVirtualKeyStates.Down) ? -1.0 : 0.0) +
                                    (coreWindow.GetKeyState(VirtualKey.S).HasFlag(CoreVirtualKeyStates.Down) ? 1.0 : 0.0);


                                var point = CoreWindow.GetForCurrentThread().PointerPosition;
                                footX = (point.X - lastX);// * .75;
                                footY = (point.Y - lastY);// * .75;

                                point = new Point(lastX, lastY);
                                coreWindow.PointerPosition = point;

                                lastX = point.X;
                                lastY = point.Y;
                                res = new PlayerInputs(footX * 10, footY * 10, bodyX, bodyY, id, ControlScheme.MouseAndKeyboard, throwing, boostPressed);

                            }
                            else
                            {
                                var point = CoreWindow.GetForCurrentThread().PointerPosition;

                                lastX = point.X;
                                lastY = point.Y;

                                res = new PlayerInputs(0, 0, 0, 0, id, ControlScheme.MouseAndKeyboard, false, Constants.NoMove);
                            }

                        });
            return res;
        }
    }
}
