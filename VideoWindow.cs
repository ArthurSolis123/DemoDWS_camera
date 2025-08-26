using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LogisticsBaseCSharp;

namespace DemoDWS
{
    public class VideoFrame
    {
        private List<Overlay> m_overlays;

        public Image Image { get; set; }

        public List<Overlay> Overlays
        {
            get
            {
                if (m_overlays == null)
                {
                    m_overlays = new List<Overlay>();
                }
                return m_overlays;
            }
        }

        public bool IsValid { get { return this.Image != null; } }

        public void Paint(Graphics g, Size size)
        {
            if (IsValid)
            {
                var image = this.Image;
                float rateX = (float)size.Width / Image.Width;
                float rateY = (float)size.Height / Image.Height;
                float rate = Math.Min(rateX, rateY);

                // draw image
                //按照图片宽高和控件宽高比小的那个，显示图片
                var targetSize = new SizeF(Image.Width * rate, Image.Height * rate).ToSize();
                //图片在控件全屏显示
                //var targetSize = size;
                var loc = new Point((size.Width - targetSize.Width) / 2, (size.Height - targetSize.Height) / 2);
                g.DrawImage(Image, new Rectangle(loc, targetSize), new Rectangle(Point.Empty, Image.Size), GraphicsUnit.Pixel);

                // draw overlays
                foreach (var ov in this.Overlays)
                {
                    ov.Paint(g, loc, rate);
                }
            }
        }

        public class Overlay
        {
            public Color ForceColor { get; set; }

            public int BorderWidth { get; set; }

            public Overlay()
            {
                this.ForceColor = Color.Green;
                this.BorderWidth = 3;
            }

            public virtual void Paint(Graphics g, Point offset, float rate)
            {

            }
        }

        public class OverlayPolygon : Overlay
        {
            private List<Point> m_points = new List<Point>();

            public List<Point> Points
            {
                get { return m_points; }
            }

            public OverlayPolygon()
            {
            }

            public OverlayPolygon(List<Point> points)
            {
                m_points = points;
            }

            public override void Paint(Graphics g, Point offset, float rate)
            {
                if (m_points.Count > 2)
                {
                    var pts = this.Points.ToArray();
                    if (!offset.IsEmpty)
                    {
                        for (int i = 0; i < pts.Length; ++i)
                        {
                            pts[i].X = (int)(rate * pts[i].X);
                            pts[i].Y = (int)(rate * pts[i].Y);
                            pts[i].Offset(offset);
                        }
                    }

                    using (var pen = new Pen(this.ForceColor, this.BorderWidth))
                    {
                        g.DrawPolygon(pen, pts);
                    }
                }
            }
        }
    }

    public class VideoWindow : UserControl
    {
        private VideoFrame m_frame;
        //private Image m_noFrameImage;

        public VideoFrame Frame
        {
            get { return m_frame; }
            set { m_frame = value; this.Invalidate(); Console.WriteLine("[UI] frame set"); }
        }

        public new string Text
        {
            get { return base.Text; }
            set { base.Text = value; this.Invalidate(); }
        }

        public VideoWindow()
        {
            InitUI();
        }

        private void InitUI()
        {
            this.Margin = Padding.Empty;
            this.Padding = Padding.Empty;
            this.DoubleBuffered = true;
            this.BackColor = Color.FromKnownColor(KnownColor.Control);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;

            // draw image
            if (m_frame != null)
            {
                m_frame.Paint(g, this.Size);
            }

            // draw text
            if (this.Text.Length > 0)
            {
                g.DrawString(this.Text, this.Font, Brushes.Black, 5, this.Height - 20);
            }
        }
    }

    public class MultiVideoWindow : UserControl
    {
        int m_rowCount = 0;
        int m_columnCount = 0;
        Dictionary<Position, VideoWindow> m_windows = new Dictionary<Position, VideoWindow>();
        TableLayoutPanel m_tablePanel = new TableLayoutPanel();

        public int Row
        {
            get { return m_rowCount; }
        }

        public int Column
        {
            get { return m_columnCount; }
        }

        public struct Position
        {
            int x;
            int y;

            public Position(int x_, int y_)
            {
                x = x_;
                y = y_;
            }
        }

        public MultiVideoWindow(int rowCount = 1, int colCount = 2)
        {
            m_tablePanel.Margin = Padding.Empty;
            m_tablePanel.Padding = Padding.Empty;
            m_tablePanel.Dock = DockStyle.Fill;
            this.Controls.Add(m_tablePanel);
            this.Font = new Font("微软雅黑", 10);
            this.BackColor = Color.FromKnownColor(KnownColor.ActiveBorder);
            //this.BackColor = Color.FromArgb(36, 40, 79);

            SetLayout(rowCount, colCount);
        }

        public void SetLayout(int rowCount, int colCount)
        {
            if (m_rowCount == rowCount && m_columnCount == colCount)
            {
                return;
            }

            m_windows.Clear();
            m_tablePanel.Controls.Clear();

            m_rowCount = rowCount;
            m_columnCount = colCount;

            m_tablePanel.RowCount = rowCount;
            m_tablePanel.ColumnCount = colCount;
            m_tablePanel.RowStyles.Clear();
            m_tablePanel.ColumnStyles.Clear();

            for (int y = 0; y < m_rowCount; ++y)
            {
                m_tablePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 1f));

                for (int x = 0; x < m_columnCount; ++x)
                {
                    if (y == 0)
                    {
                        m_tablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 1f));
                    }

                    var window = new VideoWindow();
                    window.Dock = DockStyle.Fill;
                    window.Margin = new Padding(1);
                    m_windows.Add(new Position(x, y), window);
                    m_tablePanel.Controls.Add(window, x, y);
                }
            }
        }

        public VideoWindow GetWindow(SourceType type)
        {
            switch (type)
            {
                case SourceType.None:
                    break;
                case SourceType.OrigianlImage:
                    {
                        var pos = new Position(0, 0);
                        if (m_windows.ContainsKey(pos))
                        {
                            return m_windows[pos];
                        }
                    }
                    break;
                case SourceType.MattingImage:
                    {
                        var pos = new Position(1, 0);
                        if (m_windows.ContainsKey(pos))
                        {
                            return m_windows[pos];
                        }
                    }
                    break;
                case SourceType.PanoramicImage:
                    {
                        var pos = new Position(2, 0);
                        if (m_windows.ContainsKey(pos))
                        {
                            return m_windows[pos];
                        }
                    }
                    break;
                default:
                    break;
            }

            return null;
        }

        public void ClearImage()
        {
            foreach (var p in m_windows)
            {
                p.Value.Text = string.Empty;
                p.Value.Frame = null;
            }
        }
    }
}
