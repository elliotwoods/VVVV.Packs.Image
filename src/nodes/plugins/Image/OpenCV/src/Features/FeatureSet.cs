using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV.Features2D;
using Emgu.CV.Util;
using Emgu.CV;

namespace VVVV.Nodes.OpenCV.Features
{
    /// <summary>
    /// Describes a SURF feature set for 
    /// </summary>
    public class FeatureSet
    {
        public Object Lock = new Object();
        public bool Allocated = false;
        public VectorOfKeyPoint KeyPoints;
        public Matrix<float> Descriptors;
    }
}
