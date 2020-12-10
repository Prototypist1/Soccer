//using Microsoft.Toolkit.Uwp.UI.Media;

using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace RemoteSoccer
{

    class PlayerExtension
    {
        public Ellipse foot;
        public Ellipse body;
        public TextBlock text;

        public PlayerExtension(Ellipse foot, Ellipse body, TextBlock text)
        {
            this.foot = foot ?? throw new ArgumentNullException(nameof(foot));
            this.body = body ?? throw new ArgumentNullException(nameof(body));
            this.text = text ?? throw new ArgumentNullException(nameof(text));
        }
    }

    class GoalExtension
    {
        public Ellipse ellipse;
    }

    class BallExtension
    {
        public Ellipse ellipse;
    }

    class BallWallExtension
    {
        public Ellipse ellipse;
    }

    class RenderGameState {

        private Canvas gameArea;
        private FullField gameView;
        private readonly TextBlock leftScore, rightScore;

        private Dictionary<Guid, PlayerExtension> players = new Dictionary<Guid, PlayerExtension>();
        private BallExtension ballExtension;
        private GoalExtension goal1;
        private GoalExtension goal2;
        private BallWallExtension ballWallExtension;

        public RenderGameState(Canvas gameArea, FullField gameView, TextBlock leftScore, TextBlock rightScore)
        {
            this.gameArea = gameArea ?? throw new ArgumentNullException(nameof(gameArea));
            this.gameView = gameView ?? throw new ArgumentNullException(nameof(gameView));
            this.leftScore = leftScore ?? throw new ArgumentNullException(nameof(leftScore));
            this.rightScore = rightScore ?? throw new ArgumentNullException(nameof(rightScore));
        }

        public void Init() { 
        
        }

        public void MoveTo(FrameworkElement element, double x, double y) {
            element.TransformMatrix =
                        // first we center
                        new Matrix4x4(
                            1, 0, 0, 0,
                            0, 1, 0, 0,
                            0, 0, 1, 0,
                            (float)(-element.Width / 2.0), (float)(-element.Height / 2.0), 0, 1)
                        *
                        // then we move to the right spot
                        new Matrix4x4(
                            1, 0, 0, 0,
                            0, 1, 0, 0,
                            0, 0, 1, 0,
                            (float)x, (float)y, 0, 1);
        }

        public void Update(GameState gameState) {
            #region players
            foreach (var playerPair in gameState.players)
            {
                if (players.TryGetValue(playerPair.Key, out var currentPlayer))
                {
                    //update
                    MoveTo(currentPlayer.body, playerPair.Value.body.position.x, playerPair.Value.body.position.y);
                    MoveTo(currentPlayer.text, playerPair.Value.body.position.x, playerPair.Value.body.position.y);
                    MoveTo(currentPlayer.foot, playerPair.Value.foot.position.x, playerPair.Value.foot.position.y);
                    ((SolidColorBrush)currentPlayer.body.Fill).Color = Color.FromArgb(playerPair.Value.body.a, playerPair.Value.body.r, playerPair.Value.body.g, playerPair.Value.body.b);
                    ((SolidColorBrush)currentPlayer.foot.Fill).Color = Color.FromArgb(playerPair.Value.foot.a, playerPair.Value.foot.r, playerPair.Value.foot.g, playerPair.Value.foot.b);
                    currentPlayer.text.Text = playerPair.Value.name;
                }
                else
                {
                    // add
                    var body = new Ellipse()
                    {
                        Height = 2 * Constants.footLen,
                        Width = 2 * Constants.footLen
                    };
                    var foot = new Ellipse()
                    {
                        Height = 2 * Constants.PlayerRadius,
                        Width = 2 * Constants.PlayerRadius
                    };
                    var text = new TextBlock()
                    {
                        FontSize = 1600,
                    };

                    MoveTo(body, playerPair.Value.body.position.x, playerPair.Value.body.position.y);
                    MoveTo(text, playerPair.Value.body.position.x, playerPair.Value.body.position.y);
                    MoveTo(foot, playerPair.Value.foot.position.x, playerPair.Value.foot.position.y);

                    Canvas.SetZIndex(body, Constants.bodyZ);
                    Canvas.SetZIndex(foot, Constants.footZ);
                    Canvas.SetZIndex(text, Constants.textZ);

                    body.Fill = new SolidColorBrush(Color.FromArgb(playerPair.Value.body.a, playerPair.Value.body.r, playerPair.Value.body.g, playerPair.Value.body.b));
                    foot.Fill = new SolidColorBrush(Color.FromArgb(playerPair.Value.foot.a, playerPair.Value.foot.r, playerPair.Value.foot.g, playerPair.Value.foot.b));
                    text.Foreground = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0xff, 0xff));

                    gameArea.Children.Add(body);
                    gameArea.Children.Add(foot);
                    gameArea.Children.Add(text);

                    players.Add(playerPair.Key, new PlayerExtension(foot, body, text));
                }
            }

            var toRemove = new List<Guid>();

            foreach (var currentPlayer in players)
            {
                if (!gameState.players.ContainsKey(currentPlayer.Key))
                {
                    // remove
                    toRemove.Add(currentPlayer.Key);
                    gameArea.Children.Remove(currentPlayer.Value.body);
                    gameArea.Children.Remove(currentPlayer.Value.foot);
                    gameArea.Children.Remove(currentPlayer.Value.text);
                }
            }
            foreach (var removing in toRemove)
            {
                players.Remove(removing);
            }
            #endregion


            #region ball
            if (ballExtension == null)
            {
                // create ball
                var ball = new Ellipse()
                {
                    Height = 2 * Constants.BallRadius,
                    Width = 2 * Constants.BallRadius
                };

                MoveTo(ball, gameState.ball.posistion.x, gameState.ball.posistion.y);

                ball.Fill = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0xff, 0xff));

                Canvas.SetZIndex(ball, Constants.ballZ);

                gameArea.Children.Add(ball);

                ballExtension = new BallExtension
                {
                    ellipse = ball
                };
            }
            else
            {
                // move
                MoveTo(ballExtension.ellipse, gameState.ball.posistion.x, gameState.ball.posistion.y);
            }
            #endregion


            #region goals
            if (goal1 == null)
            {
                goal1 = new GoalExtension
                {
                    ellipse = MakeGoal(gameState.goals.First())
                };
            }
            if (goal2 == null)
            {
                goal2 = new GoalExtension
                {
                    ellipse = MakeGoal(gameState.goals.Skip(1).First())
                };
            }
            #endregion

            #region ball wall
            if (ballWallExtension == null)
            {

                var ballWall = new Ellipse
                {
                    Visibility = Visibility.Collapsed,
                    Stroke = new SolidColorBrush(Color.FromArgb(0xff, 0xbb, 0xbb, 0xbb)),
                };
                Canvas.SetZIndex(ballWall, Constants.footZ);
                this.gameArea.Children.Add(ballWall);
                ballWallExtension = new BallWallExtension
                {
                    ellipse = ballWall
                };
            }
            ballWallExtension.ellipse.Visibility = gameState.CountDownState.Countdown ? Visibility.Visible : Visibility.Collapsed;
            ballWallExtension.ellipse.Width = gameState.CountDownState.Radius * 2;
            ballWallExtension.ellipse.Height = gameState.CountDownState.Radius * 2;
            ballExtension.ellipse.Opacity = gameState.CountDownState.BallOpacity;
            MoveTo(ballWallExtension.ellipse, gameState.CountDownState.X, gameState.CountDownState.Y);
            ballExtension.ellipse.StrokeThickness = gameState.CountDownState.StrokeThickness;
            #endregion

            #region score
            leftScore.Text = gameState.leftScore + "";
            rightScore.Text = gameState.rightScore + "";
            #endregion

            var (playerX, playerY, xPlus, yPlus) = gameView.Update(Array.Empty<Position>());

            this.gameArea.TransformMatrix = new Matrix4x4(
                (float)(gameView.GetTimes()), 0, 0, 0,
                0, (float)(gameView.GetTimes()), 0, 0,
                0, 0, 1, 0,
                (float)xPlus, (float)yPlus, 0, 1);
        }

        // maybe this should just be a "make thing"
        private Ellipse MakeGoal(GameState.Goal gameStateGoal)
        {
            var goal = new Ellipse()
            {
                Height = 2 * Constants.goalLen,
                Width = 2 * Constants.goalLen
            };

            MoveTo(goal, gameStateGoal.posistion.x, gameStateGoal.posistion.y);

            goal.Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0x00, 0x00));

            Canvas.SetZIndex(goal, Constants.goalZ);

            gameArea.Children.Add(goal);

            return goal;
        }
    }
}
