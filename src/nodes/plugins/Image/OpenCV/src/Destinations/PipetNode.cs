#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using ThreadState = System.Threading.ThreadState;
using System.Collections.Generic;

#endregion usings

namespace VVVV.Nodes.OpenCV
{
	public class PipetInstance : IDestinationInstance
	{
		private Object FLock = new Object();

		public override void Allocate()
		{
			
		}

		private int FChannelCount = 0;
		public int ChannelCount
		{
			get
			{
				return FChannelCount;
			}
		}

		private ISpread<Vector2D> FLookup = null;
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

		private Spread<Spread<double>> FReturn = new Spread<Spread<double>>(0);
		public Spread<Spread<double>> Return
		{
			get
			{
				lock (FLock)
				{
					Spread<Spread<double>> output = FReturn.Clone() as Spread<Spread<double>>;
					return output;
				}
			}
		}

		public override void Process()
		{
			if (FLookup == null)
				return;
			else
				lock (FLock)
				{
					FChannelCount = ImageUtils.ChannelCount(FInput.ImageAttributes.ColourFormat);
					FReturn.SliceCount = FLookup.SliceCount;

					for (int i = 0; i < FLookup.SliceCount; i++)
					{
						FReturn[i] = ImageUtils.GetPixelAsDoubles(FInput.Image, (uint) FLookup[i].x, (uint) FLookup[i].y);
					}
				}
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "Pipet", Category = "OpenCV", Version = "", Help = "Pipet in image", Tags = "")]
	#endregion PluginInfo
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

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			FPinOutput.SliceCount = InstanceCount;
			
			if (FPinInInput.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].Lookup = FPinInInput;
				}

			Output(InstanceCount);
		}

		private void Output(int InstanceCount)
		{
			//since we cant output an ISpread<ISpread<ISpread<double>>>
			//we have to do it the long way...

			Spread<Spread<Spread<double>>> returned = new Spread<Spread<Spread<double>>>(InstanceCount);
			
			int count = 0;
			Spread<int> counts = new Spread<int>(InstanceCount);

			for (int i = 0; i < InstanceCount; i++)
			{
				returned[i] = FProcessor[i].Return;
				counts[i] = returned[i].SliceCount;
				count += counts[i];
			}

			FPinOutput.SliceCount = count;

			int offset = 0;

			for (int i=0; i<InstanceCount; i++)
			{
				for (int j=0; j<counts[i]; j++)
				{
					FPinOutput[offset + j].SliceCount = FProcessor[i].ChannelCount;
					for (int c=0; c<FProcessor[i].ChannelCount; c++)
						FPinOutput[offset + j][c] = returned[i][j][c];
				}
				offset += counts[i];
			}
		}
	}
}
