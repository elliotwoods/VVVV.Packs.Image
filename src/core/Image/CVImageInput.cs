using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.CV.Core
{
	public class CVImageInput : IDisposable
	{
		private CVImageLink FLink = null;
		private bool FImageFresh = false;
		private bool FImageAttributesFresh = false;

		#region Destructor
		~CVImageInput()
		{
			Disconnect();
		}
		#endregion

		#region Events
		#region ImageUpdate
		public event EventHandler ImageUpdate;

		protected void OnImageUpdate()
		{
			if (ImageUpdate == null)
				return;
			ImageUpdate(this, EventArgs.Empty);
		}
		#endregion

		#region ImageAttributesUpdate
		public event EventHandler<ImageAttributesChangedEventArgs> ImageAttributesUpdate;

		protected void OnImageAttributesUpdate(CVImageAttributes attributes)
		{
			if (ImageAttributesUpdate == null)
				return;
			ImageAttributesUpdate(this, new ImageAttributesChangedEventArgs(attributes));
		}
		#endregion

		private void AddListeners()
		{
			FLink.ImageUpdate += ImageUpdated;
			FLink.ImageAttributesUpdate += ImageAttributesUpdated;
		}

		private void RemoveListeners()
		{
			if (Connected)
			{
				FLink.ImageUpdate -= ImageUpdated;
				FLink.ImageAttributesUpdate -= ImageAttributesUpdated;
			}
		}

		private void ImageUpdated(object sender, EventArgs e)
		{
			FImageFresh = true;
			OnImageUpdate();
		}

		private void ImageAttributesUpdated(object sender, ImageAttributesChangedEventArgs e)
		{
			FImageAttributesFresh = true;
			OnImageAttributesUpdate(FLink.ImageAttributes);
		}
		#endregion

		#region Accessors
		public CVImageLink Link
		{
			get
			{
				return FLink;
			}
		}

		public CVImageAttributes ImageAttributes
		{
			get
			{
				return FLink.ImageAttributes;
			}
		}

		public CVImage Image
		{
			get
			{
				return FLink.FrontImage;
			}
		}

		public void GetImage(CVImage target)
		{
			FLink.GetImage(target);
		}

		public void GetImage(CVImageDoubleBuffer target)
		{
			lock(target.BackLock)
			{
				FLink.GetImage(target.BackImage);
			}
		}

		public bool Allocated
		{
			get
			{
				if (FLink == null)
					return false;

				return FLink.Allocated;
			}
		}

		public bool ImageChanged
		{
			get
			{
				return FImageFresh;
			}
		}
		public void ClearImageChanged()
		{
			FImageFresh = false;
		}

		public bool ImageAttributesChanged
		{
			get
			{
				if (!Allocated)
					return false;

				return FImageAttributesFresh;
			}
			set
			{
				FImageAttributesFresh = value;
			}
		}
		public void ClearImageAttributesChanged()
		{
			FImageAttributesFresh = false;
		}


		/// <summary>
		/// Returns a pointer to raw pixels
		/// </summary>
		public IntPtr Data
		{
			get
			{
				return FLink.FrontImage.Data;
			}
		}

		/// <summary>
		/// Returns a pointer to OpenCV object
		/// </summary>
		public IntPtr CvMat
		{
			get
			{
				if (!FLink.CheckReaderLock())
					throw new Exception("Image read must be locked to this thread before operations can be performed on CvMat");
				return FLink.FrontImage.CvMat;
			}
		}

		public uint BytesPerFrame
		{
			get
			{
				return FLink.ImageAttributes.BytesPerFrame;
			}
		}
		#endregion

		#region Connection
		public void Connect(CVImageLink input)
		{
			Disconnect();

			FLink = input;

			if (FLink.Allocated)
			{
				FImageAttributesFresh = true;
				FImageFresh = true;
			}

			AddListeners();
		}

		public void Disconnect()
		{
			RemoveListeners();
			FLink = null;
		}

		public bool Connected
		{
			get
			{
				return FLink != null;
			}
		}

		public bool ConnectedTo(CVImageLink input)
		{
			return (input == FLink);
		}
		#endregion

		#region Locking
		public bool LockForReading()
		{
			if (FLink == null)
				return false;

			return FLink.LockForReading();
		}

		public void ReleaseForReading()
		{
			if (FLink != null)
				FLink.ReleaseForReading();
		}
		#endregion

		public void Dispose()
		{
			Disconnect();
		}
	}
}
