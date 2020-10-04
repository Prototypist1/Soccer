using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteSoccer
{
    internal class TranslatingGameView : IGameView
    {
        private readonly IGameView gameView;

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


        public TranslatingGameView(IGameView gameView, Guid foot, Guid outer, Guid body) {
            this.gameView = gameView;
            this.localFoot = Guid.NewGuid();
            this.localOuter = Guid.NewGuid();
            this.localBody = Guid.NewGuid();
            this.foot = foot;
            this.outer = outer;
            this.body = body;
        }

        public void HandleColorChanged(ColorChanged colorChanged)
        {
            if (TryTransfom(colorChanged.Id, out var id)) {
                gameView.HandleColorChanged(new ColorChanged (id,colorChanged.R, colorChanged.G, colorChanged.B, colorChanged.A));
            }
        }

        public void HandleNameChanged(NameChanged nameChanged)
        {
            if (TryTransfom(nameChanged.Id, out var id))
            {
                gameView.HandleNameChanged(new NameChanged(id, nameChanged.Name));
            }
        }

        public void HandleObjectsCreated(ObjectsCreated objectsCreated)
        {
            var bodies = objectsCreated.Bodies.SelectMany(x =>
            {
                if (TryTransfom(x.Id, out var id))
                {
                    return new BodyCreated[] { new BodyCreated(
                        x.X,
                        x.Y,
                        x.Z,
                        id,
                        x.Diameter,
                        x.R,
                        x.G,
                        x.B,
                        (byte)(x.A/2),
                        "") };
                }
                return new BodyCreated[] { };

            }).ToArray();

            var feet = objectsCreated.Feet.SelectMany(x =>
            {
                if (TryTransfom(x.Id, out var id))
                {
                    return new FootCreated[] { new FootCreated(
                        x.X,
                        x.Y,
                        x.Z,
                        id,
                        x.Diameter,
                        x.R,
                        x.G,
                        x.B,
                        (byte)(x.A/2)) };
                }
                return new FootCreated[] { };

            }).ToArray();

            if (bodies.Any() || feet.Any())
            {
                gameView.HandleObjectsCreated(new ObjectsCreated(feet,bodies,null,new GoalCreated[] { }, new OuterCreated[] { }));
            }
        }

        public void HandleObjectsRemoved(ObjectsRemoved objectsRemoved)
        {
            var list = objectsRemoved.List.SelectMany(x =>
            {
                if (TryTransfom(x.Id, out var id)) { 
                    return new ObjectRemoved[] { new ObjectRemoved(id) }; 
                }
                return new ObjectRemoved[] { };

            }).ToArray();

            if (list.Any()) {
                gameView.HandleObjectsRemoved(new ObjectsRemoved(list));
            }
        }

        public void HandleUpdateScore(UpdateScore updateScore)
        {
        }

        public Position[] TransforPositions(Position[] positions) {
            var list = new Position[] { };
            try
            {

                list = positions.SelectMany(x =>
                {
                    if (TryTransfom(x.Id, out var id))
                    {
                        return new Position[] { new Position(x.X, x.Y, id, x.Vx, x.Vy) };
                    }
                    return new Position[] { };

                }).ToArray();
            }
            catch (Exception e)
            {
                var db = 0;
            }
            return list;
        }

        internal Preview[] GetPreviews(Position[] local)
        {
            return local.SelectMany(x =>
            {
                if (TryTransfom(x.Id, out var id))
                {
                    return new Preview[] { new Preview(id,x.X, x.Y) };
                }
                return new Preview[] { };

            }).ToArray();
        }

        public async IAsyncEnumerable<Positions> Filter(IAsyncEnumerable<Positions> positionss) {
            await foreach (var positions in positionss)
            {
                var list = TransforPositions(positions.PositionsList);

                if (list.Any())
                {
                    // TODO
                    // CountDownState should not be here 
                    yield return new Positions(list, new Preview[] { }, positions.Frame, positions.CountDownState, new physics2.Collision[] { });
                }
            }
        }

        public Task SpoolPositions(IAsyncEnumerable<Positions> positionss)
        {
             return gameView.SpoolPositions(Filter(positionss));
        }
    }
}