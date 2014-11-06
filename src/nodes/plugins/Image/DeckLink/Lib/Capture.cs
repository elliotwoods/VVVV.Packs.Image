using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeckLinkAPI;
using System.Threading;
using System.Runtime.InteropServices;

// first reference : http://stackoverflow.com/questions/6355930/blackmagic-sdk-in-c-sharp

namespace VVVV.Nodes.DeckLink
{
	class Capture : IDisposable, IDeckLinkInputCallback
	{
		IDeckLinkInput FDevice;
		IDeckLinkOutput FOutDevice;
		_BMDDisplayMode FMode;
		_BMDVideoInputFlags FFlags;
		IDeckLinkDisplayMode FDisplayMode;
		_BMDPixelFormat FPixelFormat = _BMDPixelFormat.bmdFormat8BitYUV;
		IntPtr FData;
		IDeckLinkVideoConversion FConverter;
		IDeckLinkMutableVideoFrame rgbFrame;
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

		public void Open(DeviceRegister.DeviceIndex device, _BMDDisplayMode mode, _BMDVideoInputFlags flags)
		{
			if (Ready)
				Close();

			WorkerThread.Singleton.PerformBlocking(() =>
			{
				this.Lock.AcquireWriterLock(10000);
				try
				{
					if (device == null)
						throw (new Exception("No device selected"));

					IDeckLink rawDevice = DeviceRegister.Singleton.GetDeviceHandle(device.Index);
					FDevice = rawDevice as IDeckLinkInput;
					FOutDevice = rawDevice as IDeckLinkOutput;
					FMode = mode;
					FFlags = flags;
					FConverter = new CDeckLinkVideoConversion();

					if (FDevice == null)
						throw (new Exception("No input device connected"));

					_BMDDisplayModeSupport displayModeSupported;

					FDevice.DoesSupportVideoMode(FMode, FPixelFormat, flags, out displayModeSupported, out FDisplayMode);

					Width = FDisplayMode.GetWidth();
					Height = FDisplayMode.GetHeight();

					// inspiration http://dviz.googlecode.com/svn/trunk/src/livemix/CameraThread.cpp

					FOutDevice.CreateVideoFrame(Width,
												Height,
												Width * 4,
												_BMDPixelFormat.bmdFormat8BitBGRA,
												_BMDFrameFlags.bmdFrameFlagDefault,
												out rgbFrame);

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
					throw;
				}
				finally
				{
					this.Lock.ReleaseWriterLock();
				}
			});
		}

		public void Close()
		{
			WorkerThread.Singleton.PerformBlocking(() =>
			{
				this.Lock.AcquireWriterLock(10000);
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
			});

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

				FConverter.ConvertFrame(videoFrame, rgbFrame);

				rgbFrame.GetBytes(out FData);

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
				return Width * Height * 4;
			}
		}

		public int AvailableFrameCount
		{
			get
			{
				if (!Ready)
					return 0;

				uint count = 0;
				WorkerThread.Singleton.PerformBlocking(() =>
				{
					FDevice.GetAvailableVideoFrameCount(out count);
				});
				return (int)count;
			}
		}

		public void Flush()
		{
			if (!Ready)
				return;
			WorkerThread.Singleton.PerformBlocking(() =>
			{
				FDevice.FlushStreams();
			});
		}

		public void Dispose()
		{
			Close();
		}
	}
}
