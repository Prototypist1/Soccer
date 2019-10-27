using Common;
using Microsoft.Toolkit.Uwp.UI.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RemoteSoccer
{
    // poor mans pointer
    public class FrameRef {
        public int frame =0;
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        IGameView rge;

        private readonly Guid body = Guid.NewGuid();
        private readonly Guid foot = Guid.NewGuid();

        private bool lockCurser = true;
        private Zoomer zoomer;
        private FrameRef frame =new FrameRef();

        public MainPage()
        {
            this.InitializeComponent();


            Window.Current.CoreWindow.KeyUp += Menu_KeyUp;

            zoomer = new Zoomer(GameHolder.ActualWidth,GameHolder.ActualHeight, body);

            rge = new RenderGameEvents(GameArea, Fps, LeftScore, RightScore, zoomer, frame);
            
            if (lockCurser)
            {
                Window.Current.CoreWindow.PointerCursor = null;
            }
            else
            {
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
            }

        }

        private async Task OnDisconnect(Exception ex)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () =>
                    {
                        this.Frame.Navigate(typeof(LandingPage));
                    });

        }

        private bool sending = true;
        private TaskCompletionSource<bool> StoppedSending = new TaskCompletionSource<bool>();
        private IGame game;

        // assumed to be run on the main thread
        private async IAsyncEnumerable<PlayerInputs> MainLoop(Guid foot, Guid body)
        {
            double lastX = 0, lastY = 0, bodyX = 0, bodyY = 0, footX = 0, footY = 0;
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        () =>
                        {
                            var pointer = CoreWindow.GetForCurrentThread().PointerPosition;

                            lastX = pointer.X;
                            lastY = pointer.Y;
                        });


            var sw = new Stopwatch();
            sw.Start();

            var distrib = new int[100];

            while (sending)
            {

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () =>
                    {
                        var coreWindow = Window.Current.CoreWindow;

                        if (lockCurser)
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

                        }
                        else
                        {
                            var point = CoreWindow.GetForCurrentThread().PointerPosition;

                            lastX = point.X;
                            lastY = point.Y;

                            footX = 0; footY = 0; bodyX = 0; bodyY = 0;
                        }

                    });
            
                

                yield return new PlayerInputs(footX, footY, bodyX, bodyY, foot, body);
                frame.frame++;


                while ((1000.0 * frame.frame / 75.0) > sw.ElapsedMilliseconds) { 
                
                }

                //await Task.Delay(1);
                // let someone else have a go
                //await Task.Delay((int)Math.Max(0, (1000.0 * frame.frame / 60.0) - sw.ElapsedMilliseconds));

            }
            StoppedSending.SetResult(true);
        }



        private void GameHolder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            zoomer.UpdateWindow(GameHolder.ActualWidth, GameHolder.ActualHeight);
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var gameName =(string)e.Parameter;

            Task.Run(async () =>
            {
                try
                {
                    //game = new LocalGame();
                    game = new RemoteGame(gameName, await SingleSignalRHandler.GetOrThrow());
                    game.OnDisconnect(OnDisconnect);
                    game.SetCallbacks(rge);

                    var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;


                    var color = new byte[3];

                    if (localSettings.Values.TryGetValue(LocalSettingsKeys.PlayerColorR, out var r) &&
                        localSettings.Values.TryGetValue(LocalSettingsKeys.PlayerColorG, out var g) &&
                        localSettings.Values.TryGetValue(LocalSettingsKeys.PlayerColorB, out var b))
                    {
                        color[0] = (byte)r;
                        color[1] = (byte)g;
                        color[2] = (byte)b;
                    }
                    else
                    {
                        var random = new Random();

                        while (color[0] + color[1] + color[2] < (0xCC) || color[0] + color[1] + color[2] > (0x143))
                        {
                            random.NextBytes(color);
                        }
                    }

                    var name = "";

                    
                    if (localSettings.Values.TryGetValue(LocalSettingsKeys.PlayerName, out var savedName)) {
                        name = (string)savedName;
                    }


                    game.CreatePlayer(
                        new CreatePlayer(
                            foot,
                            body,
                            Constants.footLen * 2,
                            Constants.PlayerRadius *2,
                            color[0],
                            color[1],
                            color[2],
                            0x20,
                            color[0],
                            color[1],
                            color[2],
                            0xff, 
                            name));


                    var dontWait = rge.SpoolPositions(game.JoinChannel(new JoinChannel(game.GameName)));

                    game.StreamInputs(MainLoop(foot, body));
                }
                catch (Exception ex)
                {
                    await OnDisconnect(ex);
                }
            });

        }

        private void GameHolder_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var delta = e.GetCurrentPoint(GameHolder).Properties.MouseWheelDelta;

            zoomer.SetTimes(zoomer.GetTimes() * Math.Pow(1.1, delta / 100.0));
        }

        private void ColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            var color = ColorPicker.Color;


            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values[LocalSettingsKeys.PlayerColorR] = color.R;
            localSettings.Values[LocalSettingsKeys.PlayerColorG] = color.G;
            localSettings.Values[LocalSettingsKeys.PlayerColorB] = color.B;

            game.ChangeColor(new ColorChanged(foot, color.R, color.G, color.B, 0xff));
            game.ChangeColor(new ColorChanged(body, color.R, color.G, color.B, 0x20));
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenu();
        }

        private void ToggleMenu()
        {
            Menu.Visibility = (Windows.UI.Xaml.Visibility)(((int)Menu.Visibility + 1) % 2);
            lockCurser = !lockCurser;
            if (lockCurser)
            {
                Window.Current.CoreWindow.PointerCursor = null;
            }
            else
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
            }
        }


        private void Menu_KeyUp(CoreWindow sender, KeyEventArgs e)
        {
            if (e.VirtualKey == VirtualKey.Escape)
            {
                ToggleMenu();
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var name = Namer.Text;

            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values[LocalSettingsKeys.PlayerName] = name;

            game.NameChanged(new NameChanged(body, name));
        }

        public async Task StopSendingInputs() {
            sending = false;
            await StoppedSending.Task;
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            await StopSendingInputs();
            game.LeaveGame(new LeaveGame());
            game.OnDisconnect(null);
            game.ClearCallbacks();

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    this.Frame.Navigate(typeof(LandingPage));
                });
        }
    }
}
