using Physics2;
using Prototypist.TaskChain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Common.GameState;

namespace Common
{


    // this is a view of the current game state
    /// <summary>
    /// this is a version of game state that is sent from server to client
    /// it contains all values that are likely to change
    /// </summary>
    public class GameStateUpdate
    {
        public Collision[] Collisions { get; set; } = new Collision[] { };
        public GoalScored[] GoalsScored { get; set; } = new GoalScored[] { };
        public Player[] Players { get; set; } = new Player[] { };
        public Ball Ball { get; set; }
        public int Frame { get; set; }
        public CountDownState CountDownState { get; set; }
        public int LeftScore { get; set; }
        public int RightScore { get; set; }

        public GameStateUpdate() { }

        public GameStateUpdate(Collision[] collisions, GoalScored[] goalsScored, Player[] players, Ball ball, int frame, CountDownState countDownState, int leftScore, int rightScore)
        {
            this.Collisions = collisions ?? throw new ArgumentNullException(nameof(collisions));
            this.GoalsScored = goalsScored ?? throw new ArgumentNullException(nameof(goalsScored));
            this.Players = players ?? throw new ArgumentNullException(nameof(players));
            this.Ball = ball ?? throw new ArgumentNullException(nameof(ball));
            this.Frame = frame;
            CountDownState = countDownState;
            this.LeftScore = leftScore;
            this.RightScore = rightScore;
        }
    }

    public class GameState
    {
        // these arn't really state
        public ConcurrentLinkedList<Collision> collisions = new ConcurrentLinkedList<Collision>();
        public class Collision
        {
            public Vector Position { get; set; }
            public Vector Force { get; set; }
            public int Frame { get; set; }
            public Guid Id { get; set; }

            public Collision() { }

            public Collision(Vector position, Vector force, int frame, Guid id)
            {
                this.Position = position;
                this.Force = force;
                this.Frame = frame;
                Id = id;
            }

            public override bool Equals(object obj)
            {
                return obj is Collision collision &&
                       Id.Equals(collision.Id);
            }

            public override int GetHashCode()
            {
                return 2108858624 + Id.GetHashCode();
            }
        }
        public ConcurrentLinkedList<GoalScored> GoalsScored { get; set; } = new ConcurrentLinkedList<GoalScored>();
        public class GoalScored
        {
            public Vector Posistion { get; set; }
            public bool LeftScored { get; set; }
            public Vector Surface { get; set; }
            public int Frame { get; set; }
            public Guid Id { get; set; }

            public GoalScored() { }
            public GoalScored(Vector posistion, bool leftScored, Vector surface, int frame, Guid id)
            {
                this.Posistion = posistion;
                this.LeftScored = leftScored;
                this.Surface = surface;
                this.Frame = frame;
                Id = id;
            }

            public override bool Equals(object obj)
            {
                return obj is GoalScored scored &&
                       Id.Equals(scored.Id);
            }

            public override int GetHashCode()
            {
                return 2108858624 + Id.GetHashCode();
            }
        }

        public RawConcurrentIndexed<Guid, Player> players = new RawConcurrentIndexed<Guid, Player>();
        // these velocities are private
        // foot really move at foot.velocity.x + body.velocity.x + externalVelocity.x
        public class Player
        {
            public Vector BoostCenter { get; set; }
            public Guid Id { get; set; }
            public string Name { get; set; }
            public Body PlayerBody { get; set; }
            //public bool Throwing { get; set; }


            public class Body
            {
                public Vector Position { get; set; }
                public Vector Velocity { get; set; }
                public byte A { get; set; }
                public byte R { get; set; }
                public byte G { get; set; }
                public byte B { get; set; }

                public Body() { }

