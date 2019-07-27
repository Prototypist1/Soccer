using Physics;
using System;

namespace Common
{
    public class Center 
    {
        public double Y { get; private set; }
        public double X { get; private set; }
        public double vx, vy, maxX, minX, minY, maxY;
        private double fx, fy;

        public Center(double x, double y, double maxX, double minX, double minY, double maxY)
        {
            Y = y;
            X = x;
            this.maxX = maxX;
            this.minX = minX;
            this.minY = minY;
            this.maxY = maxY;
        }

        public void ApplyForce(double fx, double fy)
        {
            this.fx += fx;
            this.fy += fy;
        }

        public void Update()
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
                vx = X - lastX;
            }

            if (Y > maxY)
            {
                Y = maxY;
                vy = Y- lastY;
            }

            if (X < minX)
            {
                X = minX;
                vx = X - lastX;
            }

            if (Y < minY)
            {
                Y = minY;
                vy = Y - lastY;
            }

            fx = 0;
            fy = 0;

        }
    }
}