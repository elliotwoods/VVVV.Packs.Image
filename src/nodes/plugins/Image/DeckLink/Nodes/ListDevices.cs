#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;
using DeckLinkAPI;
using System.Collections.Generic;

#endregion usings

namespace VVVV.Nodes.DeckLink
{
	#region PluginInfo
	[PluginInfo(Name = "ListDevices", Category = "DeckLink", Help = "List BlackMagic DeckLink devices", Tags = "", Author = "elliotwoods", AutoEvaluate = true)]
	#endregion PluginInfo
	public class ListDevicesNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Refresh", IsBang = true, IsSingle = true)]
		ISpread<bool> FPinInRefresh;

		[Output("Device")]
		ISpread<DeviceRegister.DeviceIndex> FPinOutDevices;

		[Output("Model Name")]
		ISpread<string> FPinOutModelName;

		[Output("Display Name")]
		ISpread<string> FPinOutDisplayName;

		[Output("Status")]
		ISpread<string> FPinOutStatus;

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
			try
			{
				FPinOutDevices.SliceCount = 0;
				FPinOutModelName.SliceCount = 0;
				FPinOutDisplayName.SliceCount = 0;

				var register = DeviceRegister.Singleton;
				register.Refresh();

				for (int i = 0; i < register.Count; i++)
				{
					FPinOutDevices.Add(new DeviceRegister.DeviceIndex(i));
					FPinOutModelName.Add(register.GetModelName(i));
					FPinOutDisplayName.Add(register.GetDisplayName(i));
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
