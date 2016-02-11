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
        private int[,] DitheringArray;


        public override void Allocate()
        {
            
            //This function gets called whenever the output image needs to be initialised
            //Initialising = setting the attributes (i.e. setting the image header and allocating the memory)
            Size FSize = FInput.ImageAttributes.Size;
            FGrayScale.Initialise(FInput.Image.ImageAttributes.Size, TColorFormat.L8);
            

            FOutput.Image.Initialise(FSize, FInput.ImageAttributes.ColorFormat);

            DitheringArray = new int[FOutput.Image.Width, FOutput.Image.Height];
        }

        public override void Process()
        {

            if (!FInput.LockForReading())
                return;

            FInput.Image.GetImage(TColorFormat.L8, FGrayScale);
            FInput.ReleaseForReading(); //and  this after you've finished with FImage

            FGrayByte = FGrayScale.GetImage() as Image<Gray, byte>;
            FGrayInt = FGrayByte.Convert<Gray, int>();

            if (FInput.ImageAttributes.ColorFormat == TColorFormat.L8)
            {
                try
                {
                    PixelWiseDither();
                }
                catch (Exception e)
                {
                    Status = e.ToString();
                    ImageUtils.Log(e);
                }
            }

            //FGrayInt.Data[256, 256, 0] = 255;
            //FGrayInt.Data[256, 258, 0] = 0;
            //FGrayInt.Data[258, 256, 0] = 1024;

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
