using Physics2;
using System;
using System.Collections.Generic;
using System.Text;

namespace physics2
{

    public class PhysicsObject : IPhysicsObject, IUpdatePosition
    {

        public PhysicsObject(double mass, double x, double y, bool mobile)
        {
            Mass = mass;
            X = x;
            Y = y;
            Mobile = mobile;
        }

        public double Vx { get; protected set; }
        public double Vy { get; protected set; }
        public double X { get; protected set; }
        public double Y { get; protected set; }
        public double Mass { get; }
        public double Speed => Math.Sqrt((Vx * Vx) + (Vy * Vy));
        public bool Mobile { get; }
        public Vector Velocity
        {
            get
            {
                return new Vector(Vx, Vy);
            }
        }

        public Vector Position => new Vector(X, Y);

        public virtual void UpdateVelocity(double vx, double vy) {
            this.Vx = vx;
            this.Vy = vy;
        }

        public void ApplyForce(double fx, double fy)
        {
            UpdateVelocity(Vx + (fx / Mass), Vy + (fy / Mass));
        }

        public virtual void Update(double step, double timeLeft)
        {
            X += Vx * step;
            Y += Vy * step;
        }
    }

}
