using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RemoteSoccer
{
    public interface IReadonlyRef<T>
    {
        public T Thing { get; }
    }

    // poor mans pointer
    public class Ref<T> : IReadonlyRef<T>
    {

        public T thing;

        public Ref(T thing)
        {
            this.thing = thing;
        }

        public T Thing => thing;
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        IGameView rge;

        private readonly Guid body = Guid.NewGuid();
        private readonly Guid foot = Guid.NewGuid();

        private Ref<bool> lockCurser = new Ref<bool>(true);
        private Zoomer zoomer;
        private Ref<int> frame = new Ref<int>(0);
        private IInputs inputs;
        public MainPage()
        {
            this.InitializeComponent();


            Window.Current.CoreWindow.KeyUp += Menu_KeyUp;

            zoomer = new Zoomer(GameHolder.ActualWidth, GameHolder.ActualHeight, body);

            rge = new RenderGameEvents(GameArea, Fps, LeftScore, RightScore, zoomer, frame);

            if (lockCurser.Thing)
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
            
                await inputs.Init();

                var sw = new Stopwatch();
                sw.Start();

                while (sending)
                {
                    yield return await inputs.Next();
                    frame.thing++;


                    while ((1000.0 * frame.thing / 60.0) > sw.ElapsedMilliseconds)
                    {
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

            var gameName = (string)e.Parameter;

            Task.Run(async () =>
            {
                try
                {
                    game = new LocalGame();
                    //game = new RemoteGame(gameName, await SingleSignalRHandler.GetOrThrow());
                    game.OnDisconnect(OnDisconnect);
                    game.SetCallbacks(rge);
                    inputs = new MouseKeyboardInputs(lockCurser, game, body, foot);
                    //inputs = new ControllerInputes(lockCurser, game, body, foot);

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


                    if (localSettings.Values.TryGetValue(LocalSettingsKeys.PlayerName, out var savedName))
                    {
                        name = (string)savedName;
                    }


                    game.CreatePlayer(
                        new CreatePlayer(
                            foot,
                            body,
                            Constants.footLen * 2,
                            Constants.PlayerRadius * 2,
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
            lockCurser.thing = !lockCurser.thing;
            if (lockCurser.thing)
            {
                Window.Current.CoreWindow.PointerCursor = null;
            }
            else
            {
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
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

        public async Task StopSendingInputs()
        {
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
