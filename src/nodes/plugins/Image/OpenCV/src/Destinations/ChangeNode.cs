﻿#region using
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

#endregion

namespace VVVV.Nodes.OpenCV
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
	[PluginInfo(Name = "Change", Category = "OpenCV", Version = "", Help = "Report the number of image frames passed through this node between MainLoop frames", Author = "elliotwoods", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class ChangeNode : IDestinationNode<ChangeInstance>
	{
		[Output("Output", DimensionNames=new string[]{"frames"})]
		ISpread<int> FOutput;

		protected override void Update(int instanceCount, bool spreadChanged)
		{
			FOutput.SliceCount = instanceCount;

			for (int i = 0; i < instanceCount; i++)
			{
				FOutput[i] = FProcessor[i].Frames;
			}
		}
	}
}
