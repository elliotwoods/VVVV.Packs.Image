using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.Ximea
{
	using ParameterSet = Dictionary<Device.IntParameter, int>;

	#region PluginInfo
	[PluginInfo(Name = "Parameter",
				Category = "Ximea",
				Version = "Join",
				Help = "Set camera capture properties",
				Tags = "")]
	#endregion PluginInfo
	public class ParameterJoin : IPluginEvaluate
	{
		[Input("Parameter")]
		IDiffSpread<Device.IntParameter> FInParameter;

		[Input("Value")]
		IDiffSpread<int> FInValue;

		[Output("ParameterSet")]
		ISpread<ParameterSet> FOutput;

		bool firstRun = true;

		public void Evaluate(int SpreadMax)
		{
			if (firstRun)
			{
				firstRun = false;
				FOutput[0] = new ParameterSet();
			}

			if (FInParameter.IsChanged || FInValue.IsChanged)
			{
				var dictionary = FOutput[0];

				dictionary.Clear();

				for (int i = 0; i < SpreadMax; i++)
				{
					dictionary[FInParameter[i]] = FInValue[i];
				}

				FOutput[0] = dictionary;
			}
		}
	}
}
