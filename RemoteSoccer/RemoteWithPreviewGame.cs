using Common;
using Prototypist.TaskChain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteSoccer
{
    class RemoteWithPreviewGame : IGame
    {
        Guid foot, outer, body;
        private TranslatingGameView translatingGameView;
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

        public string GameName => remoteGame.GameName;

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



        //private IAsyncEnumerable<Positions> ConsolodatedPositions() { 

        //}


        //Positions local;
        //private async void ReadLocal(IAsyncEnumerable<Positions> asyncEnumerable)
        //{
        //    await foreach (var positions in asyncEnumerable)
        //    {
        //        local = positions;
        //    }
        //}


        //IAsyncEnumerable<Positions> localChannel = null;
        private async IAsyncEnumerable<Positions> AddLocal(IAsyncEnumerable<Positions> asyncEnumerable) {
            await foreach (var positions in asyncEnumerable)
            {

                concurrentLinkedList.RemoveStart();

                localGame.OverwritePositions(positions);

                foreach (var input in concurrentLinkedList)
                {
                    localGame.game.PlayerInputs(input);
                }
                var local = localGame.game.GetPosition();

                // let read local run
                //await Task.Yield();

                //Positions last = new Positions();


                //await foreach (var localPositions in localChannel)
                //{
                //    last = localPositions;
                //}

                //if (last.Equals( new Positions())) {
                //    yield return positions;
                //}

                //var myLocal = local;
                //if (myLocal.PositionsList == null) {
                //    yield return positions;
                //}
                //// it is wierd that I have to have localPositions
                //var localPositions = positions;
                var myPositions = positions;
                myPositions.PositionsList = positions.PositionsList.Union(translatingGameView.TransforPositions( local)).ToArray();
                yield return myPositions;
            }
        }

        public IAsyncEnumerable<Positions> JoinChannel(JoinChannel joinChannel)
        {
            //ReadLocal(localGame.JoinChannel(joinChannel));

            //ReadLocal(translatingGameView.Filter(localGame.JoinChannel(joinChannel)));
            //remoteGame.JoinChannel(joinChannel);//
            return AddLocal( remoteGame.JoinChannel(joinChannel));



                //AsyncEnumerableEx.Merge(
                //translatingGameView.Filter(localGame.JoinChannel(joinChannel)),
                //remoteGame.JoinChannel(joinChannel));
            //remoteGame.JoinChannel(joinChannel);// localGame.JoinChannel(joinChannel);

            //
        }


        private async IAsyncEnumerable<PlayerInputs> PassThrough(IAsyncEnumerable<PlayerInputs> inputs)
        {
            await foreach (var item in inputs)
            {
                concurrentLinkedList.Add(item);
                //localGame.PlayerInputs(item);
                yield return item;
            }
        }

        ConcurrentLinkedList<PlayerInputs> concurrentLinkedList = new ConcurrentLinkedList<PlayerInputs>();

        public void StreamInputs(IAsyncEnumerable<PlayerInputs> inputs)
        {
            remoteGame.StreamInputs(PassThrough(inputs));
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
            translatingGameView = new TranslatingGameView(gameView, foot, outer, body);
            localGame.SetCallbacks(translatingGameView);
            remoteGame.SetCallbacks(gameView);
        }

    }
}
