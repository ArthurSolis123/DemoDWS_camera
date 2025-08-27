/*
 *This main logic
 *ImageView image display (for reference only)
 *ImageSaver image storage (for reference only)
 *ImageWrapper image encapsulation class
 *ImageHelper Image operation analysis class
 *PlatformComponent interface class
 *VideoWindow Picture Display Column Class (for reference only)
 *
 *Implement image decoding using: ImageHelper.CreateImage, which may take time
 *The return result is ImageWrapper
 *
 *The picture display needs to be decoded first. Please operate on a separate thread for decoding.
 * 
 * */

using LogisticsBaseCSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using System.Net;
using System.Net.Sockets;

namespace DemoDWS
{
    public partial class Form1DWS : Form
    {
        //==================================VARIABLES=====================================
        #region VARIABLE
        //Udp Broadcaaster for the bar code
        private UdpBroadcaster udpBroadcaster;

        private string default_text = "No detection";
        //Image count
        private long _realImgCount = 0;

        // Maximum limit for queues
        private const int MAXQUEUESIZE = 64;

        // DWS management instance, providing DWS-related startup operations, stop operations, etc.
        private LogisticsWrapper dwsManager;

        // The package information collection reported on the DWS base layer, including the package barcode, original picture, and page sheet picture
        private Queue<BaseCodeData> packageItems = new Queue<BaseCodeData>();

        // Threads that process package information, used to process package information collection reported by DWS underlying layer
        private Thread processPackageThread;

        // Is DWS running an identity? The value is true after starting DWS, and the value is false after closing DWS.
        private bool isRunningDWS = false;

        //The associated container of camera key value and camera hardware information CameraInfos information
        private Dictionary<string, CameraInfo> camerInfoMap = new Dictionary<string, CameraInfo>();

        // Component list
        private List<PlatformComponent> m_ComponentList = new List<PlatformComponent>();

        // Camera slot mapping (full key and a short token for robust matching)
        private string _leftKeyFull, _rightKeyFull;

        //Hold Barcode on screen for how long
        private int CodeHoldSeconds = 5;

        //Hold timer for left barcode on the UI
        private System.Windows.Forms.Timer _codeHoldTimerLeft;

        //Hold timer for left barcode on the UI
        private System.Windows.Forms.Timer _codeHoldTimerRight;

        //Package Count
        private long _pkgCount = 0;

        #endregion  // VARIABLE

        //==================================FUNCTIONS=====================================
        #region FUNCTION
        //Constructor
        public Form1DWS()
        {
            //Initialize the components used in Main gui window
            InitializeComponent();

            //For the timers used for keeping the barcodes on the screen for a set amount of time
            _codeHoldTimerLeft = new System.Windows.Forms.Timer();
            _codeHoldTimerLeft.Interval = Math.Max(100, CodeHoldSeconds * 1000);
            _codeHoldTimerLeft.Tick += (s, e) =>
            {
                lblCode.Text = default_text;
                _codeHoldTimerLeft.Stop();
            };
            _codeHoldTimerRight = new System.Windows.Forms.Timer();
            _codeHoldTimerRight.Interval = Math.Max(100, CodeHoldSeconds * 1000);
            _codeHoldTimerRight.Tick += (s, e) =>
            {
                label2.Text = default_text;
                _codeHoldTimerRight.Stop();
            };

            //Image display
            var m_imageDisplayArea = new ImageView();
            m_imageDisplayArea.Dock = DockStyle.Fill;
            Pan_Dis.Controls.Add(m_imageDisplayArea);
            m_ComponentList.Add(m_imageDisplayArea);

            //Broadcast Barcode
            udpBroadcaster = new UdpBroadcaster("192.168.18.255", 9000);
        }

