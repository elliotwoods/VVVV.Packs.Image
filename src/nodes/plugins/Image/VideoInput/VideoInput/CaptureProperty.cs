#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;
using VideoInputSharp;
using System.Collections.Generic;

#endregion usings

namespace VVVV.Nodes.OpenCV.VideoInput
{
	#region PluginInfo
	[PluginInfo(Name = "CaptureProperty", Category = "CV.Image", Version = "DirectShow", Help = "Set properties for DirectShow video", Tags = "", Author="elliotwoods", AutoEvaluate = true)]
	#endregion PluginInfo
	public class CapturePropertyNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Property")]
		IDiffSpread<Property> FPinInProperty;

		[Input("Value", MinValue=0.0, MaxValue=1.0)]
		IDiffSpread<float> FPinInValue;

		[Output("PropertyPair", IsSingle=true)]
		ISpread<Dictionary<Property, float>> FPinOutOutput;

		[Import]
		ILogger FLogger;

		Dictionary<Property, float> FOutput = new Dictionary<Property, float>();

		#endregion fields & pins

		[ImportingConstructor]
		public CapturePropertyNode(IPluginHost host)
		{

		}

		bool FFirstRun = true;
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FPinInProperty.IsChanged || FPinInValue.IsChanged)
			{
				FOutput.Clear();
				for (int i = 0; i < SpreadMax; i++)
				{
					if (!FOutput.ContainsKey(FPinInProperty[i]))
						FOutput.Add(FPinInProperty[i], FPinInValue[i]);
				}
				FPinOutOutput[0] = FOutput;
			}
		}
	}
}
