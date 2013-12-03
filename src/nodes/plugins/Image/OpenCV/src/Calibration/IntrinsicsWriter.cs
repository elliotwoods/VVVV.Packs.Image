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
	[PluginInfo(Name = "Writer",
				Category = "CV.Transform",
				Version = "Intrinsics",
				Help = "Write intrinsics to a file",
				Tags = "",
				AutoEvaluate=true)]
	#endregion PluginInfo
	public class IntrinsicsWriterNode : IPluginEvaluate
	{
		[Input("Intrinsics")]
		ISpread<Intrinsics> FInIntrinsics;

		[Input("Filename", StringType = StringType.Filename)]
		ISpread<string> FInFilename;

		[Input("Write", IsBang = true)]
		ISpread<bool> FInWrite;

		[Output("Status")]
		ISpread<string> FOutStatus;

		IFormatter FFormatter = new BinaryFormatter();

		public void Evaluate(int SpreadMax)
		{
			FOutStatus.SliceCount = SpreadMax;

			for (int i = 0; i < SpreadMax; i++)
			{
				if (FInWrite[i] && FInIntrinsics[i] != null)
				{
					try
					{
						var file = new FileStream(FInFilename[i], FileMode.Create);
						FFormatter.Serialize(file, FInIntrinsics[i]);
						file.Close();
						FOutStatus[i] = "OK";
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
