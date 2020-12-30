using Common;
using Physics2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Common.Game;

namespace physics2
{
    public class PhysicsEngine
    {
        private Ball ball;
        private readonly List<PhysicsObjectWithFixedLine> parameters = new List<PhysicsObjectWithFixedLine>();
        private readonly List<Player> players = new List<Player>();
        private readonly List<(Ball, IGoalManager)> goals = new List<(Ball, IGoalManager)>();

        public readonly List<PhysicsObject> items = new List<PhysicsObject>();

        private readonly GameStateTracker gameStateTracker;

        //
        public PhysicsEngine(GameStateTracker gameStateTracker)
        {
            this.gameStateTracker = gameStateTracker;
        }

        public IEnumerable<(Player, Player)> PlayerPairs()
        {
            foreach (var p1 in players)
            {
                foreach (var p2 in players.Skip(players.IndexOf(p1) + 1))
                {
                    yield return (p1, p2);
                }
            }
        }

        // try to move forward by 1 time;
        public Collision[] Simulate(int simulationFrame)
        {
            // who needs fine grained locking??
            lock (players)
            {
                lock (goals)
                {
                    lock (parameters)
                    {

                        var collisions = new List<Collision>();

                        var events = new List<IEvent>();

                        var timeLeft = 1.0;
                        while (timeLeft > 0)
                        {

                            if (ball.OwnerOrNull == null)
                            {
                                foreach (var parameter in parameters)
                                {
                                    PhysicsMath.TryPushBallLine(ball, ball, ball.GetCircle(), parameter.GetLine());
                                }
                            }

                            foreach (var player in players)
                            {
                                foreach (var parameter in parameters)
                                {
                                    PhysicsMath.TryPushBallLine(player, player.Body.Outer, new Circle(player.Padding), parameter.GetLine());
                                }
                            }

                            if (gameStateTracker.TryGetBallWall(out var ballwall))
                            {
                                foreach (var player in players)
                                {
                                    PhysicsMath.TryPushBallWall(player, player.Body.Outer, new Circle(player.Padding), ballwall.x, ballwall.y, ballwall.radius);
                                }
                            }

                            if (ball.OwnerOrNull == null)
                            {
                                foreach (var parameter in parameters)
                                {
                                    if (PhysicsMath.TryNextCollisionBallLine(ball, parameter, ball, parameter, ball.GetCircle(), parameter.GetLine(), timeLeft, out var @event))
                                    {
                                        //if (ball.OwnerOrNull != null)
                                        //{
                                        //    events.Add(new DropBallWrapper(@event, ball, simulationFrame));
                                        //}
                                        //else
                                        //{
                                        events.Add(@event);
                                        //}
                                    }
                                }
                            }

                            foreach (var player in players)
                            {
                                if (ball.OwnerOrNull == null && player.LastHadBall + Constants.ThrowTimeout < simulationFrame)
                                {
                                    if (PhysicsMath.TryPickUpBall(
                                        ball,
                                        player,
                                        player.X,
                                        player.Y,
                                        player.Vx,
                                        player.Vy,
                                        ball.GetCircle(),
                                        new Circle(player.Padding),
                                        timeLeft,
                                        out var @event))
                                    {
                                        events.Add(@event);
                                    }
                                }


                                foreach (var parameter in parameters)
                                {
                                    if (PhysicsMath.TryNextCollisionBallLine(
                                        player,
                                        parameter,
                                        player.Body.Outer,
                                        parameter,
                                        new Circle(player.Padding),
                                        parameter.GetLine(),
                                        timeLeft,
                                        out var @event2))
                                    {
                                        events.Add(@event2);
                                    }
                                }
                            }

                            foreach (var (p1, p2) in PlayerPairs())
                            {
                                if (PhysicsMath.TryCollisionBall2(
                                    p1,
                                    p2,
                                    p1.Body.Outer.externalForce,
                                    p2.Body.Outer.externalForce,
                                    new Circle(p1.Padding),
                                    new Circle(p2.Padding),
                                    timeLeft,
                                    ball.OwnerOrNull == p1 ? PhysicsMath.BallState.obj1 : ball.OwnerOrNull == p2 ? PhysicsMath.BallState.obj2 : PhysicsMath.BallState.neither,
                                    out var @event))
                                {
                                    if (ball.OwnerOrNull == p1)
                                    {
                                        events.Add(new TakeBallWrapper(@event, ball, p2));
                                    }
                                    else if (ball.OwnerOrNull == p2)
                                    {
                                        events.Add(new TakeBallWrapper(@event, ball, p1));
                                    }
                                    else
                                    {
                                        events.Add(@event);
                                    }
                                }
                            }

                            if (gameStateTracker.TryGetBallWall(out var ballWall))
                            {
                                var ballWallPhysicObject = new PhysicsObject(1, ballWall.x, ballWall.y, false);
                                foreach (var player in players)
                                {
                                    if (PhysicsMath.TryCollisionBall(
                                        player,
                                        ballWallPhysicObject,
                                        player.Body.Outer.externalForce,
                                        ballWallPhysicObject,
                                        new Circle(player.Padding),
                                        new Circle(ballWall.radius),
                                        timeLeft,
                                        out var @event))
                                    {
                                        events.Add(@event);
                                    }
                                }
                            }

                            foreach (var goal in goals)
                            {
                                if (goal.Item2.IsEnabled() &&
                                    PhysicsMath.TryCollisionBall(
                                        ball,
                                        goal.Item1,
                                        ball,
                                        goal.Item1,
                                        ball.GetCircle(),
                                        goal.Item1.GetCircle(), timeLeft, out var @event))
                                {
                                    // man I hate goals
                                    // we need to replace the event
                                    if (@event.res.IsIt(out var collision))
                                    {
                                        collision.IsGoal = true;

                                        var radius = goal.Item1.GetCircle().Radius;

                                        var realPos = goal.Item1.Position.NewAdded(new Vector(collision.X - goal.Item1.X, collision.Y - goal.Item1.Y).NewUnitized().NewScaled(radius));

                                        collision.X = realPos.x;
                                        collision.Y = realPos.y;

                                        var dx = goal.Item1.X - collision.X;
                                        var dy = goal.Item1.Y - collision.Y;

                                        collision.Fx = -dy;
                                        collision.Fy = dx;

                                        events.Add(goal.Item2.GetGoalEvent(@event.Time, collision));
                                    }
                                }
                            }

                            //if (events.Any(x => x.Time <= 0))
                            //{
                            //    var db = 0;
                            //}

                            events = events
                                .Where(x => x.Time > 0)
                                .ToList();

                            if (events.Any())
                            {
                                var toEnact = events.OrderBy(x => x.Time).First();

                                // move everyone
                                foreach (var physicObject in PhysicsObject())
                                {
                                    physicObject.Update(toEnact.Time, timeLeft);
                                }

                                if (toEnact.Enact().IsIt(out var collision))
                                {
                                    collisions.Add(collision);
                                }

                                timeLeft -= toEnact.Time;
                            }
                            else
                            {
                                foreach (var physicObject in PhysicsObject())
                                {
                                    physicObject.Update(timeLeft, timeLeft);
                                }

                                timeLeft -= timeLeft;
                            }
                            events = new List<IEvent>();

                        }
                        return collisions.ToArray();
                    }
                }
            }
        }

