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
	[FilterInstance("UnsharpMask", Help = "Sharpen the image using the UnsharpMask algorithm", Author = "velcrome", Credits = "", Tags = "sharpen")]
	public class UnsharpMaskInstance : IFilterInstance
	{
		[Input("Width", IsSingle = true, DefaultValue = 3, MinValue = 0, MaxValue = 64)]
		public int Width = 3;

		[Input("Gamma", IsSingle = true, DefaultValue = 0.5)]
		public double Gamma = 0.5;

		[Input("WeightMask", IsSingle = true, DefaultValue = 0.5)]
		public double WeightMask = 0.5;

		[Input("WeightOrig", IsSingle = true, DefaultValue = 1.5)]
		public double WeightOrig = 1.5;

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
}