using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using VVVV.PluginInterfaces.V2;
using System;

namespace VVVV.Nodes.OpenCV
{
	#region Interfaces
	public abstract class CMPInstance : IFilterInstance
	{
		public double Threshold = 0.5;
		protected double Mult = byte.MaxValue;
		protected readonly CVImage Buffer = new CVImage();

		private bool FPassOriginal;
		public bool PassOriginal
		{
			set
			{
				FPassOriginal = value;
				ReAllocate();
			}
		}

		public override void Allocate()
		{
			Buffer.Initialise(FInput.ImageAttributes.Size, TColorFormat.L8);

			Mult = FInput.ImageAttributes.BytesPerPixel > 4 ? float.MaxValue : byte.MaxValue;
		}

		public override void Process()
		{
			if (FInput.ImageAttributes.ChannelCount == 1)
			{
				if (!FInput.LockForReading())
					return;
				try
				{
					Compare(FInput.CvMat);
				}
				finally
				{
					FInput.ReleaseForReading();
				}
			}
			else
			{
				FInput.GetImage(Buffer);
				Compare(Buffer.CvMat);
			}

			if (FPassOriginal)
				FOutput.Image.SetImage(FInput.Image);
			if (FPassOriginal)
			{
				CvInvoke.cvNot(Buffer.CvMat, Buffer.CvMat);
				CvInvoke.cvSet(FOutput.Image.CvMat, new MCvScalar(0.0), Buffer.CvMat);
				FOutput.Send();
			}
			else
				FOutput.Send(Buffer);
		}

		protected abstract void Compare(IntPtr CvMat);
	}

	public abstract class CMPNode<T> : IFilterNode<T> where T : CMPInstance, new()
	{
		[Input("Threshold", DefaultValue = 0.5, MinValue = 0, MaxValue = 1)]
		IDiffSpread<double> FThresholdIn;

		[Input("Pass original", DefaultValue = 0)]
		IDiffSpread<bool> FPassOriginalIn;

		protected override void Update(int instanceCount, bool spreadChanged)
		{
			if (FThresholdIn.IsChanged)
				for (int i = 0; i < instanceCount; i++)
					FProcessor[i].Threshold = FThresholdIn[i];

			if (FPassOriginalIn.IsChanged)
				for (int i = 0; i < instanceCount; i++)
					FProcessor[i].PassOriginal = FPassOriginalIn[i];
		}
	}
	#endregion Interfaces

	#region Instances
	public class GTInstance : CMPInstance
	{
		protected override void Compare(IntPtr CvMat)
		{
			CvInvoke.cvCmpS(CvMat, Threshold * Mult, Buffer.CvMat, CMP_TYPE.CV_CMP_GT);
		}
	}

	public class LTInstance : CMPInstance
	{
		protected override void Compare(IntPtr CvMat)
		{
			CvInvoke.cvCmpS(CvMat, Threshold * Mult, Buffer.CvMat, CMP_TYPE.CV_CMP_LT);
		}
	}

	public class EQInstance : CMPInstance
	{
		protected override void Compare(IntPtr CvMat)
		{
			CvInvoke.cvCmpS(CvMat, Threshold * Mult, Buffer.CvMat, CMP_TYPE.CV_CMP_EQ);
		}
	}
	#endregion Instances

	#region Nodes
	[PluginInfo(Name = ">", Help = "Greater than", Category = "OpenCV", Version = "Filter Scalar")]
	public class GTNode : CMPNode<GTInstance>
	{ }

	[PluginInfo(Name = "<", Help = "Less than", Category = "OpenCV", Version = "Filter Scalar")]
	public class LTNode : CMPNode<LTInstance>
	{ }

	[PluginInfo(Name = "=", Help = "Equal to", Category = "OpenCV", Version = "Filter Scalar")]
	public class EQNode : CMPNode<EQInstance>
	{ }
	#endregion nodes
}
