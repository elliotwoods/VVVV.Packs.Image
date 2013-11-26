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
        public double Rotation = 0.0;
        public double Scale = 1.0;
        public Vector2D Center = new Vector2D(0.5, 0.5);

        public override void Allocate()
        {
            FOutput.Image.Initialise(FInput.Image.ImageAttributes);
        }

        public override void Process()
        {
            Scale = VMath.Clamp(Scale, 0.0, Double.MaxValue);
            var CenterOpenCV = new PointF();
            CenterOpenCV.X = FInput.Image.Size.Width * (float)(Center.x + 1.0f) / 2.0f;
            CenterOpenCV.Y = FInput.Image.Size.Height * (float)(1.0f - Center.y) / 2.0f;

            var rotation = new Emgu.CV.RotationMatrix2D<double>(CenterOpenCV, Rotation * 360.0, Scale);
            if (FInput.LockForReading())
            {
                try
                {
                    CvInvoke.cvWarpAffine(FInput.CvMat, FOutput.CvMat, rotation.Ptr, (int)Emgu.CV.CvEnum.WARP.CV_WARP_FILL_OUTLIERS, new MCvScalar(0, 0, 0));
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
    [PluginInfo(Name = "Rotate", Category = "OpenCV", Version = "Continuous", Help = "Rotate an image using normalised rotation.", Author = "elliotwoods", Credits = "", Tags = "")]
    #endregion PluginInfo
    public class RotateContinuousNode : IFilterNode<RotateContinuousInstance>
    {
        [Input("Rotation")]
        IDiffSpread<double> FRotation;

        [Input("Scale", DefaultValue = 1.0)]
        IDiffSpread<double> FScale;

        [Input("Center", DefaultValue = 0.5)]
        IDiffSpread<Vector2D> FCenter;

        protected override void Update(int InstanceCount, bool SpreadChanged)
        {
			if (FRotation.IsChanged || SpreadChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].Rotation = FRotation[i];
					FProcessor[i].FlagForProcess();
				}
			}

			if (FScale.IsChanged || SpreadChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].Scale = FScale[i];
					FProcessor[i].FlagForProcess();
				}
			}

			if (FCenter.IsChanged || SpreadChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].Center = FCenter[i];
					FProcessor[i].FlagForProcess();
				}
			}
        }
    }
}
