using physics2;
using System;
using System.Collections.Generic;
using System.Text;

namespace Physics2
{
    internal static  class math
    {

        private const double CLOSE = .01;

        internal static MightBeCollision DoCollision(PhysicsObject physicsObject1, PhysicsObject physicsObject2, double radius1, double radius2, Vector normal)
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
                physicsObject2.Velocity = normal.NewScaled(-2 * v2).NewAdded(physicsObject2.Velocity);
                return new MightBeCollision(new Collision(
                    physicsObject2.X + normal.NewScaled(radius2).x,
                    physicsObject2.Y + normal.NewScaled(radius2).y,
                    normal.NewScaled(-2 * v2 * m2).x,
                    normal.NewScaled(-2 * v2 * m2).y,
                    false
                ));
            }
            else if (physicsObject2.Mobile == false)
            {
                physicsObject1.Velocity = normal.NewScaled(-2 * v1).NewAdded(physicsObject1.Velocity);
                return new MightBeCollision(new Collision(
                    physicsObject1.X + normal.NewScaled(radius1).x,
                    physicsObject1.Y + normal.NewScaled(radius1).y,
                    normal.NewScaled(-2 * v1 * m1).x,
                    normal.NewScaled(-2 * v1 * m1).y,
                    false
                ));
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
                return new MightBeCollision(new Collision(
                    physicsObject2.X + normal.NewScaled(radius2).x,
                    physicsObject2.Y + normal.NewScaled(radius2).y,
                    normal.NewScaled(vf1).x,
                    normal.NewScaled(vf1).y,
                    false
                ));
            }

        }

        private static Vector GetNormal(PhysicsObject physicsObject1, PhysicsObject physicsObject2)
        {
            var dx = physicsObject1.X - physicsObject2.X;
            var dy = physicsObject1.Y - physicsObject2.Y;
            var normal = new Vector(dx, dy).NewUnitized();
            return normal;
        }

        private static bool IsGood(double vf, double v)
        {
            return vf != v;
        }


        internal static bool TryCollisionBallTime(PhysicsObject self, PhysicsObject that, Circle c1, Circle c2,  double endTime, out double time)
        {

            double startTime, thisX0, thatX0, thisY0, thatY0, DX, DY;
            // how  are they moving relitive to us
            double DVX = that.Vx - self.Vx,
                   DVY = that.Vy - self.Vy;

            if (self.Time > that.Time)
            {
                startTime = self.Time;
                thisX0 = self.X;
                thisY0 = self.Y;
                thatX0 = that.X + (that.Vx * (self.Time - that.Time));
                thatY0 = that.Y + (that.Vy * (self.Time - that.Time));
            }
            else
            {
                startTime = that.Time;
                thatX0 = that.X;
                thatY0 = that.Y;
                thisX0 = self.X + (self.Vx * (that.Time - self.Time));
                thisY0 = self.Y + (self.Vy * (that.Time - self.Time));
            }
            // how far they are from us
            DX = thatX0 - thisX0;
            DY = thatY0 - thisY0;

            // if the objects are not moving towards each other dont bother
            var V = -new Vector(DVX, DVY).Dot(new Vector(DX, DY).NewUnitized());
            if (V <= 0)
            {
                time = default;
                return false;
            }

            var R = c1.Radious + c2.Radious;

            var A = (DVX * DVX) + (DVY * DVY);
            var B = 2 * ((DX * DVX) + (DY * DVY));
            var C = (DX * DX) + (DY * DY) - (R * R);

            if (TrySolveQuadratic(A, B, C, out var res) && startTime + res <= endTime)
            {

                time = startTime + res;
                return true;
            }
            time = default;
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

        
        protected override bool TryNextCollisionLine(PhysicsObject self, PhysicsObject line, Circle circle, Line lineShape, double endTime, out IEvent collision)
        {

            var normalDistance = new Vector(self.X, self.Y).Dot(lineShape.NormalUnit.NewUnitized());//myPhysicsObject.X
            var normalVelocity = new Vector(self.Vx, self.Vy).Dot(lineShape.NormalUnit.NewUnitized());//myPhysicsObject.Vx
            var lineNormalDistance = lineShape.NormalDistance;//line.X

            if (lineNormalDistance > normalDistance)
            {
                if (normalVelocity > 0)
                {
                    var time = (lineNormalDistance - (normalDistance + circle.Radious)) / normalVelocity;
                    if (self.Time + time < endTime)
                    {
                        var force = line.shape.NormalUnit.NewScaled(-2 * normalVelocity);

                        collision = new UpdatePositionVelocityEvent(
                            self.Time + time,
                            self,
                            self.X + (time * self.Vx),
                            self.Y + (time * self.Vy),
                            self.Vx + force.x,
                            self.Vy + force.y,
                            new MightBeCollision(new Collision(
                                self.X + (time * self.Vx) + (line.shape.NormalUnit.NewUnitized().x* self.shape.Radius), 
                                self.Y + (time * self.Vy) + (line.shape.NormalUnit.NewUnitized().y * self.shape.Radius),
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
                    var time = (lineNormalDistance - (normalDistance - circle.Radious)) / normalVelocity;
                    if (self.Time + time < endTime)
                    {
                        var force = line.shape.NormalUnit.NewScaled(-2 * normalVelocity);

                        collision = new UpdatePositionVelocityEvent(
                            self.Time + time,
                            self,
                            self.X + (time * self.Vx),
                            self.Y + (time * self.Vy),
                            self.Vx + force.x,
                            self.Vy + force.y,
                            new MightBeCollision(new Collision(
                                self.X + (time * self.Vx) + (-line.shape.NormalUnit.NewUnitized().x * self.shape.Radius), 
                                self.Y + (time * self.Vy) + (-line.shape.NormalUnit.NewUnitized().y * self.shape.Radius), 
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
                    var time = (lineNormalDistance - (normalDistance - circle.Radious)) / normalVelocity;
                    if (self.Time + time < endTime)
                    {
                        collision = new UpdatePositionVelocityEvent(
                            self.Time + time,
                            self,
                            self.X + (time * self.Vx),
                            self.Y + (time * self.Vy),
                            -self.Vx,
                            self.Vy,
                            new MightBeCollision(new Collision(
                                self.X + (time * self.Vx) + (-line.shape.NormalUnit.NewUnitized().x * self.shape.Radius), 
                                self.Y + (time * self.Vy) + (-line.shape.NormalUnit.NewUnitized().y * self.shape.Radius), 
                                -self.Vx*2*self.Mass, 
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
                    var time = (lineNormalDistance - (normalDistance + self.shape.Radius)) / normalVelocity;
                    if (self.Time + time < endTime)
                    {
                        collision = new UpdatePositionVelocityEvent(
                            self.Time + time,
                            self,
                            self.X + (time * self.Vx),
                            self.Y + (time * self.Vy),
                            -self.Vx,
                            self.Vy,
                            new MightBeCollision(new Collision(
                                self.X + (time * self.Vx) + (line.shape.NormalUnit.NewUnitized().x * self.shape.Radius), 
                                self.Y + (time * self.Vy) + (line.shape.NormalUnit.NewUnitized().y * self.shape.Radius),
                                0, 
                                -2*self.Vy*self.Mass, 
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
