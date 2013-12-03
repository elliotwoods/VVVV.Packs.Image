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

namespace VVVV.CV.Nodes
{
	public class NoiseInstance : IStaticGeneratorInstance
	{
		string FLoadedImage = "";
		Size FSize = new Size(32, 32);
		float FRadius = 1;

		Random FRandom = new Random();
		public override bool NeedsThread()
		{
			return false;
		}

		public void Refresh()
		{
			FOutput.Image.Initialise(FSize, TColorFormat.RGBA32F);
			FillRandomValues();
			FOutput.Send();
		}

		public int Dimension
		{
			set
			{
				if (value < 1)
					value = 1;

				if (value > 4096)
					value = 4096;

				double valueLog2 = Math.Log((double)value) / Math.Log(2.0d);
				value = 1 << (int)valueLog2;

				FSize.Width = value;
				FSize.Height = value;

				Refresh();
			}
		}

		private unsafe void FillRandomValues()
		{
			float* xyzt = (float*)FOutput.Data.ToPointer();

			int width = FSize.Width;
			int height = FSize.Height;
			for (int i = 0; i < width * height; i++)
			{
				*xyzt++ = ((float)FRandom.NextDouble() - 0.5f) * 2.0f * FRadius;
				*xyzt++ = ((float)FRandom.NextDouble() - 0.5f) * 2.0f * FRadius;
				*xyzt++ = ((float)FRandom.NextDouble() - 0.5f) * 2.0f * FRadius;
				*xyzt++ = 1;
			}
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "Noise", Category = "CV.Image", Help = "Generator 32F noise", Author = "", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class NoiseNode : IGeneratorNode<NoiseInstance>
	{
		#region fields & pins
		[Input("Refresh", IsBang = true)]
		ISpread<bool> FRefresh;

		[Input("Dimension", DefaultValue = 32, MinValue=1, MaxValue=4096)]
		IDiffSpread<int> FDimension;

		[Import()]
		ILogger FLogger;
		#endregion fields&pins

		[ImportingConstructor()]
		public NoiseNode()
		{

		}

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			for (int i = 0; i < InstanceCount; i++)
				if (FRefresh[i])
					FProcessor[i].Refresh();

			if (FDimension.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Dimension = FDimension[i];
		}
	}
}
