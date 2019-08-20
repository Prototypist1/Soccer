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


        MightBeCollision Enact(GridManager gridManager, EventManager eventManager, double endtime);
    }

    public class MightBeCollision
    {
        Collision collision;
        bool isIt;

        public MightBeCollision(Collision collision)
        {
            this.collision = collision;
            this.isIt = true;
        }

        public MightBeCollision()
        {
            this.isIt = false;
        }

        public bool IsIt(out Collision collision) {
            collision = this.collision;
            return isIt;
        }
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

        public Collision[] Simulate(double time)
        {
            foreach (var item in items)
            {
                EventManager.WhatHappensNext(item, gridManager, eventManager, time);
            }
            return eventManager.RunAll(time, gridManager);
        }
    }


    public struct Collision
    {
        public Collision(double x, double y, double fx, double fy, bool isGoal)
        {
            X = x;
            Y = y;
            Fx = fx;
            Fy = fy;
            IsGoal = isGoal;
        }

        public double X { get; set; }
        public double Y { get; set; }
        public double Fx { get; set; }
        public double Fy { get; set; }
        public bool IsGoal { get; set; }
    }


}
