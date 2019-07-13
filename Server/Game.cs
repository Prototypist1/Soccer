using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Microsoft.AspNetCore.SignalR;
using Physics;

namespace Server
{
    public class Game {
        private int PlayerCount = 0;
        private int frame=0;
        private readonly PhysicsEngine physicsEngine = new PhysicsEngine(100, 900, 1600);
        private readonly Dictionary<Guid, PhysicsObject> feet = new Dictionary<Guid, PhysicsObject>();
        private readonly Dictionary<Guid, Center> bodies = new Dictionary<Guid, Center>();
        private readonly Guid ballId;
        private readonly PhysicsObject ball;
        private readonly List<ObjectCreated> objectsCreated = new List<ObjectCreated>();

        private readonly List<List<PlayerInputs>> frames = new List<List<PlayerInputs>>();

        public Game() {
            ballId = Guid.NewGuid();
            ball = PhysicsObjectBuilder.Ball(1, 40, 450, 450);
            physicsEngine.AddObject(ball);
            var points = new[] {
                (new Vector(210,10) ,new Vector(590,10)),
                (new Vector(10,210) ,new Vector(210,10)),
                (new Vector(10,590) ,new Vector(10,210)),
                (new Vector(210,790),new Vector(10,590)),
                (new Vector(590,790),new Vector(210,790)),
                (new Vector(790,590),new Vector(590,790)),
                (new Vector(790,210),new Vector(790,590)),
                (new Vector(590,10) ,new Vector(790,210))
            };

            foreach (var side in points)
            {
                var line = PhysicsObjectBuilder.Line(side.Item1, side.Item2);

                physicsEngine.AddObject(line);
            }
        }

        public IReadOnlyList<ObjectCreated> GetObjectsCreated() => objectsCreated;

        internal List<ObjectCreated> CreatePlayer(CreatePlayer createPlayer)
        {
            double radius = 40;
            double startX = 400;
            double startY = 400;
            var foot = PhysicsObjectBuilder.Ball(1, radius, startX, startY);

            physicsEngine.AddObject(foot);
            feet[createPlayer.foot] = foot;

            var body = new Center(
                startX,
                startY,
                1600-radius,
                radius,
                radius,
                900-radius);
            bodies[createPlayer.body] = body;
            PlayerCount++;

            var res = new List<ObjectCreated>(){
                new ObjectCreated(body.X, body.Y, createPlayer.body),
                new ObjectCreated(foot.X, foot.Y, createPlayer.foot) };

            objectsCreated.AddRange(res);
            return res;
        }

        internal List<Positions> PlayerInputs(PlayerInputs playerInputs)
        {
            while (playerInputs.frame >= frames.Count) {
                frames.Add(new List<PlayerInputs>());
            }
            frames[playerInputs.frame].Add(playerInputs);
            var res = new List<Positions>();
            while (frames[frame].Count == PlayerCount) {
                frame++;
                foreach (var input in frames[frame])
                {
                    var body = bodies[input.bodyId];
                    var speed = .2;
                    var keyForce = new Vector(input.bodyX, input.bodyY);
                    if (keyForce.Length > 0)
                    {
                        keyForce = keyForce.NewUnitized().NewScaled(speed);
                        body.ApplyForce(keyForce.x, keyForce.y);
                    }

                    var foot = feet[input.footId];
                    var max = 200.0;

                    var target = new Vector(foot.X + input.footX - (body.X), foot.Y + input.footY - (body.Y));

                    if (target.Length > max)
                    {
                        target = target.NewScaled(max / target.Length);
                    }

                    var targetVx = ((target.x + body.X + body.vx) - foot.X);
                    var targetVy = (target.y + body.Y + body.vy) - foot.Y;

                    foot.ApplyForce(
                        (targetVx - foot.Vx) * foot.Mass / 2.0,
                        (targetVy - foot.Vy) * foot.Mass / 2.0);
                }
                physicsEngine.Simulate(frame);
                foreach (var center in bodies.Values)
                {
                    center.Update();
                }
                res.Add(new Positions(GetPosition().ToArray()));
            }
            return res;
        }

        private IEnumerable<Position> GetPosition() {
            yield return new Position(ball.X, ball.Y, ballId);
            foreach (var foot in feet)
            {
                yield return new Position(foot.Value.X, foot.Value.Y, foot.Key);
            }
            foreach (var body in bodies)
            {
                yield return new Position(body.Value.X, body.Value.Y, body.Key);
            }
        }
    }
}
