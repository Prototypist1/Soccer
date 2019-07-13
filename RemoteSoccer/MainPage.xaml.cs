using Common;
using Physics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RemoteSoccer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly Dictionary<Guid, UIElement> elements = new Dictionary<Guid, UIElement>();

        public MainPage()
        {
            this.InitializeComponent();
            Task.Run(async () =>
            {
                await Task.Delay(5000);
                var handler = await SignalRHandler.Create(
                    HandlePositions,
                    HandleObjectsCreated);
                var game = Guid.NewGuid();

                handler.Send(new CreateGame(game));

                var foot = Guid.NewGuid();
                var body = Guid.NewGuid();
                handler.Send(game, new CreatePlayer(foot,body));

                StartReadingInput(handler,foot,body, game);
            });
        }

        private void HandleObjectsCreated(ObjectsCreated objectsCreated) {
            foreach (var objectCreated in objectsCreated.objects)
            {
                if (!elements.ContainsKey(objectCreated.id)) {
                    var ellispe = new Ellipse()
                    {
                        Width = 80,
                        Height = 80,
                        Fill = new SolidColorBrush(Colors.Black),
                    };
                    elements.Add(objectCreated.id, ellispe);
                    GameArea.Children.Add(ellispe);
                }
            }

        }

        private void HandlePositions(Positions positions) {
            foreach (var position in positions.positions)
            {
                if (elements.TryGetValue(position.Id, out var element)) {
                    Canvas.SetLeft(element, position.X);
                    Canvas.SetTop(element, position.Y);
                }
            }
        }

        private void StartReadingInput(SignalRHandler handler, Guid foot, Guid body, Guid game)
        {
            var frame = 0;

            double lastX=0, lastY=0;

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            while (true)
            {

                var point = CoreWindow.GetForCurrentThread().PointerPosition;


                var footX = frame == 0 ? 0 : point.X - lastX;
                var footY = frame == 0 ? 0 : point.Y - lastY;

                lastX = point.X;
                lastY = point.Y;

                var bodyX =
                        (Window.Current.CoreWindow.GetKeyState(VirtualKey.A).HasFlag(CoreVirtualKeyStates.Down) ? -1.0 : 0.0) +
                        (Window.Current.CoreWindow.GetKeyState(VirtualKey.D).HasFlag(CoreVirtualKeyStates.Down) ? 1.0 : 0.0);
                var bodyY =
                        (Window.Current.CoreWindow.GetKeyState(VirtualKey.W).HasFlag(CoreVirtualKeyStates.Down) ? -1.0 : 0.0) +
                        (Window.Current.CoreWindow.GetKeyState(VirtualKey.S).HasFlag(CoreVirtualKeyStates.Down) ? 1.0 : 0.0);

                handler.Send(game,
                    new PlayerInputs(frame, footX, footY, bodyX, bodyY, foot, body));

                frame++;

                while (stopWatch.ElapsedMilliseconds < ((1000 * frame)/ 16.0))
                {
                }
            }

        }

    }
}
