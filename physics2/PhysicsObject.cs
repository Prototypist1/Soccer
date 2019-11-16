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

        public double Vx { get; set; }
        public double Vy { get; set; }
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
            set
            {
                Vx = value.x;
                Vy = value.y;
            }
        }

        public Vector Position => new Vector(X, Y);

        public void ApplyForce(double fx, double fy)
        {
            Vx += fx / Mass;
            Vy += fy / Mass;
        }

        public virtual void Update(double step, double timeLeft)
        {
            X += Vx * step;
            Y += Vy * step;
        }
    }

}
