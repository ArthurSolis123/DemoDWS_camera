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
                            else if (retImg.PixelFormat == TJPixelFormats.TJPF_BGR)
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
    }
}
