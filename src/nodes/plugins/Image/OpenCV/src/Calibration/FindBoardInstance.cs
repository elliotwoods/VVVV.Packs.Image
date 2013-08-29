using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Drawing;
using VVVV.Utils.VMath;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.OpenCV
{
	public class FindBoardInstance : IDestinationInstance, IDisposable
	{
		#region constants
		readonly Vector2D CMinimumSourceXY = new Vector2D(0, 0);
		readonly Vector2D CMinimumDestXY = new Vector2D(-1, 1);
		readonly Vector2D CMaximumDestXY = new Vector2D(1, -1);
		#endregion

		Size BoardSize = new Size(9, 6);
		Spread<Vector2D> FFoundPoints = new Spread<Vector2D>(0);
		Object FFoundPointsLock = new Object();

		public bool TestAtLowResolution = false;
		public bool Enabled = true;
		public bool SearchSuccessful = false;

		CVImage FGrayscale = new CVImage();
		CVImage FLowResolution = new CVImage();

		public void SetSize(int x, int y)
		{
			BoardSize.Width = x;
			BoardSize.Height = y;
		}

		public Spread<Vector2D> GetFoundCorners()
		{
			lock (FFoundPointsLock)
				return (Spread<Vector2D>)FFoundPoints.Clone();
		}

		public override void Allocate()
		{
			FGrayscale.Initialise(FInput.ImageAttributes.Size, TColorFormat.L8);
		}

		override public void Process()
		{
			if (!Enabled)
				return;

			FInput.Image.GetImage(TColorFormat.L8, FGrayscale);

			Size SizeNow = BoardSize;

			bool isOk = true;
			if (TestAtLowResolution)
			{
				if (!FLowResolution.Allocated) {
					FLowResolution.Initialise(new Size(1024, 1024), TColorFormat.L8);
				}
				CvInvoke.cvResize(FGrayscale.CvMat, FLowResolution.CvMat, INTER.CV_INTER_LINEAR);
				isOk = (CameraCalibration.FindChessboardCorners(FLowResolution.GetImage() as Image<Gray, byte>, SizeNow, CALIB_CB_TYPE.ADAPTIVE_THRESH) != null);
			}

			PointF[] points = null;
			if (isOk)
			{
				points = CameraCalibration.FindChessboardCorners(FGrayscale.GetImage() as Image<Gray, byte>, SizeNow, CALIB_CB_TYPE.ADAPTIVE_THRESH);
			}
			
			lock (FFoundPointsLock)
			{
				if (points == null)
					FFoundPoints.SliceCount = 0;
				else
				{
					SearchSuccessful = true;
					FFoundPoints.SliceCount = SizeNow.Width * SizeNow.Height;
					for (int i = 0; i < FFoundPoints.SliceCount; i++)
					{
						FFoundPoints[i] = new Vector2D(points[i].X, points[i].Y);
					}
				}
			}

		}

		void IDisposable.Dispose()
		{
			FGrayscale.Dispose();
			FLowResolution.Dispose();
		}
	}
}