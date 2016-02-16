#region using
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using VVVV.PluginInterfaces.V2;
//using VVVV.Utils.VMath;
using System;
//using VVVV.Utils.VColor;
using VVVV.CV.Core;
#endregion

namespace VVVV.CV.Nodes
{
    [FilterInstance("FloydSteinberg", Help = "Use the source of this filter as a reference on how to write your own filters")]
    public class FloydSteinbergInstance : IFilterInstance
    {

        private readonly CVImage FGrayScale = new CVImage();
        private Image<Gray, int> FGrayInt;
        private Image<Gray, byte> FGrayByte;

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
            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            //Status = "";

            if (!FInput.LockForReading())
                return;

            FInput.GetImage(FGrayScale);
            FInput.ReleaseForReading();

            FGrayByte = FGrayScale.GetImage() as Image<Gray, byte>;
            FGrayInt = FGrayByte.Convert<Gray, int>();

            //Status += "reading: " + sw.ElapsedMilliseconds.ToString();
            //sw.Restart();


            PixelWiseDither();

            //Status += " dithering: " + sw.ElapsedMilliseconds.ToString();
            //sw.Restart();

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
            //Status += " writing: " + sw.ElapsedMilliseconds.ToString();

     

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
                    dither(y, x, width, height);
                    //ditherPointer(rgb, y, x, width, height);
                }
            }

        }

        private unsafe void dither(int y, int x, int width, int height)
        {

            int error = 0;

            // threshold
            if (FGrayInt.Data[y, x, 0] < 128)
            {
                error = FGrayInt.Data[y, x, 0] / 16;
                FGrayInt.Data[y, x, 0] = 0;
            }
            else
            {
                error = (FGrayInt.Data[y, x, 0] -255) / 16;
                FGrayInt.Data[y, x, 0] = 255;
            }

            //add error to neighbours
            if (y - 1 >= 0 && x - 1 >= 0)
                FGrayInt.Data[y - 1, x - 1, 0] += error * 3;
            if (y + 1 < height)
                FGrayInt.Data[y + 1, x, 0] += error * 5;
            if (y + 1 < height && x + 1 < width)
                FGrayInt.Data[y + 1, x + 1, 0] += error;
            if (x + 1 < width)
                FGrayInt.Data[y, x + 1, 0] += error * 7;

        }

        private unsafe void ditherPointer(byte* ptr, int y, int x, int width, int height)
        {
            int error = 0;

            int currentpixel = (y * width) + x;

            int value = (int)ptr[currentpixel];
            int v2 = FGrayInt.Bitmap.GetPixel(y, x).R;


            if (ptr[currentpixel] < 128)
            {
                error = ptr[currentpixel] / 16;
                ptr[currentpixel] = 0;
            }
            else
            {
                error = (ptr[currentpixel] - 255) / 16;
                ptr[currentpixel] = 255;
            }

            //if (row + 1 < height && col - 1 >= 0) ptr[((row + 1) * width) + (col - 1)] += divisor * 3;
            //if (row + 1 < height) ptr[((row + 1) * width) + col] += divisor * 5;
            //if (row + 1 < height && col + 1 < width) ptr[((row + 1) * width) + (col + 1)] +=divisor;
            //if (col + 1 < width) ptr[(row * width) + (col + 1)] += divisor * 7;
        }
    }
}
