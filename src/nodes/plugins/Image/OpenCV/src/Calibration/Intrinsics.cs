using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.Utils.VMath;
using Emgu.CV;
using System.Drawing;

namespace VVVV.CV.Nodes
{
	[Serializable()]
	public class Intrinsics
	{
		public IntrinsicCameraParameters intrinsics {get; private set;}
		public Size SensorSize;

		public Intrinsics(IntrinsicCameraParameters intrinsics, Size SensorSize)
		{
			this.intrinsics = intrinsics;
			this.SensorSize = SensorSize;
		}

		public Matrix4x4 Matrix
		{
			get
			{
				Matrix4x4 m = new Matrix4x4();

				m[0, 0] = intrinsics.IntrinsicMatrix[0, 0];
				m[1, 1] = intrinsics.IntrinsicMatrix[1, 1];
				m[2, 0] = intrinsics.IntrinsicMatrix[0, 2];
				m[2, 1] = intrinsics.IntrinsicMatrix[1, 2];
				m[2, 2] = 1;
				m[2, 3] = 1;
				m[3, 3] = 0;

				return m;
			}
		}

		public Matrix4x4 NormalisedMatrix
		{
			get
			{
				double width = (double) this.SensorSize.Width;
				double height = (double) this.SensorSize.Height;

				Matrix4x4 m = this.Matrix;
				m[0, 0] /= width;
				m[0, 0] *= 2.0;

				m[1, 1] /= height;
				m[1, 1] *= 2.0;

				m[2, 0] /= width;
				m[2, 0] *= 2.0;
				m[2, 0] = m[2, 0] - 1.0;

				m[2, 1] /= height;
				m[2, 1] *= 2.0;
				m[2, 1] = 1.0 - m[2, 1];


				return m;
			}
		}
	}
}
