using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using FeralTic.DX11;
using FeralTic.DX11.Resources;
using VVVV.Core.Logging;
using VVVV.DX11;
using VVVV.DX11.Lib;
using VVVV.CV.Core.ThreadUtils;
using xiApi.NET;

namespace VVVV.Nodes.Ximea
{
	class Device : IDisposable
	{
		public enum IntParameter
		{
			AEAG,
			Exposure,
			Gain,
			RegionWidth,
			RegionHeight,
			RegionX,
			RegionY
		}

		public class Specification
		{
			public Specification()
			{
			}

			public Specification(int Width, int Height, string Name, string Type, string Serial)
			{
				this.Width = Width;
				this.Height = Height;
				this.Name = Name;
				this.Type = Type;
				this.Serial = Serial;
			}

			public int Width;
			public int Height;

			public string Name;
			public string Type;
			public string Serial;
		}

		class DoubleBuffer
		{
			public byte[] Front;
			public byte[] Back;

			public ReaderWriterLock LockFront = new ReaderWriterLock();

			public void Swap()
			{
				LockFront.AcquireWriterLock(1000);

				try
				{
					var temp = Front;
					Front = Back;
					Back = temp;
				}
				catch
				{
				}
				finally
				{
					LockFront.ReleaseWriterLock();
				}
			}
		}

		public Device()
		{
			FThread = new WorkerThread();
			FThread.Name = "Ximea Thread";
			FThread.ThreadWait += FThread_ThreadWait;
		}

		[Import()]
		ILogger FLogger;

		DoubleBuffer FDoubleBuffer = new DoubleBuffer();
		public int Timeout = 500;
		public int FFrameWidth = 0;
		public int FFrameHeight = 0;
		public int FFrameIndex = -1;
		public double FTimestamp = 0;
		public double FFramerate = 0;
		bool FDataNewInternal = false;
		bool FDataNewPublished = false;
		bool FDataNewForTexture = false;

		Specification FSpecification = new Specification();
		public Specification DeviceSpecification
		{
			get
			{
				return FSpecification;
			}
		}

		WorkerThread FThread;
		xiCam FDevice = new xiCam();

		int FDeviceID = -1;
		public int DeviceID
		{
			set
			{
				if (FDeviceID == value)
					return;

				FDeviceID = value;

				if (this.Running)
				{
					if (FDeviceID >= 0 && FDeviceID < (new xiCam()).GetNumberDevices())
					{
						Start();
					}
					else
					{
						Stop();
					}
				}
			}
			get
			{
				return FDeviceID;
			}
		}

		ITrigger FTrigger = null;
		public ITrigger Trigger
		{
			set
			{
				if (value == FTrigger)
					return;


				//--
				//if we had a software trigger, detach event
				//
				var oldSoftwareTrigger = FTrigger as ISoftwareTrigger;
				if (oldSoftwareTrigger != null)
				{
					oldSoftwareTrigger.Trigger -= SoftwareTrigger_Trigger;
				}
				//
				//--


				FTrigger = value;
				

				//--
				//if new trigger is software, attach event
				//
				if (FTrigger != null && FTrigger.GetTriggerType() == TriggerType.Software)
				{
					var SoftwareTrigger = FTrigger as ISoftwareTrigger;
					SoftwareTrigger.Trigger += SoftwareTrigger_Trigger;
				}
				//
				//--


				if (this.Running)
					Start(); //restart
			}
		}

		void SoftwareTrigger_Trigger(object sender, EventArgs e)
		{
			if (Running && FTrigger == sender)
			{
				FThread.Perform(() =>
				{
					FDevice.SetParam(PRM.TRG_SOFTWARE, 0);
				});
			}
		}

