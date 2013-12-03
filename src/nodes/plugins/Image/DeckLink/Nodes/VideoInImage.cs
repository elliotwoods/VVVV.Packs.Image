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
using System.Runtime.InteropServices;
using DeckLinkAPI;
using Emgu.CV;

// reference : http://stackoverflow.com/questions/6355930/blackmagic-sdk-in-c-sharp

namespace VVVV.Nodes.DeckLink
{
	public class VideoInInstance : IGeneratorInstance, IDeckLinkInputCallback
	{
		IDeckLinkInput FDevice = null;
		public IDeckLink Device
		{
			set
			{
				if (value == null) {
					FDevice = null;
					Status = "Please connect an input device";
					Close();
					return;
				}
				FDevice = value as IDeckLinkInput;
				if (FDevice == null)
				{
					Status = "You have connected a device which does not support video capture";
					Close();
				}
				else
				{
					Restart();
				}
			}
		}

		_BMDDisplayMode FVideoMode;
		public _BMDDisplayMode VideoMode
		{
			set
			{
				FVideoMode = value;
				ReAllocate();
				Restart();
			}
		}

		_BMDPixelFormat FPixelFormat = _BMDPixelFormat.bmdFormat8BitYUV;
		public _BMDPixelFormat PixelFormat
		{
			set
			{
				FPixelFormat = value;
				ReAllocate();
				Restart();
			}
		}

		_BMDVideoInputFlags FVideoInputFlags;
		public ISpread<_BMDVideoInputFlags> VideoInputFlags
		{
			set
			{
				FVideoInputFlags = _BMDVideoInputFlags.bmdVideoInputFlagDefault;
				foreach (var flag in value)
					FVideoInputFlags |= flag;
			}
		}

		IDeckLinkDisplayMode FDisplayMode;
		TColorFormat FColorFormat = TColorFormat.RGBA8;

		protected override bool Open()
		{
			if (FDevice == null && false)
				return false;
			else
			{
				try
				{
					string status;

					//this is a hack, we recreate the device here
					IDeckLinkIterator iterator = new CDeckLinkIterator();
					IDeckLink device;
					iterator.Next(out device);
					FDevice = (IDeckLinkInput)device;

					_BMDDisplayModeSupport supported;

					FDevice.DoesSupportVideoMode(FVideoMode, FPixelFormat, FVideoInputFlags, out supported, out FDisplayMode);
					if (supported.HasFlag(_BMDDisplayModeSupport.bmdDisplayModeNotSupported))
					{
						status = "Display mode supported";
					}
					else if (supported.HasFlag(_BMDDisplayModeSupport.bmdDisplayModeSupportedWithConversion))
					{
						status = "Display mode supported with conversion";
					}
					else
					{
						throw(new Exception("Display mode not supported"));
					}

					FDevice.EnableVideoInput(FVideoMode, FPixelFormat, FVideoInputFlags);
					FDevice.SetCallback(this);
					FDevice.StartStreams();

					Status = "OK : " + status;
					return true;
				}
				catch (Exception e)
				{
					Status = e.Message;
					return false;
				}
			}
		}

		protected override void Close()
		{
			try
			{
				FDevice.StopStreams();
				FDevice.DisableVideoInput();
				Status = "Closed";
			}
			catch (Exception e)
			{
				Status = e.Message;
			}
		}

		CVImage FImageYUV;

		public override void Allocate()
		{
			FOutput.Image.Initialise(new Size(FDisplayMode.GetWidth(), FDisplayMode.GetHeight()), TColorFormat.RGBA8);
		}

		bool FFlush = false;
		public void Flush()
		{
			FFlush = true;
		}

		uint FFramesAvailable = 0;
		public int FramesAvailable
		{
			get
			{
				return (int)FFramesAvailable;
			}
		}

		protected override void Generate()
		{
			FDevice.GetAvailableVideoFrameCount(out FFramesAvailable);
			if (FFlush)
			{
				FFlush = false;
				FDevice.FlushStreams();
			}
		}

		public void VideoInputFormatChanged(_BMDVideoInputFormatChangedEvents notificationEvents, IDeckLinkDisplayMode newDisplayMode, _BMDDetectedVideoInputFormatFlags detectedSignalFlags)
		{
			FDisplayMode = newDisplayMode;
			ReAllocate();
		}

		public void VideoInputFrameArrived(IDeckLinkVideoInputFrame videoFrame, IDeckLinkAudioInputPacket audioPacket)
		{
			IntPtr data;
			videoFrame.GetBytes(out data);
			ImageUtils.RawYUV2RGBA(data, FOutput.Data, FOutput.Image.ImageAttributes.PixelsPerFrame);
			//FOutput.Image.SetPixels(data);
			FOutput.Send();
			System.Runtime.InteropServices.Marshal.ReleaseComObject(videoFrame);
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "VideoIn", Category = "DeckLink", Version = "Image", Help = "Captures video from BlackMagic DeckLink devices", Author = "elliotwoods", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class VideoInNode : IGeneratorNode<VideoInInstance>
	{
		#region fields & pins
		[Input("Device")]
		IDiffSpread<IDeckLink> FPinInDevice;

		[Input("Video mode")]
		IDiffSpread<_BMDDisplayMode> FPinInMode;

		//[Input("Pixel format")]
		IDiffSpread<_BMDPixelFormat> FPinInPixelFormat;

		[Input("Flags")]
		IDiffSpread<ISpread<_BMDVideoInputFlags>> FPinInFlags;

		[Input("Flush Streams", IsBang=true)]
		ISpread<bool> FPinInFlush;

		[Output("Frames Available")]
		ISpread<int> FPinOutFramesAvailable;

		#endregion fields & pins

		// import host and hand it to base constructor
		[ImportingConstructor]
		public VideoInNode(IPluginHost host)
		{

		}

		//called when data for any output pin is requested
		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			if (FPinInDevice.IsChanged)
			{
				for (int i=0; i<InstanceCount; i++)
				{
					FProcessor[i].Device = FPinInDevice[i];
				}
			}

			if (FPinInMode.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].VideoMode = FPinInMode[i];
				}
			}

			/*
			if (FPinInPixelFormat.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].PixelFormat = FPinInPixelFormat[i];
				}
			}
			*/

			if (FPinInFlags.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].VideoInputFlags = FPinInFlags[i];
				}
			}

			FPinOutFramesAvailable.SliceCount = InstanceCount;
			for (int i = 0; i < InstanceCount; i++)
			{
				if (FPinInFlush[i])
					FProcessor[i].Flush();

				FPinOutFramesAvailable[i] = FProcessor[i].FramesAvailable;
			}
		}
	}
}
