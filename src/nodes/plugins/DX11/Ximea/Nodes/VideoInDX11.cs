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

namespace VVVV.Nodes.Ximea
{
	class Device : IDisposable
	{
		xiCam FDevice;

		int FID = -1;
		public int ID
		{
			set
			{
				if (value != FID)
				{
					FID = value;
					if (this.IsOpen)
					{
						this.Open();
					}
				}
			}
		}

		bool FIsOpen = false;
		public bool IsOpen
		{
			get
			{
				return FIsOpen;
			}
		}

		public void Open()
		{
			this.Close();

			FDevice = new xiCam();
			FDevice.OpenDevice(FID);

			FDevice.SetParam(PRM.EXPOSURE, 1291);
			FDevice.SetParam(PRM.AEAG, 0);
			FDevice.SetParam(PRM.GPI_SELECTOR, 1);
			FDevice.SetParam(PRM.GPI_MODE, GPI_MODE.TRIGGER);
			FDevice.SetParam(PRM.TRG_SOURCE, TRG_SOURCE.EDGE_RISING);

			FDevice.SetParam(PRM.HEIGHT, 1536);
			FDevice.SetParam(PRM.OFFSET_Y, (2048 - 1536) / 2);

			FDevice.StartAcquisition();

			FWidth = FDevice.GetParamInt(PRM.WIDTH);
			FHeight = FDevice.GetParamInt(PRM.HEIGHT);			
			FIsOpen = true;
		}

		public void Close()
		{
			if (this.IsOpen && FDevice != null)
			{
				FDevice.CloseDevice();
				FDevice = null;
				FIsOpen = false;
			}
		}

		int FWidth = 0;
		public int Width
		{
			get
			{
				return FWidth;
			}
		}

		int FHeight = 0;
		public int Height
		{
			get
			{
				return FHeight;
			}
		}

		int FTimeout = 100;
		public int Timeout
		{
			set
			{
				this.FTimeout = value;
			}
		}

		public void Update(DX11Resource<DX11DynamicTexture2D> textureSlice, DX11RenderContext context)
		{
			if (!IsOpen)
			{
				return;
			}
			
			DX11DynamicTexture2D tex;
			
			//create texture if necessary
			//should also check if properties (width,height) changed
			if (!textureSlice.Contains(context))
			{
				tex = new DX11DynamicTexture2D(context, FWidth, FHeight, SlimDX.DXGI.Format.R8_UNorm);
				textureSlice[context] = tex;
			}
			else
			{
				tex = textureSlice[context];
			}
			
			byte[] imageData;

			try
			{
				FDevice.GetImage(out imageData, FTimeout);
			}
			catch
			{
				return;
			}

			//write data to surface
			if (FWidth == tex.GetRowPitch())
			{
				tex.WriteData(imageData);
			}
			else
			{
				GCHandle pinnedArray = GCHandle.Alloc(imageData, GCHandleType.Pinned);
				tex.WriteDataPitch(pinnedArray.AddrOfPinnedObject(), imageData.Length);
				pinnedArray.Free();
			}
		}

		public void Dispose()
		{
			Close();
		}
	}

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

		[Input("Timeout", IsSingle = true, MinValue = 0, DefaultValue=100)]
		ISpread<int> FInTimeout;

		[Input("Open", IsSingle = true)]
		IDiffSpread<bool> FInOpen;

		[Output("Texture Out", Order = 0)]
		protected Pin<DX11Resource<DX11DynamicTexture2D>> FTextureOut;

		[Output("Open")]
		ISpread<bool> FOutOpen;

		[Output("Width")]
		ISpread<int> FOutWidth;

		[Output("Height")]
		ISpread<int> FOutHeight;

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

			if (FInDeviceID.IsChanged || FInOpen.IsChanged)
			{
				FDevice.ID = FInDeviceID[0];
				if (FDevice.IsOpen != FInOpen[0])
				{
					if (FInOpen[0])
					{
						FDevice.Open();
					}
					else
					{
						FDevice.Close();
					}
				}
			}

			if (FInTimeout.IsChanged)
			{
				FDevice.Timeout = FInTimeout[0];
			}

			FOutWidth[0] = FDevice.Width;
			FOutHeight[0] = FDevice.Height;
			FOutOpen[0] = FDevice.IsOpen;
		}

		public void Dispose()
		{
			foreach (var texture in FTextureOut)
			{
				texture.Dispose();
			}
		}

		public void Update(IPluginIO pin, DX11RenderContext context)
		{
			FDevice.Update(this.FTextureOut[0], context);
		}
		
		public void Destroy(IPluginIO pin, DX11RenderContext context, bool force)
		{
			throw new NotImplementedException();
		}
	}
}
