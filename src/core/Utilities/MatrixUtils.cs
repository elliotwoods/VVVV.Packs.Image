using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using System.Drawing;
using Emgu.CV.Structure;

namespace VVVV.CV.Core
{
	public class MatrixUtils
	{
		public static PointF[] ImagePoints(ISpread<Vector2D> input)
		{
			PointF[] imagePoints = new PointF[input.SliceCount];

			for (int i = 0; i < input.SliceCount; i++)
			{
				imagePoints[i].X = (float)input[i].x;
				imagePoints[i].Y = (float)input[i].y;
			}

			return imagePoints;
		}

		public static PointF[][] ImagePoints(ISpread<Vector2D> input, int pointsPerImage)
		{
			int images = input.SliceCount / pointsPerImage;

			PointF[][] imagePoints = new PointF[images][];

			for (int i = 0; i < images; i++)
			{
				imagePoints[i] = new PointF[pointsPerImage];
				for (int j = 0; j < pointsPerImage; j++)
				{
					imagePoints[i][j].X = (float)input[i * pointsPerImage + j].x;
					imagePoints[i][j].Y = (float)input[i * pointsPerImage + j].y;
				}
			}

			return imagePoints;
		}

		public static MCvPoint3D32f[] ObjectPoints(ISpread<Vector3D> input, bool toVVVV)
		{
			MCvPoint3D32f[] objectPoints = new MCvPoint3D32f[input.SliceCount];

			for (int i = 0; i < input.SliceCount; i++)
			{
				objectPoints[i].x = (float)input[i].x;
				objectPoints[i].y = toVVVV ? - (float)input[i].y : (float) input[i].y;
				objectPoints[i].z = toVVVV ? - (float)input[i].z : (float) input[i].z;
			}

			return objectPoints;
		}

		public static Matrix4x4 ConvertToVVVV(Matrix4x4 OpenCVMatrix)
		{
			return VMath.Scale(1, -1, -1) * OpenCVMatrix * VMath.Scale(1, -1, -1);
		}
	}
}
