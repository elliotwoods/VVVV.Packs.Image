using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DeckLinkAPI;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.DeckLink.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "EnumerateModes", Category = "DeckLink", Version = "Output", Help = "Enumerate available output modes for attached BlackMagic devices.", Tags = "", Author = "elliotwoods", AutoEvaluate = true)]
	#endregion PluginInfo
	public class EnumerateOutputModes : IPluginEvaluate
	{
		#region fields & pins
		[Input("Refresh", IsSingle=true, IsBang=true)]
		ISpread<bool> FInRefresh;

		[Input("Flags", IsSingle = true)]
		IDiffSpread<_BMDVideoOutputFlags> FInFlags;

		[Input("Mode", EnumName="DeckLinkOutputMode")]
		IDiffSpread<EnumEntry> FInMode;

		[Output("Mode")]
		ISpread<ModeRegister.ModeIndex> FOutMode;

		[Output("Width")]
		ISpread<int> FOutWidth;

		[Output("Height")]
		ISpread<int> FOutHeight;

		[Output("FrameRate")]
		ISpread<double> FOutFrameRate;

		[Output("Status")]
		ISpread<string> FOutStatus;

		[Import]
		ILogger FLogger;
		bool firstRun = true;
		#endregion fields & pins

		public void Evaluate(int SpreadMax)
		{
			if (FInRefresh[0] || FInFlags.IsChanged || firstRun)
			{
				firstRun = false;
				FOutStatus.SliceCount = 1;
				try
				{
					_BMDVideoOutputFlags flags = _BMDVideoOutputFlags.bmdVideoOutputFlagDefault;
					for (int i = 0; i < FInFlags.SliceCount; i++)
					{
						if (i == 0)
							flags = FInFlags[i];
						else
							flags |= FInFlags[i];
					}
					ModeRegister.Singleton.Refresh(flags);

					string firstKey = ModeRegister.Singleton.Modes.Keys.First();
					EnumManager.UpdateEnum("DeckLinkOutputMode", firstKey, ModeRegister.Singleton.EnumStrings);
					FOutStatus[0] = "OK";
				}
				catch(Exception e)
				{
					FOutStatus[0] = e.Message;
				}
			}

			if (FInMode.IsChanged)
			{
				FOutMode.SliceCount = SpreadMax;
				FOutWidth.SliceCount = SpreadMax;
				FOutHeight.SliceCount = SpreadMax;
				FOutFrameRate.SliceCount = SpreadMax;
				FOutStatus.SliceCount = SpreadMax;

				for (int slice = 0; slice < SpreadMax; slice++)
				{
					try
					{
						var modes = ModeRegister.Singleton.Modes;

						var selection = FInMode[slice];
						if (!modes.ContainsKey(selection.Name))
							throw (new Exception("No valid mode selected (" + selection.Name + ")"));

						var mode = modes[selection.Name];
						FOutMode[slice] = new ModeRegister.ModeIndex(selection.Name);
						FOutWidth[slice] = mode.Width;
						FOutHeight[slice] = mode.Height;
						FOutFrameRate[slice] = mode.FrameRate;
						
						FOutStatus[slice] = "OK";
					}
					catch (Exception e)
					{
						FOutStatus[slice] = e.Message;
					}
				}
			}
		}
	}
}
