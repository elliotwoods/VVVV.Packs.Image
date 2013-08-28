using xiApi.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using VVVV.DX11;
using FeralTic.DX11.Resources;
using VVVV.PluginInterfaces.V1;
using System.ComponentModel.Composition;
using FeralTic.DX11;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

namespace VVVV.Nodes.Ximea
{
	using ParameterSet = Dictionary<Device.IntParameter, int>;

	#region PluginInfo
	[PluginInfo(Name = "VideoIn",
				Category = "Ximea",
				Version = "DX11 Texture",
				Help = "Capture from Ximea camera to DX11 texture",
				Tags = "")]
	#endregion PluginInfo
	public class VideoInDX11Node : IPluginEvaluate, IDisposable, IDX11ResourceProvider, IPartImportsSatisfiedNotification
	{
		#region fields & pins
		IPluginHost FHost;

		[Input("Device ID", IsSingle=true)]
		ISpread<int> FInDeviceID;

		[Input("Timeout", MinValue = 0, DefaultValue=500)]
		ISpread<int> FInTimeout;

		[Input("Wait For Frame")]
		ISpread<bool> FInWaitForFrame;

		[Input("ParameterSet")]
		ISpread<ParameterSet> FInParameterSet;

		[Input("Trigger", Visibility = PinVisibility.Hidden)]
		IDiffSpread<ITrigger> FInTrigger;

		[Input("Enabled", IsSingle = true)]
		IDiffSpread<bool> FInEnabled;

		[Output("Texture Out", Order = 0)]
		protected Pin<DX11Resource<DX11DynamicTexture2D>> FTextureOut;

		[Output("Running")]
		ISpread<bool> FOutRunning;

		[Output("Framerate")]
		ISpread<double> FOutFramerate;

		[Output("Timestamp")]
		ISpread<double> FOutTimestamp;

		[Output("Specification")]
		ISpread<Device.Specification> FOutSpecification;

		[Output("Status")]
		ISpread<string> FOutStatus;

//		[Import]
//		IHDEHost FHDEHost;

		Device FDevice = new Device();
		#endregion fields & pins

		[ImportingConstructor()]
		public VideoInDX11Node(IPluginHost Host)
		{
			this.FHost = Host;
		}

		public void Evaluate(int SpreadMax)
		{
			if (this.FTextureOut[0] == null)
			{
				this.FTextureOut[0] = new DX11Resource<DX11DynamicTexture2D>();
			}

			bool MaybeReinitialised = false;

			try
			{
				if (this.FInTrigger.IsChanged)
				{
					this.FDevice.Trigger = this.FInTrigger[0];
					MaybeReinitialised = true;
				}

				if (this.FInDeviceID.IsChanged)
				{
					if (this.FDevice.DeviceID != this.FInDeviceID[0])
					{
						this.FDevice.DeviceID = this.FInDeviceID[0];
						MaybeReinitialised = true;
					}
				}

				if (this.FInEnabled.IsChanged)
				{
					this.FDevice.Enabled = this.FInEnabled[0];
					MaybeReinitialised = true;
				}

				if ((FInParameterSet.IsChanged || MaybeReinitialised) && FInParameterSet[0] != null)
				{
					foreach (var parameter in FInParameterSet[0])
					{
						FDevice.SetParameter(parameter.Key, parameter.Value);
					}
				}

				if (MaybeReinitialised)
				{
					FOutSpecification[0] = FDevice.DeviceSpecification;
					FOutStatus[0] = "OK";
				}
			}
			catch(Exception e)
			{
				FOutStatus[0] = e.Message;
			}

			FOutRunning[0] = FDevice.Running;
			FOutTimestamp[0] = FDevice.Timestamp;
			FOutFramerate[0] = FDevice.Framerate;
		}

		public void Update(IPluginIO pin, DX11RenderContext context)
		{
			FDevice.Update(this.FTextureOut[0], context);
		}

		public void Destroy(IPluginIO pin, DX11RenderContext context, bool force)
		{
			foreach (var texture in FTextureOut)
			{
				texture.Dispose(context);
			}
		}

		public void Dispose()
		{
			foreach (var texture in FTextureOut)
			{
				if (texture != null)
				{
					texture.Dispose();
				}
			}

			FDevice.Dispose();
		}

		public void OnImportsSatisfied()
		{
			//FHDEHost.MainLoop.OnPresent += MainLoop_OnPresent;
		}

		void MainLoop_OnPresent(object sender, EventArgs e)
		{
			if (FInWaitForFrame[0] && FDevice.Running)
			{
				TimeSpan sleepTime = new TimeSpan(100);
				Stopwatch waitingTime = new Stopwatch();
				waitingTime.Start();

				int timeout = FInTimeout[0];
				FDevice.UpdateFrameAvailable();

				while (!FDevice.DataNew)
				{
					Thread.Sleep(sleepTime);
					FDevice.UpdateFrameAvailable();

					if (waitingTime.ElapsedMilliseconds > timeout)
						break;
				}
			}
		}
	}
}
