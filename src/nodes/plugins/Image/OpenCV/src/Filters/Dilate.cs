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
	[FilterInstance("Dilate", Help = "Inflate features in image, i.e. grow noise", Author = "elliotwoods", Tags = "denoise")]
	public class DilateInstance : IFilterInstance
	{
		private int FIterations = 1;
		[Input("Iterations", MinValue = 0, MaxValue = 64, DefaultValue = 1)]
		public int Iterations
		{
			set
			{
				if (value < 0)
					value = 0;
				if (value > 64)
					value = 64;

				FIterations = value;
			}
		}

		public override void Allocate()
		{
			FOutput.Image.Initialise(FInput.Image.ImageAttributes);
		}

		public override void Process()
		{
			if (!FInput.LockForReading())
				return;
			CvInvoke.cvDilate(FInput.CvMat, FOutput.CvMat, IntPtr.Zero, FIterations);
			FInput.ReleaseForReading();

			FOutput.Send();
		}
	}
}
