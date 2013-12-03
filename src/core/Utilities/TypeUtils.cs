using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV.CvEnum;
using Emgu.CV;
using Emgu.CV.Structure;
using SlimDX.Direct3D9;
using System.Drawing;
using System.Runtime.InteropServices;
using VVVV.PluginInterfaces.V2;

namespace VVVV.CV.Core
{
	public class TypeUtils
	{
		public static TColorFormat ToGrayscale(TColorFormat Format)
		{
			switch (Format)
			{
				case TColorFormat.RGBA32F:
				case TColorFormat.RGB32F:
				case TColorFormat.L32F:
					return TColorFormat.L32F;

				case TColorFormat.L32S:
					return TColorFormat.L32S;

				case TColorFormat.L16:
					return TColorFormat.L16;

				case TColorFormat.RGBA8:
				case TColorFormat.RGB8:
				case TColorFormat.L8:
					return TColorFormat.L8;

			}
			throw (new Exception("ToGrayscale does not support format " + Format.ToString()));
		}
	}
}
