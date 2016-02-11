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
        //[Input("Add")]
        //public RGBAColor ColorAdd
        //{
        //    //we don't expose the raw value, in case
        //    //we need to do some error checking
        //    //OpenCV loves throwing exceptions if its arguments aren't perfect :(
        //    //(in this instance we're not passing arguments to OpenCv)
        //    set
        //    {
        //        FColorAdd[0] = (byte)(value.R * 255.0);
        //        FColorAdd[1] = (byte)(value.G * 255.0);
        //        FColorAdd[2] = (byte)(value.B * 255.0);
        //    }

        //    //if changing these properties means we need to change the output image
        //    //size or colour type, then we need to call
        //    //Allocate();
        //}

        public override void Allocate()
        {
            //This function gets called whenever the output image needs to be initialised
            //Initialising = setting the attributes (i.e. setting the image header and allocating the memory)
            Size FSize = FInput.ImageAttributes.Size;
            //FHalfSize.Width /= 2;
            //FHalfSize.Height /= 2;
            FGrayScale.Initialise(FInput.Image.ImageAttributes.Size, TColorFormat.L8);
            

            FOutput.Image.Initialise(FSize, FInput.ImageAttributes.ColorFormat);
        }

        public override void Process()
        {

            ////var temp;

            //FInput.Image.GetImage(TColorFormat.L8, FGrayScale);

            //var tempimage  = FGrayScale.GetImage() as Image<Gray, byte>;

            ////Gray t = FGrayScaleImage[1, 1];

            //FGrayScaleImage = tempimage.Convert<Gray, int>();
            ////ditherImage(FGrayScaleImage, 1, 1);




            //MCvMat m = new MCvMat(10, 10, DepthType.Cv32F, 1);
            //float[,,] arr = new float[10, 10, 1];
            //m.CopyTo(arr);

            //If we want to pull out an image in a specific format
            //then we must have a local instance of a CVImage initialised to that format
            //and use
            //FInput.Image.GetImage(TColorFormat.L8, FInputL8);
            //in that example, we expect to have a FInputL8 locally which has been intialised
            //with the correct size and colour format


            //Whenever you access the pixels directly of FInput
            //e.g. when using the .CvMat accessor
            //you must lock it for reading using 
            if (!FInput.LockForReading()) //this
                return;

            FInput.Image.GetImage(TColorFormat.L8, FGrayScale);
            FGrayByte = FGrayScale.GetImage() as Image<Gray, byte>;
            FGrayInt = FGrayByte.Convert<Gray, int>();

            if (FInput.ImageAttributes.ColorFormat == TColorFormat.L8)
            {
                try
                {
                    //PixelWiseDitherPtr();
                    PixelWiseDither();
                }
                catch (Exception e)
                {
                    Status = e.ToString();
                    ImageUtils.Log(e);
                }
            }

            FGrayByte.Data[256, 256, 0] = (byte)255;
            FGrayByte.Data[256, 258, 0] = (byte)0;
            FGrayByte.Data[258, 256, 0] = (byte)254;
            FInput.ReleaseForReading(); //and  this after you've finished with FImage

            //ImageUtils.CopyImage(FGrayScaleImage, FOutput, FOutput.Image.ImageAttributes.Size);
            //FOutput.Image.SetImage(FGrayScaleImage);
            //var copyImage = FGrayScaleImage.Convert<Gray, byte>();
            //ImageUtils.CopyImage(FGrayByte.Convert<Gray, byte>() as IImage, FOutput.Image);
            ImageUtils.CopyImage(FGrayByte as IImage, FOutput.Image);
            FOutput.Send();
        }

        private unsafe void PixelWiseDither()
        {
            //here's an example of accessing the pixels one by one
            //note the 'unsafe' in the function header

            //we've also presumed that the image is of the format RGB8 in order for
            //this example to work
            //byte* rgb = (byte*)FOutput.Data.ToPointer();
            int width = FOutput.Image.Width;
            int height = FOutput.Image.Height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; ++x)
                {
                    //ditherImage(FGrayInt, i, j, width, height);
                    ditherImage(FGrayByte, y, x, width, height);
                }
            }

        }

        private unsafe void ditherImage(Image<Gray, byte> FGrayByte, int y, int x, int width, int height)
        {

            int error = 0;

            int value = (int)FGrayByte.Data[y, x, 0];
            //int v2 = gb.Bitmap.GetPixel(row, col).R;
            //int c = (int)gb[ row, col].Intensity;

            if (FGrayByte.Data[y, x, 0] < 128)
            {
                error = FGrayByte.Data[y, x, 0] / 16;
                FGrayByte.Data[y, x, 0] = (byte)0;
            }
            else
            {
                error = FGrayByte.Data[y, x, 0] / 16;
                FGrayByte.Data[y, x, 0] = (byte)255;
            }

            // add error to neighbours
            if (y - 1 >= 0 && x + 1 < width)
            {
                //var before = FGrayByte.Data[row + 1, col - 1, 0];
                FGrayByte.Data[y - 1, x + 1, 0] += (byte)(error * 3);
                //var after = FGrayByte.Data[row + 1, col - 1, 0];
            }
            if (x + 1 < width)
            {
                var before = FGrayByte.Data[y, x + 1, 0];
                FGrayByte.Data[y, x + 1, 0] += (byte)(error * 5);
                var after = FGrayByte.Data[y, x + 1, 0];
            }
            if (y + 1 < height && x + 1 < width)
            {
                FGrayByte.Data[y + 1, x + 1, 0] += (byte)error;
            }
            if (y + 1 < height)
            {
                FGrayByte.Data[y + 1, x, 0] += (byte)(error * 7);
            }

            int valueAfter = (int)FGrayByte.Data[y, x, 0];

            //FGrayByte.Data[y, x, 0] = (byte)( 255 - FGrayByte.Data[y, x, 0]);
        }

        private unsafe void PixelWiseDitherPtr()
        {
            //here's an example of accessing the pixels one by one
            //note the 'unsafe' in the function header

            //we've also presumed that the image is of the format RGB8 in order for
            //this example to work
            byte* rgb = (byte*)FOutput.Data.ToPointer();
            int width = FOutput.Image.Width;
            int height = FOutput.Image.Height;

            //for simplicity, i haven't clamped the colour values here
            for (int i = 0; i < width; ++i)
            {
                for (int j = 0; j < height; j++)
                {
                    //int v = FGrayScaleImage.Data[i, j, 0];
                    //int v2 = FGrayScaleImage.Bitmap.GetPixel(i, j).R;
                    ditherPointer(rgb, i, j, width, height);
                }
            }

        }

        private unsafe void ditherPointer(byte* ptr, int row, int col, int width, int height)
        {
            int divisor = 0;

            int currentpixel = (row * width) + col;

            int value = (int)ptr[currentpixel];
            int v2 = FGrayInt.Bitmap.GetPixel(row, col).R;


            if (ptr[currentpixel] < 128)
            {
                divisor = ptr[currentpixel]  / 16;
                ptr[currentpixel] = 0;
            }
            else
            {
                divisor = ptr[currentpixel]  / 16;
                ptr[currentpixel] = 1;
            }

            if (row + 1 < height && col - 1 >= 0)       ptr[((row + 1 ) * width) + (col - 1)]   += (byte)(divisor * 3);
            if (row + 1 < height)                       ptr[((row + 1)  * width) +  col     ]   += (byte)(divisor * 5);
            if (row + 1 < height && col + 1 < width)    ptr[((row + 1)  * width) + (col + 1)]   += (byte)divisor;
            if (col + 1 < width)                        ptr[(row        * width) + (col + 1)]   += (byte)(divisor * 7);
        }

        /*
        public void Dither(Image<Gray, byte> FGrayScaleImage, int Zeile, int Spalte, int width)
        {
            int Teiler = 0;
            if (FGrayScaleImage[Zeile, Spalte] < 128)
            {
                Teiler = (int)FGrayScaleImage[Zeile, Spalte].Intensity * 255 / 16;
                //FGrayScaleImage[Zeile, Spalte].Intensity = 0.0;
            }
            else
            {
                //Teiler = (FGrayScaleImage[Zeile, Spalte].Intensity - 255) / 16;
                //FGrayScaleImage[Zeile, Spalte] = 1;
            }
            //FGrayScaleImage[Zeile + 1, Spalte - 1] += (Teiler * 3);
            //FGrayScaleImage[Zeile + 1, Spalte] += (Teiler * 5);
            //FGrayScaleImage[Zeile + 1, Spalte + 1] += Teiler;
            //FGrayScaleImage[Zeile, Spalte + 1] += (Teiler * 7);
        }
        */


        //            Image<Bgr, Byte> original = new Image<Bgr, byte>(1024, 768);
        //            Stopwatch evaluator = newStopwatch();
        //            int repetitions = 20;
        //            Bgr color = newBgr(100, 40, 243);

        //            evaluator.Start();
        //            for (int run = 0; run<repetitions; run++)
        //            {
        //                for (int j = 0; j<original.Cols; j++)
        //                {
        //                    for (int i = 0; i<original.Rows; i++)
        //                    {
        //                        original[i, j] = color;
        //                    }
        //                }
        //            }

        //evaluator.Stop();
        //    Console.WriteLine("Average execution time for {0} iteration \n using column per row access: {1}ms\n", repetitions, evaluator.ElapsedMilliseconds / repetitions);
        //    }
    }
}
