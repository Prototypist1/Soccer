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
        Controller,
        AI
    }

    public class PlayerInputs
    {
        public ControlScheme ControlScheme { get; set; }
        public double FootX { get; set; }
        public double FootY { get; set; }
        public double BodyX { get; set; }
        public double BodyY { get; set; }
        public Guid Id { get; set; }
        public bool Throwing { get; set; }
        public Guid Boost { get; set; }

        public PlayerInputs(double footX, double footY, double bodyX, double bodyY, Guid Id, ControlScheme controlScheme, bool throwing, Guid boost)
        {
            this.FootX = footX;
            this.FootY = footY;
            this.BodyX = bodyX;
            this.BodyY = bodyY;
            this.Id = Id;
            this.ControlScheme = controlScheme;
            this.Throwing = throwing;
            this.Boost = boost;
        }

        public override string ToString()
        {
            return $"{BodyX},{BodyY}";
        }
    }
}
