#region using
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using System;
using VVVV.Utils.VColor;
using VVVV.CV.Core;

#endregion

namespace VVVV.CV.Nodes
{
	[FilterInstance("S+H")]
	public class S_HInstance : IFilterInstance
	{
		bool FFlagForSet = false;
		[Input("Set")]
		public bool Set {
			set
			{
				if (value)
				{
					FFlagForSet = true;
				}
			}
		}

		public override void Allocate()
		{
		}

		public override void Process()
		{
			if (FFlagForSet)
			{
				FOutput.Image = FInput.Image;
				FOutput.Send();
				FFlagForSet = false;
			}
		}

	}
}
