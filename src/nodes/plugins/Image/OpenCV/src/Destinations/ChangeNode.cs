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
using System.Diagnostics;
using VVVV.CV.Core;

#endregion

namespace VVVV.CV.Nodes
{
	public class ChangeInstance : IDestinationInstance
	{
		int FFrames = 0;
		public int Frames
		{
			get
			{
				int f = FFrames;
				FFrames = 0;
				return f;
			}
		}

		public override void Allocate()
		{

		}

		public override void Process()
		{
			FFrames++;
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "Change", Category = "CV.Image", Help = "Report the number of image frames passed through this node between MainLoop frames", Author = "elliotwoods", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class ChangeNode : IDestinationNode<ChangeInstance>
	{
		[Output("Output", DimensionNames=new string[]{"frames"})]
		ISpread<int> FOutput;

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			FOutput.SliceCount = InstanceCount;

			for (int i = 0; i < InstanceCount; i++)
			{
				FOutput[i] = FProcessor[i].Frames;
			}
		}
	}
}
