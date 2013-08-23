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
using xiApi.NET;

namespace VVVV.Nodes.Ximea
{
	class Device : IDisposable
	{
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

		enum Instruction
		{
			SetDeviceID,
			Open,
			Close,
			StartAcquisition
		}

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

		class InstructionQueue<Parameter, Value>
		{
			public Dictionary<Parameter, Value> Content = new Dictionary<Parameter, Value>();
			public Object Lock = new Object();

			public void Add(Parameter Parameter, Value Value)
			{
				lock (Lock)
				{
					Content[Parameter] = Value;
				}
			}

			public void BlockUntilEmpty(int Interval = 1)
			{
				bool empty = false;

				while (true)
				{
					lock (Lock)
					{
						empty = this.Content.Count == 0;
					}

					if (empty)
						return;
					else
						Thread.Sleep(Interval);
				}
			}
		}

		[Import()]
		ILogger FLogger;

		InstructionQueue<Instruction, int> FInstructionQueue = new InstructionQueue<Instruction, int>();
		InstructionQueue<IntParameter, int> FParameterChangeQueue = new InstructionQueue<IntParameter, int>();
		DoubleBuffer FDoubleBuffer = new DoubleBuffer();
		public int Timeout = 500;
		public int FFrameWidth = 0;
		public int FFrameHeight = 0;
		public int FFrameIndex = -1;
		public double FTimestamp = 0;
		public double FFramerate = 0;
		bool FDataNew = false;
		bool FDataNewPublic = false;

		Specification FSpecification = new Specification();
		public Specification DeviceSpecification
		{
			get
			{
				return FSpecification;
			}
		}

		Thread FThread;
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

				var oldSoftwareTrigger = FTrigger as SoftwareTrigger;
				if (oldSoftwareTrigger != null)
				{
					oldSoftwareTrigger.Trigger -= SoftwareTrigger_Trigger;
				}

				FTrigger = value;
				
				if (FTrigger != null && FTrigger.GetTriggerType() == TriggerType.Software)
				{
					var SoftwareTrigger = FTrigger as SoftwareTrigger;
					SoftwareTrigger.Trigger += SoftwareTrigger_Trigger;
				}

				if (this.Running)
					Start(); //restart
			}
		}

		void SoftwareTrigger_Trigger(object sender, EventArgs e)
		{
			lock (FInstructionQueue.Lock)
			{
				if (Running && FTrigger == sender)
				{
					FDevice.SetParam(PRM.TRG_SOFTWARE, 0);
				}
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
				if (FDataNewPublic)
				{
					FDataNewPublic = false;
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		void Start()
		{
			Stop();

			FThread = new Thread(ThreadedFunction);
			FThread.Name = "Ximea Device";
			FThread.Start();

			FInstructionQueue.Add(Instruction.Open, FDeviceID);
			FInstructionQueue.Add(Instruction.StartAcquisition, 0);
			FInstructionQueue.BlockUntilEmpty();
		}

		void Stop()
		{
			if (FThread == null)
				return;

			FInstructionQueue.Add(Instruction.Close, 0);
			FInstructionQueue.BlockUntilEmpty();
			FThread.Join();
			FThread = null;
		}

		void ProcessInstructionQueue()
		{
			lock (FInstructionQueue.Lock)
			{
				foreach (var operation in FInstructionQueue.Content)
				{
					switch (operation.Key)
					{
						case Instruction.Open:
							try
							{
								FDevice.OpenDevice(operation.Value);
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
										default:
											throw (new Exception("Trigger type not handled"));
									}
								}
							}
							catch
							{
								FRunning = false;
							}
							break;
						case Instruction.Close:
							FDevice.CloseDevice();
							FRunning = false;
							break;
						case Instruction.StartAcquisition:
							FDevice.StartAcquisition();
							FRunning = true;
							break;
					}
				}
				FInstructionQueue.Content.Clear();
			}
		}

		void ProcessParameterQueue()
		{
			lock (FParameterChangeQueue.Lock)
			{
				try
				{
					foreach (var operation in FParameterChangeQueue.Content)
					{
						switch (operation.Key)
						{
							case IntParameter.AEAG:
								FDevice.SetParam(PRM.AEAG, operation.Value);
								break;
							case IntParameter.Exposure:
								FDevice.SetParam(PRM.EXPOSURE, operation.Value);
								break;
							case IntParameter.Gain:
								FDevice.SetParam(PRM.GAIN, (float) operation.Value);
								break;
							case IntParameter.RegionHeight:
								int value = operation.Value;
								value /= 4;
								value *= 4;
								if (value < 4)
									value = 4;

								FDevice.SetParam(PRM.HEIGHT, value);
								break;
							case IntParameter.RegionWidth:
								FDevice.SetParam(PRM.WIDTH, operation.Value);
								break;
							case IntParameter.RegionX:
								FDevice.SetParam(PRM.OFFSET_X, operation.Value);
								break;
							case IntParameter.RegionY:
								FDevice.SetParam(PRM.OFFSET_Y, operation.Value);
								break;
						}
					}
					FParameterChangeQueue.Content.Clear();
				}
				catch (Exception e)
				{
					FParameterChangeQueue.Content.Clear();
				}
			}
		}

		void ThreadedFunction()
		{
			do
			{
				ProcessInstructionQueue();
				
				if (FRunning)
				{
					ProcessParameterQueue();

					int width = FDevice.GetParamInt(PRM.WIDTH);
					int height = FDevice.GetParamInt(PRM.HEIGHT);
					int size = width * height;

					if (FDoubleBuffer.Back == null || FDoubleBuffer.Back.Length != size)
					{
						FDoubleBuffer.Back = new byte[size];
					}

					try
					{
						FDevice.GetImage(FDoubleBuffer.Back, Timeout);
						
						var imageParams = FDevice.GetLastImageParams();
						FFrameIndex = imageParams.GetFrameNum();

						var timestamp = imageParams.GetTimestamp();
						FFramerate = 1.0 / (timestamp - FTimestamp);
						FTimestamp = timestamp;

						FDataNew = true;
						FDataNewPublic = true;
						FFrameWidth = width;
						FFrameHeight = height;
						FDoubleBuffer.Swap();
					}
					catch
					{
					}
				}
			} while (FRunning);

		}

		public void Update(DX11Resource<DX11DynamicTexture2D> textureSlice, DX11RenderContext context)
		{
			if (!this.Running || !FDataNew)
			{
				return;
			}
			FDataNew = false;

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

		public void SetParameter(IntParameter Parameter, int Value)
		{
			FParameterChangeQueue.Add(Parameter, Value);
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
		}
	}
}
