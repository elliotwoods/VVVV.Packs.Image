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
        private Camera cam = null;
        public uEye.Defines.Status camStatus { get; set; }

        public List<int> PossibleBinningX = new List<int>();
        public List<int> PossibleBinningY = new List<int>();

        public List<int> PossibleSubsamplingX = new List<int>();
        public List<int> PossibleSubsamplingY = new List<int>();

        //private Rectangle AOI;

        public VVVV.Utils.VMath.Vector2D framerateRange { get; set; }

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



        public override bool Open()
		{

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

                //initCam();
                camStatus = cam.Init(FCamId);

                

                configureOutput();

                // start capturee
                camStatus = cam.Memory.Allocate();
                camStatus = cam.Acquisition.Capture();

                // query infos
                QueryCameraCapabilities();

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


        private void configureOutput()
        {
            cam.EventFrame -= onFrameEvent;

            // memory reallocation
            Int32[] memList;
            camStatus = cam.Memory.GetList(out memList);
            camStatus = cam.Memory.Free(memList);
            camStatus = cam.Memory.Allocate();

            int height;
            int width;

            int bpp;
            camStatus = cam.PixelFormat.GetBytesPerPixel(out bpp);

            int MemID;
            camStatus = cam.Memory.GetActive(out MemID);

            uEye.Defines.ColorMode pixFormat;
            camStatus = cam.PixelFormat.Get(out pixFormat);

            TColorFormat format = GetColor(pixFormat);

            camStatus = cam.Memory.GetSize(MemID, out width, out height);

            Rectangle a;
            camStatus = cam.Size.AOI.Get(out a);

            //FOutput.Image.Initialise(width, height, format);
            FOutput.Image.Initialise(a.Width, a.Height, format);
            
            cam.EventFrame += onFrameEvent;
        }

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

                int w, h;
                camObject.Memory.GetSize(s32MemId, out w, out h);

                //int s32Factor;
                //camObject.Size.Subsampling.GetFactorVertical(out s32Factor);

                //copy to FOutput
                IntPtr memPtr;
                camObject.Memory.ToIntPtr(out memPtr);
                camObject.Memory.CopyImageMem(memPtr, s32MemId, FOutput.Data);

                FOutput.Send();
            }

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
            byte* data = (byte*)FOutput.Data.ToPointer() + 3;

            int count = FOutput.Image.Width * FOutput.Image.Height;
            for (int i = 0; i < count; i++)
            {
                *data = 255;
                data += 4;
            }
        }


        public void QueryCameraCapabilities()
        {
            // binning
            queryHorizontalBinning();
            queryVerticalBinning();

            // subsampling
            queryHorizontalSubsampling();
            queryVerticalSubsampling();

            // framerate
            queryFramerate();           

            // get camera aoi range size
            //uEye.Types.Range<Int32> rangeWidth, rangeHeight;
            //camStatus = cam.Size.AOI.GetSizeRange(out rangeWidth, out rangeHeight);

            // get actual aoi
            //System.Drawing.Rectangle rect;
            //camStatus = cam.Size.AOI.Get(out rect);

        }


        #region setParameters

        public void SetBinning(string binningX, string binningY)
        {
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
            SubsamplingMode modeX = (SubsamplingMode)Enum.Parse(typeof(SubsamplingMode), subsamplingX);
            SubsamplingMode modeY = (SubsamplingMode)Enum.Parse(typeof(SubsamplingMode), subsamplingY);

            if (camOpen)
                camStatus = cam.Size.Subsampling.Set(modeX | modeY);

            configureOutput();
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


            //// memory reallocation
            //Int32[] memList;
            //camStatus = cam.Memory.GetList(out memList);
            //camStatus = cam.Memory.Free(memList);
            //camStatus = cam.Memory.Allocate();

            configureOutput();

        }

        public void SetFrameRate(double fps)
        {
            camStatus = cam.Timing.Framerate.Set(fps);
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

        public void queryFramerate()
        {
            if (cam != null)
            {
                Range<double> range;
                camStatus = cam.Timing.Framerate.GetFrameRateRange(out range);
                framerateRange = new Vector2D(range.Minimum, range.Maximum);
            }
        }

        #endregion queryParameterSets





        
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
       
        [Input("mirror Horizontal")]
        IDiffSpread<bool> mirrorHorizontal;

        [Input("mirror Vertical")]
        IDiffSpread<bool> mirrorVertical;

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
		IDiffSpread<int> FInFps;

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

            FOutSubsamplingXModes.SliceCount = InstanceCount;
            FOutSubsamplingYModes.SliceCount = InstanceCount;
            FOutBinningXModes.SliceCount = InstanceCount;
            FOutBinningYModes.SliceCount = InstanceCount;

            FOutFormats.SliceCount = InstanceCount;
            FOutFramerateRange.SliceCount = InstanceCount;

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
                queryRequest = true;

            if (queryRequest)
            {
                for (int i = 0; i < InstanceCount; i++)
                {
                    if (FProcessor[i].camOpen)
                    {
                        queryFeatures(InstanceCount, i);
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


            if (SpreadCountChanged || FInCamId.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                    FProcessor[i].CamId = FInCamId[i];
            }
          

			if (SpreadCountChanged || FColorMode.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].ColorMode = FColorMode[i];
			}



            // query FramerateRange
            if (FInBinningX.IsChanged || FInBinningY.IsChanged || FInSubsamplingX.IsChanged || FInSubsamplingY.IsChanged ||
                FInCrop.IsChanged || FInAOI.IsChanged || FInFps.IsChanged)
            {
                for (int i = 0; i < InstanceCount; i++)
                {
                    FProcessor[i].queryFramerate();
                    FOutFramerateRange[i] = FProcessor[i].framerateRange;
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
    }
}
