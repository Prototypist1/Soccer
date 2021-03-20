using System;

namespace Common
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


        public const double goalLen = 10000;
        public const double ballWallLen = 15_000;
        public const int PlayerRadius = 2000;//1400;//;750 ;//750;//750;// 500;//300;
        public const double footLen = PlayerRadius * 3;//6000;//
        public const int BallRadius = 1600;//1100;//500;//150;//250;//30;//
        public const double BallMass = 1;

        //public const double speedLimit = 500;

        public const double MaxLean = 0;

        //public const double MimimunThrowingSpped = 150;

        public const double MinPlayerCollisionForce = 100;//= 600;
        public const double ExtraBallTakeForce = 400;//= 1000;
        public const double BallTakeForce = 200;


        public const int ThrowTimeout = 5;
        public const int MaxDeltaV = 40;

        public const double EnergyAdd = .4;//100;//.5;//250_000 ;//400;
        //public const double SpeedScale = 1;
        //public const double Add = 100;
        //public const double ToThe = 2;//1.9;
        // fastest you can move your foot
        public const double speedLimit = 800;//;1000;//2000;//3000;
        // firction on the ball
        public const double FrictionDenom = 150;
        public const int bodyStartAt = 300;//250;
        public const double bodyEpo = 1.01;//1.015;
        //public const int bodySpeedLimit = 500;
        public const int BoostSpread = 3;
        public const double ThrowScale = .12;
        public const double maxThrowPower = 1600;
        public const double BoostConsumption = .0001;
        public const double BoostFade = .5;



        public static Guid NoMove = Guid.Empty;
        public const double ExternalVelocityFriction = .96;
        public const double ExtraBoostCost = 1;
    }
}
