using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public struct Position {
        public double X { get; set; }
        public double Y { get; set; }
        public double Vx { get; set; }
        public double Vy { get; set; }
        public Guid Id { get; set; }

        public Position(double x, double y, Guid id, double vx, double vy)
        {
            X = x;
            Y = y;
            Id = id;
            Vx = vx;
            Vy = vy;
        }
    }

    public struct Positions
    {
        public Position[] PositionsList { get; set; }
        public int Frame { get; set; }
        public CountDownState CountDownState { get;set;}

        public Positions(Position[] positionsList, int frame, CountDownState countDownState)
        {
            this.PositionsList = positionsList ?? throw new ArgumentNullException(nameof(positionsList));
            Frame = frame;
            this.CountDownState = countDownState;
        }
    }
}
