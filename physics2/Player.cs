using Physics2;

namespace physics2
{
    public class Player : PhysicsObject
    {
        public double Padding;
        private readonly double length;
        public readonly PointCloudPartical start, end;

        // position is realitive to player
        public class PointCloudPartical
        {
            public double X, Y, Tx, Ty;

            public PointCloudPartical(double x, double y, double tx, double ty)
            {
                X = x;
                Y = y;
                Tx = tx;
                Ty = ty;
            }
        }

        public Player(double mass, double x, double y, bool mobile, double length, double padding) : base(mass, x, y, mobile)
        {
            this.length = length;
            start = new PointCloudPartical(x, y, x, y);
            end = new PointCloudPartical(x, y, x, y);

        }

        public override void UpdateVelocity(double vx, double vy)
        {

            base.UpdateVelocity(vx, vy);

            if (new Vector(vx, vy).Length != 0)
            {
                start.Tx = GetParallelVector().x;
                start.Ty = GetParallelVector().y;

            }
            else {
                start.Tx = 0;
                start.Ty = 0;
            }

            end.Tx = -start.Tx;
            end.Ty = -start.Ty;

            var s = new Vector(start.X, start.Y).Length == 0? new Vector(start.Tx,start.Ty) :new Vector(start.X, start.Y).NewUnitized().NewScaled(GetLength()/2.0);
            start.X = s.x;
            start.Y = s.y;

            var e = new Vector(end.X, end.Y).Length == 0 ? new Vector(end.Tx, end.Ty) : new Vector(end.X, end.Y).NewUnitized().NewScaled(GetLength() / 2.0);
            end.X = e.x;
            end.Y = e.y;

        }

        public override void Update(double step, double timeLeft)
        {
            start.X = (start.X * ((timeLeft - step) / timeLeft)) + (start.Tx * (step / timeLeft));
            start.Y = (start.Y * ((timeLeft - step) / timeLeft)) + (start.Ty * (step / timeLeft));

            end.X = (end.X * ((timeLeft - step) / timeLeft)) + (end.Tx * (step / timeLeft)); ;
            end.Y = (end.Y * ((timeLeft - step) / timeLeft)) + (end.Ty * (step / timeLeft));

            base.Update(step, timeLeft);
        }


        public double GetLength() => GetParallelVector().Length * 2;

        private Vector GetParallelVector()
        {
            // duplicate code
            // serach for {3E1769BA-B690-4440-87BE-C74113D0D5EC}
            var v = new Physics2.Vector(-Vy * 20, Vx * 20);
            if (v.Length > length * .5)
            {
                v = v.NewScaled(length * .5 / v.Length);
            }
            return v;
        }

    }

}
