using Physics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Soccer
{
    public class GameModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private double frameNumber = 0;
        private PhyisEngineInbetween engine;
        private Player player;

        public void Start(Canvas canvas)
        {
            engine = new PhyisEngineInbetween(100, 2000, 2000, canvas);

            {
                var ball = PhysicsObjectBuilder.Ball(1, 40, 450, 450);

                var elispe = new Ellipse()
                {
                    Width = 80,
                    Height = 80,
                    Fill = new SolidColorBrush(Colors.Red),

                };

                var area = new Ellipse()
                {
                    Width = 400,
                    Height = 400,
                    Fill = new SolidColorBrush(Colors.Red),
                    Opacity = .2
                };

                player = engine.AddPlayer(ball, elispe, area, 40, 40,210,590,210,590);
            }

            {
                var ball = PhysicsObjectBuilder.Ball(1, 40, 300, 300);

                var elispe = new Ellipse()
                {
                    Width = 80,
                    Height = 80,
                    Fill = new SolidColorBrush(Colors.Black),

                };
                engine.AddBall(ball, elispe, 40, 40);
            }

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
                var line = PhysicsObjectBuilder.Line(side.Item1,side.Item2);

                var rectangle = new Line()
                {
                    X1 = side.Item1.x,
                    X2 = side.Item2.x,
                    Y1 = side.Item1.y,
                    Y2 = side.Item2.y,
                    Stroke = new SolidColorBrush(Colors.Black),
                };
                engine.AddBall(line, rectangle,0,0);
            }

            var point = CoreWindow.GetForCurrentThread().PointerPosition;
            lastX = point.X;
            lastY = point.Y;

            PrivateStart();
        }

        double lastX, lastY;
        private void MouseAt(double x, double y)
        {
            var vx = x - lastX;
            var vy = y - lastY;

            if (player != null) {
                player.SetTargetV(vx, vy);
            }

            lastX = x;
            lastY = y;
        }

        public Ball[] PhysicsItems { get; private set; } = new Ball[] { };
        public string Message { get; private set; } = "";

        private async void PrivateStart()
        {
            await Task.Delay(500);
            var sw = new Stopwatch();
            sw.Start();
            var rand = new Random();
            while (true)
            {

                await Task.Delay(1);
                frameNumber++;

                var point = Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition;
                MouseAt(point.X, point.Y);


#pragma warning disable IDE0047 // Remove unnecessary parentheses
                var keyForce = new Vector((
                        (Window.Current.CoreWindow.GetKeyState(VirtualKey.A).HasFlag(CoreVirtualKeyStates.Down) ? -1.0 : 0.0) +
                        (Window.Current.CoreWindow.GetKeyState(VirtualKey.D).HasFlag(CoreVirtualKeyStates.Down) ? 1.0 : 0.0)),
                    (
                        (Window.Current.CoreWindow.GetKeyState(VirtualKey.W).HasFlag(CoreVirtualKeyStates.Down) ? -1.0 : 0.0) +
                        (Window.Current.CoreWindow.GetKeyState(VirtualKey.S).HasFlag(CoreVirtualKeyStates.Down) ? 1.0 : 0.0)));
#pragma warning restore IDE0047 // Remove unnecessary parentheses

                var speed = .2;

                if (keyForce.Length > 0)
                {

                    keyForce = keyForce.NewUnitized().NewScaled(speed);
                    player.ForceOnCenter(keyForce.x, keyForce.y);

                }
                //else {
                //    player.Damp();
                //}

                engine.Update();
                Message = $"fps: {frameNumber / sw.Elapsed.TotalSeconds}";

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Message)));
            }


        }
    }
}