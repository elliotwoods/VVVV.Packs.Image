using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.CV.Core;
using FlyCapture2Managed;

namespace VVVV.Nodes.FlyCapture
{
	class Utils
	{
		static public TColorFormat GetFormat(PixelFormat format)
		{
			switch(format)
			{
				case PixelFormat.PixelFormatBgr:
				case PixelFormat.PixelFormatRgb8:
                    return TColorFormat.RGB8;

				case PixelFormat.PixelFormatMono8:
				case PixelFormat.PixelFormatRaw8:
                    return TColorFormat.L8;

				case PixelFormat.PixelFormatMono16:
				case PixelFormat.PixelFormatRaw16:
                    return TColorFormat.L16;
			}
			throw (new Exception("Unsupported PixelFormat"));
		}

		static public float GetFramerate(FrameRate rate)
		{
			switch(rate)
			{
				case FrameRate.FrameRate240:
					return 240f;
				case FrameRate.FrameRate120:
					return 120f;
				case FrameRate.FrameRate60:
					return 60f;
				case FrameRate.FrameRate30:
					return 30f;
				case FrameRate.FrameRate15:
					return 15f;
				case FrameRate.FrameRate7_5:
					return 7.5f;
				case FrameRate.FrameRate3_75:
					return 3.75f;
				case FrameRate.FrameRate1_875:
					return 1.875f;
			}
			return 0;
		}
	}
}
