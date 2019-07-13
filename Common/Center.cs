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

            X += vx;
            Y += vy;

            if (X > maxX)
            {
                X = maxX;
                vx = Math.Min(0, vx);
            }

            if (Y > maxY)
            {
                Y = maxY;
                vy = Math.Min(0, vy);
            }

            if (X < minX)
            {
                X = minX;
                vx = Math.Max(0, vx);
            }

            if (Y < minY)
            {
                Y = minY;
                vy = Math.Max(0, vy);
            }

            fx = -vx / 100.0;
            fy = -vy / 100.0;

        }
    }
}