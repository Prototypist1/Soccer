using System;

namespace Common
{
    public struct CreatePlayer {
        public CreatePlayer(Guid foot, Guid body, double bodyDiameter, double footDiameter, byte bodyR, byte bodyG, byte bodyB, byte bodyA, byte footR, byte footG, byte footB, byte footA, string name,string subId)
        {
            Foot = foot;
            Body = body;
            BodyDiameter = bodyDiameter;
            FootDiameter = footDiameter;
            BodyR = bodyR;
            BodyG = bodyG;
            BodyB = bodyB;
            BodyA = bodyA;
            FootR = footR;
            FootG = footG;
            FootB = footB;
            FootA = footA;
            Name = name;
            SubId = subId;
        }

        public Guid Foot { get; set; }
        public Guid Body { get; set; }
        public double BodyDiameter { get; set; }
        public double FootDiameter { get; set; }
        public byte BodyR { get; set; }
        public byte BodyG { get; set; }
        public byte BodyB { get; set; }
        public byte BodyA { get; set; }
        public byte FootR { get; set; }
        public byte FootG { get; set; }
        public byte FootB { get; set; }
        public byte FootA { get; set; }
        public string Name { get; set; }
        public string SubId { get; }
    }
}
