//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Common;

//namespace RemoteSoccer
//{
//    interface IGame
//    {
//        string GameName { get; }

//        void ChangeColor(ColorChanged colorChanged);
//        void ClearCallbacks();
//        void CreatePlayer(CreatePlayer createPlayer);
//        IAsyncEnumerable<Positions> JoinChannel(JoinChannel joinChannel);
//        void LeaveGame(LeaveGame leaveGame);
//        void NameChanged(NameChanged nameChanged);
//        void OnDisconnect(Func<Exception, Task> onDisconnect);
//        void SetCallbacks(IGameView gameView);
//        void StreamInputs(IAsyncEnumerable<PlayerInputs> inputs);
//        void ResetGame(ResetGame resetGame);
//    }
//}