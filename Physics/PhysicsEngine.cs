using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Physics.PhysicsObject;

namespace Physics
{

    internal static class EvemtExtensions
    {
        public static bool HappensBefore(this IEvent self, IEvent other)
        {
            if (self.Time < other.Time)
            {
                return true;
            }

            if (self.Time == other.Time)
            {
                return self is EventManager.CollisiphysicsObjectEven && other is EventManager.MoveEvent;
            }

            return false;
        }
    }

    internal interface IEvent
    {
        double Time { get; }


        void Enact(GridManager gridManager, EventManager eventManager, double endtime);
    }

    public class PhysicsEngine
    {

        public readonly List<PhysicsObject> items = new List<PhysicsObject>();
        private readonly GridManager gridManager;
        private readonly EventManager eventManager;

        public PhysicsEngine(double stepSize, double height, double width)
        {
            gridManager = new GridManager(stepSize, height, width);
            eventManager = new EventManager();
        }

        public void AddObject(PhysicsObject physicsObject)
        {
            items.Add(physicsObject);
            physicsObject.AddToGrid(gridManager);
        }

        public void Simulate(double time)
        {
            foreach (var item in items)
            {
                EventManager.WhatHappensNext(item, gridManager, eventManager, time);
            }
            eventManager.RunAll(time, gridManager);
        }
    }
}
