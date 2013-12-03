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

using OptiTrackNET;

using VVVV.CV.Core;
using VVVV.CV.Core;

#endregion usings

namespace VVVV.Nodes.OptiTrack
{
	public class VideoInInstance : IGeneratorInstance
	{
		public VideoInInstance()
		{
			this.FrameID = -1;
			this.Objects = new List<TrackingObject>();
		}

        Object FCameraLock = new Object();
		MCamera FCamera = null;
		public MCamera Camera
		{
			set
			{
				if (value == FCamera)
					return;

				lock (FLockProperties)
				{
                    lock (FCameraLock)
                    {
                        FCamera = value;
                    }
                    this.Restart();
				}
			}
		}

		VideoMode FMode;
		public VideoMode Mode
		{
			set
			{
				lock (FLockProperties)
				{
					FMode = value;
					this.Restart();
				}								
			}
		}

		bool FGetLatestFrame;
		public bool GetLatestFrame
		{
			set
			{
				this.FGetLatestFrame = value;
			}
		}

		CaptureProperty FCaptureProperty;
		public CaptureProperty CaptureProperty
		{
			set
			{
				try
				{
					this.FCaptureProperty = value;
					if (this.IsOpen)
						ApplyCaptureProperty();
				}
				catch (Exception e)
				{
					this.Status = e.Message;
				}
			}
		}

		void ApplyCaptureProperty()
		{
			if (FCaptureProperty == null)
				return;
			FCamera.SetExposure(FCaptureProperty.Exposure);
			FCamera.SetAEC(FCaptureProperty.AEC);
			FCamera.SetAGC(FCaptureProperty.AGC);
			FCamera.SetFrameRate(FCaptureProperty.Framerate);
			FCamera.SetIntensity(FCaptureProperty.Intensity);
			FCamera.SetLED(FCaptureProperty.StatusLEDs, FCaptureProperty.StatusLEDsEnabled);
			FCamera.SetThreshold(FCaptureProperty.Threshold);
			FCamera.SetMJPEGQuality(FCaptureProperty.MJPEGQuality);
			FCamera.SetIRFilter(FCaptureProperty.IRFilter);
		}

        public override bool Open()
		{
			try
			{
                lock (FCameraLock)
                {
                    if (FCamera == null)
                    {
                        MCameraManager.WaitForInitialization();
                        FCamera = MCameraManager.GetCamera();
                        if (FCamera == null || !FCamera.IsValid())
                        {
                            throw (new Exception("Cannot open camera, no device attached"));
                        }
                    }

                    FCamera.SetVideoType(this.FMode);
                    FCamera.Start();
                    ApplyCaptureProperty();
                    FCamera.FrameAvailable += FCamera_FrameAvailable;
                    Status = "OK";
                    return true;
                }
			}
			catch (Exception e)
			{
				this.Status = e.Message;
				return false;
			}
		}

        public override void Close()
		{
			FCamera.FrameAvailable -= FCamera_FrameAvailable;
			FCamera.Stop(true);
			Status = "Closed";
		}

		void FCamera_FrameAvailable(object sender, EventArgs e)
		{
			//if we've set to non-threaded mode, then generate here
			if (!this.NeedsThread())
				this.Generate();
		}

		public override void Allocate()
		{
			try
			{
				if (!FCamera.IsValid())
					throw(new Exception("Camera not ready"));
				FOutput.Image.Initialise(new System.Drawing.Size((int)FCamera.Width(), (int)FCamera.Height()), TColorFormat.L8);
			}
			catch(Exception e)
			{
				this.ReAllocate(); // try again next frame
			}
		}

		public int FrameID { get; private set; }

		protected override void Generate()
		{
			var frame =  this.FGetLatestFrame ? FCamera.GetLatestFrame() : FCamera.GetFrame();
			if (frame != null)
			{
				if (frame.FrameID() != this.FrameID)
				{
					if (FMode == VideoMode.GrayscaleMode)
						frame.GetGrayscaleData(FOutput.Data);
					else
						frame.Rasterize(FOutput.Data);

					this.FrameID = frame.FrameID();
					FOutput.Send();

					lock (this.Objects)
					{
						this.Objects.Clear();
						for (int i = 0; i < frame.ObjectCount(); i++)
						{
							this.Objects.Add(new TrackingObject(frame.Object(i)));
						}
					}
				}
				frame.Release();
			}
		}

		public override bool NeedsThread()
		{
			return true;
		}

		public List<TrackingObject> Objects { get; private set; }		
	}

	#region PluginInfo
	[PluginInfo(Name = "VideoIn", Category = "OptiTrack", Help = "Capture frames from camera device", Tags = "")]
	#endregion PluginInfo
	public class VideoInNode : IGeneratorNode<VideoInInstance>
	{
		#region fields & pins

		[Input("Device")]
		IDiffSpread<MCamera> FInCamera;

		[Input("Mode")]
		IDiffSpread<VideoMode> FInMode;

		[Input("Get Latest Frame")]
		IDiffSpread<bool> FInGetLatestFrame;

		[Input("Capture Properties")]
		IDiffSpread<CaptureProperty> FInCaptureProperty;

		[Output("Frame ID")]
		ISpread<int> FOutFrameID;

		[Output("Objects")]
		ISpread<ISpread<TrackingObject>> FOutObjects;

		Context FContext = new Context();

		#endregion

		[ImportingConstructor]
		public VideoInNode()
		{
			if (MCameraManager.AreCamerasInitialized())
			{
				MCameraManager.EnableDevelopment();
				MCameraManager.WaitForInitialization();
			}
		}

		//called when data for any output pin is requested
		protected override void Update(int InstanceCount, bool changed)
		{
			if (FInCamera.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Camera = FInCamera[i];

			if (FInMode.IsChanged || changed)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Mode = FInMode[i];

			if (FInGetLatestFrame.IsChanged || changed)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].GetLatestFrame = FInGetLatestFrame[i];

			if (FInCaptureProperty.IsChanged || changed)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].CaptureProperty = FInCaptureProperty[i];

			FOutFrameID.SliceCount = InstanceCount;
			FOutObjects.SliceCount = InstanceCount;

			for (int i = 0; i < InstanceCount; i++)
			{
				FOutFrameID[i] = FProcessor[i].FrameID;

				lock (FProcessor[i].Objects)
				{
					FOutObjects[i].SliceCount = FProcessor[i].Objects.Count;
					for (int j = 0; j < FProcessor[i].Objects.Count; j++)
					{
						FOutObjects[i][j] = FProcessor[i].Objects[j];
					}
				}
			}
		}
	}
}