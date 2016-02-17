using System;
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

namespace VVVV.Nodes.OpenCV.CLEye
{
	#region PluginInfo
	[PluginInfo(Name = "ListDevices", Category = "uEye", Help = "List available uEye camera devices w/ Information per connected Device", Tags = "", AutoEvaluate=true)]
	#endregion PluginInfo
	public class ListDevicesNode : IPluginEvaluate
	{
		#region fields
		[Input("Update", IsBang=true, IsSingle=true)]
		ISpread<bool> FUpdate;

        //------------------------

		//[Output("Devices")]
		//ISpread<string> FDevices;

        [Output("Camera ID")]
        ISpread<long> FOutCameraId;

        [Output("Device ID")]
        ISpread<long> FOutDeviceId;

        [Output("in Use")]
        ISpread<bool> FOutInUse;

        [Output("Model")]
        ISpread<string> FOutModel;

        [Output("Sensor ID")]
        ISpread<long> FOutSensorId;

        [Output("Serial Number")]
        ISpread<string> FOutSerialNumber;

        [Output("SeCamera Status")]
        ISpread<long> FOutCamStatus;

        [Output("Status")]
		ISpread<string> FStatus;
		bool FFirstRun = true;
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
            FillList();
        }

		void FillList()
		{
			try
			{
                if (camera == null)
                {
                    camera = new Camera();

                    camera.EventDevicePluggedIn += cameraDevicesChanged;
                    camera.EventDeviceRemove += cameraDevicesChanged;
                    camera.EventDeviceReconnect += cameraDevicesChanged;
                }

                uEye.Types.CameraInformation[] camList;
                uEye.Info.Camera.GetCameraList(out camList);


                // TODO: test if those return different values
                //uEye.Types.CameraInfo camInfo;
                //camera.Information.GetCameraInfo(out camInfo);

                //camInfo.ID;
                //camInfo.CameraID;
                //camInfo.BoardType;
                //camInfo.Version;

                int numCams = camList.Length;

                //FDevices.SliceCount = numCams;
                FOutCameraId.SliceCount = numCams;
                FOutDeviceId.SliceCount = numCams;
                FOutInUse.SliceCount = numCams;
                FOutModel.SliceCount = numCams;
                FOutSensorId.SliceCount = numCams;
                FOutSerialNumber.SliceCount = numCams;
                FOutCamStatus.SliceCount = numCams;

                for (int i = 0; i < camList.Length; i++)
                {
                    //FDevices[i] = camList[i].
                    FOutCameraId[i] = camList[i].CameraID;
                    FOutDeviceId[i] = camList[i].DeviceID;
                    FOutInUse[i] = camList[i].InUse;
                    FOutModel[i] = camList[i].Model;
                    FOutSensorId[i] = camList[i].SensorID;
                    FOutSerialNumber[i] = camList[i].SerialNumber;
                    FOutCamStatus[i] = camList[i].Status;

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