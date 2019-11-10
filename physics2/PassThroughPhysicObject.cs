using Physics2;
using System;

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

}
