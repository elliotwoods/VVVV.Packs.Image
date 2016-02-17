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

        [Output("Camera ID")]
        ISpread<long> FOutCameraId;

        [Output("Device ID")]
        ISpread<long> FOutDeviceId;

        [Output("in Use")]
        ISpread<bool> FOutInUse;

        [Output("Model")]
        ISpread<string> FOutModel;

        [Output("Sensor ID", Visibility = PinVisibility.Hidden)]
        ISpread<long> FOutSensorId;

        [Output("Serial Number")]
        ISpread<string> FOutSerialNumber;

        [Output("Camera Status")]
        ISpread<long> FOutCamStatus;

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


        public void Evaluate(int SpreadMax)
		{
			if (FFirstRun || FUpdate[0])
			{
				FFirstRun = false;
				FillList();
			}
		}

        // retrigger Event
        void cameraDevicesChanged(object sender, EventArgs e)
        {
            //uEye.Camera Cam = sender as uEye.Camera; // not needed, but strange, that it makes this underlying rcw exception
            FStatus[0] = "uEye camera (dis)connected";
            FLogger.Log(LogType.Debug, "uEye camera (dis)connected");
            FillList();
        }

		void FillList()
		{
			try
			{
                if (camera == null)
                {
                    camera = new Camera();

                    uEye.Info.Camera.EventNewDevice += cameraDevicesChanged;
                    uEye.Info.Camera.EventDeviceRemoved += cameraDevicesChanged;
                }

                uEye.Types.CameraInformation[] camList;
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
                    FOutCameraId[i] = camList[i].CameraID;
                    FOutDeviceId[i] = camList[i].DeviceID;
                    FOutInUse[i] = camList[i].InUse;
                    FOutModel[i] = camList[i].Model;
                    FOutSensorId[i] = camList[i].SensorID;
                    FOutSerialNumber[i] = camList[i].SerialNumber;
                    FOutCamStatus[i] = camList[i].Status;

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