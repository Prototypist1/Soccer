using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Interactive;
using System.Threading.Tasks;

namespace RemoteSoccer
{
    class RemoteWithPreviewGame : IGame
    {
        private readonly LocalGame localGame;
        private readonly RemoteGame remoteGame;

        public string GameName { get; }

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
            //if (TryTransfom(createPlayer.Foot, out var _))
            //{
            //    localGame.CreatePlayer(new CreatePlayer (
            //        localFoot,
            //        localBody,
            //        localOuter,
            //        createPlayer.BodyDiameter,
            //        createPlayer.FootDiameter,
            //        createPlayer.BodyR,
            //        createPlayer.BodyG,
            //        createPlayer.BodyB,
            //        createPlayer.BodyA,
            //        createPlayer.FootR,
            //        createPlayer.FootG,
            //        createPlayer.FootB,
            //        createPlayer.FootA,"",
            //        createPlayer.SubId));
            //}

            localGame.CreatePlayer(createPlayer);
            remoteGame.CreatePlayer(createPlayer);
        }

        public IAsyncEnumerable<Positions> JoinChannel(JoinChannel joinChannel)
        {
            return localGame.JoinChannel(joinChannel).Merge(remoteGame.JoinChannel(joinChannel));
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
            localGame.SetCallbacks(new TranslatingGameView( gameView));
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
            remoteGame.StreamInputs(PassThrough(inputs));
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
