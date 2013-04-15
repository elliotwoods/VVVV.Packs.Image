using OptiTrackNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.OptiTrack
{
	#region PluginInfo
	[PluginInfo(Name = "Info",
				Category = "OptiTrack",
				Version = "",
				Help = "Outputs information about the OptiTrack camera",
				Tags = "")]
	#endregion PluginInfo
	public class InfoNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Device")]
		IDiffSpread<MCamera> FInCamera;

		[Input("Update", IsBang = true)]
		ISpread<bool> FInUpdate;

		[Output("Width")]
		ISpread<int> FOutWidth;

		[Output("Height")]
		ISpread<int> FOutHeight;

		[Output("Model")]
		ISpread<int> FOutModel;

		[Output("Serial")]
		ISpread<int> FOutSerial;

		[Output("IR Filter Switch Available")]
		ISpread<bool> FOutIRFilterSwitchAvailable;

		[Output("Camera Temperature")]
		ISpread<double> FOutCameraTemperature;

		Context FContext = new Context();

		#endregion fields & pins

		public void Evaluate(int SpreadMax)
		{
			bool allupdate = (FInCamera.IsChanged);

			if (FInCamera.SliceCount == 0 || FInCamera[0] == null)
			{
				SetOutputCount(0);
				return;
			}

			SetOutputCount(SpreadMax);

			for (int i = 0; i < SpreadMax; i++)
			{
				if (allupdate || FInUpdate[i])
				{
					FOutWidth[i] = FInCamera[i].Width();
					FOutHeight[i] = FInCamera[i].Height();
					FOutModel[i] = FInCamera[i].Model();
					FOutSerial[i] = FInCamera[i].Serial();
					FOutIRFilterSwitchAvailable[i] = FInCamera[i].IsFilterSwitchAvailable();
					FOutCameraTemperature[i] = FInCamera[i].CameraTemp();
				}
			}
		}

		void SetOutputCount(int count)
		{
			FOutWidth.SliceCount = count;
			FOutHeight.SliceCount = count;
			FOutModel.SliceCount = count;
			FOutSerial.SliceCount = count;
			FOutIRFilterSwitchAvailable.SliceCount = count;
			FOutCameraTemperature.SliceCount = count;
		}
	}
}
