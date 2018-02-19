using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Windows;
using System.Threading;

namespace NCKH_WAND
{
    class Serial_Data: MainWindow
    {
        string[] Baud_Rate = {"2400", "4800", "9600", "14400", "19200", "38400", "56000", "57600", "115200"};
        string[] Databits = { "7", "8", "9" };
        string[] Parity = {"None", "Even", "Odd"};
        string[] Stopbit = {"0", "1", "2"};

        const int ReceivedBytesThreshold = 12;

        SerialPort _Sp = new SerialPort();
        static string _DataRead = "";

        public Serial_Data()
        {
            //Thread th = new Thread(new ThreadStart(MyCallbackFunction));
            //th.Start();
        }

        private void MyCallbackFunction()
        {
            //while (true)
            //{
            //    if (_DataRead.Length > 0)
            //    Dispatcher.BeginInvoke((Action)(() => aaa.Text = _DataRead));
            //}
        }

        public List<string> Get_PORT()
        {
            List<string> temp = new List<string>();
            string[] ports = SerialPort.GetPortNames();
            temp.AddRange(ports);
            return temp;
        }

        public List<string> Get_Baud()
        {
            List<string> temp = new List<string>();
            temp.AddRange(Baud_Rate);
            return temp;
        }

        public List<string> Get_Databits()
        {
            List<string> temp = new List<string>();
            temp.AddRange(Databits);
            return temp;
        }

        public List<string> Get_Parity()
        {
            List<string> temp = new List<string>();
            temp.AddRange(Parity);
            return temp;
        }

        public List<string> Get_Stopbit()
        {
            List<string> temp = new List<string>();
            temp.AddRange(Stopbit);
            return temp;
        }

        public Boolean connect(string ComName, string Baud, string Databits, string Parity, string Stopbit)
        {
            try
            {
                if (!_Sp.IsOpen)
                {
                    _Sp.PortName = ComName;
                    _Sp.BaudRate = Convert.ToInt16(Baud);
                    _Sp.DataBits = Convert.ToInt16(Databits);

                    if (Parity == "None") _Sp.Parity = System.IO.Ports.Parity.None;
                    else if (Parity == "Even") _Sp.Parity = System.IO.Ports.Parity.Even;
                    else if (Parity == "Odd") _Sp.Parity = System.IO.Ports.Parity.Odd;

                    if (Stopbit == "0") _Sp.StopBits = StopBits.None;
                    else if (Stopbit == "1") _Sp.StopBits = StopBits.One;
                    else if (Stopbit == "2") _Sp.StopBits = StopBits.Two;

                    _Sp.Handshake = Handshake.None;
                    _Sp.ReceivedBytesThreshold = ReceivedBytesThreshold;

                    _Sp.DataReceived += new SerialDataReceivedEventHandler(SP_Received);
                    _Sp.Open();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
            return true;
        }

        private static void SP_Received(Object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort newsp = (SerialPort)sender;
            DataRead = newsp.ReadExisting();
            //_DataRead += _Sp.ReadChar();
            //GlobalVar.sem_data_received.Release();
            //if (_DataRead[_DataRead.Length - 1] == '*')
            GlobalVar.OK = true;
                //MessageBox.Show(_DataRead);
        }

        public string GetData()
        {
            return _DataRead;
        }

        public void SendData(string data)
        {
            _Sp.Write(data);
        }

        public static string DataRead
        {
            get { return _DataRead; }
            set { _DataRead = value; }
        }
    }
}
