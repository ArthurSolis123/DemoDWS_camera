using LogisticsBaseCSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DemoDWS
{
    public static class Utils
    {
        /// <summary>        
        /// 时间戳转为C#格式时间        
        /// </summary>        
        /// <param name=”timeStamp”></param>        
        /// <returns></returns>        
        public static DateTime ConvertStringToDateTime(string timeStamp)
        {
            DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long lTime = long.Parse(timeStamp + "0000");
            TimeSpan toNow = new TimeSpan(lTime);
            return dtStart.Add(toNow);
        }

        // <summary>        
        /// 时间戳转为string格式        
        /// </summary>        
        /// <param name=”timeStamp”></param>        
        /// <returns></returns>       
        public static string ConvertStringToDateTime(long timeStamp)
        {
            DateTime UnixTimeBase = new DateTime(1970, 1, 1).ToLocalTime();
            var dt = UnixTimeBase.AddMilliseconds(timeStamp);
            return dt.ToString("yyyy/MM/dd HH:mm:ss.fff");
        }

        /// <summary>
        /// 以方位+类型+条码的格式拼接条码信息
        /// </summary>
        /// <param name="CodesInfo"></param>
        /// <returns></returns>
        public static string AppendString1(SingleCodeInfo[] CodesInfo)
        {
            try
            {
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < CodesInfo.Length; i++)
                {
                    SingleCodeInfo codes = CodesInfo[i];
                    if (i != 0)
                    {
                        builder.Append(",");
                    }
                    string typeStr = codes.CodeTypeP == SingleCodeInfo.CodeType.Barcode ? "1D" : "2D";
                    builder.Append(codes.Position + "_" + typeStr + "_" + codes.Code);
                }

                return builder.ToString();
            }
            catch (Exception ex)
            {
                LogHelper.Log.Error("执行AppendString1异常", ex);
                return string.Empty;
            }
        }
    }
}
