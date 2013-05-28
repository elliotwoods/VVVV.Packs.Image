using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OptiTrackNET;

namespace VVVV.Nodes.OptiTrack
{
	class Context
	{
		public Context()
		{
			if (MCameraManager.AreCamerasInitialized())
			{
#if (DEBUG)
				MCameraManager.EnableDevelopment();
#endif
				MCameraManager.WaitForInitialization();
			}
		}
	}
}
