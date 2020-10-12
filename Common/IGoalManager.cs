namespace physics2
{
    // why would you interface this?
    public interface IGoalManager
    {
        bool IsEnabled();
        IEvent GetGoalEvent(double time, Collision collision);
    }
}