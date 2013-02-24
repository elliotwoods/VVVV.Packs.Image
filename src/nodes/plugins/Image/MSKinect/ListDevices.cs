#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

using ThreadState = System.Threading.ThreadState;
using System.Collections.Generic;

using Microsoft.Kinect;

#endregion usings

namespace VVVV.Nodes.OpenCV.Kinect
{
	#region PluginInfo
	[PluginInfo(Name = "ListDevices", Category = "OpenCV", Version = "Kinect",  Help = "OpenNI context loader", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class ListDevicesNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Refresh", IsSingle=true, IsBang=true)]
		ISpread<bool> FRefresh;

		[Output("Devices")]
		ISpread<KinectDevice> FSensors;

		[Output("ID")]
		ISpread<string> FID;

		[Output("Status")]
		ISpread<String> FStatus;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		bool FFirstRun = true;

		[ImportingConstructor]
		public ListDevicesNode(IPluginHost host)
		{

		}

		public void Dispose()
		{

		}

		public void Evaluate(int SpreadMax)
		{
			if (FFirstRun || FRefresh[0])
			{
				FFirstRun = false;
				try
				{
					Refresh();
					FStatus[0] = "OK";
				}
				catch (Exception e)
				{
					FStatus[0] = e.Message;
				}
			}
		}

		void Refresh()
		{
			var sensors = KinectSensor.KinectSensors;
			int count = sensors.Count;

			FSensors.SliceCount = count;
			FID.SliceCount = count;

			for (int i = 0; i < sensors.Count; i++) {
				var sensor = sensors[i];
				FSensors[i] = new KinectDevice(sensor);
				FID[i] = sensor.UniqueKinectId;
			}
		}
	}
}
