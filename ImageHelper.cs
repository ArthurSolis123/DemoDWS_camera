using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LogisticsBaseCSharp;
using TurboJpegWrapper;

namespace DemoDWS
{
    public static class ImageHelper
    {
        [Obsolete("please use RawImage.ToBitmap() instead.")]
        public static ImageWrapper CreateImage(RawImage imageInfo)
        {
            return new ImageWrapper(imageInfo.ToBitmap(), null);
        }

        public static Bitmap ToBitmap(this RawImage imageInfo)
        {
            if (imageInfo.ImageData == IntPtr.Zero || imageInfo.DataSize <= 0 || imageInfo.Height <= 0 || imageInfo.Width <= 0)
            {
                return null;
            }

            var type = (LogisticsAPIStruct.EImageType)imageInfo.Type;
            
                switch (type)
                {
                    case LogisticsAPIStruct.EImageType.eImageTypeNormal:
                    case LogisticsAPIStruct.EImageType.eImageTypeBGR:
                        {
                            int channels = (type == LogisticsAPIStruct.EImageType.eImageTypeBGR ? 3 : 1);
                            PixelFormat fmt = (channels == 3 ? PixelFormat.Format24bppRgb : PixelFormat.Format8bppIndexed);
                            Bitmap returnBmp = new Bitmap(imageInfo.Width, imageInfo.Height, fmt);
                            if (channels == 1)
                            {
                                var palette = returnBmp.Palette;
                                for (var ii = 0; ii < 256; ii++)
                                    palette.Entries[ii] = Color.FromArgb(ii, ii, ii);
                                returnBmp.Palette = palette;
                            }

                            var bmpData = returnBmp.LockBits(new Rectangle(0, 0, imageInfo.Width, imageInfo.Height), ImageLockMode.ReadWrite, fmt);
                            if (imageInfo.Width % 4 != 0)
                            {
                                for (int i = 0; i < imageInfo.Height; ++i)
                                {
                                    LogisticsAPI.CopyMemory(bmpData.Scan0 + bmpData.Stride * i, imageInfo.ImageData + imageInfo.Width * channels * i, imageInfo.Width * channels);
                                }
                            }
                            else
                            {
                                LogisticsAPI.CopyMemory(bmpData.Scan0, imageInfo.ImageData, imageInfo.DataSize);
                            }
                            returnBmp.UnlockBits(bmpData);
                            return returnBmp;
                        }
                    case LogisticsAPIStruct.EImageType.eImageTypeJpeg:
                        {
                            using (var tjDecompress = new TJDecompressor())
                            {
                                var imgType = LogisticsAPIStruct.EImageType.eImageTypeNormal;
                                var retImg = tjDecompress.Decompress(imageInfo.ImageData, (ulong)imageInfo.DataSize, TJFlags.NONE);

                                if (retImg.PixelFormat == TJPixelFormats.TJPF_GRAY)
                                {
                                    imgType = LogisticsAPIStruct.EImageType.eImageTypeNormal;
                                }
                                else if( retImg.PixelFormat == TJPixelFormats.TJPF_BGR)
                                {
                                    imgType = LogisticsAPIStruct.EImageType.eImageTypeBGR;
                                }

                                IntPtr tempPtr = Marshal.AllocHGlobal(retImg.Data.Length);

                                Marshal.Copy(retImg.Data, 0, tempPtr, retImg.Data.Length);

                                var rawImg = new RawImage(retImg.Width, retImg.Height, (int)imgType, retImg.Data.Length, tempPtr, imageInfo.ImageIndex);
                                return rawImg.ToBitmap();
                            }
                        }
                    default:
                        break;
                }
            


            return null;
        }

