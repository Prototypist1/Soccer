//using Common;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace RemoteSoccer
//{


//    class RemoteGame : IGame
//    {
//        public string GameName { get; }
//        private readonly SingleSignalRHandler.SignalRHandler handler;

//        public RemoteGame(string gameName, SingleSignalRHandler.SignalRHandler handler)
//        {
//            this.GameName = gameName ?? throw new ArgumentNullException(nameof(gameName));
//            this.handler = handler;
//        }

//        public void OnDisconnect(Func<Exception, Task> onDisconnect)
//        {
//            handler.SetOnClosed(onDisconnect);
//        }

//        public void SetCallbacks(IGameView gameView)
//        {
//            handler.SetCallBacks(gameView);
//        }

//        public void CreatePlayer(CreatePlayer createPlayer)
//        {
//            handler.Send(GameName, createPlayer);
//        }

//        public void StreamInputs(IAsyncEnumerable<PlayerInputs> inputs)
//        {
//            handler.Send(GameName, inputs);
//        }

//        public void ChangeColor(ColorChanged colorChanged)
//        {
//            handler.Send(GameName, colorChanged);
//        }

//        public void NameChanged(NameChanged nameChanged)
//        {
//            handler.Send(GameName, nameChanged);
//        }

//        public void LeaveGame(LeaveGame leaveGame)
//        {
//            handler.Send(leaveGame);
//        }

//        public void ClearCallbacks()
//        {
//            handler.ClearCallBacks();
//        }

//        public void ResetGame(ResetGame resetGame)
//        {
//            handler.Send(resetGame);
//        }

//        public IAsyncEnumerable<Positions> JoinChannel(JoinChannel joinChannel)
//        {
//            return handler.JoinChannel(joinChannel);
//        }

//    }
//}
