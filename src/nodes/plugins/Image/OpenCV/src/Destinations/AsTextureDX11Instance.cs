using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Runtime.InteropServices;
using FeralTic.DX11.Resources;
using FeralTic.DX11;
using VVVV.DX11;

namespace VVVV.Nodes.OpenCV
{
    public unsafe class AsTextureDX11Instance : IDestinationInstance, IDisposable
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

		public DX11Resource<DX11DynamicTexture2D> OutputSlice = null;
		Dictionary<DX11RenderContext, bool> FNeedsRefresh = new Dictionary<DX11RenderContext, bool>();
		
		CVImageDoubleBuffer FBuffer;
        TColorFormat FConvertedFormat;
        bool FNeedsConversion;
		bool FInputOk = false;

		Object FLockTexture = new Object();
		Object FLockImageAllocation = new Object();


        public override void Allocate()
        {
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
            lock (FLockTexture)
            {
                //ImageChanged so mark needs refresh on created textures
                foreach (var key in FNeedsRefresh.Keys.ToList())
                {
                    FNeedsRefresh[key] = true;
                }
            }

	        if (!FNeedsConversion) return;
	        
			FInput.GetImage(FBuffer);
	        FBuffer.Swap();
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

		public void DropContext(DX11RenderContext context)
		{
			this.OutputSlice[context].Dispose();
			this.FNeedsRefresh.Remove(context);
		}

        public void UpdateTexture(DX11RenderContext context)
        {
            lock (FLockTexture)
            {
                if (!FInputOk)
                    return;

				DX11DynamicTexture2D tex;
				if (!this.OutputSlice.Contains(context))
				{
					tex = new DX11DynamicTexture2D(context, Width, Height, GetFormat(FBuffer.ImageAttributes.ColorFormat));
					this.OutputSlice[context] = tex;
					FNeedsRefresh[context] = true;
				}
				else
				{
					tex = this.OutputSlice[context];
					if (tex.Width != this.Width || tex.Height != this.Height || tex.Format != GetFormat(FBuffer.ImageAttributes.ColorFormat))
					{
						tex.Dispose();
						tex = new DX11DynamicTexture2D(context, Width, Height, GetFormat(FBuffer.ImageAttributes.ColorFormat));
						this.OutputSlice[context] = tex;
					}
				}

               if (!FNeedsRefresh[context])
				   return;

                try
                {
					Size imageSize = FBuffer.ImageAttributes.Size;

					FBuffer.LockForReading();
					try
					{
						if (!FBuffer.FrontImage.Allocated)
							throw (new Exception());

						int rowPitch = tex.GetRowPitch();

						if (FBuffer.ImageAttributes.Stride == rowPitch)
						{
							tex.WriteData(FBuffer.FrontImage.Data, FBuffer.ImageAttributes.BytesPerFrame);
						}
						else
						{
							tex.WriteDataPitch(FBuffer.FrontImage.Data, (int) FBuffer.ImageAttributes.BytesPerFrame, FBuffer.ImageAttributes.Stride / FBuffer.ImageAttributes.Width);
						}

						FNeedsRefresh[context] = false;
					}
					catch (Exception e)
					{
						ImageUtils.Log(e);
					}
					finally
					{
						FBuffer.ReleaseForReading();
					}
                }
                catch (Exception e)
                {
                    throw (e);
                }
            }
        }

		void Dispose()
		{
			foreach (var context in FNeedsRefresh)
			{
				OutputSlice[context.Key].Dispose();
			}
		}
	}
}
