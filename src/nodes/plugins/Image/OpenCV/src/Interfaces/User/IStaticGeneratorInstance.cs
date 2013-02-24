using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.OpenCV
{
	/// <summary>
	/// Inherit from this if you have a Generator which doesn't need to open and close a device
	/// Perhaps this should be switched to be the default, with a seperate IDeviceGeneratorInstace
	/// </summary>
	public abstract class IStaticGeneratorInstance : IGeneratorInstance
	{
		protected override bool Open()
		{
			return true;
		}

		protected override void Close()
		{
		}
	}
}
