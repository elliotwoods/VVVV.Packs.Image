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

namespace VVVV.CV.Nodes
{

	#region PluginInfo
	[PluginInfo(Name = "Intrinsics", Category = "CV.Transform", Version = "Join", Help = "Join intrinsics params", Tags = "")]
	#endregion PluginInfo
	public class IntrinsicsJoinNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins

        [Input("Sensor Size")]
        ISpread<Vector2D> FPinInSensorSize;

        [Input("VVVV Projection Transform")]
        ISpread<Matrix4x4> FPinInCameraTransform;

        [Input("Distortion Coefficients")]
        ISpread<ISpread<Double>> FPinInDistiortonCoefficients;

		[Output("Intrinsics")]
        ISpread<Intrinsics> FPinOutIntrinsics;



		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
        public IntrinsicsJoinNode(IPluginHost host)
		{

		}

		public void Dispose()
		{
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			//if (!FPinInIntrinsics.IsChanged)
			//    return;

            if (FPinInCameraTransform[0] == null)
			{
                FPinOutIntrinsics.SliceCount = 0;
			}
			else
			{
                FPinOutIntrinsics.SliceCount = SpreadMax;

				for (int i = 0; i < SpreadMax; i++)
				{
                    IntrinsicCameraParameters cp = new IntrinsicCameraParameters();
                    // Inspiration / details from http://strawlab.org/2011/11/05/augmented-reality-with-OpenGL/ - See Intrinsics.cs too
                    cp.IntrinsicMatrix[0, 0] = FPinInCameraTransform[i][0, 0] * FPinInSensorSize[i].x / 2;// =L15*P8/2
                    cp.IntrinsicMatrix[1, 1] = FPinInCameraTransform[i][1, 1] * FPinInSensorSize[i].y / 2;// =L15*P8/2
                    cp.IntrinsicMatrix[0, 2] = (FPinInCameraTransform[i][2, 0] + 1)* FPinInSensorSize[i].x / 2;// =L15*P8/2
                    cp.IntrinsicMatrix[1, 2] = (FPinInCameraTransform[i][2, 1] +1) * FPinInSensorSize[i].y / 2;// =L15*P8/2
                    cp.IntrinsicMatrix[2, 2] = 1;
                    FPinOutIntrinsics[i] = new Intrinsics(cp, new Size((int)FPinInSensorSize[i].x, (int)FPinInSensorSize[i].y));
				}
			}
		}
	}
}
