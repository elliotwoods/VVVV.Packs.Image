using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using System.ComponentModel.Composition;
using VVVV.Core.Logging;

namespace VVVV.Nodes.OpenCV
{
	#region PluginInfo
	[PluginInfo(Name = "Cons", Category = "CV", Help = "Cons 2 inputs (temporary, will replace with templated version when i find it, i.e. >2 inputs. but dont need that right now)", Tags = "")]
	#endregion PluginInfo
	public class ConsNode : Cons<CVImageLink>
	{
	}
}
