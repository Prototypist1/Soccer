using Physics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Soccer
{
    public class Player : IInbetween
    {
        private class Center {
            public double x, y, vx, vy, fx, fy;

            public void Update() {

                vx += fx;
                vy += fy;

                x += vx;
                y += vy;

                //fx = -vx / 100.0;
                //fy = -vy / 100.0;
            }
        }


        public readonly PhysicsObject physicsObject;
        private readonly double top;
        private readonly double left;
        private readonly Center center = new Center();

        private double targetVx, targetVy;

        public UIElement Element
        {
            get;
        }

        public Player(PhysicsObject physicsObject, UIElement Element, double top, double left)
        {
            this.physicsObject = physicsObject;

            this.center.x = physicsObject.X;
            this.center.y = physicsObject.Y;

            this.center.vx = .1;

            this.Element = Element;
            this.top = top;
            this.left = left;
            Update();
        }

        public void ForceOnCenter(double fx, double fy) {
            center.fx += fx;
            center.fy += fy;
        }

        public void SetTargetV(double vx, double vy) {
            this.targetVx = vx;
            this.targetVy = vy;
        }

        public void Update()
        {
            Canvas.SetTop(Element, physicsObject.Y - top);
            Canvas.SetLeft(Element, physicsObject.X - left);

            var target  = new Vector( physicsObject.X + targetVx - (center.x),physicsObject.Y + targetVy - (center.y));

            var max = 200.0;
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