
using physics2;
using Physics2;
using System;

namespace Common
{

    public class Center 
    {
        public double Y { get; private set; }
        public PhysicsObject Foot { get; }
        public double X { get; private set; }
        public double vx, vy, radius;

        public Center(double x, double y, PhysicsObject foot, double radius)
        {
            Y = y;
            X = x;
            Foot = foot ?? throw new ArgumentNullException(nameof(foot));
            this.radius = radius;
        }

        public void ApplyForce(double fx, double fy)
        {
            vx += fx;
            vy += fy;
        }

        public void Update(bool useBallWall,(double x, double y, double radius) ballWall)
        {

            var lastX = X;
            var lastY = Y;


            var v = new Vector(vx, vy);

            vx = v.x;
            vy = v.y;

            X += vx;
            Y += vy;

            //if (X > maxX)
            //{
            //    X = maxX;
            //}

            //if (Y > maxY)
            //{
            //    Y = maxY;
            //}

            //if (X < minX)
            //{
            //    X = minX;
            //}

            //if (Y < minY)
            //{
            //    Y = minY;
            //}

            if (useBallWall)
            {
                var dis = new Vector(X - ballWall.x, Y - ballWall.y);

                if (dis.Length == 0)
                {
                    dis = new Vector(0, .1);
                }

                if (dis.Length < ballWall.radius + radius)
                {
                    dis = dis.NewUnitized().NewScaled(ballWall.radius + radius);
                    X = ballWall.x + dis.x;
                    Y = ballWall.y + dis.y;
                }
            }

            vx = X - lastX;
            vy = Y - lastY;

        }
    }
}