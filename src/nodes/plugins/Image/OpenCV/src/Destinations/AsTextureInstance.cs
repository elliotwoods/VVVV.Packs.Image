using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using System.Drawing;
using SlimDX.Direct3D9;
using SlimDX;
using VVVV.Utils.SlimDX;
using System.ComponentModel.Composition;
using VVVV.Core.Logging;
using VVVV.CV.Core;

namespace VVVV.CV.Nodes
{
	class AsTextureInstance : IDestinationInstance
	{
		public int Width { get; private set; }
		public int Height { get; private set; }

		CVImageDoubleBuffer FBufferConverted;
		TColorFormat FConvertedFormat;
		bool FNeedsConversion = false;

		Object FLockTexture = new Object();
		private Dictionary<Texture, bool> FNeedsRefresh = new Dictionary<Texture,bool>();

        public ILogger Logger;
        
		private bool FNeedsTexture = false;
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
			FNeedsConversion = ImageUtils.NeedsConversion(FInput.ImageAttributes.ColorFormat, out FConvertedFormat);
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

			if (FNeedsConversion)
			{
				FInput.GetImage(FBufferConverted);
				FBufferConverted.Swap();
			}
		}

		private bool InputOK
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
					if (FInput== null)
						return false;
					if (!FInput.Allocated)
						return false;
				}
				
				return true;
			}
		}

		public Texture CreateTexture(Device device)
		{
			lock (FLockTexture)
			{
				if (InputOK)
				{
					Texture output;

					if (FNeedsConversion)
						output = ImageUtils.CreateTexture(FBufferConverted.ImageAttributes.Clone() as CVImageAttributes, device);
					else
						output = ImageUtils.CreateTexture(FInput.ImageAttributes, device);

					FNeedsRefresh.Add(output, true);
					return output;
				} 
				else
					return TextureUtils.CreateTexture(device, 1, 1);
			}
		}

		public void UpdateTexture(Texture texture)
		{
			lock (FLockTexture)
			{
				if (!InputOK)
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
					return;
				}

				bool ex = texture.Device is DeviceEx;
				Surface srf = texture.GetSurfaceLevel(0);
				DataRectangle rect = srf.LockRectangle(ex ? LockFlags.Discard : LockFlags.None);

				try
				{
					Size imageSize = FNeedsConversion ? FBufferConverted.ImageAttributes.Size : FInput.ImageAttributes.Size;

					if (srf.Description.Width != imageSize.Width || srf.Description.Height != imageSize.Height)
					{
						throw (new Exception("AsTextureInstance : srf dimensions don't match image dimensions"));
					}

                    if (!FInput.Image.Allocated)
                    {
                        throw (new Exception("Image not allocated"));
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

							rect.Data.WriteRange(FBufferConverted.FrontImage.Data, FBufferConverted.ImageAttributes.BytesPerFrame);
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
							rect.Data.WriteRange(FInput.Data, FInput.ImageAttributes.BytesPerFrame);
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
                    Logger.Log(e);
				}
				finally
				{
					srf.UnlockRectangle();
				}
			}
		}
	}
}
