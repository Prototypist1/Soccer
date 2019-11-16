using physics2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Physics2
{


    internal static class PhysicsMath
    {

        private const double CLOSE = .01;

        internal static DoubleUpdatePositionVelocityEvent DoCollision(
            PhysicsObject physicsObject1, 
            IPhysicsObject physicsObject2, 
            double radius1, 
            double time, 
            Vector normal,
            Vector velocityVector1,
            Vector velocityVector2
            )
        {
            // update the V of both

            // when a collision happen how does it go down?
            // the velocities we care about are normal to the line
            // we find the normal and take the dot product

            var v1 = normal.Dot(velocityVector1);
            var m1 = physicsObject1.Mass;

            var v2 = normal.Dot(velocityVector2);
            var m2 = physicsObject2.Mass;



            if (physicsObject1.Mobile == false)
            {
                var o2v = normal.NewScaled(-2 * v2).NewAdded(velocityVector2);
                return new DoubleUpdatePositionVelocityEvent(
                    time,
                    physicsObject1,
                    physicsObject1.Vx,
                    physicsObject1.Vy,
                    physicsObject2,
                    o2v.x,
                    o2v.y,
                    new MightBeCollision(
                     new Collision(
                        physicsObject1.X + (time * physicsObject1.Vx) + normal.NewScaled(radius1).x,
                        physicsObject1.Y + (time * physicsObject1.Vy) + normal.NewScaled(radius1).y,
                    normal.NewScaled(-2 * v2 * m2).x,
                    normal.NewScaled(-2 * v2 * m2).y,
                    false
                )));
            }
            else if (physicsObject2.Mobile == false)
            {
                var v1o = normal.NewScaled(-2 * v1).NewAdded(velocityVector1);
                return new DoubleUpdatePositionVelocityEvent(
                    time,
                    physicsObject1,
                    v1o.x,
                    v1o.y,
                    physicsObject2,
                    physicsObject2.Vx,
                    physicsObject2.Vy,
                    new MightBeCollision(
                        new Collision(
                        physicsObject1.X + (time * physicsObject1.Vx) + normal.NewScaled(radius1).x,
                        physicsObject1.Y + (time * physicsObject1.Vy) + normal.NewScaled(radius1).y,
                            normal.NewScaled(-2 * v2 * m2).x,
                            normal.NewScaled(-2 * v2 * m2).y,
                            false)));
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
                    // b^2 - 4ac
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

                {
                    var o2v = normal.NewScaled(vf2).NewAdded(normal.NewScaled(-v2)).NewAdded(velocityVector2);

                    var f = (vf2 - v2) * m2;
                    var vf1 = v1 - (f / m1);
                    var o1v = normal.NewScaled(vf1).NewAdded(normal.NewScaled(-v1)).NewAdded(velocityVector1);
                    return new DoubleUpdatePositionVelocityEvent(
                    time,
                    physicsObject1,
                    o1v.x,
                    o1v.y,
                    physicsObject2,
                    o2v.x,
                    o2v.y,
                    new MightBeCollision(new Collision(
                        physicsObject1.X + (time * physicsObject1.Vx) + normal.NewScaled(radius1).x,
                        physicsObject1.Y + (time * physicsObject1.Vy) + normal.NewScaled(radius1).y,
                        normal.NewScaled(f).x,
                        normal.NewScaled(f).y,
                        false
                    )));
                }
            }
        }

        private static Vector GetNormal(IPhysicsObject physicsObject1, IPhysicsObject physicsObject2, double time)
        {
            var dx = physicsObject1.X + (time * physicsObject1.Vx) - (physicsObject2.X + (time * physicsObject2.Vx));
            var dy = physicsObject1.Y + (time * physicsObject1.Vy) - (physicsObject2.Y + (time * physicsObject2.Vy));
            var normal = new Vector(dx, dy).NewUnitized();
            return normal;
        }

        private static bool IsGood(double vf, double v)
        {
            return vf != v;
        }


        internal static bool TryCollisionBall(PhysicsObject self, 
            IPhysicsObject collider,
            double particalX,
            double particalY,
            double particalVx,
            double particalVy,
            Circle c1, 
            Circle c2, 
            double endTime, 
            out IEvent evnt)
        {

            // how  are they moving relitive to us
            double DVX = particalVx - self.Vx,
                   DVY = particalVy - self.Vy;

            var thisX0 = self.X;
            var thisY0 = self.Y;
            var thatX0 = particalX;
            var thatY0 = particalY;

            // how far they are from us
            var DX = thatX0 - thisX0;
            var DY = thatY0 - thisY0;

            // if the objects are not moving towards each other dont bother
            var V = -new Vector(DVX, DVY).Dot(new Vector(DX, DY).NewUnitized());
            if (V <= 0)
            {
                evnt = default;
                return false;
            }

            var R = c1.Radius + c2.Radius;

            var A = (DVX * DVX) + (DVY * DVY);
            var B = 2 * ((DX * DVX) + (DY * DVY));
            var C = (DX * DX) + (DY * DY) - (R * R);

            if (TrySolveQuadratic(A, B, C, out var time) && time <= endTime)
            {

                evnt = DoCollision(self, collider, c1.Radius, time, GetNormal(self, collider, time), self.Velocity, collider.Velocity);
                return true;
            }
            evnt = default;
            return false;
        }



        internal static bool TryCollisionPointCloudParticle(
            PhysicsObject self,
            PhysicsObject collider,
            double particalX,
            double particalY,
            double particalVx,
            double particalVy
            , Circle c1, 
            Circle c2, double endTime, out IEvent evnt)
        {

            // how  are they moving relitive to us
            double DVX = particalVx - self.Vx,
                   DVY = particalVy - self.Vy;

            var thisX0 = self.X;
            var thisY0 = self.Y;

            // how far they are from us
            var DX = particalX - thisX0;
            var DY = particalY - thisY0;

            // if the objects are not moving towards each other dont bother
            var V = -new Vector(DVX, DVY).Dot(new Vector(DX, DY).NewUnitized());
            if (V <= 0)
            {
                evnt = default;
                return false;
            }

            var R = c1.Radius + c2.Radius;

            var A = (DVX * DVX) + (DVY * DVY);
            var B = 2 * ((DX * DVX) + (DY * DVY));
            var C = (DX * DX) + (DY * DY) - (R * R);

            if (TrySolveQuadratic(A, B, C, out var time) && time <= endTime)
            {

                evnt = DoCollision(self, collider, c1.Radius, time, new Vector(collider.Vx, collider.Vy).NewUnitized(), self.Velocity, collider.Velocity);
                return true;
            }
            evnt = default;
            return false;
        }


        public static bool TrySolveQuadratic(double a, double b, double c, out double res)
        {
            var sqrtpart = (b * b) - (4 * a * c);
            double x1, x2;
            if (sqrtpart > 0)
            {
                x1 = (-b + Math.Sqrt(sqrtpart)) / (2 * a);
                x2 = (-b - Math.Sqrt(sqrtpart)) / (2 * a);
                res = Math.Min(x1, x2);
                return true;
            }
            else if (sqrtpart < 0)
            {
                res = default;
                return false;
            }
            else
            {
                res = (-b + Math.Sqrt(sqrtpart)) / (2 * a);
                return true;
            }
        }


        public static  IEnumerable<double> SolveQuadratic(double a, double b, double c)
        {
            var sqrtpart = (b * b) - (4 * a * c);
            double x1, x2;
            if (sqrtpart > 0)
            {
                x1 = (-b + Math.Sqrt(sqrtpart)) / (2 * a);
                x2 = (-b - Math.Sqrt(sqrtpart)) / (2 * a);
                return new[] { x1,x2};
            }
            else if (sqrtpart < 0)
            {
                
                return new double[] { };
            }
            else
            {
                return new double[] { (-b + Math.Sqrt(sqrtpart)) / (2 * a) };
            }
        }


        internal static bool TryNextCollisionBallLine(PhysicsObject self, PhysicsObject line, Circle circle, Line lineShape, double endTime, out IEvent collision)
        {

            var normalDistance = new Vector(self.X, self.Y).Dot(lineShape.NormalUnit.NewUnitized());//myPhysicsObject.X
            var normalVelocity = new Vector(self.Vx - line.Vx, self.Vy - line.Vy).Dot(lineShape.NormalUnit.NewUnitized());//myPhysicsObject.Vx
            var lineNormalDistance = lineShape.NormalDistance;//line.X

            if (lineNormalDistance > normalDistance)
            {
                if (normalVelocity > 0)
                {
                    var time = (lineNormalDistance - (normalDistance + circle.Radius)) / normalVelocity;

                    var directionDistance = self.Position.NewAdded(self.Velocity.NewScaled(time)).Dot(lineShape.DirectionUnit) -
                    line.Position.NewAdded(line.Velocity.NewScaled(time)).Dot(lineShape.DirectionUnit);


                    if (time < endTime && Math.Abs(directionDistance) < lineShape.Length * .5)
                    {
                        collision = DoCollision(self, line, circle.Radius, time, lineShape.NormalUnit, self.Velocity, line.Velocity);
                        return true;
                    }
                    else
                    {
                        collision = default;
                        return false;
                    }
                }
                else
                {
                    collision = default;
                    return false;
                }
            }
            else if (lineNormalDistance < normalDistance)
            {
                if (normalVelocity < 0)
                {
                    var time = (lineNormalDistance - (normalDistance - circle.Radius)) / normalVelocity;

                    var directionDistance = self.Position.NewAdded(self.Velocity.NewScaled(time)).Dot(lineShape.DirectionUnit) -
                    line.Position.NewAdded(line.Velocity.NewScaled(time)).Dot(lineShape.DirectionUnit);


                    if (time < endTime && Math.Abs(directionDistance) < lineShape.Length * .5)
                    {
                        collision = DoCollision(self, line, circle.Radius, time, lineShape.NormalUnit, self.Velocity, line.Velocity);
                        return true;
                    }
                    else
                    {
                        collision = default;
                        return false;
                    }
                }
                else
                {
                    collision = default;
                    return false;
                }
            }
            else //if (lineNormalDistance == normalDistance)
            {
                if (normalVelocity < 0)
                {
                    var time = (lineNormalDistance - (normalDistance - circle.Radius)) / normalVelocity;

                    var directionDistance = self.Position.NewAdded(self.Velocity.NewScaled(time)).Dot(lineShape.DirectionUnit) -
                    line.Position.NewAdded(line.Velocity.NewScaled(time)).Dot(lineShape.DirectionUnit);


                    if (time < endTime && Math.Abs(directionDistance) < lineShape.Length * .5)
                    {
                        collision = DoCollision(self, line, circle.Radius, time, lineShape.NormalUnit, self.Velocity, line.Velocity);
                        return true;
                    }
                    else
                    {
                        collision = default;
                        return false;
                    }
                }
                else if (normalVelocity > 0)
                {
                    var time = (lineNormalDistance - (normalDistance + circle.Radius)) / normalVelocity;

                    var directionDistance = self.Position.NewAdded(self.Velocity.NewScaled(time)).Dot(lineShape.DirectionUnit) -
                    line.Position.NewAdded(line.Velocity.NewScaled(time)).Dot(lineShape.DirectionUnit);


                    if (time < endTime && Math.Abs(directionDistance) < lineShape.Length * .5)
                    {
                        collision = DoCollision(self, line, circle.Radius, time, lineShape.NormalUnit, self.Velocity, line.Velocity);
                        return true;
                    }
                    else
                    {
                        collision = default;
                        return false;
                    }
                }
                else //normalVelocity == 0
                {
                    // how?? :(
                    collision = default;
                    return false;
                }
            }
        }

        internal static bool TryCollisionBallLine2(
            PhysicsObject ball, 
            PhysicsObject line, 
            Circle circle, 
            Line lineShape, 
            double endTime, 
            Vector lineParallelStart,
            Vector lineParallelEnd,
            out IEvent collision) {

            var DDX = ball.Vx - line.Vx;
            var DDY = ball.Vy - line.Vy;
            var DX = ball.X - line.X;
            var DY = ball.Y - line.Y;
            var PX = lineParallelStart.x;
            var PY = lineParallelStart.y;
            var DPX = lineParallelEnd.x - lineParallelStart.x;
            var DPY = lineParallelEnd.y - lineParallelStart.y;

            var A = DDX * DPY - DDY * DPX;
            var B = DPY * DX - DPX * DY + DDX * PY - DDY * PX;
            var C = DX * PY - DY * PX;


            var times = SolveQuadratic(A, B, C).Where(x=>x>0 && x<endTime && AreCloseAtTime(x)).ToArray();

            if (!times.Any()) {
                collision = default;
                return false;
            }

            // the point we collided with is not moving at the speed 
            
            var collisionTime = times.First();

            var D = new Vector(DX + DDX * collisionTime, DY + DDY * collisionTime);

            var collisionPosition = D.NewAdded(new Vector(line.X + line.Vx * collisionTime, line.Y + line.Vy * collisionTime));

            var startPosition = new Vector(line.X, line.Y).NewAdded(new Vector(PX, PY).NewUnitized().NewScaled(D.Length));

            var linePointVelocity = collisionPosition.NewAdded(startPosition.NewMinus()).NewScaled(collisionTime);

            var lineNormal = new Vector(-(PY + DPY * collisionTime), -(PX + DPX * collisionTime));

            collision = DoCollision(ball,line,circle.Radius, collisionTime, lineNormal, ball.Velocity, linePointVelocity);
            return true;

            bool AreCloseAtTime(double time) {
                var DXtime = DX + DDX * time;
                var DYtime = DY + DDY * time;
                return lineShape.Length / 2.0 < new Vector(DXtime, DYtime).Length;
            }
        }

        //internal static bool TryNextCollisionBallLineSweep(PhysicsObject self, Line startSweep, Line endSweep, Circle circle,  double endTime, out IEvent collision)
        //{

        //    var normalDistance = new Vector(self.X, self.Y).Dot(startSweep.NormalUnit.NewUnitized());//myPhysicsObject.X
        //    var normalVelocity = new Vector(self.Vx - line.Vx, self.Vy - line.Vy).Dot(startSweep.NormalUnit.NewUnitized());//myPhysicsObject.Vx
        //    var lineNormalDistance = startSweep.NormalDistance;//line.X

        //    if (lineNormalDistance > normalDistance)
        //    {
        //        if (normalVelocity > 0)
        //        {
        //            var time = (lineNormalDistance - (normalDistance + circle.Radius)) / normalVelocity;

        //            var directionDistance = self.Position.NewAdded(self.Velocity.NewScaled(time)).Dot(startSweep.DirectionUnit) -
        //            line.Position.NewAdded(line.Velocity.NewScaled(time)).Dot(startSweep.DirectionUnit);


        //            if (time < endTime && Math.Abs(directionDistance) < startSweep.Length * .5)
        //            {
        //                collision = DoCollision(self, line, circle.Radius, 0, time, startSweep.NormalUnit);
        //                return true;
        //            }
        //            else
        //            {
        //                collision = default;
        //                return false;
        //            }
        //        }
        //        else
        //        {
        //            collision = default;
        //            return false;
        //        }
        //    }
        //    else if (lineNormalDistance < normalDistance)
        //    {
        //        if (normalVelocity < 0)
        //        {
        //            var time = (lineNormalDistance - (normalDistance - circle.Radius)) / normalVelocity;

        //            var directionDistance = self.Position.NewAdded(self.Velocity.NewScaled(time)).Dot(startSweep.DirectionUnit) -
        //            line.Position.NewAdded(line.Velocity.NewScaled(time)).Dot(startSweep.DirectionUnit);


        //            if (time < endTime && Math.Abs(directionDistance) < startSweep.Length * .5)
        //            {
        //                collision = DoCollision(self, line, circle.Radius, 0, time, startSweep.NormalUnit);
        //                return true;
        //            }
        //            else
        //            {
        //                collision = default;
        //                return false;
        //            }
        //        }
        //        else
        //        {
        //            collision = default;
        //            return false;
        //        }
        //    }
        //    else //if (lineNormalDistance == normalDistance)
        //    {
        //        if (normalVelocity < 0)
        //        {
        //            var time = (lineNormalDistance - (normalDistance - circle.Radius)) / normalVelocity;

        //            var directionDistance = self.Position.NewAdded(self.Velocity.NewScaled(time)).Dot(startSweep.DirectionUnit) -
        //            line.Position.NewAdded(line.Velocity.NewScaled(time)).Dot(startSweep.DirectionUnit);


        //            if (time < endTime && Math.Abs(directionDistance) < lineShape.Length * .5)
        //            {
        //                collision = DoCollision(self, line, circle.Radius, 0, time, startSweep.NormalUnit);
        //                return true;
        //            }
        //            else
        //            {
        //                collision = default;
        //                return false;
        //            }
        //        }
        //        else if (normalVelocity > 0)
        //        {
        //            var time = (lineNormalDistance - (normalDistance + circle.Radius)) / normalVelocity;

        //            var directionDistance = self.Position.NewAdded(self.Velocity.NewScaled(time)).Dot(startSweep.DirectionUnit) -
        //            line.Position.NewAdded(line.Velocity.NewScaled(time)).Dot(lineShape.DirectionUnit);


        //            if (time < endTime && Math.Abs(directionDistance) < lineShape.Length * .5)
        //            {
        //                collision = DoCollision(self, line, circle.Radius, 0, time, lineShape.NormalUnit);
        //                return true;
        //            }
        //            else
        //            {
        //                collision = default;
        //                return false;
        //            }
        //        }
        //        else //normalVelocity == 0
        //        {
        //            // how?? :(
        //            collision = default;
        //            return false;
        //        }
        //    }
        //}



    }
}
