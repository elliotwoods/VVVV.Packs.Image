using xiApi.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.Ximea
{
    #region PluginInfo
    [PluginInfo(Name = "ListDevices",
                Category = "Ximea",
                Version = "",
                Help = "List Ximea capture devices",
                Tags = "")]
    #endregion PluginInfo
    public class ListDevicesNode : IPluginEvaluate
    {
        #region fields & pins
		[Input("Refresh", IsBang = true, IsSingle = true)]
		ISpread<bool> FInRefresh;

		[Input("Get properties", IsSingle = true)]
		ISpread<bool> FInGetProperties;

        [Output("ID")]
        ISpread<int> FOutDeviceID;

        [Output("Name")]
        ISpread<string> FOutDeviceName;

        [Output("Type")]
        ISpread<string> FOutDeviceType;

        [Output("Serial")]
        ISpread<string> FOutDeviceSerial;

        bool firstRun = true;

        #endregion fields & pins

        public void Evaluate(int SpreadMax)
        {
            if (firstRun || FInRefresh[0])
            {
                firstRun = false;

                xiCam queryDevice = new xiCam();

                int deviceCount = queryDevice.GetNumberDevices();

                FOutDeviceID.SliceCount = deviceCount;
                FOutDeviceName.SliceCount = deviceCount;
				FOutDeviceType.SliceCount = deviceCount;
                FOutDeviceSerial.SliceCount = deviceCount;

                for (int i =0; i<deviceCount; i++)
                {
					string name = "";
					string type = "";
					string serial = "";
					
					if (FInGetProperties[0])
					{
						queryDevice.OpenDevice(i);
						name = queryDevice.GetParamString(PRM.DEVICE_NAME);
						type = queryDevice.GetParamString(PRM.DEVICE_TYPE);
						serial = queryDevice.GetParamString(PRM.DEVICE_SN);
						queryDevice.CloseDevice();
					}
					
					FOutDeviceID[i] = i;
					FOutDeviceName[i] = name;
					FOutDeviceSerial[i] = serial;
					FOutDeviceType[i] = type;
                }
            }
        }
    }
}
