using Canon.Eos.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.EDSDK.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "Shoot", Category = "EDSDK", Help = "Takes a photo using a Canon camera", Tags = "Canon", Author = "elliotwoods", AutoEvaluate = true)]
	#endregion PluginInfo
	public class ShootNode : IPluginEvaluate
	{
		[Input("Device")]
		IDiffSpread<EosCamera> FInDevices;

		[Input("Save On Camera")]
		IDiffSpread<bool> FInSaveOnCamera;

		[Input("Save On Computer")]
		IDiffSpread<bool> FInSaveOnComputer;

		[Input("Save Location", StringType=StringType.Directory)]
		IDiffSpread<string> FInSaveLocation;

		[Input("Shoot", IsBang=true)]
		ISpread<bool> FInShoot;

		[Output("Status")]
		ISpread<string> FOutStatus;

		bool FFirstRun = true;

		public void Evaluate(int SpreadMax)
		{
			FOutStatus.SliceCount = SpreadMax;

			if (FInDevices.IsChanged || FInSaveOnCamera.IsChanged || FInSaveOnComputer.IsChanged || FInSaveLocation.IsChanged)
			{
				for (int i = 0; i < SpreadMax; i++)
				{
					try
					{
						var camera = FInDevices[i];
						if (camera == null)
							continue;

						if (FInSaveOnCamera[i] && FInSaveOnComputer[i])
							camera.SavePicturesToHostAndCamera(FInSaveLocation[i]);
						else if (FInSaveOnCamera[i])
							camera.SavePicturesToCamera();
						else if (FInSaveOnComputer[i])
							camera.SavePicturesToHost(FInSaveLocation[i]);
						else if (FInSaveOnCamera.IsChanged || FInSaveOnComputer.IsChanged && FFirstRun)
							throw(new Exception("Canon.Eos.Framework doesn't support turning off save pictures to camera or comptuer if you've set this flag previously unless the camera is reset. For the remainder of this session you will need to select either to save to the camera or the computer"));

						FOutStatus[i] = "OK";
					}
					catch (Exception e)
					{
						FOutStatus[i] = e.Message;
					}
				}
			}

			for (int i = 0; i < SpreadMax; i++)
			{
				try
				{
					if (FInShoot[i])
						if (FInDevices[i] != null)
						{
							FInDevices[i].TakePicture();
							FOutStatus[i] = "OK";
						}

				}
				catch (Exception e)
				{
					FOutStatus[i] = e.Message;
				}
			}

			FFirstRun = false;
		}
	}
}
