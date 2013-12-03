using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using VVVV.PluginInterfaces.V2;
using System;
using VVVV.CV.Core;

namespace VVVV.CV.Nodes
{
	[FilterInstance("Resize", Help = "Resize an image", Author = "elliotwoods", Credits = "alg")]
	public class ResizeInstance : IFilterInstance
	{
		private Size FSize = new Size(640, 480);

		[Input("Width", DefaultValue = 640, MinValue = 1)]
		public int Width
		{
			set
			{
				if (value > 1)
				{
					FSize.Width = value;
				}
				else
				{
					FSize.Width = 1;
				}
			}
		}

		[Input("Height", DefaultValue = 480, MinValue = 1)]
		public int Height
		{
			set
			{
				if (value > 1)
				{
					FSize.Height = value;
				}
				else
				{
					FSize.Height = 1;
				}
			}
		}

		public override void Allocate()
		{
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
}
