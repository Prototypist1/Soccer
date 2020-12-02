using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteSoccer
{
    internal class TranslatingGameView : IGameView
    {
        //private readonly IGameView gameView;

        private readonly Guid /*localFoot, localOuter, localBody,*/ foot, outer, body;

        private bool TryTransfom(Guid guid)
        {
            if (guid == foot)
            {
                //localVersion = localFoot;
                return true;
            }
            if (guid == outer)
            {
                //localVersion = localOuter;
                return true;
            }
            if (guid == body)
            {
                //localVersion = localBody;
                return true;
            }
            //localVersion = default;
            return false;
        }


        public TranslatingGameView(Guid foot, Guid outer, Guid body) {
            //this.gameView = gameView;
            //this.localFoot = Guid.NewGuid();
            //this.localOuter = Guid.NewGuid();
            //this.localBody = Guid.NewGuid();
            this.foot = foot;
            this.outer = outer;
            this.body = body;
        }

        public void HandleColorChanged(ColorChanged colorChanged)
        {
            //if (TryTransfom(colorChanged.Id)) {
            //    gameView.HandleColorChanged(new ColorChanged (colorChanged.Id, colorChanged.R, colorChanged.G, colorChanged.B, colorChanged.A));
            //}
        }

        public void HandleNameChanged(NameChanged nameChanged)
        {
            //if (TryTransfom(nameChanged.Id))
            //{
            //    gameView.HandleNameChanged(new NameChanged(nameChanged.Id, nameChanged.Name));
            //}
        }

        public void HandleObjectsCreated(ObjectsCreated objectsCreated)
        {
            //var bodies = objectsCreated.Bodies.Where(x => TryTransfom(x.Id)).ToArray();

            //var feet = objectsCreated.Feet.Where(x => TryTransfom(x.Id)).ToArray();

            //if (bodies.Any() || feet.Any())
            //{
            //    gameView.HandleObjectsCreated(new ObjectsCreated(feet,bodies,null,new GoalCreated[] { }, new OuterCreated[] { },objectsCreated.LeftScore, objectsCreated.RightScore));
            //}
        }

        public void HandleObjectsRemoved(ObjectsRemoved objectsRemoved)
        {
            //var list = objectsRemoved.List.Where(x => TryTransfom(x.Id)).ToArray();

            //if (list.Any()) {
            //    gameView.HandleObjectsRemoved(new ObjectsRemoved(list));
            //}
        }

        public void HandleUpdateScore(UpdateScore updateScore)
        {
        }

        //public Position[] TransforPositions(Position[] positions) {
        //    var list = new Position[] { };
        //    try
        //    {

        //        list = positions.Where(x => TryTransfom(x.Id)).ToArray();
        //    }
        //    catch (Exception e)
        //    {
        //        var db = 0;
        //    }
        //    return list;
        //}

        internal Preview[] GetPreviews(Position[] local)
        {
            return local.SelectMany(x =>
            {
                if (TryTransfom(x.Id))
                {
                    return new Preview[] { new Preview(x.Id, x.X, x.Y,x.Id == foot,x.Vx,x.Vy,x.Throwing) };
                }
                return new Preview[] { };

            }).ToArray();
        }

        //public async IAsyncEnumerable<Positions> Filter(IAsyncEnumerable<Positions> positionss) {
        //    await foreach (var positions in positionss)
        //    {
        //        var list = TransforPositions(positions.PositionsList);

        //        if (list.Any())
        //        {
        //            // TODO
        //            // CountDownState should not be here 
        //            yield return new Positions(list, new Preview[] { }, positions.Frame, positions.CountDownState, new physics2.Collision[] { });
        //        }
        //    }
        //}

        public Task SpoolPositions(IAsyncEnumerable<Positions> positionss)
        {
            return Task.CompletedTask;
             //return gameView.SpoolPositions(Filter(positionss));
        }
    }
}