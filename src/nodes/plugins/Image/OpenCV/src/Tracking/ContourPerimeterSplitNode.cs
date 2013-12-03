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
	[PluginInfo(Name = "ContourPerimeter", Category = "CV.Contour", Version="Split", Help = "Split contour perimeter out", Tags = "")]
	#endregion PluginInfo
	public class ContourPerimeterSplitNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Input")]
		ISpread<ContourPerimeter> FInput;

		[Output("Position")]
		ISpread<ISpread<Vector2D>> FOutPosition;

		[Output("Length")]
		ISpread<double> FOutLength;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
		public ContourPerimeterSplitNode(IPluginHost host)
		{

		}

		public void Dispose()
		{
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FInput.SliceCount == 0 || FInput[0] == null)
			{
				FOutPosition.SliceCount = 0;
				FOutLength.SliceCount = 0;
				return;
			}

			FOutPosition.SliceCount = SpreadMax;
			FOutLength.SliceCount = SpreadMax;

			for (int i = 0; i < SpreadMax; i++)
			{
				FOutLength[i] = FInput[i].Length;
				FOutPosition[i].SliceCount = FInput[i].Points.Length;

				for (int j = 0; j < FInput[i].Points.Length; j++)
				{
					FOutPosition[i][j] = new Vector2D(FInput[i].Points[j].X, FInput[i].Points[j].Y);
				}
			}
		}

	}
}
