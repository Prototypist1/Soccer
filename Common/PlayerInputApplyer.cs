using Physics2;
using Prototypist.TaskChain;
using System;
using System.Collections.Generic;

namespace Common
{
    public static class PlayerInputApplyer
    {
        private class MouseDrag
        {
            public Guid Id;
            public ConcurrentLinkedList<Vector> moves;
            public Vector residual = new Vector();

        }

        private static ConcurrentIndexed<Guid, MouseDrag> mouseDrags = new ConcurrentIndexed<Guid, MouseDrag>();

        public static bool HitInStride(Vector ballStart, Vector playerStart, Vector playerVelocity, double time, out Vector throwVelocity, out Vector catchLocation)
        {
            var diff = playerStart.NewAdded(ballStart.NewMinus());

            // don't try to hit yourself in stride
            if (diff.Length == 0)
            {
                throwVelocity = default;
                catchLocation = default;
                return false;
            }

            catchLocation = playerStart.NewAdded(playerVelocity.NewScaled(time));

            var v = HowHardToThrow(catchLocation.NewAdded(ballStart.NewMinus()).Length, time);

            if (v > Constants.maxThrowPower)
            {
                throwVelocity = default;
                catchLocation = default;
                return false;
            }
            throwVelocity = catchLocation.NewAdded(ballStart.NewMinus()).NewUnitized().NewScaled(v);
            return true;
        }

        public static bool HitInStride(Vector ballStart, Vector playerStart, Vector playerVelocity, out Vector throwVelocity, out Vector catchLocation)
        {
            var diff = playerStart.NewAdded(ballStart.NewMinus());

            // don't try to hit yourself in stride
            if (diff.Length == 0)
            {
                throwVelocity = default;
                catchLocation = default;
                return false;
            }

            var speedAWay = diff.NewUnitized().Dot(playerVelocity);

            var t1 = HowLongItTakesBallToGo(diff.Length, Constants.maxThrowPower);
            var t2 = HowLongItTakesBallToGo(diff.Length + speedAWay * t1, Constants.maxThrowPower);
            var t3 = HowLongItTakesBallToGo(diff.Length + speedAWay * t2, Constants.maxThrowPower);

            if (t3 == Double.MaxValue)
            {
                throwVelocity = default;
                catchLocation = default;
                return false;
            }
            catchLocation = playerStart.NewAdded(playerVelocity.NewScaled(t3));
            throwVelocity = diff.NewAdded(playerVelocity.NewScaled(t3)).NewUnitized().NewScaled(Constants.maxThrowPower);
            return true;
        }

        // how hard do I have to throw it for a player to catch it at a given point
        //public static Vector RequiredThrow(Vector playerStart, Vector ballStart, Vector target)
        //{

        //    var playerTime = HowQuicklyCanAPlayerMove(target.NewAdded(playerStart.NewMinus()).Length);
        //    var speed = HowHardToThrow(target.NewAdded(ballStart.NewMinus()).Length, playerTime);

        //    return target.NewAdded(ballStart.NewMinus()).NewUnitized().NewScaled(speed);
        //}

