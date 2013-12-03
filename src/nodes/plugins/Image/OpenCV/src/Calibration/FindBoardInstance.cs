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
using VVVV.CV.Core;

namespace VVVV.CV.Nodes
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
		public bool SearchFailed = false;

		CVImage FGrayscale = new CVImage();
		CVImage FLowResolution = new CVImage();
		CVImage FCropped = new CVImage();

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
		}

		override public void Process()
		{
			if (!Enabled)
				return;

			if (!FGrayscale.Allocated || FGrayscale.Size != FInput.ImageAttributes.Size)
			{
				FGrayscale.Initialise(FInput.ImageAttributes.Size, TColorFormat.L8);
			}

			FInput.Image.GetImage(TColorFormat.L8, FGrayscale);

			Size SizeNow = BoardSize;

			PointF[] points = null;

			if (TestAtLowResolution)
			{
				if (!FLowResolution.Allocated) {
					FLowResolution.Initialise(new Size(1024, 1024), TColorFormat.L8);
				}
				CvInvoke.cvResize(FGrayscale.CvMat, FLowResolution.CvMat, INTER.CV_INTER_LINEAR);
				var lowResPoints = CameraCalibration.FindChessboardCorners(FLowResolution.GetImage() as Image<Gray, byte>, SizeNow, CALIB_CB_TYPE.ADAPTIVE_THRESH);

				if (lowResPoints != null) {
					int minX = FGrayscale.Width;
					int minY = FGrayscale.Height;
					int maxX = 0;
					int maxY = 0;

					foreach(var point in lowResPoints)
					{
						if ((int)point.X > maxX)
							maxX = (int)point.X;

						if ((int)point.Y > maxY)
							maxY = (int)point.Y;

						if ((int)point.X < minX)
							minX = (int)point.X;

						if ((int)point.Y < minY)
							minY = (int)point.Y;
					}

					minX = minX * FGrayscale.Width / 1024;
					maxX = maxX * FGrayscale.Width / 1024;
					minY = minY * FGrayscale.Height / 1024;
					maxY = maxY * FGrayscale.Height / 1024;

					int boardResolutionMin = Math.Min(SizeNow.Width, SizeNow.Height);
					int strideX = (maxX - minX) / boardResolutionMin;
					int strideY = (maxY - minY) / boardResolutionMin;

					minX -= strideX * 2;
					maxX += strideX * 2;
					minY -= strideY * 2;
					maxY += strideY * 2;

					if (minX < 0)
					{
						minX = 0;
					}
					if (minY < 0)
					{
						minY = 0;
					}
					if (maxX > FGrayscale.Width - 1)
					{
						maxX = FGrayscale.Width - 1;
					}
					if (maxY > FGrayscale.Height - 1)
					{
						maxY = FGrayscale.Height - 1;
					}

					Rectangle rect = new Rectangle(minX, minY, maxX-minX, maxY-minY);

					CvInvoke.cvSetImageROI(FGrayscale.CvMat, rect);
					FCropped.Initialise(new Size(rect.Width, rect.Height), TColorFormat.L8);
					CvInvoke.cvCopy(FGrayscale.CvMat, FCropped.CvMat, IntPtr.Zero);
					CvInvoke.cvResetImageROI(FGrayscale.CvMat);

					points = CameraCalibration.FindChessboardCorners(FCropped.GetImage() as Image<Gray, byte>, SizeNow, CALIB_CB_TYPE.ADAPTIVE_THRESH);

					if (points != null)
					{
						for (int iPoint = 0; iPoint < points.Length; iPoint++)
						{
							points[iPoint].X += minX;
							points[iPoint].Y += minY;
						}
					}
				}
			} else {
				points = CameraCalibration.FindChessboardCorners(FGrayscale.GetImage() as Image<Gray, byte>, SizeNow, CALIB_CB_TYPE.ADAPTIVE_THRESH);
			}
			
			lock (FFoundPointsLock)
			{
				if (points == null)
				{
					SearchFailed = true;
					FFoundPoints.SliceCount = 0;
				}
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