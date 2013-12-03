#region using
using System.ComponentModel.Composition;
using System.Drawing;
using System;

using Emgu.CV;
using Emgu.CV.Structure;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using VVVV.CV.Core;
#endregion

namespace VVVV.CV.Nodes.StructuredLight
{
	public class DecodeInstance : IDestinationInstance
	{
		CVImage FGreyscale = new CVImage();
		CVImage FPositive = new CVImage();
		CVImage FNegative = new CVImage();
		public ScanSet ScanSet = new ScanSet();
		public IPayload Payload
		{
			set
			{
				ScanSet.Payload = value;
				ReAllocate();				
			}
		}

		public TimestampRegister TimestampRegister = null;
		public bool WaitForTimestamp = true;

		bool FApply = false;
		public bool Apply
		{
			set
			{
				 FApply = value;
			}
		}

		public bool Ready
		{
			get
			{
				return ScanSet.Payload != null && FInput.Allocated && FGreyscale.Allocated;
			}
		}

		public override void Allocate()
		{
			FGreyscale.Initialise(FInput.ImageAttributes.Size, TColorFormat.L8);
			FPositive.Initialise(FGreyscale.ImageAttributes);
			FNegative.Initialise(FGreyscale.ImageAttributes);
			ScanSet.Allocate(FInput.ImageAttributes.Size);
		}

		int FFramesDetected = 0;
		public int FramesDetected
		{
			get
			{
				int temp = FFramesDetected;
				FFramesDetected = 0;
				return temp;
			}
		}

		ulong LastFrameCaptured = ulong.MaxValue;
		ulong CurrentBalancedFrame = 0;
		public override void Process()
		{
			if (!Ready)
				return;

			if (FNeedsReset)
			{
				FNeedsReset = false;
				ResetMaps();
			}

			if (FApply && TimestampRegister != null)
			{
				FInput.LockForReading();
				try
				{
					ulong Frame;
					if (TimestampRegister.Lookup(FInput.Image.Timestamp, out Frame))
					{
						if (!(WaitForTimestamp && Frame == LastFrameCaptured))
						{
							LastFrameCaptured = Frame;
							FFramesDetected++;

							if (!WaitForTimestamp)
								FApply = false;

							if (ScanSet.Payload.Balanced)
							{
								bool positive = Frame % 2 == 0;
								FInput.GetImage(positive ? FPositive : FNegative);

								if (!positive && Frame / 2 == CurrentBalancedFrame)
									ApplyBalanced(Frame / 2);
								CurrentBalancedFrame = Frame / 2;
							}
						}
				
					}

				}
				finally
				{
					FInput.ReleaseForReading();
				}
				ScanSet.Evaluate();
			}

		}

		unsafe void ApplyBalanced(ulong balancedFrame)
		{
			uint CameraPixelCount = FInput.ImageAttributes.PixelsPerFrame;
			float additionFactor = 1.0f / (float)(ScanSet.Payload.FrameCount / 2);

			lock (ScanSet)
			{
				fixed (ulong* dataFixed = &ScanSet.EncodedData[0])
				{
					fixed (float* strideFixed = &ScanSet.Distance[0])
					{
						fixed (byte* luminanceFixed = &ScanSet.Luminance[0])
						{
							ulong* data = dataFixed;
							float* stride = strideFixed;
							byte* luminance = luminanceFixed;

							byte* positive = (byte*)FPositive.Data.ToPointer();
							byte* negative = (byte*)FNegative.Data.ToPointer();

							int intFrame = (int)balancedFrame;
							for (uint i = 0; i < CameraPixelCount; i++)
							{
								*stride = *stride++ * (1.0f - additionFactor) + (float)(*positive - *negative) * additionFactor;

								//*luminance = (byte) ((float) *luminance * (float) balancedFrame + 
								//	Math.Min(((float)(*positive) + (float)(*negative)) * additionFactor, 255.0f));

								if (*positive++ > *negative++)
									*data++ |= (ulong)1 << intFrame;
								else
									*data++ &= ~((ulong)1 << intFrame);
							}
						}
					}
				}
			}
		}

		[DllImport("msvcrt.dll")]
		private static unsafe extern void memset(void* dest, int c, int count);

		unsafe void ResetMaps()
		{
			if (!FInput.Allocated || !FGreyscale.Allocated)
				return;

			int CameraPixelCount = ScanSet.CameraPixelCount;

			ScanSet.Clear();

			byte* high = (byte*)FPositive.Data.ToPointer();
			byte* low = (byte*)FNegative.Data.ToPointer();

			memset((void*)high, 0, CameraPixelCount);
			memset((void*)low, 0, CameraPixelCount);
		}

		bool FNeedsReset = false;
		public void Reset()
		{
			FNeedsReset = true;
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "Decode", Category = "CV.StructuredLight", Help = "Decode structured light patterns", Author = "elliotwoods", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class DecodeNode : IDestinationNode<DecodeInstance>
	{
		#region fields & pins
		[Input("Apply")]
		ISpread<bool> FPinInApply;

		[Input("Reset", IsBang=true)]
		IDiffSpread<bool> FPinInReset;

		[Input("Properties")]
		IDiffSpread<IPayload> FPinInProperties;

		[Input("Timestamps")]
		IDiffSpread<TimestampRegister> FPinInTimestamps;

		[Input("Wait for timestamp", DefaultValue=1)]
		IDiffSpread<bool> FPinInWaitTimestamp;

		[Output("Output")]
		ISpread<ScanSet> FPinOutOutput;

		[Output("Frames detected")]
		ISpread<int> FPinOutFramesDetected;

		[Import()]
		ILogger FLogger;

		bool FFirstRun = true;
		#endregion fields&pins

		[ImportingConstructor()]
		public DecodeNode()
		{

		}

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			for (int i = 0; i < InstanceCount; i++)
				FProcessor[i].Apply = FPinInApply[i];

			if (FPinInReset.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					if (FPinInReset[i])
						FProcessor[i].Reset();

			if (FPinInProperties.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Payload = FPinInProperties[i];

			if (FPinInTimestamps.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].TimestampRegister = FPinInTimestamps[i];

			if (FPinInWaitTimestamp.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].WaitForTimestamp = FPinInWaitTimestamp[i];

			//this is a little hacky /**HACK**/
			if (SpreadChanged || FPinOutOutput[0] == null)
			{
				FPinOutOutput.SliceCount = InstanceCount;
				FPinOutFramesDetected.SliceCount = InstanceCount;
				for (int i = 0; i < InstanceCount; i++)
					FPinOutOutput[i] = FProcessor[i].ScanSet;
			}

			for (int i = 0; i < InstanceCount; i++)
				FPinOutFramesDetected[i] = FProcessor[i].FramesDetected;
		}

	}
}
