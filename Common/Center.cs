using Physics;
using System;

namespace Common
{

    public class Center 
    {
        public double Y { get; private set; }
        public PhysicsObject Foot { get; }
        public double X { get; private set; }
        public double vx, vy, maxX, minX, minY, maxY;
        private double fx, fy;

        public Center(double x, double y, double maxX, double minX, double minY, double maxY, PhysicsObject foot)
        {
            Y = y;
            X = x;
            this.maxX = maxX;
            this.minX = minX;
            this.minY = minY;
            this.maxY = maxY;
            Foot = foot ?? throw new ArgumentNullException(nameof(foot));
        }

        public void ApplyForce(double fx, double fy)
        {
            this.fx += fx;
            this.fy += fy;
        }

        public void Update(bool useBallWall,(double x, double y, double radius) ballWall)
        {

            vx += fx;
            vy += fy;

            var lastX = X;
            var lastY = Y;

            X += vx;
            Y += vy;

            if (X > maxX)
            {
                X = maxX;
            }

            if (Y > maxY)
            {
                Y = maxY;
            }

            if (X < minX)
            {
                X = minX;
            }

            if (Y < minY)
            {
                Y = minY;
            }

            if (useBallWall)
            {
                var dis = new Vector(X - ballWall.x, Y - ballWall.y);

                if (dis.Length == 0)
                {
                    dis = new Vector(0, .1);
                }

                if (dis.Length < ballWall.radius)
                {
                    dis = dis.NewUnitized().NewScaled(ballWall.radius);
                    X = ballWall.x + dis.x;
                    Y = ballWall.y + dis.y;
                }
            }

            vx = X - lastX;
            vy = Y - lastY;

            fx = 0;
            fy = 0;

        }
    }
}