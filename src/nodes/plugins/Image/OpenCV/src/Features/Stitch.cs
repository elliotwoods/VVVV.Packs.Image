using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Stitching;

namespace VVVV.CV.Nodes.Features
{
    #region PluginInfo
    [PluginInfo(Name = "Stitch", Category = "CV.Features", Help = "Stitch together a spread of Images")]
    #endregion PluginInfo
    public class Stitch : IPluginEvaluate
    {
        [Input("Input")]
        ISpread<ISpread<CVImageLink>> FInput;

        [Input("Do", IsBang=true)]
        ISpread<bool> FDo;

        [Output("Output")]
        ISpread<CVImageLink> FOutput;

        [Output("Status")]
        ISpread<string> FStatus;

        Stitcher FStitcher = new Stitcher(false);

        public void Evaluate(int SpreadMax)
        {
            for (int i = SpreadMax; i < FOutput.SliceCount; i++)
                FOutput[i].Dispose();
            while (FOutput.SliceCount < SpreadMax)
                FOutput.Add(new CVImageLink());

            FStatus.SliceCount = SpreadMax;

            for (int i = 0; i < SpreadMax; i++)
            {
                if (!FDo[i])
                    continue;

                var inputSpread = FInput[i];
                var output = FOutput[i];

                foreach(var image in inputSpread)
                {
                    image.LockForReading();
                }

                CVImage result = new CVImage();
                try
                {
                    int size = inputSpread.SliceCount;

                    Image<Bgr, Byte>[] images = new Image<Bgr, byte>[size];
                    List<CVImage> ToDispose = new List<CVImage>();

                    for (int j = 0; j < size; j++)
                    {
                        if (inputSpread[j].FrontImage.ImageAttributes.ColourFormat == TColorFormat.RGB8)
                        {
                            images[j] = inputSpread[j].FrontImage.GetImage() as Image<Bgr, Byte>;
                        }
                        else
                        {
                            var image = new CVImage();
                            ToDispose.Add(image);
                            image.Initialise(inputSpread[j].FrontImage.Size, TColorFormat.RGB8);
                            inputSpread[j].FrontImage.GetImage(image);
                            images[j] = image.GetImage() as Image<Bgr, Byte>;
                        }
                    }

                    result.SetImage(FStitcher.Stitch(images));

                    foreach (var image in ToDispose)
                    {
                        image.Dispose();
                    }

                    FStatus[i] = "OK";
                }
                catch (Exception e)
                {
                    FStatus[i] = e.Message;
                }
                finally
                {
                    foreach(var image in inputSpread)
                    {
                        image.ReleaseForReading();
                    }
                }

                output.Send(result);
            }
        }
    }
}
