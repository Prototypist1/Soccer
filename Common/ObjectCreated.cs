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
        public ObjectCreated(double x, double y, int z, Guid id, double diameter, byte r, byte g, byte b, byte a)
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
        }
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


    public class FootCreated : ObjectCreated
    {
        public FootCreated(double x, double y, int z, Guid id, double diameter, byte r, byte g, byte b, byte a, string name) : base(x, y, z, id, diameter, r, g, b, a)
        {
            this.Name = name;
        }
        public string Name { get; set; }

    }

    public class BodyCreated: ObjectCreated
    {
        public BodyCreated(double x, double y, int z, Guid id, double diameter, byte r, byte g, byte b, byte a) : base(x, y, z, id, diameter, r, g, b, a)
        {
        }

    }

    public class GoalCreated : ObjectCreated
    {
        public GoalCreated(double x, double y, int z, Guid id, double diameter, byte r, byte g, byte b, byte a) : base(x, y, z, id, diameter, r, g, b, a)
        {
        }
    }

    public class BallCreated : ObjectCreated
    {
        public BallCreated(double x, double y, int z, Guid id, double diameter, byte r, byte g, byte b, byte a) : base(x, y, z, id, diameter, r, g, b, a)
        {
        }
    }

    public struct ObjectsCreated {
        public ObjectsCreated(FootCreated[] feet, BodyCreated[] bodies, BallCreated ball, GoalCreated[] goals)
        {
            Feet = feet ?? throw new ArgumentNullException(nameof(feet));
            Bodies = bodies ?? throw new ArgumentNullException(nameof(bodies));
            Ball = ball;
            Goals = goals ?? throw new ArgumentNullException(nameof(goals));
        }

        public FootCreated[] Feet { get; set; }
        public BodyCreated[] Bodies { get; set; }
        public BallCreated Ball { get; set; }
        public GoalCreated[] Goals { get; set; }

    }

}
