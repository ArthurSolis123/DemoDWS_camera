using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace DemoDWS
{
    static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            AllocConsole();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //初始化日志相关的函数
            FileInfo fileInfo = new FileInfo(Environment.CurrentDirectory + "\\log.config");
            log4net.Config.XmlConfigurator.Configure(fileInfo);

            Application.Run(new Form1DWS());
        }
    }
}
