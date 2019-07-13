using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Physics.EventManager;


// TODO I really need to combine physics objects and shapes
// sub classes...
namespace Physics
{
    public struct Bounds {
        public readonly double maxX, maxY, minX, minY;

        public Bounds(double maxX, double maxY, double minX, double minY)
        {
            this.maxX = maxX;
            this.maxY = maxY;
            this.minX = minX;
            this.minY = minY;
        }
    }

    internal interface IShape {
        bool Mobile { get; }
        bool TryNextCollision(PhysicsObject self, PhysicsObject that, double endTime, out IEvent collision);
        HashSet<PhysicsObject> AddToGrid(PhysicsObject physicsObject, GridManager gridManager);
        void RemoveFromGrid(PhysicsObject physicsObject, GridManager gridManager);
    }

    internal abstract class Shape: IShape
    {
        public abstract bool Mobile { get; }

        public bool TryNextCollision(PhysicsObject self, PhysicsObject that, double endTime, out IEvent collision)
        {

            if (that is PhysicsObject<Ball> ball)
            {
                return TryNextCollisionBall(self, ball, endTime, out collision);
            }
            if (that is PhysicsObject<Line> line)
            {
                return TryNextCollisionLine(self, line, endTime, out collision);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        protected abstract bool TryNextCollisionLine(PhysicsObject self, PhysicsObject<Line> horizontalLine, double endTime, out IEvent collision);
        protected abstract bool TryNextCollisionBall(PhysicsObject self, PhysicsObject<Ball> verticalLine, double endTime, out IEvent collision);
        public abstract HashSet<PhysicsObject> AddToGrid(PhysicsObject physicsObject, GridManager gridManager);
        public abstract void RemoveFromGrid(PhysicsObject physicsObject, GridManager gridManager);
    }


    internal class Line : Shape
    {
        /// <summary>
        /// end should be clockwise of start
        /// </summary>
        public Line(Vector start, Vector end)
        {
            Start = start;
            End = end;
            var directionUnit = end.NewAdded(start.NewMinus()).NewUnitized();

            NormalUnit = new Vector(-directionUnit.y, directionUnit.x);

            NormalDistance = end.Dot(NormalUnit);
        }

        public Vector NormalUnit { get;  }
        public double NormalDistance { get;  }
        public Vector Start { get; }
        public Vector End { get; }

        public override bool Mobile => false;

        private class Proposal
        {
            public readonly double time;
            public readonly int x;
            public readonly int y;

            public Proposal(double time, int x, int y)
            {
                this.time = time;
                this.x = x;
                this.y = y;
            }
        }

        public override HashSet<PhysicsObject> AddToGrid(PhysicsObject physicsObject, GridManager gridManager)
        {
            var couldHits = new HashSet<PhysicsObject>();

            // ok so start at the start the is the last point
            var timeAt = 0.0;
            var v = End.NewAdded(Start.NewMinus());

            while (true)
            {

                var at = Start.NewAdded(v.NewScaled(timeAt));


                var proposals = new List<Proposal>();
                // find the next point
                //  place it crosses a vertical line
                if (v.x > 0)
                {
                    proposals.Add(new Proposal(
                        (((Math.Floor(at.x / gridManager.stepSize) + 1) * gridManager.stepSize) - at.x) / v.x,
                        (int)Math.Floor(at.x / gridManager.stepSize) + 1,
                        (int)Math.Floor(at.y/ gridManager.stepSize)));
                }
                if (v.x < 0)
                {
                    proposals.Add(new Proposal(
                        (((Math.Ceiling(at.x / gridManager.stepSize) - 1) * gridManager.stepSize) - at.x) / v.x,
                        (int)Math.Ceiling(at.x / gridManager.stepSize) - 1,
                        (int)Math.Floor(at.y / gridManager.stepSize)));
                }
                //  place it crosses a horizontal line
                if (v.y > 0)
                {
                    proposals.Add(new Proposal(
                        (((Math.Floor(at.y / gridManager.stepSize) + 1) * gridManager.stepSize) - at.y) / v.y,
                        (int)Math.Floor(at.x / gridManager.stepSize),
                        (int)Math.Floor(at.y / gridManager.stepSize) + 1));
                }
                if (v.y < 0)
                {
                    proposals.Add(new Proposal(
                        (((Math.Ceiling(at.y / gridManager.stepSize) - 1) * gridManager.stepSize) - at.y) / v.y,
                        (int)Math.Floor(at.x / gridManager.stepSize),
                        (int)Math.Ceiling(at.y / gridManager.stepSize) - 1));
                }
                //  the end of the line 
                if (v.x != 0)
                {
                    proposals.Add(new Proposal(
                        (End.x - at.x) / v.x,
                        (int)Math.Floor(End.x / gridManager.stepSize),
                        (int)Math.Floor(End.y / gridManager.stepSize)));
                }
                else if (v.y != 0)
                {
                    proposals.Add(new Proposal(
                        (End.y - at.y) / v.y,
                        (int)Math.Floor(End.x / gridManager.stepSize),
                        (int)Math.Floor(End.y / gridManager.stepSize)));
                }
                else
                {
                    // no good!! a point line is not ok!
                    throw new Exception();
                }

                var take = proposals.OrderBy(x=>x.time).First();
                timeAt += take.time;

                var current = gridManager.Grid[take.x, take.y];

                if (!current.Contains(physicsObject))
                {
                    couldHits.UnionWith(current);
                    gridManager.Grid[take.x, take.y].Add(physicsObject);
                }

                if (take == proposals.Last())
                {
                    return couldHits;
                }
            }
        }

        public override void RemoveFromGrid(PhysicsObject physicsObject, GridManager gridManager)
        {
            // ok so start at the start the is the last point
            var timeAt = 0.0;
            var v = End.NewAdded(Start.NewMinus());

            while (true)
            {

                var at = Start.NewAdded(v.NewScaled(timeAt));


                var proposals = new List<Proposal>();
                // find the next point
                //  place it crosses a vertical line
                if (v.x > 0)
                {
                    proposals.Add(new Proposal(
                        (((Math.Floor(at.x / gridManager.stepSize) + 1) * gridManager.stepSize) - at.x) / v.x,
                        (int)Math.Floor(at.x / gridManager.stepSize) + 1,
                        (int)Math.Floor(at.y / gridManager.stepSize)));
                }
                if (v.x < 0)
                {
                    proposals.Add(new Proposal(
                        (((Math.Ceiling(at.x / gridManager.stepSize) - 1) * gridManager.stepSize) - at.x) / v.x,
                        (int)Math.Ceiling(at.x / gridManager.stepSize) - 1,
                        (int)Math.Floor(at.y / gridManager.stepSize)));
                }
                //  place it crosses a horizontal line
                if (v.y > 0)
                {
                    proposals.Add(new Proposal(
                        (((Math.Floor(at.y / gridManager.stepSize) + 1) * gridManager.stepSize) - at.y) / v.y,
                        (int)Math.Floor(at.x / gridManager.stepSize),
                        (int)Math.Floor(at.y / gridManager.stepSize) + 1));
                }
                if (v.y < 0)
                {
                    proposals.Add(new Proposal(
                        (((Math.Ceiling(at.y / gridManager.stepSize) - 1) * gridManager.stepSize) - at.y) / v.y,
                        (int)Math.Floor(at.x / gridManager.stepSize),
                        (int)Math.Ceiling(at.y / gridManager.stepSize) - 1));
                }
                //  the end of the line 
                if (v.x != 0)
                {
                    proposals.Add(new Proposal(
                        (End.x - at.x) / v.x,
                        (int)Math.Floor(End.x),
                        (int)Math.Floor(End.y)));
                }
                else if (v.y != 0)
                {
                    proposals.Add(new Proposal(
                        (End.y - at.y) / v.y,
                        (int)Math.Floor(End.x),
                        (int)Math.Floor(End.y)));
                }
                else
                {
                    // no good!! a point line is not ok!
                    throw new Exception();
                }

                var take = proposals.OrderBy(x => x.time).First();
                timeAt += take.time;

                gridManager.Grid[take.x, take.y].Remove(physicsObject);

                if (take == proposals.Last())
                {
                    return;
                }
            }
        }

        protected override bool TryNextCollisionBall(PhysicsObject self, PhysicsObject<Ball> ball, double endTime, out IEvent collision)
        {
            // lines defer to the ball
            return ball.shape.TryNextCollision(ball, self, endTime, out collision);
        }

        protected override bool TryNextCollisionLine(PhysicsObject self, PhysicsObject<Line> line, double endTime, out IEvent collision)
        {
            // lines don't care about other lines
            collision = default;
            return false;
        }
    }

    internal class Ball: Shape
    {
        

        private readonly double Radius;

        public override bool Mobile => true;

        public Ball(double radius)
        {
            Radius = radius;
        }

        protected override bool TryNextCollisionBall(PhysicsObject myPhysicsObject, PhysicsObject<Ball> that, double endTime, out IEvent collision) 
        {
            // slop town
            // i think shapes should be more public and own or exten physics object
            // but 🤷‍ whatever
            if (!(myPhysicsObject is PhysicsObject<Ball> self)) {
                throw new Exception();
            }

            double startTime, thisX0, thatX0, thisY0, thatY0, DX, DY;
            // how  are they moving relitive to us
            double DVX = that.Vx - self.Vx,
                   DVY = that.Vy - self.Vy;

            if (self.Time > that.Time)
            {
                startTime = self.Time;
                thisX0 = self.X;
                thisY0 = self.Y;
                thatX0 = that.X + (that.Vx * (self.Time - that.Time));
                thatY0 = that.Y + (that.Vy * (self.Time - that.Time));
            }
            else
            {
                startTime = that.Time;
                thatX0 = that.X;
                thatY0 = that.Y;
                thisX0 = self.X + (self.Vx * (that.Time - self.Time));
                thisY0 = self.Y + (self.Vy * (that.Time - self.Time));
            }
            // how far they are from us
            DX = thatX0 - thisX0;
            DY = thatY0 - thisY0;


            // if the objects are not moving towards each other dont bother
            var V = -new Vector(DVX, DVY).Dot(new Vector(DX, DY).NewUnitized());
            if (V <= 0)
            {
                collision = default;
                return false;
            }


            var R = self.shape.Radius + that.shape.Radius;

            var A = (DVX * DVX) + (DVY * DVY);
            var B = 2 * ((DX * DVX) + (DY * DVY));
            var C = (DX * DX) + (DY * DY) - (R * R);

            if (TrySolveQuadratic(A, B, C, out var res) && startTime + res <= endTime)
            {

                collision = new CollisiphysicsObjectEven(
                    startTime + res,
                    self,
                    that,
#pragma warning disable IDE0047 // Remove unnecessary parentheses
                    self.X + (self.Vx * ((startTime + res) - self.Time)),
                    self.Y + (self.Vy * ((startTime + res) - self.Time)),
                    that.X + (that.Vx * ((startTime + res) - that.Time)),
                    that.Y + (that.Vy * ((startTime + res) - that.Time)));
#pragma warning restore IDE0047 // Remove unnecessary parentheses

                return true;
            }
            collision = default;
            return false;

        }

        protected override bool TryNextCollisionLine(PhysicsObject myPhysicsObject, PhysicsObject<Line> line, double endTime, out IEvent collision)
        {
            if (!(myPhysicsObject is PhysicsObject<Ball> self))
            {
                throw new Exception();
            }

            var normalDistance =  new Vector(self.X, self.Y).Dot(line.shape.NormalUnit.NewUnitized()) ;//myPhysicsObject.X
            var normalVelocity = new Vector(self.Vx, self.Vy).Dot(line.shape.NormalUnit.NewUnitized()) ;//myPhysicsObject.Vx
            var lineNormalDistance = line.shape.NormalDistance;//line.X

            if (lineNormalDistance > normalDistance)
            {
                if (normalVelocity > 0)
                {
                    var time = (lineNormalDistance - (normalDistance + self.shape.Radius)) / normalVelocity;
                    if (self.Time + time < endTime)
                    {
                        var force = line.shape.NormalUnit.NewScaled(-2 * normalVelocity);

                        collision = new UpdatePositionVelocityEvent(
                            self.Time + time,
                            self,
                            self.X + (time * self.Vx),
                            self.Y + (time * self.Vy),
                            self.Vx + force.x,
                            self.Vy + force.y);
                        return true;
                    }
                    else
                    {
                        collision = default;
                        return false;
                    }
                }
                else
                {
                    collision = default;
                    return false;
                }
            }
            else if (lineNormalDistance < normalDistance)
            {
                if (normalVelocity < 0)
                {
                    var time = (lineNormalDistance - (normalDistance - self.shape.Radius)) / normalVelocity;
                    if (self.Time + time < endTime)
                    {
                        var force = line.shape.NormalUnit.NewScaled(-2 * normalVelocity);

                        collision = new UpdatePositionVelocityEvent(
                            self.Time + time,
                            self,
                            self.X + (time * self.Vx),
                            self.Y + (time * self.Vy),
                            self.Vx + force.x,
                            self.Vy + force.y);
                        return true;
                    }
                    else
                    {
                        collision = default;
                        return false;
                    }
                }
                else
                {
                    collision = default;
                    return false;
                }
            }
            else //if (lineNormalDistance == normalDistance)
            {
                if (normalVelocity < 0)
                {
                    var time = (lineNormalDistance - (normalDistance - self.shape.Radius)) / normalVelocity;
                    if (self.Time + time < endTime)
                    {
                        collision = new UpdatePositionVelocityEvent(
                            self.Time + time,
                            self,
                            self.X + (time * self.Vx),
                            self.Y + (time * self.Vy),
                            -self.Vx,
                            self.Vy);
                        return true;
                    }
                    else
                    {
                        collision = default;
                        return false;
                    }
                }
                else if (normalVelocity > 0)
                {
                    var time = (lineNormalDistance - (normalDistance + self.shape.Radius)) / normalVelocity;
                    if (self.Time + time < endTime)
                    {
                        collision = new UpdatePositionVelocityEvent(
                            self.Time + time,
                            self,
                            self.X + (time * self.Vx),
                            self.Y + (time * self.Vy),
                            -self.Vx,
                            self.Vy);
                        return true;
                    }
                    else
                    {
                        collision = default;
                        return false;
                    }
                }
                else //normalVelocity == 0
                {
                    // how?? :(
                    collision = default;
                    return false;
                }
            }
        }

        public bool TrySolveQuadratic(double a, double b, double c, out double res)
        {
            var sqrtpart = (b * b) - (4 * a * c);
            double x1, x2;
            if (sqrtpart > 0)
            {
                x1 = (-b + Math.Sqrt(sqrtpart)) / (2 * a);
                x2 = (-b - Math.Sqrt(sqrtpart)) / (2 * a);
                res = Math.Min(x1, x2);
                return true;
            }
            else if (sqrtpart < 0)
            {
                res = default;
                return false;
            }
            else
            {
                res = (-b + Math.Sqrt(sqrtpart)) / (2 * a);
                return true;
            }
        }

        public override HashSet<PhysicsObject> AddToGrid(PhysicsObject physicsObject,GridManager gridManager)
        {
            var couldHits = new HashSet<PhysicsObject>();

            var inner = GetBounds(physicsObject);

            if (Mobile)
            {
                inner = new Bounds(
                    Help.BoundAddition(inner.maxX, gridManager.stepSize) / gridManager.stepSize,
                    Help.BoundAddition(inner.maxY, gridManager.stepSize) / gridManager.stepSize,
                    Help.BoundAddition(inner.minX, -gridManager.stepSize) / gridManager.stepSize,
                    Help.BoundAddition(inner.minY, -gridManager.stepSize) / gridManager.stepSize);
            }

            var bound = new Bounds(
                Math.Floor(gridManager.width / gridManager.stepSize) - 1,
                Math.Floor(gridManager.height / gridManager.stepSize) - 1,
                0,
                0);

            for (var x = (int)Math.Floor(Math.Max(inner.minX, bound.minX)); x <= (int)Math.Floor(Math.Min(inner.maxX, bound.maxX)); x++)
            {

                for (var y = (int)Math.Floor(Math.Max(inner.minY, bound.minY)); y <= (int)Math.Floor(Math.Min(inner.maxY, bound.maxY)); y++)
                {
                    couldHits.UnionWith(gridManager.Grid[x, y]);
                    gridManager.Grid[x, y].Add(physicsObject);
                }
            }

            return couldHits;

        }

        private Bounds GetBounds(PhysicsObject physicsObject)
        {
            var inner =  new Bounds(Radius, Radius, -Radius, -Radius);

            return new Bounds(
                Help.BoundAddition(inner.maxX, physicsObject.X),
                Help.BoundAddition(inner.maxY, physicsObject.Y),
                Help.BoundAddition(inner.minX, physicsObject.X),
                Help.BoundAddition(inner.minX, physicsObject.Y));
        }

        public override void RemoveFromGrid(PhysicsObject physicsObject, GridManager gridManager)
        {
            var inner = GetBounds(physicsObject);
            if (Mobile)
            {
                inner = new Bounds(
                    Help.BoundAddition(inner.maxX, gridManager.stepSize) / gridManager.stepSize,
                    Help.BoundAddition(inner.maxY, gridManager.stepSize) / gridManager.stepSize,
                    Help.BoundAddition(inner.minX, -gridManager.stepSize) / gridManager.stepSize,
                    Help.BoundAddition(inner.minY, -gridManager.stepSize) / gridManager.stepSize);
            }

            var bound = new Bounds(
                Math.Floor(gridManager.width / gridManager.stepSize) - 1,
                Math.Floor(gridManager.height / gridManager.stepSize) - 1,
                0,
                0);

            for (var x = (int)Math.Floor(Math.Max(inner.minX, bound.minX)); x <= (int)Math.Floor(Math.Min(inner.maxX, bound.maxX)); x++)
            {

                for (var y = (int)Math.Floor(Math.Max(inner.minY, bound.minY)); y <= (int)Math.Floor(Math.Min(inner.maxY, bound.maxY)); y++)
                {
                    gridManager.Grid[x, y].Remove(physicsObject);
                }
            }
        }
    }

    public static class PhysicsObjectBuilder {

        public static PhysicsObject Player(double mass, double radius, double x, double y) => new PhysicsObject<Ball>(mass, new Ball(radius), x, y);
        public static PhysicsObject Ball(double mass,double radius, double x, double y) => new PhysicsObject<Ball>(mass,new Ball(radius),x,y);
        public static PhysicsObject Line(Vector start, Vector end)
        {
            return new PhysicsObject<Line>(0, new Line(start,end), 0, 0);
        }
    }

    public abstract class PhysicsObject
    {

        public PhysicsObject(double mass, double x, double y)
        {
            Mass = mass;
            X = x;
            Y = y;
        }

        public double Time { get; internal set; } = 0;
        public double Vx { get; set; }
        public double Vy { get; set; }
        public double X { get; internal set; }
        public double Y { get; internal set; }
        public double Mass { get; }
        public double Speed => Math.Sqrt((Vx * Vx) + (Vy * Vy));
        public abstract bool Mobile { get; }
        public Vector Velocity
        {
            get
            {
                return new Vector(Vx, Vy);
            }
            set
            {
                Vx = value.x;
                Vy = value.y;
            }
        }

        public Vector Position => new Vector(X, Y);

        public void ApplyForce(double fx, double fy)
        {
            Vx += fx / Mass;
            Vy += fy / Mass;
        }

        internal abstract bool TryNextCollision(PhysicsObject that, double endTime, out IEvent collision);

        internal abstract HashSet<PhysicsObject> AddToGrid(GridManager gridManager);

        internal abstract void RemoveFromGrid(GridManager gridManager);
    }

    internal class  PhysicsObject<TShape>: PhysicsObject
        where TShape : IShape
    {

        public PhysicsObject(double mass, TShape shape, double x, double y): base(mass,x,y)
        {
            this.shape = shape;
        }

        public readonly TShape shape;

        public override bool Mobile => shape.Mobile;

        internal override bool TryNextCollision(PhysicsObject that, double endTime, out IEvent collision)
        {
            return shape.TryNextCollision(this, that, endTime, out collision);
        }


        internal override HashSet<PhysicsObject> AddToGrid(GridManager gridManager) => shape.AddToGrid(this,gridManager);

        internal override void RemoveFromGrid(GridManager gridManager) => shape.RemoveFromGrid(this,gridManager);

    }

    public static class Help {
        public static double BoundAddition(double x1, double x2) {
            if (x1 == double.MaxValue || x1 == double.MinValue) {
                return x1;
            }
            return x1 + x2;
        }
    }
}
