using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using System.ComponentModel.Composition;
using VVVV.Core.Logging;
using VVVV.Nodes;
using VVVV.CV.Core;

namespace VVVV.CV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "Cons", Category = "CV.Image")]
	#endregion PluginInfo
	public class ConsNode : Cons<CVImageLink>
	{
	}
}
