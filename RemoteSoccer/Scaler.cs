using Physics;

namespace RemoteSoccer
{
    public class Scaler {
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
            else {

                yPlus = padding;
                times = scaleY;
                xPlus = (windowWidth - (gameWidth * times)) / 2.0;
            }
        }

        public double ScaleX(double x)
        {
            return(x * times) + xPlus;

        }

        public double ScaleY(double y)
        {
            return (y * times) + yPlus;
        }

        public double Scale(double diameter) {
            return diameter * times;
        }

    }
}
