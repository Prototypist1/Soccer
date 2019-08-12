using System;
using System.Collections.Generic;

namespace Common
{

    public struct ObjectRemoved {
        public ObjectRemoved(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }

    public struct ObjectsRemoved
    {
        public ObjectsRemoved(ObjectRemoved[] list)
        {
            List = list ?? throw new ArgumentNullException(nameof(list));
        }

        public ObjectRemoved[] List { get; set; }

    }

    public class ObjectCreated
    {
        public ObjectCreated(double x, double y, int z, Guid id, double diameter, byte r, byte g, byte b, byte a, string name)
        {
            X = x;
            Y = y;
            Z = z;
            Id = id;
            Diameter = diameter;
            R = r;
            G = g;
            B = b;
            A = a;
            Name = name;
        }
        public string Name { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public int Z { get; set; }
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
