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
using CLEyeMulticam;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.OpenCV.CLEye
{
	#region PluginInfo
	[PluginInfo(Name = "ListDevices", Category = "CLEye", Help = "List available camera devices", Tags = "", AutoEvaluate=true)]
	#endregion PluginInfo
	public class ListDevicesNode : IPluginEvaluate
	{
		#region fields
		[Input("Update", IsBang=true, IsSingle=true)]
		ISpread<bool> FUpdate;

		[Output("Devices")]
		ISpread<string> FDevices;

		[Output("Status")]
		ISpread<string> FStatus;
		bool FFirstRun = true;
		#endregion

	
		public void Evaluate(int SpreadMax)
		{
			if (FFirstRun || FUpdate[0])
			{
				FFirstRun = false;
				FillList();
			}
		}

		void FillList()
		{
			try
			{
				int nCams = CLEyeCameraDevice.CLEyeGetCameraCount();
				FDevices.SliceCount = nCams;

				for (int i = 0; i < nCams; i++)
					FDevices[i] = CLEyeCameraDevice.CLEyeGetCameraUUID(i).ToString();
				FStatus[0] = "OK";
			}
			catch (Exception e)
			{
				FStatus[0] = e.Message;
			}
		}
	}
}