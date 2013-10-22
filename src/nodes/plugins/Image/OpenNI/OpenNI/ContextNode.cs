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
	[PluginInfo(Name = "Context", Category = "OpenCV", Version = "OpenNI", Help = "OpenNI context loader", Tags = "", AutoEvaluate=true)]
	#endregion PluginInfo
	public class ContextNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Enabled")]
		ISpread<bool> FPinInEnabled;

        [Input("Creation Info")]
        ISpread<string> FPinInNodes;

		[Output("Context")]
		ISpread<Device> FPinOutContext;

		[Output("Status")]
		ISpread<String> FPinOutStatus;

		[Import]
		ILogger FLogger;

		string FStatus = "";

        Spread<Device> FState = new Spread<Device>(0);
		License FLicense = new License();
		#endregion fields & pins

		[ImportingConstructor]
		public ContextNode(IPluginHost host)
		{
            FLicense.Vendor = "PrimeSense";
            FLicense.Key = "0KOIk2JeIBYClPWVnMoRKn5cdY4";
		}

		public void Dispose()
		{
            for (int i=0; i<FState.SliceCount; i++)
			    Close(i);
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
            CheckSliceCount(SpreadMax);

			for (int i = 0; i < SpreadMax; i++)
			{
				if (FPinInEnabled[i] && !FState[i].Running)
					Open(i);
				else if (!FPinInEnabled[i] && FState[i].Running)
					Close(i);
                if (FPinOutContext[i] != FState[i])
                    FPinOutContext[i] = FState[i];
			}
		}

        void CheckSliceCount(int SpreadMax)
        {
            if (SpreadMax == FState.SliceCount)
                return;

            for (int i = FState.SliceCount; i < SpreadMax; i++)
                FState.Add<Device>(new Device());
            for (int i = SpreadMax; i < FState.SliceCount; i++)
            {
                FState[i] = null;
                FState.RemoveAt<Device>(i);
            }

            FPinOutContext.SliceCount = FState.SliceCount;
            FPinOutStatus.SliceCount = FState.SliceCount;
        }

        void Open(int i)
		{
			try
			{
				Close(i);


                Device state = FState[i];
                var context = new Context();
				FState[i].Context = context;
                context.AddLicense(FLicense);
                context.GlobalMirror = false;

                NodeInfoList list = context.EnumerateProductionTrees(global::OpenNI.NodeType.Device, null);
                NodeInfo node = null;
                if (FPinInNodes[i] != "")
                {
                    foreach (NodeInfo nodeitem in list)
                    {
                        if (nodeitem.CreationInfo == FPinInNodes[i])
                        {
                            node = nodeitem;
                            break;
                        }
                    }

                    if (node == null)
                        throw (new Exception("This device is unavailable. Check upstream ListDevices node"));

                    context.CreateProductionTree(node);
                }

                state.DepthGenerator = new DepthGenerator(context);
                MapOutputMode depthMode = new MapOutputMode();
                depthMode.FPS = 30;
                depthMode.XRes = 320;
                depthMode.YRes = 240;

                state.DepthGenerator.MapOutputMode = depthMode;
                state.DepthGenerator.StartGenerating();

                state.Start();

                FPinOutContext[i] = state;
                FPinOutStatus[i] = "OK";
			}
			catch (Exception e)
			{
				Close(i);
				FPinOutStatus[i] = e.Message;
			}
		}

		void Close(int i)
		{
			if (FState[i].Running)
			{
				FState[i].Stop();
                if (FState[i].Context != null)
				{
                    FState[i].Context.Release();
				}
			}
		}

	}
}
