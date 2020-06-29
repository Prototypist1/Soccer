using Common;
using Physics2;
using System.Collections.Generic;
using System.Linq;

namespace physics2
{
    public class Player : PhysicsObject
    {
        public bool Throwing = false;

        public int LastHadBall { get; internal set; } = -Constants.ThrowTimeout;
        public Center Body { get; internal set; }

        public readonly double Padding;
        //private readonly double length;
        //public List<PointCloudPartical> particles ;

        // position is realitive to player
        //public struct PointCloudPartical
        //{
        //    public readonly double X, Y, Vx, Vy, Scale;

        //    public PointCloudPartical(double x, double y, double vx, double vy, double scale)
        //    {
        //        X = x;
        //        Y = y;
        //        Vx = vx;
        //        Vy = vy;
        //        Scale = scale;
        //    }

        //    public PointCloudPartical Update(double step)
        //    {
        //        return new PointCloudPartical(X + Vx * step, Y + Vy * step, Vx, Vy, Scale);
        //    }

        public Player(double mass, double x, double y, bool mobile, /*double length, */double padding) : base(mass, x, y, mobile)
        {
            //this.length = length;
            this.Padding = padding;
            //particles = new List<PointCloudPartical>();
            //for (int i = -10; i <= 10; i++)
            //{
            //    particles.Add(new PointCloudPartical(X, Y, Vx, Vy,i/10.0));
            //}
        }

        public override void UpdateVelocity(double vx, double vy)
        {
            base.UpdateVelocity(vx, vy);

            //var parallelVector = GetParallelVector();

            //var targetPoints = particles.Select(x=> new Vector(X,Y)
            //    .NewAdded(parallelVector.NewScaled(x.Scale))
            //    .NewAdded(new Vector(Vx,Vy)))
            //    .ToList();

            //var firstParticle = new Vector(particles.First().X, particles.First().Y);
            //var firstTarget = new Vector(targetPoints.First().x, targetPoints.First().x);
            //var lastTarget = new Vector(targetPoints.Last().y, targetPoints.Last().y);

            //if (firstParticle.Distance(firstTarget) > firstParticle.Distance(lastTarget)) {
            //    targetPoints.Reverse();
            //}

            //particles = particles.Zip(targetPoints, (old,target) => {
            //    return new PointCloudPartical(
            //        old.X,
            //        old.Y,
            //        target.x - old.X,
            //        target.y - old.Y,
            //        old.Scale);
            //  }  ).ToList();

            //if (new Vector(vx, vy).Length != 0)
            //{
            //    start.Tx = GetParallelVector().x;
            //    start.Ty = GetParallelVector().y;

            //}
            //else {
            //    start.Tx = 0;
            //    start.Ty = 0;
            //}

            //end.Tx = -start.Tx;
            //end.Ty = -start.Ty;

            //var s = new Vector(start.X, start.Y).Length == 0? new Vector(start.Tx,start.Ty) :new Vector(start.X, start.Y).NewUnitized().NewScaled(GetLength()/2.0);
            //start.X = s.x;
            //start.Y = s.y;

            //var e = new Vector(end.X, end.Y).Length == 0 ? new Vector(end.Tx, end.Ty) : new Vector(end.X, end.Y).NewUnitized().NewScaled(GetLength() / 2.0);
            //end.X = e.x;
            //end.Y = e.y;

        }

        public override void Update(double step, double timeLeft)
        {
        //    start.X = (start.X * ((timeLeft - step) / timeLeft)) + (start.Tx * (step / timeLeft));
        //    start.Y = (start.Y * ((timeLeft - step) / timeLeft)) + (start.Ty * (step / timeLeft));

        //    end.X = (end.X * ((timeLeft - step) / timeLeft)) + (end.Tx * (step / timeLeft)); ;
        //    end.Y = (end.Y * ((timeLeft - step) / timeLeft)) + (end.Ty * (step / timeLeft));

            base.Update(step, timeLeft);

            //particles = particles.Select(x => x.Update(step)).ToList();
        }


        //public double GetLength() => GetParallelVector().Length * 2;

        //public Vector GetParallelVector()
        //{
        //    // duplicate code
        //    // serach for {3E1769BA-B690-4440-87BE-C74113D0D5EC}
        //    var v = new Physics2.Vector(-Vy*4  , Vx * 4);
        //    if (v.Length > length * .5)
        //    {
        //        v = v.NewScaled(length * .5 / v.Length);
        //    }
        //    return v;
        //}

    }

}
