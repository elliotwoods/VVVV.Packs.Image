using Emgu.CV;
using Emgu.CV.Structure;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System;
using VVVV.CV.Core;

namespace VVVV.CV.Nodes
{
	public class FileImageInstance : IStaticGeneratorInstance
	{
		string FFilename = "";
		
		public override bool NeedsThread()
		{
			return false;
		}

		public string Filename
		{
			set
			{
				if (FFilename != value)
				{
					FFilename = value;
					LoadImage();
				}
			}
		}

		public void Reload()
		{
			LoadImage();
		}

		private void LoadImage()
		{
			try
			{
				FOutput.Image.LoadFile(FFilename);
				FOutput.Send();
				Status = "OK";
			}
			catch (Exception e)
			{
				Status = e.Message;
			}
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "FileImage", Category = "CV.Image", Help = "Loads image file from disk.", Author = "alg", Tags = "")]
	#endregion PluginInfo
	public class FileImageNode : IGeneratorNode<FileImageInstance>
	{
		#region fields & pins
		[Input("Filename", StringType = StringType.Filename, DefaultString = null)] 
		IDiffSpread<string> FPinInFilename;

		[Input("Reload", IsBang = true)]
		ISpread<bool> FPinInReload;

		[Import]
		ILogger FLogger;
		#endregion fields&pins

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			if (FPinInFilename.IsChanged)
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Filename = FPinInFilename[i];

			for (int i = 0; i < InstanceCount; i++)
			{
				if (FPinInReload[i])
					FProcessor[i].Reload();
			}
		}
	}
}
