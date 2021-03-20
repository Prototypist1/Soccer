using Common;
using physics2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace RemoteSoccer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OnlineGame : Page
    {
        public OnlineGame()
        {
            this.InitializeComponent();
        }


        private Ref<bool> lockCurser = new Ref<bool>(false);
        private RenderGameState2 renderGameState;
        private Game2 game;
        private MouseZoomer zoomer;
        private FieldDimensions fieldDimensions = FieldDimensions.Default;
        private readonly Guid playerId = Guid.NewGuid();
        private const int BodyA = 0x40;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);


            Window.Current.CoreWindow.KeyUp += Menu_KeyUp;
            ToggleCurser();

            var gameInfo = (GameInfo)e.Parameter;

            var gameName = gameInfo.gameName;

            var inputs = new MouseKeyboardInputs(lockCurser, playerId);

            game = new Game2();

            zoomer = new MouseZoomer(GameHolder.ActualWidth, GameHolder.ActualHeight, fieldDimensions, (GameState gs) =>
            {


                return gs.players.Where(x => x.Key == playerId).Select(x => x.Value.PlayerBody.Position)
                .Union(new[] { gs.GameBall.Posistion })
                .ToArray();

            });
            //zoomer = new FullField(GameHolder.ActualWidth, GameHolder.ActualHeight, fieldDimensions.xMax / 2.0, fieldDimensions.yMax / 2.0);
            renderGameState = new RenderGameState2(Canvas, zoomer, LeftScore, RightScore);

            var signalRHandler = SingleSignalRHandler.GetOrThrow();
            var color = GetColor();
            signalRHandler.Send(gameName, new AddPlayerEvent(
                playerId,
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

            inputs.Init().ContinueWith(_ =>
            {
                signalRHandler.Send(gameName, Inputs(inputs));
            });

            var dontWait = MainLoop(signalRHandler.JoinChannel(new JoinChannel(gameName)));
        }

        private void Menu_KeyUp(CoreWindow sender, KeyEventArgs e)
        {
            if (e.VirtualKey == VirtualKey.Escape)
            {
                ToggleCurser();
            }
        }


        private void ToggleCurser()
        {
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

        async Task MainLoop(IAsyncEnumerable<GameStateUpdate> enumerable)
        {
            await foreach (var item in enumerable)
            {
                game.gameState.Handle(item);
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () =>
                    {
                        Canvas.Invalidate();
                    });
            }
        }

        private static byte[] GetColor()
        {
            var color = new byte[3];

            var random = new Random();

            do
            {
                random.NextBytes(color);
            }
            while (color[0] + color[1] + color[2] < (0xCC) || color[0] + color[1] + color[2] > (0x143));

            return color;
        }

        private async IAsyncEnumerable<PlayerInputs> Inputs(MouseKeyboardInputs mouseKeyboardInputs)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var counter = 0L;
            while (true)
            {
                yield return await mouseKeyboardInputs.Next();
                counter++;


                while ((1000.0 * counter / 60.0) > stopWatch.ElapsedMilliseconds)
                {
                }
                //try
                //{
                //    await Task.Delay((int)(((counter * 1000) / 60.0) - stopWatch.ElapsedMilliseconds));
                //}
                //catch (ArgumentOutOfRangeException) { 
                //    // we asked it to delay a negetive time
                //    // oh well
                //}
            }
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

        private void GameHolder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            zoomer.UpdateWindow(GameHolder.ActualWidth, GameHolder.ActualHeight);
        }

    }
}
