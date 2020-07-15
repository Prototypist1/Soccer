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
        private readonly IGame game;
        double lastX = 0, lastY = 0;
        private readonly Guid body;
        private readonly Guid foot;

        public MouseKeyboardInputs(IReadonlyRef<bool> lockCurser, IGame game, Guid body, Guid foot)
        {
            this.lockCurser = lockCurser ?? throw new ArgumentNullException(nameof(lockCurser));
            this.game = game ?? throw new ArgumentNullException(nameof(game));
            this.body = body;
            this.foot = foot;
        }

        public async Task Init()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    var pointer = CoreWindow.GetForCurrentThread().PointerPosition;


                    CoreWindow.GetForCurrentThread().PointerPressed += MouseKeyboardInputs_PointerPressed;
                    CoreWindow.GetForCurrentThread().PointerReleased += MouseKeyboardInputs_PointerReleased;

                    lastX = pointer.X;
                    lastY = pointer.Y;
                });
        }

        bool mouseDown = false;

        private void MouseKeyboardInputs_PointerReleased(CoreWindow sender, PointerEventArgs args)
        {
            mouseDown = false;
        }
        private void MouseKeyboardInputs_PointerPressed(CoreWindow sender, PointerEventArgs args)
        {
            mouseDown = true;
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
                                    game.ResetGame(new ResetGame(game.GameName));
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
                                res = new PlayerInputs(footX, footY, bodyX, bodyY, foot, body, false, mouseDown);

                            }
                            else
                            {
                                var point = CoreWindow.GetForCurrentThread().PointerPosition;

                                lastX = point.X;
                                lastY = point.Y;

                                res = new PlayerInputs(0, 0, 0, 0, foot, body, false, false);
                            }

                        });
            return res;
        }
    }
}
