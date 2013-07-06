using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX.Direct3D9;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.DeckLink.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "InterimInfo",
				Category = "DeckLink",
				Version = "EX9.Texture",
				Help = "Get the properties for an interim 32bit word texture which represents the desired colourspace.", Tags = "")]
	#endregion PluginInfo
	public class InterimTextureInfo : IPluginEvaluate
	{
		[Input("Mode")]
		IDiffSpread<ModeRegister.ModeIndex> FInMode;

		[Output("Format", EnumName="TextureFormat")]
		ISpread<EnumEntry> FOutFormat;
		
		[Output("Width")]
		ISpread<int> FOutWidth;

		[Output("Height")]
		ISpread<int> FOutHeight;

		EnumEntry FEnumEntry;

		InterimTextureInfo()
		{
			var count = EnumManager.GetEnumEntryCount("TextureFormat");
			for (int i = 0; i < count; i++)
			{
				var entry = EnumManager.GetEnumEntry("TextureFormat", i);
				if (entry.Name == "A8R8G8B8")
				{
					FEnumEntry = entry;
					break;
				}
			}
		}

		public void Evaluate(int SpreadMax)
		{
			if (FInMode.IsChanged)
			{
				FOutWidth.SliceCount = SpreadMax;
				FOutHeight.SliceCount = SpreadMax;
				FOutFormat.SliceCount = SpreadMax;

				for (int i = 0; i < SpreadMax; i++)
				{
					if (FInMode[i] == null)
					{
						FOutWidth[i] = 0;
						FOutHeight[i] = 0;
						FOutFormat[i] = FEnumEntry;
					}
					else
					{
						var mode = ModeRegister.Singleton.Modes[FInMode[i].Index];
						FOutWidth[i] = mode.CompressedWidth;
						FOutHeight[i] = mode.Height;
						FOutFormat[i] = FEnumEntry;
					}
				}
			}
		}
	}
}