                public Body(Vector posistion, Vector velocity, byte a, byte r, byte g, byte b)
                {
                    this.Position = posistion;
                    this.Velocity = velocity;
                    this.A = a;
                    this.R = r;
                    this.G = g;
                    this.B = b;
                }
            }
            public Foot PlayerFoot { get; set; }
            public class Foot
            {
                public Vector Position { get; set; }
                public Vector Velocity { get; set; }
                public byte A { get; set; }
                public byte R { get; set; }
                public byte G { get; set; }
                public byte B { get; set; }

                public Foot() { }

                public Foot(Vector posistion, Vector velocity, byte a, byte r, byte g, byte b)
                {
                    this.Position = posistion;
                    this.Velocity = velocity;
                    this.A = a;
                    this.R = r;
                    this.G = g;
                    this.B = b;
                }
            }

            public Player() { }

            // I don't love this design
            // these things are really for playerInputApplyer + physicsEngine
            // most consumers of GameState are not interested in them 
            public Vector ExternalVelocity { get; set; }
            public Vector BoostVelocity { get; set; }
            public Vector ProposedThrow { get; set; }
            public int LastHadBall { get; set; }
            public double Mass { get; set; }
            public Vector ThrowStart { get; set; }
            public double Boosts { get; set; } = 3;

        }
        public Goal LeftGoal { get; set; }
        public Goal RightGoal { get; set; }
        public class Goal
        {
            public Vector Posistion { get; set; }
            public bool LeftGoal { get; set; }

            public Goal() { }

            public Goal(Vector posistion, bool leftGoal)
            {
                this.Posistion = posistion;
                this.LeftGoal = leftGoal;
            }
        }
        public Ball GameBall { get; set; }
        public class Ball
        {
            public Vector Posistion { get; set; }
            public Vector Velocity { get; set; }
            public Guid? OwnerOrNull { get; set; } = null;
            internal double Mass { get; set; } = Constants.BallMass;

            public Ball() { }

            public Ball(Vector posistion, Vector velocity)
            {
                this.Posistion = posistion;
                this.Velocity = velocity;
            }
        }
        public int Frame { get; set; }
        public CountDownState CountDownState { get; set; }
        public int LeftScore { get; set; }
        public int RightScore { get; set; }

        public class PerimeterSegment
        {
            public Vector Start { get; set; }
            public Vector End { get; set; }

            public PerimeterSegment() { }

            public PerimeterSegment(Vector start, Vector end)
            {
                this.Start = start;
                this.End = end;
            }
        }
        public PerimeterSegment[] PerimeterSegments { get; set; }
        // previews 

    }

    public static class GameStateUpdater
    {

        public static void Handle(this GameState state, InitGameStateEvent evnt)
        {
            state.GameBall = new GameState.Ball(evnt.ballPosition, evnt.ballVelocity);

            state.LeftGoal = new GameState.Goal(evnt.leftGoalPosition, true);
            state.RightGoal = new GameState.Goal(evnt.rightGoalPosition, false);

            state.PerimeterSegments = new[]
            {
                new GameState.PerimeterSegment(new  Vector(0, 0),new  Vector(evnt.fieldDimensions.xMax, 0)),
                new GameState.PerimeterSegment(new  Vector(0, evnt.fieldDimensions.yMax),new  Vector(0, 0)),
                new GameState.PerimeterSegment(new  Vector(evnt.fieldDimensions.xMax,evnt.fieldDimensions.yMax),new  Vector(0,evnt.fieldDimensions.yMax)),
                new GameState.PerimeterSegment(new  Vector(evnt.fieldDimensions.xMax,0),new  Vector(evnt.fieldDimensions.xMax,evnt.fieldDimensions.yMax))
            };
        }

