#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;
using Canon.Eos.Framework;
using System.Collections.Generic;

#endregion usings

namespace VVVV.Nodes.EDSDK
{
	#region PluginInfo
	[PluginInfo(Name = "ListDevices", Category = "EDSDK", Help = "List connected Canon cameras using EDSDK", Tags = "Canon", Author = "elliotwoods", AutoEvaluate = true)]
	#endregion PluginInfo
	public class ListDevicesNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Refresh", IsBang = true, IsSingle = true)]
		ISpread<bool> FPinInRefresh;

		[Output("Device")]
		ISpread<EosCamera> FPinOutDevices;

		[Output("Status")]
		ISpread<string> FPinOutStatus;

		[Import]
		ILogger FLogger;

		bool FFirstRun = true;

		#endregion fields & pins

		[ImportingConstructor]
		public ListDevicesNode(IPluginHost host)
		{
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FFirstRun || FPinInRefresh[0])
			{
				LoadList();
				FFirstRun = false;
			}
		}

		private void LoadList()
		{
			try
			{
				FPinOutDevices.SliceCount = 0;

				foreach (var camera in Context.Cameras)
				{
					if (camera.IsSessionOpen)
					{
						FPinOutDevices.Add(camera);
					}
				}

				FPinOutStatus[0] = "OK";
			}
			catch (Exception e)
			{
				FPinOutStatus[0] = "ERROR : " + e.Message;
			}
		}
	}
}