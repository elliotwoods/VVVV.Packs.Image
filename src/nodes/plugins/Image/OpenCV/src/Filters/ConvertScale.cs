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

#endregion

namespace VVVV.Nodes.OpenCV
{
	public class ConvertScaleInstance : IFilterInstance
	{
		public override void Allocate()
		{
			FOutput.Image.Initialise(FInput.ImageAttributes);
		}

		double FScale = 1.0;
		public double Scale		{
			set
			{
				this.FScale = value;
			}
		}

		double FOffset = 0.0;
		public double Offset
		{
			set
			{
				this.FOffset = value;
			}
		}

		public override void Process()
		{
			FInput.LockForReading();
			try
			{
				CvInvoke.cvConvertScale(FInput.CvMat, FOutput.CvMat, FScale, FOffset);
			}
			finally
			{
				FInput.ReleaseForReading();
			}
			FOutput.Send();
		}

	}

	#region PluginInfo
	[PluginInfo(Name = "ConvertScale", Category = "CV", Version = "Filter", Help = "Scale and offset an Image by a Value", Tags = "contrast", Author = "elliotwoods", Credits = "")]
	#endregion PluginInfo
	public class ConvertScaleNode : IFilterNode<ConvertScaleInstance>
	{
		[Input("Scale", DefaultValue = 1.0)]
		IDiffSpread<double> FInScale;

		[Input("Offset", DefaultValue = 0.0)]
		IDiffSpread<double> FInOffset;

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			if (FInScale.IsChanged || SpreadChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].Scale = FInScale[i];
					FProcessor[i].FlagForProcess();
				}
			}

			if (FInOffset.IsChanged || SpreadChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].Offset = FInOffset[i];
					FProcessor[i].FlagForProcess();
				}
			}

		}
	}
}
