using Common;
using Physics2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteSoccer
{
    class AIInputs2 : IInputs
    {
        GameState gameState;
        private readonly Guid self;
        private readonly Guid[] teammates;

        public FieldDimensions fieldDimensions;
        private readonly bool leftGoal;

        public AIInputs2(GameState gameState, Guid self, Guid[] teammates, FieldDimensions fieldDimensions, bool leftGoal)
        {
            this.gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
            this.self = self;
            this.teammates = teammates;
            this.fieldDimensions = fieldDimensions;
            this.leftGoal = leftGoal;
        }

        public Task Init() => Task.CompletedTask;

        Vector lastDirectionFoot;
        Vector lastDirection;
        int throwing = 0;
        public Task<PlayerInputs> Next()
        {
            var inputs = new PlayerInputs()
            {
                ControlScheme = ControlScheme.AI,
                Boost = false,
                Id = self,
            };

            var r = new Random();
            if (r.NextDouble() < (1 / 30.0))
            {
                lastDirection = GenerateDirection();
                lastDirectionFoot = GenerateDirectionFoot();
            }
            inputs.BodyX = lastDirection.x;
            inputs.BodyY = lastDirection.y;

            if (r.NextDouble() < (1 / 10.0))
            {
                lastDirectionFoot = GenerateDirectionFoot();
            }
            var me = gameState.players[self];
            var currentOffset = me.PlayerFoot.Position.NewAdded(me.PlayerBody.Position.NewMinus());
            var move = lastDirectionFoot.NewAdded(currentOffset.NewMinus()).NewScaled(1 / 20.0);

            inputs.FootX = move.x;
            inputs.FootY = move.y;

            var throwAt = 30;
            if (gameState.GameBall.OwnerOrNull == self && throwing < throwAt && ShouldThrow(out var direction))
            {
                lastThrow = direction;
                inputs.Throwing = true;
                inputs.FootX = direction.x;
                inputs.FootY = direction.y;
                throwing++;
            }
            else if (gameState.GameBall.OwnerOrNull == self && throwing > 0 && throwing < throwAt)
            {
                inputs.Throwing = true;
                inputs.FootX = lastThrow.x;
                inputs.FootY = lastThrow.y;
                throwing++;
            }
            else
            {
                throwing = 0;
            }

            return Task<PlayerInputs>.FromResult(inputs);
        }

        private Vector GoalWeScoreOn()
        {
            if (leftGoal)
            {
                return gameState.LeftGoal.Posistion;
            }
            return gameState.RightGoal.Posistion;
        }

        private Vector GoalTheyScoreOn()
        {
            if (leftGoal)
            {
                return gameState.RightGoal.Posistion;
            }
            return gameState.LeftGoal.Posistion;
        }

        private Lazy<Vector[]> goalOffsets = new Lazy<Vector[]>(() => {
            var r = new Random();

            // this isn't a good random for a circle. it perfers pie/4 to pie/2
            return new int[10].Select(_ => new Vector((1 - (2 * r.NextDouble())), (1 - (2 * r.NextDouble()))).NewUnitized().NewScaled(Constants.goalLen * r.NextDouble())).ToArray();
        });

        private bool ShouldThrow(out Vector direction)
        {
            // you have to have the ball to throw
            if (gameState.GameBall.OwnerOrNull != self)
            {
                direction = default;
                return false;
            }

            var myBody = gameState.players[self].PlayerBody.Position;


            // if someone is right on top of ya don't throw
            //foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
            //{
            //    // bad to be near the badie
            //    if (myBody.NewAdded(player.Value.PlayerBody.Position.NewMinus()).Length < Constants.footLen) {
            //        direction = default;
            //        return false;
            //    }
            //}

            var r = new Random();

            //// consider taking a shot on goal
            //var clear = goalOffsets.Value
            //    .Select(x => GoalWeScoreOn().NewAdded(x))
            //    .Where(pos => CanPass(pos, gameState.players.Where(x => x.Key != self).Select(x => x.Value.PlayerBody.Position).Union(new Vector[] { GoalTheyScoreOn() }).ToArray()))
            //    .Select(x => x.NewAdded(myBody.NewMinus()).NewUnitized())
            //    .ToArray();

            //if (clear.Any()) 
            //{
            //    direction = clear[0];
            //    return true;
            //}

            // consider passing
            //var options = gameState.players
            //    .Where(x => teammates.Contains(x.Key))
            //    .Where(x=> CanPass(x.Value.PlayerFoot.Position))
            //    .Select(teamie => (
            //        direction: Scale(ref teamie, gameState.players[self].PlayerFoot.Position),
            //        positionValue: EvaluatePass(teamie.Value.PlayerFoot.Position)))
            //    .OrderByDescending(x => x.positionValue)
            //    .ToArray();

            var options = new int[500]
                .Select(_ => new Vector(r.Next(0, (int)fieldDimensions.xMax), r.Next(0, (int)fieldDimensions.yMax)))
                .Where(pos => CanPass(pos, gameState.players.Where(x => x.Key != self).Select(x => x.Value.PlayerBody.Position).Union(new Vector[] { GoalTheyScoreOn() }).ToArray()))
                .Select(pos => (
                    direction: Scale(pos, gameState.players[self].PlayerFoot.Position),
                    positionValue: EvaluatePassToSpace(pos, false)))
                .OrderByDescending(x => x.positionValue)
                .ToArray();

            if (options.Any() && options[0].positionValue > EvaluatePassToSpace(myBody, true) + Constants.footLen)
            {
                direction = options[0].direction;
                return true;
            }

            direction = default;
            return false;
        }

        private static Vector Scale(ref KeyValuePair<Guid, GameState.Player> teamie, Vector myPosition)
        {
            var direction = teamie.Value.PlayerBody.Position.NewAdded(myPosition.NewMinus());
            if (direction.Length > 60000)
            {
                return direction.NewUnitized();
            }
            else
            {
                return direction.NewUnitized().NewScaled(Math.Sqrt(direction.Length / 60000.0));
            }
        }

        private static Vector Scale(Vector target, Vector myPosition)
        {
            var direction = target.NewAdded(myPosition.NewMinus());
            if (direction.Length > 60000)
            {
                return direction.NewUnitized();
            }
            else
            {
                return direction.NewUnitized().NewScaled(Math.Sqrt(direction.Length / 60000.0));
            }
        }

        private double EvaluatePass(Vector position)
        {
            var res = 0.0;

            // don't go near the other team
            foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
            {
                res -= TowardsWithIn(position, player.Value.PlayerBody.Position, 3, Constants.footLen * 2);
            }

            // go to the goal
            res += Towards(position, GoalWeScoreOn(), .5);



            var myPosition = gameState.players[self].PlayerBody.Position;

            // bad to be too close to us
            res -= TowardsWithIn(position, myPosition, 3, Constants.footLen * 2);
            res -= TowardsWithOut(position, myPosition, 3, Constants.footLen * 10);

            return res;
        }


        private double EvaluatePassToSpace(Vector position, bool evaluateSelf)
        {
            var res = 0.0;

            // don't go near the other team
            foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
            {
                res -= TowardsWithIn(position, player.Value.PlayerBody.Position, 1, Constants.footLen * 10);
                res -= TowardsWithIn(position, player.Value.PlayerBody.Position, 3, Constants.footLen * 2);
            }

            if (!evaluateSelf)
            {
                // do go near your team
                foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key) || x.Key == self))
                {
                    res += TowardsWithIn(position, player.Value.PlayerBody.Position, 1, Constants.footLen * 10);
                    res += TowardsWithIn(position, player.Value.PlayerBody.Position, 2, Constants.footLen * 2);
                }
            }

            // shoot at the goal
            res += TowardsWithIn(position, GoalWeScoreOn(), 10, Constants.goalLen);

            // go to the goal
            res += Towards(position, GoalWeScoreOn(), 1);

            // go away from our goal
            res -= Towards(position, GoalTheyScoreOn(), 1);
            res -= TowardsWithIn(position, GoalTheyScoreOn(), 10, Constants.footLen * 4);

            if (!evaluateSelf)
            {
                var myPosition = gameState.players[self].PlayerBody.Position;

                // bad to be too close to us
                res -= TowardsWithIn(position, myPosition, 3, Constants.footLen * 2);
                res -= TowardsWithOut(position, myPosition, 3, Constants.footLen * 8);
            }

            return res;
        }


        private bool CanPass(Vector target, Vector[] obsticals)
        {
            var myPosition = gameState.players[self].PlayerFoot.Position;

            var passDirection = target.NewAdded(myPosition.NewMinus());
            foreach (var obstical in obsticals)
            {
                if (PassIsBlockedBy(target, obstical) > .9)
                {
                    return false;
                }
            }
            return true;
        }

        private double PassIsBlockedBy(Vector target, Vector obstical)
        {
            var passDirection = target.NewAdded(gameState.GameBall.Posistion.NewMinus());
            var obsticalDirection = obstical.NewAdded(gameState.GameBall.Posistion.NewMinus());
            if (passDirection.Length < obsticalDirection.Length)
            {
                return 0;
            }
            return passDirection.NewUnitized().Dot(obsticalDirection.NewUnitized());
        }

        private double PassIsBlockedBy(Vector target)
        {
            return gameState.players
                .Where(x => !teammates.Contains(x.Key) && x.Key != self)
                .Select(obstical => PassIsBlockedBy(target, obstical.Value.PlayerBody.Position))
                .Union(new[] { PassIsBlockedBy(target, GoalTheyScoreOn()) })
                .Max();
        }

        private Vector GenerateDirection()
        {
            var myPosition = gameState.players[self].PlayerBody.Position;

            var r = new Random();

            var best = new int[100]
                // this isn't a good random it perfers 
                .Select(_ => myPosition.NewAdded(new Vector((1 - (2 * r.NextDouble())), (1 - (2 * r.NextDouble()))).NewUnitized().NewScaled(Constants.goalLen * r.NextDouble())))
                .Where(pos => pos.x < fieldDimensions.xMax && pos.x > 0 && pos.y < fieldDimensions.yMax && pos.y > 0)
                .Select(pos => (position: pos, score: GlobalEvaluate(pos)))
                .OrderByDescending(pair => pair.score)
                .First();

            var direction = best.position.NewAdded(myPosition.NewMinus());

            if (direction.Length > 0)
            {
                direction = direction.NewUnitized();
            }

            return direction;
        }


        private Lazy<Vector[]> footOffsets = new Lazy<Vector[]>(() => {
            var r = new Random();

            // this isn't a good random for a circle. it perfers pie/4 to pie/2
            return new int[100].Select(_ => new Vector((1 - (2 * r.NextDouble())), (1 - (2 * r.NextDouble()))).NewUnitized().NewScaled(Constants.footLen * 1.5 * r.NextDouble())).ToArray();
        });
        private Vector lastThrow;

        private Vector GenerateDirectionFoot()
        {
            var myPosition = gameState.players[self].PlayerBody.Position;


            var best = footOffsets.Value
                .Select(x => myPosition.NewAdded(x))
                .Where(pos => pos.x < fieldDimensions.xMax && pos.x > 0 && pos.y < fieldDimensions.yMax && pos.y > 0)
                .Select(pos => (position: pos, score: GlobalEvaluateFoot(pos)))
                .OrderByDescending(pair => pair.score)
                .First();

            var direction = best.position.NewAdded(myPosition.NewMinus());

            return direction;
        }

        // positive is good 
        public double GlobalEvaluate(Vector myPosition)
        {
            var res = 0.0;

            if (gameState.GameBall.OwnerOrNull == self) // when you have the ball
            {
                // don't go near the other team
                foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                {
                    res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 3, Constants.footLen * 2);
                }

                // go to the goal
                res += Towards(myPosition, GoalWeScoreOn(), .5);
            }
            else if (gameState.GameBall.OwnerOrNull == null)// when no one has the ball
            {
                // go towards the ball
                // unless other teammates are closer
                res += Towards(myPosition, gameState.GameBall.Posistion, 1);

                // if you are the closest on your team to the ball you need to get it
                if (gameState.players.Where(x => teammates.Contains(x.Key))
                    .Union(new[] { new KeyValuePair<Guid, GameState.Player>(self, gameState.players[self]) })
                    .Select(pair => (pair, pair.Value.PlayerBody.Position.NewAdded(GoalTheyScoreOn().NewMinus()).Length))
                    .OrderBy(pair => pair.Length)
                    .Skip(1)
                    .Select(pair => pair.pair)
                    .Select(pair => (pair.Key, pair.Value.PlayerBody.Position.NewAdded(gameState.GameBall.Posistion.NewMinus()).Length))
                    .OrderBy(pair => pair.Length)
                    .First().Key == self)
                {
                    res += Towards(myPosition, gameState.GameBall.Posistion, 5);
                }

                // really gotards the ball if you are close to it
                res += TowardsWithIn(myPosition, gameState.GameBall.Posistion, 10, Constants.footLen * 5);

                // spread out ?
                foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key)))
                {
                    res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 2, Constants.footLen * 12);
                }

                res += Goalie(myPosition, 4);
            }
            else if (teammates.Contains((Guid)gameState.GameBall.OwnerOrNull)) // one of you teammates has the ball
            {
                // go towards the goal
                res += Towards(myPosition, GoalWeScoreOn(), .5);

                // stay away from your teammates
                foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key)))
                {
                    res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 3, Constants.footLen * 8);
                }

                // don't get to close to the other teams players
                foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                {
                    res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 1, Constants.footLen * 8);
                    res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 3, Constants.footLen * 3);
                }

                // don't hang out where they can't pass to you
                // PassIsBlockedBy has werid units thus the Constants.footLen
                res -= PassIsBlockedBy(myPosition) * Constants.footLen * 6;

                // don't get too far from the ball
                res -= TowardsWithOut(myPosition, gameState.GameBall.Posistion, 3, Constants.footLen * 12);

                // we like to go the way we are going
                var currentVelocity = gameState.players[self].PlayerBody.Velocity;
                if (currentVelocity.Length > 0)
                {
                    res += myPosition.NewAdded(gameState.players[self].PlayerBody.Position.NewMinus()).Dot(currentVelocity.NewUnitized()) * Constants.footLen / 1000.0;
                }

            }
            else
            { // the other team has the ball

                // go towards your goal
                res += Towards(myPosition, GoalTheyScoreOn(), 1);
                // but don't go in it
                res -= TowardsWithIn(myPosition, GoalTheyScoreOn(), 2, Constants.footLen);

                // stay away from your teammates
                foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key)))
                {
                    res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 5, Constants.footLen * 2);
                    res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 1, Constants.footLen * 5);
                }

                // go towards the ball
                res += Towards(myPosition, gameState.GameBall.Posistion, 1);
                // go towards the ball hard if you are close
                res += TowardsWithIn(myPosition, gameState.GameBall.Posistion, 10, Constants.footLen * 4);

                // go towards players of the other team
                foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                {
                    res += TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 5, Constants.footLen * 2);
                    res += TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 1, Constants.footLen * 5);
                }

                // try and be between them and the goal
                //res += PassIsBlockedBy(GoalTheyScoreOn(), myPosition) * Constants.footLen * 2;

                // try to be between the ball and the other team
                foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                {
                    res += PassIsBlockedBy(GoalTheyScoreOn(), myPosition) * Constants.footLen;
                }

                res += Goalie(myPosition, 4);
            }


            // this is mostly just annoying
            // stay away from edges
            //res += TowardsXWithIn(myPosition, new Vector(0, 0), -1, Constants.footLen));
            //res += TowardsXWithIn(myPosition, new Vector(fieldDimensions.xMax, 0), -1, Constants.footLen));

            //res += TowardsYWithIn(myPosition, new Vector(0, 0), -1, Constants.footLen));
            //res += TowardsYWithIn(myPosition, new Vector(0, fieldDimensions.yMax), -1, Constants.footLen));

            return res;
        }

        private double Goalie(Vector myPosition, double scale)
        {
            if (gameState.players.Where(x => teammates.Contains(x.Key))
                .Union(new[] { new KeyValuePair<Guid, GameState.Player>(self, gameState.players[self]) })
                .Select(pair => (pair.Key, pair.Value.PlayerBody.Position.NewAdded(GoalTheyScoreOn().NewMinus()).Length))
                .OrderBy(pair => pair.Length)
                .First().Key == self)
            {
                // be between the ball and the goal
                return Towards(myPosition, gameState.GameBall.Posistion.NewAdded(GoalTheyScoreOn()).NewScaled(.5), scale);
            }
            else
            {
                return 0;
            }
        }

        public double GlobalEvaluateFoot(Vector myPosition)
        {
            var myBody = gameState.players[self].PlayerBody.Position;

            var res = 0.0;

            if (gameState.GameBall.OwnerOrNull == self) // when you have the ball
            {
                // don't go near the other team
                foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                {
                    res -= TowardsWithInBody(myBody, myPosition, player.Value.PlayerFoot.Position, 4, Constants.footLen * 1.5);
                }

                // go to the goal
                res += TowardsWithInBody(myBody, myPosition, GoalWeScoreOn(), 1, Constants.footLen * 2);
            }
            else if (gameState.GameBall.OwnerOrNull == null)// when no one has the ball
            {
                // go towards the ball when it is in play
                if (!gameState.CountDownState.Countdown)
                {
                    res += TowardsWithInBody(myBody, myPosition, gameState.GameBall.Posistion, 10, Constants.footLen * 1.5);
                }
            }
            else if (teammates.Contains((Guid)gameState.GameBall.OwnerOrNull)) // one of you teammates has the ball
            {

                // stay away from your teammates
                foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key)))
                {
                    res -= TowardsWithIn(myPosition, player.Value.PlayerFoot.Position, 1, Constants.footLen);
                }

                // bop the other team
                foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                {
                    res += TowardsWithInBody(myBody, myPosition, player.Value.PlayerFoot.Position, 2, Constants.footLen * 1.5);
                }
            }
            else // the other team has the ball
            {

                // stay away from your teammates
                foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key)))
                {
                    res -= TowardsWithIn(myPosition, player.Value.PlayerFoot.Position, 1, Constants.footLen * 2);
                }

                // go towards the ball hard if you are close
                res += TowardsWithInBody(myBody, myPosition, gameState.GameBall.Posistion, 10, Constants.footLen * 1.5);

                // go towards players of the other team
                foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                {
                    res += TowardsWithInBody(myBody, myPosition, player.Value.PlayerFoot.Position, 2, Constants.footLen * 1.5);
                }
            }

            // a small force back towards the center
            res += TowardsWithIn(myPosition, gameState.players[self].PlayerBody.Position, .5, Constants.footLen / 3.0);


            // feet don't like to stay still while extended
            if (gameState.players[self].PlayerFoot.Position.NewAdded(gameState.players[self].PlayerBody.Position.NewMinus()).Length > Constants.footLen / 2.0)
            {
                res -= TowardsWithIn(myPosition, gameState.players[self].PlayerFoot.Position, .5, Constants.footLen / 3.0);
            }

            // this is mostly just annoying
            // stay away from edges
            //res += TowardsXWithIn(myPosition, new Vector(0, 0), -1, Constants.footLen));
            //res += TowardsXWithIn(myPosition, new Vector(fieldDimensions.xMax, 0), -1, Constants.footLen));

            //res += TowardsYWithIn(myPosition, new Vector(0, 0), -1, Constants.footLen));
            //res += TowardsYWithIn(myPosition, new Vector(0, fieldDimensions.yMax), -1, Constants.footLen));

            return res;
        }

        private Vector TowardsXWithIn(Vector us, Vector them, double scale, double whenWithIn)
        {

            var startWith = them.NewAdded(us.NewMinus());
            var len = Math.Abs(startWith.x);
            if (len > 0 && len < whenWithIn)
            {
                return new Vector(startWith.x, 0).NewUnitized().NewScaled(scale * (whenWithIn - len));
            }
            return new Vector(0, 0);
        }

        private Vector TowardsYWithIn(Vector us, Vector them, double scale, double whenWithIn)
        {

            var startWith = them.NewAdded(us.NewMinus());
            var len = Math.Abs(startWith.y);
            if (len > 0 && len < whenWithIn)
            {
                return new Vector(0, startWith.y).NewUnitized().NewScaled(scale * (whenWithIn - len));
            }
            return new Vector(0, 0);
        }

        private double Towards(Vector us, Vector them, double scale)
        {
            var startWith = them.NewAdded(us.NewMinus());
            return -startWith.Length * scale;
        }

        //private Vector ConeWithIn(Vector us, Vector them, double scale, double whenWithIn)
        //{
        //    var startWith = them.NewAdded(us.NewMinus());
        //    var len = startWith.Length;
        //    if (len > 0 && len < whenWithIn)
        //    {
        //        return startWith.NewUnitized().NewScaled(scale* (whenWithIn - len));
        //    }
        //    return new Vector(0, 0);
        //}

        private double Cone(Vector us, Vector them, double scale)
        {
            var startWith = them.NewAdded(us.NewMinus());
            if (startWith.Length > 0)
            {
                return startWith.Length * scale;
            }
            return 0;
        }


        private double Cone(double length, double scale, double radius)
        {
            if (length < radius)
            {
                return (radius - length) * scale;
            }
            return 0;
        }

        private double TowardsWithIn(Vector us, Vector them, double scale, double whenWithIn)
        {

            var startWith = them.NewAdded(us.NewMinus());
            var len = startWith.Length;
            if (len > 0 && len < whenWithIn)
            {
                return scale * (whenWithIn - len);
            }
            return 0;
        }

        private double TowardsWithInBody(Vector body, Vector us, Vector them, double scale, double whenWithIn)
        {

            var startWith = them.NewAdded(body.NewMinus());
            var len = startWith.Length;
            if (len > 0 && len < whenWithIn)
            {
                return -scale * them.NewAdded(us.NewMinus()).Length;
            }
            return 0;
        }


        private double TowardsWithOut(Vector us, Vector them, double scale, double whenWithOut)
        {

            var startWith = them.NewAdded(us.NewMinus());
            var len = startWith.Length;
            if (len > whenWithOut)
            {
                return scale * (len - whenWithOut);
            }
            return 0;
        }


        //private Vector Cone(Vector us, Vector them, double scale)
        //{
        //    var startWith = them.NewAdded(us.NewMinus());
        //    if (startWith.Length > 0)
        //    {
        //        return startWith.NewScaled(scale);
        //    }
        //    return new Vector(0, 0);
        //}

        private double AntiCone(double length, double scale, double radius)
        {
            if (length > radius)
            {
                return (length - radius) * scale;
            }
            return 0;
        }

        private double Bell(double length, double peak, double width)
        {
            return peak * Math.Pow(Math.E, -(length * length) / (2 * width * width));
        }
    }

    // this should really be in Common
    class AIInputs : IInputs
    {
        GameState gameState;
        private readonly Guid self;
        private readonly Guid[] teammates;

        public FieldDimensions fieldDimensions;
        private readonly bool leftGoal;

        public AIInputs(GameState gameState, Guid self, Guid[] teammates, FieldDimensions fieldDimensions, bool leftGoal)
        {
            this.gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
            this.self = self;
            this.teammates = teammates;
            this.fieldDimensions = fieldDimensions;
            this.leftGoal = leftGoal;
        }

        public Task Init() => Task.CompletedTask;
        
        Vector lastDirectionFoot;
        Vector lastDirection;
        int throwing = 0;
        public Task<PlayerInputs> Next()
        {
            var inputs = new PlayerInputs()
            {
                ControlScheme = ControlScheme.AI,
                Boost = false,
                Id = self,
            };

            var r = new Random();
            if (r.NextDouble() < (1 / 30.0))
            {
                lastDirection = GenerateDirection();
                lastDirectionFoot = GenerateDirectionFoot();
            }
            inputs.BodyX = lastDirection.x;
            inputs.BodyY = lastDirection.y;

            if (r.NextDouble() < (1 / 10.0))
            {
                lastDirectionFoot = GenerateDirectionFoot();
            }
            var me = gameState.players[self];
            var currentOffset = me.PlayerFoot.Position.NewAdded(me.PlayerBody.Position.NewMinus());
            var move = lastDirectionFoot.NewAdded(currentOffset.NewMinus()).NewScaled(1 / 20.0);

            inputs.FootX = move.x;
            inputs.FootY = move.y;

            var throwAt = 30;
            if (gameState.GameBall.OwnerOrNull ==self && throwing < throwAt && ShouldThrow(out var direction))
            {
                lastThrow = direction;
                inputs.Throwing = true;
                inputs.FootX = direction.x;
                inputs.FootY = direction.y;
                throwing++;
            }else if (gameState.GameBall.OwnerOrNull == self && throwing > 0 && throwing < throwAt) {
                inputs.Throwing = true;
                inputs.FootX = lastThrow.x;
                inputs.FootY = lastThrow.y;
                throwing++;
            }
            else {
                throwing = 0;
            }

            return Task<PlayerInputs>.FromResult(inputs);
        }

        private Vector GoalWeScoreOn() {
            if (leftGoal)
            {
                return gameState.LeftGoal.Posistion;
            }
            return gameState.RightGoal.Posistion;
        }

        private Vector GoalTheyScoreOn()
        {
            if (leftGoal)
            {
                return gameState.RightGoal.Posistion;
            }
            return gameState.LeftGoal.Posistion;
        }

        private Lazy<Vector[]> goalOffsets = new Lazy<Vector[]>(() => {
            var r = new Random();

            // this isn't a good random for a circle. it perfers pie/4 to pie/2
            return new int[10].Select(_ => new Vector((1 - (2 * r.NextDouble())), (1 - (2 * r.NextDouble()))).NewUnitized().NewScaled(Constants.goalLen * r.NextDouble())).ToArray();
        });

        private bool ShouldThrow(out Vector direction)
        {
            // you have to have the ball to throw
            if (gameState.GameBall.OwnerOrNull != self)
            {
                direction = default;
                return false;
            }

            var myBody = gameState.players[self].PlayerBody.Position;


            // if someone is right on top of ya don't throw
            //foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
            //{
            //    // bad to be near the badie
            //    if (myBody.NewAdded(player.Value.PlayerBody.Position.NewMinus()).Length < Constants.footLen) {
            //        direction = default;
            //        return false;
            //    }
            //}

            var r = new Random();

            //// consider taking a shot on goal
            //var clear = goalOffsets.Value
            //    .Select(x => GoalWeScoreOn().NewAdded(x))
            //    .Where(pos => CanPass(pos, gameState.players.Where(x => x.Key != self).Select(x => x.Value.PlayerBody.Position).Union(new Vector[] { GoalTheyScoreOn() }).ToArray()))
            //    .Select(x => x.NewAdded(myBody.NewMinus()).NewUnitized())
            //    .ToArray();

            //if (clear.Any()) 
            //{
            //    direction = clear[0];
            //    return true;
            //}

            // consider passing
            //var options = gameState.players
            //    .Where(x => teammates.Contains(x.Key))
            //    .Where(x=> CanPass(x.Value.PlayerFoot.Position))
            //    .Select(teamie => (
            //        direction: Scale(ref teamie, gameState.players[self].PlayerFoot.Position),
            //        positionValue: EvaluatePass(teamie.Value.PlayerFoot.Position)))
            //    .OrderByDescending(x => x.positionValue)
            //    .ToArray();

            var options = new int[500]
                .Select(_ => new Vector(r.Next(0, (int)fieldDimensions.xMax), r.Next(0, (int)fieldDimensions.yMax)))
                .Where(pos => CanPass(pos, gameState.players.Where(x=>x.Key != self).Select(x=>x.Value.PlayerBody.Position).Union(new Vector[] { GoalTheyScoreOn()}).ToArray()))
                .Select(pos => (
                    direction: Scale(pos, gameState.players[self].PlayerFoot.Position),
                    positionValue: EvaluatePassToSpace(pos,false)))
                .OrderByDescending(x => x.positionValue)
                .ToArray();

            if (options.Any() && options[0].positionValue > EvaluatePassToSpace(myBody,true) + Constants.footLen)
            {
                direction = options[0].direction;
                return true;
            }

            direction = default;
            return false;
        }

        private static Vector Scale(ref KeyValuePair<Guid, GameState.Player> teamie, Vector myPosition)
        {
            var direction = teamie.Value.PlayerBody.Position.NewAdded(myPosition.NewMinus());
            if (direction.Length > 60000) {
                return direction.NewUnitized();
            } else {
                return direction.NewUnitized().NewScaled(Math.Sqrt(direction.Length / 60000.0));
            }
        }

        private static Vector Scale(Vector target, Vector myPosition)
        {
            var direction = target.NewAdded(myPosition.NewMinus());
            if (direction.Length > 60000)
            {
                return direction.NewUnitized();
            }
            else
            {
                return direction.NewUnitized().NewScaled(Math.Sqrt(direction.Length / 60000.0));
            }
        }

        private double EvaluatePass(Vector position)
        {
            var res = 0.0;
            
            // don't go near the other team
            foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
            {
                res -= TowardsWithIn(position, player.Value.PlayerBody.Position, 3, Constants.footLen * 2);
            }

            // go to the goal
            res += Towards(position, GoalWeScoreOn(), .5);



            var myPosition = gameState.players[self].PlayerBody.Position;

            // bad to be too close to us
            res -= TowardsWithIn(position, myPosition, 3, Constants.footLen * 2);
            res -= TowardsWithOut(position, myPosition, 3, Constants.footLen * 10);

            return res;
        }


        private double EvaluatePassToSpace(Vector position, bool evaluateSelf)
        {
            var res = 0.0;

            // don't go near the other team
            foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
            {
                res -= TowardsWithIn(position, player.Value.PlayerBody.Position, 1, Constants.footLen * 10);
                res -= TowardsWithIn(position, player.Value.PlayerBody.Position, 3, Constants.footLen * 2);
            }
            
            if (!evaluateSelf)
            {
                // do go near your team
                foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key) || x.Key == self))
                {
                    res += TowardsWithIn(position, player.Value.PlayerBody.Position, 1, Constants.footLen * 10);
                    res += TowardsWithIn(position, player.Value.PlayerBody.Position, 2, Constants.footLen * 2);
                }
            }

            // shoot at the goal
            res += TowardsWithIn(position, GoalWeScoreOn(), 10, Constants.goalLen );

            // go to the goal
            res += Towards(position, GoalWeScoreOn(), 1);

            // go away from our goal
            res -= Towards(position, GoalTheyScoreOn(), 1);
            res -= TowardsWithIn(position, GoalTheyScoreOn(), 10, Constants.footLen * 4);

            if (!evaluateSelf)
            {
                var myPosition = gameState.players[self].PlayerBody.Position;

                // bad to be too close to us
                res -= TowardsWithIn(position, myPosition, 3, Constants.footLen * 2);
                res -= TowardsWithOut(position, myPosition, 3, Constants.footLen * 8);
            }

            return res;
        }


        private bool CanPass(Vector target, Vector[] obsticals)
        {
            var myPosition = gameState.players[self].PlayerFoot.Position;

            var passDirection = target.NewAdded(myPosition.NewMinus());
            foreach (var obstical in obsticals)
            {
                if (PassIsBlockedBy(target,obstical) > .9)
                {
                    return false;
                }
            }
            return true;
        }

        private double PassIsBlockedBy(Vector target, Vector obstical) {
            var passDirection = target.NewAdded(gameState.GameBall.Posistion.NewMinus());
            var obsticalDirection = obstical.NewAdded(gameState.GameBall.Posistion.NewMinus());
            if (passDirection.Length < obsticalDirection.Length) {
                return 0;
            }
            return passDirection.NewUnitized().Dot(obsticalDirection.NewUnitized());
        }

        private double PassIsBlockedBy(Vector target)
        {
            return gameState.players
                .Where(x => !teammates.Contains(x.Key) && x.Key != self)
                .Select(obstical => PassIsBlockedBy(target, obstical.Value.PlayerBody.Position))
                .Union(new[] { PassIsBlockedBy(target, GoalTheyScoreOn()) })
                .Max();
        }

        private Vector GenerateDirection()
        {
            var myPosition = gameState.players[self].PlayerBody.Position;

            var r = new Random();

            var best = new int[100] 
                // this isn't a good random it perfers 
                .Select(_ => myPosition.NewAdded(new Vector((1 - (2 * r.NextDouble())), (1 - (2 * r.NextDouble()))).NewUnitized().NewScaled(Constants.goalLen * r.NextDouble())))
                .Where(pos => pos.x < fieldDimensions.xMax && pos.x > 0 && pos.y < fieldDimensions.yMax && pos.y > 0)
                .Select(pos => (position: pos, score: GlobalEvaluate(pos)))
                .OrderByDescending(pair => pair.score)
                .First();

            var direction = best.position.NewAdded(myPosition.NewMinus());

            if (direction.Length > 0) {
                direction = direction.NewUnitized();
            }

            return direction;
        }


        private Lazy<Vector[]> footOffsets = new Lazy<Vector[]>(() => {
            var r = new Random();

            // this isn't a good random for a circle. it perfers pie/4 to pie/2
            return new int[100].Select(_ => new Vector((1 - (2 * r.NextDouble())), (1 - (2 * r.NextDouble()))).NewUnitized().NewScaled(Constants.footLen*1.5 * r.NextDouble())).ToArray();
        });
        private Vector lastThrow;

        private Vector GenerateDirectionFoot()
        {
            var myPosition = gameState.players[self].PlayerBody.Position;


            var best = footOffsets.Value
                .Select(x => myPosition.NewAdded(x))
                .Where(pos => pos.x < fieldDimensions.xMax && pos.x>0 && pos.y< fieldDimensions.yMax && pos.y >0)
                .Select(pos => (position: pos, score: GlobalEvaluateFoot(pos)))
                .OrderByDescending(pair => pair.score)
                .First();

            var direction = best.position.NewAdded(myPosition.NewMinus());
            
            return direction;
        }

        // positive is good 
        public double GlobalEvaluate(Vector myPosition)
        {
            var res = 0.0;

            if (gameState.GameBall.OwnerOrNull == self) // when you have the ball
            {
                // don't go near the other team
                foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                {
                    res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 3, Constants.footLen * 2);
                }

                // go to the goal
                res += Towards(myPosition, GoalWeScoreOn(), .5);
            }
            else if (gameState.GameBall.OwnerOrNull == null)// when no one has the ball
            {
                // go towards the ball
                // unless other teammates are closer
                res += Towards(myPosition, gameState.GameBall.Posistion, 1);

                // if you are the closest on your team to the ball you need to get it
                if (gameState.players.Where(x => teammates.Contains(x.Key))
                    .Union(new[] { new KeyValuePair<Guid, GameState.Player>(self, gameState.players[self]) })
                    .Select(pair => (pair, pair.Value.PlayerBody.Position.NewAdded(GoalTheyScoreOn().NewMinus()).Length))
                    .OrderBy(pair => pair.Length)
                    .Skip(1)
                    .Select(pair => pair.pair)
                    .Select(pair => (pair.Key, pair.Value.PlayerBody.Position.NewAdded(gameState.GameBall.Posistion.NewMinus()).Length))
                    .OrderBy(pair => pair.Length)
                    .First().Key == self) {
                    res += Towards(myPosition, gameState.GameBall.Posistion, 5);
                }

                // really gotards the ball if you are close to it
                res += TowardsWithIn(myPosition, gameState.GameBall.Posistion, 10, Constants.footLen * 5);

                // spread out ?
                foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key)))
                {
                    res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 2, Constants.footLen * 12);
                }

                res += Goalie(myPosition, 4);
            }
            else if (teammates.Contains((Guid)gameState.GameBall.OwnerOrNull)) // one of you teammates has the ball
            {
                // go towards the goal
                res += Towards(myPosition, GoalWeScoreOn(), .5);

                // stay away from your teammates
                foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key)))
                {
                    res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 3, Constants.footLen * 8);
                }

                // don't get to close to the other teams players
                foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                {
                    res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 1, Constants.footLen * 8);
                    res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 3, Constants.footLen * 3);
                }

                // don't hang out where they can't pass to you
                // PassIsBlockedBy has werid units thus the Constants.footLen
                res -= PassIsBlockedBy(myPosition)* Constants.footLen * 6;

                // don't get too far from the ball
                res -= TowardsWithOut(myPosition, gameState.GameBall.Posistion, 3, Constants.footLen * 12);

                // we like to go the way we are going
                var currentVelocity = gameState.players[self].PlayerBody.Velocity;
                if (currentVelocity.Length > 0)
                {
                    res += myPosition.NewAdded(gameState.players[self].PlayerBody.Position.NewMinus()).Dot(currentVelocity.NewUnitized()) * Constants.footLen / 1000.0;
                }

            } else
            { // the other team has the ball

                // go towards your goal
                res += Towards(myPosition, GoalTheyScoreOn(), 1);
                // but don't go in it
                res -= TowardsWithIn(myPosition, GoalTheyScoreOn(), 2, Constants.footLen);

                // stay away from your teammates
                foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key)))
                {
                    res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 5, Constants.footLen * 2);
                    res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 1, Constants.footLen * 5);
                }

                // go towards the ball
                res += Towards(myPosition, gameState.GameBall.Posistion, 1);
                // go towards the ball hard if you are close
                res += TowardsWithIn(myPosition, gameState.GameBall.Posistion, 10, Constants.footLen * 4);

                // go towards players of the other team
                foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                {
                    res += TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 5, Constants.footLen * 2);
                    res += TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 1, Constants.footLen * 5);
                }

                // try and be between them and the goal
                //res += PassIsBlockedBy(GoalTheyScoreOn(), myPosition) * Constants.footLen * 2;

                // try to be between the ball and the other team
                foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                {
                    res += PassIsBlockedBy(GoalTheyScoreOn(), myPosition) * Constants.footLen;
                }

                res += Goalie(myPosition,4);
            }


            // this is mostly just annoying
            // stay away from edges
            //res += TowardsXWithIn(myPosition, new Vector(0, 0), -1, Constants.footLen));
            //res += TowardsXWithIn(myPosition, new Vector(fieldDimensions.xMax, 0), -1, Constants.footLen));

            //res += TowardsYWithIn(myPosition, new Vector(0, 0), -1, Constants.footLen));
            //res += TowardsYWithIn(myPosition, new Vector(0, fieldDimensions.yMax), -1, Constants.footLen));

            return res;
        }

        private double Goalie(Vector myPosition, double scale)
        {
            if (gameState.players.Where(x => teammates.Contains(x.Key))
                .Union(new[] { new KeyValuePair<Guid, GameState.Player>(self, gameState.players[self]) })
                .Select(pair => (pair.Key, pair.Value.PlayerBody.Position.NewAdded(GoalTheyScoreOn().NewMinus()).Length))
                .OrderBy(pair => pair.Length)
                .First().Key == self)
            {
                // be between the ball and the goal
                return Towards(myPosition, gameState.GameBall.Posistion.NewAdded(GoalTheyScoreOn()).NewScaled(.5), scale);
            }
            else {
                return 0;
            }
        }

        public double GlobalEvaluateFoot(Vector myPosition)
        {
            var myBody = gameState.players[self].PlayerBody.Position;

            var res = 0.0;

            if (gameState.GameBall.OwnerOrNull == self) // when you have the ball
            {
                // don't go near the other team
                foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                {
                    res -= TowardsWithInBody(myBody, myPosition, player.Value.PlayerFoot.Position, 4, Constants.footLen * 1.5);
                }

                // go to the goal
                res += TowardsWithInBody(myBody, myPosition, GoalWeScoreOn(), 1, Constants.footLen * 2);
            }
            else if (gameState.GameBall.OwnerOrNull == null)// when no one has the ball
            {
                // go towards the ball when it is in play
                if (!gameState.CountDownState.Countdown)
                {
                    res += TowardsWithInBody(myBody, myPosition, gameState.GameBall.Posistion, 10, Constants.footLen * 1.5);
                }
            }
            else if (teammates.Contains((Guid)gameState.GameBall.OwnerOrNull)) // one of you teammates has the ball
            {

                // stay away from your teammates
                foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key)))
                {
                    res -= TowardsWithIn(myPosition, player.Value.PlayerFoot.Position, 1, Constants.footLen );
                }

                // bop the other team
                foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                {
                    res += TowardsWithInBody(myBody, myPosition, player.Value.PlayerFoot.Position, 2, Constants.footLen * 1.5);
                }
            }
            else // the other team has the ball
            { 

                // stay away from your teammates
                foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key)))
                {
                    res -= TowardsWithIn(myPosition, player.Value.PlayerFoot.Position, 1, Constants.footLen *2);
                }

                // go towards the ball hard if you are close
                res += TowardsWithInBody(myBody,myPosition, gameState.GameBall.Posistion, 10, Constants.footLen * 1.5);

                // go towards players of the other team
                foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                {
                    res += TowardsWithInBody(myBody, myPosition, player.Value.PlayerFoot.Position, 2, Constants.footLen * 1.5);
                }
            }

            // a small force back towards the center
            res += TowardsWithIn(myPosition, gameState.players[self].PlayerBody.Position, .5, Constants.footLen / 3.0);


            // feet don't like to stay still while extended
            if (gameState.players[self].PlayerFoot.Position.NewAdded(gameState.players[self].PlayerBody.Position.NewMinus()).Length > Constants.footLen / 2.0)
            {
                res -= TowardsWithIn(myPosition, gameState.players[self].PlayerFoot.Position, .5, Constants.footLen / 3.0);
            }

            // this is mostly just annoying
            // stay away from edges
            //res += TowardsXWithIn(myPosition, new Vector(0, 0), -1, Constants.footLen));
            //res += TowardsXWithIn(myPosition, new Vector(fieldDimensions.xMax, 0), -1, Constants.footLen));

            //res += TowardsYWithIn(myPosition, new Vector(0, 0), -1, Constants.footLen));
            //res += TowardsYWithIn(myPosition, new Vector(0, fieldDimensions.yMax), -1, Constants.footLen));

            return res;
        }

        private Vector TowardsXWithIn(Vector us, Vector them, double scale, double whenWithIn) {

            var startWith = them.NewAdded(us.NewMinus());
            var len = Math.Abs( startWith.x);
            if (len > 0 && len < whenWithIn)
            {
                return new Vector(startWith.x,0).NewUnitized().NewScaled(scale * (whenWithIn - len));
            }
            return new Vector(0, 0);
        }

        private Vector TowardsYWithIn(Vector us, Vector them, double scale, double whenWithIn)
        {

            var startWith = them.NewAdded(us.NewMinus());
            var len = Math.Abs(startWith.y);
            if (len > 0 && len < whenWithIn)
            {
                return new Vector(0, startWith.y).NewUnitized().NewScaled(scale * (whenWithIn - len));
            }
            return new Vector(0, 0);
        }

        private double Towards(Vector us, Vector them, double scale) {
            var startWith = them.NewAdded(us.NewMinus());
            return -startWith.Length * scale;
        }

        //private Vector ConeWithIn(Vector us, Vector them, double scale, double whenWithIn)
        //{
        //    var startWith = them.NewAdded(us.NewMinus());
        //    var len = startWith.Length;
        //    if (len > 0 && len < whenWithIn)
        //    {
        //        return startWith.NewUnitized().NewScaled(scale* (whenWithIn - len));
        //    }
        //    return new Vector(0, 0);
        //}

        private double Cone(Vector us, Vector them, double scale)
        {
            var startWith = them.NewAdded(us.NewMinus());
            if (startWith.Length > 0)
            {
                return startWith.Length * scale;
            }
            return 0;
        }


        private double Cone(double length, double scale, double radius)
        {
            if (length < radius) {
                return (radius - length)  * scale;
            }
            return 0;
        }

        private double TowardsWithIn(Vector us, Vector them, double scale, double whenWithIn)
        {

            var startWith = them.NewAdded(us.NewMinus());
            var len = startWith.Length;
            if (len > 0 && len < whenWithIn)
            {
                return scale * (whenWithIn - len);
            }
            return 0;
        }

        private double TowardsWithInBody(Vector body, Vector us, Vector them, double scale, double whenWithIn)
        {

            var startWith = them.NewAdded(body.NewMinus());
            var len = startWith.Length;
            if (len > 0 && len < whenWithIn)
            {
                return -scale * them.NewAdded(us.NewMinus()).Length;
            }
            return 0;
        }


        private double TowardsWithOut(Vector us, Vector them, double scale, double whenWithOut)
        {

            var startWith = them.NewAdded(us.NewMinus());
            var len = startWith.Length;
            if (len > whenWithOut)
            {
                return scale * (len - whenWithOut);
            }
            return 0;
        }


        //private Vector Cone(Vector us, Vector them, double scale)
        //{
        //    var startWith = them.NewAdded(us.NewMinus());
        //    if (startWith.Length > 0)
        //    {
        //        return startWith.NewScaled(scale);
        //    }
        //    return new Vector(0, 0);
        //}

        private double AntiCone(double length, double scale, double radius)
        {
            if (length > radius)
            {
                return (length - radius) * scale;
            } 
            return 0;
        }

        private double Bell(double length, double peak, double width)
        {
            return peak * Math.Pow(Math.E, -(length*length) / (2*width*width));
        }
    }
}
