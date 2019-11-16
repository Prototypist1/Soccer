using Physics2;

namespace physics2
{
    public interface IPhysicsObject
    {
        double Mass { get; }
        bool Mobile { get; }
        Vector Position { get; }
        double Speed { get; }
        Vector Velocity { get; set; }
        double Vx { get; set; }
        double Vy { get; set; }
        double X { get;  }
        double Y { get;  }

        void ApplyForce(double fx, double fy);
    }
}