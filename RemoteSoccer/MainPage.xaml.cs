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
using System.Threading;
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
using Windows.UI.Xaml.Media.Imaging;
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
        private readonly Ellipse ballWall;
        private const double footLen = 400;
        private const double xMax = 12800;
        private const double yMax = 6400;

        private readonly Guid body = Guid.NewGuid();
        private readonly Guid foot = Guid.NewGuid();

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
                Visibility = Visibility.Collapsed,
                Stroke = new SolidColorBrush(Color.FromArgb(0xff, 0xbb, 0xbb, 0xbb)),
                StrokeThickness = 20
            };

            Canvas.SetZIndex(ballWall, 0);

            GameArea.Children.Add(ballWall);


            var field = new Rectangle()
            {
                Width = xMax,
                Height = yMax,
                Fill =
                //new ImageBrush() {
                //    ImageSource = new BitmapImage(new Uri(@"ms-appx:///Assets/nice.jpg")),
                //    Stretch = Stretch.UniformToFill,
                //},  
                new SolidColorBrush(Color.FromArgb(0xff, 0xdd, 0xdd, 0xdd)),
                //Opacity = .08,
            };
            Canvas.SetZIndex(field, -2);
            GameArea.Children.Add(field);

            var lineBrush =
                new SolidColorBrush(Color.FromArgb(0xff, 0xcc, 0xcc, 0xcc));
            for (double i = 0; i <= yMax; i += yMax / 4)
            {
                var line = new Line()
                {
                    X1 = 0,
                    X2 = xMax,
                    Y1 = i,
                    Y2 = i,
                    Stroke = lineBrush,
                    StrokeThickness = 5,
                };
                Canvas.SetZIndex(line, -1);
                GameArea.Children.Add(line);
            }
            for (double i = 0; i <= xMax; i += yMax / 4)
            {
                var line = new Line()
                {
                    X1 = i,
                    X2 = i,
                    Y1 = 0,
                    Y2 = yMax,
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
                    foreach (var objectCreated in objectsCreated.Objects)
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

                            if (objectCreated.Name == "ball")
                            {
                                ball = ellipse;
                            }

                            elements.Add(objectCreated.Id, new ElementEntry(ellipse, color));// new ElementEntry(ellipse, objectCreated.X, objectCreated.Y, objectCreated.Diameter));
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


        private long longestGap = 0;
        private long lastHandlePositions = 0;
        private int currentFrame = 0;
        private string gameName;

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

                foreach (var position in positions.PositionsList)
                {

                    if (elements.TryGetValue(position.Id, out var element))
                    {
                        if (position.Id == body)
                        {
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
                    Fps.Text = $"time to draw: {(stopWatch.ElapsedTicks - ticks)/ (double)TimeSpan.TicksPerMillisecond:f2}{Environment.NewLine}" +
                        $"longest gap: {longestGap}{Environment.NewLine}" +
                        $"frame lag: {frame - positions.Frame}{Environment.NewLine}" +
                        $"hold shift to use mouse";
                    longestGap = 0;
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
                lastHandlePositions = sw.ElapsedTicks;
                stopWatch = sw;

                var distrib = new int[100];

                while (true)
                {
                    var coreWindow = Window.Current.CoreWindow;
                    if (coreWindow.GetKeyState(VirtualKey.R).HasFlag(CoreVirtualKeyStates.Down))
                    {
                        handler.Send(new ResetGame(game));
                    }

                    var bodyX =
                        (coreWindow.GetKeyState(VirtualKey.A).HasFlag(CoreVirtualKeyStates.Down) ? -1.0 : 0.0) +
                        (coreWindow.GetKeyState(VirtualKey.D).HasFlag(CoreVirtualKeyStates.Down) ? 1.0 : 0.0);
                    var bodyY =
                        (coreWindow.GetKeyState(VirtualKey.W).HasFlag(CoreVirtualKeyStates.Down) ? -1.0 : 0.0) +
                        (coreWindow.GetKeyState(VirtualKey.S).HasFlag(CoreVirtualKeyStates.Down) ? 1.0 : 0.0);

                    var point = CoreWindow.GetForCurrentThread().PointerPosition;
                    var footX = (point.X - lastX) * .75;
                    var footY = (point.Y - lastY) * .75;

                    if (!coreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
                    {
                        point = new Point(500, 500);
                        coreWindow.PointerPosition = point;
                        coreWindow.PointerCursor = null;
                    }
                    else
                    {
                        if (coreWindow.PointerCursor == null)
                        {
                            coreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
                        }
                    }
                    lastX = point.X;
                    lastY = point.Y;

                    if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Escape).HasFlag(CoreVirtualKeyStates.Down))
                    {
                        Application.Current.Exit();
                    }

                    handler.Send(game,
                        new PlayerInputs(footX, footY, bodyX, bodyY, foot, body));

                    frame++;

                    
                    // let someone else have a go
                    await Task.Delay((int)Math.Max(0, (1000 * frame / 60) - stopWatch.ElapsedMilliseconds));
                 
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

            gameName = (string)e.Parameter;


            Task.Run(async () =>
            {
                try
                {
                    var handler = await SingleSignalRHandler.GetOrThrow();
                    var random = new Random();

                    var color = new byte[3];

                    while (color[0] + color[1] + color[2] < (0xCC) || color[0] + color[1] + color[2] > (0x143 ))
                    {
                        random.NextBytes(color);
                    }

                    handler.Send(
                        gameName,
                        new CreatePlayer(
                            foot,
                            body,
                            footLen * 2,
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
                        HandleUpdateScore,
                        HandleColorChanged);

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

        private void GameHolder_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var delta = e.GetCurrentPoint(GameHolder).Properties.MouseWheelDelta;

            times = times * Math.Pow(1.1, delta / 100.0);
        }

        private void ColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            var color = ColorPicker.Color;
            SingleSignalRHandler.GetOrThrow().ContinueWith(x =>
            {
                x.Result.Send(gameName, new ColorChanged(foot, color.R, color.G, color.B, 0xff));
                x.Result.Send(gameName, new ColorChanged(body, color.R, color.G, color.B, 0x20));
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Menu.Visibility = Visibility.Collapsed;
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