        // (Main Loop) Specific methods for processing package information, such as displaying barcode information, original image, cutout etc.
        private void ProcessPackage()
        {
            while (isRunningDWS)
            {
                try
                {
                    BaseCodeData e = null;

                    lock (packageItems)
                    {
                        if (packageItems.Count > 0)
                        {
                            e = packageItems.Dequeue();
                        }
                    }

                    if (e == null)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    var codeJoint = string.Join("_", e.CodeList);

                    if (e.OutputResult == 0)
                    {
                        //Processing the barcode information of the package
                        // LogHelper.Log.InfoFormat("Processing parcel information [barcode only]： {0}", string.Join("_", e.CodeList));
                        foreach (var comp in m_ComponentList)
                        {
                            comp.OnPacketResultReached(this, e);
                        }
                    }
                    else if (e.OutputResult == 1)
                    {
                        //Display barcode information
                        this.BeginInvoke(new Action(() =>
                        {
                            //Print the code on the GUI
                            UpdateCameraLabels(e.CameraID, e.CodeList);

                        }));

                        foreach (var comp in m_ComponentList)
                        {
                            comp.OnPacketResultReached(this, e);
                        }

                        //Process the barcode information of the package
                        LogHelper.Log.InfoFormat("Processing parcel information [barcode]]： {0}", codeJoint);

                    }
                }
                catch (Exception ee)
                {
                    LogHelper.Log.Error("ProcessPackage internal loop exception", ee);
                }
                Thread.Sleep(10);
            }
        }

        // Get all camera information
        private void getAllCameraInfos()
        {
            // Get list of all working cameras from the DWS SDK
            var cameraInfoList = dwsManager.GetWorkCameraInfo();

            // Check if we successfully got camera information
            if (cameraInfoList != null)
            {
                // Loop through each camera in the list
                foreach (var cameraInfo in cameraInfoList)
                {
                    // Skip any null camera entries (safety check)
                    if (cameraInfo == null) { continue; }

                    // Get the unique identifier for this camera
                    var key = cameraInfo.camDevExtraInfo;

                    // Skip cameras without a valid identifier
                    if (string.IsNullOrWhiteSpace(key)) continue;

                    // Add camera to our dictionary if it's not already there Dictionary prevents duplicate cameras
                    if (!camerInfoMap.ContainsKey(key))
                    { camerInfoMap.Add(key, cameraInfo); }
                }
            }
            else
            {
                LogHelper.Log.InfoFormat("The camera failed to obtain device information");
            }

            // Configure the image display with the first two cameras found
            var keys = camerInfoMap.Keys.ToList();
            // We need at least 2 cameras for left/right display
            if (keys.Count == 2)
            {
                // Assign cameras to displays
                var leftKey = keys[0];
                var rightKey = keys[1];

                // Find the ImageView component in our component list
                var imgView = m_ComponentList.OfType<ImageView>().FirstOrDefault();
                if (imgView != null)
                {
                    // Tell ImageView which camera goes to which display slot
                    imgView.ConfigureCameraSlots(leftKey, rightKey);

                    // Store full camera keys for later use
                    _leftKeyFull = leftKey;
                    _rightKeyFull = rightKey;

                    //Initialize labels with prefixes
                    lblCode.Text = default_text;
                    label2.Text = default_text;
                }
            }
        }

        //Show text on the gui
        public void LogTextOnUI(string text)
        {
            if (listBoxLog.InvokeRequired)
            {
                this.BeginInvoke((Action)(() =>
                {
                    LogTextOnUI(text);
                }));
                return;
            }

            listBoxLog.Items.Add(text);
            listBoxLog.SelectedIndex = listBoxLog.Items.Count - 1;

            if (listBoxLog.Items.Count > 200)
            {
                listBoxLog.Items.RemoveAt(0);
            }
        }

        // Get all camera status
        private void getAllCameraStatus()
        {
            // Get current status of all cameras from DWS SDK
            var cameraInfoList = dwsManager.GetCamerasStatus();
            if (cameraInfoList != null)
            {
                // Loop through each camera's status
                foreach (var cameraInfo in cameraInfoList)
                {
                    //Logs Device key, device userid and whether it is online or not
                    LogTextOnUI(string.Format("The camera: device.Key：{0}， device.UserId：{1}， device.onlineState：{2}"
                        , cameraInfo.key, cameraInfo.deviceUserID, cameraInfo.isOnline ? "Online" : "Offline"));
                }
            }
        }

