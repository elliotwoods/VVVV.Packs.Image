#region usings

using System;
using System.ComponentModel.Composition;
using Emgu.CV.CvEnum;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using VVVV.CV.Core;

#endregion usings

namespace VVVV.CV.Nodes
{
	public class CaptureVideoInstance : IGeneratorInstance
	{
		private int FRequestedWidth = 0;
		private int FRequestedHeight = 0;

		Capture FCapture;

		private int FDeviceID = 0;
		public int DeviceID
		{
			get
			{
				return FDeviceID;
			}
			set
			{
				FDeviceID = value;
				Restart();
			}
		}

		private int FWidth = 640;
		public int Width
		{
			get
			{
				return FWidth;
			}
			set
			{
				FWidth = value;
				Restart();
			}
		}

		private int FHeight = 480;
		public int Height
		{
			get
			{
				return FHeight;
			}
			set
			{
				FHeight = value;
				Restart();
			}
		}

		private int FFramerate = 30;
		public int Framerate
		{
			get
			{
				return FFramerate;
			}
			set
			{
				FFramerate = value;
				Restart();
			}
		}

        public override bool Open()
		{
			Close();

			try
			{
				FCapture = new Capture(FDeviceID);
				FCapture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_FRAME_WIDTH, FWidth);
				FCapture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT, FHeight);
				FCapture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_FPS, FFramerate);

				Status = "OK";
				return true;
			}
			catch (Exception e)
			{
				Status = e.Message;
				return false;
			}
		}

        public override void Close()
		{
            if (FCapture == null)
                return;

			try
			{
				FCapture.Dispose();
				Status = "Closed";
			}
			catch (Exception e)
			{
				Status = e.Message;
			}
		}

        public override void Allocate()
        {
            FOutput.Image.Initialise(new Size(FCapture.Width, FCapture.Height), TColorFormat.RGB8);
        }

		protected override void Generate()
		{
			IImage capbuffer = FCapture.QueryFrame();
			if (ImageUtils.IsIntialised(capbuffer))
			{
				FOutput.Image.SetImage(capbuffer);
				FOutput.Send();
			}
		}
}

	#region PluginInfo
	[PluginInfo(Name = "VideoIn",
			  Category = "CV.Image",
			  Version = "VfW",
			  Help = "Captures from DShow device to IPLImage",
			  Tags = "")]
	#endregion PluginInfo
	public class CaptureVideoNode : IGeneratorNode<CaptureVideoInstance>
	{
		#region fields & pins
		[Input("Device ID", MinValue = 0)]
		IDiffSpread<int> FPinInDeviceID;

		[Input("Width", MinValue = 32, MaxValue = 8192, DefaultValue = 640)]
		IDiffSpread<int> FPinInWidth;

		[Input("Height", MinValue = 32, MaxValue = 8192, DefaultValue = 480)]
		IDiffSpread<int> FPinInHeight;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		// import host and hand it to base constructor
		[ImportingConstructor]
		public CaptureVideoNode(IPluginHost host)
		{

		}

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			if (FPinInDeviceID.IsChanged || SpreadChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].DeviceID = FPinInDeviceID[i];

            if (FPinInWidth.IsChanged || SpreadChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Width = FPinInWidth[i];

            if (FPinInHeight.IsChanged || SpreadChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Height = FPinInHeight[i];
		}
	}
}
