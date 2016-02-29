using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using VVVV.CV.Core;
using VVVV.Utils.VMath;
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
        #region fields

        private Camera cam = null;
        public Status camStatus { get; set; }

        public BinningMode supportedBinning;
        public SubsamplingMode supportedSubsampling;

        public double currentFramerate;
        public Range<double> framerateRange;

        public Range<int> AOIWidth, AOIHeight;
        public Range<int> CropXRange, CropYRange;

        //public int gainMaster;
        //public int gainRed;
        //public int gainGreen;
        //public int gainBlue;

        public bool gainAutoSupported;
        public bool gainBoostSupported;

        public Range<int> pixelClockRange;
        public int[] pixelClockList;

        public uEye.Defines.Whitebalance.AntiFlickerMode supportedAntiflicker;


        // Timing
        public bool exposureSupported;
        public Range<double> exposureRange;
        public int pixelClockCurrent;
        public double exposureCurrent;


        private bool frameAvailable = false;

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

        #endregion fields


        public override bool Open()
		{
            Status = "";
            Status += "\n Open() ";

            try
            {
                cam = new Camera();

                camStatus = cam.Init(FCamId);

                // set flags
                camOpen = true;

                configureOutput();

                startCapture();

                QueryCameraCapabilities();

                // set flags
                camOpen = true;

                checkParams = true;

                //Status = "OK";
                Status += camStatus.ToString();
                return true;
            }
            catch (Exception e)
			{
				Status = e.Message;
				return false;
			}
		}

        private void startCapture()
        {
            Status += "\n startCapture()";
            camStatus = cam.Memory.Allocate();
            camStatus = cam.Acquisition.Capture();

            cam.EventFrame += onFrameEvent;
        }

        private void stopCapture()
        {
            Status += "\n stopCapture()";
            cam.EventFrame -= onFrameEvent;
            camStatus = cam.Acquisition.Stop();
            //camStatus = cam.Memory.Free();
        }

        private void configureOutput()
        {
            Status += "\n configureOutput()";
            //cam.EventFrame -= onFrameEvent;

            //int MemID;
            //camStatus = cam.Memory.GetActive(out MemID);

            //int lastMemID;
            //camStatus = cam.Memory.GetLast(out lastMemID);

            //bool locked;
            //cam.Memory.GetLocked(MemID, out locked);

            //Status += "\n LOCKED = " + locked + " - " + MemID + " | " + lastMemID;

            // memory reallocation
            int[] memList;
            camStatus = cam.Memory.GetList(out memList);
            camStatus = cam.Memory.Free(memList);
            camStatus = cam.Memory.Allocate();

            Status += "\n memRealloc " + camStatus;


            uEye.Defines.ColorMode pixFormat;
            camStatus = cam.PixelFormat.Get(out pixFormat);

            TColorFormat format = GetColor(pixFormat);

            Status += "\n format " + camStatus;

            Rectangle a;
            camStatus = cam.Size.AOI.Get(out a);

            Status += "\n AOI " + camStatus;


            Status += "\n initialize Output ";
            FOutput.Image.Initialise(a.Width, a.Height, format);
            
            //cam.EventFrame += onFrameEvent;

            Status += camStatus;
        }

        public override void Close()
        {
            Status += "\n Close()";
            if (cam != null)
            {
                try
                { 
                    //bool started;
                    //camStatus = cam.Acquisition.HasStarted(out started);

                    //cam.EventFrame -= onFrameEvent;

                    stopCapture();

                    int[] MemIds;
                    camStatus = cam.Memory.GetList(out MemIds);

                    camStatus = cam.Memory.Free(MemIds);

                    //if (started)
                    //    camStatus = cam.Acquisition.Stop();

                    cam.Exit();
                
                    cam = null;

                    camOpen = false;
                }
                catch (Exception e)
                {
                    Status = e.Message;
                }
        }
        }

        private void onFrameEvent(object sender, EventArgs e)
        {
            frameAvailable = true;
        }

        private TColorFormat GetColor(uEye.Defines.ColorMode color)
        {
            switch (color)
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
            if (frameAvailable)
            {
                this.Status = "";
                this.Status += "\n onFrameEvent() ";

                bool started;
                camStatus = cam.Acquisition.HasStarted(out started);

                bool finished;
                camStatus = cam.Acquisition.IsFinished(out finished);

                if (cam.IsOpened && started && finished)
                {
                    int MemId;
                    camStatus = cam.Memory.GetActive(out MemId);

                    int lastMemID;
                    camStatus = cam.Memory.GetLast(out lastMemID);

                    bool locked;
                    camStatus = cam.Memory.GetLocked(MemId, out locked);

                    Status += "\n LOCKED = " + locked + " - " + MemId + " | " + lastMemID + " ";

                    int w, h;
                    camStatus = cam.Memory.GetSize(MemId, out w, out h);


                    camStatus = cam.Memory.Lock(MemId);
                    //copy to FOutput
                    IntPtr memPtr;
                    camStatus = cam.Memory.ToIntPtr(out memPtr);
                    camStatus = cam.Memory.CopyImageMem(memPtr, MemId, FOutput.Data);

                    camStatus = cam.Memory.Unlock(MemId);

                    FOutput.Send();
                }

                this.Status += camStatus;
            }
        }

        public void QueryCameraCapabilities() // only do this on opening
        {
            Status += "\n QueryCameraCapabilities()";

            // binning
            camStatus = cam.Size.Binning.GetSupported(out supportedBinning);

            // subsampling
            camStatus = cam.Size.Subsampling.GetSupported(out supportedSubsampling);


            //cam.AutoFeatures.Sensor.Whitebalance.Supported;
            //cam.AutoFeatures.Sensor.Framerate.Supported;

            //cam.AutoFeatures.Sensor.AntiFlicker.GetSupported(out supportetAntiflicker)

            //cam.AutoFeatures.Sensor.BacklightCompensation;
            //cam.AutoFeatures.Sensor.Contrast;
            //cam.AutoFeatures.Sensor.GainShutter;
            //cam.AutoFeatures.Sensor.Shutter;



            //cam.AutoFeatures.Software.Framerate;
            //cam.AutoFeatures.Software.FrameSkip;
            //cam.AutoFeatures.Software.Gain;
            //cam.AutoFeatures.Software.Hysteresis;
            //cam.AutoFeatures.Software.PeakWhite;
            //cam.AutoFeatures.Software.Reference;
            //cam.AutoFeatures.Software.Shutter;
            //cam.AutoFeatures.Software.Speed;
            //cam.AutoFeatures.Software.WhiteBalance;

        }


        private double clampRange (double value, Range<double> range)
        {
            if (value < range.Minimum)
                return range.Minimum;
            if (value > range.Maximum)
                return range.Maximum;

            while ((value % range.Increment) != 0)
                --value;
            return value;
        }

        private int clampRange(int value, Range<int> range)
        {
            if (value < range.Minimum)
                return range.Minimum;
            if (value > range.Maximum)
                return range.Maximum;

            return value;
        }

        private static BinningMode clampRange (BinningMode value, BinningMode supported)
        {
            if ((value & supported) == value)
                return value;

            return BinningMode.Disable;
        }

        private static SubsamplingMode clampRange(SubsamplingMode value, SubsamplingMode supported)
        {
            if ((value & supported) == value)
                return value;

            return SubsamplingMode.Disable;
        }

        #region setParameters

        // Size
        public void SetBinning(string binningX, string binningY)
        {
            Status += "\n SetBinning()";
            BinningMode modeX = (BinningMode)Enum.Parse(typeof(BinningMode), binningX);
            BinningMode modeY = (BinningMode)Enum.Parse(typeof(BinningMode), binningY);

            if (camOpen) // needed?
            {

                BinningMode _modeX = clampRange(modeX, supportedBinning);
                BinningMode _modeY = clampRange(modeY, supportedBinning);

                camStatus = cam.Size.Binning.Set(_modeX | _modeY);

                configureOutput();
            }        
        }

        public void SetSubsampling(string subsamplingX, string subsamplingY)
        {
            Status += "\n SetSubsampling()";
            SubsamplingMode modeX = (SubsamplingMode)Enum.Parse(typeof(SubsamplingMode), subsamplingX);
            SubsamplingMode modeY = (SubsamplingMode)Enum.Parse(typeof(SubsamplingMode), subsamplingY);

            if (camOpen)
            {

                SubsamplingMode _modeX = clampRange(modeX, supportedSubsampling);
                SubsamplingMode _modeY = clampRange(modeY, supportedSubsampling);

                camStatus = cam.Size.Subsampling.Set(_modeX | _modeY);

                configureOutput();
            }            
        }

        public void mirrorHorizontal(bool Enable)
        {
            Status += "\n mirrorHorizontal()";
            camStatus = cam.RopEffect.Set(uEye.Defines.RopEffectMode.LeftRight, Enable);
        }

        public void mirrorVertical(bool Enable)
        {
            Status += "\n mirrorVertical()";
            camStatus = cam.RopEffect.Set(uEye.Defines.RopEffectMode.UpDown, Enable);
        }

        public void SetAoi(int left, int top, int width, int height)
        {
            Status += "\n SetAoi()";

            queryAOI();
            
            width = clampRange(width, AOIWidth);
            height = clampRange(height, AOIHeight);
            left = clampRange(left, CropXRange);
            top = clampRange(top, CropYRange);

            camStatus = cam.Size.AOI.Set(left, top, width, height);

            // changing aoi may affect timing
            queryTiming();

            configureOutput();
        }     

        public void setColorMode(ColorMode mode)
        {
            Status += "\n setColorMode()";
            stopCapture();

            camStatus = cam.PixelFormat.Set(mode);

            startCapture();

            configureOutput();
        }

        public void setGainMaster(int value)
        {
            cam.Gain.Hardware.Scaled.SetMaster(value);
        }

        public void setGainRed(int value)
        {
            cam.Gain.Hardware.Scaled.SetRed(value);
        }

        public void setGainGreen(int value)
        {
            cam.Gain.Hardware.Scaled.SetGreen(value);
        }

        public void setGainBlue(int value)
        {
            cam.Gain.Hardware.Scaled.SetBlue(value);
        }

        public void setAutoGain(bool enable)
        {
            cam.AutoFeatures.Software.Gain.SetEnable(enable);
        }

        public void setGainBoost(bool enable)
        {
            cam.Gain.Hardware.Boost.SetEnable(enable);
        }        

        public void setWhitebalance(bool enable)
        {
            cam.AutoFeatures.Software.WhiteBalance.SetEnable(enable);
        }


        public void SetFrameRate(double fps)
        {
            Status += "\n SetFrameRate()";

            camStatus = cam.Timing.Framerate.Set(fps);
        }

        public void setExposure(double exp)
        {
            cam.Timing.Exposure.Set(exp);
            //cam.Timing.PixelClock.
        }

        public void setPixelClock(int value)
        {
            cam.Timing.PixelClock.Set(value);
        }


        #endregion setParameters


        #region queryParameterSets

        public void queryAOI()
        {
            if (camOpen)
            {
                camStatus = cam.Size.AOI.GetSizeRange(out AOIWidth, out AOIHeight);
                camStatus = cam.Size.AOI.GetPosRange(out CropXRange, out CropYRange);

            }
        }

        public void queryGain()
        {
            //camStatus = cam.Gain.Hardware.Scaled.GetMaster(out gainMaster);
            //camStatus = cam.Gain.Hardware.Scaled.GetRed(out gainRed);
            //camStatus = cam.Gain.Hardware.Scaled.GetGreen(out gainGreen);
            //camStatus = cam.Gain.Hardware.Scaled.GetBlue(out gainBlue);

            gainBoostSupported = cam.Gain.Hardware.Boost.Supported;

            gainAutoSupported = cam.AutoFeatures.Sensor.Gain.Supported;

        }

        public void queryTiming()
        {
            cam.Timing.Exposure.GetSupported(out exposureSupported);
            cam.Timing.Exposure.GetRange(out exposureRange);
            cam.Timing.Exposure.Get(out exposureCurrent);

            cam.Timing.PixelClock.GetRange(out pixelClockRange);
            cam.Timing.PixelClock.GetList(out pixelClockList);
            cam.Timing.PixelClock.Get(out pixelClockCurrent);

            cam.Timing.Framerate.GetFrameRateRange(out framerateRange);

            cam.Timing.Framerate.GetCurrentFps(out currentFramerate);

        }

        #endregion queryParameterSets


    }


    #region enums
    public enum BinningYMode
    {
        Disable = 0,
        Vertical2X = 1,
        Vertical4X = 4,
        Vertical3X = 16,
        Vertical5X = 64,
        Vertical6X = 256,
        Vertical8X = 1024,
        Vertical16X = 4096
    }

    public enum BinningXMode
    {
        Disable = 0,
        Horizontal2X = 2,
        Horizontal4X = 8,
        Horizontal3X = 32,
        Horizontal5X = 128,
        Horizontal6X = 512,
        Horizontal8X = 2048,
        Horizontal16X = 8192
    }

    public enum SubsamplingYMode
    {
        Disable = 0,
        Vertical2X = 1,
        Vertical4X = 4,
        Vertical3X = 16,
        Vertical5X = 128,
        Vertical6X = 256,
        Vertical8X = 1024,
        Vertical16X = 4096
    }

    public enum SubsamplingXMode     
    {
        Disable = 0,
        Horizontal2X = 2,
        Horizontal4X = 8,
        Horizontal3X = 32,
        Horizontal5X = 128,
        Horizontal6X = 512,
        Horizontal8X = 2048,
        Horizontal16X = 8192
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
       
        [Input("Mirror X")]
        IDiffSpread<bool> FInMirrorX;

        [Input("Mirror Y")]
        IDiffSpread<bool> FInMirrorY;

        [Input("Format Id")]
        IDiffSpread<int> FInFormatId;

        [Input("AOI")]
        IDiffSpread<VVVV.Utils.VMath.Vector2D> FInAOI;

        [Input("Crop")]
        IDiffSpread<VVVV.Utils.VMath.Vector2D> FInCrop;

        [Input("Color Mode")]
		IDiffSpread<uEye.Defines.ColorMode> FColorMode;

		[Input("FPS", DefaultValue=30, MinValue=0, MaxValue=1024)]
		IDiffSpread<int> FInFps;

        [Input("Exposure", DefaultValue = 0.1, MinValue = 0)]
        IDiffSpread<double> FInExposure;

        [Input("Pixelclock", DefaultValue = 20, MinValue = 0)]
        IDiffSpread<int> FInPixelClock;

        // ------------------------------------------------------------

        [Output("Framerate Range")]
        ISpread<VVVV.Utils.VMath.Vector2D> FOutFramerateRange;

        [Output("Framerate")]
        ISpread<double> FOutcurrentFramerate;

        [Output("supported  Binning X")]
        ISpread<ISpread<string>> FOutBinningXModes;

        [Output("supported  Binning Y")]
        ISpread<ISpread<string>> FOutBinningYModes;

        [Output("supported  Subsampling X")]
        ISpread<ISpread<string>> FOutSubsamplingXModes;

        [Output("supported  Subsampling Y")]
        ISpread<ISpread<string>> FOutSubsamplingYModes;

        [Output("AOI Width Range", Visibility = PinVisibility.Hidden)]
        ISpread<Vector2D> FOutAOIWidthRange;

        [Output("AOI Height Range", Visibility = PinVisibility.Hidden)]
        ISpread<Vector2D> FOutAOIHeightRange;

        [Output("CropX Range", Visibility = PinVisibility.Hidden)]
        ISpread<Vector2D> FOutCropXRange;

        [Output("CropY Range", Visibility = PinVisibility.Hidden)]
        ISpread<Vector2D> FOutCropYRange;

        [Output("Automatic Gain Supported")]
        ISpread<bool> FOutgainAutoSupported;

        [Output("Gain BoostSupported")]
        ISpread<bool> FOutgainBoostSupported;

        [Output("supported Antiflicker Mode", Visibility = PinVisibility.Hidden)]
        ISpread<uEye.Defines.Whitebalance.AntiFlickerMode> FOutsupportetAntiflicker;

        [Output("Exposure Supported")]
        ISpread<bool> FOutexposureSupported;

        [Output("currennt PixelClock")]
        ISpread<int> FOutpixelClockCurrent;

        [Output("PixelClock Range", Visibility = PinVisibility.Hidden)]
        ISpread<Vector2D> FOutpixelClockRange;

        [Output("currennt Exposure")]
        ISpread<double> FOutexposureCurrent;

        [Output("Exposure Range", Visibility = PinVisibility.Hidden)]
        ISpread<Vector2D> FOutexposureRange;

        [Import()]
        public ILogger FLogger;

        bool firstframe = true;

        bool queryRequest = false;
        #endregion fields and pins

        override protected void Update(int InstanceCount, bool SpreadCountChanged)
		{
            #region set slicecounts
            // framerate
            FOutFramerateRange.SliceCount = InstanceCount;
            FOutcurrentFramerate.SliceCount = InstanceCount;

            // binning / subsampling
            FOutSubsamplingXModes.SliceCount = InstanceCount;
            FOutSubsamplingYModes.SliceCount = InstanceCount;
            FOutBinningXModes.SliceCount = InstanceCount;
            FOutBinningYModes.SliceCount = InstanceCount;

            // AOI
            FOutAOIWidthRange.SliceCount = InstanceCount;
            FOutAOIWidthRange.SliceCount = InstanceCount;
            FOutCropXRange.SliceCount = InstanceCount;
            FOutCropYRange.SliceCount = InstanceCount;

            // gain
            FOutgainAutoSupported.SliceCount = InstanceCount;
            FOutgainBoostSupported.SliceCount = InstanceCount;

            FOutgainAutoSupported.SliceCount = InstanceCount;
            FOutgainBoostSupported.SliceCount = InstanceCount;

            //Timing
            FOutpixelClockRange.SliceCount = InstanceCount;
            FOutpixelClockCurrent.SliceCount = InstanceCount;

            FOutexposureCurrent.SliceCount = InstanceCount;
            FOutexposureSupported.SliceCount = InstanceCount;

            // Antiflicker
            FOutsupportetAntiflicker.SliceCount = InstanceCount;
            #endregion set slicecounts

            for (int i = 0; i < InstanceCount; i++)
            {

                if (FProcessor[i].checkParams)
                {
                    FProcessor[i].Status += "\n checkParams()";

                    setBinning(i);
                    setSubsampling(i);
                    setAOI(i);
                    setFramerate(i);
                    setColorMode(i);

                    FProcessor[i].setPixelClock(FInPixelClock[i]);
                    FProcessor[i].setExposure(FInExposure[i]);

                    FProcessor[i].checkParams = false;
                }
            }

            // query Featuresets
            if ((FPinInEnabled.IsChanged || SpreadCountChanged) && FPinInEnabled[0] )
                queryRequest = true;

            if (queryRequest)
            {
                for (int i = 0; i < InstanceCount; i++)
                {
                    if (FProcessor[i].camOpen)
                    {
                        FProcessor[i].Status += "\n queryRequest";
                        queryFeatures(InstanceCount, i);
                        //queryFramerateRange(i);
                        queryTiming(i);
                        queryAOIRange(i);
                        queryRequest = false;  // that's not so cool to be lazy here
                    }
                }
              
                      
            }

            // set subsampling
            if (FInSubsamplingX.IsChanged || FInSubsamplingY.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                {
                    setSubsampling(i);
                    queryAOIRange(i);
                }
            }

            // set binning
            if (FInBinningX.IsChanged || FInBinningY.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                {
                    setBinning(i);
                    queryAOIRange(i);
                }
                    

            }

            // set AOI
            if (FInAOI.IsChanged || FInCrop.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                {
                    setAOI(i);
                    queryAOIRange(i);
                }
            }

            // set FrameRate
            if (SpreadCountChanged || FInFps.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                    setFramerate(i);
            }

            // set mirroring
            if (FInMirrorX.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                    if (FProcessor[i].Enabled) FProcessor[i].mirrorHorizontal(FInMirrorX[i]);
            }

            if (FInMirrorY.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                    if (FProcessor[i].Enabled) FProcessor[i].mirrorVertical(FInMirrorY[i]);
            }

            // set camId
            if (SpreadCountChanged || FInCamId.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                    FProcessor[i].CamId = FInCamId[i];
            }
          
            // set Colormode
			if (SpreadCountChanged || FColorMode.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
                    if (FProcessor[i].Enabled)  FProcessor[i].setColorMode(FColorMode[i]);
			}

            // set Exposure
            if (SpreadCountChanged || FInExposure.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                    if (FProcessor[i].Enabled) FProcessor[i].setExposure(FInExposure[i]);
            }

            // set PixelClock
            if (SpreadCountChanged || FInPixelClock.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                    if (FProcessor[i].Enabled) FProcessor[i].setPixelClock(FInPixelClock[i]);
            }

            // query Timing
            if (FInBinningX.IsChanged || FInBinningY.IsChanged || FInSubsamplingX.IsChanged || FInSubsamplingY.IsChanged ||
                FInCrop.IsChanged || FInAOI.IsChanged || FInFps.IsChanged || FInExposure.IsChanged || FInPixelClock.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                {
                    if (FProcessor[i].Enabled)
                    {
                        queryTiming(i);
                    }
                }
            }


            // get timing values every frame
            if (!FPinInEnabled.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                {
                    try
                    {
                        if (FProcessor[i].Enabled)
                        {
                            queryTiming(i);
                        }
                    }
                    catch (Exception e)
                    {
                        FLogger.Log(LogType.Error, e.ToString());
                    }
                }
            }
            


            if (firstframe) firstframe = false;
        }

        private void queryTiming(int instanceId)
        {
            FProcessor[instanceId].Status += "\n queryTiming";
            //queryFramerateRange(i);
            FProcessor[instanceId].queryTiming();

            FOutcurrentFramerate[instanceId] = FProcessor[instanceId].currentFramerate;
            FOutFramerateRange[instanceId] = new Vector2D(FProcessor[instanceId].framerateRange.Minimum, FProcessor[instanceId].framerateRange.Maximum);

            FOutexposureSupported[instanceId] = FProcessor[instanceId].exposureSupported;

            FOutpixelClockCurrent[instanceId] = FProcessor[instanceId].pixelClockCurrent;
            FOutpixelClockRange[instanceId] = new Vector2D(FProcessor[instanceId].pixelClockRange.Minimum, FProcessor[instanceId].pixelClockRange.Maximum);

            FOutexposureCurrent[instanceId] = FProcessor[instanceId].exposureCurrent;
            FOutexposureRange[instanceId] = new Vector2D(FProcessor[instanceId].exposureRange.Minimum, FProcessor[instanceId].exposureRange.Maximum);



            //queryAOIRange(instanceId);
        }

        private void queryAOIRange(int instanceId)
        {
            FProcessor[instanceId].queryAOI();
            FOutAOIWidthRange[instanceId] = new Vector2D(FProcessor[instanceId].AOIWidth.Minimum, FProcessor[instanceId].AOIWidth.Maximum);
            FOutAOIHeightRange[instanceId] = new Vector2D(FProcessor[instanceId].AOIHeight.Minimum, FProcessor[instanceId].AOIHeight.Maximum);
            FOutCropXRange[instanceId] = new Vector2D(FProcessor[instanceId].CropXRange.Minimum, FProcessor[instanceId].CropXRange.Maximum);
            FOutCropYRange[instanceId] = new Vector2D(FProcessor[instanceId].CropYRange.Minimum, FProcessor[instanceId].CropYRange.Maximum);
        }

        private void setSubsampling(int instanceId)
        {
            if (FProcessor[instanceId].Enabled)
            {
                string x = FInSubsamplingX[instanceId].ToString();
                string y = FInSubsamplingY[instanceId].ToString();

                FProcessor[instanceId].SetSubsampling(x, y);
            }
        }

        private void setBinning(int instanceId)
        {
            if (FProcessor[instanceId].Enabled)
            {
                string x = FInBinningX[instanceId].ToString();
                string y = FInBinningY[instanceId].ToString();

                FProcessor[instanceId].SetBinning(x, y);
            }
        }

        private void setAOI(int instanceId)
        {
                if (FProcessor[instanceId].Enabled)
                {
                    FProcessor[instanceId].SetAoi((int)FInCrop[instanceId].x, (int)FInCrop[instanceId].y,
                                            (int)FInAOI[instanceId].x, (int)FInAOI[instanceId].y);
                }
        }

        private void setFramerate(int instanceId)
        {
            if (FProcessor[instanceId].Enabled)
            {
                FProcessor[instanceId].SetFrameRate(FInFps[instanceId]);
            }
        }

        private void queryFeatures(int InstanceCount, int instanceId)
        {
            if (FProcessor[instanceId].camOpen)
            {
                FLogger.Log(LogType.Debug, "query parameter for camera " + instanceId);

                FOutBinningXModes[instanceId].SliceCount = 0;
                FOutBinningYModes[instanceId].SliceCount = 0;
                FOutSubsamplingXModes[instanceId].SliceCount = 0;
                FOutSubsamplingYModes[instanceId].SliceCount = 0;

                int numsupported = Enum.GetValues(typeof(BinningXMode)).Length;  // it's the same for all 4 enums

                Array bxModes = Enum.GetValues(typeof(BinningXMode));
                Array byModes = Enum.GetValues(typeof(BinningYMode));
                Array sxModes = Enum.GetValues(typeof(SubsamplingXMode));
                Array syModes = Enum.GetValues(typeof(SubsamplingYMode));

                for (int i = 0; i < numsupported; i++)
                {
                    BinningXMode bx = (BinningXMode)bxModes.GetValue(i);
                    BinningMode bmodeX = (BinningMode)Enum.Parse(typeof(BinningMode), bx.ToString());
                    if ((bmodeX & FProcessor[instanceId].supportedBinning) == bmodeX)
                    {
                        FOutBinningXModes[instanceId].Add<string>(bmodeX.ToString());
                    }
                

                    BinningYMode by = (BinningYMode)byModes.GetValue(i);
                    BinningMode bmodeY = (BinningMode)Enum.Parse(typeof(BinningMode), by.ToString());
                    if ((bmodeY & FProcessor[instanceId].supportedBinning) == bmodeY)
                    {
                        FOutBinningYModes[instanceId].Add<string>(bmodeY.ToString());
                    }


                    SubsamplingXMode sx = (SubsamplingXMode)sxModes.GetValue(i);
                    SubsamplingMode smodeX = (SubsamplingMode)Enum.Parse(typeof(SubsamplingMode), sx.ToString());
                    if ((smodeX & FProcessor[instanceId].supportedSubsampling) == smodeX)
                    {
                        FOutSubsamplingXModes[instanceId].Add<string>(smodeX.ToString());
                    }


                    SubsamplingYMode sy = (SubsamplingYMode)syModes.GetValue(i);
                    SubsamplingMode smodeY = (SubsamplingMode)Enum.Parse(typeof(SubsamplingMode), sy.ToString());
                    if ((smodeY & FProcessor[instanceId].supportedSubsampling) == smodeY)
                    {
                        FOutSubsamplingYModes[instanceId].Add<string>(smodeY.ToString());
                    }

                }

                queryRequest = false;
            }
            else
            {
                //FProcessor[instanceId]
            }
            
        }

        private void setColorMode(int instanceId)
        {
            if (FProcessor[instanceId].Enabled)
            {
                FProcessor[instanceId].setColorMode(FColorMode[instanceId]);
            }
        }
    }
}
