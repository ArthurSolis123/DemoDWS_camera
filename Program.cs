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


        /// The main entry point of the application.
        [STAThread]
        static void Main()
        {

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //Initialize log-related functions
            FileInfo fileInfo = new FileInfo(Environment.CurrentDirectory + "\\log.config");
            log4net.Config.XmlConfigurator.Configure(fileInfo);

            Application.Run(new Form1DWS());
        }
    }
}
