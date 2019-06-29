using Physics;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Soccer
{
    public class Player : IInbetween
    {


        public readonly PhysicsObject physicsObject;
        private readonly double top;
        private readonly double left;

        private double targetVx, targetVy;

        public UIElement Element
        {
            get;
        }

        public Player(PhysicsObject physicsObject, UIElement Element, double top, double left)
        {
            this.physicsObject = physicsObject;

            this.Element = Element;
            this.top = top;
            this.left = left;
            Update();
        }

        public void SetTargetV(double vx, double vy) {
            this.targetVx = vx;
            this.targetVy = vy;
        }

        public void Update()
        {
            Canvas.SetTop(Element, physicsObject.Y - top);
            Canvas.SetLeft(Element, physicsObject.X - left);
            physicsObject.ApplyForce(
                (targetVx - physicsObject.Vx) * physicsObject.Mass,
                (targetVy - physicsObject.Vy) * physicsObject.Mass);
        }
    }



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
            //physicsObject.ApplyForce(-physicsObject.Vx * physicsObject.Mass / 1000, -physicsObject.Vy * physicsObject.Mass / 1000);
        }
    }

    public class PhyisEngineInbetween : IInbetween
    {
        private readonly PhysicsEngine physicsEngine;
        private readonly Canvas canvas;
        private List<IInbetween> items = new List<IInbetween>();
        private int time=0;

        public PhyisEngineInbetween(double stepSize, double height, double width, Canvas canvas)
        {
            physicsEngine = new PhysicsEngine(stepSize,height,width);
            this.canvas = canvas;
        }

        public void AddPlayer(PhysicsObject physicsObject, UIElement element, double top, double left) {
            var toAdd = new Player(physicsObject, element, top, left);
            physicsEngine.AddObject(toAdd.physicsObject);
            this.canvas.Children.Add(toAdd.Element);
            items.Add(toAdd);
        }

        public void AddBall(PhysicsObject physicsObject, UIElement element,double top, double left) {
            var toAdd = new Ball(physicsObject, element, top, left);
            physicsEngine.AddObject(toAdd.physicsObject);
            this.canvas.Children.Add(toAdd.Element);
            items.Add(toAdd);

        }

        public UIElement Element => canvas;

        public void Update()
        {
            physicsEngine.Simulate(time++);
            foreach (var item in items)
            {
                item.Update();
            }
            canvas.UpdateLayout();
        }
    }
}