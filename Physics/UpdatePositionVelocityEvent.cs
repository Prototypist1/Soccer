namespace Physics
{

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

        public MightBeCollision Enact(GridManager gridManager, EventManager eventManager, double endtime)
        {
            if (myPhysicsObject.X != start_x ||
                   myPhysicsObject.Y != start_y ||
                   myPhysicsObject.Vx != start_vx ||
                   myPhysicsObject.Vy != start_vy)
            {
                return new MightBeCollision();
            }

            myPhysicsObject.RemoveFromGrid(gridManager);

            myPhysicsObject.X = x;
            myPhysicsObject.Y = y;
            myPhysicsObject.Vx = vx;
            myPhysicsObject.Vy = vy;
            myPhysicsObject.Time = Time;

            EventManager.WhatHappensNext(myPhysicsObject, gridManager, eventManager, endtime);

            return res;
        }
    }
}