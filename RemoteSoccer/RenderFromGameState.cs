﻿//using Microsoft.Toolkit.Uwp.UI.Media;

using Common;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.UI;
using Windows.UI.Xaml.Controls;
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


    class Smoother
    {

        private readonly IZoomer zoomer;
        private double lastTimes;
        private bool initTimes = false;
        private (double, double, double, double) lastUpdate;
        private bool initUpdate = false;
        private const double Scale = 10;

        public Smoother(IZoomer zoomer)
        {
            this.zoomer = zoomer ?? throw new ArgumentNullException(nameof(zoomer));
        }

        public double GetTimes()
        {
            if (!initTimes)
            {
                lastTimes = zoomer.GetTimes();
                initTimes = true;
                return lastTimes;
            }
            lastTimes = (zoomer.GetTimes() + Scale * lastTimes) / (1 + Scale);
            return lastTimes;
        }

        public (double, double, double, double) Update(GameState gameState)
        {
            if (!initUpdate)
            {
                lastUpdate = zoomer.Update(gameState);
                initUpdate = true;
                return lastUpdate;
            }
            var update = zoomer.Update(gameState);
            lastUpdate = (
                (update.Item1 + Scale * lastUpdate.Item1) / (1 + Scale),
                (update.Item2 + Scale * lastUpdate.Item2) / (1 + Scale),
                (update.Item3 + Scale * lastUpdate.Item3) / (1 + Scale),
                (update.Item4 + Scale * lastUpdate.Item4) / (1 + Scale));
            return lastUpdate;
        }
    }

    class RenderGameState2
    {
        //private CanvasControl canvas;
        private Smoother zoomer;

        private readonly TextBlock leftScore, rightScore;


        //private readonly MediaPlayer bell;
        //private readonly LinkedList<MediaPlayer> collisionSounds = new LinkedList<MediaPlayer>();

        private List<GameState.GoalScored> goalScoreds = new List<GameState.GoalScored>();
        private List<GameState.Collision> collisions = new List<GameState.Collision>();

        public RenderGameState2(/*CanvasControl canvas,*/ IZoomer zoomer, TextBlock leftScore, TextBlock rightScore)
        {
            //this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            this.zoomer = new Smoother(zoomer) ?? throw new ArgumentNullException(nameof(zoomer));
            this.leftScore = leftScore ?? throw new ArgumentNullException(nameof(leftScore));
            this.rightScore = rightScore ?? throw new ArgumentNullException(nameof(rightScore));


            //bell = new MediaPlayer();
            //bell.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/bell.wav"));

            //var random = new Random();
            //for (int i = 0; i < 10; i++)
            //{
            //    var player = new MediaPlayer();
            //    player.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/hit{random.Next(1, 4)}.wav"));
            //    collisionSounds.AddLast(player);
            //}
        }

        public void Update(GameState gameState, CanvasDrawEventArgs args)
        {
            var (playerX, playerY, xPlus, yPlus) = zoomer.Update(gameState);

            var scale = (float)zoomer.GetTimes();

            // walls
            foreach (var segment in gameState.PerimeterSegments)
            {
                var width = 1_000_000;

                var xOffset = 0f;

                if (segment.Start.y > segment.End.y)
                {
                    xOffset = -width / 2f;
                }
                else if (segment.Start.y < segment.End.y)
                {
                    xOffset = width / 2f;
                }

                var yOffset = 0f;

                if (segment.Start.x > segment.End.x)
                {
                    yOffset = width / 2f;
                }
                else if (segment.Start.x < segment.End.x)
                {
                    yOffset = -width / 2f;
                }


                DrawLine(
                    segment.Start.x + xOffset,
                    segment.Start.y + yOffset,
                    segment.End.x + xOffset,
                    segment.End.y + yOffset,
                    Color.FromArgb(0x11, 0x00, 0x00, 0x00),
                    width,
                    CanvasCapStyle.Square);
            }

            // goals
            DrawFilledCircle(
                gameState.LeftGoal.Posistion.x, gameState.LeftGoal.Posistion.y, Constants.goalLen, Color.FromArgb(0xff, 0xff, 0xff, 0xff));
            DrawFilledCircle(
                gameState.RightGoal.Posistion.x, gameState.RightGoal.Posistion.y, Constants.goalLen, Color.FromArgb(0xff, 0xff, 0xff, 0xff));


            // has ball highlight
            DrawCircle(gameState.GameBall.Posistion.x, gameState.GameBall.Posistion.y, Constants.BallRadius,
                Color.FromArgb((byte)((gameState.CountDownState.Countdown ? gameState.CountDownState.BallOpacity : 1) * 0xff), 0xff, 0xff, 0xff), 35 / scale);


            // players bodies
            foreach (var playerPair in gameState.players)
            {
                DrawFilledCircle(playerPair.Value.PlayerBody.Position.x, playerPair.Value.PlayerBody.Position.y, Constants.playerCenterRadius, Color.FromArgb(0xff, playerPair.Value.PlayerBody.R, playerPair.Value.PlayerBody.G, playerPair.Value.PlayerBody.B));
                //DrawFilledCircle(playerPair.Value.PlayerBody.Position.x, playerPair.Value.PlayerBody.Position.y, Constants.footLen, Color.FromArgb(playerPair.Value.PlayerBody.A, playerPair.Value.PlayerBody.R, playerPair.Value.PlayerBody.G, playerPair.Value.PlayerBody.B));
            }


            // draw number of boosts
            foreach (var playerPair in gameState.players)
            {
                if (playerPair.Value.Boosts > 0)
                {
                    DrawCircle(playerPair.Value.PlayerFoot.Position.x, playerPair.Value.PlayerFoot.Position.y,
                        Constants.PlayerRadius + (playerPair.Value.Boosts * 2.0 / scale)  + (2 / scale),
                        Color.FromArgb(0x22, playerPair.Value.PlayerFoot.R, playerPair.Value.PlayerFoot.G, playerPair.Value.PlayerFoot.B),
                        (float)(playerPair.Value.Boosts * 4.0 / scale));
                }
            }

            // players feet
            foreach (var playerPair in gameState.players)
            {
                DrawFilledCircle(playerPair.Value.PlayerFoot.Position.x, playerPair.Value.PlayerFoot.Position.y, Constants.PlayerRadius, Color.FromArgb(playerPair.Value.PlayerFoot.A, playerPair.Value.PlayerFoot.R, playerPair.Value.PlayerFoot.G, playerPair.Value.PlayerFoot.B));

                //if (playerPair.Value.BoostVelocity.Length > 10)
                //{
                //    var boostV = playerPair.Value.BoostVelocity;//.NewAdded(playerPair.Value.PlayerBody.Velocity).NewAdded(playerPair.Value.PlayerFoot.Velocity).NewAdded(playerPair.Value.ExternalVelocity).NewAdded(playerPair.Value.BoostVelocity);
                //    DrawLine(
                //        playerPair.Value.PlayerFoot.Position.x,
                //        playerPair.Value.PlayerFoot.Position.y,
                //        playerPair.Value.PlayerFoot.Position.x + (boostV.x * 15),
                //        playerPair.Value.PlayerFoot.Position.y + (boostV.y * 15),
                //        Color.FromArgb(0x20, playerPair.Value.PlayerFoot.R, playerPair.Value.PlayerFoot.G, playerPair.Value.PlayerFoot.B),
                //        Constants.PlayerRadius * 2);
                //}

                //var vel = playerPair.Value.PlayerBody.Velocity;//.NewAdded(playerPair.Value.PlayerBody.Velocity).NewAdded(playerPair.Value.PlayerFoot.Velocity).NewAdded(playerPair.Value.ExternalVelocity).NewAdded(playerPair.Value.BoostVelocity);
                //DrawLine(
                //    playerPair.Value.PlayerFoot.Position.x,
                //    playerPair.Value.PlayerFoot.Position.y,
                //    playerPair.Value.PlayerFoot.Position.x + (vel.x * 15),
                //    playerPair.Value.PlayerFoot.Position.y + (vel.y * 15),
                //    Color.FromArgb(0x20, playerPair.Value.PlayerFoot.R, playerPair.Value.PlayerFoot.G, playerPair.Value.PlayerFoot.B),
                //    Constants.PlayerRadius );
            }

            // ball
            DrawFilledCircle(gameState.GameBall.Posistion.x, gameState.GameBall.Posistion.y, Constants.BallRadius, Color.FromArgb(
                (byte)((gameState.CountDownState.Countdown ? gameState.CountDownState.BallOpacity : 1) * 0xff)
                , 0x00, 0x00, 0x00));

            // ball wall
            if (gameState.CountDownState.Countdown)
            {
                DrawCircle(gameState.CountDownState.X, gameState.CountDownState.Y, gameState.CountDownState.Radius - (gameState.CountDownState.StrokeThickness / 2.0),
                    Color.FromArgb((byte)((1 - gameState.CountDownState.BallOpacity) * 0xff), 0x88, 0x88, 0x88), (float)gameState.CountDownState.StrokeThickness);
            }

            // score
            leftScore.Text = gameState.RightScore + "";
            rightScore.Text = gameState.LeftScore + ""; 

            // goals scored
            foreach (var goalScored in gameState.GoalsScored.Except(goalScoreds))
            {
                //Task.Run(() =>
                //{
                //    bell.Volume = 3;
                //    bell.AudioBalance = goalScored.LeftScored ? 0 : 1;
                //    bell.Play();
                //});
                goalScoreds.Add(goalScored);
            }

            var goalAnimationLength = 120.0;
            goalScoreds = goalScoreds.Where(x => gameState.Frame - x.Frame < goalAnimationLength).ToList();
            foreach (var goalSocred in goalScoreds)
            {
                DrawLine(
                    goalSocred.Posistion.x + (goalSocred.Surface.x * 1000 * (gameState.Frame - goalSocred.Frame)),
                    goalSocred.Posistion.y + (goalSocred.Surface.y * 1000 * (gameState.Frame - goalSocred.Frame)),
                    goalSocred.Posistion.x - (goalSocred.Surface.x * 1000 * (gameState.Frame - goalSocred.Frame)),
                    goalSocred.Posistion.y - (goalSocred.Surface.y * 1000 * (gameState.Frame - goalSocred.Frame)),
                    Color.FromArgb((byte)(1.0 - (((gameState.Frame - goalSocred.Frame) / goalAnimationLength)) * 0xff), 0x00, 0x00, 0x00), 1 / scale,
                    CanvasCapStyle.Round);
            }

            // collisions
            foreach (var collision in gameState.collisions.Except(collisions))
            {
                if (collision.Force.Length > 100)
                {
                    //    Task.Run(() =>
                    //{
                    //var item = collisionSounds.First.Value;
                    //collisionSounds.RemoveFirst();
                    //collisionSounds.AddLast(item);

                    //    item.Volume = (collision.Force.Length * collision.Force.Length / 100.0);
                    //    item.AudioBalance = collision.Position.x / FieldDimensions.Default.xMax;
                    //    item.Play();
                    //});
                }
                collisions.Add(collision);
            }

            var timeDenom = 100.0;
            collisions = collisions.Where(x => gameState.Frame - x.Frame < x.Force.Length / timeDenom).Take(10).ToList();
            foreach (var collision in collisions)
            {
                DrawLine(
                    collision.Position.x + ((collision.Force.y + (collision.Force.x / 10.0)) * .5 * (gameState.Frame - collision.Frame)),
                    collision.Position.y - ((collision.Force.x + (collision.Force.y / 10.0)) * .5 * (gameState.Frame - collision.Frame)),
                    collision.Position.x + ((collision.Force.y + (collision.Force.x / 10.0)) * 1 * (gameState.Frame - collision.Frame)),
                    collision.Position.y - ((collision.Force.x + (collision.Force.y / 10.0)) * 1 * (gameState.Frame - collision.Frame)),
                    Color.FromArgb((byte)(1.0 - (((gameState.Frame - collision.Frame) / (collision.Force.Length / timeDenom))) * 0xff), 0x00, 0x00, 0x00), 1 / scale,
                    CanvasCapStyle.Round);

                DrawLine(
                    collision.Position.x - ((collision.Force.y + (collision.Force.x / 10.0)) * .5 * (gameState.Frame - collision.Frame)),
                    collision.Position.y + ((collision.Force.x + (collision.Force.y / 10.0)) * .5 * (gameState.Frame - collision.Frame)),
                    collision.Position.x - ((collision.Force.y + (collision.Force.x / 10.0)) * 1 * (gameState.Frame - collision.Frame)),
                    collision.Position.y + ((collision.Force.x + (collision.Force.y / 10.0)) * 1 * (gameState.Frame - collision.Frame)),
                    Color.FromArgb((byte)(1.0 - (((gameState.Frame - collision.Frame) / (collision.Force.Length / timeDenom))) * 0xff), 0x00, 0x00, 0x00), 1 / scale,
                    CanvasCapStyle.Round);

                DrawLine(
                    collision.Position.x + ((collision.Force.y - (collision.Force.x / 10.0)) * .5 * (gameState.Frame - collision.Frame)),
                    collision.Position.y - ((collision.Force.x - (collision.Force.y / 10.0)) * .5 * (gameState.Frame - collision.Frame)),
                    collision.Position.x + ((collision.Force.y - (collision.Force.x / 10.0)) * 1 * (gameState.Frame - collision.Frame)),
                    collision.Position.y - ((collision.Force.x - (collision.Force.y / 10.0)) * 1 * (gameState.Frame - collision.Frame)),
                    Color.FromArgb((byte)(1.0 - (((gameState.Frame - collision.Frame) / (collision.Force.Length / timeDenom))) * 0xff), 0x00, 0x00, 0x00), 1 / scale,
                    CanvasCapStyle.Round);

                DrawLine(
                    collision.Position.x - ((collision.Force.y - (collision.Force.x / 10.0)) * .5 * (gameState.Frame - collision.Frame)),
                    collision.Position.y + ((collision.Force.x - (collision.Force.y / 10.0)) * .5 * (gameState.Frame - collision.Frame)),
                    collision.Position.x - ((collision.Force.y - (collision.Force.x / 10.0)) * 1 * (gameState.Frame - collision.Frame)),
                    collision.Position.y + ((collision.Force.x - (collision.Force.y / 10.0)) * 1 * (gameState.Frame - collision.Frame)),
                    Color.FromArgb((byte)(1.0 - (((gameState.Frame - collision.Frame) / (collision.Force.Length / timeDenom))) * 0xff), 0x00, 0x00, 0x00), 1 / scale,
                    CanvasCapStyle.Round);
            }

            foreach (var collision in gameState.debugs.Where(x => gameState.Frame - x.Frame< 180)) {
                DrawCircle(collision.Target.x, collision.Target.y, Constants.BallRadius, Colors.Black, 1 / scale);
            }

            // draw throw preview
            foreach (var playerPair in gameState.players)
            {
                //if (gameState.GameBall.OwnerOrNull == playerPair.Key)
                //{
                var toThrow = playerPair.Value.ProposedThrow;//.NewAdded(playerPair.Value.PlayerBody.Velocity).NewAdded(playerPair.Value.PlayerFoot.Velocity).NewAdded(playerPair.Value.ExternalVelocity).NewAdded(playerPair.Value.BoostVelocity);
                DrawLine(
                    playerPair.Value.PlayerFoot.Position.x,
                    playerPair.Value.PlayerFoot.Position.y,
                    playerPair.Value.PlayerFoot.Position.x + (toThrow.x * 30),
                    playerPair.Value.PlayerFoot.Position.y + (toThrow.y * 30),
                    Color.FromArgb(0xff, playerPair.Value.PlayerFoot.R, playerPair.Value.PlayerFoot.G, playerPair.Value.PlayerFoot.B),
                    1 / scale,
                    CanvasCapStyle.Round);
                //}
            }

            void DrawFilledCircle(double x, double y, double rad, Color color)
            {
                args.DrawingSession.FillCircle(
                   (float)((x * scale) + xPlus), (float)((y * scale) + yPlus), (float)(rad * scale), color);
            }

            void DrawCircle(double x, double y, double rad, Color color, float strokeWidth)
            {
                args.DrawingSession.DrawCircle(
                   (float)((x * scale) + xPlus), (float)((y * scale) + yPlus), (float)(rad * scale), color, strokeWidth * scale);
            }

            void DrawLine(double x1, double y1, double x2, double y2, Color color, float strokeWidth, CanvasCapStyle capStyle) 
            {
                args.DrawingSession.DrawLine(
                    new Vector2((float)((x1 * scale) + xPlus), (float)((y1 * scale) + yPlus)),
                    new Vector2((float)((x2 * scale) + xPlus), (float)((y2 * scale) + yPlus)),
                    color,
                    strokeWidth * scale,
                    new CanvasStrokeStyle
                    {
                        EndCap = capStyle
                    });
            }
        }
    }

    //class RenderGameState
    //{

    //    private Canvas gameArea;
    //    private FullField gameView;
    //    private readonly TextBlock leftScore, rightScore;

    //    private Dictionary<Guid, PlayerExtension> players = new Dictionary<Guid, PlayerExtension>();
    //    private BallExtension ballExtension;
    //    private GoalExtension leftGoal;
    //    private GoalExtension rightGoal;
    //    private BallWallExtension ballWallExtension;
    //    private List<ParameterExtension> parameter;

    //    public RenderGameState(Canvas gameArea, FullField gameView, TextBlock leftScore, TextBlock rightScore)
    //    {
    //        this.gameArea = gameArea ?? throw new ArgumentNullException(nameof(gameArea));
    //        this.gameView = gameView ?? throw new ArgumentNullException(nameof(gameView));
    //        this.leftScore = leftScore ?? throw new ArgumentNullException(nameof(leftScore));
    //        this.rightScore = rightScore ?? throw new ArgumentNullException(nameof(rightScore));
    //    }

    //    public void Init()
    //    {

    //    }

    //    private void MoveTo(FrameworkElement element, double x, double y)
    //    {
    //        element.TransformMatrix =
    //                    // first we center
    //                    new Matrix4x4(
    //                        1, 0, 0, 0,
    //                        0, 1, 0, 0,
    //                        0, 0, 1, 0,
    //                        (float)(-element.Width / 2.0), (float)(-element.Height / 2.0), 0, 1)
    //                    *
    //                    // then we move to the right spot
    //                    new Matrix4x4(
    //                        1, 0, 0, 0,
    //                        0, 1, 0, 0,
    //                        0, 0, 1, 0,
    //                        (float)x, (float)y, 0, 1);
    //    }

    //    public void Update(GameState gameState)
    //    {
    //        #region players
    //        foreach (var playerPair in gameState.players)
    //        {
    //            if (players.TryGetValue(playerPair.Key, out var currentPlayer))
    //            {
    //                //update
    //                MoveTo(currentPlayer.body, playerPair.Value.PlayerBody.Position.x, playerPair.Value.PlayerBody.Position.y);
    //                MoveTo(currentPlayer.text, playerPair.Value.PlayerBody.Position.x, playerPair.Value.PlayerBody.Position.y);
    //                MoveTo(currentPlayer.foot, playerPair.Value.PlayerFoot.Position.x, playerPair.Value.PlayerFoot.Position.y);
    //                ((SolidColorBrush)currentPlayer.body.Fill).Color = Color.FromArgb(playerPair.Value.PlayerBody.A, playerPair.Value.PlayerBody.R, playerPair.Value.PlayerBody.G, playerPair.Value.PlayerBody.B);
    //                ((SolidColorBrush)currentPlayer.foot.Fill).Color = Color.FromArgb(playerPair.Value.PlayerFoot.A, playerPair.Value.PlayerFoot.R, playerPair.Value.PlayerFoot.G, playerPair.Value.PlayerFoot.B);
    //                currentPlayer.text.Text = playerPair.Value.Name;
    //            }
    //            else
    //            {
    //                // add
    //                var body = new Ellipse()
    //                {
    //                    Height = 2 * Constants.footLen,
    //                    Width = 2 * Constants.footLen
    //                };
    //                var foot = new Ellipse()
    //                {
    //                    Height = 2 * Constants.PlayerRadius,
    //                    Width = 2 * Constants.PlayerRadius
    //                };
    //                var text = new TextBlock()
    //                {
    //                    FontSize = 1600,
    //                };

    //                MoveTo(body, playerPair.Value.PlayerBody.Position.x, playerPair.Value.PlayerBody.Position.y);
    //                MoveTo(text, playerPair.Value.PlayerBody.Position.x, playerPair.Value.PlayerBody.Position.y);
    //                MoveTo(foot, playerPair.Value.PlayerFoot.Position.x, playerPair.Value.PlayerFoot.Position.y);

    //                Canvas.SetZIndex(body, Constants.bodyZ);
    //                Canvas.SetZIndex(foot, Constants.footZ);
    //                Canvas.SetZIndex(text, Constants.textZ);

    //                body.Fill = new SolidColorBrush(Color.FromArgb(playerPair.Value.PlayerBody.A, playerPair.Value.PlayerBody.R, playerPair.Value.PlayerBody.G, playerPair.Value.PlayerBody.B));
    //                foot.Fill = new SolidColorBrush(Color.FromArgb(playerPair.Value.PlayerFoot.A, playerPair.Value.PlayerFoot.R, playerPair.Value.PlayerFoot.G, playerPair.Value.PlayerFoot.B));
    //                text.Foreground = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0xff, 0xff));

    //                gameArea.Children.Add(body);
    //                gameArea.Children.Add(foot);
    //                gameArea.Children.Add(text);

    //                players.Add(playerPair.Key, new PlayerExtension(foot, body, text));
    //            }
    //        }

    //        var toRemove = new List<Guid>();

    //        foreach (var currentPlayer in players)
    //        {
    //            if (!gameState.players.ContainsKey(currentPlayer.Key))
    //            {
    //                // remove
    //                toRemove.Add(currentPlayer.Key);
    //                gameArea.Children.Remove(currentPlayer.Value.body);
    //                gameArea.Children.Remove(currentPlayer.Value.foot);
    //                gameArea.Children.Remove(currentPlayer.Value.text);
    //            }
    //        }
    //        foreach (var removing in toRemove)
    //        {
    //            players.Remove(removing);
    //        }
    //        #endregion


    //        #region ball
    //        if (ballExtension == null)
    //        {
    //            // create ball
    //            var ball = new Ellipse()
    //            {
    //                Height = 2 * Constants.BallRadius,
    //                Width = 2 * Constants.BallRadius,
    //                Fill = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0x00, 0x00))
    //            };

    //            MoveTo(ball, gameState.GameBall.Posistion.x, gameState.GameBall.Posistion.y);

    //            Canvas.SetZIndex(ball, Constants.ballZ);

    //            gameArea.Children.Add(ball);

    //            ballExtension = new BallExtension
    //            {
    //                ellipse = ball
    //            };
    //        }
    //        else
    //        {
    //            // move
    //            MoveTo(ballExtension.ellipse, gameState.GameBall.Posistion.x, gameState.GameBall.Posistion.y);
    //        }
    //        #endregion


    //        #region goals
    //        if (leftGoal == null)
    //        {
    //            leftGoal = new GoalExtension
    //            {
    //                ellipse = MakeGoal(gameState.LeftGoal)
    //            };
    //        }
    //        if (rightGoal == null)
    //        {
    //            rightGoal = new GoalExtension
    //            {
    //                ellipse = MakeGoal(gameState.RightGoal)
    //            };
    //        }
    //        #endregion

    //        #region ball wall
    //        if (ballWallExtension == null)
    //        {

    //            var ballWall = new Ellipse
    //            {
    //                Visibility = Visibility.Collapsed,
    //                Stroke = new SolidColorBrush(Color.FromArgb(0x88, 0xff, 0xff, 0xff)),
    //            };
    //            Canvas.SetZIndex(ballWall, Constants.footZ);
    //            this.gameArea.Children.Add(ballWall);
    //            ballWallExtension = new BallWallExtension
    //            {
    //                ellipse = ballWall
    //            };
    //        }
    //        ballWallExtension.ellipse.Visibility = gameState.CountDownState.Countdown ? Visibility.Visible : Visibility.Collapsed;
    //        ballWallExtension.ellipse.Width = gameState.CountDownState.Radius * 2;
    //        ballWallExtension.ellipse.Height = gameState.CountDownState.Radius * 2;
    //        ballExtension.ellipse.Opacity = gameState.CountDownState.Countdown ? gameState.CountDownState.BallOpacity : 1;
    //        MoveTo(ballWallExtension.ellipse, gameState.CountDownState.X, gameState.CountDownState.Y);
    //        ballWallExtension.ellipse.StrokeThickness = gameState.CountDownState.StrokeThickness;
    //        #endregion

    //        #region score
    //        leftScore.Text = gameState.LeftScore + "";
    //        rightScore.Text = gameState.RightScore + "";
    //        #endregion

    //        #region Walls
    //        if (parameter == null)
    //        {
    //            parameter = new List<ParameterExtension>();
    //            foreach (var segment in gameState.PerimeterSegments)
    //            {
    //                var line = new Line
    //                {
    //                    Stroke = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0x00, 0x00)),
    //                    X1 = segment.Start.x,
    //                    Y1 = segment.Start.y,
    //                    X2 = segment.End.x,
    //                    Y2 = segment.End.y,
    //                    StrokeThickness = 100
    //                };
    //                var parameterPart = new ParameterExtension
    //                {
    //                    line = line
    //                };

    //                parameter.Add(parameterPart);
    //                Canvas.SetZIndex(line, Constants.footZ);
    //                this.gameArea.Children.Add(line);
    //            }
    //        }
    //        #endregion

    //        var (playerX, playerY, xPlus, yPlus) = gameView.Update(Array.Empty<Position>());

    //        this.gameArea.TransformMatrix = new Matrix4x4(
    //            (float)(gameView.GetTimes()), 0, 0, 0,
    //            0, (float)(gameView.GetTimes()), 0, 0,
    //            0, 0, 1, 0,
    //            (float)xPlus, (float)yPlus, 0, 1);
    //    }

    //    // maybe this should just be a "make thing"
    //    private Ellipse MakeGoal(GameState.Goal gameStateGoal)
    //    {
    //        var goal = new Ellipse()
    //        {
    //            Height = 2 * Constants.goalLen,
    //            Width = 2 * Constants.goalLen
    //        };

    //        MoveTo(goal, gameStateGoal.Posistion.x, gameStateGoal.Posistion.y);

    //        goal.Fill = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0xff, 0xff));

    //        Canvas.SetZIndex(goal, Constants.goalZ);

    //        gameArea.Children.Add(goal);

    //        return goal;
    //    }
    //}
}