        public static void Handle(this GameState gameState, GameStateUpdate gameStateUpdate)
        {
            gameState.Frame = gameStateUpdate.Frame;
            gameState.GameBall = gameStateUpdate.Ball;
            var nextCollisions = new ConcurrentLinkedList<Collision>();
            foreach (var item in gameStateUpdate.Collisions)
            {
                nextCollisions.Add(item);
            }
            gameState.collisions = nextCollisions;
            gameState.CountDownState = gameStateUpdate.CountDownState;
            var nextGoalsScores = new ConcurrentLinkedList<GoalScored>();
            foreach (var item in gameStateUpdate.GoalsScored)
            {
                nextGoalsScores.Add(item);
            }
            gameState.GoalsScored = nextGoalsScores;
            var nextPlayers = new RawConcurrentIndexed<Guid, Player>();
            foreach (var item in gameStateUpdate.Players)
            {
                nextPlayers.TryAdd(item.Id,item);
            }
            gameState.players = nextPlayers;
            gameState.RightScore = gameStateUpdate.RightScore;
            gameState.LeftScore = gameStateUpdate.LeftScore;
        }

        public static GameStateUpdate GetGameStateUpdate(this GameState gameState)
        {
            return new GameStateUpdate(gameState.collisions.ToArray(), gameState.GoalsScored.ToArray(), gameState.players.Values.ToArray(), gameState.GameBall, gameState.Frame, gameState.CountDownState, gameState.LeftScore, gameState.RightScore);
        }


        public static void Handle(this GameState gameState, UpdatePlayerEvent evnt)
        {
            if (gameState.players.TryGetValue(evnt.id, out var player))
            {
                player.Name = evnt.Name;
                player.PlayerBody.A = evnt.bodyA;
                player.PlayerBody.R = evnt.bodyR;
                player.PlayerBody.G = evnt.bodyG;
                player.PlayerBody.B = evnt.bodyB;
                player.PlayerFoot.A = evnt.footA;
                player.PlayerFoot.R = evnt.footR;
                player.PlayerFoot.G = evnt.footG;
                player.PlayerFoot.B = evnt.footB;
            }
        }
        public static void Handle(this GameState gameState, AddPlayerEvent evnt)
        {
            gameState.players.TryAdd(evnt.id, new GameState.Player
            {
                Id = evnt.id,
                Name = evnt.Name,
                ExternalVelocity = new Vector(0, 0),
                BoostVelocity = new Vector(0, 0),
                PlayerBody = new GameState.Player.Body(evnt.posistion, new Vector(0, 0), evnt.bodyA, evnt.bodyR, evnt.bodyG, evnt.bodyB),
                PlayerFoot = new GameState.Player.Foot(evnt.posistion, new Vector(0, 0), evnt.footA, evnt.footR, evnt.footG, evnt.footB),
                Mass = 1,
            });
        }
        public static void Handle(this GameState gameState, RemovePlayerEvent evnt)
        {
            gameState.players.TryRemove(evnt.id, out _);
        }
        public static void Handle(this GameState gameState, ResetGameEvent evnt)
        {
            gameState.LeftScore = 0;
            gameState.RightScore = 0;
        }
        //public static void Handle(this GameState gameState, UpdateFrameEvent evnt)
        //{
        //    if (evnt.frame > gameState.frame)
        //    {
        //        gameState.frame = evnt.frame;

        //        foreach (var update in evnt.updatePlayers)
        //        {
        //            if (gameState.players.TryGetValue(update.Id, out var player))
        //            {
        //                player.body.position = update.bodyPosition;
        //                player.body.velocity = update.bodyVelocity;
        //                player.foot.position = update.footPosition;
        //                player.foot.velocity = update.footVelocity;
        //                player.externalVelocity = update.externalVelocity;
        //            }
        //        }

        //        gameState.ball.velocity = evnt.updateBall.velocity;
        //        gameState.ball.posistion = evnt.updateBall.posistion;

        //        gameState.CountDownState.Countdown = evnt.updateCountDown.countdown;
        //        gameState.CountDownState.BallOpacity = evnt.updateCountDown.ballOpacity;
        //        gameState.CountDownState.Radius = evnt.updateCountDown.radius;
        //        gameState.CountDownState.StrokeThickness = evnt.updateCountDown.strokeThickness;
        //        gameState.CountDownState.X = evnt.updateCountDown.posistion.x;
        //        gameState.CountDownState.Y = evnt.updateCountDown.posistion.y;
        //        if (evnt.goalScored != null)
        //        {
        //            GameStateUpdater.Handle(gameState,evnt.goalScored);
        //        }

