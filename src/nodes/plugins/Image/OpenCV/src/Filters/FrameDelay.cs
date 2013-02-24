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
	public class FrameDelayInstance : IFilterInstance
	{
		CVImage FBuffer = new CVImage();

		public override void Allocate()
		{
			FBuffer.Initialise(FInput.ImageAttributes);
		}

		public override void Process()
		{
			if (FInput.Allocated)
			{
				if (FBuffer.Allocated)
				{
					FOutput.Image.SetImage(FBuffer);
					FOutput.Send();
				}

				FBuffer.SetImage(FInput.Image);
			}
		}

	}

	#region PluginInfo
	[PluginInfo(Name = "FrameDelay", Category = "OpenCV", Version = "", Help = "Delay output by 1 frame", Author = "elliotwoods", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class FrameDelayNode : IFilterNode<FrameDelayInstance>
	{

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
		}
	}
}