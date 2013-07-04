using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DeckLinkAPI;

namespace VVVV.Nodes.DeckLink
{
	class ModeRegister
	{
		public class ModeIndex
		{
			public ModeIndex(string Index)
			{
				this.Index = Index;
			}

			public string Index;
		}

		public static ModeRegister Singleton = new ModeRegister();

		Dictionary<string, IDeckLinkDisplayMode> FModes = new Dictionary<string, IDeckLinkDisplayMode>();
		public Dictionary<string, IDeckLinkDisplayMode> Modes
		{
			get
			{
				return this.FModes;
			}
		}

		public ModeRegister()
		{
		}

		public void Refresh()
		{
			WorkerThread.Singleton.PerformBlocking(() =>
			{
				foreach (var mode in FModes.Values)
					Marshal.ReleaseComObject(mode);
				FModes.Clear();

				DeviceRegister.Singleton.Refresh();
				foreach (var device in DeviceRegister.Singleton.Devices)
				{
					var output = (IDeckLinkOutput)device.DeviceHandle;
					if (output == null)
						throw (new Exception("Device has no outputs"));

					IDeckLinkDisplayModeIterator modeIterator;
					output.GetDisplayModeIterator(out modeIterator);

					while (true)
					{
						IDeckLinkDisplayMode mode;

						modeIterator.Next(out mode);
						if (mode == null)
							break;

						string modeString;
						mode.GetName(out modeString);
						if (FModes.ContainsKey(modeString))
						{
							Marshal.ReleaseComObject(mode);
						}
						else
						{
							FModes.Add(modeString, mode);
						}
					}
				}
			});
		}

		public string[] EnumStrings
		{
			get
			{
				return FModes.Keys.ToArray();
			}
		}
	}
}
