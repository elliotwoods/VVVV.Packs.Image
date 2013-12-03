using System;
using System.ComponentModel.Composition;

using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;


using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

using VVVV.CV.Core;
using VideoInputSharp;
using System.Runtime.InteropServices;
using VVVV.CV.Core;

namespace VVVV.Nodes.OpenCV.VideoInput
{
	public class VideoInInstance : IGeneratorInstance
	{
		Capture FCapture = new Capture();

		private int FDeviceID;
		public int DeviceID
		{
			set
			{
				if (FDeviceID == value)
					return;
				FDeviceID = value;
				Restart();
			}
		}

		private int FWidth = 640;
		public int Width
		{
			set
			{
				if (FWidth == value)
					return;
				FWidth = value;
				Restart();
			}
		}

		private int FHeight = 480;
		public int Height
		{
			set
			{
				if (FHeight == value)
					return;
				FHeight = value;
				Restart();
			}
		}

		private int FFramerate = 30;
		public int Framerate
		{
			set
			{
				if (FFramerate == value)
					return;
				FFramerate = value;
				Restart();
			}
		}

        public override bool Open()
		{
			lock (DeviceLock.LockDevices)
			{
				try
				{
                    int numDevices = Capture.ListDevices().Length;
                    if (numDevices == 0)
                    {
                        throw new Exception("No devices found");
                    }
					if (!FCapture.Open(FDeviceID % numDevices, FFramerate, FWidth, FHeight))
					{
						throw new Exception("Failed to open device");
					}

                    //should this always be performed?
					ReAllocate();

					Status = "OK";
					return true;
				}
				catch (Exception e)
				{
					Status = e.Message;
					return false;
				}
			}
		}

        public override void Close()
		{
			lock (DeviceLock.LockDevices)
			{
				try
				{
					FCapture.Close();
					Status = "Closed";
				}
				catch (Exception e)
				{
					Status = e.Message;
				}
			}
		}

		public override void Allocate()
		{
			FOutput.Image.Initialise(new Size(FCapture.GetWidth(), FCapture.GetHeight()), TColorFormat.RGB8);
		}

		public void ShowSettings()
		{
			FCapture.ShowSettings();
		}

		protected override void Generate()
		{
			GetPixels();
			FOutput.Send();
		}

		private unsafe void GetPixels()
		{
			FCapture.GetPixels(FOutput.Image.Data);
		}

		public void SetProperties(Dictionary<Property, float> PropertySet)
		{
			if (PropertySet == null)
				return;

			foreach (var property in PropertySet)
			{
				FCapture.SetProperty(property.Key, property.Value);
			}
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "VideoIn", Category = "CV.Image", Version = "DirectShow", Help = "Captures video from DirectShow devices", Author = "elliotwoods", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class VideoInNode : IGeneratorNode<VideoInInstance>
	{
		#region fields & pins
		[Input("Device ID", MinValue = 0)]
		IDiffSpread<int> FPinInDeviceID;

		[Input("Width", MinValue=32, MaxValue=8192, DefaultValue=640)]
		IDiffSpread<int> FPinInWidth;
		
		[Input("Height", MinValue=32, MaxValue=8192, DefaultValue=480)]
		IDiffSpread<int> FPinInHeight;

		[Input("FPS", MinValue=1, DefaultValue=30)]
		IDiffSpread<int> FPinInFPS;

		[Input("Show Settings", IsBang = true)]
		ISpread<bool> FPinInShowSettings;

		[Input("Properties")]
		IDiffSpread<Dictionary<Property, float>> FPinInProperties;

		#endregion fields & pins

		// import host and hand it to base constructor
		[ImportingConstructor]
		public VideoInNode(IPluginHost host)
		{
		
		}

		//called when data for any output pin is requested
		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			if (FPinInDeviceID.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].DeviceID = FPinInDeviceID[i];
			}

			if (FPinInWidth.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Width = FPinInWidth[i];
			}

			if (FPinInHeight.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Height = FPinInHeight[i];
			}

			if (FPinInFPS.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Framerate = FPinInFPS[i];
			}

			for (int i = 0; i < InstanceCount; i++)
				if (FPinInShowSettings[i])
					FProcessor[i].ShowSettings();

			if (FPinInProperties.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].SetProperties(FPinInProperties[i]);
		}
	}
}
