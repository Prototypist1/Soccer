using Common;
using Physics2;
using System;
using System.Linq;

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

        public double GetTimes() => Math.Min(this.viewFrameWidth / ((centerX * 2) + Constants.footLen), this.viewFrameHeight / ((centerY * 2) + Constants.footLen));

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

        public void SetBallId(Guid guid)
        {
        }
    }

    class ShowAllPositions : IZoomer
    {

        private double viewFrameWidth;
        private double viewFrameHeight;
        private double centerX;
        private double centerY;
        private double times;
        private Guid? ballId;
        private FieldDimensions fieldDimensions;

        public ShowAllPositions(double viewFrameWidth, double viewFrameHeight, FieldDimensions fieldDimensions)
        {
            this.viewFrameWidth = viewFrameWidth;
            this.viewFrameHeight = viewFrameHeight;
            this.times = 0;
            this.fieldDimensions = fieldDimensions;
        }

        public double GetTimes() => times;

        public (double, double, double, double) Update(Position[] positionsList)
        {
            var pos = positionsList.Where(x=> x.Id != ballId ).Select(x => new Vector(x.X, x.Y)).ToList();
            pos.Add(new Vector(Constants.footLen - Constants.playerPadding, Constants.footLen - Constants.playerPadding));
            pos.Add(new Vector(fieldDimensions.xMax - ((Constants.footLen) - Constants.playerPadding), fieldDimensions.yMax - ((Constants.footLen) - Constants.playerPadding)));

            var xMax = pos.Select(x => x.x).Max();
            var xMin = pos.Select(x => x.x).Min();
            var yMax = pos.Select(x => x.y).Max();
            var yMin = pos.Select(x => x.y).Min();
            centerX = (xMax + xMin) / 2.0; 
            centerY = (yMax + yMin) / 2.0;

            times = Math.Min(viewFrameWidth/(Math.Max(fieldDimensions.xMax * .75, (2* Constants.footLen) +  xMax - xMin)), viewFrameHeight / Math.Max(fieldDimensions.yMax*.75, (2 * Constants.footLen) + yMax - yMin));
            
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


        public void SetTimes(double _)
        {
        }

        public void SetBallId(Guid guid)
        {
            ballId = guid;
        }
    }
}
