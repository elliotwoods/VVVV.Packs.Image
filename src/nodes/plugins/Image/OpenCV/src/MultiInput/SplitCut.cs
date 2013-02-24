using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using System.Drawing;

namespace VVVV.Nodes.OpenCV
{
	#region PluginInfo
	[PluginInfo(Name = "SplitCut", Category = "OpenCV", Version = "", Help = "Split a single input image into multiple output images based on y offsets", Author = "elliotwoods", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class SplitCutNode : IPluginEvaluate
	{
		#region fields
		[Input("Input", IsSingle = true)]
		IDiffSpread<CVImageLink> FPinInInput;

		[Input("End Y", MinValue = 0, MaxValue = 1)]
		IDiffSpread<double> FPinInOffset;

		[Output("Output")]
		ISpread<CVImageLink> FPinOutOutput;

		CVImageOutputSpread FOutput;
		CVImageInput FInput = new CVImageInput();

		Spread<int> FStart = new Spread<int>(0);
		Spread<int> FSize = new Spread<int>(0);
		bool FFirstRun = true;
		bool FResizeOut = false;

		#endregion fields

		void IPluginEvaluate.Evaluate(int SpreadMax)
		{
			if (FFirstRun)
			{
				FFirstRun = false;
				FOutput = new CVImageOutputSpread(FPinOutOutput);
			}

			if (FPinInInput.IsChanged || FPinInOffset.IsChanged)
			{
				if (FPinInInput[0] != null)
				{
					FInput.Connect(FPinInInput[0]);
					FInput.ImageAttributesUpdate += new EventHandler<ImageAttributesChangedEventArgs>(FInput_ImageAttributesUpdate);
					FInput.ImageUpdate += new EventHandler(FInput_ImageUpdate);
					Initialise();
				}
				else
				{
					FInput.Disconnect();
				}
			}
			else if (FPinInOffset.IsChanged)
			{
				Initialise();
			}

			if (FResizeOut)
			{
				FOutput.AlignOutputPins();
			}
		}

		void FInput_ImageUpdate(object sender, EventArgs e)
		{
			FInput.LockForReading();
			try
			{
				int width = FInput.ImageAttributes.Width * (int)FInput.ImageAttributes.BytesPerPixel;
				for (int i = 0; i < FPinInOffset.SliceCount; i++)
				{
					if (FOutput[i].Image.Allocated)
					{
						if (FPinInOffset[i] * width + FOutput[i].Image.ImageAttributes.BytesPerFrame > FInput.ImageAttributes.BytesPerFrame)
							continue;
						FOutput[i].Image.SetPixels(FInput.Image.Data + width * FStart[i]);
						//FOutput[i].Image.SetImage(FInput.Image);
						FOutput[i].Send();
					}
				}
			}
			finally
			{
				FInput.ReleaseForReading();
			}
		}

		void FInput_ImageAttributesUpdate(object sender, ImageAttributesChangedEventArgs e)
		{
			Initialise();
		}

		void Initialise()
		{
			if (FInput.Connected && FInput.Allocated)
			{
				int SpreadMax = FPinInOffset.SliceCount;
				FOutput.SliceCount = SpreadMax;
				FStart.SliceCount = SpreadMax;
				FSize.SliceCount = SpreadMax;
				int lastY = 0;
				for (int i = 0; i < SpreadMax; i++)
				{
					FStart[i] = lastY;
					FSize[i] = Math.Min((int)(FPinInOffset[i] * (double)FInput.ImageAttributes.Height), FInput.ImageAttributes.Height) - lastY;
					lastY += FSize[i];

					FOutput[i].Image.Initialise(new Size(FInput.ImageAttributes.Width, FSize[i]), FInput.ImageAttributes.ColourFormat);
				}
			}

			FResizeOut = true;
		}
	}
}
