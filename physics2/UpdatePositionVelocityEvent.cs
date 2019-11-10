using physics2;

namespace Physics2
{

    internal readonly struct DoubleUpdatePositionVelocityEvent : IEvent
    {
        private readonly IPhysicsObject myPhysicsObject_1;
        private readonly double vx_1;
        private readonly double vy_1;

        private readonly IPhysicsObject myPhysicsObject_2;
        private readonly double vx_2;
        private readonly double vy_2;

        private readonly MightBeCollision res;

        public DoubleUpdatePositionVelocityEvent(
            double time,
            IPhysicsObject myPhysicsObject_1, 
            double vx_1, 
            double vy_1,
            IPhysicsObject myPhysicsObject_2,
            double vx_2,
            double vy_2,
            MightBeCollision res)
        {
            this.Time = time;

            this.myPhysicsObject_1 = myPhysicsObject_1;
            this.vx_1 = vx_1;
            this.vy_1 = vy_1;

            this.myPhysicsObject_2 = myPhysicsObject_2;
            this.vx_2 = vx_2;
            this.vy_2 = vy_2;

            this.res = res;
        }

        public double Time { get; }

        public MightBeCollision Enact()
        {
            myPhysicsObject_1.Vx = vx_1;
            myPhysicsObject_1.Vy = vy_1;
            myPhysicsObject_2.Vx = vx_2;
            myPhysicsObject_2.Vy = vy_2;

            return res;
        }
    }

    internal readonly struct UpdatePositionVelocityEvent : IEvent
    {
        private readonly PhysicsObject myPhysicsObject;
        private readonly double vx;
        private readonly double vy;
        private readonly MightBeCollision res;

        public UpdatePositionVelocityEvent(double time, PhysicsObject myPhysicsObject, double x, double y, double vx, double vy, MightBeCollision res)
        {
            this.Time = time;
            this.myPhysicsObject = myPhysicsObject;
            this.vx = vx;
            this.vy = vy;
            this.res = res;
        }

        public double Time { get; }

        public MightBeCollision Enact()
        {
            myPhysicsObject.Vx = vx;
            myPhysicsObject.Vy = vy;

            return res;
        }
    }
}