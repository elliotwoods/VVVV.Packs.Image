using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.OpenCV
{
    public class FilterInstanceAttribute : Attribute
    {
        public string Name { get; set; }

        public FilterInstanceAttribute(string name)
        {
            Name = name;
        }
    }
}
