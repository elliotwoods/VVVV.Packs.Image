using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Runtime.InteropServices;
using FeralTic.DX11.Resources;
using FeralTic.DX11;
using VVVV.DX11;
using VVVV.CV.Core;

namespace VVVV.CV.Nodes
{
    public unsafe class AsTextureDX11Instance : IDestinationInstance, IDisposable
    {
		class Resource : IDisposable
		{
			public DX11DynamicTexture2D Texture = null;
			public bool NeedsRefresh = false;

			public void Dispose()
			{
				this.Texture.Dispose();
			}
		};

        public int Width { get; private set; }
        public int Height { get; private set; }

		public DX11Resource<DX11DynamicTexture2D> OutputSlice = null;
		Dictionary<DX11RenderContext, Resource> FTextures = new Dictionary<DX11RenderContext, Resource>();
		
		CVImageDoubleBuffer FBuffer;
        TColorFormat FConvertedFormat;
        bool FNeedsConversion;
		bool FInputOk = false;

		Object FLockTexture = new Object();
		Object FLockImageAllocation = new Object();

        public override void Allocate()
        {
			//allocate w.r.t. incoming image
			lock (FLockImageAllocation)
			{
				FInputOk = false;
				FNeedsConversion = ImageUtils.NeedsConversion(FInput.ImageAttributes.ColorFormat, out FConvertedFormat);
				FBuffer = new CVImageDoubleBuffer();
				if (FNeedsConversion)
				{
					FBuffer.Initialise(new CVImageAttributes(FInput.ImageAttributes.Size, FConvertedFormat));
				}
				else
				{
					FBuffer.Initialise(FInput.ImageAttributes);
				}

				this.Width = FInput.ImageAttributes.Width;
				this.Height = FInput.ImageAttributes.Height;
				FInputOk = true;
			}
        }

        public override void Process()
        {
			//called on upstream image update
			if (FNeedsConversion)
			{
				FInput.GetImage(FBuffer.BackImage);
				FBuffer.Swap();
			}

			lock (FLockTexture)
			{
				//ImageChanged so mark needs refresh on created textures
				foreach (var texture in FTextures)
				{
					var resource = texture.Value;
					resource.NeedsRefresh = true;
				}
			}
        }


        public static SlimDX.DXGI.Format GetFormat(TColorFormat format)
        {
            switch (format)
            {
                case TColorFormat.L8:
                    return SlimDX.DXGI.Format.R8_UNorm;
                case TColorFormat.L16 :
                    return SlimDX.DXGI.Format.R16_UNorm;
                case TColorFormat.L32F:
                    return SlimDX.DXGI.Format.R32_Float;
                case TColorFormat.RGBA8:
                    return SlimDX.DXGI.Format.B8G8R8A8_UNorm;
				case TColorFormat.RGB32F:
					return SlimDX.DXGI.Format.R32G32B32_Float;
                case TColorFormat.RGBA32F:
                    return SlimDX.DXGI.Format.R32G32B32A32_Float;
				default:
					throw (new Exception("Image type not supported by DX11 texture"));
            }
        }

		public void DestroyTexture(DX11RenderContext context)
		{
			this.FTextures[context].Dispose();
			this.FTextures.Remove(context);
		}

        public void UpdateTexture(DX11RenderContext context)
        {
			lock (FLockTexture)
			{
				if (!FInputOk || !FBuffer.FrontImage.Allocated)
					return;

				CheckTextureAllocation(context);

				var resource = FTextures[context];

				if (resource.NeedsRefresh)
				{
					WriteTexture(resource);
				}
			}
        }

		void CheckTextureAllocation(DX11RenderContext context)
		{
			//check if the texture we've got allocated for this context doesn't meet the current image attributes
			//this coule be on an OnImageAttributesChanged, but perhaps lazy reallocation is best
			if (FTextures.ContainsKey(context))
			{
				var tex = FTextures[context].Texture;
				if (tex.Width != this.Width || tex.Height != this.Height || tex.Format != GetFormat(FBuffer.ImageAttributes.ColorFormat))
				{
					tex.Dispose();
					FTextures.Remove(context);
				}
			}

			//check if we've not currently got a texture allocated for this context
			if (!FTextures.ContainsKey(context))
			{
				var tex = new DX11DynamicTexture2D(context, Width, Height, GetFormat(FBuffer.ImageAttributes.ColorFormat));
				var resource = new Resource()
				{
					Texture = tex,
					NeedsRefresh = this.FInput.Allocated,
				};
				FTextures[context] = resource;
				this.OutputSlice[context] = tex;
			}
		}

		void WriteTexture(Resource resource)
		{
			var texture = resource.Texture;

			CVImage image;

			if (FNeedsConversion)
			{
				FBuffer.LockForReading();
				image = FBuffer.FrontImage;
			}
			else
			{
				FInput.LockForReading();
				image = FInput.Image;
			}
			
			try
			{
				var imageAttributes = image.ImageAttributes;

				if (imageAttributes.Stride == texture.GetRowPitch())
				{
					//write raw
					texture.WriteData(image.Data, imageAttributes.BytesPerFrame);
				}
				else
				{
					//write with pitch
					texture.WriteDataPitch(image.Data, (int)imageAttributes.BytesPerFrame, imageAttributes.Stride / imageAttributes.Width);
				}

				resource.NeedsRefresh = false;
			}
			catch (Exception e)
			{
				ImageUtils.Log(e);
			}
			finally
			{
				if (FNeedsConversion)
				{
					FBuffer.ReleaseForReading();
				}
				else
				{
					FInput.ReleaseForReading();
				}
            }
		}

		public void Dispose()
		{
			var contexts = this.FTextures.Keys.ToList();

			foreach (var context in contexts)
			{
				this.DestroyTexture(context);
			}
		}
	}
}
