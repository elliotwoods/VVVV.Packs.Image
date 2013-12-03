using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System;
using FreeImageAPI;

using VVVV.CV.Core;
using System.Drawing.Imaging;
using System.Drawing;
using VVVV.CV.Core;

namespace VVVV.Nodes.OpenCV.FreeImageNodes
{
	public class FileImageInstance : IStaticGeneratorInstance
	{
		string FFilename = "";

		public override bool NeedsThread()
		{
			return false;
		}

		public string Filename
		{
			set
			{
				if (FFilename != value)
				{
					FFilename = value;
					LoadImage();
				}
			}
		}

		public void Reload()
		{
			LoadImage();
		}

		private void LoadImage()
		{
			try
			{
                if (!System.IO.File.Exists(FFilename))
                {
                    throw (new Exception("Given filename '" + FFilename + "' is not a file"));
                }

				FREE_IMAGE_FORMAT format = FreeImage.GetFileType(FFilename, 0);
				FIBITMAP bmp = FreeImage.Load(format, FFilename, FREE_IMAGE_LOAD_FLAGS.JPEG_ACCURATE);

				if (bmp.IsNull == true)
					throw (new Exception("Couldn't load file"));

				if (FreeImage.GetColorType(bmp) == FREE_IMAGE_COLOR_TYPE.FIC_PALETTE || FreeImage.GetBPP(bmp) < 8)
				{
					FIBITMAP converted;
					//we need some conversion from strange pallettes
					if (FreeImage.IsTransparent(bmp))
						converted = FreeImage.ConvertTo32Bits(bmp);
					else if (FreeImage.IsGreyscaleImage(bmp))
						converted = FreeImage.ConvertTo8Bits(bmp);
					else
						converted = FreeImage.ConvertTo24Bits(bmp);
					FreeImage.Unload(bmp);
					bmp = converted;
				}

				//now we should have a fairly sensible 8, 24 or 32bit (uchar) image
				//or a float / hdr image
				uint width = FreeImage.GetWidth(bmp);
				uint height = FreeImage.GetHeight(bmp);
				uint bpp = FreeImage.GetBPP(bmp);
				FREE_IMAGE_TYPE type = FreeImage.GetImageType(bmp);
				
				TColorFormat CVFormat;
				if (type == FREE_IMAGE_TYPE.FIT_BITMAP)
				{
					//standard image (8bbp)
					uint channels = bpp / 8;
					switch (channels)
					{
						case (1):
							CVFormat = TColorFormat.L8;
							break;
						case (3):
							CVFormat = TColorFormat.RGB8;
							break;
						case (4):
							CVFormat = TColorFormat.RGBA8;
							break;
						default:
							CVFormat = TColorFormat.UnInitialised;
							break;
					}
				}
				else
				{
					switch (type)
					{
						case (FREE_IMAGE_TYPE.FIT_INT16):
							CVFormat = TColorFormat.L16;
							break;
						case (FREE_IMAGE_TYPE.FIT_FLOAT):
							CVFormat = TColorFormat.L32F;
							break;

						case (FREE_IMAGE_TYPE.FIT_INT32):
							CVFormat = TColorFormat.L32S;
							break;

						case (FREE_IMAGE_TYPE.FIT_RGBF):
							CVFormat = TColorFormat.RGB32F;
							break;

						case (FREE_IMAGE_TYPE.FIT_RGBAF):
							CVFormat = TColorFormat.RGBA32F;
							break;

						default:
							CVFormat = TColorFormat.UnInitialised;
							break;
					}
				}

				if (CVFormat == TColorFormat.UnInitialised)
				{
					FreeImage.Unload(bmp);
					throw (new Exception("VVVV.Nodes.OpenCV doesn't support this colour type \"" + type.ToString() + "\" yet. Please ask!"));
				}

				IntPtr data = FreeImage.GetBits(bmp);

				FOutput.Image.Initialise(new Size((int)width, (int)height), CVFormat);
				FOutput.Image.SetPixels(data);
				ImageUtils.FlipImageVertical(FOutput.Image);
				FOutput.Send();
				FreeImage.Unload(bmp);
				Status = "OK";
			}
			catch (Exception e)
			{
				Status = e.Message;
			}
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "FileImage", Category = "CV.Image", Version = "FreeImage", Help = "Loads image file from disk.", Tags = "")]
	#endregion PluginInfo
	public class FileImageNode : IGeneratorNode<FileImageInstance>
	{
		#region fields & pins
		[Input("Filename", StringType = StringType.Filename, DefaultString = null)]
		IDiffSpread<string> FPinInFilename;

		[Input("Reload", IsBang = true)]
		ISpread<bool> FPinInReload;

		[Import]
		ILogger FLogger;
		#endregion fields&pins

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			if (FPinInFilename.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Filename = FPinInFilename[i];

			for (int i = 0; i < InstanceCount; i++)
			{
				if (FPinInReload[i])
					FProcessor[i].Reload();
			}
		}
	}
}
