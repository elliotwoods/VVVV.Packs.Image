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

        public List<int> PossibleBinningX = new List<int>();
        public List<int> PossibleBinningY = new List<int>();

        public List<int> PossibleSubsamplingX = new List<int>();
        public List<int> PossibleSubsamplingY = new List<int>();

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
            /*
            Note on multi - camera environments
            When using multiple cameras in parallel operation on a single system, you should
            assign a unique camera ID to each camera.To initialize or select a camera with
            Init(), s32Cam must previously have been set to the desired camera ID.
            To initialize or select the next available camera without specifying a camera ID,
            s32Cam has to be preset with 0.
            */

            try
            {
                cam = new Camera();

                camStatus = cam.Init(FCamId);

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

            int MemID;
            camStatus = cam.Memory.GetActive(out MemID);

            int lastMemID;
            camStatus = cam.Memory.GetLast(out lastMemID);

            bool locked;
            cam.Memory.GetLocked(MemID, out locked);

            Status += "\n LOCKED = " + locked + " - " + MemID + " | " + lastMemID;

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


            Status += "\n initialize Outpout ";
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

        public void QueryCameraCapabilities()
        {
            Status += "\n QueryCameraCapabilities()";

            // binning
            queryHorizontalBinning();
            queryVerticalBinning();

            // subsampling
            queryHorizontalSubsampling();
            queryVerticalSubsampling();

            // framerate
            //queryFramerate();           
            queryTiming();

            // get camera aoi range size
            //uEye.Types.Range<Int32> rangeWidth, rangeHeight;
            //camStatus = cam.Size.AOI.GetSizeRange(out rangeWidth, out rangeHeight);

            // get actual aoi
            //System.Drawing.Rectangle rect;
            //camStatus = cam.Size.AOI.Get(out rect);

        }


        #region setParameters

        // Size
        public void SetBinning(string binningX, string binningY)
        {
            Status += "\n SetBinning()";
            BinningMode modeX = (BinningMode)Enum.Parse(typeof(BinningMode), binningX);
            BinningMode modeY = (BinningMode)Enum.Parse(typeof(BinningMode), binningY);

            if (camOpen)
                camStatus = cam.Size.Binning.Set(modeX | modeY);
            else
            {
                camStatus = cam.Size.Binning.Set(modeX | modeY);
            }

            configureOutput();
        }

        public void SetSubsampling(string subsamplingX, string subsamplingY)
        {
            Status += "\n SetSubsampling()";
            SubsamplingMode modeX = (SubsamplingMode)Enum.Parse(typeof(SubsamplingMode), subsamplingX);
            SubsamplingMode modeY = (SubsamplingMode)Enum.Parse(typeof(SubsamplingMode), subsamplingY);

            if (camOpen)
                camStatus = cam.Size.Subsampling.Set(modeX | modeY);

            configureOutput();
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

            camStatus = cam.Size.AOI.Set(left, top, width, height);

            configureOutput();
        }     

        // format
        public void setColorMode(ColorMode mode)
        {
            Status += "\n setColorMode()";
            stopCapture();

            camStatus = cam.PixelFormat.Set(mode);

            startCapture();

            configureOutput();
        }
 
        // Colors
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

        // Timing
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

        private void queryHorizontalBinning()
        {
            Status += "\n queryHorizontalBinning()";
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
            Status += "\n queryVerticalBinning()";
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
            Status += "\n queryHorizontalSubsampling()";
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
            Status += "\n queryVerticalSubsampling()";
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

        public void queryAOI()
        {
            camStatus = cam.Size.AOI.GetSizeRange(out AOIWidth, out AOIHeight);

            camStatus = cam.Size.AOI.GetPosRange(out CropXRange, out CropYRange);
        }

        //public void queryFramerate()
        //{
            
        //    if (cam != null /*&& camOpen*/)
        //    {
        //        Status += "\n queryFramerate()";
        //        Range<double> range;
        //        camStatus = cam.Timing.Framerate.GetFrameRateRange(out range);
        //        framerateRange = new Vector2D(range.Minimum, range.Maximum);

        //        Status += camStatus;
        //    }
        //}

        public void queryGain()
        {
            //camStatus = cam.Gain.Hardware.Scaled.GetMaster(out gainMaster);
            //camStatus = cam.Gain.Hardware.Scaled.GetRed(out gainRed);
            //camStatus = cam.Gain.Hardware.Scaled.GetGreen(out gainGreen);
            //camStatus = cam.Gain.Hardware.Scaled.GetBlue(out gainBlue);

            gainBoostSupported = cam.Gain.Hardware.Boost.Supported;

            gainAutoSupported = cam.AutoFeatures.Sensor.Gain.Supported;

        }

        private void querySoftwareAutofeatures()
        {
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

        public void gquerySensorAutofeatures()
        {
            //cam.AutoFeatures.Sensor.Whitebalance.Supported;
            //cam.AutoFeatures.Sensor.Framerate.Supported;

            //cam.AutoFeatures.Sensor.AntiFlicker.GetSupported(out supportetAntiflicker)
        
            //cam.AutoFeatures.Sensor.BacklightCompensation;
            //cam.AutoFeatures.Sensor.Contrast;
            //cam.AutoFeatures.Sensor.GainShutter;
            //cam.AutoFeatures.Sensor.Shutter;
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
                    setSubsampling(i);
            }

            // set binning
            if (FInBinningX.IsChanged || FInBinningY.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                    setBinning(i);
            }

            // set AOI
            if (FInAOI.IsChanged || FInCrop.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                    setAOI(i);
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

        //private void queryFramerateRange(int instanceId)
        //{
        //    FProcessor[instanceId].queryFramerate();
        //    FOutFramerateRange[instanceId] = FProcessor[instanceId].framerateRange;
        //}

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
                FProcessor[instanceId].QueryCameraCapabilities();

                FOutSubsamplingXModes[instanceId].SliceCount = FProcessor[instanceId].PossibleSubsamplingX.Count;
                FOutSubsamplingYModes[instanceId].SliceCount = FProcessor[instanceId].PossibleSubsamplingY.Count;
                FOutBinningXModes[instanceId].SliceCount = FProcessor[instanceId].PossibleSubsamplingY.Count;
                FOutBinningYModes[instanceId].SliceCount = FProcessor[instanceId].PossibleSubsamplingY.Count;


                for (int m = 0; m < FProcessor[instanceId].PossibleSubsamplingX.Count; m++)
                    FOutSubsamplingXModes[instanceId][m] = Enum.GetName(typeof(SubsamplingXMode), FProcessor[instanceId].PossibleSubsamplingX[m]);

                for (int m = 0; m < FProcessor[instanceId].PossibleSubsamplingY.Count; m++)
                    FOutSubsamplingYModes[instanceId][m] = Enum.GetName(typeof(SubsamplingYMode), FProcessor[instanceId].PossibleSubsamplingY[m]);

                for (int m = 0; m < FProcessor[instanceId].PossibleBinningX.Count; m++)
                    FOutBinningXModes[instanceId][m] = Enum.GetName(typeof(BinningXMode), m);

                for (int m = 0; m < FProcessor[instanceId].PossibleBinningY.Count; m++)
                    FOutBinningYModes[instanceId][m] = Enum.GetName(typeof(BinningYMode), m);

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
