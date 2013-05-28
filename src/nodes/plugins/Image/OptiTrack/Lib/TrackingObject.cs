using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OptiTrackNET;
using VVVV.Utils.VMath;

namespace VVVV.Nodes.OptiTrack
{
	public class TrackingObject
	{
		public TrackingObject(MObject source)
		{
			this.Position = new Vector2D((double)source.X(), (double)source.Y());
			this.Area = source.Area();
			this.Width = source.Width();
			this.Height = source.Height();
			this.Left = source.Left();
			this.Right = source.Right();
			this.Top = source.Top();
			this.Bottom = source.Bottom();
		}

		public Vector2D Position { get; private set; }
		public double Area { get; private set; }
		public double Width { get; private set; }
		public double Height { get; private set; }
		public double Left { get; private set; }
		public double Right { get; private set; }
		public double Top { get; private set; }
		public double Bottom { get; private set; }
	}
}
