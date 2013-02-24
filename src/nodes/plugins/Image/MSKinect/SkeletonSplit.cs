#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

using ThreadState = System.Threading.ThreadState;
using System.Collections.Generic;

using Microsoft.Kinect;

#endregion usings

namespace VVVV.Nodes.OpenCV.Kinect
{
	#region PluginInfo
	[PluginInfo(Name = "Skeleton", Category = "OpenCV", Version = "Kinect, Split",  Help = "Split Skeleton into joint data spreads", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class SkeletonSplitNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Skeleton")]
		ISpread<ISpread<Skeleton>> FSkeletons;

		[Output("Status")]
		ISpread<String> FStatus;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
		public SkeletonSplitNode(IPluginHost host)
		{

		}

		public void Dispose()
		{

		}

		public void Evaluate(int SpreadMax)
		{
		}
	}
}
