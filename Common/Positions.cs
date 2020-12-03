using physics2;
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
        // it is a little weird for Position to have Throwing
        // because it is just a general position
        // I use it for the ball and the foot and the body
        // this is an obviously stupid design
        // I should have a different kinds of positions
        //public bool Throwing { get; set; }

        public Position(double x, double y, Guid id, double vx, double vy)
        {
            X = x;
            Y = y;
            Id = id;
            Vx = vx;
            Vy = vy;
           // Throwing = thowing;
        }
    }



    public struct Preview {
        public Preview(Guid id, double x, double y, bool isFoot
            //, double vx, double vy, bool throwing
            )
        {
            Id = id;
            X = x;
            Y = y;
            IsFoot = isFoot;
            //Vx = vx;
            //Vy = vy;
            //Throwing = throwing;
        }

        public bool IsFoot { get; set; }
        public Guid Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        //public double Vx { get; set; }
        //public double Vy { get; set; }
        //public bool Throwing { get; set; }
    }

    public struct Positions
    {
        public Position[] PositionsList { get; set; }
        public Preview[] Previews { get; set; }
        public Collision[] Collisions { get; set; }
        public int Frame { get; set; }
        public CountDownState CountDownState { get;set;}

        public Positions(Position[] positionsList, Preview[] previews, int frame, CountDownState countDownState, Collision[] collisions)
        {
            this.PositionsList = positionsList ?? throw new ArgumentNullException(nameof(positionsList));
            this.Previews = previews;
            Frame = frame;
            this.CountDownState = countDownState;
            Collisions = collisions ?? throw new ArgumentNullException(nameof(collisions));
        }
    }
}
