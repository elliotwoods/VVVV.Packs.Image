using Emgu.CV;
using Emgu.CV.Structure;

using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace VVVV.Nodes.OpenCV
{
	public class WithinRangeHSVInstance : IFilterInstance
	{
		public Vector3D Minimum { private get; set; }
		public Vector3D Maximum { private get; set; }

		private double FMult = byte.MaxValue;

		private readonly CVImage FBuffer = new CVImage();
		private readonly CVImage FHSVImage = new CVImage();


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
			FBuffer.Initialise(FInput.ImageAttributes.Size, TColorFormat.L8);
			FHSVImage.Initialise(FInput.ImageAttributes.Size, TColorFormat.HSV32F);

			FMult = FInput.ImageAttributes.BytesPerPixel > 4 ? float.MaxValue : byte.MaxValue;
		}

		public override void Process()
		{
			FInput.GetImage(FHSVImage);
			Compare();

			if (FPassOriginal)
			{
				FOutput.Image.SetImage(FInput.Image);

				CvInvoke.cvNot(FBuffer.CvMat, FBuffer.CvMat);
				CvInvoke.cvSet(FOutput.Image.CvMat, new MCvScalar(0.0), FBuffer.CvMat);
				FOutput.Send();
			}
			else
				FOutput.Send(FBuffer);
		}

		private void Compare()
		{
			CvInvoke.cvInRangeS(FHSVImage.CvMat, new MCvScalar(Minimum.x * FMult, Minimum.y * FMult, Minimum.z * FMult), new MCvScalar(Maximum.x * FMult, Maximum.y * FMult, Maximum.z * FMult), FBuffer.CvMat);
		}
	}

	[PluginInfo(Name = "WithinRange", Help = "Check if value is in target range", Category = "OpenCV", Version = "Filter HSV", Author = "alg")]
	public class WithinRangeHSVNode : IFilterNode<WithinRangeHSVInstance>
	{
		[Input("Minimum HSV ", DefaultValues = new double[]{0,0,0}, MinValue = 0, MaxValue = 1)]
		ISpread<Vector3D> FMinimumIn;

		[Input("Maximum HSV ", DefaultValues = new double[] { 1, 1, 1 }, MinValue = 0, MaxValue = 1)]
		ISpread<Vector3D> FMaximumIn;

		[Input("Pass Original", DefaultValue = 0, IsToggle = true)]
		ISpread<bool> FPassOriginalIn;

		protected override void Update(int instanceCount, bool spreadChanged)
		{
			for (var i = 0; i < instanceCount; i++)
			{
				FProcessor[i].Minimum = FMinimumIn[i];
				FProcessor[i].Maximum = FMaximumIn[i];
				FProcessor[i].PassOriginal = FPassOriginalIn[i];
			}			
		}
	}
}
