

namespace RemoteSoccer
{

    public class FollowBodyScaler : IScaler
    {

        private readonly double times, xPlus, yPlus;

        public FollowBodyScaler(
            double times,
            double centerX,
            double centerY,
            double windowWidth,
            double windowHeight)
        {
            this.times = times;

            xPlus = (windowWidth / 2.0) - (centerX * times);
            yPlus = (windowHeight / 2.0) - (centerY * times);
        }

        public double ScaleX(double x)
        {
            return (x * times) + xPlus;

        }

        public double ScaleY(double y)
        {
            return (y * times) + yPlus;
        }

        public double Scale(double diameter)
        {
            return diameter * times;
        }
        public double UnScaleX(double x)
        {
            return (x - xPlus) / times;
        }

        public double UnScaleY(double y)
        {
            return (y - yPlus) / times;
        }

        public double UnScale(double diameter)
        {
            return diameter / times;
        }
    }


    public class DontScale : IScaler
    {
        public double Scale(double diameter) => diameter;

        public double ScaleX(double x) => x;

        public double ScaleY(double y) => y;

        public double UnScale(double diameter) => diameter;

        public double UnScaleX(double x) => x;

        public double UnScaleY(double y) => y;
    }

    public class Scaler : IScaler
    {
        private const double padding = 10;
        private readonly double times, xPlus, yPlus;

        public Scaler(double windowWidth, double windowHeight, double gameWidth, double gameHeight)
        {
            var scaleX = (windowWidth - (2 * padding)) / gameWidth;
            var scaleY = (windowHeight - (2 * padding)) / gameHeight;

            if (scaleX < scaleY)
            {
                xPlus = padding;
                times = scaleX;
                yPlus = (windowHeight - (gameHeight * times)) / 2.0;

            }
            else
            {

                yPlus = padding;
                times = scaleY;
                xPlus = (windowWidth - (gameWidth * times)) / 2.0;
            }
        }

        public double ScaleX(double x)
        {
            return (x * times) + xPlus;

        }

        public double ScaleY(double y)
        {
            return (y * times) + yPlus;
        }

        public double Scale(double diameter)
        {
            return diameter * times;
        }

        public double UnScaleX(double x)
        {
            return (x - xPlus) /times ;
        }

        public double UnScaleY(double y)
        {
            return (y - yPlus) / times;
        }

        public double UnScale(double diameter)
        {
            return diameter / times;
        }
    }
}
