using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.CV.Nodes.Calibration
{
	#region PluginInfo
	[PluginInfo(Name = "Reader",
				Category = "CV.Transform",
				Version = "Intrinsics",
				Help = "Read intrinsics from a file",
				Tags = "",
				AutoEvaluate=true)]
	#endregion PluginInfo
	public class IntrinsicsReaderNode : IPluginEvaluate
	{
		[Input("Filename", StringType = StringType.Filename)]
		ISpread<string> FInFilename;

		[Input("Read", IsBang = true)]
		ISpread<bool> FInRead;

		[Output("Intrinsics")]
		ISpread<Intrinsics> FOutIntrinsics;

		[Output("Status")]
		ISpread<string> FOutStatus;

		IFormatter FFormatter = new BinaryFormatter();

		public void Evaluate(int SpreadMax)
		{
			FOutStatus.SliceCount = SpreadMax;

			for (int i = 0; i < SpreadMax; i++)
			{
				if (FInRead[i])
				{
					try
					{
						var file = new FileStream(FInFilename[i], FileMode.Open);
						FOutIntrinsics[i] = (Intrinsics)FFormatter.Deserialize(file);
						FOutStatus[i] = "OK";
						file.Close();
					}
					catch (Exception e)
					{
						FOutStatus[i] = e.Message;
					}
				}
			}
		}
	}
}
