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
	[FilterInstance("WithinRange")]
	public class WithinRangeInstance : IFilterInstance
	{
		[Input("Minimum", DefaultValue = 0)]
		public double Minimum = 0;

		[Input("Maximum", DefaultValue = 1)]
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
}
