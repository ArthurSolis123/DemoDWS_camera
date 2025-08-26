using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DemoDWS
{
    public class LogHelper
    {
        /// <summary>
        /// Log实例，用于打日志
        /// </summary>
        public static log4net.ILog Log = log4net.LogManager.GetLogger("MV Log");
    }
}
