using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Common;
using Microsoft.AspNetCore.SignalR;
using Physics;
using Prototypist.TaskChain;

namespace Server
{
    public class Game
    {
        private int players = 0;


        private readonly JumpBallConcurrent<PhysicsEngine> physicsEngine = new JumpBallConcurrent<PhysicsEngine>(new PhysicsEngine(1000, Constants.xMax, Constants.yMax));
        private readonly ConcurrentIndexed<Guid, PhysicsObject> feet = new ConcurrentIndexed<Guid, PhysicsObject>();
        private readonly ConcurrentIndexed<Guid, Center> bodies = new ConcurrentIndexed<Guid, Center>();
        private readonly Guid ballId;
        private readonly PhysicsObject ball;

        private readonly ConcurrentSet<FootCreated> feetCreaated = new ConcurrentSet<FootCreated>();
        private readonly ConcurrentSet<BodyCreated> bodiesCreated = new ConcurrentSet<BodyCreated>();
        private readonly BallCreated ballCreated;
        private readonly ConcurrentSet<GoalCreated> goalsCreated = new ConcurrentSet<GoalCreated>();

        private readonly ConcurrentIndexed<string, List<ObjectCreated>> connectionObjects = new ConcurrentIndexed<string, List<ObjectCreated>>();

        private ConcurrentLinkedList<PlayerInputs> playersInputs = new ConcurrentLinkedList<PlayerInputs>();
        public DateTime LastInputUTC { get; private set; } = DateTime.Now;
        private int leftScore = 0, rightScore = 0;


        private readonly GameStateTracker gameStateTracker;
        private readonly System.Threading.Channels.Channel<Positions> channel;

        private class GameStateTracker
        {

            private readonly Action resetBallAction;
            private readonly double ballStartX, ballStartY;

            public CountDownState UpdateGameState()
            {
                var res = new CountDownState();
                res.Countdown = false;
                if (gameState != play)
                {
                    gameState++;
                }
                if (gameState == resetBall)
                {
                    resetBallAction();
                }
                if (gameState >= startCountDown && gameState < endCountDown)
                {
                    res.Countdown = true;
                    if (TryGetBallWall(out var tuple))
                    {
                        res.StrokeThickness = tuple.radius - (circleSize * (gameState - startCountDown) / ((double)(endCountDown - startCountDown)));
                        res.Radius = tuple.radius;
                        res.BallOpacity = Math.Min(1.0, Math.Abs(((endCountDown - startCountDown) - 2 * (gameState - startCountDown)) / ((double)(endCountDown - startCountDown))));
                        res.X = tuple.x;
                        res.Y = tuple.y;
                    }
                    else
                    {
                        throw new Exception("bug");
                    }
                }
                if (gameState == endCountDown)
                {
                    gameState = play;
                }
                return res;
            }

            private const double circleSize = 300;

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

            public bool CanScore() => gameState == play;

            public void Scored()
            {
                gameState = 1;
            }

            public bool TryGetBallWall(out (double x, double y, double radius) ballWall)
            {
                if (gameState >= startGrowingCircle)
                {
                    ballWall = (ballStartX,
                        ballStartY,
                        circleSize * ((Math.Min(gameState, stopGrowingCircle) - startGrowingCircle) / (double)(stopGrowingCircle - startGrowingCircle)));
                    return true;
                }
                ballWall = default;
                return false;
            }
        }

        internal void NameChanged(NameChanged nameChanged)
        {
            foreach (var element in bodiesCreated)
            {
                if (element.Id == nameChanged.Id)
                {
                    element.Name = nameChanged.Name;
                }
            }
        }

        internal ChannelReader<Positions> GetReader()
        {
            return channel.Reader;
        }

        internal void ColorChanged(ColorChanged colorChanged)
        {
            foreach (var element in feetCreaated)
            {
                if (element.Id == colorChanged.Id)
                {
                    element.G = colorChanged.G;
                    element.R = colorChanged.R;
                    element.B = colorChanged.B;
                    element.A = colorChanged.A;
                }
            }
            foreach (var element in bodiesCreated)
            {
                if (element.Id == colorChanged.Id)
                {
                    element.G = colorChanged.G;
                    element.R = colorChanged.R;
                    element.B = colorChanged.B;
                    element.A = colorChanged.A;
                }
            }
        }

