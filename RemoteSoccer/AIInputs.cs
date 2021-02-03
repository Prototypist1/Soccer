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
        Func<GameState, Vector> target = _ => new Vector(0, 0);
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
            if (r.NextDouble() < (1 / 10.0))
            {
                target = GenerateDirection();
            }

            var concreteTarget = target(gameState);
            if (concreteTarget.Length > 1)
            {
                concreteTarget = concreteTarget.NewUnitized();
            }
            inputs.BodyX = concreteTarget.x;
            inputs.BodyY = concreteTarget.y;

            if (r.NextDouble() < (1 / 1.0))
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

        private Lazy<Vector[]> throwOffsets = new Lazy<Vector[]>(() => {
            var r = new Random();

            // this isn't a good random for a circle. it perfers pie/4 to pie/2
            return new int[100].Select(_ => new Vector((1 - (2 * r.NextDouble())), (1 - (2 * r.NextDouble()))).NewUnitized().NewScaled(Constants.footLen * 10 * r.NextDouble())).ToArray();
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

            // I think we could probably search this space much more efficently 
            var options = throwOffsets.Value
                .Select(pos => pos.NewAdded(myBody))
                .Where(pos => pos.x > 0 && pos.x < fieldDimensions.xMax && pos.y < 0 && pos.y > fieldDimensions.yMax)
                // always include the goal
                .Union(goalOffsets.Value.Select(x => GoalWeScoreOn().NewAdded(x)))
                // always include your team
                .Union(gameState.players
                   .Where(x => teammates.Contains(x.Key))
                   .Select(x => x.Value.PlayerFoot.Position))
                .Where(pos => CanPass(pos, gameState.players.Where(x => x.Key != self).Select(x => x.Value.PlayerBody.Position).Union(new Vector[] { GoalTheyScoreOn() }).ToArray()))
                .Select(pos => (
                    direction: Scale(pos, gameState.players[self].PlayerFoot.Position),
                    positionValue: EvaluatePassToSpace(pos, false)))
                .OrderByDescending(x => x.positionValue)
                .ToArray();

            if (options.Any() && options[0].positionValue > EvaluatePassToSpace(myBody, true) + 2 * Constants.footLen)
            {
                direction = options[0].direction;
                return true;
            }

            direction = default;
            return false;
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

        private double EvaluatePassToSpace(Vector position, bool evaluateSelf)
        {
            var res = 0.0;

            // don't go near the other team
            foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
            {
                res -= TowardsWithIn(position, player.Value.PlayerBody.Position, 1, Constants.footLen * 5);
                res -= TowardsWithIn(position, player.Value.PlayerBody.Position, 3, Constants.footLen * 2);
            }

            if (!evaluateSelf)
            {
                // do go near your team
                foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key) || x.Key == self))
                {
                    res += TowardsWithIn(position, player.Value.PlayerBody.Position, 1, Constants.footLen * 5);
                    res += TowardsWithIn(position, player.Value.PlayerBody.Position, 3, Constants.footLen * 2);
                }
            }

            // shoot at the goal
            res += TowardsWithIn(position, GoalWeScoreOn(), 10, Constants.goalLen);

            // go to the goal
            res += Towards(position, GoalWeScoreOn(), 2);

            // go away from our goal
            res -= TowardsWithIn(position, GoalTheyScoreOn(), 1, Constants.footLen * 10);
            res -= TowardsWithIn(position, GoalTheyScoreOn(), 10, Constants.footLen * 4);

            if (!evaluateSelf)
            {
                var myPosition = gameState.players[self].PlayerBody.Position;

                // bad to be too close to us
                res -= TowardsWithIn(position, myPosition, 5, Constants.footLen * 4);
                res -= TowardsWithOut(position, myPosition, 1, Constants.footLen * 12);
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

        private Func<GameState, Vector> GenerateDirection()
        {
            var myPosition = gameState.players[self].PlayerBody.Position;

            var r = new Random();

            var getOtherPlayers = gameState.players
                .Where(x => !teammates.Contains(x.Key) && x.Key != self)
                .Select(x => (Func<GameState, Vector>)((GameState gs) => gs.players[x.Key].PlayerBody.Position.NewAdded(gs.players[self].PlayerBody.Position.NewMinus())));

            var awayFromOtherPlayers = gameState.players
                .Where(x => !teammates.Contains(x.Key) && x.Key != self)
                .Select(x => (Func<GameState, Vector>)((GameState gs) => gs.players[self].PlayerBody.Position.NewAdded(gs.players[x.Key].PlayerBody.Position.NewMinus())));

            var getOtherPlayersOffset = gameState.players
                .Where(x => !teammates.Contains(x.Key) && x.Key != self)
                .SelectMany( x=> new int[10]
                     .Select(_ => (x.Key,offset: new Vector((1 - (2 * r.NextDouble())), (1 - (2 * r.NextDouble()))).NewUnitized().NewScaled(Constants.goalLen * r.NextDouble())))
                )
                .Select(x => (Func<GameState, Vector>)((GameState gs) => gs.players[x.Key].PlayerBody.Position.NewAdded(x.offset).NewAdded(gs.players[self].PlayerBody.Position.NewMinus())));

            var getBall = new[] {
                (Func<GameState, Vector>)((GameState gs) => gs.GameBall.Posistion.NewAdded(gs.players[self].PlayerBody.Position.NewMinus()))
            };

            var getGoalie = new[] {
                (Func<GameState, Vector>)((GameState gs) => gs.GameBall.Posistion.NewAdded(GoalTheyScoreOn()).NewScaled(.5).NewAdded(gs.players[self].PlayerBody.Position.NewMinus()))
            };

            var getGoal = new[] {
                (Func<GameState, Vector>)((GameState gs) => GoalWeScoreOn().NewAdded(gs.players[self].PlayerBody.Position.NewMinus()))
            };

            var random = new int[100]
                .Select(_ => new Vector((1 - (2 * r.NextDouble())), (1 - (2 * r.NextDouble()))).NewUnitized().NewScaled(Constants.goalLen * r.NextDouble()))
                .Select(vec => (Func<GameState, Vector>)((GameState _) => vec));

            var best = random
                .Union(getBall)
                .Union(getOtherPlayers)
                .Union(awayFromOtherPlayers)
                .Union(getOtherPlayersOffset)
                .Union(getGoalie)
                .Union(getGoal)
                .Select(generator => {
                    var dir = generator(gameState);
                    if (dir.Length > Constants.goalLen)
                    {
                        dir = dir.NewUnitized().NewScaled(Constants.goalLen);
                    }
                    var prospect = myPosition.NewAdded(dir);

                    return (generator, pos: prospect);
                })
                .Where(pair => pair.pos.x < fieldDimensions.xMax && pair.pos.x > 0 && pair.pos.y < fieldDimensions.yMax && pair.pos.y > 0)
                .Select(pos => (generator: pos.generator, score: GlobalEvaluate(pos.pos)))
                .OrderByDescending(pair => pair.score)
                .First();

            return best.generator;
        }


        private Lazy<Vector[]> footOffsets = new Lazy<Vector[]>(() => {
            var r = new Random();

            // this isn't a good random for a circle. it perfers pie/4 to pie/2
            return new int[25].Select(_ => new Vector((1 - (2 * r.NextDouble())), (1 - (2 * r.NextDouble()))).NewUnitized().NewScaled(Constants.footLen * 1.5 * r.NextDouble())).ToArray();
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

                var lastHadBall = gameState.players.OrderByDescending(x => x.Value.LastHadBall).First();
                var framesPassed = gameState.Frame - lastHadBall.Value.LastHadBall;
                if (lastHadBall.Value.LastHadBall != 0 && framesPassed < 30)
                {
                    // res += GlobalEvaluate_UpForGrabs(myPosition) * (30 - framesPassed) / 30.0;
                    if (teammates.Contains(lastHadBall.Key) || lastHadBall.Key == self)
                    {
                        res += GlobalEvaluate_OurTeamsBall(myPosition);// * (framesPassed) / 30.0;
                    }
                    else
                    {
                        res += GlobalEvaluate_TheirBall(myPosition);// * (framesPassed) / 30.0;
                    }
                }
                else
                {
                    res += GlobalEvaluate_UpForGrabs(myPosition);
                }
            }
            else if (teammates.Contains((Guid)gameState.GameBall.OwnerOrNull)) // one of you teammates has the ball
            {
                res += GlobalEvaluate_OurTeamsBall(myPosition);
            }
            else
            {
                res += GlobalEvaluate_TheirBall(myPosition);
            }

            // this is mostly just annoying
            // stay away from edges
            //res += TowardsXWithIn(myPosition, new Vector(0, 0), -1, Constants.footLen));
            //res += TowardsXWithIn(myPosition, new Vector(fieldDimensions.xMax, 0), -1, Constants.footLen));

            //res += TowardsYWithIn(myPosition, new Vector(0, 0), -1, Constants.footLen));
            //res += TowardsYWithIn(myPosition, new Vector(0, fieldDimensions.yMax), -1, Constants.footLen));

            return res;
        }


        private double GlobalEvaluate_TheirBall_Role(Vector myPosition)
        {
            var teamAndSelf = gameState.players.Values
                    .Where(x => teammates.Contains(x.Id))
                    .Union(new[] { gameState.players[self] }).ToArray();

            // most important is to have a goalie
            var goalie = teamAndSelf
                .Select(pair => (pair.Id, pair.PlayerBody.Position.NewAdded(GoalTheyScoreOn().NewMinus()).Length))
                .OrderBy(pair => pair.Length)
                .First();

            if (goalie.Id == self)
            {
                return Towards(myPosition, gameState.GameBall.Posistion.NewAdded(GoalTheyScoreOn()).NewScaled(.5), 4);
            }

            teamAndSelf = teamAndSelf.Where(x => x.Id != goalie.Id).ToArray();

            if (gameState.GameBall.OwnerOrNull.HasValue)
            {
                // next we need someone to go after the guy with the ball
                var getTheBall = teamAndSelf
                    .Select(pair => (pair.Id, pair.PlayerBody.Position.NewAdded(gameState.players[gameState.GameBall.OwnerOrNull.Value].PlayerBody.Position.NewMinus()).Length))
                    .OrderBy(pair => pair.Length)
                    .First();

                if (getTheBall.Id == self)
                {
                    return Towards(myPosition, gameState.players[gameState.GameBall.OwnerOrNull.Value].PlayerBody.Position, 4);
                }

                teamAndSelf = teamAndSelf.Where(x => x.Id != getTheBall.Id).ToArray();

            }


            // finally we go after the other players 
            foreach (var (baddie, _) in gameState.players.Values
               .Where(x => !teammates.Contains(x.Id) && x.Id != self && (!gameState.GameBall.OwnerOrNull.HasValue || x.Id != gameState.players[gameState.GameBall.OwnerOrNull.Value].Id))
               .Select(x => (x, x.PlayerBody.Position.NewAdded(GoalTheyScoreOn().NewMinus()).Length))
               .OrderByDescending(x => x.Length))
            {
                var getTheBaddie = teamAndSelf
                    .Select(pair => (pair.Id, pair.PlayerBody.Position.NewAdded(baddie.PlayerBody.Position.NewMinus()).Length))
                    .OrderBy(pair => pair.Length)
                    .First();
                if (getTheBaddie.Id == self)
                {
                    return Towards(myPosition, baddie.PlayerBody.Position, 4);
                }

                teamAndSelf = teamAndSelf.Where(x => x.Id != getTheBaddie.Id).ToArray();
            }
            return 0;
        }

        private double GlobalEvaluate_TheirBall(Vector myPosition)
        {
            var res = GlobalEvaluate_TheirBall_Role(myPosition);
            // the other team has the ball

            // go towards your goal
            res += Towards(myPosition, GoalTheyScoreOn(), 1);
            // but don't go in it
            res -= TowardsWithIn(myPosition, GoalTheyScoreOn(), 10, Constants.footLen * 2);

            // go towards the ball
            res += Towards(myPosition, gameState.GameBall.Posistion, 1);
            // go towards the ball hard if you are close
            res += TowardsWithIn(myPosition, gameState.GameBall.Posistion, 7, Constants.footLen * 4);

            //var currentVelocity = gameState.players[self].PlayerBody.Velocity;
            //if (currentVelocity.Length > 0)
            //{
            //    res += myPosition.NewAdded(gameState.players[self].PlayerBody.Position.NewMinus()).Dot(currentVelocity.NewUnitized()) * Constants.footLen / 1000.0;
            //}

            //var goalieForce = Goalie(myPosition, 4);
            //var hasTask = goalieForce > 0;
            //res += goalieForce;

            //if (!hasTask)
            //{
            //    // go towards players of the other team
            //    // when you are near them
            //    foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
            //    {
            //        var force = TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 5, Constants.footLen * 3);
            //        hasTask |= force != 0;
            //        res += force;
            //    }
            //}

            //// go towards players on the other team if you are not near someone and no one on your team is near them
            //if (!hasTask)
            //{
            //    foreach (var player in gameState.players
            //        .Where(x => !teammates.Contains(x.Key) && x.Key != self)
            //        .Where(x=> !gameState.players
            //            .Where(x => teammates.Contains(x.Key))
            //            .Where(y=> y.Value.PlayerBody.Position.NewAdded(x.Value.PlayerBody.Position.NewMinus()).Length < Constants.footLen * 3)
            //            .Any()))
            //    {
            //        var force = Towards(myPosition, player.Value.PlayerBody.Position, 100);
            //    }
            //}

            // try and be between them and the goal
            //res += PassIsBlockedBy(GoalTheyScoreOn(), myPosition) * Constants.footLen * 2;

            // i don't think I need this
            // the force towards the ball will pull them to the ball side of their target
            // try to be between the ball and the other team
            //foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
            //{
            //    res += PassIsBlockedBy(GoalTheyScoreOn(), myPosition) * Constants.footLen;
            //}

            return res;
        }

        private double GlobalEvaluate_OurTeamsBall(Vector myPosition)
        {
            var res = 0.0;
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

            return res;
        }

        private double GlobalEvaluate_UpForGrabs(Vector myPosition)
        {
            var res = 0.0;

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
        Func<GameState,Vector> target = _=> new Vector(0,0);
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
            if (r.NextDouble() < (1 / 10.0))
            {
                target = GenerateDirection();
            }

            var concreteTarget = target(gameState);
            if (concreteTarget.Length > 1) {
                concreteTarget = concreteTarget.NewUnitized();
            }
            inputs.BodyX = concreteTarget.x;
            inputs.BodyY = concreteTarget.y;

            if (r.NextDouble() < (1 / 1.0))
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

        private Lazy<Vector[]> throwOffsets = new Lazy<Vector[]>(() => {
            var r = new Random();

            // this isn't a good random for a circle. it perfers pie/4 to pie/2
            return new int[100].Select(_ => new Vector((1 - (2 * r.NextDouble())), (1 - (2 * r.NextDouble()))).NewUnitized().NewScaled(Constants.footLen * 10 * r.NextDouble())).ToArray();
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

            // I think we could probably search this space much more efficently 
            var options = throwOffsets.Value
                .Select(pos => pos.NewAdded(myBody))
                .Where(pos => pos.x > 0&& pos.x <fieldDimensions.xMax && pos.y < 0 && pos.y > fieldDimensions.yMax)
                // always include the goal
                .Union(goalOffsets.Value.Select(x => GoalWeScoreOn().NewAdded(x))) 
                // always include your team
                .Union(gameState.players
                   .Where(x => teammates.Contains(x.Key))
                   .Select(x => x.Value.PlayerFoot.Position))
                .Where(pos => CanPass(pos, gameState.players.Where(x=>x.Key != self).Select(x=>x.Value.PlayerBody.Position).Union(new Vector[] { GoalTheyScoreOn()}).ToArray()))
                .Select(pos => (
                    direction: Scale(pos, gameState.players[self].PlayerFoot.Position),
                    positionValue: EvaluatePassToSpace(pos,false)))
                .OrderByDescending(x => x.positionValue)
                .ToArray();

            if (options.Any() && options[0].positionValue > EvaluatePassToSpace(myBody,true) + 2 * Constants.footLen)
            {
                direction = options[0].direction;
                return true;
            }

            direction = default;
            return false;
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

        private double EvaluatePassToSpace(Vector position, bool evaluateSelf)
        {
            var res = 0.0;

            // don't go near the other team
            foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
            {
                res -= TowardsWithIn(position, player.Value.PlayerBody.Position, 1, Constants.footLen * 5);
                res -= TowardsWithIn(position, player.Value.PlayerBody.Position, 3, Constants.footLen * 2);
            }
            
            if (!evaluateSelf)
            {
                // do go near your team
                foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key) || x.Key == self))
                {
                    res += TowardsWithIn(position, player.Value.PlayerBody.Position, 1, Constants.footLen * 5);
                    res += TowardsWithIn(position, player.Value.PlayerBody.Position, 3, Constants.footLen * 2);
                }
            }

            // shoot at the goal
            res += TowardsWithIn(position, GoalWeScoreOn(), 10, Constants.goalLen );

            // go to the goal
            res += Towards(position, GoalWeScoreOn(), 2);

            // go away from our goal
            res -= TowardsWithIn(position, GoalTheyScoreOn(), 1, Constants.footLen * 10);
            res -= TowardsWithIn(position, GoalTheyScoreOn(), 10, Constants.footLen * 4);

            if (!evaluateSelf)
            {
                var myPosition = gameState.players[self].PlayerBody.Position;

                // bad to be too close to us
                res -= TowardsWithIn(position, myPosition, 5, Constants.footLen * 4);
                res -= TowardsWithOut(position, myPosition, 1, Constants.footLen * 12);
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

        private Func<GameState,Vector> GenerateDirection()
        {
            var myPosition = gameState.players[self].PlayerBody.Position;

            var r = new Random();


            var getOtherPlayers = gameState.players
                .Where(x => !teammates.Contains(x.Key) && x.Key != self)
                .Select(x => (Func<GameState, Vector>)((GameState gs) => gs.players[x.Key].PlayerBody.Position.NewAdded(gs.players[self].PlayerBody.Position.NewMinus())));

            var getBall = new[] {
                (Func<GameState, Vector>)((GameState gs) => gs.GameBall.Posistion.NewAdded(gs.players[self].PlayerBody.Position.NewMinus()))
            };

            var getGoalie = new[] {
                (Func<GameState, Vector>)((GameState gs) => gs.GameBall.Posistion.NewAdded(GoalTheyScoreOn()).NewScaled(.5).NewAdded(gs.players[self].PlayerBody.Position.NewMinus()))
            };

            var random = new int[100]
                .Select(_ => new Vector((1 - (2 * r.NextDouble())), (1 - (2 * r.NextDouble()))).NewUnitized().NewScaled(Constants.goalLen * r.NextDouble()))
                .Select(vec => (Func<GameState, Vector>)((GameState _) => vec));

            var best = random
                .Union(getBall)
                .Union(getOtherPlayers)
                .Union(getGoalie)
                .Select(generator => {
                    var dir = generator(gameState);
                    if (dir.Length > Constants.goalLen)
                    {
                        dir = dir.NewUnitized().NewScaled(Constants.goalLen);
                    }
                    var prospect = myPosition.NewAdded(dir);
                    
                    return (generator, pos: prospect); })
                .Where(pair => pair.pos.x < fieldDimensions.xMax && pair.pos.x > 0 && pair.pos.y < fieldDimensions.yMax && pair.pos.y > 0)
                .Select(pos => (generator: pos.generator, score: GlobalEvaluate(pos.pos)))
                .OrderByDescending(pair => pair.score)
                .First();
            
            return best.generator;
        }


        private Lazy<Vector[]> footOffsets = new Lazy<Vector[]>(() => {
            var r = new Random();

            // this isn't a good random for a circle. it perfers pie/4 to pie/2
            return new int[25].Select(_ => new Vector((1 - (2 * r.NextDouble())), (1 - (2 * r.NextDouble()))).NewUnitized().NewScaled(Constants.footLen*1.5 * r.NextDouble())).ToArray();
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

                var lastHadBall = gameState.players.OrderByDescending(x => x.Value.LastHadBall).First();
                var framesPassed = gameState.Frame - lastHadBall.Value.LastHadBall;
                if (lastHadBall.Value.LastHadBall != 0 && framesPassed < 30)
                {
                   // res += GlobalEvaluate_UpForGrabs(myPosition) * (30 - framesPassed) / 30.0;
                    if (teammates.Contains(lastHadBall.Key) || lastHadBall.Key == self)
                    {
                        res += GlobalEvaluate_OurTeamsBall(myPosition);// * (framesPassed) / 30.0;
                    }
                    else
                    {
                        res += GlobalEvaluate_TheirBall(myPosition);// * (framesPassed) / 30.0;
                    }
                }
                else
                {
                    res += GlobalEvaluate_UpForGrabs(myPosition);
                }
            }
            else if (teammates.Contains((Guid)gameState.GameBall.OwnerOrNull)) // one of you teammates has the ball
            {
                res += GlobalEvaluate_OurTeamsBall(myPosition);
            }
            else
            {
                res += GlobalEvaluate_TheirBall(myPosition);
            }

            // this is mostly just annoying
            // stay away from edges
            //res += TowardsXWithIn(myPosition, new Vector(0, 0), -1, Constants.footLen));
            //res += TowardsXWithIn(myPosition, new Vector(fieldDimensions.xMax, 0), -1, Constants.footLen));

            //res += TowardsYWithIn(myPosition, new Vector(0, 0), -1, Constants.footLen));
            //res += TowardsYWithIn(myPosition, new Vector(0, fieldDimensions.yMax), -1, Constants.footLen));

            return res;
        }


        private double GlobalEvaluate_TheirBall_Role(Vector myPosition) {
            var teamAndSelf = gameState.players.Values
                    .Where(x => teammates.Contains(x.Id))
                    .Union(new[] { gameState.players[self] }).ToArray();

            // most important is to have a goalie
            var goalie = teamAndSelf
                .Select(pair => (pair.Id, pair.PlayerBody.Position.NewAdded(GoalTheyScoreOn().NewMinus()).Length))
                .OrderBy(pair => pair.Length)
                .First();

            if (goalie.Id == self)
            {
                return Towards(myPosition, gameState.GameBall.Posistion.NewAdded(GoalTheyScoreOn()).NewScaled(.5), 4);
            }

            teamAndSelf = teamAndSelf.Where(x => x.Id != goalie.Id).ToArray();

            if (gameState.GameBall.OwnerOrNull.HasValue)
            {
                // next we need someone to go after the guy with the ball
                var getTheBall = teamAndSelf
                    .Select(pair => (pair.Id, pair.PlayerBody.Position.NewAdded(gameState.players[gameState.GameBall.OwnerOrNull.Value].PlayerBody.Position.NewMinus()).Length))
                    .OrderBy(pair => pair.Length)
                    .First();

                if (getTheBall.Id == self)
                {
                    return Towards(myPosition, gameState.players[gameState.GameBall.OwnerOrNull.Value].PlayerBody.Position, 4);
                }
    
                teamAndSelf = teamAndSelf.Where(x => x.Id != getTheBall.Id).ToArray();

            }


            // finally we go after the other players 
            foreach (var (baddie, _) in gameState.players.Values
               .Where(x => !teammates.Contains(x.Id) && x.Id != self && (!gameState.GameBall.OwnerOrNull.HasValue || x.Id != gameState.players[gameState.GameBall.OwnerOrNull.Value].Id))
               .Select(x => (x, x.PlayerBody.Position.NewAdded(GoalTheyScoreOn().NewMinus()).Length))
               .OrderByDescending(x => x.Length))
            {
                var getTheBaddie = teamAndSelf
                    .Select(pair => (pair.Id, pair.PlayerBody.Position.NewAdded(baddie.PlayerBody.Position.NewMinus()).Length))
                    .OrderBy(pair => pair.Length)
                    .First();
                if (getTheBaddie.Id == self)
                {
                    return Towards(myPosition, baddie.PlayerBody.Position, 4);
                }

                teamAndSelf = teamAndSelf.Where(x => x.Id != getTheBaddie.Id).ToArray();
            }
            return 0;
        }

        private double GlobalEvaluate_TheirBall(Vector myPosition)
        {
            var res = GlobalEvaluate_TheirBall_Role(myPosition);
            // the other team has the ball

            // go towards your goal
            res += Towards(myPosition, GoalTheyScoreOn(), 1);
            // but don't go in it
            res -= TowardsWithIn(myPosition, GoalTheyScoreOn(), 10, Constants.footLen * 2);

            // go towards the ball
            res += Towards(myPosition, gameState.GameBall.Posistion, 1);
            // go towards the ball hard if you are close
            res += TowardsWithIn(myPosition, gameState.GameBall.Posistion, 5, Constants.footLen * 4);


            var currentVelocity = gameState.players[self].PlayerBody.Velocity;
            if (currentVelocity.Length > 0)
            {
                res += myPosition.NewAdded(gameState.players[self].PlayerBody.Position.NewMinus()).Dot(currentVelocity.NewUnitized()) * Constants.footLen / 1000.0;
            }

            //var goalieForce = Goalie(myPosition, 4);
            //var hasTask = goalieForce > 0;
            //res += goalieForce;

            //if (!hasTask)
            //{
            //    // go towards players of the other team
            //    // when you are near them
            //    foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
            //    {
            //        var force = TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 5, Constants.footLen * 3);
            //        hasTask |= force != 0;
            //        res += force;
            //    }
            //}

            //// go towards players on the other team if you are not near someone and no one on your team is near them
            //if (!hasTask)
            //{
            //    foreach (var player in gameState.players
            //        .Where(x => !teammates.Contains(x.Key) && x.Key != self)
            //        .Where(x=> !gameState.players
            //            .Where(x => teammates.Contains(x.Key))
            //            .Where(y=> y.Value.PlayerBody.Position.NewAdded(x.Value.PlayerBody.Position.NewMinus()).Length < Constants.footLen * 3)
            //            .Any()))
            //    {
            //        var force = Towards(myPosition, player.Value.PlayerBody.Position, 100);
            //    }
            //}

            // try and be between them and the goal
            //res += PassIsBlockedBy(GoalTheyScoreOn(), myPosition) * Constants.footLen * 2;

            // i don't think I need this
            // the force towards the ball will pull them to the ball side of their target
            // try to be between the ball and the other team
            //foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
            //{
            //    res += PassIsBlockedBy(GoalTheyScoreOn(), myPosition) * Constants.footLen;
            //}


            return res;
        }

        private double GlobalEvaluate_OurTeamsBall(Vector myPosition)
        {
            var res = 0.0;
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

            return res;
        }

        private double GlobalEvaluate_UpForGrabs(Vector myPosition)
        {
            var res = 0.0;

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
