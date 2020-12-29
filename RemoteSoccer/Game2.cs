using Common;
using physics2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Common.Game;

namespace RemoteSoccer
{
    internal class Game2
    {

        public readonly GameState gameState;
        private readonly GameStateTracker gameStateTracker;

        public Game2() {
            FieldDimensions field = FieldDimensions.Default;


            gameState = new GameState();

            gameState.Handle(new InitGameStateEvent(
                new Physics2.Vector(field.xMax / 2.0, field.yMax / 2.0),
                new Physics2.Vector(0, 0),
                new Physics2.Vector(0, field.yMax / 2.0),
                new Physics2.Vector(field.xMax, field.yMax / 2.0),
                field));

            // GameStateTracker is a bit weird
            this.gameStateTracker = new Game.GameStateTracker(
                       (x, y) => {
                           gameState.ball.ownerOrNull = null;
                           gameState.ball.posistion = new Physics2.Vector(gameState.CountDownState.X, gameState.CountDownState.Y);
                           gameState.ball.velocity = new Physics2.Vector(0, 0);
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
        internal void ApplyInputs(Dictionary<Guid,PlayerInputs> inputs)
        {
            gameState.Handle(gameStateTracker.UpdateGameState());
            PlayerInputApplyer.Apply(gameState, inputs);
            
            gameState.Simulate(gameStateTracker);
        }


        internal void CreatePlayer(AddPlayerEvent createPlayer)
        {
            gameState.Handle(createPlayer);
        }

        internal void LeaveGame(RemovePlayerEvent removePlayerEvent)
        {
            gameState.Handle(removePlayerEvent);
        }

        internal void UpdatePlayer(UpdatePlayerEvent updatePlayerEvent)
        {
            gameState.Handle(updatePlayerEvent);
        }
    }
}