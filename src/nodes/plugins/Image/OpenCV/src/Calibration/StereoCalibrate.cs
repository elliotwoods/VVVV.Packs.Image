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
	[PluginInfo(Name = "StereoCalibrate", Category = "CV.Transform", Help = "Finds extrinsics between 2 cameras", Tags = "")]
	#endregion PluginInfo
	public class StereoCalibrateNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Image Points 1")]
		ISpread<Vector2D> FPinInImage1;

		[Input("Image Points 2")]
		ISpread<Vector2D> FPinInImage2;

		[Input("Object Points")]
		ISpread<Vector3D> FPinInObject;

		[Input("Resolution")]
		ISpread<Vector2D> FPinInSensorSize;

		[Input("Intrinsics 1", IsSingle=true)]
		ISpread<Intrinsics> FPinInIntrinsics1;

		[Input("Intrinsics 2", IsSingle = true)]
		ISpread<Intrinsics> FPinInIntrinsics2;

		[Input("Do", IsBang=true, IsSingle=true)]
		ISpread<bool> FPinInDo;

		[Output("Extrinsics")]
		ISpread<Extrinsics> FPinOutExtrinsics;

		[Output("World transform")]
		ISpread<Matrix4x4> FPinOutTransform;

		[Output("Success")]
		ISpread<bool> FPinOutSuccess;

		[Output("Status")]
		ISpread<string> FStatus;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
		public StereoCalibrateNode(IPluginHost host)
		{

		}

		public void Dispose()
		{

		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FPinInDo[0])
			{
				int nPointsPerImage = FPinInObject.SliceCount;
				if (nPointsPerImage == 0)
				{
					FStatus[0] = "Insufficient points";
					return;
				}
				int nImages = Math.Max(FPinInImage1.SliceCount, FPinInImage2.SliceCount) / nPointsPerImage;

				CALIB_TYPE flags = CALIB_TYPE.DEFAULT;
				MCvTermCriteria termCrit = new MCvTermCriteria(100, 0.001);
				MCvPoint3D32f[][] objectPoints = new MCvPoint3D32f[nImages][];
				PointF[][] imagePoints1 = new PointF[nImages][];
				PointF[][] imagePoints2 = new PointF[nImages][];
				Size imageSize = new Size( (int) FPinInSensorSize[0].x, (int) FPinInSensorSize[0].y);
				ExtrinsicCameraParameters interCameraExtrinsics;
				Matrix<double> foundamentalMatrix;
				Matrix<double> essentialMatrix;
				IntrinsicCameraParameters intrinsics1 = FPinInIntrinsics1[0].intrinsics;
				IntrinsicCameraParameters intrinsics2 = FPinInIntrinsics2[0].intrinsics;

				imagePoints1 = MatrixUtils.ImagePoints(FPinInImage1, nPointsPerImage);
				imagePoints2 = MatrixUtils.ImagePoints(FPinInImage2, nPointsPerImage);

				for (int i=0; i<nImages; i++)
				{
					objectPoints[i] = MatrixUtils.ObjectPoints(FPinInObject, true);
				}

				try
				{
					CameraCalibration.StereoCalibrate(objectPoints, imagePoints1, imagePoints2, intrinsics1, intrinsics2, imageSize, flags, termCrit, out interCameraExtrinsics, out foundamentalMatrix, out essentialMatrix);

					Extrinsics extrinsics = new Extrinsics(interCameraExtrinsics);
					FPinOutExtrinsics[0] = extrinsics;
					FPinOutTransform[0] = extrinsics.Matrix;

					FPinOutSuccess[0] = true;
					FStatus[0] = "OK";
				}
				catch (Exception e)  {
					FPinOutSuccess[0] = false;
					FStatus[0] = e.Message;
				}
			}

		}
	}
}
