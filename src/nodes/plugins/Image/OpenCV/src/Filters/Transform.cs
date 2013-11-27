#region using
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.NonGeneric;
using VVVV.Utils.VMath;
using System;
using VVVV.Utils.VColor;
using System.Linq;

#endregion

namespace VVVV.Nodes.OpenCV
{
    public class RotateContinuousInstance : IFilterInstance
    {
		Matrix4x4 FTrasform = new Matrix4x4();
		Object FTransformLock = new Object();
		public Matrix4x4 Transform
		{
			set
			{
				lock (FTransformLock)
				{
					FTrasform = value;
				}
			}
		}

		public bool UseCenter = false;

        public override void Allocate()
        {
            FOutput.Image.Initialise(FInput.Image.ImageAttributes);
        }

        public override void Process()
        {
			var matrix = new Emgu.CV.Matrix<double>(2, 3);

			Matrix4x4 transform = new Matrix4x4();
			lock (FTransformLock)
			{
				//copy the transform out
				for (int i = 0; i < 16; i++)
				{
					transform[i] = FTrasform[i];
				}
			}

			if (UseCenter)
			{
				double halfWidth = FInput.ImageAttributes.Width / 2;
				double halfHeight = FInput.ImageAttributes.Height / 2;

				transform = VMath.Translate(-halfWidth, -halfHeight, 0) * transform * VMath.Translate(halfWidth, halfHeight, 0);
			}

			matrix[0, 0] = transform[0, 0];
			matrix[1, 0] = transform[0, 1];
			matrix[0, 1] = transform[1, 0];
			matrix[1, 1] = transform[1, 1];
			matrix[0, 2] = transform[3, 0];
			matrix[1, 2] = transform[3, 1];

            if (FInput.LockForReading())
            {
                try
                {
                    CvInvoke.cvWarpAffine(FInput.CvMat, FOutput.CvMat, matrix.Ptr, (int)Emgu.CV.CvEnum.WARP.CV_WARP_FILL_OUTLIERS, new MCvScalar(0, 0, 0));
                }
                finally
                {
                    FInput.ReleaseForReading();
                }
            }

            FOutput.Send();
        }

    }

    #region PluginInfo
    [PluginInfo(Name = "Transform", Category = "CV", Version = "Filter", Help = "Apply an affine transformation to an image (e.g. translate, rotate, scale, skew).", Author = "elliotwoods")]
    #endregion PluginInfo
    public class RotateContinuousNode : IFilterNode<RotateContinuousInstance>
    {
        [Input("Transform")]
        IDiffSpread<Matrix4x4> FInTransform;

		[Input("Apply To Image Center")]
		IDiffSpread<bool> FInUseCenter;

        protected override void Update(int InstanceCount, bool SpreadChanged)
        {
			if (FInTransform.IsChanged || SpreadChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].Transform = FInTransform[i];
					FProcessor[i].FlagForProcess();
				}
			}

			if (FInUseCenter.IsChanged || SpreadChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].UseCenter = FInUseCenter[i];
					FProcessor[i].FlagForProcess();
				}
			}
        }
    }
}
