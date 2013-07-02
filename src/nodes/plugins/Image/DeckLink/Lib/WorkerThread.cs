using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace VVVV.Nodes.DeckLink
{
	class WorkerThread : IDisposable
	{
		public delegate void WorkItem();
		public static WorkerThread Singleton = new WorkerThread();

		Thread FThread;
		bool FRunning;
		Object FLock = new Object();
		Queue<WorkItem> FWorkQueue = new Queue<WorkItem>();
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

		public void PerformBlocking(WorkItem item)
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

		public void Perform(WorkItem item)
		{
			lock (FLock)
			{
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
						break;
					else
						Thread.Sleep(1);
				}
			}
		}
	}
}
