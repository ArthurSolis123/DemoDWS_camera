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



namespace DemoDWS
{

    public partial class Form1DWS : Form
    {

        #region VARIABLE
        private long _realImgCount = 0;

        /// <summary>
        /// Maximum limit for queues
        /// </summary>
        private const int MAXQUEUESIZE = 64;
        /// <summary>
        /// DWS management instance, providing DWS-related startup operations, stop operations, etc.
        /// </summary>
        private LogisticsWrapper dwsManager;

        /// <summary>
        /// The package information collection reported on the DWS base layer, including the package barcode, weight, volume, original picture, and page sheet picture
        /// </summary>
        private Queue<BaseCodeData> packageItems = new Queue<BaseCodeData>();

        /// <summary>
        /// Threads that process package information, used to process package information collection reported by DWS underlying layer
        /// </summary>
        private Thread processPackageThread;

        /// <summary>
        /// Is DWS running an identity? The value is true after starting DWS, and the value is false after closing DWS.
        /// </summary>
        private bool isRunningDWS = false;

        /// <summary>
        ///The real-time picture number of cameras is associated with containers, storing the number of real-time pictures of cameras
        /// <summary>
        private Dictionary<string, int> realImagecount = new Dictionary<string, int>();

        /// <summary>
        ///The associated container of camera key value and camera hardware information CameraInfos information
        /// <summary>
        private Dictionary<string, CameraInfo> camerInfoMap = new Dictionary<string, CameraInfo>();

        /// <summary>
        /// Component list
        /// </summary>
        private List<PlatformComponent> m_ComponentList = new List<PlatformComponent>();

        #endregion  // VARIABLE

        #region FUNC

        public Form1DWS()
        {
            //Console.WriteLine("initi");
            InitializeComponent();

            //Image display
            var m_imageDisplayArea = new ImageView();
            m_imageDisplayArea.Dock = DockStyle.Fill;
            Pan_Dis.Controls.Add(m_imageDisplayArea);

            // Save the picture
            var m_imageSaver = new ImageSaver();

            m_ComponentList.Add(m_imageDisplayArea);
            m_ComponentList.Add(m_imageSaver);
        }


        /// <summary>
        /// Get all camera information
        /// </summary>
        private void getAllCameraInfos()
        {
            System.Console.WriteLine("[getAllCameraInfos] asking SDK...");
            LogHelper.Log.InfoFormat("[getAllCameraInfos]start getAllCameraInfos");

            var cameraInfoList = dwsManager.GetWorkCameraInfo();
            if (cameraInfoList != null)
            {
                System.Console.WriteLine($"SUCCESS: Found {cameraInfoList.Count()} cameras");
                LogHelper.Log.InfoFormat("The camera successfully obtains device information, the number of cameras {0}", cameraInfoList.Count());
                var list = cameraInfoList.ToList();

                int idx = 0;
                foreach (var cameraInfo in cameraInfoList)
                {
                    if (cameraInfo == null)
                    {
                        System.Console.WriteLine($"[Camera {idx}] ERROR: Camera info is null");
                        idx++;
                        continue;
                    }

                    var key = cameraInfo.camDevExtraInfo;
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        // System.Console.WriteLine($"[Camera {idx}] WARNING: Camera key is empty, using IP instead");
                    }

                    if (!camerInfoMap.ContainsKey(key))
                    {
                        camerInfoMap.Add(key, cameraInfo);
                        // System.Console.WriteLine($"[Camera {idx}] Added to camera map with key: '{key}'");
                    }
                    else
                    {
                        // System.Console.WriteLine($"[Camera {idx}] Already in camera map");
                    }

                    idx++;
                }
            }
            else
            {
                // System.Console.WriteLine("ERROR: No cameras found!");
                LogHelper.Log.InfoFormat("The camera failed to obtain device information");
            }

            LogHelper.Log.InfoFormat("[getAllCameraInfos]end getAllCameraInfos");

            // Print final camera map
            //System.Console.WriteLine($"Final camera map contains {camerInfoMap.Keys.Count} cameras:");
            foreach (var key in camerInfoMap.Keys)
            {
                //System.Console.WriteLine($"  - Camera key: '{key}'");
            }

            var keys = camerInfoMap.Keys.ToList();
            if (keys.Count >= 2)
            {
                var leftKey = keys[0];
                var rightKey = keys[1];

                //System.Console.WriteLine($"Configuring display: LEFT='{leftKey}', RIGHT='{rightKey}'");

                var imgView = m_ComponentList.OfType<ImageView>().FirstOrDefault();
                if (imgView != null)
                {
                    imgView.ConfigureCameraSlots(leftKey, rightKey);
                    //Console.WriteLine($"[ROUTE] LEFT={leftKey}  RIGHT={rightKey}");
                }
            }
            else
            {
                //System.Console.WriteLine($"ERROR: Need at least 2 cameras but only found {keys.Count}");
            }
        }

