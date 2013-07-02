#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;
using DeckLinkAPI;
using System.Collections.Generic;

#endregion usings

namespace VVVV.Nodes.DeckLink
{
	#region PluginInfo
	[PluginInfo(Name = "ListDevices", Category = "DeckLink", Help = "List BlackMagic DeckLink devices", Tags = "", Author = "Elliot Woods", AutoEvaluate = true)]
	#endregion PluginInfo
	public class ListDevicesNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Refresh", IsBang = true, IsSingle = true)]
		ISpread<bool> FPinInRefresh;

		[Output("Device")]
		ISpread<IDeckLink> FPinOutDevices;

		[Output("Model Name")]
		ISpread<string> FPinOutModelName;

		[Output("Display Name")]
		ISpread<string> FPinOutDisplayName;

		[Output("Status")]
		ISpread<string> FPinOutStatus;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
		public ListDevicesNode(IPluginHost host)
		{

		}

		bool FFirstRun = true;
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FFirstRun || FPinInRefresh[0])
			{
				FFirstRun = false;
				Refresh();
			}
		}

		private void Refresh()
		{
			try
			{
				IDeckLink deckLink;
				IDeckLinkIterator iterator;
				List<IDeckLink> deviceList = new List<IDeckLink>();

				
				FPinOutDevices.SliceCount = 0;
				FPinOutModelName.SliceCount = 0;
				FPinOutDisplayName.SliceCount = 0;

				WorkerThread.Singleton.PerformBlocking(() =>
				{
					iterator = new CDeckLinkIterator();
					if (iterator == null)
						throw (new Exception("Please check DeckLink drivers are installed."));
					
					while (true)
					{
						iterator.Next(out deckLink);

						if (deckLink == null)
							break;
						else
							deviceList.Add(deckLink);
					}
				});

				FPinOutDevices.SliceCount = deviceList.Count;
				FPinOutModelName.SliceCount = deviceList.Count;
				FPinOutDisplayName.SliceCount = deviceList.Count;

				for (int i = 0; i < deviceList.Count; i++)
				{
					deckLink = deviceList[i];

					FPinOutDevices[i] = deckLink;

					string model = "";
					string name = "";
					
					WorkerThread.Singleton.PerformBlocking(() =>
					{
						deckLink.GetModelName(out model);
						deckLink.GetDisplayName(out name);
					});

					FPinOutModelName[i] = name;
					FPinOutDisplayName[i] = name;
				}

				FPinOutStatus[0] = "OK";
			}

			catch (Exception e)
			{
				FPinOutStatus[0] = "ERROR : " + e.Message;
			}

		}

	}
}
