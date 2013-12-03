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
    [FilterInstance("FrameDelay", Author = "elliotwoods")]
	public class FrameDelayInstance : IFilterInstance
	{
		CVImage FBuffer = new CVImage();

		public override void Allocate()
		{
			FBuffer.Initialise(FInput.ImageAttributes);
		}

		public override void Process()
		{
			if (FInput.Allocated)
			{
				if (FBuffer.Allocated)
				{
					FOutput.Image.SetImage(FBuffer);
					FOutput.Send();
				}

				FBuffer.SetImage(FInput.Image);
			}
		}

	}
}