using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{

    public struct ObjectForce {
        public readonly double fx;
        public readonly double fy;
        public readonly Guid id;

        public ObjectForce(double fx, double fy, Guid id)
        {
            this.fx = fx;
            this.fy = fy;
            this.id = id;
        }
    }

    public struct PlayerInputs
    {
        public readonly ObjectForce Body;
        public readonly ObjectForce Foot;

        public PlayerInputs(ObjectForce body, ObjectForce foot)
        {
            Body = body;
            Foot = foot;
        }
    }
}
