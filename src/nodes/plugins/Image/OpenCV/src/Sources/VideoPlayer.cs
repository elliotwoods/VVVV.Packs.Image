#region usings

using System;
using System.ComponentModel.Composition;
using Emgu.CV.CvEnum;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using VVVV.CV.Core;

#endregion usings

namespace VVVV.CV.Nodes
{
    public class VideoPlayerInstance : IGeneratorInstance
    {
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

        int FrameDecodedIndex = 0;
        int FrameCount = 0;

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
                this.FrameRate = FCapture.GetCaptureProperty(CAP_PROP.CV_CAP_PROP_FPS);
                this.FrameCount = (int) FCapture.GetCaptureProperty(CAP_PROP.CV_CAP_PROP_FRAME_COUNT);
                this.Length = (double)this.FrameCount / this.FrameRate;
                this.Position = 0.0;
                this.FrameDecodedIndex = 0;

                FTimer = new System.Windows.Forms.Timer();
                FTimer.Interval = (int) (1000.0 * 1.0 / this.FrameRate);
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
            int FrameTargetIndex = (int) ((DateTime.Now - FStarted).TotalSeconds * this.FrameRate);
            bool isEnd = (FrameTargetIndex >= this.FrameCount);
            if (isEnd)
            {
                FrameTargetIndex = this.FrameCount - 1;
            }

            bool newFrame = false;
            while (FrameDecodedIndex < FrameTargetIndex)
            {
                FOutput.Image.SetImage(FCapture.QueryFrame());
                FrameDecodedIndex++;
                newFrame = true;
            }

            if (newFrame)
            {
                this.Position = FCapture.GetCaptureProperty(CAP_PROP.CV_CAP_PROP_POS_MSEC) / 1000.0;
                FOutput.Send();
            }

            if (isEnd)
            {
                //rewind
                FCapture.SetCaptureProperty(CAP_PROP.CV_CAP_PROP_POS_FRAMES, 0.0);
                this.FrameDecodedIndex = 0;
                this.FStarted = DateTime.Now;
            }
        }
    }

    #region PluginInfo
    [PluginInfo(Name = "VideoPlayer",
              Category = "CV.Image",
              Help = "Plays a video file to an Image stream",
              Tags = "")]
    #endregion PluginInfo
    public class VideoPlayerNode : IGeneratorNode<VideoPlayerInstance>
    {
        #region fields & pins
        [Input("Filename", StringType=StringType.Filename)]
        IDiffSpread<string> FFilename;

        [Output("Framerate")]
        ISpread<double> FFramerate;

        [Output("Position")]
        ISpread<double> FPosition;

        [Output("Length")]
        ISpread<double> FLength;

        [Import]
        ILogger FLogger;

        #endregion fields & pins

        protected override void Update(int InstanceCount, bool SpreadChanged)
        {
            if (FFilename.IsChanged || SpreadChanged)
                for (int i = 0; i < InstanceCount; i++)
                    FProcessor[i].Filename = FFilename[i];

            if (SpreadChanged)
            {
                FFramerate.SliceCount = InstanceCount;
                FLength.SliceCount = InstanceCount;
                FPosition.SliceCount = InstanceCount;
            }

            for (int i = 0; i < InstanceCount; i++)
            {
                FFramerate[i] = FProcessor[i].FrameRate;
                FPosition[i] = FProcessor[i].Position;
                FLength[i] = FProcessor[i].Length;
            }
        }
    }
}
