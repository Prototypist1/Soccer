using Physics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Soccer
{
    public class Ball : IInbetween
    {
        public readonly PhysicsObject physicsObject;
        public UIElement Element { get; }
        private readonly double top;
        private readonly double left;

        public Ball(PhysicsObject physicsObject, UIElement Element, double top, double left)
        {
            this.physicsObject = physicsObject;

            this.Element = Element;
            this.top = top;
            this.left = left;
            Update();
        }



        public void Update()
        {
            Canvas.SetTop(Element, physicsObject.Y - top);
            Canvas.SetLeft(Element, physicsObject.X - left);
            physicsObject.ApplyForce(-physicsObject.Vx * physicsObject.Mass / 100, -physicsObject.Vy * physicsObject.Mass / 100);
        }
    }
}