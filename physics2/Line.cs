using Physics;
using Physics2;
using System;
using System.Collections.Generic;
using System.Text;

namespace physics2
{
    internal class Line 
    {
        /// <summary>
        /// end should be clockwise of start
        /// </summary>
        public Line(Vector start, Vector end)
        {
            Start = start;
            End = end;
            var directionUnit = end.NewAdded(start.NewMinus()).NewUnitized();

            NormalUnit = new Vector(-directionUnit.y, directionUnit.x);

            NormalDistance = end.Dot(NormalUnit);
        }

        public Vector NormalUnit { get; }
        public double NormalDistance { get; }
        public Vector Start { get; }
        public Vector End { get; }
    }

}
