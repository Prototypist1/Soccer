using Physics2;

namespace physics2
{
    public interface IPhysicsObject
    {
        double Mass { get; }
        bool Mobile { get; }
        Vector Position { get; }
        double Speed { get; }
        Vector Velocity { get;  }
        double Vx { get;  }
        double Vy { get; }
        double X { get;  }
        double Y { get;  }
        void UpdateVelocity(double vx, double vy);
        void ApplyForce(double fx, double fy);
    }
}