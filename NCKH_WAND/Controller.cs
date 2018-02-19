using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace NCKH_WAND
{
    class Controller
    {
        int pitch = 0, roll = 0, yaw = 0;
        const int standby_radius = 5;
        const int standby_radius_yaw = 8;
        public int Center_Pitch, Center_Roll, Center_Yaw;

        DispatcherTimer SpeedX = new DispatcherTimer();
        DispatcherTimer SpeedY = new DispatcherTimer();
        DispatcherTimer CheckDoubleClick = new DispatcherTimer();

        win32.POINT pos = new win32.POINT();

        Serial_Data _SData = new Serial_Data();
        const int Interval = 30;
        const double ratio = 1.5;
        const int DoubleClickInterval = 150;
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
        /************************************************************
        *                  Mouse Event Init
        ************************************************************/
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, IntPtr dwExtraInfo);
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        /************************************************************
        *                  Key Event
        ************************************************************/
        // Get a handle to an application window.
        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName,
            string lpWindowName);

        // Activate an application window.
        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint numberOfInputs, INPUT[] inputs, int sizeOfInputStructure);

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

            CheckDoubleClick.Tick += CheckDoubleClick_Tick;
            CheckDoubleClick.Interval = TimeSpan.FromMilliseconds(Interval);
            CheckDoubleClick.Start();
        }

        private void CheckDoubleClick_Tick(object sender, EventArgs e)
        {
            string temp = _SData.GetData();
            try
            {
                if (temp[11] == '@')
                {
                    SendMouseLeftClick(pos);
                    //Thread.Sleep(150);
                    //temp = _SData.GetData();
                    //if (temp[11] == '@')
                    //{
                    //    sendMouseDoubleClick(pos);
                    //}
                }
            }
            catch
            {

            }
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
                    *UP, DOWN KEYBOARD
                    ************************************************************/
                    if (CheckLEFT(_SData.GetData()))
                    {
                        SendKeyPress(KeyCode.LEFT);
                    }
                    else if (CheckRIGHT(_SData.GetData()))
                    {
                        SendKeyPress(KeyCode.RIGHT);
                    }
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
            // Check Yaw
            if (((yaw < (Center_Yaw + standby_radius_yaw)) && (yaw > (Center_Yaw - standby_radius_yaw))) || (Center_Yaw == 0))
            {
                rspeed = 0;
            }
            else if ((yaw < (Center_Yaw - standby_radius_yaw)))
            {
                int temp = (Center_Yaw - standby_radius_yaw);
                rspeed = yaw - temp;
            }
            else if ((yaw > (Center_Yaw + standby_radius_yaw)))
            {
                int temp = (Center_Yaw + standby_radius_yaw);
                rspeed = yaw - temp;
            }
            return -rspeed;
        }

        private Boolean CheckCalibration(string data)
        {
            if (data.IndexOf("CALIBRATION") >= 0)
            {
                return true;
            }
            return false;
        }

        private Boolean CheckLEFT(string data)
        {
            if (data.IndexOf("LEFT") >= 0)
            {
                return true;
            }
            return false;
        }
        private Boolean CheckRIGHT(string data)
        {
            if (data.IndexOf("RIGHT") >= 0)
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
        private void SendMouseLeftClick(win32.POINT p)
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, p.x, p.y, 0, desktop);
        }
        private void SendMouseRightClick(win32.POINT p)
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, p.x, p.y, 0, desktop);
        }
        void sendMouseDoubleClick(win32.POINT p)
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, p.x, p.y, 0, desktop);

            Thread.Sleep(150);

            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, p.x, p.y, 0, desktop);
        }

        void sendMouseRightDoubleClick(win32.POINT p)
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, p.x, p.y, 0, desktop);

            Thread.Sleep(150);

            mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, p.x, p.y, 0, desktop);
        }

        void sendMouseDown()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 50, 50, 0, desktop);
        }

        void sendMouseUp()
        {
            mouse_event(MOUSEEVENTF_LEFTUP, 50, 50, 0, desktop);
        }



















        /************************************************************
        *KEYBOARD EVENT
        ************************************************************/
        public static void SendKeyPress(KeyCode keyCode)
        {
            INPUT input = new INPUT
            {
                Type = 1
            };
            input.Data.Keyboard = new KEYBDINPUT()
            {
                Vk = (ushort)keyCode,
                Scan = 0,
                Flags = 0,
                Time = 0,
                ExtraInfo = IntPtr.Zero,
            };

            INPUT input2 = new INPUT
            {
                Type = 1
            };
            input2.Data.Keyboard = new KEYBDINPUT()
            {
                Vk = (ushort)keyCode,
                Scan = 0,
                Flags = 2,
                Time = 0,
                ExtraInfo = IntPtr.Zero
            };
            INPUT[] inputs = new INPUT[] { input, input2 };
            if (SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT))) == 0)
                throw new Exception();
        }

        /// <summary>
        /// Send a key down and hold it down until sendkeyup method is called
        /// </summary>
        /// <param name="keyCode"></param>
        public static void SendKeyDown(KeyCode keyCode)
        {
            INPUT input = new INPUT
            {
                Type = 1
            };
            input.Data.Keyboard = new KEYBDINPUT();
            input.Data.Keyboard.Vk = (ushort)keyCode;
            input.Data.Keyboard.Scan = 0;
            input.Data.Keyboard.Flags = 0;
            input.Data.Keyboard.Time = 0;
            input.Data.Keyboard.ExtraInfo = IntPtr.Zero;
            INPUT[] inputs = new INPUT[] { input };
            if (SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT))) == 0)
            {
                throw new Exception();
            }
        }

        /// <summary>
        /// Release a key that is being hold down
        /// </summary>
        /// <param name="keyCode"></param>
        public static void SendKeyUp(KeyCode keyCode)
        {
            INPUT input = new INPUT
            {
                Type = 1
            };
            input.Data.Keyboard = new KEYBDINPUT();
            input.Data.Keyboard.Vk = (ushort)keyCode;
            input.Data.Keyboard.Scan = 0;
            input.Data.Keyboard.Flags = 2;
            input.Data.Keyboard.Time = 0;
            input.Data.Keyboard.ExtraInfo = IntPtr.Zero;
            INPUT[] inputs = new INPUT[] { input };
            if (SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT))) == 0)
                throw new Exception();

        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct INPUT
        {
            public uint Type;
            public MOUSEKEYBDHARDWAREINPUT Data;
        }

        /// <summary>
        /// http://social.msdn.microsoft.com/Forums/en/csharplanguage/thread/f0e82d6e-4999-4d22-b3d3-32b25f61fb2a
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        internal struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public HARDWAREINPUT Hardware;
            [FieldOffset(0)]
            public KEYBDINPUT Keyboard;
            [FieldOffset(0)]
            public MOUSEINPUT Mouse;
        }

        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/ms646310(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct HARDWAREINPUT
        {
            public uint Msg;
            public ushort ParamL;
            public ushort ParamH;
        }

        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/ms646310(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBDINPUT
        {
            public ushort Vk;
            public ushort Scan;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        /// <summary>
        /// http://social.msdn.microsoft.com/forums/en-US/netfxbcl/thread/2abc6be8-c593-4686-93d2-89785232dacd
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT
        {
            public int X;
            public int Y;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        public enum KeyCode : ushort
        {
            #region Media

            /// <summary>
            /// Next track if a song is playing
            /// </summary>
            MEDIA_NEXT_TRACK = 0xb0,

            /// <summary>
            /// Play pause
            /// </summary>
            MEDIA_PLAY_PAUSE = 0xb3,

            /// <summary>
            /// Previous track
            /// </summary>
            MEDIA_PREV_TRACK = 0xb1,

            /// <summary>
            /// Stop
            /// </summary>
            MEDIA_STOP = 0xb2,

            #endregion

            #region math

            /// <summary>Key "+"</summary>
            ADD = 0x6b,
            /// <summary>
            /// "*" key
            /// </summary>
            MULTIPLY = 0x6a,

            /// <summary>
            /// "/" key
            /// </summary>
            DIVIDE = 0x6f,

            /// <summary>
            /// Subtract key "-"
            /// </summary>
            SUBTRACT = 0x6d,

            #endregion

            #region Browser
            /// <summary>
            /// Go Back
            /// </summary>
            BROWSER_BACK = 0xa6,
            /// <summary>
            /// Favorites
            /// </summary>
            BROWSER_FAVORITES = 0xab,
            /// <summary>
            /// Forward
            /// </summary>
            BROWSER_FORWARD = 0xa7,
            /// <summary>
            /// Home
            /// </summary>
            BROWSER_HOME = 0xac,
            /// <summary>
            /// Refresh
            /// </summary>
            BROWSER_REFRESH = 0xa8,
            /// <summary>
            /// browser search
            /// </summary>
            BROWSER_SEARCH = 170,
            /// <summary>
            /// Stop
            /// </summary>
            BROWSER_STOP = 0xa9,
            #endregion

            #region Numpad numbers
            /// <summary>
            /// 
            /// </summary>
            NUMPAD0 = 0x60,
            /// <summary>
            /// 
            /// </summary>
            NUMPAD1 = 0x61,
            /// <summary>
            /// 
            /// </summary>
            NUMPAD2 = 0x62,
            /// <summary>
            /// 
            /// </summary>
            NUMPAD3 = 0x63,
            /// <summary>
            /// 
            /// </summary>
            NUMPAD4 = 100,
            /// <summary>
            /// 
            /// </summary>
            NUMPAD5 = 0x65,
            /// <summary>
            /// 
            /// </summary>
            NUMPAD6 = 0x66,
            /// <summary>
            /// 
            /// </summary>
            NUMPAD7 = 0x67,
            /// <summary>
            /// 
            /// </summary>
            NUMPAD8 = 0x68,
            /// <summary>
            /// 
            /// </summary>
            NUMPAD9 = 0x69,

            #endregion

            #region Fkeys
            /// <summary>
            /// F1
            /// </summary>
            F1 = 0x70,
            /// <summary>
            /// F10
            /// </summary>
            F10 = 0x79,
            /// <summary>
            /// 
            /// </summary>
            F11 = 0x7a,
            /// <summary>
            /// 
            /// </summary>
            F12 = 0x7b,
            /// <summary>
            /// 
            /// </summary>
            F13 = 0x7c,
            /// <summary>
            /// 
            /// </summary>
            F14 = 0x7d,
            /// <summary>
            /// 
            /// </summary>
            F15 = 0x7e,
            /// <summary>
            /// 
            /// </summary>
            F16 = 0x7f,
            /// <summary>
            /// 
            /// </summary>
            F17 = 0x80,
            /// <summary>
            /// 
            /// </summary>
            F18 = 0x81,
            /// <summary>
            /// 
            /// </summary>
            F19 = 130,
            /// <summary>
            /// 
            /// </summary>
            F2 = 0x71,
            /// <summary>
            /// 
            /// </summary>
            F20 = 0x83,
            /// <summary>
            /// 
            /// </summary>
            F21 = 0x84,
            /// <summary>
            /// 
            /// </summary>
            F22 = 0x85,
            /// <summary>
            /// 
            /// </summary>
            F23 = 0x86,
            /// <summary>
            /// 
            /// </summary>
            F24 = 0x87,
            /// <summary>
            /// 
            /// </summary>
            F3 = 0x72,
            /// <summary>
            /// 
            /// </summary>
            F4 = 0x73,
            /// <summary>
            /// 
            /// </summary>
            F5 = 0x74,
            /// <summary>
            /// 
            /// </summary>
            F6 = 0x75,
            /// <summary>
            /// 
            /// </summary>
            F7 = 0x76,
            /// <summary>
            /// 
            /// </summary>
            F8 = 0x77,
            /// <summary>
            /// 
            /// </summary>
            F9 = 120,

            #endregion

            #region Other
            /// <summary>
            /// 
            /// </summary>
            OEM_1 = 0xba,
            /// <summary>
            /// 
            /// </summary>
            OEM_102 = 0xe2,
            /// <summary>
            /// 
            /// </summary>
            OEM_2 = 0xbf,
            /// <summary>
            /// 
            /// </summary>
            OEM_3 = 0xc0,
            /// <summary>
            /// 
            /// </summary>
            OEM_4 = 0xdb,
            /// <summary>
            /// 
            /// </summary>
            OEM_5 = 220,
            /// <summary>
            /// 
            /// </summary>
            OEM_6 = 0xdd,
            /// <summary>
            /// 
            /// </summary>
            OEM_7 = 0xde,
            /// <summary>
            /// 
            /// </summary>
            OEM_8 = 0xdf,
            /// <summary>
            /// 
            /// </summary>
            OEM_CLEAR = 0xfe,
            /// <summary>
            /// 
            /// </summary>
            OEM_COMMA = 0xbc,
            /// <summary>
            /// 
            /// </summary>
            OEM_MINUS = 0xbd,
            /// <summary>
            /// 
            /// </summary>
            OEM_PERIOD = 190,
            /// <summary>
            /// 
            /// </summary>
            OEM_PLUS = 0xbb,

            #endregion

            #region KEYS

            /// <summary>
            /// 
            /// </summary>
            KEY_0 = 0x30,
            /// <summary>
            /// 
            /// </summary>
            KEY_1 = 0x31,
            /// <summary>
            /// 
            /// </summary>
            KEY_2 = 50,
            /// <summary>
            /// 
            /// </summary>
            KEY_3 = 0x33,
            /// <summary>
            /// 
            /// </summary>
            KEY_4 = 0x34,
            /// <summary>
            /// 
            /// </summary>
            KEY_5 = 0x35,
            /// <summary>
            /// 
            /// </summary>
            KEY_6 = 0x36,
            /// <summary>
            /// 
            /// </summary>
            KEY_7 = 0x37,
            /// <summary>
            /// 
            /// </summary>
            KEY_8 = 0x38,
            /// <summary>
            /// 
            /// </summary>
            KEY_9 = 0x39,
            /// <summary>
            /// 
            /// </summary>
            KEY_A = 0x41,
            /// <summary>
            /// 
            /// </summary>
            KEY_B = 0x42,
            /// <summary>
            /// 
            /// </summary>
            KEY_C = 0x43,
            /// <summary>
            /// 
            /// </summary>
            KEY_D = 0x44,
            /// <summary>
            /// 
            /// </summary>
            KEY_E = 0x45,
            /// <summary>
            /// 
            /// </summary>
            KEY_F = 70,
            /// <summary>
            /// 
            /// </summary>
            KEY_G = 0x47,
            /// <summary>
            /// 
            /// </summary>
            KEY_H = 0x48,
            /// <summary>
            /// 
            /// </summary>
            KEY_I = 0x49,
            /// <summary>
            /// 
            /// </summary>
            KEY_J = 0x4a,
            /// <summary>
            /// 
            /// </summary>
            KEY_K = 0x4b,
            /// <summary>
            /// 
            /// </summary>
            KEY_L = 0x4c,
            /// <summary>
            /// 
            /// </summary>
            KEY_M = 0x4d,
            /// <summary>
            /// 
            /// </summary>
            KEY_N = 0x4e,
            /// <summary>
            /// 
            /// </summary>
            KEY_O = 0x4f,
            /// <summary>
            /// 
            /// </summary>
            KEY_P = 80,
            /// <summary>
            /// 
            /// </summary>
            KEY_Q = 0x51,
            /// <summary>
            /// 
            /// </summary>
            KEY_R = 0x52,
            /// <summary>
            /// 
            /// </summary>
            KEY_S = 0x53,
            /// <summary>
            /// 
            /// </summary>
            KEY_T = 0x54,
            /// <summary>
            /// 
            /// </summary>
            KEY_U = 0x55,
            /// <summary>
            /// 
            /// </summary>
            KEY_V = 0x56,
            /// <summary>
            /// 
            /// </summary>
            KEY_W = 0x57,
            /// <summary>
            /// 
            /// </summary>
            KEY_X = 0x58,
            /// <summary>
            /// 
            /// </summary>
            KEY_Y = 0x59,
            /// <summary>
            /// 
            /// </summary>
            KEY_Z = 90,

            #endregion

            #region volume
            /// <summary>
            /// Decrese volume
            /// </summary>
            VOLUME_DOWN = 0xae,

            /// <summary>
            /// Mute volume
            /// </summary>
            VOLUME_MUTE = 0xad,

            /// <summary>
            /// Increase volue
            /// </summary>
            VOLUME_UP = 0xaf,

            #endregion


            /// <summary>
            /// Take snapshot of the screen and place it on the clipboard
            /// </summary>
            SNAPSHOT = 0x2c,

            /// <summary>Send right click from keyboard "key that is 2 keys to the right of space bar"</summary>
            RightClick = 0x5d,

            /// <summary>
            /// Go Back or delete
            /// </summary>
            BACKSPACE = 8,

            /// <summary>
            /// Control + Break "When debuging if you step into an infinite loop this will stop debug"
            /// </summary>
            CANCEL = 3,
            /// <summary>
            /// Caps lock key to send cappital letters
            /// </summary>
            CAPS_LOCK = 20,
            /// <summary>
            /// Ctlr key
            /// </summary>
            CONTROL = 0x11,

            /// <summary>
            /// Alt key
            /// </summary>
            ALT = 18,

            /// <summary>
            /// "." key
            /// </summary>
            DECIMAL = 110,

            /// <summary>
            /// Delete Key
            /// </summary>
            DELETE = 0x2e,


            /// <summary>
            /// Arrow down key
            /// </summary>
            DOWN = 40,

            /// <summary>
            /// End key
            /// </summary>
            END = 0x23,

            /// <summary>
            /// Escape key
            /// </summary>
            ESC = 0x1b,

            /// <summary>
            /// Home key
            /// </summary>
            HOME = 0x24,

            /// <summary>
            /// Insert key
            /// </summary>
            INSERT = 0x2d,

            /// <summary>
            /// Open my computer
            /// </summary>
            LAUNCH_APP1 = 0xb6,
            /// <summary>
            /// Open calculator
            /// </summary>
            LAUNCH_APP2 = 0xb7,

            /// <summary>
            /// Open default email in my case outlook
            /// </summary>
            LAUNCH_MAIL = 180,

            /// <summary>
            /// Opend default media player (itunes, winmediaplayer, etc)
            /// </summary>
            LAUNCH_MEDIA_SELECT = 0xb5,

            /// <summary>
            /// Left control
            /// </summary>
            LCONTROL = 0xa2,

            /// <summary>
            /// Left arrow
            /// </summary>
            LEFT = 0x25,

            /// <summary>
            /// Left shift
            /// </summary>
            LSHIFT = 160,

            /// <summary>
            /// left windows key
            /// </summary>
            LWIN = 0x5b,


            /// <summary>
            /// Next "page down"
            /// </summary>
            PAGEDOWN = 0x22,

            /// <summary>
            /// Num lock to enable typing numbers
            /// </summary>
            NUMLOCK = 0x90,

            /// <summary>
            /// Page up key
            /// </summary>
            PAGE_UP = 0x21,

            /// <summary>
            /// Right control
            /// </summary>
            RCONTROL = 0xa3,

            /// <summary>
            /// Return key
            /// </summary>
            ENTER = 13,

            /// <summary>
            /// Right arrow key
            /// </summary>
            RIGHT = 0x27,

            /// <summary>
            /// Right shift
            /// </summary>
            RSHIFT = 0xa1,

            /// <summary>
            /// Right windows key
            /// </summary>
            RWIN = 0x5c,

            /// <summary>
            /// Shift key
            /// </summary>
            SHIFT = 0x10,

            /// <summary>
            /// Space back key
            /// </summary>
            SPACE_BAR = 0x20,

            /// <summary>
            /// Tab key
            /// </summary>
            TAB = 9,

            /// <summary>
            /// Up arrow key
            /// </summary>
            UP = 0x26,

        }
    }
}
