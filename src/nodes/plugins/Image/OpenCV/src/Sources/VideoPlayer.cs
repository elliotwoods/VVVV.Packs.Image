using System;
using System.ComponentModel.Composition;
using Emgu.CV.CvEnum;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;
using Emgu.CV;

namespace VVVV.Nodes.OpenCV
{
    public class VideoPlayerInstance : IGeneratorInstance
    {
        public override bool NeedsAllocate()
        {
            return false;
        }

        Capture FCapture;

        string FFilename;
        public string Filename
        {
            set
            {
                FFilename = value;
                Restart();
            }
        }

        int FFrameDecodedIndex;
        int FFrameCount;

        public double Position { get; private set; }
        public double Length { get; private set; }
        public double FrameRate {get; private set;}
        DateTime FStarted = DateTime.Now;

        System.Windows.Forms.Timer FTimer = new System.Windows.Forms.Timer();

        public override bool Open()
        {
            try
            {
                if (!System.IO.File.Exists(FFilename))
                {
                    throw (new Exception("File '" + FFilename + "' does not exist"));
                }

                FCapture = new Capture(FFilename);
                FCapture.GetCaptureProperty(CAP_PROP.CV_CAP_PROP_FRAME_COUNT);
                FrameRate = FCapture.GetCaptureProperty(CAP_PROP.CV_CAP_PROP_FPS);
                FFrameCount = (int) FCapture.GetCaptureProperty(CAP_PROP.CV_CAP_PROP_FRAME_COUNT);
                Length = FFrameCount / FrameRate;
                Position = 0.0;
                FFrameDecodedIndex = 0;

                FTimer = new System.Windows.Forms.Timer {Interval = (int) (1000.0*1.0/FrameRate)};
	            FStarted = DateTime.Now;

                Status = "OK";
                return true;
            }
            catch (Exception e)
            {
                Status = e.Message;
                return false;
            }
        }

        public override void Close()
        {
            if (FCapture == null)
                return;

            try
            {
                FCapture.Dispose();
                Status = "Closed";
            }
            catch (Exception e)
            {
                Status = e.Message;
            }
        }

        public override void Allocate()
        {
        }

        protected override void Generate()
        {
            var frameTargetIndex = (int) ((DateTime.Now - FStarted).TotalSeconds * FrameRate);
            var isEnd = (frameTargetIndex >= FFrameCount);
            if (isEnd)
            {
                frameTargetIndex = FFrameCount - 1;
            }

            var newFrame = false;
            while (FFrameDecodedIndex < frameTargetIndex)
            {
                FOutput.Image.SetImage(FCapture.QueryFrame());
                FFrameDecodedIndex++;
                newFrame = true;
            }

            if (newFrame)
            {
                Position = FCapture.GetCaptureProperty(CAP_PROP.CV_CAP_PROP_POS_MSEC) / 1000.0;
                FOutput.Send();
            }

            if (isEnd)
            {
                //rewind
                FCapture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_POS_FRAMES, 0.0);
                FFrameDecodedIndex = 0;
                FStarted = DateTime.Now;
            }
        }
    }

    [PluginInfo(Name = "VideoPlayer", Category = "OpenCV", Version = "", Help = "Plays a video file to an Image stream", Tags = "")]
    public class VideoPlayerNode : IGeneratorNode<VideoPlayerInstance>
    {
        [Input("Filename", StringType=StringType.Filename)]
        IDiffSpread<string> FFilenameIn;

        [Output("Framerate")]
        ISpread<double> FFramerateIn;

        [Output("Position")]
        ISpread<double> FPositionIn;

        [Output("Length")]
        ISpread<double> FLengthIn;

        protected override void Update(int instanceCount, bool spreadChanged)
        {
            if (FFilenameIn.IsChanged || spreadChanged)
                for (var i = 0; i < instanceCount; i++)
                    FProcessor[i].Filename = FFilenameIn[i];

            if (spreadChanged)
            {
                FFramerateIn.SliceCount = instanceCount;
                FLengthIn.SliceCount = instanceCount;
                FPositionIn.SliceCount = instanceCount;
            }

            for (var i = 0; i < instanceCount; i++)
            {
                FFramerateIn[i] = FProcessor[i].FrameRate;
                FPositionIn[i] = FProcessor[i].Position;
                FLengthIn[i] = FProcessor[i].Length;
            }
        }
    }
}
