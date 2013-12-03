#region using
using System.ComponentModel.Composition;
using System.Drawing;
using System;

using Emgu.CV;
using Emgu.CV.Structure;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;
using System.Collections.Generic;
using VVVV.CV.Core;
#endregion

namespace VVVV.CV.Nodes.StructuredLight
{
	public class EncodeInstance : IStaticGeneratorInstance
	{
		IPayload FPayload = null;
		public IPayload Payload
		{
			set
			{
				FPayload = value;
				ReAllocate();
			}
		}

		public TimestampRegister Timestamps = new TimestampRegister();

		public int Frame = 0;
		public int FrameRendered = -1;

		public override void Allocate()
		{
			if (Allocated)
			{
				lock (FPayload)
				{
					FOutput.Image.Initialise(FPayload.Size, TColorFormat.L8);
					Timestamps.Initialise(FPayload.FrameCount);
					FrameRendered = -1;
					Status = "OK";
				}
			}
			else
			{
				Status = "Waiting for payload";
			}
		}

		protected override void Generate()
		{
			if (Allocated && FrameRendered != Frame)
			{
				Update();
				FrameRendered = Frame;
				Timestamps.Add((ulong)Frame, FOutput.Image.Timestamp);
				FOutput.Send();
			}
		}

		unsafe void Update()
		{
			int frame = Frame;

			byte* outPix = (byte*)FOutput.Data.ToPointer();

			lock (FPayload)
			{
				if (FPayload.Width != FOutput.Image.Width || FPayload.Height != FOutput.Image.Height)
					return;
				fixed (ulong* inPix = &FPayload.Data[0])
				{
					ulong* mov = inPix;

					int extraStride = FOutput.Image.ImageAttributes.Stride - FOutput.Image.Width;

					if (FPayload.Balanced)
					{
						byte high = frame % 2 == 0 ? (byte)255 : (byte)0;
						byte low = high == (byte)255 ? (byte)0 : (byte)255;

						frame /= 2;

						for (uint y = 0; y < FPayload.Height; y++)
						{
							for (uint x = 0; x < FPayload.Width; x++)
								*outPix++ = (*mov++ & (ulong)1 << frame) == (ulong)1 << frame? high : low;
							outPix += extraStride;
						}
					}
					else
						for (uint y = 0; y < FPayload.Height; y++)
						{
							for (uint x = 0; x < FPayload.Width; x++)
								*outPix++ = (*mov++ & (ulong)1 << frame) == (ulong)1 << frame ? (byte)255 : (byte)0;
							outPix += extraStride;
						}
				}
			}
		}

		bool Allocated
		{
			get
			{
				return FPayload != null;
			}
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "Encode", Category = "CV.StructuredLight", Help = "Encode structured light patterns", Author = "elliotwoods", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class EncodeNode : IGeneratorNode<EncodeInstance>
	{
		#region fields & pins
		[Input("Frame", IsSingle=true, MinValue=0)]
		IDiffSpread<int> FPinInFrame;

		[Input("Payload", IsSingle=true)]
		IDiffSpread<IPayload> FPinInPayload;

		[Output("Timestamps")]
		ISpread<TimestampRegister> FPinOutTimestamps;

		IPayload FPayload;

		bool FNeedsUpdate = false;
		#endregion fields&pins

		[ImportingConstructor()]
		public EncodeNode()
		{

		}

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			if (FPinInFrame.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Frame = FPinInFrame[i];

			if (FPinInPayload.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Payload = FPinInPayload[i];

			if (SpreadChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
					FPinOutTimestamps[i] = FProcessor[i].Timestamps;
			}
		}
	}
}
