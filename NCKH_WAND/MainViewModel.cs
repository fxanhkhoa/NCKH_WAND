using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;

namespace NCKH_WAND
{
    class MainViewModel
    {
        private readonly BackgroundWorker Worker_Add_Point = new BackgroundWorker();
        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        UInt32 time = 0;
        public MainViewModel()
        {
            this.Title = "Example 2";
            Worker_Add_Point.DoWork += Worker_Add_Point_DoWork;
            //Worker_Add_Point.RunWorkerAsync();

            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(500);
            //dispatcherTimer.Start();
            //this.Points = new List<DataPoint>();
            this.Points = new List<DataPoint>
                              {
                                  new DataPoint(0,0)                                  
                              };
            this.Points2 = new List<DataPoint>
                              {
                                  new DataPoint(0,0)
                              };
            //this.Points2 = new List<DataPoint>
            //                  {
            //                      new DataPoint(0.5, 4.9),
            //                      new DataPoint(15, 12),
            //                      new DataPoint(21, 15),
            //                      new DataPoint(36, 16),
            //                      new DataPoint(47, 12),
            //                      new DataPoint(51, 12)
            //                  };
            //this.Points.Add(new DataPoint(5, 6));
        }

        private void Worker_Add_Point_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (GlobalVar.DataOK)
                {
                    this.Points.Add(new DataPoint(time, GlobalVar.Pitch));
                    this.Points2.Add(new DataPoint(time, GlobalVar.Yaw));
                    GlobalVar.DataOK = false;
                }
            }
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            time++;
            if (time == UInt32.MaxValue)
            {
                time = 0;

                this.Points.Clear();
                this.Points2.Clear();
            }
        }

        public string Title { get; private set; }
        public IList<DataPoint> Points { get; private set; }
        public IList<DataPoint> Points2 { get; private set; }
    }
}
