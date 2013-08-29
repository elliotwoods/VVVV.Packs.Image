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

namespace VVVV.Nodes.OpenCV.Calibration
{
	public enum TCoordinateSystem { VVVV, OpenCV };

	#region PluginInfo
	[PluginInfo(Name = "CalibrateCamera", Category = "OpenCV", Help = "Finds intrinsics for a single camera", Tags = "", AutoEvaluate=true)]
	#endregion PluginInfo
	public class CalibrateCameraNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Image Points")]
		ISpread<Vector2D> FPinInImage;

		[Input("Object Points")]
		ISpread<Vector3D> FPinInObject;

		[Input("Resolution")]
		ISpread<Vector2D> FPinInSensorSize;

		[Input("Flags")]
		ISpread<CALIB_TYPE> FPinInFlags;

		[Input("Intrinsic Guess", IsSingle=true)]
		ISpread<Intrinsics> FPinInIntrinsics;

		[Input("Coordinates", IsSingle=true)]
		ISpread<TCoordinateSystem> FPinInCoordSystem;

		[Input("Do", IsBang=true, IsSingle=true)]
		ISpread<bool> FPinInDo;

		[Output("Intrinsics")]
		ISpread<Intrinsics> FPinOutIntrinsics;

		[Output("Extrinsics Per Board")]
		ISpread<Extrinsics> FPinOutExtrinsics;

		[Output("View per board")]
		ISpread<Matrix4x4> FPinOutView;

		[Output("Projection")]
		ISpread<Matrix4x4> FPinOutProjection;

		[Output("Reprojection Error")]
		ISpread<Double> FPinOutError;

		[Output("Success")]
		ISpread<bool> FPinOutSuccess;

		[Output("Status")]
		ISpread<string> FStatus;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
		public CalibrateCameraNode(IPluginHost host)
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
				bool useVVVVCoords = FPinInCoordSystem[0] == TCoordinateSystem.VVVV;

				if (nPointsPerImage == 0)
				{
					FStatus[0] = "Insufficient points";
					return;
				}
				int nImages = FPinInImage.SliceCount / nPointsPerImage;

				MCvPoint3D32f[][] objectPoints = new MCvPoint3D32f[nImages][];
				PointF[][] imagePoints = new PointF[nImages][];
				Size imageSize = new Size( (int) FPinInSensorSize[0].x, (int) FPinInSensorSize[0].y);
				CALIB_TYPE flags = new CALIB_TYPE();
				IntrinsicCameraParameters intrinsicParam = new IntrinsicCameraParameters();
				ExtrinsicCameraParameters[] extrinsicsPerView;
				GetFlags(out flags);

				if (flags.HasFlag(CALIB_TYPE.CV_CALIB_USE_INTRINSIC_GUESS))
				{
					if (FPinInIntrinsics[0] == null)
					{
						Matrix<double> mat = intrinsicParam.IntrinsicMatrix;
						mat[0, 0] = FPinInSensorSize[0].x / 2.0d;
						mat[1, 1] = FPinInSensorSize[0].y / 2.0d;
						mat[0, 2] = FPinInSensorSize[0].x / 2.0d;
						mat[1, 2] = FPinInSensorSize[0].y / 2.0d;
						mat[2, 2] = 1;
					}
					else
					{
						intrinsicParam = FPinInIntrinsics[0].intrinsics;
					}

				}

				imagePoints = MatrixUtils.ImagePoints(FPinInImage, nPointsPerImage);

				for (int i=0; i<nImages; i++)
				{
					objectPoints[i] = new MCvPoint3D32f[nPointsPerImage];

					for (int j=0; j<nPointsPerImage; j++)
					{
						objectPoints[i] = MatrixUtils.ObjectPoints(FPinInObject, useVVVVCoords);
					}
				}

				try
				{
					FPinOutError[0] = CameraCalibration.CalibrateCamera(objectPoints, imagePoints, imageSize, intrinsicParam, flags, out extrinsicsPerView);

					Intrinsics intrinsics = new Intrinsics(intrinsicParam, imageSize);
					FPinOutIntrinsics[0] = intrinsics;
					if (useVVVVCoords)
						FPinOutProjection[0] = intrinsics.Matrix;
					else
						FPinOutProjection[0] = intrinsics.Matrix;

					FPinOutExtrinsics.SliceCount = nImages;
					FPinOutView.SliceCount = nImages;
					for (int i = 0; i < nImages; i++)
					{
						Extrinsics extrinsics = new Extrinsics(extrinsicsPerView[i]);
						FPinOutExtrinsics[i] = extrinsics;

						if (useVVVVCoords)
							FPinOutView[i] = MatrixUtils.ConvertToVVVV(extrinsics.Matrix);
						else
							FPinOutView[i] = extrinsics.Matrix;
					}

					FPinOutSuccess[0] = true;
					FStatus[0] = "OK";
				}
				catch (Exception e)  {
					FPinOutSuccess[0] = false;
					FStatus[0] = e.Message;
				}
			}

		}

		private void GetFlags(out CALIB_TYPE flags)
		{
			flags = 0;
			foreach (var flag in FPinInFlags)
				flags |= flag;
		}
	}
}
