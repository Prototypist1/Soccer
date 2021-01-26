using Common;
using Physics2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteSoccer
{
    // this should really be in Common
    class AIInputs : IInputs
    {
        GameState gameState;
        private readonly Guid self;
        private readonly Guid[] teammates;

        public FieldDimensions fieldDimensions;

        public AIInputs(GameState gameState, Guid self, Guid[] teammates, FieldDimensions fieldDimensions)
        {
            this.gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
            this.self = self;
            this.teammates = teammates;
            this.fieldDimensions = fieldDimensions;
        }

        public Task Init() => Task.CompletedTask;

        bool needsNewDirection = true;

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
            if (needsNewDirection || r.NextDouble() < (1 / 20.0))
            {
                needsNewDirection = false;
                lastDirection = GenerateDirection();
            }
            inputs.BodyX = lastDirection.x;
            inputs.BodyY = lastDirection.y;

            lastDirectionFoot = GenerateDirectionFoot();
            if (lastDirectionFoot.Length > 0)
            {
                lastDirectionFoot = lastDirectionFoot.NewUnitized().NewScaled(100);
            }
            inputs.FootX = lastDirectionFoot.x;
            inputs.FootY = lastDirectionFoot.y;

            if (throwing < 30 && ShouldThrow(out var direction))
            {
                inputs.Throwing = true;
                inputs.FootX = direction.x;
                inputs.FootY = direction.y;
                throwing++;
            }
            else {
                throwing = 0;
            }

            return Task<PlayerInputs>.FromResult(inputs);
        }

        private bool ShouldThrow(out Vector direction)
        {
            // you have to have the ball to throw
            if (gameState.GameBall.OwnerOrNull != self)
            {
                direction = default;
                return false;
            }
            var myPosition = gameState.players[self].PlayerBody.Position;

            var r = new Random();

            // consider taking a shot on goal
            var clear = new int[] { 10 }
                .Select(_ => gameState.LeftGoal.Posistion.NewAdded(new Vector((1 - (2 * r.NextDouble())), (1 - (2 * r.NextDouble()))).NewUnitized().NewScaled(Constants.goalLen * r.NextDouble())))
                .Where(x => CanPass(x))
                .Select(x => x.NewAdded(myPosition.NewMinus()).NewUnitized())
                .ToArray();

            if (clear.Any()) 
            {
                direction = clear[0];
                return true;
            }
            
            // consider passing
            var options = gameState.players
                .Where(x => teammates.Contains(x.Key))
                .Where(x=>CanPass(x.Value.PlayerBody.Position))
                .Select(teamie => (
                    direction: Scale(ref teamie, myPosition),
                    positionValue: Evaluate(teamie.Value.PlayerBody.Position)))
                .OrderByDescending(x => x.positionValue)
                .ToArray();

            if (options.Any() && options[0].positionValue > Evaluate(myPosition) + 1000)
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

        private double Evaluate(Vector position)
        {
            var myPosition = gameState.players[self].PlayerBody.Position;
            var sum = 0.0;

            // bad to be far from goal
            sum -= gameState.LeftGoal.Posistion.NewAdded(position).Length;

            // bad to be too close to us
            if (position.NewAdded(myPosition.NewMinus()).Length > 0)
            {
                sum -= Cone(position.NewAdded(myPosition.NewMinus()).Length, 2, Constants.footLen * 2);
            }

            // bad to be too far
            sum -= AntiCone(position.NewAdded(myPosition.NewMinus()).Length, .1, Constants.footLen * 10);

            foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
            {
                // bad to be near the badie
                sum -= Cone(position.NewAdded(player.Value.PlayerFoot.Position.NewMinus()).Length, 2, Constants.footLen * 2);
            }

            return sum;
        }

        private bool CanPass(Vector target)
        {
            var myPosition = gameState.players[self].PlayerFoot.Position;

            var passDirection = target.NewAdded(myPosition.NewMinus());
            foreach (var obstical in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
            {
                var obsticalDirection = obstical.Value.PlayerBody.Position.NewAdded(myPosition.NewMinus());
                if (passDirection.NewUnitized().Dot(obsticalDirection.NewUnitized()) > .9 && passDirection.Length > obsticalDirection.Length)
                {
                    return false;
                }
            }
            // don't pass through your own gaol
            {
                var obsticalDirection = gameState.RightGoal.Posistion.NewAdded(myPosition.NewMinus());
                if (passDirection.NewUnitized().Dot(obsticalDirection.NewUnitized()) > .9 && passDirection.Length > obsticalDirection.Length)
                {
                    return false;
                }
            }

            return true;
        }



        private Vector GenerateDirection()
        {
            var myPosition = gameState.players[self].PlayerBody.Position;


            var direction = GlobalEvaluate(myPosition);
            if (direction.Length != 0)
            {
                direction = direction.NewUnitized().NewScaled(100);
            }

            for (int i = 0; i < 100; i++)
            {
                myPosition = myPosition.NewAdded(direction);
                direction = GlobalEvaluate(myPosition);
                if (direction.Length != 0)
                {
                    direction = direction.NewUnitized().NewScaled(100);
                }
            }

            direction = myPosition.NewAdded(gameState.players[self].PlayerBody.Position.NewMinus());

            if (direction.Length != 0)
            {
                direction = direction.NewUnitized();
            }

            return direction;
        }

        private Vector GenerateDirectionFoot()
        {
            var myPosition = gameState.players[self].PlayerFoot.Position;


            var direction = GlobalEvaluateFoot(myPosition);
            if (direction.Length != 0)
            {
                direction = direction.NewUnitized().NewScaled(100);
            }

            for (int i = 0; i < 100; i++)
            {
                myPosition = myPosition.NewAdded(direction);
                direction = GlobalEvaluateFoot(myPosition);
                if (direction.Length != 0)
                {
                    direction = direction.NewUnitized().NewScaled(100);
                }
            }

            direction = myPosition.NewAdded(gameState.players[self].PlayerFoot.Position.NewMinus());

            if (direction.Length != 0)
            {
                direction = direction.NewUnitized();
            }

            return direction;
        }

        // positive is good 
        public Vector GlobalEvaluate(Vector myPosition)
        {
            var res = new Vector(0,0);

            var hasBall = gameState.GameBall.OwnerOrNull == self;

            if (gameState.GameBall.OwnerOrNull == self) // when you have the ball
            {
                // don't go near the other team
                foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                {
                    res = res.NewAdded(TowardsWithIn(myPosition, player.Value.PlayerBody.Position, -6, Constants.footLen * 3));
                }

                // go to the goal
                res = res.NewAdded(Towards(myPosition, gameState.LeftGoal.Posistion, 1));
            }
            else if (gameState.GameBall.OwnerOrNull == null)// when no one has the ball
            {
                // go towards the ball
                res = res.NewAdded(Towards(myPosition, gameState.GameBall.Posistion, 2));

                // really gotards the ball if you are close to it
                res = res.NewAdded(TowardsWithIn(myPosition, gameState.GameBall.Posistion, 10, Constants.footLen * 4));

                // spread out
                foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key)))
                {
                    res = res.NewAdded(TowardsWithIn(myPosition, player.Value.PlayerBody.Position, -1, Constants.footLen * 6));
                }
            }
            else if (teammates.Contains((Guid)gameState.GameBall.OwnerOrNull)) // one of you teammates has the ball
            {
                // go towards the goal
                res = res.NewAdded(Towards(myPosition, gameState.LeftGoal.Posistion, 1));

                // stay away from your teammates
                foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key)))
                {
                    res = res.NewAdded(TowardsWithIn(myPosition, player.Value.PlayerBody.Position, -1, Constants.footLen * 6));
                }

                // don't get to close to the other teams players
                foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                {
                    res.NewAdded(TowardsWithIn(myPosition, player.Value.PlayerBody.Position, -1, Constants.footLen * 2));
                }
            } else { // the other team has the ball

                // go towards your goal
                res = res.NewAdded(Towards(myPosition, gameState.RightGoal.Posistion, 1));

                // stay away from your teammates
                foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key)))
                {
                    res = res.NewAdded(TowardsWithIn(myPosition, player.Value.PlayerBody.Position, -2, Constants.footLen * 5));
                }

                // go towards the ball
                res = res.NewAdded(Towards(myPosition, gameState.GameBall.Posistion, 1));
                // go towards the ball hard if you are close
                res = res.NewAdded(TowardsWithIn(myPosition, gameState.GameBall.Posistion, 10, Constants.footLen * 4));

                // go towards players of the other team
                foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                {
                    res = res.NewAdded(TowardsWithIn(myPosition, player.Value.PlayerBody.Position, 2, Constants.footLen * 3));
                }

            }

            // this is mostly just annoying
            // stay away from edges
            //res = res.NewAdded(TowardsXWithIn(myPosition, new Vector(0, 0), -1, Constants.footLen));
            //res = res.NewAdded(TowardsXWithIn(myPosition, new Vector(fieldDimensions.xMax, 0), -1, Constants.footLen));

            //res = res.NewAdded(TowardsYWithIn(myPosition, new Vector(0, 0), -1, Constants.footLen));
            //res = res.NewAdded(TowardsYWithIn(myPosition, new Vector(0, fieldDimensions.yMax), -1, Constants.footLen));

            return res;
        }

        public Vector GlobalEvaluateFoot(Vector myPosition)
        {
            var res = new Vector(0, 0);

            var hasBall = gameState.GameBall.OwnerOrNull == self;

            if (gameState.GameBall.OwnerOrNull == self) // when you have the ball
            {
                // don't go near the other team
                foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                {
                    res = res.NewAdded(TowardsWithIn(myPosition, player.Value.PlayerFoot.Position, -4, Constants.footLen * 2));
                }

                // go to the goal
                res = res.NewAdded(TowardsWithIn(myPosition, gameState.LeftGoal.Posistion, 1, Constants.footLen * 2));
            }
            else if (gameState.GameBall.OwnerOrNull == null)// when no one has the ball
            {
                // go towards the ball
                res = res.NewAdded(TowardsWithIn(myPosition, gameState.GameBall.Posistion, 1, Constants.footLen * 2));

            }
            else if (teammates.Contains((Guid)gameState.GameBall.OwnerOrNull)) // one of you teammates has the ball
            {

                // stay away from your teammates
                foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key)))
                {
                    res = res.NewAdded(TowardsWithIn(myPosition, player.Value.PlayerFoot.Position, -1, Constants.footLen ));
                }

                // don't get to close to the other teams players
                foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                {
                    res.NewAdded(TowardsWithIn(myPosition, player.Value.PlayerFoot.Position, -1, Constants.footLen * 2));
                }
            }
            else
            { // the other team has the ball

                // go towards your goal
                res = res.NewAdded(TowardsWithIn(myPosition, gameState.GameBall.Posistion, 2, Constants.footLen * 2));

                // stay away from your teammates
                foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key)))
                {
                    res = res.NewAdded(TowardsWithIn(myPosition, player.Value.PlayerFoot.Position, -1, Constants.footLen *2));
                }

                // go towards the ball hard if you are close
                res = res.NewAdded(TowardsWithIn(myPosition, gameState.GameBall.Posistion, 10, Constants.footLen * 2));

                // go towards players of the other team
                foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key) && x.Key != self))
                {
                    res = res.NewAdded(TowardsWithIn(myPosition, player.Value.PlayerFoot.Position, 2, Constants.footLen * 2));
                }
            }

            // a small force back towards the center
            res = res.NewAdded(Towards(myPosition, gameState.players[self].PlayerBody.Position, .1));

            // this is mostly just annoying
            // stay away from edges
            //res = res.NewAdded(TowardsXWithIn(myPosition, new Vector(0, 0), -1, Constants.footLen));
            //res = res.NewAdded(TowardsXWithIn(myPosition, new Vector(fieldDimensions.xMax, 0), -1, Constants.footLen));

            //res = res.NewAdded(TowardsYWithIn(myPosition, new Vector(0, 0), -1, Constants.footLen));
            //res = res.NewAdded(TowardsYWithIn(myPosition, new Vector(0, fieldDimensions.yMax), -1, Constants.footLen));

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

        private Vector Towards(Vector us, Vector them, double scale) {
            var startWith = them.NewAdded(us.NewMinus());
            if (startWith.Length > 0) {
                return startWith.NewScaled(scale);
            }
            return new Vector(0, 0);
        }

        private Vector TowardsWithIn(Vector us, Vector them, double scale, double whenWithIn)
        {
            var startWith = them.NewAdded(us.NewMinus());
            var len = startWith.Length;
            if (len > 0 && len < whenWithIn)
            {
                return startWith.NewUnitized().NewScaled(scale* (whenWithIn - len));
            }
            return new Vector(0, 0);
        }

        private double Cone(double length, double scale, double radius)
        {
            if (length < radius) {
                return (radius - length)  * scale;
            }
            return 0;
        }

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
