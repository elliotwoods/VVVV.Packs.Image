using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using VVVV.PluginInterfaces.V2;
using System;
using Emgu.CV.Structure;

namespace VVVV.Nodes.OpenCV
{
	public class OpticalFlowHSInstance : IFilterInstance
	{
		private Size FSize;

		private CVImage FCurrent = new CVImage();
		private CVImage FPrevious = new CVImage();
		private CVImage FVelocityX = new CVImage();
		private CVImage FVelocityY = new CVImage();

		private double FLambda = 0.1;
		public double Lambda
		{	set
			{
				if (value < 0)
					value = 0;

				if (value > 1)
					value = 1;

				FLambda = value;
			}
		}

		private int FIterations = 100;
		public int Iterations
		{
			set
			{
				if (value < 1)
					value = 1;
				if (value > 500)
					value = 500;

				FIterations = value;
			}
		}

		public bool UsePrevious = false;

		public override void Allocate()
		{
			FSize = FInput.ImageAttributes.Size;
			FOutput.Image.Initialise(FSize, TColorFormat.RGB32F);

			FCurrent.Initialise(FSize, TColorFormat.L8);
			FPrevious.Initialise(FSize, TColorFormat.L8);
			FVelocityX.Initialise(FSize, TColorFormat.L32F);
			FVelocityY.Initialise(FSize, TColorFormat.L32F);
		}

		public override void Process()
		{
			CVImage swap = FPrevious;
			FPrevious = FCurrent;
			FCurrent = swap;

			FInput.Image.GetImage(TColorFormat.L8, FCurrent);

			Image<Gray, byte> p = FPrevious.GetImage() as Image<Gray, byte>;
			Image<Gray, byte> c = FCurrent.GetImage() as Image<Gray, byte>;
			Image<Gray, float> vx = FVelocityX.GetImage() as Image<Gray, float>;
			Image<Gray, float> vy = FVelocityY.GetImage() as Image<Gray, float>;

			OpticalFlow.HS(p, c, UsePrevious, vx, vy, FLambda, new MCvTermCriteria(FIterations));

			CopyToRgb();
			FOutput.Send();

		}

		private unsafe void CopyToRgb()
		{
			float* sourcex = (float*) FVelocityX.Data.ToPointer();
			float* sourcey = (float*) FVelocityY.Data.ToPointer();
			float* dest = (float*) FOutput.Image.Data.ToPointer();

			for (int i = 0; i < FSize.Width * FSize.Height; i++)
			{
				*dest++ = *sourcex++;
				*dest++ = *sourcey++;
				*dest++ = 0.0f;
			}
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "OpticalFlowHS", Category = "OpenCV", Help = "Perform HS optical flow on image", Author = "elliotwoods", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class OpticalFlowHSNode : IFilterNode<OpticalFlowHSInstance>
	{
		[Input("Lambda", DefaultValue = 0.1, MinValue = 0, MaxValue = 1)]
		IDiffSpread<double> FPinInLambda;

		[Input("Maximum Iterations", DefaultValue = 100, MinValue = 1, MaxValue = 500)]
		IDiffSpread<int> FPinInIterations;

		[Input("Use Previous Velocity")]
		IDiffSpread<bool> FPinInUsePrevious;

		protected override void Update(int instanceCount, bool spreadChanged)
		{
			if (FPinInLambda.IsChanged)
				for (int i = 0; i < instanceCount; i++)
					FProcessor[i].Lambda = FPinInLambda[0];

			if (FPinInIterations.IsChanged)
				for (int i = 0; i < instanceCount; i++)
					FProcessor[i].Iterations = FPinInIterations[0];

			if (FPinInUsePrevious.IsChanged)
				for (int i = 0; i < instanceCount; i++)
					FProcessor[i].UsePrevious = FPinInUsePrevious[0];
		}
	}
}
