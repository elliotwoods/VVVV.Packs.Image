using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.CV.Core
{
	public abstract class IInstance
	{
		/// <summary>
		/// This should be replaced to different functions
		/// depending on whether the node has an input or not
		/// </summary>
		public abstract void Allocate();
		public abstract void Process();

		protected Object FLockProperties = new Object();

		private Object FLockStatus = new Object();
		private string FStatus;
		public string Status
		{
			get
			{
				lock (FLockStatus)
					return FStatus;
			}
			set
			{
				lock (FLockStatus)
					FStatus = value;
			}
		}

        /// <summary>
        /// Flag to mark when allocation is required. This is deprecreciated in IGeneratorInstance
        /// in favour of FActionQueue.
        /// </summary>
		protected bool FNeedsAllocate = true;
		public bool NeedsAllocate
		{
			get
			{
				return FNeedsAllocate;
			}
		}
		public void ClearNeedsAllocate()
		{
			FNeedsAllocate = false;
		}

		/// <summary>
		/// If you don't want a constantly running thread (e.g. for an image loader)
		/// then override this to return false.
		/// You will need to thread lock the current object (i.e. 'lock (this)') to access its resources.
		/// </summary>
		/// <returns></returns>
		public virtual bool NeedsThread()
		{
			return true;
		}

		/// <summary>
		/// Calls Initialise on next thread loop.
		/// Feel free this to call multiple times in 1 evaluate.
		/// If not threaded, forces an immediate Initialise with lock
		/// </summary>
		virtual public void ReAllocate()
		{
			if (this.NeedsThread())
				FNeedsAllocate = true;
			else
				lock (this)
					Allocate();
		}
	}
}
