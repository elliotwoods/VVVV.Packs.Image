using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using System.Threading;
using System.Collections;

namespace VVVV.CV.Core
{
	public abstract class IProcess<T> : IEnumerable<T>
	{
		protected Spread<T> FProcess = new Spread<T>(0);

		protected Thread FThread;
		protected bool FThreadRunning = false;
		protected Object FLockProcess = new Object();

		#region IEnumerable
		public ProcessEnum<T> GetEnumerator()
		{
			return new ProcessEnum<T>(FProcess);
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
 			return (IEnumerator<T>) GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return (IEnumerator)GetEnumerator();
		}
		#endregion
	}

	public class ProcessEnum<T> : IEnumerator<T>
	{
		protected Spread<T> FProcesses;
		int FPosition = -1;

		public ProcessEnum(Spread<T> Processes)
		{
			FProcesses = Processes;
		}

		public T  Current
		{
			get
			{
				return FProcesses[FPosition];
			}
		}

		public void  Dispose()
		{
 			
		}

		public bool  MoveNext()
		{
 			FPosition++;
			return FPosition < FProcesses.SliceCount;
		}

		public void  Reset()
		{
			FPosition = 0;
		}

		object IEnumerator.Current
		{
			get
			{
				return Current;
			}
		}
	}
}
