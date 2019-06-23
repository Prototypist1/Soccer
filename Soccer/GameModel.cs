using Physics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Soccer
{
    public class GameModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private double frameNumber = 0;
        private PhyisEngineInbetween engine;

        public void Start(Canvas canvas) {
            engine = new PhyisEngineInbetween(100, 2000, 2000, canvas);

            var r = new Random();

            foreach (var x in new[] { 300,400,500,600,700,800,900,1000})
            {
                foreach (var y in new[] {   300, 400, 500,600,700, 800, 900, 1000 })
                {
                    engine.AddItem(1, 40, x, y, r.NextDouble()*2 -1, r.NextDouble() * 2 - 1);
                }
            }

            PrivateStart();
        }

        public PhysicsObjectInbetween[] PhysicsItems { get; private set; } =  new PhysicsObjectInbetween[] {};
        public string Message { get; private set; } = "";

        private async void PrivateStart()
        {
            await Task.Delay(500);
            var sw = new Stopwatch();
            sw.Start();
            var rand = new Random();
            while(true)
            {

                await Task.Delay(1);
                frameNumber++;

                engine.Update();
                Message = $"fps: {frameNumber / sw.Elapsed.TotalSeconds}";

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Message)));
            }

            
        }
    }
}