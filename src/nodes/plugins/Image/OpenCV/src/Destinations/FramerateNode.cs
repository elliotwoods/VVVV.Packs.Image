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
	public enum FramerateNodeMode { Filtered, Raw };

	public class FramerateInstance : IDestinationInstance
	{
		public double Framerate;
		Stopwatch FTimer = new Stopwatch();
		TimeSpan FPeriod;

		public FramerateNodeMode Mode = FramerateNodeMode.Filtered;

		public override void Allocate()
		{
			
		}

		public override void Process()
		{
			FPeriod = FTimer.Elapsed;
			if (FPeriod.TotalSeconds != 0)
			{
				double thisFrame = 1.0d / FPeriod.TotalSeconds;
				if (Mode == FramerateNodeMode.Raw || double.IsNaN(Framerate) || double.IsInfinity(Framerate))
					Framerate = thisFrame;
				else
				{
					Framerate = 0.9 * Framerate + 0.1 * thisFrame;
				}
			}

			FTimer.Reset();
			FTimer.Start();
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "Framerate", Category = "CV.Image", Help = "Report the framerate that an image is being updated at", Author = "elliotwoods", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class FramerateNode : IDestinationNode<FramerateInstance>
	{
		[Input("Mode")]
		ISpread<FramerateNodeMode> FMode;

		[Output("Framerate", DimensionNames=new string[]{"fps"})]
		ISpread<double> FFramerate;

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			FFramerate.SliceCount = InstanceCount;

			for (int i = 0; i < InstanceCount; i++)
			{
				FProcessor[i].Mode = FMode[i];
				FFramerate[i] = FProcessor[i].Framerate;
			}
		}
	}
}