        public void SetBall(Ball physicsObject)
        {
            ball = physicsObject;
        }

        public void AddWall(PhysicsObjectWithFixedLine physicsObject)
        {
            lock (parameters)
            {
                parameters.Add(physicsObject);
            }
        }

        public void AddPlayer(Player physicsObject)
        {
            lock (players)
            {
                players.Add(physicsObject);
            }
        }


        public void RemovePlayer(Player physicsObject)
        {
            lock (players)
            {
                players.Remove(physicsObject);
            }
        }

        public void AddGoal(Ball physicsObject, IGoalManager goalManger)
        {
            lock (goals)
            {
                goals.Add((physicsObject, goalManger));
            }
        }

        private IEnumerable<IUpdatePosition> PhysicsObject()
        {
            yield return ball;
            foreach (var parameter in parameters)
            {
                yield return parameter;
            }
            foreach (var player in players)
            {
                yield return player;
            }
            foreach (var player in players)
            {
                yield return player.Body;
            }
            foreach (var player in players)
            {
                yield return player.Body.Outer;
            }
            foreach (var player in players)
            {
                yield return player.Body.Outer.externalForce;
            }
            foreach (var goal in goals)
            {
                yield return goal.Item1;
            }
        }
    }

    internal interface IUpdatePosition
    {
        void Update(double step, double timeLeft);
    }

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
        public static Collision[] Simulate(this GameState gameState, GameStateTracker gameStateTracker)
        {
            // who needs fine grained locking??


            var collisions = new List<Collision>();

            var events = new List<UpdateAction>();

            var timeLeft = 1.0;
            while (timeLeft > 0)
            {
                if (gameState.ball.ownerOrNull == null)
                {
                    foreach (var perimeterSegment in gameState.perimeterSegments)
                    {
                        PhysicsMath2.TryPushBallLine(gameState.ball, perimeterSegment);
                    }
                }

                foreach (var player in gameState.players.Values)
                {
                    foreach (var perimeterSegment in gameState.perimeterSegments)
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

                if (gameState.ball.ownerOrNull == null)
                {
                    foreach (var parameter in gameState.perimeterSegments)
                    {
                        if (PhysicsMath2.TryBallLineCollision(gameState.ball.posistion, gameState.ball.velocity, parameter.start, parameter.end, new Vector(0, 0), Constants.BallRadius, out var time))
                        {
                            events.Add(new UpdateAction
                            {
                                time = time,
                                action = () =>
                                {
                                    gameState.ball.velocity = gameState.ball.velocity.NewAdded(PhysicsMath2.HitWall(gameState.ball.velocity, parameter.start, parameter.end));
                                }
                            });
                        }
                    }
                }

                foreach (var player in gameState.players.Values)
                {
                    if (gameState.ball.ownerOrNull == null && player.lastHadBall + Constants.ThrowTimeout < gameState.frame)
                    {
                        if (PhysicsMath2.TryBallBallCollistion(
                            gameState.ball.posistion,
                            player.foot.position,
                            gameState.ball.velocity,
                            player.foot.velocity.NewAdded(player.body.velocity).NewAdded(player.externalVelocity),
                            Constants.BallRadius + Constants.PlayerRadius,
                            out var time))
                        {
                            events.Add(new UpdateAction
                            {
                                time = time,
                                action = () =>
                                {
                                    gameState.ball.ownerOrNull = player;
                                    gameState.ball.posistion = player.foot.position;
                                    gameState.ball.velocity = player.foot.velocity.NewAdded(player.body.velocity).NewAdded(player.externalVelocity);
                                }
                            });
                        }
                    }

                    foreach (var parameter in gameState.perimeterSegments)
                    {
                        if (PhysicsMath2.TryBallLineCollision(
                            player.foot.position,
                            player.foot.velocity.NewAdded(player.body.velocity).NewAdded(player.externalVelocity),
                            parameter.start,
                            parameter.end,
                            new Vector(0, 0),
                            Constants.BallRadius,
                            out var time))
                        {
                            events.Add(new UpdateAction
                            {
                                time = time,
                                action = () =>
                                {

                                    // add a collision 
                                    var directionalUnit = PhysicsMath2.DirectionalUnit(parameter.start, parameter.end);
                                    var normal = PhysicsMath2.NormalUnit(parameter.start, parameter.end, directionalUnit);

                                    var collisionLocation = player.foot.position.NewAdded(normal.NewScaled(-Constants.PlayerRadius));
                                    var force = normal.NewScaled(2 * player.foot.velocity.NewAdded(player.body.velocity).NewAdded(player.externalVelocity).Dot(normal));
                                    gameState.collisions.Add(new GameState.Collision(collisionLocation, force));
                                    
                                    // half the foot and body velocity becomes external so you bounce a bit
                                    player.externalVelocity = player.externalVelocity.NewAdded( PhysicsMath2.HitWall(player.foot.velocity.NewScaled(.5).NewAdded(player.body.velocity.NewScaled(.5)).NewAdded(player.externalVelocity), parameter.start, parameter.end));
                                    player.body.velocity = player.body.velocity.NewAdded(PhysicsMath2.HitWall(player.body.velocity.NewScaled(.5), parameter.start, parameter.end));
                                    player.foot.velocity = player.foot.velocity.NewAdded(PhysicsMath2.HitWall(player.foot.velocity.NewScaled(.5), parameter.start, parameter.end));

                                    // add a collision 
                                    gameState.collisions.Add(new GameState.Collision(collisionLocation, force));
                                }
                            });
                        }
                    }
                }

                foreach (var (p1, p2) in PlayerPairs(gameState))
                {
                    if (PhysicsMath2.TryBallBallCollistion(
                        p1.foot.position,
                        p2.foot.position,
                        p1.foot.velocity.NewAdded(p1.body.velocity).NewAdded(p1.externalVelocity),
                        p2.foot.velocity.NewAdded(p2.body.velocity).NewAdded(p2.externalVelocity),
                        Constants.PlayerRadius + Constants.PlayerRadius,
                        out var time))
                    {
                        events.Add(new UpdateAction
                        {
                            time = time,
                            action = () =>
                            {
                                var force = PhysicsMath2.GetCollisionForce(
                                    p1.foot.velocity.NewAdded(p1.body.velocity).NewAdded(p1.externalVelocity),
                                    p2.foot.velocity.NewAdded(p2.body.velocity).NewAdded(p2.externalVelocity),
                                    p1.foot.position,
                                    p2.foot.position,
                                    p1.mass,
                                    p2.mass);

                                force = force.NewAdded(force.NewUnitized().NewScaled(Constants.MinPlayerCollisionForce));

                                var normal = p1.foot.position.NewAdded(p2.foot.position.NewScaled(-1)).NewUnitized();

                                double part1, part2;

                                if (gameState.ball.ownerOrNull == p1)
                                {
                                    force = force.NewAdded(force.NewUnitized().NewScaled(Constants.ExtraBallTakeForce));
                                    part1 = 1;
                                    part2 = -1;
                                }
                                else if (gameState.ball.ownerOrNull == p2)
                                {
                                    force = force.NewAdded(force.NewUnitized().NewScaled(Constants.ExtraBallTakeForce));
                                    part1 = -1;
                                    part2 = 1;
                                }
                                else
                                {
                                    var velocityVector1 = p1.foot.velocity.NewAdded(p1.body.velocity).NewAdded(p1.externalVelocity);
                                    var velocityVector2 = p2.foot.velocity.NewAdded(p2.body.velocity).NewAdded(p2.externalVelocity);

                                    var denom = velocityVector1.NewMinus().NewAdded(velocityVector2).Dot(normal);

                                    var vv1dot = velocityVector1.Dot(normal.NewMinus());
                                    var vv2dot = velocityVector2.Dot(normal);

                                    part1 = Math.Min(1, Math.Max(-1, (vv2dot / denom) - (vv1dot / denom)));
                                    part2 = -part1;
                                }

                                p1.externalVelocity = p1.externalVelocity.NewAdded(force.NewScaled(-(1 + part1) / p1.mass));
                                p2.externalVelocity = p2.externalVelocity.NewAdded(force.NewScaled((1 + part2) / p2.mass));

                                if (gameState.ball.ownerOrNull == p1)
                                {
                                    gameState.ball.ownerOrNull = p2;
                                    gameState.ball.posistion = p2.foot.position;
                                    gameState.ball.velocity = p2.externalVelocity.NewAdded(p2.body.velocity).NewAdded(p2.foot.velocity);
                                }
                                else if (gameState.ball.ownerOrNull == p2)
                                {
                                    gameState.ball.ownerOrNull = p1;
                                    gameState.ball.posistion = p1.foot.position;
                                    gameState.ball.velocity = p1.externalVelocity.NewAdded(p1.body.velocity).NewAdded(p1.foot.velocity);
                                }

                                var collisionLocation = p1.foot.position.NewAdded(normal.NewScaled(Constants.PlayerRadius));

                                // add a collision 
                                gameState.collisions.Add(new GameState.Collision(collisionLocation, force.NewScaled(2)));
                            }
                        });
                    }
                }

                if (gameStateTracker.TryGetBallWall(out var ballWall))
                {
                    foreach (var player in gameState.players.Values)
                    {
                        if (PhysicsMath2.TryBallBallCollistion(
                            player.foot.position,
                            new Vector(ballWall.x, ballWall.y),
                            player.foot.velocity.NewAdded(player.body.velocity).NewAdded(player.externalVelocity),
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
                                    var normal = new Vector(ballWall.x, ballwall.y).NewAdded(player.foot.position.NewMinus()).NewUnitized();

                                    // add a collision 
                                    var collisionLocation = player.foot.position.NewAdded(normal.NewScaled(Constants.PlayerRadius));
                                    var force = normal.NewScaled(2 * player.foot.velocity.NewAdded(player.body.velocity).NewAdded(player.externalVelocity).Dot(normal));
                                    gameState.collisions.Add(new GameState.Collision(collisionLocation, force));

                                    // half the foot and body velocity becomes external so you bounce a bit
                                    player.externalVelocity = player.externalVelocity.NewAdded(normal.NewScaled(-1 * player.externalVelocity.NewScaled(2).NewAdded(player.foot.velocity).NewAdded(player.body.velocity).Dot(normal)));
                                    player.foot.velocity = player.foot.velocity.NewAdded(normal.NewScaled(-1 * player.foot.velocity.Dot(normal)));
                                    player.body.velocity = player.body.velocity.NewAdded(normal.NewScaled(-1 * player.body.velocity.Dot(normal)));
                                }
                            });
                        }
                    }
                }

                if (gameStateTracker.CanScore())
                {

                    foreach (var goal in new[] { gameState.leftGoal, gameState.rightGoal })
                    {
                        if (PhysicsMath2.TryBallBallCollistion(
                            gameState.ball.posistion,
                            goal.posistion,
                            gameState.ball.velocity,
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
                                        var normal = gameState.ball.posistion.NewAdded(goal.posistion.NewScaled(-1)).NewUnitized();

                                        var position = goal.posistion.NewAdded(normal.NewScaled(Constants.goalLen));

                                        GameStateUpdater.Handle(gameState, new GameState.GoalScored(position, goal.leftGoal));

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
            return collisions.ToArray();

        }

        private static void MoveStuff(GameState gameState, double time)
        {
            // move everyone
            foreach (var player in gameState.players.Values)
            {
                player.foot.position = player.foot.position
                    .NewAdded(player.foot.velocity.NewScaled(time))
                    .NewAdded(player.body.velocity.NewScaled(time))
                    .NewAdded(player.externalVelocity.NewScaled(time));

                player.body.position = player.body.position
                    .NewAdded(player.body.velocity.NewScaled(time))
                    .NewAdded(player.externalVelocity.NewScaled(time));
            }
            // move ball
            if (gameState.ball.ownerOrNull != null)
            {
                gameState.ball.posistion = gameState.ball.ownerOrNull.foot.position;
                gameState.ball.velocity = gameState.ball.ownerOrNull.foot.velocity;
            }
            else
            {
                gameState.ball.posistion = gameState.ball.posistion
                    .NewAdded(gameState.ball.velocity.NewScaled(time));
            }
        }

    }
}
