using Common;
using Physics2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteSoccer
{

    class AITeam
    {

        const double Unit = 5000;
        private const int throwCountdown = 10;
        private static Random r = new Random();
        private readonly IReadOnlyDictionary<Guid, AITeamMember> team;
        private readonly GameState gameState;
        private readonly FieldDimensions fieldDimensions;
        private readonly bool leftGoal;
        private int updating = 0;

        public IEnumerable<(Guid, IInputs)> GetPlayers()
        {
            return team.Select(x => (x.Key, (IInputs)x.Value));
        }

        public AITeam(GameState gameState, Guid[] teammates, FieldDimensions fieldDimensions, bool leftGoal)
        {
            team = teammates.ToDictionary(x => x, x => new AITeamMember(this, x));
            this.gameState = gameState;
            this.fieldDimensions = fieldDimensions;
            this.leftGoal = leftGoal;

        }

        private class AITeamMember : IInputs
        {
            private readonly AITeam aITeam;
            public PlayerInputs inputs;

            public AITeamMember(AITeam aITeam, Guid id)
            {
                this.aITeam = aITeam;
                inputs = new PlayerInputs(0, 0, 0, 0, id, ControlScheme.AI, false, false);
            }

            public Task Init() => Task.CompletedTask;

            public Task<PlayerInputs> Next()
            {
                aITeam.RequestUpdate();
                return Task.FromResult(inputs);
            }
        }

        private void RequestUpdate()
        {
            if (Interlocked.CompareExchange(ref updating, 1, 0) == 0)
            {
                Task.Run(() =>
                {
                    try
                    {
                        Update();
                    }
                    catch (Exception e)
                    {
                    }
                    finally
                    {
                        updating = 0;
                    }
                });
            }
        }
        Guid lastKnowBallCarrier = Guid.NewGuid();
        enum WhosBall
        {
            OurBall,
            TheirBall,
        }
        private WhosBall whosBall;
        private void Update()
        {
            if (gameState.GameBall.OwnerOrNull != lastKnowBallCarrier)
            {
                foreach (var member in team)
                {
                    member.Value.inputs.Throwing = false;
                }
                lastKnowBallCarrier = gameState.GameBall.OwnerOrNull.GetValueOrDefault(Guid.NewGuid());
            }

            if (gameState.CountDownState.Countdown)// no one has the ball
            {
                Defense();
            }
            else if (gameState.GameBall.OwnerOrNull is Guid owner)
            {
                if (team.TryGetValue(owner, out var hasBall)) // we have the ball
                {
                    whosBall = WhosBall.OurBall;
                    var toAssign = team.Where(x => x.Key != owner).ToList();

                    {

                        var hasBallGenerators = GetPositionGenerators(owner)
                            .Select(pos => (generator: pos.generator, score: HasBallEvaluator(pos.pos)))
                            .OrderByDescending(pair => pair.score)
                            .ToList();

                        if (hasBallGenerators.Any())
                        {
                            UpdateDirection(hasBall, hasBallGenerators.First().generator);
                        }
                        else
                        {
                            UpdateDirection(hasBall, (GameState gs) => new Vector(0.0, 0.0));
                        }

                        var myBody = gameState.players[owner].PlayerFoot.Position;


                        // if you have a good shot on goal take it
                        var shotsOnGoal = goalOffsets.Value
                            .Select(x => GoalWeScoreOn().NewAdded(x))
                            .Select(x =>
                            {
                                var diff = x.NewAdded(myBody.NewMinus());
                                var howLongToScore = diff.Length / Constants.maxThrowPower;
                                var proposedThrow = diff.NewUnitized().NewScaled(Constants.maxThrowPower);
                                return (howLongToScore, proposedThrow);

                            })
                            .Where(pair => gameState.players.Values
                                .Where(x=> x.PlayerFoot.Position.NewAdded(gameState.GameBall.Posistion.NewMinus()).Length > Unit * 1 )
                                .All(x => pair.howLongToScore < PlayerInputApplyer.IntersectBallTime(x.PlayerBody.Position, gameState.GameBall.Posistion, pair.proposedThrow)))
                            .OrderBy(x => x.howLongToScore)
                            .Select(x => x.proposedThrow)
                            .ToArray();

                        if (shotsOnGoal.Any())
                        {
                            if (!hasBall.inputs.Throwing)
                            {
                                startedThrowing = gameState.Frame;
                                hasBall.inputs.Throwing = true;
                            }

                            var foot = shotsOnGoal.First().NewUnitized();
                            hasBall.inputs.FootX = foot.x;
                            hasBall.inputs.FootY = foot.y;
                            goto next;
                        }


                        // think about throwing near you teammates

                        var passes = toAssign
                            .SelectMany(x => throwOffsets.Value
                                .Select(pos =>
                                {
                                    var playerPos = gameState.players[x.Key].PlayerFoot.Position;
                                    // TODO consider which way your teammate is running
                                    var target = pos.NewAdded(playerPos);
                                    var diff = target.NewAdded(myBody.NewMinus());
                                    var proposedThrow = diff.NewUnitized().NewScaled(Constants.maxThrowPower);
                                    var howLongToCatch = PlayerInputApplyer.IntersectBallTime(playerPos, gameState.GameBall.Posistion, proposedThrow);
                                    return (howLongToCatch, proposedThrow, x.Key);
                                }))
                            .Where(pair => 
                                gameState.players
                                .Where(x=> x.Key != pair.Key && x.Value.PlayerFoot.Position.NewAdded(gameState.GameBall.Posistion.NewMinus()).Length > Unit * 1 )
                                .All(x => pair.howLongToCatch < PlayerInputApplyer.IntersectBallTime(x.Value.PlayerBody.Position, gameState.GameBall.Posistion, pair.proposedThrow)))
                            .Select(x => (x.proposedThrow, value: EvaluatePass(x.proposedThrow.NewScaled(x.howLongToCatch).NewAdded(gameState.GameBall.Posistion))))
                            .OrderByDescending(x => x.value)
                            .ToArray();

                        if (passes.Any() && passes.First().value > EvaluatePass(gameState.GameBall.Posistion))
                        {
                            if (!hasBall.inputs.Throwing)
                            {
                                startedThrowing = gameState.Frame;
                                hasBall.inputs.Throwing = true;
                            }
                            var foot = passes.First().proposedThrow.NewUnitized();
                            hasBall.inputs.FootX = foot.x;
                            hasBall.inputs.FootY = foot.y;
                            goto next;
                        }
                    }
                next:
                    if (startedThrowing + throwCountdown < gameState.Frame)
                    {
                        hasBall.inputs.Throwing = false;
                    };
                    OffenseNotBall(toAssign);
                }
                else  // they have the ball
                {
                    whosBall = WhosBall.TheirBall;
                    Defense();
                }
            }
            else if (whosBall == WhosBall.OurBall){
                // figure out who will get to the ball first, they run at it
                var hot =team.Select(x =>
                {
                    var playerPos = gameState.players[x.Key].PlayerFoot.Position;
                    // TODO consider which way your teammate is running
                    var howLongToCatch = PlayerInputApplyer.IntersectBallTime(playerPos, gameState.GameBall.Posistion, gameState.GameBall.Velocity);
                    return (x, howLongToCatch);
                }).OrderBy(x => x.howLongToCatch)
                .First().x;

                UpdateDirection(hot.Value, (GameState gs) => PlayerInputApplyer.IntersectBallDirection(gs.players[hot.Key].PlayerBody.Position, gs.GameBall.Posistion, gs.GameBall.Velocity));

                OffenseNotBall(team.Where(x=>x.Key != hot.Key).ToList());
            }
            else if (whosBall == WhosBall.TheirBall)
            {
                Defense();
            }

            foreach (var member in team)
            {
                if (!member.Value.inputs.Throwing)
                {
                    var move = GenerateDirectionFoot(gameState.players[member.Key].PlayerBody.Position, member.Key);

                    if (move.Length > 0) {
                        move = move.NewUnitized().NewScaled(1000);
                    }

                    member.Value.inputs.FootX = move.x;
                    member.Value.inputs.FootY = move.y;

                    //if ( move.Length > 100)
                    //{
                    //    member.Value.inputs.Boost = true;
                    //}
                }
            }
        }

        private static Vector RandomVector() {
            var rads = r.NextDouble() * Math.PI * 2;
            return new Vector(Math.Sin(rads), Math.Cos(rads));
        }

        // why are these lazy?! they are always going to be initialzed
        private Lazy<Vector[]> footOffsets = new Lazy<Vector[]>(() =>
        {
            return new int[25]
            .Select(_ => RandomVector().NewScaled(Unit * 6 * r.NextDouble()))
            .Union(new[] { new Vector(0,0)})
            .ToArray();
        });

        private Vector GenerateDirectionFoot(Vector myPosition,Guid self)
        {

            var list = footOffsets.Value
                .Select(x => myPosition.NewAdded(x))
                .Union(new[] { myPosition })
                .Where(pos => pos.x < fieldDimensions.xMax && pos.x > 0 && pos.y < fieldDimensions.yMax && pos.y > 0)
                .Select(pos => (position: pos, score: GlobalEvaluateFoot(pos, myPosition, self)))
                .OrderByDescending(pair => pair.score)
                .ToArray();

            if (list.Any())
            {

                var direction = list.First().position.NewAdded(myPosition.NewMinus());

                return direction;
            }

            return new Vector(0, 0);

        }


        public double GlobalEvaluateFoot( Vector myPosition, Vector myBody, Guid self)
        {
            var res = 0.0;

            var snapshot = gameState.GameBall.OwnerOrNull;

            if (snapshot == self) // when you have the ball
            {
                // don't go near the other team
                foreach (var player in gameState.players.Where(x => !team.ContainsKey(x.Key) ))
                {
                    res -= TowardsWithInBody(myBody, myPosition, player.Value.PlayerFoot.Position, 4, Unit * 1.5);
                }

                // go to the goal
                res += TowardsWithInBody(myBody, myPosition, GoalWeScoreOn(), 1, PlayerInputApplyer.HowFarCanIBoost(gameState.players[self].Boosts) - Unit);
            }
            else if (!(snapshot is Guid owner))// when no one has the ball
            {
                // go towards the ball when it is in play
                if (!gameState.CountDownState.Countdown)
                {
                    res += TowardsWithInBody(myBody, myPosition, gameState.GameBall.Posistion, 10, PlayerInputApplyer.HowFarCanIBoost(gameState.players[self].Boosts) - Unit);
                }
            }
            else if (team.ContainsKey(owner)) // one of you teammates has the ball
            {

                // stay away from your teammates
                //foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key)))
                //{
                //    res -= TowardsWithIn(myPosition, player.Value.PlayerFoot.Position, 1, Unit * .5);
                //}

                //// bop the other team
                //foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                //{
                //    res += TowardsWithInBody(myBody, myPosition, player.Value.PlayerFoot.Position, 2, Unit * 6);
                //}
            }
            else // the other team has the ball
            {

                // stay away from your teammates
                //foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key)))
                //{
                //    res -= TowardsWithIn(myPosition, player.Value.PlayerFoot.Position, 1, Unit * .5);
                //}

                // go towards the ball hard if you are close
                res += TowardsWithInBody(myBody, myPosition, gameState.GameBall.Posistion, 10, PlayerInputApplyer.HowFarCanIBoost(gameState.players[self].Boosts) - Unit);

                // go towards players of the other team
                //foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                //{
                //    res += TowardsWithInBody(myBody, myPosition, player.Value.PlayerFoot.Position, 1, Unit * 6);
                //}
            }

            // a small force back towards the center
            res += Towards(myPosition, gameState.players[self].PlayerBody.Position, .1);


            //// feet don't like to stay still while extended
            //if (gameState.players[self].PlayerFoot.Position.NewAdded(gameState.players[self].PlayerBody.Position.NewMinus()).Length > Unit / 2.0)
            //{
            //    res -= TowardsWithIn(myPosition, gameState.players[self].PlayerFoot.Position, .5, Unit / 3.0);
            //}

            // this is mostly just annoying
            // stay away from edges
            //res += TowardsXWithIn(myPosition, new Vector(0, 0), -1, Unit));
            //res += TowardsXWithIn(myPosition, new Vector(fieldDimensions.xMax, 0), -1, Unit));

            //res += TowardsYWithIn(myPosition, new Vector(0, 0), -1, Unit));
            //res += TowardsYWithIn(myPosition, new Vector(0, fieldDimensions.yMax), -1, Unit));

            return res;
        }

        private void OffenseNotBall(List<KeyValuePair<Guid, AITeamMember>> toAssign)
        {
            foreach (var player in toAssign)
            {
                var myPlayer = player;
                var cutterGenerators = cutOffsets.Value
                    .Select<Vector, Func<GameState, Vector>>(x => _ => x)
                    .Select(generator => (generator: generator, score: CutEvaluator(gameState.players[ player.Key].PlayerBody.Position.NewAdded(generator(gameState)), player.Key)))
                    .OrderByDescending(pair => pair.score)
                    .ToList();

                if (cutterGenerators.Any())
                {
                    UpdateDirection(player.Value, cutterGenerators.First().generator);
                }
                else
                {
                    UpdateDirection(player.Value, (GameState gs) => new Vector(0.0, 0.0));
                }
            }
        }

        private void Defense()
        {
            var noneGoalie = Goalie(team.ToArray());
            var toAssign = GetTheBall(noneGoalie).ToList();

            {
                foreach (var (baddie, _) in gameState.players.Values
                  .Where(x => !team.ContainsKey(x.Id) && x.Id != gameState.GameBall.OwnerOrNull.GetValueOrDefault(Guid.NewGuid()))
                  .Select(x => (x, x.PlayerBody.Position.NewAdded(GoalTheyScoreOn().NewMinus()).Length))
                  .OrderBy(x => x.Length))
                {

                    var getTheBaddies = toAssign
                       .Select(pair => (pair, gameState.players[pair.Key].PlayerBody.Position.NewAdded(baddie.PlayerBody.Position.NewMinus()).Length))
                       .OrderBy(pair => pair.Length)
                       .ToList();

                    if (!getTheBaddies.Any())
                    {
                        continue;
                    }

                    var getTheBaddie = getTheBaddies.First();

                    toAssign.Remove(getTheBaddie.pair);

                    var guardGenerators = GetPositionGenerators(getTheBaddie.pair.Key)
                        .Select(pos => (generator: pos.generator, score: GuardPlayerEvaluator(baddie.Id)(pos.pos)))
                        .OrderByDescending(pair => pair.score)
                        .ToList();

                    if (guardGenerators.Any())
                    {
                        UpdateDirection(getTheBaddie.pair.Value, guardGenerators.First().generator);
                    }
                    else
                    {
                        UpdateDirection(getTheBaddie.pair.Value, (GameState gs) => new Vector(0.0, 0.0));
                    }
                }
            }
        }

        private KeyValuePair<Guid, AITeamMember>[] GetTheBall(KeyValuePair<Guid, AITeamMember>[] toAssign)
        {
            var getTheBall = toAssign
               .Select(pair => (pair, gameState.players[pair.Key].PlayerBody.Position.NewAdded(gameState.GameBall.Posistion.NewMinus()).Length))
               .OrderBy(pair => pair.Length)
               .First();

            var getTheBallGenerators = GetPositionGenerators(getTheBall.pair.Key)
                .Select(pos => (generator: pos.generator, score: GetTheBallEvaluator(pos.pos)))
                .OrderByDescending(pair => pair.score)
                .ToList();

            if (getTheBallGenerators.Any())
            {
                UpdateDirection(getTheBall.pair.Value, getTheBallGenerators.First().generator);
            }
            else
            {
                UpdateDirection(getTheBall.pair.Value, (GameState gs) => new Vector(0.0, 0.0));
            }

            return toAssign.Except(new[] { getTheBall.pair }).ToArray();
        }

        private KeyValuePair<Guid, AITeamMember>[] Goalie(KeyValuePair<Guid, AITeamMember>[] toAssign)
        {
            var goalie = toAssign
                .Select(pair => (pair, gameState.players[pair.Key].PlayerBody.Position.NewAdded(GoalTheyScoreOn().NewMinus()).Length))
                .OrderBy(pair => pair.Length)
                .First();


            var goalieGenerators = GetPositionGenerators(goalie.pair.Key)
                .Select(pos => (generator: pos.generator, score: GoalieEvaluator(pos.pos)))
                .OrderByDescending(pair => pair.score)
                .ToList();

            if (goalieGenerators.Any())
            {
                UpdateDirection(goalie.pair.Value, goalieGenerators.First().generator);
            }
            else
            {
                UpdateDirection(goalie.pair.Value, (GameState gs) => new Vector(0.0, 0.0));
            }


            return toAssign.Except(new[] { goalie.pair }).ToArray();

        }

        private double EvaluatePass(Vector position)
        {

            var res = 0.0;
            // go to the goal
            res += Towards(position, GoalWeScoreOn(), 4);
            // but calcel it out once you are close enough to shoot, we don't really care if you are close or really close
            res -= TowardsWithIn(position, GoalWeScoreOn(), 4, Unit * 4);

            // go away from our goal
            res -= TowardsWithIn(position, GoalTheyScoreOn(), 1, Unit * 12);
            res -= TowardsWithIn(position, GoalTheyScoreOn(), 10, Unit * 4);

            return res;
        }

        private void UpdateDirection(AITeamMember goalie, Func<GameState, Vector> generator)
        {
            var concreteTarget = generator(gameState);
            if (Double.IsNaN(concreteTarget.Length))
            {
                var aahhhh = 0;
            }

            if (concreteTarget.Length > 1)
            {
                concreteTarget = concreteTarget.NewUnitized();
            }
            goalie.inputs.BodyX = concreteTarget.x;
            goalie.inputs.BodyY = concreteTarget.y;
        }

        private List<(Func<GameState, Vector> generator, Vector pos)> GetPositionGenerators(Guid self)
        {
            var myPosition = gameState.players[self].PlayerBody.Position;

            var getOtherPlayers = gameState.players
                .Where(x => !team.ContainsKey(x.Key))
                .Select(x => (Func<GameState, Vector>)((GameState gs) => gs.players[x.Key].PlayerBody.Position.NewAdded(gs.players[self].PlayerBody.Position.NewMinus())));

            var getBall = new[] {
                (Func<GameState, Vector>)((GameState gs) => gs.GameBall.Posistion.NewAdded(gs.players[self].PlayerBody.Position.NewMinus()))
            };

            var getGoalie = new[] {
                (Func<GameState, Vector>)((GameState gs) => gs.GameBall.Posistion.NewAdded(GoalTheyScoreOn()).NewScaled(.5).NewAdded(gs.players[self].PlayerBody.Position.NewMinus()))
            };

            var random = new int[100]
                .Select(_ => RandomVector().NewScaled(Constants.goalLen * r.NextDouble()))
                .Select(vec => (Func<GameState, Vector>)((GameState _) => vec));

            return random
                .Union(getBall)
                .Union(getOtherPlayers)
                .Union(getGoalie)
                .Select(generator =>
                {
                    var dir = generator(gameState);
                    if (dir.Length > Constants.goalLen)
                    {
                        dir = dir.NewUnitized().NewScaled(Constants.goalLen);
                    }
                    var prospect = myPosition.NewAdded(dir);

                    return (generator, pos: prospect);
                })
                .Where(pair => pair.pos.x < fieldDimensions.xMax && pair.pos.x > 0 && pair.pos.y < fieldDimensions.yMax && pair.pos.y > 0)
                .ToList();
        }

        private double GoalieEvaluator(Vector position)
        {
            // TODO 
            // if the ball is closest to you go ahead and grab it
            return Towards(position, gameState.GameBall.Posistion.NewAdded(GoalTheyScoreOn()).NewScaled(.5), 4);
        }


        private double HasBallEvaluator(Vector position)
        {
            var res = 0.0;
            // don't go near the other team
            foreach (var player in gameState.players.Where(x => !team.ContainsKey(x.Key)))
            {
                res -= TowardsWithIn(position, player.Value.PlayerBody.Position, 4, Unit * 4);
            }

            // go to the goal
            res += Towards(position, GoalWeScoreOn(), .5);
            res += TowardsWithIn(position, GoalTheyScoreOn(), 10, Constants.goalLen);
            // dont go in your own goal
            res -= Towards(position, GoalTheyScoreOn(), .5);
            res -= TowardsWithIn(position, GoalTheyScoreOn(), 6, Constants.goalLen + Unit);

            return res;
        }

        private double CutEvaluator(Vector myPosition, Guid self)
        {
            var res = 0.0;
            // go towards the goal
            res += Towards(myPosition, GoalWeScoreOn(), 1);

            // stay away from your teammates
            foreach (var player in gameState.players.Where(x => team.ContainsKey(x.Key) && x.Key != self))
            {
                res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 3, Unit * 6);
            }

            // don't get too close to the other teams players
            foreach (var player in gameState.players.Where(x => !team.ContainsKey(x.Key)))
            {
                res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 1, Unit * 3);
                res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 3, Unit * 1);
            }

            // don't hang out where they can't pass to you
            // PassIsBlockedBy has werid units thus the Unit
            //res -= PassIsBlockedBy(myPosition) * Unit * 6;

            // don't get too far from the ball
            res -= TowardsWithOut(myPosition, gameState.GameBall.Posistion, 3, Unit * 12);

            // we like to go the way we are going
            var currentVelocity = gameState.players[self].PlayerBody.Velocity;
            if (currentVelocity.Length > 0)
            {
                res += myPosition.NewAdded(gameState.players[self].PlayerBody.Position.NewMinus()).Dot(currentVelocity.NewUnitized()) * Unit / 1000.0;
            }

            return res;
        }

        private Lazy<Vector[]> throwOffsets = new Lazy<Vector[]>(() =>
        {
            // this isn't a good random for a circle. it perfers pie/4 to pie/2
            return new int[500].Select(_ => RandomVector().NewScaled(Unit * 4 * r.NextDouble())).ToArray();
        });

        private Lazy<Vector[]> cutOffsets = new Lazy<Vector[]>(() =>
        {
            // this isn't a good random for a circle. it perfers pie/4 to pie/2
            return new int[25].Select(_ => RandomVector().NewScaled(Unit * 3 * r.NextDouble())).ToArray();
        });


        private Lazy<Vector[]> goalOffsets = new Lazy<Vector[]>(() =>
        {
            // this isn't a good random for a circle. it perfers pie/4 to pie/2
            return new int[10].Select(_ => RandomVector().NewScaled(Constants.goalLen)).ToArray();
        });
        private int startedThrowing;

        private bool CanPass(Vector target, Vector[] obsticals)
        {
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


        private double GetTheBallEvaluator(Vector position)
        {
            return Towards(position, gameState.GameBall.Posistion, 4);
        }

        private Func<Vector, double> GuardPlayerEvaluator(Guid playerId) => (Vector position) =>
        {
            if (gameState.players.TryGetValue(playerId, out var player))
            {
                return Towards(position, player.PlayerBody.Position, 4);
            }
            return 0;
        };


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
            if (len < whenWithIn)
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

    }

    // this should really be in Common
    class AIInputs : IInputs
    {
        const double Unit = 5000;

        private static Random r = new Random();
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
        Func<GameState, Vector> target = _ => new Vector(0, 0);
        int throwing = 0;
        public Task<PlayerInputs> Next()
        {
            var inputs = new PlayerInputs(0, 0, 0, 0, self, ControlScheme.AI, false, false);

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
            var move = lastDirectionFoot.NewAdded(currentOffset.NewMinus()).NewScaled(1 / 10.0);

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

            if (!inputs.Throwing && move.Length > 100)
            {
                inputs.Boost = true;
            }

            return Task.FromResult(inputs);
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

        private Lazy<Vector[]> goalOffsets = new Lazy<Vector[]>(() =>
        {
            // this isn't a good random for a circle. it perfers pie/4 to pie/2
            return new int[10].Select(_ => new Vector((1 - (2 * r.NextDouble())), (1 - (2 * r.NextDouble()))).NewUnitized().NewScaled(Constants.goalLen * r.NextDouble())).ToArray();
        });

        private Lazy<Vector[]> throwOffsets = new Lazy<Vector[]>(() =>
        {
            // this isn't a good random for a circle. it perfers pie/4 to pie/2
            return new int[100].Select(_ => new Vector((1 - (2 * r.NextDouble())), (1 - (2 * r.NextDouble()))).NewUnitized().NewScaled(Unit * 10 * r.NextDouble())).ToArray();
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
            //    if (myBody.NewAdded(player.Value.PlayerBody.Position.NewMinus()).Length < Unit)
            //    {
            //        direction = default;
            //        return false;
            //    }
            //}

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
                .Where(pos => CanPass(pos, gameState.players
                    .Where(x => !teammates.Contains(x.Key) && x.Key != self)
                    .Select(x => x.Value.PlayerBody.Position)
                    .Union(new Vector[] { GoalTheyScoreOn() })
                    .ToArray()))
                .Select(pos => EvaluatePassToSpace(pos, false))
                .OrderByDescending(x => x.score)
                .ToArray();

            if (options.Any() && options[0].score > EvaluatePassToSpace(myBody, true).score + (2 * Unit))
            {
                direction = options[0].input;
                return true;
            }

            direction = default;
            return false;
        }

        //private static Vector Scale(Vector target, Vector myPosition)
        //{
        //    var direction = target.NewAdded(myPosition.NewMinus());
        //    if (direction.Length > 60000)
        //    {
        //        return direction.NewUnitized();
        //    }
        //    else
        //    {
        //        return direction.NewUnitized().NewScaled(Math.Sqrt(direction.Length / 60000.0));
        //    }
        //}

        private (double score, Vector input) EvaluatePassToSpace(Vector position, bool evaluateSelf)
        {

            var res = 0.0;

            var diff = position.NewAdded(gameState.players[self].PlayerBody.Position.NewMinus());



            // shoot at the goal
            var goalAdd = TowardsWithIn(position, GoalWeScoreOn(), 10, Constants.goalLen);
            res += goalAdd;

            // go to the goal
            res += Towards(position, GoalWeScoreOn(), 4);
            // but calcel it out once you are close enough to shoot, we don't really care if you are close or really close
            res -= TowardsWithIn(position, GoalWeScoreOn(), 4, Unit * 8);

            // go away from our goal
            res -= TowardsWithIn(position, GoalTheyScoreOn(), 1, Unit * 12);
            res -= TowardsWithIn(position, GoalTheyScoreOn(), 10, Unit * 4);

            if (goalAdd > 0)
            {
                if (diff.Length > 0)
                {
                    diff = diff.NewUnitized();
                }
                return (res, diff);
            }

            // it's bad to be near the other team
            foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
            {
                res -= TowardsWithIn(position, player.Value.PlayerBody.Position, 3, Unit * 5);
            }

            var ourClosestPlayers = gameState.players
            .Where(x => teammates.Contains(x.Value.Id))
            .Select(x => (player: x.Value, length: position.NewAdded(x.Value.PlayerBody.Position.NewMinus()).Length))
            .Select(x => (x.player, time: PlayerInputApplyer.HowQuicklyCanAPlayerMove(x.length)))
            .Select(x => (x.player, howHard: PlayerInputApplyer.HowHardToThrow(diff.Length, (int)x.time), x.time))
            .Where(x => x.howHard < Constants.maxThrowPower)
            .OrderBy(x => x.time)
            .ToList();

            var theyllGetThereAt = gameState.players
                .Where(x => !teammates.Contains(x.Value.Id)) //&& x.Value.Id != self we dont want to be too close
                .Select(x => (player: x.Value, length: position.NewAdded(x.Value.PlayerBody.Position.NewMinus()).Length))
                .Select(x => PlayerInputApplyer.HowQuicklyCanAPlayerMove(x.length))
                .Union(new double[] { 100 })
                .OrderBy(x => x)
                .FirstOrDefault();

            if (ourClosestPlayers.Any())
            {
                var closestPlayer = ourClosestPlayers.First();
                var lead = 8;
                if (closestPlayer.time + lead > theyllGetThereAt && !evaluateSelf)
                {
                    res -= Unit * (closestPlayer.time + lead - theyllGetThereAt) / 10.0;
                }

                // short throws are bad
                if (closestPlayer.howHard < Constants.maxThrowPower * .5)
                {
                    res -= Unit * 2;
                }

                if (diff.Length > 0)
                {
                    diff = diff.NewUnitized().NewScaled(closestPlayer.howHard / Constants.maxThrowPower);
                }

                return (res, diff);

            }
            else if (!evaluateSelf)
            {
                res -= Unit * 20;
            }

            if (diff.Length > 0)
            {
                diff = diff.NewUnitized();
            }

            return (res, diff.NewUnitized());
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
            if (obsticalDirection.Length == 0 || passDirection.Length < obsticalDirection.Length)
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

            var list = random
                .Union(getBall)
                .Union(getOtherPlayers)
                .Union(getGoalie)
                .Select(generator =>
                {
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
                .ToList();

            if (list.Any())
            {
                return list.First().generator;
            }


            return (GameState gs) => new Vector(0.0, 0.0);
        }


        private Lazy<Vector[]> footOffsets = new Lazy<Vector[]>(() =>
        {
            // this isn't a good random for a circle. it perfers pie/4 to pie/2
            return new int[25].Select(_ => new Vector((1 - (2 * r.NextDouble())), (1 - (2 * r.NextDouble()))).NewUnitized().NewScaled(Unit * 6 * r.NextDouble())).ToArray();
        });
        private Vector lastThrow;

        private Vector GenerateDirectionFoot()
        {
            var myPosition = gameState.players[self].PlayerBody.Position;


            var list = footOffsets.Value
                .Select(x => myPosition.NewAdded(x))
                .Union(new[] { myPosition })
                .Where(pos => pos.x < fieldDimensions.xMax && pos.x > 0 && pos.y < fieldDimensions.yMax && pos.y > 0)
                .Select(pos => (position: pos, score: GlobalEvaluateFoot(pos)))
                .OrderByDescending(pair => pair.score)
                .ToArray();

            if (list.Any())
            {

                var direction = list.First().position.NewAdded(myPosition.NewMinus());

                return direction;
            }

            return new Vector(0, 0);

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
                    res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 3, Unit * 2);
                    //res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 4, Unit * 3);
                    //res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 1, Unit * 5);
                }

                // go to the goal
                res += Towards(myPosition, GoalWeScoreOn(), .5);
            }
            else if (gameState.GameBall.OwnerOrNull == null)// when no one has the ball
            {

                var lastHadBall = gameState.players.OrderByDescending(x => x.Value.LastHadBall).First();
                var framesPassed = gameState.Frame - lastHadBall.Value.LastHadBall;
                if (lastHadBall.Value.LastHadBall != 0 && framesPassed < 30 && !gameState.CountDownState.Countdown)
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
            //res += TowardsXWithIn(myPosition, new Vector(0, 0), -1, Unit));
            //res += TowardsXWithIn(myPosition, new Vector(fieldDimensions.xMax, 0), -1, Unit));

            //res += TowardsYWithIn(myPosition, new Vector(0, 0), -1, Unit));
            //res += TowardsYWithIn(myPosition, new Vector(0, fieldDimensions.yMax), -1, Unit));

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
               .OrderBy(x => x.Length))
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

            // go towards your goal
            res += Towards(myPosition, GoalTheyScoreOn(), 1);
            // but don't go in it
            res -= TowardsWithIn(myPosition, GoalTheyScoreOn(), 10, Unit * 2);

            // go towards the ball
            res += Towards(myPosition, gameState.GameBall.Posistion, 1);
            // go towards the ball hard if you are close
            res += TowardsWithIn(myPosition, gameState.GameBall.Posistion, 5, Unit * 4);


            var currentVelocity = gameState.players[self].PlayerBody.Velocity;
            if (currentVelocity.Length > 0)
            {
                res += myPosition.NewAdded(gameState.players[self].PlayerBody.Position.NewMinus()).Dot(currentVelocity.NewUnitized()) * Unit / 1000.0;
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
            //        var force = TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 5, Unit * 3);
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
            //            .Where(y=> y.Value.PlayerBody.Position.NewAdded(x.Value.PlayerBody.Position.NewMinus()).Length < Unit * 3)
            //            .Any()))
            //    {
            //        var force = Towards(myPosition, player.Value.PlayerBody.Position, 100);
            //    }
            //}

            // try and be between them and the goal
            //res += PassIsBlockedBy(GoalTheyScoreOn(), myPosition) * Unit * 2;

            // i don't think I need this
            // the force towards the ball will pull them to the ball side of their target
            // try to be between the ball and the other team
            //foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
            //{
            //    res += PassIsBlockedBy(GoalTheyScoreOn(), myPosition) * Unit;
            //}


            return res;
        }

        private double GlobalEvaluate_OurTeamsBall(Vector myPosition)
        {
            var res = 0.0;
            // go towards the goal
            res += Towards(myPosition, GoalWeScoreOn(), 1);

            // stay away from your teammates
            foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key)))
            {
                res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 3, Unit * 8);
            }

            // don't get to close to the other teams players
            foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
            {
                res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 1, Unit * 8);
                res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 3, Unit * 3);
            }

            // don't hang out where they can't pass to you
            // PassIsBlockedBy has werid units thus the Unit
            res -= PassIsBlockedBy(myPosition) * Unit * 6;

            // don't get too far from the ball
            res -= TowardsWithOut(myPosition, gameState.GameBall.Posistion, 3, Unit * 12);

            // we like to go the way we are going
            var currentVelocity = gameState.players[self].PlayerBody.Velocity;
            if (currentVelocity.Length > 0)
            {
                res += myPosition.NewAdded(gameState.players[self].PlayerBody.Position.NewMinus()).Dot(currentVelocity.NewUnitized()) * Unit / 1000.0;
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
            res += TowardsWithIn(myPosition, gameState.GameBall.Posistion, 10, Unit * 5);

            // spread out ?
            foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key)))
            {
                res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 2, Unit * 12);
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
                    res -= TowardsWithInBody(myBody, myPosition, player.Value.PlayerFoot.Position, 4, Unit * 1.5);
                }

                // go to the goal
                res += TowardsWithInBody(myBody, myPosition, GoalWeScoreOn(), 1, PlayerInputApplyer.HowFarCanIBoost(gameState.players[self].Boosts) - Unit);
            }
            else if (gameState.GameBall.OwnerOrNull == null)// when no one has the ball
            {
                // go towards the ball when it is in play
                if (!gameState.CountDownState.Countdown)
                {
                    res += TowardsWithInBody(myBody, myPosition, gameState.GameBall.Posistion, 10, PlayerInputApplyer.HowFarCanIBoost(gameState.players[self].Boosts) - Unit);
                }
            }
            else if (teammates.Contains((Guid)gameState.GameBall.OwnerOrNull)) // one of you teammates has the ball
            {

                // stay away from your teammates
                //foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key)))
                //{
                //    res -= TowardsWithIn(myPosition, player.Value.PlayerFoot.Position, 1, Unit * .5);
                //}

                //// bop the other team
                //foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                //{
                //    res += TowardsWithInBody(myBody, myPosition, player.Value.PlayerFoot.Position, 2, Unit * 6);
                //}
            }
            else // the other team has the ball
            {

                // stay away from your teammates
                //foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key)))
                //{
                //    res -= TowardsWithIn(myPosition, player.Value.PlayerFoot.Position, 1, Unit * .5);
                //}

                // go towards the ball hard if you are close
                res += TowardsWithInBody(myBody, myPosition, gameState.GameBall.Posistion, 10, PlayerInputApplyer.HowFarCanIBoost(gameState.players[self].Boosts) - Unit);

                // go towards players of the other team
                //foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                //{
                //    res += TowardsWithInBody(myBody, myPosition, player.Value.PlayerFoot.Position, 1, Unit * 6);
                //}
            }

            // a small force back towards the center
            res += Towards(myPosition, gameState.players[self].PlayerBody.Position, .1);


            //// feet don't like to stay still while extended
            //if (gameState.players[self].PlayerFoot.Position.NewAdded(gameState.players[self].PlayerBody.Position.NewMinus()).Length > Unit / 2.0)
            //{
            //    res -= TowardsWithIn(myPosition, gameState.players[self].PlayerFoot.Position, .5, Unit / 3.0);
            //}

            // this is mostly just annoying
            // stay away from edges
            //res += TowardsXWithIn(myPosition, new Vector(0, 0), -1, Unit));
            //res += TowardsXWithIn(myPosition, new Vector(fieldDimensions.xMax, 0), -1, Unit));

            //res += TowardsYWithIn(myPosition, new Vector(0, 0), -1, Unit));
            //res += TowardsYWithIn(myPosition, new Vector(0, fieldDimensions.yMax), -1, Unit));

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

    }
}
