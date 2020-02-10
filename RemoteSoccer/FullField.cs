using Common;
using System;

namespace RemoteSoccer
{
    class FullField : IZoomer
    {

        private double viewFrameWidth;
        private double viewFrameHeight;
        private readonly double centerX;
        private readonly double centerY;

        public FullField(double viewFrameWidth, double viewFrameHeight, double centerX, double centerY)
        {
            this.viewFrameWidth = viewFrameWidth;
            this.viewFrameHeight = viewFrameHeight;
            this.centerX = centerX;
            this.centerY = centerY;

            
        }

        public double GetTimes() => Math.Min(this.viewFrameWidth / ((centerX * 2) + 200), this.viewFrameHeight / ((centerY * 2)+200));

        public (double, double, double, double) Update(Position[] positionsList)
        {
            var times = GetTimes();
            return (
                centerX,
                centerY,
                (viewFrameWidth / 2.0) - (centerX * times),
                (viewFrameHeight / 2.0) - (centerY * times));
        }

        public void UpdateWindow(double actualWidth, double actualHeight)
        {
            this.viewFrameWidth = actualWidth;
            this.viewFrameHeight = actualHeight;
        }


        public void SetTimes(double v)
        {
         //   times = v;
        }
    }
}
