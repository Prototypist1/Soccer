using Physics2;
using System;
using System.Collections.Generic;

namespace Common
{
    public static class PlayerInputApplyer {

        private static double E(double v)
        {
            var simpleV = Math.Max(0, v - Constants.bodyStartAt);

            var distanceToTop = ((Constants.bodySpeedLimit - Constants.bodyStartAt) - simpleV) / (Constants.bodySpeedLimit - Constants.bodyStartAt);

            var eDistanceToTop = Math.Pow(Math.Max(0, distanceToTop), .5);

            return ((1 - eDistanceToTop) * (Constants.bodySpeedLimit - Constants.bodyStartAt)) + Constants.bodyStartAt;
        }
        private static double EInverse(double e)
        {
            var p2 = Math.Min(1, (e - Constants.bodyStartAt) / (Constants.bodySpeedLimit - Constants.bodyStartAt));
            var p1 = 1 - p2;

            return Constants.bodyStartAt + ((e - Constants.bodyStartAt) * p1) + ((Constants.bodySpeedLimit - Constants.bodyStartAt) * p2);
        }

        public static void Apply(this GameState state, Dictionary<Guid, PlayerInputs> inputs)
        {
            // TODO what about count down state?

            // if the ball is not being held apply friction to it
            if (state.ball.ownerOrNull == null)
            {
                if (state.ball.velocity.Length > 0)
                {
                    var friction = state.ball.velocity.NewUnitized().NewScaled(-state.ball.velocity.Length * state.ball.velocity.Length * state.ball.mass / (175.0 * 175));
                    state.ball.velocity = state.ball.velocity.NewAdded(friction);

                }

                if (state.ball.velocity.Length > 0)
                {
                    var friction = state.ball.velocity.NewUnitized().NewScaled(-state.ball.velocity.Length * state.ball.mass / Constants.FrictionDenom);
                    state.ball.velocity = state.ball.velocity.NewAdded(friction);
                }
            }

            // loop over the players and update them
            foreach (var player in state.players.Values)
            {
                player.externalVelocity = player.externalVelocity.NewScaled(.8);

                var forceThrow = false;

                // handle inputs
                if (inputs.TryGetValue(player.id, out var input)) {


                    if (player.throwing && !input.Throwing && state.ball.ownerOrNull == player)
                    {
                        forceThrow = true;
                    }

                    player.throwing = input.Throwing;


                    if (input.ControlScheme == ControlScheme.SipmleMouse)
                    {

                        var dx = input.BodyX - player.body.position.x;
                        var dy = input.BodyY - player.body.position.y;

                        if (dx != 0 || dy != 0)
                        {

                            // base velocity becomes the part of the velocity in the direction of the players movement
                            var v = new Vector(player.body.velocity.x + player.externalVelocity.x, player.body.velocity.y + player.externalVelocity.y);
                            var f = new Vector(dx, dy).NewUnitized();
                            var with = v.Dot(f);
                            var baseValocity = with > 0 ? f.NewUnitized().NewScaled(with) : new Vector(0, 0);

                            var engeryAdd = player == state.ball.ownerOrNull ? Constants.EnergyAdd / 2.0 : Constants.EnergyAdd;

                            //
                            var finalE = E(Math.Sqrt(Math.Pow(baseValocity.x, 2) + Math.Pow(baseValocity.y, 2))) + engeryAdd;
                            var inputAmount = new Vector(input.BodyX, input.BodyY).Length;
                            if (inputAmount < .1)
                            {
                                finalE = 0;
                            }
                            else if (inputAmount < 1)
                            {
                                finalE = Math.Min(finalE, (inputAmount - .1) * (inputAmount - .1) * engeryAdd * 100);
                            }

                            var finalSpeed = EInverse(finalE);
                            var finalVelocity = f.NewScaled(finalSpeed);

                            if (finalVelocity.Length > new Vector(dx, dy).Length)
                            {
                                finalVelocity = finalVelocity.NewUnitized().NewScaled(new Vector(dx, dy).Length);
                            }

                            player.body.velocity = finalVelocity;// / 2.0;
                        }
                        else
                        {
                            player.body.velocity = new Vector(0, 0);
                        }
                    }
                    else if (input.BodyX != 0 || input.BodyY != 0)
                    {
                        // crush oppozing forces
                        if (input.ControlScheme == ControlScheme.MouseAndKeyboard)
                        {
                            var v = new Vector(player.body.velocity.x, player.body.velocity.y);
                            var f = new Vector(Math.Sign(input.BodyX), Math.Sign(input.BodyY));
                            var with = v.Dot(f) / f.Length;
                            if (with <= 0)
                            {
                                player.body.velocity = new Vector( 0,0);
                            }
                            else
                            {
                                var withVector = f.NewUnitized().NewScaled(with);
                                var notWith = v.NewAdded(withVector.NewScaled(-1));
                                var notWithScald = notWith.Length > withVector.Length ? notWith.NewUnitized().NewScaled(with) : notWith;

                                player.body.velocity = new Vector( withVector.x + notWithScald.x,withVector.y + notWithScald.y);
                            }


                            var damp = .98;


                            var engeryAdd = player == state.ball.ownerOrNull ? Constants.EnergyAdd / 2.0 : Constants.EnergyAdd;

                            var R0 = EInverse(E(Math.Sqrt(Math.Pow(player.body.velocity.x, 2) + Math.Pow(player.body.velocity.y, 2))) + engeryAdd);
                            var a = Math.Pow(Math.Sign(input.BodyX), 2) + Math.Pow(Math.Sign(input.BodyY), 2);
                            var b = 2 * ((Math.Sign(input.BodyX) * player.body.velocity.x * damp) + (Math.Sign(input.BodyY) * player.body.velocity.y * damp));
                            var c = Math.Pow(player.body.velocity.x * damp, 2) + Math.Pow(player.body.velocity.y * damp, 2) - Math.Pow(R0, 2);

                            var t = (-b + Math.Sqrt(Math.Pow(b, 2) - (4 * a * c))) / (2 * a);

                            player.body.velocity = new Vector( (damp * player.body.velocity.x) + (t * input.BodyX),(damp * player.body.velocity.y) + (t * input.BodyY));// / 2.0;
                        }
                        else if (input.ControlScheme == ControlScheme.Controller)
                        {
                            // base velocity becomes the part of the velocity in the direction of the players movement
                            var v = new Vector(player.body.velocity.x, player.body.velocity.y);
                            var f = new Vector(input.BodyX, input.BodyY).NewUnitized();
                            var with = v.Dot(f);
                            var baseValocity = with > 0 ? f.NewUnitized().NewScaled(with) : new Vector(0, 0);

                            var engeryAdd = player == state.ball.ownerOrNull ? Constants.EnergyAdd / 2.0 : Constants.EnergyAdd;

                            //
                            var finalE = E(Math.Sqrt(Math.Pow(baseValocity.x, 2) + Math.Pow(baseValocity.y, 2))) + engeryAdd;
                            var inputAmount = new Vector(input.BodyX, input.BodyY).Length;
                            if (inputAmount < .1)
                            {
                                finalE = 0;
                            }
                            else if (inputAmount < 1)
                            {
                                finalE = Math.Min(finalE, (inputAmount - .1) * (inputAmount - .1) * engeryAdd * 100);
                            }

                            var finalSpeed = EInverse(finalE);
                            var finalVelocity = f.NewScaled(finalSpeed);


                            var vector = new Vector(finalVelocity.x - player.body.velocity.x, finalVelocity.y - player.body.velocity.y);

                            player.body.velocity = new Vector(player.body.velocity.x + vector.x, player.body.velocity.y + vector.y);
                        }
                    }
                    else
                    {
                        player.body.velocity = new Vector(0, 0);
                    }
                }

                // handle throwing
                {

                    if (player == state.ball.ownerOrNull)
                    {
                        state.ball.velocity = player.externalVelocity.NewAdded(player.body.velocity).NewAdded(player.foot.velocity);
                    }

                    var throwV = player.foot.velocity;

                    // I think force throw is making throwing harder
                    if (forceThrow && player == state.ball.ownerOrNull)
                    {

                        var newPart = 1;//Math.Max(1, throwV.Length);
                        var oldPart = 2;// Math.Max(1, ball.proposedThrow.Length);

                        player.proposedThrow = new Vector(
                                ((throwV.x * newPart) + (player.proposedThrow.x * oldPart)) / (newPart + oldPart),
                                ((throwV.y * newPart) + (player.proposedThrow.y * oldPart)) / (newPart + oldPart));

                        // throw the ball!
                        // duplicate code // {C7BF7AF7-2C8E-4094-85F6-E7C19F6F71C9}
                        state.ball.velocity = player.proposedThrow.NewAdded(player.body.velocity).NewAdded(player.externalVelocity);
                        state.ball.ownerOrNull.lastHadBall = state.frame;
                        state.ball.ownerOrNull = null;
                        forceThrow = false;
                        player.proposedThrow = new Vector();
                    }
                    else if (player.throwing)
                    {

                        if (player.proposedThrow.Length > Constants.MimimunThrowingSpped && (throwV.Length * 1.3 < player.proposedThrow.Length || throwV.Dot(player.proposedThrow) < 0) && player.throwing && player == state.ball.ownerOrNull)
                        {
                            // throw the ball!
                            // duplicate code // {C7BF7AF7-2C8E-4094-85F6-E7C19F6F71C9}
                            state.ball.velocity = player.proposedThrow.NewAdded(player.body.velocity).NewAdded(player.externalVelocity);
                            state.ball.ownerOrNull.lastHadBall = state.frame;
                            state.ball.ownerOrNull = null;
                            forceThrow = false;
                            player.proposedThrow = new Vector();
                        }
                        else if (player.proposedThrow.Length == 0)
                        {
                            player.proposedThrow = throwV;
                        }
                        else
                        {
                            var newPart = 1;//Math.Max(1, throwV.Length);
                            var oldPart = 4;// Math.Max(1, ball.proposedThrow.Length);
                            player.proposedThrow = new Vector(
                                        ((throwV.x * newPart) + (player.proposedThrow.x * oldPart)) / (newPart + oldPart),
                                        ((throwV.y * newPart) + (player.proposedThrow.y * oldPart)) / (newPart + oldPart));
                        }
                    }
                    else
                    {
                        player.proposedThrow = new Vector();
                    }
                }
            }

            state.frame++;

        }
    }
}
