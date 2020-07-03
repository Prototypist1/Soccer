using Common;
using Physics2;
using System;
using System.Collections.Generic;
using System.Linq;

namespace physics2
{
    public class Player : IPhysicsObject, IUpdatePosition
    {
        public readonly Guid id;
        public Player(double x, double y, double padding, Guid id)
        {
            this.X = x;
            this.Y = y;
            //this.length = length;
            this.Padding = padding;
            this.id = id;
            //particles = new List<PointCloudPartical>();
            //for (int i = -10; i <= 10; i++)
            //{
            //    particles.Add(new PointCloudPartical(X, Y, Vx, Vy,i/10.0));
            //}
        }

        public bool Throwing = false;

        public int LastHadBall { get; internal set; } = -Constants.ThrowTimeout;
        public Center Body { get; internal set; }

        public readonly double Padding;
        public double personalVx, personalVy;


        private double privateMass = 1;
        public double Mass => Body.Mass + privateMass;
        public bool Mobile => Body.Mobile;
        public Vector Position => new Vector(X, Y);
        public double Speed => Math.Sqrt((Vx * Vx) + (Vy * Vy));
        public Vector Velocity => new Vector(Vx, Vy);
        public double Y { get; private set; }
        public double X { get; private set; }

        public double Vx => personalVx + Body.Vx;
        public double Vy => personalVy + Body.Vy;

        public void ApplyForce(double fx, double fy)
        {
            personalVx += (fx / 3.0) / privateMass;
            personalVy += (fy / 3.0) / privateMass;
            Body.ApplyForce(fx* 2.0 / 3.0, fy * 2.0 / 3.0);
        }

        public void Update(double step, double timeLeft)
        {
            X += Vx * step;
            Y += Vy * step;
        }
    }

}
