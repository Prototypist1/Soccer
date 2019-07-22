namespace RemoteSoccer
{
    public interface IScaler
    {
        double Scale(double diameter);
        double ScaleX(double x);
        double ScaleY(double y);

        double UnScaleX(double x);
        double UnScaleY(double y);
        double UnScale(double diameter);
    }
}