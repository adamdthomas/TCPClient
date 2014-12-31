using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TCPUIClient
{
    class UIAutomation
    {
        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName,
        string lpWindowName);

        // Activate an application window.
        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        // Send a series of key presses to the Calculator application. 
        public static void KeysToApp(string classname, string windowname, string[] commands,int WaitBetweenInMiliSeconds, int WaitAfterInSeconds)
        {
            // Get a handle to the Calculator application. The window class 
            // and window name were obtained using the Spy++ tool.
            IntPtr AppHandle = FindWindow(classname, windowname);

            // Verify that Calculator is a running process. 
            if (AppHandle == IntPtr.Zero)
            {
                MessageBox.Show("Application is not running.");
                return;
            }

            // Make Calculator the foreground application and send it  
            // a set of calculations.
            SetForegroundWindow(AppHandle);

            foreach (var cmd in commands)
            {
                SendKeys.SendWait(cmd);
                WebAutomationToolkit.Utilities.Wait(0,WaitBetweenInMiliSeconds);

            }

            WebAutomationToolkit.Utilities.Wait(WaitAfterInSeconds);


        }


    }
}
