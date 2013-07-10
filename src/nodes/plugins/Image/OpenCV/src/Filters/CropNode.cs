using System;
using System.Drawing;
using Emgu.CV;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace VVVV.Nodes.OpenCV.Filters
{
	public class CropInstance : IFilterInstance
	{
		public Rectangle CropRectangle { private get; set; }

		public override void Allocate()
		{
			FOutput.Image.Initialise(new Size(CropRectangle.Width, CropRectangle.Height), FInput.Image.ImageAttributes.ColourFormat);
		}

		public CropInstance()
		{
			CropRectangle = new Rectangle(0, 0, 100, 100);
		}

		public override void Process()
		{
			if (!FInput.LockForReading()) return;

			CvInvoke.cvSetImageROI(FInput.CvMat, CropRectangle);
			CvInvoke.cvCopy(FInput.CvMat, FOutput.CvMat, IntPtr.Zero);

			CvInvoke.cvResetImageROI(FInput.CvMat);
			
			FInput.ReleaseForReading();
			FOutput.Send();
		}
	}

	[PluginInfo(Name = "Crop", Category = "OpenCV", Help = "Crop image", Author = "alg", Tags = "")]
	public class CropNode :IFilterNode<CropInstance>
	{
		[Input("Crop Rectangle", DefaultValues = new double[] { 0, 0, 100, 100}, DimensionNames = new[]{"px"})]
		private ISpread<Vector4D> FCropRectangleIn; 
		
		protected override void Update(int instanceCount, bool spreadChanged)
		{
			CheckParams(instanceCount);
		}

		private void CheckParams(int instanceCount)
		{
			for (var i = 0; i < instanceCount; i++)
			{
				FProcessor[i].CropRectangle = new Rectangle((int) FCropRectangleIn[i].x, (int) FCropRectangleIn[i].y, (int) FCropRectangleIn[i].z, (int) FCropRectangleIn[i].w);
			}
		}
	}
}
