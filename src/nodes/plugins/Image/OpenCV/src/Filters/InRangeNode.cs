using Emgu.CV;
using Emgu.CV.Structure;

using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace VVVV.Nodes.OpenCV
{
	public class InRangeInstance : IFilterInstance
	{
		public Vector3D LowerEdge { private get; set; }
		public Vector3D UpperEdge { private get; set; }

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
			CvInvoke.cvInRangeS(FHSVImage.CvMat, new MCvScalar(LowerEdge.x, LowerEdge.y, LowerEdge.z), new MCvScalar(UpperEdge.x, UpperEdge.y, UpperEdge.z), FBuffer.CvMat);
		}
	}

	[PluginInfo(Name = "InRange", Help = "Check if value is in target range", Category = "OpenCV", Version = "Filter, HSV")]
	public class InRangeNode : IFilterNode<InRangeInstance>
	{
		[Input("Lower Value", DefaultValue = 0.5)]
		ISpread<Vector3D> FLowerIn;

		[Input("Upper Value", DefaultValue = 0.5)]
		ISpread<Vector3D> FUpperIn;

		[Input("Pass Original", DefaultValue = 0, IsToggle = true)]
		ISpread<bool> FPassOriginalIn;

		protected override void Update(int instanceCount, bool spreadChanged)
		{
			for (var i = 0; i < instanceCount; i++)
			{
				FProcessor[i].LowerEdge = FLowerIn[i];
				FProcessor[i].UpperEdge = FUpperIn[i];
				FProcessor[i].PassOriginal = FPassOriginalIn[i];
			}			
		}
	}
}
