﻿using physics2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Physics2
{


    internal static class PhysicsMath
    {

        private const double CLOSE = .01;

        internal static DoubleUpdatePositionVelocityEvent DoCollisionInfiniteMass(
            PhysicsObject physicsObject,
            IPhysicsObject physicsObjectInfiniteMass,
            double radius1,
            double time,
            Vector normal,
            Vector velocityVector,
            Vector velocityVectorInfiniteMass)
        {

            var normalVelocity1 = normal.Dot(velocityVector);

            var normalVelocity2 = normal.Dot(velocityVectorInfiniteMass);

            var finalV1 = velocityVector
                .NewAdded(normal.NewScaled(normalVelocity1).NewMinus())
                .NewAdded(normal.NewScaled(normalVelocity1).NewMinus())
                .NewAdded(normal.NewScaled(normalVelocity2))
                .NewAdded(normal.NewScaled(normalVelocity2));

            var force = physicsObject.Mass *((normalVelocity1 * 2) + (normalVelocity2 * 2));

            return new DoubleUpdatePositionVelocityEvent(
                            time,
                            physicsObject,
                            finalV1.x,
                            finalV1.y,
                            physicsObjectInfiniteMass,
                            velocityVectorInfiniteMass.x,
                            velocityVectorInfiniteMass.y,
                            new MightBeCollision(new Collision(
                                physicsObject.X + (time * physicsObject.Vx) + normal.NewScaled(-radius1).x,
                                physicsObject.Y + (time * physicsObject.Vy) + normal.NewScaled(-radius1).y,
                                normal.NewScaled(force).x,
                                normal.NewScaled(force).y,
                                false
                            )));


        }

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
                var o2v = normal.NewScaled(-2 * v2).NewAdded(physicsObject2.Velocity);
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
                var v1o = normal.NewScaled(-2 * v1).NewAdded(physicsObject1.Velocity);
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
                    var o2v = normal.NewScaled(vf2).NewAdded(normal.NewScaled(-v2)).NewAdded(physicsObject2.Velocity);

                    var f = (vf2 - v2) * m2;
                    var vf1 = v1 - (f / m1);
                    var o1v = normal.NewScaled(vf1).NewAdded(normal.NewScaled(-v1)).NewAdded(physicsObject1.Velocity);
                    return new DoubleUpdatePositionVelocityEvent(
                    time,
                    physicsObject1,
                    o1v.x,
                    o1v.y,
                    physicsObject2,
                    o2v.x,
                    o2v.y,
                    new MightBeCollision(new Collision(
                        physicsObject1.X + (time * physicsObject1.Vx) + normal.NewScaled(-radius1).x,
                        physicsObject1.Y + (time * physicsObject1.Vy) + normal.NewScaled(-radius1).y,
                        normal.NewScaled(f).x,
                        normal.NewScaled(f).y,
                        false
                    )));
                }
            }
        }

        private static Vector GetNormal(IPhysicsObject physicsObject1, double X, double Y, double Vx, double Vy, double time)
        {
            var dx = physicsObject1.X + (time * physicsObject1.Vx) - (X + (time * Vx));
            var dy = physicsObject1.Y + (time * physicsObject1.Vy) - (Y + (time * Vy));
            var normal = new Vector(dx, dy).NewUnitized();
            return normal;
        }

        private static bool IsGood(double vf, double v)
        {
            return vf != v;
        }

        internal static bool TryCollisionBallInfiniteMass(PhysicsObject self,
            IPhysicsObject collider,
            double particalX,
            double particalY,
            double particalVx,
            double particalVy,
            Circle c1,
            Circle c2,
            double endTime,
            out DoubleUpdatePositionVelocityEvent evnt)
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
                evnt = DoCollisionInfiniteMass(self, collider, c1.Radius, time, GetNormal(self, particalX, particalY, particalVx, particalVy, time), self.Velocity, collider.Velocity);
                return true;
            }
            evnt = default;
            return false;
        }


        internal static bool TryPickUpBall(Ball ball,
            Player player,
            double particalX,
            double particalY,
            double particalVx,
            double particalVy,
            Circle c1,
            Circle c2,
            double endTime,
            out UpdateOwnerEvent evnt)
        {

            // how  are they moving relitive to us
            double DVX = particalVx - ball.Vx,
                   DVY = particalVy - ball.Vy;

            var thisX0 = ball.X;
            var thisY0 = ball.Y;
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
                evnt = new UpdateOwnerEvent(ball, time,player);
                return true;
            }
            evnt = default;
            return false;
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
            out DoubleUpdatePositionVelocityEvent evnt)
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
                evnt = DoCollision(self, collider, c1.Radius, time, GetNormal(self, particalX, particalY, particalVx, particalVy, time), self.Velocity, collider.Velocity);
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
            if (a == 0)
            {
                if (b == 0)
                {
                    if (c == 0)
                    {
                        res = 0;
                        return true;
                    }
                    res = default;
                    return false;
                }
                res = -c / b;
                return true;
            }

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


        public static IEnumerable<double> SolveQuadratic(double a, double b, double c)
        {
            if (a == 0)
            {
                if (b == 0)
                {
                    if (c == 0)
                    {
                        return new double[] { 0 };
                    }
                    return new double[] { };
                }
                return new double[] { -c / b };
            }

            var sqrtpart = (b * b) - (4 * a * c);

            if (sqrtpart > 0)
            {
                return new[] { (-b + Math.Sqrt(sqrtpart)) / (2 * a), (-b - Math.Sqrt(sqrtpart)) / (2 * a) };
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


        //internal static bool TryCollisionBallLine3(
        //    PhysicsObject ball,
        //    PhysicsObject line,


        //    ) { 


        //}

        internal static bool TryCollisionBallLine2(
            PhysicsObject ball,
            PhysicsObject line,
            Circle circle,
            double length,
            double endTime,
            Vector lineParallelStart,
            Vector DlineParallel,
            out IEvent collision)
        {

            //DlineParallel.NewScaled(1/ lineParallelStart.Length)
            var DDX = ball.Vx - (line.Vx);
            var DDY = ball.Vy - (line.Vy);
            //var unitLineParallel = lineParallelStart.NewUnitized().NewScaled(circle.Radius);
            var DX = ball.X - (line.X);
            var DY = ball.Y - (line.Y);

            //if (new Vector(DX, DY).Dot(new Vector(DDX, DDY)) > 0)
            //{
            //    collision = default;
            //    return false;
            //}

            var PX = lineParallelStart.x;
            var PY = lineParallelStart.y;
            var DPX = DlineParallel.x;
            var DPY = DlineParallel.y;

            var A = (DDX * DPY) - (DDY * DPX);

            var B = (DPY * DX) + (DDX * PY) - (DDY * PX) - (DPX * DY);
            var C = (DX * PY) - (DY * PX);


            var times = SolveQuadratic(A, B, C).ToArray();

            //if (new Vector(DX, DY).Length < 500)
            //{
            //    var db = 0;
            //}


            times = times.Where(x => Math.Abs((x * x * A) + (x * B) + C) < .001).ToArray();

            if (B != 0 && !times.Any())
            {
                if (Math.Abs(((-C / B) * (-C / B) * A)) < .001)
                {
                    var x = times.ToList();
                    x.Add(-C / B);
                    times = x.ToArray();
                }
            }


            //if (times.Any()) {
            //    var temp = times.First();
            //    var answer = temp * temp * A + temp * B + C;
            //    if (Math.Abs(answer) > 1) {

            //    }
            //}

            //if (times.Skip(1).Any())
            //{
            //    var temp2 = times.Skip(1).First();
            //    var answer2 = temp2 * temp2 * A + temp2 * B + C;
            //    if (Math.Abs(answer2) > 1)
            //    {
            //        var db = 0;
            //    }
            //}

            //if (times.Any(x => x < 0 && x > -.5 && AreCloseAtTime(x)))
            //{
            //    var db = 0;
            //}

            times = times.Where(x => x > 0 && x <= endTime && AreCloseAtTime(x) && AreMovingTogether(x)).ToArray();

            if (!times.Any())
            {
                collision = default;
                return false;
            }

            // the point we collided with is not moving at the speed 
            {
                var collisionTime = times.OrderBy(x => x).First();



                var D = new Vector(DX + (DDX * collisionTime), DY + (DDY * collisionTime));

                var endpoint = new Vector(PX, PY).NewAdded(new Vector(DPX, DPY).NewScaled(collisionTime));

                var index = endpoint.Length == 0 ? 0 : D.Length / endpoint.Length * Math.Sign(endpoint.Dot(D));

                var lineNormal = new Vector(PY + (DPY * collisionTime), -(PX + (DPX * collisionTime))).NewUnitized();

                var linePointVelocity = line.Velocity.NewAdded(new Vector(DPX, DPY).NewScaled(index));

                collision = DoCollision(ball, line, 0, collisionTime, lineNormal, ball.Velocity, linePointVelocity);
                return true;
            }

            bool AreCloseAtTime(double time)
            {
                var DXtime = DX + (DDX * time);
                var DYtime = DY + (DDY * time);
                return length / 2.0 > new Vector(DXtime, DYtime).Length;
            }

            bool AreMovingTogether(double time)
            {
                var D = new Vector(DX + (DDX * time), DY + (DDY * time));

                var endpoint = new Vector(PX, PY).NewAdded(new Vector(DPX, DPY).NewScaled(time));

                var index = endpoint.Length == 0 ? 0 : D.Length / endpoint.Length * Math.Sign(endpoint.Dot(D));

                var linePointVelocity = line.Velocity.NewAdded(new Vector(DPX, DPY).NewScaled(index));

                var dv = ball.Velocity.NewAdded(linePointVelocity.NewMinus());

                var d = ball.Position.NewAdded(line.Position.NewAdded(new Vector(PX, PY).NewScaled(index)).NewMinus());

                return dv.Dot(d) < 0;
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
