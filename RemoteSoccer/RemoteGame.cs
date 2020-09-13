using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteSoccer
{
    class RemoteWithPreviewGame : IGame
    {
        Guid foot, outer, body;
        private readonly LocalGame localGame;
        private readonly RemoteGame remoteGame;

        public RemoteWithPreviewGame(Guid foot, Guid outer, Guid body, string gameName, SingleSignalRHandler.SignalRHandler handler, FieldDimensions fieldDimensions)
        {
            this.foot = foot;
            this.outer = outer;
            this.body = body;
            this.localGame = new LocalGame(fieldDimensions);
            this.remoteGame = new RemoteGame(gameName, handler);
        }

        public string GameName => localGame.GameName;

        public void ChangeColor(ColorChanged colorChanged)
        {
            localGame.ChangeColor(colorChanged);
            remoteGame.ChangeColor(colorChanged);
        }

        public void ClearCallbacks()
        {
            localGame.ClearCallbacks();
            remoteGame.ClearCallbacks();
        }

        public void CreatePlayer(CreatePlayer createPlayer)
        {
            localGame.CreatePlayer(createPlayer);
            remoteGame.CreatePlayer(createPlayer);
        }

        public IAsyncEnumerable<Positions> JoinChannel(JoinChannel joinChannel)
        {
            return remoteGame.JoinChannel(joinChannel);//AsyncEnumerableEx.Merge(localGame.JoinChannel(joinChannel), remoteGame.JoinChannel(joinChannel));
        }

        public void LeaveGame(LeaveGame leaveGame)
        {
            localGame.LeaveGame(leaveGame);
            remoteGame.LeaveGame(leaveGame);
        }

        public void NameChanged(NameChanged nameChanged)
        {
            localGame.NameChanged(nameChanged);
            remoteGame.NameChanged(nameChanged);
        }

        public void OnDisconnect(Func<Exception, Task> onDisconnect)
        {
            localGame.OnDisconnect(onDisconnect);
            remoteGame.OnDisconnect(onDisconnect);
        }

        public void ResetGame(ResetGame resetGame)
        {
            localGame.ResetGame(resetGame);
            remoteGame.ResetGame(resetGame);
        }

        public void SetCallbacks(IGameView gameView)
        {
            localGame.SetCallbacks(new TranslatingGameView( gameView, foot, outer, body));
            remoteGame.SetCallbacks(gameView);
        }

        private async IAsyncEnumerable<PlayerInputs> PassThrough(IAsyncEnumerable<PlayerInputs> inputs) {
            await foreach (var item in inputs)
            {
                localGame.PlayerInputs(item);
                yield return item;
            }
        }

        public void StreamInputs(IAsyncEnumerable<PlayerInputs> inputs)
        {
            //PassThrough()
            remoteGame.StreamInputs(inputs);
        }
    }

    class RemoteGame : IGame
    {
        public string GameName { get; }
        private readonly SingleSignalRHandler.SignalRHandler handler;

        public RemoteGame(string gameName, SingleSignalRHandler.SignalRHandler handler)
        {
            this.GameName = gameName ?? throw new ArgumentNullException(nameof(gameName));
            this.handler = handler;
        }

        public void OnDisconnect(Func<Exception, Task> onDisconnect)
        {
            handler.SetOnClosed(onDisconnect);
        }

        public void SetCallbacks(IGameView gameView)
        {
            handler.SetCallBacks(gameView);
        }

        public void CreatePlayer(CreatePlayer createPlayer)
        {
            handler.Send(GameName, createPlayer);
        }

        public void StreamInputs(IAsyncEnumerable<PlayerInputs> inputs)
        {
            handler.Send(GameName, inputs);
        }

        public void ChangeColor(ColorChanged colorChanged)
        {
            handler.Send(GameName, colorChanged);
        }

        public void NameChanged(NameChanged nameChanged)
        {
            handler.Send(GameName, nameChanged);
        }

        public void LeaveGame(LeaveGame leaveGame)
        {
            handler.Send(leaveGame);
        }

        public void ClearCallbacks()
        {
            handler.ClearCallBacks();
        }

        public void ResetGame(ResetGame resetGame)
        {
            handler.Send(resetGame);
        }

        public IAsyncEnumerable<Positions> JoinChannel(JoinChannel joinChannel)
        {
            return handler.JoinChannel(joinChannel);
        }

    }
}
