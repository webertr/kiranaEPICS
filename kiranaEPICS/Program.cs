using System;
using System.Runtime.InteropServices;


namespace kiranaEPICS
{

    class Program
    {
        // Get a handle to an application window.
        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        // Activate an application window.
        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        static void Main(string[] args)
        {
            // Get a handle to the Calculator application. The window class
            // and window name were obtained using the Spy++ tool.
            //IntPtr kiranaHandle = FindWindow("TfrmKTestMain", "Kirana");
            IntPtr kiranaHandle = FindWindow("TAboutDlg", "About");

            // Verify that Calculator is a running process.
            if (kiranaHandle == IntPtr.Zero)
            {
                System.Console.WriteLine("Did not find window");
            } else
            {
                System.Console.WriteLine("Found window");
            }

            SetForegroundWindow(kiranaHandle);

            System.Windows.Forms.SendKeys.SendWait("{ENTER}");

            System.Console.ReadKey();

            // Make Calculator the foreground application and send it 
            // a set of calculations.
            //SetForegroundWindow(calculatorHandle);
            //System.Windows.Forms.SendKeys.SendWait("111");

        }
    }
}
