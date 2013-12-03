using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using System.Drawing;
using Emgu.CV;

namespace VVVV.CV.Nodes
{
    #region PluginInfo
    [PluginInfo(Name = "Homography", Category = "2d", Help = "Solve homography (allows for fitting > 4 points).", Tags = "opencv")]
    #endregion PluginInfo
    public class Homography : IPluginEvaluate
    {
        [Input("Transform In")]
        IDiffSpread<Matrix4x4> FTransformIn;

        [Input("Source")]
        IDiffSpread<ISpread<Vector2D>> FSource;

        [Input("Target")]
        IDiffSpread<ISpread<Vector2D>> FTarget;

        [Output("Transform Out")]
        ISpread<Matrix4x4> FTransformOut;

        [Output("Status")]
        ISpread<string> FStatus;

        public void Evaluate(int SpreadMax)
        {
            if (FSource.IsChanged || FTarget.IsChanged)
            {
                SpreadMax = Math.Max(FSource.SliceCount, FTarget.SliceCount);
                FTransformOut.SliceCount = SpreadMax;
                FStatus.SliceCount = SpreadMax;

                for (int slice = 0; slice < SpreadMax; slice++)
                {
                    try
                    {
                        var source = FSource[slice];
                        var target = FTarget[slice];

                        if (source.SliceCount < 4 || target.SliceCount < 4)
                        {
                            throw(new Exception("You need at least 4 source and 4 target points"));
                        }

                        int sliceSpreadMax = Math.Max(source.SliceCount, target.SliceCount);
                        PointF[] sourcePoints = new PointF[sliceSpreadMax];
                        PointF[] targetPoints = new PointF[sliceSpreadMax];

                        for (int i = 0; i < sliceSpreadMax; i++)
                        {
                            sourcePoints[i].X = (float)source[i].x;
                            sourcePoints[i].Y = (float)source[i].y;

                            targetPoints[i].X = (float)target[i].x;
                            targetPoints[i].Y = (float)target[i].y;
                        }

                        var matrix = CameraCalibration.FindHomography(sourcePoints, targetPoints, Emgu.CV.CvEnum.HOMOGRAPHY_METHOD.LMEDS, 0.0);

                        FTransformOut[slice] = new Matrix4x4(matrix[0, 0], matrix[1, 0], 0.0, matrix[2, 0],
                                                             matrix[0, 1], matrix[1, 1], 0.0, matrix[2, 1],
                                                             0.0, 0.0, 1.0, 0.0,
                                                             matrix[0, 2], matrix[1, 2], 0.0, matrix[2, 2]);

                        FStatus[slice] = "OK";
                    }
                    catch (Exception e)
                    {
                        FTransformOut[slice] = VMath.IdentityMatrix;
                        FStatus[slice] = e.Message;
                    }
                }
            }
        }
    }
}
