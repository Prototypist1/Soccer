//using Common;
//using Physics2;
//using System;

//namespace physics2
//{
//    public class Ball : PhysicsObject
//    {
//        private readonly Circle circle;

//        public Player OwnerOrNull= null;

//        public Ball(double mass, double x, double y, bool mobile, Circle circle) : base(mass, x, y, mobile)
//        {

//            this.circle = circle ?? throw new System.ArgumentNullException(nameof(circle));
//        }

//        public void Reset(double x, double y) {
//            this.X = x;
//            this.Y = y;
//            this.Vx = 0;
//            this.Vy = 0;
//            this.OwnerOrNull = null;
//        }

//        public Circle GetCircle()
//        {
//            return circle;
//        }

//        //public void ConsiderThrowing(int frame) {
//        //    if (OwnerOrNull != null && OwnerOrNull.Throwing) {
//        //        if (throwingSpeedHit)
//        //        {
//        //            if (OwnerOrNull.Velocity.Length < Constants.MimimunThrowingSpped)
//        //            {
//        //                this.OwnerOrNull.LastHadBall = frame;
//        //                this.OwnerOrNull = null;
//        //                this.throwingSpeedHit = false;
                        
//        //            }
//        //        }
//        //        else if (OwnerOrNull.Velocity.Length > Constants.MimimunThrowingSpped)
//        //        {
//        //            throwingSpeedHit = true;
//        //        }
//        //        if (new Random().Next(1000) == 5) {
//        //            var db = 0;
//        //        }
//        //    }
//        //    if (OwnerOrNull != null && !OwnerOrNull.Throwing) {
//        //        this.throwingSpeedHit = false;
//        //    }
//        //}
//    }

//}
