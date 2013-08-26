using Emgu.CV;
using Emgu.CV.CvEnum;
using VVVV.PluginInterfaces.V2;
using System;

namespace VVVV.Nodes.OpenCV
{
	public class WithinRangeInstance : IFilterInstance
	{
		public double Minimum = 0;
		public double Maximum = 1;

		CVImage FImageGT = new CVImage();
		CVImage FImageLT = new CVImage();

		public override void Allocate()
		{
			FImageGT.Initialise(FInput.Image.ImageAttributes.Size, TColorFormat.L8);
			FImageLT.Initialise(FInput.Image.ImageAttributes.Size, TColorFormat.L8);
			FOutput.Image.Initialise(FInput.Image.ImageAttributes.Size, TColorFormat.L8);
		}

		public override void Process()
		{
			if (!FInput.LockForReading())
				return;
			CvInvoke.cvCmpS(FInput.CvMat, Minimum, FImageGT.CvMat, CMP_TYPE.CV_CMP_GE);
			CvInvoke.cvCmpS(FInput.CvMat, Maximum, FImageLT.CvMat, CMP_TYPE.CV_CMP_LE);
			FInput.ReleaseForReading();

			CvInvoke.cvAnd(FImageGT.CvMat, FImageLT.CvMat, FOutput.CvMat, IntPtr.Zero);
			FOutput.Send();
		}

	}

	[PluginInfo(Name = "WithinRange", Category = "OpenCV", Version = "Filter Scalar", Help = "Check if value is in target range", Author = "elliotwoods")]
	public class WithinRangeNode : IFilterNode<WithinRangeInstance>
	{
		[Input("Minimum", DefaultValue = 0)]
		IDiffSpread<double> FMinimum;

		[Input("Maximum", DefaultValue = 1)]
		IDiffSpread<double> FMaximum;

		protected override void Update(int instanceCount, bool spreadChanged)
		{
			if (FMinimum.IsChanged)
				for (int i = 0; i < instanceCount; i++)
					FProcessor[i].Minimum = FMinimum[i];

			if (FMaximum.IsChanged)
				for (int i = 0; i < instanceCount; i++)
					FProcessor[i].Maximum = FMaximum[i];
		}
	}
}
