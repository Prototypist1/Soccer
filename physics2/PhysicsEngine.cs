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

        // try to move forward by 1 time;
        public Collision[] Simulate()
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
                        events.Add(@event);
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

                    if (PhysicsMath.TryCollisionBallLine2(
                        ball,
                        player,
                        ball.GetCircle(),
                        player.GetLength(),
                        timeLeft,
                        new Vector(player.start.X, player.start.Y),
                        new Vector(player.start.Tx, player.start.Ty).NewAdded(new Vector(player.start.X, player.start.Y).NewMinus()).NewScaled(1 / timeLeft),
                        out var @event
                        ))
                    {
                        events.Add(@event);
                    }

                    //for (var i = -1.0; i <= 1.0; i += .10)
                    //{
                    //    if (PhysicsMath.TryCollisionBall(
                    //        ball,
                    //        player,
                    //        (player.start.X * i) + player.X,
                    //        (player.start.Y * i) + player.Y,
                    //        player.Vx + ((player.start.Tx - player.start.X) * i * (1 / timeLeft)),
                    //        player.Vy + ((player.start.Ty - player.start.Y) * i * (1 / timeLeft)),
                    //        ball.GetCircle(),
                    //        new Circle(100),
                    //        timeLeft,
                    //        out var @event))
                    //    {
                    //        events.Add(@event);
                    //    }
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
                foreach (var goal in goals)
                {
                    if (goal.Item2.IsEnabled() && PhysicsMath.TryCollisionBall(ball, goal.Item1, goal.Item1.X, goal.Item1.Y, goal.Item1.Vx, goal.Item1.Vy, ball.GetCircle(), goal.Item1.GetCircle(), timeLeft, out var @event))
                    {
                        // man I hate goals
                        // we need to replace the event
                        events.Add(goal.Item2.GetGoalEvent(@event.Time));
                    }
                }

                events = events.Where(x => x.Time > 0).ToList();

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
