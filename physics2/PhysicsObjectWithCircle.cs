namespace physics2
{
    public class Ball : PhysicsObject
    {
        private readonly Circle circle;

        private readonly double x0,y0;

        public Ball(double mass, double x, double y, bool mobile, Circle circle) : base(mass, x, y, mobile)
        {
            this.x0 = x;
            this.y0 = y;
            this.circle = circle ?? throw new System.ArgumentNullException(nameof(circle));
        }

        public void Reset() {
            this.X = x0;
            this.Y = y0;
            this.Vx = 0;
            this.Vy = 0;
        }

        public Circle GetCircle()
        {
            return circle;
        }
    }

}
