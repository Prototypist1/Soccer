using physics2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Physics2
{


    internal static class math
    {

        private const double CLOSE = .01;



        internal static DoubleUpdatePositionVelocityEvent DoCollision(PhysicsObject physicsObject1, PhysicsObject physicsObject2, double radius1, double radius2, double time, Vector normal)
        {
            // update the V of both

            // when a collision happen how does it go down?
            // the velocities we care about are normal to the line
            // we find the normal and take the dot product

            var v1 = normal.Dot(physicsObject1.Velocity);
            var m1 = physicsObject1.Mass;

            var v2 = normal.Dot(physicsObject2.Velocity);
            var m2 = physicsObject2.Mass;



            if (physicsObject1.Mobile == false)
            {
                var o2v = normal.NewScaled(-2 * v2).NewAdded(physicsObject2.Velocity);
                return new DoubleUpdatePositionVelocityEvent(
                    time,
                    physicsObject1,
                    physicsObject1.X + (time * physicsObject1.Vx),
                    physicsObject1.Y + (time * physicsObject1.Vy),
                    physicsObject1.Vx,
                    physicsObject1.Vy,
                    physicsObject2,
                    physicsObject2.X + (time * physicsObject2.Vx),
                    physicsObject2.Y + (time * physicsObject2.Vy),
                    o2v.x,
                    o2v.y,
                    new MightBeCollision(
                     new Collision(
                    physicsObject2.X + normal.NewScaled(radius2).x,
                    physicsObject2.Y + normal.NewScaled(radius2).y,
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
                    physicsObject1.X + (time * physicsObject1.Vx),
                    physicsObject1.Y + (time * physicsObject1.Vy),
                    v1o.x,
                    v1o.y,
                    physicsObject2,
                    physicsObject2.X + (time * physicsObject2.Vx),
                    physicsObject2.Y + (time * physicsObject2.Vy),
                    physicsObject2.Vx,
                    physicsObject2.Vy,
                    new MightBeCollision(
                        new Collision(
                            physicsObject2.X + (time * physicsObject2.Vx) + normal.NewScaled(radius2).x,
                            physicsObject2.Y + (time * physicsObject2.Vy) + normal.NewScaled(radius2).y,
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

                {
                    var o2v = normal.NewScaled(vf2).NewAdded(normal.NewScaled(-v2)).NewAdded(physicsObject2.Velocity);

                    var f = (vf2 - v2) * m2;
                    var vf1 = v1 - (f / m1);
                    var o1v = normal.NewScaled(vf1).NewAdded(normal.NewScaled(-v1)).NewAdded(physicsObject1.Velocity);
                    return new DoubleUpdatePositionVelocityEvent(
                    time,
                    physicsObject1,
                    physicsObject1.X + (time * physicsObject1.Vx),
                    physicsObject1.Y + (time * physicsObject1.Vy),
                    o1v.x,
                    o1v.y,
                    physicsObject2,
                    physicsObject2.X + (time * physicsObject2.Vx),
                    physicsObject2.Y + (time * physicsObject2.Vy),
                    o2v.x,
                    o2v.y,
                    new MightBeCollision(new Collision(
                        physicsObject2.X + (time * physicsObject2.Vx) + normal.NewScaled(radius2).x,
                        physicsObject2.Y + (time * physicsObject2.Vy) + normal.NewScaled(radius2).y,
                        normal.NewScaled(f).x,
                        normal.NewScaled(f).y,
                        false
                    )));
                }
            }
        }

        private static Vector GetNormal(PhysicsObject physicsObject1, PhysicsObject physicsObject2, double time)
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


        internal static bool TryCollisionBall(PhysicsObject self, PhysicsObject that, Circle c1, Circle c2, double endTime, out IEvent evnt)
        {

            // how  are they moving relitive to us
            double DVX = that.Vx - self.Vx,
                   DVY = that.Vy - self.Vy;

            var thisX0 = self.X;
            var thisY0 = self.Y;
            var thatX0 = that.X;
            var thatY0 = that.Y;

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

                evnt = DoCollision(self, that, c1.Radius, c2.Radius, time, GetNormal(self, that, time));
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
                res = Math.Min( x1,x2);
                return true;
            }
            else if (sqrtpart < 0)
            {
                res = default;
                return false;
            }
            else
            {
                res =  (-b + Math.Sqrt(sqrtpart)) / (2 * a) ;
                return true;
            }
        }

        internal static bool TryNextCollisionBallLine(PhysicsObject self, PhysicsObject line, Circle circle, Line lineShape, double endTime, out IEvent collision)
        {

            var normalDistance = new Vector(self.X, self.Y).Dot(lineShape.NormalUnit.NewUnitized());//myPhysicsObject.X
            var normalVelocity = new Vector(self.Vx, self.Vy).Dot(lineShape.NormalUnit.NewUnitized());//myPhysicsObject.Vx
            var lineNormalDistance = lineShape.NormalDistance;//line.X

            if (lineNormalDistance > normalDistance)
            {
                if (normalVelocity > 0)
                {
                    var time = (lineNormalDistance - (normalDistance + circle.Radius)) / normalVelocity;
                    if (time < endTime)
                    {
                        var force = lineShape.NormalUnit.NewScaled(-2 * normalVelocity);

                        collision = new UpdatePositionVelocityEvent(
                            time,
                            self,
                            self.X + (time * self.Vx),
                            self.Y + (time * self.Vy),
                            self.Vx + force.x,
                            self.Vy + force.y,
                            new MightBeCollision(new Collision(
                                self.X + (time * self.Vx) + (lineShape.NormalUnit.NewUnitized().x * circle.Radius),
                                self.Y + (time * self.Vy) + (lineShape.NormalUnit.NewUnitized().y * circle.Radius),
                                force.x,
                                force.y,
                                false)));
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
                    if (time < endTime)
                    {
                        var force = lineShape.NormalUnit.NewScaled(-2 * normalVelocity);

                        collision = new UpdatePositionVelocityEvent(
                            time,
                            self,
                            self.X + (time * self.Vx),
                            self.Y + (time * self.Vy),
                            self.Vx + force.x,
                            self.Vy + force.y,
                            new MightBeCollision(new Collision(
                                self.X + (time * self.Vx) + (-lineShape.NormalUnit.NewUnitized().x * circle.Radius),
                                self.Y + (time * self.Vy) + (-lineShape.NormalUnit.NewUnitized().y * circle.Radius),
                                force.x,
                                force.y,
                                false)));
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
                    if (time < endTime)
                    {
                        collision = new UpdatePositionVelocityEvent(
                            time,
                            self,
                            self.X + (time * self.Vx),
                            self.Y + (time * self.Vy),
                            -self.Vx,
                            self.Vy,
                            new MightBeCollision(new Collision(
                                self.X + (time * self.Vx) + (-lineShape.NormalUnit.NewUnitized().x * circle.Radius),
                                self.Y + (time * self.Vy) + (-lineShape.NormalUnit.NewUnitized().y * circle.Radius),
                                -self.Vx * 2 * self.Mass,
                                0,
                                false)));
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
                    if (time < endTime)
                    {
                        collision = new UpdatePositionVelocityEvent(
                            time,
                            self,
                            self.X + (time * self.Vx),
                            self.Y + (time * self.Vy),
                            -self.Vx,
                            self.Vy,
                            new MightBeCollision(new Collision(
                                self.X + (time * self.Vx) + (lineShape.NormalUnit.NewUnitized().x * circle.Radius),
                                self.Y + (time * self.Vy) + (lineShape.NormalUnit.NewUnitized().y * circle.Radius),
                                0,
                                -2 * self.Vy * self.Mass,
                                false)));
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
    }
}
