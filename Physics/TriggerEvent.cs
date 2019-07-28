using System;

namespace Physics
{
    internal readonly struct TriggerEvent : IEvent
    {
        private readonly PhysicsObject myPhysicsObject;
        private readonly double start_vx;
        private readonly double start_vy;
        private readonly double start_x;
        private readonly double start_y;
        private readonly Action<PhysicsObject> callback;

        public TriggerEvent(double time, PhysicsObject myPhysicsObject, Action<PhysicsObject> callback)
        {
            this.Time = time;
            this.myPhysicsObject = myPhysicsObject ?? throw new ArgumentNullException(nameof(myPhysicsObject));
            this.callback = callback ?? throw new ArgumentNullException(nameof(callback));
            this.start_vx = myPhysicsObject.Vx;
            this.start_vy = myPhysicsObject.Vy;
            this.start_x = myPhysicsObject.X;
            this.start_y = myPhysicsObject.Y;
        }

        public double Time { get; }

        public void Enact(GridManager gridManager, EventManager eventManager, double endtime)
        {
            if (myPhysicsObject.X != start_x ||
                   myPhysicsObject.Y != start_y ||
                   myPhysicsObject.Vx != start_vx ||
                   myPhysicsObject.Vy != start_vy)
            {
                return;
            }

            myPhysicsObject.RemoveFromGrid(gridManager);

            myPhysicsObject.X = myPhysicsObject.X + (Time - myPhysicsObject.Time) * myPhysicsObject.Vx;
            myPhysicsObject.Y = myPhysicsObject.Y + (Time - myPhysicsObject.Time) * myPhysicsObject.Vy;
            myPhysicsObject.Time = Time;

            callback(myPhysicsObject);

            EventManager.WhatHappensNext(myPhysicsObject, gridManager, eventManager, endtime);
        }
    }
}