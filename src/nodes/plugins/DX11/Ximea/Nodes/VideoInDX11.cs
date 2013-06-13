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
	#region PluginInfo
	[PluginInfo(Name = "VideoIn",
				Category = "Ximea",
				Version = "DX11 Texture",
				Help = "Capture from Ximea camera to DX11 texture",
				Tags = "")]
	#endregion PluginInfo
	public class VideoInDX11Node : IPluginEvaluate, IDisposable, IDX11ResourceProvider
	{
		#region fields & pins
		IPluginHost FHost;

		[Input("Device ID", IsSingle=true)]
		ISpread<int> FInDeviceID;

		[Input("Timeout", IsSingle = true, MinValue = 0, DefaultValue=500)]
		ISpread<int> FInTimeout;

		[Input("Wait For Frame")]
		ISpread<bool> FInWaitForFrame;

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

			if (this.FInDeviceID.IsChanged)
			{
				this.FDevice.DeviceID = this.FInDeviceID[0];
				MaybeReinitialised = true;
			}

			if (this.FInEnabled.IsChanged)
			{
				this.FDevice.Enabled = this.FInEnabled[0];
				MaybeReinitialised = true;
			}

			if (MaybeReinitialised)
			{
				FOutSpecification[0] = FDevice.DeviceSpecification;
			}

			if (FInWaitForFrame[0] && FDevice.Running)
			{
				Stopwatch Timer = new Stopwatch();
				Timer.Start();

				while (Timer.ElapsedMilliseconds < FInTimeout[0] && !FDevice.DataNew)
				{
					Thread.Sleep(1);
				}
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
	}
}
