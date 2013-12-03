using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace VVVV.CV.Core
{
	/// <summary>
	/// An object instantiated inside an IGeneratorInstance or IFilterInstance
	/// There is a local image instance 'Image', you should initialise this image and send it using Send()
	/// </summary>
	public class CVImageOutput : IDisposable
	{
		CVImageLink FLink = new CVImageLink();
		public CVImageLink Link { get { return FLink; } }

		public CVImage Image = new CVImage();

		/// <summary>
		/// Sends the internal image
		/// </summary>
		public void Send()
		{
			Link.Send(Image);
		}

		/// <summary>
		/// Sends an image to the link, ignoring the internal buffer
		/// </summary>
		public void Send(CVImage image)
		{
			Link.Send(image);
		}

		public IntPtr Data
		{
			get
			{
				return Image.Data;
			}
		}

		public IntPtr CvMat
		{
			get
			{
				return Image.CvMat;
			}
		}

		public void Dispose()
		{
			FLink.Dispose();
		}
	}
}
