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
	[FilterInstance("OpticalFlow", Version = "Lucas-Kanade", Help = "Perform LK optical flow on image", Author = "elliotwoods")]
	public class OpticalFlowLKInstance : IFilterInstance
	{
		private Size FSize;

		private CVImage FCurrent = new CVImage();
		private CVImage FPrevious = new CVImage();
		private CVImage FVelocityX = new CVImage();
		private CVImage FVelocityY = new CVImage();

		private Size FWindowSize = new Size(5, 5);
		[Input("Window Size", IsSingle = true, DefaultValue = 5, MinValue = 1, MaxValue = 15)]
		public int WindowSize
		{
			get
			{
				return FWindowSize.Width;
			}
			set
			{
				if (value < 1)
					value = 1;

				if (value > 15)
					value = 15;

				value += (value+1) % 2;

				FWindowSize.Width = value;
				FWindowSize.Height = value;
			}
		}

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

			OpticalFlow.LK(p, c, FWindowSize, vx, vy);

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
}
