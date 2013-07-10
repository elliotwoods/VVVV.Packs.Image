using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using VVVV.PluginInterfaces.V2;
using System;

namespace VVVV.Nodes.OpenCV
{
	public class ImageResizeInstance : IFilterInstance
	{
		private Size FSize = new Size(640, 480);

		public void SetSize(int Width, int Height)
		{
			FSize.Width = Width;
			FSize.Height = Height;

			if (FSize.Width < 1)
				FSize.Width = 1;

			if (FSize.Height < 1)
				FSize.Height = 1;

			if (FInput.Allocated)
			{
				Allocate();
				Process();
			}
		}

		public override void Allocate()
		{
			if(!FInput.Allocated) return;

			FOutput.Image.Initialise(FSize, FInput.ImageAttributes.ColorFormat);
			FOutput.Image.Allocate();
		}

		public override void Process()
		{
			try
			{
				CvInvoke.cvResize(FInput.Image.CvMat, FOutput.Image.CvMat, INTER.CV_INTER_LINEAR);
				FOutput.Send();
			}
			catch (Exception e)
			{
				ImageUtils.Log(e);
			}
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "ImageResize", Category = "OpenCV", Help = "Resize Image", Author = "elliotwoods", Credits = "alg", Tags = "")]
	#endregion PluginInfo
	public class ImageResizeNode : IFilterNode<ImageResizeInstance>
	{
		[Input("Width", DefaultValue = 640, MinValue = 1)]
		private IDiffSpread<int> FWidth;

		[Input("Height", DefaultValue = 480, MinValue = 1)]
		private IDiffSpread<int> FHeight;

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			if (FWidth.IsChanged || FHeight.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].SetSize(FWidth[i], FHeight[i]);
		}
	}
}
