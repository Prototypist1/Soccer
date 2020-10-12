using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RemoteSoccer
{

    class LocalGame: IGame
    {
        private const string ConnectionId = "local-player";
        private IGameView gameView;
        public readonly Game game;

        public string GameName => "local-game";

        public LocalGame(FieldDimensions fieldDimensions)
        {
            this.game = new Game();
            this.game.Init(x => gameView?.HandleUpdateScore(x), fieldDimensions);
        }

        public void CreatePlayer(CreatePlayer createPlayer) {
            try
            {
                gameView?.HandleObjectsCreated(game.GetObjectsCreated());
                gameView?.HandleObjectsCreated(game.CreatePlayer(ConnectionId + "|" + createPlayer.SubId, createPlayer));
            }
            catch (Exception e) { 
            
            }
        }

        public void ResetGame(ResetGame resetGame) {
            gameView?.HandleUpdateScore(game.Reset());
        }


        public void ChangeColor(ColorChanged colorChanged) {
            game.ColorChanged(colorChanged);
            gameView?.HandleColorChanged(colorChanged);
        }

        public void NameChanged(NameChanged nameChanged) {

            game.NameChanged(nameChanged);
            gameView?.HandleNameChanged(nameChanged);
        }

        public void ClearCallbacks()
        {
            gameView = null;
        }

        public void OverwritePositions(Positions positions) {
            game.SetPositionsAndClearInputes(positions.PositionsList);
        }

        public IAsyncEnumerable<Positions> JoinChannel(JoinChannel joinChannel)
        {
            return game.GetReader();
        }

        public void LeaveGame(LeaveGame leaveGame)
        {
            if (game.TryDisconnect(ConnectionId + "|" + leaveGame.SubId, out var objectRemoveds))
            {
                gameView?.HandleObjectsRemoved(new ObjectsRemoved(objectRemoveds.ToArray()));
            }
        }

        public void OnDisconnect(Func<Exception, Task> onDisconnect)
        {
            // this is for remote games
        }

        public void SetCallbacks(IGameView gameView)
        {
            this.gameView = gameView;
        }
        public void PlayerInputs(PlayerInputs playerInputs) {

            game.PlayerInputs(playerInputs);
        }

        public async void StreamInputs(IAsyncEnumerable<PlayerInputs> inputs)
        {
            await foreach (var item in inputs)
            {
                game.PlayerInputs(item);
            }
        }
    }
}
