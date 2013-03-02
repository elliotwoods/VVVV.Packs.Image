using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.Nodes.OpenCV.Features;

namespace VVVV.Nodes.OpenCV.Features.Criteria
{
    public abstract class ICriteria
    {
        abstract public bool Accept(FeatureSet FeatureSet, int Index);
        
        public event EventHandler Update;
        public void OnUpdate()
        {
            if (Update == null)
                return;
            Update(this, EventArgs.Empty);
        }
    }
}