        // assuming the player runs at max speed the whole time
        // and assuming the ball doesn't have friction
        public static (double, Vector) IntersectBallTime(Vector start, Vector ballStart, Vector ballVelocity, Vector palyerVelocity, double padding, bool boost =false)
        {

            if (ballStart.NewAdded(start.NewMinus()).Length == 0)
            {
                return (0, start);
            }

            var at = 1.0;

            while (true)
            {
                var dissToBall = ballStart.NewAdded(start.NewMinus()).Length;
                var ballAt = ballStart;
                if (ballVelocity.Length > 0)
                {
                    ballAt = ballStart.NewAdded(ballVelocity.NewUnitized().NewScaled(DistanceBallTravels(ballVelocity.Length, at)));
                    ballAt = new Vector(ballAt.x % (2 * FieldDimensions.Default.xMax), ballAt.y % (2 * FieldDimensions.Default.yMax));
                    if (ballAt.x < -FieldDimensions.Default.xMax)
                    {
                        ballAt = new Vector((-(ballAt.x + FieldDimensions.Default.xMax)), ballAt.y);
                    }
                    else if (ballAt.x < 0)
                    {
                        ballAt = new Vector(-ballAt.x, ballAt.y);
                    }
                    else if (ballAt.x > FieldDimensions.Default.xMax)
                    {
                        ballAt = new Vector((2 * FieldDimensions.Default.xMax) - ballAt.x, ballAt.y);
                    }

                    if (ballAt.y < -FieldDimensions.Default.yMax)
                    {
                        ballAt = new Vector(ballAt.x, (-(ballAt.y + FieldDimensions.Default.yMax)));
                    }
                    else if (ballAt.y < 0)
                    {
                        ballAt = new Vector(ballAt.x, -ballAt.y);
                    }
                    else if (ballAt.y > FieldDimensions.Default.yMax)
                    {
                        ballAt = new Vector(ballAt.x, (2 * FieldDimensions.Default.yMax) - ballAt.y);
                    }

                    dissToBall = ballAt.NewAdded(start.NewMinus()).Length;
                }

                var diff = ballAt.NewAdded(start.NewMinus());

                var playerVelocityDot = diff.NewUnitized().Dot(palyerVelocity);
                var playerStartSpeed = new Vector(0, 0);
                if (playerVelocityDot > 0)
                {
                    playerStartSpeed = diff.NewUnitized().NewScaled(playerVelocityDot);
                }

                var dissTravelled = DistancePlayerTravels(playerStartSpeed.Length, at) + ((boost ? Constants.speedLimit : 0) * at) + padding;// + HowFarCanIBoost(boost);
                if (dissToBall > dissTravelled)
                {
                    at += 1;
                }
                else
                {
                    break;
                }
            }

            //while (true)
            //{
            //    var dissToBall = ballStart.NewAdded(start.NewMinus()).Length;
            //    var ballAt = ballStart;
            //    if (ballVelocity.Length > 0)
            //    {
            //        ballAt = ballStart.NewAdded(ballVelocity.NewUnitized().NewScaled(DistanceBallTravels(ballVelocity.Length, at)));
            //        ballAt = new Vector(ballAt.x % (2 * FieldDimensions.Default.xMax), ballAt.y % (2 * FieldDimensions.Default.yMax));
            //        if (ballAt.x < -FieldDimensions.Default.xMax)
            //        {
            //            ballAt = new Vector((-(ballAt.x + FieldDimensions.Default.xMax)), ballAt.y);
            //        }
            //        else if (ballAt.x < 0)
            //        {
            //            ballAt = new Vector(-ballAt.x, ballAt.y);
            //        }
            //        else if (ballAt.x > FieldDimensions.Default.xMax)
            //        {
            //            ballAt = new Vector((2 * FieldDimensions.Default.xMax) - ballAt.x, ballAt.y);
            //        }

            //        if (ballAt.y < -FieldDimensions.Default.yMax)
            //        {
            //            ballAt = new Vector(ballAt.x, (-(ballAt.y + FieldDimensions.Default.yMax)));
            //        }
            //        else if (ballAt.y < 0)
            //        {
            //            ballAt = new Vector(ballAt.x, -ballAt.y);
            //        }
            //        else if (ballAt.y > FieldDimensions.Default.yMax)
            //        {
            //            ballAt = new Vector(ballAt.x, (2 * FieldDimensions.Default.yMax) - ballAt.y);
            //        }

            //        dissToBall = ballAt.NewAdded(start.NewMinus()).Length;
            //    }

            //    var diff = ballAt.NewAdded(start.NewMinus());

            //    var playerVelocityDot = diff.NewUnitized().Dot(palyerVelocity);
            //    var playerStartSpeed = new Vector(0, 0);
            //    if (playerVelocityDot > 0)
            //    {
            //        playerStartSpeed = diff.NewUnitized().NewScaled(playerVelocityDot);
            //    }

            //    var dissTravelled = DistancePlayerTravels(playerStartSpeed.Length, at) + ((boost ? Constants.speedLimit : 0) * at) + padding;// + HowFarCanIBoost(boost);
            //    if (dissToBall < dissTravelled)
            //    {
            //        at -= 1;
            //    }
            //    else
            //    {
            //        break;
            //    }
            //}

            if (ballVelocity.Length == 0) {
                return (at, ballStart);
            }
            else { 
                return (at, ballStart.NewAdded(ballVelocity.NewUnitized().NewScaled(DistanceBallTravels(ballVelocity.Length, at))));
            }

        }

