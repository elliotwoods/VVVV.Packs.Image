using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using VVVV.Utils.VMath;

namespace VVVV.Nodes.OpenCV
{
	public class Extrinsics
	{
		public ExtrinsicCameraParameters extrinsics;

		public Extrinsics(ExtrinsicCameraParameters extrinsics)
		{
			this.extrinsics = extrinsics;
		}

		public Matrix4x4 Matrix
		{
			get
			{
				Matrix<double> t = extrinsics.ExtrinsicMatrix;

				Matrix4x4 m = new Matrix4x4();
				for (int x = 0; x < 3; x++)
					for (int y = 0; y < 4; y++)
						m[y, x] = t[x, y];

				m[0, 3] = 0;
				m[1, 3] = 0;
				m[2, 3] = 0;
				m[3, 3] = 1;

				return m;
			}
		}

	}
}
