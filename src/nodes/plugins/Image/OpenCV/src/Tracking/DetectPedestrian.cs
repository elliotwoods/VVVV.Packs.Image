using System.ComponentModel.Composition;
using System.Drawing;
using Emgu.CV.GPU;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Collections.Generic;
using System;
using VVVV.CV.Core;

namespace VVVV.CV.Nodes.Tracking
{
    public class TrackingPedestrian
    {
        public Vector2D Position;
        public Vector2D Scale;
    }

    public class TrackingPedestrianInstance : IDestinationInstance
    {
        private readonly Vector2D FMinimumSourceXY = new Vector2D(0, 0);
        private readonly Vector2D FMinimumDestXY = new Vector2D(-0.5, 0.5);
        private readonly Vector2D FMaximumDestXY = new Vector2D(0.5, -0.5);

        //private HOGDescriptor hog = new HOGDescriptor();
        private HOGDescriptor hog;
        //private GpuHOGDescriptor gpuhog = new GpuHOGDescriptor();
        private GpuHOGDescriptor gpuhog;

        private readonly CVImage FBgrImage = new CVImage();
        private readonly List<TrackingPedestrian> FTrackingPedestrians = new List<TrackingPedestrian>();

        //public ILogger Logger;

        #region tracking params
        public double   hitThreshold   { get; set; }
        public Size     winStride      { get; set; }
        public Size     padding        { get; set; }
        public double   scale          { get; set; }
        public int      finalThreshold { get; set; }
        public bool     useMeanShiftGrouping { get; set; }
        public bool     AllowGpu { get; set; }

        // for HogDescriptor
        public Size winSize { get; set; }
        public Size blockSize { get; set; }
        public Size blockStride { get; set; }
        public Size cellSize { get; set; }
        public int nbins { get; set; }
        public double winSigma { get; set; }
        public double L2HysThreshold { get; set; }
        public bool gammaCorrection { get; set; }
        public int nLevels { get; set; }
        #endregion

    public TrackingPedestrianInstance()
        {
            hitThreshold = 0.0;
            winStride = new Size(4, 4);
            padding = new Size(8, 8);
            scale = 1.05;
            finalThreshold = 2;
            useMeanShiftGrouping = false;
            AllowGpu = true;

            winSize = new Size(64, 128);
            blockSize = new Size(16, 16);
            blockStride = new Size(8, 8);
            cellSize = new Size(8, 8);
            nbins = 9;
            winSigma = -1;
            L2HysThreshold = 0.2;
            gammaCorrection = true;
            nLevels = 64;

            CreateHog();

        }

        public void CreateHog()
        {
            if (GpuInvoke.HasCuda && AllowGpu)
            {
                try
                {
                    //gpuhog = new GpuHOGDescriptor();
                    gpuhog = new GpuHOGDescriptor(this.winSize, this.blockSize, this.blockStride, this.cellSize, this.nbins, this.winSigma, this.L2HysThreshold, this.gammaCorrection, this.nLevels);
                    gpuhog.SetSVMDetector(GpuHOGDescriptor.GetDefaultPeopleDetector());
                    //gpuhog.SetSVMDetector(GpuHOGDescriptor.GetPeopleDetector64x128()); // there are 3 different detectors built-in. maybe others work better?
                }
                catch (Exception e)
                {
                    Status = e.ToString();
                }

            }
            else
            {
                try
                { 
                    hog = new HOGDescriptor(this.winSize, this.blockSize, this.blockStride, this.cellSize, this.nbins, 1, this.winSigma, this.L2HysThreshold, this.gammaCorrection);
                    hog.SetSVMDetector(HOGDescriptor.GetDefaultPeopleDetector());
                }
                catch (Exception e)
                {
                    Status = e.ToString();
                }
            }
        }

        public List<TrackingPedestrian> TrackingPedestrians
        {
            get { return FTrackingPedestrians; }
        }


        public override void Allocate()
        {
            FBgrImage.Initialise(FInput.Image.ImageAttributes.Size, TColorFormat.RGB8);
        }

