using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using System.Drawing;
using Emgu.CV;

namespace VVVV.Nodes.OpenCV
{
    #region PluginInfo
    [PluginInfo(Name = "Homography", Category = "OpenCV", Version = "Transform", Help = "Solve homography (allows for fitting > 4 points).", Tags = "")]
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

        [Output("Raw Matrix")]
        ISpread<ISpread<double>> FOutput;

        [Output("Status")]
        ISpread<string> FStatus;

        public void Evaluate(int SpreadMax)
        {
            if (FSource.IsChanged || FTarget.IsChanged)
            {
                SpreadMax = Math.Max(FSource.SliceCount, FTarget.SliceCount);
                FOutput.SliceCount = SpreadMax;
                FTransformOut.SliceCount = SpreadMax;
                FStatus.SliceCount = SpreadMax;

                for (int slice = 0; slice < SpreadMax; slice++)
                {
                    try
                    {
                        var source = FSource[slice];
                        var target = FTarget[slice];

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

                        var matrix = CameraCalibration.GetPerspectiveTransform(sourcePoints, targetPoints);

                        var output = FOutput[slice];
                        output.SliceCount = 9;
                        for (int i = 0; i < 3; i++)
                        {
                            for (int j = 0; j < 3; j++)
                            {
                                output[i + j * 3] = matrix[i, j];
                            }
                        }

                        FTransformOut[slice] = new Matrix4x4(matrix[0, 0], matrix[1, 0], 0.0, matrix[2, 0],
                                                             matrix[0, 1], matrix[1, 1], 0.0, matrix[2, 1],
                                                             0.0, 0.0, 1.0, 0.0,
                                                             matrix[0, 2], matrix[1, 2], 0.0, matrix[2, 2]);

                        FStatus[slice] = "OK";
                    }
                    catch (Exception e)
                    {
                        FStatus[slice] = e.Message;
                    }
                }
            }
        }
    }
}
