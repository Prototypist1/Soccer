using Common;
using Microsoft.Toolkit.Uwp.UI.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace RemoteSoccer
{
    class Zoomer
    {

        private double viewFrameWidth;
        private double viewFrameHeight;
        private Guid body;
        private double times = .1;

        public Zoomer(double viewFrameWidth, double viewFrameHeight, Guid body)
        {
            this.viewFrameWidth = viewFrameWidth;
            this.viewFrameHeight = viewFrameHeight;
            this.body = body;
        }

        public double GetTimes() => times;

        internal (double, double, double, double) Update(Position[] positionsList)
        {

            foreach (var position in positionsList)
            {
                if (position.Id == body)
                {
                    return (position.X,
                        position.Y,
                        (viewFrameWidth / 2.0) - (position.X * times),
                    (viewFrameHeight / 2.0) - (position.Y * times));
                }
            }

            throw new Exception("we are following something without a position");
        }

        internal void UpdateWindow(double actualWidth, double actualHeight)
        {
            this.viewFrameWidth = actualWidth;
            this.viewFrameHeight = actualHeight;
        }

        internal void SetTimes(double v)
        {
            times = v;
        }
    }

    class RenderGameEvents : IGameView
    {

        private class ElementEntry
        {
            private const int V = 0;
            public readonly Shape element;
            private readonly Brush[] brushes;

            public ElementEntry(Shape element, Color color)
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


        private Ellipse ball;
        private readonly LinkedList<MediaPlayer> players = new LinkedList<MediaPlayer>();

        private readonly MediaPlayer bell;

        private readonly Canvas gameArea;
        private readonly TextBlock fps;

        private readonly TextBlock leftScore, rightScore;
        private readonly Zoomer zoomer;

        private Random random = new Random();
        private readonly IReadonlyRef<int> frame;

        public RenderGameEvents(
            Canvas gameArea,
            TextBlock fps,
            TextBlock leftScore,
            TextBlock rightScore,
            Zoomer zoomer,
            IReadonlyRef<int> frame)
        {
            this.frame = frame;
            this.fps = fps;
            this.gameArea = gameArea;
            this.leftScore = leftScore;
            this.rightScore = rightScore;
            this.zoomer = zoomer;
            for (int i = 0; i < 10; i++)
            {

                var player = new MediaPlayer();
                player.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/hit{random.Next(1, 4)}.wav"));
                players.AddLast(player);

            }

            bell = new MediaPlayer();
            bell.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/bell.mp3"));


            ballWall = new Ellipse
            {
                Visibility = Visibility.Collapsed,
                Stroke = new SolidColorBrush(Color.FromArgb(0xff, 0xbb, 0xbb, 0xbb)),
                StrokeThickness = 20
            };

            Canvas.SetZIndex(ballWall, 0);

            this.gameArea.Children.Add(ballWall);




            var field = new Rectangle()
            {
                Width = Constants.xMax,
                Height = Constants.yMax,
                Fill = new SolidColorBrush(Color.FromArgb(0xff, 0xdd, 0xdd, 0xdd)),
            };
            Canvas.SetZIndex(field, -2);
            this.gameArea.Children.Add(field);

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
                this.gameArea.Children.Add(line);
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
                this.gameArea.Children.Add(line);
            }

        }

        public void HandleObjectsRemoved(ObjectsRemoved objectsRemoved)
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

        public async void HandleColorChanged(ColorChanged obj)
        {

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        () =>
                        {
                            var element = elements[obj.Id];
                            element.element.Fill = new SolidColorBrush(Color.FromArgb(obj.A, obj.R, obj.G, obj.B));
                            element.element.Stroke = new SolidColorBrush(Color.FromArgb(obj.A, obj.R, obj.G, obj.B));
                        });
        }

        public async void HandleNameChanged(NameChanged obj)
        {

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        () =>
                        {
                            var text = texts[obj.Id];
                            text.Text = obj.Name;
                        });
        }

        public void HandleObjectsCreated(ObjectsCreated objectsCreated)
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

                Shape shape;

                if (objectCreated is FootCreated)
                {
                    var points = new PointCollection();
                    points.Add(new Windows.Foundation.Point(0, 0));
                    points.Add(new Windows.Foundation.Point(0, 0));
                    points.Add(new Windows.Foundation.Point(0, 0));
                    points.Add(new Windows.Foundation.Point(0, 0));

                    var polygon = new Polygon()
                    {
                        Points = points,
                        Fill = new SolidColorBrush(color),
                        Stroke = new SolidColorBrush(color),//new SolidColorBrush(Color.FromArgb(0x88, 0xff, 0xff, 0xff)),
                        StrokeThickness = 2*Constants.playerPadding,
                        StrokeLineJoin = PenLineJoin.Round
                    };
                    shape = polygon;

                    shape.TransformMatrix =
                    // then we move to the right spot
                    new System.Numerics.Matrix4x4(
                        1, 0, 0, 0,
                        0, 1, 0, 0,
                        0, 0, 1, 0,
                        (float)(objectCreated.X), (float)(objectCreated.Y), 0, 1);
                }
                else
                {

                    var ellipse = new Ellipse()
                    {
                        Width = objectCreated.Diameter,
                        Height = objectCreated.Diameter,
                        Fill = new SolidColorBrush(color),
                    };
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

                    shape = ellipse;

                    if (objectCreated is BallCreated) {
                        ball = ellipse;
                    }


                }

                Canvas.SetZIndex(shape, objectCreated.Z);


                elements.Add(objectCreated.Id, new ElementEntry(shape, color));// new ElementEntry(ellipse, objectCreated.X, objectCreated.Y, objectCreated.Diameter));
                this.gameArea.Children.Add(shape);

                if (objectCreated is BodyCreated foot)
                {

                    var sum = foot.R + foot.G + foot.B;

                    var text = new TextBlock()
                    {
                        Text = foot.Name,
                        FontSize = 200,
                        Foreground = new SolidColorBrush(Color.FromArgb(0x88, 0xff, 0xff, 0xff))
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

                    this.gameArea.Children.Add(text);
                    texts.Add(foot.Id, text);
                }


            }
        }


        double framesRecieved = 0;
        private Stopwatch stopWatch = new Stopwatch();

        private long droppedFrames = 0;
        private long framesInGroup = 0;
        private long longestGap = 0;
        private long lastHandlePositions = 0;
        private int currentFrame = 0;


        Dictionary<Line, DateTime> lineTimes = new Dictionary<Line, DateTime>();

        public async Task HandlePositions(Positions positions)
        {

            framesRecieved++;
            framesInGroup++;

        Top:
            var myCurrentFrame = currentFrame;
            if (myCurrentFrame > positions.Frame)
            {
                droppedFrames++;
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


            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
            CoreDispatcherPriority.High,
            () =>
            {


                var (playerX, playerY, xPlus, yPlus) = zoomer.Update(positions.PositionsList);

                foreach (var position in positions.PositionsList)
                {

                    if (elements.TryGetValue(position.Id, out var element))
                    {


                        if (element.element is Ellipse)
                        {
                            var v = Math.Sqrt((position.Vx * position.Vx) + (position.Vy * position.Vy));


                            var Stretch = (v / element.element.Width);


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
                        }
                        else if (element.element is Polygon polygon)
                        {

                            var points = new PointCollection();
                            var skip = polygon.Points.Count > 2 ? 1 : 0;
                            double p1 = .98, p2 = 1 - p1;

                            for (int i = skip; i < (polygon.Points.Count / 2); i++)
                            {
                                var point = polygon.Points[i];
                                var pair = polygon.Points[(polygon.Points.Count - 1) - i];

                                points.Add(new Windows.Foundation.Point((point.X * p1 + pair.X * p2) - position.Vx, (point.Y * p1 + pair.Y * p2) - position.Vy));
                            }


                            // duplicate code
                            // serach for {3E1769BA-B690-4440-87BE-C74113D0D5EC}
                            var vv = new Physics2.Vector(position.Vx *10 , position.Vy *10);

                            if (vv.Length > Constants.PlayerRadius)
                            {
                                vv = vv.NewScaled(Constants.PlayerRadius / vv.Length);
                            }

                            points.Add(new Windows.Foundation.Point(vv.y, -vv.x));
                            points.Add(new Windows.Foundation.Point(-vv.y, vv.x));

                            for (int i = (polygon.Points.Count / 2); i < polygon.Points.Count - skip; i++)
                            {
                                var point = polygon.Points[i];
                                var pair = polygon.Points[(polygon.Points.Count - 1) - i];

                                points.Add(new Windows.Foundation.Point((point.X * p1 + pair.X * p2) - position.Vx, (point.Y * p1 + pair.Y * p2) - position.Vy));
                            }

                            polygon.Points = points;

                            polygon.TransformMatrix =
                                // then we move to the right spot
                                new System.Numerics.Matrix4x4(
                                    1, 0, 0, 0,
                                    0, 1, 0, 0,
                                    0, 0, 1, 0,
                                    (float)(position.X), (float)(position.Y), 0, 1);
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
                }


                var now = DateTime.Now;
                foreach (var child in this.gameArea.Children.ToList())
                {
                    if (child is Line line && lineTimes.TryGetValue(line, out var time) && now - time > TimeSpan.FromMilliseconds(800))
                    {
                        this.gameArea.Children.Remove(line);
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

                        Task.Run(() =>
                        {
                            item.Volume = Math.Min(1, (new Physics2.Vector(collision.Fx, collision.Fy).Length * new Physics2.Vector(collision.Fx, collision.Fy).Length / (400 * Math.Max(1, Math.Log(d)))));
                            item.AudioBalance = dx / d;
                            item.Play();
                        });
                    }
                    else
                    {

                        var dx = collision.X - playerX;
                        var dy = collision.Y - playerY;
                        var d = Math.Sqrt((dx * dx) + (dy * dy));
                        Task.Run(() =>
                        {
                            bell.Volume = Math.Min(1, .05 + (5.0 / (Math.Max(1, Math.Log(d)))));
                            bell.AudioBalance = dx / d;
                            bell.Play();
                        });
                    }

                    if (collision.IsGoal)
                    {

                        var line = new Line
                        {

                            X1 = (collision.Fx),
                            Y1 = (collision.Fy),
                            X2 = -(collision.Fx),
                            Y2 = -(collision.Fy),
                            StrokeThickness = 20,
                            Stroke = new SolidColorBrush(Colors.Black),
                            Opacity = 1,
                            OpacityTransition = new ScalarTransition() { Duration = TimeSpan.FromMilliseconds(800), },
                            Scale = new Vector3(.2f, .2f, 1f),
                            ScaleTransition = new Vector3Transition() { Duration = TimeSpan.FromMilliseconds(600) },
                            Translation = new Vector3((float)collision.X, (float)collision.Y, 0)

                        };
                        lineTimes.Add(line, now);
                        this.gameArea.Children.Add(line);
                        line.Opacity = 0f;
                        line.Scale = new Vector3(2, 2, 1);
                        Canvas.SetZIndex(line, Constants.footZ);
                    }
                    else
                    {

                        var line1 = new Line
                        {

                            X1 = -(collision.Fy * 10) * 2,
                            Y1 = (collision.Fx * 10) * 2,
                            X2 = -(10 * collision.Fy / 1.2),
                            Y2 = (10 * collision.Fx / 1.2),
                            StrokeThickness = 5 * 2,
                            Stroke = new SolidColorBrush(Colors.Black),
                            Opacity = 1,
                            OpacityTransition = new ScalarTransition() { Duration = TimeSpan.FromMilliseconds(400), },
                            Scale = new Vector3(.4f, .4f, 1f),
                            ScaleTransition = new Vector3Transition() { Duration = TimeSpan.FromMilliseconds(200) },
                            Translation = new Vector3((float)collision.X, (float)collision.Y, 0)

                        };
                        lineTimes.Add(line1, now);
                        this.gameArea.Children.Add(line1);
                        line1.Opacity = 0f;
                        line1.Scale = new Vector3(2, 2, 1);
                        Canvas.SetZIndex(line1, Constants.footZ);

                        var line2 = new Line
                        {

                            X1 = (collision.Fy * (10)) * 2,
                            Y1 = -(collision.Fx * (10)) * 2,
                            X2 = (10 * collision.Fy / 1.2),
                            Y2 = -(10 * collision.Fx / 1.2),
                            StrokeThickness = (5) * 2,
                            Stroke = new SolidColorBrush(Colors.Black),
                            Opacity = 1,
                            OpacityTransition = new ScalarTransition() { Duration = TimeSpan.FromMilliseconds(400), },
                            Scale = new Vector3(.4f, .4f, 1f),
                            ScaleTransition = new Vector3Transition() { Duration = TimeSpan.FromMilliseconds(100) },
                            Translation = new Vector3((float)collision.X, (float)collision.Y, 0)
                        };
                        lineTimes.Add(line2, now);
                        this.gameArea.Children.Add(line2);
                        line2.Opacity = 0f;
                        line2.Scale = new Vector3(2, 2, 1);
                        Canvas.SetZIndex(line2, Constants.footZ);
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

                this.gameArea.TransformMatrix = new Matrix4x4(
                    (float)(zoomer.GetTimes()), 0, 0, 0,
                    0, (float)(zoomer.GetTimes()), 0, 0,
                    0, 0, 1, 0,
                    (float)xPlus, (float)yPlus, 0, 1);

                if (frame.Thing % 20 == 0 && stopWatch != null)
                {
                    fps.Text = $"time to draw: {(stopWatch.ElapsedTicks - ticks) / (double)TimeSpan.TicksPerMillisecond:f2}{Environment.NewLine}" +
                        $"longest gap: {longestGap}{Environment.NewLine}" +
                        $"frame lag: {frame.Thing - positions.Frame}{Environment.NewLine}" +
                        $"frames: {framesInGroup * 3}{Environment.NewLine}" +
                        $"dropped: {droppedFrames * 3}{Environment.NewLine}" +
                        $"Escape: Show options";
                    framesInGroup = 0;
                    longestGap = 0;
                    droppedFrames = 0;
                }
            });

        }

        public async void HandleUpdateScore(UpdateScore obj)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        () =>
                        {
                            leftScore.Text = obj.Left.ToString();
                            rightScore.Text = obj.Right.ToString();
                        });
        }

        public async Task SpoolPositions(IAsyncEnumerable<Positions> positionss)
        {
            stopWatch.Start();
            await foreach (var item in positionss)
            {
                var dontWait = HandlePositions(item);
            }
        }

    }
}
