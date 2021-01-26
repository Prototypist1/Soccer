using Common;
using Physics2;
using Prototypist.TaskChain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace physics2
{
    public class Game2
    {

        // this is a bit weird
        // GameState and GameStateTracker
        // why is my game state in two classes?
        // what is gameStateTracker for?
        public readonly GameState gameState;
        private readonly GameStateTracker gameStateTracker;

        public Game2()
        {
            var field = FieldDimensions.Default;

            gameState = new GameState();

            gameState.Handle(new InitGameStateEvent(
                new Physics2.Vector(field.xMax / 2.0, field.yMax / 2.0),
                new Physics2.Vector(0, 0),
                new Physics2.Vector(Constants.goalLen + Constants.footLen*2, field.yMax / 2.0),
                new Physics2.Vector(field.xMax - (Constants.goalLen + Constants.footLen * 2), field.yMax / 2.0),
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

            gameState.Handle(gameStateTracker.GetCountDownState());
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

            // clear out effects after a few frames
            var nextCollisions = new ConcurrentLinkedList<GameState.Collision>();
            foreach (var item in gameState.collisions.Where(x => x.Frame + 3 > gameState.Frame))
            {
                nextCollisions.Add(item);
            }
            gameState.collisions = nextCollisions;
            var nextGoalsScored = new ConcurrentLinkedList<GameState.GoalScored>();
            foreach (var item in gameState.GoalsScored.Where(x => x.Frame + 3 > gameState.Frame))
            {
                nextGoalsScored.Add(item);
            }
            gameState.GoalsScored = nextGoalsScored;
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
}
