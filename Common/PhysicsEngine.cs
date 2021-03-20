using Common;
using Physics2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using static Common.Game;


namespace physics2
{

    public static class PhysicsEngine2
    {

        public static IEnumerable<(GameState.Player, GameState.Player)> PlayerPairs(GameState gameState)
        {
            foreach (var p1 in gameState.players.Values)
            {
                foreach (var p2 in gameState.players.Values)
                {
                    if (p1 != p2)
                    {
                        yield return (p1, p2);
                    }
                }
            }
        }

        private class UpdateAction
        {
            public double time;
            public Action action;

        }

        // try to move forward by 1 time;
        public static void Simulate(this GameState gameState, GameStateTracker gameStateTracker)
        {
            // who needs fine grained locking??

            // we don't really need a whole list, we just take the first one
            var events = new List<UpdateAction>();

            var timeLeft = 1.0;
            while (timeLeft > 0)
            {
                if (gameState.GameBall.OwnerOrNull == null)
                {
                    foreach (var perimeterSegment in gameState.PerimeterSegments)
                    {
                        PhysicsMath2.TryPushBallLine(gameState.GameBall, perimeterSegment);
                    }
                }

                foreach (var player in gameState.players.Values)
                {
                    foreach (var perimeterSegment in gameState.PerimeterSegments)
                    {
                        PhysicsMath2.TryPushBallLine(player, perimeterSegment);
                    }
                }

                if (gameStateTracker.TryGetBallWall(out var ballwall))
                {
                    foreach (var player in gameState.players.Values)
                    {
                        PhysicsMath2.TryPushBallWall(player, ballwall);
                    }
                }

                if (gameState.GameBall.OwnerOrNull == null)
                {
                    foreach (var parameter in gameState.PerimeterSegments)
                    {
                        if (PhysicsMath2.TryBallLineCollision(gameState.GameBall.Posistion, gameState.GameBall.Velocity, parameter.Start, parameter.End, new Vector(0, 0), Constants.BallRadius, out var time))
                        {
                            events.Add(new UpdateAction
                            {
                                time = time,
                                action = () =>
                                {
                                    var force = PhysicsMath2.HitWall(gameState.GameBall.Velocity, parameter.Start, parameter.End);
                                    gameState.GameBall.Velocity = gameState.GameBall.Velocity.NewAdded(force);

                                    var directionalUnit = PhysicsMath2.DirectionalUnit(parameter.Start, parameter.End);
                                    var normal = PhysicsMath2.NormalUnit(parameter.Start, parameter.End, directionalUnit);

                                    var collisionLocation = gameState.GameBall.Posistion.NewAdded(normal.NewScaled(-Constants.BallRadius));

                                    gameState.collisions.Add(new GameState.Collision(collisionLocation, force, gameState.Frame, Guid.NewGuid()));
                                }
                            });
                        }
                    }
                }

                foreach (var player in gameState.players.Values)
                {
                    if (gameState.GameBall.OwnerOrNull == null && player.LastHadBall + Constants.ThrowTimeout < gameState.Frame)
                    {
                        if (PhysicsMath2.TryBallBallCollistion(
                            gameState.GameBall.Posistion,
                            player.PlayerFoot.Position,
                            gameState.GameBall.Velocity,
                            player.PlayerFoot.Velocity.NewAdded(player.PlayerBody.Velocity).NewAdded(player.ExternalVelocity).NewAdded(player.BoostVelocity),
                            Constants.BallRadius + Constants.PlayerRadius,
                            out var time))
                        {
                            events.Add(new UpdateAction
                            {
                                time = time,
                                action = () =>
                                {
                                    //player.PlayerBody.Velocity = player.PlayerBody.Velocity.NewScaled(.5);
                                    gameState.GameBall.OwnerOrNull = player.Id;
                                    gameState.GameBall.Posistion = player.PlayerFoot.Position;
                                    gameState.GameBall.Velocity = player.PlayerFoot.Velocity.NewAdded(player.PlayerBody.Velocity).NewAdded(player.ExternalVelocity).NewAdded(player.BoostVelocity);
                                }
                            });
                        }
                    }

                    foreach (var parameter in gameState.PerimeterSegments)
                    {
                        if (PhysicsMath2.TryBallLineCollision(
                            player.PlayerFoot.Position,
                            player.PlayerFoot.Velocity.NewAdded(player.PlayerBody.Velocity).NewAdded(player.ExternalVelocity).NewAdded(player.BoostVelocity),
                            parameter.Start,
                            parameter.End,
                            new Vector(0, 0),
                            Constants.PlayerRadius,
                            out var time))
                        {
                            events.Add(new UpdateAction
                            {
                                time = time,
                                action = () =>
                                {

                                    // add a collision 
                                    var directionalUnit = PhysicsMath2.DirectionalUnit(parameter.Start, parameter.End);
                                    var normal = PhysicsMath2.NormalUnit(parameter.Start, parameter.End, directionalUnit);

                                    var collisionLocation = player.PlayerFoot.Position.NewAdded(normal.NewScaled(-Constants.PlayerRadius));
                                    var force = normal.NewScaled(2 * player.PlayerFoot.Velocity.NewAdded(player.PlayerBody.Velocity).NewAdded(player.BoostVelocity).NewAdded(player.ExternalVelocity.NewScaled(.75)).Dot(normal));
                                    gameState.collisions.Add(new GameState.Collision(collisionLocation, force, gameState.Frame, Guid.NewGuid()));

                                    // half the foot and body velocity becomes external so you bounce a bit
                                    // some of the external for is lost so you can't loose the ball bounce off the wall and get it back
                                    player.ExternalVelocity = player.ExternalVelocity
                                        .NewAdded(normal.NewScaled(player.PlayerBody.Velocity.Dot(normal)).NewMinus())
                                        .NewAdded(normal.NewScaled(player.PlayerFoot.Velocity.Dot(normal)).NewMinus())
                                        .NewAdded(normal.NewScaled(player.BoostVelocity.Dot(normal)).NewMinus())
                                        .NewAdded(normal.NewScaled(player.ExternalVelocity.Dot(normal)).NewScaled(1.5).NewMinus());
                                    player.PlayerBody.Velocity = player.PlayerBody.Velocity.NewAdded(normal.NewScaled(player.PlayerBody.Velocity.Dot(normal)).NewMinus());
                                    player.PlayerFoot.Velocity = player.PlayerFoot.Velocity.NewAdded(normal.NewScaled(player.PlayerFoot.Velocity.Dot(normal)).NewMinus());
                                    player.BoostVelocity = player.BoostVelocity.NewAdded(normal.NewScaled(player.BoostVelocity.Dot(normal)).NewMinus());

                                }
                            });
                        }
                    }
                }

                foreach (var (p1, p2) in PlayerPairs(gameState))
                {

                    if (PhysicsMath2.TryBallBallCollistion(
                        p1.PlayerFoot.Position,
                        p2.PlayerFoot.Position,
                        p1.PlayerFoot.Velocity.NewAdded(p1.PlayerBody.Velocity).NewAdded(p1.ExternalVelocity).NewAdded(p1.BoostVelocity),
                        p2.PlayerFoot.Velocity.NewAdded(p2.PlayerBody.Velocity).NewAdded(p2.ExternalVelocity).NewAdded(p2.BoostVelocity),
                        Constants.PlayerRadius + Constants.PlayerRadius,
                        out var time))
                    {
                        events.Add(new UpdateAction
                        {
                            time = time,
                            action = () =>
                            {
                                var force = PhysicsMath2.GetCollisionForce(
                                    p1.PlayerFoot.Velocity.NewAdded(p1.PlayerBody.Velocity).NewAdded(p1.ExternalVelocity).NewAdded(p1.BoostVelocity),
                                    p2.PlayerFoot.Velocity.NewAdded(p2.PlayerBody.Velocity).NewAdded(p2.ExternalVelocity).NewAdded(p2.BoostVelocity),
                                    p1.PlayerFoot.Position,
                                    p2.PlayerFoot.Position,
                                    p1.Mass,
                                    p2.Mass);



                                var normal = p1.PlayerFoot.Position.NewAdded(p2.PlayerFoot.Position.NewMinus()).NewUnitized();

                                if ((gameState.GameBall.OwnerOrNull == p1.Id || gameState.GameBall.OwnerOrNull == p2.Id) && force.Length > Constants.BallTakeForce)
                                {
                                    force = force.NewAdded(force.NewUnitized().NewScaled(Constants.ExtraBallTakeForce));
                                }

                                p2.ExternalVelocity = p2.ExternalVelocity.NewAdded(force);
                                p1.ExternalVelocity = p1.ExternalVelocity.NewAdded(force.NewMinus());

                                //double part1, part2;

                                //if (gameState.GameBall.OwnerOrNull == p1.Id)
                                //{
                                //    //force = force.NewAdded(force.NewUnitized().NewScaled(Constants.ExtraBallTakeForce));
                                //    part1 = 1;
                                //    part2 = -1;
                                //}
                                //else if (gameState.GameBall.OwnerOrNull == p2.Id)
                                //{
                                //    //force = force.NewAdded(force.NewUnitized().NewScaled(Constants.ExtraBallTakeForce));
                                //    part1 = -1;
                                //    part2 = 1;
                                //}
                                //else
                                //{
                                //var velocityVector1 = p1.PlayerFoot.Velocity.NewAdded(p1.PlayerBody.Velocity).NewAdded(p1.ExternalVelocity).NewAdded(p1.BoostVelocity);
                                //var velocityVector2 = p2.PlayerFoot.Velocity.NewAdded(p2.PlayerBody.Velocity).NewAdded(p2.ExternalVelocity).NewAdded(p2.BoostVelocity);

                                //var denom = velocityVector1.NewMinus().NewAdded(velocityVector2).Dot(normal);

                                //var vv1dot = velocityVector1.Dot(normal.NewMinus());
                                //var vv2dot = velocityVector2.Dot(normal);

                                //part1 = Math.Min(1, Math.Max(-1, (vv2dot / denom) - (vv1dot / denom)));
                                //part2 = -part1;
                                //}


                                //var forceForP1 = force.NewScaled(-(1 + part1) / p1.Mass);
                                //if (p1.PlayerBody.Velocity.Dot(forceForP1) < 0)
                                //{
                                //    var remove = forceForP1.NewUnitized().NewScaled(p1.PlayerBody.Velocity.Dot(forceForP1.NewMinus().NewUnitized()));

                                //    if (remove.Length >= forceForP1.Length)
                                //    {
                                //        remove = remove.NewUnitized().NewScaled(forceForP1.Length);
                                //    }

                                //    p1.PlayerBody.Velocity = p1.PlayerBody.Velocity.NewAdded(remove);

                                //    forceForP1 = forceForP1.NewUnitized().NewScaled(forceForP1.Length - remove.Length);
                                //}
                                //p1.ExternalVelocity = p1.ExternalVelocity.NewAdded(forceForP1);


                                //var forceForP2 = force.NewScaled((1 + part2) / p2.Mass);
                                //if (p2.PlayerBody.Velocity.Dot(forceForP2) < 0)
                                //{
                                //    var remove = forceForP2.NewUnitized().NewScaled(p2.PlayerBody.Velocity.Dot(forceForP2.NewMinus().NewUnitized()));

                                //    if (remove.Length >= forceForP2.Length)
                                //    {
                                //        remove = remove.NewUnitized().NewScaled(forceForP2.Length);
                                //    }

                                //    p2.PlayerBody.Velocity = p2.PlayerBody.Velocity.NewAdded(remove);

                                //    forceForP2 = forceForP2.NewUnitized().NewScaled(forceForP2.Length - remove.Length);
                                //}
                                //p2.ExternalVelocity = p2.ExternalVelocity.NewAdded(forceForP2);

                                if (gameState.GameBall.OwnerOrNull == p1.Id && force.Length > Constants.BallTakeForce)
                                {

                                    gameState.GameBall.OwnerOrNull = null;
                                    //gameState.GameBall.OwnerOrNull = p2.Id;
                                    //gameState.GameBall.Posistion = p2.PlayerFoot.Position;
                                    //gameState.GameBall.Velocity = p2.ExternalVelocity.NewAdded(p2.PlayerBody.Velocity).NewAdded(p2.PlayerFoot.Velocity).NewAdded(p2.BoostVelocity);

                                    //p1.PlayerBody.Velocity = p1.PlayerBody.Velocity.NewScaled(.5);
                                }
                                else if (gameState.GameBall.OwnerOrNull == p2.Id && force.Length > Constants.BallTakeForce)
                                {
                                    gameState.GameBall.OwnerOrNull = null;
                                    //gameState.GameBall.OwnerOrNull = p1.Id;
                                    //gameState.GameBall.Posistion = p1.PlayerFoot.Position;
                                    //gameState.GameBall.Velocity = p1.ExternalVelocity.NewAdded(p1.PlayerBody.Velocity).NewAdded(p1.PlayerFoot.Velocity).NewAdded(p1.BoostVelocity);

                                    //p2.PlayerBody.Velocity = p2.PlayerBody.Velocity.NewScaled(.5);
                                }

                                var collisionLocation = p2.PlayerFoot.Position.NewAdded(normal.NewScaled(Constants.PlayerRadius));

                                // add a collision 
                                gameState.collisions.Add(new GameState.Collision(collisionLocation, force.NewScaled(2), gameState.Frame, Guid.NewGuid()));
                            }
                        });
                    }
                }

                if (gameStateTracker.TryGetBallWall(out var ballWall))
                {
                    foreach (var player in gameState.players.Values)
                    {
                        if (PhysicsMath2.TryBallBallCollistion(
                            player.PlayerFoot.Position,
                            new Vector(ballWall.x, ballWall.y),
                            player.PlayerFoot.Velocity.NewAdded(player.PlayerBody.Velocity).NewAdded(player.ExternalVelocity).NewAdded(player.BoostVelocity),
                            new Vector(0, 0),
                            Constants.PlayerRadius + ballWall.radius,
                            out var time))
                        {
                            events.Add(new UpdateAction
                            {
                                time = time,
                                action = () =>
                                {
                                    // from player to ball wall
                                    var normal = new Vector(ballWall.x, ballwall.y).NewAdded(player.PlayerFoot.Position.NewMinus()).NewUnitized();

                                    var collisionLocation = player.PlayerFoot.Position.NewAdded(normal.NewScaled(Constants.PlayerRadius));
                                    var force = normal.NewScaled(2 * player.PlayerFoot.Velocity.NewAdded(player.PlayerBody.Velocity).NewAdded(player.BoostVelocity).NewAdded(player.ExternalVelocity.NewScaled(.75)).Dot(normal));
                                    gameState.collisions.Add(new GameState.Collision(collisionLocation, force, gameState.Frame, Guid.NewGuid()));

                                    // half the foot and body velocity becomes external so you bounce a bit
                                    // some of the external for is lost so you can't loose the ball bounce off the wall and get it back
                                    player.ExternalVelocity = player.ExternalVelocity
                                        .NewAdded(normal.NewScaled(player.PlayerBody.Velocity.Dot(normal)).NewMinus())
                                        .NewAdded(normal.NewScaled(player.PlayerFoot.Velocity.Dot(normal)).NewMinus())
                                        .NewAdded(normal.NewScaled(player.BoostVelocity.Dot(normal)).NewMinus())
                                        .NewAdded(normal.NewScaled(player.ExternalVelocity.Dot(normal)).NewScaled(1.5).NewMinus());
                                    player.PlayerBody.Velocity = player.PlayerBody.Velocity.NewAdded(normal.NewScaled(player.PlayerBody.Velocity.Dot(normal)).NewMinus());
                                    player.PlayerFoot.Velocity = player.PlayerFoot.Velocity.NewAdded(normal.NewScaled(player.PlayerFoot.Velocity.Dot(normal)).NewMinus());
                                    player.BoostVelocity = player.BoostVelocity.NewAdded(normal.NewScaled(player.BoostVelocity.Dot(normal)).NewMinus());
                                }
                            });
                        }
                    }
                }

                if (gameStateTracker.CanScore())
                {

                    foreach (var goal in new[] { gameState.LeftGoal, gameState.RightGoal })
                    {
                        if (PhysicsMath2.TryBallBallCollistion(
                            gameState.GameBall.Posistion,
                            goal.Posistion,
                            gameState.GameBall.Velocity,
                            new Vector(0, 0),
                            Constants.BallRadius + Constants.goalLen,
                            out var time))
                        {
                            events.Add(new UpdateAction
                            {
                                time = time,
                                action = () =>
                                {
                                    if (gameStateTracker.CanScore())
                                    {
                                        var normal = gameState.GameBall.Posistion.NewAdded(goal.Posistion.NewScaled(-1)).NewUnitized();

                                        var position = goal.Posistion.NewAdded(normal.NewScaled(Constants.goalLen));

                                        GameStateUpdater.Handle(gameState, new GameState.GoalScored(position, goal.LeftGoal, new Vector(normal.y,-normal.x), gameState.Frame, Guid.NewGuid()));

                                        gameStateTracker.Scored();
                                    }
                                }
                            });
                        }
                    }
                }

                events = events
                    .Where(x => x.time > 0)
                    .Where(x => x.time <= timeLeft)
                    .ToList();

                if (events.Any())
                {
                    var toEnact = events.OrderBy(x => x.time).First();

                    MoveStuff(gameState, toEnact.time);

                    toEnact.action();

                    timeLeft -= toEnact.time;
                }
                else
                {
                    MoveStuff(gameState, timeLeft);

                    timeLeft -= timeLeft;
                }
                events = new List<UpdateAction>();

            }
        }

