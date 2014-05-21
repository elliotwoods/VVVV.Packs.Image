using System;
using FeralTic.DX11;
using FeralTic.DX11.Resources;
using VVVV.CV.Core;
using VVVV.DX11;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

namespace VVVV.CV.Nodes
{
	[PluginInfo(Name = "AsTexture", Category = "CV.Image", Version = "DX11.Texture2D", Help = "Converts CVImage to DX11 Texture", Tags = "")]
	public class AsTextureDX11Node : IPluginEvaluate, IDX11ResourceProvider, IDisposable
	{
		[Input("Image")]
		ISpread<CVImageLink> FPinInImage;

		[Output("Texture Out")]
		Pin<DX11Resource<DX11DynamicTexture2D>> FTextureOutput;

		private ProcessDestination<AsTextureDX11Instance> FProcessor;

		public void Evaluate(int SpreadMax)
		{
			if (FProcessor == null)
				FProcessor = new ProcessDestination<AsTextureDX11Instance>(FPinInImage);

			bool needsInit = FProcessor.CheckInputSize();
			
			if (needsInit)
			{
				foreach (var textureOut in FTextureOutput)
				{
					if (textureOut != null)
						textureOut.Dispose();
				}
				FTextureOutput.SliceCount = FProcessor.SliceCount;
				for (int i = 0; i < FProcessor.SliceCount; i++)
				{
					var textureSlice = new DX11Resource<DX11DynamicTexture2D>();
					FProcessor[i].OutputSlice = textureSlice;
					FTextureOutput[i] = textureSlice;
				}
			}
		}

		public void Update(IPluginIO pin, DX11RenderContext context)
		{
			foreach (var processor in FProcessor)
			{
				processor.UpdateTexture(context);
			}
		}

		public void Destroy(IPluginIO pin, DX11RenderContext context, bool force)
		{
			foreach (var processor in FProcessor)
			{
				processor.DestroyTexture(context);
			}
		}

		public void Dispose()
		{
			if (FProcessor != null)
				FProcessor.Dispose();
		}
	}
}
