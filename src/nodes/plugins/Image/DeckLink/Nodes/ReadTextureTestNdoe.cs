using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.DeckLink.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "ReadTexture", Category = "Testing", Author = "Elliot Woods", AutoEvaluate = true)]
	#endregion PluginInfo
	public class ReadTextureTestNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Handle")]
		IDiffSpread<uint> FInHandle;

		[Input("Width", DefaultValue = 64)]
		IDiffSpread<int> FInWidth;

		[Input("Height", DefaultValue = 64)]
		IDiffSpread<int> FInHeight;
		
		[Input("Format", EnumName = "TextureFormat")]
		IDiffSpread<EnumEntry> FInFormat;

		[Input("Usage", EnumName = "TextureUsage")]
		IDiffSpread<EnumEntry> FInUsage;

		[Output("Status")]
		ISpread<string> FPinOutStatus;

		[Import]
		ILogger FLogger;
		#endregion fields & pins

		ReadTexture FReadBack = null;

		public void Evaluate(int SpreadMax)
		{
			if (FInHandle.IsChanged || FInWidth.IsChanged || FInHeight.IsChanged || FInFormat.IsChanged || FInUsage.IsChanged)
			{
				if (FReadBack != null)
					FReadBack.Dispose();

				
			}
		}

		public void Dispose()
		{
			if (FReadBack != null)
				FReadBack.Dispose();
		}
	}
}
