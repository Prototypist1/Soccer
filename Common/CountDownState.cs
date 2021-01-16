using System;

namespace Common
{
    public class CountDownState
    {

        public CountDownState() { }
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

    public struct ColorChanged {
        public ColorChanged(Guid id, byte r, byte g, byte b, byte a)
        {
            Id = id;
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Guid Id { get; set; }
        public byte R { get; set;}
        public byte G { get; set;}
        public byte B { get; set;}
        public byte A { get; set; }
    }


    public struct NameChanged
    {
        public NameChanged(Guid id, string name)
        {
            Id = id;
            Name = name;
        }
        public Guid Id { get; set; }

        public string Name { get; set; }
    }
}
