using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using VVVV.PluginInterfaces.V2;
using System;
using Emgu.CV.Structure;

namespace VVVV.Nodes.OpenCV
{
	public class UnsharpMaskInstance : IFilterInstance
	{
		public int Width = 3;

		public double WeightMask = 0.5;
		public double WeightOrig = 1.5;

		public double Gamma = 0.5;

		public override void Allocate()
		{
			FOutput.Image.Initialise(FInput.ImageAttributes);
		}

		public override void Process()
		{
			if (!FInput.LockForReading())
				return;
			try
			{
				CvInvoke.cvSmooth(FInput.CvMat, FOutput.CvMat, SMOOTH_TYPE.CV_GAUSSIAN, Width * 2 + 1, 0, 0, 0);
				CvInvoke.cvAddWeighted(FInput.CvMat, WeightOrig, FOutput.CvMat, -WeightMask, Gamma, FOutput.CvMat);
			}
			finally
			{
				FInput.ReleaseForReading();
			}

			FOutput.Send();
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "UnsharpMask", Category = "CV", Version = "Filter", Help = "Sharpen the image using the UnsharpMask algorithm", Author = "velcrome", Credits = "", Tags = "sharpen")]
	#endregion PluginInfo
	public class UnsharpMaskNode : IFilterNode<UnsharpMaskInstance>
	{
		[Input("Width", IsSingle = true, DefaultValue = 3, MinValue = 0, MaxValue = 64)]
		IDiffSpread<int> FPinInWidth;

		[Input("Gamma", IsSingle = true, DefaultValue = 0.5)]
		IDiffSpread<double> FPinGamma;

		[Input("WeightMask", IsSingle = true, DefaultValue = 0.5)]
		IDiffSpread<double> FPinWeightMask;

		[Input("WeightOrig", IsSingle = true, DefaultValue = 1.5)]
		IDiffSpread<double> FPinWeightOrig;

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			if (SpreadChanged || FPinInWidth.IsChanged || FPinGamma.IsChanged || FPinWeightMask.IsChanged || FPinWeightOrig.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].Width = FPinInWidth[0];
					FProcessor[i].Gamma = FPinGamma[0];
					FProcessor[i].WeightMask = FPinWeightMask[0];
					FProcessor[i].WeightOrig = FPinWeightOrig[0];
					FProcessor[i].FlagForProcess();
				}
			}
		}
	}
}