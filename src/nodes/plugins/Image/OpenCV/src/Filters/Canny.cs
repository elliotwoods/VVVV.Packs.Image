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
	[FilterInstance("Canny", Help = "Find edges in image using Canny filter", Author = "elliotwoods", Credits = "", Tags = "edge detection")]
	public class CannyInstance : IFilterInstance
	{
		[Input("Threshold Min")]
		public double ThresholdMin = 20;

		[Input("Threshold Max")]
		public double ThresholdMax = 40;

		private int FWindowSize = 3;
		[Input("Window size", MinValue = 3, MaxValue = 7, DefaultValue = 3)]
		public int WindowSize
		{
			set
			{
				if (value < 3)
					value = 3;

				if (value > 7)
					value = 7;

				value += (value + 1) % 2;

				FWindowSize = value;
			}
		}

		private bool FNeedsConversion = false;
		private CVImage FGrayscale = new CVImage();

		public override void Allocate()
		{
			TColorFormat AsGrayscale = TypeUtils.ToGrayscale(FInput.ImageAttributes.ColorFormat);
			FNeedsConversion = (AsGrayscale != FInput.ImageAttributes.ColorFormat);

			if (FNeedsConversion)
			{
				FGrayscale.Initialise(FInput.ImageAttributes.Size, AsGrayscale);
			}

			FOutput.Image.Initialise(FGrayscale.ImageAttributes);
		}

		public override void Process()
		{
			if (FNeedsConversion)
			{
				FInput.GetImage(FGrayscale);
				CvInvoke.cvCanny(FGrayscale.CvMat, FOutput.CvMat, ThresholdMin, ThresholdMax, FWindowSize);
			}
			else
			{
				FInput.LockForReading();
				try
				{
					CvInvoke.cvCanny(FInput.CvMat, FOutput.CvMat, ThresholdMin, ThresholdMax, FWindowSize);
				}
				finally
				{
					FInput.ReleaseForReading();
				}
			}
			FOutput.Send();
		}

	}
}
