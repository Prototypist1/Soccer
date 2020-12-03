using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Common;
using physics2;
using Physics2;
using Prototypist.TaskChain;

namespace Common
{
    public class Game
    {
        private int players = 0;

        private JumpBallConcurrent<PhysicsEngine> physicsEngine;

        // maybe lock feed and bodies at the same time?
        // a signal jumpball
        private readonly JumpBallConcurrent<Dictionary<Guid, Player>> feet = new JumpBallConcurrent<Dictionary<Guid, Player>>(new Dictionary<Guid, Player>());
        private readonly JumpBallConcurrent<Dictionary<Guid, Center>> bodies = new JumpBallConcurrent<Dictionary<Guid, Center>>(new Dictionary<Guid, Center>());
        private Guid ballId;
        private Ball ball;

        private readonly ConcurrentSet<FootCreated> feetCreaated = new ConcurrentSet<FootCreated>();
        private readonly ConcurrentSet<BodyCreated> bodiesCreated = new ConcurrentSet<BodyCreated>();
        //private readonly ConcurrentSet<OuterCreated> bodyNoLeansCreated = new ConcurrentSet<OuterCreated>();

        private BallCreated ballCreated;
        private readonly ConcurrentSet<GoalCreated> goalsCreated = new ConcurrentSet<GoalCreated>();

        private class ConnectionStuff {
            public List<ObjectCreated> objectsCreated;

            public ConnectionStuff(List<ObjectCreated> objectsCreated, Guid body, Player foot)
            {
                this.objectsCreated = objectsCreated ?? throw new ArgumentNullException(nameof(objectsCreated));
                Body = body;
                Foot = foot;
            }

            public Guid Body { get; }
            public Player Foot { get; }
        }


        private readonly ConcurrentIndexed<string, ConnectionStuff> connectionObjects = new ConcurrentIndexed<string, ConnectionStuff>();

        private ConcurrentDictionary<Guid, ConcurrentLinkedList<PlayerInputs>> playersInputs = new
            ConcurrentDictionary<Guid, ConcurrentLinkedList<PlayerInputs>>();
        //ConcurrentLinkedList<PlayerInputs>();
        public DateTime LastInputUTC { get; private set; } = DateTime.Now;



        private GameStateTracker gameStateTracker;
        private FieldDimensions field;