        //Check if Camera is left Camera using the key
        private bool IsLeftCamera(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return !string.IsNullOrEmpty(_leftKeyFull) && key == _leftKeyFull;
        }

        //Check if Camera is right Camera
        private bool IsRightCamera(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return !string.IsNullOrEmpty(_rightKeyFull) && key == _rightKeyFull;
        }

        //Set the Left Label
        private void SetLeftLabel(string codeText)
        {
            if (this.IsDisposed) return;
            this.BeginInvoke(new Action(() =>
            {
                lblCode.Text = codeText;
                _codeHoldTimerLeft.Stop();
                _codeHoldTimerLeft.Interval = Math.Max(100, CodeHoldSeconds * 1000);
                _codeHoldTimerLeft.Start();
            }));
        }
        //Set right label
        private void SetRightLabel(string codeText)
        {
            if (this.IsDisposed) return;
            this.BeginInvoke(new Action(() =>
            {
                label2.Text = codeText;
                _codeHoldTimerRight.Stop();
                _codeHoldTimerRight.Interval = Math.Max(100, CodeHoldSeconds * 1000);
                _codeHoldTimerRight.Start();
            }));
        }

        //Update the labels (barcodes) shown in GUI
        private void UpdateCameraLabels(string cameraKey, IList<string> codes)
        {
            if (codes == null || codes.Count == 0) return;

            // Join codes (you can customize formatting)
            string codeJoint = string.Join(",", codes);

            // Ignore explicit noread
            if (string.Equals(codeJoint, "noread", StringComparison.OrdinalIgnoreCase))
                return;

            //This is what shows the barcode on the GUI
            if (IsLeftCamera(cameraKey))
                SetLeftLabel(codeJoint);
            else if (IsRightCamera(cameraKey))
                SetRightLabel(codeJoint);
        }



        #endregion  // FUNC

