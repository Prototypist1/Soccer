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

        }

        private static ConcurrentIndexed<Guid, MouseDrag> mouseDrags = new ConcurrentIndexed<Guid, MouseDrag>();

        // how hard do I have to throw it for a player to catch it at a given point
        public static Vector RequiredThrow(Vector playerStart, Vector ballStart, Vector target)
        {

            var playerTime = HowQuicklyCanAPlayerMove(target.NewAdded(playerStart.NewMinus()).Length);
            var speed = HowHardToThrow(target.NewAdded(ballStart.NewMinus()).Length, playerTime);

            return target.NewAdded(ballStart.NewMinus()).NewUnitized().NewScaled(speed);
        }

        // assuming the player runs at max speed the whole time
        // and assuming the ball doesn't have friction
        public static double IntersectBallTime(Vector start, Vector ballStart, Vector ballVelocity, Vector palyerVelocity)
        {
            var diff = ballStart.NewAdded(start.NewMinus());

            if (diff.Length == 0)
            {
                return 0;
            }

            var speedAWay = diff.NewUnitized().Dot(ballVelocity);

            if (speedAWay > Constants.bodySpeedLimit)
            {
                return double.MaxValue;
            }

            var speedPerpendicular = new Vector(diff.y, -diff.x).NewUnitized().Dot(ballVelocity);
            if (Math.Abs(speedPerpendicular) > Constants.bodySpeedLimit)
            {
                return double.MaxValue;
            }

            var effectiveSpeed = Math.Sqrt((Constants.bodySpeedLimit * Constants.bodySpeedLimit) - (speedPerpendicular * speedPerpendicular));

            if (effectiveSpeed - speedAWay <= 0)
            {
                return double.MaxValue;
            }

            var playerVelocityDot = diff.NewUnitized().Dot(palyerVelocity);
            var playerStartSpeed = new Vector(0, 0);
            if (playerVelocityDot > 0)
            {
                playerStartSpeed = diff.NewUnitized().NewScaled(playerVelocityDot);
            }

            var at = 256.0;
            for (var rate = 128.0; rate >= 1; rate *= .5)
            {
                var diss = diff.Length + DistanceBallTravels(speedAWay, at) - DistancePlayerTravels(playerStartSpeed.Length, at);
                if (diss == 0)
                {
                    return at;
                }
                if (diss > 0)
                {
                    at += rate;
                }
                else
                {
                    at -= rate;
                }
            }
            return at;
        }

        // I wish I could insert a drawing to show what I am doing here
        // you try to maintain your direction relative to the ball
        // and any extra speed you have you spend on moving towards the ball
        public static Vector IntersectBallDirection(Vector start, Vector ballStart, Vector ballVelocity)
        {
            var diff = ballStart.NewAdded(start.NewMinus());

            if (diff.Length == 0)
            {
                return new Vector(0.0, 0.0);
            }

            if (ballVelocity.Length == 0)
            {
                return diff.NewUnitized();
            }

            var speedAWay = diff.NewUnitized().Dot(ballVelocity);

            if (speedAWay > Constants.bodySpeedLimit)
            {
                //it's hopeless just run at it
                return diff.NewUnitized();
            }

            var speedPerpendicular = new Vector(diff.y, -diff.x).NewUnitized().Dot(ballVelocity);
            if (Math.Abs(speedPerpendicular) > Constants.bodySpeedLimit)
            {
                //it's hopeless just run at it
                return diff.NewUnitized();
            }

            var effectiveSpeed = Math.Sqrt((Constants.bodySpeedLimit * Constants.bodySpeedLimit) - (speedPerpendicular * speedPerpendicular));

            if (effectiveSpeed - speedAWay <= 0)
            {
                //it's hopeless just run at it
                return diff.NewUnitized();
            }
            var res = new Vector(diff.y, -diff.x).NewUnitized().NewScaled(speedPerpendicular)
                .NewAdded(diff.NewUnitized().NewScaled(effectiveSpeed));

            if (res.Length == 0 || double.IsNaN(res.Length))
            {
                var ahh = 0;
            }

            return res
                .NewUnitized();
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
            return Math.Pow(boost * (3.0 / 2.0) / Constants.BoostConsumption, (2.0 / 3.0));
        }

        public static double HowQuicklyCanAPlayerMove(double length)
        {
            return length / Constants.bodySpeedLimit;
        }

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
            return velocity * Constants.FrictionDenom * (1.0 - Math.Pow((Constants.FrictionDenom - 1) / Constants.FrictionDenom, time));
        }

        public static double DistancePlayerTravels(double v0, double time)
        {
            // figure out how long it took to reach our current speed
            var t0 = VtoE(v0) / Constants.EnergyAdd;
            return DistancePlayerTravelsFromStop(time + t0) - DistancePlayerTravelsFromStop(t0);
        }

        public static double DistancePlayerTravelsFromStop(double time)
        {
            return time * Constants.bodyStartAt + (((time * Constants.EnergyAdd + 1) * Math.Log(time * Constants.EnergyAdd + 1) - time * Constants.EnergyAdd) / (Math.Log(2) * Constants.EnergyAdd));
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
            var simpleV = Math.Max(0, v - Constants.bodyStartAt) + 1;
            return Math.Pow(1.1,/*Math.E*/ simpleV);
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

            return Math.Log(e, 1.1) - 1 + Constants.bodyStartAt;
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
                player.ExternalVelocity = player.ExternalVelocity.NewScaled(.95);

                //if (state.Frame % 90 == 0) {
                player.Boosts = Math.Min(3, Math.Max(player.Boosts + 1.0 / 90.0, -1));
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
                        // crush oppozing forces
                        if (input.ControlScheme == ControlScheme.MouseAndKeyboard)
                        {
                            var v = new Vector(player.PlayerBody.Velocity.x, player.PlayerBody.Velocity.y);
                            var f = new Vector(Math.Sign(input.BodyX), Math.Sign(input.BodyY));
                            var with = v.Dot(f) / f.Length;
                            if (with <= 0)
                            {
                                player.PlayerBody.Velocity = new Vector(0, 0);
                            }
                            else
                            {
                                var withVector = f.NewUnitized().NewScaled(with);
                                var notWith = v.NewAdded(withVector.NewScaled(-1));
                                var notWithScald = notWith.Length > withVector.Length ? notWith.NewUnitized().NewScaled(with) : notWith;

                                player.PlayerBody.Velocity = new Vector(withVector.x + notWithScald.x, withVector.y + notWithScald.y);
                            }

                            var damp = .98;

                            var engeryAdd = /*player.Id == state.GameBall.OwnerOrNull ? Constants.EnergyAdd / 2.0 :*/ Constants.EnergyAdd;

                            var R0 = EtoV(VtoE(Math.Sqrt(Math.Pow(player.PlayerBody.Velocity.x, 2) + Math.Pow(player.PlayerBody.Velocity.y, 2))) + engeryAdd);
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
                            var with = player.PlayerBody.Velocity.Dot(f);
                            var baseValocity = with > 0 ? f.NewUnitized().NewScaled(with) : new Vector(0, 0);

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


                    var max = Constants.footLen - Constants.PlayerRadius;// - Constants.PlayerRadius;

                    if (!input.Throwing)
                    {
                        if (input.ControlScheme == ControlScheme.Controller)
                        {
                            var tx = (input.FootX * max) + player.PlayerBody.Position.x;
                            var ty = (input.FootY * max) + player.PlayerBody.Position.y;

                            var vector = new Vector(tx - player.PlayerFoot.Velocity.x, ty - player.PlayerFoot.Velocity.y);


                            var v = new Vector(tx - player.PlayerFoot.Position.x, ty - player.PlayerFoot.Position.y);

                            var len = v.Length;
                            if (len != 0)
                            {
                                var speedLimit = SpeedLimit(len);
                                v = v.NewUnitized().NewScaled(speedLimit);
                            }
                            player.PlayerFoot.Velocity = v;
                        }
                        else if (input.ControlScheme == ControlScheme.MouseAndKeyboard || input.ControlScheme == ControlScheme.AI)
                        {
                            //if (true) {

                            player.BoostVelocity = new Vector(0, 0);


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

                            if (player.Boosts <= 0)
                            {
                                drag.moves = new ConcurrentLinkedList<Vector>();
                            }
                            else
                            {
                                var move = new Vector(input.FootX, input.FootY).NewScaled(1.0 / Constants.BoostSpread);

                                if (input.Boost != Constants.NoMove)
                                {
                                    if (move.Length != 0)
                                    {
                                        var speedLimit = SpeedLimit2(move.Length);
                                        move = move.NewUnitized().NewScaled(speedLimit);
                                    }
                                    for (int i = 0; i < Constants.BoostSpread; i++)
                                    {
                                        drag.moves.Add(move);
                                    }
                                }

                                if (drag.moves.TryGetFirst(out move))
                                {
                                    drag.moves.RemoveStart();

                                    if (player.Boosts > 0)
                                    {
                                        player.BoostVelocity = move;
                                    }
                                }
                            }


                            player.Boosts -= Constants.BoostConsumption * player.BoostVelocity.Length * Math.Sqrt(player.BoostCenter.Length);
                            player.BoostCenter = player.BoostCenter.NewAdded(player.BoostVelocity);
                            if (player.BoostVelocity.Length < .1)
                            {
                                player.BoostCenter = player.BoostCenter.NewScaled(.95);
                            }


                        }
                        else if (input.ControlScheme == ControlScheme.SipmleMouse)
                        {

                            var dx = input.FootX - player.PlayerBody.Position.x;
                            var dy = input.FootY - player.PlayerBody.Position.y;

                            var d = new Vector(dx, dy);

                            if (d.Length > max)
                            {
                                d = d.NewUnitized().NewScaled(max);
                            }

                            var validTx = d.x + player.PlayerBody.Position.x;
                            var validTy = d.y + player.PlayerBody.Position.y;

                            var vx = validTx - player.PlayerFoot.Position.x;
                            var vy = validTy - player.PlayerFoot.Position.y;

                            var v = new Vector(vx, vy);

                            // simple mouse drive the speed limit when it needs to
                            var len = v.Length;
                            if (len > Constants.speedLimit)
                            {
                                v = v.NewUnitized().NewScaled(Constants.speedLimit);
                            }

                            player.PlayerFoot.Velocity = v;
                        }
                        else
                        {

                            player.BoostVelocity = new Vector(0, 0);// player.BoostVelocity.NewScaled(Constants.BoostFade);
                            player.Boosts -= Constants.BoostConsumption * player.BoostVelocity.Length * player.BoostCenter.Length;
                            player.BoostCenter = player.BoostCenter.NewAdded(player.BoostVelocity);
                            player.BoostCenter = player.BoostCenter.NewScaled(.95);

                            if (input.ControlScheme == ControlScheme.Controller)
                            {
                                player.PlayerFoot.Velocity = new Vector(0, 0);
                            }
                            else if (input.ControlScheme == ControlScheme.MouseAndKeyboard || input.ControlScheme == ControlScheme.AI)
                            {
                                // you can boost and throw
                                // no you can't
                                //var move = new Vector(input.FootX, input.FootY);

                                //if (move.Length != 0)
                                //{
                                //    var speedLimit = SpeedLimit(move.Length);
                                //    move = move.NewUnitized().NewScaled(speedLimit);
                                //}
                                //// shared code {8F7CB418-A1DF-4932-A55C-922032F7C48C}
                                //if (input.Boost && player.Boosts >= 0)
                                //{
                                //    var move2 = move.NewScaled(Constants.BoostPower);
                                //    player.BoostVelocity = move2;
                                //    player.BoostCenter = player.BoostCenter.NewAdded(move2);
                                //    player.Boosts -= Constants.BoostConsumption * move2.Length * player.BoostCenter.Length;
                                //}
                                //else
                                //{
                                //    player.BoostCenter = new Vector();
                                //    player.BoostVelocity = new Vector(0.0, 0.0);
                                //}

                                var diff = player.PlayerBody.Position.NewAdded(player.PlayerFoot.Position.NewMinus());
                                if (diff.Length > max)
                                {
                                    player.PlayerFoot.Velocity = player.PlayerFoot.Velocity
                                        .NewAdded(diff.NewUnitized().NewScaled((diff.Length - max) / 2.0));// .NewScaled((1.0- partYou)/100.0)
                                }
                                else
                                {
                                    player.PlayerFoot.Velocity = new Vector(0, 0);
                                }
                            }
                            else
                            {
                                throw new NotImplementedException("zzzzzzzzzzzz");
                            }
                        }
                    }


                    // throwing 2
                    if (!player.Throwing && input.Throwing)
                    {
                        player.ProposedThrow = new Vector(0, 0);
                    }
                    if (input.Throwing)
                    {
                        if (input.ControlScheme == ControlScheme.Controller)
                        {
                            player.ProposedThrow = new Vector(input.FootX, input.FootY).NewScaled(Constants.maxThrowPower);
                        }
                        else if (input.ControlScheme == ControlScheme.AI)
                        {
                            player.ProposedThrow = new Vector(input.FootX, input.FootY).NewScaled(Constants.maxThrowPower);//.NewAdded(player.PlayerBody.Velocity.NewMinus());
                        }
                        else if (input.ControlScheme == ControlScheme.MouseAndKeyboard)
                        {
                            player.ProposedThrow = player.ProposedThrow.NewAdded(new Vector(input.FootX, input.FootY).NewScaled(2));

                            if (player.ProposedThrow.Length > Constants.maxThrowPower)
                            {
                                player.ProposedThrow = player.ProposedThrow.NewUnitized().NewScaled(Constants.maxThrowPower);
                            }
                        }
                        else if (input.ControlScheme == ControlScheme.SipmleMouse)
                        {
                            throw new NotImplementedException();
                        }
                    }

                    if (player.Throwing && !input.Throwing && state.GameBall.OwnerOrNull == player.Id)
                    {
                        state.GameBall.Velocity = player.ProposedThrow;//.NewAdded(player.PlayerBody.Velocity).NewAdded(player.ExternalVelocity).NewAdded(player.BoostVelocity);
                                                                       //player.PlayerBody.Velocity = player.PlayerBody.Velocity.NewScaled(2);
                        state.players[state.GameBall.OwnerOrNull.Value].LastHadBall = state.Frame;
                        state.GameBall.OwnerOrNull = null;
                        player.ProposedThrow = new Vector();
                    }

                    player.Throwing = input.Throwing;




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
