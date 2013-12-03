using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.CV.Core
{
	public abstract class IGeneratorInstance : IInstance, IInstanceOutput, IDisposable
	{
        enum Action
        {
            Open,
            Close,
            Allocate
        }
        Queue<Action> FActionQueue = new Queue<Action>();

        void Enqueue(Action Action)
        {
            this.FActionQueue.Enqueue(Action);
        }

        public void ProcessActionQueue()
        {
            while (FActionQueue.Count > 0)
            {
                var action = FActionQueue.Dequeue();
                switch (action)
                {
                    case Action.Open:
                        this.FOpen = Open();
                        break;
                    case Action.Close:
                        if (this.FOpen)
                        {
                            Close();
                        }
                        this.FOpen = false;
                        break;
                    case Action.Allocate:
                        Allocate();
                        break;
                }
            }
        }

		protected CVImageOutput FOutput;

		/// <summary>
		/// This is invalid for generators
		/// </summary>
		public override void Allocate() {}

		/// <summary>
		/// Open the device for capture. This is called from inside the thread
		/// </summary>
        public abstract bool Open();
		/// <summary>
		/// Close the capture device. This is called from inside the thread
		/// </summary>
		public abstract void Close();

		private bool FOpen = false;
		public bool IsOpen
		{
			get
			{
				return FOpen;	
			}
		}

		/// <summary>
		/// Message the thread to start the capture device. This is called from outside the thread (e.g. the plugin node)
		/// </summary>
		public void Start()
		{
            if (this.NeedsThread())
            {
                this.Enqueue(Action.Open);
            }
            else
            {
                this.FOpen = this.Open();
                Allocate();
            }
        }

		/// <summary>
		/// Message the thread to stop the capture device. This is called from outside the thread (e.g. the plugin node)
		/// </summary>
		public void Stop()
		{
            if (this.NeedsThread())
            {
                this.Enqueue(Action.Close);
            } else {
                this.Close();
                this.FOpen = false;
            }
		}

        /// <summary>
        /// Message the thread to allocate frame.
        /// </summary>
        public override void ReAllocate()
        {
            if (this.NeedsThread())
            {
                this.Enqueue(Action.Allocate);
            } else {
                Allocate();
            }
        }

		/// <summary>
		/// Used to restart the device (e.g. you change a setting). If not open, this action does nothing
		/// </summary>
		public void Restart()
		{
            if (this.IsOpen)
            {
                Stop();
                Start();
            }
		}

		override public void Process()
		{
			lock (FLockProperties)
			{
				if (IsOpen)
				{
					if (FOutput.Image.Allocated == false && this.NeedsAllocate)
					{
						this.ClearNeedsAllocate();
						ReAllocate();
					}
					else
					{
						FOutput.Image.Timestamp = DateTime.UtcNow.Ticks - TimestampDelay * 10000;
						Generate();
					}
				}
			}
		}

		public void SetOutput(CVImageOutput output)
		{
			FOutput = output;
		}

		public int TimestampDelay = 0;

		/// <summary>
		/// For threaded generators you must override this function
		/// For non-threaded generators, you use your own function
		/// </summary>
		protected virtual void Generate() { }

		private bool FEnabled = false;
		public bool Enabled
		{
			get
			{
				return FEnabled;
			}
			set
			{
				if (FEnabled == value)
					return;
				lock (FLockProperties)
				{
                    FEnabled = value;

					if (value)
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

		public void Dispose()
		{
            Stop();
		}
	}
}
