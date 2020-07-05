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
        private int players = 0;

        private readonly JumpBallConcurrent<PhysicsEngine> physicsEngine;
        private readonly JumpBallConcurrent<Dictionary<Guid, Player>> feet = new JumpBallConcurrent<Dictionary<Guid, Player>>(new Dictionary<Guid, Player>());
        private readonly JumpBallConcurrent<Dictionary<Guid, Center>> bodies = new JumpBallConcurrent<Dictionary<Guid, Center>>(new Dictionary<Guid, Center>());
        private readonly Guid ballId;
        private readonly Ball ball;

        private readonly ConcurrentSet<FootCreated> feetCreaated = new ConcurrentSet<FootCreated>();
        private readonly ConcurrentSet<BodyCreated> bodiesCreated = new ConcurrentSet<BodyCreated>();
        private readonly ConcurrentSet<OuterCreated> bodyNoLeansCreated = new ConcurrentSet<OuterCreated>();

        private readonly BallCreated ballCreated;
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

        private ConcurrentLinkedList<PlayerInputs> playersInputs = new ConcurrentLinkedList<PlayerInputs>();
        public DateTime LastInputUTC { get; private set; } = DateTime.Now;



        private readonly GameStateTracker gameStateTracker;
        private readonly FieldDimensions field;

        public class GameStateTracker
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
            private const int stopGrowingCircle = 100;
            private const int resetBall = 100;
            private const int startCountDown = 0;
            private const int endCountDown = 200;

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
            foreach (var element in bodyNoLeansCreated)
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
            ball.OwnerOrNull = null;
            return new UpdateScore() { Left = gameStateTracker.leftScore, Right = gameStateTracker.rightScore };
        }

        public Game(Action<UpdateScore> onUpdateScore, FieldDimensions field)
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
            var leftGoal = new Ball(1, 0, field.yMax / 2.0, false, new Circle(Constants.footLen));


            try
            {
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
            }
            catch (Exception)
            {

            }

            var rightGoalId = Guid.NewGuid();

            var rightGoal = new Ball(1, field.xMax, field.yMax / 2.0, false, new Circle(Constants.footLen));

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
                (new Vector(0,0) ,new Vector(field.xMax-1,0)),
                (new Vector(0,field.yMax-1 ) ,new Vector(0,0)),
                (new Vector(field.xMax-1,field.yMax-1),new Vector(0,field.yMax-1)),
                (new Vector(field.xMax-1,field.yMax-1),new Vector(field.xMax-1,0)),
            };

            gameStateTracker = new GameStateTracker(
                ball.Reset,
                field.xMax / 2.0,
                field.yMax / 2.0
            );
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
                bodyNoLeansCreated.ToArray());
        }

        public ObjectsCreated CreatePlayer(string connectionId, CreatePlayer createPlayer)
        {
            var random = new Random();

            double startX = random.NextDouble()* field.xMax;
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


            var bodyNoLeanCreated = new OuterCreated(
                        body.Outer.X,
                        body.Outer.Y,
                        Constants.bodyZ,
                        createPlayer.Outer,
                        createPlayer.BodyDiameter + (Constants.MaxLean*2),
                        createPlayer.BodyR,
                        createPlayer.BodyG,
                        createPlayer.BodyB,
                        (byte)((int)createPlayer.BodyA/2)
                        );

            bodyNoLeansCreated.AddOrThrow(bodyNoLeanCreated);

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

            connectionObjects.AddOrThrow(connectionId,new ConnectionStuff( new List<ObjectCreated>(){
                bodyCreated,
                footCreated ,
                bodyNoLeanCreated
            },
            createPlayer.Body,
            foot));


            Interlocked.Add(ref players, 1);

            return new ObjectsCreated(
                new[] { footCreated }, 
                new[] { bodyCreated }, 
                null, 
                new GoalCreated[] { },
                new [] { bodyNoLeanCreated });
        }

        public bool TryDisconnect(string connectionId, out List<ObjectRemoved> objectRemoveds)
        {
            if (connectionObjects.TryRemove(connectionId, out var toRemoves))
            {
                Interlocked.Add(ref players, -1);

                bodies.Run(x => { x.Remove(toRemoves.Body); return x; });

                feet.Run(x => { x.Remove(toRemoves.Foot.id); return x; });
                physicsEngine.Run(x => { x.RemovePlayer(toRemoves.Foot); return x; });

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

                    var bodyNoLean = bodyNoLeansCreated.SingleOrDefault(x => x.Id == item.Id);
                    if (bodyNoLean != null)
                    {
                        bodyNoLeansCreated.RemoveOrThrow(bodyNoLean);
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

                if (ball.OwnerOrNull == null) {

                    if (ball.Velocity.Length > 0)
                    {
                        var friction = ball.Velocity.NewUnitized().NewScaled(-ball.Velocity.Length * ball.Mass / 50.0);

                        ball.ApplyForce(
                            friction.x,
                            friction.y);

                    }

                    if (ball.Velocity.Length > 1)
                    {
                        var friction = ball.Velocity.NewUnitized().NewScaled(-1);

                        ball.ApplyForce(
                            friction.x,
                            friction.y);
                    }
                    else
                    {
                        ball.ApplyForce(
                            -ball.Velocity.x,
                            -ball.Velocity.y);
                    }
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

                        externalForces.Vy = externalForces.Vy * .85;
                        externalForces.Vx = externalForces.Vx * .85;

                        if (inputSet.TryGetValue(center.Key, out var input))
                        {

                            foot.Throwing = input.Throwing;

                            if (input.BodyX != 0 || input.BodyY != 0)
                            {
                                // crush oppozing forces
                                if (!input.Controller)
                                {
                                    throw new NotImplementedException("sad!");
                                    //var v = new Vector(body.Outer.Vx, body.Outer.Vy);
                                    //var f = new Vector(Math.Sign(input.BodyX), Math.Sign(input.BodyY));
                                    //var with = v.Dot(f) / f.Length;
                                    //if (with <= 0)
                                    //{
                                    //    body.Outer.ApplyForce(-body.Outer.Vx, -body.Outer.Vy);
                                    //}
                                    //else
                                    //{
                                    //    var withVector = f.NewUnitized().NewScaled(with);
                                    //    var notWith = v.NewAdded(withVector.NewScaled(-1));
                                    //    var notWithScald = notWith.Length > withVector.Length ? notWith.NewUnitized().NewScaled(with) : notWith;


                                    //    body.Outer.ApplyForce(-body.Outer.Vx + withVector.x + notWithScald.x, -body.Outer.Vy + withVector.y + notWithScald.y);
                                    //}


                                    //var damp = .98;

                                    //var R0 = EInverse(E(Math.Sqrt(Math.Pow(body.Outer.Vx, 2) + Math.Pow(body.Outer.Vy, 2))) + EnergyAdd);
                                    //var a = Math.Pow(Math.Sign(input.BodyX), 2) + Math.Pow(Math.Sign(input.BodyY), 2);
                                    //var b = 2 * ((Math.Sign(input.BodyX) * body.Outer.Vx * damp) + (Math.Sign(input.BodyY) * body.Outer.Vy * damp));
                                    //var c = Math.Pow(body.Outer.Vx * damp, 2) + Math.Pow(body.Outer.Vy * damp, 2) - Math.Pow(R0, 2);

                                    //var t = (-b + Math.Sqrt(Math.Pow(b, 2) - (4 * a * c))) / (2 * a);

                                    //body.Outer.ApplyForce(-(1 - damp) * body.Outer.Vx, -(1 - damp) * body.Outer.Vy);
                                    //body.Outer.ApplyForce(t * input.BodyX, t * input.BodyY);
                                }
                                else
                                {


                                    // base velocity becomes the part of the velocity in the direction of the players movement
                                    var v = new Vector(body.Outer.privateVx, body.Outer.privateVy);
                                    var f = new Vector(input.BodyX, input.BodyY).NewUnitized();
                                    var with = v.Dot(f);
                                    var baseValocity = with > 0 ? f.NewUnitized().NewScaled(with) : new Vector(0, 0);

                                    var engeryAdd = foot == ball.OwnerOrNull ? EnergyAdd / 2.0 : EnergyAdd;

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

                                    //if (vector.Length == 0)
                                    //{

                                    //}
                                    //else if (vector.Length > Constants.MaxDeltaV)
                                    //{
                                    //    body.Outer.Vx += vector.NewUnitized().NewScaled(Constants.MaxDeltaV).x;
                                    //    body.Outer.Vy += vector.NewUnitized().NewScaled(Constants.MaxDeltaV).y;
                                    //}
                                    //else
                                    //{
                                    //    body.Outer.Vx += vector.x;
                                    //    body.Outer.Vy += vector.y;
                                    //}

                                }
                            }
                            else
                            {
                                var vector = new Vector(-outer.privateVx, -outer.privateVy);

                                body.Outer.privateVx += vector.x;// / 2.0;
                                body.Outer.privateVy += vector.y;// / 2.0;

                                //if (vector.Length == 0)
                                //{

                                //}
                                //else if (vector.Length > Constants.MaxDeltaV)
                                //{
                                //    outer.Vx += vector.NewUnitized().NewScaled(Constants.MaxDeltaV).x;
                                //    outer.Vy += vector.NewUnitized().NewScaled(Constants.MaxDeltaV).y;
                                //}
                                //else
                                //{
                                //    outer.Vx += vector.x;
                                //    outer.Vy += vector.y;
                                //}
                            }

                            if (input.Controller)
                            {


                                var tx = (input.BodyX * Constants.MaxLean) + body.Outer.X;
                                var ty = (input.BodyY * Constants.MaxLean) + body.Outer.Y;

                                var vector = new Vector(tx - body.personalVx, ty - body.personalVy);

                                body.personalVx = (tx - body.X)/2.0;
                                body.personalVy = (ty - body.Y)/2.0;
                            }
                        }

                        //if (gameStateTracker.TryGetBallWall(out var ballWall)) {
                        //    var dis = new Vector( outer.X - ballWall.x, outer.Y - ballWall.y);
                        //    if (dis.Length == 0) {
                        //        dis = new Vector(1, 0);
                        //    }
                        //    if (dis.Length < ballWall.radius + Constants.footLen) {
                        //        var t = dis.NewUnitized().NewScaled(ballWall.radius + Constants.footLen).NewAdded(new Vector(ballWall.x,ballWall.y));

                        //        var vector = new Vector(t.x - outer.X, t.y - outer.Y);

                        //        outer.X += vector.x;
                        //        outer.Y += vector.y;

                        //        body.X += vector.x;
                        //        body.Y += vector.y;

                        //        foot.X += vector.x;
                        //        foot.Y += vector.y;
                        //    }
                        //}

                        var max = Constants.footLen - Constants.PlayerRadius;// - Constants.PlayerRadius;
                        
                        {
                            var tx = (input.FootX * max) + body.X;
                            var ty = (input.FootY * max) + body.Y;

                            var vector = new Vector(tx - foot.personalVx, ty - foot.personalVy);

                            foot.personalVx = (tx - foot.X)/2.0;
                            foot.personalVy = (ty - foot.Y)/ 2.0;
                        }

                        if (foot == ball.OwnerOrNull)
                        {

                            var dx = foot.X - ball.X;
                            var dy = foot.Y - ball.Y;

                            ball.UpdateVelocity(ball.OwnerOrNull.Vx + dx, ball.OwnerOrNull.Vy + dy);


                            if (foot.Throwing)
                            {
                                var throwV = new Vector(foot.Vx - body.Vx, foot.Vy - body.Vy);

                                if (ball.proposedThrow.Length > Constants.MimimunThrowingSpped)
                                {
                                    if (throwV.Length > ball.proposedThrow.Length && throwV.Dot(ball.proposedThrow) > 0)
                                    {
                                        ball.proposedThrow = throwV;
                                    }
                                    else if (throwV.Length * 2 < ball.proposedThrow.Length || throwV.Dot(ball.proposedThrow) < 0)
                                    {
                                        // throw the ball!
                                        ball.UpdateVelocity(ball.proposedThrow.x + body.Vx, ball.proposedThrow.y + body.Vy);
                                        ball.proposedThrow = new Vector();
                                        ball.OwnerOrNull.LastHadBall = simulationTime;
                                        ball.OwnerOrNull = null;
                                    }
                                }
                                else if (throwV.Length > Constants.MimimunThrowingSpped)
                                {
                                    ball.proposedThrow = throwV;
                                }
                            }
                            else
                            {
                                ball.proposedThrow = new Vector();
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


                positions = new Positions(GetPosition().ToArray(), simulationTime, countDownSate, collisions);
            }
            var next = new Node(positions);
            lastPositions.next.SetResult(next);
            lastPositions = next;
        }

        private int running = 0;


        private const int EnergyAdd =250000 ;//400;
        private const double SpeedScale = 1;
        private const double Add = 0;
        private const double ToThe = 3;//1.9;
        private double E(double v) => Math.Pow(Math.Max(0, v - Add), ToThe) * SpeedScale;
        private double EInverse(double e) => Math.Pow(e / SpeedScale, 1 / ToThe) + Add;

        public void PlayerInputs(PlayerInputs playerInputs)
        {
            //var lastLast = LastInputUTC;
            playersInputs.Add(playerInputs);
            while (playersInputs.Count >= players && Interlocked.CompareExchange(ref running, 1, 0) == 0)
            {
                Apply();
                running = 0;
            }
        }

        private Position[] GetPosition()
        {
            Player[] players = null;
            Center[] centers = null;
            feet.Run(x => { players = x.Values.ToArray(); return x; });
            bodies.Run(x => { centers = x.Values.ToArray(); return x; });


            var list = new Position[1+ players.Length + (centers.Length*2)];
            var at = 0;
            list[at]=new Position(ball.X, ball.Y, ballId, ball.Vx, ball.Vy);
            at++;

            foreach (var foot in players)
            {
                list[at] = new Position(foot.X, foot.Y, foot.id, foot.Vx, foot.Vy);
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