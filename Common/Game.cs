using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using physics2;
using Physics2;
using Prototypist.TaskChain;

namespace Common
{
    public class Game
    {
        private const int EnergyAdd = 800;
        private int players = 0;


        private readonly JumpBallConcurrent<PhysicsEngine> physicsEngine = new JumpBallConcurrent<PhysicsEngine>(new PhysicsEngine());
        private readonly ConcurrentIndexed<Guid, PhysicsObject> feet = new ConcurrentIndexed<Guid, PhysicsObject>();
        private readonly ConcurrentIndexed<Guid, Center> bodies = new ConcurrentIndexed<Guid, Center>();
        private readonly Guid ballId;
        private readonly Ball ball;

        private readonly ConcurrentSet<FootCreated> feetCreaated = new ConcurrentSet<FootCreated>();
        private readonly ConcurrentSet<BodyCreated> bodiesCreated = new ConcurrentSet<BodyCreated>();
        private readonly BallCreated ballCreated;
        private readonly ConcurrentSet<GoalCreated> goalsCreated = new ConcurrentSet<GoalCreated>();

        private readonly ConcurrentIndexed<string, List<ObjectCreated>> connectionObjects = new ConcurrentIndexed<string, List<ObjectCreated>>();

        private ConcurrentLinkedList<PlayerInputs> playersInputs = new ConcurrentLinkedList<PlayerInputs>();
        public DateTime LastInputUTC { get; private set; } = DateTime.Now;



        private readonly GameStateTracker gameStateTracker;

