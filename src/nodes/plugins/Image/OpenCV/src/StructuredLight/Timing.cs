using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.CV.Nodes.StructuredLight
{
	#region PluginInfo
	[PluginInfo(Name = "Timing", Category = "CV.StructuredLight", Version = "DSLR", Help = "Time a structured light capture", Author = "elliotwoods", AutoEvaluate = true, Tags = "")]
	#endregion PluginInfo
	public  class TimingNode : IPluginEvaluate
	{
		[Input("Payload", IsSingle=true)]
		IDiffSpread<IPayload> FInPayload;

		[Input("Pre Capture Delay", DefaultValue = 1000)]
		ISpread<int> FInPreDelay;

		[Input("Post Capture Delay", DefaultValue = 500)]
		ISpread<int> FInPostDelay;

		[Input("Go", IsBang = true, IsSingle = true)]
		ISpread<bool> FInGo;

		[Input("Abort", IsBang = true, IsSingle = true)]
		ISpread<bool> FInAbort;

		[Output("Initialise", IsBang = true)]
		ISpread<bool> FOutInitialise;

		[Output("FrameIndex")]
		ISpread<int> FOutFrameIndex;

		[Output("Capture", IsBang = true)]
		ISpread<bool> FOutCapture;

		[Output("Complete")]
		ISpread<bool> FOutComplete;

		delegate bool ActionQueueEntry(); // returns true if should wait a frame before next action
		List<ActionQueueEntry> FActionQueue = new List<ActionQueueEntry>();
		DateTime FLastFrameSent = new DateTime();

		void PopulateActionQueue()
		{
			uint frameCount = FInPayload[0].FrameCount;
			FActionQueue.Add(() =>
			{
				FOutComplete[0] = false;
				return false;
			});
			FActionQueue.Add(() =>
			{
				FOutInitialise[0] = true;
				return true;
			});
			FActionQueue.Add(() =>
			{
				FOutInitialise[0] = false;
				return false;
			});
			for (uint i = 0; i < frameCount; i++)
			{
				var i1 = i;
				FActionQueue.Add(() =>
				{
					FOutFrameIndex[0] = (int)i1;
					FLastFrameSent = DateTime.Now;
					return true;
				});
				FActionQueue.Add(PreWait);
				FActionQueue.Add(() =>
				{
					FOutCapture[0] = true;
					return true;
				});
				FActionQueue.Add(() =>
				{
					FOutCapture[0] = false;
					return false;
				});
				FActionQueue.Add(PostWait);
			}
			FActionQueue.Add(() =>
			{
				FOutComplete[0] = true;
				return false;
			});
		}

		bool PreWait()
		{
			if ((DateTime.Now - FLastFrameSent).TotalMilliseconds < FInPreDelay[0])
			{
				FActionQueue.Insert(0, PreWait);
				return true;
			}
			else
			{
				return false;
			}
		}

		bool PostWait()
		{
			if ((DateTime.Now - FLastFrameSent).TotalMilliseconds < FInPostDelay[0])
			{
				FActionQueue.Insert(0, PostWait);
				return true;
			}
			else
			{
				return false;
			}
		}

		public void Evaluate(int SpreadMax)
		{
			if (FInGo[0] && FInPayload[0] != null)
			{
				FActionQueue.Clear();
				PopulateActionQueue();
			}

			if (FInAbort[0])
			{
				FActionQueue.Clear();
			}

			while (FActionQueue.Count > 0)
			{
				var action = FActionQueue[0];
				FActionQueue.RemoveAt(0);

				if (action())
				{
					//if we need to wait until next frame after this action, then continue
					return;
				}
			}
		}
	}
}
