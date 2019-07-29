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
        private const double xMax = 6400;
        private const double yMax = 3200;
        private const int Radius = 40;
        private readonly JumpBallConcurrent<PhysicsEngine> physicsEngine = new JumpBallConcurrent<PhysicsEngine>( new PhysicsEngine(100, xMax+100, yMax+100));
        private readonly ConcurrentIndexed<Guid, PhysicsObject> feet = new ConcurrentIndexed<Guid, PhysicsObject>();
        private readonly ConcurrentIndexed<Guid, Center> bodies = new ConcurrentIndexed<Guid, Center>();
        private readonly Guid ballId;
        private readonly PhysicsObject ball;
        private readonly ConcurrentSet<ObjectCreated> objectsCreated = new ConcurrentSet<ObjectCreated>();
        private readonly ConcurrentIndexed<string, List<ObjectCreated>> connectionObjects = new ConcurrentIndexed<string, List<ObjectCreated>>();

        private ConcurrentLinkedList<PlayerInputs> playersInputs = new ConcurrentLinkedList<PlayerInputs>();
        public DateTime LastInput { get; private set; } = DateTime.Now;
        private int leftScore=0, rightScore=0;

        private const int goalZ = 0;
        private const int bodyZ = 1;
        private const int ballZ = 2;
        private const int footZ = 2;

        private readonly GameStateTracker gameStateTracker;

        private class GameStateTracker {

            private readonly Action resetBallAction;
            private readonly double ballStartX, ballStartY;

            public CountDownState UpdateGameState() {
                var res = new CountDownState();
                res.Countdown = false;
                if (gameState != play) {
                    gameState++;
                }
                if (gameState == resetBall) {
                    resetBallAction();
                }
                if (gameState >= startCountDown && gameState < endCountDown) {
                    res.Countdown = true;
                    res.CurrentFrame = gameState;
                    res.FinalFrame = endCountDown;
                    if (TryGetBallWall(out var tuple)) {
                        res.Radius = tuple.radius;
                        res.X = tuple.x;
                        res.Y = tuple.y;
                    }
                    else {
                        throw new Exception("bug");
                    }
                }
                if (gameState == endCountDown) {
                    gameState = play;
                }
                return res;
            }

            private const double circleSize = 200;

            private const int play = -1;
            private const int startGrowingCircle = 0;
            private const int stopGrowingCircle = 300;
            private const int resetBall = 300;
            private const int startCountDown = 0;
            private const int endCountDown = 600;

            private int gameState = play;

            public GameStateTracker(Action resetBallAction, double ballStartX, double ballStartY)
            {
                this.resetBallAction = resetBallAction ?? throw new ArgumentNullException(nameof(resetBallAction));
                this.ballStartX = ballStartX;
                this.ballStartY = ballStartY;
            }

            public bool CanScore() => gameState== play;

            public void Scored()
            {
                if (gameState == play)
                {
                    gameState = 1;
                }
                else {
                    throw new Exception("bug");
                }
            }

            public bool TryGetBallWall(out (double x, double y, double radius) ballWall) {
                if (gameState >= startGrowingCircle)
                {
                    ballWall = (ballStartX, ballStartY, circleSize*((Math.Min(gameState,stopGrowingCircle)- startGrowingCircle)/(double)(stopGrowingCircle- startGrowingCircle)));
                    return true;
                }
                ballWall = default;
                return false;
            }
        }


        public Game(Action<UpdateScore> onUpdateScore) {
            if (onUpdateScore == null)
            {
                throw new ArgumentNullException(nameof(onUpdateScore));
            }

            ballId = Guid.NewGuid();
            ball = PhysicsObjectBuilder.Ball(1, Radius, 800, 450);

            objectsCreated.AddOrThrow(new ObjectCreated(
               ball.X,
               ball.Y,
               ballZ,
               ballId,
               80,
               0,
               0,
               0,
               255));

            var leftGoalId = Guid.NewGuid();
            var leftGoal = PhysicsObjectBuilder.Goal(footLen, (footLen * 3), yMax / 2.0,x=>x== ball, x=>{
                if (gameStateTracker.CanScore())
                {
                    gameStateTracker.Scored();
                    rightScore++;
                    onUpdateScore(new UpdateScore() { Left = leftScore, Right = rightScore });
                }
            });
            objectsCreated.AddOrThrow(new ObjectCreated(
               leftGoal.X,
               leftGoal.Y,
               goalZ,
               leftGoalId,
               footLen*2,
               0xdd,
               0xdd,
               0xdd,
               0xff));

            var rightGoalId = Guid.NewGuid();
            var rightGoal = PhysicsObjectBuilder.Goal(footLen, xMax - (footLen * 3), yMax / 2.0, x =>x==ball, x=> {
                if (gameStateTracker.CanScore())
                {
                    gameStateTracker.Scored();
                    leftScore++;
                    onUpdateScore(new UpdateScore() { Left = leftScore, Right = rightScore });
                }
            });
            objectsCreated.AddOrThrow(new ObjectCreated(
               rightGoal.X,
               rightGoal.Y,
               goalZ,
               rightGoalId,
               footLen * 2,
               0xdd,
               0xdd,
               0xdd,
               0xff));


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

            gameStateTracker = new GameStateTracker(() =>
            {
                ball.X = xMax / 2.0;
                ball.Y = yMax / 2.0;
                ball.Vx = 0;
                ball.Vy = 0;
            },
            xMax / 2.0,
            yMax / 2.0
            );

            physicsEngine.Run(x =>
            {
                x.AddObject(ball);
                x.AddObject(leftGoal);
                x.AddObject(rightGoal);
                foreach (var side in points)
                {
                    var line = PhysicsObjectBuilder.Line(side.Item1, side.Item2);

                    x.AddObject(line);
                }
                return x;
            });
            
        }

        public IReadOnlyList<ObjectCreated> GetObjectsCreated() => objectsCreated.ToArray();

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
                xMax,
                0,
                0,
                yMax,
                foot,
                createPlayer.BodyDiameter / 2.0
                );
            bodies[createPlayer.Body] = body;

            var bodyCreated = new ObjectCreated(
                    body.X,
                    body.Y,
                    bodyZ,
                    createPlayer.Body,
                    createPlayer.BodyDiameter,
                    createPlayer.BodyR,
                    createPlayer.BodyG,
                    createPlayer.BodyB,
                    createPlayer.BodyA);
            var footCreated = new ObjectCreated(
                    foot.X,
                    foot.Y,
                    footZ,
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
                objectsCreated.AddOrThrow(item);
            }

            connectionObjects.AddOrThrow(connectionId, res);

            return res;
        }

        internal bool TryDisconnect(string connectionId, out List<ObjectRemoved> objectRemoveds)
        {
            if (connectionObjects.TryRemove(connectionId, out var toRemoves)) {
                objectRemoveds=  toRemoves.Select(x => new ObjectRemoved(x.Id)).ToList();
                foreach (var item in toRemoves)
                {
                    objectsCreated.RemoveOrThrow(item);
                }
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
            const double MaxForce = 1;
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
                var countDownSate = gameStateTracker.UpdateGameState();

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
                        f = Bound(f, new Vector(body.vx, body.vy));

                        body.ApplyForce(f.x, f.y);
                        body.Update(gameStateTracker.TryGetBallWall(out var tup),tup);

                        var foot = body.Foot;

                        // apply whatever force was applied to that body
                        foot.ApplyForce(
                            (body.vx - lastVx)* foot.Mass,
                            (body.vy - lastVy) * foot.Mass);


                        var max = footLen- Radius;

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
                        body.Update(gameStateTracker.TryGetBallWall(out var tup), tup);

                        var foot = body.Foot;

                        // apply full force to get us to the bodies current pos
                        foot.ApplyForce(
                            (body.vx - lastVx) * foot.Mass,
                            (body.vy - lastVy) * foot.Mass);

                        var max = 200.0;

                        var target = new Vector(foot.X - lastX, foot.Y - lastY);

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
                }

                simulationTime++;
                physicsEngine.Run(x =>
                {
                    x.Simulate(simulationTime);
                    return x;
                });

                positions = new Positions(GetPosition().ToArray(), simulationTime, countDownSate);
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

                with = with.NewScaled(Math.Pow( Math.Max(0,(maxSpeed - velocity.Length)) / maxSpeed,2));


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
