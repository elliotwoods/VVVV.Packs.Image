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

using FlyCapture2;
using FlyCapture2Managed;

using VVVV.CV.Core;

#endregion usings

namespace VVVV.Nodes.FlyCapture
{
	#region PluginInfo
	[PluginInfo(Name = "ListCameras", Category = "FlyCapture", Help = "List FlyCapture camera devices", Tags = "")]
	#endregion PluginInfo
	public class CameraListNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Refresh", IsBang=true, IsSingle=true)]
		ISpread<bool> FPinInRefresh;

		[Output("GUID")]
		ISpread<ManagedPGRGuid> FPinOutGUID;

		[Output("Info")]
		ISpread<CameraInfo> FPinOutInfo;

		[Output("Count", IsSingle=true)]
		ISpread<int> FPinOutCount;
		[Import]
		ILogger FLogger;

		bool FFirstRun = true;
		#endregion fields & pins

		[ImportingConstructor]
		public CameraListNode(IPluginHost host)
		{

		}

		public void Dispose()
		{

		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FPinInRefresh[0] || FFirstRun)
			{
				Refresh();
				FFirstRun = false;
			}
		}

		void Refresh()
		{
			int nDevices = (int)Context.Bus.GetNumOfCameras();
			ManagedPGRGuid guid;
			ManagedCamera cam = new ManagedCamera();

			FPinOutGUID.SliceCount = nDevices;
			FPinOutInfo.SliceCount = nDevices;
			FPinOutCount[0] = nDevices;

			for (int i = 0; i < nDevices; i++)
			{
				guid = Context.Bus.GetCameraFromIndex((uint)i);

				FPinOutGUID[i] = guid;

				cam.Connect(guid);
				FPinOutInfo[(int)i] = cam.GetCameraInfo();
				cam.Disconnect();
			}

			
			cam.Dispose();
		}
	}
}
