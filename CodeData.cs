using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogisticsBaseCSharp;

namespace DemoDWS
{
    // Used with the underlying interface callback image, it already contains copy and release
    public class RawImage
    {
        //Image Properties
        public RawImage(int width, int height, int type, int datasize, IntPtr data, uint imageIndex)
        {
            Width = width;
            Height = height;
            Type = type;
            DataSize = datasize;
            ImageData = data;
            ImageIndex = imageIndex;
        }

        public int Width { get; set; }

        public int Height { get; set; }

        public int Type { get; set; }

        public int DataSize { get; set; }

        public IntPtr ImageData { get; set; }

        public uint ImageIndex { get; set; }

        /// A deep copy of VslbImage will be released during class destruction
        public static implicit operator RawImage(LogisticsAPIStruct.VslbImage image)
        {
            var imgcpy = image.Clone();
            return new RawImage(imgcpy.width, imgcpy.height, imgcpy.type, imgcpy.dataSize, imgcpy.ImageData, image.img_idx);
        }

        /// Free memory, here memory is unmanaged memory
        ~RawImage()
        {
            if (ImageData != IntPtr.Zero)
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(ImageData);
            }
        }
    }

    /// Information on the barcode weight and volume of the package
    public class BaseCodeData
    {
        /// The value is 0, which indicates the barcode information of the package; the value is 1, which indicates the barcode, weight and volume information of the package.
        ///For a package, first report the barcode information of the package, and then report the barcode, weight and volume information of the bound package.
        public int OutputResult { get; set; }

        /// Original image of the package
        public RawImage OriImage { get; set; }

        /// Package pastry cutout
        public RawImage WayImage { get; set; }

        /// The barcode list of packages. If the barcode is not read, there is only one element in the list, and the value is noread
        public List<string> CodeList { get; set; }

        /// A list of arrays of barcode points coordinates to circle the barcodes in the picture with a picture frame
        ///A barcode point coordinate array contains five point coordinates, two of which are the same, making it easier to draw a complete box with a straight line
        public List<Point[]> AreaList { get; set; }

        /// Camera serial number
        public string CameraID { get; set; }

    }

}
