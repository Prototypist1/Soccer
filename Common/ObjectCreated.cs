using System;

namespace Common
{

    public struct ObjectCreated
    {
        public ObjectCreated(double x, double y, Guid id, double diameter, byte r, byte g, byte b, byte a)
        {
            X = x;
            Y = y;
            Id = id;
            Diameter = diameter;
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public double X { get; set; }
        public double Y { get; set; }
        public Guid Id { get; set; }
        public double Diameter { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }
    }

    public struct ObjectsCreated {
        public ObjectCreated[] Objects { get; set; }

        public ObjectsCreated(ObjectCreated[] objects)
        {
            this.Objects = objects ?? throw new ArgumentNullException(nameof(objects));
        }
    }

}
