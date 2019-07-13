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

    public struct ObjectsCreated {
        public readonly ObjectCreated[] objects;

        public ObjectsCreated(ObjectCreated[] objects)
        {
            this.objects = objects ?? throw new ArgumentNullException(nameof(objects));
        }
    }

}
