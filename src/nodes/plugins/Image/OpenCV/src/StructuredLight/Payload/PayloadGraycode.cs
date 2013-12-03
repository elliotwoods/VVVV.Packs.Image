using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.CV.Nodes.StructuredLight
{
	class PayloadGraycode : IPayload
	{
		public PayloadGraycode(int Width, int Height, bool Balanced) :
			base(Width, Height, Balanced)
		{
		}

		public unsafe override void Render()
		{
			ulong i = 0;
			for (uint y = 0; y < Height; y++)
				for (uint x = 0; x < Width; x++)
				{
					Data[i] = x ^ (x >> 1) +
								((y ^ (y >> 1)) << (int) FrameCountWidth);
					DataInverse[Data[i]] = i;

					i++;
				}
		}

		protected override int GetMaxIndex()
		{
			return 1 << ((int) FrameCountWidth + (int) FrameCountHeight);
		}
	}
}
