using System;
using VVVV.CV.Core;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace VVVV.CV.Nodes
{
	public class PipetInstance : IDestinationInstance
	{
		private Object FLock = new Object();

		public override void Allocate()
		{
			
		}

		public int ChannelCount { get; private set; }

		private ISpread<Vector2D> FLookup;
		public ISpread<Vector2D> Lookup
		{
			set
			{
				lock (FLock)
				{
					FLookup = value;
				}
			}
		}

		private readonly Spread<Spread<double>> FReturn = new Spread<Spread<double>>(0);
		public Spread<Spread<double>> Return
		{
			get
			{
				lock (FLock)
				{
					return FReturn.Clone();
				}
			}
		}

		public override void Process()
		{
			if (FLookup == null) return;

			lock (FLock)
			{
				ChannelCount = ImageUtils.ChannelCount(FInput.ImageAttributes.ColorFormat);
				FReturn.SliceCount = FLookup.SliceCount;

				for (var i = 0; i < FLookup.SliceCount; i++)
				{
					FReturn[i] = ImageUtils.GetPixelAsDoubles(FInput.Image, (uint) FLookup[i].x, (uint) FLookup[i].y);
				}
			}
		}
	}

	[PluginInfo(Name = "Pipet", Category = "CV.Image", Help = "Pipet in image using pixel coordinates", Tags = "")]
	public class PipetNode : IDestinationNode<PipetInstance>
	{
		[Input("Position", DimensionNames=new string[1]{"px"})]
		IDiffSpread<Vector2D> FPinInInput;
		
		[Output("Output")]
		ISpread<ISpread<double>> FPinOutput;

		protected override bool OneInstancePerImage()
		{
			return true;
		}

		protected override void Update(int instanceCount, bool spreadChanged)
		{
			FPinOutput.SliceCount = instanceCount;
			
			if (FPinInInput.IsChanged)
				for (int i = 0; i < instanceCount; i++)
				{
					FProcessor[i].Lookup = FPinInInput;
				}

			Output(instanceCount);
		}

		private void Output(int instanceCount)
		{
			//since we cant output an ISpread<ISpread<ISpread<double>>>
			//we have to do it the long way...

			var returned = new Spread<Spread<Spread<double>>>(instanceCount);
			
			var count = 0;
			var counts = new Spread<int>(instanceCount);

			for (var i = 0; i < instanceCount; i++)
			{
				returned[i] = FProcessor[i].Return;
				counts[i] = returned[i].SliceCount;
				count += counts[i];
			}

			FPinOutput.SliceCount = count;

			var offset = 0;

			for (var i = 0; i < instanceCount; i++)
			{
				for (var j = 0; j < counts[i]; j++)
				{
					FPinOutput[offset + j].SliceCount = FProcessor[i].ChannelCount;
					for (var c = 0; c < FProcessor[i].ChannelCount; c++)
						FPinOutput[offset + j][c] = returned[i][j][c];
				}
				offset += counts[i];
			}
		}
	}
}
