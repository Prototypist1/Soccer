using Common;
using physics2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Physics2.PhysicsMath;

namespace Physics2
{
    internal static class PhysicsMath2
    {

        private const double CLOSE = .01;

        public static Vector GetCollisionForce(Vector v1, Vector v2, Vector position1, Vector postition2, double m1, double m2)
        {
            var dis = position1.NewAdded(postition2.NewMinus());

            var normal = dis.NewUnitized();

            var s1 = v1.Dot(normal);
            var s2 = v2.Dot(normal);

            var c1 = (s1 * m1) + (s2 * m2);
            var c2 = (s1 * s1 * m1) + (s2 * s2 * m2);

            var A = (m2 * m2) + (m2 * m1);
            var B = -2 * m2 * c1;
            var C = (c1 * c1) - (c2 * m1);


            double sf2;

            if (A != 0)
            {
                // b^2 - 4ac
                var D = (B * B) - (4 * A * C);

                if (D >= 0)
                {
                    var sf2_plus = (-B + Math.Sqrt(D)) / (2 * A);
                    var sf2_minus = (-B - Math.Sqrt(D)) / (2 * A);

                    if (IsGood(sf2_minus, s2) && IsGood(sf2_plus, s2) && sf2_plus != sf2_minus)
                    {
                        if (Math.Abs(s2 - sf2_plus) > Math.Abs(s2 - sf2_minus))
                        {
                            if (Math.Abs(s2 - sf2_minus) > CLOSE)
                            {
                                throw new Exception("we are getting physicsObject2 vf2s: " + sf2_plus + "," + sf2_minus + " for vi2: " + v2);
                            }
                            sf2 = sf2_plus;
                        }
                        else
                        {
                            if (Math.Abs(s2 - sf2_plus) > CLOSE)
                            {
                                throw new Exception("we are getting physicsObject2 vf2s: " + sf2_plus + "," + sf2_minus + " for vi2: " + v2);
                            }
                            sf2 = sf2_minus;
                        }
                    }
                    else if (IsGood(sf2_minus, s2))
                    {
                        sf2 = sf2_minus;
                    }
                    else if (IsGood(sf2_plus, s2))
                    {
                        sf2 = sf2_plus;
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

            return normal.NewScaled((sf2 - s2) * m2);
        }

        internal static DoubleUpdatePositionVelocityEvent DoCollision2(
            IPhysicsObject physicsObject1,
            IPhysicsObject physicsObject2,
            double radius1,
            double time,
            Vector normal,
            Vector velocityVector1,
            Vector velocityVector2,
            Vector position1,
            BallState ballState
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
                return new DoubleUpdatePositionVelocityEvent(
                    time,
                    physicsObject1,
                    0,
                    0,
                    physicsObject2,
                    normal.NewScaled(-2 * v2 * m2).x,
                    normal.NewScaled(-2 * v2 * m2).y,
                    new MightBeCollision(
                     new Collision(
                        position1.x + (time * velocityVector1.x) + normal.NewScaled(radius1).x,
                        position1.y + (time * velocityVector1.y) + normal.NewScaled(radius1).y,
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
                    normal.NewScaled(-2 * v1 * m1).x,
                    normal.NewScaled(-2 * v1 * m1).y,
                    physicsObject2,
                    0,
                    0,
                    new MightBeCollision(
                        new Collision(
                        position1.x + (time * velocityVector1.x) + normal.NewScaled(radius1).x,
                        position1.y + (time * velocityVector1.y) + normal.NewScaled(radius1).y,
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
                    //var o2v = normal.NewScaled(vf2).NewAdded(normal.NewScaled(-v2)).NewAdded(physicsObject2.Velocity);

                    var f = (vf2 - v2) * m2;

                    //if (Math.Abs(f) < Constants.MinPlayerCollisionForce) {
                    f += Constants.MinPlayerCollisionForce * Math.Sign(f);
                    //}

                    //var vf1 = v1 - (f / m1);
                    //var o1v = normal.NewScaled(vf1).NewAdded(normal.NewScaled(-v1)).NewAdded(physicsObject1.Velocity);

                    // we know they are moving together



                    double part1, part2;

                    switch (ballState)
                    {
                        case BallState.neither:
                            var denom = velocityVector1.NewMinus().NewAdded(velocityVector2).Dot(normal);

                            var vv1dot = velocityVector1.Dot(normal.NewMinus());
                            var vv2dot = velocityVector2.Dot(normal);

                            part1 = Math.Min(1, Math.Max(-1, vv1dot / denom));
                            part2 = Math.Min(1, Math.Max(-1, vv2dot / denom));
                            break;
                        case BallState.obj1:
                            f += Constants.MinPlayerCollisionForce * Math.Sign(f);
                            part1 = -1;
                            part2 = 1;
                            break;
                        case BallState.obj2:
                            f += Constants.MinPlayerCollisionForce * Math.Sign(f);
                            part1 = 1;
                            part2 = -1;
                            break;
                        default:
                            throw new Exception("incomplete enum");
                    }


                    return new DoubleUpdatePositionVelocityEvent(
                        time,
                        physicsObject1,
                        normal.NewScaled(-f * (1 + part2)).x,
                        normal.NewScaled(-f * (1 + part2)).y,
                        physicsObject2,
                        normal.NewScaled(f * (1 + part1)).x,
                        normal.NewScaled(f * (1 + part1)).y,
                        new MightBeCollision(new Collision(
                            position1.x + (time * velocityVector1.x) + normal.NewScaled(-radius1).x,
                            position1.y + (time * velocityVector1.y) + normal.NewScaled(-radius1).y,
                            normal.NewScaled(f).x,
                            normal.NewScaled(f).y,
                            false
                        )));
                }
            }
        }
        private static bool IsGood(double vf, double v)
        {
            return vf != v;
        }

        internal static bool TryBallBallCollistion(Vector position1, Vector position2, Vector velocity1, Vector velocity2, double combinedRadious, out double timeOfCollision) {
            // how  are they moving relitive to us
            double DVX = velocity2.x - velocity1.x,
                   DVY = velocity2.y - velocity1.y;

            // how far they are from us
            var DX = position2.x - position1.x;
            var DY = position2.y - position1.y;

            // if the objects are not moving towards each other dont bother
            var V = -new Vector(DVX, DVY).Dot(new Vector(DX, DY).NewUnitized());
            if (V <= 0)
            {
                timeOfCollision = -1;
                return false;
            }

            var R = combinedRadious;

            var A = (DVX * DVX) + (DVY * DVY);
            var B = 2 * ((DX * DVX) + (DY * DVY));
            var C = (DX * DX) + (DY * DY) - (R * R);

            if (TrySolveQuadratic(A, B, C, out var time)) {

                timeOfCollision = time;
                return true;
            }

            timeOfCollision = -1;
            return false;
        }


        public static Vector DirectionalUnit(Vector start, Vector end) {
            return end.NewAdded(start.NewMinus()).NewUnitized();
        }

        public static Vector NormalUnit(Vector start, Vector end, Vector directionalUnit) {

            return new Vector(-directionalUnit.y, directionalUnit.x);
        }


        internal static bool TryBallLineCollision(
            Vector ballPosition,
            Vector ballVelocity,
            Vector wallStart,
            Vector wallEnd,
            Vector wallVelocity,
            double radius,
            out double timeOfCollision)
        {
            var directionalUnit = DirectionalUnit(wallStart, wallEnd);
            var normalUnit = NormalUnit(wallStart, wallEnd, directionalUnit);

            var normalDistance = ballPosition.Dot(normalUnit);
            var normalVelocity = ballVelocity.NewAdded(wallVelocity.NewMinus()).Dot(normalUnit);
            var lineNormalDistance = wallEnd.Dot(normalUnit);

            var lineCenter = wallStart.NewScaled(.5).NewAdded(wallEnd.NewScaled(.5));
            var lineLength = wallEnd.Distance(wallStart);

            if (lineNormalDistance > normalDistance)
            {
                if (normalVelocity > 0)
                {
                    var time = (lineNormalDistance - (normalDistance + radius)) / normalVelocity;

                    var directionDistance = ballPosition.NewAdded(ballVelocity.NewScaled(time)).Dot(directionalUnit) -
                    lineCenter.NewAdded(wallVelocity.NewScaled(time)).Dot(directionalUnit);


                    if (Math.Abs(directionDistance) <= (lineLength * .5))
                    {
                        timeOfCollision = time;
                        return true;
                    }
                    else
                    {
                        timeOfCollision = -1;
                        return false;
                    }
                }
                else
                {
                    timeOfCollision = -1;
                    return false;
                }
            }
            else if (lineNormalDistance < normalDistance)
            {
                if (normalVelocity < 0)
                {
                    var time = (lineNormalDistance - (normalDistance - radius)) / normalVelocity;

                    var directionDistance = ballPosition.NewAdded(ballVelocity.NewScaled(time)).Dot(directionalUnit) -
                    lineCenter.NewAdded(wallVelocity.NewScaled(time)).Dot(directionalUnit);


                    if (Math.Abs(directionDistance) < lineLength * .5)
                    {
                        timeOfCollision = time;
                        return true;
                    }
                    else
                    {
                        timeOfCollision = -1;
                        return false;
                    }
                }
                else
                {
                    timeOfCollision = -1;
                    return false;
                }
            }
            else //if (lineNormalDistance == normalDistance)
            {
                if (normalVelocity < 0)
                {
                    var time = (lineNormalDistance - (normalDistance - radius)) / normalVelocity;

                    var directionDistance = ballPosition.NewAdded(ballVelocity.NewScaled(time)).Dot(directionalUnit) -
                    lineCenter.NewAdded(wallVelocity.NewScaled(time)).Dot(directionalUnit);


                    if (Math.Abs(directionDistance) < lineLength * .5)
                    {
                        timeOfCollision = time;
                        return true;
                    }
                    else
                    {
                        timeOfCollision = -1;
                        return false;
                    }
                }
                else if (normalVelocity > 0)
                {
                    var time = (lineNormalDistance - (normalDistance + radius)) / normalVelocity;

                    var directionDistance = ballPosition.NewAdded(ballVelocity.NewScaled(time)).Dot(directionalUnit) -
                    lineCenter.NewAdded(wallVelocity.NewScaled(time)).Dot(directionalUnit);


                    if ( Math.Abs(directionDistance) < lineLength * .5)
                    {
                        timeOfCollision = time;
                        return true;
                    }
                    else
                    {
                        timeOfCollision = -1;
                        return false;
                    }
                }
                else //normalVelocity == 0
                {
                    // how?? :(
                    timeOfCollision = -1;
                    return false;
                }
            }
        }

        internal static Vector HitWall(Vector velocity, Vector start, Vector end)
        {
            var directionalUnit = DirectionalUnit(start, end);
            var normalUnit = NormalUnit(start, end, directionalUnit);

            return velocity.NewAdded(normalUnit.NewScaled( velocity.Dot(normalUnit) * 2));
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


        internal static void TryPushBallLine(GameState.Ball ball, GameState.PerimeterSegment perimeterSegment)
        {
            throw new NotImplementedException();
        }
        internal static void TryPushBallLine(GameState.Player player, GameState.PerimeterSegment perimeterSegment)
        {
            throw new NotImplementedException();
        }

        internal static void TryPushBallWall(GameState.Player player, (double x, double y, double radius) ballwall)
        {
            throw new NotImplementedException();
        }

    }

    internal static class PhysicsMath
    {

        private const double CLOSE = .01;

        //internal static DoubleUpdatePositionVelocityEvent DoCollisionInfiniteMass(
        //    PhysicsObject physicsObject,
        //    IPhysicsObject physicsObjectInfiniteMass,
        //    double radius1,
        //    double time,
        //    Vector normal,
        //    Vector velocityVector,
        //    Vector velocityVectorInfiniteMass)
        //{

        //    var normalVelocity1 = normal.Dot(velocityVector);

        //    var normalVelocity2 = normal.Dot(velocityVectorInfiniteMass);

        //    var finalV1 = velocityVector
        //        .NewAdded(normal.NewScaled(normalVelocity1).NewMinus())
        //        .NewAdded(normal.NewScaled(normalVelocity1).NewMinus())
        //        .NewAdded(normal.NewScaled(normalVelocity2))
        //        .NewAdded(normal.NewScaled(normalVelocity2));

        //    var force = physicsObject.Mass *((normalVelocity1 * 2) + (normalVelocity2 * 2));

        //    return new DoubleUpdatePositionVelocityEvent(
        //                    time,
        //                    physicsObject,
        //                    finalV1.x,
        //                    finalV1.y,
        //                    physicsObjectInfiniteMass,
        //                    velocityVectorInfiniteMass.x,
        //                    velocityVectorInfiniteMass.y,
        //                    new MightBeCollision(new Collision(
        //                        physicsObject.X + (time * physicsObject.Vx) + normal.NewScaled(-radius1).x,
        //                        physicsObject.Y + (time * physicsObject.Vy) + normal.NewScaled(-radius1).y,
        //                        normal.NewScaled(force).x,
        //                        normal.NewScaled(force).y,
        //                        false
        //                    )));


        //}

        internal static DoubleUpdatePositionVelocityEvent DoCollision(
            IPhysicsObject physicsObject1,
            IPhysicsObject physicsObject2,
            double radius1,
            double time,
            Vector normal,
            Vector velocityVector1,
            Vector velocityVector2,
            Vector position1
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
                return new DoubleUpdatePositionVelocityEvent(
                    time,
                    physicsObject1,
                    0,
                    0,
                    physicsObject2,
                    normal.NewScaled(-2 * v2 * m2).x,
                    normal.NewScaled(-2 * v2 * m2).y,
                    new MightBeCollision(
                     new Collision(
                        position1.x + (time * velocityVector1.x) + normal.NewScaled(radius1).x,
                        position1.y + (time * velocityVector1.y) + normal.NewScaled(radius1).y,
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
                    normal.NewScaled(-2* v1 * m1).x,
                    normal.NewScaled(-2 * v1 * m1).y,
                    physicsObject2,
                    0,
                    0,
                    new MightBeCollision(
                        new Collision(
                        position1.x + (time * velocityVector1.x) + normal.NewScaled(radius1).x,
                        position1.y + (time * velocityVector1.y) + normal.NewScaled(radius1).y,
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
                    //var o2v = normal.NewScaled(vf2).NewAdded(normal.NewScaled(-v2)).NewAdded(physicsObject2.Velocity);

                    var f = (vf2 - v2) * m2;
                    //var vf1 = v1 - (f / m1);
                    //var o1v = normal.NewScaled(vf1).NewAdded(normal.NewScaled(-v1)).NewAdded(physicsObject1.Velocity);

                    return new DoubleUpdatePositionVelocityEvent(
                        time,
                        physicsObject1,
                        normal.NewScaled(-f).x,
                        normal.NewScaled(-f).y,
                        physicsObject2,
                        normal.NewScaled(f).x,
                        normal.NewScaled(f).y,
                        new MightBeCollision(new Collision(
                            position1.x + (time * velocityVector1.x) + normal.NewScaled(-radius1).x,
                            position1.y + (time * velocityVector1.y) + normal.NewScaled(-radius1).y,
                            normal.NewScaled(f).x,
                            normal.NewScaled(f).y,
                            false
                        )));
                }
            }
        }

        internal static DoubleUpdatePositionVelocityEvent DoCollision2(
            IPhysicsObject physicsObject1,
            IPhysicsObject physicsObject2,
            double radius1,
            double time,
            Vector normal,
            Vector velocityVector1,
            Vector velocityVector2,
            Vector position1,
            BallState ballState
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
                return new DoubleUpdatePositionVelocityEvent(
                    time,
                    physicsObject1,
                    0,
                    0,
                    physicsObject2,
                    normal.NewScaled(-2 * v2 * m2).x,
                    normal.NewScaled(-2 * v2 * m2).y,
                    new MightBeCollision(
                     new Collision(
                        position1.x + (time * velocityVector1.x) + normal.NewScaled(radius1).x,
                        position1.y + (time * velocityVector1.y) + normal.NewScaled(radius1).y,
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
                    normal.NewScaled(-2 * v1 * m1).x,
                    normal.NewScaled(-2 * v1 * m1).y,
                    physicsObject2,
                    0,
                    0,
                    new MightBeCollision(
                        new Collision(
                        position1.x + (time * velocityVector1.x) + normal.NewScaled(radius1).x,
                        position1.y + (time * velocityVector1.y) + normal.NewScaled(radius1).y,
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
                    //var o2v = normal.NewScaled(vf2).NewAdded(normal.NewScaled(-v2)).NewAdded(physicsObject2.Velocity);

                    var f = (vf2 - v2) * m2;

                    //if (Math.Abs(f) < Constants.MinPlayerCollisionForce) {
                    f += Constants.MinPlayerCollisionForce * Math.Sign(f);
                    //}

                    //var vf1 = v1 - (f / m1);
                    //var o1v = normal.NewScaled(vf1).NewAdded(normal.NewScaled(-v1)).NewAdded(physicsObject1.Velocity);

                    // we know they are moving together



                    double part1 ,part2;

                    switch (ballState)
                    {
                        case BallState.neither:
                            var denom = velocityVector1.NewMinus().NewAdded(velocityVector2).Dot(normal);

                            var vv1dot = velocityVector1.Dot(normal.NewMinus());
                            var vv2dot = velocityVector2.Dot(normal);

                            part1 = Math.Min(1, Math.Max(-1, vv1dot / denom));
                            part2 = Math.Min(1, Math.Max(-1, vv2dot / denom));
                            break;
                        case BallState.obj1:
                            f += Constants.MinPlayerCollisionForce * Math.Sign(f);
                            part1 = -1;
                            part2 = 1;
                            break;
                        case BallState.obj2:
                            f += Constants.MinPlayerCollisionForce * Math.Sign(f);
                            part1 = 1;
                            part2 = -1;
                            break;
                        default:
                            throw new Exception("incomplete enum");
                    }


                    return new DoubleUpdatePositionVelocityEvent(
                        time,
                        physicsObject1,
                        normal.NewScaled(-f*(1+ part2)).x,
                        normal.NewScaled(-f * (1 + part2)).y,
                        physicsObject2,
                        normal.NewScaled(f * (1 + part1)).x,
                        normal.NewScaled(f * (1 + part1)).y,
                        new MightBeCollision(new Collision(
                            position1.x + (time * velocityVector1.x) + normal.NewScaled(-radius1).x,
                            position1.y + (time * velocityVector1.y) + normal.NewScaled(-radius1).y,
                            normal.NewScaled(f).x,
                            normal.NewScaled(f).y,
                            false
                        )));
                }
            }
        }


        private static Vector GetNormal(IPhysicsObject physicsObject1, IPhysicsObject partical, double time)
        {
            var dx = (physicsObject1.X + (time * physicsObject1.Vx)) - (partical.X + (time * partical.Vx));
            var dy = (physicsObject1.Y + (time * physicsObject1.Vy)) - (partical.Y + (time * partical.Vy));
            return new Vector(dx, dy).NewUnitized();
        }

        private static bool IsGood(double vf, double v)
        {
            return vf != v;
        }

        //internal static bool TryCollisionBallInfiniteMass(PhysicsObject self,
        //    IPhysicsObject collider,
        //    double particalX,
        //    double particalY,
        //    double particalVx,
        //    double particalVy,
        //    Circle c1,
        //    Circle c2,
        //    double endTime,
        //    out DoubleUpdatePositionVelocityEvent evnt)
        //{

        //    // how  are they moving relitive to us
        //    double DVX = particalVx - self.Vx,
        //           DVY = particalVy - self.Vy;

        //    var thisX0 = self.X;
        //    var thisY0 = self.Y;
        //    var thatX0 = particalX;
        //    var thatY0 = particalY;

        //    // how far they are from us
        //    var DX = thatX0 - thisX0;
        //    var DY = thatY0 - thisY0;

        //    // if the objects are not moving towards each other dont bother
        //    var V = -new Vector(DVX, DVY).Dot(new Vector(DX, DY).NewUnitized());
        //    if (V <= 0)
        //    {
        //        evnt = default;
        //        return false;
        //    }

        //    var R = c1.Radius + c2.Radius;

        //    var A = (DVX * DVX) + (DVY * DVY);
        //    var B = 2 * ((DX * DVX) + (DY * DVY));
        //    var C = (DX * DX) + (DY * DY) - (R * R);

        //    if (TrySolveQuadratic(A, B, C, out var time) && time <= endTime)
        //    {
        //        evnt = DoCollisionInfiniteMass(self, collider, c1.Radius, time, GetNormal(self, particalX, particalY, particalVx, particalVy, time), self.Velocity, collider.Velocity);
        //        return true;
        //    }
        //    evnt = default;
        //    return false;
        //}


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

        internal static bool TryCollisionBall(
            IPhysicsObject collide1,
            IPhysicsObject collide2,
            IPhysicsObject applyForces1,
            IPhysicsObject applyForces2,
            Circle c1,
            Circle c2,
            double endTime,
            out DoubleUpdatePositionVelocityEvent evnt)
        {

            // how  are they moving relitive to us
            double DVX = collide2.Vx - collide1.Vx,
                   DVY = collide2.Vy - collide1.Vy;

            var thisX0 = collide1.X;
            var thisY0 = collide1.Y;
            var thatX0 = collide2.X;
            var thatY0 = collide2.Y;

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
                evnt = DoCollision(applyForces1, applyForces2, c1.Radius, time, GetNormal(collide1, collide2, time), collide1.Velocity, collide2.Velocity, collide1.Position);
                return true;
            }
            evnt = default;
            return false;
        }

        public enum BallState { 
            neither,
            obj1,
            obj2
        }

        internal static bool TryCollisionBall2(
            IPhysicsObject collide1,
            IPhysicsObject collide2,
            IPhysicsObject applyForces1,
            IPhysicsObject applyForces2,
            Circle c1,
            Circle c2,
            double endTime,
            BallState ballState,
            out DoubleUpdatePositionVelocityEvent evnt)
        {

            // how  are they moving relitive to us
            double DVX = collide2.Vx - collide1.Vx,
                   DVY = collide2.Vy - collide1.Vy;

            var thisX0 = collide1.X;
            var thisY0 = collide1.Y;
            var thatX0 = collide2.X;
            var thatY0 = collide2.Y;

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
                evnt = DoCollision2(applyForces1, applyForces2, c1.Radius, time, GetNormal(collide1, collide2, time), collide1.Velocity, collide2.Velocity, collide1.Position, ballState);
                return true;
            }
            evnt = default;
            return false;
        }

        //internal static bool TryCollisionPointCloudParticle(
        //    PhysicsObject self,
        //    PhysicsObject collider,
        //    double particalX,
        //    double particalY,
        //    double particalVx,
        //    double particalVy
        //    , Circle c1,
        //    Circle c2, double endTime, out IEvent evnt)
        //{

        //    // how  are they moving relitive to us
        //    double DVX = particalVx - self.Vx,
        //           DVY = particalVy - self.Vy;

        //    var thisX0 = self.X;
        //    var thisY0 = self.Y;

        //    // how far they are from us
        //    var DX = particalX - thisX0;
        //    var DY = particalY - thisY0;

        //    // if the objects are not moving towards each other dont bother
        //    var V = -new Vector(DVX, DVY).Dot(new Vector(DX, DY).NewUnitized());
        //    if (V <= 0)
        //    {
        //        evnt = default;
        //        return false;
        //    }

        //    var R = c1.Radius + c2.Radius;

        //    var A = (DVX * DVX) + (DVY * DVY);
        //    var B = 2 * ((DX * DVX) + (DY * DVY));
        //    var C = (DX * DX) + (DY * DY) - (R * R);

        //    if (TrySolveQuadratic(A, B, C, out var time) && time <= endTime)
        //    {

        //        evnt = DoCollision(self, collider, c1.Radius, time, new Vector(collider.Vx, collider.Vy).NewUnitized(), self.Velocity, collider.Velocity);
        //        return true;
        //    }
        //    evnt = default;
        //    return false;
        //}


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


        internal static void TryPushBallLine(
            IPhysicsObject self,
            IPhysicsObject applyForces1,
            Circle circle, 
            Line lineShape)
        {

            var normalDistance = new Vector(self.X, self.Y).Dot(lineShape.NormalUnit.NewUnitized());
            var lineNormalDistance = lineShape.NormalDistance;

            var violation = Math.Abs(lineNormalDistance - normalDistance) - circle.Radius;
            if (violation < 0) {
                var violationVector = lineShape.NormalUnit.NewUnitized().NewScaled(violation* Math.Sign(lineNormalDistance - normalDistance));
                applyForces1.ApplyForce(violationVector.x * applyForces1.Mass, violationVector.y * applyForces1.Mass);
            }

        }

        internal static void TryPushBallWall(
            IPhysicsObject self,
            IPhysicsObject applyForces1,
            Circle circle,
            double x,
            double y,
            double rad)
        {
            var dis = self.Position.NewAdded(new Vector(-x, -y));

            if (dis.Length == 0) {
                dis = new Vector(0.01, 0);
            }

            if (dis.Length < circle.Radius + rad) {
                var toApply = dis.NewUnitized().NewScaled((circle.Radius + rad) - dis.Length);
                applyForces1.ApplyForce(toApply.x * applyForces1.Mass, toApply.y * applyForces1.Mass);
            }

        }

        internal static bool TryNextCollisionBallLine(
            IPhysicsObject self, 
            PhysicsObject line,
            IPhysicsObject applyForces1,
            IPhysicsObject applyLine,
            Circle circle, 
            Line lineShape, 
            double endTime, 
            out IEvent collision)
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


                    if (time <= endTime && Math.Abs(directionDistance) < lineShape.Length * .5)
                    {
                        collision = DoCollision(applyForces1, applyLine, circle.Radius, time, lineShape.NormalUnit, self.Velocity, line.Velocity, self.Position);
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
                        collision = DoCollision(self, line, circle.Radius, time, lineShape.NormalUnit, self.Velocity, line.Velocity, self.Position);
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
                        collision = DoCollision(self, line, circle.Radius, time, lineShape.NormalUnit, self.Velocity, line.Velocity, self.Position);
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
                        collision = DoCollision(self, line, circle.Radius, time, lineShape.NormalUnit, self.Velocity, line.Velocity, self.Position);
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

                collision = DoCollision(ball, line, 0, collisionTime, lineNormal, ball.Velocity, linePointVelocity, ball.Position);
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
