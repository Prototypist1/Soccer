using Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RemoteSoccer
{
    internal class TranslatingGameView : IGameView
    {
        private IGameView gameView;


        private readonly Guid localFoot, localOuter, localBody, foot, outer, body;

        private bool TryTransfom(Guid guid, out Guid localVersion)
        {
            if (guid == foot)
            {
                localVersion = localFoot;
                return true;
            }
            if (guid == outer)
            {
                localVersion = localOuter;
                return true;
            }
            if (guid == body)
            {
                localVersion = localBody;
                return true;
            }
            localVersion = default;
            return false;
        }


        public TranslatingGameView(IGameView gameView)
        {
            this.gameView = gameView;
        }

        public void HandleColorChanged(ColorChanged colorChanged)
        {
            throw new NotImplementedException();
            gameView.HandleColorChanged(colorChanged);
        }

        public void HandleNameChanged(NameChanged nameChanged)
        {
            throw new NotImplementedException();
            gameView.HandleNameChanged(nameChanged);
        }

        public void HandleObjectsCreated(ObjectsCreated objectsCreated)
        {
            throw new NotImplementedException();
            gameView.HandleObjectsCreated(objectsCreated);
        }

        public void HandleObjectsRemoved(ObjectsRemoved objectsRemoved)
        {
            throw new NotImplementedException();
            gameView.HandleObjectsRemoved(objectsRemoved);
        }

        public void HandleUpdateScore(UpdateScore updateScore)
        {
            throw new NotImplementedException();
            gameView.HandleUpdateScore(updateScore);
        }

        public Task SpoolPositions(IAsyncEnumerable<Positions> positionss)
        {
            throw new NotImplementedException();
            return gameView.SpoolPositions(positionss);
        }
    }
}