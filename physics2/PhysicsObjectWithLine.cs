using System;

namespace physics2
{
    public abstract class PhysicsObjectWithLine : PhysicsObject
    {
        public PhysicsObjectWithLine(double mass, double x, double y, bool mobile) : base(mass, x, y, mobile)
        {
        }

        public abstract Line GetLine();

        internal IPhysicsObject GetStart()
        {
            return new PassThroughPhysicObject(this, GetLine().Start.NewAdded(GetLine().Center.NewMinus()));
        }

        internal IPhysicsObject GetEnd()
        {
            return new PassThroughPhysicObject(this, GetLine().End.NewAdded(GetLine().Center.NewMinus()));
        }
    }

    public class PhysicsObjectWithFixedLine : PhysicsObjectWithLine
    {
        private readonly Line line;

        public PhysicsObjectWithFixedLine(double mass, Line line, bool mobile) : base(mass,line.Center.x , line.Center.y , mobile)
        {
            this.line = line;
        }

        public override Line GetLine() => line;
    }

    public class Player : PhysicsObjectWithLine
    {
        private readonly double length;

        public Player(double mass, double x, double y, bool mobile, double length) : base(mass, x, y, mobile)
        {
            this.length = length;
        }

        public override Line GetLine() {
            
            // this is shared code
            var v = new Physics2.Vector(Vx * 10, Vy * 10);
            if (v.Length > length*.5)
            {
                v = v.NewScaled(length * .5 / v.Length);
            }
            return new Line(
                new Physics2.Vector(X - v.y, Y + v.x),
                new Physics2.Vector(X + v.y, Y - v.x));
        }
    }

    public class PhysicsObjectWithCircle : PhysicsObject
    {
        private readonly Circle circle;

        public PhysicsObjectWithCircle(double mass, double x, double y, bool mobile, Circle circle) : base(mass, x, y, mobile)
        {
            this.circle = circle ?? throw new System.ArgumentNullException(nameof(circle));
        }

        public Circle GetCircle()
        {
            return circle;
        }
    }

}
