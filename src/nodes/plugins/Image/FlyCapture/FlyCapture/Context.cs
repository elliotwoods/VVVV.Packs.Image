using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlyCapture2Managed;

namespace VVVV.Nodes.FlyCapture
{
	class Context
	{
		static ManagedBusManager FBus = new ManagedBusManager();

		static public ManagedBusManager Bus
		{
			get
			{ return FBus; }
		}
	}
}