        public override void Process()
        {
            Rectangle[] rectangles;

            // load image
            FInput.Image.GetImage(TColorFormat.RGB8, FBgrImage);

            var tempImage = FBgrImage.GetImage() as Image<Rgb, byte>;

            var image = FBgrImage.GetImage();

            

            // invoke detection
            if (GpuInvoke.HasCuda && AllowGpu)
            {
                var bgra = tempImage.Convert<Bgra, byte>();
                rectangles = GPUDetectPedestrian(bgra);
            }
            else
            {
                var bgrImage = tempImage.Convert<Bgr, byte>();
                rectangles = DetectPedestrian(bgrImage);
            }


            // parse detected objects
            FTrackingPedestrians.Clear();
            foreach (var rectangle in rectangles)
            {
                var trackingPedestrian = new TrackingPedestrian();

                var center = new Vector2D(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2);
                var maximumSourceXY = new Vector2D(FBgrImage.Width, FBgrImage.Height);

                trackingPedestrian.Position = VMath.Map(center, FMinimumSourceXY, maximumSourceXY, FMinimumDestXY,
                                                    FMaximumDestXY, TMapMode.Float);
                trackingPedestrian.Scale = VMath.Map(new Vector2D(rectangle.Width, rectangle.Height), FMinimumSourceXY.x, maximumSourceXY.x, 0,
                                                 1, TMapMode.Float);

                FTrackingPedestrians.Add(trackingPedestrian);
            }
        }

        private Rectangle[] DetectPedestrian(Image<Bgr, byte> bgrImage)
        {
            if (bgrImage == null)
            {
                Status = "Can't get image or convert it";
                return new Rectangle[0];
            }

            //bgrImage._EqualizeHist(); // does it make it relly better?

            var result =  hog.DetectMultiScale(bgrImage, hitThreshold, winStride, padding, scale, finalThreshold, useMeanShiftGrouping);

            return result;

        }

        
        private Rectangle[] GPUDetectPedestrian(Image<Bgra, byte> bgraImage)
        {
            if (bgraImage == null)
            {
                Status = "Can't get image or convert it";
                return new Rectangle[0];
            }

            try
            {
                using (var gpuImage = new GpuImage<Bgra, byte>(bgraImage))
                {

                    var t = gpuhog.ToString();

                    if (winStride.Width % blockStride.Width != 0 || winStride.Height % blockStride.Height != 0)
                    {
                        winStride = blockStride;
                    }

                    var settings =  gpuhog.DetectMultiScale(gpuImage, hitThreshold, winStride, new Size(0,0), scale, finalThreshold);

                    var simple = gpuhog.DetectMultiScale(gpuImage);


                    return simple;
                }
            }
            catch (Exception e)
            {
                Status = "Exception: " + e;
                return new Rectangle[0];
            }
        }


        //
        // try this generic function could be better 
        //
        // if image is not in BGRA format, convert it...
        // 

        private Rectangle[] GPUDetectPedestrianTest<Tcol, Tdepth>(Image<Tcol, Tdepth> image)
        where Tcol : struct, IColor
        where Tdepth : new()
        {
            //if (image.GetType() == typeof(TColorFormat) )
            //{
            //    // test format or convert
            //}

            var bgraImage = image.Convert<Bgra, byte>();

            try
            {
                using (var gpuImage = new GpuImage<Bgra, byte>(bgraImage))
                {
                    return gpuhog.DetectMultiScale(gpuImage);
                }
            }
            catch (Exception e)
            {
                Status = "Exception: " + e;
                return new Rectangle[0];
            }
        }




    }

    // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------



    [PluginInfo(Name = "DetectPedestrian", Category = "CV.Image", Help = "Tracks pedestrians", Author = "sebl", Credits = "elliotwoods", Tags = "tracking, pedestrian, people")]
    public class PedestrianTrackingNode : IDestinationNode<TrackingPedestrianInstance>
    {
        #region fields & pins
        [Input("Hit Threshold", DefaultValue = 0.0, MinValue = 0)]
        private IDiffSpread<double> FHitThreshold;

        [Input("Win Stride", DefaultValues = new double[] { 4, 4} )]
        private IDiffSpread<Vector2D> FWinStride;

        [Input("Padding", DefaultValues = new double[] { 8, 8 })]
        private IDiffSpread<Vector2D> FPadding;

        [Input("Scale", DefaultValue = 1.05, MinValue = 0)]
        private IDiffSpread<double> FScale;

        [Input("Final Threshold", DefaultValue = 2, MinValue = 0)]
        private IDiffSpread<int>  FFinalThreshold;

        [Input("Use Mean Shift Grouping", DefaultBoolean = false)]
        private IDiffSpread<bool> FUseMeanShiftGrouping;

        [Input("Allow GPU", DefaultBoolean = false)]
        private IDiffSpread<bool> FAllowGpuIn;

        [Input("nBins", DefaultValue = 9)]
        private IDiffSpread<int> FInNbins;

