using System;
using System.Runtime.InteropServices;
using Castle.Core;


namespace kiranaEPICS
{

    class Program
    {
        // Global variables for the EPICS interface
        private static int armValue;
        private static int saveValue;
        private static int shotNumber;
        private static string fileBase;
        private static string folderBase;
        private static string shotNumberString;
        private static string folderBaseTemplate = "K:\\Fuze Data\\FuZE Kirana Movies\\???";
        private static EpicsSharp.ChannelAccess.Client.CAClient client;
        private static EpicsSharp.ChannelAccess.Client.Channel armChannel;
        private static EpicsSharp.ChannelAccess.Client.Channel shotNumberChannel;
        private static EpicsSharp.ChannelAccess.Client.Channel saveChannel;
        private static readonly string armPV = "FuZE:ControlPLC:KiranaAcquire";
        private static readonly string shotNumberPV = "FuZE:DataServer:ShotNumber";
        private static readonly string savePV = "FuZE:ControlPLC:KiranaSave";
        private static System.Threading.Mutex mutexKirana = new System.Threading.Mutex();

        // global variables for the Kirana API
        private static TestStack.White.UIItems.Button armButton;
        private static string armButtonID;
        private static TestStack.White.UIItems.IUIItem frameRate;
        private static string frameRateID;
        private static TestStack.White.UIItems.IUIItem exposure;
        private static string exposureID;
        private static TestStack.White.UIItems.WindowStripControls.ToolStrip toolBar;
        private static string toolBarID;
        private static TestStack.White.UIItems.IUIItem loadButton;
        private static TestStack.White.UIItems.IUIItem saveButton;
        private static TestStack.White.UIItems.IUIItem linkCameraButton;
        private static TestStack.White.UIItems.Finders.SearchCriteria searchCriteria;
        private static TestStack.White.UIItems.WindowItems.Window window;
        private static TestStack.White.Factory.InitializeOption initializeOption;
        private static TestStack.White.Application application = TestStack.White.Application.Launch("C:\\Program Files (x86)\\Specialised Imaging\\Kirana\\Kirana.exe");

        static void Main(string[] args)
        {


            // Sleeping for 10 seconds while application loads
            System.Threading.Thread.Sleep(10000);

            // Setting up Kirana software interface
            initializeOption = TestStack.White.Factory.InitializeOption.NoCache;
            window = application.GetWindow("Kirana", initializeOption);

            // Finding the arm button. Need to disable a thrown Exception, NonComVisibleClass or something.
            armButton = window.Get<TestStack.White.UIItems.Button>("Arm");
            armButtonID = armButton.Id;

            // Find the top tool bar that has a "save" and "load" option
            searchCriteria = TestStack.White.UIItems.Finders.SearchCriteria.ByClassName("TToolBar").AndIndex(0);
            toolBar = (TestStack.White.UIItems.WindowStripControls.ToolStrip)window.Get(searchCriteria);
            toolBarID = toolBar.Id;
            TestStack.White.UIItems.IUIItem[] toolBarList = toolBar.Items.ToArray();
            loadButton = toolBarList[0];
            saveButton = toolBarList[1];
            linkCameraButton = toolBarList[3];

            saveButton = toolBarList[1];
            System.Console.WriteLine("Tool Bar Number: {0}", loadButton.Name);
            System.Console.WriteLine("Tool Bar Enabled: {0}", loadButton.Enabled);

            // Here is another method to get buttons. Get one of the "TPanel" class. THere should be alot.
            searchCriteria = TestStack.White.UIItems.Finders.SearchCriteria.ByClassName("TPanel").AndIndex(4);
            TestStack.White.UIItems.Panel test = (TestStack.White.UIItems.Panel)window.Get(searchCriteria);

            // Get all of the items in the panel as a collection
            TestStack.White.UIItems.UIItemCollection collectionTest = test.Items;

            // Convert the collection to an array. Now you can pull off IUIItems, and potentially click.
            TestStack.White.UIItems.IUIItem[] collectionArray = (TestStack.White.UIItems.IUIItem[])collectionTest.ToArray();
            exposure = collectionArray[0];
            exposureID = exposure.Id;
            frameRate = collectionArray[1];
            frameRateID = frameRate.Id;
            System.Console.WriteLine("Exposure Enabled: {0}", exposure.Enabled);
            System.Console.WriteLine("Exposure Name: {0}", exposure.Name);
            System.Console.WriteLine("Exposure ID: {0}", exposure.Id);
            System.Console.WriteLine("Frame Rate Enabled: {0}", frameRate.Enabled);
            System.Console.WriteLine("Frame Rate Name: {0}", frameRate.Name);
            System.Console.WriteLine("Frame Rate ID: {0}", frameRate.Id);

            System.Console.WriteLine("Finished");

            // Code to interface with EPICS
            if (true)
            {
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

                // Creating channel for save PV and getting initial value
                saveChannel = client.CreateChannel<int>(savePV);
                saveValue = saveChannel.Get<int>(1);

                // Setting callback for save monitor
                saveChannel.MonitorChanged += new EpicsSharp.ChannelAccess.Client.ChannelValueDelegate(saveCallBack);
            }

            System.Threading.Thread.Sleep(5000);

            // Clicking on the link camera button
            linkCameraButton.Click();

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
         * Method that will update the save value. Called as callback from PV change
         */
        public static void saveCallBack(EpicsSharp.ChannelAccess.Client.Channel channel, object newValue)
        {
            saveValue = (int)newValue;
            System.Threading.ThreadStart childRef = new System.Threading.ThreadStart(saveThread);
            System.Threading.Thread childThread = new System.Threading.Thread(childRef);
            childThread.Start();
        }

        /*
         * Method to arm the Kirana. Called from the callback function as a thread
         */
        public static void armKiranaThread()
        {
            mutexKirana.WaitOne();

            if (armValue.Equals(1))
            {
                if (armButton.Enabled)
                {
                    armButton.Click();
                    System.Console.WriteLine("Arming the Kirana with: {0}", armValue);
                }
                else
                {
                    System.Console.WriteLine("Cannot arm Kirana, arm button disabled");
                }
            }

            mutexKirana.ReleaseMutex();
        }

        /*
         * Method called by the callback to update the shot number
         * Sets the folder base, and the file base, or the shot number.
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

        /*
         * Method to save the Kirana. Called from the callback function as a thread
         */
        public static void saveThread()
        {
            mutexKirana.WaitOne();    
            
            if (saveValue.Equals(1))
            {
                if (saveButton.Enabled)
                {
                    saveButton.Click();
                    saveButton.Enter(folderBase + "\\" + fileBase + ".SVF");
                    saveButton.Enter("\n");
                    System.Console.WriteLine("Saving the Kirana video with: {0}", saveValue);
                }
                else
                {
                    System.Console.WriteLine("Save button not enabled");
                }
            }

            mutexKirana.ReleaseMutex();
        }
    }
}