        // I wish I could insert a drawing to show what I am doing here
        // you try to maintain your direction relative to the ball
        // and any extra speed you have you spend on moving towards the ball
        public static Vector IntersectBallDirection(Vector start, Vector ballStart, Vector ballVelocity, Vector palyerVelocity, double padding, bool boost = false)
        {
            if (ballStart.NewAdded(start.NewMinus()).Length == 0)
            {
                return new Vector(0, 0);
            }

            var at = 1.0;

            while (true)
            {
                var dissToBall = ballStart.NewAdded(start.NewMinus()).Length;
                var ballAt = ballStart;
                if (ballVelocity.Length > 0)
                {
                    ballAt = ballStart.NewAdded(ballVelocity.NewUnitized().NewScaled(DistanceBallTravels(ballVelocity.Length, at)));
                    ballAt = new Vector(ballAt.x % (2 * FieldDimensions.Default.xMax), ballAt.y % (2 * FieldDimensions.Default.yMax));
                    if (ballAt.x < -FieldDimensions.Default.xMax)
                    {
                        ballAt = new Vector((-(ballAt.x + FieldDimensions.Default.xMax)), ballAt.y);
                    }
                    else if (ballAt.x < 0)
                    {
                        ballAt = new Vector(-ballAt.x, ballAt.y);
                    }
                    else if (ballAt.x > FieldDimensions.Default.xMax)
                    {
                        ballAt = new Vector((2 * FieldDimensions.Default.xMax) - ballAt.x, ballAt.y);
                    }

                    if (ballAt.y < -FieldDimensions.Default.yMax)
                    {
                        ballAt = new Vector(ballAt.x, (-(ballAt.y + FieldDimensions.Default.yMax)));
                    }
                    else if (ballAt.y < 0)
                    {
                        ballAt = new Vector(ballAt.x, -ballAt.y);
                    }
                    else if (ballAt.y > FieldDimensions.Default.yMax)
                    {
                        ballAt = new Vector(ballAt.x, (2 * FieldDimensions.Default.yMax) - ballAt.y);
                    }

                    dissToBall = ballAt.NewAdded(start.NewMinus()).Length;
                }

                var diff = ballAt.NewAdded(start.NewMinus());

                var playerVelocityDot = diff.NewUnitized().Dot(palyerVelocity);
                var playerStartSpeed = new Vector(0, 0);
                if (playerVelocityDot > 0)
                {
                    playerStartSpeed = diff.NewUnitized().NewScaled(playerVelocityDot);
                }

                var dissTravelled = DistancePlayerTravels(playerStartSpeed.Length , at) + ((boost ? Constants.speedLimit : 0)* at) + padding;// + HowFarCanIBoost(boost);
                if (dissToBall > dissTravelled)
                {
                    at += 1;
                }
                else
                {
                    break;
                }
            }

            //while (true)
            //{
            //    var dissToBall = ballStart.NewAdded(start.NewMinus()).Length;
            //    var ballAt = ballStart;
            //    if (ballVelocity.Length > 0)
            //    {
            //        ballAt = ballStart.NewAdded(ballVelocity.NewUnitized().NewScaled(DistanceBallTravels(ballVelocity.Length, at)));
            //        ballAt = new Vector(ballAt.x % (2 * FieldDimensions.Default.xMax), ballAt.y % (2 * FieldDimensions.Default.yMax));
            //        if (ballAt.x < -FieldDimensions.Default.xMax)
            //        {
            //            ballAt = new Vector((-(ballAt.x + FieldDimensions.Default.xMax)), ballAt.y);
            //        }
            //        else if (ballAt.x < 0)
            //        {
            //            ballAt = new Vector(-ballAt.x, ballAt.y);
            //        }
            //        else if (ballAt.x > FieldDimensions.Default.xMax)
            //        {
            //            ballAt = new Vector((2 * FieldDimensions.Default.xMax) - ballAt.x, ballAt.y);
            //        }

            //        if (ballAt.y < -FieldDimensions.Default.yMax)
            //        {
            //            ballAt = new Vector(ballAt.x, (-(ballAt.y + FieldDimensions.Default.yMax)));
            //        }
            //        else if (ballAt.y < 0)
            //        {
            //            ballAt = new Vector(ballAt.x, -ballAt.y);
            //        }
            //        else if (ballAt.y > FieldDimensions.Default.yMax)
            //        {
            //            ballAt = new Vector(ballAt.x, (2 * FieldDimensions.Default.yMax) - ballAt.y);
            //        }

            //        dissToBall = ballAt.NewAdded(start.NewMinus()).Length;
            //    }

            //    var diff = ballAt.NewAdded(start.NewMinus());

            //    var playerVelocityDot = diff.NewUnitized().Dot(palyerVelocity);
            //    var playerStartSpeed = new Vector(0, 0);
            //    if (playerVelocityDot > 0)
            //    {
            //        playerStartSpeed = diff.NewUnitized().NewScaled(playerVelocityDot);
            //    }

            //    var dissTravelled = DistancePlayerTravels(playerStartSpeed.Length, at) + ((boost ? Constants.speedLimit : 0) * at) + padding;// + HowFarCanIBoost(boost);
            //    if (dissToBall < dissTravelled)
            //    {
            //        at -= 1;
            //    }
            //    else
            //    {
            //        break;
            //    }
            //}

            if (ballVelocity.Length > 0)
            {
                return ballStart.NewAdded(ballVelocity.NewUnitized().NewScaled(DistanceBallTravels(ballVelocity.Length, at))).NewAdded(start.NewMinus()).NewUnitized();
            }
            return ballStart.NewAdded(start.NewMinus()).NewUnitized();
        }


        public static double HowLongItTakesBallToGo(double diff, double velocity)
        {
            // diff = velocity * (( 1.0 - Math.Pow((Constants.FrictionDenom - 1) / Constants.FrictionDenom, time)) * Constants.FrictionDenom)
            // diff / velocity=  (( 1.0 - Math.Pow((Constants.FrictionDenom - 1) / Constants.FrictionDenom, time)) * Constants.FrictionDenom)
            // (diff / (velocity * Constants.FrictionDenom)) - 1 = - Math.Pow((Constants.FrictionDenom - 1) / Constants.FrictionDenom, time)
            // 1 - (diff / (velocity * Constants.FrictionDenom)) = Math.Pow((Constants.FrictionDenom - 1) / Constants.FrictionDenom, time)
            // log base (Constants.FrictionDenom - 1) / Constants.FrictionDenom of (1- (diff / (velocity * Constants.FrictionDenom))) = time
            if (velocity * Constants.FrictionDenom <= diff)
            {
                return double.MaxValue;
            }

            return Math.Log((1 - (diff / (velocity * Constants.FrictionDenom))), (Constants.FrictionDenom - 1) / Constants.FrictionDenom);
        }


