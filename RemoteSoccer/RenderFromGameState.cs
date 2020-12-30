//using Microsoft.Toolkit.Uwp.UI.Media;

using Common;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
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

    class ParameterExtension
    {
        public Line line;
    }

    class RenderGameState2 {
        private CanvasControl canvas;
        private FullField zoomer;

        private readonly TextBlock leftScore, rightScore;


        private readonly MediaPlayer bell;
        private readonly LinkedList<MediaPlayer> collisionSounds = new LinkedList<MediaPlayer>();

        public RenderGameState2(CanvasControl canvas, FullField zoomer, TextBlock leftScore, TextBlock rightScore)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.zoomer = zoomer ?? throw new ArgumentNullException(nameof(zoomer));
            this.leftScore = leftScore ?? throw new ArgumentNullException(nameof(leftScore));
            this.rightScore = rightScore ?? throw new ArgumentNullException(nameof(rightScore));


            bell = new MediaPlayer();
            bell.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/bell.wav"));
                    
            var random = new Random();
            for (int i = 0; i < 10; i++)
            {

                var player = new MediaPlayer();
                player.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/hit{random.Next(1, 4)}.wav"));
                collisionSounds.AddLast(player);

            }

        }

        public void Update(GameState gameState, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            var (playerX, playerY, xPlus, yPlus) = zoomer.Update(Array.Empty<Position>());

            var scale = (float) zoomer.GetTimes();

            // goals
            DrawFilledCircle(
                gameState.leftGoal.posistion.x, gameState.leftGoal.posistion.y, Constants.goalLen, Color.FromArgb(0xff, 0xff, 0xff, 0xff));
            DrawFilledCircle(
                gameState.rightGoal.posistion.x, gameState.rightGoal.posistion.y, Constants.goalLen, Color.FromArgb(0xff, 0xff, 0xff, 0xff));

            // players bodies
            foreach (var playerPair in gameState.players)
            {
                DrawFilledCircle(playerPair.Value.body.position.x, playerPair.Value.body.position.y, Constants.footLen, Color.FromArgb(playerPair.Value.body.a, playerPair.Value.body.r, playerPair.Value.body.g, playerPair.Value.body.b));
                
            }
            // has ball highlight
            DrawCircle(gameState.ball.posistion.x, gameState.ball.posistion.y, Constants.BallRadius, Color.FromArgb(0xff, 0xff, 0xff, 0xff), 20 / scale);


            // players feet
            foreach (var playerPair in gameState.players)
            {
                DrawFilledCircle(playerPair.Value.foot.position.x, playerPair.Value.foot.position.y, Constants.PlayerRadius, Color.FromArgb(playerPair.Value.foot.a, playerPair.Value.foot.r, playerPair.Value.foot.g, playerPair.Value.foot.b));
            }

            // ball
            DrawFilledCircle(gameState.ball.posistion.x, gameState.ball.posistion.y, Constants.BallRadius, Color.FromArgb(
                (byte)((gameState.CountDownState.Countdown ? gameState.CountDownState.BallOpacity : 1)* 0xff)
                , 0x00, 0x00, 0x00));

            // ball wall
            if (gameState.CountDownState.Countdown) {
                DrawCircle(gameState.CountDownState.X, gameState.CountDownState.Y, gameState.CountDownState.Radius- (gameState.CountDownState.StrokeThickness/2.0),
                    Color.FromArgb((byte)((1 - gameState.CountDownState.BallOpacity) * 0xff), 0x88, 0x88, 0x88), (float)gameState.CountDownState.StrokeThickness);
            }

            // score
            leftScore.Text = gameState.leftScore + "";
            rightScore.Text = gameState.rightScore + "";

            // walls
            foreach (var segment in gameState.perimeterSegments)
            {
                args.DrawingSession.DrawLine(new Vector2((float)((segment.start.x * scale) + xPlus), (float)((segment.start.y * scale) + yPlus)),
                    new Vector2((float)((segment.end.x * scale) + xPlus), (float)((segment.end.y * scale) + yPlus)),
                    Color.FromArgb(0xff, 0x00, 0x00, 0x00));
            }

            //
            var ourGoals = gameState.goalsScored;
            gameState.goalsScored = new List<GameState.GoalScored>();
            foreach (var goalSocred in ourGoals)
            {
                Task.Run(() =>
                {
                    bell.Volume = 3;
                    bell.AudioBalance = goalSocred.leftScored ? 0:1;
                    bell.Play();
                });
            }

            var ourCollisions = gameState.collisions;
            gameState.collisions = new List<GameState.Collision>();
            foreach (var collision in ourCollisions)
            {
                if (collision.force.Length > 100)
                {
                    Task.Run(() =>
                    {
                        var item = collisionSounds.First.Value;
                        collisionSounds.RemoveFirst();
                        collisionSounds.AddLast(item);

                        item.Volume = (collision.force.Length * collision.force.Length / 100.0);
                        item.AudioBalance = collision.position.x / FieldDimensions.Default.xMax;
                        item.Play();
                    });
                }
            }

            void DrawFilledCircle(double x, double y, double rad, Color color) {
                args.DrawingSession.FillCircle(
                   (float)((x * scale) + xPlus), (float)((y * scale) + yPlus), (float)(rad * scale), color);
            }

            void DrawCircle(double x, double y, double rad, Color color, float strokeWidth)
            {
                args.DrawingSession.DrawCircle(
                   (float)((x * scale) + xPlus), (float)((y * scale) + yPlus), (float)(rad * scale), color, strokeWidth * scale);
            }
        }
    }

    class RenderGameState {

        private Canvas gameArea;
        private FullField gameView;
        private readonly TextBlock leftScore, rightScore;

        private Dictionary<Guid, PlayerExtension> players = new Dictionary<Guid, PlayerExtension>();
        private BallExtension ballExtension;
        private GoalExtension leftGoal;
        private GoalExtension rightGoal;
        private BallWallExtension ballWallExtension;
        private List<ParameterExtension> parameter;

        public RenderGameState(Canvas gameArea, FullField gameView, TextBlock leftScore, TextBlock rightScore)
        {
            this.gameArea = gameArea ?? throw new ArgumentNullException(nameof(gameArea));
            this.gameView = gameView ?? throw new ArgumentNullException(nameof(gameView));
            this.leftScore = leftScore ?? throw new ArgumentNullException(nameof(leftScore));
            this.rightScore = rightScore ?? throw new ArgumentNullException(nameof(rightScore));
        }

        public void Init() { 
        
        }

        private void MoveTo(FrameworkElement element, double x, double y) {
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
                    Width = 2 * Constants.BallRadius,
                    Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0x00, 0x00))
                };

                MoveTo(ball, gameState.ball.posistion.x, gameState.ball.posistion.y);

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
            if (leftGoal == null)
            {
                leftGoal = new GoalExtension
                {
                    ellipse = MakeGoal(gameState.leftGoal)
                };
            }
            if (rightGoal == null)
            {
                rightGoal = new GoalExtension
                {
                    ellipse = MakeGoal(gameState.rightGoal)
                };
            }
            #endregion

            #region ball wall
            if (ballWallExtension == null)
            {

                var ballWall = new Ellipse
                {
                    Visibility = Visibility.Collapsed,
                    Stroke = new SolidColorBrush(Color.FromArgb(0x88, 0xff, 0xff, 0xff)),
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
            ballWallExtension.ellipse.Height =  gameState.CountDownState.Radius * 2;
            ballExtension.ellipse.Opacity = gameState.CountDownState.Countdown ? gameState.CountDownState.BallOpacity: 1 ;
            MoveTo(ballWallExtension.ellipse, gameState.CountDownState.X, gameState.CountDownState.Y);
            ballWallExtension.ellipse.StrokeThickness = gameState.CountDownState.StrokeThickness;
            #endregion

            #region score
            leftScore.Text = gameState.leftScore + "";
            rightScore.Text = gameState.rightScore + "";
            #endregion

            #region Walls
            if (parameter == null) {
                parameter = new List<ParameterExtension>();
                foreach (var segment in gameState.perimeterSegments)
                {
                    var line = new Line
                    {
                        Stroke = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0x00, 0x00)),
                        X1 = segment.start.x,
                        Y1 = segment.start.y,
                        X2 = segment.end.x,
                        Y2 = segment.end.y,
                        StrokeThickness = 100
                    };
                    var parameterPart = new ParameterExtension
                        {
                        line = line
                        };

                    parameter.Add(parameterPart);
                    Canvas.SetZIndex(line, Constants.footZ);
                    this.gameArea.Children.Add(line);
                }
            }
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

            goal.Fill = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0xff, 0xff));

            Canvas.SetZIndex(goal, Constants.goalZ);

            gameArea.Children.Add(goal);

            return goal;
        }
    }
}
