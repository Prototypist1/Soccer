using System;

namespace Common
{

    public class GameStateTracker
    {
        public int leftScore = 0, rightScore = 0;
        private readonly Action<double, double> resetBallAction;
        private double ballStartX, ballStartY;
        private readonly double maxX;
        private readonly double minX;
        private readonly double maxY;
        private readonly double minY;
        private readonly Random random = new Random();

        public CountDownState UpdateGameState()
        {

            if (gameState != play)
            {
                gameState++;
            }
            return GetCountDownState();
        }

        public CountDownState GetCountDownState()
        {
            var res = new CountDownState()
            {
                Countdown = false
            };
            if (gameState == resetBall)
            {
                resetBallAction(ballStartX, ballStartY);
            }
            if (gameState >= startCountDown && gameState < endCountDown)
            {
                res.Countdown = true;
                if (TryGetBallWall(out var tuple))
                {
                    res.StrokeThickness = tuple.radius - (Constants.ballWallLen * (gameState - startCountDown) / ((double)(endCountDown - startCountDown)));
                    res.Radius = tuple.radius;
                    res.BallOpacity = gameState > resetBall ? (gameState - resetBall) / (double)(endCountDown - resetBall) : ((resetBall - startCountDown) - (gameState - startCountDown)) / (double)(resetBall - startCountDown);
                    res.X = tuple.x;
                    res.Y = tuple.y;
                }
                else
                {
                    throw new Exception("bug");
                }
            }
            if (gameState == endCountDown)
            {
                gameState = play;
            }
            return res;
        }

        private const int play = -1;
        private const int startGrowingCircle = 0;
        private const int stopGrowingCircle = 100;
        private const int resetBall = 100;
        private const int startCountDown = 0;
        private const int endCountDown = 800;

        private int gameState = play;

        public GameStateTracker(Action<double, double> resetBallAction, double maxX, double minX, double maxY, double minY)
        {
            this.resetBallAction = resetBallAction ?? throw new ArgumentNullException(nameof(resetBallAction));
            this.maxX = maxX;
            this.minX = minX;
            this.maxY = maxY;
            this.minY = minY;
        }

        public bool CanScore() => gameState == play;

        public void Scored()
        {
            gameState = 1;
            this.ballStartX = random.NextDouble() * (maxX - minX) + minX;
            this.ballStartY = random.NextDouble() * (maxY - minY) + minY;
        }

        public bool TryGetBallWall(out (double x, double y, double radius) ballWall)
        {
            if (gameState >= startGrowingCircle)
            {
                ballWall = (
                    ballStartX,
                    ballStartY,
                    Constants.ballWallLen * ((Math.Min(gameState, stopGrowingCircle) - startGrowingCircle) / (double)(stopGrowingCircle - startGrowingCircle)));
                return true;
            }
            ballWall = default;
            return false;
        }
    }

}