        //        foreach (var collision in evnt.collisions)
        //        {
        //            gameState.collisions.Add(collision);
        //        }
        //    }
        //}
        public static void Handle(this GameState gameState, CountDownState countDownState)
        {
            gameState.CountDownState = countDownState;
        }

        public static void Handle(this GameState gameState, GameState.GoalScored evnt)
        {
            gameState.GoalsScored.Add(evnt);
            if (evnt.LeftScored)
            {
                gameState.LeftScore++;
            }
            else
            {
                gameState.RightScore++;
            }
        }
    }

    public class InitGameStateEvent
    {
        internal Vector ballPosition;
        internal Vector ballVelocity;
        internal Vector leftGoalPosition;
        internal Vector rightGoalPosition;
        internal FieldDimensions fieldDimensions;

        public InitGameStateEvent(Vector ballPosition, Vector ballVelocity, Vector leftGoalPosition, Vector rightGoalPosition, FieldDimensions fieldDimensions)
        {
            this.ballPosition = ballPosition;
            this.ballVelocity = ballVelocity;
            this.leftGoalPosition = leftGoalPosition;
            this.rightGoalPosition = rightGoalPosition;
            this.fieldDimensions = fieldDimensions;
        }
    }

    public class UpdatePlayerEvent
    {
        public Guid id;
        public string Name;
        public byte bodyA, bodyR, bodyG, bodyB, footA, footR, footG, footB;

        public UpdatePlayerEvent(Guid id, string name, byte bodyA, byte bodyR, byte bodyG, byte bodyB, byte footA, byte footR, byte footG, byte footB)
        {
            this.id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            this.bodyA = bodyA;
            this.bodyR = bodyR;
            this.bodyG = bodyG;
            this.bodyB = bodyB;
            this.footA = footA;
            this.footR = footR;
            this.footG = footG;
            this.footB = footB;
        }
    }

    public class AddPlayerEvent
    {
        public Guid id;
        public string Name;
        public byte bodyA, bodyR, bodyG, bodyB, footA, footR, footG, footB;
        public Vector posistion;

        public AddPlayerEvent(Guid id, string name, byte bodyA, byte bodyR, byte bodyG, byte bodyB, byte footA, byte footR, byte footG, byte footB, Vector posistion)
        {
            this.id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            this.bodyA = bodyA;
            this.bodyR = bodyR;
            this.bodyG = bodyG;
            this.bodyB = bodyB;
            this.footA = footA;
            this.footR = footR;
            this.footG = footG;
            this.footB = footB;
            this.posistion = posistion;
        }
    }

    public class RemovePlayerEvent
    {
        public Guid id;

        public RemovePlayerEvent(Guid id)
        {
            this.id = id;
        }
    }

    public class ResetGameEvent { }
    public class UpdatePlayerPositionEvevnt
    {
        public Guid Id;
        public Vector bodyPosition, bodyVelocity;
        public Vector footPosition, footVelocity;
        public Vector externalVelocity;
    }
    public class UpdateBallEvent
    {
        public Vector posistion, velocity;
    }
    public class UpdateCountDownState
    {
        public bool countdown;
        public Vector posistion;
        public double radius;
        public double strokeThickness;
        public double ballOpacity;
    }

    //public class UpdateFrameEvent {
    //    public int frame;
    //    public UpdatePlayerPositionEvevnt[] updatePlayers;
    //    public UpdateBallEvent updateBall;
    //    public UpdateCountDownState updateCountDown;
    //    public GameState.GoalScored goalScored;
    //    public GameState.Collision[] collisions;

    //}
}
