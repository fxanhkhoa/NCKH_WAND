using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NCKH_WAND
{
    class GlobalVar
    {
        private static Semaphore _sem_data_received = new Semaphore(0, 1);
        private static Serial_Data _Serial_Data;
        private static Boolean _OK = false;
        private static Boolean _DataOK= false;
        private static int _Pitch;
        private static int _Yaw;
        private static int _speedX;
        private static int _speedY;
        

        public static Semaphore sem_data_received
        {
            get { return _sem_data_received; }
            set { _sem_data_received = value; }
        }

        public static Serial_Data Serial_Data
        {
            get { return _Serial_Data; }
            set { _Serial_Data = value; }
        }

        public static Boolean OK
        {
            get { return _OK; }
            set { _OK = value; }
        }

        public static Boolean DataOK
        {
            get { return _DataOK; }
            set { _DataOK = value; }
        }

        public static int Pitch
        {
            get { return _Pitch; }
            set { _Pitch = value; }
        }

        public static int Yaw
        {
            get { return _Yaw; }
            set { _Yaw = value; }
        }

        public static int speedX
        {
            get { return _speedX; }
            set { _speedX = value; }
        }

        public static int speedY
        {
            get { return _speedY; }
            set { _speedY = value; }
        }
    }
}
