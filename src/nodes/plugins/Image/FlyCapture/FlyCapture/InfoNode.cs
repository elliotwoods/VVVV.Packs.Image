#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

using ThreadState = System.Threading.ThreadState;
using System.Collections.Generic;

using FlyCapture2;
using FlyCapture2Managed;

using VVVV.CV.Core;

#endregion usings

namespace VVVV.Nodes.FlyCapture
{
	abstract public class InfoNodeBase : IPluginEvaluate
	{
		#region fields & pins
		[Input("Info")]
		protected IDiffSpread<CameraInfo> FPinInInfo;
		#endregion

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FPinInInfo.IsChanged)
				Refresh();
		}

		void Refresh()
		{
			int nDevices = FPinInInfo.SliceCount;
			CameraInfo info;

			if (FPinInInfo[0] == null)
				nDevices = 0;

			SetOutputSliceCount(nDevices);

			for (int i = 0; i < nDevices; i++)
			{
				info = FPinInInfo[i];
				SetOutput(info, i);
			}
		}

		abstract protected void SetOutputSliceCount(int count);

		abstract protected void SetOutput(CameraInfo info, int i);
	}

	#region PluginInfo
	[PluginInfo(Name = "Info", Version="General", Category = "FlyCapture", Help = "Give device info for FlyCapture camera devices", Tags = "")]
	#endregion PluginInfo
	public class InfoGeneralNode : InfoNodeBase
	{
		#region fields & pins
		[Output("Vendor")]
		ISpread<string> FVendor;

		[Output("Model")]
		ISpread<string> FModel;

		[Output("Serial")]
		ISpread<int> FSerial;

		[Output("Sensor")]
		ISpread<string> FSensor;

		[Output("Resolution")]
		ISpread<string> FResolution;

		[Output("Color")]
		ISpread<bool> FColor;
		#endregion fields & pins

		protected override void SetOutputSliceCount(int count)
		{
			FVendor.SliceCount = FModel.SliceCount = FSerial.SliceCount = FSensor.SliceCount = FResolution.SliceCount = FColor.SliceCount = count;
		}

		protected override void SetOutput(CameraInfo info, int i)
		{
			FVendor[i] = info.vendorName;
			FModel[i] = info.modelName;
			FSerial[i] = (int)info.serialNumber;
			FSensor[i] = info.sensorInfo;
			FResolution[i] = info.sensorResolution;
			FColor[i] = info.isColorCamera;
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "Info", Version = "Driver", Category = "FlyCapture", Help = "Give device info for FlyCapture camera devices", Tags = "")]
	#endregion PluginInfo
	public class InfoBusNode : InfoNodeBase
	{
		#region fields & pins
		[Output("Driver")]
		ISpread<string> FDriver;

		[Output("Type")]
		ISpread<string> FType;

		[Output("Firmware")]
		ISpread<string> FFirmware;

		[Output("Bus")]
		ISpread<int> FBus;

		[Output("Bus Speed")]
		ISpread<string> FBusSpeed;

		[Output("PCIe Speed")]
		ISpread<string> FPCIeSpeed;

		[Output("User Defined Name")]
		ISpread<string> FUserName;

		[Output("Node number")]
		ISpread<int> FNodeNumber;
		#endregion fields & pins

		protected override void SetOutputSliceCount(int count)
		{
			FDriver.SliceCount = FType.SliceCount = FBus.SliceCount = FBusSpeed.SliceCount = FPCIeSpeed.SliceCount = FFirmware.SliceCount = FUserName.SliceCount = FNodeNumber.SliceCount = count;
		}

		protected override void SetOutput(CameraInfo info, int i)
		{
			FDriver[i] = info.driverName;
			FType[i] = info.driverType.ToString();
			FBus[i] = (int)info.busNumber;
			FBusSpeed[i] = info.maximumBusSpeed.ToString();
			FPCIeSpeed[i] = info.pcieBusSpeed.ToString();
			FFirmware[i] = info.firmwareVersion;
			FUserName[i] = info.userDefinedName;
			FNodeNumber[i] = info.nodeNumber;
			/*
			info.firmwareBuildTime;
			info.iidcVersion;
			*/
		}
	}
}
