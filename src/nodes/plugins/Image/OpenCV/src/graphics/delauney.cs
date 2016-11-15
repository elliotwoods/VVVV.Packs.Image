#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;

#endregion usings

namespace VVVV.Nodes
{
    #region PluginInfo
    [PluginInfo(Name = "Delauney", Category = "Value", Author = "sebl", Help = "Yet another Delauney plugin, but quite fast", Tags = "", Credits = "Woei")]
    #endregion PluginInfo
    public class ValueDelauneyNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("Input")]
        IDiffSpread<Vector2D> FInput;

        [Output("Triangles")]
        ISpread<ISpread<Vector2D>> FDelauneyTriangles;

        //[Import()]
        //ILogger FLogger;
        #endregion fields & pins

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {

            if (FInput.IsChanged)
            {

                PointF[] pts = new PointF[FInput.SliceCount];
                for (int i = 0; i < FInput.SliceCount; i++)
                    pts[i] = new PointF((float)FInput[i].x, (float)FInput[i].y);

                //Triangle2DF[] delaunayTriangles;
                Triangle2DF[] delaunayTriangles;

                using (PlanarSubdivision subdivision = new PlanarSubdivision(pts))
                {
                    //Obtain the delaunay's triangulation from the set of points;
                    delaunayTriangles = subdivision.GetDelaunayTriangles();

                }

                FDelauneyTriangles.SliceCount = delaunayTriangles.Length;

                for (int d = 0; d < delaunayTriangles.Length; d++)
                {
                    FDelauneyTriangles[d].SliceCount = 3;

                    FDelauneyTriangles[d][0] = new Vector2D(delaunayTriangles[d].V0.X, delaunayTriangles[d].V0.Y);
                    FDelauneyTriangles[d][1] = new Vector2D(delaunayTriangles[d].V1.X, delaunayTriangles[d].V1.Y);
                    FDelauneyTriangles[d][2] = new Vector2D(delaunayTriangles[d].V2.X, delaunayTriangles[d].V2.Y);
                }


            }
        }


    }
}
