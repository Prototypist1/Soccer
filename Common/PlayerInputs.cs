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
        public readonly int frame;
        public readonly double footX;
        public readonly double footY;
        public readonly double bodyX;
        public readonly double bodyY;
        public readonly Guid footId;
        public readonly Guid bodyId;

        public PlayerInputs(int frame, double footX, double footY, double bodyX, double bodyY, Guid footId, Guid bodyId)
        {
            this.frame = frame;
            this.footX = footX;
            this.footY = footY;
            this.bodyX = bodyX;
            this.bodyY = bodyY;
            this.footId = footId;
            this.bodyId = bodyId;
        }
    }
}
