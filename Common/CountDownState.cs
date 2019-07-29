namespace Common
{
    public struct CountDownState
    {
        public CountDownState(bool countdown, double x, double y, double radius, int currentFrame, int finalFrame)
        {
            Countdown = countdown;
            X = x;
            Y = y;
            Radius = radius;
            CurrentFrame = currentFrame;
            FinalFrame = finalFrame;
        }

        public bool Countdown { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Radius { get; set; }
        public int CurrentFrame { get; set; }
        public int FinalFrame { get; set; }
    }
}
