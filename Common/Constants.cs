﻿namespace Common
{
    public static class Constants {
        public const int fieldZ = 0;
        public const int lineZ = 1;
        public const int goalZ = 2;
        public const int bodyZ = 3;
        public const int bodyPreviewZ = 4;
        public const int textZ = 5;
        public const int footPreviewZ = 6;
        public const int footZ = 7;
        public const int ballZ = 8;
        public const int effectZ = 9;
        public const int mouseZ = 10;

        //public const double playerPadding = 200;


        public const double goalLen = 6000;
        public const double footLen = 5000;
        public const int PlayerRadius = 900 ;//750;//750;// 500;//300;
        public const int BallRadius = 650;//150;//250;//30;//
        public const double BallMass = 1;

        //public const double speedLimit = 500;

        public const double MaxLean = 0;

        public const double MimimunThrowingSpped = 150;

        public const double MinPlayerCollisionForce = 300;

        public const double ExtraBallTakeForce = 200;

        public const int ThrowTimeout = 5;
        public const int MaxDeltaV = 40;

        public const double EnergyAdd = .4;//250_000 ;//400;
        //public const double SpeedScale = 1;
        //public const double Add = 100;
        //public const double ToThe = 2;//1.9;
        // fastest you can move your foot
        public const double speedLimit = 1000;
        // firction on the ball
        public const double FrictionDenom = 500;
        public const int bodyStartAt = 70;
        public const int bodySpeedLimit = 250;
    }
}
