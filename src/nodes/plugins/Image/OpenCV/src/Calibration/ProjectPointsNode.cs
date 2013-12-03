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
	[PluginInfo(Name = "ProjectPoints", Category = "CV.Transform", Help = "Apply extrinsics and intrinsics to a set of 3d points to get projected 2d points", Tags = "camera calibration")]
	#endregion PluginInfo
	public class ProjectPointsNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Input")]
		ISpread<Vector3D> FPinInInput;

		[Input("Intrinsics")]
		ISpread<IntrinsicCameraParameters> FPinInIntrinsics;

		[Input("Extrinsics")]
		ISpread<ExtrinsicCameraParameters> FPinInExtrinsics;

		[Output("Output")]
		ISpread<Vector2D> FPinOutOutput;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
		public ProjectPointsNode(IPluginHost host)
		{

		}

		public void Dispose()
		{

		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FPinOutOutput.SliceCount = FPinInInput.SliceCount;

			if (FPinInIntrinsics[0] == null || FPinInExtrinsics[0] == null)
				return;

			MCvPoint3D32f[] o = new MCvPoint3D32f[1];
			PointF[] im = new PointF[1];

			for (int i=0; i<FPinInInput.SliceCount; i++)
			{
				o[0].x = (float)FPinInInput[i].x;
				o[0].y = (float)FPinInInput[i].y;
				o[0].z = (float)FPinInInput[i].z;

				im = CameraCalibration.ProjectPoints(o, FPinInExtrinsics[0], FPinInIntrinsics[0]);
				FPinOutOutput[i] = new Vector2D((double)im[0].X, (double)im[0].Y);
			}
		}

	}
}
