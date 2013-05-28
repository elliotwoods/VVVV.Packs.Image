using System;
using Emgu.CV.CvEnum;
using VVVV.PluginInterfaces.V2;
using Emgu.CV;
using System.Drawing;

namespace VVVV.Nodes.OpenCV
{
	public class CaptureVideoInstance : IGeneratorInstance
	{
		Capture FCapture;

		private int FDeviceId;
		public int DeviceId
		{
			get
			{
				return FDeviceId;
			}
			set
			{
				if (value == FDeviceId) return;

				FDeviceId = value;
				Restart();
				Open();
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
				if(value == FWidth) return;

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
				if(value == FHeight) return;

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
				if(value == FFramerate) return;

				FFramerate = value;
				Restart();
			}
		}

		protected override bool Open()
		{
			Close();

			try
			{
				FCapture = new Capture(FDeviceId);
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

		protected override void Close()
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
			if (!ImageUtils.IsIntialised(capbuffer)) return;
			
			FOutput.Image.SetImage(capbuffer);
			FOutput.Send();
		}
}

	[PluginInfo(Name = "VideoIn", Category = "OpenCV", Version = "", Help = "Captures from DShow device to IPLImage", Tags = "")]
	public class CaptureVideoNode : IGeneratorNode<CaptureVideoInstance>
	{
		[Input("Device ID", MinValue = 0)]
		ISpread<int> FDeviceIDIn;

		[Input("Width", MinValue = 32, MaxValue = 8192, DefaultValue = 640)]
		ISpread<int> FWidthIn;

		[Input("Height", MinValue = 32, MaxValue = 8192, DefaultValue = 480)]
		ISpread<int> FHeightIn;

		protected override void Update(int instanceCount, bool spreadChanged)
		{
			for (int i = 0; i < instanceCount; i++)
			{
				FProcessor[i].DeviceId = FDeviceIDIn[i];
				FProcessor[i].Width = FWidthIn[i];
				FProcessor[i].Height = FHeightIn[i];
			}			
		}
	}
}
