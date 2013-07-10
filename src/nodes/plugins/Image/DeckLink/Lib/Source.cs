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

		uint FFramesInBuffer = 0;
		public int FramesInBuffer
		{
			get
			{
				return (int) FFramesInBuffer;
			}
		}

		public delegate void FrameServeHandler(IntPtr data);
		/// <summary>
		/// Callback for scheduled playback
		/// </summary>
		public event FrameServeHandler NewFrame;
		void OnNewFrame(IntPtr data)
		{
			if (NewFrame != null)
				NewFrame(data);
		}


		public Source(IDeckLink device, ModeRegister.Mode mode, bool useDeviceCallbacks)
		{
			this.Initialise(device, mode, useDeviceCallbacks);
		}

		public void Initialise(IDeckLink device, ModeRegister.Mode mode, bool useDeviceCallbacks)
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
					//scheduled playback
					if (useDeviceCallbacks == true)
					{
						FOutputDevice.SetScheduledFrameCompletionCallback(this);
						this.FFrameIndex = 0;
						for (int i = 0; i < (int)this.Framerate; i++)
						{
							ScheduleFrame(true);
						}
						FOutputDevice.StartScheduledPlayback(0, 100, 1.0);
					}
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

			WorkerThread.Singleton.PerformBlocking(() =>
			{
				int scheduledPlayback;
				FOutputDevice.IsScheduledPlaybackRunning(out scheduledPlayback);
				if (scheduledPlayback != 0)
				{
					long unused;
					FOutputDevice.StopScheduledPlayback(0, out unused, 1000);
				}
				FOutputDevice.DisableVideoOutput();
			});

			Marshal.ReleaseComObject(FVideoFrame);
		}

		void FillBlack(IntPtr data)
		{
			MemSet(data, 0, (IntPtr)(Mode.CompressedWidth * Mode.Height * 4));
		}

		public Object LockBuffer = new Object();

		public void SendFrame(byte[] buffer)
		{
			lock (LockBuffer)
			{
				IntPtr outBuffer = IntPtr.Zero;
				WorkerThread.Singleton.PerformBlocking(() =>
				{
					FVideoFrame.GetBytes(out outBuffer);
				});
				Marshal.Copy(buffer, 0, outBuffer, buffer.Length);
			}
			WorkerThread.Singleton.Perform(() =>
			{
				FOutputDevice.DisplayVideoFrameSync(FVideoFrame);
			});
		}

		public void Dispose()
		{
			Stop();
		}

		public void ScheduledFrameCompleted(IDeckLinkVideoFrame completedFrame, _BMDOutputFrameCompletionResult result)
		{
			ScheduleFrame(false);
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

		public void ScheduledPlaybackHasStopped()
		{
		}
	}
}
