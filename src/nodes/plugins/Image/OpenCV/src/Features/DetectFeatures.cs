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
using Emgu.CV.Features2D;

#endregion usings

namespace VVVV.Nodes.OpenCV.Features
{
    public class DetectFeaturesInstance : IDestinationInstance
    {
        public FeatureSet FeaturesSet {get; private set;}
        public SURFDetector Detector = new SURFDetector(500, false);
        CVImage FGrayScale = new CVImage();
        bool FNewOutput = false;

        public DetectFeaturesInstance()
        {
            this.FeaturesSet = new FeatureSet();
        }

        public double HessianThreshold
        {
            set
            {
                this.Detector = new SURFDetector(value, false);
            }
        }

        public bool NewOutput
        {
            get
            {
                if (FNewOutput)
                {
                    FNewOutput = false;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public override void Allocate()
        {
            FGrayScale.Initialise(FInput.Image.Size, TColorFormat.L8);
        }

        public override void Process()
        {
            lock (this.FeaturesSet.Lock)
            {
                FInput.GetImage(FGrayScale);
                var gray = FGrayScale.GetImage() as Image<Gray, Byte>;
                this.FeaturesSet.KeyPoints = this.Detector.DetectKeyPointsRaw(gray, null);
                this.FeaturesSet.Descriptors = this.Detector.ComputeDescriptorsRaw(gray, null, this.FeaturesSet.KeyPoints);
                this.FeaturesSet.Allocated = true;
            }
            FNewOutput = true;
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "DetectFeatures", Category = "OpenCV", Version = "SURF", Help = "Find feature points in 2D image", Tags = "")]
    #endregion PluginInfo
    public class DetectFeaturesNode : IDestinationNode<DetectFeaturesInstance>
    {
        [Input("Hessian Threshold", MinValue=0, DefaultValue=500)]
        IDiffSpread<double> FInHessianThreshold;

        [Output("Output")]
        ISpread<FeatureSet> FOutput;
        
        [Output("Position")]
        ISpread<ISpread<Vector2D>> FOutPosition;

        protected override void Update(int InstanceCount, bool SpreadChanged)
        {
            if (FInHessianThreshold.IsChanged || SpreadChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                {
                    FProcessor[i].HessianThreshold = FInHessianThreshold[i];
                }
            }

            if (SpreadChanged)
            {
                FOutput.SliceCount = InstanceCount;
                FOutPosition.SliceCount = InstanceCount;

                for (int i = 0; i < InstanceCount; i++)
                {
                    FOutput[i] = FProcessor[i].FeaturesSet;
                }
            }

            for (int i = 0; i < InstanceCount; i++)
            {
                if (FProcessor[i].NewOutput)
                {
                    var positions = FProcessor[i].FeaturesSet.KeyPoints;
                    FOutPosition[i].SliceCount = positions.Size;

                    for (int j = 0; j < positions.Size; j++)
                    {
                        var point = positions[j];
                        FOutPosition[i][j] = new Vector2D(point.Point.X, point.Point.Y);
                    }
                }
            }
        }
    }
}
