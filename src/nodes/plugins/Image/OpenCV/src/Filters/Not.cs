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

#endregion

namespace VVVV.Nodes.OpenCV
{
	public class NotInstance : IFilterInstance
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

	#region PluginInfo
	[PluginInfo(Name = "Not", Category = "OpenCV", Version = "Filter", Help = "Invert image", Author = "", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class NotNode : IFilterNode<NotInstance>
	{
		protected override void Update(int SpreadMax, bool SpreadChanged)
		{
		}
	}
}
