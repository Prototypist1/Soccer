
using physics2;
using Physics2;
using System;

namespace Common
{


    public class Outer : IPhysicsObject, IUpdatePosition
    {
        public Guid Id { get; }

        public Outer(double x, double y, Guid id)
        {
            Id = id;
            X = x;
            Y = y;
        }

        public double Mass => 10;
        public bool Mobile => true;

        public Vector Position => new Vector(X, Y);

        public double Speed => Velocity.Length;

        public Vector Velocity => new Vector(Vx, Vy);

        public double Vx { get;  set; }

        public double Vy { get;  set; }

        public double X
        {
            get; private set;
        }

        public double Y
        {
            get; private set;
        }

        public void ApplyForce(double fx, double fy)
        {
            Vx += fx / Mass;
            Vy += fy / Mass;
        }

        public void Update(double step, double timeLeft)
        {
            X += Vx * step;
            Y += Vy * step;
        }
    }

    public class Center : IPhysicsObject, IUpdatePosition
    {
        public Outer Outer { get; internal set; }
        public Player Foot { get; internal set; }

        public double personalVx, personalVy;
        private double radius;

        public double Mass => Outer.Mass;
        public bool Mobile => Outer.Mobile;
        public Vector Position => new Vector(X, Y);
        public double Speed => Math.Sqrt((Vx * Vx) + (Vy * Vy));
        public Vector Velocity => new Vector(Vx, Vy);
        public double Y { get; private set; }
        public double X { get; private set; }

        public double Vx => personalVx + Outer.Vx;
        public double Vy => personalVy + Outer.Vy;

        //public double leanX=0, leanY=0;

        //public Guid LeanId { get; }


        public Center(double x, double y, Player foot, double radius/*, Guid leanId*/)
        {
            Y = y;
            X = x;
            Foot = foot ?? throw new ArgumentNullException(nameof(foot));
            this.radius = radius;
            //LeanId = leanId;
        }

        public void ApplyForce(double fx, double fy)
        {
            Outer.ApplyForce(fx, fy);
        }

        public void Update(double step, double timeLeft)
        {
            X += Vx * step;
            Y += Vy * step;
        }

        //public void Update(bool useBallWall, (double x, double y, double radius) ballWall) {
        //    Update(useBallWall, ballWall, this.leanX, this.leanY);
        //}

        //public void Update(bool useBallWall,(double x, double y, double radius) ballWall, double NextleanX, double NextleanY)
        //    {
        //        var lastLeanX = this.leanX;
        //        var lastLeanY = this.leanY;

        //        this.leanX = NextleanX;
        //        this.leanY = NextleanY;

        //        X += Vx;
        //        Y += Vy;

        //        if (useBallWall)
        //        {
        //            var dis = new Vector(X - ballWall.x, Y - ballWall.y);

        //            if (dis.Length == 0)
        //            {
        //                dis = new Vector(0, .1);
        //            }

        //            if (dis.Length < ballWall.radius + radius)
        //            {
        //                dis = dis.NewUnitized().NewScaled(ballWall.radius + radius);
        //                X = ballWall.x + dis.x;
        //                Y = ballWall.y + dis.y;

        //                //// get the part going toward the center 
        //                //var partTowards = v.Dot(dis.NewUnitized().NewMinus());
        //                //// get the remaining part
        //                //var remainingPart = v.NewAdded(dis.NewUnitized().NewScaled(partTowards));

        //                //// scale the remaining part to be all the speed
        //                //var goal = remainingPart.NewUnitized().NewScaled(v.Length);
        //                //ApplyForce(goal.x - v.x, goal.y - v.y);

        //            }
        //        }

        //        X = X - lastLeanX + NextleanX;
        //        Y = Y - lastLeanY + NextleanY;

        //    }
    }
}