using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using VVVV.CV.Core;
using VVVV.Utils;
using uEye;
using uEye.Defines;
using uEye.Types;
using System.Drawing;
using Emgu.CV;

namespace VVVV.Nodes.OpenCV.IDS
{
	public class VideoInInstance : IGeneratorInstance
	{
        private Camera cam = null;
        private uEye.Defines.Status camStatus { get; set; }

        public bool camOpen = false;

        private int FCamId = 0;
        public int CamId
        {
            get
            {
                return FCamId;
            }
            set
            {
                FCamId = value;
                Restart();
}
        }
        

        // Resolution
        private uEye.Types.Size<int> MaxSize { get; set; }
        private VVVV.Utils.VMath.Vector2D FResolution;
        public VVVV.Utils.VMath.Vector2D Resolution
        {
            set
            {
                FResolution = value;
                Restart();
            }
        }

        // Formats - ~ Resolution
        private int ResX = 640;
        private int ResY = 480;
        public ImageFormatInfo[] FormatInfoList;
        private int FFormat = 0;
        public int Format
        {
            set
            {
                FFormat = value;

                setFormat(FFormat);
            }
        }

        private void setFormat(int id)
        {
            if (camOpen)
            {
                ResX = FormatInfoList[FFormat].Size.Width;
                ResY = FormatInfoList[FFormat].Size.Height;

                //cam.EventFrame -= onFrameEvent;
                Restart();
            }

            //setFormat(FFormat);
            //ReAllocate();
            //Restart();
        }

        // FPS
        public Range<double> frameRateRange { get; set; }
        private int FFps = 30;
        public int Fps
        {
            set
            {
                FFps = value;
                //Restart();
                SetFrameRate((double)FFps);
            }
        }

        private void SetFrameRate(double fps)
        {
            if (camOpen)
                cam.Timing.Framerate.Set(FFps);

        }

        // colorMode
        private ColorMode FColorMode;
		public ColorMode ColorMode
		{
			set
			{
				FColorMode = value;
				Restart();
			}
		}


		//private bool FParameterChange = false;
		//Object FParameterLock = new Object();
		//Dictionary<CLEyeCameraParameter, int> FParameters;
		//public Dictionary<CLEyeCameraParameter, int> Parameters
		//{
		//	set
		//	{
		//		lock (FParameterLock)
		//		{
		//			FParameters = value;
		//		}
		//		FParameterChange = true;
		//	}
		//}

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
                
                // format
                cam.PixelFormat.Set(FColorMode);

                uEye.Defines.ColorMode pixFormat;
                cam.PixelFormat.Get(out pixFormat);

                cam.Size.ImageFormat.Set((uint)FFormat);

                // start capturee
                camStatus = cam.Memory.Allocate();
                camStatus = cam.Acquisition.Capture();

                // query infos
                int bpp;
                cam.PixelFormat.GetBytesPerPixel(out bpp);

                uEye.Types.ImageInfo info;
                cam.Information.GetImageInfo(0, out info);

                uEye.Types.SensorInfo si;
                cam.Information.GetSensorInfo(out si);

                MaxSize = si.MaxSize;

                TColorFormat format = GetColor(pixFormat);

                Range<double> frr;
                cam.Timing.Framerate.GetFrameRateRange(out frr);
                frameRateRange = frr;

                ImageFormatInfo[] fil;
                cam.Size.ImageFormat.GetList(out fil);
                FormatInfoList = fil;

                ResX = FormatInfoList[FFormat].Size.Width;
                ResY = FormatInfoList[FFormat].Size.Height;
                // init Output 
                FOutput.Image.Initialise(MaxSize.Width, MaxSize.Height, format);
                //FOutput.Image.Initialise(ResX, ResY, format);

                // attach Event
                cam.EventFrame += onFrameEvent;

                camOpen = true;

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

            


            IntPtr mem;
            camObject.Memory.GetActive(out mem);
            bool started;
            camObject.Acquisition.HasStarted(out started);
            if (camObject.IsOpened && started)
            {
                int s32MemId;
                camObject.Memory.GetLast(out s32MemId);


                // image size differs from format????
                ImageInfo iii;
                camObject.Information.GetImageInfo(s32MemId, out iii);

                int h;
                int w;
                camObject.Memory.GetHeight(s32MemId, out h);
                camObject.Memory.GetWidth(s32MemId, out w);

                int _x, _y, _b, _p;
                camObject.Memory.Inquire(s32MemId, out _x, out _y, out _b, out _p);
                //camObject.Memory.Inquire(FFormat, out _x, out _y, out _b, out _p);

                //copy to FOutput
                IntPtr memPtr;
                camObject.Memory.ToIntPtr(out memPtr);
                camObject.Memory.CopyImageMem(memPtr, s32MemId, FOutput.Data);

                FOutput.Send();
            }

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

