using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using VVVV.CV.Core;
using VVVV.Utils;
using VVVV.Core.Logging;
using uEye;
using uEye.Defines;
using uEye.Types;
using System.Drawing;
using Emgu.CV;
using System.ComponentModel.Composition;

namespace VVVV.Nodes.OpenCV.IDS
{
	public class VideoInInstance : IGeneratorInstance
	{
        private Camera cam = null;
        private uEye.Defines.Status camStatus { get; set; }

        public List<int> PossibleBinningX = new List<int>();
        public List<int> PossibleBinningY = new List<int>();

        public List<int> PossibleSubsamplingX = new List<int>();
        public List<int> PossibleSubsamplingY = new List<int>();

        public bool checkParams = false;

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
                camStatus = cam.PixelFormat.Set(FColorMode);

                uEye.Defines.ColorMode pixFormat;
                camStatus = cam.PixelFormat.Get(out pixFormat);

                camStatus = cam.Size.ImageFormat.Set((uint)FFormat);

                // start capturee
                camStatus = cam.Memory.Allocate();
                camStatus = cam.Acquisition.Capture();

                // query infos
                queryHorizontalBinning();


                int bpp;
                camStatus = cam.PixelFormat.GetBytesPerPixel(out bpp);

                uEye.Types.ImageInfo info;
                camStatus = cam.Information.GetImageInfo(0, out info);

                uEye.Types.SensorInfo si;
                camStatus = cam.Information.GetSensorInfo(out si);

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

                checkParams = true;

