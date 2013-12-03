using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using System.Threading;

namespace VVVV.CV.Core
{
	public class ProcessFilter<T> : IProcess<T>, IDisposable where T : IFilterInstance, new()
	{
		CVImageInputSpread FInput;
		public CVImageInputSpread Input { get { return FInput; } }

		CVImageOutputSpread FOutput;
		public CVImageOutputSpread Output { get { return FOutput; } }

		public ProcessFilter(ISpread<CVImageLink> inputPin, ISpread<CVImageLink> outputPin)
		{
			FInput = new CVImageInputSpread(inputPin);
			FOutput = new CVImageOutputSpread(outputPin);
			
			StartThread();
		}

		private void ThreadedFunction()
		{
			while (FThreadRunning)
			{
				if (FInput.Connected)
				{
					lock (FLockProcess)
					{
						try
						{
							for (int i = 0; i < SliceCount; i++)
							{
								if (!FInput[i].Allocated)
									continue;

								if (FInput[i].ImageAttributesChanged || FProcess[i].NeedsAllocate)
								{
									FInput[i].ClearImageAttributesChanged();
									FProcess[i].ClearNeedsAllocate();

									FProcess[i].ClearNeedsAllocate();
									FProcess[i].Allocate();
								}

								if (FInput[i].ImageChanged || FProcess[i].FlaggedForProcess)
								{
									FInput[i].ClearImageChanged();
									FProcess[i].ClearFlagForProcess();
									FProcess[i].TransferTimestamp();
									FProcess[i].Process();
								}
							}

						}		
						catch (Exception e)
						{
							ImageUtils.Log(e);
						}
					}

					Thread.Sleep(1);
				}
				else
				{
					Thread.Sleep(10);
				}
			}
		}

		private void StartThread()
		{
			FThreadRunning = true;
			FThread = new Thread(ThreadedFunction);
            FThread.Name = "OpenCV Filter";
            FThread.Start();
		}

		private void StopThread()
		{
			if (FThreadRunning)
			{
				FThreadRunning = false;
				FThread.Join();
			}
		}

		#region Spread access

		public T GetProcessor(int index)
		{
			return FProcess[index];
		}

		public CVImageInput GetInput(int index)
		{
			return FInput[index];
		}

		public CVImageOutput GetOutput(int index)
		{
			return FOutput[index];
		}

		public int SliceCount
		{
			get
			{
				return FProcess.SliceCount;
			}
		}
		#endregion

		public bool CheckInputSize()
		{
			return CheckInputSize(FInput.SliceCount);
		}

		/// <summary>
		/// Check the SliceCount
		/// </summary>
		/// <param name="SpreadMax">New SliceCount</param>
		/// <returns>true if changes were made</returns>
		public bool CheckInputSize(int SpreadMax)
		{
			if (!FInput.CheckInputSize() && FOutput.SliceCount == SpreadMax)
				return false;

			lock (FLockProcess)
			{
				if (FInput.SliceCount == 0)
					SpreadMax = 0;
				else if (FInput[0] == null)
					SpreadMax = 0;

				for (int i = FProcess.SliceCount; i < SpreadMax; i++)
					Add(FInput[i]);

				if (FProcess.SliceCount > SpreadMax)
				{
					for (int i = SpreadMax; i < FProcess.SliceCount; i++)
						Dispose(i);

					FProcess.SliceCount = SpreadMax;
					FOutput.SliceCount = SpreadMax;
				}

				FOutput.AlignOutputPins();
			}

			return true;
		}

		private void Add(CVImageInput input)
		{
			CVImageOutput output = new CVImageOutput();
			T addition = new T();

			addition.SetInput(input);
			addition.SetOutput(output);

			FProcess.Add(addition);
			FOutput.Add(output);
		}

		public T this[int index]
		{
			get
			{
				return FProcess[index];
			}
		}

		protected void Resize(int count)
		{
			FProcess.SliceCount = count;
			FOutput.AlignOutputPins();
		}

		public void Dispose()
		{
			StopThread();

			foreach (var process in FProcess)
			{
				var disposableContainer = process as IDisposable;
				if (disposableContainer != null)
				{
					disposableContainer.Dispose();
				}
			}

			FInput.Dispose();
			FOutput.Dispose();
		}

		protected void Dispose(int i)
		{
			var disposableContainer = FProcess[i] as IDisposable;
			if (disposableContainer != null)
			{
				disposableContainer.Dispose();
			}
			FInput[i].Dispose();
			FOutput[i].Dispose();
		}
	}
}
