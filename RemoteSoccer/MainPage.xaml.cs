using Common;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.UI.Media;
using Physics;
using Prototypist.Fluent;
using Prototypist.TaskChain;
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
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
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
        //private class ElementEntry
        //{
        //    public readonly Ellipse element;
        //    public double X;
        //    public double Y;
        //    public double Diameter;

        //    public ElementEntry(Ellipse element, double x, double y, double diameter)
        //    {
        //        this.element = element ?? throw new ArgumentNullException(nameof(element));
        //        X = x;
        //        Y = y;
        //        Diameter = diameter;
        //    }
        //}

        private readonly Dictionary<Guid, Ellipse> elements = new Dictionary<Guid, Ellipse>();
        private readonly Ellipse ballWall;
        private const double footLen = 200;
        private const double xMax = 6400;
        private const double yMax = 3200;
        private readonly Brush[] brushes;

        private Guid body, foot;

        public MainPage()
        {
            this.InitializeComponent();

            brushes = new Brush[100];
            var color = Color.FromArgb(0x80, 0x00, 0x80, 0x00);
            var deadColor = Color.FromArgb(0x00, 0x00, 0x80, 0x00);
            brushes[0] = new SolidColorBrush(color);
            for (int i = 1; i < 100; i++)
            {
                var stops = new GradientStopCollection();
                stops.Add(new GradientStop()
                {
                    Color = deadColor,
                    Offset = 1,
                });
                stops.Add(new GradientStop()
                {
                    Color = color,
                    Offset = 1 - (i/100.0),
                });
                brushes[i] = new RadialGradientBrush(stops);
            }

            viewFrameWidth = GameHolder.ActualWidth;
            viewFrameHeight = GameHolder.ActualHeight;

            Task.Run(async () =>
            {
                try
                {
                    (await SingleSignalRHandler.GetOrThrow()).SetOnClosed(OnDisconnect);
                }
                catch (Exception ex)
                {
                    await OnDisconnect(ex);
                }
            });

            ballWall = new Ellipse
            {
                Visibility = Visibility.Collapsed,
                Stroke = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0x00, 0x00)),
                StrokeThickness = 20
            };

            Canvas.SetZIndex(ballWall, 0);

            GameArea.Children.Add(ballWall);

            var pointsCollection = new PointCollection();
            pointsCollection.Add(new Point(footLen, 0));
            pointsCollection.Add(new Point(0, footLen));
            pointsCollection.Add(new Point(0, yMax - footLen));
            pointsCollection.Add(new Point(footLen, yMax));
            pointsCollection.Add(new Point(xMax - footLen, yMax));
            pointsCollection.Add(new Point(xMax, yMax - footLen));
            pointsCollection.Add(new Point(xMax, footLen));
            pointsCollection.Add(new Point(xMax - footLen, 0));
            {
                var stops = new GradientStopCollection();
                stops.Add(new GradientStop()
                {
                    Offset = 0,
                    Color = Colors.Blue,//.FromArgb(0xff, 0x33, 0x33, 0x33)
                });
                stops.Add(new GradientStop()
                {
                    Offset = 1,
                    Color = Colors.Red,//.FromArgb(0xff, 0x66, 0x66, 0x66)
                });

                var poly = new Polygon()
                {
                    Points = pointsCollection,
                    Fill = new LinearGradientBrush()
                    {
                        GradientStops = stops,
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 0)
                    },
                };
                Canvas.SetZIndex(poly, -1);
                GameArea.Children.Add(poly);
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
                            var ellipse = new Ellipse()
                            {
                                Width = objectCreated.Diameter,
                                Height = objectCreated.Diameter,
                                Fill = new SolidColorBrush(Color.FromArgb(
                                    objectCreated.A,
                                    objectCreated.R,
                                    objectCreated.G,
                                    objectCreated.B)),
                            };

                            Canvas.SetZIndex(ellipse, objectCreated.Z);
                            ellipse.TransformMatrix =
                            // first we center
                            new System.Numerics.Matrix4x4(
                                1, 0, 0, 0,
                                0, 1, 0, 0,
                                0, 0, 1, 0,
                                (float)(-ellipse.Width / 2.0), (float)(-ellipse.Height / 2.0), 0, 1)
                            *
                            // then we move to the right spot
                            new System.Numerics.Matrix4x4(
                                1, 0, 0, 0,
                                0, 1, 0, 0,
                                0, 0, 1, 0,
                                (float)(objectCreated.X), (float)(objectCreated.Y), 0, 1);

                            if (objectCreated.Name == "ball") {
                                ball = ellipse;
                            }

                            elements.Add(objectCreated.Id, ellipse);// new ElementEntry(ellipse, objectCreated.X, objectCreated.Y, objectCreated.Diameter));
                            GameArea.Children.Add(ellipse);
                        }
                    }
                });
        }


        private void HandleObjectsRemoved(ObjectsRemoved objectsRemoved)
        {
            var dontwait = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    foreach (var objectRemoved in objectsRemoved.List)
                    {
                        elements.Remove(objectRemoved.Id);
                    }
                });
        }


        double framesRecieved = 0;
        private double viewFrameWidth;
        private double viewFrameHeight;
        private double times = .5;
        private double xPlus = 0;
        private double yPlus = 0;
        private Ellipse ball;
        private int frame = 0;
        private Stopwatch stopWatch;

        private void HandlePositions(Positions positions)
        {

            framesRecieved++;

            var dontwait = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
            CoreDispatcherPriority.Normal,
            () =>
            {


                foreach (var position in positions.PositionsList)
                {

                    if (elements.TryGetValue(position.Id, out var element))
                    {
                        if (position.Id == body)
                        {
                            times = .5;
                            xPlus = (viewFrameWidth / 2.0) - (position.X * times);
                            yPlus = (viewFrameHeight / 2.0) - (position.Y * times);
                        }


                        var v = Math.Sqrt((position.Vx * position.Vx) + (position.Vy * position.Vy));

                        var Stretch = (v/ element.Width);

                        element.Fill = brushes[Math.Min(99, (int)(100 * v / element.Width))];

                        if (v != 0)
                        {
                           

                            element.TransformMatrix = 
                                // first we center
                            new System.Numerics.Matrix4x4(
                                1, 0, 0, 0,
                                0,1, 0, 0,
                                0, 0, 1, 0,
                                (float)(-element.Width/2.0), (float)(-element.Height / 2.0), 0, 1) 
                                // then we stretch
                            * new System.Numerics.Matrix4x4(
                               (float)(1 + Stretch), 0, 0, 0,
                                0, 1, 0, 0,
                                0, 0, 1, 0,
                                0, 0, 0, 1)
                            // slide it back a little bit
                            * new System.Numerics.Matrix4x4(
                                1, 0, 0, 0,
                                0, 1, 0, 0,
                                0, 0, 1, 0,
                                (float)(-v/2.0), 0, 0, 1)
                            // then we rotate
                            * new System.Numerics.Matrix4x4(
                                (float)(position.Vx / v), (float)(position.Vy / v), 0, 0,
                                (float)(-position.Vy / v), (float)(position.Vx / v), 0, 0,
                                0, 0, 1, 0,
                                0, 0, 0, 1)
                            // then we move to the right spot
                            * new System.Numerics.Matrix4x4(
                                1, 0, 0, 0,
                                0, 1, 0, 0,
                                0, 0, 1, 0,
                                (float)(position.X), (float)(position.Y), 0, 1);
                        }
                        else {

                            element.TransformMatrix =
                            // first we center
                            new System.Numerics.Matrix4x4(
                                1, 0, 0, 0,
                                0, 1, 0, 0,
                                0, 0, 1, 0,
                                (float)(-element.Width / 2.0), (float)(-element.Height / 2.0), 0, 1)
                            *
                            // then we move to the right spot
                            new System.Numerics.Matrix4x4(
                                1, 0, 0, 0,
                                0, 1, 0, 0,
                                0, 0, 1, 0,
                                (float)(position.X ), (float)(position.Y ), 0, 1);
                        }

                        //Canvas.SetLeft(element, position.X - (element.Width / 2.0));
                        //Canvas.SetTop(element, position.Y - (element.Height / 2.0));
                    }
                }

                Canvas.SetLeft(ballWall, positions.CountDownState.X - positions.CountDownState.Radius);
                Canvas.SetTop(ballWall, positions.CountDownState.Y - positions.CountDownState.Radius);


                ballWall.Visibility = positions.CountDownState.Countdown ? Visibility.Visible : Visibility.Collapsed;

                if (positions.CountDownState.Countdown)
                {
                    ballWall.Width = positions.CountDownState.Radius * 2;
                    ballWall.Height = positions.CountDownState.Radius * 2;
                    ballWall.StrokeThickness = positions.CountDownState.StrokeThickness;
                    if (ball != null)
                    {
                        ball.Opacity = positions.CountDownState.BallOpacity;
                    }
                }

                GameArea.TransformMatrix = new System.Numerics.Matrix4x4(
                    (float)times, 0, 0, 0,
                    0, (float)times, 0, 0,
                    0, 0, 1, 0,
                    (float)xPlus, (float)yPlus, 0, 1);

                if (frame % 60 == 0 && stopWatch != null)
                {
                    Fps.Text = $"frames send per second: {frame / stopWatch.Elapsed.TotalSeconds}{Environment.NewLine}" +
                        $"frame recieved: {framesRecieved / stopWatch.Elapsed.TotalSeconds}{Environment.NewLine}" +
                        $"frame lag: {frame - positions.Frame}";
                }
            });
        }

        // assumed to be run on the main thread
        private async void MainLoop(SingleSignalRHandler.SignalRHandler handler, Guid foot, Guid body, string game)
        {
            try
            {
                var pointer = CoreWindow.GetForCurrentThread().PointerPosition;
                double lastX = pointer.X, lastY = pointer.Y;

                var sw = new Stopwatch();
                sw.Start();

                stopWatch = sw;

                var distrib = new int[100];

                while (true)
                {

                    var point = CoreWindow.GetForCurrentThread().PointerPosition;
                    //CoreWindow.GetForCurrentThread().PointerPosition = new Point(500, 500);
                    var bodyX =
                        (Window.Current.CoreWindow.GetKeyState(VirtualKey.A).HasFlag(CoreVirtualKeyStates.Down) ? -1.0 : 0.0) +
                        (Window.Current.CoreWindow.GetKeyState(VirtualKey.D).HasFlag(CoreVirtualKeyStates.Down) ? 1.0 : 0.0);
                    var bodyY =
                        (Window.Current.CoreWindow.GetKeyState(VirtualKey.W).HasFlag(CoreVirtualKeyStates.Down) ? -1.0 : 0.0) +
                        (Window.Current.CoreWindow.GetKeyState(VirtualKey.S).HasFlag(CoreVirtualKeyStates.Down) ? 1.0 : 0.0);

                    var footX = point.X - lastX;
                    var footY = point.Y - lastY;

                    lastX = point.X;
                    lastY = point.Y;

                    handler.Send(game,
                        new PlayerInputs(footX, footY, bodyX, bodyY, foot, body));

                    frame++;

                    // let someone else have a go
                    await Task.Delay((int)Math.Max(1, (1000 * frame / 60) - stopWatch.ElapsedMilliseconds));
                }
            }
#pragma warning disable CS0168 // Variable is declared but never used
            catch (Exception e)
#pragma warning restore CS0168 // Variable is declared but never used
            {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
                var db = 0;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
            }

        }



        private void GameHolder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            viewFrameWidth = GameHolder.ActualWidth;
            viewFrameHeight = GameHolder.ActualHeight;
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var gameName = (string)e.Parameter;


            Task.Run(async () =>
            {
                try
                {
                    foot = Guid.NewGuid();
                    body = Guid.NewGuid();
                    var handler = await SingleSignalRHandler.GetOrThrow();
                    var random = new Random();

                    var color = new byte[3];
                    random.NextBytes(color);

                    handler.Send(
                        gameName,
                        new CreatePlayer(
                            foot,
                            body,
                            400,
                            80,
                            color[0],
                            color[1],
                            color[2],
                            0x20,
                            color[0],
                            color[1],
                            color[2],
                            0xff),
                        HandlePositions,
                        HandleObjectsCreated,
                        HandleObjectsRemoved,
                        HandleUpdateScore);

                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        () => MainLoop(handler, foot, body, gameName));
                }
#pragma warning disable CS0168 // Variable is declared but never used
                catch (Exception ex)
                {
#pragma warning restore CS0168 // Variable is declared but never used
#pragma warning disable CS0219 // Variable is assigned but its value is never used
                    var db = 0;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
                }
            });

        }

        private async void HandleUpdateScore(UpdateScore obj)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        () =>
                        {
                            LeftScore.Text = obj.Left.ToString();
                            RightScore.Text = obj.Right.ToString();
                        });
        }
    }
}
