using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Canon.Eos.Framework;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.EDSDK.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "Battery", Category = "EDSDK", Help = "Get battery information from the camera", Tags = "Canon", Author = "elliotwoods")]
	#endregion PluginInfo
	public class BatteryNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Device")]
		IDiffSpread<EosCamera> FInDevices;

		[Input("Refresh", IsSingle=true, IsBang=true)]
		ISpread<bool> FInRefresh;

		[Output("Battery Level")]
		ISpread<int> FOutBatteryLevel;

		[Output("Battery quality")]
		ISpread<string> FOutBatteryQuality;
		#endregion fields & pins

		[ImportingConstructor]
		public BatteryNode(IPluginHost host)
		{
		}

		public void Evaluate(int SpreadMax)
		{
			if (FInDevices.IsChanged || FInRefresh[0])
			{
				FOutBatteryLevel.SliceCount = 0;
				FOutBatteryQuality.SliceCount = 0;

				foreach(var camera in FInDevices)
				{
					if (camera == null)
					{
						continue;
					}

					FOutBatteryLevel.Add((int)camera.BatteryLevel);
					try
					{
						FOutBatteryQuality.Add(camera.BatteryQuality.ToString());
					}
					catch (Exception e)
					{
						FOutBatteryQuality.Add(e.Message);
					}
				}
			}
		}

	}
}
