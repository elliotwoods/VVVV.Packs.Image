#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using System.Drawing;
using Emgu.CV;
#endregion usings

namespace VVVV.Nodes
{
    #region PluginInfo
    [PluginInfo(Name = "Voronoi", Category = "Value", Author="sebl", Help = "Yet another Voronoi plugin, but quite fast", Tags = "", Credits ="Woei")]
    #endregion PluginInfo
    public class ValueVoronoiNode : IPluginEvaluate
    {
        #region fields & pins
        [Input("Input")]
        IDiffSpread<Vector2D> FInput;

        [Output("Vertices")]
        ISpread<ISpread<Vector2D>> FVoronoiVertices;

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {

            if (FInput.IsChanged)
            {

                PointF[] pts = new PointF[FInput.SliceCount];
                for (int i = 0; i < FInput.SliceCount; i++)
                    pts[i] = new PointF((float)FInput[i].x, (float)FInput[i].y);

                VoronoiFacet[] voronoiFacets;

                using (PlanarSubdivision subdivision = new PlanarSubdivision(pts))
                {
                    voronoiFacets = subdivision.GetVoronoiFacets();
                }


                FVoronoiVertices.SliceCount = FInput.SliceCount;

                for (int f = 0; f < pts.Length; f++)
                {

                    for (int c = 0; c < voronoiFacets.Length; c++)
                    {
                        if (pts[f].Equals(voronoiFacets[c].Point))
                        {

                            FVoronoiVertices[f].SliceCount = voronoiFacets[c].Vertices.Length;
                            for (int v = 0; v < voronoiFacets[c].Vertices.Length; v++)
                            {
                                FVoronoiVertices[f][v] = new Vector2D(voronoiFacets[c].Vertices[v].X, voronoiFacets[c].Vertices[v].Y);


                            }
                        }
                    }
                }


            }
        }


    }
}
