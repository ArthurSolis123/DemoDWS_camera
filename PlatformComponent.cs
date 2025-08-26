using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DemoDWS
{
    /// <summary>
    /// 平台UI组件, 对应主界面上的各个面板
    /// </summary>
    public class WindowPanel : UserControl, PlatformComponent
    {
        public WindowPanel()
        {
        }

        /// <summary>
        /// 清除数据
        /// </summary>
        public virtual void clear()
        {
        }

        /// <summary>
        /// 初始化, 创建平台对象时被调用
        /// </summary>
        /// <returns></returns>
        public virtual int Init()
        {
            return 0;
        }

        /// <summary>
        /// 反初始化, 删除平台对象时被调用
        /// </summary>
        public virtual void Uninit()
        {
        }

        /// <summary>
        /// 启动组件, 点击主界面启动按钮时被调用
        /// </summary>
        /// <returns></returns>
        public virtual int Start()
        {
            return 0;
        }

        /// <summary>
        /// 停止组件, 点击主界面停止按钮时被调用
        /// </summary>
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

        public virtual void OnPanoramaReached(object o, LogisticsBaseCSharp.IpcCombineInfoArgs arg)
        {

        }

        public virtual void OnAllCameraCodeInfoReached(object sender, LogisticsBaseCSharp.AllCameraCodeInfoArgs e)
        {
        }
    }


    /// <summary>
    /// 平台组件接口
    /// </summary>
    public interface PlatformComponent
    {
        /// <summary>
        /// 初始化, 创建平台时被调用
        /// </summary>
        /// <returns></returns>
        int Init();

        /// <summary>
        /// 反初始化, 销毁平台时被调用
        /// </summary>
        void Uninit();

        /// <summary>
        /// 启动组件, 启动工程时被调用
        /// </summary>
        /// <returns></returns>
        int Start();

        /// <summary>
        /// 停止组件, 停止工程时被调用
        /// </summary>
        void Stop();

        /// <summary>
        /// 相机连接事件处理函数
        /// </summary>
        /// <param name="arg"></param>
        void OnCameraConnectionChanged(object o, LogisticsBaseCSharp.CameraStatusArgs arg);

        /// <summary>
        /// 实时图片处理函数
        /// </summary>
        /// <param name="arg"></param>
        void OnRealTimeImageReached(object o, LogisticsBaseCSharp.RealImageArgs arg);

        /// <summary>
        /// 包裹结果处理函数
        /// </summary>
        /// <param name="arg"></param>
        void OnPacketResultReached(object o, BaseCodeData arg);

        /// <summary>
        /// 实时错误信息回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arg"></param>
        void OnErrorInfoCallBack(object sender, LogisticsBaseCSharp.RealErrorInfo arg);

        /// <summary>
        /// 全景图回调
        /// </summary>
        /// <param name="o"></param>
        /// <param name="arg"></param>
        void OnPanoramaReached(object o, LogisticsBaseCSharp.IpcCombineInfoArgs arg);

        /// <summary>
        /// 所有相机回调
        /// </summary>
        /// <param name="o"></param>
        /// <param name="arg"></param>
        void OnAllCameraCodeInfoReached(object sender, LogisticsBaseCSharp.AllCameraCodeInfoArgs e);
    }
    
    /// <summary>
    /// 主界面面板类型
    /// </summary>
    public enum EPanelType
    {
        Display,              ///< 图像显示

        Num                   ///< 面板总数, 新面板在此之前定义
    }
}
