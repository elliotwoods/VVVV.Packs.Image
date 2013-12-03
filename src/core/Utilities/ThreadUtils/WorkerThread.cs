using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace VVVV.CV.Core.ThreadUtils
{
	public class WorkerThread : IDisposable
	{
		public delegate void WorkItemDelegate();

		Thread FThread;
		bool FRunning;
		Object FLock = new Object();
		Queue<WorkItemDelegate> FWorkQueue = new Queue<WorkItemDelegate>();
		Exception FException;

		public WorkerThread()
		{
			FRunning = true;
			FThread = new Thread(ThreadedFunction);
			FThread.Start();
		}

		public string Name
		{
			get
			{
				return FThread.Name;
			}
			set
			{
				FThread.Name = value;
			}
		}

		public event EventHandler ThreadWait;

		void ThreadedFunction()
		{
			while (FRunning)
			{
				bool noWorkDone = true;
				this.FException = null;

				lock (FLock)
				{
					if (FWorkQueue.Count > 0)
					{
						noWorkDone = false;
						var workItem = FWorkQueue.Dequeue();
						try
						{
							workItem();
						}
						catch (Exception e)
						{
							this.FException = e;
						}
					}
				}
				if (noWorkDone)
				{
					if (ThreadWait == null)
						Thread.Sleep(1);
					else
					{
						try
						{
							ThreadWait(this, EventArgs.Empty);
						}
						catch (Exception e)
						{
							this.FException = e;
						}
					}
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
				if (!FRunning)
				{
					throw (new Exception("Thread [" + FThread.Name + "] is not running, so BlockUntilEmpty should not be called."));
				}

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
