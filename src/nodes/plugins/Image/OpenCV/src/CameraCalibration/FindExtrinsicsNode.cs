#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using ThreadState = System.Threading.ThreadState;
using System.Collections.Generic;

#endregion usings

namespace VVVV.Nodes.OpenCV
{

    #region PluginInfo
    [PluginInfo(Name = "FindExtrinsics", Category = "OpenCV", Help = "Find extrinsics of object", Tags = "")]
    #endregion PluginInfo
    public class FindExtrinsicsNode : IPluginEvaluate, IDisposable
    {
        #region fields & pins
        [Input("Object points")]
        IDiffSpread<ISpread<Vector3D>> FPinInObjectPoints;

        [Input("Image points")]
        IDiffSpread<ISpread<Vector3D>> FPinInImagePoints;

        [Input("Intrinsics")]
        IDiffSpread<IntrinsicCameraParameters> FPinInIntrinsics;

        [Output("Extrinsics")]
        ISpread<ExtrinsicCameraParameters> FPinOutExtrinsics;

        [Output("Status")]
        ISpread<string> FPinOutStatus;

        [Import]
        ILogger FLogger;

        #endregion fields & pins

        [ImportingConstructor]
        public FindExtrinsicsNode(IPluginHost host)
        {

        }

        public void Dispose()
        {

        }

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            FPinOutExtrinsics.SliceCount = SpreadMax;
            FPinOutStatus.SliceCount = SpreadMax;

            if (FPinInImagePoints.IsChanged || FPinInObjectPoints.IsChanged)
            {

                MCvPoint3D32f[] objectPoints;
                PointF[] imagePoints;
                for (int i = 0; i < SpreadMax; i++)
                {
                    if (FPinInObjectPoints[i].SliceCount != FPinInImagePoints.SliceCount)
                    {
                        FPinOutStatus[i] = "Number of object and image points does not match";
                        continue;
                    }

                    if (FPinInIntrinsics[i] == null)
                    {
                        FPinOutStatus[i] = "Waiting for intrinsics";
                        continue;
                    }

                    int nPoints = FPinInObjectPoints[i].SliceCount;

                    objectPoints = new MCvPoint3D32f[nPoints];
                    imagePoints = new PointF[nPoints];

                    ExtrinsicCameraParameters extrinsics = CameraCalibration.FindExtrinsicCameraParams2(objectPoints, imagePoints, FPinInIntrinsics[i]);

                    if (extrinsics == null)
                    {
                        FPinOutStatus[i] = "Something went wrong";
                        continue;
                    }

                    FPinOutExtrinsics[i] = extrinsics;
                    FPinOutStatus[i] = "ok";
                }
            }
        }

    }
}
