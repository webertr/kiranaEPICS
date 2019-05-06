using System;
using System.Runtime.InteropServices;
using Castle.Core;


namespace kiranaEPICS
{

    class Program
    {

        // Global variables for the EPICS interface
        private static int armValue;
        private static int shotNumber;
        private static string fileBase;
        private static string folderBase;
        private static string shotNumberString;
        private static string folderBaseTemplate = "K:\\Fuze Data\\Spectroscopy (NAS)\\Data\\???";
        private static EpicsSharp.ChannelAccess.Client.CAClient client;
        private static EpicsSharp.ChannelAccess.Client.Channel armChannel;
        private static EpicsSharp.ChannelAccess.Client.Channel shotNumberChannel;
        private static readonly string armPV = "FuZE:ControlPLC:KiranaAcquire";
        private static readonly string shotNumberPV = "FuZE:DataServer:ShotNumber";
        private static System.Threading.Mutex mutexKirana = new System.Threading.Mutex();

        // global variables for the Kirana API
        private static TestStack.White.UIItems.Button armButton;
        private static TestStack.White.UIItems.WindowItems.Window window;
        private static TestStack.White.Factory.InitializeOption initializeOption;
        private static TestStack.White.Application application = TestStack.White.Application.Launch("C:\\Program Files (x86)\\Specialised Imaging\\Kirana\\Kirana.exe");

        static void Main(string[] args)
        {

            // Code to interface with EPICS
            if (false) {
                // Setting up EPICS channel access client
                client = new EpicsSharp.ChannelAccess.Client.CAClient();

                // Specifying the gateway so it doesn't search the other networks for PV's. It will slow it down.
                client.Configuration.SearchAddress = "10.10.10.249";

                // Creating channel for arm PV and getting intial value
                armChannel = client.CreateChannel<int>(armPV);
                armValue = armChannel.Get<int>(1);

                // Setting callback for monitor
                armChannel.MonitorChanged += new EpicsSharp.ChannelAccess.Client.ChannelValueDelegate(armCallBack);

                // Creating channel for shotnumber PV.
                shotNumberChannel = client.CreateChannel<int>(shotNumberPV);

                // Setting callback for monitor
                shotNumberChannel.MonitorChanged += new EpicsSharp.ChannelAccess.Client.ChannelValueDelegate(shotNumberCallBack);
            }


            // Setting up Kirana software interface
            initializeOption = TestStack.White.Factory.InitializeOption.NoCache;
            window = application.GetWindow("Kirana", initializeOption);

            // Finding the arm button. Need to disable a thrown Exception, NonComVisibleClass or something.
            armButton = window.Get<TestStack.White.UIItems.Button>("Arm");

            // Another way to do it.
            //window.Get<TestStack.White.UIItems.UIItem>("Arm").Click();

            // Sleeping for 10 seconds
            //System.Threading.Thread.Sleep(10000);

            // Closing the application
            //window.TitleBar.CloseButton.Click();

            System.Console.WriteLine("Finished");

            // Infinite sleep
            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);

            return;

        }

        /*
         * Method that is called by the PV callback to arm the Kirana
         */
        public static void armCallBack(EpicsSharp.ChannelAccess.Client.Channel channel, object newValue)
        {
            armValue = (int)newValue;
            System.Threading.ThreadStart childRef = new System.Threading.ThreadStart(armKiranaThread);
            System.Threading.Thread childThread = new System.Threading.Thread(childRef);
            childThread.Start();
        }

        /*
         * Method that will update the shot number value. Called as callback from PV change
         */
        public static void shotNumberCallBack(EpicsSharp.ChannelAccess.Client.Channel channel, object newValue)
        {
            shotNumberString = newValue.ToString();
            shotNumber = (int)newValue;
            System.Threading.ThreadStart childRef = new System.Threading.ThreadStart(shotNumberThread);
            System.Threading.Thread childThread = new System.Threading.Thread(childRef);
            childThread.Start();
        }

        /*
         * Method to arm the Kirana. Called from the callback function as a thread
         */
        public static void armKiranaThread()
        {
            mutexKirana.WaitOne();
            armButton.Click();
            System.Console.WriteLine("Arming the Kirana with: {0}", armValue);
            mutexKirana.ReleaseMutex();
        }

        /*
         * Method called by the callback to update the shot number
         */
        public static void shotNumberThread()
        {
            mutexKirana.WaitOne();
            fileBase = shotNumberString;
            int baseShotNumber = shotNumber / 1000;
            string baseShotNumberString = baseShotNumber.ToString();
            folderBase = folderBaseTemplate.Replace("???", baseShotNumberString);
            System.IO.Directory.CreateDirectory(folderBase);
            System.Console.WriteLine("Updated shot number to: {0}", shotNumber);
            mutexKirana.ReleaseMutex();
        }
    }
}
