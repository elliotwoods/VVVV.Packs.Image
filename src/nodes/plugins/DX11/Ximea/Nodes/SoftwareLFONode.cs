using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.Ximea
{
	#region PluginInfo
	[PluginInfo(Name = "LFO",
				Category = "Ximea",
				Version = "Trigger",
				Help = "",
				Tags = "")]
	#endregion PluginInfo
	public class SoftwareLFONode : IPluginEvaluate
	{
		[Input("Frequency", IsSingle = true, DefaultValue=90)]
		IDiffSpread<double> FInFrequency;

		[Input("Phase", IsSingle = true)]
		IDiffSpread<double> FInPhase;

		[Output("Trigger")]
		ISpread<ITrigger> FOutClock;

		SoftwareLFO FClock = new SoftwareLFO();
		bool FFirstRun = true;

		public void Evaluate(int SpreadMax)
		{
			if (FFirstRun)
			{
				FFirstRun = false;
				FOutClock[0] = FClock;
			}

			if (FInFrequency.IsChanged)
			{
				FClock.Frequency = FInFrequency[0];
			}

			if (FInPhase.IsChanged)
			{
				FClock.Phase = FInPhase[0];
			}
		}
	}
}
