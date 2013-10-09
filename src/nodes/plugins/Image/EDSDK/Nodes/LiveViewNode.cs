using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Canon.Eos.Framework;
using VVVV.Nodes.OpenCV;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.EDSDK
{
	#region PluginInfo
	[PluginInfo(Name = "LiveView", Category = "EDSDK", Help = "Bring in a live view stream from the camera", Tags = "Canon", Author = "elliotwoods", AutoEvaluate = true)]
	#endregion Plugin
	public class LiveViewNode : IPluginEvaluate
	{
		[Input("Device", IsSingle=true)]
		ISpread<EosCamera> FInDevices;

		[Input("Enabled", IsSingle = true)]
		ISpread<bool> FInEnabled;

		[Output("Output")]
		ISpread<CVImageLink> FOutImage;

		[Output("Status")]
		ISpread<string> FOutStatus;

		EosCamera FCamera;
		bool FFirstRun = true;

		LiveViewNode()
		{
			
		}

		public void Evaluate(int SpreadMax)
		{
			if (FFirstRun)
			{
				FOutImage[0] = new CVImageLink();
				FFirstRun = false;
			}

			if (FInDevices[0] != FCamera || FInEnabled.IsChanged)
			{
				try
				{
					throw (new Exception("Actually, this node doesn't work yet, sorry"));
					if (FCamera != null)
					{
						Disconnect();
					}
					if (FInEnabled[0])
					{
						FCamera = FInDevices[0];
						if (FCamera != null)
						{
							Connect();
						}
					}
					FOutStatus[0] = "OK";
				}
				catch (Exception e)
				{
					FOutStatus[0] = e.Message;
				}
			}
		}

		void Connect()
		{
			FCamera.LiveViewUpdate += FCamera_LiveViewUpdate;
			FCamera.LiveViewDevice = EosLiveViewDevice.Host;
			FCamera.StartLiveView();
		}

		void FCamera_LiveViewUpdate(object sender, Canon.Eos.Framework.Eventing.EosLiveImageEventArgs e)
		{
			
		}

		void Disconnect()
		{
			FCamera.LiveViewUpdate -= FCamera_LiveViewUpdate;
			FCamera.StopLiveView();
		}
	}
}
