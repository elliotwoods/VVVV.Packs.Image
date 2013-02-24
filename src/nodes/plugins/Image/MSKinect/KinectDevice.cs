using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;

namespace VVVV.Nodes.OpenCV.Kinect
{
	class KinectDevice
	{
		public KinectSensor Sensor { get; private set; }
		public KinectDevice(KinectSensor Sensor)
		{
			this.Sensor = Sensor;
		}

		bool FEnabled = false;
		public bool Enabled
		{
			get
			{
				return FEnabled;
			}

			private set
			{
				if (FEnabled == value)
					return;

				if (FEnabled)
					this.Sensor.Start();
				else
					this.Sensor.Stop();
			}
		}

		bool FSkeletonEnabled = false;
		public bool SkeletonEnabled
		{
			get
			{
				return FSkeletonEnabled;
			}
			set
			{
				if (FSkeletonEnabled == value)
					return;

				this.Enabled = true;

				this.FSkeletonEnabled = value;

				if (FSkeletonEnabled)
					this.Sensor.SkeletonStream.Enable();
				else
					this.Sensor.SkeletonStream.Disable();
			}
		}

	}
}
