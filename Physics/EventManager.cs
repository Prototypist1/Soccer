using System;
using System.Collections.Generic;
using System.Linq;

namespace Physics
{
    internal class EventManager
    {

        public static void WhatHappensNext(PhysicsObject physicsObject, GridManager gridManager, EventManager eventManager, double endTime)
        {
            var couldHits = physicsObject.AddToGrid(gridManager);

            var nextStep = Math.Min(endTime, physicsObject.Time + (gridManager.stepSize / physicsObject.Speed));

            foreach (var couldHit in couldHits)
            {
                if (physicsObject != couldHit && physicsObject.TryNextCollision(couldHit, nextStep, out var collision))
                {
                    eventManager.AddEvent(collision);
                }
            }

            // we put in move events even if it is not moving so we update times
            if (physicsObject.Mobile && physicsObject.Time < endTime)
            {
                eventManager.AddMoveEvent(nextStep, physicsObject);
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

            private bool IsGood(double vf, double v)
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


                physicsObject1.RemoveFromGrid(gridManager);
                physicsObject2.RemoveFromGrid(gridManager);

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
                var normal = new Vector(dx, dy).NewUnitized();

                var v1 = normal.Dot(physicsObject1.Velocity);
                var m1 = physicsObject1.Mass;

                var v2 = normal.Dot(physicsObject2.Velocity);
                var m2 = physicsObject2.Mass;


                if (physicsObject1.Mobile == false)
                {
                    physicsObject2.Velocity = normal.NewScaled(-2 * v2).NewAdded(physicsObject2.Velocity);
                }
                else if (physicsObject2.Mobile == false)
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
                            var vf2_plus = (-B + Math.Sqrt(D)) / (2 * A);
                            var vf2_minus = (-B - Math.Sqrt(D)) / (2 * A);

                            if (IsGood(vf2_minus, v2) && IsGood(vf2_plus, v2) && vf2_plus != vf2_minus)
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
                            else if (IsGood(vf2_minus, v2))
                            {
                                vf2 = vf2_minus;
                            }
                            else if (IsGood(vf2_plus, v2))
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

                physicsObject.RemoveFromGrid(gridManager);
                physicsObject.X = x;
                physicsObject.Y = y;
                physicsObject.Time = Time;
                WhatHappensNext(physicsObject, gridManager, eventManager, endtime);
            }

        }



        private readonly LinkedList<IEvent> Events = new LinkedList<IEvent>();

        public void AddMoveEvent(double time, PhysicsObject physicsObject)
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
}
