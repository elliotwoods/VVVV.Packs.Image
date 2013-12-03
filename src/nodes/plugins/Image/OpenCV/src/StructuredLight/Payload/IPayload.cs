using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using VVVV.CV.Core;

namespace VVVV.CV.Nodes.StructuredLight
{
	public abstract class IPayload
	{
		public IPayload(int Width, int Height, bool Balanced)
		{
			this.Balanced = Balanced;

			if (Width < 1)
				Width = 1;
			if (Height < 1)
				Height = 1;

			this.Width = (uint)Width;
			this.Height = (uint)Height;

			this.Data = new ulong[this.PixelCount];
			this.DataInverse = new ulong[GetMaxIndex()];
			MaxIndexCached = (ulong) GetMaxIndex();
			Render();
		}

		protected abstract int GetMaxIndex();
		public ulong MaxIndexCached { get; private set;}
		
		public uint Width { get; private set; }
		public uint Height { get; private set; }
		/// <summary>
		/// The 2D size of the map being encoded (i.e. projector image dimensions)
		/// </summary>
		public Size Size
		{
			get
			{
				return new Size((int)Width, (int)Height);
			}
		}
		public int PixelCount
		{
			get
			{
				return (int)Width * (int)Height;
			}
		}
		public uint FrameCount
		{
			get
			{
				return (FrameCountWidth + FrameCountHeight) * (Balanced ? (uint)2 : (uint)1);
			}
		}
		public uint FrameCountWidth
		{
			get
			{
				return (uint)Math.Ceiling(Math.Log((double)Width, 2.0d));
			}
		}
		public uint FrameCountHeight 
		{
			get
			{
				return (uint)Math.Ceiling(Math.Log((double)Height, 2.0d));
			}
		}

		public CVImageAttributes FrameAttributes
		{
			get
			{
				return new CVImageAttributes(this.Size, TColorFormat.L8);
			}
		}

		public bool Balanced = true;

		public ulong[] Data { get; protected set; }
		public ulong[] DataInverse { get; protected set; }

		public abstract void Render();
	}
}
