using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DemoDWS
{
    // error message
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

    // When starting DWS, some possible exceptions return information
    public enum EAppRunStatus
    {
        eAppStatusInitOK = 0000,                    //Initialization successfully
        eRunStatusIvalidHandlerror,                 //The handle is not available, the underlying initialization fails
        eRunStatusAlreadyRunError,                  //Some software has failed to run or initialize. Please close the software and restart it.
        eRunStatusInitialAgainError,                //The software has been initialized, please close the software and restart it	
        eRunStatusNoCfg = 1000,                     //Failed to load the configuration file. Please check whether the path or configuration file naming is correct.
        eRunStatusCfgError,                         //The configuration file format is incorrect. Please confirm whether the configuration file content format is correct.
        eRunStatusNoEncryptedDog = 2200,            //dongle not detected, check or plug and unplug
        eRunStatusAlgorithmError,                   //Dongle, initialization algorithm failed
        eRunStatusBarcodeAlgError = 2300,           //Failed to initialize the 1R code algorithm 
        eRunStatusBarcodeAlgfinError,               //Deinitialization of 1R code algorithm failed
        eRunStatusDMcodeAlgError = 2400,            //Failed to initialize the QR code algorithm
        eRunStatusDMcodeAlgfinError,                //Failed to deinitialize the QR code algorithm 
        eRunStatusMattingAlgError = 2500,           //Initialization of the cutout algorithm failed 
        eRunStatusMattingAlgfinError,               //Deinitialization cutout algorithm failed
        eRunStatusIpcGrayAlgError = 2600,           //Initialization of panoramic grayscale recognition algorithm failed 
        eRunStatusIpcGrayAlgfinError,               //Deinitialization of panoramic grayscale recognition algorithm failed 
        eRunStatusCameraNumError = 3000,            //Missing camera, confirm that the actual number of cameras can be connected meets the configuration number
        eRunStatusCameraOpendError,                 //Some cameras have been connected, make sure the camera is not connected
        eRunStatusCameraListNumError,               //The number of configured camera lists is inconsistent with the actual configured num
        eRunStatusCameraListUnmatch,                //The configured camera list, some cameras do not exist, confirm that all cameras in the list exist and can be connected
        eRunStatusSoftEncryptionError,              //Camera authorization failed, confirm that all cameras are authorized
        eRunStatusIpcCameraError,                   //Initializing the panoramic camera failed, confirm that the panoramic camera is connected
        eRunStatusNtpServerError,                   //Failed to enable time synchronization service
        eRunStatus3DCameraError = 4000,             //3D camera initialization failed, client checks that 3D camera is available
        eRunStatusWeightError = 5000,               //The initialization of the weighing module failed. Debugging confirms that the scale can be connected normally under the current configuration.
        eRunStatusCodeRuleFilterError = 6000,       //The initialization of the barcode filtering rule module fails. Check whether the filtering rule configuration format is correct.
        eRunStatusModuleOutputError = 7000,         //The output rule module is initialized for failure. Check whether the output module is configured correctly.
        eRunStatusModuleDbDataError,                //Database module creation failed
        eRunStatusLocalImagePathError = 8000,       //The local image folder does not exist, confirm that the local path is correct
        eRunStatusLocalImageNumError,               //The number of local image paths is incorrect. Make sure that the number of local path folders is consistent with the configuration
        eRunStatusLocalImageInitError,				//Local image folder mode initialization failed, check the path
    }
}