        /// <summary>
        /// Get all camera status
        /// </summary>
        private void getAllCameraStatus()
        {
            LogHelper.Log.InfoFormat("[getAllCameraStatus]start getAllCameraStatus");

            var cameraInfoList = dwsManager.GetCamerasStatus();
            if (cameraInfoList != null)
            {
                foreach (var cameraInfo in cameraInfoList)
                {
                    LogHelper.Log.InfoFormat("The camera is getting normal: device.Key：{0}， device.UserId：{1}， device.onlineState：{2}"
                        , cameraInfo.key, cameraInfo.deviceUserID, cameraInfo.isOnline ? "Online" : "Offline");
                }
            }
            else
            {
                LogHelper.Log.InfoFormat("The camera failed to obtain device information");
            }

            LogHelper.Log.InfoFormat("[getAllCameraStatus]end getAllCameraStatus");
        }

        private void LogTextOnUI(string text)
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
        private long _pkgCount = 0;
        private long _pkgImgCount = 0;
        /// <summary>
        /// Specific methods for receiving package information reported by DWS
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void PackageInfoCallBack(object o, LogisticsCodeEventArgs e)
        {
            var n1 = Interlocked.Increment(ref _pkgCount);
            Console.WriteLine($"[PackageInfo] {DateTime.Now:HH:mm:ss.fff} got package result #{n1}");
            try
            {
                VolumeInfo vv = new VolumeInfo
                {
                    Length = e.VolumeInfo.length,
                    Width = e.VolumeInfo.width,
                    Height = e.VolumeInfo.height,
                    Volume = e.VolumeInfo.volume,
                };

                BaseCodeData info = new BaseCodeData
                {
                    OutputResult = e.OutputResult,
                    CameraID = e.CameraID,
                    CodeList = e.CodeList,
                    AreaList = e.AreaList,
                    Weight = e.Weight,
                    VolumeInfo = vv,
                    OriImage = e.OriginalImage,
                    WayImage = e.WaybillImage,
                    CodeTimeStamp = e.CodeTimeStamp,
                    CodesInfo = e.CodesInfo,
                    BagTimeInfo = new TimeInfo
                    {
                        TimeCallback = e.Bag_TimeInfo.timeCallback,
                        TimeCodeParse = e.Bag_TimeInfo.timeCodeParse,
                        TimeCollect = e.Bag_TimeInfo.timeCollect,
                        TimeDown = e.Bag_TimeInfo.timeDown,
                        TimeFrameGet = e.Bag_TimeInfo.timeFrameGet,
                        TimeFrameSend = e.Bag_TimeInfo.timeFrameSend,
                        TimeUp = e.Bag_TimeInfo.timeUp,
                        TimVol = e.Bag_TimeInfo.timVol,
                        TimWeight = e.Bag_TimeInfo.timWeight
                    },
                    WeightInfo = new WeightData
                    {
                        Flag = e.WeightData.flag,
                        OrigData = e.WeightData.origData,
                        Weight = e.WeightData.weight,
                        WeightTimeStamp = e.WeightData.weightTimeStamp
                    },
                };
                // --- DEBUG: confirm images arrived ---
                try
                {
                    if (info.OriImage != null || info.WayImage != null)
                    {
                        var n = System.Threading.Interlocked.Increment(ref _pkgImgCount);

                        string ori = (info.OriImage == null)
                            ? "null"
                            : $"{info.OriImage.Width}x{info.OriImage.Height} type={info.OriImage.Type} bytes={info.OriImage.DataSize}";
                        string way = (info.WayImage == null)
                            ? "null"
                            : $"{info.WayImage.Width}x{info.WayImage.Height} type={info.WayImage.Type} bytes={info.WayImage.DataSize}";

                        //Console.WriteLine($"[IMG #{n}] cam='{info.CameraID}'  OriImage={ori}  WaybillImage={way}");
                    }
                    else
                    {
                        //Console.WriteLine("[IMG] no image objects attached on this package.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[IMG] logging error: " + ex.Message);
                }
                // --- end DEBUG ---


                string codes = string.Join("_", e.CodeList);

                if (e.OutputResult == 0)
                {
                    LogHelper.Log.InfoFormat("Callback gets package information (barcode only) {0}", codes);
                }
                else
                {
                    LogHelper.Log.InfoFormat("Callback to get package information (barcode weight and volume) {0}", codes);
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

                if (e.OutputResult != 0)
                {
                    if (camerInfoMap.ContainsKey(info.CameraID))
                    {
                        int i = 0;
                        CameraInfo cameraInfo = camerInfoMap[info.CameraID];

                        LogHelper.Log.InfoFormat("[test cameraInfo]Camera{0} device information device.camDevID：{1}， device.camDevModelName：{2}， device.camDevSerialNumber：{3}, device.camDevVendor：{4}， device.camDevFirewareVersion：{5}， device.camDevExtraInfo：{6}"
                           , i, cameraInfo.camDevID, cameraInfo.camDevModelName, cameraInfo.camDevSerialNumber, cameraInfo.camDevVendor, cameraInfo.camDevFirewareVersion, cameraInfo.camDevExtraInfo);
                    }
                }

                //Name barcode information according to orientation, barcode type, barcode content ---The second way to print barcode
                {
                    string codeInfo = Utils.AppendString1(e.CodesInfo);
                    LogHelper.Log.InfoFormat("[testlog][allCodeInfo]curretn code size:{0} , info is {1}", e.CodesInfo.Length, codeInfo);
                }


            }
            catch (Exception ex)
            {
                LogHelper.Log.Error("Execute PackageInfoCallBack exception", ex);
            }
        }

        /// <summary>
        /// Specific methods for processing package information, such as displaying barcode information, original image, cutout, weight, volume
        /// </summary>
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
                        LogHelper.Log.InfoFormat("Processing parcel information [barcode only]： {0}", string.Join("_", e.CodeList));
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
                            lblCode.Text = string.Format("Barcode content： {0}", codeJoint);
                            lblCodeCount.Text = string.Format("Number of barcodes: {0} Weight: {1} g Length: {2} mm Width: {3} mm Height: {4} mm, Volume: {5} mm3", e.CodeList.Count, e.Weight, e.VolumeInfo.Length, e.VolumeInfo.Width, e.VolumeInfo.Height, e.VolumeInfo.Volume);
                        }));

                        foreach (var comp in m_ComponentList)
                        {
                            comp.OnPacketResultReached(this, e);
                        }

                        //Process the barcode, weight, volume information of the package
                        LogHelper.Log.InfoFormat("Processing parcel information [barcode weight volume]]： {0}", codeJoint);


                        var height = Math.Round(e.VolumeInfo.Height, 2);
                        var width = Math.Round(e.VolumeInfo.Width, 2);
                        var length = Math.Round(e.VolumeInfo.Length, 2);
                        var volume = Math.Round(e.VolumeInfo.Volume, 2);

                        //Inclination angle, angle between the long side of the object and the direction of movement
                        //float angle = e.VolumeInfo.slantAngle;

                        //Point coordinates
                        //var x0 = e.VolumeInfo.vertices[0].x;

                        this.BeginInvoke(new Action(() =>
                        {
                            //Display barcode information
                            LogTextOnUI(DateTime.Now.ToLongTimeString() + " Received parcel information");
                            LogTextOnUI("Barcode " + codeJoint);
                            LogTextOnUI("weight " + e.Weight + "gram");
                            LogTextOnUI("volume " + volume + "Cubic millimeters");
                            LogTextOnUI("length " + length + "mm");
                            LogTextOnUI("width " + width + "mm");
                            LogTextOnUI("high" + height + "mm");
                        }));
                    }
                }
                catch (Exception ee)
                {
                    LogHelper.Log.Error("ProcessPackage internal loop exception", ee);
                }

                Thread.Sleep(10);
            }
        }

        #endregion  // FUNC

        #region CALLBACK

        /// <summary>
        /// 相机断线回调
        /// </summary>
        /// <param name="o"></param>
        /// <param name="cameraKey"></param>
        private void CameraDisconnectCallBack(object o, CameraStatusArgs status)
        {
            LogHelper.Log.ErrorFormat("Camera Status Update Key: {0} UserID: {1} Status: {2}", status.CameraKey, status.CameraUserID, status.IsOnline ? "在线" : "离线");
            LogHelper.Log.ErrorFormat("cameraStatus.IsOnline = {0} ", status.IsOnline);
        }

        /// <summary>
        /// Callback of all camera scan code information after the package is completed
        /// </summary>
        /// <param name="o"></param>
        /// <param name="infoArgs"></param>
        private void AllCameraCodeInfoCBCallBack(object o, AllCameraCodeInfoArgs infoArgs)
        {

            foreach (var codeData in infoArgs.SingleCameraCodeInfoList)
            {
                //Console.WriteLine($"[AllCameraCodeInfo] Camera: '{codeData.Key}'");
                //Console.WriteLine($"  - Codes found: {codeData.CodeList.Count}");

                // Check if image data exists (VslbImage type)
                bool hasImage = (codeData.OriginalImage.ImageData != IntPtr.Zero && codeData.OriginalImage.dataSize > 0);
                //Console.WriteLine($"  - Has image: {(hasImage ? "YES" : "NO")}");

                if (hasImage)
                {
                    //Console.WriteLine($"  - Image size: {codeData.OriginalImage.width}x{codeData.OriginalImage.height}");
                    //Console.WriteLine($"  - Image data size: {codeData.OriginalImage.dataSize} bytes");

                    // Check if this is our industrial camera
                    if (codeData.Key.Contains("CL00212JBY00049") || codeData.Key.Contains("AB3600MG000"))
                    {
                        //Console.WriteLine("  - *** THIS IS THE INDUSTRIAL CAMERA! ***");
                    }

                    // Convert VslbImage to RawImage for display
                    RawImage rawImage = codeData.OriginalImage;  // Uses implicit conversion

                    // Send to ImageView for display
                    foreach (var comp in m_ComponentList)
                    {
                        var pkg = new BaseCodeData
                        {
                            OutputResult = 1,
                            OriImage = rawImage,
                            AreaList = null,
                            CodeList = codeData.CodeList,
                            CameraID = codeData.Key,
                            CodeTimeStamp = codeData.CodeTimeStamp
                        };
                        comp.OnPacketResultReached(this, pkg);
                    }
                }
                else
                {
                    //Console.WriteLine($"  - No image data from camera '{codeData.Key}'");
                }
            }
        }

        /// <summary>
        ///After the package is finished, Ipc camera and barcode puzzle information callback
        /// </summary>
        /// <param name="o"></param>
        /// <param name="infoArgs"></param>
        private void IpcCombineInfoCBCallBack(object o, IpcCombineInfoArgs infoArgs)
        {
            foreach (var comp in m_ComponentList)
            {
                comp.OnPanoramaReached(this, infoArgs);
            }
        }

        /// <summary>
        /// Real-time camera image information callback
        /// </summary>
        /// <param name="o"></param>
        /// <param name="infoArgs"></param>
        private void RealImageCBCallBack(object o, RealImageArgs infoArgs)
        {
            var n = Interlocked.Increment(ref _realImgCount);
            if (n % 30 == 0)
            { // Only log every 30th frame
                Console.WriteLine($"[RealImage] Frame #{n} from {infoArgs.cameraIp}");
            }

            // Check if this is our industrial camera
            if (infoArgs.cameraIp.Contains("CL00212JBY00049") || infoArgs.cameraIp.Contains("AB3600MG000"))
            {
                //Console.WriteLine("[RealImage] *** INDUSTRIAL CAMERA IMAGE RECEIVED! ***");
            }


            if (infoArgs.realImage.ImageData == IntPtr.Zero)
            {
                //Console.WriteLine($"[RealImage] ERROR: ImageData is empty from camera '{infoArgs.cameraIp}'");
                return;
            }


            // Build a "fake" package that contains just an original frame.
            var pkg = new BaseCodeData
            {
                OutputResult = 0,
                OriImage = infoArgs.realImage,
                AreaList = null,
                CodeList = new List<string>(),
                CameraID = infoArgs.cameraIp,
                CodeTimeStamp = 0
            };




            //Console.WriteLine($"[RealImage] Sending image from camera '{pkg.CameraID}' to display components");

            foreach (var comp in m_ComponentList)
            {
                comp.OnPacketResultReached(this, pkg);
            }
        }
        #endregion  // CALLBACK

        #region EVENT
        /// <summary>
        /// Start DWS
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
            //Console.WriteLine($"Init status={status}  ({ErrorInfo.GetErrorMessage(status)})");  // shows if cfg loaded OK
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

            //Turn on the callback function that registers all camera code reading information
            //Enable the callback function that registers all camera code reading information
            bool b = dwsManager.AttachAllCameraCodeinfoCB();
            if (!b)
            {
                //Console.WriteLine("[STARTUP] WARNING: Failed to attach AllCameraCodeInfo callback!");
            }

            //Open the callback function for registering panoramic camera and barcode cutout splicing image information
            bool c = dwsManager.AttachIpcCombineInfoCB();


            //Open the callback function for registering the camera's real-time picture information
            bool d = dwsManager.AttachRealImageCB();

            //MessageBox.Show(b.ToString());

            //Register the method of camera disconnection callback. When the camera in the DWS device is disconnected, the relevant camera information will be called back to the CameraDisconnectCallBack method.
            dwsManager.CameraDisconnectEventHandler += CameraDisconnectCallBack;

            status = dwsManager.Start();//Make judgments on the return value uploaded from the bottom layer and pop up the corresponding pop-up box
            //Console.WriteLine($"Start status={status}  ({ErrorInfo.GetErrorMessage(status)})"); // shows if SDK actually started
            retStr = ErrorInfo.GetErrorMessage(status);
            LogTextOnUI("dwsManager.Start ret val:{" + status + "},ret info:");
            LogTextOnUI("{" + retStr + " }" + "\r\n");
            LogTextOnUI("\r\n");

            LogHelper.Log.InfoFormat("[ dwsManager.Start ret val:{0},ret info:{1}.\n", status, retStr);

            if (status == (int)EAppRunStatus.eAppStatusInitOK)
            {
                //Register package information callback method. When the DWS device scans the package information, it will callback to the PackageInfoCallBack method.
                dwsManager.CodeHandle += PackageInfoCallBack;
                //Register the method of camera disconnection callback. When the camera in the DWS device is disconnected, the relevant camera information will be called back to the CameraDisconnectCallBack method.
                //dwsManager.CameraDisconnectEventHandler += CameraDisconnectCallBack;

                //Scan the code information of all cameras after the registration package is completed
                dwsManager.AllCameraCodeInfoEventHandler += AllCameraCodeInfoCBCallBack;

                //Ipc camera and barcode puzzle information after registration package ends
                dwsManager.IpcCombineInfoEventHandler += IpcCombineInfoCBCallBack;

                //Register camera real-time picture information
                dwsManager.RealImageEventHandler += RealImageCBCallBack;

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

            LogTextOnUI(DateTime.Now.ToLongTimeString() + " Successfully started DWS");

            //Get all camera device information
            getAllCameraInfos();
            getAllCameraStatus();

        }

        /// <summary>
        /// 关闭DWS
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClose_Click(object sender, EventArgs e)
        {
            if (isRunningDWS == false)
            {
                LogTextOnUI(DateTime.Now.ToLongTimeString() + " DWS has been shut down successfully, no need to close it again");
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

            //Cancel the code scan information of all cameras after the package is completed
            dwsManager.AllCameraCodeInfoEventHandler -= AllCameraCodeInfoCBCallBack;

            //After the package is cancelled, the Ipc camera and barcode puzzle information is completed.
            dwsManager.IpcCombineInfoEventHandler -= IpcCombineInfoCBCallBack;

            //Unregister the camera's real-time picture information
            dwsManager.RealImageEventHandler -= RealImageCBCallBack;

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

            LogTextOnUI(DateTime.Now.ToLongTimeString() + " 关闭DWS成功");
        }


        /// <summary>
        /// View the configuration file of the underlying SDK
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSetParam_Click(object sender, EventArgs e)
        {
            if (isRunningDWS == true)
            {
                return;
            }

            Process.Start(@".\Cfg\LogisticsBase.cfg");
        }

        /// <summary>
        /// View client logs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCSLog_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", @".\CSLog");
        }

        /// <summary>
        /// View the underlying SDK log
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLog_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", @".\Log");
        }

        /// <summary>
        /// Clear the historical barcode list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClearCodeList_Click(object sender, EventArgs e)
        {
            listBoxLog.Items.Clear();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (isRunningDWS == true)
            {
                //Console.WriteLine("=== MANUAL TRIGGER PRESSED ===");
                //Console.WriteLine("Triggering both cameras...");

                int status = dwsManager.CameraSoftTrigger();//Execute a single soft trigger
                if (0 == status)
                {
                    //Console.WriteLine("Soft trigger executed successfully");
                    LogTextOnUI(DateTime.Now.ToLongTimeString() + " Soft trigger execution succeeded");
                }
                else
                {
                    //Console.WriteLine($"Soft trigger failed with status: {status}");
                    LogTextOnUI(DateTime.Now.ToLongTimeString() + " Execution soft trigger failed");
                    LogTextOnUI("\r\n");
                }

                //Console.WriteLine("=== TRIGGER COMPLETE ===");
            }
            else
            {
                LogTextOnUI(DateTime.Now.ToLongTimeString() + " The soft trigger failed, please start the DWS software first");
                LogTextOnUI("\r\n");
            }
        }

        private void MainForm_Closing(object sender, EventArgs e)
        {
            this.btnClose_Click(this, new EventArgs());
        }

        #endregion  // EVENT

        private void lblCode_Click(object sender, EventArgs e)
        {

        }
    }
}
