using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using System.Runtime.InteropServices;
using Emgu.CV.CvEnum;
using System.Drawing;

namespace VVVV.CV.Core
{
	public class CVImage : ImageBase, IDisposable
	{
		IImage FImage;
		/// <summary>
		/// Timestamp of this image frame in ticks
		/// </summary>
		public long Timestamp { get; set; }

		public override IImage GetImage()
		{
			return FImage;
		}

		public void Initialise(CVImageAttributes attributes)
		{
			Initialise(attributes.Size, attributes.ColorFormat);
		}

		public bool Initialise(System.Drawing.Size size, TColorFormat format)
		{
			bool changedAttributes = FImageAttributes.CheckChanges(format, size);

			if (changedAttributes || this.Allocated == false)
			{
				Allocate();
				return true;
			}
			else
				return false;
		}

		public bool Initialise(int Width, int Height, TColorFormat Format)
		{
			return this.Initialise(new System.Drawing.Size(Width, Height), Format);
		}

		public void GetImage(TColorFormat format, CVImage target)
		{
			if (format == this.NativeFormat)
				ImageUtils.CopyImage(this, target);
			else
				ImageUtils.CopyImageConverted(this, target);
		}

		/// <summary>
		/// Copy CVImage into target CVImage 
		/// </summary>
		/// <param name="target"></param>
		public void GetImage(CVImage target)
		{
            if (target.NativeFormat == TColorFormat.UnInitialised)
            {
                target.Initialise(this.ImageAttributes);
            }

            GetImage(target.ImageAttributes.ColorFormat, target);
		}

		public unsafe bool SetImage(IImage source)
		{
			if (source == null)
				return false;

			TColorFormat sourceFormat = ImageUtils.GetFormat(source);
			bool Reinitialise = Initialise(source.Size, sourceFormat);

			ImageUtils.CopyImage(source, this);
			this.Timestamp = DateTime.UtcNow.Ticks;

			return Reinitialise;
		}

		public bool SetImage(CVImage source)
		{
			if (source == null)
				return false;

			if (source.NativeFormat == TColorFormat.UnInitialised)
				return false;

			bool Reinitialise = Initialise(source.Size, source.NativeFormat);

			ImageUtils.CopyImage(source, this);
			this.Timestamp = source.Timestamp;

			return Reinitialise;
		}

		public bool SetImage(Bitmap source)
		{
			if (source == null)
				return false;

			TColorFormat format = ImageUtils.GetFormat(source.PixelFormat);
			if (format == TColorFormat.UnInitialised)
				return false;

			bool Reinitialise = Initialise(source.Size, format);

			var bitmapData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, source.PixelFormat);
			ImageUtils.CopyImage(bitmapData.Scan0, this);
			source.UnlockBits(bitmapData);
			this.Timestamp = DateTime.UtcNow.Ticks;

			return Reinitialise;
		}

		/// <summary>
		/// Copy data from pointer. Presume we're initialised and data is of correct size
		/// </summary>
		/// <param name="rawData">Raw pixel data of correct size</param>
		public void SetPixels(IntPtr rawData)
		{
			if (rawData == IntPtr.Zero)
				return;

			ImageUtils.CopyImage(rawData, this);
		}

		/// <summary>
		/// Copy data from byte array. Presume we're initialised and data is of correct size.
		/// </summary>
		/// <param name="rawData"></param>
		public void SetPixels(byte[] rawData)
		{
			ImageUtils.CopyImage(rawData, this);
		}

		override public void Allocate()
		{
			if (FImage != null)
			{
				FImage.Dispose();
			}
			FImage = ImageUtils.CreateImage(this.Width, this.Height, this.NativeFormat);
			this.Timestamp = 0;
		}

		public void LoadFile(string filename)
		{
			this.SetImage(new Image<Bgr,  byte>(filename));
		}

		public void SaveFile(string filename)
		{
			this.GetImage().Save(filename);
		}

		public void Dispose()
		{
			if (FImage != null)
			{
				FImage.Dispose();
				FImage = null;
			}
		}
	}
}
