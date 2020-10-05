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


        // throwing info
        public Vector proposedThrow = new Vector();
        public bool Throwing = false;
        public bool ForceThrow = false;

        public int LastHadBall { get; internal set; } = -Constants.ThrowTimeout;
        public Center Body { get; internal set; }

        public readonly double Padding;
        public double personalVx, personalVy;


        private double privateMass = 0;
        public double Mass => Body.Mass + privateMass;
        public bool Mobile => Body.Mobile;
        public Vector Position => new Vector(X, Y);
        public double Speed => Math.Sqrt((Vx * Vx) + (Vy * Vy));
        public Vector Velocity => new Vector(Vx, Vy);
        public double Y { get; set; }
        public double X { get; set; }

        public double Vx => personalVx + Body.Vx;
        public double Vy => personalVy + Body.Vy;

        public void ApplyForce(double fx, double fy)
        {

            //personalVx += fx / Mass;
            //personalVy += fy / Mass;

            //var fxForMe = (fx * (privateMass / Mass));
            //var fyForMe = (fy * (privateMass / Mass));
            //var fxOther = fx - fxForMe;
            //var fyOther = fy - fyForMe;
            //personalVx += fxForMe / Mass;
            //personalVy += fyForMe / Mass;
            //Body.ApplyForce(fxOther * (Body.Mass / Mass), fyOther * (Body.Mass / Mass));
            Body.ApplyForce(fx, fy);
        }

        public void Update(double step, double timeLeft)
        {
            X += Vx * step;
            Y += Vy * step;
        }
    }

}
