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

        private bool frameAvailable = false;

        public bool checkParams = false;

        public bool camOpen = false;

        // geometry
        public BinningMode supportedBinning;
        public BinningMode currentBinningX;
        public BinningMode currentBinningY;
        public SubsamplingMode supportedSubsampling;
        public SubsamplingMode currentSubsamplingX;
        public SubsamplingMode currentSubsamplingY;

        public Range<int> AOIWidth, AOIHeight;
        public Range<int> CropXRange, CropYRange;

        // gain 
        public int currentgainRed;
        public int currentgainGreen;
        public int currentgainBlue;

        // features
        public bool gainAutoSupported;
        public bool gainBoostSupported;
        public bool whitebalanceSupported;

        public uEye.Defines.Whitebalance.AntiFlickerMode supportedAntiflicker;
        //public bool isGainRedSupported, isGainBlueSupported, isGainGreenSupported, isGainMasterSupported;

        // Timing
        public double currentFramerate;
        public Range<double> framerateRange;

        public bool exposureSupported;
        public Range<double> exposureRange;
        public double currentExposure;

        public int currentPixelClock;
        public Range<int> pixelClockRange;
        public int[] pixelClockList;

        #endregion fields

        public override bool Open()
		{
            try
            {
                cam = new Camera();

                cam.EventDeviceRemove += camDisconnect;
                cam.EventDeviceUnPlugged += camDisconnect;
                cam.EventDeviceReconnect += camReconnect;
                cam.EventDevicePluggedIn += camReconnect;

                camStatus = cam.Init(FCamId);

                configureOutput();

                startCapture();

                QueryCameraCapabilities();

                camOpen = true;

                checkParams = true;

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
            camStatus = cam.Memory.Allocate();
            camStatus = cam.Acquisition.Capture();

            cam.EventFrame += onFrameEvent;

        }

        private void stopCapture()
        {
            cam.EventFrame -= onFrameEvent;
            camStatus = cam.Acquisition.Stop();
        }

        private void configureOutput()
        {
            // memory reallocation
            int[] memList;
            camStatus = cam.Memory.GetList(out memList);
            camStatus = cam.Memory.Free(memList);
            camStatus = cam.Memory.Allocate();

            uEye.Defines.ColorMode pixFormat;
            camStatus = cam.PixelFormat.Get(out pixFormat);

            TColorFormat format = GetColor(pixFormat);

            Rectangle a;
            camStatus = cam.Size.AOI.Get(out a);

            FOutput.Image.Initialise(a.Width, a.Height, format);
        }

        public override void Close()
        {
            if (cam != null)
            {
                try
                { 
                    stopCapture();

                    cam.EventDeviceRemove -= camDisconnect;
                    cam.EventDeviceUnPlugged -= camDisconnect;
                    cam.EventDeviceReconnect -= camReconnect;
                    cam.EventDevicePluggedIn -= camReconnect;

                    int[] MemIds;
                    camStatus = cam.Memory.GetList(out MemIds);
                    camStatus = cam.Memory.Free(MemIds);

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

        private void camDisconnect(object sender, EventArgs e)
        {
            Close();
        }

        private void camReconnect(object sender, EventArgs e)
        {
            Restart();
        }

        protected override void Generate()
        {
            if (frameAvailable)
            {
                updateFrameRate();

                bool started;
                camStatus = cam.Acquisition.HasStarted(out started);

                bool finished;
                camStatus = cam.Acquisition.IsFinished(out finished);

                if (cam.IsOpened && started && finished)
                {
                    int MemId;
                    camStatus = cam.Memory.GetActive(out MemId);

                    int w, h;
                    camStatus = cam.Memory.GetSize(MemId, out w, out h);


                    //copy to FOutput
                    camStatus = cam.Memory.Lock(MemId);
                    
                    IntPtr memPtr;
                    camStatus = cam.Memory.ToIntPtr(out memPtr);
                    camStatus = cam.Memory.CopyImageMem(memPtr, MemId, FOutput.Data);

                    camStatus = cam.Memory.Unlock(MemId);

                    FOutput.Send();
                }
            }
        }

        public void QueryCameraCapabilities() // only do this on opening
        {

            // binning
            camStatus = cam.Size.Binning.GetSupported(out supportedBinning);

            // subsampling
            camStatus = cam.Size.Subsampling.GetSupported(out supportedSubsampling);

            gainBoostSupported = cam.Gain.Hardware.Boost.Supported;

            gainAutoSupported = cam.AutoFeatures.Sensor.Gain.Supported;

            //cam.Gain.Hardware.GetSupported(out isGainMasterSupported, out isGainRedSupported,
            //                               out isGainGreenSupported, out isGainBlueSupported);


            whitebalanceSupported = cam.AutoFeatures.Sensor.Whitebalance.Supported;

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

        
        #region helpers

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

        private double clampRange (double value, Range<double> range)
        {
            if (value < range.Minimum)
                return range.Minimum;
            if (value > range.Maximum)
                return range.Maximum;
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

        #endregion helpers

        #region setParameters

        // Size
        public void SetBinning(string binningX, string binningY)
        {
            BinningMode modeX = (BinningMode)Enum.Parse(typeof(BinningMode), binningX);
            BinningMode modeY = (BinningMode)Enum.Parse(typeof(BinningMode), binningY);

            if (camOpen) // needed?
            {
                currentBinningX = clampRange(modeX, supportedBinning);
                currentBinningY = clampRange(modeY, supportedBinning);

                camStatus = cam.Size.Binning.Set(currentBinningX | currentBinningY);

                configureOutput();
            }        
        }

        public void SetSubsampling(string subsamplingX, string subsamplingY)
        {
            SubsamplingMode modeX = (SubsamplingMode)Enum.Parse(typeof(SubsamplingMode), subsamplingX);
            SubsamplingMode modeY = (SubsamplingMode)Enum.Parse(typeof(SubsamplingMode), subsamplingY);

            if (camOpen)
            {
                currentSubsamplingX = clampRange(modeX, supportedSubsampling);
                currentSubsamplingY = clampRange(modeY, supportedSubsampling);

                camStatus = cam.Size.Subsampling.Set(currentSubsamplingX | currentSubsamplingY);

                configureOutput();
            }            
        }

        public void mirrorHorizontal(bool Enable)
        {
            camStatus = cam.RopEffect.Set(uEye.Defines.RopEffectMode.LeftRight, Enable);
        }

        public void mirrorVertical(bool Enable)
        {
            camStatus = cam.RopEffect.Set(uEye.Defines.RopEffectMode.UpDown, Enable);
        }

        public void SetAoi(int left, int top, int width, int height)
        {
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
            stopCapture();

            camStatus = cam.PixelFormat.Set(mode);

            startCapture();

            configureOutput();
        }

        public void setAWB(bool value)
        {
            cam.AutoFeatures.Software.WhiteBalance.SetEnable(value);
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
            if (gainAutoSupported)
                cam.AutoFeatures.Software.Gain.SetEnable(enable);
        }

        public void setGainBoost(bool enable)
        {
            if (gainBoostSupported)
                cam.Gain.Hardware.Boost.SetEnable(enable);
        }

        public void setWhitebalance(bool enable)
        {
            if (whitebalanceSupported)
            cam.AutoFeatures.Software.WhiteBalance.SetEnable(enable);
        }


        public void SetFrameRate(double fps)
        {
            currentFramerate = clampRange(fps, framerateRange);
            camStatus = cam.Timing.Framerate.Set(currentFramerate);
        }

        public void setExposure(double exp)
        {
            var expClamped = clampRange(exp, exposureRange);
            cam.Timing.Exposure.Set(exp);
        }

        public void setPixelClock(int pixelClock)
        {
            currentPixelClock = clampRange(pixelClock, pixelClockRange);
            cam.Timing.PixelClock.Set(currentPixelClock);
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

        public void queryColorGain()
        {
            cam.Gain.Hardware.Scaled.GetRed(out currentgainRed);
            cam.Gain.Hardware.Scaled.GetGreen(out currentgainGreen);
            cam.Gain.Hardware.Scaled.GetBlue(out currentgainBlue);
        }

        public void queryTiming()
        {
            cam.Timing.Exposure.GetSupported(out exposureSupported);
            cam.Timing.Exposure.GetRange(out exposureRange);
            cam.Timing.Exposure.Get(out currentExposure);

            cam.Timing.PixelClock.GetRange(out pixelClockRange);
            cam.Timing.PixelClock.GetList(out pixelClockList);
            cam.Timing.PixelClock.Get(out currentPixelClock);

            cam.Timing.Framerate.GetFrameRateRange(out framerateRange);

            cam.Timing.Framerate.GetCurrentFps(out currentFramerate);

        }

        public void updateFrameRate()
        {
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

        [Input("AOI")]
        IDiffSpread<Vector2D> FInAOI;

        [Input("Crop")]
        IDiffSpread<Vector2D> FInCrop;

        [Input("Color Mode")]
		IDiffSpread<uEye.Defines.ColorMode> FColorMode;

        [Input("Gain Boost")]
        IDiffSpread<bool> FInGainBoost;

        [Input("Auto White Balance (Sensor)")]
        IDiffSpread<bool> FInWhitebalance;

        [Input("Gain Master", DefaultValue = 0.1, MinValue = 0, MaxValue = 1)]
        IDiffSpread<double> FInGainMaster;

        [Input("Gain Red", DefaultValue = 0.1, MinValue = 0, MaxValue = 1)]
        IDiffSpread<Vector3D> FInGainColor;

        [Input("Auto White Balance (Gain)")]
        IDiffSpread<bool> FInAWB;

        

        [Input("FPS", DefaultValue = 30, MinValue = 0, MaxValue = 1024)]
        IDiffSpread<int> FInFps;

        [Input("Exposure", DefaultValue = 0.1, MinValue = 0)]
        IDiffSpread<double> FInExposure;

        [Input("Pixelclock", DefaultValue = 20, MinValue = 0)]
        IDiffSpread<int> FInPixelClock;

        // ------------------------------------------------------------

        [Output("supported  Binning X")]
        ISpread<ISpread<string>> FOutBinningXModes;

        [Output("current  Binning X")]
        ISpread<string> FOutBinningX;

        [Output("supported  Binning Y")]
        ISpread<ISpread<string>> FOutBinningYModes;

        [Output("current  Binning Y")]
        ISpread<string> FOutBinningY;

        [Output("supported  Subsampling X")]
        ISpread<ISpread<string>> FOutSubsamplingXModes;

        [Output("current  Subsampling X")]
        ISpread<string> FOutSubsamplingX;

        [Output("supported  Subsampling Y")]
        ISpread<ISpread<string>> FOutSubsamplingYModes;

        [Output("current  Subsampling Y")]
        ISpread<string> FOutSubsamplingY;

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

        [Output("Gain Boost Supported")]
        ISpread<bool> FOutgainBoostSupported;

        [Output("current Color Gain")]
        ISpread<Vector3D> FOutcurrentColorGain;

        [Output("Whitebalance Supported")]
        ISpread<bool> FOutWhitebalance;

        [Output("supported Antiflicker Mode", Visibility = PinVisibility.Hidden)]
        ISpread<uEye.Defines.Whitebalance.AntiFlickerMode> FOutsupportetAntiflicker;

        [Output("Framerate Range")]
        ISpread<Vector2D> FOutRangeFramerate;

        [Output("Framerate")]
        ISpread<double> FOutCurrentFramerate;

        [Output("Exposure Supported")]
        ISpread<bool> FOutSupportedExposure;

        [Output("currennt PixelClock")]
        ISpread<int> FOutCurrentPixelClock;

        [Output("PixelClock Range", Visibility = PinVisibility.Hidden)]
        ISpread<Vector2D> FOutRangePixelClock;

        [Output("currennt Exposure")]
        ISpread<double> FOutCurrentExposure;

        [Output("Exposure Range", Visibility = PinVisibility.Hidden)]
        ISpread<Vector2D> FOutRangeExposure;

        [Import()]
        public ILogger FLogger;

        bool firstframe = true;

        bool queryRequest = false;
        #endregion fields and pins

        override protected void Update(int InstanceCount, bool SpreadCountChanged)
		{
            #region set slicecounts
            // framerate
            FOutRangeFramerate.SliceCount = InstanceCount;
            FOutCurrentFramerate.SliceCount = InstanceCount;

            // binning / subsampling
            FOutBinningX.SliceCount = InstanceCount;
            FOutBinningY.SliceCount = InstanceCount;
            FOutBinningXModes.SliceCount = InstanceCount;
            FOutBinningYModes.SliceCount = InstanceCount;

            FOutSubsamplingX.SliceCount = InstanceCount;
            FOutSubsamplingY.SliceCount = InstanceCount;
            FOutSubsamplingXModes.SliceCount = InstanceCount;
            FOutSubsamplingYModes.SliceCount = InstanceCount;

            // AOI
            FOutAOIWidthRange.SliceCount = InstanceCount;
            FOutAOIWidthRange.SliceCount = InstanceCount;
            FOutCropXRange.SliceCount = InstanceCount;
            FOutCropYRange.SliceCount = InstanceCount;

            // gain
            FOutcurrentColorGain.SliceCount = InstanceCount;
            FOutgainAutoSupported.SliceCount = InstanceCount;
            FOutgainBoostSupported.SliceCount = InstanceCount;

            FOutWhitebalance.SliceCount = InstanceCount;


            //Timing
            FOutRangePixelClock.SliceCount = InstanceCount;
            FOutCurrentPixelClock.SliceCount = InstanceCount;

            FOutCurrentExposure.SliceCount = InstanceCount;
            FOutSupportedExposure.SliceCount = InstanceCount;

            // Antiflicker
            FOutsupportetAntiflicker.SliceCount = InstanceCount;
            #endregion set slicecounts

            for (int i = 0; i < InstanceCount; i++)
            {
                if (FProcessor[i].checkParams && FProcessor[i].camOpen)
                {
                    queryFeatures(i);

                    setBinning(i);
                    setSubsampling(i);
                    setAOI(i);
                    setMirrorX(i);
                    setMirrorY(i);

                    //setColorMode(i);

                    setGainBoost(i);
                    setMasterGain(i);
                    setColorGain(i);

                    setWhitebalance(i);

                    setFramerate(i);
                    setColorMode(i);
                    setPixelclock(i);
                    setExposure(i);

                    queryTiming(i);
                    queryAOIRange(i);

                    FProcessor[i].checkParams = false;
                }
            }

            // query Featuresets
            if ((FPinInEnabled.IsChanged || SpreadCountChanged) && FPinInEnabled[0])
                queryRequest = true;


            if ((FPinInEnabled.IsChanged || SpreadCountChanged) && FPinInEnabled[0] && queryRequest)
            {
                for (int i = 0; i < InstanceCount; i++)
                {
                    if (FProcessor[i].camOpen)
                    {
                        queryFeatures(i);
                        queryTiming(i);
                        queryAOIRange(i);
                        queryRequest = false;  // that's not so cool to be lazy here
                    }
                }
            }

            // set binning & subsampling
            if (FInSubsamplingX.IsChanged || FInSubsamplingY.IsChanged || FInBinningX.IsChanged || FInBinningY.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                {
                    setBinning(i);
                    setSubsampling(i);

                    FOutBinningX[i] = FProcessor[i].currentBinningX.ToString();
                    FOutBinningY[i] = FProcessor[i].currentBinningY.ToString();

                    FOutSubsamplingX[i] = FProcessor[i].currentSubsamplingX.ToString();
                    FOutSubsamplingY[i] = FProcessor[i].currentSubsamplingY.ToString();

                    queryAOIRange(i);
                    queryTiming(i);
                }
            }

            // set AOI
            if (FInAOI.IsChanged || FInCrop.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                {
                    setAOI(i);
                    queryAOIRange(i);
                    queryTiming(i);

                }
            }

            // set FrameRate
            if (SpreadCountChanged || FInFps.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                {
                    setFramerate(i);
                    queryTiming(i);
                }
            }

            // set mirroring
            if (FInMirrorX.IsChanged || FInMirrorY.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                    setMirrorX(i);
            }

            if (FInMirrorY.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                    setMirrorY(i);
            }

            // set camId
            if (SpreadCountChanged || FInCamId.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                {
                    FProcessor[i].CamId = FInCamId[i];

                }
            }
          
            // set Colormode
			if (SpreadCountChanged || FColorMode.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
                {
                    if (FProcessor[i].camOpen)
                    {
                        FProcessor[i].setColorMode(FColorMode[i]);
                    }
                }                    
			}

            // set gain
            if (FInGainColor.IsChanged || FInAWB.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                {
                    setColorGain(i);
                    queryGain(i);
                }
            }


            if (FInGainBoost.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                {
                    setGainBoost(i);
                }
            }


            if (FInGainMaster.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                {
                    setMasterGain(i);
                }
            }
            
            // set Exposure
            if (SpreadCountChanged || FInExposure.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                {
                    FLogger.Log(LogType.Debug, "set Exposure for  camera " + i);
                    if (FProcessor[i].camOpen)
                    {
                        FProcessor[i].setExposure(FInExposure[i]);
                        queryTiming(i);
                    }

                }
            }

            // set PixelClock
            if (SpreadCountChanged || FInPixelClock.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                {
                    if (FProcessor[i].camOpen)
                    {
                        FProcessor[i].setPixelClock(FInPixelClock[i]);
                        queryTiming(i);
                    }
                }
            }

            // query Timing
            if (FInBinningX.IsChanged || FInBinningY.IsChanged || FInSubsamplingX.IsChanged || FInSubsamplingY.IsChanged ||
                FInCrop.IsChanged || FInAOI.IsChanged || FInFps.IsChanged || FInExposure.IsChanged || FInPixelClock.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                {
                    if (FProcessor[i].camOpen)
                    {
                        queryTiming(i);
                    }
                }
            }


            // do everyframe
            if (!FPinInEnabled.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                {
                    if (FProcessor[i].camOpen)
                    {

                        FOutCurrentFramerate[i] = FProcessor[i].currentFramerate;
                    }

                    if (FInAWB[i])
                    {
                        queryGain(i);
                    }
                }
            }




            if (firstframe) firstframe = false;
        }

        

        private void queryGain(int instanceId)
        {
            if (FProcessor[instanceId].camOpen)
            {
                FProcessor[instanceId].queryColorGain();

                FOutcurrentColorGain[instanceId] = new Vector3D(FProcessor[instanceId].currentgainRed / 100.0, FProcessor[instanceId].currentgainGreen / 100.0, FProcessor[instanceId].currentgainBlue / 100.0);
            }
        }


        private void queryFeatures(int instanceId)
        {
            if (FProcessor[instanceId].camOpen)
            {
                #region binning/subsampling

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
                #endregion binning/subsampling

                FOutgainAutoSupported[instanceId] = FProcessor[instanceId].gainAutoSupported;

                FOutgainBoostSupported[instanceId] =  FProcessor[instanceId].gainBoostSupported;

                FOutWhitebalance[instanceId] = FProcessor[instanceId].whitebalanceSupported;

                queryRequest = false;
            }
        }

        private void queryTiming(int instanceId)
        {
            if (FProcessor[instanceId].camOpen)
            {
                FProcessor[instanceId].queryTiming();

                FOutCurrentFramerate[instanceId] = FProcessor[instanceId].currentFramerate;
                FOutRangeFramerate[instanceId] = new Vector2D(FProcessor[instanceId].framerateRange.Minimum, FProcessor[instanceId].framerateRange.Maximum);

                FOutSupportedExposure[instanceId] = FProcessor[instanceId].exposureSupported;

                FOutCurrentPixelClock[instanceId] = FProcessor[instanceId].currentPixelClock;
                FOutRangePixelClock[instanceId] = new Vector2D(FProcessor[instanceId].pixelClockRange.Minimum, FProcessor[instanceId].pixelClockRange.Maximum);

                FOutCurrentExposure[instanceId] = FProcessor[instanceId].currentExposure;
                FOutRangeExposure[instanceId] = new Vector2D(FProcessor[instanceId].exposureRange.Minimum, FProcessor[instanceId].exposureRange.Maximum);

            }
        }

        private void queryAOIRange(int instanceId)
        {
            if (FProcessor[instanceId].camOpen)
            {
                FProcessor[instanceId].queryAOI();
                FOutAOIWidthRange[instanceId] = new Vector2D(FProcessor[instanceId].AOIWidth.Minimum, FProcessor[instanceId].AOIWidth.Maximum);
                FOutAOIHeightRange[instanceId] = new Vector2D(FProcessor[instanceId].AOIHeight.Minimum, FProcessor[instanceId].AOIHeight.Maximum);
                FOutCropXRange[instanceId] = new Vector2D(FProcessor[instanceId].CropXRange.Minimum, FProcessor[instanceId].CropXRange.Maximum);
                FOutCropYRange[instanceId] = new Vector2D(FProcessor[instanceId].CropYRange.Minimum, FProcessor[instanceId].CropYRange.Maximum);
            }
        }


        private void setSubsampling(int instanceId)
        {
            if (FProcessor[instanceId].camOpen)
            {
                string x = FInSubsamplingX[instanceId].ToString();
                string y = FInSubsamplingY[instanceId].ToString();

                FProcessor[instanceId].SetSubsampling(x, y);
            }
        }

        private void setBinning(int instanceId)
        {
            if (FProcessor[instanceId].camOpen)
            {
                string x = FInBinningX[instanceId].ToString();
                string y = FInBinningY[instanceId].ToString();

                FProcessor[instanceId].SetBinning(x, y);
            }
        }

        private void setAOI(int instanceId)
        {
                if (FProcessor[instanceId].camOpen)
                {
                    FProcessor[instanceId].SetAoi((int)FInCrop[instanceId].x, (int)FInCrop[instanceId].y,
                                            (int)FInAOI[instanceId].x, (int)FInAOI[instanceId].y);
                }
        }

        private void setMirrorX(int instanceId)
        {
            if (FProcessor[instanceId].camOpen)
            {
                FProcessor[instanceId].mirrorHorizontal(FInMirrorX[instanceId]);
            }
        }

        private void setMirrorY(int instanceId)
        {
            if (FProcessor[instanceId].camOpen)
            {
                FProcessor[instanceId].mirrorVertical(FInMirrorY[instanceId]);
            }
        }


        private void setMasterGain(int instanceId)
        {
            if (FProcessor[instanceId].camOpen)
            {
                FProcessor[instanceId].setGainMaster((int)(FInGainMaster[instanceId] * 100));
            }
        }

        private void setColorGain(int instanceId)
        {
            if (FProcessor[instanceId].camOpen)
            {
                if (FInAWB[instanceId])
                {
                    FProcessor[instanceId].setAWB(true);
                }
                else
                {
                    FProcessor[instanceId].setAWB(false);

                    FProcessor[instanceId].setGainRed((int)(FInGainColor[instanceId].x * 100));
                    FProcessor[instanceId].setGainGreen((int)(FInGainColor[instanceId].y * 100));
                    FProcessor[instanceId].setGainBlue((int)(FInGainColor[instanceId].z * 100));

                }

                queryGain(instanceId);
            }
        }

        private void setGainBoost(int instanceId)
        {
            FProcessor[instanceId].setGainBoost(FInGainBoost[instanceId]);
        }

        private void setWhitebalance(int instanceId)
        {
            FProcessor[instanceId].setWhitebalance(FInWhitebalance[instanceId]);
        }


        private void setFramerate(int instanceId)
        {
            if (FProcessor[instanceId].camOpen)
            {
                FProcessor[instanceId].SetFrameRate(FInFps[instanceId]);
            }
        }

        private void setPixelclock(int instanceId)
        {
            if (FProcessor[instanceId].camOpen)
            {
                FProcessor[instanceId].setPixelClock(FInPixelClock[instanceId]);
            }
        }

        private void setExposure(int instanceId)
        {
            if (FProcessor[instanceId].camOpen)
            {
                FProcessor[instanceId].setExposure(FInExposure[instanceId]);
            }
        }


        private void setColorMode(int instanceId)
        {
            if (FProcessor[instanceId].camOpen)
            {
                FProcessor[instanceId].setColorMode(FColorMode[instanceId]);
            }
        }
    }
}
