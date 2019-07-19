using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Microsoft.AspNetCore.SignalR;
using Physics;
using Prototypist.TaskChain;
using Prototypist.TaskChain.DataTypes;

namespace Server
{
    public class Game {
        private int PlayerCount = 0;
        private int frame=0;
        private const double footLen = 200;
        private const double xMax = 1600;
        private const double yMax = 900;
        private readonly JumpBallConcurrent<PhysicsEngine> physicsEngine = new JumpBallConcurrent<PhysicsEngine>( new PhysicsEngine(100, xMax+100, yMax+100));
        private readonly ConcurrentIndexed<Guid, PhysicsObject> feet = new ConcurrentIndexed<Guid, PhysicsObject>();
        private readonly ConcurrentIndexed<Guid, Center> bodies = new ConcurrentIndexed<Guid, Center>();
        private readonly Guid ballId;
        private readonly PhysicsObject ball;
        private readonly ConcurrentArrayList<ObjectCreated> objectsCreated = new ConcurrentArrayList<ObjectCreated>();

        private readonly ConcurrentArrayList<ConcurrentArrayList<PlayerInputs>> frames = new ConcurrentArrayList<ConcurrentArrayList<PlayerInputs>>();

        public Game() {
            ballId = Guid.NewGuid();
            ball = PhysicsObjectBuilder.Ball(1, 40, 450, 450);

            objectsCreated.EnqueAdd(new ObjectCreated(
                ball.X,
                ball.Y,
                ballId,
                80,
                0,
                0,
                0,
                255));

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


            physicsEngine.Run(x =>
            {
                x.AddObject(ball);
                foreach (var side in points)
                {
                    var line = PhysicsObjectBuilder.Line(side.Item1, side.Item2);

                    x.AddObject(line);
                }
                return x;
            });
            
        }

        public IReadOnlyList<ObjectCreated> GetObjectsCreated() => objectsCreated;

        internal List<ObjectCreated> CreatePlayer(CreatePlayer createPlayer)
        {
            double startX = 400;
            double startY = 400;
            var foot = PhysicsObjectBuilder.Ball(1, createPlayer.FootDiameter/2.0, startX, startY);

            physicsEngine.Run(x => { x.AddObject(foot); return x; });
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

            objectsCreated.EnqueAddSet(res);
            return res;
        }

        int processing = 0;

        internal List<Positions> PlayerInputs(PlayerInputs playerInputs)
        {
            while (playerInputs.Frame >= frames.Count) {
                frames.EnqueAdd(new ConcurrentArrayList<PlayerInputs>());
            }
            frames[playerInputs.Frame].EnqueAdd(playerInputs);
            var res = new List<Positions>();

            bool tryIt = true;

            while (tryIt && frames.Count > frame && frames[frame].Count == PlayerCount) {

                tryIt = Interlocked.CompareExchange(ref processing, 1, 0) == 0;

                if (tryIt)
                {

                    ball.ApplyForce(
                        -(ball.Vx * ball.Mass) / 100.0,
                        -(ball.Vy * ball.Mass) / 100.0);

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
                    physicsEngine.Run(x =>
                    {
                        x.Simulate(frame); return x;
                    });
                    foreach (var center in bodies.Values)
                    {
                        center.Update();
                    }
                    res.Add(new Positions(GetPosition().ToArray(), frame - 1));

                    processing = 0;
                }
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
