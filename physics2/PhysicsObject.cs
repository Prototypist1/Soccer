using Physics2;
using System;
using System.Collections.Generic;
using System.Text;

namespace physics2
{

    public class PhysicsObject
    {

        public PhysicsObject(double mass, double x, double y)
        {
            Mass = mass;
            X = x;
            Y = y;
        }

        public double Vx { get; set; }
        public double Vy { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
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

    }

}
