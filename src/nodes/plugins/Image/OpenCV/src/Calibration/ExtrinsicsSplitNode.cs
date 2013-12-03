#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using ThreadState = System.Threading.ThreadState;
using System.Collections.Generic;

#endregion usings

namespace VVVV.CV.Nodes
{

	#region PluginInfo
	[PluginInfo(Name = "Extrinsics", Category = "CV.Transform", Version="Split", Help = "Split intrinsics out", Tags = "")]
	#endregion PluginInfo
	public class ExtrinsicsSplitNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Extrinsics")]
		IDiffSpread<Extrinsics> FPinInExtrinsics;

		[Output("Transform")]
		ISpread<Matrix4x4> FPinOutTransform;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
		public ExtrinsicsSplitNode(IPluginHost host)
		{

		}

		public void Dispose()
		{

		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			//if (!FPinInExtrinsics.IsChanged)
			//    return;

			if (FPinInExtrinsics[0] == null)
			{
				FPinOutTransform.SliceCount = 0;
				return;
			}
			else
			{
				FPinOutTransform.SliceCount = SpreadMax;

				for (int i=0; i<SpreadMax; i++)
				{
					FPinOutTransform[i] = FPinInExtrinsics[i].Matrix;
				}
			}
		}

	}
}
