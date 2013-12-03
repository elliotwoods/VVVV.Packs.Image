using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using System.Threading;
using System.Threading.Tasks;

namespace VVVV.CV.Core
{
	public class ProcessGenerator<T> : IProcess<T>, IDisposable where T : IGeneratorInstance, new()
	{
		CVImageOutputSpread FOutput;
		public CVImageOutputSpread Output { get { return FOutput; } }

		public ProcessGenerator(ISpread<CVImageLink> outputPin)
		{
			FOutput = new CVImageOutputSpread(outputPin);

			T testThreaded = new T();
			if (testThreaded.NeedsThread())
				StartThread();
		}

		private void ThreadedFunction()
		{
			while (FThreadRunning)
			{
				lock (FLockProcess)
				{
					try
					{
						Parallel.For(0, FProcess.SliceCount, i =>
							{
                                FProcess[i].ProcessActionQueue();
								FProcess[i].Process();
							});
					}
					catch (Exception e)
					{
						ImageUtils.Log(e);
					}
				}
				Thread.Sleep(1);
			}
		}

		private void StartThread()
		{
			FThreadRunning = true;
			FThread = new Thread(ThreadedFunction);
            FThread.Name = "OpenCV Generator";
            FThread.Start();
		}

		private void StopThread()
		{
			if (FThreadRunning)
			{
				foreach (var process in FProcess)
				{
					process.Dispose();
				}
				foreach (var process in FProcess)
				{
					while (process.IsOpen == true)
						Thread.Sleep(5);
				}

				FThreadRunning = false;
				FThread.Join();
			}
		}

		#region Spread access

		public T GetProcessor(int index)
		{
			return FProcess[index];
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

		/// <summary>
		/// Check the SliceCount. Generally called from Node's evaluate
		/// </summary>
		/// <param name="SpreadMax">New SliceCount</param>
		/// <returns>true if changes were made</returns>
		public bool CheckInputSize(int SpreadMax)
		{
			if (FProcess.SliceCount == SpreadMax)
				return false;

			lock (FLockProcess)
			{
				for (int i = FProcess.SliceCount; i < SpreadMax; i++)
					Add();

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

		private void Add()
		{
			CVImageOutput output = new CVImageOutput();
			T addition = new T();

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
				process.Dispose();

			FOutput.Dispose();
		}

		protected void Dispose(int i)
		{
			FProcess[i].Dispose();
			FOutput[i].Dispose();
		}
	}
}
