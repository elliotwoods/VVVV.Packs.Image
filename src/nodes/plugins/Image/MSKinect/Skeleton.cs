#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

using ThreadState = System.Threading.ThreadState;
using System.Collections.Generic;

using Microsoft.Kinect;

#endregion usings

namespace VVVV.Nodes.OpenCV.Kinect
{
	class SkeletonScene
	{
		KinectDevice FDevice = null;
	
		public SkeletonScene(KinectDevice device)
		{
			FDevice = device;
			if (FDevice == null)
				return;

			FDevice.SkeletonEnabled = true;
			FSkeletons = new Skeleton[FDevice.Sensor.SkeletonStream.FrameSkeletonArrayLength];

			FDevice.Sensor.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(Sensor_SkeletonFrameReady);
		}

		void Sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
		{
			using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
			{
				skeletonFrame.CopySkeletonDataTo(this.FSkeletons);
			}
		}

		Skeleton[] FSkeletons;
		public void GetSkeletons(ISpread<Skeleton> spread)
		{
			spread.SliceCount = 0;
			foreach(var s in FSkeletons)
				spread.Add(s);
		}
	}


	#region PluginInfo
	[PluginInfo(Name = "Skeleton", Category = "OpenCV", Version = "Kinect",  Help = "OpenNI context loader", Tags = "", AutoEvaluate = true)]
	#endregion PluginInfo
	public class SkeletonNode : IPluginEvaluate, IDisposable
	{
		#region fields & pins
		[Input("Device")]
		IDiffSpread<KinectDevice> FPinInDevice;

		[Output("Skeleton")]
		ISpread<ISpread<Skeleton>> FPinOutSkeletons;

		[Output("Status")]
		ISpread<String> FStatus;

		[Import]
		ILogger FLogger;

		#endregion fields & pins

		Dictionary<KinectDevice, SkeletonScene> FScenes = new Dictionary<KinectDevice,SkeletonScene>();

		[ImportingConstructor]
		public SkeletonNode(IPluginHost host)
		{

		}

		public void Dispose()
		{

		}

		public void Evaluate(int SpreadMax)
		{
			if (FPinInDevice.IsChanged)
				Attach();

			FPinOutSkeletons.SliceCount = FScenes.Count ;
			int i = 0;
			foreach (var scene in FScenes)
			{
				scene.Value.GetSkeletons(FPinOutSkeletons[i++]);
			}
		}

		void Attach()
		{
			FScenes.Clear();
			foreach (var device in FPinInDevice)
			{
				if (device != null)
					FScenes.Add(device, new SkeletonScene(device));
			}
		}
	}
}