                Status = "OK";
				return true;
			}
			catch (Exception e)
			{
				Status = e.Message;
				return false;
			}
		}
        

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

                cam.EventFrame -= onFrameEvent;

                if (started)
                    camStatus = cam.Acquisition.Stop();

                cam.Exit();
                cam = null;

                camOpen = false;
            }
		}

        public void queryFramerate()
        {

        }

        public void QueryCameraCapabilities()
        {
            // binning
            queryHorizontalBinning();
            queryVerticalBinning();

            // subsampling
            queryHorizontalSubsampling();
            queryVerticalSubsampling();

            

            // get camera aoi range size
            //uEye.Types.Range<Int32> rangeWidth, rangeHeight;
            //camStatus = cam.Size.AOI.GetSizeRange(out rangeWidth, out rangeHeight);

            // get actual aoi
            //System.Drawing.Rectangle rect;
            //camStatus = cam.Size.AOI.Get(out rect);

            // get pos range
            //uEye.Types.Range<Int32> rangePosX, rangePosY;
            //camStatus = cam.Size.AOI.GetPosRange(out rangePosX, out rangePosY);
        }


        #region setParameters

        public void SetBinning(string binningX, string binningY)
        {
            BinningMode modeX = (BinningMode)Enum.Parse(typeof(BinningMode), binningX);
            BinningMode modeY = (BinningMode)Enum.Parse(typeof(BinningMode), binningY);

            if (camOpen)
                camStatus = cam.Size.Binning.Set(modeX | modeY);
        }

        public void SetSubsampling(string subsamplingX, string subsamplingY)
        {
            SubsamplingMode modeX = (SubsamplingMode)Enum.Parse(typeof(SubsamplingMode), subsamplingX);
            SubsamplingMode modeY = (SubsamplingMode)Enum.Parse(typeof(SubsamplingMode), subsamplingY);

            if (camOpen)
                camStatus = cam.Size.Subsampling.Set(modeX | modeY);
        }

        private void mirrorHorizontal(bool Enable)
        {
            camStatus = cam.RopEffect.Set(uEye.Defines.RopEffectMode.LeftRight, Enable);
        }

        private void mirrorVertical(bool Enable)
        {
            camStatus = cam.RopEffect.Set(uEye.Defines.RopEffectMode.UpDown, Enable);
        }

        public void SetAoi(int left, int top, int width, int height)
        {
            //System.Drawing.Rectangle rect = new Rectangle();

            uEye.Types.Range<Int32> rangeWidth, rangeHeight;
            camStatus = cam.Size.AOI.GetSizeRange(out rangeWidth, out rangeHeight);

            uEye.Types.Range<Int32> rangePosX, rangePosY;
            camStatus = cam.Size.AOI.GetPosRange(out rangePosX, out rangePosY);

            while ((width % rangeWidth.Increment) != 0)
                --width;

            while ((height % rangeHeight.Increment) != 0)
                --height;

            while ((left % rangePosX.Increment) != 0)
                --left;

            while ((top % rangePosY.Increment) != 0)
                --top;

            //camStatus = cam.Size.AOI.Get(out rect);

            //rect.Width = width;
            //rect.Height = height;

            //rect.X = left;
            //rect.Y = top;

            camStatus = cam.Size.AOI.Set(left, top, width, height);

            //camStatus = cam.Size.AOI.Set(rect);


            // memory reallocation
            Int32[] memList;
            camStatus = cam.Memory.GetList(out memList);
            camStatus = cam.Memory.Free(memList);
            camStatus = cam.Memory.Allocate();

        }

        #endregion setParameters


        #region queryParameterSets

        private void queryHorizontalBinning()
        {
            PossibleBinningX.Clear();
            PossibleBinningX.Add(0);

            if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Horizontal2X))
                PossibleBinningX.Add(1);
            if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Horizontal3X))
                PossibleBinningX.Add(2);
            if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Horizontal4X))
                PossibleBinningX.Add(3);
            if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Horizontal5X))
                PossibleBinningX.Add(4);
            if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Horizontal6X))
                PossibleBinningX.Add(5);
            if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Horizontal8X))
                PossibleBinningX.Add(6);
            if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Horizontal16X))
                PossibleBinningX.Add(7);            
        }

        private void queryVerticalBinning()
        {
            PossibleBinningY.Clear();
            PossibleBinningY.Add(0);

            if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Vertical2X))
                PossibleBinningY.Add(1);
            if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Vertical3X))
                PossibleBinningY.Add(2);
            if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Vertical4X))
                PossibleBinningY.Add(3);
            if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Vertical5X))
                PossibleBinningY.Add(4);
            if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Vertical6X))
                PossibleBinningY.Add(5);
            if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Vertical8X))
                PossibleBinningY.Add(6);
            if (cam.Size.Binning.IsSupported(uEye.Defines.BinningMode.Vertical16X))
                PossibleBinningY.Add(7);
        }

        private void queryHorizontalSubsampling()
        {
            PossibleSubsamplingX.Clear();
            PossibleSubsamplingX.Add(0);

            if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Horizontal2X))
                PossibleSubsamplingX.Add(1);
            if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Horizontal3X))
                PossibleSubsamplingX.Add(2);
            if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Horizontal4X))
                PossibleSubsamplingX.Add(3);
            if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Horizontal5X))
                PossibleSubsamplingX.Add(4);
            if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Horizontal6X))
                PossibleSubsamplingX.Add(5);
            if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Horizontal8X))
                PossibleSubsamplingX.Add(6);
            if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Horizontal16X))
                PossibleSubsamplingX.Add(7);
        }

        private void queryVerticalSubsampling()
        {
            PossibleSubsamplingY.Clear();
            PossibleSubsamplingY.Add(0);

            if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Vertical2X))
                PossibleSubsamplingY.Add(1);
            if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Vertical3X))
                PossibleSubsamplingY.Add(2);
            if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Vertical4X))
                PossibleSubsamplingY.Add(3);
            if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Vertical5X))
                PossibleSubsamplingY.Add(4);
            if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Vertical6X))
                PossibleSubsamplingY.Add(5);
            if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Vertical8X))
                PossibleSubsamplingY.Add(6);
            if (cam.Size.Subsampling.IsSupported(uEye.Defines.SubsamplingMode.Vertical16X))
                PossibleSubsamplingY.Add(7);
        }

        # endregion queryParameterSets

        



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





    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region enums
    public enum BinningYMode
    {
        Disable,
        Vertical2X,
        Vertical3X,
        Vertical4X,
        Vertical5X,
        Vertical6X,
        Vertical8X,
        Vertical16X
    }

    public enum BinningXMode
    {
        Disable,
        Horizontal2X,
        Horizontal3X,
        Horizontal4X,
        Horizontal5X,
        Horizontal6X,
        Horizontal8X,
        Horizontal16X
    }

    public enum SubsamplingYMode
    {
        Disable,
        Vertical2X,
        Vertical3X,
        Vertical4X,
        Vertical5X,
        Vertical6X,
        Vertical8X,
        Vertical16X
    }

    public enum SubsamplingXMode     
    {
        Disable,
        Horizontal2X,
        Horizontal3X,
        Horizontal4X,
        Horizontal5X,
        Horizontal6X,
        Horizontal8X,
        Horizontal16X
    }
    #endregion enums

    #region PluginInfo
    [PluginInfo(Name = "VideoIn", Category = "uEye", Help = "Capture from camera devices", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class VideoInNode : IGeneratorNode<VideoInInstance>
	{
        #region fields and pins
        [Input("Camera Id")]
        IDiffSpread<int> FInCamId;

        [Input("Binning X", DefaultEnumEntry = "Disable")]
        public IDiffSpread<BinningXMode> FInBinningX;

        [Input("Binning Y", DefaultEnumEntry = "Disable")]
        public IDiffSpread<BinningYMode> FInBinningY;

        [Input("Subsampling X", DefaultEnumEntry = "Disable")]
        public IDiffSpread<SubsamplingXMode> FInSubsamplingX;

        [Input("Subsampling Y", DefaultEnumEntry = "Disable")]
        public IDiffSpread<SubsamplingYMode> FInSubsamplingY;

        [Input("Format Id")]
        IDiffSpread<int> FInFormatId;

        [Input("Resolution")]
		IDiffSpread<VVVV.Utils.VMath.Vector2D  > FResolution;

        [Input("AOI")]
        IDiffSpread<VVVV.Utils.VMath.Vector2D> FInAOI;

        [Input("Crop")]
        IDiffSpread<VVVV.Utils.VMath.Vector2D> FInCrop;

        [Input("Color Mode")]
		IDiffSpread<uEye.Defines.ColorMode> FColorMode;

		[Input("FPS", DefaultValue=30, MinValue=0, MaxValue=1024)]
		IDiffSpread<int> FFps;

        [Output("Framerate Range")]
        ISpread<VVVV.Utils.VMath.Vector2D> FOutFramerateRange;

        [Output("supported  Binning X")]
        ISpread<ISpread<string>> FOutBinningXModes;

        [Output("supported  Binning Y")]
        ISpread<ISpread<string>> FOutBinningYModes;

        [Output("supported  Subsampling X")]
        ISpread<ISpread<string>> FOutSubsamplingXModes;

        [Output("supported  Subsampling Y")]
        ISpread<ISpread<string>> FOutSubsamplingYModes;

        [Output("available Formats")]
        ISpread<ISpread<string>> FOutFormats;

        [Import()]
        public ILogger FLogger;

        bool firstframe = true;

        bool queryRequest = false;
        #endregion fields and pins

        override protected void Update(int InstanceCount, bool SpreadCountChanged)
		{
            for (int i = 0; i < InstanceCount; i++)
            {
                if (FProcessor[i].checkParams)
                {
                    setBinning(i);
                    setSubsampling(i);
                    setAOI(i);

                    FProcessor[i].checkParams = false;
                }
            }

            // query Featuresets
            if ((FPinInEnabled.IsChanged || SpreadCountChanged) && FPinInEnabled[0] )
            {
                FLogger.Log(LogType.Debug, "make query request");
                queryRequest = true;
            }

            if (queryRequest)
            {
                queryFeatures(InstanceCount);
            }

            // set subsampling
            if ((FInSubsamplingX.IsChanged || FInSubsamplingY.IsChanged) /*&& firstframe == false*/)
            {
                for (int i = 0; i < InstanceCount; i++)
                    setSubsampling(i);
            }

            // set binning
            if ((FInBinningX.IsChanged || FInBinningY.IsChanged) && firstframe == false)
            {
                for (int i = 0; i < InstanceCount; i++)
                    setBinning(i);
            }

            // set AOI
            if (FInAOI.IsChanged || FInCrop.IsChanged /*|| (FPinInEnabled.IsChanged && FPinInEnabled[0])*/ )
            {
                for (int i = 0; i < InstanceCount; i++)
                    setAOI(i);
            }


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

            if (firstframe) firstframe = false;
        }

        private void setSubsampling(int instanceId)
        {
            if (FProcessor[instanceId].Enabled)
            {
                string x = FInSubsamplingX[instanceId].ToString();
                string y = FInSubsamplingY[instanceId].ToString();

                if (!FProcessor[instanceId].PossibleSubsamplingX.Contains((int)FInSubsamplingX[instanceId]))
                {
                    FLogger.Log(LogType.Debug, FInSubsamplingX[instanceId].ToString() + " is not supported");
                    x = "Disable";
                }

                if (!FProcessor[instanceId].PossibleSubsamplingY.Contains((int)FInSubsamplingY[instanceId]))
                {
                    FLogger.Log(LogType.Debug, FInSubsamplingY[instanceId].ToString() + " is not supported");
                    y = "Disable";
                }

                //FLogger.Log(LogType.Debug, "set subsampling of instance " + i + " to " + x + " | " + y);

                FProcessor[instanceId].SetSubsampling(x, y);
            }
        }

        private void setBinning(int instanceId)
        {
            if (FProcessor[instanceId].Enabled)
            {
                string x = FInBinningX[instanceId].ToString();
                string y = FInBinningY[instanceId].ToString();

                if (!FProcessor[instanceId].PossibleBinningX.Contains((int)FInBinningX[instanceId]))
                {
                    FLogger.Log(LogType.Debug, FInBinningX[instanceId].ToString() + " is not supported");
                    x = "Disable";
                }

                if (!FProcessor[instanceId].PossibleBinningY.Contains((int)FInBinningY[instanceId]))
                {
                    FLogger.Log(LogType.Debug, FInBinningY[instanceId].ToString() + " is not supported");
                    y = "Disable";
                }

                FProcessor[instanceId].SetBinning(x, y);
            }
        }

        private void setAOI(int instanceIdi)
        {
                if (FProcessor[instanceIdi].Enabled)
                {
                    FProcessor[instanceIdi].SetAoi((int)FInCrop[instanceIdi].x, (int)FInCrop[instanceIdi].y,
                                            (int)FInAOI[instanceIdi].x, (int)FInAOI[instanceIdi].y);
                }
        }

        private void queryFeatures(int InstanceCount)
        {
            FOutSubsamplingXModes.SliceCount = InstanceCount;
            FOutSubsamplingYModes.SliceCount = InstanceCount;
            FOutBinningXModes.SliceCount = InstanceCount;
            FOutBinningYModes.SliceCount = InstanceCount;

            for (int i = 0; i < InstanceCount; i++)
            {
                if (FProcessor[i].camOpen)
                {
                    FLogger.Log(LogType.Debug, "query parameter for camera " + i);
                    FProcessor[i].QueryCameraCapabilities();

                    FOutSubsamplingXModes[i].SliceCount = FProcessor[i].PossibleSubsamplingX.Count;
                    FOutSubsamplingYModes[i].SliceCount = FProcessor[i].PossibleSubsamplingY.Count;
                    FOutBinningXModes[i].SliceCount = FProcessor[i].PossibleSubsamplingY.Count;
                    FOutBinningYModes[i].SliceCount = FProcessor[i].PossibleSubsamplingY.Count;


                    for (int m = 0; m < FProcessor[i].PossibleSubsamplingX.Count; m++)
                        FOutSubsamplingXModes[i][m] = Enum.GetName(typeof(SubsamplingXMode), m);

                    for (int m = 0; m < FProcessor[i].PossibleSubsamplingY.Count; m++)
                        FOutSubsamplingYModes[i][m] = Enum.GetName(typeof(SubsamplingYMode), m);

                    for (int m = 0; m < FProcessor[i].PossibleBinningX.Count; m++)
                        FOutBinningXModes[i][m] = Enum.GetName(typeof(BinningXMode), m);

                    for (int m = 0; m < FProcessor[i].PossibleBinningY.Count; m++)
                        FOutBinningYModes[i][m] = Enum.GetName(typeof(BinningYMode), m);

                    queryRequest = false;
                }
            }
        }
    }
}
