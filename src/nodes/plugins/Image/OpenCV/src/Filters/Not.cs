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
	[FilterInstance("NOT", Help = "Invert image")]
	public class NOTInstance : IFilterInstance
	{
		public override void Allocate()
		{
			FOutput.Image.Initialise(FInput.ImageAttributes);
		}

		public override void Process()
		{
			if (!FInput.LockForReading()) //this
				return;
			CvInvoke.cvNot(FInput.CvMat, FOutput.CvMat);
			FInput.ReleaseForReading(); //and  this after you've finished with FImage
			FOutput.Send();
		}

	}
}
