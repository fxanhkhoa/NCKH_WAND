using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using OxyPlot;
using OxyPlot.Series;
using System.ComponentModel;
using System.Windows.Threading;
using System.Threading;

namespace NCKH_WAND
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly BackgroundWorker Worker_refresh = new BackgroundWorker();
        private DispatcherTimer timer = new DispatcherTimer();
        private Controller _Controller;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Serial_Add_Element()
        {
            List<string> temp = new List<string>();

            // Get All PortName
            temp = GlobalVar.Serial_Data.Get_PORT();
            combo_box_COM.ItemsSource = temp;
            //combo_box_COM.SelectedIndex = 0;

            //Get BaudRate
            temp = GlobalVar.Serial_Data.Get_Baud();
            combo_box_BaudRate.ItemsSource = temp;
            combo_box_BaudRate.SelectedIndex = 2;

            //Get Databits
            temp = GlobalVar.Serial_Data.Get_Databits();
            combo_box_Databits.ItemsSource = temp;
            combo_box_Databits.SelectedIndex = 1;

            //Get Parity
            temp = GlobalVar.Serial_Data.Get_Parity();
            combo_box_Parity.ItemsSource = temp;
            combo_box_Parity.SelectedIndex = 0;

            //Get Stopbit
            temp = GlobalVar.Serial_Data.Get_Stopbit();
            combo_box_Stopbit.ItemsSource = temp;
            combo_box_Stopbit.SelectedIndex = 1;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GlobalVar.Serial_Data = new Serial_Data();
            _Controller = new Controller(true);
            Serial_Add_Element();
            Worker_refresh.DoWork += Worker_refresh_Dowork;
            Worker_refresh.RunWorkerAsync();

            timer.Tick += timer_Tick;
            timer.Interval = TimeSpan.FromMilliseconds(200);
            timer.Start();

        }

        private void btn_Serial_Connect_Click(object sender, RoutedEventArgs e)
        {
            string comName, Baud, Databits, Parity, Stopbit;
            Boolean OK;

            try
            {
                comName = combo_box_COM.SelectedItem.ToString();
                Baud = combo_box_BaudRate.SelectedItem.ToString();
                Databits = combo_box_Databits.SelectedItem.ToString();
                Parity = combo_box_Parity.SelectedItem.ToString();
                Stopbit = combo_box_Stopbit.SelectedItem.ToString();

                OK = GlobalVar.Serial_Data.connect(comName, Baud, Databits, Parity, Stopbit);
                if (OK)
                {
                    ProgressBar_Connection_Status.Value = 50;

                }
                else
                {
                    ProgressBar_Connection_Status.Value = 0;
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void btn_Serial_Refresh_Click(object sender, RoutedEventArgs e)
        {
            Serial_Add_Element();
            Plot1.InvalidatePlot(true);
        }

        private void Worker_refresh_Dowork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (GlobalVar.OK)
                {
                    Dispatcher.BeginInvoke((Action)(() => aaa.Text = GlobalVar.Serial_Data.GetData()));
                    Dispatcher.BeginInvoke((Action)(() => Speed.Text = GlobalVar.speedX.ToString() + " " + GlobalVar.speedY.ToString()));
                    Dispatcher.BeginInvoke((Action)(() => Center.Text = _Controller.Center_Pitch.ToString() + " " + _Controller.Center_Yaw.ToString()));
                }
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            Plot1.InvalidatePlot(true);
        }

        private void btn_BLE_Connect_Click(object sender, RoutedEventArgs e)
        {
            GlobalVar.Serial_Data.SendData("AT+COND43639BC1EA7");
            Thread.Sleep(2000);
            ProgressBar_Connection_Status.Value = 100;
        }
    }
}