        public class GameStateTracker
        {
            public int leftScore = 0, rightScore = 0;
            private readonly Action<double, double> resetBallAction;
            private double ballStartX, ballStartY;
            private readonly double maxX;
            private readonly double minX;
            private readonly double maxY;
            private readonly double minY;
            private readonly Random random = new Random();

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
                    resetBallAction(ballStartX, ballStartY);
                }
                if (gameState >= startCountDown && gameState < endCountDown)
                {
                    res.Countdown = true;
                    if (TryGetBallWall(out var tuple))
                    {
                        res.StrokeThickness = tuple.radius - (Constants.footLen * (gameState - startCountDown) / ((double)(endCountDown - startCountDown)));
                        res.Radius = tuple.radius;
                        res.BallOpacity = gameState > resetBall ? (gameState - resetBall) / (double)(endCountDown - resetBall) : ((resetBall - startCountDown) - (gameState - startCountDown)) / (double)(resetBall - startCountDown);
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
            private const int stopGrowingCircle = 100;
            private const int resetBall = 100;
            private const int startCountDown = 0;
            private const int endCountDown = 600;

            private int gameState = play;

            public GameStateTracker(Action<double, double> resetBallAction, double maxX, double minX, double maxY, double minY)
            {
                this.resetBallAction = resetBallAction ?? throw new ArgumentNullException(nameof(resetBallAction));
                this.maxX = maxX;
                this.minX = minX;
                this.maxY = maxY;
                this.minY = minY;
            }

            public bool CanScore() => gameState == play;

            public void Scored()
            {
                gameState = 1;
                this.ballStartX = random.NextDouble() * (maxX - minX) + minX;
                this.ballStartY = random.NextDouble() * (maxY - minY) + minY;
            }

            public bool TryGetBallWall(out (double x, double y, double radius) ballWall)
            {
                if (gameState >= startGrowingCircle)
                {
                    ballWall = (
                        ballStartX,
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
                private readonly Collision collision;

                public UpdateScoreEvent(GameStateTracker gameStateTracker, Action<UpdateScore> onUpdateScore, bool right, double time, Collision collision)
                {
                    this.gameStateTracker = gameStateTracker ?? throw new ArgumentNullException(nameof(gameStateTracker));
                    this.onUpdateScore = onUpdateScore ?? throw new ArgumentNullException(nameof(onUpdateScore));
                    this.right = right;
                    Time = time;
                    this.collision = collision;
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

                    return new MightBeCollision(collision);
                }
            }

            public IEvent GetGoalEvent(double time, Collision collision)
            {
                return new UpdateScoreEvent(gameStateTracker, onUpdateScore, right, time, collision);
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

        private class Node
        {
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
                get; private set;
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
            //foreach (var element in bodyNoLeansCreated)
            //{
            //    if (element.Id == colorChanged.Id)
            //    {
            //        element.G = colorChanged.G;
            //        element.R = colorChanged.R;
            //        element.B = colorChanged.B;
            //        element.A = colorChanged.A;
            //    }
            //}
        }

        public UpdateScore Reset()
        {
            gameStateTracker.Scored();
            gameStateTracker.leftScore = 0;
            gameStateTracker.rightScore = 0;
            ball.OwnerOrNull = null;
            return new UpdateScore() { Left = gameStateTracker.leftScore, Right = gameStateTracker.rightScore };
        }

        public void Init(Action<UpdateScore> onUpdateScore, FieldDimensions field)
        {

            if (onUpdateScore == null)
            {
                throw new ArgumentNullException(nameof(onUpdateScore));
            }


            ballId = Guid.NewGuid();

            ball = new Ball(Constants.BallMass, field.xMax / 2, field.yMax / 2, true, new Circle(Constants.BallRadius));

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
            var leftGoal = new Ball(1, 0, field.yMax / 2.0, false, new Circle(Constants.goalLen));


            try
            {
                goalsCreated.AddOrThrow(new GoalCreated(
               leftGoal.X,
               leftGoal.Y,
               Constants.goalZ,
               leftGoalId,
               Constants.goalLen * 2,
               0xee,
               0xee,
               0xee,
               0xff));
            }
            catch (Exception)
            {

            }

            var rightGoalId = Guid.NewGuid();

            var rightGoal = new Ball(1, field.xMax, field.yMax / 2.0, false, new Circle(Constants.goalLen));

            goalsCreated.AddOrThrow(new GoalCreated(
               rightGoal.X,
               rightGoal.Y,
               Constants.goalZ,
               rightGoalId,
               Constants.goalLen * 2,
               0xee,
               0xee,
               0xee,
               0xff));


            var points = new[] {
                (new Vector(0,0) ,new Vector(field.xMax-1,0)),
                (new Vector(0,field.yMax-1 ) ,new Vector(0,0)),
                (new Vector(field.xMax-1,field.yMax-1),new Vector(0,field.yMax-1)),
                (new Vector(field.xMax-1,field.yMax-1),new Vector(field.xMax-1,0)),
            };

            gameStateTracker = new GameStateTracker(
                ball.Reset,
                (field.xMax / 2.0) + Constants.footLen,
                (field.xMax / 2.0) - Constants.footLen,
                field.yMax - Constants.footLen,
                Constants.footLen);

            physicsEngine = new JumpBallConcurrent<PhysicsEngine>(new PhysicsEngine(gameStateTracker));// 

            var rightGoalManger = new GoalManager(gameStateTracker, onUpdateScore, true);
            var leftGoalManger = new GoalManager(gameStateTracker, onUpdateScore, false);

            try
            {
                physicsEngine.Run(x =>
                {
                    x.SetBall(ball);
                    x.AddGoal(leftGoal, leftGoalManger);
                    x.AddGoal(rightGoal, rightGoalManger);
                    foreach (var side in points)
                    {
                        var line = new Line(side.Item1, side.Item2);
                        var pos = side.Item1.NewAdded(side.Item2).NewScaled(.5);
                        // the position on this is pretty wierd
                        // pretty sure it is not used
                        var linePhysicObject = new PhysicsObjectWithFixedLine(1, line, false);
                        x.AddWall(linePhysicObject);
                    }
                    return x;
                });

            }
            catch (Exception e) {
                var db = 0;
            }
            this.field = field;
        }

        public ObjectsCreated GetObjectsCreated()
        {
            return new ObjectsCreated(
                feetCreaated.ToArray(),
                bodiesCreated.ToArray(),
                ballCreated,
                goalsCreated.ToArray(),
                //bodyNoLeansCreated.ToArray(),
                gameStateTracker.leftScore,
                gameStateTracker.rightScore);
        }

        public ObjectsCreated CreatePlayer(string connectionId, CreatePlayer createPlayer)
        {
            var random = new Random();

            double startX = random.NextDouble() * field.xMax;
            double startY = random.NextDouble() * field.yMax;

            var foot = new Player(startX, startY, Constants.PlayerRadius, createPlayer.Foot);

            var body = new Center(
                startX,
                startY,
                foot,
                //createPlayer.BodyDiameter / 2.0,
                createPlayer.Body
                );

            foot.Body = body;

            var externalForce = new ExternalForce() {
                X = startX,
                Y = startY
            };

            var outer = new Outer(
                startX,
                startY,
                createPlayer.Outer,
                externalForce);

            body.Outer = outer;

            try
            {
                physicsEngine.Run(x => { x.AddPlayer(foot); return x; });
            }
            catch (Exception e) {
                var db = 0;
            }

            feet.Run(x => { x[createPlayer.Foot] = foot; return x; });


            bodies.Run(x => { x[createPlayer.Body] = body; return x; });

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


            //var bodyNoLeanCreated = new OuterCreated(
            //            body.Outer.X,
            //            body.Outer.Y,
            //            Constants.bodyZ,
            //            createPlayer.Outer,
            //            createPlayer.BodyDiameter + (Constants.MaxLean*2),
            //            createPlayer.BodyR,
            //            createPlayer.BodyG,
            //            createPlayer.BodyB,
            //            (byte)((int)createPlayer.BodyA/2)
            //            );

            //bodyNoLeansCreated.AddOrThrow(bodyNoLeanCreated);

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

            connectionObjects.AddOrThrow(connectionId, new ConnectionStuff(new List<ObjectCreated>(){
                bodyCreated,
                footCreated ,
                //bodyNoLeanCreated
            },
            createPlayer.Body,
            foot));


            Interlocked.Add(ref players, 1);

            return new ObjectsCreated(
                new[] { footCreated },
                new[] { bodyCreated },
                null,
                new GoalCreated[] { },
                //new [] { bodyNoLeanCreated },
                gameStateTracker.leftScore,
                gameStateTracker.rightScore);
        }

        public bool TryDisconnect(string connectionId, out List<ObjectRemoved> objectRemoveds)
        {
            if (connectionObjects.TryRemove(connectionId, out var toRemoves))
            {
                Interlocked.Add(ref players, -1);

                bodies.Run(x => { x.Remove(toRemoves.Body); return x; });

                feet.Run(x => { x.Remove(toRemoves.Foot.id); return x; });
                physicsEngine.Run(x => { x.RemovePlayer(toRemoves.Foot); return x; });


                playersInputs.TryRemove(toRemoves.Foot.id, out var _);


                objectRemoveds = new List<ObjectRemoved>();
                foreach (var item in toRemoves.objectsCreated)
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

                    //var bodyNoLean = bodyNoLeansCreated.SingleOrDefault(x => x.Id == item.Id);
                    //if (bodyNoLean != null)
                    //{
                    //    bodyNoLeansCreated.RemoveOrThrow(bodyNoLean);
                    //    objectRemoveds.Add(new ObjectRemoved(item.Id));
                    //}
                }
                return true;
            }
            objectRemoveds = default;
            return false;
        }

        private int simulationTime = 0;

        private IEnumerable<Dictionary<Guid, PlayerInputs>> playersInputsSpool()
        {
            while (playersInputs.All(x => x.Value.Any()) || playersInputs.Any(x => x.Value.Count > 2))
            {
                var res =new  Dictionary<Guid, PlayerInputs>();
                foreach (var item in playersInputs)
                {
                    var innerRes = item.Value.First();
                    item.Value.RemoveStart();
                    res[innerRes.BodyId] = innerRes;
                }
                yield return res;
            }
        }

        //private IEnumerable<PlayerInputs> OneSetSpool() {
        //    foreach (var item in playersInputs)
        //    {
        //        var res = item.Value.First();
        //        item.Value.RemoveStart();
        //        yield return res;
        //    }
        //}

        private void Apply()
        {
            Positions positions = default;
            foreach (var inputSet in playersInputsSpool())
            {
                var countDownSate = gameStateTracker.UpdateGameState();

                if (ball.OwnerOrNull == null) {

                    if (ball.Velocity.Length > 0)
                    {
                        var friction = ball.Velocity.NewUnitized().NewScaled(-ball.Velocity.Length * ball.Velocity.Length * ball.Mass / (175.0 * 175));

                        ball.ApplyForce(
                            friction.x,
                            friction.y);

                    }

                    if (ball.Velocity.Length > 0)
                    {
                        var friction = ball.Velocity.NewUnitized().NewScaled(-ball.Velocity.Length * ball.Mass / Constants.FrictionDenom);

                        ball.ApplyForce(
                            friction.x,
                            friction.y);

                    }

                    //if (ball.Velocity.Length > 1)
                    //{
                    //    var friction = ball.Velocity.NewUnitized().NewScaled(-1);

                    //    ball.ApplyForce(
                    //        friction.x,
                    //        friction.y);
                    //}
                    //else
                    //{
                    //    ball.ApplyForce(
                    //        -ball.Velocity.x,
                    //        -ball.Velocity.y);
                    //}
                }


                //ball.ApplyForce(
                //    (-ball.Vx *Math.Pow( 10/(10+ Math.Abs(ball.Vx)),2) * ball.Mass) ,
                //    (-ball.Vy * Math.Pow(10 / (10 + Math.Abs(ball.Vy)),2) * ball.Mass));

                //ball.ApplyForce(
                //    -Math.Sign(ball.Vx) *Math.Min(.02,Math.Abs(ball.Vx)),
                //    -Math.Sign(ball.Vy) * Math.Min(.02,Math.Abs(ball.Vy)));
                try
                {
                    KeyValuePair<Guid,Center>[] itterate= null;
                    bodies.Run(x => { itterate = x.ToArray(); return x; });

                    foreach (var center in itterate)
                    {
                        var body = center.Value;
                        var lastX = body.X;
                        var lastY = body.Y;

                        var lastVx = body.Vx;
                        var lastVy = body.Vy;


                        var foot = body.Foot;

                        var outer = body.Outer;

                        var externalForces = center.Value.Outer.externalForce;

                        externalForces.Vy = externalForces.Vy * .8;
                        externalForces.Vx = externalForces.Vx * .8;

                        if (inputSet.TryGetValue(center.Key, out var input))
                        {

                            if (foot.Throwing && !input.Throwing && ball.OwnerOrNull == foot)
                            {
                                foot.ForceThrow = true;
                            }
                            else
                            {
                                foot.ForceThrow = false;
                            }

                            foot.Throwing = input.Throwing;

                            if (input.ControlScheme == ControlScheme.SipmleMouse)
                            {

                                var dx = input.BodyX - body.X;
                                var dy = input.BodyY - body.Y;

                                if (dx != 0 || dy != 0)
                                {

                                    // base velocity becomes the part of the velocity in the direction of the players movement
                                    var v = new Vector(body.Outer.privateVx, body.Outer.privateVy);
                                    var f = new Vector(dx, dy).NewUnitized();
                                    var with = v.Dot(f);
                                    var baseValocity = with > 0 ? f.NewUnitized().NewScaled(with) : new Vector(0, 0);

                                    var engeryAdd = foot == ball.OwnerOrNull ? Constants.EnergyAdd / 2.0 : Constants.EnergyAdd;

                                    //
                                    var finalE = E(Math.Sqrt(Math.Pow(baseValocity.x, 2) + Math.Pow(baseValocity.y, 2))) + engeryAdd;
                                    var inputAmount = new Vector(input.BodyX, input.BodyY).Length;
                                    if (inputAmount < .1)
                                    {
                                        finalE = 0;
                                    }
                                    else if (inputAmount < 1)
                                    {
                                        finalE = Math.Min(finalE, (inputAmount - .1) * (inputAmount - .1) * engeryAdd * 100);
                                    }

                                    var finalSpeed = EInverse(finalE);
                                    var finalVelocity = f.NewScaled(finalSpeed);

                                    if (finalVelocity.Length > new Vector(dx, dy).Length)
                                    {
                                        finalVelocity = finalVelocity.NewUnitized().NewScaled(new Vector(dx, dy).Length);
                                    }

                                    body.Outer.privateVx = finalVelocity.x;// / 2.0;
                                    body.Outer.privateVy = finalVelocity.y;// / 2.0;
                                }
                                else {
                                    body.Outer.privateVx = 0;
                                    body.Outer.privateVy = 0;
                                }
                            }
                            else 
                            if (input.BodyX != 0 || input.BodyY != 0)
                            {
                                // crush oppozing forces
                                if (input.ControlScheme == ControlScheme.MouseAndKeyboard)
                                {
                                    var v = new Vector(outer.privateVx, outer.privateVy);
                                    var f = new Vector(Math.Sign(input.BodyX), Math.Sign(input.BodyY));
                                    var with = v.Dot(f) / f.Length;
                                    if (with <= 0)
                                    {
                                        outer.privateVx = 0;
                                        outer.privateVy = 0;
                                    }
                                    else
                                    {
                                        var withVector = f.NewUnitized().NewScaled(with);
                                        var notWith = v.NewAdded(withVector.NewScaled(-1));
                                        var notWithScald = notWith.Length > withVector.Length ? notWith.NewUnitized().NewScaled(with) : notWith;

                                        outer.privateVx = withVector.x + notWithScald.x;
                                        outer.privateVy = withVector.y + notWithScald.y;
                                        //outer.ApplyForce(-outer.privateVx + withVector.x + notWithScald.x, -outer.privateVy + withVector.y + notWithScald.y);
                                    }


                                    var damp = .98;


                                    var engeryAdd = foot == ball.OwnerOrNull ? Constants.EnergyAdd / 2.0 : Constants.EnergyAdd;

                                    var R0 = EInverse(E(Math.Sqrt(Math.Pow(outer.privateVx, 2) + Math.Pow(outer.privateVy, 2))) + engeryAdd);
                                    var a = Math.Pow(Math.Sign(input.BodyX), 2) + Math.Pow(Math.Sign(input.BodyY), 2);
                                    var b = 2 * ((Math.Sign(input.BodyX) * outer.privateVx * damp) + (Math.Sign(input.BodyY) * outer.privateVy * damp));
                                    var c = Math.Pow(outer.privateVx * damp, 2) + Math.Pow(outer.privateVy * damp, 2) - Math.Pow(R0, 2);

                                    var t = (-b + Math.Sqrt(Math.Pow(b, 2) - (4 * a * c))) / (2 * a);

                                    outer.privateVx = (damp * outer.privateVx) + (t * input.BodyX);// / 2.0;
                                    outer.privateVy = (damp * outer.privateVy) + (t * input.BodyY);// / 2.0;
                                }
                                else if (input.ControlScheme == ControlScheme.Controller)
                                {


                                    // base velocity becomes the part of the velocity in the direction of the players movement
                                    var v = new Vector(body.Outer.privateVx, body.Outer.privateVy);
                                    var f = new Vector(input.BodyX, input.BodyY).NewUnitized();
                                    var with = v.Dot(f);
                                    var baseValocity = with > 0 ? f.NewUnitized().NewScaled(with) : new Vector(0, 0);

                                    var engeryAdd = foot == ball.OwnerOrNull ? Constants.EnergyAdd / 2.0 : Constants.EnergyAdd;

                                    //
                                    var finalE = E(Math.Sqrt(Math.Pow(baseValocity.x, 2) + Math.Pow(baseValocity.y, 2))) + engeryAdd;
                                    var inputAmount = new Vector(input.BodyX, input.BodyY).Length;
                                    if (inputAmount < .1)
                                    {
                                        finalE = 0;
                                    }
                                    else if (inputAmount < 1)
                                    {
                                        finalE = Math.Min(finalE, (inputAmount - .1) * (inputAmount - .1) * engeryAdd * 100);
                                    }

                                    var finalSpeed = EInverse(finalE);
                                    var finalVelocity = f.NewScaled(finalSpeed);


                                    var vector = new Vector(finalVelocity.x - body.Outer.privateVx, finalVelocity.y - body.Outer.privateVy);

                                    body.Outer.privateVx += vector.x;// / 2.0;
                                    body.Outer.privateVy += vector.y;// / 2.0;

                                }
                            }
                            else
                            {
                                var vector = new Vector(-outer.privateVx, -outer.privateVy);

                                body.Outer.privateVx += vector.x;// / 2.0;
                                body.Outer.privateVy += vector.y;// / 2.0;

                            }


                            
                            //if (input.Controller == ControlScheme.Controller)
                            //{
                            //    var tx = (input.BodyX * Constants.MaxLean) + body.Outer.X;
                            //    var ty = (input.BodyY * Constants.MaxLean) + body.Outer.Y;

                            //    var vector = new Vector(tx - body.personalVx, ty - body.personalVy);

                            //    body.personalVx = (tx - body.X);// /2.0;
                            //    body.personalVy = (ty - body.Y);// /2.0;
                            //}



                            var max = Constants.footLen - Constants.PlayerRadius;// - Constants.PlayerRadius;

                            if (input.ControlScheme == ControlScheme.Controller)
                            {
                                var tx = (input.FootX * max) + body.X;
                                var ty = (input.FootY * max) + body.Y;

                                var vector = new Vector(tx - foot.personalVx, ty - foot.personalVy);


                                var v = new Vector(tx - foot.X, ty - foot.Y);

                                var len = v.Length;
                                if (len != 0)
                                {
                                    var speedLimit = SpeedLimit(len);
                                    v = v.NewUnitized().NewScaled(speedLimit);
                                }
                                foot.personalVx = v.x;//(tx - foot.X);// /2.0;
                                foot.personalVy = v.y;//(ty - foot.Y);// / 2.0;
                            }
                            else if (input.ControlScheme == ControlScheme.MouseAndKeyboard)
                            {

                                var tx = (input.FootX) + foot.X;
                                var ty = (input.FootY) + foot.Y;


                                var dx = tx - body.X;
                                var dy = ty - body.Y;

                                var d = new Vector(dx, dy);

                                if (d.Length > max)
                                {
                                    d = d.NewUnitized().NewScaled(max);
                                }

                                var validTx = d.x + body.X;
                                var validTy = d.y + body.Y;

                                var vx = validTx - foot.X;
                                var vy = validTy - foot.Y;

                                var v = new Vector(vx, vy);

                                // there is a speed limit things moving too fast are bad for online play
                                // you can get hit before you have time to respond 
                                var len = v.Length;
                                if (len != 0)
                                {
                                    var speedLimit = SpeedLimit(len);
                                    v = v.NewUnitized().NewScaled(speedLimit);
                                }
                                foot.personalVx = v.x;//(tx - foot.X);// /2.0;
                                foot.personalVy = v.y;//(ty - foot.Y);// / 2.0;
                            } else if (input.ControlScheme == ControlScheme.SipmleMouse) {

                                var dx = input.FootX - body.X;
                                var dy = input.FootY - body.Y;

                                var d = new Vector(dx, dy);

                                if (d.Length > max)
                                {
                                    d = d.NewUnitized().NewScaled(max);
                                }

                                var validTx = d.x + body.X;
                                var validTy = d.y + body.Y;

                                var vx = validTx - foot.X;
                                var vy = validTy - foot.Y;

                                var v = new Vector(vx, vy);

                                // simple mouse drive the speed limit when it needs to
                                var len = v.Length;
                                if (len > Constants.speedLimit)
                                {
                                    v = v.NewUnitized().NewScaled(Constants.speedLimit);
                                }
                                foot.personalVx = v.x;//(tx - foot.X);// /2.0;
                                foot.personalVy = v.y;//(ty - foot.Y);// / 2.0;
                            }

                        }

                        // handle throwing
                        {

                            var dx = foot.X - ball.X;
                            var dy = foot.Y - ball.Y;

                            if (foot == ball.OwnerOrNull)
                            {
                                ball.UpdateVelocity(ball.OwnerOrNull.Vx + dx, ball.OwnerOrNull.Vy + dy);
                            }


                            var throwV = new Vector(foot.Vx - body.Vx, foot.Vy - body.Vy);



                            // I think force throw is making throwing harder
                            if (foot.ForceThrow && foot == ball.OwnerOrNull)
                            {

                                var newPart = 1;//Math.Max(1, throwV.Length);
                                var oldPart = 2;// Math.Max(1, ball.proposedThrow.Length);

                                foot.proposedThrow = new Vector(
                                        ((throwV.x * newPart) + (foot.proposedThrow.x * oldPart)) / (newPart + oldPart),
                                        ((throwV.y * newPart) + (foot.proposedThrow.y * oldPart)) / (newPart + oldPart));

                                // throw the ball!
                                ball.UpdateVelocity(foot.proposedThrow.x + body.Vx, foot.proposedThrow.y + body.Vy);
                                ball.OwnerOrNull.LastHadBall = simulationTime;
                                ball.OwnerOrNull = null;
                                foot.ForceThrow = false;
                                foot.proposedThrow = new Vector();
                            }
                            else 
                            if (foot.Throwing)
                            {

                                if (foot.proposedThrow.Length > Constants.MimimunThrowingSpped && (throwV.Length * 1.3 < foot.proposedThrow.Length || throwV.Dot(foot.proposedThrow) < 0) && foot.Throwing && foot == ball.OwnerOrNull)
                                    {
                                        // throw the ball!
                                        ball.UpdateVelocity(foot.proposedThrow.x + body.Vx, foot.proposedThrow.y + body.Vy);
                                        ball.OwnerOrNull.LastHadBall = simulationTime;
                                        ball.OwnerOrNull = null;
                                        foot.ForceThrow = false;
                                    foot.proposedThrow = new Vector();
                                }
                                else if (foot.proposedThrow.Length == 0)
                                {
                                    foot.proposedThrow = new Vector(throwV.x, throwV.y);
                                }
                                else 
                                {
                                    var newPart = 1;//Math.Max(1, throwV.Length);
                                    var oldPart = 4;// Math.Max(1, ball.proposedThrow.Length);
                                    foot.proposedThrow = new Vector(
                                                ((throwV.x * newPart) + (foot.proposedThrow.x * oldPart)) / (newPart + oldPart),
                                                ((throwV.y * newPart) + (foot.proposedThrow.y * oldPart)) / (newPart + oldPart));
                                }
                            }
                            else
                            {
                                foot.proposedThrow = new Vector();
                            }
                        }
                    }
                }
                catch (Exception e) {
                    var db = 0;
                }

                simulationTime++;


                Collision[] collisions = null;

                try
                {
                    physicsEngine.Run(x =>
                    {
                        collisions = x.Simulate(simulationTime);
                        return x;
                    });
                }
                catch (Exception e) {
                    var db = 0;
                }


                positions = new Positions(GetPosition().ToArray(), new Preview[] { }, simulationTime, countDownSate, collisions);
            }

            if (positions.PositionsList.Length != 0)
            {
                var next = new Node(positions);
                lastPositions.next.SetResult(next);
                lastPositions = next;
            }
        }

        private int running = 0;
 

        private double E(double v) //=> Math.Pow(Math.Max(0, v - Constants.Add), Constants.ToThe) * Constants.SpeedScale;
        {
            var simpleV = Math.Max(0, v - Constants.bodyStartAt);

            var distanceToTop = ((Constants.bodySpeedLimit - Constants.bodyStartAt) - simpleV)/ (Constants.bodySpeedLimit - Constants.bodyStartAt);

            var eDistanceToTop = Math.Pow(Math.Max(0,distanceToTop), .5);

            return ((1 - eDistanceToTop) * (Constants.bodySpeedLimit - Constants.bodyStartAt)) + Constants.bodyStartAt;
        }
        private double EInverse(double e) //=> Math.Pow(e / Constants.SpeedScale, 1 / Constants.ToThe) + Constants.Add;
        {
            var p2 = Math.Min(1, (e- Constants.bodyStartAt) / (Constants.bodySpeedLimit- Constants.bodyStartAt));
            var p1 = 1 - p2;

            return Constants.bodyStartAt + ((e - Constants.bodyStartAt) * p1) + ((Constants.bodySpeedLimit - Constants.bodyStartAt) * p2);
        }
        private double SpeedLimit(double d) {

            var p2 = Math.Min(1, d / Constants.speedLimit);
            var p1 = 1 - p2;

            return (d * p1) + (Constants.speedLimit * p2);


        }

        // boxed guid

        private class RefGuid {
            public readonly Guid id;

            public RefGuid(Guid id)
            {
                this.id = id;
            }
        }
        //RefGuid FirstPlayerFoot;
        public void PlayerInputs(PlayerInputs playerInputs)
        {
            //var lastLast = LastInputUTC;
            playersInputs.GetOrAdd(playerInputs.FootId, new ConcurrentLinkedList<PlayerInputs>()).Add(playerInputs);


            if (Interlocked.CompareExchange(ref running, 1, 0) == 0)
            {
                Apply();
                running = 0;
            }
        }

        public void SetPositionsAndClearInputes(Position[] positions) {

            bodies.Run(x =>
            {
                var outerLookUp = x.ToDictionary(y => y.Value.Outer.Id, y => y.Value.Outer);

                foreach (var position in positions)
                {
                    if (outerLookUp.TryGetValue(position.Id, out var outer))
                    {
                        outer.X = position.X;
                        outer.Y = position.Y;
                        outer.privateVx = position.Vx;
                        outer.privateVy = position.Vy;
                    }
                }

                foreach (var position in positions)
                {
                    if (x.TryGetValue(position.Id, out var body))
                    {
                        body.X = position.X;
                        body.Y = position.Y;
                        body.personalVx = position.Vx - body.Outer.privateVx;
                        body.personalVy = position.Vy - body.Outer.privateVy;
                    }
                }
                return x;
            });
            feet.Run(x =>
            {
                foreach (var position in positions)
                {
                    if (x.TryGetValue(position.Id, out var foot)) {
                        foot.X = position.X;
                        foot.Y = position.Y;
                        foot.personalVx = position.Vx - foot.Body.Vx;
                        foot.personalVy = position.Vy - foot.Body.Vy;
                    }
                }
                return x;
            });
            playersInputs = new ConcurrentDictionary<Guid, ConcurrentLinkedList<PlayerInputs>>();
        }

        public Position[] GetPosition()
        {
            Player[] players = null;
            Center[] centers = null;
            feet.Run(x => { players = x.Values.ToArray(); return x; });
            bodies.Run(x => { centers = x.Values.ToArray(); return x; });


            var list = new Position[1 + players.Length + (centers.Length * 2)];
            var at = 0;
            list[at] = new Position(ball.X, ball.Y, ballId, ball.Vx, ball.Vy);
            at++;

            foreach (var foot in players)
            {
                list[at] = new Position(foot.X, foot.Y, foot.id, foot.Vx, foot.Vy);// 
                at++;
            }
            foreach (var body in centers)
            {
                list[at] = new Position(body.X, body.Y, body.id, body.Vx, body.Vy);
                at++;
                list[at] = new Position(
                    body.Outer.X,
                    body.Outer.Y,
                    body.Outer.Id,
                    body.Vx,
                    body.Vy);
                at++;
            }
            return list;
        }
    }
}