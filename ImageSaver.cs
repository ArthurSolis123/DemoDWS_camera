using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace DemoDWS
{
    public class ImageSaver : PlatformComponent
    {
        public class ImageStorageInfo
        {
            public List<string> CodeList { get; set; }

            public int CodeCount
            {
                get
                {
                    return CodeList != null ? CodeList.Count : 0;
                }
            }

            public RawImage Image { get; set; }

            public Int64 Timestamp { get; set; }

            public string CameraID { get; set; }

            public ImageSource Source { get; set; }

        }

        private Thread imageSaveThread;
        private bool isRunning = false;
        private Queue<ImageStorageInfo> m_imageQueue = new Queue<ImageStorageInfo>();

        private const int MaxImageQueueSize = 128;

        public ImageSaver()
        {

        }

        public int Init()
        {
            return 0;
        }

        public void Uninit()
        {

        }

        public int Start()
        {
            imageSaveThread = new Thread(HandleQueueLoop);
            isRunning = true;
            imageSaveThread.Start();
            return 0;
        }

        public void Stop()
        {
            isRunning = false;
            if (imageSaveThread != null && imageSaveThread.IsAlive)
            {
                imageSaveThread.Join();
            }
        }

        public void OnCameraConnectionChanged(object o, LogisticsBaseCSharp.CameraStatusArgs arg)
        {
        }

        public void OnRealTimeImageReached(object o, LogisticsBaseCSharp.RealImageArgs arg)
        {
        }

        public void OnPacketResultReached(object o, BaseCodeData arg)
        {
            if (arg.OutputResult == 1)
            {
                ImageStorageInfo info = new ImageStorageInfo();
                info.CodeList = arg.CodeList;
                info.Source = ImageSource.OriginalImage;
                info.Image = arg.OriImage;
                info.Timestamp = arg.CodeTimeStamp;
                info.CameraID = arg.CameraID;
                AddImage(info);
            }
        }

        public void OnErrorInfoCallBack(object sender, LogisticsBaseCSharp.RealErrorInfo arg)
        {
            // ignore
        }

        public void OnPanoramaReached(object o, LogisticsBaseCSharp.IpcCombineInfoArgs arg)
        {
            // 全景需绑定条码, 此处不做演示
        }

        public void OnAllCameraCodeInfoReached(object sender, LogisticsBaseCSharp.AllCameraCodeInfoArgs e)
        {
            foreach (var codeData in e.SingleCameraCodeInfoList)
            {
                ImageStorageInfo info = new ImageStorageInfo();
                info.CodeList = codeData.CodeList;
                info.Source = ImageSource.AllCamImage;
                info.Image = codeData.OriginalImage;
                info.Timestamp = codeData.CodeTimeStamp;
                info.CameraID = codeData.Key;
                AddImage(info);
            }
        }

        /// <summary>
        /// 添加图片到队列
        /// </summary>
        /// <param name="imageInfo"></param>
        private void AddImage(ImageStorageInfo imageInfo)
        {
            if (imageInfo.Image.ImageData != IntPtr.Zero)
            {
                lock (m_imageQueue)
                {
                    if (m_imageQueue.Count < MaxImageQueueSize)
                    {
                        m_imageQueue.Enqueue(imageInfo);
                    }
                    else
                    {
                        LogHelper.Log.Error("[ImageSaver][AddImage] Can't keep up, is the computer overloaded?");
                    }
                }
            }
        }

        private void HandleQueueLoop()
        {
            while (isRunning)
            {
                ImageStorageInfo imageInfo = null;
                lock (m_imageQueue)
                {
                    if (m_imageQueue.Count > 0)
                    {
                        imageInfo = m_imageQueue.Dequeue();
                    }
                }

                if (imageInfo == null)
                {
                    Thread.Sleep(100);
                    continue;
                }

                SaveImage(imageInfo);
            }
        }

        /// <summary>
        ///  保存图片
        /// </summary>
        /// <param name="imageInfo"></param>
        private void SaveImage(ImageStorageInfo imageInfo)
        {
            if (imageInfo.Image.ImageData == IntPtr.Zero)
            {
                return;
            }

            string dir = Path.Combine("./SaveImages", imageInfo.Source.ToString());
            if (!Directory.Exists(dir))
            {
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch (PathTooLongException ex)
                {
                    LogHelper.Log.Error("[CompImageSaver][SaveImage] Path is too long for creating directory, skipping saving image", ex);
                    return;
                }
                catch (Exception uex)
                {
                    LogHelper.Log.Error("[CompImageSaver][SaveImage] unknow exception while creating directory, error msg: " + uex.Message, uex);
                    return;
                }
            }

            string name = string.Format("{0}_{1}_{2}", string.Join("_", imageInfo.CodeList), imageInfo.Timestamp, DateTime.Now.ToString("HHmmss_fff"));
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            try
            {
                // 保存文件
                string path = Path.Combine(dir, name);
                string finalPath = string.Empty;

                finalPath = ImageHelper.SaveImage(imageInfo.Image, path);
            }
            catch (Exception ex)
            {
                LogHelper.Log.Error("Save image file error.", ex);
            }
        }
    }
}
