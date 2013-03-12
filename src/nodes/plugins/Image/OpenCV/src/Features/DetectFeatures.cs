﻿#region usings
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
                this.FeaturesSet.OnUpdate();
            }
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

        protected override void Update(int instanceCount, bool spreadChanged)
        {
            if (FInHessianThreshold.IsChanged || spreadChanged)
            {
                for (int i = 0; i < instanceCount; i++)
                {
                    FProcessor[i].HessianThreshold = FInHessianThreshold[i];
                }
            }

            if (spreadChanged)
            {
                FOutput.SliceCount = instanceCount;

                for (int i = 0; i < instanceCount; i++)
                {
                    FOutput[i] = FProcessor[i].FeaturesSet;
                }
            }
        }
    }
}
