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
using VVVV.CV.Core;

#endregion usings

namespace VVVV.CV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "FindBoard", Category = "CV.Image", Help = "Finds chessboard corners XY", Tags = "camera calibration")]
	#endregion PluginInfo
	public class FindBoardNode : IDestinationNode<FindBoardInstance>
	{
		#region fields & pins
		[Input("Board size X", IsSingle=true, DefaultValue=8)]
		IDiffSpread<int> FPinInBoardSizeX;

		[Input("Board size Y", IsSingle=true, DefaultValue=6)]
		IDiffSpread<int> FPinInBoardSizeY;

		[Input("Pre-test at 1024 resolution", IsSingle = true)]
		IDiffSpread<bool> FPinInTestLowResolution;

		[Input("Enabled", DefaultValue = 1)]
		IDiffSpread<bool> FPinInEnabled;

		[Output("Position")]
		ISpread<ISpread<Vector2D>> FPinOutPositionXY;

		[Output("Success", IsBang = true)]
		ISpread<bool> FOutSearchSuccess;

		[Output("Fail", IsBang = true, Visibility=PinVisibility.OnlyInspector)]
		ISpread<bool> FOutSearchFail;

		#endregion fields & pins

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			CheckParams(InstanceCount, SpreadChanged);
			Output(InstanceCount);
		}

		void CheckParams(int InstanceCount, bool SpreadChanged)
		{
			if (FPinInBoardSizeX.IsChanged || FPinInBoardSizeY.IsChanged || SpreadChanged)
			{
				for (int i=0; i<InstanceCount; i++)
				{
					FProcessor[i].SetSize(FPinInBoardSizeX[0], FPinInBoardSizeY[0]);
				}
			}

			if (FPinInEnabled.IsChanged || SpreadChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].Enabled = FPinInEnabled[0];
				}
			}

			if (FPinInTestLowResolution.IsChanged || SpreadChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].TestAtLowResolution = FPinInTestLowResolution[0];
				}
			}
		}

		void Output(int InstanceCount)
		{
			FPinOutPositionXY.SliceCount = InstanceCount;
			FOutSearchSuccess.SliceCount = InstanceCount;
			FOutSearchFail.SliceCount = InstanceCount;

			for (int i = 0; i < InstanceCount; i++)
			{
				FPinOutPositionXY[i] = FProcessor[i].GetFoundCorners();
				FOutSearchSuccess[i] = FProcessor[i].SearchSuccessful;
				FOutSearchFail[i] = FProcessor[i].SearchFailed;
				FProcessor[i].SearchSuccessful = false; //clear bang flag
				FProcessor[i].SearchFailed = false;
			}
		}
	}
}
