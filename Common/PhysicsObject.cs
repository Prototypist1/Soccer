using Common;
using Physics2;
using System;
using System.Collections.Generic;
using System.Text;

namespace physics2
{
    public class Game2
    {

        public readonly GameState gameState;
        private readonly GameStateTracker gameStateTracker;

        public Game2()
        {
            FieldDimensions field = FieldDimensions.Default;


            gameState = new GameState();

            gameState.Handle(new InitGameStateEvent(
                new Physics2.Vector(field.xMax / 2.0, field.yMax / 2.0),
                new Physics2.Vector(0, 0),
                new Physics2.Vector(0, field.yMax / 2.0),
                new Physics2.Vector(field.xMax, field.yMax / 2.0),
                field));

            // GameStateTracker is a bit weird
            this.gameStateTracker = new GameStateTracker(
                       (x, y) => {
                           gameState.GameBall.OwnerOrNull = null;
                           gameState.GameBall.Posistion = new Physics2.Vector(gameState.CountDownState.X, gameState.CountDownState.Y);
                           gameState.GameBall.Velocity = new Physics2.Vector(0, 0);
                       },
                        (field.xMax / 2.0) + Constants.footLen,
                        (field.xMax / 2.0) - Constants.footLen,
                        field.yMax - Constants.footLen,
                        Constants.footLen);
        }

        //internal void OnDisconnect(Func<Exception, Task> onDisconnect)
        //{

        //}

        //IAsyncEnumerable does not really make sense here
        public void ApplyInputs(Dictionary<Guid, PlayerInputs> inputs)
        {
            gameState.Handle(gameStateTracker.UpdateGameState());
            PlayerInputApplyer.Apply(gameState, inputs);

            gameState.Simulate(gameStateTracker);
        }


        public void CreatePlayer(AddPlayerEvent createPlayer)
        {
            gameState.Handle(createPlayer);
        }

        public void LeaveGame(RemovePlayerEvent removePlayerEvent)
        {
            gameState.Handle(removePlayerEvent);
        }

        public void UpdatePlayer(UpdatePlayerEvent updatePlayerEvent)
        {
            gameState.Handle(updatePlayerEvent);
        }
    }


    //public class PhysicsObject : IPhysicsObject, IUpdatePosition
    //{

    //    public PhysicsObject(double mass, double x, double y, bool mobile)
    //    {
    //        Mass = mass;
    //        X = x;
    //        Y = y;
    //        Mobile = mobile;
    //    }

    //    public double Vx { get; protected set; }
    //    public double Vy { get; protected set; }
    //    public double X { get; protected set; }
    //    public double Y { get; protected set; }
    //    public double Mass { get; }
    //    public double Speed => Math.Sqrt((Vx * Vx) + (Vy * Vy));
    //    public bool Mobile { get; }
    //    public Vector Velocity
    //    {
    //        get
    //        {
    //            return new Vector(Vx, Vy);
    //        }
    //    }

    //    public Vector Position => new Vector(X, Y);

    //    public virtual void UpdateVelocity(double vx, double vy) {
    //        this.Vx = vx;
    //        this.Vy = vy;
    //    }

    //    public void ApplyForce(double fx, double fy)
    //    {
    //        UpdateVelocity(Vx + (fx / Mass), Vy + (fy / Mass));
    //    }

    //    public virtual void Update(double step, double timeLeft)
    //    {
    //        X += Vx * step;
    //        Y += Vy * step;
    //    }
    //}

}
