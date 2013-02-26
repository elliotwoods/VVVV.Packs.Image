using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.OpenCV
{
	public abstract class IGeneratorInstance : IInstance, IInstanceOutput, IDisposable
	{
		protected CVImageOutput FOutput;

		/// <summary>
		/// This is invalid for generators
		/// </summary>
		public override void Allocate() {}

		/// <summary>
		/// Open the device for capture. This is called from inside the thread
		/// </summary>
		protected abstract bool Open();
		/// <summary>
		/// Close the capture device. This is called from inside the thread
		/// </summary>
		protected abstract void Close();

		private bool FNeedsOpen = false;
		private bool FNeedsClose = false;
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
                FNeedsOpen = true;
            else
                this.FOpen = this.Open();
		}
		/// <summary>
		/// Message the thread to stop the capture device. This is called from outside the thread (e.g. the plugin node)
		/// </summary>
		public void Stop()
		{
            if (this.NeedsThread())
                FNeedsClose = true;
            else
                this.Close();
		}

		/// <summary>
		/// Used to restart the device (e.g. you change a setting). If not open, this action does nothing
		/// </summary>
		public void Restart()
		{
            if (this.NeedsThread())
            {
                if (this.IsOpen)
                {
                    FNeedsClose = true;
                    FNeedsOpen = true;
                }
            }
            else
            {
                if (this.IsOpen)
                    Close();
                if (this.Enabled)
                    this.FOpen = Open();
            }
			
		}

		override public void Process()
		{
			lock (FLockProperties)
			{
				if (FNeedsClose)
				{
					FNeedsClose = false;
					if (FOpen)
						Close();
					FEnabled = false;
					FOpen = false;
					return;
				}

				if (FNeedsOpen && Enabled || (!IsOpen && Enabled))
				{
					FNeedsOpen = false;
                    if (IsOpen)
						Close();
					FOpen = Open();
				}

				if (IsOpen)
				{
					if (FOutput.Image.Allocated == false && this.NeedsAllocate())
						ReAllocate();
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
					if (value)
					{

						FEnabled = true;
						Start();
					}
					else
					{
						Stop();
                        FEnabled = false;
					}
				}
			}
		}

		override public void Dispose()
		{
			Enabled = false;
		}
	}
}
