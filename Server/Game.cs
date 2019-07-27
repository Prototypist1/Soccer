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
        private readonly ConcurrentIndexed<string, List<ObjectCreated>> connectionObjects = new ConcurrentIndexed<string, List<ObjectCreated>>();

        private ConcurrentLinkedList<PlayerInputs> playersInputs = new ConcurrentLinkedList<PlayerInputs>();
        public DateTime LastInput { get; private set; } = DateTime.Now;

        public Game() {
            ballId = Guid.NewGuid();
            ball = PhysicsObjectBuilder.Ball(1, 40, 800, 450);

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

        internal List<ObjectCreated> CreatePlayer(string connectionId,CreatePlayer createPlayer)
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

            var bodyCreated = new ObjectCreated(
                    body.X,
                    body.Y,
                    createPlayer.Body,
                    createPlayer.BodyDiameter,
                    createPlayer.BodyR,
                    createPlayer.BodyG,
                    createPlayer.BodyB,
                    createPlayer.BodyA);
            var footCreated = new ObjectCreated(foot.X,
                    foot.Y,
                    createPlayer.Foot,
                    createPlayer.FootDiameter,
                    createPlayer.FootR,
                    createPlayer.FootG,
                    createPlayer.FootB,
                    createPlayer.FootA);
            var res = new List<ObjectCreated>(){
                bodyCreated,
                footCreated };

            foreach (var item in res)
            {
                objectsCreated.Add(item);
            }

            connectionObjects.AddOrThrow(connectionId, res);

            return res;
        }

        internal bool TryDisconnect(string connectionId, out List<ObjectRemoved> objectRemoveds)
        {
            if (connectionObjects.TryGetValue(connectionId, out var objectCreated)) {
                objectRemoveds=  objectCreated.Select(x => new ObjectRemoved(x.Id)).ToList();
                return true;
            }
            objectRemoveds = default;
            return false;
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

            const double maxSpeed = 40.0;
            const double MaxForce = .5;
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


                foreach (var center in bodies)
                {
                    var body = center.Value;
                    var lastX = body.X;
                    var lastY = body.Y;

                    var lastVx = body.vx;
                    var lastVy = body.vy;

                    if (inputSet.TryGetValue(center.Key, out var input))
                    {

                       

                        var f = new Vector(input.BodyX, input.BodyY);
                        if (f.Length > 0)
                        {
                            f = f.NewUnitized().NewScaled(MaxForce);
                        }
                        else
                        {
                            f = new Vector(-body.vx, -body.vy);
                            if (f.Length > MaxForce)
                            {
                                f = f.NewUnitized().NewScaled(MaxForce);
                            }
                        }
                        f = Bound(f, new Vector(body.vy, body.vy));

                        body.ApplyForce(f.x, f.y);
                        body.Update();

                        var foot = feet[input.FootId];

                        // apply whatever force was applied to that body
                        foot.ApplyForce(
                            (body.vx - lastVx)* foot.Mass,
                            (body.vy - lastVy) * foot.Mass);


                        var max = 200.0;

                        var target = new Vector(foot.X + input.FootX - lastX, foot.Y + input.FootY - lastY);

                        if (target.Length > max)
                        {
                            target = target.NewScaled(max / target.Length);
                        }

                        var targetX = target.x + body.X;
                        var targetY = target.y + body.Y;

                        foot.ApplyForce(
                            (targetX - (foot.X + foot.Vx)) * foot.Mass / 1.0,
                            (targetY - (foot.Y + foot.Vy)) * foot.Mass / 1.0); 
                    }
                    else
                    {
                        body.Update();

                        var foot = feet[input.FootId];

                        // apply full force to get us to the bodies current pos
                        foot.ApplyForce(
                            (body.vx - lastVx) * foot.Mass,
                            (body.vy - lastVy) * foot.Mass);
                    }
                }

                simulationTime++;
                physicsEngine.Run(x =>
                {
                    x.Simulate(simulationTime);
                    return x;
                });

                positions = new Positions(GetPosition().ToArray(), simulationTime);
            }

            return true;

            Vector Bound(Vector force, Vector velocity)
            {
                if (force.Length == 0 || velocity.Length == 0)
                {
                    return force;
                }

                var with = velocity.NewUnitized().NewScaled(Math.Max(0, force.Dot(velocity.NewUnitized())));
                var notWith = force.NewAdded(with.NewMinus());

                with = with.NewScaled(Math.Max(0,(maxSpeed - velocity.Length)) / maxSpeed);

                return with.NewAdded(notWith);
            }
        }

        internal void PlayerInputs(PlayerInputs playerInputs)
        {
            LastInput = DateTime.Now;
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
