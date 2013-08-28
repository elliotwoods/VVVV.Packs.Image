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
	[PluginInfo(Name = "Info", Category = "EDSDK", Help = "List connected Canon cameras using EDSDK", Tags = "Canon", Author = "elliotwoods", AutoEvaluate = true)]
	#endregion PluginInfo
	public class InfoNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Device")]
		IDiffSpread<EosCamera> FInDevices;

		[Output("Product Name")]
		ISpread<string> FOutProductName;

		[Output("Serial Number")]
		ISpread<string> FOutSerialNumber;

		[Output("Port Name")]
		ISpread<string> FOutPortName;

		[Output("Owner")]
		ISpread<string> FOutOwnerName;

		[Output("Firmware")]
		ISpread<string> FOutFirmware;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
		public InfoNode(IPluginHost host)
		{
		}

		public void Evaluate(int SpreadMax)
		{
			if (FInDevices.IsChanged)
			{
				FOutProductName.SliceCount = 0;
				FOutSerialNumber.SliceCount = 0;
				FOutPortName.SliceCount = 0;
				FOutOwnerName.SliceCount = 0;
				FOutFirmware.SliceCount = 0;

				foreach(var camera in FInDevices)
				{
					if (camera == null)
						continue;

					FOutSerialNumber.Add(camera.SerialNumber);
					FOutProductName.Add(camera.ProductName);
					FOutPortName.Add(camera.PortName);
					FOutOwnerName.Add(camera.OwnerName);
					FOutFirmware.Add(camera.FirmwareVersion);
				}
			}
		}

	}
}
