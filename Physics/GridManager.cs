using System;
using System.Collections.Generic;

namespace Physics
{
    internal class GridManager
    {


        public readonly HashSet<PhysicsObject>[,] Grid;
        public readonly double height;
        public readonly double width;
        public readonly double stepSize;

        public GridManager(double stepSize, double width, double height)
        {
            this.stepSize = stepSize;
            this.height = height;
            this.width = width;
            Grid = new HashSet<PhysicsObject>[(int)Math.Ceiling(width / stepSize)+1, (int)Math.Ceiling(height / stepSize)+1];

            for (var x = 0; x < (int)Math.Ceiling(width / stepSize)+1; x++)
            {
                for (var y = 0; y < (int)Math.Ceiling(height / stepSize)+1; y++)
                {
                    Grid[x, y] = new HashSet<PhysicsObject>();
                }
            }
        }
    }
}
