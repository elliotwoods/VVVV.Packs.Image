using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace VVVV.Nodes.DeckLink
{
	class WorkerThread : IDisposable
	{
		public delegate void WorkItemDelegate();
		public static WorkerThread Singleton = new WorkerThread();

		Thread FThread;
		bool FRunning;
		Object FLock = new Object();
		Queue<WorkItemDelegate> FWorkQueue = new Queue<WorkItemDelegate>();
		Exception FException;

		WorkerThread()
		{
			FRunning = true;
			FThread = new Thread(Loop);
			FThread.SetApartmentState(ApartmentState.MTA);
			FThread.Name = "DeckLink Worker";
			FThread.Start();
		}

		void Loop()
		{
			while (FRunning)
			{
				bool empty = true;
				lock (FLock)
				{
					if (FWorkQueue.Count > 0)
					{
						empty = false;
						var workItem = FWorkQueue.Dequeue();
						try
						{
							workItem();
							this.FException = null;
						}
						catch (Exception e)
						{
							this.FException = e;
						}
					}
				}
				if (empty)
				{
					Thread.Sleep(1);
				}
			}
		}

		public void Dispose()
		{
			FRunning = false;
			FThread.Join();
		}

		public void PerformBlocking(WorkItemDelegate item)
		{
			if (Thread.CurrentThread == this.FThread)
			{
				//we're already inside the worker thread
				item();
			}
			else
			{
				Perform(item);
				BlockUntilEmpty();
				if (this.FException != null)
				{
					var e = this.FException;
					this.FException = null;
					throw (e);
				}
			}
		}

		public void Perform(WorkItemDelegate item)
		{
			lock (FLock)
			{
				FWorkQueue.Enqueue(item);
			}
		}

		public void PerformUnique(WorkItemDelegate item)
		{
			lock (FLock)
			{
				if (!FWorkQueue.Contains(item))
					FWorkQueue.Enqueue(item);
			}
		}

		public void BlockUntilEmpty()
		{
			while (true)
			{
				lock (FLock)
				{
					if (FWorkQueue.Count == 0)
						return;
				}
				Thread.Sleep(1);
			}
		}
	}
}
