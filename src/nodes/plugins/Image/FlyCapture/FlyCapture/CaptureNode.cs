#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

using System.Collections.Generic;

using FlyCapture2;
using FlyCapture2Managed;

using VVVV.CV.Core;
using VVVV.CV.Core;

#endregion usings

namespace VVVV.Nodes.FlyCapture
{
	public class CaptureInstance : IGeneratorInstance
	{
		bool FRunning = false;
		ManagedCamera FCamera = new ManagedCamera();
		ManagedImage FImage = new ManagedImage();
		private ManagedPGRGuid FGuid;
		public ManagedPGRGuid Guid
		{
			set
			{
				FGuid = value;
				Open();
			}
		}

		float FFramerate;
		public float Framerate
		{
			get
			{
				return FFramerate;
			}
		}

		string FMode;
		public string Mode
		{
			get
			{
				return FMode;
			}
		}

        public override bool Open()
		{
			try
			{
                if (FGuid == null) {
                    throw new Exception("No Camera GUID specified");
                }

				FCamera.Connect(FGuid);
				VideoMode mode = new VideoMode();
				FrameRate rate = new FrameRate();

				FCamera.GetVideoModeAndFrameRate(ref mode, ref rate);
				FMode = mode.ToString();
				FFramerate = Utils.GetFramerate(rate);

				FRunning = true;
				FCamera.StartCapture(CaptureCallback);

                ReAllocate();

				Status = "OK";
                return true;
			}
			catch(Exception e)
			{
				FRunning = false;
				Status = e.Message;
                return false;
			}
		}

        public override void Close()
		{
			if (!FRunning)
				return;

			try
			{
				FCamera.StopCapture();
				FCamera.Disconnect();
				Status = "Closed";
			}
			catch (Exception e)
			{
				Status = e.Message;
			}
			FRunning = false;
		}

        public override void Allocate()
        {
            //allocation is hackily performed on first callback
        }

		public unsafe void CaptureCallback(ManagedImage image)
		{
            if (!FOutput.Image.Allocated)
                FOutput.Image.Initialise(new Size((int)image.cols, (int)image.rows), Utils.GetFormat(image.pixelFormat));            

			FOutput.Image.SetPixels((IntPtr)image.data);
			FOutput.Send();
		}

		public override bool NeedsThread()
		{
			return false;
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "Capture", Category = "FlyCapture", Help = "Capture frames from camera device", Tags = "")]
	#endregion PluginInfo
	public class CaptureNode : IGeneratorNode<CaptureInstance>
	{
		#region fields & pins
		
		[Input("GUID")]
		IDiffSpread<ManagedPGRGuid> FPinInGUID;

		[Output("Mode")]
		ISpread<string> FPinOutMode;

		[Output("Framerate")]
		ISpread<float> FPinOutFramerate;
		
		#endregion

		[ImportingConstructor]
		public CaptureNode()
		{

		}

		//called when data for any output pin is requested
		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			if (FPinInGUID.IsChanged || SpreadChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Guid = FPinInGUID[i];

			FPinOutMode.SliceCount = InstanceCount;
			FPinOutFramerate.SliceCount = InstanceCount;

			for (int i = 0; i < InstanceCount; i++)
			{
				FPinOutMode[i] = FProcessor[i].Mode;
				FPinOutFramerate[i] = FProcessor[i].Framerate;
			}
		}
	}
}