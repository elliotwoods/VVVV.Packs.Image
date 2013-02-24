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
	public class S_HInstance : IFilterInstance
	{
		public bool Set = false;

		public override void Allocate()
		{
		}

		public override void Process()
		{
			if (Set)
			{
				FOutput.Image = FInput.Image;
				FOutput.Send();
			}
		}

	}

	#region PluginInfo
	[PluginInfo(Name = "S+H", Category = "OpenCV", Help = "S+H an Image", Author = "", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class S_HNode : IFilterNode<S_HInstance>
	{
		[Input("Set")]
		ISpread<bool> FPinInSet;

		protected override void Update(int SpreadMax, bool SpreadChanged)
		{
			for (int i = 0; i < SpreadMax; i++)
			{
				FProcessor[i].Set = FPinInSet[i];
			}
		}
	}
}
