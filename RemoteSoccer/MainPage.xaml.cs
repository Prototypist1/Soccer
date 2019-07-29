using Common;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Physics;
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
            public readonly UIElement element;
            public double X;
            public double Y;
            public double Diameter;

            public ElementEntry(UIElement element, double x, double y, double diameter)
            {
                this.element = element ?? throw new ArgumentNullException(nameof(element));
                X = x;
                Y = y;
                Diameter = diameter;
            }
        }

        private class LineScaling
        {
            public Line line;
            public double x1, x2, y1, y2;

            public LineScaling(Line line, double x1, double x2, double y1, double y2)
            {
                this.line = line ?? throw new ArgumentNullException(nameof(line));
                this.x1 = x1;
                this.x2 = x2;
                this.y1 = y1;
                this.y2 = y2;
            }
        }

        // this scaling code is confusing 
        // why do I have diameter in two places?
        private class EllipseScaling
        {
            public readonly UIElement Element;
            public readonly Ellipse ellipse;
            public double diameter;

            public EllipseScaling(UIElement element, Ellipse ellipse, double diameter)
            {
                Element = element ?? throw new ArgumentNullException(nameof(element));
                this.ellipse = ellipse ?? throw new ArgumentNullException(nameof(ellipse));
                this.diameter = diameter;
            }
        }

        private readonly Dictionary<Guid, ElementEntry> elements = new Dictionary<Guid, ElementEntry>();
        private readonly List<LineScaling> lines = new List<LineScaling>();
        private readonly List<EllipseScaling> ellipses = new List<EllipseScaling>();
        private readonly Ellipse ballWall;
        private readonly EllipseScaling ballWallScaler;
        private readonly ElementEntry ballWallEntry;
        private const double footLen = 200;
        private const double xMax = 6400;
        private const double yMax = 3200;

        private Guid body, foot;

        private ScalerManager scalerManager = new ScalerManager(new DontScale());

        private class ScalerManager {

            private IScaler scaler;
            private IScaler nextScaler;

            public ScalerManager(IScaler scaler)
            {
                this.scaler = scaler ?? throw new ArgumentNullException(nameof(scaler));
            }

            public void SetNextScaler(IScaler scaler) {
                this.nextScaler = scaler;
            }

            public bool RotateScalers(List<LineScaling> lines, List<EllipseScaling> ellipses) {
                if (nextScaler == null) {
                    return false;
                }

                this.scaler = nextScaler;
                foreach (var line in lines)
                {
                    line.line.X1 = scaler.ScaleX(line.x1);
                    line.line.X2 = scaler.ScaleX(line.x2);
                    line.line.Y1 = scaler.ScaleY(line.y1);
                    line.line.Y2 = scaler.ScaleY(line.y2);
                }
                foreach (var ellipse in ellipses)
                {
                    ellipse.ellipse.Width = scaler.Scale(ellipse.diameter);
                    ellipse.ellipse.Height = scaler.Scale(ellipse.diameter);
                }
                return true;
            }

            public IScaler GetScaler() => scaler;
        }


        public MainPage()
        {
            this.InitializeComponent();

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
                Fill = new SolidColorBrush(Color.FromArgb(0x20, 0x00, 0x00, 0x00)),
                Visibility= Visibility.Collapsed
            };
            Canvas.SetZIndex(ballWall, 0);
            ballWallScaler = new EllipseScaling(ballWall, ballWall, 0);
            ellipses.Add(ballWallScaler);

            ballWallEntry = new ElementEntry(ballWall, xMax / 2.0, yMax / 2.0, 0);

            GameArea.Children.Add(ballWall);

            var points = new[] {
                            (new Vector(footLen,0) ,new Vector(xMax- footLen,0)),
                            (new Vector(0,footLen) ,new Vector(footLen,0)),
                            (new Vector(0,yMax - footLen) ,new Vector(0,footLen)),
                            (new Vector(footLen,yMax),new Vector(0,yMax - footLen)),
                            (new Vector(xMax - footLen,yMax),new Vector(footLen,yMax)),
                            (new Vector(xMax,yMax - footLen),new Vector(xMax - footLen,yMax)),
                            (new Vector(xMax,footLen),new Vector(xMax,yMax - footLen)),
                            (new Vector(xMax- footLen,0) ,new Vector(xMax,footLen))
                        };

            foreach (var side in points)
            {
                var line = new Line()
                {
                    X1 = scalerManager.GetScaler().ScaleX(side.Item1.x),
                    X2 = scalerManager.GetScaler().ScaleX(side.Item2.x),
                    Y1 = scalerManager.GetScaler().ScaleY(side.Item1.y),
                    Y2 = scalerManager.GetScaler().ScaleY(side.Item2.y),
                    Stroke = new SolidColorBrush(Colors.Black),
                };

                lines.Add(new LineScaling(line, side.Item1.x, side.Item2.x, side.Item1.y, side.Item2.y));

                GameArea.Children.Add(line);
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
                                Width = scalerManager.GetScaler().Scale(objectCreated.Diameter),
                                Height = scalerManager.GetScaler().Scale(objectCreated.Diameter),
                                Fill = new SolidColorBrush(Color.FromArgb(
                                    objectCreated.A,
                                    objectCreated.R,
                                    objectCreated.G,
                                    objectCreated.B)),
                            };

                            //var dropShadow = new DropShadowPanel()
                            //{
                            //    Content = ellipse,
                            //    BlurRadius = 30,
                            //    ShadowOpacity = .8,
                            //    Color = Color.FromArgb(0xff,
                            //    0x00,
                            //    0x00,
                            //    0x00)
                            //};
                            Canvas.SetZIndex(ellipse, objectCreated.Z);

                            elements.Add(objectCreated.Id, new ElementEntry(ellipse, -1000, -1000, objectCreated.Diameter) {
                                X =objectCreated.X,
                                Y = objectCreated.Y
                            });
                            ellipses.Add(new EllipseScaling(ellipse, ellipse, objectCreated.Diameter));
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
        private JumpBallConcurrent<Positions> currentPositions = new JumpBallConcurrent<Positions>(new Positions(new Position[0], -1, new CountDownState { Countdown = false}));

        private void HandlePositions(Positions positions)
        {
            currentPositions.Run(x =>
            {
                if (x.Frame < positions.Frame) {
                    framesRecieved++;
                    return positions;
                }
                return x;
            });
        }

        // assumed to be run on the main thread
        private async void MainLoop(SingleSignalRHandler.SignalRHandler handler, Guid foot, Guid body, string game)
        {
            try
            {
                var frame = 0;

                double lastX = 0, lastY = 0;

                var stopWatch = new Stopwatch();
                stopWatch.Start();

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

                    var footX = frame == 0 ? 0 : point.X - lastX;
                    var footY = frame == 0 ? 0 : point.Y - lastY;

                    lastX = point.X;
                    lastY = point.Y;

                    handler.Send(game,
                        new PlayerInputs(footX, footY, bodyX, bodyY, foot, body));

                    var myPositions = currentPositions.Read();


                    ballWall.Visibility = myPositions.CountDownState.Countdown ? Visibility.Visible : Visibility.Collapsed;

                    if (myPositions.CountDownState.Countdown)
                    {
                        ballWallScaler.diameter = myPositions.CountDownState.Radius * 2;
                        ballWallEntry.Diameter = myPositions.CountDownState.Radius * 2;
                        ballWallEntry.X = myPositions.CountDownState.X;
                        ballWallEntry.Y = myPositions.CountDownState.Y;
                    }

                    foreach (var position in myPositions.PositionsList)
                    {

                        if (elements.TryGetValue(position.Id, out var element))
                        {
                            if (position.Id == body)
                            {
                                scalerManager.SetNextScaler(new FollowBodyScaler(
                                    .5,
                                    position.X,
                                    position.Y,
                                    viewFrameWidth,
                                    viewFrameHeight));
                            }
                            element.X = position.X;
                            element.Y = position.Y;
                        }
                    }


                    scalerManager.RotateScalers(lines,ellipses);

                    frame++;
                    foreach (var element in elements.Values)
                    {
                        Canvas.SetLeft(element.element, scalerManager.GetScaler().ScaleX(element.X -(element.Diameter/2.0)));
                        Canvas.SetTop(element.element, scalerManager.GetScaler().ScaleY(element.Y - (element.Diameter / 2.0)));
                    }

                    Canvas.SetLeft(ballWallEntry.element, scalerManager.GetScaler().ScaleX(ballWallEntry.X - (ballWallEntry.Diameter / 2.0)));
                    Canvas.SetTop(ballWallEntry.element, scalerManager.GetScaler().ScaleY(ballWallEntry.Y - (ballWallEntry.Diameter / 2.0)));

                    if (frame % 60 == 0)
                    {
                        Fps.Text = $"frames send per second: {frame / stopWatch.Elapsed.TotalSeconds}{Environment.NewLine}" +
                            $"frame recieved: {framesRecieved / stopWatch.Elapsed.TotalSeconds}{Environment.NewLine}" +
                            $"frame lag: {frame - myPositions.Frame}";
                    }

                    // let someone else have a go
                    await Task.Delay((int)Math.Max(1, ((1000 * frame) / 60) - stopWatch.ElapsedMilliseconds));

                    //while (stopWatch.ElapsedMilliseconds < ((1000 * frame) / 60))
                    //{
                    //}
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
            scalerManager.SetNextScaler(new Scaler(viewFrameWidth, viewFrameHeight, xMax, yMax));
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
                        () => {
                            LeftScore.Text = obj.Left.ToString();
                            RightScore.Text = obj.Right.ToString();
                        });
        }
    }
}
