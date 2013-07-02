using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using FeralTic.DX11;
using FeralTic.DX11.Resources;
using VVVV.DX11;
using xiApi.NET;

namespace VVVV.Nodes.Ximea.Lib
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
			FDevice.SetParam(PRM.BUFFER_POLICY, BUFF_POLICY.UNSAFE);

			FDevice.SetParam(PRM.AEAG, 0);
			FDevice.SetParam(PRM.EXPOSURE, 1291);

			if (FHWTrigger)
			{
				FDevice.SetParam(PRM.GPI_SELECTOR, 1);
				FDevice.SetParam(PRM.GPI_MODE, GPI_MODE.TRIGGER);
				FDevice.SetParam(PRM.TRG_SOURCE, TRG_SOURCE.EDGE_FALLING);
			}
			else
			{
				FDevice.SetParam(PRM.TRG_SOURCE, TRG_SOURCE.OFF);
			}

			FDevice.SetParam(PRM.HEIGHT, FROIHeight);
			FDevice.SetParam(PRM.OFFSET_Y, (2048 - FROIHeight) / 2);

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

		int FROIHeight = 1536;
		public int ROIHeight
		{
			set
			{
				this.FROIHeight = value;
				if (IsOpen && value != this.FROIHeight)
				{
					FDevice.SetParam(PRM.HEIGHT, FROIHeight);
					FDevice.SetParam(PRM.OFFSET_Y, (2048 - FROIHeight) / 2);
					FHeight = FROIHeight;
				}
			}
		}

		bool FHWTrigger = false;
		public bool HWTrigger
		{
			set
			{
				if (this.FHWTrigger == value)
					return;

				this.FHWTrigger = value;
				if (IsOpen)
				{
					Open();
				}
			}
		}

		byte[] data;
		bool FDataNew = false;

		public void Update(DX11Resource<DX11DynamicTexture2D> textureSlice, DX11RenderContext context)
		{
			if (!IsOpen || !FDataNew)
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
			else if (textureSlice[context].Width != this.FWidth || textureSlice[context].Height != this.FHeight)
			{
				textureSlice[context].Dispose();
				tex = new DX11DynamicTexture2D(context, FWidth, FHeight, SlimDX.DXGI.Format.R8_UNorm);
				textureSlice[context] = tex;
			}
			else
			{
				tex = textureSlice[context];
			}

			//write data to surface
			if (FWidth == tex.GetRowPitch())
			{
				tex.WriteData(data);
			}
			else
			{
				GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
				tex.WriteDataPitch(pinnedArray.AddrOfPinnedObject(), data.Length);
				pinnedArray.Free();
			}
		}

		public void UpdateCapture()
		{
			if (!IsOpen)
				return;

			try
			{
				FDevice.GetImage(out data, FTimeout);
				FDataNew = true;
			}
			catch
			{
				return;
			}
		}

		public void Dispose()
		{
			Close();
		}
	}
}
