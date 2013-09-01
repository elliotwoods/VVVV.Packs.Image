using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.Ximea
{
	abstract class ISoftwareTrigger
	{
		protected void OnTrigger()
		{
			if (Trigger != null)
				Trigger(this, EventArgs.Empty);
		}

		public event EventHandler Trigger;
	}
}
