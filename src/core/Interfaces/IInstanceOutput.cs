using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.OpenCV
{
	public interface IInstanceOutput
	{
		void SetOutput(CVImageOutput output);
	}
}