                camOpen = false;
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

                case uEye.Defines.ColorMode.Mono16:
                    return TColorFormat.L16;

                case uEye.Defines.ColorMode.RGB8Packed:
                case uEye.Defines.ColorMode.RGB8Planar:
                case uEye.Defines.ColorMode.BGR8Packed:
                    return TColorFormat.RGB8;

                case uEye.Defines.ColorMode.RGBA8Packed:
                case uEye.Defines.ColorMode.BGRA8Packed:
                    return TColorFormat.RGBA8;

				default:
					throw (new Exception("Color mode unsupported"));
			}
		}


		protected unsafe override void Generate()
		{
            //IntPtr mem;
            //cam.Memory.GetActive(out mem);
            //bool started;
            //cam.Acquisition.HasStarted(out started);
            //if (cam.IsOpened && started)
            //{
            //    int s32MemId;
            //    cam.Memory.GetLast(out s32MemId);
            //    IntPtr memPtr;
            //    cam.Memory.ToIntPtr(out memPtr);
            //    cam.Memory.CopyImageMem(memPtr, s32MemId, FOutput.Data);

            //    FOutput.Send();
            //}

            // how to copy the ptr to smth. like FOutput.Data ???

            //ImageUtils.CopyMemory(pt, mem, (uint)(FResolution.x * FResolution.y));
            //ImageUtils.CopyImage(mem, FOutput.Image);

            //if (FParameterChange)
            //    SetParameters();

            //FCamera.getPixels(FOutput.Data, 100);

            //if (FOutput.Image.NativeFormat == TColorFormat.RGBA8)
            //    SetAlphaChannel();

            
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

        [Input("Format Id")]
        IDiffSpread<int> FInFormatId;

        [Input("Resolution")]
		IDiffSpread<VVVV.Utils.VMath.Vector2D  > FResolution;

        //[Input("Format")]
        //IDiffSpread<uEye.Types.ImageFormatInfo> FColorMode;

        [Input("Color Mode")]
		IDiffSpread<uEye.Defines.ColorMode> FColorMode;

		[Input("FPS", DefaultValue=30, MinValue=0, MaxValue=1024)]
		IDiffSpread<int> FFps;

        [Output("Framerate Range")]
        ISpread<VVVV.Utils.VMath.Vector2D> FOutFramerateRange;

        [Output("available Formats")]
        ISpread<ISpread<string>> FOutFormats;

        //[Input("Properties")]
        //IDiffSpread<Dictionary<CLEyeCameraParameter, int>> FPinInProperties;

        override protected void Update(int InstanceCount, bool SpreadCountChanged)
		{
            if (SpreadCountChanged || FInCamId.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                    FProcessor[i].CamId = FInCamId[i];
            }

            if (SpreadCountChanged || FInFormatId.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                    FProcessor[i].Format = FInFormatId[i];
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
                {
                    FProcessor[i].Fps = FFps[i];
                    //FOutFramerateRange[i] = new VVVV.Utils.VMath.Vector2D(FProcessor[i].frameRateRange.Minimum, FProcessor[i].frameRateRange.Maximum);
                }
			}

            FOutFormats.SliceCount = InstanceCount;
            FOutFramerateRange.SliceCount = InstanceCount;

            for (int i = 0; i < InstanceCount; i++)
            {
                if (FProcessor[i].Enabled && FProcessor[i].camOpen)
                {
                    FOutFramerateRange[i] = new VVVV.Utils.VMath.Vector2D(FProcessor[i].frameRateRange.Minimum, FProcessor[i].frameRateRange.Maximum);

                    int numformats = FProcessor[i].FormatInfoList.Count();

                    FOutFormats[i].SliceCount = numformats;

                    for (int f=0; f<numformats; f++)
                    {
                        FOutFormats[i][f] = FProcessor[i].FormatInfoList[f].FormatName;
                    }
                }
            }

                



            //if (SpreadCountChanged || FPinInProperties.IsChanged)
            //{
            //	for (int i = 0; i < InstanceCount; i++)
            //	{
            //		FProcessor[i].Parameters = FPinInProperties[i];
            //	}
            //}
        }
	}
}
