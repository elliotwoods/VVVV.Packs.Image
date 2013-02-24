#region usings

using System.ComponentModel.Composition;
using System.Drawing;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.Collections.Generic;

#endregion usings

namespace VVVV.Nodes.OpenCV.Tracking
{
	public class TrackingObject
	{
		public Vector2D Position;
		public Vector2D Scale;
	}

	public class TrackingInstance : IDestinationInstance
	{
		private readonly Vector2D FMinimumSourceXY = new Vector2D(0, 0);
		private readonly Vector2D FMinimumDestXY = new Vector2D(-0.5, 0.5);
		private readonly Vector2D FMaximumDestXY = new Vector2D(0.5, -0.5);

		private HaarCascade FHaarCascade;
		private readonly CVImage FGrayScale = new CVImage();

		private readonly List<TrackingObject> FTrackingObjects = new List<TrackingObject>();

		#region tracking params
		private double FScaleFactor = 1.8;
		public double ScaleFactor { set { FScaleFactor = value; } }

		private int FMinNeighbors = 1;
		public int MinNeighbors { set { FMinNeighbors = value; } }

		private int FMinWidth = 64;
		public int MinWidth { set { FMinWidth = value; }}

		private int FMinHeight = 48;
		public int MinHeight { set { FMinHeight = value; } }
		#endregion

		public List<TrackingObject> TrackingObjects
		{
			get { return FTrackingObjects; }
		}

		public void LoadHaarCascade(string path)
		{
			FHaarCascade = new HaarCascade(path);
		}

		public override void Allocate()
		{
			FGrayScale.Initialise(FInput.Image.ImageAttributes.Size, TColorFormat.L8);
		}

		public override void Process()
		{
			//TODO: Status = "Load Haar file"
			if (FHaarCascade == null) return;

			FInput.Image.GetImage(FGrayScale);

			var stride = (FGrayScale.Width*3);
			var align = stride%4;

			if (align != 0)
			{
				stride += 4 - align;
			}

			//Can not work, bcs src and dest are the same.
			CvInvoke.cvEqualizeHist(FGrayScale.CvMat, FGrayScale.CvMat);

			//MCvAvgComp[] objectsDetected = FHaarCascade.Detect(grayImage, 1.8, 1, HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(grayImage.Width / 10, grayImage.Height / 10));
			MCvAvgComp[] objectsDetected = FHaarCascade.Detect(FGrayScale.GetImage() as Image<Gray, byte>, FScaleFactor,
			                                                   FMinNeighbors, HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
			                                                   new Size(FMinWidth, FMinHeight), FGrayScale.Size);

			FTrackingObjects.Clear();

			foreach (MCvAvgComp f in objectsDetected)
			{
				TrackingObject trackingObject = new TrackingObject();

				Vector2D objectCenterPosition = new Vector2D(f.rect.X + f.rect.Width/2, f.rect.Y + f.rect.Height/2);
				Vector2D maximumSourceXY = new Vector2D(FGrayScale.Width, FGrayScale.Height);

				trackingObject.Position = VMath.Map(objectCenterPosition, FMinimumSourceXY, maximumSourceXY, FMinimumDestXY,
				                                    FMaximumDestXY, TMapMode.Float);
				trackingObject.Scale = VMath.Map(new Vector2D(f.rect.Width, f.rect.Height), FMinimumSourceXY.x, maximumSourceXY.x, 0,
				                                 1, TMapMode.Float);

				FTrackingObjects.Add(trackingObject);
			}
		}
	}

	#region PluginInfo

	[PluginInfo(Name = "ObjectTracking", Category = "OpenCV", Help = "Tracks faces and eyes", Author = "alg, sugokuGENKI",
		Tags = "")]

	#endregion PluginInfo

	public class ObjectTrackingNode : IDestinationNode<TrackingInstance>
	{
		#region fields & pins

		[Input("Haar Table", DefaultString = "haarcascade_frontalface_alt2.xml", IsSingle = true, 
			StringType = StringType.Filename)] private IDiffSpread<string> FHaarPath;

		[Input("Scale Factor", DefaultValue = 1.8)] private IDiffSpread<double> FScaleFactor;

		[Input("Min Neighbors", DefaultValue = 1)] private IDiffSpread<int> FMinNeighbors;

		[Input("Min Width", DefaultValue = 64)] private IDiffSpread<int> FMinWidth;

		[Input("Min Height", DefaultValue = 48)] private IDiffSpread<int> FMinHeight;

		[Input("Enabled", DefaultValue = 1)] private ISpread<bool> FEnabled;

		[Output("Position")] private ISpread<ISpread<Vector2D>> FPinOutPositionXY;

		[Output("Scale")] private ISpread<ISpread<Vector2D>> FPinOutScaleXY;

		[Import] private ILogger FLogger;

		#endregion fields & pins

		protected override void Update(int instanceCount, bool SpreadChanged)
		{
			CheckParams(instanceCount);
			Output(instanceCount);
		}

		private void CheckParams(int instanceCount)
		{
			for (int i = 0; i < instanceCount; i++)
			{
				if (FHaarPath.IsChanged) FProcessor[i].LoadHaarCascade(FHaarPath[i]);
				
				if (FMinHeight.IsChanged)
				{
					FProcessor[i].MinHeight = FMinHeight[i];
				}
				if (FMinWidth.IsChanged)
				{
					FProcessor[i].MinWidth = FMinWidth[i];
				}
				
				if (FMinNeighbors.IsChanged) FProcessor[i].MinNeighbors = FMinNeighbors[i];
				if (FScaleFactor.IsChanged) FProcessor[i].ScaleFactor = FScaleFactor[i];
			}
		}

		private void Output(int instanceCount)
		{
			FPinOutPositionXY.SliceCount = instanceCount;
			FPinOutScaleXY.SliceCount = instanceCount;

			for (int i = 0; i < instanceCount; i++)
			{
				int count = FProcessor[i].TrackingObjects.Count;
				FPinOutPositionXY[i].SliceCount = count;
				FPinOutScaleXY[i].SliceCount = count;

				for (int j = 0; j < count; j++)
				{
					try
					{
						FPinOutPositionXY[i][j] = FProcessor[i].TrackingObjects[j].Position;
						FPinOutScaleXY[i][j] = FProcessor[i].TrackingObjects[j].Scale;
					}
					catch
					{
						FLogger.Log(LogType.Error, "Desync in threads");
					}
				}
			}
		}
	}
}