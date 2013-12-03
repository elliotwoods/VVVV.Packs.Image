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
	[FilterInstance("Average", Tags = "temporal", Help = "Average input image over N frames (outputs at input framerate / N)", Author = "elliotwoods")]
	public class TemporalAverageInstance : IFilterInstance
	{
		private int FFrames = 1;

		[Input("Frames", MinValue = 1, MaxValue = 64, DefaultValue = 1)]
		public int Frames
		{
			set
			{
				if (value < 1)
					value = 1;
				if (value > 64)
					value = 64;

				FFrames = value;
				ReAllocate();
			}
		}
		private int FFrame = 0;

		public override void Allocate()
		{
			FFrame = 0;

			FOutput.Image.Initialise(FInput.Image.ImageAttributes);
		}

		public override void Process()
		{
			if (!FInput.LockForReading())
				return;
			CvInvoke.cvAddWeighted(	FOutput.CvMat, (double) (FFrame + 1) / (double) FFrames,
									FInput.CvMat, 1.0 / (double) FFrames, 0, FOutput.CvMat);
			FInput.ReleaseForReading();

			FFrame++;
			if (FFrame >= FFrames)
			{
				FOutput.Send();
				CvInvoke.cvSet(FOutput.CvMat, new MCvScalar(0.0), IntPtr.Zero);
				FFrame = 0;
			}
		}
	}
}
