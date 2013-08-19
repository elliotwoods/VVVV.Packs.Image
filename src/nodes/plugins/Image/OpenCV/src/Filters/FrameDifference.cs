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

#endregion

namespace VVVV.Nodes.OpenCV
{
	public enum TDifferenceMode
	{
		Positive,
		Negative,
		AbsoluteDifference
	}

	public class FrameDifferenceInstance : IFilterInstance
	{
		CVImage FLastFrame = new CVImage();

		public double Threshold = 0.1;
		private bool FThresholdEnabled = false;
		public bool ThresholdEnabled
		{
			set
			{
				FThresholdEnabled = value;
				ReAllocate();
			}
		}

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

	#region PluginInfo
	[PluginInfo(Name = "FrameDifference", Category = "OpenCV", Version = "", Help = "Output difference between frames", Author = "elliotwoods", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class FrameDifferenceNode : IFilterNode<FrameDifferenceInstance>
	{
		[Input("Threshold")]
		IDiffSpread<double> FThreshold;

		[Input("Threshold Enabled")]
		IDiffSpread<bool> FThresholdEnabled;

		[Input("Difference Mode", DefaultEnumEntry = "AbsoluteDifference")]
		IDiffSpread<TDifferenceMode> FDifferenceMode;

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			if (FThreshold.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Threshold = FThreshold[i];

			if (FThresholdEnabled.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].ThresholdEnabled = FThresholdEnabled[i];

			if (FDifferenceMode.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].DifferenceMode = FDifferenceMode[i];
		}
	}
}
