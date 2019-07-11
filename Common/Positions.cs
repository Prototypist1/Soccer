using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public struct Position {
        public readonly double X;
        public readonly double Y;
        public readonly Guid Id;

        public Position(double x, double y, Guid id)
        {
            X = x;
            Y = y;
            Id = id;
        }
    }

    public struct Positions
    {
        public readonly Position[] positions;

        public Positions(Position[] positions)
        {
            this.positions = positions ?? throw new ArgumentNullException(nameof(positions));
        }
    }
}
