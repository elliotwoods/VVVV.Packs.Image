using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.OpenCV.VideoInput
{
	class DeviceLock
	{
		public static Object LockDevices = new Object();
	}
}
