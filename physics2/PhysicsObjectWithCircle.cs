using Physics2;

namespace physics2
{
    public class Ball : PhysicsObject
    {
        private readonly Circle circle;

        private readonly double x0,y0;

        public Player OwnerOrNull= null;

        // throwing info
        private Vector largestSpeed;
        

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
            this.OwnerOrNull = null;
        }

        public Circle GetCircle()
        {
            return circle;
        }

        public void ConsiderThrowing() {
            if (OwnerOrNull != null && OwnerOrNull.Throwing) {
                if (largestSpeed.Length > Constants.MimimunThrowingSpped) { 
                
                }
            }
        }
    }

}
