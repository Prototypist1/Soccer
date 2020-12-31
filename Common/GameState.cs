using Physics2;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{


    // this is a view of the current game state

    public class GameState
    {
        // these arn't really state
        public List<Collision> collisions = new List<Collision>();
        public class Collision {
            public Vector position, force;

            public Collision(Vector position, Vector force)
            {
                this.position = position;
                this.force = force;
            }
        }
        public List<GoalScored> goalsScored = new List<GoalScored>();
        public class GoalScored {
            public Vector posistion;
            public bool leftScored;
            public Vector surface;
            public int frame;

            public GoalScored(Vector posistion, bool leftScored, Vector surface, int frame)
            {
                this.posistion = posistion;
                this.leftScored = leftScored;
                this.surface = surface;
                this.frame = frame;
            }
        }

        public Dictionary<Guid, Player> players = new Dictionary<Guid, Player>();
        // these velocities are private
        // foot really move at foot.velocity.x + body.velocity.x + externalVelocity.x
        public class Player {
            public Guid id;
            public string name;
            public Body body;
            public bool throwing;

            public class Body {
                public Vector position, velocity;
                public byte a, r, g, b;

                public Body(Vector posistion, Vector velocity, byte a, byte r, byte g, byte b)
                {
                    this.position = posistion;
                    this.velocity = velocity;
                    this.a = a;
                    this.r = r;
                    this.g = g;
                    this.b = b;
                }
            }
            public Foot foot;
            public class Foot {
                public Vector position, velocity;
                public byte a, r, g, b;

                public Foot(Vector posistion, Vector velocity, byte a, byte r, byte g, byte b)
                {
                    this.position = posistion;
                    this.velocity = velocity;
                    this.a = a;
                    this.r = r;
                    this.g = g;
                    this.b = b;
                }
            }

            // I don't love this design
            // these things are really for playerInputApplyer + physicsEngine
            // most consumers of GameState are not interested in them 
            public Vector externalVelocity;
            public Vector proposedThrow;
            public int lastHadBall;
            public double mass;
        }
        public Goal leftGoal, rightGoal;
        public class Goal {
            public Vector posistion;
            public bool leftGoal;

            public Goal(Vector posistion, bool leftGoal)
            {
                this.posistion = posistion;
                this.leftGoal = leftGoal;
            }
        }
        public Ball ball;
        public class Ball {
            public Vector posistion, velocity;
            public Player ownerOrNull = null;
            internal double mass = Constants.BallMass;

            public Ball(Vector posistion, Vector velocity)
            {
                this.posistion = posistion;
                this.velocity = velocity;
            }
        }
        public int frame;
        public CountDownState CountDownState;
        public int leftScore, rightScore;

        public class PerimeterSegment {
            public Vector start, end;

            public PerimeterSegment(Vector start, Vector end)
            {
                this.start = start;
                this.end = end;
            }
        }
        public PerimeterSegment[] perimeterSegments;
        // previews 

    }

    public static class GameStateUpdater{

        public static void Handle(this GameState state, InitGameStateEvent evnt) {
            state.ball = new GameState.Ball(evnt.ballPosition, evnt.ballVelocity);

            state.leftGoal = new GameState.Goal(evnt.leftGoalPosition, true);
            state.rightGoal = new GameState.Goal(evnt.rightGoalPosition, false);

            state.perimeterSegments = new[]
            {
                new GameState.PerimeterSegment(new  Vector(0, 0),new  Vector(evnt.fieldDimensions.xMax, 0)),
                new GameState.PerimeterSegment(new  Vector(0, evnt.fieldDimensions.yMax),new  Vector(0, 0)),
                new GameState.PerimeterSegment(new  Vector(evnt.fieldDimensions.xMax,evnt.fieldDimensions.yMax),new  Vector(0,evnt.fieldDimensions.yMax)),
                new GameState.PerimeterSegment(new  Vector(evnt.fieldDimensions.xMax,evnt.fieldDimensions.yMax),new  Vector(evnt.fieldDimensions.xMax,0))
            };

        }


        public static void Handle(this GameState gameState, UpdatePlayerEvent evnt)
        {
            if (gameState.players.TryGetValue(evnt.id, out var player))
            {
                player.name = evnt.Name;
                player.body.a = evnt.bodyA;
                player.body.r = evnt.bodyR;
                player.body.g = evnt.bodyG;
                player.body.b = evnt.bodyB;
                player.foot.a = evnt.footA;
                player.foot.r = evnt.footR;
                player.foot.g = evnt.footG;
                player.foot.b = evnt.footB;
            }
        }
        public static void Handle(this GameState gameState, AddPlayerEvent evnt)
        {
            gameState.players.Add(evnt.id, new GameState.Player
            {
                id = evnt.id,
                name = evnt.Name,
                externalVelocity = new Vector(0, 0),
                body = new GameState.Player.Body(evnt.posistion, new Vector(0, 0), evnt.bodyA, evnt.bodyR, evnt.bodyG, evnt.bodyB),
                foot = new GameState.Player.Foot(evnt.posistion, new Vector(0, 0), evnt.footA, evnt.footR, evnt.footG, evnt.footB),
                mass = 1,
            });
        }
        public static void Handle(this GameState gameState, RemovePlayerEvent evnt)
        {
            gameState.players.Remove(evnt.id);
        }
        public static void Handle(this GameState gameState, ResetGameEvent evnt)
        {
            gameState.leftScore = 0;
            gameState.rightScore = 0;
        }
        public static void Handle(this GameState gameState, UpdateFrameEvent evnt)
        {
            if (evnt.frame > gameState.frame)
            {
                gameState.frame = evnt.frame;

                foreach (var update in evnt.updatePlayers)
                {
                    if (gameState.players.TryGetValue(update.Id, out var player))
                    {
                        player.body.position = update.bodyPosition;
                        player.body.velocity = update.bodyVelocity;
                        player.foot.position = update.footPosition;
                        player.foot.velocity = update.footVelocity;
                        player.externalVelocity = update.externalVelocity;
                    }
                }

                gameState.ball.velocity = evnt.updateBall.velocity;
                gameState.ball.posistion = evnt.updateBall.posistion;

                gameState.CountDownState.Countdown = evnt.updateCountDown.countdown;
                gameState.CountDownState.BallOpacity = evnt.updateCountDown.ballOpacity;
                gameState.CountDownState.Radius = evnt.updateCountDown.radius;
                gameState.CountDownState.StrokeThickness = evnt.updateCountDown.strokeThickness;
                gameState.CountDownState.X = evnt.updateCountDown.posistion.x;
                gameState.CountDownState.Y = evnt.updateCountDown.posistion.y;
                if (evnt.goalScored != null)
                {
                    GameStateUpdater.Handle(gameState,evnt.goalScored);
                }

                foreach (var collision in evnt.collisions)
                {
                    gameState.collisions.Add(collision);
                }
            }
        }
        public static void Handle(this GameState gameState, CountDownState countDownState) {
            gameState.CountDownState = countDownState;
        }

        public static void Handle(this GameState gameState, GameState.GoalScored evnt)
        {
            gameState.goalsScored.Add(evnt);
            if (evnt.leftScored)
            {
                gameState.leftScore++;
            }
            else
            {
                gameState.rightScore++;
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

    public class UpdatePlayerEvent {
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

    public class RemovePlayerEvent {
        public Guid id;

        public RemovePlayerEvent(Guid id)
        {
            this.id = id;
        }
    }

    public class ResetGameEvent {}
    public class UpdatePlayerPositionEvevnt
    {
        public Guid Id;
        public Vector bodyPosition, bodyVelocity;
        public Vector footPosition, footVelocity;
        public Vector externalVelocity;
    }
    public class UpdateBallEvent {
        public Vector posistion, velocity;
    }
    public class UpdateCountDownState {
        public bool countdown;
        public Vector posistion;
        public double radius;
        public double strokeThickness;
        public double ballOpacity;
    }

    public class UpdateFrameEvent {
        public int frame;
        public UpdatePlayerPositionEvevnt[] updatePlayers;
        public UpdateBallEvent updateBall;
        public UpdateCountDownState updateCountDown;
        public GameState.GoalScored goalScored;
        public GameState.Collision[] collisions;

    }
}
