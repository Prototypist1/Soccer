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

        public GridManager(double stepSize, double height, double width)
        {
            this.stepSize = stepSize;
            this.height = height;
            this.width = width;
            Grid = new HashSet<PhysicsObject>[(int)Math.Ceiling(width / stepSize), (int)Math.Ceiling(height / stepSize)];

            for (var x = 0; x < (int)Math.Ceiling(width / stepSize); x++)
            {
                for (var y = 0; y < (int)Math.Ceiling(height / stepSize); y++)
                {
                    Grid[x, y] = new HashSet<PhysicsObject>();
                }
            }
        }
    }
}
