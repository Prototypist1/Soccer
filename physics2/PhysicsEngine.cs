using Physics2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace physics2
{
    public class PhysicsEngine
    {
        private PhysicsObjectWithCircle ball;
        private readonly List<PhysicsObjectWithLine> parameters = new List<PhysicsObjectWithLine>();
        private readonly List<PhysicsObjectWithLine> players = new List<PhysicsObjectWithLine>();
        private readonly List<(PhysicsObjectWithCircle,  IGoalManager)> goals = new List<(PhysicsObjectWithCircle, IGoalManager)>();

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
                    if (PhysicsMath.TryNextCollisionBallLine(ball, player, ball.GetCircle(), player.GetLine(), timeLeft, out var @event))
                    {
                        events.Add(@event);
                    }
                    if (PhysicsMath.TryCollisionBall(ball, player.GetStart(), ball.GetCircle(), new Circle(0), timeLeft, out  @event))
                    {
                        events.Add(@event);
                    }
                    if (PhysicsMath.TryCollisionBall(ball, player.GetEnd(), ball.GetCircle(), new Circle(0), timeLeft, out  @event))
                    {
                        events.Add(@event);
                    }
                }
                foreach (var goal in goals)
                {
                    if (goal.Item2.IsEnabled() && PhysicsMath.TryCollisionBall(ball, goal.Item1, ball.GetCircle(), goal.Item1.GetCircle(), timeLeft, out var @event))
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
                        physicObject.X = physicObject.X + (physicObject.Vx * toEnact.Time);
                        physicObject.Y = physicObject.Y + (physicObject.Vy * toEnact.Time);
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
                        physicObject.X = physicObject.X + (physicObject.Vx * timeLeft);
                        physicObject.Y = physicObject.Y + (physicObject.Vy * timeLeft);
                    }

                    timeLeft -= timeLeft;
                }
                events = new List<IEvent>();

            }
            return collisions.ToArray();
        }

        public void SetBall(PhysicsObjectWithCircle physicsObject) {
            ball = physicsObject;
        }

        public void AddWall(PhysicsObjectWithLine physicsObject) {
            parameters.Add(physicsObject);
        }

        public void AddPlayer(PhysicsObjectWithLine physicsObject)
        {
            players.Add(physicsObject);
        }

        public void AddGoal(PhysicsObjectWithCircle physicsObject, IGoalManager goalManger)
        {
            goals.Add((physicsObject, goalManger));
        }

        private IEnumerable<PhysicsObject> PhysicsObject()
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
}
