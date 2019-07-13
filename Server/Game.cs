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
        private const double footLen = 200;
        private const double xMax = 1600;
        private const double yMax = 900;
        private readonly PhysicsEngine physicsEngine = new PhysicsEngine(100, xMax+100, yMax+100);
        private readonly Dictionary<Guid, PhysicsObject> feet = new Dictionary<Guid, PhysicsObject>();
        private readonly Dictionary<Guid, Center> bodies = new Dictionary<Guid, Center>();
        private readonly Guid ballId;
        private readonly PhysicsObject ball;
        private readonly List<ObjectCreated> objectsCreated = new List<ObjectCreated>();

        private readonly List<List<PlayerInputs>> frames = new List<List<PlayerInputs>>();

        public Game() {
            ballId = Guid.NewGuid();
            ball = PhysicsObjectBuilder.Ball(1, 40, 450, 450);

            objectsCreated.Add(new ObjectCreated(
                ball.X, 
                ball.Y, 
                ballId,
                80,
                0,
                0,
                0,
                255));

            physicsEngine.AddObject(ball);
            var points = new[] {
                (new Vector(footLen,0) ,new Vector(xMax- footLen,0)),
                (new Vector(0,footLen) ,new Vector(footLen,0)),
                (new Vector(0,yMax - footLen) ,new Vector(0,footLen)),
                (new Vector(footLen,yMax),new Vector(0,yMax - footLen)),
                (new Vector(xMax - footLen,yMax),new Vector(footLen,yMax)),
                (new Vector(xMax,yMax - footLen),new Vector(xMax - footLen,yMax)),
                (new Vector(xMax,footLen),new Vector(xMax,yMax - footLen)),
                (new Vector(xMax- footLen,0) ,new Vector(xMax,footLen))
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
            double startX = 400;
            double startY = 400;
            var foot = PhysicsObjectBuilder.Ball(1, createPlayer.FootDiameter/2.0, startX, startY);

            physicsEngine.AddObject(foot);
            feet[createPlayer.Foot] = foot;

            var body = new Center(
                startX,
                startY,
                xMax - (createPlayer.BodyDiameter / 2.0),
                (createPlayer.BodyDiameter / 2.0),
                (createPlayer.BodyDiameter / 2.0),
                yMax - (createPlayer.BodyDiameter / 2.0));
            bodies[createPlayer.Body] = body;
            PlayerCount++;

            var res = new List<ObjectCreated>(){
                new ObjectCreated(
                    body.X, 
                    body.Y, 
                    createPlayer.Body,
                    createPlayer.BodyDiameter,
                    createPlayer.BodyR,
                    createPlayer.BodyG,
                    createPlayer.BodyB,
                    createPlayer.BodyA),
                new ObjectCreated(foot.X, 
                    foot.Y, 
                    createPlayer.Foot,
                    createPlayer.FootDiameter,
                    createPlayer.FootR,
                    createPlayer.FootG,
                    createPlayer.FootB,
                    createPlayer.FootA) };

            objectsCreated.AddRange(res);
            return res;
        }

        internal List<Positions> PlayerInputs(PlayerInputs playerInputs)
        {
            while (playerInputs.Frame >= frames.Count) {
                frames.Add(new List<PlayerInputs>());
            }
            frames[playerInputs.Frame].Add(playerInputs);
            var res = new List<Positions>();
            while (frames.Count > frame && frames[frame].Count == PlayerCount) {

                ball.ApplyForce(
                    -(ball.Vx * ball.Mass) / 100.0,
                    -(ball.Vy* ball.Mass) / 100.0);

                foreach (var input in frames[frame])
                {
                    var body = bodies[input.BodyId];
                    var speed = .2;
                    var keyForce = new Vector(input.BodyX, input.BodyY);
                    if (keyForce.Length > 0)
                    {
                        keyForce = keyForce.NewUnitized().NewScaled(speed);
                        body.ApplyForce(keyForce.x, keyForce.y);
                    }

                    var foot = feet[input.FootId];
                    var max = 200.0;

                    var target = new Vector(foot.X + input.FootX - (body.X), foot.Y + input.FootY - (body.Y));

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
                frame++;
                physicsEngine.Simulate(frame);
                foreach (var center in bodies.Values)
                {
                    center.Update();
                }
                res.Add(new Positions(GetPosition().ToArray(),frame-1));
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
