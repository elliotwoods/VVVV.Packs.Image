using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using System.Threading;

namespace VVVV.CV.Core
{
	public class ProcessDestination<T> : IProcess<T>, IDisposable where T : IDestinationInstance, new()
	{
		CVImageInputSpread FInput;
		public CVImageInputSpread Input { get { return FInput; } }

		public ProcessDestination(ISpread<CVImageLink> inputPin)
		{
			FInput = new CVImageInputSpread(inputPin);

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
						for (int i = 0; i < SliceCount; i++)
						{
							if (!FInput[i].Allocated)
								continue;

							if (FInput[i].ImageAttributesChanged || FProcess[i].NeedsAllocate)
							{
								FInput[i].ClearImageAttributesChanged();
								FProcess[i].ClearNeedsAllocate();
								for (int iProcess = i; iProcess < SliceCount; iProcess += (FInput.SliceCount > 0 ? FInput.SliceCount : int.MaxValue))
									FProcess[iProcess].Allocate();
							}

							try
							{
								if (FInput[i].ImageChanged)
								{
									FInput[i].ClearImageChanged();
									for (int iProcess = i; iProcess < SliceCount; iProcess += (FInput.SliceCount > 0 ? FInput.SliceCount : int.MaxValue))
										FProcess[iProcess].Process();
								}
							}
							catch (Exception e)
							{
								ImageUtils.Log(e);
							}
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
            FThread.Name = "OpenCV Destination";
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


		ThreadMode FThreadMode = ThreadMode.Independant;
		public ThreadMode ThreadMode
		{
			set
			{
				if (value == FThreadMode)
					return;

				FThreadMode = value;
				if (FThreadMode == ThreadMode.Independant)
				{
					RemoveDirectListeners();
					StartThread();
				}
				else
				{
					StopThread();
					AddDirectListeners();
				}
			}
		}

		void AddDirectListeners()
		{
			RemoveDirectListeners();

			for (int i = 0; i < SliceCount; i++)
			{
				FInput[i].ImageUpdate += new EventHandler(FProcess[i].UpstreamDirectUpdate);
				FInput[i].ImageAttributesUpdate += new EventHandler<ImageAttributesChangedEventArgs>(FProcess[i].UpstreamDirectAttributesUpdate);
			}
		}

		void RemoveDirectListeners()
		{
			for (int i = 0; i < SliceCount; i++)
			{
				FInput[i].ImageUpdate -= new EventHandler(FProcess[i].UpstreamDirectUpdate);
				FInput[i].ImageAttributesUpdate -= new EventHandler<ImageAttributesChangedEventArgs>(FProcess[i].UpstreamDirectAttributesUpdate);
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
			if (!FInput.CheckInputSize() && FProcess.SliceCount==SpreadMax)
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
				}
			}

			return true;
		}

		private void Add(CVImageInput input)
		{
			T addition = new T();

			addition.SetInput(input);

			FProcess.Add(addition);
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
		}

		protected void Dispose(int i)
		{
			var disposableContainer = FProcess[i] as IDisposable;
			if (disposableContainer != null)
			{
				disposableContainer.Dispose();
			}
			if (i < FInput.SliceCount)
				FInput[i].Dispose();
		}

	}
}
