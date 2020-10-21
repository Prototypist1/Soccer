using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{

    public struct ObjectForce {
        public double fx;
        public double fy;
        public Guid id;

        public ObjectForce(double fx, double fy, Guid id)
        {
            this.fx = fx;
            this.fy = fy;
            this.id = id;
        }
    }

    public enum ControlScheme { 
        MouseAndKeyboard,
        SipmleMouse,
        Controller
    }

    public struct PlayerInputs
    {
        public ControlScheme ControlScheme { get; set; }
        public double FootX { get; set; }
        public double FootY { get; set; }
        public double BodyX { get; set; }
        public double BodyY { get; set; }
        public Guid FootId { get; set; }
        public Guid BodyId { get; set; }
        public bool Throwing { get; set; }

        public PlayerInputs(double footX, double footY, double bodyX, double bodyY, Guid footId, Guid bodyId, ControlScheme controlScheme, bool throwing)
        {
            this.FootX = footX;
            this.FootY = footY;
            this.BodyX = bodyX;
            this.BodyY = bodyY;
            this.FootId = footId;
            this.BodyId = bodyId;
            this.ControlScheme = controlScheme;
            this.Throwing = throwing;
        }

        public override string ToString()
        {
            return $"{BodyX},{BodyY}";
        }
    }
}
