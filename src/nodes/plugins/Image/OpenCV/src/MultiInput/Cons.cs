using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.CV.Core;
using VVVV.Nodes.Generic;

namespace VVVV.CV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "Cons", Category = "CV.Image")]
	#endregion PluginInfo
	public class ConsNode : Cons<CVImageLink>
	{
	}
}
