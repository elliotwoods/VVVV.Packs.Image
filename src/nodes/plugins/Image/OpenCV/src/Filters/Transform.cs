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

        public override void Allocate()
        {
            FOutput.Image.Initialise(FInput.Image.ImageAttributes);
        }

        public override void Process()
        {
			var matrix = new Emgu.CV.Matrix<double>(2, 3);
			lock (FTransformLock)
			{
				matrix[0, 0] = FTrasform[0, 0];
				matrix[1, 0] = FTrasform[1, 0];
				matrix[0, 1] = FTrasform[0, 1];
				matrix[1, 1] = FTrasform[1, 1];
				matrix[0, 2] = FTrasform[0, 3];
				matrix[1, 2] = FTrasform[1, 3];
			}

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
        }
    }
}
