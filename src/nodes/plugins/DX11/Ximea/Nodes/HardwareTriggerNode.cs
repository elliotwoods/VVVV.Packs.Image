using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.Ximea.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "HardwareTrigger",
				Category = "Ximea",
				Version = "Trigger",
				Help = "Use an electrical signal to trigger the Ximea's capture via the GPI port",
				Tags = "")]
	#endregion PluginInfo
	public class HardwareTriggerNode : IPluginEvaluate
	{
		class SoftwareLFO : ITrigger
		{
			public TriggerType GetTriggerType()
			{
				return TriggerType.GPI;
			}
		}

		[Input("Source")]
		IDiffSpread<HardwareTrigger.HardwareEvent> FInSource;

		[Output("Trigger Out")]
		ISpread<HardwareTrigger> FOutTrigger;

		public void Evaluate(int SpreadMax)
		{
			if (FInSource.IsChanged)
			{
				FOutTrigger.SliceCount = SpreadMax;

				for (int i = 0; i < SpreadMax; i++)
				{
					FOutTrigger[i] = new HardwareTrigger()
					{
						Source = FInSource[i]
					};
				}
			}
		}
	}
}
