using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace VVVV.CV.Nodes.Features.Criteria
{
    class ImageRegion : ICriteria
    {
        Vector2D Position;
        Vector2D Scale;

        public ImageRegion(Vector2D Position, Vector2D Scale)
        {
            this.Position = Position;
            this.Scale = Scale;
        }

        public override bool Accept(FeatureSet FeatureSet, int Index)
        {
            double x = (double) FeatureSet.KeyPoints[Index].Point.X;
            double y = (double) FeatureSet.KeyPoints[Index].Point.Y;

            return (x > Position.x - Scale.x * 0.5 && x < Position.x + Scale.x * 0.5 && y > Position.y - Scale.y * 0.5 && y < Position.y + Scale.y * 0.5);
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "ImageRegion", Category = "CV.Features", Help = "Filter features based on a position boundary in the image", Tags = "criteria")]
    #endregion PluginInfo
    public class ImageRegionNode : IPluginEvaluate
    {
        [Input("Position", DefaultValues = new double[] { 512, 512 })]
        IDiffSpread<Vector2D> FPosition;

        [Input("Scale", DefaultValues = new double[] { 1024, 1024 })]
        IDiffSpread<Vector2D> FScale;

        [Output("Criteria")]
        ISpread<ICriteria> FCriteria;

        public void Evaluate(int SpreadMax)
        {
            if (FPosition.IsChanged || FScale.IsChanged)
            {
                FCriteria.SliceCount = SpreadMax;
                for (int i = 0; i < SpreadMax; i++)
                {
                    FCriteria[i] = new ImageRegion(FPosition[i], FScale[i]);
                }
            }
        }
    }
}
