using System;
using System.Collections.Generic;
using System.Text;
using static Physics.EventManager;

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

        Bounds Size();

        bool TryNextCollision(PhysicsObject self, PhysicsObject that, double endTime, out IEvent collision);
    }

    internal abstract class Shape: IShape
    {

        public bool TryNextCollision(PhysicsObject self, PhysicsObject that, double endTime, out IEvent collision)
        {

            if (that is PhysicsObject<Ball> ball)
            {
                return TryNextCollisionBall(self, ball, endTime, out collision);
            }
            if (that is PhysicsObject<VerticalLine> verticalLine)
            {
                return TryNextCollisionVerticalLine(self, verticalLine, endTime, out collision);
            }
            if (that is PhysicsObject<HorizontalLine> horizontalLine)
            {
                return TryNextCollisionHorizontalLine(self, horizontalLine, endTime, out collision);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        protected abstract bool TryNextCollisionHorizontalLine(PhysicsObject self, PhysicsObject<HorizontalLine> horizontalLine, double endTime, out IEvent collision);
        protected abstract bool TryNextCollisionVerticalLine(PhysicsObject self, PhysicsObject<VerticalLine> verticalLine, double endTime, out IEvent collision);
        protected abstract bool TryNextCollisionBall(PhysicsObject self, PhysicsObject<Ball> verticalLine, double endTime, out IEvent collision);
        public abstract Bounds Size();
    }

    internal class VerticalLine : Shape
    {
        public override Bounds Size()
        {
            return new Bounds(0, double.MaxValue, 0, double.MinValue);
        }

        protected override bool TryNextCollisionBall(PhysicsObject self, PhysicsObject<Ball> ball, double endTime, out IEvent collision)
        {
            // lines defer to the ball
            return ball.shape.TryNextCollision(ball, self, endTime, out collision);
        }

        protected override bool TryNextCollisionVerticalLine(PhysicsObject self, PhysicsObject<VerticalLine> verticalLine, double endTime, out IEvent collision)
        {
            // lines don't care about other lines
            collision = default;
            return false;
        }

        protected override bool TryNextCollisionHorizontalLine(PhysicsObject self, PhysicsObject<HorizontalLine> horizontalLine, double endTime, out IEvent collision)
        {
            // lines don't care about other lines
            collision = default;
            return false;
        }
    }


    internal class HorizontalLine : Shape
    {
        public override Bounds Size()
        {
            return new Bounds(0, double.MaxValue, 0, double.MinValue);
        }

        protected override bool TryNextCollisionBall(PhysicsObject self, PhysicsObject<Ball> ball, double endTime, out IEvent collision)
        {
            // lines defer to the ball
            return ball.shape.TryNextCollision(ball, self, endTime, out collision);
        }

        protected override bool TryNextCollisionVerticalLine(PhysicsObject self, PhysicsObject<VerticalLine> verticalLine, double endTime, out IEvent collision)
        {
            // lines don't care about other lines
            collision = default;
            return false;
        }

        protected override bool TryNextCollisionHorizontalLine(PhysicsObject self, PhysicsObject<HorizontalLine> horizontalLine, double endTime, out IEvent collision)
        {
            // lines don't care about other lines
            collision = default;
            return false;
        }
    }


    internal class Ball: Shape
    {
        private readonly double Radius;

        public Ball(double radius)
        {
            Radius = radius;
        }

        public override Bounds Size()
        {
            return new Bounds(Radius, Radius, -Radius, -Radius);
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

            if (self.time > that.time)
            {
                startTime = self.time;
                thisX0 = self.X;
                thisY0 = self.Y;
                thatX0 = that.X + (that.Vx * (self.time - that.time));
                thatY0 = that.Y + (that.Vy * (self.time - that.time));
            }
            else
            {
                startTime = that.time;
                thatX0 = that.X;
                thatY0 = that.Y;
                thisX0 = self.X + (self.Vx * (that.time - self.time));
                thisY0 = self.Y + (self.Vy * (that.time - self.time));
            }
            // how far they are from us
            DX = thatX0 - thisX0;
            DY = thatY0 - thisY0;


            // if the objects are not moving towards each other dont bother
            var V = -new Vector(DVX, DVY).Dot(new Vector(DX, DY).NewNormalized());
            if (V <= 0)
            {
                collision = default;
                return false;
            }


            var R = (self.shape.Radius + that.shape.Radius);

            var A = (DVX * DVX) + (DVY * DVY);
            var B = 2 * ((DX * DVX) + (DY * DVY));
            var C = (DX * DX) + (DY * DY) - (R * R);

            if (TrySolveQuadratic(A, B, C, out var res) && startTime + res <= endTime)
            {

                collision = new CollisiphysicsObjectEven(
                    startTime + res,
                    self,
                    that,
                    self.X + self.Vx * ((startTime + res) - self.time),
                    self.Y + self.Vy * ((startTime + res) - self.time),
                    that.X + that.Vx * ((startTime + res) - that.time),
                    that.Y + that.Vy * ((startTime + res) - that.time));

                return true;
            }
            collision = default;
            return false;

        }


        protected override bool TryNextCollisionVerticalLine(PhysicsObject myPhysicsObject, PhysicsObject<VerticalLine> verticalLine, double endTime, out IEvent collision)
        {
            if (!(myPhysicsObject is PhysicsObject<Ball> self))
            {
                throw new Exception();
            }


            if (verticalLine.X > myPhysicsObject.X)
            {
                if (myPhysicsObject.Vx > 0)
                {
                    var time = (verticalLine.X - (myPhysicsObject.X + self.shape.Radius)) / self.Vx;
                    if (self.time + time < endTime)
                    {
                        collision = new UpdatePositionVelocityEvent(
                            self.time + time,
                            myPhysicsObject,
                            myPhysicsObject.X + (time * self.Vx),
                            myPhysicsObject.Y + (time * self.Vy),
                            -myPhysicsObject.Vx,
                            myPhysicsObject.Vy);
                        return true;
                    }
                    else {
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
            else if (verticalLine.X < myPhysicsObject.X)
            {
                if (myPhysicsObject.Vx < 0)
                {
                    var time = (verticalLine.X - (myPhysicsObject.X - self.shape.Radius)) / self.Vx;
                    if (self.time + time < endTime)
                    {
                        collision = new UpdatePositionVelocityEvent(
                            self.time + time,
                            myPhysicsObject,
                            myPhysicsObject.X + (time * self.Vx),
                            myPhysicsObject.Y + (time * self.Vy),
                            -myPhysicsObject.Vx,
                            myPhysicsObject.Vy);
                        return true;
                    }
                    else {
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
            else //if (verticalLine.X == myPhysicsObject.X)
            {
                if (myPhysicsObject.Vx < 0)
                {
                    var time = (verticalLine.X - (myPhysicsObject.X - self.shape.Radius)) / self.Vx;
                    if (self.time + time < endTime)
                    {
                        collision = new UpdatePositionVelocityEvent(
                            self.time + time,
                            myPhysicsObject,
                            myPhysicsObject.X + (time * self.Vx),
                            myPhysicsObject.Y + (time * self.Vy),
                            -myPhysicsObject.Vx,
                            myPhysicsObject.Vy);
                        return true;
                    }
                    else {
                        collision = default;
                        return false;
                    }
                }
                else if (myPhysicsObject.Vx > 0)
                {
                    var time = (verticalLine.X - (myPhysicsObject.X + self.shape.Radius)) / self.Vx;
                    if (self.time + time < endTime)
                    {
                        collision = new UpdatePositionVelocityEvent(
                            self.time + time,
                            myPhysicsObject,
                            myPhysicsObject.X + (time * self.Vx),
                            myPhysicsObject.Y + (time * self.Vy),
                            -myPhysicsObject.Vx,
                            myPhysicsObject.Vy);
                        return true;
                    }
                    else {
                        collision = default;
                        return false;
                    }
                }
                else
                {
                    // how?? :(
                    collision = default;
                    return false;
                }
            }
        }


        protected override bool TryNextCollisionHorizontalLine(PhysicsObject myPhysicsObject, PhysicsObject<HorizontalLine> verticalLine, double endTime, out IEvent collision)
        {
            if (!(myPhysicsObject is PhysicsObject<Ball> self))
            {
                throw new Exception();
            }


            if (verticalLine.Y > myPhysicsObject.Y)
            {
                if (myPhysicsObject.Vy > 0)
                {
                    var time = (verticalLine.Y - (myPhysicsObject.Y + self.shape.Radius)) / self.Vy;
                    if (self.time + time < endTime)
                    {
                        collision = new UpdatePositionVelocityEvent(
                            self.time + time,
                            myPhysicsObject,
                            myPhysicsObject.X + (time * self.Vx),
                            myPhysicsObject.Y + (time * self.Vy),
                            myPhysicsObject.Vx,
                            -myPhysicsObject.Vy);
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
            else if (verticalLine.Y < myPhysicsObject.Y)
            {
                if (myPhysicsObject.Vy < 0)
                {
                    var time = (verticalLine.Y - (myPhysicsObject.Y - self.shape.Radius)) / self.Vy;
                    if (self.time + time < endTime)
                    {
                        collision = new UpdatePositionVelocityEvent(
                            self.time + time,
                            myPhysicsObject,
                            myPhysicsObject.X + (time * self.Vx),
                            myPhysicsObject.Y + (time * self.Vy),
                            myPhysicsObject.Vx,
                            -myPhysicsObject.Vy);
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
            else //if (verticalLine.X == myPhysicsObject.X)
            {
                if (myPhysicsObject.Vy < 0)
                {
                    var time = (verticalLine.Y - (myPhysicsObject.Y - self.shape.Radius)) / self.Vy;
                    if (self.time + time < endTime)
                    {
                        collision = new UpdatePositionVelocityEvent(
                            self.time + time,
                            myPhysicsObject,
                            myPhysicsObject.X + (time * self.Vx),
                            myPhysicsObject.Y + (time * self.Vy),
                            myPhysicsObject.Vx,
                            -myPhysicsObject.Vy);
                        return true;
                    }
                    else
                    {
                        collision = default;
                        return false;
                    }
                }
                else if (myPhysicsObject.Vy > 0)
                {
                    var time = (verticalLine.Y - (myPhysicsObject.Y + self.shape.Radius)) / self.Vy;
                    if (self.time + time < endTime)
                    {
                        collision = new UpdatePositionVelocityEvent(
                            self.time + time,
                            myPhysicsObject,
                            myPhysicsObject.X + (time * self.Vx),
                            myPhysicsObject.Y + (time * self.Vy),
                            myPhysicsObject.Vx,
                            -myPhysicsObject.Vy);
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
                    // how?? :(
                    collision = default;
                    return false;
                }
            }
        }


        public bool TrySolveQuadratic(double a, double b, double c, out double res)
        {
            double sqrtpart = (b * b) - (4 * a * c);
            double x1, x2;
            if (sqrtpart > 0)
            {
                x1 = (-b + System.Math.Sqrt(sqrtpart)) / (2 * a);
                x2 = (-b - System.Math.Sqrt(sqrtpart)) / (2 * a);
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
                res = (-b + System.Math.Sqrt(sqrtpart)) / (2 * a);
                return true;
            }
        }

    }

    public static class PhysicsObjectBuilder {

        public static PhysicsObject Player(double mass, double radius, double x, double y) => new PhysicsObject<Ball>(mass, new Ball(radius), x, y);
        public static PhysicsObject Ball(double mass,double radius, double x, double y) => new PhysicsObject<Ball>(mass,new Ball(radius),x,y);

        public static PhysicsObject VerticalLine(double x) => new PhysicsObject<VerticalLine>(0, new VerticalLine(), x, 0);

        public static PhysicsObject HorizontalLine(double y) => new PhysicsObject<HorizontalLine>(0, new HorizontalLine(), 0, y);
    }

    public abstract class PhysicsObject {

        public PhysicsObject(double mass, double x, double y)
        {
            Mass = mass;
            X = x;
            Y = y;
        }

        internal bool mobile = true;
        public double time { get; internal set; } = 0;
        public double Vx { get; set; }
        public double Vy { get; set; }
        public double X { get; internal set; }
        public double Y { get; internal set; }
        public double Mass { get; }
        public double Speed => Math.Sqrt((Vx * Vx) + (Vy * Vy));
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

        public abstract Bounds GetBounds();

    }

    internal class  PhysicsObject<TShape>: PhysicsObject
        where TShape : IShape
    {

        public PhysicsObject(double mass, TShape shape, double x, double y): base(mass,x,y)
        {
            this.shape = shape;
        }

        public readonly TShape shape;
        
        internal override bool TryNextCollision(PhysicsObject that, double endTime, out IEvent collision)
        {
            return shape.TryNextCollision(this, that, endTime, out collision);
        }

        public override Bounds GetBounds()
        {
            var inner = shape.Size();
            return new Bounds(
                Help.BoundAddition(inner.maxX, X),
                Help.BoundAddition(inner.maxY, Y),
                Help.BoundAddition(inner.minX, X),
                Help.BoundAddition(inner.minX, Y));
        }
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
