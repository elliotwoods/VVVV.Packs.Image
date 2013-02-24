using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using VVVV.Nodes.OpenCV;

namespace VVVV.Nodes.OpenCV.FreeImage
{
	public class LoadImageInstance : IStaticGeneratorInstance
	{
		string FFilename = "";
		public string Filename
		{
			set
			{
				FFilename = value;
				ReInitialise();
			}
		}

		public override void Initialise()
		{
			try
			{

			}
			catch (Exception e)
			{
				Status = e.Message;
			}
		}
	}

	public class LoadImageNode : IGeneratorNode<LoadImageInstance>
	{

	}
}
