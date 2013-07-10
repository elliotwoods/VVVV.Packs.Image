using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Timers;
using DeckLinkAPI;

namespace VVVV.Nodes.DeckLink
{
	class Source : IDeckLinkVideoOutputCallback, IDisposable
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
		public ModeRegister.Mode Mode {get; private set;}

		int FFrameIndex = 0;

		IDeckLinkMemoryAllocator FMemoryAllocator = new MemoryAllocator();
		IDeckLinkMutableVideoFrame FVideoFrame;

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

		public Source(IDeckLink device, ModeRegister.Mode mode)
		{
			this.Initialise(device, mode);
		}

		public void Initialise(IDeckLink device, ModeRegister.Mode mode)
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
					_BMDDisplayModeSupport support;
					IDeckLinkDisplayMode displayMode;
					FOutputDevice.DoesSupportVideoMode(mode.DisplayModeHandle.GetDisplayMode(), mode.PixelFormat, mode.Flags, out support, out displayMode);
					if (support == _BMDDisplayModeSupport.bmdDisplayModeNotSupported)
						throw (new Exception("Mode not supported"));

					this.Mode = mode;
					this.FWidth = Mode.Width;
					this.FHeight = Mode.Height;

					Mode.DisplayModeHandle.GetFrameRate(out this.FFrameDuration, out this.FFrameTimescale);
					//
					//--


					//--
					//enable the output
					//
					FOutputDevice.EnableVideoOutput(Mode.DisplayModeHandle.GetDisplayMode(), Mode.Flags);
					//
					//--


					//--
					//generate frames
					IntPtr data;
					FOutputDevice.CreateVideoFrame(FWidth, FHeight, Mode.CompressedWidth * 4, Mode.PixelFormat, _BMDFrameFlags.bmdFrameFlagDefault, out FVideoFrame);
					FVideoFrame.GetBytes(out data);
					FillBlack(data);
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
					//initiate video feed
					FOutputDevice.StartScheduledPlayback(0, 100, 1.0);
					//FRunThread.Start();
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

		//void ThreadedTimedFunction()
		//{
		//	Stopwatch Timer = new Stopwatch();
		//	Timer.Start();

		//	int intervalMillis = (int) (1.0 / Framerate * 1000.0);
		//	TimeSpan frameInterval = new TimeSpan(0, 0, 0, intervalMillis / 1000, intervalMillis % 1000);

		//	TimeSpan sleepDuration = new TimeSpan(10);

		//	var lastFrame = Timer.Elapsed;
		//	while (FRunning)
		//	{
		//		var currentFrame = Timer.Elapsed;
		//		if (currentFrame >= lastFrame + frameInterval)
		//		{
		//			IntPtr data;
		//			FVideoFrame.GetBytes(out data);
		//			OnNewFrame(data);
		//			FOutputDevice.DisplayVideoFrameSync(FVideoFrame);
		//			lastFrame = currentFrame;
		//			FFrameIndex++;
		//		}
		//		else
		//		{
		//			Thread.Sleep(sleepDuration);
		//		}
		//	}
		//}


		public void Stop()
		{
			if (!FRunning)
				return;

			//stop new frames from being scheduled
			FRunning = false;

			//FRunThread.Join();
			//FRunThread = null;

			long unused;
			WorkerThread.Singleton.PerformBlocking(() =>
			{
				FOutputDevice.StopScheduledPlayback(0, out unused, 100);
				FOutputDevice.DisableVideoOutput();
			});

			Marshal.ReleaseComObject(FVideoFrame);
		}

		void FillBlack(IntPtr data)
		{
			MemSet(data, 0, (IntPtr)(Mode.CompressedWidth * Mode.Height * 4));
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


			long displayTime = FFrameIndex * FFrameDuration;
			FOutputDevice.ScheduleVideoFrame(FVideoFrame, displayTime, FFrameDuration, FFrameTimescale);
			FFrameIndex++;
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
