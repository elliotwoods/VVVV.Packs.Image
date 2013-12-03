using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.CV.Core
{
	public abstract class IDestinationInstance : IInstance, IInstanceInput
	{
		protected CVImageInput FInput;

		public void SetInput(CVImageInput input)
		{
			FInput = input;
			ReAllocate();
		}

		public bool HasInput(CVImageInput input)
		{
			return FInput == input;
		}

		public void UpstreamDirectUpdate(object sender, EventArgs e)
		{
			Process();
		}

		public void UpstreamDirectAttributesUpdate(object sender, ImageAttributesChangedEventArgs e)
		{
			Allocate();
			Process();
		}
	}
}
