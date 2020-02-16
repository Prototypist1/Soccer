using Common;
using System;

namespace RemoteSoccer
{
    interface IZoomer
    {
        double GetTimes();
        void SetTimes(double v);
        void SetBallId(Guid guid);
        (double, double, double, double) Update(Position[] positionsList);
        void UpdateWindow(double actualWidth, double actualHeight);
    }
}