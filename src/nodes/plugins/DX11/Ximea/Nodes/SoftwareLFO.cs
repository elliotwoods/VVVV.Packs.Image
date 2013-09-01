using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.Ximea
{
	#region PluginInfo
	[PluginInfo(Name = "LFO",
				Category = "Ximea",
				Version = "Trigger",
				Help = "",
				Tags = "")]
	#endregion PluginInfo
	public class SoftwareLFONode : IPluginEvaluate
	{
		class SoftwareLFO : ISoftwareTrigger, ITrigger, IDisposable
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
						FNextTrigger = time + TriggerOffset + (long)(Interval * 1e6);
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

		[Input("Frequency", IsSingle = true, DefaultValue=90)]
		IDiffSpread<double> FInFrequency;

		[Input("Phase", IsSingle = true)]
		IDiffSpread<double> FInPhase;

		[Output("Trigger")]
		ISpread<ITrigger> FOutClock;

		SoftwareLFO FClock = new SoftwareLFO();
		bool FFirstRun = true;

		public void Evaluate(int SpreadMax)
		{
			if (FFirstRun)
			{
				FFirstRun = false;
				FOutClock[0] = FClock;
			}

			if (FInFrequency.IsChanged)
			{
				FClock.Frequency = FInFrequency[0];
			}

			if (FInPhase.IsChanged)
			{
				FClock.Phase = FInPhase[0];
			}
		}
	}
}
