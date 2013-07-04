using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DeckLinkAPI;

namespace VVVV.Nodes.DeckLink
{
	class Source : IDeckLinkVideoOutputCallback, IDeckLinkAudioOutputCallback, IDisposable
	{
		[DllImport("msvcrt.dll", EntryPoint = "memset", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
		public static extern IntPtr MemSet(IntPtr dest, int c, IntPtr count);

		class MemoryAllocator : IDeckLinkMemoryAllocator
		{
			public void AllocateBuffer(uint bufferSize, out IntPtr allocatedBuffer)
			{
				allocatedBuffer = Marshal.AllocCoTaskMem( (int) bufferSize);
			}

			public void Commit()
			{
			}

			public void Decommit()
			{
			}

			public void ReleaseBuffer(IntPtr buffer)
			{
				Marshal.FreeCoTaskMem(buffer);
			}
		}

		IDeckLink FDevice;
		IDeckLinkOutput FOutputDevice;
		IDeckLinkDisplayMode FMode;

		int FFrameIndex = 0;

		IDeckLinkMemoryAllocator FMemoryAllocator = new MemoryAllocator();
		IDeckLinkMutableVideoFrame FVideoFrame;

		IntPtr FAudioBuffer;
		uint FAudioBufferOffset;
		uint FAudioBufferSampleLength;
		uint FAudioChannelCount = 2;
		_BMDAudioSampleRate FAudioSampleRate = _BMDAudioSampleRate.bmdAudioSampleRate48kHz;

		int FWidth = 0;
		public int Width
		{
			get
			{
				return FWidth;
			}
		}

		int FHeight = 0;
		public int Height
		{
			get
			{
				return FHeight;
			}
		}

		const int BytesPerPixel = 4;
		const _BMDPixelFormat PixelFormat = _BMDPixelFormat.bmdFormat8BitARGB;

		bool FRunning = false;
		public bool Running
		{
			get
			{
				return FRunning;
			}
		}

		long FFrameDuration = 0;
		long FFrameTimescale = 0;
		public double Framerate
		{
			get
			{
				return (double)FFrameTimescale / (double)FFrameDuration;
			}
		}

		public delegate void FrameServeHandler(IntPtr data);
		public event FrameServeHandler NewFrame;
		void OnNewFrame(IntPtr data)
		{
			if (NewFrame != null)
				NewFrame(data);
		}

		public Source(IDeckLink device, IDeckLinkDisplayMode mode)
		{
			this.Initialise(device, mode);
		}

		public void Initialise(IDeckLink device, IDeckLinkDisplayMode mode)
		{
			Stop();

			try
			{
				WorkerThread.Singleton.PerformBlocking(() => {

					//--
					//attach to device
					//
					if (device == null)
						throw (new Exception("No device"));
					if (mode == null)
						throw (new Exception("No mode selected"));

					FDevice = device;

					var outputDevice = FDevice as IDeckLinkOutput;
					if (outputDevice == null)
						throw (new Exception("Device does not support output"));
					FOutputDevice = outputDevice;

					FOutputDevice.SetScheduledFrameCompletionCallback(this);
					FOutputDevice.SetAudioCallback(this);
					//
					//--


					//--
					//set memory allocator
					//
					FOutputDevice.SetVideoOutputFrameMemoryAllocator(FMemoryAllocator);
					//
					//--


					//--
					//select mode
					//
					var flags = _BMDVideoOutputFlags.bmdVideoOutputFlagDefault;
					_BMDDisplayModeSupport support;
					IDeckLinkDisplayMode displayMode;
					FOutputDevice.DoesSupportVideoMode(mode.GetDisplayMode(), PixelFormat, flags, out support, out displayMode);
					if (support == _BMDDisplayModeSupport.bmdDisplayModeNotSupported)
						throw (new Exception("Mode not supported"));

					this.FMode = mode;
					this.FWidth = FMode.GetWidth();
					this.FHeight = FMode.GetHeight();

					FMode.GetFrameRate(out this.FFrameDuration, out this.FFrameTimescale);
					//
					//--


					//--
					//enable the outputs
					//
					FOutputDevice.EnableVideoOutput(FMode.GetDisplayMode(), flags);
					FOutputDevice.EnableAudioOutput(FAudioSampleRate, _BMDAudioSampleType.bmdAudioSampleType16bitInteger, FAudioChannelCount, _BMDAudioOutputStreamType.bmdAudioOutputStreamContinuous);
					//
					//--


					//--
					//generate one second of blank audio
					//
					this.FAudioBufferSampleLength = (uint)FAudioSampleRate;
					this.FAudioBuffer = Marshal.AllocCoTaskMem((int)(FAudioBufferSampleLength * FAudioChannelCount * (16 / 8)));
					MemSet(FAudioBuffer, 0, (IntPtr) FAudioBufferSampleLength);
					//
					//--


					//--
					//generate frames
					IntPtr data;
					FOutputDevice.CreateVideoFrame(FWidth, FHeight, FWidth * BytesPerPixel, PixelFormat, _BMDFrameFlags.bmdFrameFlagDefault, out FVideoFrame);
					FVideoFrame.GetBytes(out data);
					FillBlack(data);

					//GenerateTestPatterns();
					//
					//--


					//--
					//give one second of video preroll
					this.FFrameIndex = 0;
					for (int i = 0; i < (int)this.Framerate; i++)
					{
						ScheduleFrame(true);
					}
					//
					//--


					//--
					//give one second of audio preroll
					this.FAudioBufferOffset = 0;
					FOutputDevice.BeginAudioPreroll();
					//
					//--

					FRunning = true;
				});
			}
			catch (Exception e)
			{
				this.FWidth = 0;
				this.FHeight = 0;
				this.FRunning = false;
				throw;
			}
		}

		public void Stop()
		{
			if (!FRunning)
				return;

			//stop new frames from being scheduled
			FRunning = false;

			long unused;
			WorkerThread.Singleton.PerformBlocking(() =>
			{
				FOutputDevice.StopScheduledPlayback(0, out unused, 100);
				FOutputDevice.DisableAudioOutput();
				FOutputDevice.DisableVideoOutput();
			});

			Marshal.ReleaseComObject(FVideoFrame);
			Marshal.FreeCoTaskMem(FAudioBuffer);
		}

		void FillBlack(IntPtr data)
		{
			MemSet(data, 0, (IntPtr)(FWidth * FHeight * BytesPerPixel));
		}

		void ScheduleFrame(bool preRoll)
		{
			if (!preRoll && !FRunning)
			{
				return;
			}

			IntPtr data;
			FVideoFrame.GetBytes(out data);
			OnNewFrame(data);

			int frameRate = (int)this.Framerate;
			long displayTime = FFrameIndex * FFrameDuration;
			FOutputDevice.ScheduleVideoFrame(FVideoFrame, displayTime, FFrameDuration, FFrameTimescale);

			FFrameIndex++;
		}

		public void RenderAudioSamples(int preroll)
		{
			if (preroll != 0)
			{
				FOutputDevice.StartScheduledPlayback(0, 100, 1.0);
			}
		}

		public void ScheduledFrameCompleted(IDeckLinkVideoFrame completedFrame, _BMDOutputFrameCompletionResult result)
		{
			ScheduleFrame(false);
		}

		public void ScheduledPlaybackHasStopped()
		{
		}

		public void Dispose()
		{
			Stop();
		}
	}
}
