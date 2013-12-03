using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.CV.Core
{
	public abstract class IDestinationNode<T> : INode, IDisposable where T : IDestinationInstance, new()
	{
		[Config("Thread mode")]
		IDiffSpread<ThreadMode> FConfigThreadMode;

		[Input("Input", Order = -1)]
		private ISpread<CVImageLink> FPinInInputImage;

		protected ProcessDestination<T> FProcessor;

		public override void Evaluate(int SpreadMax)
		{
			if (FProcessor == null)
				FProcessor = new ProcessDestination<T>(FPinInInputImage);

			if (FConfigThreadMode.IsChanged)
				FProcessor.ThreadMode = FConfigThreadMode[0];

			bool countChanged = FProcessor.CheckInputSize(this.OneInstancePerImage() ? FPinInInputImage.SliceCount : SpreadMax);
			Update(FProcessor.SliceCount, countChanged);
		}

		public void Dispose()
		{
			// sometimes we get a double dispose from vvvv on quit
			if (FProcessor != null)
				FProcessor.Dispose();
		}
	}
}
