using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.Ximea.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "Specification",
				Category = "Ximea",
				Version = "Split",
				Help = "Split a Device.Specification into its component data elements",
				Tags = "")]
	#endregion PluginInfo
	public class SpecificationSplit : IPluginEvaluate
	{
		[Input("Specification")]
		IDiffSpread<Device.Specification> FInSpecification;

		[Output("Width")]
		ISpread<int> FOutWidth;

		[Output("Height")]
		ISpread<int> FOutHeight;

		[Output("Name")]
		ISpread<string> FOutName;

		[Output("Type")]
		ISpread<string> FOutType;

		[Output("Serial")]
		ISpread<string> FOutSerial;

		public void Evaluate(int SpreadMax)
		{
			if (FInSpecification.IsChanged)
			{
				FOutWidth.SliceCount = SpreadMax;
				FOutHeight.SliceCount = SpreadMax;
				FOutName.SliceCount = SpreadMax;
				FOutType.SliceCount = SpreadMax;
				FOutSerial.SliceCount = SpreadMax;

				for (int i = 0; i < SpreadMax; i++)
				{
					var spec = FInSpecification[i];

					if (spec == null)
					{
						FOutWidth[i] = 0;
						FOutHeight[i] = 0;
						FOutName[i] = "";
						FOutType[i] = "";
						FOutSerial[i] = "";
					}
					else
					{
						FOutWidth[i] = spec.Width;
						FOutHeight[i] = spec.Height;
						FOutName[i] = spec.Name;
						FOutType[i] = spec.Type;
						FOutSerial[i] = spec.Serial;
					}
				}
			}
		}
	}
}
