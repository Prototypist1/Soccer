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
}
