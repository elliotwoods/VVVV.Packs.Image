using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeckLinkAPI;
using System.Threading;
using System.Runtime.InteropServices;

namespace VVVV.Nodes.EDSDK
{
	class Capture : IDisposable, IDeckLinkInputCallback
	{
		IDeckLinkInput FDevice;
		_BMDDisplayMode FMode;
		_BMDVideoInputFlags FFlags;
		IDeckLinkDisplayMode FDisplayMode;
		_BMDPixelFormat FPixelFormat = _BMDPixelFormat.bmdFormat8BitYUV;
		IntPtr FData;
		public bool Reinitialise { get; private set;}
		public bool FreshData { get; private set; }
		public int Width { get; private set; }
		public int Height { get; private set; }
		public ReaderWriterLock Lock = new ReaderWriterLock();
		public bool Ready { get; private set; }

		public Capture()
		{
			Reinitialise = false;
			FreshData = false;
			Width = 0;
			Height = 0;
			Ready = false;
		}

		public void Open(IDeckLink device, _BMDDisplayMode mode, _BMDVideoInputFlags flags)
		{
			if (Ready)
				Close();

			try
			{
				this.Lock.AcquireWriterLock(10000);
			}
			catch
			{

			}
			try
			{
				FDevice = device as IDeckLinkInput;
				FMode = mode;
				FFlags = flags;

				if (FDevice == null)
					throw (new Exception("No input device connected"));

				_BMDDisplayModeSupport displayModeSupported;

				FDevice.DoesSupportVideoMode(FMode, FPixelFormat, flags, out displayModeSupported, out FDisplayMode);

				Width = FDisplayMode.GetWidth();
				Height = FDisplayMode.GetHeight();

				FDevice.EnableVideoInput(FMode, FPixelFormat, FFlags);
				FDevice.SetCallback(this);
				FDevice.StartStreams();

				Reinitialise = true;
				Ready = true;
				FreshData = false;
			}
			catch (Exception e)
			{
				Ready = false;
				Reinitialise = false;
				FreshData = false;
				throw (e);
			}
			finally
			{
				this.Lock.ReleaseWriterLock();
			}
		}

		public void Close()
		{
			try
			{
				this.Lock.AcquireWriterLock(10000);
			}
			catch
			{

			}
			try
			{
				if (!Ready)
					return;

				Ready = false;
				FDevice.StopStreams();
				FDevice.DisableVideoInput();
			}
			finally
			{
				this.Lock.ReleaseWriterLock();
			}
		}

		public void Dispose()
		{
			Close();
		}

		public void VideoInputFormatChanged(_BMDVideoInputFormatChangedEvents notificationEvents, IDeckLinkDisplayMode newDisplayMode, _BMDDetectedVideoInputFormatFlags detectedSignalFlags)
		{
			Reinitialise = true;
		}

		public void VideoInputFrameArrived(IDeckLinkVideoInputFrame videoFrame, IDeckLinkAudioInputPacket audioPacket)
		{
			this.Lock.AcquireWriterLock(5000);
			try
			{
				videoFrame.GetBytes(out FData);
				System.Runtime.InteropServices.Marshal.ReleaseComObject(videoFrame);
				FreshData = true;
			}
			catch
			{

			}
			finally
			{
				this.Lock.ReleaseWriterLock();
			}
		}

		public void Reinitialised()
		{
			this.Reinitialise = false;
		}

		public void Updated()
		{
			this.FreshData = false;
		}

		public IntPtr Data
		{
			get
			{
				return FData;
			}
		}

		public int BytesPerFrame
		{
			get
			{
				return Width / 2 * 4 * Height;
			}
		}

		public int AvailableFrameCount
		{
			get
			{
				if (!Ready)
					return 0;

				uint count;
				FDevice.GetAvailableVideoFrameCount(out count);
				return (int)count;
			}
		}

		public void Flush()
		{
			if (!Ready)
				return;

			FDevice.FlushStreams();
		}
	}
}
