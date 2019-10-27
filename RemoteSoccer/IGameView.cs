using Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RemoteSoccer
{
    interface IGameView
    {
        void HandleObjectsCreated(ObjectsCreated objectsCreated);
        void HandleUpdateScore(UpdateScore updateScore);
        void HandleObjectsRemoved(ObjectsRemoved objectsRemoved);
        void HandleColorChanged(ColorChanged colorChanged);
        void HandleNameChanged(NameChanged nameChanged);
        Task SpoolPositions(IAsyncEnumerable<Positions> positionss);
    }
}
