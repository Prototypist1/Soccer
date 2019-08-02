namespace Common
{
    public struct CountDownState
    {
        public CountDownState(bool countdown, double x, double y, double radius, double strokeThickness, double ballOpacity)
        {
            Countdown = countdown;
            X = x;
            Y = y;
            Radius = radius;
            StrokeThickness = strokeThickness;
            BallOpacity = ballOpacity;
        }

        public bool Countdown { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Radius { get; set; }
        public double StrokeThickness { get; set; }
        public double BallOpacity { get; set; }
    }
}
