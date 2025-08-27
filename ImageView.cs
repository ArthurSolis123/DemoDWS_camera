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
            // Prefer original; fall back to matting if needed
            var img = arg.OriImage ?? arg.WayImage;
            if (img == null || img.Width <= 0 || img.Height <= 0)
            {
                return;
            }

            // Decide which tile to use by camera key
            if (!string.IsNullOrEmpty(_leftCamKey) && arg.CameraID == _leftCamKey)
            {
                ShowImage(SourceType.OrigianlImage, arg.CameraID, img, arg.AreaList);
            }
            else if (!string.IsNullOrEmpty(_rightCamKey) && arg.CameraID == _rightCamKey)
            {
                ShowImage(SourceType.MattingImage, arg.CameraID, img, arg.AreaList);
            }
            else
            {
                ShowImage(SourceType.OrigianlImage, arg.CameraID, img, arg.AreaList);
            }
        }


        private void ShowImage(SourceType type, string cameraId, RawImage image, List<Point[]> areaList)
        {
            VideoFrameInfo frameInfo = new VideoFrameInfo();
            frameInfo.frame = new VideoFrame();
            frameInfo.rawImage = image;

            if (areaList != null)
            {
                for (int i = 0; i < areaList.Count; i++)
                {
                    var area = areaList[i];
                    var ov = new VideoFrame.OverlayPolygon();
                    ov.Points.AddRange(area);
                    frameInfo.frame.Overlays.Add(ov);
                }
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
                m_dictFrame[type] = frameInfo;
            }
        }


        // Demo for reference only. Used to refresh images on the interface. Supports JPEG images
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

                // Image decoding
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
