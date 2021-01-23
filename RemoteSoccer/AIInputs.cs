using Common;
using Physics2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteSoccer
{
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
        Vector lastDirection;
        public Task<PlayerInputs> Next()
        {
            var r = new Random();
            if (needsNewDirection || r.NextDouble() < (1 / 30.0))
            {
                needsNewDirection = false;
                lastDirection = GenerateDirection();
            }
            return Task<PlayerInputs>.FromResult(new PlayerInputs
            {
                ControlScheme = ControlScheme.Controller,
                Boost = false,
                FootX = 0,
                FootY = 0,
                Id = self,
                Throwing = false,
                BodyX = lastDirection.x,
                BodyY = lastDirection.y
            });
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

        // positive is good 
        public Vector GlobalEvaluate(Vector myPosition)
        {
            var res = new Vector(0,0);

            var hasBall = gameState.GameBall.OwnerOrNull == self;

            if (gameState.GameBall.OwnerOrNull == self) // when you have the ball
            {
                // don't go near the other team
                foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key)))
                {
                    res = res.NewAdded(TowardsWithIn(myPosition, player.Value.PlayerFoot.Position, -  4, Constants.footLen * 2));
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
                    res = res.NewAdded(TowardsWithIn(myPosition, player.Value.PlayerFoot.Position, -1, Constants.footLen * 6));
                }
            }
            else if (teammates.Contains((Guid)gameState.GameBall.OwnerOrNull)) // one of you teammates has the ball
            {
                // go towards the goal
                res = res.NewAdded(Towards(myPosition, gameState.LeftGoal.Posistion, 1));

                // stay away from your teammates
                foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key)))
                {
                    res = res.NewAdded(TowardsWithIn(myPosition, player.Value.PlayerFoot.Position, -1, Constants.footLen * 6));
                }

                // don't get to close to the other teams players
                foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key)))
                {
                    res.NewAdded(TowardsWithIn(myPosition, player.Value.PlayerFoot.Position, -4, Constants.footLen * 2));
                }
            } else { // the other team has the ball

                // go towards your goal
                res = res.NewAdded(Towards(myPosition, gameState.RightGoal.Posistion, 1));

                // stay away from your teammates
                foreach (var player in gameState.players.Where(x => teammates.Contains(x.Key)))
                {
                    res = res.NewAdded(TowardsWithIn(myPosition, player.Value.PlayerFoot.Position, -2, Constants.footLen * 5));
                }

                // go towards the ball
                res = res.NewAdded(Towards(myPosition, gameState.GameBall.Posistion, 1));
                // go towards the ball hard if you are close
                res = res.NewAdded(TowardsWithIn(myPosition, gameState.GameBall.Posistion, 10, Constants.footLen * 4));

                // go towards players of the other team
                foreach (var player in gameState.players.Where(x => !teammates.Contains(x.Key)))
                {
                    res = res.NewAdded(TowardsWithIn(myPosition, player.Value.PlayerFoot.Position, 2, Constants.footLen * 3));
                }

            }

            // this is mostly just annoying
            // stay away from edges
            res = res.NewAdded(TowardsXWithIn(myPosition, new Vector(0,0), -1, Constants.footLen));
            res = res.NewAdded(TowardsXWithIn(myPosition, new Vector(fieldDimensions.xMax, 0), -1, Constants.footLen));

            res = res.NewAdded(TowardsYWithIn(myPosition, new Vector(0, 0), -1, Constants.footLen));
            res = res.NewAdded(TowardsYWithIn(myPosition, new Vector(0, fieldDimensions.yMax), -1, Constants.footLen));

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
                return startWith.NewUnitized().NewScaled(scale);
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

        private double Cone(double length, double peak, double radius)
        {
            if (length < radius) {
                return ((radius - length) / radius) * peak;
            }
            return 0;
        }

        private double Bell(double length, double peak, double width)
        {
            return peak * Math.Pow(Math.E, -(length*length) / (2*width*width));
        }
    }
}
