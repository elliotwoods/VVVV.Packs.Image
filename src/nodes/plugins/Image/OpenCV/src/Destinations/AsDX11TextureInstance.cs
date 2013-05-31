﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using FeralTic.DX11.Resources;
using FeralTic.DX11;

namespace VVVV.Nodes.OpenCV
{
    class AsDX11TextureInstance : IDestinationInstance
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        CVImageDoubleBuffer FBufferConverted;
        TColorFormat FConvertedFormat;
        bool FNeedsConversion;

	    private readonly Object FLockTexture = new Object();
        private readonly Dictionary<DX11DynamicTexture2D, bool> FNeedsRefresh = new Dictionary<DX11DynamicTexture2D, bool>();

        private bool FNeedsTexture;
        public bool NeedsTexture
        {
            get
            {
                if (FNeedsTexture)
                {
                    FNeedsTexture = false;
                    return true;
                }
                return false;
            }
        }

        public override void Allocate()
        {
            FNeedsConversion = ImageUtils.NeedsConversion(FInput.ImageAttributes.ColourFormat, out FConvertedFormat);
            if (FNeedsConversion)
            {
                FBufferConverted = new CVImageDoubleBuffer();
                FBufferConverted.Initialise(new CVImageAttributes(FInput.ImageAttributes.Size, FConvertedFormat));
            }

            FNeedsTexture = true;
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
	        
			FInput.GetImage(FBufferConverted);
	        FBufferConverted.Swap();
        }

        private bool InputOk
        {
            get
            {
                if (FNeedsConversion)
                {
                    if (FBufferConverted == null)
                        return false;
                    if (!FBufferConverted.Allocated)
                        return false;
                }
                else
                {
                    if (FInput == null)
                        return false;
                    if (!FInput.Allocated)
                        return false;
                }

                return true;
            }
        }

        private static SlimDX.DXGI.Format GetFormat(TColorFormat format)
        {
            switch (format)
            {
                case TColorFormat.L16 :
                    return SlimDX.DXGI.Format.R16_UNorm;
                case TColorFormat.L32F:
                    return SlimDX.DXGI.Format.R32_Float;
                case TColorFormat.RGB8:
                    return SlimDX.DXGI.Format.R8G8B8A8_UNorm;
                case TColorFormat.RGBA8:
                    return SlimDX.DXGI.Format.R8G8B8A8_UNorm;
                case TColorFormat.RGB32F:
                    return SlimDX.DXGI.Format.R32G32B32A32_Float;
                case TColorFormat.RGBA32F:
                    return SlimDX.DXGI.Format.R32G32B32A32_Float;

            }
            return SlimDX.DXGI.Format.Unknown;
        }

        public DX11DynamicTexture2D CreateTexture(DX11RenderContext context)
        {
            lock (FLockTexture)
            {
	            if (InputOk)
                {

                    DX11DynamicTexture2D output;
                    if (FNeedsConversion)
                    {
                        CVImageAttributes attr = FBufferConverted.ImageAttributes.Clone() as CVImageAttributes;
                        SlimDX.DXGI.Format format = GetFormat(attr.ColourFormat);
                        output = new DX11DynamicTexture2D(context, attr.Width, attr.Height, format);
                    }                     
                    else
                    {
                        CVImageAttributes attr = FBufferConverted.ImageAttributes as CVImageAttributes;
                        SlimDX.DXGI.Format format = GetFormat(attr.ColourFormat);
                        output = new DX11DynamicTexture2D(context, attr.Width, attr.Height, format);

                    }

                    FNeedsRefresh.Add(output, true);
                    return output;
                }
	            
				return context.DefaultTextures.WhiteTexture;
            }
        }

        public void UpdateTexture(DX11DynamicTexture2D texture)
        {
            lock (FLockTexture)
            {
                if (!InputOk)
                    return;

                if (!FNeedsRefresh.ContainsKey(texture))
                {
                    FNeedsTexture = true;
                    return;
                }

                if (!FNeedsRefresh[texture])
                    return;

                if (FInput.ImageAttributesChanged)
                {
                    //reset flag we just dropped
                    FInput.ImageAttributesChanged = true;
                    return;
                }

                /*Surface srf = texture.GetSurfaceLevel(0);
                DataRectangle rect = srf.LockRectangle(LockFlags.None);*/

                try
                {
                    Size imageSize = FNeedsConversion ? FBufferConverted.ImageAttributes.Size : FInput.ImageAttributes.Size;

                    if (texture.Width != imageSize.Width || texture.Height != imageSize.Height)
                    {
                        throw (new Exception("AsTextureInstance : srf dimensions don't match image dimensions"));
                    }

                    if (FNeedsConversion)
                    {
                        FInput.GetImage(FBufferConverted);
                        FBufferConverted.Swap();
                        FBufferConverted.LockForReading();
                        try
                        {
                            if (!FBufferConverted.FrontImage.Allocated)
                                throw (new Exception());

                            texture.WriteData(FInput.Data, FInput.BytesPerFrame);
                            //rect.Data.WriteRange(FBufferConverted.FrontImage.Data, FBufferConverted.ImageAttributes.BytesPerFrame);
                            FNeedsRefresh[texture] = false;
                        }
                        catch (Exception e)
                        {
                            ImageUtils.Log(e);
                        }
                        finally
                        {
                            FBufferConverted.ReleaseForReading();
                        }

                    }
                    else
                    {
                        FInput.LockForReading();
                        try
                        {
                            texture.WriteData(FInput.Data, FInput.BytesPerFrame);
                            //rect.Data.WriteRange(FInput.Data, FInput.ImageAttributes.BytesPerFrame);
                            FNeedsRefresh[texture] = false;
                        }
                        catch (Exception e)
                        {
                            ImageUtils.Log(e);
                        }
                        finally
                        {
                            FInput.ReleaseForReading();
                        }
                    }

                }
                catch (Exception e)
                {
                    throw (e);
                }
                finally
                {
                    //srf.UnlockRectangle();
                }
            }
        }
    }
}
