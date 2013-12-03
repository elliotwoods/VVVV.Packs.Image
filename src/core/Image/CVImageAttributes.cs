using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace VVVV.CV.Core
{
	public enum TColorFormat { UnInitialised, RGB8, RGB32F, RGBA8, RGBA32F, L8, L16, L32S, L32F };

	public enum TChannelFormat { UnInitialised, Byte, UShort, UInt, Float};

	public class ImageAttributesChangedEventArgs : EventArgs
	{
		public CVImageAttributes Attributes { get; private set; }

		public ImageAttributesChangedEventArgs(CVImageAttributes attributes)
		{
			this.Attributes = attributes;
		}
	}

	public class CVImageAttributes : ICloneable
	{
		public TColorFormat ColorFormat;
		public Size FSize = new Size();

		public CVImageAttributes()
		{
			ColorFormat = TColorFormat.UnInitialised;
			FSize = new Size(0, 0);
		}

		public CVImageAttributes(Size size, TColorFormat format)
		{
			FSize = size;
			ColorFormat = format;
		}

		public CVImageAttributes(TColorFormat c, int w, int h)
		{
			ColorFormat = c;
			FSize.Width = w;
			FSize.Height = h;
		}

		public bool CheckChanges(TColorFormat c, Size s)
		{
			bool changed = false;
			if (c != ColorFormat)
			{
				ColorFormat = c;
				changed = true;
			}

			if (s != FSize)
			{
				FSize = s;
				changed = true;
			}
			return changed;
		}

		public bool Initialised
		{
			get
			{
				return ColorFormat != TColorFormat.UnInitialised;
			}
		}
		public int Width
		{
			get
			{
				return FSize.Width;
			}
		}

		public int Height
		{
			get
			{
				return FSize.Height;
			}
		}

		public Size Size
		{
			get
			{
				return FSize;
			}
		}

		public uint BytesPerPixel
		{
			get
			{
				return ImageUtils.BytesPerPixel(ColorFormat);
			}
		}

		public uint BytesPerFrame
		{
			get
			{
				return this.BytesPerPixel * this.PixelsPerFrame;
			}
		}

		public int ChannelCount
		{
			get
			{
				return ImageUtils.ChannelCount(this.ColorFormat);
			}
		}

		public int Stride
		{
			get
			{
				/**HACK**/
				//Can't seem to find the CV method to find this
				//it's called step and is included in the matrix header
				//presume 4-align
				int stride = Width * (int)this.BytesPerPixel;
				if (stride % 4 != 0)
					stride += 4 - (stride % 4);
				return stride;
			}
		}

		public uint PixelsPerFrame
		{
			get
			{
				return (uint)this.Width * (uint)this.Height;
			}
		}

		public object Clone()
		{
			return this.MemberwiseClone();
		}
	}
}
