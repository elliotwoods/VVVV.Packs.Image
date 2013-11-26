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
				FConstant = value;
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
			try
			{
				CvInvoke.cvAdaptiveThreshold(FInput.CvMat, FOutput.CvMat, FMaximum, FMethod, FType, (int)FBlockSize, FConstant);
			}
			finally
			{
				FInput.ReleaseForReading();
			}

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

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			if (FPinInMaximum.IsChanged || SpreadChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].Maximum = FPinInMaximum[i];
					FProcessor[i].FlagForProcess();
				}
			}

			if (FPinInMethod.IsChanged || SpreadChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].Method = FPinInMethod[i];
					FProcessor[i].FlagForProcess();
				}
			}

			if (FPinInType.IsChanged || SpreadChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].Type = FPinInType[i];
					FProcessor[i].FlagForProcess();
				}
			}

			if (FPinInBlockSize.IsChanged || SpreadChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].BlockSize = (uint)FPinInBlockSize[i];
					FProcessor[i].FlagForProcess();
				}
			}

			if (FPinInConstant.IsChanged || SpreadChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].Constant = (uint)FPinInConstant[i];
					FProcessor[i].FlagForProcess();
				}
			}
		}
	}
}
