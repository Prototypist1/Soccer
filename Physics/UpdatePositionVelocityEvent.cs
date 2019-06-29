namespace Physics
{
    internal class UpdatePositionVelocityEvent : IEvent
    {
        private double time;
        private PhysicsObject myPhysicsObject;
        private double start_vx;
        private double start_vy;
        private double start_x;
        private double start_y;
        private double x;
        private double y;
        private double vx;
        private double vy;

        public UpdatePositionVelocityEvent(double time, PhysicsObject myPhysicsObject, double x, double y, double vx, double vy)
        {
            this.time = time;
            this.myPhysicsObject = myPhysicsObject;
            this.start_vx = myPhysicsObject.Vx;
            this.start_vy = myPhysicsObject.Vy;
            this.start_x = myPhysicsObject.X;
            this.start_y = myPhysicsObject.Y;
            this.x = x;
            this.y = y;
            this.vx = vx;
            this.vy = vy;
        }

        public double Time => time;

        public void Enact(GridManager gridManager, EventManager eventManager, double endtime)
        {
            if (myPhysicsObject.X != start_x ||
                   myPhysicsObject.Y != start_y ||
                   myPhysicsObject.Vx != start_vx ||
                   myPhysicsObject.Vy != start_vy)
            {
                return;
            }

            gridManager.RemoveFromGrid(myPhysicsObject);

            myPhysicsObject.X = x;
            myPhysicsObject.Y = y;
            myPhysicsObject.Vx = vx;
            myPhysicsObject.Vy = vy;
            myPhysicsObject.Time = time;


            EventManager.WhatHappensNext(myPhysicsObject, gridManager, eventManager, endtime);
        }
    }
}