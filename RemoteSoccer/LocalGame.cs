using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteSoccer
{
    interface IGameView
    {
        void PlayerCreated(ObjectsCreated objectsCreated);
        void UpdateScore(UpdateScore updateScore);
        void RemoveObjects(ObjectsRemoved objectsRemoved);
        void ColorChanged(ColorChanged colorChanged);
        void NameChanged(NameChanged nameChanged);
    }

    class LocalGame
    {

        private readonly IGameView gameView;
        private readonly Game game;

        public LocalGame(IGameView gameView, Game game)
        {
            this.gameView = gameView ?? throw new ArgumentNullException(nameof(gameView));
            this.game = game ?? throw new ArgumentNullException(nameof(game));
        }


        public void CreatePlayer(string player, CreatePlayer createPlayer) {
            gameView.PlayerCreated(game.CreatePlayer(player,createPlayer));
        }

        public void ResetGame(ResetGame resetGame) {
            gameView.UpdateScore(game.Reset());
        }

        public void RemovePlayer(LeaveGame leaveGame) { 
            //??
        }

        public void ChangeColor(ColorChanged colorChanged) {
            game.ColorChanged(colorChanged);
            gameView.ColorChanged(colorChanged);
        }

        public void NameChanged(NameChanged nameChanged) {

            game.NameChanged(nameChanged);
            gameView.NameChanged(nameChanged);
        }

    }
}
