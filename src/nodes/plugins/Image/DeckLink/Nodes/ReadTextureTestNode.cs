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

		[Output("Output")]
		ISpread<byte[]> FOutOutput;

		[Output("Status")]
		ISpread<string> FPinOutStatus;

		[Import]
		ILogger FLogger;

		byte[] FBuffer = new byte[2048 * 204 * 4];

		#endregion fields & pins

		ReadTexture FReadBack = null;

		public void Evaluate(int SpreadMax)
		{
			if (FInHandle.IsChanged || FInWidth.IsChanged || FInHeight.IsChanged || FInFormat.IsChanged || FInUsage.IsChanged)
			{
				if (FReadBack != null)
					FReadBack.Dispose();

				FReadBack = new ReadTexture(FInWidth[0], FInHeight[0], FInHandle[0], FInFormat[0], FInUsage[0]);
			}

			if (FReadBack == null)
				return;

			FReadBack.ReadBack(FBuffer);
		}

		public void Dispose()
		{
			if (FReadBack != null)
				FReadBack.Dispose();
		}
	}
}
