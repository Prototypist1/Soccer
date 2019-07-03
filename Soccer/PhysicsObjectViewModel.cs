using Physics;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Soccer
{

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

        public Player AddPlayer(PhysicsObject physicsObject, UIElement element, UIElement area, double top, double left, double minX, double maxX, double minY, double maxY) {
            var toAdd = new Player(physicsObject, element,area, top, left, minX, maxX, minY, maxY);
            physicsEngine.AddObject(toAdd.physicsObject);
            this.canvas.Children.Add(area);
            this.canvas.Children.Add(toAdd.Element);
            items.Add(toAdd);
            return toAdd;
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