using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.CV.Core
{
    public class FilterInstanceAttribute : Attribute
    {
		public string Name { get; set; }
		public string Version { get; set; }
		public string Help { get; set; }
		public string Author { get; set; }
		public string Credits { get; set; }
		public string Tags { get; set; }

        public FilterInstanceAttribute(string name)
        {
            Name = name;
        }
    }
}
