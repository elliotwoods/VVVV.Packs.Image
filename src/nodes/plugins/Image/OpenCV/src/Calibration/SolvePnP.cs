﻿#region usings
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
	[PluginInfo(Name = "SolvePnP", Category = "OpenCV", Help = "Find pose of object given camera intrinsics", Tags = "")]
	#endregion PluginInfo
	public class SolvePnPNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Object Points")]
		ISpread<ISpread<Vector3D> > FPinInObject;

		[Input("Image Points")]
		ISpread<ISpread<Vector2D>> FPinInImage;

		[Input("Intrinsics")]
		ISpread<Intrinsics> FPinInIntrinsics;

		[Output("Extrinsics")]
		ISpread<Extrinsics> FPinOutExtrinsics;

		[Output("Status")]
		ISpread<String> FPinOutStatus;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
		public SolvePnPNode(IPluginHost host)
		{

		}

		public void Dispose()
		{

		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			SpreadMax = Math.Max(FPinInObject.SliceCount, FPinInImage.SliceCount);

			FPinOutExtrinsics.SliceCount = SpreadMax;
			FPinOutStatus.SliceCount = SpreadMax;

			for (int i = 0; i < SpreadMax; i++)
			{
				try
				{
					if (FPinInObject[i].SliceCount == 0 || FPinInImage[i].SliceCount == 0)
						throw new Exception("No datapoints");
					if (FPinInImage[i].SliceCount == 1)
						throw new Exception("Only 1 image point is being input per board, check SliceCount!");
					if (FPinInObject[i].SliceCount == 1)
						throw new Exception("Only 1 object point is being input per board, check SliceCount!");
					if (FPinInIntrinsics[i].intrinsics == null)
						throw new Exception("Waiting for camera calibration intrinsics");

					ExtrinsicCameraParameters extrinsics = CameraCalibration.FindExtrinsicCameraParams2(MatrixUtils.ObjectPoints(FPinInObject[i], true), MatrixUtils.ImagePoints(FPinInImage[i]), FPinInIntrinsics[i].intrinsics);
					FPinOutExtrinsics[i] = new Extrinsics(extrinsics);
					FPinOutStatus[i] = "OK";
				}
				catch (Exception e)
				{
					FPinOutStatus[i] = e.Message;
				}
				
			}
		}
	}
}
