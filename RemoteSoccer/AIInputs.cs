using Common;
using Physics2;
//using Prototypist.Fluent;
using Prototypist.TaskChain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteSoccer
{

    //class Old {
    //    public class AITeam
    //    {

    //        const double Unit = 5_000;
    //        private int playerLag = 30;
    //        private static Random r = new Random();
    //        private readonly IReadOnlyDictionary<Guid, AITeamMember> team;
    //        private readonly GameState gameState;
    //        private readonly FieldDimensions fieldDimensions;
    //        private readonly bool leftGoal;
    //        private int updating = 0;

    //        public IEnumerable<(Guid, IInputs)> GetPlayers()
    //        {
    //            return team.Select(x => (x.Key, (IInputs)x.Value));
    //        }

    //        public AITeam(GameState gameState, Guid[] teammates, FieldDimensions fieldDimensions, bool leftGoal)
    //        {
    //            team = teammates.ToDictionary(x => x, x => new AITeamMember(this, x, gameState));
    //            this.gameState = gameState;
    //            this.fieldDimensions = fieldDimensions;
    //            this.leftGoal = leftGoal;

    //        }

    //        private class InputsWithCondition
    //        {
    //            public int frame;
    //            public PlayerInputs playerInputs;
    //        }

    //        private class AITeamMember : IInputs
    //        {
    //            private readonly AITeam aITeam;
    //            public PlayerInputs inputs;
    //            private GameState gameState;
    //            public ConcurrentLinkedList<InputsWithCondition> nextInputs = new ConcurrentLinkedList<InputsWithCondition>();
    //            internal Func<GameState, Vector> generator;

    //            public AITeamMember(AITeam aITeam, Guid id, GameState gameState)
    //            {
    //                this.aITeam = aITeam;
    //                inputs = new PlayerInputs(0, 0, 0, 0, id, ControlScheme.AI, false, Constants.NoMove);
    //                this.gameState = gameState;
    //            }

    //            public Task Init() => Task.CompletedTask;

    //            public Task<PlayerInputs> Next()
    //            {
    //                aITeam.RequestUpdate();

    //                while (nextInputs.TryGetFirst(out var proposed) && proposed.frame < gameState.Frame)
    //                {
    //                    inputs = proposed.playerInputs;
    //                    nextInputs.RemoveStart();
    //                }

    //                return Task.FromResult(inputs);
    //            }
    //        }

    //        private void RequestUpdate()
    //        {
    //            if (Interlocked.CompareExchange(ref updating, 1, 0) == 0)
    //            {
    //                Task.Run(() =>
    //                {
    //                    try
    //                    {
    //                        Update();
    //                    }
    //                    catch (Exception)
    //                    {
    //                    }
    //                    finally
    //                    {
    //                        updating = 0;
    //                    }
    //                });
    //            }
    //        }
    //        enum WhosBall
    //        {
    //            OurBall,
    //            TheirBall,
    //        }
    //        private WhosBall whosBall;
    //        private void Update()
    //        {
    //            //Task.Delay(400).Wait();

    //            var inputs = team.ToDictionary(member => member.Key, member => new PlayerInputs(
    //                      0,
    //                      0,
    //                      member.Value.inputs.BodyX,
    //                      member.Value.inputs.BodyY,
    //                      member.Value.inputs.Id,
    //                      member.Value.inputs.ControlScheme,
    //                      false,
    //                      member.Value.inputs.Boost));

    //            if (gameState.CountDownState.Countdown)// no one has the ball
    //            {
    //                whosBall = WhosBall.TheirBall;
    //                UpForGrabs(inputs);
    //            }
    //            else if (gameState.GameBall.OwnerOrNull is Guid owner)
    //            {
    //                if (team.TryGetValue(owner, out var hasBall)) // we have the ball
    //                {
    //                    var hasBallInputs = inputs[owner];
    //                    whosBall = WhosBall.OurBall;
    //                    var toAssign = team.Where(x => x.Key != owner).ToList();

    //                    {
    //                        var db1 = GetPositionGenerators(owner);
    //                        var hasBallGenerators = db1
    //                            .Select(pos =>
    //                            {
    //                                var localPos = pos;
    //                                return (generator: localPos.generator, score: HasBallEvaluator(localPos.pos), pos: localPos.pos);
    //                            })
    //                            .OrderByDescending(pair => pair.score)
    //                            .ToList();

    //                        if (hasBallGenerators.Any())
    //                        {
    //                            UpdateDirection(hasBall, hasBallGenerators.First().generator, hasBallInputs);
    //                        }
    //                        else
    //                        {
    //                            UpdateDirection(hasBall, (GameState gs) => new Vector(0.0, 0.0), hasBallInputs);
    //                        }

    //                        var myBody = gameState.players[owner].PlayerFoot.Position;


    //                        // if you have a good shot on goal take it
    //                        var shotsOnGoal = goalOffsets.Value
    //                            .Select(x => GoalWeScoreOn().NewAdded(x))
    //                            .Select(x =>
    //                            {
    //                                var diff = x.NewAdded(myBody.NewMinus());
    //                                var len = Constants.maxThrowPower * r.NextDouble();
    //                                var proposedThrow = diff.NewUnitized().NewScaled(len);

    //                                return (howLongToScore: PlayerInputApplyer.HowLongItTakesBallToGo(diff.Length, len), proposedThrow);

    //                            })
    //                            .Where(pair => DontThrowAtGoal(pair.howLongToScore, pair.proposedThrow))
    //                            .Where(pair => gameState.players.Values
    //                                .Where(x => x.PlayerFoot.Position.NewAdded(gameState.GameBall.Posistion.NewMinus()).Length > Unit * 1)
    //                                .All(x =>
    //                                {
    //                                    var (time, position) = PlayerInputApplyer.IntersectBallTime(x.PlayerBody.Position, gameState.GameBall.Posistion, pair.proposedThrow, x.PlayerBody.Velocity, Constants.footLen + Constants.BallRadius);
    //                                    return pair.howLongToScore < time;
    //                                }))
    //                            .OrderBy(x => x.howLongToScore)
    //                            .Select(x => x.proposedThrow)
    //                            .ToArray();

    //                        if (shotsOnGoal.Any())
    //                        {
    //                            hasBallInputs.Throw = true;
    //                            var foot = shotsOnGoal.First().NewUnitized();
    //                            hasBallInputs.FootX = foot.x;
    //                            hasBallInputs.FootY = foot.y;
    //                            goto next;
    //                        }

    //                        // think about throwing near you teammates

    //                        var positionValue = EvaluatePass(gameState.GameBall.Posistion);
    //                        var ourSpace = Space(gameState.GameBall.Posistion);

    //                        var passes = toAssign
    //                            .SelectMany(x =>
    //                            {
    //                                var space = Space(gameState.players[x.Key].PlayerBody.Position);
    //                                return throwOffsets.Value
    //                                .Select(pos =>
    //                                {
    //                                    var playerPos = gameState.players[x.Key].PlayerFoot.Position;
    //                                    var target = pos.NewAdded(playerPos);
    //                                    var diff = target.NewAdded(myBody.NewMinus());
    //                                    var len = Constants.maxThrowPower * ((r.NextDouble() * .5) + .5);
    //                                    var proposedThrow = diff.NewUnitized().NewScaled(len);
    //                                    var (howLongToCatch, catchAt) = PlayerInputApplyer.IntersectBallTime(playerPos, gameState.GameBall.Posistion, proposedThrow, gameState.players[x.Key].PlayerBody.Velocity, Constants.footLen + Constants.BallRadius - Unit);
    //                                    return (howLongToCatch, proposedThrow, x.Key, value: EvaluatePass(proposedThrow.NewScaled(howLongToCatch).NewAdded(gameState.GameBall.Posistion)), space, catchAt);
    //                                });
    //                            })
    //                            .Where(pair => pair.howLongToCatch > 15) // don't throw really short 
    //                            .Where(pair => pair.catchAt.x > 0 && pair.catchAt.y > 0 && pair.catchAt.x < fieldDimensions.xMax && pair.catchAt.y < fieldDimensions.yMax)
    //                            .Where(pair => DontThrowAtGoal(pair.howLongToCatch, pair.proposedThrow))
    //                            .Where(pair =>
    //                                gameState.players
    //                                .Where(x => !team.ContainsKey(x.Key))
    //                                .All(x =>
    //                                {
    //                                    var (time, _) = PlayerInputApplyer.IntersectBallTime(x.Value.PlayerBody.Position, gameState.GameBall.Posistion, pair.proposedThrow, x.Value.PlayerBody.Velocity, Constants.footLen + Constants.BallRadius - Unit);
    //                                    return pair.howLongToCatch + 20 < time;

    //                                }))
    //                            .Where(pair => pair.value > positionValue + 1000 || (ourSpace < 3 * Unit && pair.space > 3 * Unit + 1000))
    //                            .OrderByDescending(x => x.value)
    //                            .ToArray();

    //                        if (passes.Any())
    //                        {
    //                            var (_, proposedThrow, id, _, _, catchAt) = passes.First();

    //                            gameState.debugs.Add(new ThrowAt
    //                            {
    //                                Frame = gameState.Frame,
    //                                Id = Guid.NewGuid(),
    //                                Target = catchAt
    //                            });

    //                            hasBallInputs.Throw = true;

    //                            //if you're hot, go for the ball
    //                            UpdateDirection(team[id], (GameState gs) => PlayerInputApplyer.IntersectBallDirection(gs.players[id].PlayerBody.Position, gs.GameBall.Posistion, proposedThrow, gs.players[id].PlayerBody.Velocity, Constants.footLen + Constants.BallRadius - Unit), inputs[id]);
    //                            toAssign = toAssign.Where(x => x.Key != id).ToList();

    //                            var foot = proposedThrow.NewScaled(1.0 / Constants.maxThrowPower);
    //                            hasBallInputs.FootX = foot.x;
    //                            hasBallInputs.FootY = foot.y;
    //                            goto next;
    //                        }

    //                        // boot it
    //                        if (ourSpace < 1 * Unit)
    //                        {
    //                            var punts = throwOffsets.Value
    //                                .Where(dir => dir.Length > 0)
    //                                .Select(dir =>
    //                                {
    //                                    var proposedThrow = dir.NewUnitized().NewScaled(Constants.maxThrowPower);
    //                                    var howLongToCatch = gameState.players.Where(x => x.Key != owner).Select(x => PlayerInputApplyer.IntersectBallTime(x.Value.PlayerBody.Position, gameState.GameBall.Posistion, proposedThrow, x.Value.PlayerBody.Velocity, Constants.footLen + Constants.BallRadius).Item1).Min();
    //                                    return (proposedThrow, value: EvaluatePass(proposedThrow.NewScaled(howLongToCatch).NewAdded(gameState.GameBall.Posistion)), howLongToCatch);
    //                                })
    //                                .Where(pair => DontThrowAtGoal(pair.howLongToCatch, pair.proposedThrow))
    //                                .OrderByDescending(x => x.value)
    //                            .ToArray();

    //                            if (punts.Any())
    //                            {
    //                                var (proposedThrow, _, _) = punts.First();

    //                                hasBallInputs.Throw = true;

    //                                var foot = proposedThrow.NewUnitized();
    //                                hasBallInputs.FootX = foot.x;
    //                                hasBallInputs.FootY = foot.y;
    //                                goto next;
    //                            }

    //                        }

    //                    }
    //                next:

    //                    OffenseNotBall(toAssign, inputs);
    //                }
    //                else  // they have the ball
    //                {
    //                    whosBall = WhosBall.TheirBall;
    //                    Defense(inputs);
    //                }
    //            }
    //            else if (whosBall == WhosBall.OurBall)
    //            {
    //                // figure out who will get to the ball first, they run at it
    //                var lastHadBall = gameState.players.OrderByDescending(y => y.Value.LastHadBall).First().Key;
    //                var hot = team
    //                    .Where(x => x.Key != lastHadBall) // don't run after it if you just had it
    //                    .Select(x =>
    //                    {
    //                        var playerPos = gameState.players[x.Key].PlayerFoot.Position;
    //                        var howLongToCatch = PlayerInputApplyer.IntersectBallTime(playerPos, gameState.GameBall.Posistion, gameState.GameBall.Velocity, gameState.players[x.Key].PlayerBody.Velocity, Constants.footLen + Constants.BallRadius - Unit);
    //                        return (x, howLongToCatch);
    //                    }).OrderBy(x => x.howLongToCatch)
    //                .First().x;

    //                UpdateDirection(hot.Value, (GameState gs) => PlayerInputApplyer.IntersectBallDirection(gs.players[hot.Key].PlayerBody.Position, gs.GameBall.Posistion, gs.GameBall.Velocity, gs.players[hot.Key].PlayerBody.Velocity, Constants.footLen + Constants.BallRadius - Unit), inputs[hot.Key]);

    //                OffenseNotBall(team.Where(x => x.Key != hot.Key).ToList(), inputs);
    //            }
    //            else if (whosBall == WhosBall.TheirBall)
    //            {
    //                Defense(inputs);
    //            }

    //            foreach (var member in inputs)
    //            {
    //                // AI boosts in it's direction of travel
    //                var move = new Vector(member.Value.BodyX, member.Value.BodyY);
    //                if (!member.Value.Throw && move.Length > 0)
    //                {
    //                    move = move.NewUnitized().NewScaled(Constants.footLen * 2);

    //                    var myFoot = gameState.players[member.Key].PlayerFoot.Position;
    //                    var myBody = gameState.players[member.Key].PlayerBody.Position;

    //                    var pos = myBody.NewAdded(move.NewUnitized().NewScaled(Constants.footLen + Unit));

    //                    var posNoBoost = myBody.NewAdded(move.NewUnitized().NewScaled(Constants.footLen));

    //                    member.Value.FootX = move.x;
    //                    member.Value.FootY = move.y;
    //                    if (GlobalEvaluateFoot(pos, myBody, member.Key) < GlobalEvaluateFoot(posNoBoost, myBody, member.Key) + Unit * 2)
    //                    {
    //                        member.Value.Boost = Constants.NoMove;
    //                    }
    //                    else if (member.Value.Boost == Constants.NoMove)
    //                    {
    //                        member.Value.Boost = Guid.NewGuid();
    //                    }

    //                }
    //                else
    //                {
    //                    member.Value.Boost = Constants.NoMove;
    //                }
    //                team[member.Key].nextInputs.Add(new InputsWithCondition
    //                {
    //                    frame = gameState.Frame + playerLag,
    //                    playerInputs = member.Value
    //                });
    //            }
    //        }

    //        private bool DontThrowAtGoal(double howLongToCatch, Vector proposedThrow)
    //        {
    //            if (proposedThrow.Length == 0)
    //            {
    //                return true;
    //            }
    //            // take the normal to the throw
    //            var throwNormal = new Vector(proposedThrow.y, -proposedThrow.x).NewUnitized();
    //            // consider the our goals possition in those coordinates
    //            var goalPositionInNormal = throwNormal.Dot(GoalTheyScoreOn());
    //            // consider the balls current position in those coordinates
    //            var ballPositionInNormal = throwNormal.Dot(gameState.GameBall.Posistion);
    //            // are they less then good lenght apart?
    //            var normalDis = Math.Abs(goalPositionInNormal - ballPositionInNormal);
    //            if (normalDis > Constants.goalLen + Constants.BallRadius)
    //            {
    //                return true;
    //            }
    //            // how long will it take to reach the goal?
    //            var parallelDis = proposedThrow.NewUnitized().Dot(GoalTheyScoreOn().NewAdded(gameState.GameBall.Posistion.NewMinus()));
    //            if (parallelDis < 0)
    //            {
    //                return true; // you are throwing away from the gaol
    //            }
    //            parallelDis += Math.Sqrt(Math.Pow(Constants.goalLen + Constants.BallRadius, 2) - Math.Pow(normalDis, 2));
    //            return howLongToCatch + 10 < PlayerInputApplyer.HowLongItTakesBallToGo(parallelDis, proposedThrow.Length);
    //        }

    //        private double Space(Vector pos) => gameState.players.Where(y => !team.ContainsKey(y.Key)).Select(x => x.Value.PlayerBody.Position.NewAdded(pos.NewMinus()).Length).Min();

    //        private static Vector RandomVector()
    //        {
    //            var rads = r.NextDouble() * Math.PI * 2;
    //            var res = new Vector(Math.Sin(rads), Math.Cos(rads));
    //            return res;
    //        }

    //        // why are these lazy?! they are always going to be initialzed
    //        private Lazy<Vector[]> footOffsets = new Lazy<Vector[]>(() =>
    //        {
    //            return new int[25]
    //            .Select(_ => RandomVector().NewScaled(Unit * 5 * r.NextDouble()))
    //            .ToArray();
    //        });

    //        private (Vector, bool) GenerateDirectionFoot(Vector myfoot, Vector myBody, Guid self)
    //        {

    //            var list = footOffsets.Value
    //                .Select(x => myfoot.NewAdded(x))
    //                .Union(new[] { myBody, myfoot })
    //                .Union(gameState.players.Select(x => x.Value.PlayerFoot.Position).ToArray()) // towards players
    //                .Union(gameState.players.Select(x => x.Value.PlayerFoot.Position.NewAdded(myfoot.NewMinus()).NewMinus().NewAdded(myfoot))) // away from players
    //                .Union(new[] { gameState.GameBall.Posistion })
    //                .Where(pos => pos.x < fieldDimensions.xMax && pos.x > 0 && pos.y < fieldDimensions.yMax && pos.y > 0)
    //                .Select(pos => (position: pos, score: GlobalEvaluateFoot(pos, myBody, self)))
    //                .OrderByDescending(pair => pair.score)
    //                .ToArray();

    //            if (list.Any())
    //            {

    //                var direction = list.First().position.NewAdded(myfoot.NewMinus());

    //                return (direction, list.First().score > GlobalEvaluateFoot(myfoot, myBody, self) + (Unit));
    //            }

    //            return (new Vector(0, 0), false);

    //        }

    //        public double GlobalEvaluateFoot(Vector proposedPos, Vector myBody, Guid self)
    //        {
    //            var res = 0.0;

    //            var snapshot = gameState.GameBall.OwnerOrNull;
    //            if (gameState.CountDownState.Countdown)
    //            {
    //                // lean away from the ball
    //                res -= TowardsWithIn(proposedPos, gameState.GameBall.Posistion, .4, (Constants.footLen * 2) + Constants.ballWallLen);
    //            }
    //            else if (snapshot == self) // when you have the ball
    //            {
    //                // don't go near the other team
    //                foreach (var player in gameState.players.Where(x => !team.ContainsKey(x.Key)))
    //                {
    //                    res -= TowardsWithInBody(myBody, proposedPos, player.Value.PlayerFoot.Position, 4, Unit * 2);
    //                }

    //                // go to the goal
    //                res += TowardsWithInBody(myBody, proposedPos, GoalWeScoreOn(), 1, PlayerInputApplyer.HowFarCanIBoost(gameState.players[self].Boosts) - Unit);
    //            }
    //            else if ((snapshot is Guid owner))// when no one has the ball
    //            {
    //                if (team.ContainsKey(owner)) // one of you teammates has the ball
    //                {
    //                }
    //                else // the other team has the ball
    //                {
    //                    res += TowardsWithInBody(myBody, proposedPos, gameState.GameBall.Posistion
    //                    .NewAdded(gameState.GameBall.Velocity.NewScaled(Math.Min(10, gameState.GameBall.Posistion.NewAdded(myBody.NewMinus()).Length / Constants.speedLimit)))
    //                    , 10, PlayerInputApplyer.HowFarCanIBoost(gameState.players[self].Boosts) - Unit);

    //                    // towards the ball
    //                    res += TowardsWithIn(proposedPos, gameState.GameBall.Posistion, .4, Constants.footLen * 5);
    //                }
    //            }
    //            else if (whosBall == WhosBall.TheirBall)
    //            {
    //                res += TowardsWithInBody(myBody, proposedPos, gameState.GameBall.Posistion
    //                .NewAdded(gameState.GameBall.Velocity.NewScaled(Math.Min(10, gameState.GameBall.Posistion.NewAdded(myBody.NewMinus()).Length / Constants.speedLimit)))
    //                , 10, PlayerInputApplyer.HowFarCanIBoost(gameState.players[self].Boosts) - Unit);


    //                // towards the ball
    //                res += TowardsWithIn(proposedPos, gameState.GameBall.Posistion, .4, Constants.footLen * 5);
    //            }
    //            else if (whosBall == WhosBall.OurBall)
    //            {
    //                // towards the ball
    //                res += TowardsWithIn(proposedPos, gameState.GameBall.Posistion, .4, Constants.footLen * 5);
    //            }

    //            // a small force in the direction of motion
    //            var myPlayer = gameState.players[self];
    //            if (myPlayer.PlayerBody.Velocity.Length > 0)
    //            {
    //                res += Towards(proposedPos, myBody.NewAdded(myPlayer.PlayerBody.Velocity.NewUnitized().NewScaled(Constants.footLen - Constants.PlayerRadius)), .1);
    //            }
    //            else
    //            {
    //                res += Towards(proposedPos, myPlayer.PlayerFoot.Position, .1);
    //            }
    //            //res += Towards(proposedPos, myBody, .1);

    //            res -= StayInBounds(proposedPos, Unit);

    //            return res;
    //        }

    //        private double StayInBounds(Vector pos, double ponishment)
    //        {
    //            if (pos.x > fieldDimensions.xMax || pos.x < 0 || pos.y > fieldDimensions.yMax || pos.y < 0)
    //            {
    //                return ponishment;
    //            }
    //            return 0;
    //        }

    //        //private double TowardsPathInBody(Vector myBody, Vector toEval, Vector ball, Vector velocity, int scale, double whenWithIn)
    //        //{

    //        //    var startWith = myBody.NewAdded(ball.NewMinus());
    //        //    var len = startWith.Length;
    //        //    if (len > 0 && len < whenWithIn && velocity.Length > 0)
    //        //    {
    //        //        var toBall = ball.NewAdded(toEval.NewMinus());
    //        //        if (velocity.Dot(toBall) > 0) {
    //        //            return -whenWithIn * scale;
    //        //        }
    //        //        return -Math.Abs(new Vector(velocity.y, -velocity.x).NewUnitized().Dot(toBall)) * scale;
    //        //    }
    //        //    return -whenWithIn * scale;
    //        //}

    //        private void OffenseNotBall(List<KeyValuePair<Guid, AITeamMember>> toAssign, Dictionary<Guid, PlayerInputs> inputs)
    //        {
    //            if (toAssign.Count > 1)
    //            {
    //                // if we are their end we have a dump
    //                if (gameState.GameBall.Posistion.NewAdded(GoalTheyScoreOn().NewMinus()).Length > gameState.GameBall.Posistion.NewAdded(GoalWeScoreOn().NewMinus()).Length)
    //                {
    //                    var dump =
    //                    toAssign
    //                        .Select(pair => (pair, value: gameState.players[pair.Key].PlayerBody.Position.NewAdded(GoalTheyScoreOn().NewMinus()).Length))
    //                        .OrderBy(x => x.value)
    //                        .First()
    //                        .pair;

    //                    GetDump(dump, inputs);

    //                    toAssign.Remove(dump);
    //                }
    //                // otherwise we have a goalie
    //                else
    //                {
    //                    toAssign = Goalie(toAssign.ToArray(), inputs).ToList();
    //                }
    //            }

    //            foreach (var player in toAssign)
    //            {
    //                //if (BehindBall(player.Key))
    //                //{
    //                GetNewCuttingTowards(player, inputs);
    //                //}
    //                //else if (player.Value.generator == null)
    //                //{
    //                //    GetNewCuttingTowards(player, inputs);
    //                //}
    //                //else if (player.Value.generator(gameState).Length < Unit)
    //                //{
    //                //    GetNewCuttingTowards(player, inputs);
    //                //}
    //                //else {
    //                //    UpdateDirection(player.Value, player.Value.generator, inputs[player.Key]);
    //                //}
    //            }
    //        }

    //        private void GetDump(KeyValuePair<Guid, AITeamMember> player, Dictionary<Guid, PlayerInputs> inputs)
    //        {
    //            var currentPos = gameState.players[player.Key].PlayerBody.Position;
    //            var cutterGenerators = cutOffsets.Value
    //                .Select<Vector, Func<GameState, Vector>>(x => gs => currentPos.NewAdded(x).NewAdded(gs.players[player.Key].PlayerBody.Position.NewMinus()))
    //                // don't try to cut somewhere outside the room
    //                .Where(generator =>
    //                {
    //                    var loc = currentPos.NewAdded(generator(gameState));
    //                    return loc.x > 0 && loc.x < fieldDimensions.xMax && loc.y > 0 && loc.y < fieldDimensions.yMax;
    //                })
    //                .Select(generator => (generator: generator, score: DumpEvaluator(currentPos.NewAdded(generator(gameState).NewScaled(.1)), player.Key)))
    //                .OrderByDescending(pair => pair.score)
    //                .ToList();

    //            if (cutterGenerators.Any())
    //            {
    //                UpdateDirection(player.Value, cutterGenerators.First().generator, inputs[player.Key]);
    //            }
    //            else
    //            {
    //                UpdateDirection(player.Value, (GameState gs) => new Vector(0.0, 0.0), inputs[player.Key]);
    //            }
    //        }

    //        private void GetNewCuttingTowards(KeyValuePair<Guid, AITeamMember> player, Dictionary<Guid, PlayerInputs> inputs)
    //        {
    //            var currentPos = gameState.players[player.Key].PlayerBody.Position;
    //            var cutterGenerators = cutOffsets.Value
    //                .Select<Vector, Func<GameState, Vector>>(x => gs => currentPos.NewAdded(x).NewAdded(gs.players[player.Key].PlayerBody.Position.NewMinus()))
    //                // don't try to cut somewhere outside the room
    //                .Where(generator =>
    //                {
    //                    var loc = currentPos.NewAdded(generator(gameState));
    //                    return loc.x > 0 && loc.x < fieldDimensions.xMax && loc.y > 0 && loc.y < fieldDimensions.yMax;
    //                })
    //                .Select(generator => (generator: generator, score: CutEvaluator(currentPos.NewAdded(generator(gameState).NewScaled(.1)), player.Key))) // we evaluate what it looks like locally
    //                .OrderByDescending(pair => pair.score)
    //                .ToList();

    //            if (cutterGenerators.Any())
    //            {
    //                UpdateDirection(player.Value, cutterGenerators.First().generator, inputs[player.Key]);
    //            }
    //            else
    //            {
    //                UpdateDirection(player.Value, (GameState gs) => new Vector(0.0, 0.0), inputs[player.Key]);
    //            }
    //        }

    //        private void UpForGrabs(Dictionary<Guid, PlayerInputs> inputs)
    //        {
    //            var toAssign = Goalie(team.ToArray(), inputs);
    //            if (toAssign.Any())
    //            {
    //                toAssign = GetTheBall(toAssign, inputs);
    //            }
    //            foreach (var player in toAssign)
    //            {
    //                var currentPos = gameState.players[player.Key].PlayerBody.Position;
    //                var myPlayer = player;
    //                var cutterGenerators = cutOffsets.Value
    //                    .Select<Vector, Func<GameState, Vector>>(x => gs => currentPos.NewAdded(x).NewAdded(gs.players[player.Key].PlayerBody.Position.NewMinus()))
    //                    .Select(generator => (generator: generator, score: UpForGrabsEvaluator(currentPos.NewAdded(generator(gameState).NewScaled(.1)), player.Key)))
    //                    .OrderByDescending(pair => pair.score)
    //                    .ToList();

    //                if (cutterGenerators.Any())
    //                {
    //                    UpdateDirection(player.Value, cutterGenerators.First().generator, inputs[player.Key]);
    //                }
    //                else
    //                {
    //                    UpdateDirection(player.Value, (GameState gs) => new Vector(0.0, 0.0), inputs[player.Key]);
    //                }
    //            }
    //        }

    //        private double UpForGrabsEvaluator(Vector myPosition, Guid self)
    //        {

    //            var res = 0.0;

    //            // stay away from your teammates
    //            foreach (var player in gameState.players.Where(x => team.ContainsKey(x.Key) && x.Key != self))
    //            {
    //                res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 3, Unit * 8);
    //            }

    //            // don't get too close to the other teams players
    //            foreach (var player in gameState.players.Where(x => !team.ContainsKey(x.Key)))
    //            {
    //                var dissToTheirEnd =
    //                gameState.players[player.Key].PlayerBody.Position.NewAdded(GoalWeScoreOn().NewMinus()).Length;
    //                var dissToOurEnd =
    //                gameState.players[player.Key].PlayerBody.Position.NewAdded(GoalTheyScoreOn().NewMinus()).Length;


    //                res += TowardsWithIn(myPosition, player.Value.PlayerBody.Position, (dissToTheirEnd - dissToOurEnd) / (Unit), Unit * 6);
    //                res += TowardsWithIn(myPosition, player.Value.PlayerBody.Position, (dissToTheirEnd - dissToOurEnd) / (Unit), Unit * 1);
    //            }

    //            // don't get too far from the ball
    //            res -= AwayWithOut(myPosition, gameState.GameBall.Posistion, 20, Unit * 10);

    //            return res;
    //        }

    //        private void Defense(Dictionary<Guid, PlayerInputs> inputs)
    //        {

    //            var toAssign = Goalie(team.ToArray(), inputs).ToList();
    //            //if (toAssign.Any())
    //            //{
    //            //    toAssign = GetTheBall(toAssign.ToArray(), inputs).ToList();
    //            //}


    //            foreach (var (baddie, _, _) in gameState.players.Values
    //                .Where(x => !team.ContainsKey(x.Id) && x.Id != gameState.GameBall.OwnerOrNull.GetValueOrDefault(Guid.NewGuid()))
    //                .Select(x => (x, x.PlayerBody.Position.NewAdded(GoalTheyScoreOn().NewMinus()).Length, hasBall: x.Id == gameState.GameBall.OwnerOrNull ? 1 : 0))
    //                .OrderByDescending(x=>x.hasBall)
    //                .ThenBy(x => x.Length))
    //            {

    //                var getTheBaddies = toAssign
    //                    .Select(pair => (pair, gameState.players[pair.Key].PlayerBody.Position.NewAdded(baddie.PlayerBody.Position.NewMinus()).Length))
    //                    .OrderBy(pair => pair.Length)
    //                    .ToList();

    //                if (!getTheBaddies.Any())
    //                {
    //                    continue;
    //                }

    //                var getTheBaddie = getTheBaddies.First();

    //                toAssign = toAssign.Except(new[] { getTheBaddie.pair }).ToList();

    //                var guardGenerators = GetPositionGenerators(getTheBaddie.pair.Key)
    //                    .Select(pos => (generator: pos.generator, score: GuardPlayerEvaluator(baddie.Id)(pos.pos)))
    //                    .OrderByDescending(pair => pair.score)
    //                    .ToList();

    //                if (guardGenerators.Any())
    //                {
    //                    UpdateDirection(getTheBaddie.pair.Value, guardGenerators.First().generator, inputs[getTheBaddie.pair.Key]);
    //                }
    //                else
    //                {
    //                    UpdateDirection(getTheBaddie.pair.Value, (GameState gs) => new Vector(0.0, 0.0), inputs[getTheBaddie.pair.Key]);
    //                }
    //            }

    //        }

    //        private KeyValuePair<Guid, AITeamMember>[] GetTheBall(KeyValuePair<Guid, AITeamMember>[] toAssign, Dictionary<Guid, PlayerInputs> inputs)
    //        {
    //            var getTheBall = toAssign
    //               .Select(pair => (pair, gameState.players[pair.Key].PlayerBody.Position.NewAdded(gameState.GameBall.Posistion.NewMinus()).Length))
    //               .OrderBy(pair => pair.Length)
    //               .First();

    //            var getTheBallGenerators = GetPositionGenerators(getTheBall.pair.Key)
    //                .Select(pos => (generator: pos.generator, score: GetTheBallEvaluator(pos.pos), pos: pos.pos))
    //                .OrderByDescending(pair => pair.score)
    //                .ToList();

    //            if (getTheBallGenerators.Any())
    //            {
    //                UpdateDirection(getTheBall.pair.Value, getTheBallGenerators.First().generator, inputs[getTheBall.pair.Key]);
    //            }
    //            else
    //            {
    //                UpdateDirection(getTheBall.pair.Value, (GameState gs) => new Vector(0.0, 0.0), inputs[getTheBall.pair.Key]);
    //            }

    //            return toAssign.Except(new[] { getTheBall.pair }).ToArray();
    //        }

    //        private KeyValuePair<Guid, AITeamMember>[] Goalie(KeyValuePair<Guid, AITeamMember>[] toAssign, Dictionary<Guid, PlayerInputs> inputs)
    //        {
    //            var goalie = toAssign
    //                .Select(pair => (pair, gameState.players[pair.Key].PlayerBody.Position.NewAdded(GoalTheyScoreOn().NewMinus()).Length))
    //                .OrderBy(pair => pair.Length)
    //                .First();


    //            var goalieGenerators = GetPositionGenerators(goalie.pair.Key)
    //                .Select(pos => new { generator = pos.generator, score = GoalieEvaluator(pos.pos, goalie.pair.Key), pos = pos.pos })
    //                .OrderByDescending(pair => pair.score)
    //                .ToList();

    //            if (goalieGenerators.Any())
    //            {
    //                UpdateDirection(goalie.pair.Value, goalieGenerators.First().generator, inputs[goalie.pair.Key]);
    //            }
    //            else
    //            {
    //                UpdateDirection(goalie.pair.Value, (GameState gs) => new Vector(0.0, 0.0), inputs[goalie.pair.Key]);
    //            }


    //            return toAssign.Except(new[] { goalie.pair }).ToArray();

    //        }

    //        private double EvaluatePass(Vector position)
    //        {

    //            var res = 0.0;
    //            // go to the goal
    //            res += Towards(position, GoalWeScoreOn(), 4);
    //            // but calcel it out once you are close enough to shoot, we don't really care if you are close or really close
    //            res -= TowardsWithIn(position, GoalWeScoreOn(), 4, Unit * 4);

    //            // go away from our goal
    //            res -= TowardsWithIn(position, GoalTheyScoreOn(), 1, Unit * 12);
    //            res -= TowardsWithIn(position, GoalTheyScoreOn(), 10, Unit * 4);

    //            return res;
    //        }

    //        private void UpdateDirection(AITeamMember goalie, Func<GameState, Vector> generator, PlayerInputs inputs)
    //        {
    //            goalie.generator = generator;
    //            var concreteTarget = generator(gameState);
    //            if (Double.IsNaN(concreteTarget.Length))
    //            {
    //                var aahhhh = 0;
    //            }

    //            if (concreteTarget.Length > 0)
    //            {
    //                concreteTarget = concreteTarget.NewUnitized();
    //            }
    //            //if (concreteTarget.Length == 0) {
    //            //    var db = 0; 
    //            //}

    //            inputs.BodyX = concreteTarget.x;
    //            inputs.BodyY = concreteTarget.y;
    //        }

    //        class GenAndPos
    //        {
    //            public Func<GameState, Vector> generator;
    //            public Vector pos;
    //        }

    //        class SimpleGenAndPos : GenAndPos
    //        {
    //            public Vector dir;
    //            public SimpleGenAndPos(Vector dir, Vector myPosition)
    //            {
    //                this.dir = new Vector(dir.x, dir.y);
    //                this.pos = myPosition.NewAdded(dir);
    //                this.generator = _ => this.dir;
    //            }
    //        }

    //        private Vector[] directions = new int[100].Select(x => RandomVector().NewScaled(Constants.goalLen * r.NextDouble())).ToArray();

    //        private List<(Func<GameState, Vector> generator, Vector pos)> GetPositionGenerators(Guid self)
    //        {
    //            var myPosition = gameState.players[self].PlayerBody.Position;

    //            var getOtherPlayers = gameState.players
    //                .Where(x => !team.ContainsKey(x.Key))
    //                .Select(x =>
    //                {
    //                    var localX = x;
    //                    return (Func<GameState, Vector>)((GameState gs) => gs.players[localX.Key].PlayerBody.Position.NewAdded(gs.players[self].PlayerBody.Position.NewMinus()));
    //                });

    //            var getBall = new[] {
    //            (Func<GameState, Vector>)((GameState gs) => gs.GameBall.Posistion.NewAdded(gs.players[self].PlayerBody.Position.NewMinus()))
    //        };

    //            var getGoalie = new[] {
    //            (Func<GameState, Vector>)((GameState gs) => gs.GameBall.Posistion.NewAdded(GoalTheyScoreOn()).NewScaled(.5).NewAdded(gs.players[self].PlayerBody.Position.NewMinus()))
    //        };

    //            var random = directions
    //                .Select(dir =>
    //                {
    //                // something weird with closures here
    //                // sometimes these all ending returning the same thing
    //                // but only sometimes
    //                var x = dir;
    //                    return (Func<GameState, Vector>)(_ => x);
    //                })
    //                .ToArray();
    //            //var random = new List<GenAndPos>();
    //            //foreach (var dir in directions)
    //            //{
    //            //    random.Add(new SimpleGenAndPos (dir, myPosition));
    //            //}

    //            var res = random
    //                .Union(getBall)
    //                .Union(getOtherPlayers)
    //                .Union(getGoalie)
    //                .Select(generator =>
    //                {
    //                    var dir = generator(gameState);
    //                    if (dir.Length > 0 && dir.Length < Unit)
    //                    {

    //                        dir = dir.NewUnitized().NewScaled(Unit);
    //                    }
    //                    else if (dir.Length > Unit)
    //                    {
    //                        dir = dir.NewUnitized().NewScaled(Unit);
    //                    }
    //                    var prospect = myPosition.NewAdded(dir);

    //                    return (generator: generator, pos: prospect);
    //                })
    //                .Where(pair => pair.pos.x < fieldDimensions.xMax && pair.pos.x > 0 && pair.pos.y < fieldDimensions.yMax && pair.pos.y > 0)
    //                .ToList();

    //            return res;
    //        }


    //        private double GoalieEvaluator(Vector position, Guid self)
    //        {

    //            var res = 0.0;

    //            if (gameState.GameBall.OwnerOrNull is Guid owner && team.ContainsKey(owner))
    //            {
    //                // if your team has the ball don't go for it
    //                res -= TowardsWithIn(position, gameState.GameBall.Posistion, 8, Unit * 4);
    //            }
    //            else
    //            {

    //                // if you are the closest by a unit just grab the ball
    //                if (gameState.players.Where(x => x.Key != self).Select(x => x.Value.PlayerBody.Position.NewAdded(gameState.GameBall.Posistion.NewMinus()).Length).OrderBy(x => x).First()
    //                    > gameState.players[self].PlayerBody.Position.NewAdded(gameState.GameBall.Posistion.NewMinus()).Length + Unit)
    //                {
    //                    return Towards(position, gameState.GameBall.Posistion, 4);
    //                }
    //            }

    //            // TODO if there are several players from the other team near the goal you need to player closer to the gaol
    //            // if there are fewer you can rush the player


    //            res += Towards(position, GoalTheyScoreOn(), 5);
    //            res += Towards(position, gameState.GameBall.Posistion, 5);
    //            //res += TowardsWithIn(position, gameState.GameBall.Posistion, 3, Constants.goalLen * 1);


    //            res -= TowardsWithIn(position, GoalTheyScoreOn(), 20, Constants.goalLen * 2);

    //            foreach (var player in gameState.players.Where(x => !team.ContainsKey(x.Key)))
    //            {
    //                res += Towards(position, player.Value.PlayerBody.Position, .1);
    //            }

    //            // if we are in count down don't go too close to the ball
    //            if (gameState.CountDownState.Countdown)
    //            {
    //                res -= TowardsWithIn(position, gameState.GameBall.Posistion, 8, Unit * 15);
    //            }

    //            if (!gameState.CountDownState.Countdown && gameState.GameBall.OwnerOrNull == null)
    //            {
    //                res += Towards(position, GoalTheyScoreOn(), 5);
    //            }


    //            res -= AwayWithOut(position, gameState.GameBall.Posistion, 4, Unit * 15);
    //            //foreach (var player in gameState.players.Where(x => team.ContainsKey(x.Key)))
    //            //{
    //            //    res -= TowardsWithIn(position, player.Value.PlayerBody.Position, 1, Unit* 5);
    //            //}

    //            // the more opponents are near the goal the more we want to be near the goal
    //            //foreach (var player in gameState.players.Where(x => !team.ContainsKey(x.Key)))
    //            //{
    //            //    if (player.Value.PlayerBody.Position.NewAdded(GoalTheyScoreOn().NewMinus()).Length < Constants.goalLen * 6)
    //            //    {
    //            //        res += Towards(position, GoalTheyScoreOn(), 1);
    //            //    }
    //            //}

    //            res -= StayInBounds(position, Unit);

    //            return res;

    //            //return Towards(position, gameState.GameBall.Posistion.NewAdded(GoalTheyScoreOn()).NewScaled(.5), 4);
    //        }




    //        // I get it 
    //        // when a play is in a sandwich
    //        // the op in front and the op behind cancel
    //        // and they just run towards the other goal
    //        private double HasBallEvaluator(Vector position)
    //        {
    //            var res = 0.0;
    //            // don't go near the other team
    //            foreach (var player in gameState.players.Where(x => !team.ContainsKey(x.Key)))
    //            {
    //                res -= TowardsWithIn(position, player.Value.PlayerBody.Position, 4, Unit * 4);
    //            }


    //            res += Towards(position, GoalWeScoreOn(), 1);

    //            var lenHome = gameState.GameBall.Posistion.NewAdded(GoalTheyScoreOn().NewMinus()).Length;
    //            var lenGoal = gameState.GameBall.Posistion.NewAdded(GoalWeScoreOn().NewMinus()).Length;
    //            if (lenHome > lenGoal)
    //            {
    //                // dont go away from the goal
    //                res -= AwayWithOut(position, GoalWeScoreOn(), 3, lenGoal + 1000);
    //                // score if you can
    //                res += TowardsWithIn(position, GoalWeScoreOn(), 10, Constants.goalLen);
    //            }
    //            else
    //            {
    //                // don't go towards your goal
    //                res -= TowardsWithIn(position, GoalTheyScoreOn(), 3, lenHome - 500);
    //                // really don't self goal
    //                res -= TowardsWithIn(position, GoalTheyScoreOn(), 6, Constants.goalLen + Unit * 3);
    //            }

    //            res -= StayInBounds(position, Unit);

    //            return res;
    //        }
    //        private double DumpEvaluator(Vector myPosition, Guid self)
    //        {

    //            var res = 0.0;

    //            // stay away from your teammates
    //            foreach (var player in gameState.players.Where(x => team.ContainsKey(x.Key) && x.Key != self))
    //            {
    //                res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 3, Unit * 4);
    //            }

    //            // don't get too close to the other teams players
    //            foreach (var player in gameState.players.Where(x => !team.ContainsKey(x.Key)))
    //            {
    //                res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 3, Unit * 6);
    //                res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 3, Unit * 1);
    //            }

    //            // don't get too far from the ball
    //            res -= AwayWithOut(myPosition, gameState.GameBall.Posistion, 20, Unit * 15);

    //            // we like to be in the line between our goal and the ball
    //            // we are going to fall back to being the goalie
    //            //res += Towards(myPosition, gameState.GameBall.Posistion, 2);


    //            //res -= Towards1D(myPosition.y, fieldDimensions.yMax * 1.0 / 3.0, 1);
    //            //res -= Towards1D(myPosition.y, fieldDimensions.yMax * 2.0 / 3.0, 1);
    //            res -= Towards1D(myPosition.x, gameState.GameBall.Posistion.x, 1);
    //            res -= Towards1D(myPosition.x, gameState.GameBall.Posistion.x + 2 * Unit * Math.Sign(GoalTheyScoreOn().x - gameState.GameBall.Posistion.x), 1);

    //            res -= StayInBounds(myPosition, Unit);

    //            return res;
    //        }

    //        private double CutEvaluator(Vector myPosition, Guid self)
    //        {
    //            var res = 0.0;


    //            // don't be behind the ball
    //            if (BehindBall(self))
    //            {
    //                res += Towards(myPosition, GoalWeScoreOn().NewAdded(gameState.GameBall.Posistion).NewScaled(.5), 4);
    //            }
    //            else
    //            {

    //                // go towards the goal
    //                res += Towards(myPosition, GoalWeScoreOn(), 2);
    //                res -= TowardsWithIn(myPosition, GoalWeScoreOn(), 1, Constants.goalLen + Unit * 3);

    //                // stay away from your teammates
    //                foreach (var player in gameState.players.Where(x => team.ContainsKey(x.Key) && x.Key != self && x.Key != gameState.GameBall.OwnerOrNull))
    //                {
    //                    res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 2, Unit * 8);
    //                }

    //                // don't get too close to the other teams players
    //                foreach (var player in gameState.players.Where(x => !team.ContainsKey(x.Key)))
    //                {
    //                    res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 1, Unit * 6);
    //                    //res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 3, Unit * .5);
    //                }

    //                // don't get too far from the ball
    //                res -= AwayWithOut(myPosition, gameState.GameBall.Posistion, 1, Unit * 12);
    //                res -= AwayWithOut(myPosition, gameState.GameBall.Posistion, 4, Unit * 20);

    //                // don't get too close to the ball
    //                res -= TowardsWithIn(myPosition, gameState.GameBall.Posistion, .5, Unit * 5);

    //                // don't run away from the ball, it's impossible to pass to you
    //                //var diff = myPosition.NewAdded(gameState.players[self].PlayerBody.Position.NewMinus());
    //                //if (diff.Length > 0) {
    //                //    res -= Math.Max(0, diff.NewUnitized().Dot(gameState.players[self].PlayerBody.Position.NewAdded(gameState.GameBall.Posistion.NewMinus()))-.5) * 1000;
    //                //}
    //            }

    //            res -= StayInBounds(myPosition, Unit);

    //            return res;
    //        }

    //        private bool BehindBall(Guid self)
    //        {
    //            return gameState.players[self].PlayerBody.Position.NewAdded(GoalWeScoreOn().NewMinus()).Length > Math.Max(gameState.GameBall.Posistion.NewAdded(GoalWeScoreOn().NewMinus()).Length, Unit * 5);
    //        }

    //        private Lazy<Vector[]> throwOffsets = new Lazy<Vector[]>(() =>
    //        {
    //            // this isn't a good random for a circle. it perfers pie/4 to pie/2
    //            return new int[50].Select(_ => RandomVector().NewScaled(Unit * 6 * r.NextDouble())).ToArray();
    //        });

    //        private Lazy<Vector[]> cutOffsets = new Lazy<Vector[]>(() =>
    //        {
    //            // this isn't a good random for a circle. it perfers pie/4 to pie/2
    //            return new int[25].Select(_ => RandomVector().NewScaled((Unit * 2) + (Unit * 3 * r.NextDouble()))).ToArray();
    //        });


    //        private Lazy<Vector[]> goalOffsets = new Lazy<Vector[]>(() =>
    //        {
    //            // this isn't a good random for a circle. it perfers pie/4 to pie/2
    //            return new int[10].Select(_ => RandomVector().NewScaled(Constants.goalLen)).ToArray();
    //        });

    //        //private int startedThrowing;

    //        private bool CanPass(Vector target, Vector[] obsticals)
    //        {
    //            foreach (var obstical in obsticals)
    //            {
    //                if (PassIsBlockedBy(target, obstical) > .9)
    //                {
    //                    return false;
    //                }
    //            }
    //            return true;
    //        }

    //        private double PassIsBlockedBy(Vector target, Vector obstical)
    //        {
    //            var passDirection = target.NewAdded(gameState.GameBall.Posistion.NewMinus());
    //            var obsticalDirection = obstical.NewAdded(gameState.GameBall.Posistion.NewMinus());
    //            if (passDirection.Length < obsticalDirection.Length)
    //            {
    //                return 0;
    //            }
    //            return passDirection.NewUnitized().Dot(obsticalDirection.NewUnitized());
    //        }


    //        private double GetTheBallEvaluator(Vector position)
    //        {

    //            var res = 0.0;

    //            res += Towards(position, gameState.GameBall.Posistion.NewAdded(gameState.GameBall.Velocity.NewScaled(playerLag + 10)), 4);

    //            res += Towards(position, GoalTheyScoreOn(), 2);

    //            res -= AwayWithOut(position, GoalTheyScoreOn(), 2, Unit * 30);

    //            res -= StayInBounds(position, Unit);

    //            return res;
    //        }

    //        private Func<Vector, double> GuardPlayerEvaluator(Guid playerId) => (Vector position) =>
    //        {
    //            var res = 0.0;

    //            if (gameState.players.TryGetValue(playerId, out var player))
    //            {
    //                res += Towards(position, player.PlayerBody.Position.NewAdded(player.PlayerBody.Velocity.NewScaled(playerLag + 10)), 4);

    //                res += Towards(position, GoalTheyScoreOn(), 2);

    //                res -= AwayWithOut(position, GoalTheyScoreOn(), 2, Unit * 40);
    //            }

    //            res -= StayInBounds(position, Unit);

    //            return res;
    //        };


    //        private Vector GoalWeScoreOn()
    //        {
    //            if (leftGoal)
    //            {
    //                return gameState.LeftGoal.Posistion;
    //            }
    //            return gameState.RightGoal.Posistion;
    //        }

    //        private Vector GoalTheyScoreOn()
    //        {
    //            if (leftGoal)
    //            {
    //                return gameState.RightGoal.Posistion;
    //            }
    //            return gameState.LeftGoal.Posistion;
    //        }


    //        private Vector TowardsXWithIn(Vector us, Vector them, double scale, double whenWithIn)
    //        {

    //            var startWith = them.NewAdded(us.NewMinus());
    //            var len = Math.Abs(startWith.x);
    //            if (len > 0 && len < whenWithIn)
    //            {
    //                return new Vector(startWith.x, 0).NewUnitized().NewScaled(scale * (whenWithIn - len));
    //            }
    //            return new Vector(0, 0);
    //        }

    //        private Vector TowardsYWithIn(Vector us, Vector them, double scale, double whenWithIn)
    //        {

    //            var startWith = them.NewAdded(us.NewMinus());
    //            var len = Math.Abs(startWith.y);
    //            if (len > 0 && len < whenWithIn)
    //            {
    //                return new Vector(0, startWith.y).NewUnitized().NewScaled(scale * (whenWithIn - len));
    //            }
    //            return new Vector(0, 0);
    //        }

    //        private double Towards1D(double at, double target, double scale)
    //        {
    //            return -Math.Abs(at - target) * scale;
    //        }

    //        private double Towards(Vector us, Vector them, double scale)
    //        {
    //            var startWith = them.NewAdded(us.NewMinus());
    //            return -startWith.Length * scale;
    //        }


    //        private double TowardsWithIn(Vector us, Vector them, double scale, double whenWithIn)
    //        {

    //            var startWith = them.NewAdded(us.NewMinus());
    //            var len = startWith.Length;
    //            if (len > 0 && len < whenWithIn)
    //            {
    //                return scale * (whenWithIn - len);
    //            }
    //            return 0;
    //        }

    //        private double TowardsWithInBody(Vector body, Vector us, Vector them, double scale, double whenWithIn)
    //        {

    //            var startWith = them.NewAdded(body.NewMinus());
    //            var len = startWith.Length;
    //            if (len < whenWithIn)
    //            {
    //                return -scale * them.NewAdded(us.NewMinus()).Length;
    //            }
    //            return 0;
    //        }


    //        private double AwayWithOut(Vector us, Vector them, double scale, double whenWithOut)
    //        {

    //            var startWith = them.NewAdded(us.NewMinus());
    //            var len = startWith.Length;
    //            if (len > whenWithOut)
    //            {
    //                return scale * (len - whenWithOut);
    //            }
    //            return 0;
    //        }

    //    }

    //}

    // this should be in common
    class AITeam
    {

        const double Unit = 5_000;
        private int playerLag = 30;
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
            team = teammates.ToDictionary(x => x, x => new AITeamMember(this, x, gameState));
            this.gameState = gameState;
            this.fieldDimensions = fieldDimensions;
            this.leftGoal = leftGoal;

        }

        private class InputsWithCondition
        {
            public int frame;
            public PlayerInputs playerInputs;
        }

        private class AITeamMember : IInputs
        {
            private readonly AITeam aITeam;
            public PlayerInputs inputs;
            private GameState gameState;
            public ConcurrentLinkedList<InputsWithCondition> nextInputs = new ConcurrentLinkedList<InputsWithCondition>();
            internal Func<GameState, Vector> generator;

            public AITeamMember(AITeam aITeam, Guid id, GameState gameState)
            {
                this.aITeam = aITeam;
                inputs = new PlayerInputs(0, 0, 0, 0, id, ControlScheme.AI, false, Constants.NoMove);
                this.gameState = gameState;
            }

            public Task Init() => Task.CompletedTask;

            public Task<PlayerInputs> Next()
            {
                aITeam.RequestUpdate();

                while (nextInputs.TryGetFirst(out var proposed) && proposed.frame < gameState.Frame)
                {
                    inputs = proposed.playerInputs;
                    nextInputs.RemoveStart();
                }

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
                        var db = 0;
                    }
                    finally
                    {
                        updating = 0;
                    }
                });
            }
        }
        enum WhosBall
        {
            OurBall,
            TheirBall
        }

        private void Update()
        {
            //Task.Delay(400).Wait();

            var inputs = team.ToDictionary(member => member.Key, member => new PlayerInputs(
                      0,
                      0,
                      0,
                      0,
                      member.Value.inputs.Id,
                      member.Value.inputs.ControlScheme,
                      false,
                      member.Value.inputs.Boost));


            if (gameState.GameBall.OwnerOrNull is Guid owner)
            {
                if (team.TryGetValue(owner, out var hasBall)) // we have the ball
                {
                    var hasBallInputs = inputs[owner];
                    var toAssign = team.Where(x => x.Key != owner).ToList();

                    {
                        var db1 = GetPositionGenerators(owner);
                        var hasBallGenerators = db1
                            .Select(pos =>
                            {
                                var localPos = pos;
                                return (generator: localPos.generator, score: HasBallEvaluator(localPos.pos), pos: localPos.pos);
                            })
                            .OrderByDescending(pair => pair.score)
                            .ToList();

                        if (hasBallGenerators.Any())
                        {
                            UpdateDirection(hasBall, hasBallGenerators.First().generator, hasBallInputs);
                        }
                        else
                        {
                            UpdateDirection(hasBall, (GameState gs) => new Vector(0.0, 0.0), hasBallInputs);
                        }

                        var myBody = gameState.players[owner].PlayerFoot.Position;


                        // if you have a good shot on goal take it
                        var shotsOnGoal = goalOffsets.Value
                            .Select(x => GoalWeScoreOn().NewAdded(x))
                            .Select(x =>
                            {
                                var diff = x.NewAdded(myBody.NewMinus());
                                var len = Constants.maxThrowPower * r.NextDouble();
                                var proposedThrow = diff.NewUnitized().NewScaled(len);

                                return (howLongToScore: PlayerInputApplyer.HowLongItTakesBallToGo(diff.Length, len), proposedThrow);

                            })
                            .Where(pair => DontThrowAtGoal(pair.howLongToScore, pair.proposedThrow))
                            .Where(pair => gameState.players.Values
                                .Where(x => x.PlayerFoot.Position.NewAdded(gameState.GameBall.Posistion.NewMinus()).Length > Unit * 1)
                                .All(x =>
                                {
                                    var (time, position) = PlayerInputApplyer.IntersectBallTime(x.PlayerBody.Position, gameState.GameBall.Posistion, pair.proposedThrow, x.PlayerBody.Velocity, Constants.footLen + Constants.BallRadius);
                                    return pair.howLongToScore < time;
                                }))
                            .OrderBy(x => x.howLongToScore)
                            .Select(x => x.proposedThrow)
                            .ToArray();

                        if (shotsOnGoal.Any())
                        {
                            hasBallInputs.Throw = true;
                            var foot = shotsOnGoal.First().NewUnitized();
                            hasBallInputs.FootX = foot.x;
                            hasBallInputs.FootY = foot.y;
                            goto next;
                        }

                        var positionValue = EvaluatePass(gameState.GameBall.Posistion);
                        var ourSpace = Space(gameState.GameBall.Posistion);

                        var passes = toAssign
                            .SelectMany(x =>
                            {
                                var space = Space(gameState.players[x.Key].PlayerBody.Position);
                                return throwOffsets.Value
                                .Select(pos =>
                                {
                                    var playerPos = gameState.players[x.Key].PlayerFoot.Position;
                                    var target = pos.NewAdded(playerPos);
                                    var diff = target.NewAdded(myBody.NewMinus());
                                    var len = Constants.maxThrowPower * ((r.NextDouble() * .5) + .5);
                                    var proposedThrow = diff.NewUnitized().NewScaled(len);

                                    var (howLongToCatch, catchAt) = PlayerInputApplyer.IntersectBallTime(playerPos, gameState.GameBall.Posistion, gameState.GameBall.Velocity, gameState.players[x.Key].PlayerBody.Velocity, Constants.footLen + Constants.BallRadius - Unit, true);
                                    if (howLongToCatch < PlayerInputApplyer.HowLongCanIBoost(gameState.players[x.Key].Boosts))
                                    {
                                        return (howLongToCatch, proposedThrow, x.Key, value: EvaluatePass(proposedThrow.NewScaled(howLongToCatch).NewAdded(gameState.GameBall.Posistion)), space, catchAt);
                                    }

                                    (howLongToCatch, catchAt) = PlayerInputApplyer.IntersectBallTime(playerPos, gameState.GameBall.Posistion, proposedThrow, gameState.players[x.Key].PlayerBody.Velocity, Constants.footLen + Constants.BallRadius - Unit);
                                    return (howLongToCatch, proposedThrow, x.Key, value: EvaluatePass(proposedThrow.NewScaled(howLongToCatch).NewAdded(gameState.GameBall.Posistion)), space, catchAt);
                                });
                            })
                            .Where(pair => pair.howLongToCatch > 25) // don't throw really short 
                            .Where(pair => pair.catchAt.x > 0 && pair.catchAt.y > 0 && pair.catchAt.x < fieldDimensions.xMax && pair.catchAt.y < fieldDimensions.yMax)
                            .Where(pair => DontThrowAtGoal(pair.howLongToCatch, pair.proposedThrow))
                            .Where(pair =>
                                gameState.players
                                .Where(x => !team.ContainsKey(x.Key))
                                .All(x =>
                                {
                                    var (time, _) = PlayerInputApplyer.IntersectBallTime(x.Value.PlayerBody.Position, gameState.GameBall.Posistion, pair.proposedThrow, x.Value.PlayerBody.Velocity, Constants.footLen + Constants.BallRadius - Unit);

                                    if (time < PlayerInputApplyer.HowLongCanIBoost(gameState.players[x.Key].Boosts))
                                    {
                                        return pair.howLongToCatch + 20 < time;
                                    }

                                    (time, _) = PlayerInputApplyer.IntersectBallTime(x.Value.PlayerBody.Position, gameState.GameBall.Posistion, pair.proposedThrow, x.Value.PlayerBody.Velocity, Constants.footLen + Constants.BallRadius - Unit);
                                    return pair.howLongToCatch + 20 < time;

                                }))
                            .Where(pair => pair.value > positionValue + 1000 || (ourSpace < 3 * Unit && pair.space > 3 * Unit + 1000))
                            .OrderByDescending(x => x.value)
                            .ToArray();

                        if (passes.Any())
                        {
                            var (_, proposedThrow, id, _, _, catchAt) = passes.First();

                            gameState.debugs.Add(new ThrowAt
                            {
                                Frame = gameState.Frame,
                                Id = Guid.NewGuid(),
                                Target = catchAt
                            });

                            hasBallInputs.Throw = true;

                            //if you're hot, go for the ball
                            //UpdateDirection(team[id], (GameState gs) => PlayerInputApplyer.IntersectBallDirection(gs.players[id].PlayerBody.Position, gs.GameBall.Posistion, proposedThrow, gs.players[id].PlayerBody.Velocity, Constants.footLen + Constants.BallRadius - Unit), inputs[id]);
                            //toAssign = toAssign.Where(x => x.Key != id).ToList();

                            var foot = proposedThrow.NewScaled(1.0 / Constants.maxThrowPower);
                            hasBallInputs.FootX = foot.x;
                            hasBallInputs.FootY = foot.y;
                            goto next;
                        }

                        // boot it
                        if (ourSpace < 1 * Unit)
                        {
                            var punts = throwOffsets.Value
                                .Where(dir => dir.Length > 0)
                                .Select(dir =>
                                {
                                    var proposedThrow = dir.NewUnitized().NewScaled(Constants.maxThrowPower);
                                    var howLongToCatch = gameState.players.Where(x => x.Key != owner).Select(x =>
                                        {
                                            var (time, _) = PlayerInputApplyer.IntersectBallTime(x.Value.PlayerBody.Position, gameState.GameBall.Posistion, proposedThrow, x.Value.PlayerBody.Velocity, Constants.footLen + Constants.BallRadius - Unit);

                                            if (time < PlayerInputApplyer.HowLongCanIBoost(gameState.players[x.Key].Boosts))
                                            {
                                                return time;
                                            }

                                            return PlayerInputApplyer.IntersectBallTime(x.Value.PlayerBody.Position, gameState.GameBall.Posistion, proposedThrow, x.Value.PlayerBody.Velocity, Constants.footLen + Constants.BallRadius).Item1;
                                        })
                                        .Min();
                                    return (proposedThrow, value: EvaluatePass(proposedThrow.NewScaled(howLongToCatch).NewAdded(gameState.GameBall.Posistion)), howLongToCatch);
                                })
                                .Where(pair => DontThrowAtGoal(pair.howLongToCatch, pair.proposedThrow))
                                .OrderByDescending(x => x.value)
                            .ToArray();

                            if (punts.Any())
                            {
                                var (proposedThrow, _, _) = punts.First();

                                hasBallInputs.Throw = true;

                                var foot = proposedThrow.NewUnitized();
                                hasBallInputs.FootX = foot.x;
                                hasBallInputs.FootY = foot.y;
                                goto next;
                            }

                        }

                    }
                next:

                    OffenseNotBall(toAssign, inputs);
                }
                else  // they have the ball
                {
                    Defense(team.ToArray(), inputs);
                }
            }
            else if (gameState.CountDownState.Countdown || gameState.players.All(x => x.Value.LastHadBall == 0 || x.Value.LastHadBall + 90 < gameState.Frame))// no one has the ball
            {
                var hot = CatchBall(inputs, Array.Empty<Guid>());

                UpForGrabs(team.Where(x => x.Key != hot.Key).ToArray(), inputs);
            }
            else if (team.ContainsKey(gameState.players.OrderByDescending(x => x.Value.LastHadBall).Select(x => x.Key).First()))
            {
                var lastHadBall = gameState.players.OrderByDescending(y => y.Value.LastHadBall).First().Key;
                var hot = CatchBall(inputs, new[] { lastHadBall });

                OffenseNotBall(team.Where(x => x.Key != hot.Key).ToList(), inputs);
            }
            else
            {
                var hot = CatchBall(inputs, Array.Empty<Guid>());

                Defense(team.Where(x => x.Key != hot.Key).ToArray(), inputs);
            }



            foreach (var member in inputs)
            {
                if (member.Value.Boost == Constants.NoMove && member.Value.FootX == 0 && member.Value.FootY == 0)
                {
                    var myFoot = gameState.players[member.Key].PlayerFoot.Position;
                    var myBody = gameState.players[member.Key].PlayerBody.Position;
                    var reset = myBody.NewAdded(myFoot.NewMinus()).NewScaled(.02);
                    member.Value.FootX = reset.x;
                    member.Value.FootY = reset.y;
                }

                //AI boosts in it's direction of travel
                //var move = new Vector(member.Value.BodyX, member.Value.BodyY);
                //if (!member.Value.Throw && move.Length > 0)
                //{
                //    move = move.NewUnitized().NewScaled(Constants.footLen * 2);

                //    var myFoot = gameState.players[member.Key].PlayerFoot.Position;
                //    var myBody = gameState.players[member.Key].PlayerBody.Position;

                //    var pos = myBody.NewAdded(move.NewUnitized().NewScaled(Constants.footLen + Unit));

                //    var posNoBoost = myBody.NewAdded(move.NewUnitized().NewScaled(Constants.footLen));

                //    member.Value.FootX = move.x;
                //    member.Value.FootY = move.y;
                //    if (GlobalEvaluateFoot(pos, myBody, member.Key) < GlobalEvaluateFoot(posNoBoost, myBody, member.Key) + Unit * 2)
                //    {
                //        member.Value.Boost = Constants.NoMove;
                //    }
                //    else if (member.Value.Boost == Constants.NoMove)
                //    {
                //        member.Value.Boost = Guid.NewGuid();
                //    }

                //}
                //else
                //{
                //    member.Value.Boost = Constants.NoMove;
                //}
                team[member.Key].nextInputs.Add(new InputsWithCondition
                {
                    frame = gameState.Frame + playerLag,
                    playerInputs = member.Value
                });
            }
        }

        private KeyValuePair<Guid, AITeamMember> CatchBall(Dictionary<Guid, PlayerInputs> inputs, Guid[] except)
        {

            var dash = team
                .Where(x => !except.Contains(x.Key))
                .Select(x =>
                {
                    var playerPos = gameState.players[x.Key].PlayerFoot.Position;
                    var (howLongToCatch, catchAt) = PlayerInputApplyer.IntersectBallTime(playerPos, gameState.GameBall.Posistion, gameState.GameBall.Velocity, gameState.players[x.Key].PlayerBody.Velocity, Constants.footLen + Constants.BallRadius - Unit, true);
                    return (x, howLongToCatch);
                })
                .Where(x => x.howLongToCatch < PlayerInputApplyer.HowLongCanIBoost(gameState.players[x.x.Key].Boosts))
                .OrderBy(x => x.howLongToCatch)
                .ToArray();

            if (dash.Any())
            {
                var dashHot = dash.First().x;

                UpdateDirection(
                    dashHot.Value,
                    (GameState gs) => PlayerInputApplyer.IntersectBallDirection(gs.players[dashHot.Key].PlayerBody.Position, gs.GameBall.Posistion, gs.GameBall.Velocity, gs.players[dashHot.Key].PlayerBody.Velocity, Constants.footLen + Constants.BallRadius - Unit, true),
                    inputs[dashHot.Key],
                    boost: true);
                return dashHot;
            }


            var hot = team
                .Where(x => !except.Contains(x.Key))
                .Select(x =>
                {
                    var playerPos = gameState.players[x.Key].PlayerFoot.Position;
                    var (howLongToCatch, catchAt) = PlayerInputApplyer.IntersectBallTime(playerPos, gameState.GameBall.Posistion, gameState.GameBall.Velocity, gameState.players[x.Key].PlayerBody.Velocity, Constants.footLen + Constants.BallRadius - Unit);
                    return (x, howLongToCatch);
                }).OrderBy(x => x.howLongToCatch)
            .First().x;

            UpdateDirection(hot.Value, (GameState gs) => PlayerInputApplyer.IntersectBallDirection(gs.players[hot.Key].PlayerBody.Position, gs.GameBall.Posistion, gs.GameBall.Velocity, gs.players[hot.Key].PlayerBody.Velocity, Constants.footLen + Constants.BallRadius - Unit), inputs[hot.Key]);
            return hot;
        }

        private bool DontThrowAtGoal(double howLongToCatch, Vector proposedThrow)
        {
            if (proposedThrow.Length == 0)
            {
                return true;
            }
            // take the normal to the throw
            var throwNormal = new Vector(proposedThrow.y, -proposedThrow.x).NewUnitized();
            // consider the our goals possition in those coordinates
            var goalPositionInNormal = throwNormal.Dot(GoalTheyScoreOn());
            // consider the balls current position in those coordinates
            var ballPositionInNormal = throwNormal.Dot(gameState.GameBall.Posistion);
            // are they less then good lenght apart?
            var normalDis = Math.Abs(goalPositionInNormal - ballPositionInNormal);
            if (normalDis > Constants.goalLen + Constants.BallRadius)
            {
                return true;
            }
            // how long will it take to reach the goal?
            var parallelDis = proposedThrow.NewUnitized().Dot(GoalTheyScoreOn().NewAdded(gameState.GameBall.Posistion.NewMinus()));
            if (parallelDis < 0)
            {
                return true; // you are throwing away from the gaol
            }
            parallelDis += Math.Sqrt(Math.Pow(Constants.goalLen + Constants.BallRadius, 2) - Math.Pow(normalDis, 2));
            return howLongToCatch + 10 < PlayerInputApplyer.HowLongItTakesBallToGo(parallelDis, proposedThrow.Length);
        }

        private double Space(Vector pos) => gameState.players.Where(y => !team.ContainsKey(y.Key)).Select(x => x.Value.PlayerBody.Position.NewAdded(pos.NewMinus()).Length).Union(new[] { 1_000_000.0 }).Min();

        private static Vector RandomVector()
        {
            var rads = r.NextDouble() * Math.PI * 2.0;
            var res = new Vector(Math.Sin(rads), Math.Cos(rads));
            return res;
        }

        // why are these lazy?! they are always going to be initialzed
        private Lazy<Vector[]> footOffsets = new Lazy<Vector[]>(() =>
        {
            return new int[25]
            .Select(_ => RandomVector().NewScaled(Unit * 5 * r.NextDouble()))
            .ToArray();
        });

        private (Vector, bool) GenerateDirectionFoot(Vector myfoot, Vector myBody, Guid self)
        {

            var list = footOffsets.Value
                .Select(x => myfoot.NewAdded(x))
                .Union(new[] { myBody, myfoot })
                .Union(gameState.players.Select(x => x.Value.PlayerFoot.Position).ToArray()) // towards players
                .Union(gameState.players.Select(x => x.Value.PlayerFoot.Position.NewAdded(myfoot.NewMinus()).NewMinus().NewAdded(myfoot))) // away from players
                .Union(new[] { gameState.GameBall.Posistion })
                .Where(pos => pos.x < fieldDimensions.xMax && pos.x > 0 && pos.y < fieldDimensions.yMax && pos.y > 0)
                .Select(pos => (position: pos, score: GlobalEvaluateFoot(pos, myBody, self)))
                .OrderByDescending(pair => pair.score)
                .ToArray();

            if (list.Any())
            {

                var direction = list.First().position.NewAdded(myfoot.NewMinus());

                return (direction, list.First().score > GlobalEvaluateFoot(myfoot, myBody, self) + (Unit));
            }

            return (new Vector(0, 0), false);

        }

        public double GlobalEvaluateFoot(Vector proposedPos, Vector myBody, Guid self)
        {
            var res = 0.0;

            var snapshot = gameState.GameBall.OwnerOrNull;
            if (gameState.CountDownState.Countdown)
            {
                // lean away from the ball
                res -= TowardsWithIn(proposedPos, gameState.GameBall.Posistion, .4, (Constants.footLen * 2) + Constants.ballWallLen);
            }
            else if (snapshot == self) // when you have the ball
            {
                // don't go near the other team
                foreach (var player in gameState.players.Where(x => !team.ContainsKey(x.Key)))
                {
                    res -= TowardsWithInBody(myBody, proposedPos, player.Value.PlayerFoot.Position, 4, Unit * 2);
                }

                // go to the goal
                res += TowardsWithInBody(myBody, proposedPos, GoalWeScoreOn(), 1, PlayerInputApplyer.HowFarCanIBoost(gameState.players[self].Boosts) - Unit);
            }
            else if ((snapshot is Guid owner))// when no one has the ball
            {
                if (team.ContainsKey(owner)) // one of you teammates has the ball
                {
                }
                else // the other team has the ball
                {
                    res += TowardsWithInBody(myBody, proposedPos, gameState.GameBall.Posistion
                    .NewAdded(gameState.GameBall.Velocity.NewScaled(Math.Min(10, gameState.GameBall.Posistion.NewAdded(myBody.NewMinus()).Length / Constants.speedLimit)))
                    , 10, PlayerInputApplyer.HowFarCanIBoost(gameState.players[self].Boosts) - Unit);

                    // towards the ball
                    res += TowardsWithIn(proposedPos, gameState.GameBall.Posistion, .4, Constants.footLen * 5);
                }
            }
            else if (team.ContainsKey(gameState.players.OrderByDescending(x => x.Value.LastHadBall).Select(x => x.Key).First()))
            {
                res += TowardsWithInBody(myBody, proposedPos, gameState.GameBall.Posistion
                .NewAdded(gameState.GameBall.Velocity.NewScaled(Math.Min(10, gameState.GameBall.Posistion.NewAdded(myBody.NewMinus()).Length / Constants.speedLimit)))
                , 10, PlayerInputApplyer.HowFarCanIBoost(gameState.players[self].Boosts) - Unit);


                // towards the ball
                res += TowardsWithIn(proposedPos, gameState.GameBall.Posistion, .4, Constants.footLen * 5);
            }
            else
            {
                // towards the ball
                res += TowardsWithIn(proposedPos, gameState.GameBall.Posistion, .4, Constants.footLen * 5);
            }

            // a small force in the direction of motion
            var myPlayer = gameState.players[self];
            if (myPlayer.PlayerBody.Velocity.Length > 0)
            {
                res += Towards(proposedPos, myBody.NewAdded(myPlayer.PlayerBody.Velocity.NewUnitized().NewScaled(Constants.footLen - Constants.PlayerRadius)), .1);
            }
            else
            {
                res += Towards(proposedPos, myPlayer.PlayerFoot.Position, .1);
            }
            //res += Towards(proposedPos, myBody, .1);

            res -= StayInBounds(proposedPos, Unit * 10);

            return res;
        }

        private double StayInBounds(Vector pos, double ponishment)
        {
            if (pos.x > fieldDimensions.xMax || pos.x < 0 || pos.y > fieldDimensions.yMax || pos.y < 0)
            {
                return ponishment;
            }
            return 0;
        }

        //private double TowardsPathInBody(Vector myBody, Vector toEval, Vector ball, Vector velocity, int scale, double whenWithIn)
        //{

        //    var startWith = myBody.NewAdded(ball.NewMinus());
        //    var len = startWith.Length;
        //    if (len > 0 && len < whenWithIn && velocity.Length > 0)
        //    {
        //        var toBall = ball.NewAdded(toEval.NewMinus());
        //        if (velocity.Dot(toBall) > 0) {
        //            return -whenWithIn * scale;
        //        }
        //        return -Math.Abs(new Vector(velocity.y, -velocity.x).NewUnitized().Dot(toBall)) * scale;
        //    }
        //    return -whenWithIn * scale;
        //}

        private void OffenseNotBall(List<KeyValuePair<Guid, AITeamMember>> toAssign, Dictionary<Guid, PlayerInputs> inputs)
        {
            if (toAssign.Count > 1)
            {
                // if we are their end we have a dump
                if (gameState.GameBall.Posistion.NewAdded(GoalTheyScoreOn().NewMinus()).Length > gameState.GameBall.Posistion.NewAdded(GoalWeScoreOn().NewMinus()).Length)
                {
                    var dump =
                    toAssign
                        .Select(pair => (pair, value: gameState.players[pair.Key].PlayerBody.Position.NewAdded(GoalTheyScoreOn().NewMinus()).Length))
                        .OrderBy(x => x.value)
                        .First()
                        .pair;

                    toAssign.Remove(dump);

                    Cutters(toAssign, inputs, new[] { dump.Key });

                    GetDump(dump, inputs);
                }
                // otherwise we have a goalie
                else
                {
                    toAssign = Goalie(toAssign.ToArray(), inputs).ToList();

                    Cutters(toAssign, inputs, Array.Empty<Guid>());
                }
            }
        }

        //private ConcurrentIndexed<Guid, int> order = new ConcurrentIndexed<Guid, int>();
        //private int frameOrderCreated = 0;
        //private int Order(Guid key) {
        //    if (gameState.Frame > frameOrderCreated + (60*5)) {
        //        order = new ConcurrentIndexed<Guid, int>();
        //        frameOrderCreated = gameState.Frame;
        //    }
        //    // I don't trust random anymore
        //    // I am not sure it is thread safe 
        //    // it seemed to like returning zeros
        //    return order.GetOrAdd(key,()=> new Random().Next(int.MinValue,int.MaxValue));
        //}
        //Dictionary<int, HashSet<KeyValuePair<Guid, AITeamMember>>> lastGrouped = null;

        private List<KeyValuePair<Guid, AITeamMember>> Ordered(List<KeyValuePair<Guid, AITeamMember>> toAssign)
        {
            //if (lastGrouped == null || !SetEqual(lastGrouped.SelectMany(x => x.Value), toAssign))
            //{
                var offset = (gameState.Frame / (60 * 8) ) % toAssign.Count;

                var ordered = toAssign
                    .OrderBy(x => x.Key.GetHashCode())
                    .ToArray();

            var list = new List<KeyValuePair<Guid, AITeamMember>>();

            foreach (var item in ordered.Skip(offset))
            {
                list.Add(item);
            }

            foreach (var item in ordered.Take(offset))
            {
                list.Add(item);
            }

            return list;


            //if (offset) {
            //    ordered = ordered.Reverse().ToArray();
            //}



            //lastGrouped =
            //return new Dictionary<int, HashSet<KeyValuePair<Guid, AITeamMember>>>
            //{
            //    { 0, ordered.Take(ordered.Length/3).ToHashSet() },
            //    { 1, ordered.Skip(ordered.Length/3).Take( ((ordered.Length*2)/3) - (ordered.Length/3)).ToHashSet() },
            //    { 2, ordered.Skip((ordered.Length*2)/3).ToHashSet() }
            //};
            //}

            //return lastGrouped;

            //var list = toAssign
            //    .Select(player =>
            //    {

            //        var currentPos = gameState.players[player.Key].PlayerBody.Position;

            //        var baseline = PurePositionCutEvaluator(currentPos, player.Key);
            //        return (player, baseline);
            //    })
            //    .OrderByDescending(x => x.baseline)
            //    .Select(x=>x.player)
            //    .ToArray();


            //return new List<HashSet<KeyValuePair<Guid, AITeamMember>>>
            //{
            //    toAssign.Where(x=> (x.Key.GetHashCode() +  offset) % 3 == 0).ToHashSet(),
            //    toAssign.Where(x=> (x.Key.GetHashCode() +  offset) % 3 == 1).ToHashSet(),
            //    toAssign.Where(x=> (x.Key.GetHashCode() +  offset) % 3 == 2).ToHashSet(),
            //};

        }

        private bool SetEqual(IEnumerable<KeyValuePair<Guid, AITeamMember>> a, IEnumerable<KeyValuePair<Guid, AITeamMember>> b)
        {
            return a.Count() == b.Count() && (a.Except(b).Count() == 0);
        }

        private void Cutters(List<KeyValuePair<Guid, AITeamMember>> toAssign, Dictionary<Guid, PlayerInputs> inputs, Guid[] dontStayAwayFrom)
        {
            //var ballToGoal= GoalWeScoreOn().NewAdded(gameState.GameBall.Posistion.NewMinus());
            //if (ballToGoal.Length > 0) {
            //    ballToGoal = ballToGoal.NewUnitized().NewScaled(Unit * 10);
            //}
            //var primeSpot = gameState.GameBall.Posistion.NewAdded(ballToGoal);
            //toAssign = toAssign
            //.OrderByDescending(x=> Order(x.Key))
            //.OrderByDescending(x => gameState.players[x.Key].PlayerBody.Position.NewAdded(primeSpot.NewMinus()).Length)
            //.ToList();

            //var first = true;

            var ordered = Ordered(toAssign);

            if (ordered.Any())
            {
                gameState.debugs.Add(new ThrowAt { Id = Guid.NewGuid(), Target = gameState.players[ordered.First().Key].PlayerFoot.Position, Frame = gameState.Frame });
            }


            while (toAssign.Any())
            {
                var player = toAssign.First();

                var list = new List<Guid>();
                var foundUs = false;
                foreach (var item in ordered)
                {
                    if (item.Key == player.Key) {
                        foundUs = true;
                        continue;
                    }

                    if (foundUs) {
                        list.Add(item.Key);
                    }
                }

                GetNewCuttingTowards(
                    player,
                    inputs,
                    team.Select(x => x.Key)
                        .Except(list.ToArray())
                        .Except(dontStayAwayFrom)
                        .ToArray());

                toAssign.RemoveAt(0);
            }
        }

        private void GetDump(KeyValuePair<Guid, AITeamMember> player, Dictionary<Guid, PlayerInputs> inputs)
        {
            var currentPos = gameState.players[player.Key].PlayerBody.Position;


            var baseline = DumpEvaluator(currentPos, player.Key);

            var cutterGenerators = cutOffsets.Value
                .Select<Vector, Func<GameState, Vector>>(x => gs => currentPos.NewAdded(x).NewAdded(gs.players[player.Key].PlayerBody.Position.NewMinus()))
                // don't try to cut somewhere outside the room
                .Where(generator =>
                {
                    var loc = currentPos.NewAdded(generator(gameState));
                    return loc.x > 0 && loc.x < fieldDimensions.xMax && loc.y > 0 && loc.y < fieldDimensions.yMax;
                })
                .Select(generator => (generator: generator, score: DumpEvaluator(currentPos.NewAdded(generator(gameState)), player.Key)))
                .OrderByDescending(pair => pair.score)
                .ToList();

            if (cutterGenerators.Any())
            {
                var first = cutterGenerators.First();
                UpdateDirection(player.Value, first.generator, inputs[player.Key], baseline + OffsetLen < first.score);
            }
            else
            {
                UpdateDirection(player.Value, (GameState gs) => new Vector(0.0, 0.0), inputs[player.Key]);
            }
        }

        private void GetNewCuttingTowards(KeyValuePair<Guid, AITeamMember> player, Dictionary<Guid, PlayerInputs> inputs, Guid[] stayAwayFrom)
        {
            var currentPos = gameState.players[player.Key].PlayerBody.Position;

            var baseline = CutEvaluator(currentPos, player.Key, stayAwayFrom);

            var cutterGenerators = cutOffsets.Value
                .Select<Vector, Func<GameState, Vector>>(x => gs => currentPos.NewAdded(x).NewAdded(gs.players[player.Key].PlayerBody.Position.NewMinus()))
                // don't try to cut somewhere outside the room
                .Where(generator =>
                {
                    var loc = currentPos.NewAdded(generator(gameState));
                    return loc.x > 0 && loc.x < fieldDimensions.xMax && loc.y > 0 && loc.y < fieldDimensions.yMax;
                })
                .Select(generator => (generator: generator, score: CutEvaluator(currentPos.NewAdded(generator(gameState)), player.Key, stayAwayFrom))) // we evaluate what it looks like locally
                .OrderByDescending(pair => pair.score)
                .ToList();

            var input = inputs[player.Key];

            var currentPathDirection = new Vector(input.BodyX, input.BodyY);
            if (currentPathDirection.Length > 0)
            {
                currentPathDirection = currentPathDirection.NewUnitized().NewScaled(OffsetLen);
            }

            var currentPathScore = CutEvaluator(currentPos.NewAdded(currentPathDirection), player.Key, stayAwayFrom);//, reallyStayAwayFrom, chase

            if (cutterGenerators.Any())
            {
                var first = cutterGenerators.First();
                //if (new Random().Next(0,100) == 0) /*first.score > currentPathScore + (OffsetLen * .5)*/
                //{
                UpdateDirection(player.Value, first.generator, input, baseline + OffsetLen < first.score);
                //}
                //else {
                //UpdateDirection(player.Value, gs => gs.players[player.Key].PlayerBody.Velocity, input, false ) ;
                //}
            }
            else
            {
                UpdateDirection(player.Value, (GameState gs) => new Vector(0.0, 0.0), input);
            }
        }

        private void UpForGrabs(KeyValuePair<Guid, AITeamMember>[] toAssign, Dictionary<Guid, PlayerInputs> inputs)
        {
            toAssign = Goalie(toAssign, inputs);
            //if (toAssign.Any())
            //{
            //    toAssign = GetTheBall(toAssign, inputs);
            //}
            foreach (var player in toAssign)
            {
                var currentPos = gameState.players[player.Key].PlayerBody.Position;
                var myPlayer = player;
                var cutterGenerators = cutOffsets.Value
                    .Select<Vector, Func<GameState, Vector>>(x => gs => currentPos.NewAdded(x).NewAdded(gs.players[player.Key].PlayerBody.Position.NewMinus()))
                    .Select(generator => (generator: generator, score: UpForGrabsEvaluator(currentPos.NewAdded(generator(gameState)), player.Key)))
                    .OrderByDescending(pair => pair.score)
                    .ToList();

                if (cutterGenerators.Any())
                {
                    UpdateDirection(player.Value, cutterGenerators.First().generator, inputs[player.Key]);
                }
                else
                {
                    UpdateDirection(player.Value, (GameState gs) => new Vector(0.0, 0.0), inputs[player.Key]);
                }
            }
        }

        private double UpForGrabsEvaluator(Vector myPosition, Guid self)
        {

            var res = 0.0;

            // stay away from your teammates
            foreach (var player in gameState.players.Where(x => team.ContainsKey(x.Key) && x.Key != self))
            {
                res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, .3, Unit * 12);
            }

            // don't get too close to the other teams players
            foreach (var player in gameState.players.Where(x => !team.ContainsKey(x.Key)))
            {
                var dissToTheirEnd =
                gameState.players[player.Key].PlayerBody.Position.NewAdded(GoalWeScoreOn().NewMinus()).Length;
                var dissToOurEnd =
                gameState.players[player.Key].PlayerBody.Position.NewAdded(GoalTheyScoreOn().NewMinus()).Length;


                res += TowardsWithIn(myPosition, player.Value.PlayerBody.Position, (dissToTheirEnd - dissToOurEnd) / (10 * Unit), Unit * 6);
                res += TowardsWithIn(myPosition, player.Value.PlayerBody.Position, (dissToTheirEnd - dissToOurEnd) / (10 * Unit), Unit * 1);
            }

            // don't get too far from the ball
            res -= AwayWithOut(myPosition, gameState.GameBall.Posistion, 2, Unit * 10);

            return res;
        }

        private void Defense(KeyValuePair<Guid, AITeamMember>[] toAssign, Dictionary<Guid, PlayerInputs> inputs)
        {

            toAssign = Goalie(toAssign, inputs);
            //if (toAssign.Any())
            //{
            //    toAssign = GetTheBall(toAssign.ToArray(), inputs);
            //}

            // if you need to get the ball, get the ball
            if (toAssign.Any() && !gameState.GameBall.OwnerOrNull.HasValue)
            {
                toAssign = GetTheBall(toAssign.ToArray(), inputs);
            }

            foreach (var (baddie, _, _) in gameState.players.Values
                .Where(x => !team.ContainsKey(x.Id))
                .Select(x => (x, x.PlayerBody.Position.NewAdded(GoalTheyScoreOn().NewMinus()).Length, hasBall: x.Id == gameState.GameBall.OwnerOrNull ? 1 : 0))
                .OrderByDescending(x => x.hasBall)
                .ThenBy(x => x.Length))
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

                toAssign = toAssign.Except(new[] { getTheBaddie.pair }).ToArray();

                var currentPos = gameState.players[getTheBaddie.pair.Key].PlayerBody.Position;
                var baseline = GuardPlayerEvaluator(baddie.Id, currentPos, gameState.players[getTheBaddie.pair.Key].Boosts);

                var guardGenerators = GetPositionGenerators(getTheBaddie.pair.Key)
                    .Select(pos => (generator: pos.generator, score: GuardPlayerEvaluator(baddie.Id, pos.pos, gameState.players[getTheBaddie.pair.Key].Boosts ), pos: pos.pos))
                    .OrderByDescending(pair => pair.score)
                    .ToList();

                if (guardGenerators.Any())
                {
                    var first = guardGenerators.First();
                    UpdateDirection(getTheBaddie.pair.Value, first.generator, inputs[getTheBaddie.pair.Key], baseline + currentPos.NewAdded(first.pos.NewMinus()).Length < first.score);
                }
                else
                {
                    UpdateDirection(getTheBaddie.pair.Value, (GameState gs) => new Vector(0.0, 0.0), inputs[getTheBaddie.pair.Key]);
                }
            }

        }

        private KeyValuePair<Guid, AITeamMember>[] GetTheBall(KeyValuePair<Guid, AITeamMember>[] toAssign, Dictionary<Guid, PlayerInputs> inputs)
        {
            var getTheBall = toAssign
               .Select(pair => (pair, gameState.players[pair.Key].PlayerBody.Position.NewAdded(gameState.GameBall.Posistion.NewMinus()).Length))
               .OrderBy(pair => pair.Length)
               .First();


            var currentPos = gameState.players[getTheBall.pair.Key].PlayerBody.Position;
            var baseline = GetTheBallEvaluator(currentPos);

            var getTheBallGenerators = GetPositionGenerators(getTheBall.pair.Key)
                .Select(pos => (generator: pos.generator, score: GetTheBallEvaluator(pos.pos), pos: pos.pos))
                .OrderByDescending(pair => pair.score)
                .ToList();

            if (getTheBallGenerators.Any())
            {
                var first = getTheBallGenerators.First();
                UpdateDirection(getTheBall.pair.Value, first.generator, inputs[getTheBall.pair.Key], baseline + first.pos.NewAdded(currentPos.NewMinus()).Length < first.score);
            }
            else
            {
                UpdateDirection(getTheBall.pair.Value, (GameState gs) => new Vector(0.0, 0.0), inputs[getTheBall.pair.Key]);
            }

            return toAssign.Except(new[] { getTheBall.pair }).ToArray();
        }

        private KeyValuePair<Guid, AITeamMember>[] Goalie(KeyValuePair<Guid, AITeamMember>[] toAssign, Dictionary<Guid, PlayerInputs> inputs)
        {
            var goalie = toAssign
                .Select(pair => (pair, gameState.players[pair.Key].PlayerBody.Position.NewAdded(GoalTheyScoreOn().NewMinus()).Length))
                .OrderBy(pair => pair.Length)
                .First();

            var currentPos = gameState.players[goalie.pair.Key].PlayerBody.Position;
            var baseline = GoalieEvaluator(currentPos, goalie.pair.Key);

            var goalieGenerators = GetPositionGenerators(goalie.pair.Key)
                .Select(pos => new { generator = pos.generator, score = GoalieEvaluator(pos.pos, goalie.pair.Key), pos = pos.pos })
                .OrderByDescending(pair => pair.score)
                .ToList();

            if (goalieGenerators.Any())
            {
                var first = goalieGenerators.First();
                UpdateDirection(goalie.pair.Value, first.generator, inputs[goalie.pair.Key], baseline + currentPos.NewAdded(first.pos.NewMinus()).Length < first.score);
            }
            else
            {
                UpdateDirection(goalie.pair.Value, (GameState gs) => new Vector(0.0, 0.0), inputs[goalie.pair.Key]);
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

        private void UpdateDirection(AITeamMember goalie, Func<GameState, Vector> generator, PlayerInputs inputs, bool boost = false)
        {
            goalie.generator = generator;
            var concreteTarget = generator(gameState);
            if (Double.IsNaN(concreteTarget.Length))
            {
                var aahhhh = 0;
            }

            if (concreteTarget.Length > 0)
            {
                concreteTarget = concreteTarget.NewUnitized();
            }
            //if (concreteTarget.Length == 0) {
            //    var db = 0; 
            //}

            inputs.BodyX = concreteTarget.x;
            inputs.BodyY = concreteTarget.y;

            if (boost && concreteTarget.Length > 0)
            {
                var boostVector = concreteTarget.NewScaled(1);
                if (boostVector.Length > 0)
                {
                    boostVector = boostVector.NewUnitized().NewScaled(Constants.speedLimit);
                }
                inputs.FootX = boostVector.x;
                inputs.FootY = boostVector.y;

                if (inputs.Boost == Constants.NoMove)
                {
                    inputs.Boost = Guid.NewGuid();
                }
            }
            else
            {
                inputs.Boost = Constants.NoMove;
            }
        }

        class GenAndPos
        {
            public Func<GameState, Vector> generator;
            public Vector pos;
        }

        class SimpleGenAndPos : GenAndPos
        {
            public Vector dir;
            public SimpleGenAndPos(Vector dir, Vector myPosition)
            {
                this.dir = new Vector(dir.x, dir.y);
                this.pos = myPosition.NewAdded(dir);
                this.generator = _ => this.dir;
            }
        }

        private Vector[] directions = new int[100].Select(x => RandomVector().NewScaled(Constants.goalLen * r.NextDouble())).ToArray();

        private List<(Func<GameState, Vector> generator, Vector pos)> GetPositionGenerators(Guid self)
        {
            var myPosition = gameState.players[self].PlayerBody.Position;

            var getOtherPlayers = gameState.players
                .Where(x => !team.ContainsKey(x.Key))
                .Select(x =>
                {
                    var localX = x;
                    return (Func<GameState, Vector>)((GameState gs) => gs.players[localX.Key].PlayerBody.Position.NewAdded(gs.players[self].PlayerBody.Position.NewMinus()));
                });

            var getBall = new[] {
                (Func<GameState, Vector>)((GameState gs) => gs.GameBall.Posistion.NewAdded(gs.players[self].PlayerBody.Position.NewMinus()))
            };

            var getGoalie = new[] {
                (Func<GameState, Vector>)((GameState gs) => gs.GameBall.Posistion.NewAdded(GoalTheyScoreOn()).NewScaled(.5).NewAdded(gs.players[self].PlayerBody.Position.NewMinus()))
            };

            var random = directions
                .Select(dir =>
                {
                    // something weird with closures here
                    // sometimes these all ending returning the same thing
                    // but only sometimes
                    var x = dir;
                    return (Func<GameState, Vector>)(_ => x);
                })
                .ToArray();
            //var random = new List<GenAndPos>();
            //foreach (var dir in directions)
            //{
            //    random.Add(new SimpleGenAndPos (dir, myPosition));
            //}

            var res = random
                .Union(getBall)
                .Union(getOtherPlayers)
                .Union(getGoalie)
                .Select(generator =>
                {
                    var dir = generator(gameState);
                    if (dir.Length > 0 && dir.Length < Unit)
                    {

                        dir = dir.NewUnitized().NewScaled(Unit);
                    }
                    else if (dir.Length > Unit)
                    {
                        dir = dir.NewUnitized().NewScaled(Unit);
                    }
                    var prospect = myPosition.NewAdded(dir);

                    return (generator: generator, pos: prospect);
                })
                .Where(pair => pair.pos.x < fieldDimensions.xMax && pair.pos.x > 0 && pair.pos.y < fieldDimensions.yMax && pair.pos.y > 0)
                .ToList();

            return res;
        }


        private double GoalieEvaluator(Vector position, Guid self)
        {

            var res = 0.0;

            if (gameState.GameBall.OwnerOrNull is Guid owner && team.ContainsKey(owner))
            {
                // if your team has the ball don't go for it
                res -= TowardsWithIn(position, gameState.GameBall.Posistion, 1, Unit * 4);
            }
            else
            {

                // if you are the closest by a unit just grab the ball
                if (gameState.players.Where(x => x.Key != self).Select(x => x.Value.PlayerBody.Position.NewAdded(gameState.GameBall.Posistion.NewMinus()).Length).OrderBy(x => x).First()
                    > gameState.players[self].PlayerBody.Position.NewAdded(gameState.GameBall.Posistion.NewMinus()).Length + Unit)
                {
                    return Towards(position, gameState.GameBall.Posistion, 1);
                }
            }

            // TODO if there are several players from the other team near the goal you need to player closer to the gaol
            // if there are fewer you can rush the player


            res += Towards(position, GoalTheyScoreOn(), .9);
            res += Towards(position, gameState.GameBall.Posistion, .8);
            //res += TowardsWithIn(position, gameState.GameBall.Posistion, 3, Constants.goalLen * 1);


            res -= TowardsWithIn(position, GoalTheyScoreOn(), 10, Constants.goalLen * 2);

            foreach (var player in gameState.players.Where(x => !team.ContainsKey(x.Key)))
            {
                res += Towards(position, player.Value.PlayerBody.Position, .1);
            }

            // if we are in count down don't go too close to the ball
            if (gameState.CountDownState.Countdown)
            {
                res -= TowardsWithIn(position, gameState.GameBall.Posistion, 1, Unit * 15);
            }

            if (!gameState.CountDownState.Countdown && gameState.GameBall.OwnerOrNull == null)
            {
                res += Towards(position, GoalTheyScoreOn(), .75);
            }


            res -= AwayWithOut(position, gameState.GameBall.Posistion, 1, Unit * 15);
            //foreach (var player in gameState.players.Where(x => team.ContainsKey(x.Key)))
            //{
            //    res -= TowardsWithIn(position, player.Value.PlayerBody.Position, 1, Unit* 5);
            //}

            // the more opponents are near the goal the more we want to be near the goal
            //foreach (var player in gameState.players.Where(x => !team.ContainsKey(x.Key)))
            //{
            //    if (player.Value.PlayerBody.Position.NewAdded(GoalTheyScoreOn().NewMinus()).Length < Constants.goalLen * 6)
            //    {
            //        res += Towards(position, GoalTheyScoreOn(), 1);
            //    }
            //}

            res -= StayInBounds(position, Unit * 10);

            return res;

            //return Towards(position, gameState.GameBall.Posistion.NewAdded(GoalTheyScoreOn()).NewScaled(.5), 4);
        }




        // I get it 
        // when a play is in a sandwich
        // the op in front and the op behind cancel
        // and they just run towards the other goal
        private double HasBallEvaluator(Vector position)
        {
            var res = 0.0;
            // don't go near the other team
            foreach (var player in gameState.players.Where(x => !team.ContainsKey(x.Key)))
            {
                res -= TowardsWithIn(position, player.Value.PlayerBody.Position, .25, Unit * 8);
                res -= TowardsWithIn(position, player.Value.PlayerBody.Position, 1, Unit * 5);
            }


            res += Towards(position, GoalWeScoreOn(), .25);

            var lenHome = gameState.GameBall.Posistion.NewAdded(GoalTheyScoreOn().NewMinus()).Length;
            var lenGoal = gameState.GameBall.Posistion.NewAdded(GoalWeScoreOn().NewMinus()).Length;
            if (lenHome > lenGoal)
            {
                // dont go away from the goal
                res -= AwayWithOut(position, GoalWeScoreOn(), .75, lenGoal + 1000);
                // score if you can
                res += TowardsWithIn(position, GoalWeScoreOn(), 2.5, Constants.goalLen);
            }
            else
            {
                // don't go towards your goal
                res -= TowardsWithIn(position, GoalTheyScoreOn(), .75, lenHome - 500);
                // really don't self goal
                res -= TowardsWithIn(position, GoalTheyScoreOn(), 1.5, Constants.goalLen + Unit * 3);
            }

            res -= StayInBounds(position, Unit * 10);

            return res;
        }

        private double DumpEvaluator(Vector myPosition, Guid self)
        {

            var res = 0.0;

            // stay away from your teammates
            foreach (var player in gameState.players.Where(x => team.ContainsKey(x.Key) && x.Key != self))
            {
                res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, .3, Unit * 12);
            }

            // don't get too close to the other teams players
            foreach (var player in gameState.players.Where(x => !team.ContainsKey(x.Key)))
            {
                res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, .1, Unit * 6);
                res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, .3, Unit * 1);
            }

            // don't get too far from the ball
            res -= AwayWithOut(myPosition, gameState.GameBall.Posistion, 2, Unit * 18);

            // we like to be in the line between our goal and the ball
            // we are going to fall back to being the goalie
            // and they can take long shots at goal
            res += Towards(myPosition, gameState.GameBall.Posistion, .1);
            res += Towards(myPosition, GoalTheyScoreOn(), .1);
            // if the ball is in the air we are risk of loosing control double down on getting ready to goalie
            if (gameState.GameBall.OwnerOrNull == null)
            {
                res += Towards(myPosition, gameState.GameBall.Posistion, .1);
                res += Towards(myPosition, GoalTheyScoreOn(), .1);
            }


            //res -= Towards1D(myPosition.y, fieldDimensions.yMax * 1.0 / 3.0, 1);
            //res -= Towards1D(myPosition.y, fieldDimensions.yMax * 2.0 / 3.0, 1);
            res -= Towards1D(myPosition.x, gameState.GameBall.Posistion.x - (1 * Unit * Math.Sign(GoalTheyScoreOn().x) - gameState.GameBall.Posistion.x), .1);
            res -= Towards1D(myPosition.x, gameState.GameBall.Posistion.x + (7 * Unit * Math.Sign(GoalTheyScoreOn().x) - gameState.GameBall.Posistion.x), .1);

            res -= StayInBounds(myPosition, Unit * 10);

            return res;
        }


        //private double PurePositionCutEvaluator(Vector myPosition, Guid self)
        //{
        //    var res = 0.0;


        //    // don't be behind the ball
        //    if (BehindBall(self))
        //    {
        //    }
        //    else
        //    {

        //    }
        //    return res;


        //}

        private double CutEvaluator(Vector myPosition, Guid self, Guid[] stayAwayFrom)//, Guid[] reallyStayAwayFro, Guid[] chase
        {
            var res = 0.0;



            // don't be behind the ball
            if (BehindBall(self))
            {
               res += Towards(myPosition, GoalWeScoreOn(), 1);//.NewAdded(gameState.GameBall.Posistion).NewScaled(.5)
            }
            else
            {


                var primeSpot = gameState.GameBall.Posistion;
                var badSpot = gameState.GameBall.Posistion;

                var toAdd = GoalWeScoreOn().NewAdded(gameState.GameBall.Posistion.NewMinus());
                if (toAdd.Length > 0)
                {
                    toAdd = toAdd.NewUnitized().NewScaled(Unit * 15);
                    primeSpot = primeSpot.NewAdded(toAdd);
                    badSpot = badSpot.NewAdded(toAdd.NewUnitized().NewScaled(-4 * Unit));
                }

                res += Towards(myPosition, primeSpot, .5);

                //res -= AwayWithOut(myPosition, gameState.GameBall.Posistion, .1, Unit * 10);
                //res -= TowardsWithIn(myPosition, gameState.GameBall.Posistion, .1, Unit * 10);
                //res += Towards(myPosition, GoalWeScoreOn(), .1);

                res -= TowardsWithIn(myPosition, badSpot, .7, Unit * 6);

                // stay away from your teammates
                foreach (var player in gameState.players.Where(x => stayAwayFrom.Contains(x.Key) && x.Key != self))
                {
                    res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, .4, Unit * 14);
                    res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position.NewAdded(player.Value.PlayerBody.Velocity.NewScaled(playerLag + 10)), .4, Unit * 14);
                }

                //foreach (var player in gameState.players.Where(x => reallyStayAwayFro.Contains(x.Key) && x.Key != self))
                //{
                //    res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, .5, Unit * 12);
                //    res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position.NewAdded(player.Value.PlayerBody.Velocity.NewScaled(playerLag + 10)), .5, Unit * 12);
                //}


                //foreach (var player in gameState.players.Where(x => chase.Contains(x.Key) && x.Key != self))
                //{
                //    res += TowardsWithIn(myPosition, player.Value.PlayerBody.Position, .2, Unit * 22);
                //}

                //foreach (var player in gameState.players.Where(x => team.ContainsKey(x.Key)))
                //{
                //    res += Towards(myPosition, player.Value.PlayerBody.Position, .05);
                //}

                // don't get too close to the other teams players
                foreach (var player in gameState.players.Where(x => !team.ContainsKey(x.Key)))
                {
                    res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, .3, Unit * 3);
                    //res -= TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 3, Unit * .5);
                }

                // don't get too far from the ball
                //res -= AwayWithOut(myPosition, primeSpot, .5, Unit * 8);
                //res -= AwayWithOut(myPosition, primeSpot, 1, Unit * 16);

                //// don't get too close to the ball
                //res -= TowardsWithIn(myPosition, gameState.GameBall.Posistion, 1, Unit * 6);

                // don't run away from the ball, it's impossible to pass to you
                //var diff = myPosition.NewAdded(gameState.players[self].PlayerBody.Position.NewMinus());
                //if (diff.Length > 0) {
                //    res -= Math.Max(0, diff.NewUnitized().Dot(gameState.players[self].PlayerBody.Position.NewAdded(gameState.GameBall.Posistion.NewMinus()))-.5) * 1000;
                //}
            }

            res -= StayInBounds(myPosition, Unit * 10);

            return res;
        }

        private bool BehindBall(Guid self)
        {
            return gameState.players[self].PlayerBody.Position.NewAdded(GoalWeScoreOn().NewMinus()).Length + (Unit * 4) > Math.Max(gameState.GameBall.Posistion.NewAdded(GoalWeScoreOn().NewMinus()).Length, Unit * 10);
        }

        private Lazy<Vector[]> throwOffsets = new Lazy<Vector[]>(() =>
        {
            // this isn't a good random for a circle. it perfers pie/4 to pie/2
            return new int[50].Select(_ => RandomVector().NewScaled(Unit * 6 * r.NextDouble())).ToArray();
        });

        const double OffsetLen = 20_000;
        private Lazy<Vector[]> cutOffsets = new Lazy<Vector[]>(() =>
        {
            // this isn't a good random for a circle. it perfers pie/4 to pie/2
            return new int[100].Select(_ => RandomVector().NewScaled(OffsetLen)).ToArray();
        });


        private Lazy<Vector[]> goalOffsets = new Lazy<Vector[]>(() =>
        {
            // this isn't a good random for a circle. it perfers pie/4 to pie/2
            return new int[10].Select(_ => RandomVector().NewScaled(Constants.goalLen * .9)).ToArray();
        });

        //private int startedThrowing;

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

            var res = 0.0;

            res += TowardsWithIn(position, gameState.GameBall.Posistion.NewAdded(gameState.GameBall.Velocity.NewScaled(playerLag + 10)), 1, Unit);
            res += Towards(position, gameState.GameBall.Posistion.NewAdded(gameState.GameBall.Velocity.NewScaled(playerLag + 10)), .5);

            //res += Towards(position, GoalTheyScoreOn(), 2);

            //res -= AwayWithOut(position, GoalTheyScoreOn(), 1, Unit * 30);

            res -= StayInBounds(position, Unit * 10);

            return res;
        }

        private double GuardPlayerEvaluator(Guid playerId, Vector position, double boost)
        {
            var res = 0.0;

            if (gameState.players.TryGetValue(playerId, out var player))
            {
                var expectedLocation = player.PlayerBody.Position.NewAdded(player.PlayerBody.Velocity.NewScaled(playerLag + 10));

                if (gameState.GameBall.OwnerOrNull == playerId && 
                    PlayerInputApplyer.HowFarCanIBoost(boost) < position.NewAdded(expectedLocation.NewMinus()).Length) {

                    res += Towards(position, expectedLocation, 1);
                }

                res += Towards(position, expectedLocation, .5);

                res += Towards(position, GoalTheyScoreOn(), .25);

                res -= AwayWithOut(position, GoalTheyScoreOn(), .125, Unit * 40);
            }

            res -= StayInBounds(position, Unit * 10);

            return res;
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

        private double Towards1D(double at, double target, double scale)
        {
            return -Math.Abs(at - target) * scale;
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


        private double AwayWithOut(Vector us, Vector them, double scale, double whenWithOut)
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
