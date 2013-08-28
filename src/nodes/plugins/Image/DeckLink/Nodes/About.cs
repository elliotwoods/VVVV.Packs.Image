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

#endregion usings

namespace VVVV.Nodes.DeckLink
{
	#region PluginInfo
	[PluginInfo(Name = "About", Category = "DeckLink", Help = "Report version details of BlackMagic DeckLink API", Tags = "", Author = "elliotwoods", AutoEvaluate = true)]
	#endregion PluginInfo
	public class AboutNode : IPluginEvaluate
	{
		#region fields & pins
		[Output("Version major")]
		ISpread<int> FPinOutVersionMajor;

		[Output("Version minor")]
		ISpread<int> FPinOutVersionMinor;

		[Output("Version point")]
		ISpread<int> FPinOutVersionPoint;

		[Output("Status")]
		ISpread<string> FPinOutStatus;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		[ImportingConstructor]
		public AboutNode(IPluginHost host)
		{

		}

		bool FFirstRun = true;
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FFirstRun)
			{
				FFirstRun = false;
				Refresh();
			}
		}

		private void Refresh()
		{
			try
			{
				IDeckLinkAPIInformation apiInfo;
				apiInfo = new CDeckLinkAPIInformation();

				long deckLinkVersion;
				apiInfo.GetInt(_BMDDeckLinkAPIInformationID.BMDDeckLinkAPIVersion, out deckLinkVersion);

				long dlVerMajor = (deckLinkVersion & 0xFF000000) >> 24;
				long dlVerMinor = (deckLinkVersion & 0x00FF0000) >> 16;
				long dlVerPoint = (deckLinkVersion & 0x0000FF00) >> 8;

				FPinOutVersionMajor[0] = (int) dlVerMajor;
				FPinOutVersionMinor[0] = (int) dlVerMinor;
				FPinOutVersionPoint[0] = (int) dlVerPoint;

				FPinOutStatus[0] = "OK";
			}

			catch (Exception e)
			{
				FPinOutStatus[0] = "ERROR : " + e.Message;
			}

		}

	}
}
