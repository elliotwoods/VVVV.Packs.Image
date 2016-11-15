using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Collections;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V1;
using System.Runtime.InteropServices;

using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using uEye;
using uEye.Types;
using VVVV.PluginInterfaces.V2;
using VVVV.Core.Logging;

namespace VVVV.Nodes.OpenCV.CLEye
{
	#region PluginInfo
	[PluginInfo(Name = "ListDevices", Category = "uEye", Author = "sebl", Credits = "Elliot, IDS",  Help = "List available uEye camera devices w/ Information per connected Device", Tags = "", AutoEvaluate=true)]
	#endregion PluginInfo
	public class ListDevicesNode : IPluginEvaluate
	{
		#region fields
		[Input("Update", IsBang=true, IsSingle=true)]
		ISpread<bool> FUpdate;

        [Input("Log Deep Info", IsBang = true, IsSingle = true, Visibility = PinVisibility.Hidden)]
        ISpread<bool> FLogDeepInfo;

        [Input("Camera ID", MinValue = 0, MaxValue = 255)]
        ISpread<int> FCameraId;

        [Input("Set Camera ID", IsBang = true)]
        ISpread<bool> FSetCameraId;

        [Output("Camera ID")]
        ISpread<int> FOutCameraId;

        [Output("Device ID")]
        ISpread<int> FOutDeviceId;


        [Output("in Use")]
        ISpread<bool> FOutInUse;

        [Output("Model")]
        ISpread<string> FOutModel;

        [Output("Sensor ID", Visibility = PinVisibility.Hidden)]
        ISpread<int> FOutSensorId;

        [Output("Serial Number")]
        ISpread<string> FOutSerialNumber;

        [Output("Camera Status")]
        ISpread<string> FOutCamStatus;

        [Output("Comport Offset", Visibility = PinVisibility.Hidden)]
        ISpread<int> FOutComportOffset;

        [Output("Link Speed (Mb)")]
        ISpread<int> FOutLinkSpeed_Mb;

        [Output("Runtime Firmware Version", Visibility = PinVisibility.Hidden)]
        ISpread<string> FOutRuntimeFirmwareVersion;

        [Output("Temperature")]
        ISpread<int> FOutTemperature;

        [Output("Status")]
		ISpread<string> FStatus;
		bool FFirstRun = true;

        [Import()]
        public ILogger FLogger;
        #endregion

        private Camera camera = null;

        CameraInformation[] camList;


        public void Evaluate(int SpreadMax)
		{
            // dreate camera object and attach events
            if (camera == null)
            {
                camera = new Camera();

                uEye.Info.Camera.EventNewDevice += cameraDevicesChanged;
                uEye.Info.Camera.EventDeviceRemoved += cameraDevicesChanged;
            }

            // update
            if (FFirstRun || FUpdate[0])
			{
				FFirstRun = false;
				FillList();
			}

            // set cam id
            for (int i = 0; i < camList.Length; i++)
            {
                if (FSetCameraId[i] && FOutCameraId.SliceCount != 0)
                {
                    camera.Init((int)FOutCameraId[i]);

                    camera.Device.SetCameraID(FCameraId[i]);

                    camera.Exit();

                    FillList();
                }                
            }

            // query deeper info
            if (FLogDeepInfo[0])
            {
                DeepInfo();
            }

        }

        // retrigger Event
        void cameraDevicesChanged(object sender, EventArgs e)
        {
            FStatus[0] = "uEye camera (dis)connected";
            FLogger.Log(LogType.Debug, "uEye camera (dis)connected");
            FillList();
        }

