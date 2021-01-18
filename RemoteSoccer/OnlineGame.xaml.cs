using Common;
using physics2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
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


        private RenderGameState2 renderGameState;
        private Game2 game;
        private FullField zoomer;
        private FieldDimensions fieldDimensions = FieldDimensions.Default;
        private readonly Guid playerId = Guid.NewGuid();
        private const int BodyA = 0x40;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var gameInfo = (GameInfo)e.Parameter;

            var gameName = gameInfo.gameName;

            var inputs = new MouseKeyboardInputs(new Ref<bool>(false), playerId);

            game = new Game2();

            zoomer = new FullField(GameHolder.ActualWidth, GameHolder.ActualHeight, fieldDimensions.xMax / 2.0, fieldDimensions.yMax / 2.0);
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
                new Physics2.Vector(fieldDimensions.xMax/2.0, fieldDimensions.yMax / 2.0)
                ));

            inputs.Init().ContinueWith(_ => {
                signalRHandler.Send(gameName, Inputs(inputs));
            });

            signalRHandler.SubscribeToGameStateUpdates(x =>
            {
                game.gameState.Handle(x);
                var dontWait = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () =>
                    {
                        Canvas.Invalidate();
                    });
            });
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

        private async IAsyncEnumerable<PlayerInputs> Inputs(MouseKeyboardInputs mouseKeyboardInputs) {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var counter = 0L;
            while (true)
            {
                yield return await mouseKeyboardInputs.Next();
                counter++;
                await Task.Delay((int)(((counter * 1000) /60.0) - stopWatch.ElapsedMilliseconds));
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
