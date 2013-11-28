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

using OpenNI;

#endregion usings

namespace VVVV.Nodes.OpenCV.OpenNI
{
    #region PluginInfo
	[PluginInfo(Name = "ListDevices", Category = "CV.Image", Version = "OpenNI", Help = "OpenNI context loader", Tags = "", AutoEvaluate = true)]
    #endregion PluginInfo
    public class ListDevicesNode : IPluginEvaluate, IDisposable
    {
        #region fields & pins
        [Input("Refresh", IsBang = true, IsSingle = true)]
        ISpread<bool> FPinInRefresh;

        [Output("Name")]
        ISpread<string> FPinOutName;

        [Output("Vendor")]
        ISpread<string> FPinOutVendor;

        [Output("Version")]
        ISpread<double> FPinOutVersion;

        [Output("Creation Info")]
        ISpread<string> FPinOutCreationInfo;

        [Output("Status")]
        ISpread<String> FPinOutStatus;

        [Import]
        ILogger FLogger;
        #endregion fields & pins

        [ImportingConstructor]
        public ListDevicesNode(IPluginHost host)
        {

        }

        public void Dispose()
        {
            
        }

        bool FFirstRun = true;
        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            if (FFirstRun || FPinInRefresh[0])
            {
                FFirstRun = false;
                Refresh();
            }
        }

        License FLicense = new License();
        Context FContext = new Context();
        List<NodeInfo> FNodeList = new List<NodeInfo>();
        void Refresh()
        {
            try
            {
                FLicense.Vendor = "PrimeSense";
                FLicense.Key = "0KOIk2JeIBYClPWVnMoRKn5cdY4";
                FContext.AddLicense(FLicense);
                
                NodeInfoList list = FContext.EnumerateProductionTrees(global::OpenNI.NodeType.Device, null);
                FNodeList.Clear();
                foreach(var node in list)
                    FNodeList.Add(node);

                FPinOutName.SliceCount = FNodeList.Count;
                FPinOutVendor.SliceCount = FNodeList.Count;
                FPinOutVersion.SliceCount = FNodeList.Count;
                FPinOutCreationInfo.SliceCount = FNodeList.Count;
                for (int i=0; i<FNodeList.Count; i++) 
                {
                    var node = FNodeList[i];
                    FPinOutName[i] = node.Description.Name;
                    FPinOutVendor[i] = node.Description.Vendor;
                    FPinOutVersion[i] = (double)node.Description.Version.Major + ((double)FNodeList[i].Description.Version.Minor / 1000.0);
                    FPinOutCreationInfo[i] = node.CreationInfo;
                }

                FPinOutStatus[0] = "OK";
            }
            catch (Exception e)
            {
                FPinOutStatus[0] = e.Message;
            }
        }
    }
}
