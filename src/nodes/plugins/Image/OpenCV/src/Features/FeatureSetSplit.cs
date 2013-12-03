using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace VVVV.CV.Nodes.Features
{
    #region PluginInfo
    [PluginInfo(Name = "FeatureSet", Category = "CV.Features", Version = "Split", Help = "Output 2D positions of feature points", Tags = "")]
    #endregion PluginInfo
    public class FeatureSetSplit : IPluginEvaluate
    {
        [Input("Input")]
        IDiffSpread<FeatureSet> FInput;

        [Output("Position")]
        ISpread<ISpread<Vector2D>> FPosition;

        bool FUpdate = false;
        List<FeatureSet> FFeatureSets = new List<FeatureSet>();

        public void Evaluate(int SpreadMax)
        {
            if (FInput.IsChanged)
            {
                foreach (var set in FFeatureSets)
                {
                    if (set != null)
                    {
                        set.Update -= new EventHandler(update);
                    }
                }
                
                FFeatureSets.Clear();

                foreach (var set in FInput)
                {
                    if (set != null)
                    {
                        set.Update += new EventHandler(update);
                        FFeatureSets.Add(set);
                    }
                }

                FUpdate = true;
            }

            if (FUpdate)
            {
                FUpdate = false;

                FPosition.SliceCount = FInput.SliceCount;
                for (int i = 0; i < FInput.SliceCount; i++)
                {
                    var input = FInput[i];
                    var output = FPosition[i];

                    if (input != null && input.Allocated)
                    {
                        var count = input.Size;

                        output.SliceCount = count;

                        for (int j = 0; j < count; j++)
                        {
                            var point = input.KeyPoints[j];
                            FPosition[i][j] = new Vector2D(point.Point.X, point.Point.Y);
                        }
                    }
                    else
                    {
                        output.SliceCount = 0;
                    }
                }
            }
        }

        void update(object sender, EventArgs e)
        {
            FUpdate = true;
        }
    }
}
