using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DemoDWS
{
    /// <summary>
    /// 错误信息
    /// </summary>
    public static class ErrorInfo
    {
        private static Dictionary<int, string> AppRunStatusInfo = new Dictionary<int, string>();

        static ErrorInfo()
        {
            AppRunStatusInfo.Add((int)EAppRunStatus.eAppStatusInitOK, "App run ok");
            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusIvalidHandlerror, "App run failed,app handle is null");
            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusInitialAgainError, "App already init,please close and reOpen");
            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusAlreadyRunError, "App already run ,please close and reOpen");
            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusNoEncryptedDog, "No EncrypDog ,please check or reinsertion");
            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusAlgorithmError, "EncrypDog,Algorithm init failed,pelase ensure there enough memory for algthorithm");


            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusNoCfg, "No cfg files,please check File path and File Suffixes");
            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusCfgError, "Cfg elem getfailed,please File content format or Whether the version information matches");

            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusBarcodeAlgError, "Algorith init failed,please check barcode Algorith param");
            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusBarcodeAlgfinError, "Barcode Algorith finalize failed");
            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusDMcodeAlgError, "Algorith init failed,please check datacode Algorith param");
            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusDMcodeAlgfinError, "Datacode Algorith finalize failed");
            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusMattingAlgError, "Algorith init failed,please check matting Algorith param");
            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusMattingAlgfinError, "Matting Algorith finalize failed");
            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusIpcGrayAlgError, "Algorith init failed,please check ipcGray Algorith param");
            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusIpcGrayAlgfinError, "IpcGray Algorith finalize failed");

            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusCameraNumError, "Init camera mode failed,Camera num not match ,please check Whether enough camera Online");
            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusCameraOpendError, "Init camera mode failed, one or more camera has opend,please check and find it");
            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusCameraListNumError, "Init camera mode failed, required camera list size not equal to CfgCameras nums");
            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusCameraListUnmatch, "Init camera mode failed,one or more camera within required cameralist, not online,please check and find it");
            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusSoftEncryptionError, "Init camera mode failed, one or more camera unauthorized ,please ensure all camera be authorized ");
            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusIpcCameraError, "Init ipc camera failed, please check whether ipc camera online or opend");
            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusNtpServerError, "Init NtpServer failed, please check computer related configuration");

            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatus3DCameraError, "3D init failed,please ensure 3D VolumeCamera can operation under the corresponding software and reOpend ! ");

            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusWeightError, "Weight mode init failed,please check cfg param then ensure can get correct protocol data by corresponding Tools");

            AppRunStatusInfo.Add((int)EAppRunStatus.eRunStatusCodeRuleFilterError, "RuleFilter mode init failed,plrease check cfg param format");
        }

        public static string GetErrorMessage(int errorCode)
        {
            string value = "";
            if (AppRunStatusInfo.TryGetValue(errorCode, out value))
            {
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
            return "Unknown error";
        }
    }


    /// <summary>
    /// 启动DWS的时候，可能的一些异常返回信息
    /// </summary>
    public enum InitStatus
    {
        StatusSuccess = 0,
        StatusNoEncryptedDog = -1,                    //未检测到加密狗
        StatusAlgorithmError = -2,                    //算法初始化失败
        StatusModulReadCodeError = -3,                //读码模块初始化失败
        StatusModulReadWeightError = -4,              //称重模块初始化失败 
        StatusCodeRuleFilterError = -5,               //条码过滤模块初始化失败
        StatusModuleOutputError = -6,                 //输出模块初始化失败
        Status3DCameraError = -7,                     //3D测体积模块初始化失败
        StatusCarmeraCountError = -8,                 //相机实际个数小于配置个数
        StatusCFGError = -9,                          //底层配置初始化失败
        StatusInitialAgainError = -10,                //SDK多次初始化异常
    }

    /// <summary>
    /// 启动DWS的时候，可能的一些异常返回信息
    /// </summary>
    public enum EAppRunStatus
    {
        eAppStatusInitOK = 0000,					//初始化成功
		eRunStatusIvalidHandlerror,					//句柄不可用，底层初始化失败
		eRunStatusAlreadyRunError,					//已有软件运行或初始化失败，请关闭软件后重启
		eRunStatusInitialAgainError,				//软件已初始化，请关闭软件重启		
	
	
		eRunStatusNoCfg = 1000,						//载入配置文件失败，请检查路径或配置文件命名是否正确
		eRunStatusCfgError,							//配置文件格式错误，请确认配置文件内容格式是否正确
		
		eRunStatusNoEncryptedDog = 2200,			//未检测到加密狗，检查或插拔加密狗
		eRunStatusAlgorithmError,					//加密狗，初始化算法失败
		eRunStatusBarcodeAlgError=2300,				//初始化一维码算法失败 
		eRunStatusBarcodeAlgfinError,				//反初始化一维码算法失败 
		eRunStatusDMcodeAlgError=2400,				//初始化二维码算法失败 
		eRunStatusDMcodeAlgfinError,				//反初始化二维码算法失败 
		eRunStatusMattingAlgError=2500,				//初始化抠图算法失败 
		eRunStatusMattingAlgfinError,				//反初始化抠图算法失败 
		eRunStatusIpcGrayAlgError=2600,				//初始化全景灰度识别算法失败 
		eRunStatusIpcGrayAlgfinError,				//反初始化全景灰度识别算法失败 
	
		eRunStatusCameraNumError = 3000,			//缺少相机，确认实际可连相机个数满足配置个数
		eRunStatusCameraOpendError,					//部分相机已被连接，确认相机未被连接
		eRunStatusCameraListNumError,				//配置的相机列表个数跟实际配置num不一致
		eRunStatusCameraListUnmatch,				//配置的相机列表，部分相机不存在，确认列表中的相机都存在且可连接
		eRunStatusSoftEncryptionError,				//相机授权失败，确认所有相机都已授权
		eRunStatusIpcCameraError,					//初始化全景相机失败，确认全景相机存在可连接
		eRunStatusNtpServerError,					//开启时间同步服务失败
	
		eRunStatus3DCameraError = 4000,				//3D相机初始化失败，客户端检查3D相机可用
	
		eRunStatusWeightError = 5000,				//称重模块初始化失败，调试确认当前配置下能正常连接称
	
		eRunStatusCodeRuleFilterError = 6000,		//条码过滤规则模块初始化失败，检查过滤规则配置格式是否正确
	
		eRunStatusModuleOutputError = 7000,			//输出规则模块初始化失败，检查输出模块配置是否正确
		eRunStatusModuleDbDataError,				//数据库模块创建失败

		eRunStatusLocalImagePathError = 8000,		//本地图片文件夹不存在，确认本地路径正确
		eRunStatusLocalImageNumError,				//本地图片路径个数不对，确认本地路径文件夹个数跟配置保持一致
		eRunStatusLocalImageInitError,				//本地图片文件夹模式初始化失败，检查路径
    }
}
