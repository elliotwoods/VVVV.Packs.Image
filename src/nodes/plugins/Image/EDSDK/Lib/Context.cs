using Canon.Eos.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.EDSDK
{
	public class Context
	{
		static EosFramework FFramework;
		static HashSet<EosCamera> FCameras = new HashSet<EosCamera>();

		public static EosFramework Framework
		{
			get
			{
				return FFramework;
			}
		}

		public static HashSet<EosCamera> Cameras
		{
			get
			{
				return FCameras;
			}
		}

		public static void Start()
		{
			FFramework = new EosFramework();
			Refresh();
			FFramework.CameraAdded +=FFramework_CameraAdded;
		}

		static void FFramework_CameraAdded(object sender, EventArgs e)
		{
			Refresh();
		}

		static private void Refresh()
		{
			foreach (var camera in FFramework.GetCameraCollection())
			{
				if (!FCameras.Contains(camera))
				{
					FCameras.Add(camera);
				}
			}
		}

		public static void Shutdown()
		{
			FFramework.Dispose();
		}
	}
}
