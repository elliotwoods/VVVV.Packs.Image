using OptiTrackNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace VVVV.Nodes.OptiTrack
{
	#region PluginInfo
	[PluginInfo(Name = "Object", Category = "OptiTrack", Version = "Split", Help = "List OptiTrack camera devices", Tags = "")]
	#endregion PluginInfo
	public class ObjectSplitNode : IPluginEvaluate
	{
		[Input("Object")]
		IDiffSpread<TrackingObject> FInObjects;

		[Output("Position")]
		ISpread<Vector2D> FOutPosition;

		[Output("Width")]
		ISpread<double> FOutWidth;

		[Output("Height")]
		ISpread<double> FOutHeight;

		[Output("Area")]
		ISpread<double> FOutArea;

		[Output("Left", Visibility=PinVisibility.OnlyInspector)]
		ISpread<double> FOutLeft;

		[Output("Right", Visibility = PinVisibility.OnlyInspector)]
		ISpread<double> FOutRight;

		[Output("Top", Visibility = PinVisibility.OnlyInspector)]
		ISpread<double> FOutTop;

		[Output("Bottom", Visibility = PinVisibility.OnlyInspector)]
		ISpread<double> FOutBottom;

		Context FContext = new Context();

		public void Evaluate(int SpreadMax)
		{
			if (FInObjects.IsChanged)
			{
				if (FInObjects.SliceCount > 0 && FInObjects[0] != null)
				{
					SetSliceCount(FInObjects.SliceCount);

					int slice = 0;
					foreach (var Object in FInObjects)
					{
						if (Object == null)
							continue;
						FOutPosition[slice] = Object.Position;
						FOutArea[slice] = Object.Area;
						FOutWidth[slice] = Object.Width;
						FOutHeight[slice] = Object.Height;
						FOutTop[slice] = Object.Top;
						FOutBottom[slice] = Object.Bottom;
						FOutLeft[slice] = Object.Left;
						FOutRight[slice] = Object.Right;

						slice++;
					}
				}
				else
				{
					SetSliceCount(0);
				}
			}
		}

		void SetSliceCount(int count)
		{
			FOutPosition.SliceCount = count;
			FOutArea.SliceCount = count;
			FOutWidth.SliceCount = count;
			FOutHeight.SliceCount = count;

			FOutLeft.SliceCount = count;
			FOutRight.SliceCount = count;
			FOutTop.SliceCount = count;
			FOutBottom.SliceCount = count;
		}
	}
}