        internal void Reset(Action<UpdateScore> onUpdateScore)
        {
            if (onUpdateScore == null)
            {
                throw new ArgumentNullException(nameof(onUpdateScore));
            }

            gameStateTracker.Scored();
            leftScore = 0;
            rightScore = 0;
            onUpdateScore(new UpdateScore() { Left = leftScore, Right = rightScore });
        }

        public Game(Action<UpdateScore> onUpdateScore, Channel<Positions> writer)
        {

            if (onUpdateScore == null)
            {
                throw new ArgumentNullException(nameof(onUpdateScore));
            }
            this.channel = writer ?? throw new ArgumentNullException(nameof(writer));


            ballId = Guid.NewGuid();
            ball = PhysicsObjectBuilder.Ball(.05, Constants.Radius * 12, Constants.xMax / 2, Constants.yMax / 2);

            ballCreated = new BallCreated(
               ball.X,
               ball.Y,
               Constants.ballZ,
               ballId,
               Constants.Radius * 12 * 2,
               0,
               0,
               0,
               255);

            var leftGoalId = Guid.NewGuid();
            var leftGoal = PhysicsObjectBuilder.Goal(Constants.footLen, (Constants.footLen * 3), Constants.yMax / 2.0, x => x == ball && gameStateTracker.CanScore(), x =>
            {
                if (gameStateTracker.CanScore())
                {
                    gameStateTracker.Scored();
                    rightScore++;
                    onUpdateScore(new UpdateScore() { Left = leftScore, Right = rightScore });
                }
            });
            goalsCreated.AddOrThrow(new GoalCreated(
               leftGoal.X,
               leftGoal.Y,
               Constants.goalZ,
               leftGoalId,
               Constants.footLen * 2,
               0xee,
               0xee,
               0xee,
               0xff));

            var rightGoalId = Guid.NewGuid();
            var rightGoal = PhysicsObjectBuilder.Goal(Constants.footLen, Constants.xMax - (Constants.footLen * 3), Constants.yMax / 2.0, x => x == ball && gameStateTracker.CanScore(), x =>
            {
                if (gameStateTracker.CanScore())
                {
                    gameStateTracker.Scored();
                    leftScore++;
                    onUpdateScore(new UpdateScore() { Left = leftScore, Right = rightScore });
                }
            });
            goalsCreated.AddOrThrow(new GoalCreated(
               rightGoal.X,
               rightGoal.Y,
               Constants.goalZ,
               rightGoalId,
               Constants.footLen * 2,
               0xee,
               0xee,
               0xee,
               0xff));


            //var points = new[] {
            //    (new Vector(footLen,0) ,new Vector(xMax- footLen,0)),
            //    (new Vector(0,footLen) ,new Vector(footLen,0)),
            //    (new Vector(0,yMax - footLen) ,new Vector(0,footLen)),
            //    (new Vector(footLen,yMax),new Vector(0,yMax - footLen)),
            //    (new Vector(xMax - footLen,yMax),new Vector(footLen,yMax)),
            //    (new Vector(xMax,yMax - footLen),new Vector(xMax - footLen,yMax)),
            //    (new Vector(xMax,footLen),new Vector(xMax,yMax - footLen)),
            //    (new Vector(xMax- footLen,0) ,new Vector(xMax,footLen))
            //};

            var points = new[] {
                (new Vector(0,0) ,new Vector(Constants.xMax-1,0)),
                (new Vector(0,Constants.yMax-1 ) ,new Vector(0,0)),
                (new Vector(Constants.xMax-1,Constants.yMax-1),new Vector(0,Constants.yMax-1)),
                (new Vector(Constants.xMax-1,Constants.yMax-1),new Vector(Constants.xMax-1,0)),
            };

            gameStateTracker = new GameStateTracker(() =>
            {
                ball.X = Constants.xMax / 2.0;
                ball.Y = Constants.yMax / 2.0;
                ball.Vx = 0;
                ball.Vy = 0;
            },
            Constants.xMax / 2.0,
            Constants.yMax / 2.0
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

        public ObjectsCreated GetObjectsCreated()
        {
            return new ObjectsCreated(feetCreaated.ToArray(), bodiesCreated.ToArray(), ballCreated, goalsCreated.ToArray());
        }

        internal ObjectsCreated CreatePlayer(string connectionId, CreatePlayer createPlayer)
        {
            double startX = 400;
            double startY = 400;
            var foot = PhysicsObjectBuilder.Ball(1, createPlayer.FootDiameter / 2.0, startX, startY);

            physicsEngine.Run(x => { x.AddObject(foot); return x; });
            feet[createPlayer.Foot] = foot;

            var body = new Center(
                startX,
                startY,
                Constants.xMax,
                0,
                0,
                Constants.yMax,
                foot,
                createPlayer.BodyDiameter / 2.0
                );
            bodies[createPlayer.Body] = body;

            var bodyCreated = new BodyCreated(
                    body.X,
                    body.Y,
                    Constants.bodyZ,
                    createPlayer.Body,
                    createPlayer.BodyDiameter,
                    createPlayer.BodyR,
                    createPlayer.BodyG,
                    createPlayer.BodyB,
                    createPlayer.BodyA,
                    "");
            bodiesCreated.AddOrThrow(bodyCreated);
            var footCreated = new FootCreated(
                    foot.X,
                    foot.Y,
                    Constants.footZ,
                    createPlayer.Foot,
                    createPlayer.FootDiameter,
                    createPlayer.FootR,
                    createPlayer.FootG,
                    createPlayer.FootB,
                    createPlayer.FootA);
            feetCreaated.AddOrThrow(footCreated);


            connectionObjects.AddOrThrow(connectionId, new List<ObjectCreated>(){
                bodyCreated,
                footCreated });


            Interlocked.Add(ref players, 1);

            return new ObjectsCreated(new[] { footCreated }, new[] { bodyCreated }, null, new GoalCreated[] { });
        }

        internal bool TryDisconnect(string connectionId, out List<ObjectRemoved> objectRemoveds)
        {
            if (connectionObjects.TryRemove(connectionId, out var toRemoves))
            {
                Interlocked.Add(ref players, -1);
                objectRemoveds = new List<ObjectRemoved>();
                foreach (var item in toRemoves)
                {
                    var foot = feetCreaated.SingleOrDefault(x => x.Id == item.Id);
                    if (foot != null)
                    {
                        feetCreaated.RemoveOrThrow(foot);
                        objectRemoveds.Add(new ObjectRemoved(item.Id));
                    }

                    var body = bodiesCreated.SingleOrDefault(x => x.Id == item.Id);
                    if (body != null)
                    {
                        bodiesCreated.RemoveOrThrow(body);
                        objectRemoveds.Add(new ObjectRemoved(item.Id));
                    }
                }
                return true;
            }
            objectRemoveds = default;
            return false;
        }

        //public async void Start()
        //{
        //    var stopWatch = new Stopwatch();
        //    stopWatch.Start();
        //    var frame = 0;
        //    while (true)
        //    {
        //        if ((out var positions))
        //        {

        //        };
        //        frame++;

        //        await Task.Delay((int)Math.Max(0, ((1000 * frame) / 60) - stopWatch.ElapsedMilliseconds));
        //        await Task.Yield();
        //        //var whatIsIt = ((1000 * frame) / 60) - stopWatch.ElapsedMilliseconds;
        //        //if (whatIsIt > 0)
        //        //{
        //        //    hit++;
        //        //}
        //        //else {
        //        //    nothit++;
        //        //}
        //    }
        //}

        private int simulationTime = 0;
        internal Positions Apply()
        {

            const double maxSpeed = 40.0;
            const double MaxForce = 8;
            Positions positions = default;

            var myPlayersInputs = Interlocked.Exchange(ref playersInputs, new ConcurrentLinkedList<PlayerInputs>());

            // shocking
            //if (!myPlayersInputs.Any())
            //{
            //}

            var frames = new List<Dictionary<Guid, PlayerInputs>>();

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
                    -(ball.Vx * ball.Mass) / 50.0,
                    -(ball.Vy * ball.Mass) / 50.0);


                foreach (var center in bodies)
                {
                    var body = center.Value;
                    var lastX = body.X;
                    var lastY = body.Y;

                    var lastVx = body.vx;
                    var lastVy = body.vy;

                    if (inputSet.TryGetValue(center.Key, out var input))
                    {

                        //if (input.BodyX != 0) {
                        //    input.BodyY = 0;
                        //}
                        var f = new Vector(input.BodyX* MaxForce, input.BodyY* MaxForce);

                        f = Bound(f, new Vector(body.vx, body.vy));

                        if (f.Length > MaxForce)
                        {
                            f = f.NewScaled(MaxForce / f.Length);
                        }

                        f = new Vector(input.BodyX == 0 ? -body.vx : f.x, input.BodyY == 0 ? -body.vy : f.y);

                        body.ApplyForce(f.x, f.y);


                        body.Update(gameStateTracker.TryGetBallWall(out var tup), tup, maxSpeed);

                        var foot = body.Foot;

                        // apply whatever force was applied to that body
                        foot.ApplyForce(
                            (body.vx - lastVx) * foot.Mass,
                            (body.vy - lastVy) * foot.Mass);


                        var max = Constants.footLen - Constants.Radius;

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
                        body.Update(gameStateTracker.TryGetBallWall(out var tup), tup, maxSpeed);

                        var foot = body.Foot;

                        // apply full force to get us to the bodies current pos
                        foot.ApplyForce(
                            (body.vx - lastVx) * foot.Mass,
                            (body.vy - lastVy) * foot.Mass);

                        var max = Constants.footLen;

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
                Collision[] collisions = null;
                physicsEngine.Run(x =>
                {
                    collisions = x.Simulate(simulationTime);
                    return x;
                });

                positions = new Positions(GetPosition().ToArray(), simulationTime, countDownSate, collisions);
            }

            return positions;

            Vector Bound(Vector force, Vector velocity)
            {
                var startLen = force.Length;
                var startX = force.x;
                var startY = force.y;

                if (force.Length == 0 || velocity.Length == 0)
                {
                    return force;
                }

                var with = velocity.NewUnitized().NewScaled(force.Length *Math.Max(0, force.NewUnitized().Dot(velocity.NewUnitized())));
                var notWith = force.NewAdded(with.NewMinus());

                var scaleBy = Math.Pow(Math.Max(0, (maxSpeed - with.Length)) / maxSpeed, 25);

                with = with.NewScaled(scaleBy);

                var res = with.NewAdded(notWith);

                return res;
            }
        }

        private int running = 0;

        internal void PlayerInputs(PlayerInputs playerInputs)
        {
            var lastLast = LastInputUTC;
            playersInputs.Add(playerInputs);
            if (Interlocked.CompareExchange(ref running, 1, 0) == 0)
            {
                var nextLast = DateTime.UtcNow;
                if (playersInputs.Count > (players / 2.0))
                {
                    var dontWait = channel.Writer.WriteAsync(Apply());
                    LastInputUTC = nextLast;
                }
                running = 0;
            }

        }

        private IEnumerable<Position> GetPosition()
        {
            yield return new Position(ball.X, ball.Y, ballId, ball.Vx, ball.Vy);
            foreach (var foot in feet)
            {
                yield return new Position(foot.Value.X, foot.Value.Y, foot.Key, foot.Value.Vx, foot.Value.Vy);
            }
            foreach (var body in bodies)
            {
                yield return new Position(body.Value.X, body.Value.Y, body.Key, body.Value.vx, body.Value.vy);
            }
        }
    }
}