using Emgu.CV;
using Emgu.CV.Structure;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System;
using System.IO;
using System.Drawing;
using VVVV.CV.Core;

namespace VVVV.CV.Nodes
{
	public class AsImageRawInstance : IStaticGeneratorInstance
	{
		public override bool NeedsThread()
		{
			return false;
		}

		bool FNeedsReload = false;
		public void Update()
		{
			if (FNeedsReload)
			{
				Reload();
			}
			FNeedsReload = false;
		}

		Stream FInput;
		public Stream Input
		{
			set
			{
				FInput = value;
				FNeedsReload = true;
			}
		}

		int FWidth;
		public int Width
		{
			set
			{
				FWidth = value;
				FNeedsReload = true;
			}
		}

		int FHeight;
		public int Height
		{
			set
			{
				FHeight = value;
				FNeedsReload = true;
			}
		}

		TColorFormat FFormat;
		public TColorFormat Format
		{
			set
			{
				FFormat = value;
				FNeedsReload = true;
			}
		}

		public void Reload()
		{
			try
			{
				if (FWidth < 1)
				{
					throw (new Exception("Width < 1"));
				}
				if (FHeight < 1)
				{
					throw (new Exception("Height < 1"));
				}
				if (FFormat == null)
				{
					throw (new Exception("No valid format selected"));
				}
				if (FInput == null)
				{
					throw (new Exception("No input stream selected"));
				}
				if (FInput.Length == 0)
				{
					throw (new Exception("Input stream empty"));
				}
				byte[] data = new byte[FInput.Length];
				FInput.Read(data, 0, (int) FInput.Length);
				FOutput.Image.Initialise(new Size(FWidth, FHeight), FFormat);
				FOutput.Image.SetPixels(data);
				FOutput.Send();
				Status = "OK";
			}
			catch(Exception e)
			{
				Status = e.Message;
			}
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "AsImage", Category = "CV.Image", Version = "Raw", Help = "Loads Raw stream into image", Tags = "")]
	#endregion PluginInfo
	public class AsImageRawNode : IGeneratorNode<AsImageRawInstance>
	{
		#region fields & pins
		[Input("Input")]
		IDiffSpread<Stream> FInInput;

		[Input("Width", MinValue = 1, DefaultValue=32)]
		IDiffSpread<int> FInWidth;

		[Input("Height", MinValue = 1, DefaultValue = 32)]
		IDiffSpread<int> FInHeight;

		[Input("Format")]
		IDiffSpread<TColorFormat> FInFormat;

		[Import]
		ILogger FLogger;
		#endregion fields&pins

		[ImportingConstructor]
		public AsImageRawNode()
		{

		}

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			if (FInInput.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Input = FInInput[i];
			}

			if (FInWidth.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Width = FInWidth[i];
			}

			if (FInHeight.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Height = FInHeight[i];
			}

			if (FInFormat.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].Format = FInFormat[i];
			}

			foreach(var processor in FProcessor)
			{
				processor.Update(); //check if things need reloading
			}
		}
	}
}
