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
	[FilterInstance("AdaptiveThreshold", Help = "Perform an adaptive threshold over the image", Author = "elliotwoods")]
	public class AdaptiveThresholdInstance : IFilterInstance
	{
		double FMaximum = 255.0;
		ADAPTIVE_THRESHOLD_TYPE FMethod;
		THRESH FType;
		uint FBlockSize;
		double FConstant;

		[Input("Maximum", DefaultValue = 255.0)]
		public double Maximum
		{
			set
			{
				this.FMaximum = value;
			}
		}

		[Input("Method")]
		public ADAPTIVE_THRESHOLD_TYPE Method
		{
			set
			{
				this.FMethod = value;
			}
		}

		[Input("Type")]
		public THRESH Type
		{
			set
			{
				this.FType = value;
			}
		}

		[Input("Block Size", MinValue = 3, MaxValue = 99)]
		public uint BlockSize
		{
			set
			{
				if (value < 3)
					value = 3;
				if (value > 99)
					value = 99;
				if (value % 2 == 0)
					value++;
				this.FBlockSize = value;
			}
		}

		[Input("Constant")]
		public double Constant
		{
			set
			{
				FConstant = value;
			}
		}

		public override void Allocate()
		{
			FOutput.Image.Initialise(FInput.ImageAttributes.Size, TColorFormat.L8);
		}

		public override void Process()
		{
			if (!FInput.LockForReading())
				return;
			try
			{
				FInput.GetImage(FOutput.Image);
				CvInvoke.cvAdaptiveThreshold(FOutput.Image.CvMat, FOutput.Image.CvMat, FMaximum, FMethod, FType, (int)FBlockSize, FConstant);
			}
			catch
			{
				FOutput.Send();
			}
			finally
			{
				FInput.ReleaseForReading();
			}

			FOutput.Send();
		}
	}
}
