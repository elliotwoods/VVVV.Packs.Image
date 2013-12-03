using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV.Features2D;
using Emgu.CV.Util;
using Emgu.CV;
using Emgu.CV.Structure;
using VVVV.CV.Nodes.Features.Criteria;

namespace VVVV.CV.Nodes.Features
{
    /// <summary>
    /// Describes a SURF feature set for 
    /// </summary>
    public class FeatureSet
    {
        public FeatureSet()
        {
        }

        public FeatureSet(FeatureSet other)
        {
            this.KeyPoints = new VectorOfKeyPoint();
            this.KeyPoints.Push(other.KeyPoints.ToArray());

            this.Descriptors = other.Descriptors.Clone();

            this.Allocated = other.Allocated;
        }

        public FeatureSet(FeatureSet other, ICriteria criteria)
        {
            var copy = new FeatureSet(other);
            this.KeyPoints = copy.KeyPoints;
            this.Descriptors = copy.Descriptors;
            this.Allocated = copy.Allocated;

            if (criteria == null)
                return;

            var keypoints = new List<MKeyPoint>(this.KeyPoints.ToArray());
            var descriptors = new List<Matrix<float>>();

            int i = 0;
            while (i < keypoints.Count)
            {
                if (criteria.Accept(this, i))
                {
                    descriptors.Add(this.Descriptors.GetRow(i));
                    i++;
                }
                else
                {
                    keypoints.RemoveAt(i);
                }
            }

            this.KeyPoints.Clear();
            this.KeyPoints.Push(keypoints.ToArray());

            var newDescriptors = new Matrix<float>(descriptors.Count, this.Descriptors.Cols);
            for (int j = 0; j < descriptors.Count; j++)
            {
                for (int col =0; col<this.Descriptors.Cols; col++)
                {
                    this.Descriptors[j, col] = descriptors[j][0, col];
                }
            }

            this.Descriptors = newDescriptors;
        }

        public Object Lock = new Object();
        public bool Allocated = false;
        public VectorOfKeyPoint KeyPoints;
        public Matrix<float> Descriptors;
        public event EventHandler Update;
        public void OnUpdate()
        {
            if (Update == null)
                return;
            Update(this, EventArgs.Empty);
        }

        public int Size
        {
            get
            {
                return KeyPoints.Size;;
            }
        }
    }
}
