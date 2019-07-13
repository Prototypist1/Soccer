using Common;
using Physics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Soccer
{

    public class Player : IInbetween
    {
        public readonly PhysicsObject physicsObject;
        private readonly UIElement area;
        private readonly double top;
        private readonly double left;
        private readonly Center center;

        private double targetVx, targetVy;

        public UIElement Element
        {
            get;
        }

        public Player(PhysicsObject physicsObject, UIElement Element, UIElement area, double top, double left, double minX, double maxX, double minY, double maxY)
        {
            this.physicsObject = physicsObject;

            this.center = new Center(
                physicsObject.X,
                physicsObject.Y,
                maxX,
                minX,
                minY,
                maxY);

            this.Element = Element;
            this.area = area;
            this.top = top;
            this.left = left;
            Update();
        }

        public void ForceOnCenter(double fx, double fy) {

            center.ApplyForce(fx,fy);
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
            Canvas.SetTop(area, center.Y - max);
            Canvas.SetLeft(area, center.X - max);

            var target  = new Vector( physicsObject.X + targetVx - (center.X),physicsObject.Y + targetVy - (center.Y));

            if (target.Length > max) {
                target = target.NewScaled(max / target.Length);
            }

            targetVx = ((target.x + center.X + center.vx)  - physicsObject.X);
            targetVy = (target.y + center.Y + center.vy) - physicsObject.Y;

            physicsObject.ApplyForce(
                (targetVx - physicsObject.Vx) * physicsObject.Mass / 2.0,
                (targetVy - physicsObject.Vy) * physicsObject.Mass / 2.0);

            center.Update();
        }
    }
}