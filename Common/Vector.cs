﻿using System;

namespace Physics2
{
    public struct Vector
    {
        public readonly double x, y;

        public double Length => Math.Sqrt((x * x) + (y * y));

        public Vector(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public Vector NewUnitized()
        {
            var d = Math.Sqrt((x * x) + (y * y));
            if (d == 0)
            {
                var ahhhh = 0;
            }
            return new Vector(x / d, y / d);
        }

        public Vector NewScaled(double s)
        {
            return new Vector(x * s, y * s);
        }

        public Vector NewMinus()
        {
            return new Vector(-x, -y);
        }

        public Vector NewAdded(Vector other)
        {
            return new Vector(x + other.x, y + other.y);
        }


        public double Dot(Vector vector)
        {
            return (x * vector.x) + (y * vector.y);
        }

        internal double Distance(Vector position)
        {
            return Math.Sqrt(((this.x - position.x) * (this.x - position.x)) + ((this.y - position.y) * (this.y - position.y)));
        }

        public override string ToString() => $"{x}, {y}";
    }
}
