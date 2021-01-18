﻿using Common;
using Microsoft.Graphics.Canvas.UI.Xaml;
using physics2;
using Prototypist.TaskChain;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Gaming.Input;
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

    class PlayerInfo
    {

        //public readonly Guid body;
        //public readonly Guid foot;
        public readonly IInputs input;

        public PlayerInfo(/*Guid body, Guid foot,*/ IInputs input, Guid localId)
        {
            //this.body = body;
            //this.foot = foot;
            this.input = input ?? throw new ArgumentNullException(nameof(input));
            LocalId = localId;
        }

        public Guid LocalId { get; }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const int BodyA = 0x40;
        //RenderGameEvents rge;

        private JumpBallConcurrent<HashSet<PlayerInfo>> localPlayers = new JumpBallConcurrent<HashSet<PlayerInfo>>(new HashSet<PlayerInfo>());

        private Ref<bool> lockCurser = new Ref<bool>(true);
        private FullField zoomer;
        private RenderGameState2 renderGameState;
        private Ref<int> frame = new Ref<int>(0);
        private FieldDimensions fieldDimensions = FieldDimensions.Default;

        //private IInputs inputs;
        public MainPage()
        {
            this.InitializeComponent();
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
        private Game2 game;

        // assumed to be run on the main thread
        private async void MainLoop()
        {
            var sw = new Stopwatch();
            sw.Start();

            while (sending)
            {
                game.ApplyInputs( 
                    (await Task.WhenAll(localPlayers.Read().Select(x=>(x.input.Next())).ToArray())).ToDictionary(x=>x.Id, x=>x));
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () =>
                    {
                        Canvas.Invalidate();
                        //renderGameState.Update(game.gameState);
                    });

                frame.thing++;

                while ((1000.0 * frame.thing / 120.0) > sw.ElapsedMilliseconds)
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


            Window.Current.CoreWindow.KeyUp += Menu_KeyUp;

            //zoomer = new ShowAllPositions(GameHolder.ActualWidth, GameHolder.ActualHeight, fieldDimensions);


            //rge = new RenderGameEvents(GameArea, Fps, LeftScore, RightScore, zoomer, frame, fieldDimensions);

            //if (lockCurser.Thing)
            //{
            //    Window.Current.CoreWindow.PointerPosition = new Windows.Foundation.Point(
            //        (Window.Current.CoreWindow.Bounds.Left + Window.Current.CoreWindow.Bounds.Right) / 2.0,
            //        (Window.Current.CoreWindow.Bounds.Top + Window.Current.CoreWindow.Bounds.Bottom) / 2.0);
            //    Window.Current.CoreWindow.PointerCursor = null;
            //}
            //else
            //{
            //    Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
            //}

            var gameInfo =  (GameInfo)e.Parameter;

            var gameName = gameInfo.gameName;

            var controlScheme = gameInfo.controlScheme;

            game = new Game2();

            zoomer = new FullField(GameHolder.ActualWidth, GameHolder.ActualHeight, fieldDimensions.xMax / 2.0, fieldDimensions.yMax / 2.0);
            //renderGameState = new RenderGameState(GameArea, zoomer, LeftScore, RightScore);
            renderGameState = new RenderGameState2(Canvas, zoomer, LeftScore, RightScore);

            Task.Run(async () =>
            {
                try
                {
                    foreach (var gamePad in Windows.Gaming.Input.Gamepad.Gamepads)
                    {
                        await CreatePlayer(gamePad);
                    }

                    Windows.Gaming.Input.Gamepad.GamepadAdded += Gamepad_GamepadAdded;
                    Windows.Gaming.Input.Gamepad.GamepadRemoved += Gamepad_GamepadRemoved;

                    MainLoop();
                }
                catch (Exception ex)
                {
                    await OnDisconnect(ex);
                }
            });


        }


        //private async IAsyncEnumerable<PlayerInputs> InterseptInputs(IAsyncEnumerable<PlayerInputs> x)
        //{
        //    await foreach(var item in x)
        //    {
        //        yield return item;
        //    }
        //}

        private void Gamepad_GamepadRemoved(object sender, Windows.Gaming.Input.Gamepad e)
        {
            RemovePlayer(e);
        }

        private void RemovePlayer(Gamepad e)
        {
            localPlayers.Run(x => { 
                var playerInfo= x.SingleOrDefault(x => x.input is ControllerInputes controllerInputes && controllerInputes.gamepad == e);
                if (playerInfo != null)
                {
                    x.Remove(playerInfo);
                    game.LeaveGame(new RemovePlayerEvent(playerInfo.LocalId));
                }
                return x;
            });
        }

        private void Gamepad_GamepadAdded(object sender, Windows.Gaming.Input.Gamepad e)
        {
            CreatePlayer(e);
        }

        // these do not really work with multiple players
        //readonly Guid body = Guid.NewGuid();
        //readonly Guid outer = Guid.NewGuid();
        //readonly Guid foot = Guid.NewGuid();

        //private async Task CreatePlayer(ControlScheme controlScheme, Guid id)
        //{
        //    IInputs inputs = null;

        //    switch (controlScheme)
        //    {
        //        case ControlScheme.MouseAndKeyboard:
        //            inputs = new MouseKeyboardInputs(lockCurser, game, id);
        //            break;
        //        case ControlScheme.SipmleMouse:
        //            var (temp, refx, refy) = SimpleMouseInputs.Create(lockCurser, game, body, foot, fieldDimensions.xMax / 2.0, fieldDimensions.yMax / 2.0);
        //            inputs = temp;
        //            rge.InitMouse(refx, refy);
        //            break;
        //        case ControlScheme.Controller:
        //            throw new NotImplementedException();
        //            break;
        //        default:
        //            break;
        //    }


        //    await inputs.Init();

        //    var newPlayer = new PlayerInfo(/*body, foot,*/ inputs, id);

        //    var added = false;
        //    while (!added)
        //    {
        //        try
        //        {
        //            localPlayers.Run(x => { x.Add(newPlayer); return x; });
        //            added = true;
        //        }
        //        catch (Exception e)
        //        {
        //            var db = 0;
        //        }
        //    }

        //    //var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        //    var color = GetColor();
        //    //}

        //    //var name = "";


        //    //if (localSettings.Values.TryGetValue(LocalSettingsKeys.PlayerName, out var savedName))
        //    //{
        //    //    name = (string)savedName;
        //    //}


        //    game.CreatePlayer(
        //        new AddPlayerEvent(id,
        //            "",
        //            BodyA,
        //            color[0],
        //            color[1],
        //            color[2],
        //            0xff,
        //            color[0],
        //            color[1],
        //            color[2],
        //            new Physics2.Vector(fieldDimensions.xMax/2.0, fieldDimensions.yMax/2.0)
        //            ));
        //}

        private async Task CreatePlayer(Windows.Gaming.Input.Gamepad gamepad)
        {
            var body = Guid.NewGuid();

            var inputs = new ControllerInputes(/*lockCurser,*/ body, /*foot,*/ gamepad,
                () =>
                {
                    var color = GetColor();
                    game.UpdatePlayer(new UpdatePlayerEvent(body, "", BodyA, color[0], color[1], color[2], 0xff, color[0], color[1], color[2]));
                });

            await inputs.Init();

            var newPlayer = new PlayerInfo(inputs, body);

            var added = false;
            while (!added)
            {
                try
                {
                    localPlayers.Run(x => { x.Add(newPlayer); return x; });
                    added = true;
                }
                catch (Exception e)
                {
                    var db = 0;
                }
            }

            //var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var color = GetColor();
            //}

            //var name = "";


            //if (localSettings.Values.TryGetValue(LocalSettingsKeys.PlayerName, out var savedName))
            //{
            //    name = (string)savedName;
            //}

            game.CreatePlayer(
                new AddPlayerEvent(body,
                    "",
                    BodyA,
                    color[0],
                    color[1],
                    color[2],
                    0xff,
                    color[0],
                    color[1],
                    color[2],
                    new Physics2.Vector(fieldDimensions.xMax / 2.0, fieldDimensions.yMax / 2.0)
                    ));
        }

        private static byte[] GetColor()
        {
            var color = new byte[3];

            //if (localSettings.Values.TryGetValue(LocalSettingsKeys.PlayerColorR, out var r) &&
            //    localSettings.Values.TryGetValue(LocalSettingsKeys.PlayerColorG, out var g) &&
            //    localSettings.Values.TryGetValue(LocalSettingsKeys.PlayerColorB, out var b))
            //{
            //    color[0] = (byte)r;
            //    color[1] = (byte)g;
            //    color[2] = (byte)b;
            //}
            //else
            //{
            var random = new Random();

            while (color[0] + color[1] + color[2] < (0xCC) || color[0] + color[1] + color[2] > (0x143))
            {
                random.NextBytes(color);
            }

            return color;
        }

        private void GameHolder_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var delta = e.GetCurrentPoint(GameHolder).Properties.MouseWheelDelta;

            zoomer.SetTimes(zoomer.GetTimes() * Math.Pow(1.1, delta / 100.0));
        }

        private void ColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            //var color = ColorPicker.Color;


            //var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            //localSettings.Values[LocalSettingsKeys.PlayerColorR] = color.R;
            //localSettings.Values[LocalSettingsKeys.PlayerColorG] = color.G;
            //localSettings.Values[LocalSettingsKeys.PlayerColorB] = color.B;

            //game.ChangeColor(new ColorChanged(foot, color.R, color.G, color.B, 0xff));
            //game.ChangeColor(new ColorChanged(body, color.R, color.G, color.B, BodyA));

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenu();
        }

        private void ToggleMenu()
        {
            Menu.Visibility = (Windows.UI.Xaml.Visibility)(((int)Menu.Visibility + 1) % 2);
            ToggleCurser();
        }

        private void ToggleCurser() {
            lockCurser.thing = !lockCurser.Thing;
            if (lockCurser.thing)
            {
                Window.Current.CoreWindow.PointerPosition = new Windows.Foundation.Point(
                    (Window.Current.CoreWindow.Bounds.Left + Window.Current.CoreWindow.Bounds.Right) / 2.0,
                    (Window.Current.CoreWindow.Bounds.Top + Window.Current.CoreWindow.Bounds.Bottom) / 2.0);
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
            //var name = Namer.Text;

            //var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            //localSettings.Values[LocalSettingsKeys.PlayerName] = name;

            //game.NameChanged(new NameChanged(body, name));
        }

        public async Task StopSendingInputs()
        {
            sending = false;
            await StoppedSending.Task;
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            await StopSendingInputs();
            PlayerInfo player = null;
            localPlayers.Run(x => { player = x.Single(); return x; });

            game.LeaveGame(new RemovePlayerEvent(player.LocalId));
            //game.OnDisconnect(null);
            //game.ClearCallbacks();

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    this.Frame.Navigate(typeof(LandingPage));
                });
        }

        private void Canvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            renderGameState.Update(game.gameState, args);
        }

        // got this from https://microsoft.github.io/Win2D/html/QuickStart.htm
        // they say it avoids memory leaks
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            this.Canvas.RemoveFromVisualTree();
            this.Canvas = null;
        }
    }
}
