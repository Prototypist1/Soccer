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
            var v = Velocity;
            if (v.Length == 0) {
                v = new Physics2.Vector(1, 0);
            }
            v = v.NewUnitized().NewScaled(length);
            return new Line(
                new Physics2.Vector(X - (v.y / 2.0), Y + (v.x / 2.0)),
                new Physics2.Vector(X + (v.y / 2.0), Y - (v.x / 2.0)));
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
