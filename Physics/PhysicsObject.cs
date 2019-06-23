using System;
using System.Collections.Generic;
using System.Text;
using static Physics.EventManager;

namespace Physics
{
    public interface IShape {

    }

    public class Ball: IShape
    {

    }

    public class  PhysicsObject
    {
        internal bool mobile = true;

        public PhysicsObject(double mass, double radious, double x, double y)
        {
            Mass = mass;
            Radius = radious;
            X = x;
            Y = y;
        }

        public double time { get; internal set; } = 0;
        public double Vx { get; set; }
        public double Vy { get; set; }
        public double X { get; internal set; }
        public double Y { get; internal set; }
        public double Mass { get; }
        public double Radius { get; }
        public double Speed => Math.Sqrt((Vx*Vx) + (Vy * Vy));
        public Vector Velocity { get {
                return new Vector(Vx, Vy); } set {
                Vx = value.x;
                Vy = value.y;
            } }

        public Vector Position => new Vector(X, Y);

        public void ApplyForce(double fx, double fy) {
            Vx += fx / Mass;
            Vy += fy / Mass;
        }


        public class Collision {
            public double time,x1,x2,y1,y2;
            public PhysicsObject o1,o2 ;
        }

        internal bool TryNextCollision(PhysicsObject that, double endTime, out IEvent collision)
        {
            double startTime, thisX0, thatX0, thisY0, thatY0, DX, DY;
            // how  are they moving relitive to us
            double DVX = that.Vx - this.Vx, 
                   DVY = that.Vy - this.Vy;

            if (this.time > that.time)
            {
                startTime = this.time;
                thisX0 = this.X;
                thisY0 = this.Y;
                thatX0 = that.X + (that.Vx * (this.time - that.time));
                thatY0 = that.Y + (that.Vy * (this.time - that.time));
            }
            else {
                startTime = that.time;
                thatX0 = that.X;
                thatY0 = that.Y;
                thisX0 = this.X + (this.Vx * (that.time - this.time));
                thisY0 = this.Y + (this.Vy * (that.time - this.time));
            }
            // how far they are from us
            DX = thatX0 - thisX0;
            DY = thatY0 - thisY0;


            // if the objects are not moving towards each other dont bother
            var V = -new Vector(DVX, DVY).Dot(new Vector(DX, DY).NewNormalized());
            if (V <= 0)
            {
                collision = default;
                return false;
            }


            var R = (this.Radius + that.Radius);

            var A = (DVX * DVX) + (DVY * DVY);
            var B = 2*((DX*DVX) + (DY*DVY));
            var C = (DX * DX) + (DY * DY) - (R * R);

            if ( TrySolveQuadratic(A,B,C, out var res) && startTime + res <= endTime ) {

                collision = new CollisiphysicsObjectEven(
                    startTime + res,
                    this,
                    that,
                    this.X + this.Vx * ((startTime + res) - this.time),
                    this.Y + this.Vy * ((startTime + res) - this.time),
                    that.X + that.Vx * ((startTime + res) - that.time),
                    that.Y + that.Vy * ((startTime + res) - that.time));
                
                return true;
            }
            collision = default;
            return false;
        }

        public bool TrySolveQuadratic(double a, double b, double c, out double res)
        {
            double sqrtpart = (b * b) - (4 * a * c);
            double x1, x2;
            if (sqrtpart > 0)
            {
                x1 = (-b + System.Math.Sqrt(sqrtpart)) / (2 * a);
                x2 = (-b - System.Math.Sqrt(sqrtpart)) / (2 * a);
                res = Math.Min(x1, x2);
                return true;
            }
            else if (sqrtpart < 0)
            {
                res = default;
                return false;
            }
            else
            {
                res = (-b + System.Math.Sqrt(sqrtpart)) / (2 * a);
                return true;
            }
        }
    }
}
