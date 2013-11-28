#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;
using VideoInputSharp;

#endregion usings

namespace VVVV.Nodes.OpenCV.VideoInput
{
	#region PluginInfo
	[PluginInfo(Name = "ListDevices", Category = "CV.Image", Version = "DirectShow", Help = "List DirectShow video capture devices", Tags = "", Author = "elliotwoods", AutoEvaluate = true)]
	#endregion PluginInfo
	public class ListDevicesNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Refresh", IsBang = true, IsSingle = true)]
		ISpread<bool> FPinInRefresh;

		[Output("Device name")]
		ISpread<string> FPinOutDeviceName;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
		public ListDevicesNode(IPluginHost host)
		{

		}

		bool FFirstRun = true;
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FFirstRun || FPinInRefresh[0])
			{
				FFirstRun = false;
				Refresh();
			}
		}

		private void Refresh()
		{
			string[] devicenames;
			lock (DeviceLock.LockDevices)
			{
				devicenames = Capture.ListDevices();
			}
			FPinOutDeviceName.SliceCount = devicenames.Length;

			for (int i = 0; i < devicenames.Length; i++)
			{
				FPinOutDeviceName[i] = devicenames[i];
			}
		}

	}
}
