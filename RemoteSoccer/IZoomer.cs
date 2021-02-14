using Common;
using System;

namespace RemoteSoccer
{
    interface IZoomer
    {
        double GetTimes();
        void SetTimes(double v);
        void SetBallId(Guid guid);
        void UpdateWindow(double actualWidth, double actualHeight);
        (double, double, double, double) Update(GameState gameState);
    }
}