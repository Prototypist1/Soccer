
using physics2;
using Physics2;
using System;

namespace Common
{

    public class Center 
    {
        public double Y { get; private set; }
        public double X { get; private set; }
        public PhysicsObject Foot { get; }
        public double vx, vy, radius;

        public double leanX=0, leanY=0;

        public Guid LeanId { get; }


        public Center(double x, double y, PhysicsObject foot, double radius, Guid leanId)
        {
            Y = y;
            X = x;
            Foot = foot ?? throw new ArgumentNullException(nameof(foot));
            this.radius = radius;
            LeanId = leanId;
        }

        public void ApplyForce(double fx, double fy)
        {
            vx += fx;
            vy += fy;
        }

        public void Update(bool useBallWall, (double x, double y, double radius) ballWall) {
            Update(useBallWall, ballWall, this.leanX, this.leanY);
        }

        public void Update(bool useBallWall,(double x, double y, double radius) ballWall, double NextleanX, double NextleanY)
        {
            var lastLeanX = this.leanX;
            var lastLeanY = this.leanY;

            this.leanX = NextleanX;
            this.leanY = NextleanY;

            var v = new Vector(vx, vy);

            vx = v.x;
            vy = v.y;

            X += vx;
            Y += vy;

            if (useBallWall)
            {
                var dis = new Vector(X - ballWall.x, Y - ballWall.y);

                if (dis.Length == 0)
                {
                    dis = new Vector(0, .1);
                }

                if (dis.Length < ballWall.radius + radius)
                {
                    dis = dis.NewUnitized().NewScaled(ballWall.radius + radius);
                    X = ballWall.x + dis.x;
                    Y = ballWall.y + dis.y;

                    //// get the part going toward the center 
                    //var partTowards = v.Dot(dis.NewUnitized().NewMinus());
                    //// get the remaining part
                    //var remainingPart = v.NewAdded(dis.NewUnitized().NewScaled(partTowards));

                    //// scale the remaining part to be all the speed
                    //var goal = remainingPart.NewUnitized().NewScaled(v.Length);
                    //ApplyForce(goal.x - v.x, goal.y - v.y);
                    
                }
            }

            
            X = X - lastLeanX + NextleanX;
            Y = Y - lastLeanY + NextleanY;

        }
    }
}