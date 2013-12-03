using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.CV.Core
{
	public abstract class IFilterNode<T> : INode, IDisposable where T : IFilterInstance, new()
	{
		[Input("Input", Order = -1)]
		private ISpread<CVImageLink> FInput;

		[Output("Output", Order = -1)]
		private ISpread<CVImageLink> FOutput;

		protected ProcessFilter<T> FProcessor;

		public override void Evaluate(int SpreadMax)
		{
			if (FProcessor == null)
				FProcessor = new ProcessFilter<T>(FInput, FOutput);

			bool changed = FProcessor.CheckInputSize(SpreadMax);
			Update(FProcessor.SliceCount, changed);
		}

		public void Dispose()
		{
			// sometimes we get a double dispose from vvvv on quit
			if (FProcessor != null)
				FProcessor.Dispose();
		}
	}
}