        private class GameStateTracker
        {
            public int leftScore = 0, rightScore = 0;
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
                        res.StrokeThickness = tuple.radius - (Constants.footLen * (gameState - startCountDown) / ((double)(endCountDown - startCountDown)));
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
                        Constants.footLen * ((Math.Min(gameState, stopGrowingCircle) - startGrowingCircle) / (double)(stopGrowingCircle - startGrowingCircle)));
                    return true;
                }
                ballWall = default;
                return false;
            }
        }

        private class GoalManager : IGoalManager
        {
            private readonly GameStateTracker gameStateTracker;
            private readonly Action<UpdateScore> onUpdateScore;
            private bool right;

            public GoalManager(GameStateTracker gameStateTracker, Action<UpdateScore> onUpdateScore, bool right)
            {
                this.gameStateTracker = gameStateTracker ?? throw new ArgumentNullException(nameof(gameStateTracker));
                this.onUpdateScore = onUpdateScore ?? throw new ArgumentNullException(nameof(onUpdateScore));
                this.right = right;
            }

            private class UpdateScoreEvent : IEvent
            {
                private readonly GameStateTracker gameStateTracker;
                private readonly Action<UpdateScore> onUpdateScore;
                private bool right;
                

                public UpdateScoreEvent(GameStateTracker gameStateTracker, Action<UpdateScore> onUpdateScore, bool right, double time)
                {
                    this.gameStateTracker = gameStateTracker ?? throw new ArgumentNullException(nameof(gameStateTracker));
                    this.onUpdateScore = onUpdateScore ?? throw new ArgumentNullException(nameof(onUpdateScore));
                    this.right = right;
                    Time = time;
                }

                public double Time
                {
                    get;
                }

                public MightBeCollision Enact()
                {
                    gameStateTracker.Scored();
                    if (right)
                    {
                        gameStateTracker.rightScore++;
                    }
                    else
                    {
                        gameStateTracker.leftScore++;

                    }
                    onUpdateScore(new UpdateScore() { Left = gameStateTracker.leftScore, Right = gameStateTracker.rightScore });

                    return new MightBeCollision();
                }
            }

            public IEvent GetGoalEvent(double time)
            {
                return new UpdateScoreEvent(gameStateTracker, onUpdateScore, right, time);
            }

            public bool IsEnabled()
            {
                return gameStateTracker.CanScore();
            }
        }

        public void NameChanged(NameChanged nameChanged)
        {
            foreach (var element in bodiesCreated)
            {
                if (element.Id == nameChanged.Id)
                {
                    element.Name = nameChanged.Name;
                }
            }
        }

        private class Node {
            public readonly Positions positions;
            public readonly TaskCompletionSource<Node> next = new TaskCompletionSource<Node>();

            public Node(Positions positions)
            {
                this.positions = positions;
            }
        }
        private Node lastPositions = new Node(new Positions());

        private class What : IAsyncEnumerable<Positions>, IAsyncEnumerator<Positions>
        {
            private Node node;

            public What(Node node)
            {
                this.node = node;
            }

            public Positions Current
            {
                get;private set;
            }

            public ValueTask DisposeAsync()
            {
                return new ValueTask();
            }

            public IAsyncEnumerator<Positions> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return this;
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                node = await node.next.Task;
                Current = node.positions;
                return true;
            }
        }

        public IAsyncEnumerable<Positions> GetReader()
        {
            // the other option is a new channel every time 
            // and we just write positions to them all
            // that might be better...

            // by better I mean faster
            // this saves me having to track a stack of channels
            return new What(lastPositions);
        }

        public void ColorChanged(ColorChanged colorChanged)
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

        public UpdateScore Reset()
        {
            gameStateTracker.Scored();
            gameStateTracker.leftScore = 0;
            gameStateTracker.rightScore = 0;
            return new UpdateScore() { Left = gameStateTracker.leftScore, Right = gameStateTracker.rightScore };
        }

        public Game(Action<UpdateScore> onUpdateScore)
        {

            if (onUpdateScore == null)
            {
                throw new ArgumentNullException(nameof(onUpdateScore));
            }


            ballId = Guid.NewGuid();

            ball = new Ball(Constants.BallMass, Constants.xMax / 2, Constants.yMax / 2, true, new Circle(Constants.BallRadius));
            
            ballCreated = new BallCreated(
               ball.X,
               ball.Y,
               Constants.ballZ,
               ballId,
               Constants.BallRadius * 2,
               0,
               0,
               0,
               255);

            var leftGoalId = Guid.NewGuid();
            var leftGoal = new Ball(1, (Constants.footLen * 3), Constants.yMax / 2.0, false, new Circle(Constants.footLen));
           

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

            var rightGoal = new Ball(1, Constants.xMax - (Constants.footLen * 3), Constants.yMax / 2.0, false, new Circle(Constants.footLen));
            
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

            var points = new[] {
                (new Vector(0,0) ,new Vector(Constants.xMax-1,0)),
                (new Vector(0,Constants.yMax-1 ) ,new Vector(0,0)),
                (new Vector(Constants.xMax-1,Constants.yMax-1),new Vector(0,Constants.yMax-1)),
                (new Vector(Constants.xMax-1,Constants.yMax-1),new Vector(Constants.xMax-1,0)),
            };

            gameStateTracker = new GameStateTracker(
                ball.Reset,
                Constants.xMax / 2.0,
                Constants.yMax / 2.0
            );

            var rightGoalManger = new GoalManager(gameStateTracker, onUpdateScore, true);
            var leftGoalManger = new GoalManager(gameStateTracker, onUpdateScore, false);

            physicsEngine.Run(x =>
            {
                x.SetBall(ball);
                x.AddGoal(leftGoal,leftGoalManger);
                x.AddGoal(rightGoal, rightGoalManger);
                foreach (var side in points)
                {
                    var line = new Line(side.Item1, side.Item2);
                    var pos = side.Item1.NewAdded(side.Item2).NewScaled(.5);
                    // the position on this is pretty wierd
                    // pretty sure it is not used
                    var linePhysicObject = new PhysicsObjectWithFixedLine(0,line, false);
                    x.AddWall(linePhysicObject);
                }
                return x;
            });
        }

        public ObjectsCreated GetObjectsCreated()
        {
            return new ObjectsCreated(feetCreaated.ToArray(), bodiesCreated.ToArray(), ballCreated, goalsCreated.ToArray());
        }

        public ObjectsCreated CreatePlayer(string connectionId, CreatePlayer createPlayer)
        {
            double startX = 400;
            double startY = 400;
            var foot = new Player(1,  startX, startY, true, Constants.PlayerRadius*2,Constants.playerPadding);

            physicsEngine.Run(x => { x.AddPlayer(foot); return x; });
            feet[createPlayer.Foot] = foot;

            var body = new Center(
                startX,
                startY,
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
                    createPlayer.Name);
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

        public bool TryDisconnect(string connectionId, out List<ObjectRemoved> objectRemoveds)
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

        private int simulationTime = 0;
        private void Apply()
        {
            Positions positions = default;

            var myPlayersInputs = Interlocked.Exchange(ref playersInputs, new ConcurrentLinkedList<PlayerInputs>());

            var frames = new List<Dictionary<Guid, PlayerInputs>>();

            foreach (var input in myPlayersInputs)
            {
                foreach (var frame in frames)
                {
                    if (!frame.ContainsKey(input.BodyId))
                    {
                        frame.Add(input.BodyId, input);
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

                if (ball.Velocity.Length > 0)
                {
                    var friction = ball.Velocity.NewUnitized().NewScaled(-ball.Velocity.Length * ball.Velocity.Length * ball.Mass / 4000.0);

                    ball.ApplyForce(
                        friction.x,
                        friction.y);

                }

                if (ball.Velocity.Length > .03)
                {
                    var friction = ball.Velocity.NewUnitized().NewScaled(-.03);

                    ball.ApplyForce(
                        friction.x,
                        friction.y);
                }
                else {
                    ball.ApplyForce(
                        -ball.Velocity.x,
                        -ball.Velocity.y);
                }


                //ball.ApplyForce(
                //    (-ball.Vx *Math.Pow( 10/(10+ Math.Abs(ball.Vx)),2) * ball.Mass) ,
                //    (-ball.Vy * Math.Pow(10 / (10 + Math.Abs(ball.Vy)),2) * ball.Mass));

                //ball.ApplyForce(
                //    -Math.Sign(ball.Vx) *Math.Min(.02,Math.Abs(ball.Vx)),
                //    -Math.Sign(ball.Vy) * Math.Min(.02,Math.Abs(ball.Vy)));

                foreach (var center in bodies)
                {
                    var body = center.Value;
                    var lastX = body.X;
                    var lastY = body.Y;

                    var lastVx = body.vx;
                    var lastVy = body.vy;

                    if (inputSet.TryGetValue(center.Key, out var input))
                    {


                        // crush oppozing forces
                        if (input.BodyX != 0 || input.BodyY != 0)
                        {
                            var v = new Vector(body.vx, body.vy);
                            var f = new Vector(Math.Sign(input.BodyX), Math.Sign(input.BodyY));
                            var with = v.Dot(f) / f.Length;
                            if (with <= 0)
                            {
                                body.ApplyForce(-body.vx, -body.vy);
                            }
                            else
                            {
                                var withVector = f.NewUnitized().NewScaled(with);
                                var notWith = v.NewAdded(withVector.NewScaled(-1));
                                var notWithScald = notWith.Length > withVector.Length ? notWith.NewUnitized().NewScaled(with) : notWith;
                                

                                body.ApplyForce(-body.vx + withVector.x + notWithScald.x, -body.vy + withVector.y + notWithScald.y);
                            }

                            var damp =.98;

                            var R0 = EInverse(E(Math.Sqrt(Math.Pow(body.vx, 2) + Math.Pow(body.vy, 2))) + EnergyAdd);
                            var a = Math.Pow(Math.Sign(input.BodyX), 2) + Math.Pow(Math.Sign(input.BodyY), 2);
                            var b = 2 * ((Math.Sign(input.BodyX) * body.vx* damp) + (Math.Sign(input.BodyY) * body.vy* damp));
                            var c = Math.Pow(body.vx* damp, 2) + Math.Pow(body.vy* damp, 2) - Math.Pow(R0, 2);

                            var t = (-b + Math.Sqrt(Math.Pow(b,2) - (4 * a * c))) / (2 * a);

                            body.ApplyForce(-(1-damp)*body.vx, -(1 - damp) * body.vy);
                            body.ApplyForce(t* input.BodyX, t * input.BodyY);
                        }
                        else {
                            body.ApplyForce(-body.vx,-body.vy);
                        }


                        body.Update(gameStateTracker.TryGetBallWall(out var tup), tup);

                        var foot = body.Foot;

                        // apply whatever force was applied to that body
                        foot.ApplyForce(
                            (body.vx - lastVx) * foot.Mass,
                            (body.vy - lastVy) * foot.Mass);


                        var max = Constants.footLen - Constants.PlayerRadius - Constants.playerPadding;


                        var target = input.Controller ?
                            new Vector(input.FootX*(Constants.footLen- Constants.PlayerRadius), input.FootY * (Constants.footLen - Constants.PlayerRadius))
                            :new Vector(foot.X + input.FootX - lastX, foot.Y + input.FootY - lastY);

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
                    collisions = x.Simulate();
                    return x;
                });


                positions = new Positions(GetPosition().ToArray(), simulationTime, countDownSate, collisions);
            }
            var next = new Node(positions);
            lastPositions.next.SetResult(next);
            lastPositions = next;
        }

        private int running = 0;

        private double E(double d) => Math.Pow(d,3.0);
        private double EInverse(double d) => Math.Pow(d,1/3.0);

        public void PlayerInputs(PlayerInputs playerInputs)
        {
            var lastLast = LastInputUTC;
            playersInputs.Add(playerInputs);
            while (playersInputs.Count > (players - 1.0) && Interlocked.CompareExchange(ref running, 1, 0) == 0)
            {
                Apply();
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