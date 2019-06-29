using System;
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

    internal class GridManager
    {
        // goood thing you wrapped this....
        private class GridEntry
        {
            public readonly HashSet<PhysicsObject> physicsObjects = new HashSet<PhysicsObject>();

            public void Add(PhysicsObject physicsObject)
            {
                physicsObjects.Add(physicsObject);
            }

            public void Remove(PhysicsObject physicsObject)
            {
                physicsObjects.Remove(physicsObject);
            }
        }

        private readonly GridEntry[,] Grid;
        private readonly double height;
        private readonly double width;

        public GridManager(double stepSize, double height, double width)
        {
            StepSize = stepSize;
            this.height = height;
            this.width = width;
            Grid = new GridEntry[(int)Math.Ceiling(width / stepSize), (int)Math.Ceiling(height / stepSize)];

            for (int x = 0; x < (int)Math.Ceiling(width / stepSize); x++)
            {
                for (int y = 0; y < (int)Math.Ceiling(height / stepSize); y++)
                {
                    Grid[x, y] = new GridEntry();
                }
            }
        }

        public double StepSize { get; internal set; }

        public HashSet<PhysicsObject> AddToGrid(PhysicsObject physicsObject)
        {
            var couldHits = new HashSet<PhysicsObject>();

            var inner = physicsObject.GetBounds();
            if (physicsObject.mobile) {
                inner = new Bounds(
                    Help.BoundAddition(inner.maxX, StepSize) / StepSize, 
                    Help.BoundAddition(inner.maxY, StepSize) / StepSize, 
                    Help.BoundAddition(inner.minX, -StepSize) / StepSize, 
                    Help.BoundAddition(inner.minY, -StepSize) / StepSize);
            }

            var bound = new Bounds(
                Math.Floor(width/StepSize)-1,
                Math.Floor(height/StepSize)-1,
                0,
                0);

            for (int x = (int)Math.Floor(Math.Max(inner.minX/StepSize, bound.minX)); x <= (int)Math.Floor(Math.Min(inner.maxX, bound.maxX)); x++)
            {

                for (int y = (int)Math.Floor(Math.Max(inner.minY, bound.minY)); y <= (int)Math.Floor(Math.Min(inner.maxY, bound.maxY)); y++)
                {
                    couldHits.UnionWith(Grid[x, y].physicsObjects);
                    Grid[x, y].Add(physicsObject);
                }
            }

            return couldHits;

        }

        public void RemoveFromGrid(PhysicsObject physicsObject)
        {
            var inner = physicsObject.GetBounds();
            if (physicsObject.mobile)
            {
                inner = new Bounds(
                    Help.BoundAddition(inner.maxX, StepSize) / StepSize,
                    Help.BoundAddition(inner.maxY, StepSize) / StepSize,
                    Help.BoundAddition(inner.minX, -StepSize) / StepSize,
                    Help.BoundAddition(inner.minY, -StepSize) / StepSize);
            }

            var bound = new Bounds(
                Math.Floor(width / StepSize) -1,
                Math.Floor(height / StepSize) - 1,
                0,
                0);

            for (int x = (int)Math.Floor(Math.Max(inner.minX, bound.minX)); x <= (int)Math.Floor(Math.Min(inner.maxX, bound.maxX)); x++)
            {

                for (int y = (int)Math.Floor(Math.Max(inner.minY, bound.minY)); y <= (int)Math.Floor(Math.Min(inner.maxY, bound.maxY)); y++)
                {
                    Grid[x, y].Remove(physicsObject);
                }
            }            
        }
    }

    internal class EventManager
    {

        public static void WhatHappensNext(PhysicsObject physicsObject, GridManager gridManager, EventManager eventManager, double endTime)
        {
            var couldHits = gridManager.AddToGrid(physicsObject);

            var nextStep = Math.Min(endTime, physicsObject.Time + (gridManager.StepSize / physicsObject.Speed));

            foreach (var couldHit in couldHits)
            {
                if (physicsObject != couldHit && physicsObject.TryNextCollision(couldHit, nextStep, out var collision))
                {
                    eventManager.AddEvent(collision);
                }
            }


            if ((physicsObject.Vx != 0 || physicsObject.Vy != 0) && physicsObject.Time < endTime)
            {
                eventManager.AddMoveEent(nextStep, physicsObject);
            }
        }


        internal struct CollisiphysicsObjectEven : IEvent
        {
            public double Time { get; }
            private readonly PhysicsObject physicsObject1, physicsObject2;

            private readonly double x1, x2, y1, y2, start_x1, start_x2, start_y1, start_y2, start_vx1, start_vx2, start_vy1, start_vy2;

            private const double CLOSE = .01;

            public CollisiphysicsObjectEven(double time, PhysicsObject physicsObject1, PhysicsObject physicsObject2, double x1, double y1, double x2, double y2)
            {
                this.Time = time;
                this.physicsObject1 = physicsObject1;
                this.physicsObject2 = physicsObject2;
                this.x1 = x1;
                this.y1 = y1;
                this.x2 = x2;
                this.y2 = y2;
                this.start_x1 = physicsObject1.X;
                this.start_y1 = physicsObject1.Y;
                this.start_x2 = physicsObject2.X;
                this.start_y2 = physicsObject2.Y;
                this.start_vx1 = physicsObject1.Vx;
                this.start_vy1 = physicsObject1.Vy;
                this.start_vx2 = physicsObject2.Vx;
                this.start_vy2 = physicsObject2.Vy;
            }

            private bool isGood(double vf, double v)
            {
                return vf != v;
            }

            public void Enact(GridManager gridManager, EventManager eventManager, double endTime)
            {
                if (physicsObject1.X != start_x1 ||
                physicsObject1.Y != start_y1 ||
                physicsObject2.X != start_x2 ||
                physicsObject2.Y != start_y2 ||
                physicsObject1.Vx != start_vx1 ||
                physicsObject1.Vy != start_vy1 ||
                physicsObject2.Vx != start_vx2 ||
                physicsObject2.Vy != start_vy2)
                {
                    return;
                }


                gridManager.RemoveFromGrid(physicsObject1);
                gridManager.RemoveFromGrid(physicsObject2);

                physicsObject1.X = x1;
                physicsObject1.Y = y1;
                physicsObject1.Time = Time;

                physicsObject2.X = x2;
                physicsObject2.Y = y2;
                physicsObject2.Time = Time;

                // update the V of both

                // when a collision happen how does it go down?
                // the velocities we care about are normal to the line
                // we find the normal and take the dot product
                var dx = physicsObject1.X - physicsObject2.X;
                var dy = physicsObject1.Y - physicsObject2.Y;
                var normal = new Vector(dx, dy).NewNormalized();

                var v1 = normal.Dot(physicsObject1.Velocity);
                var m1 = physicsObject1.Mass;

                var v2 = normal.Dot(physicsObject2.Velocity);
                var m2 = physicsObject2.Mass;


                if (physicsObject1.mobile == false)
                {
                    physicsObject2.Velocity = normal.NewScaled(-2 * v2).NewAdded(physicsObject2.Velocity);
                }
                else if (physicsObject2.mobile == false)
                {
                    physicsObject1.Velocity = normal.NewScaled(-2 * v1).NewAdded(physicsObject1.Velocity);
                }
                else
                {

                    // we do the physics and we get a quadratic for vf2
                    var c1 = (v1 * m1) + (v2 * m2);
                    var c2 = (v1 * v1 * m1) + (v2 * v2 * m2);

                    var A = (m2 * m2) + (m2 * m1);
                    var B = -2 * m2 * c1;
                    var C = (c1 * c1) - (c2 * m1);


                    double vf2;

                    if (A != 0)
                    {
                        // b^2 - 4acS
                        var D = (B * B) - (4 * A * C);

                        if (D >= 0)
                        {
                            var vf2_plus = ((-B + Math.Sqrt(D)) / (2 * A));
                            var vf2_minus = (-B - Math.Sqrt(D)) / (2 * A);

                            if (isGood(vf2_minus, v2) && isGood(vf2_plus, v2) && vf2_plus != vf2_minus)
                            {
                                if (Math.Abs(v2 - vf2_plus) > Math.Abs(v2 - vf2_minus))
                                {
                                    if (Math.Abs(v2 - vf2_minus) > CLOSE)
                                    {
                                        throw new Exception("we are getting physicsObject2 vf2s: " + vf2_plus + "," + vf2_minus + " for vi2: " + v2);
                                    }
                                    vf2 = vf2_plus;
                                }
                                else
                                {
                                    if (Math.Abs(v2 - vf2_plus) > CLOSE)
                                    {
                                        throw new Exception("we are getting physicsObject2 vf2s: " + vf2_plus + "," + vf2_minus + " for vi2: " + v2);
                                    }
                                    vf2 = vf2_minus;
                                }
                            }
                            else if (isGood(vf2_minus, v2))
                            {
                                vf2 = vf2_minus;
                            }
                            else if (isGood(vf2_plus, v2))
                            {
                                vf2 = vf2_plus;
                            }
                            else
                            {
                                throw new Exception("we are getting no vfs");
                            }
                        }
                        else
                        {
                            throw new Exception("should not be negative");
                        }
                    }
                    else
                    {
                        throw new Exception("A should not be 0! if A is zer something has 0 mass");
                    }
                    physicsObject2.Velocity = normal.NewScaled(vf2).NewAdded(normal.NewScaled(-v2)).NewAdded(physicsObject2.Velocity);

                    var f = (vf2 - v2) * m2;
                    var vf1 = v1 - (f / m1);
                    physicsObject1.Velocity = normal.NewScaled(vf1).NewAdded(normal.NewScaled(-v1)).NewAdded(physicsObject1.Velocity);
                }


                WhatHappensNext(physicsObject1, gridManager, eventManager, endTime);
                WhatHappensNext(physicsObject2, gridManager, eventManager, endTime);
            }
        }

        internal struct MoveEvent : IEvent
        {
            public double Time { get; }

            private readonly PhysicsObject physicsObject;
            private readonly double x, y, start_x, start_y, start_vx, start_vy;

            public MoveEvent(double time, PhysicsObject physicsObject, double X, double Y)
            {
                this.Time = time;
                this.physicsObject = physicsObject;
                x = X;
                y = Y;
                start_x = physicsObject.X;
                start_y = physicsObject.Y;
                this.start_vx = physicsObject.Vx;
                this.start_vy = physicsObject.Vy;
            }

            public void Enact(GridManager gridManager, EventManager eventManager, double endtime)
            {
                if (physicsObject.X != start_x ||
                physicsObject.Y != start_y ||
                physicsObject.Vx != start_vx ||
                physicsObject.Vy != start_vy)
                {
                    return;
                }

                gridManager.RemoveFromGrid(physicsObject);
                physicsObject.X = x;
                physicsObject.Y = y;
                physicsObject.Time = Time;
                WhatHappensNext(physicsObject, gridManager, eventManager, endtime);
            }

        }



        private readonly LinkedList<IEvent> Events = new LinkedList<IEvent>();

        public void AddMoveEent(double time, PhysicsObject physicsObject)
        {
            var toAdd = new MoveEvent(time, physicsObject, physicsObject.X + ((time - physicsObject.Time) * physicsObject.Vx), physicsObject.Y + ((time - physicsObject.Time) * physicsObject.Vy));
            AddEvent(toAdd);
        }

        private void AddEvent(IEvent toAdd)
        {
            if (Events.First == null)
            {
                Events.AddLast(toAdd);
            }
            else
            {
                // could be expensive
                var at = Events.First;
                while (at.Value.Time < toAdd.Time)
                {
                    if (at.Next == null)
                    {

                        Events.AddLast(toAdd);
                        return;
                    }

                    at = at.Next;
                }
                Events.AddBefore(at, toAdd);
            }
        }


        public void RunAll(double time, GridManager gridManager)
        {
            while (Events.Any())
            {
                var first = Events.First.Value;
                Events.RemoveFirst();
                first.Enact(gridManager, this, time);
            }
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
            gridManager.AddToGrid(physicsObject);
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
