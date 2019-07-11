using System;

namespace Common
{

    public struct ObjectCreated
    {
        public readonly double x;
        public readonly double y;
        public readonly Guid id;

        public ObjectCreated(double x, double y, Guid id)
        {
            this.x = x;
            this.y = y;
            this.id = id;
        }
    }


    public struct PlayerCreated {
        public readonly ObjectCreated Body;
        public readonly ObjectCreated Foot;

        public PlayerCreated(ObjectCreated body, ObjectCreated foot)
        {
            Body = body;
            Foot = foot;
        }
    }
}
