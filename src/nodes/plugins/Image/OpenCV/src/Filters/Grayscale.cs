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
	[FilterInstance("Grayscale", Help = "Converts incoming images to respective grayscale versions", Author = "elliotwoods")]
	public class GrayscaleInstance : IFilterInstance
	{
		TColorFormat FOutFormat;
		public override void Allocate()
		{
			FOutFormat = ImageUtils.MakeGrayscale(FInput.ImageAttributes.ColorFormat);

			//if we can't convert or it's already grayscale, just pass through
			if (FOutFormat == TColorFormat.UnInitialised)
				FOutFormat = FInput.ImageAttributes.ColorFormat;

			FOutput.Image.Initialise(FInput.Image.ImageAttributes.Size, FOutFormat);
		}

		public override void Process()
		{
			FInput.GetImage(FOutput.Image);
			FOutput.Send();
		}
	}
}
