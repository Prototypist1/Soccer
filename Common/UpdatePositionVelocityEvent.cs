using physics2;
using System;

namespace Physics2
{

    //internal readonly struct DropBallWrapper : IEvent
    //{
    //    private readonly IEvent evnt;
    //    private readonly Ball ball;
    //    private readonly int simulationTime;

    //    public DropBallWrapper(IEvent evnt, Ball ball, int simulationTime)
    //    {
    //        this.evnt = evnt ?? throw new ArgumentNullException(nameof(evnt));
    //        this.ball = ball ?? throw new ArgumentNullException(nameof(ball));
    //        this.simulationTime = simulationTime;
    //    }

    //    public double Time => evnt.Time;


    //    public MightBeCollision Enact()
    //    {
    //        ball.OwnerOrNull.LastHadBall = simulationTime;
    //        ball.OwnerOrNull = null;
    //        return evnt.Enact();
    //    }
    //}

    //internal readonly struct TakeBallWrapper : IEvent { 
    //    private readonly DoubleUpdatePositionVelocityEvent evnt;
    //    private readonly Ball ball;
    //    private readonly Player player;

    //    public TakeBallWrapper(DoubleUpdatePositionVelocityEvent evnt, Ball ball,Player player)
    //    {
    //        this.evnt = evnt;
    //        this.ball = ball ?? throw new ArgumentNullException(nameof(ball));
    //        this.player = player ?? throw new ArgumentNullException(nameof(player));
    //    }

    //    public double Time => evnt.Time;


    //    public MightBeCollision Enact()
    //    {
    //        ball.OwnerOrNull = player;
    //        return evnt.Enact();
    //    }
    //}

    //internal readonly struct DoubleUpdatePositionVelocityEvent : IEvent
    //{
    //    private readonly IPhysicsObject myPhysicsObject_1;
    //    private readonly double fx_1;
    //    private readonly double fy_1;

    //    private readonly IPhysicsObject myPhysicsObject_2;
    //    private readonly double fx_2;
    //    private readonly double fy_2;

    //    public readonly MightBeCollision res;

    //    public DoubleUpdatePositionVelocityEvent(
    //        double time,
    //        IPhysicsObject myPhysicsObject_1,
    //        double fx_1,
    //        double fy_1,
    //        IPhysicsObject myPhysicsObject_2,
    //        double fx_2,
    //        double fy_2,
    //        MightBeCollision res)
    //    {
    //        this.Time = time;

    //        this.myPhysicsObject_1 = myPhysicsObject_1;
    //        this.fx_1 = fx_1;
    //        this.fy_1 = fy_1;

    //        this.myPhysicsObject_2 = myPhysicsObject_2;
    //        this.fx_2 = fx_2;
    //        this.fy_2 = fy_2;

    //        this.res = res;
    //    }

    //    public double Time { get; }

    //    public MightBeCollision Enact()
    //    {
    //        myPhysicsObject_1.ApplyForce(fx_1, fy_1);
    //        myPhysicsObject_2.ApplyForce(fx_2, fy_2);

    //        return res;
    //    }
    //}


    //internal readonly struct UpdateOwnerEvent : IEvent
    //{
    //    private readonly Ball ball;
    //    private readonly Player owner;

    //    public UpdateOwnerEvent(Ball ball, double time, Player owner)
    //    {
    //        this.ball = ball;
    //        Time = time;
    //        this.owner = owner;
    //    }

    //    public double Time { get; }

    //    public MightBeCollision Enact()
    //    {
    //        ball.OwnerOrNull = owner;

    //        var dx = owner.X - ball.X;
    //        var dy = owner.Y - ball.Y;

    //        ball.UpdateVelocity(owner.Vx + dx,owner.Vy + dy);

    //        return new MightBeCollision();
    //    }
    //}


    //internal readonly struct UpdatePositionVelocityEvent : IEvent
    //{
    //    private readonly PhysicsObject myPhysicsObject;
    //    private readonly double vx;
    //    private readonly double vy;
    //    private readonly MightBeCollision res;

    //    public UpdatePositionVelocityEvent(double time, PhysicsObject myPhysicsObject, double x, double y, double vx, double vy, MightBeCollision res)
    //    {
    //        this.Time = time;
    //        this.myPhysicsObject = myPhysicsObject;
    //        this.vx = vx;
    //        this.vy = vy;
    //        this.res = res;
    //    }

    //    public double Time { get; }

    //    public MightBeCollision Enact()
    //    {
    //        myPhysicsObject.UpdateVelocity(vx, vy);

    //        return res;
    //    }
    //}
}