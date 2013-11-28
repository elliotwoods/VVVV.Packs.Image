using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using System.ComponentModel.Composition;
using VVVV.Core.Logging;
using CLEyeMulticam;
using VVVV.PluginInterfaces.V1;

namespace VVVV.Nodes.OpenCV.CLEye
{
	#region PluginInfo
	[PluginInfo(Name = "CaptureProperty", Category = "CV.Image", Version = "CLEye", Help = "Set properties for CLEye camera", Tags = "", Author = "elliotwoods", AutoEvaluate = true)]
	#endregion PluginInfo
	public class CapturePropertyNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Property")]
		IDiffSpread<CLEyeCameraParameter> FPinInProperty;

		[Input("Value")]
		IDiffSpread<int> FPinInValue;

		[Output("PropertyPair", IsSingle = true)]
		ISpread<Dictionary<CLEyeCameraParameter, int>> FPinOutOutput;

		[Import]
		ILogger FLogger;

		Dictionary<CLEyeCameraParameter, int> FOutput = new Dictionary<CLEyeCameraParameter, int>();

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
				FOutput = new Dictionary<CLEyeCameraParameter, int>();
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
