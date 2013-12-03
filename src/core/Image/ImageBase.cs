using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using System.Runtime.InteropServices;

namespace VVVV.CV.Core
{
	public abstract class ImageBase
	{
		[DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
		static extern private void CopyMemory(IntPtr Destination, IntPtr Source, uint Length);

		protected CVImageAttributes FImageAttributes = new CVImageAttributes();

		public bool Allocated
		{
			get
			{
				return GetImage() != null && Width > 0 && Height > 0;
			}
		}

		abstract public IImage GetImage();

		public CVImageAttributes ImageAttributes
		{
			get
			{
				return FImageAttributes;
			}
		}

		public TColorFormat NativeFormat
		{
			get
			{
				return ImageAttributes.ColorFormat;
			}
		}

		abstract public void Allocate();

		public int Width
		{
			get
			{
				return FImageAttributes.Width;
			}
		}

		public int Height
		{
			get
			{
				return FImageAttributes.Height;
			}
		}

		public System.Drawing.Size Size
		{
			get
			{
				if (GetImage() == null)
					return new System.Drawing.Size(0,0);
				else
					return GetImage().Size;
			}
		}

		public IntPtr CvMat
		{
			get
			{
				return GetImage().Ptr;
			}
		}

		/// <summary>
		/// Returns a pointer to the raw pixel data
		/// </summary>
		public IntPtr Data
		{
			get
			{
				if (!this.Allocated)
				{
					throw (new Exception("Image not allocated, can't access internal data"));
				}
				IntPtr value;

				int step;
				System.Drawing.Size dims;

				CvInvoke.cvGetRawData(CvMat, out value, out step, out dims);

				return value;
			}
		}
	}
}
