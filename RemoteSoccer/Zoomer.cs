using Common;
using System;

namespace RemoteSoccer
{

    class Zoomer : IZoomer
    {

        private double viewFrameWidth;
        private double viewFrameHeight;
        private Guid body;
        private double times;

        public Zoomer(double viewFrameWidth, double viewFrameHeight, Guid body, double times)
        {
            this.viewFrameWidth = viewFrameWidth;
            this.viewFrameHeight = viewFrameHeight;
            this.body = body;
            this.times = times;
        }

        public double GetTimes() => times;

        public (double, double, double, double) Update(GameState gs)
        {

            foreach (var position in gs.players)
            {
                if (position.Key == body)
                {
                    return (position.Value.PlayerBody.Position.x,
                        position.Value.PlayerBody.Position.y,
                        (viewFrameWidth / 2.0) - (position.Value.PlayerBody.Position.x * times),
                        (viewFrameHeight / 2.0) - (position.Value.PlayerBody.Position.y * times));
                }
            }

            throw new Exception("we are following something without a position");
        }

        public void UpdateWindow(double actualWidth, double actualHeight)
        {
            this.viewFrameWidth = actualWidth;
            this.viewFrameHeight = actualHeight;
        }

        public void SetTimes(double v)
        {
            times = v;
        }

        public void SetBallId(Guid guid)
        {
        }
    }
}
