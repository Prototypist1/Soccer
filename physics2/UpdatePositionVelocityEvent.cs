using physics2;

namespace Physics2
{

    internal readonly struct DoubleUpdatePositionVelocityEvent : IEvent
    {
        private readonly PhysicsObject myPhysicsObject_1;
        private readonly double start_vx_1;
        private readonly double start_vy_1;
        private readonly double start_x_1;
        private readonly double start_y_1;
        private readonly double x_1;
        private readonly double y_1;
        private readonly double vx_1;
        private readonly double vy_1;

        private readonly PhysicsObject myPhysicsObject_2;
        private readonly double start_vx_2;
        private readonly double start_vy_2;
        private readonly double start_x_2;
        private readonly double start_y_2;
        private readonly double x_2;
        private readonly double y_2;
        private readonly double vx_2;
        private readonly double vy_2;

        private readonly MightBeCollision res;

        public DoubleUpdatePositionVelocityEvent(
            double time, 
            PhysicsObject myPhysicsObject_1, 
            double x_1, 
            double y_1, 
            double vx_1, 
            double vy_1,
            PhysicsObject myPhysicsObject_2,
            double x_2,
            double y_2,
            double vx_2,
            double vy_2,
            MightBeCollision res)
        {
            this.Time = time;

            this.myPhysicsObject_1 = myPhysicsObject_1;
            this.start_vx_1 = myPhysicsObject_1.Vx;
            this.start_vy_1 = myPhysicsObject_1.Vy;
            this.start_x_1 = myPhysicsObject_1.X;
            this.start_y_1 = myPhysicsObject_1.Y;
            this.x_1 = x_1;
            this.y_1 = y_1;
            this.vx_1 = vx_1;
            this.vy_1 = vy_1;

            this.myPhysicsObject_2 = myPhysicsObject_2;
            this.start_vx_2 = myPhysicsObject_2.Vx;
            this.start_vy_2 = myPhysicsObject_2.Vy;
            this.start_x_2 = myPhysicsObject_2.X;
            this.start_y_2 = myPhysicsObject_2.Y;
            this.x_2 = x_2;
            this.y_2 = y_2;
            this.vx_2 = vx_2;
            this.vy_2 = vy_2;

            this.res = res;
        }

        public double Time { get; }

        public MightBeCollision Enact(double endtime)
        {
            if (myPhysicsObject_1.X != start_x_1 ||
                   myPhysicsObject_1.Y != start_y_1 ||
                   myPhysicsObject_1.Vx != start_vx_1 ||
                   myPhysicsObject_1.Vy != start_vy_1)
            {
                return new MightBeCollision();
            }

            if (myPhysicsObject_2.X != start_x_2 ||
                   myPhysicsObject_2.Y != start_y_2 ||
                   myPhysicsObject_2.Vx != start_vx_2 ||
                   myPhysicsObject_2.Vy != start_vy_2)
            {
                return new MightBeCollision();
            }

            myPhysicsObject_1.X = x_1;
            myPhysicsObject_1.Y = y_1;
            myPhysicsObject_1.Vx = vx_1;
            myPhysicsObject_1.Vy = vy_1;
            myPhysicsObject_1.Time = Time;

            myPhysicsObject_2.X = x_2;
            myPhysicsObject_2.Y = y_2;
            myPhysicsObject_2.Vx = vx_2;
            myPhysicsObject_2.Vy = vy_2;
            myPhysicsObject_2.Time = Time;

            return res;
        }
    }

    internal readonly struct UpdatePositionVelocityEvent : IEvent
    {
        private readonly PhysicsObject myPhysicsObject;
        private readonly double start_vx;
        private readonly double start_vy;
        private readonly double start_x;
        private readonly double start_y;
        private readonly double x;
        private readonly double y;
        private readonly double vx;
        private readonly double vy;
        private readonly MightBeCollision res;

        public UpdatePositionVelocityEvent(double time, PhysicsObject myPhysicsObject, double x, double y, double vx, double vy, MightBeCollision res)
        {
            this.Time = time;
            this.myPhysicsObject = myPhysicsObject;
            this.start_vx = myPhysicsObject.Vx;
            this.start_vy = myPhysicsObject.Vy;
            this.start_x = myPhysicsObject.X;
            this.start_y = myPhysicsObject.Y;
            this.x = x;
            this.y = y;
            this.vx = vx;
            this.vy = vy;
            this.res = res;
        }

        public double Time { get; }

        public MightBeCollision Enact(double endtime)
        {
            if (myPhysicsObject.X != start_x ||
                   myPhysicsObject.Y != start_y ||
                   myPhysicsObject.Vx != start_vx ||
                   myPhysicsObject.Vy != start_vy)
            {
                return new MightBeCollision();
            }

            myPhysicsObject.X = x;
            myPhysicsObject.Y = y;
            myPhysicsObject.Vx = vx;
            myPhysicsObject.Vy = vy;
            myPhysicsObject.Time = Time;

            return res;
        }
    }
}