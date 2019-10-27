//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Common;
//using Prototypist.Fluent;

//namespace RemoteSoccer
//{
//    public interface ISignalRHandler
//    {
//        void ClearCallBacks();
//        IAsyncEnumerable<Positions> JoinChannel(JoinChannel joinChannel);
//        Task<OrType<GameCreated, GameJoined, Exception>> Send(CreateOrJoinGame createOrJoinGame);
//        void Send(LeaveGame leave);
//        void Send(ResetGame inputs);
//        void Send(string game, ColorChanged colorChanged);
//        void Send(string game, CreatePlayer createPlayer, Action<ObjectsCreated> handleObjectsCreated, Action<ObjectsRemoved> handleObjectsRemoved, Action<UpdateScore> handleUpdateScore, Action<ColorChanged> handleColorChanged, Action<NameChanged> handleNameChanged);
//        void Send(string game, IAsyncEnumerable<PlayerInputs> inputs);
//        void Send(string game, NameChanged nameChanged);
//        void SetOnClosed(Func<Exception, Task> onClosed);
//    }

//    public class LocalGame : ISignalRHandler
//    {
//        private Game game;

//        public void ClearCallBacks()
//        {
//            throw new NotImplementedException();
//        }

//        public IAsyncEnumerable<Positions> JoinChannel(JoinChannel joinChannel) => game.GetReader();
        

//        public Task<OrType<GameCreated, GameJoined, Exception>> Send(CreateOrJoinGame createOrJoinGame)
//        {
//            throw new NotImplementedException();
//        }

//        public void Send(LeaveGame leave)
//        {
//            throw new NotImplementedException();
//        }

//        public void Send(ResetGame inputs)
//        {
//        }

//        public void Send(string game, ColorChanged colorChanged)
//        {
//            this.game.ColorChanged(colorChanged);
//        }

//        public void Send(string game, CreatePlayer createPlayer, Action<ObjectsCreated> handleObjectsCreated, Action<ObjectsRemoved> handleObjectsRemoved, Action<UpdateScore> handleUpdateScore, Action<ColorChanged> handleColorChanged, Action<NameChanged> handleNameChanged)
//        {
//            throw new NotImplementedException();
//        }

//        public void Send(string game, IAsyncEnumerable<PlayerInputs> inputs)
//        {
//            throw new NotImplementedException();
//        }

//        public void Send(string game, NameChanged nameChanged)
//        {
//            throw new NotImplementedException();
//        }

//        public void SetOnClosed(Func<Exception, Task> onClosed)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}