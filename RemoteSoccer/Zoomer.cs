using Common;
using System;

namespace RemoteSoccer
{

    class Zoomer : IZoomer
    {

        private double viewFrameWidth;
        private double viewFrameHeight;
        private Guid body;
        private double times = .1;

        public Zoomer(double viewFrameWidth, double viewFrameHeight, Guid body)
        {
            this.viewFrameWidth = viewFrameWidth;
            this.viewFrameHeight = viewFrameHeight;
            this.body = body;
        }

        public double GetTimes() => times;

        public (double, double, double, double) Update(Position[] positionsList)
        {

            foreach (var position in positionsList)
            {
                if (position.Id == body)
                {
                    return (position.X,
                        position.Y,
                        (viewFrameWidth / 2.0) - (position.X * times),
                    (viewFrameHeight / 2.0) - (position.Y * times));
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