        public static double HowFarCanIBoost(double boost)
        {
            // boost = sum sqrt(howfarIvegone) * Constants.BoostConsumption / Constants.BoostPower from 0 to howfarIvegone
            // x is howfarIvegone
            // boost = x*x * Constants.BoostConsumption / (2*Constants.BoostPower)

            // powerused = integral d ^ 1/2
            // powerused = 2/3 *c*d ^ 3/2

            return (boost / Constants.BoostConsumption) + Constants.footLen - Constants.PlayerRadius;//, (2.0 / 3.0));
            //return Math.Pow(boost * (3.0 / 2.0) / Constants.BoostConsumption, (2.0 / 3.0));
        }

        public static double HowLongCanIBoost(double boost) {
            return boost / (Constants.BoostConsumption * Constants.speedLimit);
        }

        //public static double HowQuicklyCanAPlayerMove(double length)
        //{
        //    return length / Constants.bodySpeedLimit;
        //}

        public static double HowHardToThrow(double length, double time)
        {
            if (length == 0)
            {
                return 0;
            }

            // this is a sum
            // sum v*(Constants.FrictionDenom - 1)/ Constants.FrictionDenom)^t for t in time  = lenght
            // v * Constants.FrictionDenom - (Constants.FrictionDenom - 1)/ Constants.FrictionDenom)^t * v * Constants.FrictionDenom = length
            // (1 - ((Constants.FrictionDenom - 1)/ Constants.FrictionDenom))^t) * v * Constants.FrictionDenom = length
            // v = length / ((1 - ((Constants.FrictionDenom - 1)/ Constants.FrictionDenom))^t)  * Constants.FrictionDenom)
            return length / ((1.0 - Math.Pow((Constants.FrictionDenom - 1) / Constants.FrictionDenom, time)) * Constants.FrictionDenom);
        }

        public static double DistanceBallTravels(double velocity, double time)
        {
            var r = (Constants.FrictionDenom - 1.0) / Constants.FrictionDenom;
            var a = velocity;
            return a * (1 - Math.Pow(r, time - 1)) / (1 - r);
            //return velocity * Constants.FrictionDenom * (1.0 - Math.Pow((Constants.FrictionDenom - 1.0) / Constants.FrictionDenom, time));
        }

        public static double TimeBallTakesToTravel(double velocity, double distance) {
            var r = (Constants.FrictionDenom - 1.0) / Constants.FrictionDenom;
            var a = velocity;
            //distance =  a * (1 - Math.Pow(r, time - 1)) / (1 - r);
            //distance * (1 - r) / a =  (1 - Math.Pow(r, time - 1)) ;
            //(distance * (1 - r) / a) - 1 =  - Math.Pow(r, time - 1) ;
            //-(distance * (1 - r) / a) - 1) =  Math.Pow(r, time - 1) ;
            // log base r of -(distance * (1 - r) / a) - 1 =   time - 1 ;
            // (log base r of (-(distance * (1 - r) / a) - 1 ) + 1=   time - 1 ;

            // it'll never get there
            //if (distance > a / (1 - r))
            //{
            //    return double.MaxValue;
            //}

            var inner = 1 -(distance * (1 - r) / a);
            // it'll never get there
            if (inner < 1) {
                return double.MaxValue;
            }

            return Math.Log(inner, r) + 1;
        }

        public static double DistancePlayerTravels(double v0, double time)
        {
            // figure out how long it took to reach our current speed
            var t0 = Math.Floor( VtoE(v0) / Constants.EnergyAdd);
            return DistancePlayerTravelsFromStop(time + t0) - DistancePlayerTravelsFromStop(t0);
        }



        public static double DistancePlayerTravelsFromStop(double time)
        {
            // v = t*EnergyAdd ^ .5
            return (time * Constants.bodyStartAt) + Math.Pow(time * Constants.EnergyAdd,1 + Constants.bodyRoot) / ((1 + Constants.bodyRoot) * Constants.EnergyAdd);
            //return (time * Constants.bodyStartAt) + (((time * Constants.EnergyAdd) * Math.Log(time * Constants.EnergyAdd) - time * Constants.EnergyAdd) / (Math.Log(Constants.bodyEpo) * Constants.EnergyAdd));
        }

