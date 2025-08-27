using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DemoDWS
{

    /// Platform UI components, corresponding to various panels on the main interface
    public class WindowPanel : UserControl, PlatformComponent
    {
        /// Initialization, called when creating platform object
        public virtual int Init()
        {
            return 0;
        }

        /// De-initialization, called when deleting platform objects
        public virtual void Uninit()
        {
        }

        /// Start the component, it is called when clicking the start button on the main interface
        public virtual int Start()
        {
            return 0;
        }


        /// Stop component, called when clicking the stop button on the main interface
        public virtual void Stop()
        {
        }

        public virtual void OnCameraConnectionChanged(object o, LogisticsBaseCSharp.CameraStatusArgs arg)
        {
        }

        public virtual void OnRealTimeImageReached(object o, LogisticsBaseCSharp.RealImageArgs arg)
        {
        }

        public virtual void OnPacketResultReached(object o, BaseCodeData arg)
        {
        }

        public virtual void OnErrorInfoCallBack(object sender, LogisticsBaseCSharp.RealErrorInfo arg)
        {
        }

        public virtual void OnAllCameraCodeInfoReached(object sender, LogisticsBaseCSharp.AllCameraCodeInfoArgs e)
        {
        }
    }

    /// Platform Component Interface
    public interface PlatformComponent
    {

        /// Initialize, called when creating the platform
        int Init();

        /// De-initialization, called when destroying the platform
        void Uninit();

        /// Start the component, called when starting the project
        int Start();

        /// Stop component, called when stopping the project
        void Stop();

        /// Camera connection event handling function
        void OnCameraConnectionChanged(object o, LogisticsBaseCSharp.CameraStatusArgs arg);


        /// Real-time image processing function
        void OnRealTimeImageReached(object o, LogisticsBaseCSharp.RealImageArgs arg);

        /// Package result processing function
        void OnPacketResultReached(object o, BaseCodeData arg);

        /// Real-time error message callback
        void OnErrorInfoCallBack(object sender, LogisticsBaseCSharp.RealErrorInfo arg);

        /// All camera callbacks
        void OnAllCameraCodeInfoReached(object sender, LogisticsBaseCSharp.AllCameraCodeInfoArgs e);
    }

}