        void DeepInfo()
        {
            if (camera != null)
            {
                for (int i = 0; i < camList.Length; i++)
                {
                    try
                    {
                        camera.Init((int)camList[i].CameraID);

                        uEye.Defines.Status status;


                        uEye.Types.CameraInfo camInfo;
                        status = camera.Information.GetCameraInfo(out camInfo);

                        FLogger.Log(LogType.Message, "");
                        FLogger.Log(LogType.Message, "CameraInfo: (" + status + ")");
                        FLogger.Log(LogType.Message, "");

                        FLogger.Log(LogType.Message, "ID: " + camInfo.ID);
                        FLogger.Log(LogType.Message, "Date: " + camInfo.Date);
                        FLogger.Log(LogType.Message, "CameraID: " + camInfo.CameraID);
                        FLogger.Log(LogType.Message, "BoardType: " + camInfo.BoardType);
                        FLogger.Log(LogType.Message, "Version: " + camInfo.Version);
                        FLogger.Log(LogType.Message, "SerialNumber: " + camInfo.SerialNumber);


                        DeviceInformation devInfo;
                        status = camera.Information.GetDeviceInfo(out devInfo);

                        FLogger.Log(LogType.Message, "");
                        FLogger.Log(LogType.Message, "");
                        FLogger.Log(LogType.Message, "DeviceInformation: (" + status + ")");
                        FLogger.Log(LogType.Message, "");

                        FLogger.Log(LogType.Message, "ComportOffset: " + devInfo.DeviceInfoHeartbeat.ComportOffset);
                        FLogger.Log(LogType.Message, "LinkSpeed_Mb: " + devInfo.DeviceInfoHeartbeat.LinkSpeed_Mb);
                        FLogger.Log(LogType.Message, "RuntimeFirmwareVersion: " + devInfo.DeviceInfoHeartbeat.RuntimeFirmwareVersion);
                        FLogger.Log(LogType.Message, "Temperature: " + devInfo.DeviceInfoHeartbeat.Temperature);
                        FLogger.Log(LogType.Message, "DeviceID: " + devInfo.DeviceInfoControl.DeviceID);


                        SensorInfo sInfo;
                        status = camera.Information.GetSensorInfo(out sInfo);

                        FLogger.Log(LogType.Message, "");
                        FLogger.Log(LogType.Message, "");
                        FLogger.Log(LogType.Message, "SensorInfo: (" + status + ")");
                        FLogger.Log(LogType.Message, "");

                        FLogger.Log(LogType.Message, "SensorName: " + sInfo.SensorName);

                        FLogger.Log(LogType.Message, "MasterGain: " + sInfo.MasterGain);
                        FLogger.Log(LogType.Message, "RedGain: " + sInfo.RedGain);
                        FLogger.Log(LogType.Message, "GreenGain: " + sInfo.GreenGain);
                        FLogger.Log(LogType.Message, "BlueGain: " + sInfo.BlueGain);

                        FLogger.Log(LogType.Message, "GlobalShutter: " + sInfo.GlobalShutter);
                        FLogger.Log(LogType.Message, "PixelSize: " + sInfo.PixelSize);
                        FLogger.Log(LogType.Message, "MaxSize: " + sInfo.MaxSize.Width.ToString() + " x " + sInfo.MaxSize.Height.ToString());
                        FLogger.Log(LogType.Message, "SensorColorMode: " + sInfo.SensorColorMode);

                        camera.Exit();
                    }
                    catch (Exception e)
                    {
                        FLogger.Log(LogType.Message, "Exception: " + e.ToString());
                    }
                }
            }
        }

		void FillList()
		{
			try
			{
                camList = new CameraInformation[] { };

                uEye.Info.Camera.GetCameraList(out camList);

                int numCams = camList.Length;

                FOutCameraId.SliceCount = numCams;
                FOutDeviceId.SliceCount = numCams;
                FOutInUse.SliceCount = numCams;
                FOutModel.SliceCount = numCams;
                FOutSensorId.SliceCount = numCams;
                FOutSerialNumber.SliceCount = numCams;
                FOutCamStatus.SliceCount = numCams;
                FOutComportOffset.SliceCount = numCams;
                FOutLinkSpeed_Mb.SliceCount = numCams;
                FOutRuntimeFirmwareVersion.SliceCount = numCams;
                FOutTemperature.SliceCount = numCams;

                for (int i = 0; i < camList.Length; i++)
                {
                    FOutCameraId[i] = (int)camList[i].CameraID;
                    FOutDeviceId[i] = (int)camList[i].DeviceID;
                    FOutInUse[i] = camList[i].InUse;
                    FOutModel[i] = camList[i].Model;
                    FOutSensorId[i] = (int)camList[i].SensorID;
                    FOutSerialNumber[i] = camList[i].SerialNumber;
                    string stat = Enum.GetName(typeof(uEye.Defines.Status), camList[i].Status);
                    FOutCamStatus[i] = stat;


                    //additional info per cam
                    uEye.Types.DeviceInformation di;
                    uEye.Info.Camera.GetDeviceInfo((int)camList[i].DeviceID, out di);

                    FOutComportOffset[i] = di.DeviceInfoHeartbeat.ComportOffset;
                    FOutLinkSpeed_Mb[i] = di.DeviceInfoHeartbeat.LinkSpeed_Mb;
                    FOutRuntimeFirmwareVersion[i] = di.DeviceInfoHeartbeat.RuntimeFirmwareVersion.ToString();
                    FOutTemperature[i] = di.DeviceInfoHeartbeat.Temperature;
                }
				FStatus[0] = "OK";
			}
			catch (Exception e)
			{
				FStatus[0] = e.Message;
			}
		}
	}
}