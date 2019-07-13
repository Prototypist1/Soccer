using Common;
using Physics;
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
        private class ElementEntry
        {
            public readonly Ellipse element;
            public double X;
            public double Y;
            public double Diameter;

            public ElementEntry(Ellipse element, double x, double y, double diameter)
            {
                this.element = element ?? throw new ArgumentNullException(nameof(element));
                X = x;
                Y = y;
                Diameter = diameter;
            }
        }

        private readonly Dictionary<Guid, ElementEntry> elements = new Dictionary<Guid, ElementEntry>();

        public MainPage()
        {
            this.InitializeComponent();
            var points = new[] {
                (new Vector(210,10) ,new Vector(590,10)),
                (new Vector(10,210) ,new Vector(210,10)),
                (new Vector(10,590) ,new Vector(10,210)),
                (new Vector(210,790),new Vector(10,590)),
                (new Vector(590,790),new Vector(210,790)),
                (new Vector(790,590),new Vector(590,790)),
                (new Vector(790,210),new Vector(790,590)),
                (new Vector(590,10) ,new Vector(790,210))
            };


            foreach (var side in points)
            {
                var line = new Line()
                {
                    X1 = side.Item1.x,
                    X2 = side.Item2.x,
                    Y1 = side.Item1.y,
                    Y2 = side.Item2.y,
                    Stroke = new SolidColorBrush(Colors.Black),
                };

                GameArea.Children.Add(line);
            }

            Task.Run(async () =>
            {
                var handler = await SignalRHandler.Create(
                    HandlePositions,
                    HandleObjectsCreated);
                var game = Guid.NewGuid();

                handler.Send(new CreateGame(game));

                var foot = Guid.NewGuid();
                var body = Guid.NewGuid();
                handler.Send(game, new CreatePlayer(
                    foot,
                    body,
                    400,
                    80,
                    255,
                    0,
                    0,
                    127,
                    255,
                    0,
                    0,
                    255));

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () => MainLoop(handler, foot, body, game));

            });
        }

        private void HandleObjectsCreated(ObjectsCreated objectsCreated)
        {
            var dontwait = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    foreach (var objectCreated in objectsCreated.Objects)
                    {

                        if (!elements.ContainsKey(objectCreated.Id))
                        {
                            var ellispe = new Ellipse()
                            {
                                Width = objectCreated.Diameter,
                                Height = objectCreated.Diameter,
                                Fill = new SolidColorBrush(Color.FromArgb(
                                    objectCreated.A,
                                    objectCreated.R,
                                    objectCreated.G,
                                    objectCreated.B)),
                            };
                            elements.Add(objectCreated.Id, new ElementEntry( ellispe,-1000,-1000, objectCreated.Diameter));
                            GameArea.Children.Add(ellispe);
                        }
                    }
                });
        }

        double framesRecieved = 0;
        int currentDisplayFrame = 0;
        private void HandlePositions(Positions positions)
        {
            framesRecieved++;
            if (positions.Frame > currentDisplayFrame)
            {
                currentDisplayFrame = positions.Frame;
                foreach (var position in positions.PositionsList)
                {
                    if (elements.TryGetValue(position.Id, out var element))
                    {
                        element.X = position.X - (element.Diameter / 2.0);
                        element.Y = position.Y - (element.Diameter / 2.0);
                    }
                }
            }

        }

        // assumed to be run on the main thread
        private async void MainLoop(SignalRHandler handler, Guid foot, Guid body, Guid game)
        {
            try
            {
                var frame = 0;

                double lastX = 0, lastY = 0;

                var stopWatch = new Stopwatch();
                stopWatch.Start();

                while (true)
                {

                    
                    
                    var point = CoreWindow.GetForCurrentThread().PointerPosition;
                    var bodyX =
                        (Window.Current.CoreWindow.GetKeyState(VirtualKey.A).HasFlag(CoreVirtualKeyStates.Down) ? -1.0 : 0.0) +
                        (Window.Current.CoreWindow.GetKeyState(VirtualKey.D).HasFlag(CoreVirtualKeyStates.Down) ? 1.0 : 0.0);
                    var bodyY =
                        (Window.Current.CoreWindow.GetKeyState(VirtualKey.W).HasFlag(CoreVirtualKeyStates.Down) ? -1.0 : 0.0) +
                        (Window.Current.CoreWindow.GetKeyState(VirtualKey.S).HasFlag(CoreVirtualKeyStates.Down) ? 1.0 : 0.0);

                    var footX = frame == 0 ? 0 : point.X - lastX;
                    var footY = frame == 0 ? 0 : point.Y - lastY;

                    lastX = point.X;
                    lastY = point.Y;

                    handler.Send(game,
                        new PlayerInputs(frame, footX, footY, bodyX, bodyY, foot, body));

                    frame++;

                    foreach (var element in elements.Values)
                    {
                        Canvas.SetLeft(element.element, element.X);
                        Canvas.SetTop(element.element, element.Y);
                    }

                    if (frame % 60 == 0)
                    {
                        Fps.Text = $"frames send per second: {frame / stopWatch.Elapsed.TotalSeconds}{Environment.NewLine}" +
                            $"frame recieved {framesRecieved / stopWatch.Elapsed.TotalSeconds}{Environment.NewLine}" +
                            $"frame lag {frame - currentDisplayFrame}";
                    }

                    // let someone else have a go
                    await Task.Delay((int)Math.Max(1, ((1000 * frame) / 60)- stopWatch.ElapsedMilliseconds));

                    //while (stopWatch.ElapsedMilliseconds < ((1000 * frame) / 60))
                    //{
                    //}
                }
            }
            catch (Exception e)
            {

                var db = 0;
            }

        }

    }
}
