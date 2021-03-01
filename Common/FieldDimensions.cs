﻿namespace Common
{
    public struct FieldDimensions {

        public double xMax;
        public double yMax;

        public static FieldDimensions Default = new FieldDimensions {
            xMax = 4* 16 * 5000 , //2 * 16 * 2500,
                yMax = 4* 9 * 5000,//2 * 9 * 2500
        };
    }
}
