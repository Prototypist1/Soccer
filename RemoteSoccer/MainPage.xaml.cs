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
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private class ElementEntry
        {
            private const int V = 0;
            public readonly Ellipse element;
            private readonly Brush[] brushes;

            public ElementEntry(Ellipse element, Color color)
            {
                this.element = element ?? throw new ArgumentNullException(nameof(element));
                //this.brushes = GenerateBrushes(color);
            }

            public Brush GetBrush(double width, double velocity)
            {
                return brushes[Math.Min(V - 1, (int)(V * velocity / width))];
            }

            private Brush[] GenerateBrushes(Color color)
            {
                var darkColor = Color.FromArgb(color.A, (byte)(((int)color.R) / 2), (byte)(((int)color.G) / 2), (byte)(((int)color.B) / 2));
                var deadColor = Color.FromArgb(0x00, darkColor.R, darkColor.G, darkColor.B);

                var brushes = new Brush[V];

                {
                    var stops = new GradientStopCollection();
                    stops.Add(new GradientStop()
                    {
                        Color = darkColor,
                        Offset = 1,
                    });
                    stops.Add(new GradientStop()
                    {
                        Color = color,
                        Offset = .8,
                    });
                    brushes[0] = new RadialGradientBrush(stops);
                }
                for (int i = 1; i < V; i++)
                {
                    var stops = new GradientStopCollection();
                    stops.Add(new GradientStop()
                    {
                        Color = deadColor,
                        Offset = 1,
                    });
                    var x = (i / 100.0);
                    double partDark;
                    double lightPart;
                    if (x < .2)
                    {
                        partDark = x * (1 - (x * 5 / 2.0));
                        lightPart = x * (x * 5 / 2.0);
                    }
                    else
                    {
                        partDark = .1;
                        lightPart = .1 + (x - .2);
                    }
                    var normalPartDark = partDark / (partDark + lightPart);
                    var notmalPartLight = lightPart / (partDark + lightPart);

                    stops.Add(new GradientStop()
                    {
                        Color = Color.FromArgb(
                            color.A,
                            (byte)(int)((darkColor.R * normalPartDark) + (color.R * notmalPartLight)),
                            (byte)(int)((darkColor.G * normalPartDark) + (color.G * notmalPartLight)),
                            (byte)(int)((darkColor.B * normalPartDark) + (color.B * notmalPartLight))),
                        Offset = 1 - x,
                    });
                    stops.Add(new GradientStop()
                    {
                        Color = color,
                        Offset = Math.Max(0, .8 - x),
                    });
                    brushes[i] = new RadialGradientBrush(stops);
                }
                return brushes;
            }
        }

        private readonly Dictionary<Guid, ElementEntry> elements = new Dictionary<Guid, ElementEntry>();
        private readonly Dictionary<Guid, TextBlock> texts = new Dictionary<Guid, TextBlock>();

        private readonly Ellipse ballWall;

        private readonly Guid body = Guid.NewGuid();
        private readonly Guid foot = Guid.NewGuid();

        private readonly LinkedList< MediaPlayer> players = new LinkedList<MediaPlayer>();
        private readonly MediaPlayer bell;

        public MainPage()
        {
            this.InitializeComponent();

            Window.Current.CoreWindow.KeyUp += Menu_KeyUp;

            viewFrameWidth = GameHolder.ActualWidth;
            viewFrameHeight = GameHolder.ActualHeight;

            var random = new Random();

            for (int i = 0; i < 10; i++)
            {

                var player = new MediaPlayer();
                player.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/hit{random.Next(1,4)}.wav"));
                players.AddLast(player);

            }

            bell = new MediaPlayer();
            bell.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/bell.mp3"));


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

            if (lockCurser)
            {
                Window.Current.CoreWindow.PointerCursor = null;
            }
            else
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
            }

            ballWall = new Ellipse
            {
                Visibility = Visibility.Collapsed,
                Stroke = new SolidColorBrush(Color.FromArgb(0xff, 0xbb, 0xbb, 0xbb)),
                StrokeThickness = 20
            };

            Canvas.SetZIndex(ballWall, 0);

            GameArea.Children.Add(ballWall);


            var field = new Rectangle()
            {
                Width = Constants.xMax,
                Height = Constants.yMax,
                Fill = new SolidColorBrush(Color.FromArgb(0xff, 0xdd, 0xdd, 0xdd)),
            };
            Canvas.SetZIndex(field, -2);
            GameArea.Children.Add(field);

            var lineBrush =
                new SolidColorBrush(Color.FromArgb(0xff, 0xcc, 0xcc, 0xcc));
            for (double i = 0; i <= Constants.yMax; i += Constants.yMax / 4)
            {
                var line = new Line()
                {
                    X1 = 0,
                    X2 = Constants.xMax,
                    Y1 = i,
                    Y2 = i,
                    Stroke = lineBrush,
                    StrokeThickness = 5,
                };
                Canvas.SetZIndex(line, -1);
                GameArea.Children.Add(line);
            }
            for (double i = 0; i <= Constants.xMax; i += Constants.yMax / 4)
            {
                var line = new Line()
                {
                    X1 = i,
                    X2 = i,
                    Y1 = 0,
                    Y2 = Constants.yMax,
                    Stroke = lineBrush,
                    StrokeThickness = 5,
                };
                Canvas.SetZIndex(line, -1);
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
                    foreach (var objectCreated in objectsCreated.Feet)
                    {
                        CreateOjectsView(objectCreated);
                    }

                    foreach (var objectCreated in objectsCreated.Bodies)
                    {
                        CreateOjectsView(objectCreated);
                    }

                    if (objectsCreated.Ball != null)
                    {
                        CreateOjectsView(objectsCreated.Ball);
                    }

                    foreach (var objectCreated in objectsCreated.Goals)
                    {
                        CreateOjectsView(objectCreated);
                    }
                });
        }

        private void CreateOjectsView(ObjectCreated objectCreated)
        {
            if (!elements.ContainsKey(objectCreated.Id))
            {
                var color = Color.FromArgb(
                        objectCreated.A,
                        objectCreated.R,
                        objectCreated.G,
                        objectCreated.B);

                var ellipse = new Ellipse()
                {
                    Width = objectCreated.Diameter,
                    Height = objectCreated.Diameter,
                    Fill = new SolidColorBrush(color),
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


                elements.Add(objectCreated.Id, new ElementEntry(ellipse, color));// new ElementEntry(ellipse, objectCreated.X, objectCreated.Y, objectCreated.Diameter));
                GameArea.Children.Add(ellipse);

                if (objectCreated is BodyCreated foot)
                {

                    var sum = foot.R + foot.G + foot.B;

                    var text = new TextBlock()
                    {
                        Text = foot.Name,
                        FontSize = 200,
                        Foreground = new SolidColorBrush( Color.FromArgb(0x88,0xff,0xff,0xff))
                    };

                    Canvas.SetZIndex(text, Constants.textZ);

                    text.TransformMatrix =
                    // first we center
                    new System.Numerics.Matrix4x4(
                        1, 0, 0, 0,
                        0, 1, 0, 0,
                        0, 0, 1, 0,
                        (float)(-text.ActualWidth / 2.0), (float)(-text.ActualHeight / 2.0), 0, 1)
                    *
                    // then we move to the right spot
                    new System.Numerics.Matrix4x4(
                        1, 0, 0, 0,
                        0, 1, 0, 0,
                        0, 0, 1, 0,
                        (float)(objectCreated.X), (float)(objectCreated.Y), 0, 1);

                    GameArea.Children.Add(text);
                    texts.Add(foot.Id, text);
                }

                if (objectCreated is BallCreated)
                {
                    this.ball = ellipse;
                }

            }
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
        private double times = .1;
        private double xPlus = 0;
        private double yPlus = 0;
        private Ellipse ball;
        private int frame = 0;
        private Stopwatch stopWatch;


        private long longestGap = 0;
        private long lastHandlePositions = 0;
        private int currentFrame = 0;
        private string gameName;
        private bool lockCurser = true;


        Dictionary<Line, DateTime> lineTimes = new Dictionary<Line, DateTime>();

        private void HandlePositions(Positions positions)
        {

            framesRecieved++;




        Top:
            var myCurrentFrame = currentFrame;
            if (myCurrentFrame > positions.Frame)
            {
                return;
            }
            if (Interlocked.CompareExchange(ref currentFrame, positions.Frame, myCurrentFrame) != myCurrentFrame)
            {
                goto Top;
            }

            var gap = (stopWatch.ElapsedTicks - lastHandlePositions) / TimeSpan.TicksPerMillisecond;
            longestGap = Math.Max(gap, longestGap);
            lastHandlePositions = stopWatch.ElapsedTicks;

            var ticks = stopWatch.ElapsedTicks;


            var dontwait = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
            CoreDispatcherPriority.High,
            () =>
            {

                var playerX = 0.0;
                var playerY = 0.0;

                foreach (var position in positions.PositionsList)
                {

                    if (elements.TryGetValue(position.Id, out var element))
                    {
                        if (position.Id == body)
                        {
                            playerX = position.X;
                            playerY = position.Y;
                            xPlus = (viewFrameWidth / 2.0) - (position.X * times);
                            yPlus = (viewFrameHeight / 2.0) - (position.Y * times);
                        }


                        var v = Math.Sqrt((position.Vx * position.Vx) + (position.Vy * position.Vy));

                        var Stretch = (v / element.element.Width);

                        //element.element.Fill = element.GetBrush( element.element.Width,v);

                        if (v != 0)
                        {
                            element.element.TransformMatrix =
                            // first we center
                            new System.Numerics.Matrix4x4(
                                1, 0, 0, 0,
                                0, 1, 0, 0,
                                0, 0, 1, 0,
                                (float)(-element.element.Width / 2.0), (float)(-element.element.Height / 2.0), 0, 1)
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
                                (float)(-v / 2.0), 0, 0, 1)
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
                        else
                        {

                            element.element.TransformMatrix =
                            // first we center
                            new System.Numerics.Matrix4x4(
                                1, 0, 0, 0,
                                0, 1, 0, 0,
                                0, 0, 1, 0,
                                (float)(-element.element.Width / 2.0), (float)(-element.element.Height / 2.0), 0, 1)
                            *
                            // then we move to the right spot
                            new System.Numerics.Matrix4x4(
                                1, 0, 0, 0,
                                0, 1, 0, 0,
                                0, 0, 1, 0,
                                (float)(position.X), (float)(position.Y), 0, 1);
                        }

                        //Canvas.SetLeft(element, position.X - (element.Width / 2.0));
                        //Canvas.SetTop(element, position.Y - (element.Height / 2.0));
                    }

                    if (element != null && texts.TryGetValue(position.Id, out var text))
                    {
                        text.TransformMatrix =
                            // first we center
                            new System.Numerics.Matrix4x4(
                                1, 0, 0, 0,
                                0, 1, 0, 0,
                                0, 0, 1, 0,
                                (float)(-text.ActualWidth / 2.0), (float)(-text.ActualHeight / 2.0), 0, 1)
                            *
                            // then we move to the right spot
                            new System.Numerics.Matrix4x4(
                                1, 0, 0, 0,
                                0, 1, 0, 0,
                                0, 0, 1, 0,
                                (float)(position.X), (float)(position.Y), 0, 1);
                    }
                }


                var now = DateTime.Now;
                foreach (var child in GameArea.Children.ToList())
                {
                    if (child is Line line && lineTimes.TryGetValue(line, out var time) && now - time > TimeSpan.FromMilliseconds(400))
                    {
                        GameArea.Children.Remove(line);
                        lineTimes.Remove(line);
                    }
                }

                foreach (var collision in positions.Collisions)
                {
                    if (!collision.IsGoal)
                    {
                        var item = players.First.Value;
                        players.RemoveFirst();
                        players.AddLast(item);

                        var dx = collision.X - playerX;
                        var dy = collision.Y - playerY;
                        var d = Math.Sqrt((dx * dx) + (dy * dy));

                        item.Volume = Math.Min(1, .2 + (new Physics.Vector(collision.Fx, collision.Fy).Length / (15* Math.Max(1, Math.Log(d)))));
                        item.AudioBalance = dx / d;
                        item.Play();
                    }
                    else {

                        var dx = collision.X - playerX;
                        var dy = collision.Y - playerY;
                        var d = Math.Sqrt((dx * dx) + (dy * dy));

                        bell.Volume = Math.Min(1, .2 + (5.0 / (Math.Max(1, Math.Log(d)))));
                        bell.AudioBalance = dx / d;
                        bell.Play();
                    }

                    var line1 = new Line
                    {

                        X1 = -(collision.Fy * (collision.IsGoal ? 4000 : 20)),
                        Y1 = (collision.Fx * (collision.IsGoal ? 4000 : 20)),
                        X2 = -((collision.Fy / 4.0) * (collision.IsGoal ? 4000 : 20)),
                        Y2 = ((collision.Fx / 4.0) * (collision.IsGoal ? 4000 : 20)),
                        StrokeThickness = 5,
                        Stroke = new SolidColorBrush(Colors.Black),
                        Opacity = 1,
                        OpacityTransition = new ScalarTransition() { Duration = TimeSpan.FromMilliseconds(400), },
                        Scale = new Vector3(.4f, .4f, 1f),
                        ScaleTransition = new Vector3Transition() { Duration = TimeSpan.FromMilliseconds(200) },
                        Translation = new Vector3((float)collision.X, (float)collision.Y, 0)

                    };
                    lineTimes.Add(line1, now);
                    GameArea.Children.Add(line1);
                    line1.Opacity = 0f;
                    line1.Scale = new Vector3(2, 2, 1);
                    Canvas.SetZIndex(line1, Constants.footZ);

                    var line2 = new Line
                    {

                        X1 = (collision.Fy * (collision.IsGoal ? 4000 : 20)),
                        Y1 = -(collision.Fx * (collision.IsGoal ? 4000 : 20)),
                        X2 = ((collision.Fy / 4.0) * (collision.IsGoal ? 4000 : 20)),
                        Y2 = -((collision.Fx / 4.0) * (collision.IsGoal ? 4000 : 20)),
                        StrokeThickness = 5,
                        Stroke = new SolidColorBrush(Colors.Black),
                        Opacity = 1,
                        OpacityTransition = new ScalarTransition() { Duration = TimeSpan.FromMilliseconds(400), },
                        Scale = new Vector3(.4f, .4f, 1f),
                        ScaleTransition = new Vector3Transition() { Duration = TimeSpan.FromMilliseconds(100) },
                        Translation = new Vector3((float)collision.X, (float)collision.Y, 0)
                    };
                    lineTimes.Add(line2, now);
                    GameArea.Children.Add(line2);
                    line2.Opacity = 0f;
                    line2.Scale = new Vector3(2, 2, 1);
                    Canvas.SetZIndex(line2, Constants.footZ);
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

                if (frame % 20 == 0 && stopWatch != null)
                {
                    Fps.Text = $"time to draw: {(stopWatch.ElapsedTicks - ticks) / (double)TimeSpan.TicksPerMillisecond:f2}{Environment.NewLine}" +
                        $"longest gap: {longestGap}{Environment.NewLine}" +
                        $"frame lag: {frame - positions.Frame}{Environment.NewLine}" +
                        $"Escape: Show options";
                    longestGap = 0;
                }
            });

        }

        private bool sending = true;
        private TaskCompletionSource<bool> StoppedSending = new TaskCompletionSource<bool>();
        // assumed to be run on the main thread
        private async IAsyncEnumerable<PlayerInputs> MainLoop(SingleSignalRHandler.SignalRHandler handler, Guid foot, Guid body, string game)
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
            lastHandlePositions = sw.ElapsedTicks;
            stopWatch = sw;

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
                                handler.Send(new ResetGame(game));
                            }

                            bodyX =
                                (coreWindow.GetKeyState(VirtualKey.A).HasFlag(CoreVirtualKeyStates.Down) ? -1.0 : 0.0) +
                                (coreWindow.GetKeyState(VirtualKey.D).HasFlag(CoreVirtualKeyStates.Down) ? 1.0 : 0.0);
                            bodyY =
                                (coreWindow.GetKeyState(VirtualKey.W).HasFlag(CoreVirtualKeyStates.Down) ? -1.0 : 0.0) +
                                (coreWindow.GetKeyState(VirtualKey.S).HasFlag(CoreVirtualKeyStates.Down) ? 1.0 : 0.0);

                            var point = CoreWindow.GetForCurrentThread().PointerPosition;
                            footX = (point.X - lastX) * .75;
                            footY = (point.Y - lastY) * .75;

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
                frame++;


                // let someone else have a go
                await Task.Delay((int)Math.Max(0, (1000 * frame / 60) - stopWatch.ElapsedMilliseconds));

            }
            StoppedSending.SetResult(true);
        }



        private void GameHolder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            viewFrameWidth = GameHolder.ActualWidth;
            viewFrameHeight = GameHolder.ActualHeight;
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            gameName = (string)e.Parameter;


            Task.Run(async () =>
            {
                try
                {
                    var handler = await SingleSignalRHandler.GetOrThrow();

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

                    handler.Send(
                        gameName,
                        new CreatePlayer(
                            foot,
                            body,
                            Constants.footLen * 2,
                            80,
                            color[0],
                            color[1],
                            color[2],
                            0x20,
                            color[0],
                            color[1],
                            color[2],
                            0xff, 
                            name),
                        HandleObjectsCreated,
                        HandleObjectsRemoved,
                        HandleUpdateScore,
                        HandleColorChanged,
                        HandleNameChanged);


                    var dontWait = SpoolPositions(handler.JoinChannel(new JoinChannel(gameName)));

                    handler.Send(gameName, MainLoop(handler, foot, body, gameName));
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

        private async Task SpoolPositions(IAsyncEnumerable<Positions> positionss)
        {
            await foreach (var item in positionss)
            {
                HandlePositions(item);
            }
        }

        private async void HandleColorChanged(ColorChanged obj)
        {

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        () =>
                        {
                            var element = elements[obj.Id];
                            element.element.Fill = new SolidColorBrush(Color.FromArgb(obj.A, obj.R, obj.G, obj.B));
                        });
        }

        private async void HandleNameChanged(NameChanged obj)
        {

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        () =>
                        {
                            var text = texts[obj.Id];
                            text.Text = obj.Name;
                        });
        }

        private void GameHolder_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var delta = e.GetCurrentPoint(GameHolder).Properties.MouseWheelDelta;

            times = times * Math.Pow(1.1, delta / 100.0);
        }

        private void ColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            var color = ColorPicker.Color;


            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values[LocalSettingsKeys.PlayerColorR] = color.R;
            localSettings.Values[LocalSettingsKeys.PlayerColorG] = color.G;
            localSettings.Values[LocalSettingsKeys.PlayerColorB] = color.B;

            SingleSignalRHandler.GetOrThrow().ContinueWith(x =>
            {
                x.Result.Send(gameName, new ColorChanged(foot, color.R, color.G, color.B, 0xff));
                x.Result.Send(gameName, new ColorChanged(body, color.R, color.G, color.B, 0x20));
            });
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

            SingleSignalRHandler.GetOrThrow().ContinueWith(x =>
            {
                x.Result.Send(gameName, new NameChanged(body, name));
            });
        }

        public async Task StopSendingInputs() {
            sending = false;
            await StoppedSending.Task;
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            await StopSendingInputs();
            var handler = (await SingleSignalRHandler.GetOrThrow());
            handler.Send(new LeaveGame());
            handler.SetOnClosed(null);
            handler.ClearCallBacks();

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    this.Frame.Navigate(typeof(LandingPage));
                });
        }
    }
}
