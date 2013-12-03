using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using VVVV.Utils.VMath;

namespace VVVV.CV.Nodes
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

				var translate = VVVV.Utils.VMath.VMath.Translate(t[0, 3], t[1, 3], t[2, 3]);

				Matrix4x4 rotate = VVVV.Utils.VMath.VMath.IdentityMatrix;

				for (int x = 0; x < 3; x++)
					for (int y = 0; y < 3; y++)
						rotate[y, x] = t[x, y];

				return rotate * translate;
			}
		}

	}
}
