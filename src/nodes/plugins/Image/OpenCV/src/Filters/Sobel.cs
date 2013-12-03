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
	[FilterInstance("Sobel", Help = "Find the 2D derivative of an image using the Sobel filter", Tags = "edge detection")]
	public class SobelInstance : IFilterInstance
	{
		private int FAperture = 3;
		[Input("Aperture size", MinValue = 3, MaxValue = 7, DefaultValue = 3)]
		public int Aperture
		{
			set
			{
				if (value < 3)
					value = 3;

				if (value > 7)
					value = 7;

				value += (value + 1) % 2;

				FAperture = value;
			}
		}

		private int FXOrder = 1;
		[Input("X Order", DefaultValue = 1)]
		public int XOrder
		{
			set
			{
				if (value < 0)
					value = 0;
				if (value > 8)
					value = 8;
				FXOrder = value;
			}
		}

		private int FYOrder = 1;
		[Input("Y Order", DefaultValue = 1)]
		public int YOrder
		{
			set
			{
				if (value < 0)
					value = 0;
				if (value > 8)
					value = 8;
				FYOrder = value;
			}
		}

		public override void Allocate()
		{
			FOutput.Image.Initialise(FInput.ImageAttributes);
		}

		public override void Process()
		{
			FInput.LockForReading();
			try
			{
				CvInvoke.cvSobel(FInput.CvMat, FOutput.CvMat, FXOrder, FYOrder, FAperture);
			}
			finally
			{
				FInput.ReleaseForReading();
			}
			FOutput.Send();
		}
	}
}
