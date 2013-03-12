using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using VVVV.PluginInterfaces.V2;
using System;
using Emgu.CV.Structure;

namespace VVVV.Nodes.OpenCV
{
	public class AdaptiveThresholdInstance : IFilterInstance
	{
		double FMaximum;
		ADAPTIVE_THRESHOLD_TYPE FMethod;
		THRESH FType;
		uint FBlockSize;
		double FConstant;

		public double Maximum
		{
			set
			{
				this.FMaximum = value;
			}
		}

		public ADAPTIVE_THRESHOLD_TYPE Method
		{
			set
			{
				this.FMethod = value;
			}
		}

		public THRESH Type
		{
			set
			{
				this.FType = value;
			}
		}

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

		public double Constant
		{
			set
			{
				this.Constant = value;
			}
		}

		public override void Allocate()
		{
			FOutput.Image.Initialise(FInput.ImageAttributes);
		}

		public override void Process()
		{
			if (!FInput.LockForReading())
				return;
			CvInvoke.cvAdaptiveThreshold(FInput.CvMat, FOutput.CvMat, FMaximum, FMethod, FType, (int) FBlockSize, FConstant);
			FInput.ReleaseForReading();

			FOutput.Send();

		}

	}

	#region PluginInfo
	[PluginInfo(Name = "AdaptiveThreshold", Category = "OpenCV", Help = "Perform an adaptive threshold over the image", Author = "elliotwoods", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class AdaptiveThresholdNode : IFilterNode<AdaptiveThresholdInstance>
	{
		[Input("Maximum", DefaultValue = 255.0)]
		IDiffSpread<double> FPinInMaximum;

		[Input("Adaptive Threshold Method")]
		IDiffSpread<ADAPTIVE_THRESHOLD_TYPE> FPinInMethod;

		[Input("Threshold Type")]
		IDiffSpread<THRESH> FPinInType;

		[Input("Block Size", MinValue = 3, MaxValue = 99)]
		IDiffSpread<int> FPinInBlockSize;

		[Input("Constant")]
		IDiffSpread<double> FPinInConstant;

		protected override void Update(int instanceCount, bool spreadChanged)
		{
			if (FPinInMaximum.IsChanged)
				for (int i = 0; i < instanceCount; i++)
					FProcessor[i].Maximum = FPinInMaximum[i];

			if (FPinInMethod.IsChanged)
				for (int i = 0; i < instanceCount; i++)
					FProcessor[i].Method = FPinInMethod[i];

			if (FPinInType.IsChanged)
				for (int i = 0; i < instanceCount; i++)
					FProcessor[i].Type = FPinInType[i];

			if (FPinInBlockSize.IsChanged)
				for (int i = 0; i < instanceCount; i++)
					FProcessor[i].BlockSize = (uint)FPinInBlockSize[i];

			if (FPinInConstant.IsChanged)
				for (int i = 0; i < instanceCount; i++)
					FProcessor[i].Constant = (uint)FPinInConstant[i];
		}
	}
}
