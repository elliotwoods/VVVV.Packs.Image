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
	public class BackgroundSubtractInstance : IFilterInstance
	{
		CVImage FBackground = new CVImage();

		public double Threshold = 0.1;
		private bool FThresholdEnabled = false;
		public bool ThresholdEnabled
		{
			set
			{
				FThresholdEnabled = value;
			}
		}

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

			if (FThresholdEnabled)
				CvInvoke.cvThreshold(FOutput.CvMat, FOutput.CvMat, 255.0d * Threshold, 255, THRESH.CV_THRESH_BINARY);

			FOutput.Send();
		}

	}

	#region PluginInfo
	[PluginInfo(Name = "BackgroundSubtract", Category = "OpenCV", Version = "", Help = "Output difference between current frame and captured background", Author = "elliotwoods", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class BackgroundSubtractNode : IFilterNode<BackgroundSubtractInstance>
	{
		[Input("Set", IsBang=true)]
		ISpread<bool> FHold;

		[Input("Threshold")]
		IDiffSpread<double> FThreshold;

		[Input("Threshold Enabled")]
		IDiffSpread<bool> FThresholdEnabled;

		[Input("Difference Mode", DefaultEnumEntry = "AbsoluteDifference")]
		IDiffSpread<TDifferenceMode> FDifferenceMode;

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			for (int i = 0; i < InstanceCount; i++)
			{
				if (FHold[i])
				{
					FProcessor[i].Hold();
				}
			}

			if (FThreshold.IsChanged || SpreadChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].Threshold = FThreshold[i];
					FProcessor[i].FlagForProcess();
				}
			}

			if (FThresholdEnabled.IsChanged || SpreadChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].ThresholdEnabled = FThresholdEnabled[i];
					FProcessor[i].FlagForProcess();
				}
			}

			if (FDifferenceMode.IsChanged || SpreadChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].DifferenceMode = FDifferenceMode[i];
					FProcessor[i].FlagForProcess();
				}
			}
		}
	}
}
