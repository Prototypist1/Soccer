using Physics2;
using System;
using System.Collections.Generic;
using System.Text;

namespace physics2
{

    public class PassThroughPhysicObject : IPhysicsObject {
        IPhysicsObject backing;
        Vector offset;

        public PassThroughPhysicObject(IPhysicsObject backing, Vector offset)
        {
            this.backing = backing ?? throw new ArgumentNullException(nameof(backing));
            this.offset = offset;
        }

        public double Mass
        {
            get
            {
                return backing.Mass;
            }
        }

        public bool Mobile
        {
            get
            {
                return backing.Mobile;
            }
        }

        public Vector Position
        {
            get
            {
                return backing.Position;
            }
        }

        public double Speed
        {
            get
            {
                return backing.Speed;
            }
        }

        public Vector Velocity
        {
            get
            {
                return backing.Velocity;
            }

            set
            {
                backing.Velocity = value;
            }
        }

        public double Vx
        {
            get
            {
                return backing.Vx;
            }

            set
            {
                backing.Vx = value;
            }
        }

        public double Vy
        {
            get
            {
                return backing.Vy;
            }

            set
            {
                backing.Vy = value;
            }
        }

        public double X
        {
            get
            {
                return backing.X + offset.x;
            }

            set
            {
                backing.X = value - offset.x;
            }
        }

        public double Y
        {
            get
            {
                return backing.Y + offset.y;
            }

            set
            {
                backing.Y = value - offset.y;
            }
        }

        public void ApplyForce(double fx, double fy)
        {
            backing.ApplyForce(fx, fy);
        }
    }

    public class PhysicsObject : IPhysicsObject
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
