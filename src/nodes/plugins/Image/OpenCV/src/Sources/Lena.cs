using Emgu.CV;
using Emgu.CV.Structure;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System;
using System.IO;
 
using VVVV.CV.Core;

namespace VVVV.CV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "Lena", Category = "CV.Image", Tags = "sample")]
	#endregion PluginInfo
	public class Lena : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Output("Output")]
		ISpread<CVImageLink> FPinOutput;

		[Import]
		ILogger FLogger;
		#endregion fields&pins

		bool FFirstRun = true;
		CVImageOutput FOutput = new CVImageOutput();

		public void Evaluate(int SpreadMax)
		{
			if (FFirstRun)
			{
				FPinOutput[0] = FOutput.Link;
				var dir = Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(typeof(Lena)).Location);
				FOutput.Image.LoadFile(dir + "\\..\\..\\assets\\images\\lena.jpg");
				FOutput.Send();

				FFirstRun = false;
			}
		}

		public void Dispose()
		{
			FOutput.Dispose();
		}
	}
}
