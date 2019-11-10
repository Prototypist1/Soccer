using System;
using System.Collections.Generic;
using System.Linq;

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
        private readonly LastLineTracker lastLineTracker;

        private class LastLineTracker
        {
            private class Entry : IEquatable<Entry>
            {
                private readonly double Vx, Vy, X, Y;
                public readonly Line lastline;

                public Entry(double vx, double vy, double x, double y, Line lastline)
                {
                    Vx = vx;
                    Vy = vy;
                    X = x;
                    Y = y;
                    this.lastline = lastline ?? throw new ArgumentNullException(nameof(lastline));
                }

                public override bool Equals(object obj)
                {
                    return Equals(obj as Entry);
                }

                public bool Equals(Entry other)
                {
                    return other != null &&
                           Vx == other.Vx &&
                           Vy == other.Vy &&
                           X == other.X &&
                           Y == other.Y;
                }

                public override int GetHashCode()
                {
                    var hashCode = -748391058;
                    hashCode = hashCode * -1521134295 + Vx.GetHashCode();
                    hashCode = hashCode * -1521134295 + Vy.GetHashCode();
                    hashCode = hashCode * -1521134295 + X.GetHashCode();
                    hashCode = hashCode * -1521134295 + Y.GetHashCode();
                    return hashCode;
                }
            }

            private List<Entry> list = new List<Entry>();

            private bool TryUpdate(Entry entry) {
                if (!list.Any()) {
                    list.Add(entry);
                    return true;
                }
                if (list.Last().Equals(entry))
                {
                    return false;
                }
                else {
                    list.Add(entry);
                    if (list.Count > 2)
                    {
                        list.RemoveAt(0);
                    }
                    return true;
                }
            }

            public Line TryGetLastLine(Line line,double vx, double vy,double x, double y) {
                var entry = new Entry(vx, vy, x, y, line);
                TryUpdate(entry);
                var at= list.IndexOf(entry)-1;
                if (at < 0) {
                    return line;
                }
                return list[at].lastline;
            }
        }

        public Player(double mass, double x, double y, bool mobile, double length) : base(mass, x, y, mobile)
        {
            this.length = length;
        }

        public (Line,Line) GetSweep() {
            var line = GetLine();
            var lastLine= lastLineTracker.TryGetLastLine(GetLine(), Vx, Vy, X, Y);

            //var beforeRotation = new Line(
            //    line.Center.NewAdded(lastLine.Start.NewAdded(lastLine.Center.NewMinus())),
            //    line.Center.NewAdded(lastLine.End.NewAdded(lastLine.Center.NewMinus()))
            //    );

            var v = new Physics2.Vector(Vx, Vy);
            if (v.Length != 0) {
                var toAdd = lastLine.Start.NewAdded(lastLine.Center.NewMinus()).Dot(v.NewUnitized());
                var newCenter = lastLine.Center.NewAdded(v.NewUnitized().NewScaled(-Math.Abs(toAdd)));
                var startRun = new Line(
                    newCenter.NewAdded(line.Start.NewAdded(line.Center.NewMinus())),
                    newCenter.NewAdded(line.End.NewAdded(line.Center.NewMinus()))
                );
                var endRun = new Line(
                    line.Start.NewAdded(v),
                    line.End.NewAdded(v));
                return (startRun, endRun);
            }
            return (line, line);
        }

        public override Line GetLine() {
            
            // this is shared code
            var v = new Physics2.Vector(Vx * 10, Vy * 10);
            if (v.Length > length*.5)
            {
                v = v.NewScaled(length * .5 / v.Length);
            }
            var res = new Line(
                new Physics2.Vector(X - v.y, Y + v.x),
                new Physics2.Vector(X + v.y, Y - v.x));

            return res;
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
