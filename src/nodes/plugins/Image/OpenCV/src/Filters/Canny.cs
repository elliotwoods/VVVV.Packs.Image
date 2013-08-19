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
	public class CannyInstance : IFilterInstance
	{

		public double ThresholdMin = 20;
		public double ThresholdMax = 40;

		private bool FNeedsConversion = false;
		private CVImage FGrayscale = new CVImage();

		private int FAperture = 5;
		public int Aperture
		{
			set
			{
				if (value < 3)
					value = 3;

				if (value > 7)
					value = 7;

				value += (value + 1) % 2;

				FAperture = value;
			}
		}

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
				CvInvoke.cvCanny(FGrayscale.CvMat, FOutput.CvMat, ThresholdMin, ThresholdMax, FAperture);
			}
			else
			{
				FInput.LockForReading();
				try
				{
					CvInvoke.cvCanny(FInput.CvMat, FOutput.CvMat, ThresholdMin, ThresholdMax, FAperture);
				}
				finally
				{
					FInput.ReleaseForReading();
				}
			}
			FOutput.Send();
		}

	}

	#region PluginInfo
	[PluginInfo(Name = "Canny", Category = "OpenCV", Version = "", Help = "Find edges in image using Canny filter", Author = "elliotwoods", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class CannyNode : IFilterNode<CannyInstance>
	{
		[Input("Threshold min", DefaultValue = 20)]
		IDiffSpread<double> FThresholdMin;

		[Input("Threshold max", DefaultValue = 40)]
		IDiffSpread<double> FThresholdMax;

		[Input("Window size", MinValue = 3, MaxValue = 7, DefaultValue = 3)]
		IDiffSpread<int> FWindowSize;

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			if (FThresholdMin.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].ThresholdMin = FThresholdMin[i];

			if (FThresholdMax.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].ThresholdMax = FThresholdMax[i];

			if (FWindowSize.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Aperture = FWindowSize[i];

		}
	}
}
