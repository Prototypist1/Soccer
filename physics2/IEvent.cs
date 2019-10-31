namespace physics2
{
    internal interface IEvent
    {
        double Time { get; }

        MightBeCollision Enact(double endtime);
    }

}
