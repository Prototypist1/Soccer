using Common;

namespace RemoteSoccer
{
    interface IZoomer
    {
        double GetTimes();
        void SetTimes(double v);
        (double, double, double, double) Update(Position[] positionsList);
        void UpdateWindow(double actualWidth, double actualHeight);
    }
}