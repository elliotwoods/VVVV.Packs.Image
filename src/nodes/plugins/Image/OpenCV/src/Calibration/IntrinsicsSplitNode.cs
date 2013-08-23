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
	[PluginInfo(Name = "Intrinsics", Category = "OpenCV", Version="Split", Help = "Split intrinsics out", Tags = "")]
	#endregion PluginInfo
	public class IntrinsicsSplitNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Intrinsics")]
		IDiffSpread<Intrinsics> FPinInIntrinsics;

		[Output("Distortion Coefficients")]
		ISpread<ISpread<Double> > FPinOutDistiortonCoefficients;

		[Output("Camera Matrix")]
		ISpread<ISpread<Double>> FPinOutCameraMatrix;

		[Output("Camera")]
		ISpread<Matrix4x4> FPinOutCameraTransform;

		[Output("Normalised projection")]
		ISpread<Matrix4x4> FPinOutProjectionTransform;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
		public IntrinsicsSplitNode(IPluginHost host)
		{

		}

		public void Dispose()
		{
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			//if (!FPinInIntrinsics.IsChanged)
			//    return;

			if (FPinInIntrinsics[0] == null)
			{
				FPinOutCameraMatrix.SliceCount = 0;
				FPinOutCameraTransform.SliceCount = 0;
				FPinOutProjectionTransform.SliceCount = 0;
				FPinOutDistiortonCoefficients.SliceCount = 0;
			}
			else
			{
				FPinOutDistiortonCoefficients.SliceCount = SpreadMax;
				FPinOutCameraTransform.SliceCount = SpreadMax;
				FPinOutCameraMatrix.SliceCount = SpreadMax;
				FPinOutProjectionTransform.SliceCount = SpreadMax;

				for (int i = 0; i < SpreadMax; i++)
				{
					FPinOutDistiortonCoefficients[i].SliceCount = 5;
					for (int j = 0; j < 5; j++)
						FPinOutDistiortonCoefficients[i][j] = FPinInIntrinsics[i].intrinsics.DistortionCoeffs[j, 0];


					FPinOutCameraMatrix[i].SliceCount = 9;
					for (int k = 0; k < 3; k++)
						for (int j = 0; j < 3; j++)
						{
							FPinOutCameraMatrix[i][k + j * 3] = FPinInIntrinsics[i].intrinsics.IntrinsicMatrix[j, k];
						}

					FPinOutCameraTransform[i] = FPinInIntrinsics[i].Matrix;
					FPinOutProjectionTransform[i] = FPinInIntrinsics[i].NormalisedMatrix;
				}
			}
		}
	}
}
