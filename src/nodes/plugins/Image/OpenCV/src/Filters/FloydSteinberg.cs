#region using
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using System;
using VVVV.Utils.VColor;
using VVVV.CV.Core;

#endregion

namespace VVVV.CV.Nodes
{
    [FilterInstance("FloydSteinberg", Help = "Use the source of this filter as a reference on how to write your own filters")]
    public class FloydSteinbergInstance : IFilterInstance
    {

        //example of a property
        private byte[] FColorAdd = new byte[3];

        private readonly CVImage FGrayScale = new CVImage();
        private Emgu.CV.Image<Gray, int> FGrayInt;
        private Emgu.CV.Image<Gray, byte> FGrayByte;

        private TColorFormat FOutFormat;

        public override void Allocate()
        {
            FOutFormat = ImageUtils.MakeGrayscale(FInput.ImageAttributes.ColorFormat);

            //if we can't convert or it's already grayscale, just pass through
            if (FOutFormat == TColorFormat.UnInitialised)
                FOutFormat = FInput.ImageAttributes.ColorFormat;

            FOutput.Image.Initialise(FInput.Image.ImageAttributes.Size, FOutFormat);

            FGrayScale.Initialise(FInput.Image.ImageAttributes.Size, FOutFormat);

        }

        public override void Process()
        {

            if (!FInput.LockForReading())
                return;

            FInput.GetImage(FGrayScale);

            FInput.ReleaseForReading();

            FGrayByte = FGrayScale.GetImage() as Image<Gray, byte>;
            FGrayInt = FGrayByte.Convert<Gray, int>();

            PixelWiseDither();
            //try
            //{
            //    PixelWiseDither();
            //}
            //catch (Exception e)
            //{
            //    Status = e.ToString();
            //    ImageUtils.Log(e);
            //}


            ImageUtils.CopyImage(FGrayInt.Convert<Gray, byte>() as IImage, FOutput.Image);
            FOutput.Send();
        }

        private unsafe void PixelWiseDither()
        {
            int width = FOutput.Image.Width;
            int height = FOutput.Image.Height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; ++x)
                {
                    ditherImage(FGrayInt, y, x, width, height);
                }
            }

        }

        private unsafe void ditherImage(Image<Gray, int> image, int y, int x, int width, int height)
        {

            int error = 0;

            //int value = image.Data[y, x, 0];

            if (image.Data[y, x, 0] < 128)
            {
                error = image.Data[y, x, 0] / 16;
                image.Data[y, x, 0] = 0;
            }
            else
            {
                error = (image.Data[y, x, 0] -255) / 16;
                image.Data[y, x, 0] = 255;
            }

            //add error to neighbours
            if (y - 1 >= 0 && x - 1 >= 0)
                image.Data[y - 1, x - 1, 0] += error * 3;
            if (y + 1 < height)
                image.Data[y + 1, x, 0] += error * 5;
            if (y + 1 < height && x + 1 < width)
                image.Data[y + 1, x + 1, 0] += error;
            if (x + 1 < width)
                image.Data[y, x + 1, 0] += error * 7;

        }
    }
}
