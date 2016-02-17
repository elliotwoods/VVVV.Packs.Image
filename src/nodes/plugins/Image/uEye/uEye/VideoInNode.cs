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

        private void QueryCameraCapabilities()
        {
            // get camera aoi range size
            uEye.Types.Range<Int32> rangeWidth, rangeHeight;
            camStatus = cam.Size.AOI.GetSizeRange(out rangeWidth, out rangeHeight);

            // get actual aoi
            System.Drawing.Rectangle rect;
            camStatus = cam.Size.AOI.Get(out rect);

            // get pos range
            uEye.Types.Range<Int32> rangePosX, rangePosY;
            camStatus = cam.Size.AOI.GetPosRange(out rangePosX, out rangePosY);

            // subsampling && binning
            updateHorizontalBinning();
            updateVerticalBinning();

            updateHorizontalSubsampling();
            updateVerticalSubsampling();

        }


        #region setParameters

        private void mirrorHorizontal(bool Enable)
        {
            camStatus = cam.RopEffect.Set(uEye.Defines.RopEffectMode.LeftRight, Enable);
        }

        private void mirrorVertical(bool Enable)
        {
            camStatus = cam.RopEffect.Set(uEye.Defines.RopEffectMode.UpDown, Enable);
        }

        private void SetAoiWidth(int s32Value)
        {
            System.Drawing.Rectangle rect;

            uEye.Types.Range<Int32> rangeWidth, rangeHeight;
            camStatus = cam.Size.AOI.GetPosRange(out rangeWidth, out rangeHeight);

            while ((s32Value % rangeWidth.Increment) != 0)
            {
                --s32Value;
            }

            camStatus = cam.Size.AOI.Get(out rect);
            rect.Width = s32Value;

            camStatus = cam.Size.AOI.Set(rect);


            // memory reallocation
            Int32[] memList;
            camStatus = cam.Memory.GetList(out memList);
            camStatus = cam.Memory.Free(memList);
            camStatus = cam.Memory.Allocate();

        }

        private void SetAoiHeight(int s32Value)
        {
            System.Drawing.Rectangle rect;

            uEye.Types.Range<Int32> rangeWidth, rangeHeight;
            camStatus = cam.Size.AOI.GetPosRange(out rangeWidth, out rangeHeight);

            while ((s32Value % rangeHeight.Increment) != 0)
            {
                --s32Value;
            }

            camStatus = cam.Size.AOI.Get(out rect);
            rect.Height = s32Value;

            camStatus = cam.Size.AOI.Set(rect);

            // memory reallocation
            Int32[] memList;
            camStatus = cam.Memory.GetList(out memList);
            camStatus = cam.Memory.Free(memList);
            camStatus = cam.Memory.Allocate();
        }

        private void SetAoiLeft(int s32Value)
        {
            System.Drawing.Rectangle rect;

            uEye.Types.Range<Int32> rangePosX, rangePosY;
            camStatus = cam.Size.AOI.GetPosRange(out rangePosX, out rangePosY);

            while ((s32Value % rangePosX.Increment) != 0)
            {
                --s32Value;
            }

            camStatus = cam.Size.AOI.Get(out rect);
            rect.X = s32Value;

            camStatus = cam.Size.AOI.Set(rect);

            // update aoi width
            uEye.Types.Range<Int32> rangeWidth, rangeHeight;
            camStatus = cam.Size.AOI.GetSizeRange(out rangeWidth, out rangeHeight);
        }

        private void SetAoiTop(int s32Value)
        {
            System.Drawing.Rectangle rect;

            uEye.Types.Range<Int32> rangePosX, rangePosY;
            camStatus = cam.Size.AOI.GetPosRange(out rangePosX, out rangePosY);

            while ((s32Value % rangePosY.Increment) != 0)
            {
                --s32Value;
            }

            camStatus = cam.Size.AOI.Get(out rect);
            rect.Y = s32Value;

            camStatus = cam.Size.AOI.Set(rect);

            // update aoi height
            uEye.Types.Range<Int32> rangeWidth, rangeHeight;
            camStatus = cam.Size.AOI.GetSizeRange(out rangeWidth, out rangeHeight);
        }

        #endregion setParameters


        #region queryParameterSets

        private void updateHorizontalBinning()
        {
            uEye.Defines.BinningMode mode;
            camStatus = cam.Size.Binning.GetSupported(out mode);

            List<BinningMode> SupportedHorizontalBinnings = new List<BinningMode>();

            if ((mode & uEye.Defines.BinningMode.Disable) == mode)
            {
                // horizontal binning is not supported
            }
            else
            {
                if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Horizontal2X))
                {
                    SupportedHorizontalBinnings.Add(uEye.Defines.BinningMode.Horizontal2X);
                }
                if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Horizontal3X))
                {
                    SupportedHorizontalBinnings.Add(uEye.Defines.BinningMode.Horizontal3X);
                }
                if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Horizontal4X))
                {
                    SupportedHorizontalBinnings.Add(uEye.Defines.BinningMode.Horizontal4X);
                }
                if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Horizontal5X))
                {
                    SupportedHorizontalBinnings.Add(uEye.Defines.BinningMode.Horizontal5X);
                }
                if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Horizontal6X))
                {
                    SupportedHorizontalBinnings.Add(uEye.Defines.BinningMode.Horizontal6X);
                }
                if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Horizontal8X))
                {
                    SupportedHorizontalBinnings.Add(uEye.Defines.BinningMode.Horizontal8X);
                }
                if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Horizontal16X))
                {
                    SupportedHorizontalBinnings.Add(uEye.Defines.BinningMode.Horizontal16X);
                }
            }

            Int32 s32Factor;
            camStatus = cam.Size.Binning.GetFactorHorizontal(out s32Factor);
        }

        private void updateVerticalBinning()
        {
            uEye.Defines.BinningMode mode;
            camStatus = cam.Size.Binning.GetSupported(out mode);

            List<BinningMode> SupportedVerticalBinnings = new List<BinningMode>();

            if ((mode & uEye.Defines.BinningMode.Disable) == mode)
            {
                // vertical binning not supported
            }
            else
            {
                if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Vertical2X))
                {
                    SupportedVerticalBinnings.Add(uEye.Defines.BinningMode.Vertical2X);
                }
                if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Vertical3X))
                {
                    SupportedVerticalBinnings.Add(uEye.Defines.BinningMode.Vertical3X);
                }
                if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Vertical4X))
                {
                    SupportedVerticalBinnings.Add(uEye.Defines.BinningMode.Vertical4X);
                }
                if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Vertical5X))
                {
                    SupportedVerticalBinnings.Add(uEye.Defines.BinningMode.Vertical5X);
                }
                if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Vertical6X))
                {
                    SupportedVerticalBinnings.Add(uEye.Defines.BinningMode.Vertical6X);
                }
                if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Vertical8X))
                {
                    SupportedVerticalBinnings.Add(uEye.Defines.BinningMode.Vertical8X);
                }
                if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Vertical16X))
                {
                    SupportedVerticalBinnings.Add(uEye.Defines.BinningMode.Vertical16X);
                }
            }

            Int32 s32Factor;
            camStatus = cam.Size.Binning.GetFactorVertical(out s32Factor);
        }

        private void updateHorizontalSubsampling()
        {
            List<SubsamplingMode> SubsamplingHorizontal = new List<SubsamplingMode>();

            uEye.Defines.SubsamplingMode mode;
            camStatus = cam.Size.Subsampling.GetSupported(out mode);
            if ((mode & uEye.Defines.SubsamplingMode.Disable) == mode)
            {
                // Horizontal Subsampling not supported
            }
            else
            {
                if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Horizontal2X))
                {
                    SubsamplingHorizontal.Add(uEye.Defines.SubsamplingMode.Horizontal2X);
                }
                if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Horizontal3X))
                {
                    SubsamplingHorizontal.Add(uEye.Defines.SubsamplingMode.Horizontal3X);
                }
                if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Horizontal4X))
                {
                    SubsamplingHorizontal.Add(uEye.Defines.SubsamplingMode.Horizontal4X);
                }
                if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Horizontal5X))
                {
                    SubsamplingHorizontal.Add(uEye.Defines.SubsamplingMode.Horizontal5X);
                }
                if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Horizontal6X))
                {
                    SubsamplingHorizontal.Add(uEye.Defines.SubsamplingMode.Horizontal6X);
                }
                if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Horizontal8X))
                {
                    SubsamplingHorizontal.Add(uEye.Defines.SubsamplingMode.Horizontal8X);
                }
                if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Horizontal16X))
                {
                    SubsamplingHorizontal.Add(uEye.Defines.SubsamplingMode.Horizontal16X);
                }
            }

            Int32 s32Factor;

            camStatus = cam.Size.Subsampling.GetFactorHorizontal(out s32Factor);
        }

        private void updateVerticalSubsampling()
        {
            // vertical Subsampling
            List<SubsamplingMode> comboBoxFormatSubsamplingVertical = new List<SubsamplingMode>();

            uEye.Defines.SubsamplingMode mode;
            camStatus = cam.Size.Subsampling.GetSupported(out mode);

            if ((mode & uEye.Defines.SubsamplingMode.Disable) == mode)
            {
                // vertical Subsampling not supported
            }
            else
            {
                if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Vertical2X))
                {
                    comboBoxFormatSubsamplingVertical.Add(SubsamplingMode.Vertical2X);
                }
                if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Vertical3X))
                {
                    comboBoxFormatSubsamplingVertical.Add(SubsamplingMode.Vertical3X);
                }
                if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Vertical4X))
                {
                    comboBoxFormatSubsamplingVertical.Add(SubsamplingMode.Vertical4X);
                }
                if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Vertical5X))
                {
                    comboBoxFormatSubsamplingVertical.Add(SubsamplingMode.Vertical5X);
                }
                if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Vertical6X))
                {
                    comboBoxFormatSubsamplingVertical.Add(SubsamplingMode.Vertical6X);
                }
                if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Vertical8X))
                {
                    comboBoxFormatSubsamplingVertical.Add(SubsamplingMode.Vertical8X);
                }
                if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Vertical16X))
                {
                    comboBoxFormatSubsamplingVertical.Add(SubsamplingMode.Vertical16X);
                }
            }

            Int32 s32Factor;
            camStatus = cam.Size.Subsampling.GetFactorVertical(out s32Factor);
        }

        # endregion queryParameterSets

        



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

        [Input("AOI")]
        IDiffSpread<VVVV.Utils.VMath.Vector2D> FAOI;

        [Input("Crop")]
        IDiffSpread<VVVV.Utils.VMath.Vector2D> FCrop;

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
