using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Threading;

namespace NCKH_WAND
{
    class Controller
    {
        int pitch = 0, roll = 0, yaw = 0;
        const int standby_radius = 5;
        public int Center_Pitch, Center_Roll, Center_Yaw;

        DispatcherTimer SpeedX = new DispatcherTimer();
        DispatcherTimer SpeedY = new DispatcherTimer();

        win32.POINT pos = new win32.POINT();

        Serial_Data _SData = new Serial_Data();
        const int Interval = 30;
        const double ratio = 1.5;
        const int
            stand = 0,
            up = 1,
            down = 2,
            left = 3,
            right = 5;

        const int
            type_UpDown = 1,
            type_LeftRight = 2,
            type_diagonal = 3;
                  
        /************************************************************
        *                  Import User32 & function
        ************************************************************/
        [DllImport("User32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("User32.dll")]
        static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);
        /************************************************************
        *                  Get Desktop
        ************************************************************/
        IntPtr desktop = GetDC(IntPtr.Zero);

        // Init Background Worker
        private readonly BackgroundWorker Worker_Data = new BackgroundWorker();

        public Controller(Boolean worker_on)
        {
            if (worker_on)
            {
                Worker_Data.DoWork += Worker_Data_DoWork;
                Worker_Data.RunWorkerAsync();
            }

            SpeedX.Tick += SpeedX_Tick;
            SpeedX.Interval = TimeSpan.FromMilliseconds(Interval);
            SpeedX.Start();

            SpeedY.Tick += SpeedY_Tick;
            SpeedY.Interval = TimeSpan.FromMilliseconds(Interval);
            SpeedY.Start();
        }

        private void SpeedX_Tick(object sender, EventArgs e)
        {
            int speed_temp = SpeedOfX(yaw);
            GlobalVar.speedX = speed_temp;
            if (speed_temp != 0)
            {
                win32.ClientToScreen(desktop, ref pos);
                win32.GetCursorPos(out pos);
                //if (speed_temp > 0)
                    win32.SetCursorPos(pos.x + (int)(speed_temp / ratio), pos.y);
                //else
                //    win32.SetCursorPos(pos.x - speed_temp, pos.y);
            }
        }

        private void SpeedY_Tick(object sender, EventArgs e)
        {
            int speed_temp = SpeedOfY(pitch);
            GlobalVar.speedY = speed_temp;
            if (speed_temp != 0)
            {
                //SpeedY.Interval = TimeSpan.FromMilliseconds(Interval - Math.Abs(speed_temp) * 3);
                win32.ClientToScreen(desktop, ref pos);
                win32.GetCursorPos(out pos);
                //if (speed_temp > 0)
                    win32.SetCursorPos(pos.x, pos.y + speed_temp);
                //else
                //    win32.SetCursorPos(pos.x, pos.y - 1);
            }
        }

        private void Worker_Data_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (GlobalVar.OK == true)
                {
                    /************************************************************
                    *Processing Data and Recognize Motion
                    ************************************************************/
                    if (CheckCalibration(_SData.GetData()))
                    {
                        Center_Pitch = pitch;
                        Center_Roll = roll;
                        Center_Yaw = yaw;
                        //MessageBox.Show(pitch.ToString());
                        GlobalVar.OK = false;
                        GlobalVar.DataOK = true;
                    }
                    else
                    {
                        /************************************************************
                        *Processing Data and Push to Chart
                        ************************************************************/
                        //Processing Data
                        try {
                            pitch = CalPitch(_SData.GetData());
                            roll = CalRoll(_SData.GetData());
                            yaw = CalYaw(_SData.GetData());
                        }
                        catch
                        {

                        }
                        //Push to Global Variable
                        GlobalVar.Pitch = pitch;
                        GlobalVar.Yaw = yaw;
                        //Notify data is done
                        GlobalVar.OK = false;
                        GlobalVar.DataOK = true;
                    }

                    /************************************************************
                    *Refresh the Interval
                    ************************************************************/

                }
            }
        }

        private int Find_Motion(int pitch, int roll, int yaw)
        {
            //use to detecting each motion ------->  True: yes.   False: no.
            Boolean tpitch = false, troll = false, tyaw = false;

            // Check Pitch
            if ((pitch < (Center_Pitch + standby_radius)) || (pitch > (Center_Pitch - standby_radius)))
            {
                tpitch = false;
            }
            else tpitch = true;

            // Check Roll
            if ((roll < (Center_Roll + standby_radius)) || (roll > (Center_Roll - standby_radius)))
            {
                troll = false;
            }
            else troll = true;

            // Check Yaw
            if ((yaw < (Center_Yaw + standby_radius)) || (yaw > (Center_Yaw - standby_radius)))
            {
                tyaw = false;
            }
            else tyaw = true;

            if ((tpitch = true) && (tyaw = false))
            {
                return type_UpDown;
            } 
            else if ((tpitch = false) && (tyaw = true))
            {
                return type_LeftRight;
            }
            return type_diagonal;
        }

        private int SpeedOfY(int pitch)
        {
            int rspeed = 0;
            if (pitch == 0) return 0;
            // Check Pitch
            if (((pitch < (Center_Pitch + standby_radius)) && (pitch > (Center_Pitch - standby_radius))) || (Center_Pitch == 0))
            {
                rspeed = 0;
            }
            else if ((pitch < (Center_Pitch - standby_radius)))
            {
                int temp = (Center_Pitch - standby_radius);
                rspeed = pitch - temp;
            }
            else if ((pitch > (Center_Pitch + standby_radius)))
            {
                int temp = (Center_Pitch + standby_radius);
                rspeed = pitch - temp;
            }
            return rspeed;
        }

        private int SpeedOfX(int yaw)
        {
            int rspeed = 0;
            if (yaw == 0) return 0;
            if (((yaw < (Center_Yaw + standby_radius)) && (yaw > (Center_Yaw - standby_radius))) || (Center_Yaw == 0))
            {
                rspeed = 0;
            }
            else if ((yaw < (Center_Yaw - standby_radius)))
            {
                int temp = (Center_Yaw - standby_radius);
                rspeed = yaw - temp;
            }
            else if ((yaw > (Center_Yaw + standby_radius)))
            {
                int temp = (Center_Yaw + standby_radius);
                rspeed = yaw - temp;
            }
            return rspeed;
        }

        private Boolean CheckCalibration(string data)
        {
            if (data.IndexOf("Calibration") >= 0)
            {
                return true;
            }
            return false;
        }

        private int CalPitch(string data)
        {
            string temp = data.Substring(0, 3);
            return Convert.ToInt16(temp);
        }

        private int CalRoll(string data)
        {
            string temp = data.Substring(4, 3);
            return Convert.ToInt16(temp);
        }

        private int CalYaw(string data)
        {
            string temp = data.Substring(8, 3);
            return Convert.ToInt16(temp);
        }

        public int GetPitch()
        {
            return pitch;
        }
        public int GetRoll()
        {
            return roll;
        }
        public int GetYaw()
        {
            return yaw;
        }
    }
}
