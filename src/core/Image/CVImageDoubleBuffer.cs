using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using System.Threading;
using System.Drawing;

namespace VVVV.CV.Core
{
	public class CVImageDoubleBuffer : IDisposable
	{
		#region Data
		private CVImage FBackBuffer = new CVImage();
		private CVImage FFrontBuffer = new CVImage();
		private CVImageAttributes FImageAttributes;
		private bool FAllocated = false;
		#endregion

		#region Locking
		private ReaderWriterLock FFrontLock = new ReaderWriterLock();
		private Object FBackLock = new Object();
		private Object FAttributesLock = new Object();
		public static int LockTimeout = 500;

		public bool LockForReading()
		{
			try
			{
				FFrontLock.AcquireReaderLock(CVImageDoubleBuffer.LockTimeout);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public Object BackLock
		{
			get
			{
				return FBackLock;
			}
		}

		public void ReleaseForReading()
		{
			try
			{
				FFrontLock.ReleaseReaderLock();
			}
			catch
			{

			}
		}

		public bool CheckReaderLock()
		{
			return FFrontLock.IsReaderLockHeld;
		}
		#endregion

		#region Events
		//these virtual functions are overwriten in child classes that want to trigger events
		public virtual void OnImageUpdate() { }
		public virtual void OnImageAttributesUpdate(CVImageAttributes attributes) { }
		#endregion

		#region Swapping and copying
		/// <summary>
		/// Swap the front buffer and back buffer
		/// </summary>
		public void Swap()
		{
			FAllocated = true;

			lock (FBackLock)
			{
				FFrontLock.AcquireWriterLock(LockTimeout);
				try
				{
					CVImage swap = FBackBuffer;
					FBackBuffer = FFrontBuffer;
					FFrontBuffer = swap;
				}
				finally
				{
					FFrontLock.ReleaseWriterLock();
				}
			}

			OnImageUpdate();
		}
		#endregion

		#region Get/set the image
		public void GetImage(CVImage target)
		{
			LockForReading();
			try
			{
				FFrontBuffer.GetImage(target);
			}
			finally
			{
				ReleaseForReading();
			}
		}

		/// <summary>
		/// Copy the input image into the back buffer
		/// </summary>
		/// <param name="source">Input image</param>
		public void Send(CVImage source)
		{
			bool Reinitialise;

			lock (FBackLock)
				Reinitialise = FBackBuffer.SetImage(source);

			if (Reinitialise)
				InitialiseFrontFromBack();

			Swap();
		}

		/// <summary>
		/// Copy the input image into the back buffer
		/// </summary>
		/// <param name="source">Input image</param>
		public void Send(IImage source)
		{
			bool Reinitialise;

			lock (FBackLock)
				Reinitialise = FBackBuffer.SetImage(source);

			if (Reinitialise)
			{
				InitialiseFrontFromBack();
			}

			Swap();
		}

		public void Send(Bitmap source)
		{
			bool Reinitialise;

			lock (FBackLock)
				Reinitialise = FBackBuffer.SetImage(source);

			if (Reinitialise)
			{
				InitialiseFrontFromBack();
			}

			Swap();
		}

		public void Initialise(CVImageAttributes attributes)
		{
			FImageAttributes = attributes;

			InitialiseBack();
			InitialiseFrontFromBack();
		}

		void InitialiseBack()
		{
			lock (FBackLock)
			{
				FBackBuffer.Initialise(FImageAttributes);
			}
		}

		void InitialiseFrontFromBack()
		{
			lock (FBackLock)
				lock (FAttributesLock)
				{
					FImageAttributes = FBackBuffer.ImageAttributes;

					FFrontLock.AcquireWriterLock(LockTimeout);
					try
					{
						FFrontBuffer.SetImage(FBackBuffer);
					}
					finally
					{
						FFrontLock.ReleaseWriterLock();
					}
				}

			OnImageAttributesUpdate(FImageAttributes);
		}
		#endregion

		#region Accessors
		/// <summary>
		/// Get the front buffer. Be sure to lock the front buffer for reading!
		/// </summary>
		public CVImage FrontImage
		{
			get { return FFrontBuffer; }
		}

		/// <summary>
		/// Get the back buffer. Be sure to lock the back buffer for writing!
		/// </summary>
		public CVImage BackImage
		{
			get { return FBackBuffer; }
		}

		public CVImageAttributes ImageAttributes
		{
			get {
				lock (FAttributesLock)
					return FImageAttributes;
			}
		}

		public bool Allocated
		{
			get
			{
				return FFrontBuffer.Allocated;
			}
		}

		public ReaderWriterLock FrontLock
		{
			get
			{
				return FFrontLock;
			}
		}
		#endregion

		public void Dispose()
		{
			lock (FBackLock)
			{
				FFrontLock.AcquireWriterLock(100);

				FFrontBuffer.Dispose();
				FBackBuffer.Dispose();

				FFrontLock.ReleaseWriterLock();
			}
		}
	}
}
