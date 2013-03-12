using Emgu.CV;
using Emgu.CV.Structure;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System;

namespace VVVV.Nodes.OpenCV
{
	public class ImageLoadInstance : IStaticGeneratorInstance
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
	[PluginInfo(Name = "ImageLoad", Category = "OpenCV", Help = "Loads RGB texture", Author = "alg", Tags = "")]
	#endregion PluginInfo
	public class ImageLoadNode : IGeneratorNode<ImageLoadInstance>
	{
		#region fields & pins
		[Input("Filename", StringType = StringType.Filename, DefaultString = null)] 
		IDiffSpread<string> FPinInFilename;

		[Input("Reload", IsBang = true)]
		ISpread<bool> FPinInReload;

		[Import]
		ILogger FLogger;
		#endregion fields&pins

		[ImportingConstructor]
		public ImageLoadNode()
		{

		}

		protected override void Update(int instanceCount, bool spreadChanged)
		{
			if (FPinInFilename.IsChanged)
				for (int i = 0; i < instanceCount; i++)
					FProcessor[i].Filename = FPinInFilename[i];

			for (int i = 0; i < instanceCount; i++)
			{
				if (FPinInReload[i])
					FProcessor[i].Reload();
			}
		}
	}
}