        private static double VtoE(double v)
        {
            //if (v < Constants.bodyStartAt)
            //{
            //    return v;
            //}
            //else
            //{
            //var simpleV = Math.Max(0, v - Constants.bodyStartAt);

            //var distanceToTop = ((Constants.bodySpeedLimit - Constants.bodyStartAt) - simpleV) / (Constants.bodySpeedLimit - Constants.bodyStartAt);

            //var eDistanceToTop = Math.Pow(Math.Max(0, distanceToTop), .5);

            //return ((1 - eDistanceToTop) * (Constants.bodySpeedLimit - Constants.bodyStartAt)) + Constants.bodyStartAt;
            //}
            // we need something we can integrate so that we can estiamate how far a player can go
            var simpleV = Math.Max(0, v - Constants.bodyStartAt);
            return Math.Pow(simpleV,1 / Constants.bodyRoot);
            //return Math.Pow(Constants.bodyEpo,/*Math.E*/ simpleV);
        }
        private static double EtoV(double e)
        {
            //if (e < VtoE(Constants.bodyStartAt))
            //{
            //    return e;
            //}
            //else {
            //var p2 = Math.Min(1, (e - Constants.bodyStartAt) / (Constants.bodySpeedLimit - Constants.bodyStartAt));
            //var p1 = 1 - p2;

            //return Constants.bodyStartAt + ((e - Constants.bodyStartAt) * p1) + ((Constants.bodySpeedLimit - Constants.bodyStartAt) * p2);
            //}
            return Math.Pow(e,Constants.bodyRoot) + Constants.bodyStartAt;
            //return Math.Log(Math.Max(1,e), Constants.bodyEpo) + Constants.bodyStartAt;
        }

        private static double SpeedLimit2(double d)
        {
            return Constants.speedLimit * (1.0 - (1.0 / (1.0 + (d * 10 / Constants.speedLimit))));
        }

        private static double SpeedLimit(double d)
        {

            var p2 = Math.Min(1, d / Constants.speedLimit);
            var p1 = 1 - p2;

            return (d * p1) + (Constants.speedLimit * p2);
        }

