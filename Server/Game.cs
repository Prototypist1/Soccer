using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Microsoft.AspNetCore.SignalR;
using Physics;
using Prototypist.TaskChain;

namespace Server
{
    public class Game {
        private const double footLen = 200;
        private const double xMax = 1600;
        private const double yMax = 900;
        private readonly JumpBallConcurrent<PhysicsEngine> physicsEngine = new JumpBallConcurrent<PhysicsEngine>( new PhysicsEngine(100, xMax+100, yMax+100));
        private readonly ConcurrentIndexed<Guid, PhysicsObject> feet = new ConcurrentIndexed<Guid, PhysicsObject>();
        private readonly ConcurrentIndexed<Guid, Center> bodies = new ConcurrentIndexed<Guid, Center>();
        private readonly Guid ballId;
        private readonly PhysicsObject ball;
        private readonly ConcurrentLinkedList<ObjectCreated> objectsCreated = new ConcurrentLinkedList<ObjectCreated>();

        private ConcurrentLinkedList<PlayerInputs> playersInputs = new ConcurrentLinkedList<PlayerInputs>();

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

            foreach (var item in res)
            {
                objectsCreated.Add(item);
            }
            return res;
        }

        public async void Start(Func<Positions,Task> onPositionsUpdate) {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var frame = 0;
            while (true)
            {
                if (Apply(out var positions)){
                    await onPositionsUpdate(positions);
                };
                frame++;
                await Task.Delay((int)Math.Max(1, ((1000 * frame) / 60) - stopWatch.ElapsedMilliseconds));
            }
        }

        private int simulationTime = 0;
        internal bool Apply(out Positions positions) {

            positions = default;

            var myPlayersInputs = Interlocked.Exchange(ref playersInputs, new ConcurrentLinkedList<PlayerInputs>());

            if (!myPlayersInputs.Any()) {
                return false;
            }

            var frames = new List<Dictionary<Guid,PlayerInputs>>();

            foreach (var input in myPlayersInputs)
            {
                foreach (var frame in frames)
                {
                    if (frame.TryAdd(input.BodyId, input))
                    {
                        goto done;
                    }
                }

                frames.Add(new Dictionary<Guid, PlayerInputs> {
                    { input.BodyId,input}
                });

                done:;
            }


            foreach (var inputSet in frames)
            {
                ball.ApplyForce(
                    -(ball.Vx * ball.Mass) / 100.0,
                    -(ball.Vy * ball.Mass) / 100.0);

                foreach (var input in inputSet.Values)
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

                    var targetVx = (target.x + body.X + body.vx) - foot.X;
                    var targetVy = (target.y + body.Y + body.vy) - foot.Y;

                    foot.ApplyForce(
                        (targetVx - foot.Vx) * foot.Mass / 2.0,
                        (targetVy - foot.Vy) * foot.Mass / 2.0);
                }
                simulationTime++;
                physicsEngine.Run(x =>
                {
                    x.Simulate(simulationTime);
                    return x;
                });
                foreach (var center in bodies.Values)
                {
                    center.Update();
                }
                positions = new Positions(GetPosition().ToArray(), simulationTime);
            }

            return true;
        }

        internal void PlayerInputs(PlayerInputs playerInputs)
        {
            playersInputs.Add(playerInputs);
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
