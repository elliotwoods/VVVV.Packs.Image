using System.ComponentModel.Composition;
using System.Drawing;
using Emgu.CV.GPU;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Collections.Generic;
using System;
using VVVV.CV.Core;

namespace VVVV.CV.Nodes.Tracking
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

		private CascadeClassifier FCascadeClassifier;
		private GpuCascadeClassifier FGpuCascadeClassifier;

		private readonly CVImage FGrayScale = new CVImage();
		private readonly List<TrackingObject> FTrackingObjects = new List<TrackingObject>();

		#region tracking params
		public double ScaleFactor { get; set; }
		public int MinNeighbors { get; set; }
		public Size MinSize { get; set; }
		public Size MaxSize { get; set; }
		public bool AllowGpu { get; set; }
		#endregion

		public TrackingInstance()
		{
			ScaleFactor = 1.8;
			MinNeighbors = 1;
			MinSize = new Size(20, 20);
			MaxSize = Size.Empty;
			AllowGpu = true;
		}

		public List<TrackingObject> TrackingObjects
		{
			get { return FTrackingObjects; }
		}

		public void LoadHaarCascade(string path)
		{
			FCascadeClassifier = new CascadeClassifier(path);
			FGpuCascadeClassifier = new GpuCascadeClassifier(path);
		}

		public override void Allocate()
		{
			FGrayScale.Initialise(FInput.Image.ImageAttributes.Size, TColorFormat.L8);
		}

		public override void Process()
		{
			Rectangle[] rectangles;
			FInput.Image.GetImage(TColorFormat.L8, FGrayScale);
			var grayImage = FGrayScale.GetImage() as Image<Gray, byte>;
			if (GpuInvoke.HasCuda && AllowGpu)
			{
				rectangles = ProcessOnGpu(grayImage);
			}
			else
			{
				rectangles = ProcessOnCpu(grayImage);
			}

			FTrackingObjects.Clear();
			foreach (var rectangle in rectangles)
			{
				var trackingObject = new TrackingObject();

				var center = new Vector2D(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2);
				var maximumSourceXY = new Vector2D(FGrayScale.Width, FGrayScale.Height);

				trackingObject.Position = VMath.Map(center, FMinimumSourceXY, maximumSourceXY, FMinimumDestXY,
													FMaximumDestXY, TMapMode.Float);
				trackingObject.Scale = VMath.Map(new Vector2D(rectangle.Width, rectangle.Height), FMinimumSourceXY.x, maximumSourceXY.x, 0,
												 1, TMapMode.Float);

				FTrackingObjects.Add(trackingObject);
			}
		}

		private Rectangle[] ProcessOnGpu(Image<Gray, byte> grayImage)
		{
			if (FGpuCascadeClassifier == null)
			{
				Status = "Can't load Haar file";
				return new Rectangle[0];
			}

			using (var gpuImage = new GpuImage<Gray, byte>(grayImage))
			{
				return FGpuCascadeClassifier.DetectMultiScale(gpuImage, ScaleFactor, MinNeighbors, MinSize);
			}
		}

		private Rectangle[] ProcessOnCpu(Image<Gray, byte> grayImage)
		{
			if (FCascadeClassifier == null)
			{
				Status = "Can't load Haar file";
				return new Rectangle[0];
			}

			

			if (grayImage == null)
			{
				Status = "Can't get image or convert it to grayscale";
				return new Rectangle[0];
			}

			grayImage._EqualizeHist();

			return FCascadeClassifier.DetectMultiScale(grayImage, ScaleFactor, MinNeighbors, MinSize, MaxSize);

			
		}
	}

	[PluginInfo(Name = "TrackObject", Category = "CV.Image", Help = "Tracks faces and eyes", Author = "alg", Credits = "elliotwoods", Tags = "face, haar")]
	public class ObjectTrackingNode : IDestinationNode<TrackingInstance>
	{
		#region fields & pins
		[Input("Haar Table", DefaultString = "haarcascade_frontalface_alt2.xml", IsSingle = true, StringType = StringType.Filename)] 
		private IDiffSpread<string> FHaarPathIn;

		[Input("Scale Factor", DefaultValue = 1.8, MinValue = 1)] 
		private ISpread<double> FScaleFactorIn;

		[Input("Min Neighbors", DefaultValue = 1)] 
		private ISpread<int> FMinNeighborsIn;

		[Input("Min Size", DefaultValues = new double[] {20, 20})] 
		private ISpread<Vector2D> FMinSizeIn;

		[Input("Max Size", DefaultValues = new double[] {0, 0}, Visibility = PinVisibility.Hidden)]
		private ISpread<Vector2D> FMaxSizeIn;

		[Input("Allow GPU", DefaultBoolean = true, Visibility = PinVisibility.OnlyInspector)]
		private ISpread<bool> FAllowGpuIn;

		[Output("Position")] 
		private ISpread<ISpread<Vector2D>> FPositionXYOut;

		[Output("Scale")] 
		private ISpread<ISpread<Vector2D>> FScaleXYOut;

		[Output("Status")] 
		private ISpread<string> FStatusOut;

		[Import] 
		private ILogger FLogger;
		#endregion fields & pins

		protected override void Update(int instanceCount, bool spreadChanged)
		{
			FStatusOut.SliceCount = instanceCount;
			CheckParams(instanceCount);
			Output(instanceCount);
		}

		private void CheckParams(int instanceCount)
		{
			for (var i = 0; i < instanceCount; i++)
			{
				try
				{
					if (FHaarPathIn.IsChanged) FProcessor[i].LoadHaarCascade(FHaarPathIn[i]);

					FProcessor[i].MinSize = new Size((int)FMinSizeIn[i].x, (int)FMaxSizeIn[i].y);
					FProcessor[i].MaxSize = new Size((int)FMaxSizeIn[i].x, (int)FMaxSizeIn[i].y);
					FProcessor[i].MinNeighbors = FMinNeighborsIn[i];
					FProcessor[i].ScaleFactor = FScaleFactorIn[i];
					FProcessor[i].AllowGpu = FAllowGpuIn[i];
					
					FStatusOut[i] = "OK";
				}
				catch(Exception e)
				{
					FStatusOut[i] = e.Message;
				}
			}
		}

		private void Output(int instanceCount)
		{
			FPositionXYOut.SliceCount = instanceCount;
			FScaleXYOut.SliceCount = instanceCount;

			for (int i = 0; i < instanceCount; i++)
			{
				var count = FProcessor[i].TrackingObjects.Count;
				FPositionXYOut[i].SliceCount = count;
				FScaleXYOut[i].SliceCount = count;

				for (int j = 0; j < count; j++)
				{
					try
					{
						FPositionXYOut[i][j] = FProcessor[i].TrackingObjects[j].Position;
						FScaleXYOut[i][j] = FProcessor[i].TrackingObjects[j].Scale;
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