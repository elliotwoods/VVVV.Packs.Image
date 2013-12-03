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
	[FilterInstance("Template", Help = "Use the source of this filter as a reference on how to write your own filters")]
	public class TemplateInstance : IFilterInstance
	{
		//example of a property
		private byte [] FColorAdd = new byte[3];
		
		[Input("Add")]
		public RGBAColor ColorAdd
		{
			//we don't expose the raw value, in case
			//we need to do some error checking
			//OpenCV loves throwing exceptions if its arguments aren't perfect :(
			//(in this instance we're not passing arguments to OpenCv)
			set
			{
				FColorAdd[0] = (byte)(value.R * 255.0);
				FColorAdd[1] = (byte)(value.G * 255.0);
				FColorAdd[2] = (byte)(value.B * 255.0);
			}

			//if changing these properties means we need to change the output image
			//size or colour type, then we need to call
			//Allocate();
		}

		public override void Allocate()
		{
			//This function gets called whenever the output image needs to be initialised
			//Initialising = setting the attributes (i.e. setting the image header and allocating the memory)
			Size FHalfSize = FInput.ImageAttributes.Size;
			FHalfSize.Width /=2;
			FHalfSize.Height /=2;

			FOutput.Image.Initialise(FHalfSize, FInput.ImageAttributes.ColorFormat);
		}

		public override void Process()
		{
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
			CvInvoke.cvPyrDown(FInput.CvMat, FOutput.CvMat, FILTER_TYPE.CV_GAUSSIAN_5x5);
			FInput.ReleaseForReading(); //and  this after you've finished with FImage

			if (FInput.ImageAttributes.ColorFormat==TColorFormat.RGB8)
				PixelWiseAdd();
			
			FOutput.Send();
		}

		private unsafe void PixelWiseAdd()
		{
			//here's an example of accessing the pixels one by one
			//note the 'unsafe' in the function header

			//we've also presumed that the image is of the format RGB8 in order for
			//this example to work
			byte* rgb = (byte*)FOutput.Data.ToPointer();
			int width = FOutput.Image.Width;
			int height = FOutput.Image.Height;

			//for simplicity, i haven't clamped the colour values here
			for (int i = 0; i < width * height; ++i)
			{
				*rgb++ += FColorAdd[0];
				*rgb++ += FColorAdd[1];
				*rgb++ += FColorAdd[2];
			}
		}
	}
}
