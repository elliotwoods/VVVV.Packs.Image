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
	[FilterInstance("ConvertScale", Help = "Scale and offset an Image by a Value", Tags = "contrast", Author = "elliotwoods", Credits = "")]
	public class ConvertScaleInstance : IFilterInstance
	{
		public override void Allocate()
		{
			FOutput.Image.Initialise(FInput.ImageAttributes);
		}

		[Input("Scale")]
		public double Scale = 1.0;

		[Input("Offset")]
		public double Offset = 0.0;

		public override void Process()
		{
			FInput.LockForReading();
			try
			{
				CvInvoke.cvConvertScale(FInput.CvMat, FOutput.CvMat, Scale, Offset);
			}
			finally
			{
				FInput.ReleaseForReading();
			}
			FOutput.Send();
		}
	}
}
