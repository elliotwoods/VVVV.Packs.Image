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
	[FilterInstance("BackgroundSubtract", Help = "Output difference between current frame and captured background", Author = "elliotwoods", Tags = "tracking")]
	public class BackgroundSubtractInstance : IFilterInstance
	{
		[Input("Set", IsBang=true)]
		public bool Set {
			set
			{
				if (value) {
					this.FFlagForHold = true;
				}
			}
		}

		[Input("Threshold")]
		public double Threshold = 0.2;

		[Input("Threshold Enabled")]
		public bool ThresholdEnabled = false;

		CVImage FBackground = new CVImage();

		[Input("Difference Mode", DefaultEnumEntry = "AbsoluteDifference")]
		public TDifferenceMode DifferenceMode = TDifferenceMode.AbsoluteDifference;

		bool FFlagForHold = false;
		public void Hold()
		{
			FFlagForHold = true;
		}

		public override void Allocate()
		{
			FOutput.Image.Initialise(FInput.ImageAttributes.Size, TColorFormat.L8);
			FBackground.Initialise(FInput.ImageAttributes.Size, TColorFormat.L8);
		}

		private bool NeedsConversion { get { return FInput.ImageAttributes.ColorFormat != TColorFormat.L8; } }

		public override void Process()
		{
			if (FFlagForHold)
			{
				FInput.Image.GetImage(FBackground);
				FFlagForHold = false;
			}

			FInput.GetImage(FOutput.Image); // temporary

			if (DifferenceMode == TDifferenceMode.AbsoluteDifference)
				CvInvoke.cvAbsDiff(FOutput.CvMat, FBackground.CvMat, FOutput.CvMat);

			if (ThresholdEnabled)
				CvInvoke.cvThreshold(FOutput.CvMat, FOutput.CvMat, 255.0d * Threshold, 255, THRESH.CV_THRESH_BINARY);

			FOutput.Send();
		}

	}
}