		public bool Enabled
		{
			set
			{
				if (this.Running && !value)
					Stop();
				if (!this.Running && value)
					Start();
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

		public double Timestamp
		{
			get
			{
				return FTimestamp;
			}
		}

		public double Framerate
		{
			get
			{
				return FFramerate;
			}
		}

		public bool DataNew
		{
			get
			{
				return FDataNewPublished;
			}
		}

		void Start()
		{
			Stop();

			if (Count == 0)
			{
				throw (new Exception("No device found"));
			}

			FThread.PerformBlocking(() =>
			{
				FDevice.OpenDevice(FDeviceID);
				FSpecification.Width = FDevice.GetParamInt(PRM.WIDTH);
				FSpecification.Height = FDevice.GetParamInt(PRM.HEIGHT);
				FSpecification.Name = FDevice.GetParamString(PRM.DEVICE_NAME);
				FSpecification.Type = FDevice.GetParamString(PRM.DEVICE_TYPE);
				FSpecification.Serial = FDevice.GetParamString(PRM.DEVICE_SN);
				if (FTrigger == null)
				{
					FDevice.SetParam(PRM.TRG_SOURCE, TRG_SOURCE.OFF);
				}
				else
				{
					switch (FTrigger.GetTriggerType())
					{
						case TriggerType.Software:
							FDevice.SetParam(PRM.TRG_SOURCE, TRG_SOURCE.SOFTWARE);
							break;
						case TriggerType.GPI:
							HardwareTrigger hardwareTrigger = FTrigger as HardwareTrigger;
							if (hardwareTrigger == null)
							{
								throw (new Exception("Hardware trigger not properly initialised"));
							}
							FDevice.SetParam(PRM.GPI_SELECTOR, 1);
							FDevice.SetParam(PRM.GPI_MODE, GPI_MODE.TRIGGER);
							switch(hardwareTrigger.Source)
							{
								case HardwareTrigger.HardwareEvent.RisingEdge:
									FDevice.SetParam(PRM.TRG_SOURCE, TRG_SOURCE.EDGE_RISING);
									break;
								case HardwareTrigger.HardwareEvent.FallingEdge:
									FDevice.SetParam(PRM.TRG_SOURCE, TRG_SOURCE.EDGE_FALLING);
									break;
								case HardwareTrigger.HardwareEvent.WhilstHigh:
									throw(new Exception("WhilstHigh is not implemented"));
							}

							break;
						default:
							throw (new Exception("Trigger type not handled"));
					}
				}
				FDevice.StartAcquisition();
				FRunning = true;
			});
		}

		void Stop()
		{
			if (FThread == null)
				return;

			FRunning = false;
			FThread.PerformBlocking(() =>
			{
				FDevice.CloseDevice();
			});
		}

		void FThread_ThreadWait(object sender, EventArgs e)
		{
			if (FRunning)
			{
				int width = FDevice.GetParamInt(PRM.WIDTH);
				int height = FDevice.GetParamInt(PRM.HEIGHT);
				int size = width * height;

				if (FDoubleBuffer.Back == null || FDoubleBuffer.Back.Length != size)
				{
					FDoubleBuffer.Back = new byte[size];
				}

				FDevice.GetImage(FDoubleBuffer.Back, Timeout);
						
				var imageParams = FDevice.GetLastImageParams();
				FFrameIndex = imageParams.GetFrameNum();

				var timestamp = imageParams.GetTimestamp();
				FFramerate = 1.0 / (timestamp - FTimestamp);
				FTimestamp = timestamp;

				FDataNewInternal = true;
				FDataNewForTexture = true;
				FFrameWidth = width;
				FFrameHeight = height;
				FDoubleBuffer.Swap();
			}
		}

		public void Update(DX11Resource<DX11DynamicTexture2D> textureSlice, DX11RenderContext context)
		{
			if (!this.Running || !FDataNewForTexture)
			{
				return;
			}
			FDataNewForTexture = false;

			DX11DynamicTexture2D tex;

			//create texture if necessary
			//should also check if properties (width,height) changed
			if (!textureSlice.Contains(context))
			{
				tex = new DX11DynamicTexture2D(context, FFrameWidth, FFrameHeight, SlimDX.DXGI.Format.R8_UNorm);
				textureSlice[context] = tex;
			}
			else if (textureSlice[context].Width != this.FFrameWidth || textureSlice[context].Height != this.FFrameHeight)
			{
				textureSlice[context].Dispose();
				tex = new DX11DynamicTexture2D(context, FFrameWidth, FFrameHeight, SlimDX.DXGI.Format.R8_UNorm);
				textureSlice[context] = tex;
			}
			else
			{
				tex = textureSlice[context];
			}

			FDoubleBuffer.LockFront.AcquireReaderLock(100);
			try
			{
				//write data to surface
				if (FFrameWidth == tex.GetRowPitch())
				{
					tex.WriteData(FDoubleBuffer.Front);
				}
				else
				{
					GCHandle pinnedArray = GCHandle.Alloc(FDoubleBuffer.Front, GCHandleType.Pinned);
					tex.WriteDataPitch(pinnedArray.AddrOfPinnedObject(), FDoubleBuffer.Front.Length);
					pinnedArray.Free();
				}
			}
			catch
			{
			}
			finally
			{
				FDoubleBuffer.LockFront.ReleaseReaderLock();
			}
		}

		public void UpdateFrameAvailable()
		{
			FDataNewPublished = FDataNewInternal;
			FDataNewInternal = false;
		}

		public void SetParameter(IntParameter Parameter, int Value)
		{
			FThread.Perform(() =>
				{
					switch (Parameter)
					{
						case IntParameter.AEAG:
							FDevice.SetParam(PRM.AEAG, Value);
							break;
						case IntParameter.Exposure:
							FDevice.SetParam(PRM.EXPOSURE, Value);
							break;
						case IntParameter.Gain:
							FDevice.SetParam(PRM.GAIN, (float)Value);
							break;
						case IntParameter.RegionHeight:
							int value = Value;
							value /= 4;
							value *= 4;
							if (value < 4)
								value = 4;

							FDevice.SetParam(PRM.HEIGHT, value);
							break;
						case IntParameter.RegionWidth:
							FDevice.SetParam(PRM.WIDTH, Value);
							break;
						case IntParameter.RegionX:
							FDevice.SetParam(PRM.OFFSET_X, Value);
							break;
						case IntParameter.RegionY:
							FDevice.SetParam(PRM.OFFSET_Y, Value);
							break;
					}
				});
		}

		static int Count
		{
			get
			{
				return (new xiCam()).GetNumberDevices();
			}
		}

		public void Dispose()
		{
			Stop();
			FThread.Dispose();
		}
	}
}