        private static void MoveStuff(GameState gameState, double time)
        {
            // move everyone
            foreach (var player in gameState.players.Values)
            {
                player.PlayerFoot.Position = player.PlayerFoot.Position
                    .NewAdded(player.PlayerFoot.Velocity.NewScaled(time))
                    .NewAdded(player.PlayerBody.Velocity.NewScaled(time))
                    .NewAdded(player.ExternalVelocity.NewScaled(time))
                    .NewAdded(player.BoostVelocity.NewScaled(time));

                player.PlayerBody.Position = player.PlayerBody.Position
                    .NewAdded(player.PlayerBody.Velocity.NewScaled(time))
                    .NewAdded(player.ExternalVelocity.NewScaled(time))
                    .NewAdded(player.BoostVelocity.NewScaled(time));
            }
            // move ball
            if (gameState.GameBall.OwnerOrNull != null)
            {
                var carrier = gameState.players[gameState.GameBall.OwnerOrNull.Value];
                gameState.GameBall.Posistion = carrier.PlayerFoot.Position;
                gameState.GameBall.Velocity = carrier.PlayerFoot.Velocity.NewAdded(carrier.PlayerBody.Velocity).NewAdded(carrier.ExternalVelocity).NewAdded(carrier.BoostVelocity);
            }
            else
            {
                gameState.GameBall.Posistion = gameState.GameBall.Posistion
                    .NewAdded(gameState.GameBall.Velocity.NewScaled(time));
            }
        }

    }
}
