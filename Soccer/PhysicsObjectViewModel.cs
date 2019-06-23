using Physics;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Soccer
{


    public class PhysicsObjectInbetween : IInbetween
    {
        public readonly PhysicsObject physicsObject;
        public UIElement Element => elipse;
        private readonly Ellipse elipse;

        public PhysicsObjectInbetween(double mass, double radius, double x, double y, double vx,double vy)
        {
            this.physicsObject = new PhysicsObject(mass, radius, x, y) {
                Vx = vx,
                Vy = vy
            };

            elipse = new Ellipse()
            {
                Width = 2 * physicsObject.Radius,
                Height = 2 * physicsObject.Radius,
                Fill = new SolidColorBrush(Windows.UI.Colors.Blue)
        };

            Update();
        }



        public void Update()
        {
            Canvas.SetTop(elipse, physicsObject.Y - physicsObject.Radius);
            Canvas.SetLeft(elipse, physicsObject.X - physicsObject.Radius);
            physicsObject.ApplyForce(-physicsObject.Vx * physicsObject.Mass / 1000, -physicsObject.Vy * physicsObject.Mass / 1000);
        }
    }

    public class PhyisEngineInbetween : IInbetween
    {
        private readonly PhysicsEngine physicsEngine;
        private readonly Canvas canvas;
        private List<PhysicsObjectInbetween> items = new List<PhysicsObjectInbetween>();
        private int time=0;

        public PhyisEngineInbetween(double stepSize, double height, double width, Canvas canvas)
        {
            physicsEngine = new PhysicsEngine(stepSize,height,width);
            this.canvas = canvas;
        }

        public void AddItem(double mass, double radius, double x, double y, double vx, double vy) {
            var toAdd = new PhysicsObjectInbetween(mass, radius, x, y,vx,vy);
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