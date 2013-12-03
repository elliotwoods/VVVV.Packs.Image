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
	public class AverageInstance : IDestinationInstance
	{
		public Spread<double> Average
		{
			get
			{
				Spread<double> value = new Spread<double>(FChannelCount);

				if (FChannelCount > 0)
					value[0] = FAverage.v0;
				if (FChannelCount > 1)
					value[1] = FAverage.v1;
				if (FChannelCount > 2)
					value[2] = FAverage.v2;
				if (FChannelCount > 3)
					value[3] = FAverage.v3;

				return value;
			}
		}

		public Spread<double> StandardDeviation
		{
			get
			{
				Spread<double> value = new Spread<double>(FChannelCount);

				if (FChannelCount > 0)
					value[0] = FStandardDeviation.v0;
				if (FChannelCount > 1)
					value[1] = FStandardDeviation.v1;
				if (FChannelCount > 2)
					value[2] = FStandardDeviation.v2;
				if (FChannelCount > 3)
					value[3] = FStandardDeviation.v3;

				return value;
			}
		}

		MCvScalar FAverage = new MCvScalar();
		MCvScalar FStandardDeviation = new MCvScalar();
		int FChannelCount = 1;

		public override void Allocate()
		{
			
		}

		public override void Process()
		{
			FChannelCount = ImageUtils.ChannelCount(FInput.ImageAttributes.ColorFormat);

			if (!FInput.LockForReading())
				return;
			CvInvoke.cvAvgSdv(FInput.CvMat, ref FAverage, ref FStandardDeviation, IntPtr.Zero);
			FInput.ReleaseForReading();
		}

	}

	#region PluginInfo
	[PluginInfo(Name = "Average", Category = "CV.Image", Help = "Returns the mean and standard deviation of the pixel values within an image.", Author = "elliotwoods", Credits = "", Tags = "mean, standard deviation")]
	#endregion PluginInfo
	public class AverageNode : IDestinationNode<AverageInstance>
	{
		[Output("Average")]
		ISpread<ISpread<double>> FAverage;

		[Output("Standard Deviation")]
		ISpread<ISpread<double>> FStandardDeviation;

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			FAverage.SliceCount = InstanceCount;
			FStandardDeviation.SliceCount = InstanceCount;

			for (int i = 0; i < InstanceCount; i++)
			{
				FAverage[i] = FProcessor[i].Average;
				FStandardDeviation[i] = FProcessor[i].StandardDeviation;
			}
		}
	}
}
