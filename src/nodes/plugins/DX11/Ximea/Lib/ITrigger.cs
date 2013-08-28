using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.Ximea
{
	enum TriggerType
	{
		Default,
		Software,
		GPI
	}

	interface ITrigger
	{
		TriggerType GetTriggerType();
	}
}
