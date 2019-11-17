using System;

namespace physics2
{
    public class PointCloudPartical {
        Player player;
        // between +1 and -1
        double index;

        public PointCloudPartical(Player player, double index, double x, double y)
        {
            this.player = player ?? throw new ArgumentNullException(nameof(player));
            this.index = index;
            X = x;
            Y = y;
        }

        public void Update(double step, double timeLeft)
        {
            X += Vx(timeLeft) * step;
            Y += Vy(timeLeft) * step;
        }

        public double X { get; private set; }
        public double Y { get; private set; }
        public double Vx(double timeRemaining)=> timeRemaining ==0?player.Vx :(player.Position
            .NewAdded(player.Velocity.NewScaled(timeRemaining))
            .NewAdded( player.GetVector().NewScaled( index)).x - X)/timeRemaining;
        public double Vy(double timeRemaining) => timeRemaining == 0 ? player.Vy: (player.Position
            .NewAdded(player.Velocity.NewScaled(timeRemaining))
            .NewAdded(player.GetVector().NewScaled(index)).y - Y)/timeRemaining;
    }

}
