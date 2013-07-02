using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.Ximea
{
	abstract class SoftwareTrigger
	{
		protected void OnTrigger()
		{
			if (Trigger != null)
				Trigger(this, EventArgs.Empty);
		}

		public event EventHandler Trigger;
	}

	class SoftwareLFO : SoftwareTrigger, ITrigger, IDisposable
	{
		public double Interval = 1.0 / 90.0;
		public double Phase = 0.0;
		bool FRunning;

		public double Frequency
		{
			get
			{
				return 1.0 / Interval;
			}
			set
			{
				Interval = 1.0 / value;
			}
		}

		public bool Running
		{
			get
			{
				return FRunning;
			}
		}

		int TriggerOffset
		{
			get
			{
				return (int)((double)Interval * Phase);
			}
		}

		public SoftwareLFO()
		{
			FRunning = true;
			FTimer.Start();
			FThread = new Thread(ThreadedFunction);
			FThread.Start();
		}

		Thread FThread;
		Stopwatch FTimer = new Stopwatch();
		System.Int64 FNextTrigger = 0;

		void ThreadedFunction()
		{
			while (FRunning)
			{
				var time = FTimer.ElapsedTicks * 1000L * 1000L / (Stopwatch.Frequency);
				var timeRemaining = FNextTrigger - time;
				if (timeRemaining <= 0)
				{
					OnTrigger();
					FNextTrigger = time + TriggerOffset + (long) (Interval * 1e6);
				}
				else if (timeRemaining > 200000)
				{
					Thread.Sleep(100);
				}
				else if (timeRemaining > 20000)
				{
					Thread.Sleep(10);
				}
				else if (timeRemaining > 2000)
				{
					Thread.Sleep(1);
				}
			}
		}

		public void Dispose()
		{
			FRunning = false;
		}

		public TriggerType GetTriggerType()
		{
			return TriggerType.Software;
		}
	}
}