        public static void Apply(this GameState state, Dictionary<Guid, PlayerInputs> inputs)
        {
            // TODO what about count down state?

            // if the ball is not being held apply friction to it
            if (state.GameBall.OwnerOrNull == null)
            {
                //if (state.GameBall.Velocity.Length > 0)
                //{
                //    var friction = state.GameBall.Velocity.NewUnitized().NewScaled(-state.GameBall.Velocity.Length * state.GameBall.Velocity.Length * state.GameBall.Mass / (175.0 * 175));
                //    state.GameBall.Velocity = state.GameBall.Velocity.NewAdded(friction);

                //}

                if (state.GameBall.Velocity.Length > 0)
                {
                    var friction = state.GameBall.Velocity.NewUnitized().NewScaled(-state.GameBall.Velocity.Length * state.GameBall.Mass / Constants.FrictionDenom);
                    state.GameBall.Velocity = state.GameBall.Velocity.NewAdded(friction);
                }
            }

            // loop over the players and update them
            foreach (var player in state.players.Values)
            {
                player.ExternalVelocity = player.ExternalVelocity.NewScaled(Constants.ExternalVelocityFriction);

                //if (state.Frame % 90 == 0) {
                player.Boosts = Math.Min(2, Math.Max(player.Boosts + 1.0 / 90.0, -1));
                //}

                // handle inputs
                if (inputs.TryGetValue(player.Id, out var input))
                {



                    if (input.ControlScheme == ControlScheme.SipmleMouse)
                    {

                        var dx = input.BodyX - player.PlayerBody.Position.x;
                        var dy = input.BodyY - player.PlayerBody.Position.y;

                        if (dx != 0 || dy != 0)
                        {

                            // base velocity becomes the part of the velocity in the direction of the players movement
                            var v = new Vector(player.PlayerBody.Velocity.x, player.PlayerBody.Velocity.y);
                            var f = new Vector(dx, dy).NewUnitized();
                            var with = v.Dot(f);
                            var baseValocity = with > 0 ? f.NewUnitized().NewScaled(with) : new Vector(0, 0);

                            var engeryAdd = /*player.Id == state.GameBall.OwnerOrNull ? Constants.EnergyAdd / 2.0 :*/ Constants.EnergyAdd;

                            //
                            var finalE = VtoE(Math.Sqrt(Math.Pow(baseValocity.x, 2) + Math.Pow(baseValocity.y, 2))) + engeryAdd;
                            var inputAmount = new Vector(input.BodyX, input.BodyY).Length;
                            if (inputAmount < .1)
                            {
                                finalE = 0;
                            }
                            else if (inputAmount < 1)
                            {
                                finalE = Math.Min(finalE, (inputAmount - .1) * (inputAmount - .1) * engeryAdd * 100);
                            }

                            var finalSpeed = EtoV(finalE);
                            var finalVelocity = f.NewScaled(finalSpeed);

                            if (finalVelocity.Length > new Vector(dx, dy).Length)
                            {
                                finalVelocity = finalVelocity.NewUnitized().NewScaled(new Vector(dx, dy).Length);
                            }

                            player.PlayerBody.Velocity = finalVelocity;// / 2.0;
                        }
                        else
                        {
                            player.PlayerBody.Velocity = new Vector(0, 0);
                        }
                    }
                    else if (input.BodyX != 0 || input.BodyY != 0)
                    {
                        if (input.ControlScheme == ControlScheme.MouseAndKeyboard)
                        {
                            var v = new Vector(player.PlayerBody.Velocity.x, player.PlayerBody.Velocity.y);
                            if (v.Length > 0)
                            {
                                var f = new Vector(Math.Sign(input.BodyX), Math.Sign(input.BodyY)).NewUnitized();
                                var with = v.NewUnitized().Dot(f);
                                if (with <= 0)
                                {
                                    player.PlayerBody.Velocity = new Vector(0, 0);
                                }
                                else
                                {
                                    var withVector = f.NewScaled(with * v.Length);

                                    //var notWith = v.Length - withVector.Length;
                                    var notWith = v.NewAdded(withVector.NewScaled(-1));
                                    // dont' crush stuff with in 45 degrees
                                    var notWithScald = notWith.Length > withVector.Length ? notWith.NewScaled(with) : notWith;

                                    player.PlayerBody.Velocity = withVector.NewAdded(notWithScald);

                                    //player.PlayerBody.Velocity = f.NewScaled(withVector.Length + (notWith * Math.Sqrt(with)));//new Vector(withVector.x + notWithScald.x, withVector.y + notWithScald.y);
                                }
                            }

                            var damp = .9;//.98;

                            var engeryAdd = /*player.Id == state.GameBall.OwnerOrNull ? Constants.EnergyAdd / 2.0 :*/ Constants.EnergyAdd;

                            var R0 = EtoV(VtoE(player.PlayerBody.Velocity.Length) + engeryAdd);
                            var a = Math.Pow(Math.Sign(input.BodyX), 2) + Math.Pow(Math.Sign(input.BodyY), 2);
                            var b = 2 * ((Math.Sign(input.BodyX) * player.PlayerBody.Velocity.x * damp) + (Math.Sign(input.BodyY) * player.PlayerBody.Velocity.y * damp));
                            var c = Math.Pow(player.PlayerBody.Velocity.x * damp, 2) + Math.Pow(player.PlayerBody.Velocity.y * damp, 2) - Math.Pow(R0, 2);

                            var t = (-b + Math.Sqrt(Math.Pow(b, 2) - (4 * a * c))) / (2 * a);

                            player.PlayerBody.Velocity = new Vector((damp * player.PlayerBody.Velocity.x) + (t * input.BodyX), (damp * player.PlayerBody.Velocity.y) + (t * input.BodyY));// / 2.0;

                        }
                        else if (input.ControlScheme == ControlScheme.Controller || input.ControlScheme == ControlScheme.AI)
                        {
                            // base velocity becomes the part of the velocity in the direction of the players movement
                            var f = new Vector(input.BodyX, input.BodyY).NewUnitized();
                            var baseValocity = new Vector(0, 0);
                            if (player.PlayerBody.Velocity.Length > 0)
                            {
                                var with = player.PlayerBody.Velocity.NewUnitized().Dot(f);
                                if (with > 0)
                                {
                                    var withVector = f.NewScaled(with * player.PlayerBody.Velocity.Length);
                                    var notWith = player.PlayerBody.Velocity.Length - withVector.Length;
                                    baseValocity = withVector.NewAdded(f.NewScaled(notWith * with));
                                }
                            }


                            var engeryAdd = Constants.EnergyAdd;// player.Id == state.GameBall.OwnerOrNull ? Constants.EnergyAdd / 2.0 : Constants.EnergyAdd;

                            //
                            var finalE = VtoE(baseValocity.Length) + engeryAdd;
                            var inputAmount = new Vector(input.BodyX, input.BodyY).Length;
                            if (inputAmount < .1)
                            {
                                player.PlayerBody.Velocity = new Vector(0, 0);
                            }
                            else if (inputAmount < 0.9)
                            {
                                var finalSpeed = ((inputAmount - .1) / .8) * Constants.bodyStartAt;
                                var finalVelocity = f.NewScaled(finalSpeed);
                                player.PlayerBody.Velocity = finalVelocity;
                            }
                            else
                            {
                                var finalSpeed = EtoV(finalE);
                                var finalVelocity = f.NewScaled(finalSpeed);
                                player.PlayerBody.Velocity = finalVelocity;
                            }

                            // 
                            //if (input.Boost && player.Boosts >= 0)
                            //{
                            //    var move = f.NewScaled(Constants.BoostPower);
                            //    player.BoostVelocity = move;
                            //    player.BoostCenter = player.BoostCenter.NewAdded(move);
                            //    player.Boosts -= Constants.BoostConsumption * move.Length * player.BoostCenter.Length;
                            //}
                            //else {
                            //    player.BoostCenter = new Vector();
                            //    player.BoostVelocity = new Vector(0.0, 0.0);
                            //}
                        }
                    }
                    else
                    {
                        player.PlayerBody.Velocity = new Vector(0, 0);
                    }

                    player.BoostVelocity = player.BoostVelocity.NewScaled(0.90);

                    if (input.ControlScheme == ControlScheme.Controller)
                    {
                        var currentOffset = player.PlayerFoot.Position.NewAdded(player.PlayerBody.Position.NewMinus());
                        var targetOffset = new Vector(input.FootX, input.FootY).NewScaled(Constants.footLen - Constants.PlayerRadius);

                        var offsetDiff = targetOffset.NewAdded(currentOffset.NewMinus());

                        if (offsetDiff.Length < Constants.speedLimit && new Vector(input.BodyX, input.BodyY).Length > .98 && player.Boosts > 0 && input.Boost != Constants.NoMove)
                        {
                            player.PlayerFoot.Velocity = offsetDiff;
                            player.BoostVelocity = player.BoostVelocity.NewScaled(0.2).NewAdded(new Vector(input.BodyX, input.BodyY).NewUnitized().NewScaled(Constants.speedLimit - offsetDiff.Length));

                        }
                        else if (offsetDiff.Length > Constants.speedLimit)
                        {
                            offsetDiff.NewUnitized().NewScaled(Constants.speedLimit);
                            player.PlayerFoot.Velocity = offsetDiff;
                        }
                        else
                        {
                            player.PlayerFoot.Velocity = offsetDiff;
                        }
                    }
                    else if (input.ControlScheme == ControlScheme.AI)
                    {
                        var drag = mouseDrags.GetOrAdd(input.Id, new MouseDrag
                        {
                            Id = input.Boost,
                            moves = new ConcurrentLinkedList<Vector>()
                        });

                        if (input.Boost != Constants.NoMove && drag.Id != input.Boost)
                        {
                            drag.Id = input.Boost;
                            drag.moves = new ConcurrentLinkedList<Vector>();
                        }

                        var move = new Vector(input.FootX, input.FootY);//.NewScaled(1.0 / Constants.BoostSpread);

                        var totallyToAdd = drag.residual.NewAdded(move);
                        if (totallyToAdd.Length > Constants.speedLimit && input.Boost != Constants.NoMove)
                        {
                            while (totallyToAdd.Length > Constants.speedLimit)
                            {
                                var toAdd = totallyToAdd.NewUnitized().NewScaled(Constants.speedLimit);
                                drag.moves.Add(toAdd);
                                totallyToAdd = totallyToAdd.NewAdded(toAdd.NewMinus());
                            }
                            drag.residual = totallyToAdd;
                        }
                        else
                        {
                            if (move.Length > Constants.speedLimit)
                            {
                                move = move.NewUnitized().NewScaled(Constants.speedLimit);
                            }
                            drag.moves.Add(move);
                            drag.residual = new Vector(0, 0);
                        }

                        if (drag.moves.TryGetFirst(out var moveToApply))
                        {
                            drag.moves.RemoveStart();

                            var targetPos = player.PlayerFoot.Position.NewAdded(moveToApply);

                            var resultingOffest = targetPos.NewAdded(player.PlayerBody.Position.NewMinus());

                            if (resultingOffest.Length > Constants.footLen - Constants.PlayerRadius && moveToApply.Length > 0 && player.Boosts > 0 && input.Boost != Constants.NoMove)
                            {

                                var currentOffset = player.PlayerFoot.Position.NewAdded(player.PlayerBody.Position.NewMinus());
                                var targetOffest = resultingOffest.NewUnitized().NewScaled(Constants.footLen - Constants.PlayerRadius);

                                var offsetDiff = targetOffest.NewAdded(currentOffset.NewMinus());

                                player.PlayerFoot.Velocity = offsetDiff;

                                player.BoostVelocity = player.BoostVelocity.NewScaled(0.2).NewAdded(moveToApply.NewUnitized().NewScaled(Constants.speedLimit - offsetDiff.Length));

                            }
                            else if (resultingOffest.Length > Constants.footLen - Constants.PlayerRadius && moveToApply.Length > 0)
                            {
                                var currentOffset = player.PlayerFoot.Position.NewAdded(player.PlayerBody.Position.NewMinus());
                                var targetOffest = resultingOffest.NewUnitized().NewScaled(Constants.footLen - Constants.PlayerRadius);

                                var offsetDiff = targetOffest.NewAdded(currentOffset.NewMinus());

                                player.PlayerFoot.Velocity = offsetDiff;
                            }
                            else
                            {
                                player.PlayerFoot.Velocity = moveToApply;
                            }

                        }
                        else if (drag.Id != Constants.NoMove)
                        {
                            drag.Id = Constants.NoMove;
                        }
                    }

                    player.Boosts -= Constants.BoostConsumption * player.BoostVelocity.Length;


                    // throwing 2
                    //if (!player.Throwing && input.Throw)
                    //{
                    //    player.ProposedThrow = new Vector(0, 0);
                    //}
                    //if (input.Boost == Constants.NoMove)
                    //{
                    if (input.ControlScheme == ControlScheme.Controller)
                    {
                        player.ProposedThrow = new Vector(input.FootX, input.FootY).NewScaled(Constants.maxThrowPower);
                    }
                    else if (input.ControlScheme == ControlScheme.AI)
                    {
                        if (new Vector(input.FootX, input.FootY).Length > 1)
                        {
                            player.ProposedThrow = new Vector(0, 0);
                        }
                        else
                        {

                            player.ProposedThrow = new Vector(input.FootX, input.FootY).NewScaled(Constants.maxThrowPower);//.NewAdded(player.PlayerBody.Velocity.NewMinus());
                        }
                    }
                    else if (input.ControlScheme == ControlScheme.MouseAndKeyboard)
                    {
                        player.ProposedThrow = player.ProposedThrow.NewAdded(new Vector(input.FootX, input.FootY).NewScaled(.5));

                        if (player.ProposedThrow.Length > Constants.maxThrowPower)
                        {
                            player.ProposedThrow = player.ProposedThrow.NewUnitized().NewScaled(Constants.maxThrowPower);
                        }
                    }
                    else if (input.ControlScheme == ControlScheme.SipmleMouse)
                    {
                        throw new NotImplementedException();
                    }
                    //}

                    if (input.Throw && state.GameBall.OwnerOrNull == player.Id)
                    {
                        state.GameBall.Velocity = player.ProposedThrow;//.NewAdded(player.PlayerBody.Velocity).NewAdded(player.ExternalVelocity).NewAdded(player.BoostVelocity);
                                                                       //player.PlayerBody.Velocity = player.PlayerBody.Velocity.NewScaled(2);
                        state.players[state.GameBall.OwnerOrNull.Value].LastHadBall = state.Frame;
                        state.GameBall.OwnerOrNull = null;
                        //player.ProposedThrow = new Vector();
                    }

                    //player.Throwing = input.Throw;




                    // handle throwing
                    //{

                    //    if (player == state.ball.ownerOrNull)
                    //    {
                    //        state.ball.velocity = player.externalVelocity.NewAdded(player.body.velocity).NewAdded(player.foot.velocity);
                    //    }

                    //    var throwV = player.foot.velocity;

                    //    // I think force throw is making throwing harder
                    //    if (forceThrow && player == state.ball.ownerOrNull)
                    //    {

                    //        var newPart = 1;//Math.Max(1, throwV.Length);
                    //        var oldPart = 2;// Math.Max(1, ball.proposedThrow.Length);

                    //        player.proposedThrow = new Vector(
                    //                ((throwV.x * newPart) + (player.proposedThrow.x * oldPart)) / (newPart + oldPart),
                    //                ((throwV.y * newPart) + (player.proposedThrow.y * oldPart)) / (newPart + oldPart));

                    //        // throw the ball!
                    //        // duplicate code // {C7BF7AF7-2C8E-4094-85F6-E7C19F6F71C9}
                    //        state.ball.velocity = player.proposedThrow.NewAdded(player.body.velocity).NewAdded(player.externalVelocity);
                    //        state.ball.ownerOrNull.lastHadBall = state.frame;
                    //        state.ball.ownerOrNull = null;
                    //        forceThrow = false;
                    //        player.proposedThrow = new Vector();
                    //    }
                    //    else if (player.throwing)
                    //    {

                    //        if (player.proposedThrow.Length > Constants.MimimunThrowingSpped && (throwV.Length * 1.3 < player.proposedThrow.Length || throwV.Dot(player.proposedThrow) < 0) && player.throwing && player == state.ball.ownerOrNull)
                    //        {
                    //            // throw the ball!
                    //            // duplicate code // {C7BF7AF7-2C8E-4094-85F6-E7C19F6F71C9}
                    //            state.ball.velocity = player.proposedThrow.NewAdded(player.body.velocity).NewAdded(player.externalVelocity);
                    //            state.ball.ownerOrNull.lastHadBall = state.frame;
                    //            state.ball.ownerOrNull = null;
                    //            forceThrow = false;
                    //            player.proposedThrow = new Vector();
                    //        }
                    //        else if (player.proposedThrow.Length == 0)
                    //        {
                    //            player.proposedThrow = throwV;
                    //        }
                    //        else
                    //        {
                    //            var newPart = 1;//Math.Max(1, throwV.Length);
                    //            var oldPart = 4;// Math.Max(1, ball.proposedThrow.Length);
                    //            player.proposedThrow = new Vector(
                    //                        ((throwV.x * newPart) + (player.proposedThrow.x * oldPart)) / (newPart + oldPart),
                    //                        ((throwV.y * newPart) + (player.proposedThrow.y * oldPart)) / (newPart + oldPart));
                    //        }
                    //    }
                    //    else
                    //    {
                    //        player.proposedThrow = new Vector();
                    //    }
                    //}
                }

                state.Frame++;

            }

        }
    }
}
