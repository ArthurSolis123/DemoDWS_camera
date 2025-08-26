/*
 * 自封装图片类
 * 用于解决JPEG图片内存泄露问题
 * 
 * */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace DemoDWS
{
    /// <summary>
    /// 图片封装
    /// 请保持ImageWrapper和Image的生命周期一致
    /// </summary>
    public class ImageWrapper : IDisposable
    {
        private bool disposed = false;

        public Image Image { get; set; }

        private MemoryStream MemoryStream { get; set; }

        public ImageWrapper(Image bitmap, MemoryStream ms)
        {
            Image = bitmap;
            MemoryStream = ms;
        }

        ~ImageWrapper()
        {
            Dispose(false);
        }

        private void clean()
        {
            if (Image != null)
            {
                Image.Dispose();
                Image = null;
            }

            if (MemoryStream != null)
            {
                MemoryStream.Close();
                MemoryStream.Dispose();
                MemoryStream = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            //if (disposing)
            {
                clean();
                disposed = true;

                // 图片资源清理过慢, 需要强制GC清理图片资源
                // 主要用于避免狂扫处理大量图片时图片积压在内存里
                // 可能会在GC自动清理前导致软件内存不足闪退
                GC.Collect();
            }

            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
