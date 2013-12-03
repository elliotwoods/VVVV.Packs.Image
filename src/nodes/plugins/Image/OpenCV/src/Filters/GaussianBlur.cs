using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using VVVV.PluginInterfaces.V2;
using System;
using Emgu.CV.Structure;
using VVVV.CV.Core;

namespace VVVV.CV.Nodes
{
    [FilterInstance("GaussianBlur")]
	public class GaussianBlurInstance : IFilterInstance
	{
        [Input("Width")]
		public int Width = 3;

		public override void Allocate()
		{
			FOutput.Image.Initialise(FInput.ImageAttributes);
		}

		public override void Process()
		{
			if (Width == 0)
				FOutput.Image.SetImage(FInput.Image);
			else
			{
				if (!FInput.LockForReading())
					return;
				CvInvoke.cvSmooth(FInput.CvMat, FOutput.CvMat, SMOOTH_TYPE.CV_GAUSSIAN, Width*2+1, 0, 0, 0);
				FInput.ReleaseForReading();
			}
			
			FOutput.Send();
		}
	}
}
