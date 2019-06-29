﻿using Physics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
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
                var ball = PhysicsObjectBuilder.Ball(1, 40, 100, 100);

                var elispe = new Ellipse()
                {
                    Width = 80,
                    Height = 80,
                    Fill = new SolidColorBrush(Colors.Red),

                };
                engine.AddPlayer(ball, elispe, 40, 40);
            }

            //var r = new Random();

            foreach (var x in new[] { 300})
            {
                foreach (var y in new[] { 300 })
                {
                    var ball = PhysicsObjectBuilder.Ball(1, 40, x, y);

                    //ball.Vx = r.NextDouble() * 2 - 1;
                    //ball.Vy = r.NextDouble() * 2 - 1;
                    var elispe = new Ellipse()
                    {
                        Width = 80,
                        Height = 80,
                        Fill = new SolidColorBrush(Colors.Black),

                    };
                    engine.AddBall(ball, elispe, 40,40);

                }
            }


            foreach (var i in new[] { 100, 800 })
            {

                var line = PhysicsObjectBuilder.VerticalLine(i);

                var rectangle = new Rectangle()
                {
                    Width = 1,
                    Height = 2000,
                    Fill = new SolidColorBrush(Colors.Black),
                };
                engine.AddBall(line, rectangle,0,0);
            }


            foreach (var i in new[] { 100, 800 })
            {

                var line = PhysicsObjectBuilder.HorizontalLine(i);

                var rectangle = new Rectangle()
                {
                    Width = 2000,
                    Height = 1,
                    Fill = new SolidColorBrush(Colors.Black),
                };
                engine.AddBall(line, rectangle, 0, 0);
            }

            PrivateStart();
        }

        bool firstFrame = true;
        double lastX, lastY;
        internal void MouseAt(Point position)
        {
            if (firstFrame) {
                lastX = position.X;
                lastY = position.Y;
                firstFrame = false;
                return;
            }

            var vx = position.X - lastX;
            var vy = position.Y - lastY;

            if (player != null) {
                player.SetTargetV(vx, vy);
            }

            lastX = position.X;
            lastY = position.Y;
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

                engine.Update();
                Message = $"fps: {frameNumber / sw.Elapsed.TotalSeconds}";

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Message)));
            }


        }
    }
}