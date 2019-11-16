using Physics2;

namespace physics2
{
    public class Player : PhysicsObject
    {
        public double Padding;
        private readonly double length;
        public readonly PointCloudPartical start, end;

        public readonly PointCloudPartical[] line;


        public Player(double mass, double x, double y, bool mobile, double length, double padding) : base(mass, x, y, mobile)
        {
            this.length = length;
            start = new PointCloudPartical(this, -1, x, y);
            line = new[] {
                new PointCloudPartical(this,-.9,x,y),
                new PointCloudPartical(this,0,x,y),
                new PointCloudPartical(this,.9,x,y)
            };
            end = new PointCloudPartical(this, 1, x, y);

        }

        public override void Update(double step, double timeLeft)
        {
            foreach (var entry in line)
            {
                entry.Update(step, timeLeft);
            }
            start.Update(step, timeLeft);
            end.Update(step, timeLeft);

            base.Update(step, timeLeft);
        }

        public Vector GetVector()
        {

            // this is shared code
            var v = new Physics2.Vector(-Vy * 10, Vx * 10);
            if (v.Length > length * .5)
            {
                v = v.NewScaled(length * .5 / v.Length);
            }
            return v;
        }

    }

}
