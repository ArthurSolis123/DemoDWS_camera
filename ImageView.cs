/*
 * 图片异步显示
 * 过程仅供参考
 * 图片解码统一使用ImageHelper.CreateImage
 * 
 * */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using LogisticsBaseCSharp;

namespace DemoDWS
{
    public partial class ImageView : WindowPanel
    {
        private MultiVideoWindow m_videoWindow;
        private Thread m_displayThread;
        private bool m_running = false;


        private Dictionary<SourceType, VideoFrameInfo> m_dictFrame = new Dictionary<SourceType, VideoFrameInfo>();
        // ImageView.cs (inside class ImageView)
        private string _leftCamKey = null;
        private string _rightCamKey = null;

        public void ConfigureCameraSlots(string leftCamKey, string rightCamKey)
        {
            _leftCamKey = leftCamKey;
            _rightCamKey = rightCamKey;
            //Console.WriteLine($"[CFG] Left='{_leftCamKey}', Right='{_rightCamKey}'");
        }

        public ImageView()
        {
            InitializeComponent();

            m_videoWindow = new MultiVideoWindow(1, 2);
            m_videoWindow.Dock = DockStyle.Fill;
            this.Controls.Add(m_videoWindow);
        }

        struct VideoFrameInfo
        {
            public VideoFrame frame;
            public string text;
            public RawImage rawImage;
        }

        public override int Start()
        {
            m_displayThread = new Thread(this.DisplayThreadProc);
            m_running = true;
            m_displayThread.Start();

            return base.Start();
        }

        public override void Stop()
        {
            if (m_displayThread != null)
            {
                m_running = false;
                m_displayThread.Join();
            }
        }

        public override void OnPacketResultReached(object o, BaseCodeData arg)
        {
            //Console.WriteLine($"[ImageView] Received packet from camera: '{arg.CameraID}'");
            //Console.WriteLine($"[ImageView] Left camera key: '{_leftCamKey}', Right camera key: '{_rightCamKey}'");

            // Prefer original; fall back to matting if needed
            var img = arg.OriImage ?? arg.WayImage;
            if (img == null || img.Width <= 0 || img.Height <= 0)
            {
                //Console.WriteLine("[ImageView] ERROR: No usable image in packet");
                return;
            }

            //Console.WriteLine($"[ImageView] Valid image: {img.Width}x{img.Height}");

            // Decide which tile to use by camera key
            if (!string.IsNullOrEmpty(_leftCamKey) && arg.CameraID == _leftCamKey)
            {
                //Console.WriteLine($"[ImageView] Displaying on LEFT tile for camera: {arg.CameraID}");
                ShowImage(SourceType.OrigianlImage, arg.CameraID, img, arg.AreaList);
            }
            else if (!string.IsNullOrEmpty(_rightCamKey) && arg.CameraID == _rightCamKey)
            {
                //Console.WriteLine($"[ImageView] Displaying on RIGHT tile for camera: {arg.CameraID}");
                ShowImage(SourceType.MattingImage, arg.CameraID, img, arg.AreaList);
            }
            else
            {
                //Console.WriteLine($"[ImageView] No matching camera slot found for: '{arg.CameraID}'");
                //Console.WriteLine($"[ImageView] Available slots - Left: '{_leftCamKey}', Right: '{_rightCamKey}'");
                // Fallback: send to left
                //Console.WriteLine($"[ImageView] Using FALLBACK LEFT tile for camera: {arg.CameraID}");
                ShowImage(SourceType.OrigianlImage, arg.CameraID, img, arg.AreaList);
            }
        }

        public override void OnPanoramaReached(object o, IpcCombineInfoArgs arg)
        {
            ShowImage(SourceType.PanoramicImage, arg.PicInfo, arg.ipcImage, null);
        }

        private void ShowImage(SourceType type, string cameraId, RawImage image, List<Point[]> areaList)
        {
            //Console.WriteLine($"[ShowImage] Attempting to show image from camera '{cameraId}' on {type}");
            //Console.WriteLine($"[ShowImage] Image size: {image?.Width}x{image?.Height}");

            VideoFrameInfo frameInfo = new VideoFrameInfo();
            frameInfo.frame = new VideoFrame();
            frameInfo.rawImage = image;

            //Console.WriteLine($"[OVERLAY DEBUG] Creating overlays for {cameraId}:");
            //Console.WriteLine($"  - AreaList count: {areaList?.Count ?? 0}");
            if (areaList != null)
            {
                for (int i = 0; i < areaList.Count; i++)
                {
                    var area = areaList[i];
                    //Console.WriteLine($"  - Area {i}: {area?.Length ?? 0} points");
                    if (area != null && area.Length > 0)
                    {
                        //Console.WriteLine($"    First point: ({area[0].X}, {area[0].Y})");
                        //Console.WriteLine($"    Last point: ({area[area.Length - 1].X}, {area[area.Length - 1].Y})");
                    }

                    var ov = new VideoFrame.OverlayPolygon();
                    ov.Points.AddRange(area);
                    frameInfo.frame.Overlays.Add(ov);
                }
                //Console.WriteLine($"  - Total overlays created: {frameInfo.frame.Overlays.Count}");
            }
            else
            {
                //Console.WriteLine("  - No area list provided for overlay creation");
            }


            if (areaList != null)
            {
                foreach (var area in areaList)
                {
                    var ov = new VideoFrame.OverlayPolygon();
                    ov.Points.AddRange(area);
                    frameInfo.frame.Overlays.Add(ov);
                }
            }
            frameInfo.text = type.ToString();
            lock (m_dictFrame)
            {
                // m_dictFrame.Add(type, frameInfo);
                m_dictFrame[type] = frameInfo;
            }
        }


        // 仅演示供参考 用于界面刷新图像 支持JPEG图片
        private void DisplayThreadProc()
        {
            VideoFrame lastFrm = null;
            while (m_running)
            {
                KeyValuePair<SourceType, VideoFrameInfo>? pair = null;

                lock (m_dictFrame)
                {
                    if (m_dictFrame.Count > 0)
                    {
                        pair = m_dictFrame.First();
                        m_dictFrame.Remove(pair.Value.Key);
                    }
                }

                if (pair == null)
                {
                    Thread.Sleep(100);
                    continue;
                }

                var p = pair.Value;

                // 图片解码
                p.Value.frame.Image = ImageHelper.ToBitmap(p.Value.rawImage);

                this.BeginInvoke(new Action(() =>
                {
                    var window = m_videoWindow.GetWindow(p.Key);
                    if (window != null)
                    {
                        lastFrm = window.Frame;
                        window.Frame = p.Value.frame;
                        window.Text = p.Value.text;

                        if (lastFrm != null && lastFrm.Image != null)
                        {
                            lastFrm.Image.Dispose();
                            lastFrm.Image = null;
                            lastFrm = null;
                        }
                    }
                }));
            }
        }
    }

    public enum SourceType
    {
        None,
        OrigianlImage,
        MattingImage,
        PanoramicImage,
    }
}
