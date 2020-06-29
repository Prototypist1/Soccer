
using physics2;
using Physics2;
using System;

namespace Common
{

    public class CenterFoot : IPhysicsObject {
        private readonly Center center;
        private readonly Player foot;

        public CenterFoot(Center center, Player foot)
        {
            this.center = center ?? throw new ArgumentNullException(nameof(center));
            this.foot = foot ?? throw new ArgumentNullException(nameof(foot));
        }

        public double Mass
        {
            get
            {
                return foot.Mass;
            }
        }

        public bool Mobile
        {
            get
            {
                return foot.Mobile;
            }
        }

        public Vector Position
        {
            get
            {
                return foot.Position;
            }
        }

        public double Speed
        {
            get
            {
                return foot.Speed;
            }
        }

        public Vector Velocity
        {
            get
            {
                return foot.Velocity;
            }
        }

        public double Vx
        {
            get
            {
                return foot.Vx;
            }
        }

        public double Vy
        {
            get
            {
                return foot.Vy;
            }
        }

        public double X
        {
            get
            {
                return foot.X;
            }
        }

        public double Y
        {
            get
            {
                return foot.Y;
            }
        }

        public void ApplyForce(double fx, double fy)
        {
            foot.ApplyForce(fx, fy);
            center.ApplyForce(fx*(center.Mass/foot.Mass), fy * (center.Mass / foot.Mass));
        }

        public void UpdateVelocity(double vx, double vy)
        {
            var fx = (vx - foot.Vx) * foot.Mass;
            var fy = (vy - foot.Vy) * foot.Mass;
            ApplyForce(fx, fy);
        }
    }

    public class Center //: IPhysicsObject
    {
        public double Mass => 1;
        public bool Mobile => true;
        public Vector Position => new Vector(X, Y);
        public double Speed => Math.Sqrt((Vx * Vx) + (Vy * Vy));
        public Vector Velocity => new Vector(Vx, Vy);
        public double Y { get; private set; }
        public double X { get; private set; }
        public Player Foot { get; }
        public double Vx { get; private set; } 
        public double Vy { get; private set; }
        private double radius;

        public double leanX=0, leanY=0;

        public Guid LeanId { get; }


        public Center(double x, double y, Player foot, double radius, Guid leanId)
        {
            Y = y;
            X = x;
            Foot = foot ?? throw new ArgumentNullException(nameof(foot));
            this.radius = radius;
            LeanId = leanId;
        }

        public void UpdateVelocity(double vx, double vy)
        {
            Vx = vx;
            Vy = vy;

        }

        public void ApplyForce(double fx, double fy)
        {
            Vx += fx;
            Vy += fy;
        }

        //public void Update(bool useBallWall, (double x, double y, double radius) ballWall) {
        //    Update(useBallWall, ballWall, this.leanX, this.leanY);
        //}

        public void Update(bool useBallWall,(double x, double y, double radius) ballWall, double NextleanX, double NextleanY)
        {
            var lastLeanX = this.leanX;
            var lastLeanY = this.leanY;

            this.leanX = NextleanX;
            this.leanY = NextleanY;

            X += Vx;
            Y += Vy;

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