        [Input("winSigma", DefaultValue = -1)]
        private IDiffSpread<int> FInWinSigma;

        [Input("L2HysThreshold", DefaultValue = 0.2)]
        private IDiffSpread<double> FInL2HysThreshold;

        [Input("Gamma Correction", DefaultBoolean = true)]
        private IDiffSpread<bool> FInGammaCorrection;

        [Input("GPU nLevels", DefaultValue = 64, MinValue = 1)]
        private IDiffSpread<int> FInNLevels;

        [Input("Block Stride Multiplicator", MinValue = 1, DefaultValue = 1)]
        private IDiffSpread<int> FBlockStridemulti;

        [Input("Cell Size", DefaultValues = new double[] { 8, 8 })]
        private IDiffSpread<Vector2D> FCellSize;


        [Output("Position")]
        private ISpread<ISpread<Vector2D>> FPositionXYOut;

        [Output("Scale")]
        private ISpread<ISpread<Vector2D>> FScaleXYOut;

        [Output("Status")]
        private ISpread<string> FStatusOut;

        [Import]
        private ILogger FLogger;
        #endregion fields & pins

        protected override void Update(int instanceCount, bool spreadChanged)
        {
            FStatusOut.SliceCount = instanceCount;
            CheckParams(instanceCount);
            Output(instanceCount);
        }

        private void CheckParams(int instanceCount)
        {
            for (var i = 0; i < instanceCount; i++)
            {
                try
                {
                    if (FHitThreshold.IsChanged ||
                        FWinStride.IsChanged ||
                        FPadding.IsChanged ||
                        FScale.IsChanged ||
                        FFinalThreshold.IsChanged ||
                        FUseMeanShiftGrouping.IsChanged ||
                        FAllowGpuIn.IsChanged ||
                        FInNbins.IsChanged ||
                        FInWinSigma.IsChanged ||
                        FInL2HysThreshold.IsChanged ||
                        FInGammaCorrection.IsChanged ||
                        FInNLevels.IsChanged ||
                        FBlockStridemulti.IsChanged ||
                        FCellSize.IsChanged)
                    {
                        FProcessor[i].hitThreshold = FHitThreshold[i];
                        FProcessor[i].winStride = new Size((int)FWinStride[i].x, (int)FWinStride[i].y);
                        FProcessor[i].padding = new Size((int)FPadding[i].x, (int)FPadding[i].y);
                        FProcessor[i].scale = FScale[i];
                        FProcessor[i].finalThreshold = FFinalThreshold[i];
                        FProcessor[i].useMeanShiftGrouping = FUseMeanShiftGrouping[i];
                        FProcessor[i].AllowGpu = FAllowGpuIn[i];

                        FProcessor[i].nbins = FInNbins[i];
                        FProcessor[i].winSigma = FInWinSigma[i];
                        FProcessor[i].L2HysThreshold = FInL2HysThreshold[i];
                        FProcessor[i].gammaCorrection = FInGammaCorrection[i];
                        FProcessor[i].nLevels = FInNLevels[i];

                        //FProcessor[i].blockStride = new Size((int)FBlockStride[i].x, (int)FBlockStride[i].y);
                        FProcessor[i].blockStride = new Size((int)FCellSize[i].x * FBlockStridemulti[i], (int)FCellSize[i].y * FBlockStridemulti[i]);

                        FProcessor[i].cellSize = new Size((int)FCellSize[i].x, (int)FCellSize[i].y);


                        FProcessor[i].CreateHog();

                        FProcessor[i].Process();

                        FLogger.Log(LogType.Message, "Parameters changed");
                    }
                    
                    FStatusOut[i] = "OK";
                }
                catch (Exception e)
                {
                    FStatusOut[i] = e.Message;
                }
            }
        }

        private void Output(int instanceCount)
        {
            FPositionXYOut.SliceCount = instanceCount;
            FScaleXYOut.SliceCount = instanceCount;

            for (int i = 0; i < instanceCount; i++)
            {
                var count = FProcessor[i].TrackingPedestrians.Count;
                FPositionXYOut[i].SliceCount = count;
                FScaleXYOut[i].SliceCount = count;

                for (int j = 0; j < count; j++)
                {
                    try
                    {
                        FPositionXYOut[i][j] = FProcessor[i].TrackingPedestrians[j].Position;
                        FScaleXYOut[i][j] = FProcessor[i].TrackingPedestrians[j].Scale;
                    }
                    catch (Exception e)
                    {
                        FLogger.Log(LogType.Error, "Desync in threads");
                    }
                }
            }
        }
    }
}