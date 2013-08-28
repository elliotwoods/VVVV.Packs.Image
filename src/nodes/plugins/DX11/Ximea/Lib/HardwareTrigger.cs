using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.Ximea
{
	class HardwareTrigger : ITrigger
	{
		public enum HardwareEvent
		{
			RisingEdge,
			FallingEdge,
			WhilstHigh
		}

		public HardwareEvent Source;

		public TriggerType GetTriggerType()
		{
			return TriggerType.GPI;
		}
	}
}
