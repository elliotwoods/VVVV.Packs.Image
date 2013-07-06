using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using SlimDX;
using SlimDX.Direct3D9;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.DeckLink
{
	public enum TextureType
	{
		None,
		RenderTarget,
		DepthStencil,
		Dynamic
	}

	/// <summary>
	/// A method for copying data back from a DX9Ex shared Surface on the GPU
	/// </summary>
	class ReadTexture : IDisposable
	{
		struct Vertex
		{
			public Vector4 Position;
			public Vector2 TextureCoord;

			static public int GetSize()
			{
				return Vector4.SizeInBytes + Vector2.SizeInBytes;
			}
		}

		Direct3DEx FContext;
		DeviceEx FDevice;
		Control FHiddenControl;
		bool FInitialised = false;

		int FWidth;
		int FHeight;
		IntPtr FHandle;
		Format FFormat;
		Usage FUsage;

		Texture FTextureShared;
		Texture FTextureCopied;
		Surface FSurfaceOffscreen;

		public ReadTexture(int width, int height, uint handle, EnumEntry formatEnum, EnumEntry usageEnum)
		{
			Format format;
			if (formatEnum.Name == "INTZ")
				format = D3DX.MakeFourCC((byte)'I', (byte)'N', (byte)'T', (byte)'Z');
			else if (formatEnum.Name == "RAWZ")
				format = D3DX.MakeFourCC((byte)'R', (byte)'A', (byte)'W', (byte)'Z');
			else if (formatEnum.Name == "RESZ")
				format = D3DX.MakeFourCC((byte)'R', (byte)'E', (byte)'S', (byte)'Z');
			else if (formatEnum.Name == "No Specific")
				throw (new Exception("Texture mode not supported"));
			else
				format = (Format)Enum.Parse(typeof(Format), formatEnum, true);

			var usage = Usage.Dynamic;
			if (usageEnum.Index == (int)(TextureType.RenderTarget))
				usage = Usage.RenderTarget;
			else if (usageEnum.Index == (int)(TextureType.DepthStencil))
				usage = Usage.DepthStencil;

			this.FWidth = width;
			this.FHeight = height;
			this.FHandle = (IntPtr)unchecked((int)handle);
			this.FFormat = format;
			this.FUsage = usage;

			Initialise();
		}

		public ReadTexture(int width, int height, IntPtr handle, Format format, Usage usage)
		{
			this.FWidth = width;
			this.FHeight = height;
			this.FHandle = handle;
			this.FFormat = format;
			this.FUsage = usage;

			Initialise();
		}

		void Initialise()
		{
			if (this.FHandle == (IntPtr) 0)
				throw (new Exception("No shared texture handle set"));
			this.FContext = new Direct3DEx();

			this.FHiddenControl = new Control();
			this.FHiddenControl.Visible = false;
			this.FHiddenControl.Width = this.FWidth;
			this.FHiddenControl.Height = this.FHeight;
			
			var flags = CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.PureDevice | CreateFlags.FpuPreserve;
			this.FDevice = new DeviceEx(FContext, 0, DeviceType.Hardware, this.FHiddenControl.Handle, flags, new PresentParameters()
			{
				BackBufferWidth = this.FWidth,
				BackBufferHeight = this.FHeight
			});

			this.FTextureShared = new Texture(this.FDevice, this.FWidth, this.FHeight, 1, FUsage, FFormat, Pool.Default, ref this.FHandle);
			this.FTextureCopied = new Texture(this.FDevice, this.FWidth, this.FHeight, 1, Usage.RenderTarget, FFormat, Pool.Default);

			var description = FTextureCopied.GetLevelDescription(0);
			this.FSurfaceOffscreen = Surface.CreateOffscreenPlainEx(FDevice, FWidth, FHeight, description.Format, Pool.SystemMemory, Usage.None);
			this.FInitialised = true;
		}

		/// <summary>
		/// Read back the data from the texture into a CPU buffer
		/// </summary>
		/// <param name="buffer"></param>
		public void ReadBack(byte[] buffer)
		{
			Stopwatch Timer = new Stopwatch();
			Timer.Start();
			try
			{
				FDevice.StretchRectangle(this.FTextureShared.GetSurfaceLevel(0), this.FTextureCopied.GetSurfaceLevel(0), TextureFilter.None);
				FDevice.GetRenderTargetData(this.FTextureCopied.GetSurfaceLevel(0), FSurfaceOffscreen);

				var rect = FSurfaceOffscreen.LockRectangle(LockFlags.ReadOnly);
				try
				{
					rect.Data.Read(buffer, 0, buffer.Length);
					FSurfaceOffscreen.UnlockRectangle();
				}
				catch (Exception e)
				{
					FSurfaceOffscreen.UnlockRectangle();
					throw;
				}
			}
			catch (Exception e)
			{
				FDevice.EndScene();
				throw;
			}
			Timer.Stop();
			Debug.Print(Timer.Elapsed.TotalMilliseconds.ToString());
		}

		public int BufferLength
		{
			get
			{
				return this.FWidth * this.FHeight * 4;
			}
		}

		public void Dispose()
		{
			FTextureShared.Dispose();
			FTextureCopied.Dispose();
			FSurfaceOffscreen.Dispose(); 
			
			FContext.Dispose();
			FDevice.Dispose();
			FHiddenControl.Dispose();
		}
	}
}
