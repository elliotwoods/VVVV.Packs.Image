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
	[PluginInfo(Name = "Delauney", Category = "CV.Contour", Help = "Convert contour perimeter to triangles", Tags = "")]
	#endregion PluginInfo
	public class ContourDelauneyNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Input")]
		ISpread<ContourPerimeter> FPinInInput;

		[Input("Apply")]
		ISpread<bool> FPinInApply;

		[Output("Vertex position")]
		ISpread<ISpread<Vector2D>> FPinOutPosition;

		[Output("Triangle area")]
		ISpread<ISpread<double>> FPinOutArea;

		[Output("Triangle centroid")]
		ISpread<ISpread<Vector2D>> FPinOutCentroid;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		#region thread
		Thread FThread;

		Object FLockPoints = new Object();
		Spread<PointF[]> FPoints = new Spread<PointF[]>(0);

		Object FLockTriangles = new Object();
		Spread<Triangle2DF[]> FTriangles = new Spread<Triangle2DF[]>(0);

		bool FApply = false;
		bool FResults = false;
		#endregion

		[ImportingConstructor]
		public ContourDelauneyNode(IPluginHost host)
		{
			FThread = new Thread(ThreadedFunction);
			FThread.Start();
		}

		public void Dispose()
		{
			if (FThread != null)
			{
				FThread.Abort();
				FThread = null;
			}
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FPinInInput[0] == null)
			{
				FPinOutPosition.SliceCount = 0;
				FPinOutArea.SliceCount = 0;
				FPinOutCentroid.SliceCount = 0;
				return;
			}

			if (FPinInApply[0])
			{
				FApply = true;

				lock (FLockPoints)
				{
					FPoints.SliceCount = SpreadMax;

					for (int i = 0; i < SpreadMax; i++)
						FPoints[i] = FPinInInput[i].Points.Clone() as PointF[];
				}
			}

			if (FResults)
			{
				lock (FLockTriangles)
				{
					FPinOutPosition.SliceCount = FTriangles.SliceCount;
					FPinOutArea.SliceCount = FTriangles.SliceCount;
					FPinOutCentroid.SliceCount = FTriangles.SliceCount;
					
					for (int i = 0; i < FTriangles.SliceCount; i++)
					{
						FPinOutPosition[i].SliceCount = FTriangles[i].Length * 3;
						FPinOutArea[i].SliceCount = FTriangles[i].Length;
						FPinOutCentroid[i].SliceCount = FTriangles[i].Length;

						for (int j = 0; j < FTriangles[i].Length; j++)
						{
							FPinOutPosition[i][j * 3 + 0] = new Vector2D(FTriangles[i][j].V0.X, FTriangles[i][j].V0.Y);
							FPinOutPosition[i][j * 3 + 1] = new Vector2D(FTriangles[i][j].V1.X, FTriangles[i][j].V1.Y);
							FPinOutPosition[i][j * 3 + 2] = new Vector2D(FTriangles[i][j].V2.X, FTriangles[i][j].V2.Y);

							FPinOutArea[i][j] = FTriangles[i][j].Area;
							FPinOutCentroid[i][j] = new Vector2D(FTriangles[i][j].Centeroid.X, FTriangles[i][j].Centeroid.Y);
						}
					}
				}

			}
		}

		public void ThreadedFunction()
		{
			while (true)
			{
				while (!FApply)
					Thread.Sleep(5);

				int SliceCount;
				Spread<PointF[]> points;
				Spread<Triangle2DF[]> triangles;

				lock (FLockPoints)
				{
					SliceCount = FPoints.SliceCount;
					points = new Spread<PointF[]>(SliceCount);

					for (int i = 0; i < SliceCount; i++)
					{
						points[i] = new PointF[FPoints[i].Length];
						Array.Copy(FPoints[i], points[i], FPoints[i].Length);
					}
	
				}

				triangles = new Spread<Triangle2DF[]>(SliceCount);

				for (int i = 0; i < SliceCount; i++)
				{
					PlanarSubdivision subdivision = new PlanarSubdivision(points[i] as PointF[]);
					triangles[i] = subdivision.GetDelaunayTriangles(false);
				}

				lock (FTriangles)
				{
					FTriangles.SliceCount = SliceCount;

					Triangle2DF t;
					for (int i = 0; i < SliceCount; i++)
					{
						FTriangles[i] = new Triangle2DF[triangles[i].Length];
						for (int j = 0; j < triangles[i].Length; j++ )
						{
							t = triangles[i][j];
							FTriangles[i][j] = new Triangle2DF(t.V0, t.V1, t.V2);
						}
					}
				}

				FResults = true;
			}
		}

	}
}
