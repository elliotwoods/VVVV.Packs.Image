using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using VVVV.CV.Core;
using CLEyeMulticam;
using System.Drawing;
using VVVV.CV.Core;

namespace VVVV.Nodes.OpenCV.CLEye
{
	public class VideoInInstance : IGeneratorInstance
	{
		private Guid FDevice = new Guid();
		public string Device
		{
			set
			{
				try
				{
					FDevice = new Guid(value);
					Restart();
					Status = "";
				}
				catch (Exception e)
				{
					Status = e.Message;
				}
			}
		}

		private CLEyeCameraResolution FResolution;
		public CLEyeCameraResolution Resolution
		{
			set
			{
				FResolution = value;
				Restart();
			}
		}

		private CLEyeCameraColorMode FColorMode;
		public CLEyeCameraColorMode ColorMode
		{
			set
			{
				FColorMode = value;
				Restart();
			}
		}

		private int FFps = 30;
		public int Fps
		{
			set
			{
				FFps = value;
				Restart();
			}
		}

		private bool FLED = true;
		public bool LED
		{
			set
			{
				if (this.FCamera != null)
				{
					FLED = value;
					FCamera.setLED(FLED);
				}
			}
		}

		private bool FParameterChange = false;
		Object FParameterLock = new Object();
		Dictionary<CLEyeCameraParameter, int> FParameters;
		public Dictionary<CLEyeCameraParameter, int> Parameters
		{
			set
			{
				lock (FParameterLock)
				{
					FParameters = value;
				}
				FParameterChange = true;
			}
		}

		private void SetParameters()
		{
			if (FCamera == null || FParameters == null)
				return;

			lock (FParameterLock)
			{
				foreach (var param in FParameters)
				{
					FCamera.SetParameter(param.Key, param.Value);
				}
			}

			FParameterChange = false;
		}

		CLEyeCameraDevice FCamera = null;

		public override bool Open()
		{
			try
			{
				if (FDevice == new Guid())
					FDevice = CLEyeCameraDevice.CLEyeGetCameraUUID(0);
				FCamera = new CLEyeCameraDevice(FResolution, FColorMode, FFps);
				FCamera.Start(FDevice) ;
				FOutput.Image.Initialise(GetSize(FResolution), GetColor(FColorMode));

				FCamera.setLED(FLED);
				FParameterChange = true;

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
			if (FCamera != null)
			{
				FCamera.Stop();
				FCamera = null;
			}
		}

		private Size GetSize(CLEyeCameraResolution resolution)
		{
			switch(resolution)
			{
				case CLEyeCameraResolution.CLEYE_QVGA:
					return new Size(320, 240);
				case CLEyeCameraResolution.CLEYE_VGA:
					return new Size(640, 480);
				default:
					throw (new Exception("Size unsupported"));
			}
		}

		private TColorFormat GetColor(CLEyeCameraColorMode color)
		{
			switch(color)
			{
				case CLEyeCameraColorMode.CLEYE_BAYER_RAW:
					return TColorFormat.L8;

				case CLEyeCameraColorMode.CLEYE_COLOR_PROCESSED:
					return TColorFormat.RGBA8;

				case CLEyeCameraColorMode.CLEYE_COLOR_RAW:
					return TColorFormat.RGBA8;

				case CLEyeCameraColorMode.CLEYE_MONO_PROCESSED:
					return TColorFormat.L8;

				case CLEyeCameraColorMode.CLEYE_MONO_RAW:
					return TColorFormat.L8;
				default:
					throw (new Exception("Color mode unsupported"));
			}
		}

		protected override void Generate()
		{
			if (FParameterChange)
				SetParameters();

			FCamera.getPixels(FOutput.Data, 100);
			
			if (FOutput.Image.NativeFormat == TColorFormat.RGBA8)
				SetAlphaChannel();

			FOutput.Send();
		}

		private unsafe void SetAlphaChannel()
		{	
			byte* data = (byte*) FOutput.Data.ToPointer() + 3;

			int count = FOutput.Image.Width * FOutput.Image.Height;
			for (int i = 0; i < count; i++)
			{
				*data = 255;
				data += 4;
			}
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "VideoIn", Category = "CLEye", Help = "Capture from camera devices", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class VideoInNode : IGeneratorNode<VideoInInstance>
	{
		[Input("Device")]
		IDiffSpread<string> FDevices;

		[Input("Resolution")]
		IDiffSpread<CLEyeCameraResolution> FResolution;

		[Input("Color Mode")]
		IDiffSpread<CLEyeCameraColorMode> FColorMode;

		[Input("FPS", DefaultValue=30, MinValue=5, MaxValue=120)]
		IDiffSpread<int> FFps;

		[Input("LED", DefaultValue = 1)]
		IDiffSpread<bool> FLED;

		[Input("Properties")]
		IDiffSpread<Dictionary<CLEyeCameraParameter, int>> FPinInProperties;

		override protected void Update(int InstanceCount, bool SpreadCountChanged)
		{
			if (SpreadCountChanged || FDevices.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Device = FDevices[i];
			}

			if (SpreadCountChanged || FResolution.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Resolution = FResolution[i];
			}

			if (SpreadCountChanged || FColorMode.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].ColorMode = FColorMode[i];
			}

			if (SpreadCountChanged || FFps.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Fps = FFps[i];
			}

			if (SpreadCountChanged || FLED.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].LED = FLED[i];
			}

			if (SpreadCountChanged || FPinInProperties.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].Parameters = FPinInProperties[i];
				}
			}
		}
	}
}
