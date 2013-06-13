using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

		public enum Trigger
		{
			Default,
			HardwareRisingEdge
		}

		class InstructionQueue<Parameter, Value>
		{
			public Dictionary<Parameter, Value> Content = new Dictionary<Parameter, Value>();
			public Object Lock = new Object();

			public void Add(Parameter Parameter, Value Value)
			{
				lock (Lock)
				{
					Content.Add(Parameter, Value);
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

		InstructionQueue<Instruction, int> FInstructionQueue = new InstructionQueue<Instruction, int>();
		InstructionQueue<IntParameter, int> FParameterChangeQueue = new InstructionQueue<IntParameter, int>();
		DoubleBuffer FDoubleBuffer = new DoubleBuffer();
		public int Timeout = 500;
		public int FFrameWidth = 0;
		public int FFrameHeight = 0;
		public int FFrameIndex = -1;
		public double FTimestamp = 0;
		public double FFramerate = 0;

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

		public bool Running
		{
			get
			{
				return FThread != null;
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

		void ThreadedFunction()
		{
			bool FRunning = true;

			do
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

									FRunning = true;
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
								break;
						}
					}
					FInstructionQueue.Content.Clear();
				}

				lock (FParameterChangeQueue.Lock)
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
								FDevice.SetParam(PRM.GAIN, operation.Value);
								break;
							case IntParameter.RegionHeight:
								FDevice.SetParam(PRM.HEIGHT, operation.Value);
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

				if (FRunning)
				{
					FFrameWidth = FDevice.GetParamInt(PRM.WIDTH);
					FFrameHeight = FDevice.GetParamInt(PRM.HEIGHT);
					int size = FFrameWidth * FFrameHeight;

					if (FDoubleBuffer.Back == null || FDoubleBuffer.Back.Length != size)
					{
						FDoubleBuffer.Back = new byte[size];
					}

					try
					{
						FDevice.GetImage(FDoubleBuffer.Back, Timeout);
						FDoubleBuffer.Swap();
						
						var imageParams = FDevice.GetLastImageParams();
						FFrameIndex = imageParams.GetFrameNum();

						var timestamp = imageParams.GetTimestamp();
						FFramerate = 1.0 / (timestamp - FTimestamp);
						FTimestamp = timestamp;
					}
					catch
					{
					}
					
				}
			} while (FRunning);

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
