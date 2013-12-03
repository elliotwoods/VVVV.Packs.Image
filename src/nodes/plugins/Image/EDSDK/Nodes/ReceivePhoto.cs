using Canon.Eos.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.CV.Core;
using VVVV.PluginInterfaces.V2;
using VVVV.CV.Core;

namespace VVVV.Nodes.EDSDK
{
	#region PluginInfo
	[PluginInfo(Name = "ReceivePhoto", Category = "EDSDK", Help = "Receives photos as OpenCV image assets", Tags = "Canon", Author = "elliotwoods", AutoEvaluate = true)]
	#endregion PluginInfo
	public class ReceivePhotoNode : IPluginEvaluate, IDisposable
	{
		[Input("Device")]
		IDiffSpread<EosCamera> FInDevices;

		[Output("Output")]
		ISpread<CVImageLink> FOutImage;

		[Output("On Receive", IsBang=true)]
		ISpread<bool> FOutOnReceive;

		public void Evaluate(int SpreadMax)
		{
			if (FInDevices.IsChanged)
			{
				AddListeners();
			}

			FOutOnReceive.SliceCount = SpreadMax;
			for (int i = 0; i < SpreadMax; i++)
			{
				if (FPictureTaken.Contains(i))
					FOutOnReceive[i] = true;
				else
					FOutOnReceive[i] = false;
			}
			FPictureTaken.Clear();
		}

		Dictionary<int, EosCamera> FListeningTo = new Dictionary<int, EosCamera>();
		HashSet<int> FPictureTaken = new HashSet<int>();

		void AddListeners()
		{
			RemoveListeners();

			int count = FInDevices.SliceCount;
			SetupOutput(count);

			for (int i = 0; i < count; i++)
			{
				var camera = FInDevices[i];
				if (camera == null)
					continue;

				FListeningTo.Add(i, camera);
				camera.PictureTaken += camera_PictureTaken;
			}
		}

		void RemoveListeners()
		{
			foreach (var camera in FListeningTo)
			{
				camera.Value.PictureTaken -= camera_PictureTaken;
			}

			FListeningTo.Clear();
		}

		void camera_PictureTaken(object sender, Canon.Eos.Framework.Eventing.EosImageEventArgs e)
		{
			var camera = sender as EosCamera;
			if (camera == null)
				return;

			if (FListeningTo.ContainsValue(camera))
			{
				foreach (var key in FListeningTo.Keys)
				{
					if (FListeningTo[key] == camera)
					{
						FPictureTaken.Add(key);
						var bitmap = e.GetBitmap();
						FOutImage[key].Send(bitmap);
						bitmap.Dispose();
					}
				}
			}
		}

		void SetupOutput(int count)
		{
			FOutImage.SliceCount = count;
			for (int i = 0; i < count; i++)
			{
				FOutImage[i] = new CVImageLink();
			}
		}

		public void Dispose()
		{
			RemoveListeners();
		}
	}
}
