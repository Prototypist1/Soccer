using Physics;
using System;

namespace Common
{

    public class Center 
    {
        public double Y { get; private set; }
        public PhysicsObject Foot { get; }
        public double X { get; private set; }
        public double vx, vy, maxX, minX, minY, maxY , radius;
        private double fx, fy;

        public Center(double x, double y, double maxX, double minX, double minY, double maxY, PhysicsObject foot, double radius)
        {
            Y = y;
            X = x;
            this.maxX = maxX;
            this.minX = minX;
            this.minY = minY;
            this.maxY = maxY;
            Foot = foot ?? throw new ArgumentNullException(nameof(foot));
            this.radius = radius;
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

            if (X > maxX-radius)
            {
                X = maxX - radius;
            }

            if (Y > maxY - radius)
            {
                Y = maxY - radius;
            }

            if (X < minX + radius)
            {
                X = minX + radius;
            }

            if (Y < minY + radius)
            {
                Y = minY + radius;
            }

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

            fx = 0;
            fy = 0;

        }
    }
}