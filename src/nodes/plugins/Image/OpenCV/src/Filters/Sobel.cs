#region using
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using System;
using VVVV.Utils.VColor;

#endregion

namespace VVVV.Nodes.OpenCV
{
	public class SobelInstance : IFilterInstance
	{
		private int FAperture = 5;
		public int Aperture
		{
			set
			{
				if (value < 3)
					value = 3;

				if (value > 7)
					value = 7;

				value += (value + 1) % 2;

				FAperture = value;
			}
		}

		private int FXOrder = 1;
		public int XOrder
		{
			set
			{
				if (value < 0)
					value = 0;
				if (value > 8)
					value = 8;
				FXOrder = value;
			}
		}

		private int FYOrder = 1;
		public int YOrder
		{
			set
			{
				if (value < 0)
					value = 0;
				if (value > 8)
					value = 8;
				FYOrder = value;
			}
		}

		public override void Allocate()
		{
			FOutput.Image.Initialise(FInput.ImageAttributes);
		}

		public override void Process()
		{
			FInput.LockForReading();
			try
			{
				CvInvoke.cvSobel(FInput.CvMat, FOutput.CvMat, FXOrder, FYOrder, FAperture);
			}
			finally
			{
				FInput.ReleaseForReading();
			}
			FOutput.Send();
		}

	}

	#region PluginInfo
	[PluginInfo(Name = "Sobel", Category = "OpenCV", Version = "", Help = "Find the 2D derivative of an image using the Sobel filter", Author = "elliotwoods", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class SobelNode : IFilterNode<SobelInstance>
	{
		[Input("X Order", DefaultValue = 1)]
		IDiffSpread<int> FInXOrder;

		[Input("Y Order", DefaultValue = 1)]
		IDiffSpread<int> FInYOrder;

		[Input("Aperture size", MinValue = 3, MaxValue = 7, DefaultValue = 3)]
		IDiffSpread<int> FInApertureSize;

		protected override void Update(int instanceCount, bool spreadChanged)
		{
			if (FInXOrder.IsChanged)
				for (int i = 0; i < instanceCount; i++)
					FProcessor[i].XOrder = FInXOrder[i];

			if (FInYOrder.IsChanged)
				for (int i = 0; i < instanceCount; i++)
					FProcessor[i].YOrder = FInYOrder[i];

			if (FInApertureSize.IsChanged)
				for (int i = 0; i < instanceCount; i++)
					FProcessor[i].Aperture = FInApertureSize[i];

		}
	}
}