        // 存JPEG, 作为例子,仅演示供参考
        public static string SaveImage(RawImage imageInfo, string path, IDrawingContent drawContent = null)
        {
            if (imageInfo.ImageData == IntPtr.Zero || imageInfo.DataSize <= 0 || imageInfo.Height <= 0 || imageInfo.Width <= 0)
            {
                return null;
            }

            if (drawContent == null && imageInfo.Type == (int)LogisticsBaseCSharp.LogisticsAPIStruct.EImageType.eImageTypeJpeg)
            {
                path = path + ".jpg";
                byte[] bytes = new byte[imageInfo.DataSize];
                System.Runtime.InteropServices.Marshal.Copy(imageInfo.ImageData, bytes, 0, imageInfo.DataSize);
                File.WriteAllBytes(path, bytes);
            }
            else
            {
                path = path + ".jpg";

                if (drawContent != null)
                {
                    using (var image = imageInfo.ToBitmap())
                    {
                        if (image != null)
                        {
                            using (Bitmap bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format24bppRgb))
                            {
                                using (var g = Graphics.FromImage(bitmap))
                                {
                                    Pen p = new Pen(Color.LightGreen, 2);
                                    Font f = new Font("微软雅黑", 56, FontStyle.Regular, GraphicsUnit.Pixel);
                                    var brush = new SolidBrush(Color.LightGreen);

                                    g.DrawImage(image, 0, 0);

                                    if (drawContent != null)
                                    {
                                        g.DrawString(drawContent.ToString(), f, brush, 2, 2);

                                        if (drawContent.AreaPoints != null)
                                        {
                                            foreach (var point in drawContent.AreaPoints)
                                            {
                                                g.DrawPolygon(p, point);
                                            }
                                        }
                                    }
                                }

                                bitmap.Save(path, GetImageFormat());
                            }
                        }
                    }
                }
                else
                {
                    SaveRawImageToJpegInner(imageInfo, path);
                }
            }
            return path;
        }


    private static void SaveRawImageToJpegInner(RawImage image, string path, int imageQuality = 60)
    {
        var bytes = new byte[image.DataSize];
        Marshal.Copy(image.ImageData, bytes, 0, image.DataSize);
        var stride = image.Width * (image.Type == 0 ? 1 : 3);
        var jFormat = image.Type == 0 ? TJPixelFormats.TJPF_GRAY : TJPixelFormats.TJPF_BGR;
        var samOpt = image.Type == 0 ? TJSubsamplingOptions.TJSAMP_GRAY : TJSubsamplingOptions.TJSAMP_422;

        byte[] retBytes = null;

        using (var jCompressor = new TJCompressor())
        {
            retBytes = jCompressor.Compress(bytes, stride, image.Width, image.Height, jFormat, samOpt, imageQuality, TJFlags.NONE);
        }

        if (retBytes != null)
        {
            File.WriteAllBytes(path, retBytes);
        }
    }

    // 存JPEG, 作为例子,仅演示供参考
    public static System.Drawing.Imaging.ImageFormat GetImageFormat()
        {
            /*
            switch (RuntimeConfig.Instance.ImageStorage.Format)
            {
            case ImageStorageConfig.FileFormat.BMP:
                return System.Drawing.Imaging.ImageFormat.Bmp;
            case ImageStorageConfig.FileFormat.JPG:
                return System.Drawing.Imaging.ImageFormat.Jpeg;
            case ImageStorageConfig.FileFormat.PNG:
                return System.Drawing.Imaging.ImageFormat.Png;
            default:
                break;
            }
             * */
            return System.Drawing.Imaging.ImageFormat.Jpeg;
        }


        public interface IDrawingContent
        {
            List<Point[]> AreaPoints { get; set; }
        }

        // 仅演示供参考
        public class DrawBasicMark : IDrawingContent
        {
            private string time;
            private string[] codes;
            private double weight;
            private double length;
            private double width;
            private double height;
            //private string volume;

            public DrawBasicMark(string time, string[] codes, double weight, double length, double width, double height)
            {
                this.time = time;
                this.codes = codes;
                this.weight = weight;
                this.length = length;
                this.width = width;
                this.height = height;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendFormat("时间: {0}\n", time);

                for (int i = 0; i < codes.Length; i++)
                {
                    sb.AppendFormat("条码: {0}\n", codes[i]);
                }

                {
                    sb.AppendFormat("体积: {0} mm * {1} mm * {2} mm\n",
                        Math.Round(length, 2),
                        Math.Round(width, 2),
                        Math.Round(height, 2));
                }

                return sb.ToString();
            }

            public List<Point[]> AreaPoints { get; set; }
        }

    }
}
