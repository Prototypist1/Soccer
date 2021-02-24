using Common;
using physics2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

            var D = new Vector(DX, DY);

            // uhhh 
            if (D.Length == 0) {
                timeOfCollision = -1;
                return false;
            }

            // if the objects are not moving towards each other dont bother
            var V = -new Vector(DVX, DVY).Dot(D.NewUnitized());
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

            return normalUnit.NewScaled( velocity.Dot(normalUnit) * -2);
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


        internal static void TryPushBallBall(GameState.Player player1, GameState.Player player2) {

            // TODO
            // I want to make it possible to hold space
            // if you are holding your ground you dont move
            // if you are pushing you bounce off
            // or... maybe not
            // if you want to hold your ground you have to push back

            var dis = player1.PlayerBody.Position.NewAdded(player2.PlayerBody.Position.NewMinus());

            var violation = Constants.footLen * 8 - dis.Length;
            if (violation> 0) {

                player1.ExternalVelocity = player1.ExternalVelocity.NewAdded(dis.NewUnitized().NewScaled(violation * .005));
                player2.ExternalVelocity = player2.ExternalVelocity.NewAdded(dis.NewUnitized().NewScaled(-violation * .005));
            }
        }

        internal static void TryPushBallLine(GameState.Ball ball, GameState.PerimeterSegment perimeterSegment)
        {
            var directionalUnit = DirectionalUnit(perimeterSegment.Start, perimeterSegment.End);
            var normalUnit = NormalUnit(perimeterSegment.Start, perimeterSegment.End, directionalUnit);

            var normalDistance = ball.Posistion.Dot(normalUnit);
            var lineNormalDistance = perimeterSegment.Start.Dot(normalUnit);

            var violation = Math.Abs(lineNormalDistance - normalDistance) - Constants.BallRadius;
            if (violation < 0)
            {
                var violationVector = normalUnit.NewScaled(violation * Math.Sign(lineNormalDistance - normalDistance));
                ball.Velocity = ball.Velocity.NewAdded(violationVector);
            }
        }
        internal static void TryPushBallLine(GameState.Player player, GameState.PerimeterSegment perimeterSegment)
        {
            var directionalUnit = DirectionalUnit(perimeterSegment.Start, perimeterSegment.End);
            var normalUnit = NormalUnit(perimeterSegment.Start, perimeterSegment.End, directionalUnit);

            var normalDistance = player.PlayerFoot.Position.Dot(normalUnit);
            var lineNormalDistance = perimeterSegment.Start.Dot(normalUnit);

            var violation = Math.Abs(lineNormalDistance - normalDistance) - Constants.BallRadius;
            if (violation < 0)
            {
                var violationVector = normalUnit.NewScaled(violation * Math.Sign(lineNormalDistance - normalDistance));
                player.ExternalVelocity = player.ExternalVelocity.NewAdded(violationVector);
            }
        }

        internal static void TryPushBallWall(GameState.Player player, (double x, double y, double radius) ballwall)
        {
            var dis = player.PlayerFoot.Position.NewAdded(new Vector(-ballwall.x, -ballwall.y));


            if (dis.Length < Constants.PlayerRadius + ballwall.radius)
            {
                var violationVector = (dis.Length == 0 ? new Vector(1,0) : dis.NewUnitized()).NewScaled((Constants.PlayerRadius + ballwall.radius) - dis.Length);
                player.ExternalVelocity = player.ExternalVelocity.NewAdded(violationVector);
            }
        }

    }


    public enum BallState
    {
        neither,
        obj1,
        obj2
    }

}
