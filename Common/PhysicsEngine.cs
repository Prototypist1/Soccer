using Common;
using Physics2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace physics2
{
    public class PhysicsEngine
    {
        private Ball ball;
        private readonly List<PhysicsObjectWithFixedLine> parameters = new List<PhysicsObjectWithFixedLine>();
        private readonly List<Player> players = new List<Player>();
        private readonly List<(Ball, IGoalManager)> goals = new List<(Ball, IGoalManager)>();

        public readonly List<PhysicsObject> items = new List<PhysicsObject>();

        public IEnumerable<(Player, Player)> PlayerPairs()
        {
            foreach (var p1 in players)
            {
                foreach (var p2 in players.Skip(players.IndexOf(p1)+1))
                {
                    yield return (p1, p2);
                }
            }
        }

        // try to move forward by 1 time;
        public Collision[] Simulate(int simulationFrame)
        {
            var collisions = new List<Collision>();

            var events = new List<IEvent>();

            var timeLeft = 1.0;
            while (timeLeft > 0)
            {

                foreach (var parameter in parameters)
                {
                    if (PhysicsMath.TryNextCollisionBallLine(ball, parameter, ball.GetCircle(), parameter.GetLine(), timeLeft, out var @event))
                    {
                        if (ball.OwnerOrNull != null)
                        {
                            events.Add(new DropBallWrapper(@event, ball, simulationFrame));
                        }
                        else
                        {
                            events.Add(@event);
                        }
                    }
                }
                foreach (var player in players)
                {
                    //{
                    //foreach (var partical in player.line)
                    //{
                    //    if (PhysicsMath.TryCollisionPointCloudParticle(ball, player, partical.X, partical.Y, partical.Vx(timeLeft), partical.Vy(timeLeft), ball.GetCircle(), new Circle(player.Padding), timeLeft, out var @event))
                    //    {
                    //        events.Add(@event);
                    //    }
                    //}
                    //}

                    //if (PhysicsMath.TryCollisionBallLine2(
                    //    ball,
                    //    player,
                    //    ball.GetCircle(),
                    //    player.GetLength(),
                    //    timeLeft,
                    //    new Vector(player.start.X, player.start.Y),
                    //    new Vector(player.start.Tx, player.start.Ty).NewAdded(new Vector(player.start.X, player.start.Y).NewMinus()).NewScaled(1 / timeLeft),
                    //    out var @event
                    //    ))
                    //{
                    //    events.Add(@event);
                    //}

                    //var parallelVector = player.GetParallelVector();
                    //for (var i = -1.0; i <= 1.0; i += 0.05)
                    //{
                    if (ball.OwnerOrNull == null && player.LastHadBall + Constants.ThrowTimeout < simulationFrame)
                    {
                        if (PhysicsMath.TryPickUpBall(
                            ball,
                            player,
                            player.X,      //(parallelVector.x * i) +
                            player.Y,      //(parallelVector.y * i) +
                            player.Vx,     // + ((player.start.Tx - player.start.X) * i * (1 / timeLeft)),
                            player.Vy,     // + ((player.start.Ty - player.start.Y) * i * (1 / timeLeft)),
                            ball.GetCircle(),
                            new Circle(player.Padding),
                            timeLeft,
                            out var @event))
                        {
                            events.Add(@event);
                        }
                    }
                    //}

                    //{
                    //    var start = player.start;
                    //    if (PhysicsMath.TryCollisionBall(ball, player, start.X, start.Y, start.Vx(timeLeft), start.Vy(timeLeft), ball.GetCircle(), new Circle(player.Padding), timeLeft, out var @event))
                    //    {
                    //        events.Add(@event);
                    //    }
                    //}
                    //{
                    //    var end = player.end;
                    //    if (PhysicsMath.TryCollisionBall(ball, player, end.X, end.Y, end.Vx(timeLeft), end.Vy(timeLeft), ball.GetCircle(), new Circle(player.Padding), timeLeft, out var @event))
                    //    {
                    //        events.Add(@event);
                    //    }
                    //}
                }

                foreach (var (p1, p2) in PlayerPairs())
                {
                    if (PhysicsMath.TryCollisionBall(
                        p1,
                        p2,
                        p1.Body.Outer,
                        p2.Body.Outer,
                        new Circle(p1.Padding),
                        new Circle(p2.Padding),
                        timeLeft,
                        out var @event))
                    {
                        if (ball.OwnerOrNull == p1)
                        {
                            events.Add(new TakeBallWrapper( @event,ball,p2));
                        }
                        else if (ball.OwnerOrNull == p2)
                        {
                            events.Add(new TakeBallWrapper(@event, ball, p1));
                        }
                        else { 
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

        public void SetBall(Ball physicsObject)
        {
            ball = physicsObject;
        }

        public void AddWall(PhysicsObjectWithFixedLine physicsObject)
        {
            parameters.Add(physicsObject);
        }

        public void AddPlayer(Player physicsObject)
        {
            players.Add(physicsObject);
        }


        public void RemovePlayer(Player physicsObject)
        {
            players.Remove(physicsObject);
        }

        public void AddGoal(Ball physicsObject, IGoalManager goalManger)
        {
            goals.Add((physicsObject, goalManger));
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
