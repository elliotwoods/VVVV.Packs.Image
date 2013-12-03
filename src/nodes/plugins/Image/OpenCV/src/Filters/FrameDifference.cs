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
	public enum TDifferenceMode
	{
		Positive,
		Negative,
		AbsoluteDifference
	}

	[FilterInstance("FrameDifference", Help = "Output difference between frames", Author = "elliotwoods")]
	public class FrameDifferenceInstance : IFilterInstance
	{
		CVImage FLastFrame = new CVImage();

		[Input("Threshold")]
		public double Threshold = 0.1;

		private bool FThresholdEnabled = false;
		[Input("Threshold Enabled")]
		public bool ThresholdEnabled
		{
			set
			{
				FThresholdEnabled = value;
				ReAllocate();
			}
		}

		[Input("Mode", DefaultEnumEntry="AbsoluteDifference")]
		public TDifferenceMode DifferenceMode = TDifferenceMode.AbsoluteDifference;

		public override void Allocate()
		{
			FOutput.Image.Initialise(FInput.ImageAttributes);
			FLastFrame.Initialise(FInput.ImageAttributes);
		}

		public override void Process()
		{
			FInput.LockForReading();
			try
			{
				switch (DifferenceMode)
				{
					case TDifferenceMode.AbsoluteDifference:
						CvInvoke.cvAbsDiff(FInput.CvMat, FLastFrame.CvMat, FOutput.CvMat);
						break;
					case TDifferenceMode.Negative:
						CvInvoke.cvSub(FInput.CvMat, FLastFrame.CvMat, FOutput.CvMat, new IntPtr());
						break;
					case TDifferenceMode.Positive:
						CvInvoke.cvSub(FLastFrame.CvMat, FInput.CvMat, FOutput.CvMat, new IntPtr());
						break;
				}
			}
			catch
			{
			}
			finally
			{
				FInput.ReleaseForReading();
			}

			if (FThresholdEnabled)
			{
				if (FInput.ImageAttributes.ColorFormat != TColorFormat.L8)
					Status = "Cannot perform threshold on image type " + FInput.ImageAttributes.ColorFormat.ToString() + ". Can only perform threshold on L8";
				else
					CvInvoke.cvThreshold(FOutput.CvMat, FOutput.CvMat, 255.0d * Threshold, 255, THRESH.CV_THRESH_BINARY);
			}

			FInput.GetImage(FLastFrame);

			FOutput.Send();
		}
	}
}
