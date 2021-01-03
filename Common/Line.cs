//using Physics2;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace physics2
//{
//    public class Line 
//    {
//        /// <summary>
//        /// end should be clockwise of start
//        /// </summary>
//        public Line(Vector start, Vector end)
//        {
//            Start = start;
//            End = end;
//            DirectionUnit = end.NewAdded(start.NewMinus()).NewUnitized();

//            NormalUnit = new Vector(-DirectionUnit.y, DirectionUnit.x);

//            NormalDistance = end.Dot(NormalUnit);
//        }

//        public Vector DirectionUnit { get; }
//        public Vector NormalUnit { get; }
//        public double NormalDistance { get; }
//        public Vector Start { get; }
//        public Vector End { get; }

//        public double Length => End.NewAdded(Start.NewMinus()).Length;
//        public Vector Center => Start.NewAdded(End).NewScaled(.5);
//    }

//}
