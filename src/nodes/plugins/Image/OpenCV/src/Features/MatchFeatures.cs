using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using Emgu.CV.Features2D;
using Emgu.CV;
using VVVV.Utils.VMath;
using System.Diagnostics;

namespace VVVV.CV.Nodes.Features
{
    #region PluginInfo
    [PluginInfo(Name = "MatchFeatures", Category = "CV.Features", Help = "Find correspondeces between a pair of FeatureSet's.", Tags = "")]
    #endregion PluginInfo
    public class MatchFeatures : IPluginEvaluate
    {
        [Input("Input 1")]
        ISpread<FeatureSet> FInput1;

        [Input("Input 2")]
        ISpread<FeatureSet> FInput2;

        [Input("Uniqueness Threshold", DefaultValue = 0.8)]
        ISpread<double> FUniqueness;

        [Input("Do", IsBang=true)]
        ISpread<bool> FDo;

        [Output("Status")]
        ISpread<string> FStatus;

        [Output("Positions 1")]
        ISpread<ISpread<Vector2D>> FOutPositions1;

        [Output("Positions 2")]
        ISpread<ISpread<Vector2D>> FOutPositions2;

        public void Evaluate(int SpreadMax)
        {
            FStatus.SliceCount = SpreadMax;
            FOutPositions1.SliceCount = SpreadMax;
            FOutPositions2.SliceCount = SpreadMax;

            for (int i = 0; i < SpreadMax; i++)
            {
                if (!FDo[i])
                    continue;

                var input1 = FInput1[i];
                var input2 = FInput2[i];

                if (input1 == null || input2 == null)
                    continue;
                if (!input1.Allocated || !input2.Allocated)
                    continue;

                Matrix<byte> mask;
                var matcher = new BruteForceMatcher<float>(DistanceType.L2);
                matcher.Add(input2.Descriptors);

                var indices = new Matrix<int>(input1.Descriptors.Rows, 2);
                using (Matrix<float> distance = new Matrix<float>(input1.Descriptors.Rows, 2))
                {
                    matcher.KnnMatch(input1.Descriptors, indices, distance, 2, null);
                    mask = new Matrix<byte>(distance.Rows, 1);
                    mask.SetValue(255);
                    Features2DToolbox.VoteForUniqueness(distance, FUniqueness[i], mask);
                }

                int nonZeroCount = CvInvoke.cvCountNonZero(mask);
                nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(input2.KeyPoints, input1.KeyPoints, indices, mask, 1.5, 20);

                var positions1 = FOutPositions1[i];
                var positions2 = FOutPositions2[i];

                positions1.SliceCount = 0;
                positions2.SliceCount = 0;

                for (int j = 0; j < mask.Rows; j++)
                {
                    if (mask[j, 0] != 0)
                    {
                        var index2 = indices[j, 0];
                        var point1 = input1.KeyPoints[j];
                        var point2 = input2.KeyPoints[index2];

                        positions1.Add(new Vector2D(point1.Point.X, point1.Point.Y));
                        positions2.Add(new Vector2D(point2.Point.X, point2.Point.Y));
                    }
                }
            }
        }
    }
}
