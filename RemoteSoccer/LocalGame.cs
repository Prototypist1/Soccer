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
        private readonly Game game;

        public string GameName => "local-game";

        public LocalGame()
        {
            this.game = new Game(x => gameView?.HandleUpdateScore(x));
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

        public IAsyncEnumerable<Positions> JoinChannel(JoinChannel joinChannel)
        {
            return game.GetReader();
        }

        public void LeaveGame(LeaveGame leaveGame)
        {
            if (game.TryDisconnect(ConnectionId, out var objectRemoveds))
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

        public async void StreamInputs(IAsyncEnumerable<PlayerInputs> inputs)
        {
            await foreach (var item in inputs)
            {
                game.PlayerInputs(item);
            }
        }
    }
}
