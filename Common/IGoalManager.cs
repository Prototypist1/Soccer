namespace physics2
{
    public interface IGoalManager
    {
        bool IsEnabled();
        IEvent GetGoalEvent(double time, Collision collision);
    }
}