        //==================================CALLBACKS=====================================
        #region CALLBACK
        // Specific methods for receiving package information reported by DWS
        private void PackageInfoCallBack(object o, LogisticsCodeEventArgs e)
        {
            try
            {
                BaseCodeData info = new BaseCodeData
                {
                    OutputResult = e.OutputResult,
                    CameraID = e.CameraID,
                    CodeList = e.CodeList,
                    AreaList = e.AreaList,

                    OriImage = e.OriginalImage,
                };

                // Simple barcode detection check
                string codes = string.Join(",", info.CodeList ?? new List<string>());

                if (codes != default_text && !string.IsNullOrEmpty(codes))
                {
                    // Broadcast the barcode and camera IP
                    udpBroadcaster.SendBarcode(codes, info.CameraID);

                    //Used for logging bar code in the log on GUI if not needed than delete this if statement
                    if (codes != "noread")
                    {
                        if (IsLeftCamera(info.CameraID))
                        {
                            LogTextOnUI($"{DateTime.Now:HH:mm:ss} - Camera 1 Barcode: {codes}");
                        }
                        else
                        {
                            LogTextOnUI($"{DateTime.Now:HH:mm:ss} - Camera 2 Barcode: {codes}");
                        }
                    }
                }

                lock (packageItems)
                {
                    // Control the maximum number of queues
                    if (packageItems.Count < MAXQUEUESIZE)
                    {
                        packageItems.Enqueue(info);
                    }
                    else
                    {
                        LogHelper.Log.Error("packageItems lose data");
                    }
                }

                if (e.OutputResult == 0)
                {
                    // Update UI for barcode-only results too
                    UpdateCameraLabels(info.CameraID, info.CodeList);

                    // Send to display components
                    foreach (var comp in m_ComponentList)
                    {
                        comp.OnPacketResultReached(this, info);
                    }
                }

                // Add to queue for both results
                lock (packageItems)
                {
                    if (packageItems.Count < MAXQUEUESIZE)
                    {
                        packageItems.Enqueue(info);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log.Error("Execute PackageInfoCallBack exception", ex);
            }
        }

        // Camera disconnection callback
        private void CameraDisconnectCallBack(object o, CameraStatusArgs status)
        {
            LogHelper.Log.ErrorFormat("Camera Status Update Key: {0} UserID: {1} Status: {2}", status.CameraKey, status.CameraUserID, status.IsOnline ? "Online" : "Offline");
            LogHelper.Log.ErrorFormat("cameraStatus.IsOnline = {0} ", status.IsOnline);
        }

        #endregion  // CALLBACK

        //==================================EVENTS========================================
        #region EVENT

        /// Start DWS
        private void btnStart_Click(object sender, EventArgs e)
        {
            var retStr = "";
            if (isRunningDWS == true)
            {
                LogTextOnUI(DateTime.Now.ToLongTimeString() + " DWS has been started successfully, no need to start again");
                return;
            }

            dwsManager = LogisticsBaseCSharp.LogisticsWrapper.Instance;

            int status = dwsManager.Initialization(".\\Cfg\\LogisticsBase.cfg");

            if (status != (int)EAppRunStatus.eAppStatusInitOK)
            {
                retStr = ErrorInfo.GetErrorMessage(status);
                LogTextOnUI("After dwsManager.Initialization return ret :" + status + " ret info:" + retStr);
                LogHelper.Log.InfoFormat("[dwsManager.Initialization]After dwsManager.Initialization return ret:{0},ret info:{1}"
                       , status, retStr);
                return;
            }

            //Enable the camera disconnection reporting function on the underlying DWS
            dwsManager.AttachCameraDisconnectCB();

            //Enable the callback function that registers all camera code reading information
            bool b = dwsManager.AttachAllCameraCodeinfoCB();

            //Open the callback function for registering panoramic camera and barcode cutout splicing image information
            bool c = dwsManager.AttachIpcCombineInfoCB();

            //Open the callback function for registering the camera's real-time picture information
            bool d = dwsManager.AttachRealImageCB();

            //Register the method of camera disconnection callback. When the camera in the DWS device is disconnected, the relevant camera information will be called back to the CameraDisconnectCallBack method.
            dwsManager.CameraDisconnectEventHandler += CameraDisconnectCallBack;

            status = dwsManager.Start();//Make judgments on the return value uploaded from the bottom layer and pop up the corresponding pop-up box

            retStr = ErrorInfo.GetErrorMessage(status);

            // LogTextOnUI("dwsManager.Start ret val:{" + status + "},ret info:");
            // LogTextOnUI("{" + retStr + " }" + "\r\n");
            // LogTextOnUI("\r\n");

            LogHelper.Log.InfoFormat("[ dwsManager.Start ret val:{0},ret info:{1}.\n", status, retStr);

            if (status == (int)EAppRunStatus.eAppStatusInitOK)
            {
                //Register package information callback method. When the DWS device scans the package information, it will callback to the PackageInfoCallBack method.
                dwsManager.CodeHandle += PackageInfoCallBack;

            }
            else
            {
                //Turn off the camera disconnection reporting function of the DWS underlying layer
                dwsManager.DetachCameraDisconnectCB();

                //Uninstall the logical callback function for scanning the code result processing
                dwsManager.DetachAllCameraCodeinfoCB();

                //Uninstall the panoramic camera and barcode cutout splicing diagram information result processing logic callback function
                dwsManager.DetachIpcCombineInfoCB();

                //Uninstall the camera's real-time image information result processing logic callback function
                dwsManager.DetachRealImageCB();

                //Register the method of camera disconnection callback. When the camera in the DWS device is disconnected, the relevant camera information will be called back to the CameraDisconnectCallBack method.
                dwsManager.CameraDisconnectEventHandler -= CameraDisconnectCallBack;

                /////Execute the Stop method of the DWS layer
                Task.Factory.StartNew(() =>
                {
                    if (!dwsManager.StopApp())
                    {
                        LogHelper.Log.Info("Stop execution failed. Please refer to the log file /Log/Default/log");
                        return;
                    }
                }).Wait();
                return;
            }

            //Set the ID of whether DWS is running to true
            isRunningDWS = true;

            foreach (var comp in m_ComponentList)
            {
                comp.Init();
                comp.Start();
            }

            //Start the thread that processes the package information
            //The background thread must be, otherwise the foreground thread will be displayed as a deadlock when the result in the callback is displayed.
            processPackageThread = new Thread(ProcessPackage);
            processPackageThread.IsBackground = true;
            processPackageThread.Start();

            LogTextOnUI(DateTime.Now.ToLongTimeString() + " Successfully started BarCode Reading");

            //Get all camera device information
            getAllCameraInfos();
            getAllCameraStatus();
        }


        /// Close DWS
        private void btnClose_Click(object sender, EventArgs e)
        {
            if (isRunningDWS == false)
            {
                LogTextOnUI(DateTime.Now.ToLongTimeString() + " Scanner has been shut down successfully, no need to close it again");
                return;
            }

            //Set the ID of whether DWS is running to false
            isRunningDWS = false;

            //Stop the thread and judge the status to ensure that the thread is disconnected
            if (processPackageThread != null && processPackageThread.IsAlive)
            {
                processPackageThread.Join();
            }

            //Turn off the camera disconnection reporting function of the DWS underlying layer
            dwsManager.DetachCameraDisconnectCB();

            //Uninstall the logical callback function for scanning the code result processing
            dwsManager.DetachAllCameraCodeinfoCB();

            //Uninstall the panoramic camera and barcode cutout splicing diagram information result processing logic callback function
            dwsManager.DetachIpcCombineInfoCB();

            //Uninstall the camera's real-time image information result processing logic callback function
            dwsManager.DetachRealImageCB();

            //Method to cancel the callback of package information.
            dwsManager.CodeHandle -= PackageInfoCallBack;

            //Unregister the method of unregistering the camera's disconnection callback.
            dwsManager.CameraDisconnectEventHandler -= CameraDisconnectCallBack;

            foreach (var comp in m_ComponentList)
            {
                comp.Uninit();
                comp.Stop();
            }

            //Execute the underlying Stop method of DWS
            if (!dwsManager.StopApp())
            {
                LogHelper.Log.Info("Stop execution failed. Please refer to the log file /Log/Default/log");
                return;
            }

            LogTextOnUI(DateTime.Now.ToLongTimeString() + " Scanner shutdown succeeded");

            //udp Close
            udpBroadcaster.udpClose();
        }


        /// View the underlying SDK log (turn on if you need it)
        private void btnLog_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", @".\Log");
        }

        private void MainForm_Closing(object sender, EventArgs e)
        {
            this.btnClose_Click(this, new EventArgs());
        }
        #endregion  // EVENT

        //==================================UICOMPONENTS==================================
        #region UICOMPONTNENTS
        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void tlpCameras_Paint(object sender, PaintEventArgs e)
        {

        }

        private void lblCode_Click(object sender, EventArgs e)
        {

        }

        private void groupBox5_Enter(object sender, EventArgs e)
        {

        }
        #endregion  // EVENT

    }
    //UDP broadcaster class
    public class UdpBroadcaster
    {
        private UdpClient udpClient;
        private string broadcastIp;
        private int port;

        //Constructor
        public UdpBroadcaster(string broadcastIp, int port)
        {
            this.broadcastIp = broadcastIp;
            this.port = port;
            this.udpClient = new UdpClient();
            this.udpClient.EnableBroadcast = true;
        }

        // Method to send barcode and camera IP over UDP
        public void SendBarcode(string barcode, string cameraIp)
        {
            string message = $"{cameraIp}|{barcode}";  // Format: camera IP | barcode
            byte[] data = Encoding.UTF8.GetBytes(message);

            // Send data to the broadcast address and port
            udpClient.Send(data, data.Length, new IPEndPoint(IPAddress.Parse(broadcastIp), port));
            Console.WriteLine($"Broadcasting barcode: {barcode} from camera IP: {cameraIp}");
        }

        // Close the UDP client
        public void udpClose()
        {
            udpClient.Close();
        }
    }

}