using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using System.ComponentModel.Composition;
using VVVV.Core.Logging;

namespace VVVV.Nodes.OpenCV
{
	#region PluginInfo
	[PluginInfo(Name = "Cons", Category = "OpenCV", Help = "Cons 2 inputs (temporary, will replace with templated version when i find it, i.e. >2 inputs. but dont need that right now)", Tags = "")]
	#endregion PluginInfo
	public class ConsNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Input 1")]
		IDiffSpread<CVImageLink> FPinInInput1;

		[Input("Input 2")]
		IDiffSpread<CVImageLink> FPinInInput2;

		[Output("Output")]
		ISpread<CVImageLink> FPinOutOutput;

		#endregion fields & pins

		[ImportingConstructor]
		public ConsNode(IPluginHost host)
		{

		}

		public void Evaluate(int SpreadMax)
		{
			if (FPinInInput1.IsChanged || FPinInInput2.IsChanged)
			{
				FPinOutOutput.SliceCount = 0;
				foreach (var image in FPinInInput1)
					if (image != null)
						FPinOutOutput.Add(image);
				foreach (var image in FPinInInput2)
					if (image != null)
						FPinOutOutput.Add(image);
			}
		}
	}
}
