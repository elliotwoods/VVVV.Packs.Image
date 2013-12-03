using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV.CvEnum;
using Emgu.CV;
using Emgu.CV.Structure;
using SlimDX.Direct3D9;
using System.Drawing;
using System.Runtime.InteropServices;
using VVVV.PluginInterfaces.V2;
using System.Drawing.Imaging;

namespace VVVV.CV.Core
{
	public class ImageUtils
	{
		[DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
		public static extern void CopyMemory(IntPtr Destination, IntPtr Source, uint Length);

		public static void Log(Exception e)
		{
			System.Diagnostics.Debug.Print(e.Message);
		}

		public static COLOR_CONVERSION ConvertRoute(TColorFormat src, TColorFormat dst)
		{
			switch (src)
			{
				case TColorFormat.L8:
					{
						switch (dst)
						{
							case TColorFormat.RGBA8:
								return COLOR_CONVERSION.CV_GRAY2RGBA;
						}
						break;
					}

				case TColorFormat.RGB8:
					{
						switch (dst)
						{
							case TColorFormat.L8:
								return COLOR_CONVERSION.CV_RGB2GRAY;

							case TColorFormat.RGBA8:
								return COLOR_CONVERSION.CV_RGB2RGBA;
						}
						break;
					}
				case TColorFormat.RGBA8:
					{
						switch (dst)
						{
							case TColorFormat.L8:
								return COLOR_CONVERSION.CV_RGBA2GRAY;
						}
						break;
					}

				case TColorFormat.RGB32F:
					{
						switch (dst)
						{
							case TColorFormat.L32F:
								return COLOR_CONVERSION.CV_RGBA2GRAY;

							case TColorFormat.RGBA32F:
								return COLOR_CONVERSION.CV_RGB2RGBA;
						}
						break;
					}
			}

			return COLOR_CONVERSION.CV_COLORCVT_MAX;
		}

		public static IImage CreateImage(int width, int height, TColorFormat format)
		{
			switch(format)
			{
				case TColorFormat.L8:
					return new Image<Gray, byte>(width, height);
				case TColorFormat.L16:
					return new Image<Gray, ushort>(width, height);
				case TColorFormat.L32S:
					return new Image<Gray, int>(width, height);
				case TColorFormat.L32F:
					return new Image<Gray, float>(width, height);

				case TColorFormat.RGB8:
					return new Image<Rgb, byte>(width, height);
				case TColorFormat.RGB32F:
					return new Image<Rgb, float>(width, height);

				case TColorFormat.RGBA8:
					return new Image<Rgba, byte>(width, height);
				case TColorFormat.RGBA32F:
					return new Image<Rgba, float>(width, height);
			}

			throw (new NotImplementedException("We have not implemented the automatic creation of this image type"));
		}

		public static TColorFormat GetFormat(IImage image)
		{
			Image<Gray, byte> ImageL8 = image as Image<Gray, byte>;
			if (ImageL8 != null)
				return TColorFormat.L8;

			Image<Gray, ushort> ImageL16 = image as Image<Gray, ushort>;
			if (ImageL16 != null)
				return TColorFormat.L16;
			
			Image<Rgb, byte> ImageRGB8 = image as Image<Rgb, byte>;
			if (ImageRGB8 != null)
				return TColorFormat.RGB8;
			//camera captures seem to arrive as bgr even though rgb
			//may need to revisit this later on
			Image<Bgr, byte> ImageBGR8 = image as Image<Bgr, byte>;
			if (ImageBGR8 != null)
				return TColorFormat.RGB8;

			Image<Rgb, float> ImageRGB32F = image as Image<Rgb, float>;
			if (ImageRGB32F != null)
				return TColorFormat.RGB32F;

			Image<Rgba, byte> ImageRGBA8 = image as Image<Rgba, byte>;
			if (ImageRGBA8 != null)
				return TColorFormat.RGBA8;

			return TColorFormat.UnInitialised;
		}

		public static TColorFormat GetFormat(PixelFormat format)
		{
			switch (format)
			{
				case PixelFormat.Format32bppRgb:
					return TColorFormat.RGBA8;
				case PixelFormat.Format24bppRgb:
					return TColorFormat.RGB8;
			}
			throw (new Exception("Color format not supported by GetFormat(PixelFormat)"));
		}

		public static uint BytesPerPixel(TColorFormat format)
		{
			switch (format)
			{
				case TColorFormat.L8:
					return 1;
				case TColorFormat.L16:
					return 2;
				case TColorFormat.L32F:
					return 4;

				case TColorFormat.RGB8:
					return 3;

				case TColorFormat.RGB32F:
					return 3 * sizeof(float);

				case TColorFormat.RGBA8:
					return 4;

				case TColorFormat.RGBA32F:
					return 4 * sizeof(float);

				default:
					throw(new NotImplementedException("We haven't implemented BytesPerPixel for this type"));
			}
		}

		public static int ChannelCount(TColorFormat format)
		{
			switch (format)
			{
				case TColorFormat.L8:
					return 1;
				case TColorFormat.L16:
					return 1;

				case TColorFormat.RGB8:
					return 3;

				case TColorFormat.RGB32F:
					return 3;

				case TColorFormat.RGBA8:
					return 4;

				case TColorFormat.RGBA32F:
					return 4;

				default:
					return 0;
			}
		}

		public static TColorFormat MakeGrayscale(TColorFormat format)
		{
			switch (format)
			{
				case TColorFormat.RGB8:
					return TColorFormat.L8;

				case TColorFormat.RGB32F:
					return TColorFormat.L32F;

				case TColorFormat.RGBA8:
					return TColorFormat.L8;

				case TColorFormat.RGBA32F:
					return TColorFormat.L32F;

				default:
					return TColorFormat.UnInitialised;
			}
		}

		public static TChannelFormat ChannelFormat(TColorFormat format)
		{
			switch(format)
			{
				case TColorFormat.L8:
				case TColorFormat.RGB8:
				case TColorFormat.RGBA8:
					return TChannelFormat.Byte;

				case TColorFormat.L16:
					return TChannelFormat.UShort;

				case TColorFormat.L32F:
				case TColorFormat.RGB32F:
				case TColorFormat.RGBA32F:
					return TChannelFormat.Float;

				default:
					throw (new Exception("We haven't implemented ChannelFormat for this TColorFormat"));
			}
		}

		public static Format GetDXFormat(TColorFormat format)
		{
			switch (format)
			{
				case TColorFormat.L8:
					return Format.L8;
				case TColorFormat.L16:
					return Format.L16;
				case TColorFormat.L32F:
					return Format.R32F;

				case TColorFormat.RGBA32F:
					return Format.A32B32G32R32F;

				case TColorFormat.RGBA8:
					return Format.A8R8G8B8;

				default:
					throw (new NotImplementedException("Cannot create a texture to match Image's format"));
			}
		}

		public static string AsString(TColorFormat format)
		{
			switch (format)
			{
				case TColorFormat.L8:
					return "L8";
				case TColorFormat.L16:
					return "L16";

				case TColorFormat.RGB8:
					return "RGB8";

				case TColorFormat.RGB32F:
					return "RGB32F";

				case TColorFormat.RGBA8:
					return "RGBA8";

				case TColorFormat.RGBA32F:
					return "RGBA32F";

				default:
					throw (new NotImplementedException("We haven't implemented AsString for this type"));
			}
		}

		public static Texture CreateTexture(CVImageAttributes attributes, Device device)
		{ 
			TColorFormat format = attributes.ColorFormat;
			TColorFormat newFormat;
			bool useConverted = NeedsConversion(format, out newFormat);


			bool ex = device is DeviceEx;
			var pool = ex ? Pool.Default : Pool.Managed;
			var usage = (ex ? Usage.Dynamic : Usage.None) & ~Usage.AutoGenerateMipMap;

			try
			{
				return new Texture(device, Math.Max(attributes.Width, 1), Math.Max(attributes.Height, 1), 1, usage, GetDXFormat(useConverted ? newFormat : format), pool);
			}
			catch (Exception e)
			{
				ImageUtils.Log(e);
                return new Texture(device, 1, 1, 1, usage, Format.X8R8G8B8, Pool.Managed);
			}
		}

		public static bool NeedsConversion(TColorFormat format, out TColorFormat targetFormat)
		{
			switch(format)
			{
				case TColorFormat.RGB8:
					targetFormat = TColorFormat.RGBA8;
					return true;

				case TColorFormat.RGB32F:
					targetFormat = TColorFormat.RGBA32F;
					return true;

				default:
					targetFormat = TColorFormat.UnInitialised;
					return false;
			}
		}

		public static void CopyImage(CVImage source, CVImage target)
		{
			if (source.Size != target.Size)
				throw (new Exception("Can't copy between these 2 images, they differ in dimensions"));

			if (source.NativeFormat != target.NativeFormat)
				throw (new Exception("Can't copy between these 2 images, they differ in pixel colour format"));

			CopyImage(source.CvMat, target.CvMat, target.ImageAttributes.BytesPerFrame);
		}

		public static void CopyImage(IImage source, CVImage target)
		{
			if (source.Size != target.Size)
				throw (new Exception("Can't copy between these 2 images, they differ in dimensions"));

			if (GetFormat(source) != target.NativeFormat)
				throw (new Exception("Can't copy between these 2 images, they differ in pixel colour format"));

			CopyImage(source.Ptr, target.CvMat, target.ImageAttributes.BytesPerFrame);
		}

		public static void CopyImage(IntPtr source, CVImage target)
		{
			CopyMemory(target.Data, source, target.ImageAttributes.BytesPerFrame);
		}

		public static void CopyImage(byte[] source, CVImage target)
		{
			Marshal.Copy(source, 0, target.Data, (int) target.ImageAttributes.BytesPerFrame);
		}

		/// <summary>
		/// Copys by hand raw image data from source to target
		/// </summary>
		/// <param name="source">CvArray object</param>
		/// <param name="target">CvArray object</param>
		/// <param name="size">Size in bytes</param>
		public static void CopyImage(IntPtr source, IntPtr target, uint size)
		{
			IntPtr sourceRaw;
			IntPtr targetRaw;

			int step;
			Size dims;

			CvInvoke.cvGetRawData(source, out sourceRaw, out step, out dims);
			CvInvoke.cvGetRawData(target, out targetRaw, out step, out dims);

			CopyMemory(targetRaw, sourceRaw, size);
		}

		public static void CopyImageConverted(CVImage source, CVImage target)
		{
            if (target.Size != source.Size)
            {
                target.Initialise(source.Size, target.NativeFormat);
            }

			COLOR_CONVERSION route = ConvertRoute(source.NativeFormat, target.NativeFormat);

			if (route == COLOR_CONVERSION.CV_COLORCVT_MAX)
			{
				CvInvoke.cvConvert(source.CvMat, target.CvMat);
			} else {
				try
				{
					CvInvoke.cvCvtColor(source.CvMat, target.CvMat, route);
				}
				catch
				{
					//CV likes to throw here sometimes, but the next frame it's fine
				}
			}

		}

		public static void FlipImageVertical(CVImage image)
		{
			FlipImageVertical(image, image);
		}

		public static void FlipImageVertical(CVImage source, CVImage target)
		{
			CvInvoke.cvFlip(source.CvMat, target.CvMat, FLIP.VERTICAL);
		}

		public static void FlipImageHorizontal(CVImage image)
		{
			FlipImageHorizontal(image, image);
		}

		public static void FlipImageHorizontal(CVImage source, CVImage target)
		{
			CvInvoke.cvFlip(source.CvMat, target.CvMat, FLIP.HORIZONTAL);
		}

		public static bool IsIntialised(IImage image)
		{
			if (image == null)
				return false;

			if (image.Size.Width==0 || image.Size.Height==0)
				return false;

			return true;
		}

		/// <summary>
		/// Get a pixel's channels as doubles
		/// </summary>
		/// <param name="source">Image to lookup</param>
		/// <param name="row">0.0 to 1.0</param>
		/// <param name="column">0.0 to 1.0</param>
		/// <returns></returns>
		public static Spread<double> GetPixelAsDoubles(CVImage source, double x, double y)
		{
			uint row = (uint) (x * (double)source.Width);
			uint col = (uint) (y * (double)source.Height);

			return GetPixelAsDoubles(source, row, col);
		}

		public static unsafe Spread<double> GetPixelAsDoubles(CVImage source, uint column, uint row)
		{
			TColorFormat format = source.ImageAttributes.ColorFormat;
			uint channelCount = (uint)ChannelCount(format);

			if (channelCount == 0)
			{
				return new Spread<double>(0);
			}

			uint width = (uint)source.Width;
			uint height = (uint)source.Height;
			Spread<double> output = new Spread<double>((int)channelCount);

			row %= height;
			column %= width;

			switch (ChannelFormat(format))
			{
				case TChannelFormat.Byte:
					{
						byte* d = (byte*)source.Data.ToPointer();
						for (uint channel = 0; channel < channelCount; channel++)
						{
							output[(int)channel] = (double)d[(column + row * width) * channelCount + channel];
						}
						break;
					}

				case TChannelFormat.Float:
					{
						float* d = (float*)source.Data.ToPointer();
						for (uint channel = 0; channel < channelCount; channel++)
						{
							output[(int)channel] = (double)d[(column + row * width) * channelCount + channel];
						}
						break;
					}

				case TChannelFormat.UShort:
					{
						ushort* d = (ushort*)source.Data.ToPointer();
						for (uint channel = 0; channel < channelCount; channel++)
						{
							output[(int)channel] = (double)d[(column + row * width) * channelCount + channel];
						}
						break;
					}
			}
			return output;
		}

		static byte asByte(int value)
		{
			//return (byte)value;
			if (value > 255)
				return 255;
			else if (value < 0)
				return 0;
			else
				return (byte)value;
		}

		static unsafe void PixelYUV2RGB(byte * rgb, byte y, byte u, byte v)
		{
			int C = y - 16;
			int D = u - 128;
			int E = v - 128;

			rgb[2] = asByte((298 * C + 409 * E + 128) >> 8);
			rgb[1] = asByte((298 * C - 100 * D - 208 * E + 128) >> 8);
			rgb[0] = asByte((298 * C + 516 * D + 128) >> 8);
		}

		public static unsafe void RawYUV2RGBA(IntPtr source, IntPtr destination, uint size)
		{
			byte * yuv = (byte*) source;
			byte* rgba = (byte*) destination;

			for (uint i=0; i<size / 2; i++)
			{
				rgba[0] = yuv[1];
				//PixelYUV2RGB(rgba, yuv[1], yuv[0], yuv[2]);
				rgba[3] = 255;

				rgba += 4;

				rgba[0] = yuv[3];
				//PixelYUV2RGB(rgba, yuv[3], yuv[0], yuv[2]);
				rgba[3] = 255;

				rgba += 4;

				yuv += 4;
			}
		}
	}
}
