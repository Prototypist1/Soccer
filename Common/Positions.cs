using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public struct Position {
        public double X { get; set; }
        public double Y { get; set; }
        public Guid Id { get; set; }

        public Position(double x, double y, Guid id)
        {
            X = x;
            Y = y;
            Id = id;
        }
    }

    public struct Positions
    {
        public Position[] PositionsList { get; set; }
        public int Frame { get; set; }
        public CountDownState CountDownState { get;set;}

        public Positions(Position[] positions, int frame, CountDownState countDownState)
        {
            this.PositionsList = positions ?? throw new ArgumentNullException(nameof(positions));
            Frame = frame;
            this.CountDownState = countDownState;
        }
    }
}
