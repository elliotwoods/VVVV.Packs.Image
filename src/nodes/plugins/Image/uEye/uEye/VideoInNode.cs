using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using VVVV.CV.Core;
using uEye;
using uEye.Defines;
using System.Drawing;

namespace VVVV.Nodes.OpenCV.IDS
{
	public class VideoInInstance : IGeneratorInstance
	{
        private Camera cam = null;
        public int FCamId { get; set;}
        private uEye.Defines.Status camStatus { get; set; }

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
		public Dictionary<uEye.Parameter, int> Parameters
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
			//if (FCamera == null || FParameters == null)
			//	return;

			//lock (FParameterLock)
			//{
			//	foreach (var param in FParameters)
			//	{
			//		FCamera.SetParameter(param.Key, param.Value);
			//	}
			//}

			//FParameterChange = false;
		}



        ////////////////////////////////////////////////////
        // OPEN
        ////////////////////////////////////////////////////
        public override bool Open()
		{

            //Note on multi-camera environments
            //When using multiple cameras in parallel operation on a single system, you should 
            //assign a unique camera ID to each camera.To initialize or select a camera with 
            //Init(), s32Cam must previously have been set to the desired camera ID.
            //To initialize or select the next available camera without specifying a camera ID, 
            //s32Cam has to be preset with 0.

            try
			{
                cam = new Camera();
                camStatus = cam.Init(FCamId);

                if (camStatus != uEye.Defines.Status.Success)
                    camStatus = cam.Memory.Allocate();

                if (camStatus != uEye.Defines.Status.Success)
                    camStatus = cam.Acquisition.Capture();


                // initialize Output ... probably not working, if those 
                // return values are not yet known. need to tryout
                uEye.Types.ImageInfo info;
                cam.Information.GetImageInfo(0, out info);

                uEye.Defines.ColorMode pixFormat;
                cam.PixelFormat.Get(out pixFormat);

                int w = info.ImageSize.Width;
                int h = info.ImageSize.Height;

                TColorFormat format = GetColor(pixFormat);

                // ok init the Output now
                FOutput.Image.Initialise(w, h, format);



                // can this work via Event or must i use Generator??
                cam.EventFrame += onFrameEvent;

                //CB_Auto_Gain_Balance.Enabled = Camera.AutoFeatures.Software.Gain.Supported;
                //CB_Auto_White_Balance.Enabled = Camera.AutoFeatures.Software.WhiteBalance.Supported;

                //            if (FDevice == new Guid())
                //	FDevice = CLEyeCameraDevice.CLEyeGetCameraUUID(0);
                //FCamera = new CLEyeCameraDevice(FResolution, FColorMode, FFps);
                //FCamera.Start(FDevice) ;
                //FOutput.Image.Initialise(GetSize(FResolution), GetColor(FColorMode));

                //FCamera.setLED(FLED);
                //FParameterChange = true;

                Status = "OK";
				return true;
			}
			catch (Exception e)
			{
				Status = e.Message;
				return false;
			}
		}
        /// <summary>
        /// Event that fires when new frame is available from the cam
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onFrameEvent(object sender, EventArgs e)
        {
            uEye.Camera camObject = sender as uEye.Camera;

            Int32 s32MemID;
            camObject.Memory.GetActive(out s32MemID);

            // or directly:
            //Camera.Image.
            //Camera.Memory.CopyImageMem(FOutput.Data);
            //FOutput.Data

            //Camera.Display.Render(s32MemID, displayHandle, uEye.Defines.DisplayRenderMode.FitToWindow);
        }

        /// <summary>
        /// when instance is destroyed
        /// </summary>
        public override void Close()
		{
            if (cam != null)
            {
                bool started;
                camStatus = cam.Acquisition.HasStarted(out started);

                if (started)
                    camStatus = cam.Acquisition.Stop();

                cam.Exit();
                cam = null;
            }
		}


        /// <summary>
        /// maps uEye.Defines.ColorMode to VVVV.CV.Core.TColorFormat
        /// TODO: only basic format are mapped, yet.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        private TColorFormat GetColor(uEye.Defines.ColorMode color)
		{
			switch(color)
			{
                case uEye.Defines.ColorMode.Mono8:
                    return TColorFormat.L8;

                case uEye.Defines.ColorMode.RGB8Packed:
                    return TColorFormat.RGB8;

                case uEye.Defines.ColorMode.RGB8Planar:
                    return TColorFormat.RGB8;

                case uEye.Defines.ColorMode.RGBA8Packed:
                    return TColorFormat.RGBA8;

                case uEye.Defines.ColorMode.Mono16:
                    return TColorFormat.L16;

                case uEye.Defines.ColorMode.BGR8Packed:
                    return TColorFormat.RGB8;

                case uEye.Defines.ColorMode.BGRA8Packed:
                    return TColorFormat.RGBA8;

				default:
					throw (new Exception("Color mode unsupported"));
			}
		}


		protected override void Generate()
		{
            IntPtr mem;
            cam.Memory.GetActive(out mem);

            // how to copy the ptr to smth. like FOutput.Data ???



            if (FParameterChange)
                SetParameters();

            FCamera.getPixels(FOutput.Data, 100);

            if (FOutput.Image.NativeFormat == TColorFormat.RGBA8)
                SetAlphaChannel();

            FOutput.Send();
        }


        // why add always an alpha channel? does the output need to be rgba?
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
	[PluginInfo(Name = "VideoIn", Category = "uEye", Help = "Capture from camera devices", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class VideoInNode : IGeneratorNode<VideoInInstance>
	{
        [Input("Camera Id")]
        IDiffSpread<int> FInCamId;

        [Input("Device")]
		IDiffSpread<string> FDevices;

		[Input("Resolution")]
		IDiffSpread<VVVV.Utils.VMath.Vector2D  > FResolution;

		[Input("Color Mode")]
		IDiffSpread<uEye.Defines.ColorMode> FColorMode;

		[Input("FPS", DefaultValue=30, MinValue=5, MaxValue=120)]
		IDiffSpread<int> FFps;

		[Input("LED", DefaultValue = 1)]
		IDiffSpread<bool> FLED;

		[Input("Properties")]
		IDiffSpread<Dictionary<CLEyeCameraParameter, int>> FPinInProperties;

		override protected void Update(int InstanceCount, bool SpreadCountChanged)
		{
            if (SpreadCountChanged || FInCamId.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                    FProcessor[i].FCamId = FInCamId[i];
            }


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
