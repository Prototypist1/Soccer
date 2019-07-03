using Physics;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Soccer
{
    public class Player : IInbetween
    {
        private class Center {
            public double x, y, vx, vy, fx, fy, maxX, minX, minY, maxY;

            public void Update() {

                vx += fx;
                vy += fy;

                x += vx;
                y += vy;

                if (x > maxX) {
                    x = maxX;
                    vx = Math.Min(0,vx);
                }

                if (y > maxY)
                {
                    y = maxY;
                    vy = Math.Min(0, vy);
                }

                if (x < minX)
                {
                    x = minX;
                    vx = Math.Max(0, vx);
                }

                if (y < minY)
                {
                    y = minY;
                    vy = Math.Max(0, vy);
                }

                fx = -vx / 100.0;
                fy = -vy / 100.0;
                
            }
        }


        public readonly PhysicsObject physicsObject;
        private readonly UIElement area;
        private readonly double top;
        private readonly double left;
        private readonly Center center = new Center();

        private double targetVx, targetVy;

        public UIElement Element
        {
            get;
        }

        public Player(PhysicsObject physicsObject, UIElement Element, UIElement area, double top, double left, double minX, double maxX, double minY, double maxY)
        {
            this.physicsObject = physicsObject;

            this.center.x = physicsObject.X;
            this.center.y = physicsObject.Y;
            this.center.maxX = maxX;
            this.center.minX = minX;
            this.center.maxY = maxY;
            this.center.minY = minY;
            
            this.Element = Element;
            this.area = area;
            this.top = top;
            this.left = left;
            Update();
        }

        public void ForceOnCenter(double fx, double fy) {
            center.fx += fx;
            center.fy += fy;
        }

        //public void Damp() {

        //    center.fx = -center.vx / 50.0;
        //    center.fy = -center.vy / 50.0;
        //}

        public void SetTargetV(double vx, double vy) {
            this.targetVx = vx;
            this.targetVy = vy;
        }

        public void Update()
        {
            Canvas.SetTop(Element, physicsObject.Y - top);
            Canvas.SetLeft(Element, physicsObject.X - left);


            var max = 200.0;
            Canvas.SetTop(area, center.y - max);
            Canvas.SetLeft(area, center.x - max);

            var target  = new Vector( physicsObject.X + targetVx - (center.x),physicsObject.Y + targetVy - (center.y));

            if (target.Length > max) {
                target = target.NewScaled(max / target.Length);
            }

            targetVx = ((target.x + center.x + center.vx)  - physicsObject.X);
            targetVy = (target.y + center.y + center.vy) - physicsObject.Y;

            physicsObject.ApplyForce(
                (targetVx - physicsObject.Vx) * physicsObject.Mass / 2.0,
                (targetVy - physicsObject.Vy) * physicsObject.Mass / 2.0);

            center.Update();
        }
    }
}