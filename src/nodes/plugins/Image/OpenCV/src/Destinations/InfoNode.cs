#region usings
using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;

using SlimDX;
using SlimDX.Direct3D9;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.EX9;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Utils.SlimDX;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;

using System.Collections.Generic;
using Emgu.CV.CvEnum;
using System.Drawing;
using VVVV.CV.Core;

#endregion usings

namespace VVVV.CV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "Info",
				Category = "CV.Image",
				Help = "Outputs information about the Images in a spread",
				Tags = "")]
	#endregion PluginInfo
	public class InfoNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Image")]
		ISpread<CVImageLink> FPinInImage;

		[Output("Allocated")]
		ISpread<bool> FPinOutAllocated;

		[Output("Width")]
		ISpread<int> FPinOutWidth;

		[Output("Height")]
		ISpread<int> FPinOutHeight;

		[Output("Format")]
		ISpread<string> FPinOutFormat;

		[Import]
		ILogger FLogger;

		bool FInitialised = false;
		CVImageInputSpread FInputs;
		#endregion fields & pins

		// import host and hand it to base constructor
		[ImportingConstructor()]
		public InfoNode(IPluginHost host)
		{
			
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (!FInitialised)
			{
				FInputs = new CVImageInputSpread(FPinInImage);
				FInitialised = true;
			}

			FInputs.CheckInputSize();

			Output(FInputs.SliceCount);
		}
		
		private void Output(int count)
		{
			FPinOutAllocated.SliceCount = count;
			FPinOutWidth.SliceCount = count;
			FPinOutHeight.SliceCount = count;
			FPinOutFormat.SliceCount = count;

			for (int i = 0; i < count; i++)
			{
				FPinOutAllocated[i] = FInputs[i].Allocated;
				if (!FInputs[i].Allocated)
				{
					FPinOutWidth[i] = 0;
					FPinOutHeight[i] = 0;
					FPinOutFormat[i] = "";
				}
				else
				{
					FPinOutWidth[i] = FInputs[i].ImageAttributes.Width;
					FPinOutHeight[i] = FInputs[i].ImageAttributes.Height;
					FPinOutFormat[i] = ImageUtils.AsString(FInputs[i].ImageAttributes.ColorFormat);
				}
			}
		}
	}
}
