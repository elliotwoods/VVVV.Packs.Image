using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using VVVV.Nodes.OpenCV;
using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V1;
using System.Drawing;
using System.Runtime.InteropServices;

using FeralTic.DX11.Resources;
using VVVV.DX11;
using SlimDX.DXGI;
using VVVV.Core.Logging;
using SlimDX.Direct3D11;
using SlimDX;

namespace VVVV.Nodes.OpenCV
{
	[PluginInfo(Name = "AsImage",
				Category = "OpenCV",
				Version = "DX11.Texture2D",
				Help = "Converts DX11.Texture2D to CVImageLink",
				Tags = "")]
	public unsafe class FromDX11TextureNode : IPluginEvaluate, IDX11ResourceDataRetriever, IDisposable
	{
		[DllImport("msvcrt.dll", EntryPoint = "memcpy")]
		public unsafe static extern void CopyMemory(IntPtr pDest, IntPtr pSrc, int length);

		[Input("Input")]
		Pin<DX11Resource<DX11Texture2D>> FInput;

		[Output("Output")]
		ISpread<CVImageLink> FOutput;

		[Import()]
		protected IPluginHost FHost;

		[Import()]
		protected ILogger FLogger;

		bool FInitialised = false;
		Texture2D FOffscreenBuffer = null;

		public void Evaluate(int SpreadMax)
		{
			if (!FInitialised)
			{
				FOutput[0] = new CVImageLink();
				FInitialised = true;
			}

			try
			{
				if (FInput.PluginIO.IsConnected)
				{
					if (this.RenderRequest != null) { this.RenderRequest(this, this.FHost); }

					if (this.AssignedContext == null) { return; }

					var device = this.AssignedContext.Device;
					var context = this.AssignedContext;

					DX11Texture2D t = this.FInput[0][this.AssignedContext];
					var height = t.Height;
					var width = t.Width;
					
					var imageLink = FOutput[0];
					var imageAttributes = imageLink.ImageAttributes;
					var desiredImageFormat = ToOpenCVFormat(t.Format);

					if (desiredImageFormat == TColorFormat.UnInitialised)
						throw (new Exception("No suitible image type available for this texture type" + t.Format.ToString()));
					

					//--
					//check attributes and reinitialise the image if we haven't got the right image ready
					//
					if (imageAttributes == null || FOffscreenBuffer == null || !imageAttributes.Initialised || FOffscreenBuffer.Description.Format != t.Description.Format || imageAttributes.Width != t.Width || imageAttributes.Height != t.Height || imageAttributes.ColorFormat != desiredImageFormat)
					{
						if (FOffscreenBuffer != null)
							FOffscreenBuffer.Dispose();

						var description = new Texture2DDescription()
						{
							Width = width,
							Height = height,
							Format = t.Format,
							MipLevels = 1,
							Usage = ResourceUsage.Staging,
							BindFlags = BindFlags.None,
							CpuAccessFlags = CpuAccessFlags.Read,
							SampleDescription = new SampleDescription(1, 0),
							ArraySize = 1
						};
						
						FOffscreenBuffer = new Texture2D(this.AssignedContext.Device, description);

						imageLink.Initialise(new CVImageAttributes(desiredImageFormat, t.Width, t.Height));
					}
					//
					//--


					//--
					//copy the texture to offscreen buffer
					//
					context.CurrentDeviceContext.CopyResource(t.Resource, FOffscreenBuffer);
					//
					//--


					//--
					//copy the data out of the offscreen buffer
					//
					var surface = FOffscreenBuffer.AsSurface();
					var bytesPerRow = imageAttributes.Stride;

					var data = MapForRead(context.CurrentDeviceContext);
					lock (imageLink.BackLock)
					{
						var image = imageLink.BackImage;
						try
						{
							var source = data.Data.DataPointer;

							image.SetPixels(source);

							var destination = image.Data;

							for (int row = 0; row < t.Height; row++)
							{
								CopyMemory(destination, source, bytesPerRow);

								source += data.RowPitch;
								destination += bytesPerRow;
							}
						}
						finally
						{
							UnMap(context.CurrentDeviceContext);
						}
					}
					//
					//--

					imageLink.Swap();
				}
			}
			catch (Exception e)
			{
				FLogger.Log(e);
			}
		}

		public DataBox MapForRead(DeviceContext ctx)
        {
            return ctx.MapSubresource(this.FOffscreenBuffer, 0, MapMode.Read, SlimDX.Direct3D11.MapFlags.None);
        }

		public void UnMap(DeviceContext ctx)
		{
			ctx.UnmapSubresource(this.FOffscreenBuffer, 0);
		}

		TColorFormat ToOpenCVFormat(SlimDX.DXGI.Format format)
		{
			switch (format)
			{
				case Format.B8G8R8A8_UNorm:
				case Format.B8G8R8X8_UNorm:
				case Format.B8G8R8A8_UNorm_SRGB:
				case Format.B8G8R8X8_UNorm_SRGB:
				case Format.B8G8R8A8_Typeless:
				case Format.R8G8B8A8_Typeless:
				case Format.R8G8B8A8_UInt:
				case Format.R8G8B8A8_UNorm:
				case Format.R8G8B8A8_UNorm_SRGB:
					return TColorFormat.RGBA8;

				case Format.R16_UInt:
				case Format.R16_UNorm:
				case Format.R16_Typeless:
					return TColorFormat.L16;

				case Format.R32G32B32A32_Float:
					return TColorFormat.RGBA32F;

				case Format.R32_Float:
					return TColorFormat.L32F;

				case Format.R32_SInt:
					return TColorFormat.L32S;

				default:
					return TColorFormat.UnInitialised;
			}
		}

		public FeralTic.DX11.DX11RenderContext AssignedContext
		{
			get;
			set;
		}

		public event DX11RenderRequestDelegate RenderRequest;

		public void Dispose()
		{
			if (FOffscreenBuffer != null)
				FOffscreenBuffer.Dispose();
		}
	}
}