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

namespace VVVV.Nodes.OpenCV
{
	#region PluginInfo
	[PluginInfo(Name = "FindBoard", Category = "OpenCV", Help = "Finds chessboard corners XY", Tags = "")]
	#endregion PluginInfo
	public class FindBoardNode : IDestinationNode<FindBoardInstance>
	{
		#region fields & pins
		[Input("Board size X", IsSingle=true, DefaultValue=8)]
		IDiffSpread<int> FPinInBoardSizeX;

		[Input("Board size Y", IsSingle=true, DefaultValue=6)]
		IDiffSpread<int> FPinInBoardSizeY;

		[Input("Enabled", DefaultValue = 1)]
		IDiffSpread<bool> FPinInEnabled;

		[Output("Position")]
		ISpread<ISpread<Vector2D>> FPinOutPositionXY;
		#endregion fields & pins

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			CheckParams(InstanceCount);
			Output(InstanceCount);
		}

		void CheckParams(int InstanceCount)
		{
			if (FPinInBoardSizeX.IsChanged || FPinInBoardSizeY.IsChanged)
				for (int i=0; i<InstanceCount; i++)
				{
					FProcessor[i].SetSize(FPinInBoardSizeX[0], FPinInBoardSizeY[0]);
				}

			if (FPinInEnabled.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].Enabled = FPinInEnabled[0];
				}
		}

		void Output(int InstanceCount)
		{
			FPinOutPositionXY.SliceCount = InstanceCount;

			for (int i = 0; i < InstanceCount; i++)
			{
				FPinOutPositionXY[i] = FProcessor[i].GetFoundCorners();
			}
		}
	}
}
