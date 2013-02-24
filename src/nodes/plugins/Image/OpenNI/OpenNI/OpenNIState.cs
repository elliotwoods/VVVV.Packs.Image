using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenNI;
using System.Threading;

namespace VVVV.Nodes.OpenCV.OpenNI
{
    interface Listener
    {
        void ContextInitialise();
        void ContextUpdate();
    }

	class Device
	{
		public string CreationInfo = "";
		public Context Context;
		/// <summary>
		/// We have a special instance here since we
		/// want the thread to always wait on this
		/// generator.
		/// </summary>
		public DepthGenerator DepthGenerator;

        HashSet<Listener> FListeners = new HashSet<Listener>();

        void OnInitialised()
        {
            foreach (var listener in FListeners)
            {
                listener.ContextInitialise();
            }
        }

        void OnUpdate()
        {
            foreach (var listener in FListeners)
            {
                listener.ContextUpdate();
            }
        }

        public void RegisterListener(Listener listener)
        {
            FListeners.Add(listener);
            if (this.Running)
                listener.ContextInitialise();
        }

        public void UnregisterListener(Listener listener)
        {
            if (FListeners.Contains(listener))
                FListeners.Remove(listener);
        }

		private bool FRunning = false;
		public bool Running 
		{
			get
			{
				return FRunning;
			}
		}

		public string Status = "";

		private Thread FThread;
		public void Start()
		{
			if (FRunning)
				Stop();

			FThread = new Thread(ThreadedFunction);
			FRunning = true;
			FThread.Start();
			OnInitialised();
		}

		public void Stop()
		{
			if (!FRunning)
				return;

			FRunning = false;
			FThread.Join();
			FThread = null;
		}

		private void ThreadedFunction()
		{
			while (this.Running)
			{
				try
				{
					Context.WaitOneUpdateAll(DepthGenerator);
					this.OnUpdate();
					Status = "OK";
				}
				catch (Exception e)
				{
					Status = e.Message;
				}
			}
		}
	}
}
