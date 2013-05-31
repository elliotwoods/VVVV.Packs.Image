using System;
using FeralTic.DX11;
using FeralTic.DX11.Resources;
using VVVV.DX11;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.OpenCV
{
	[PluginInfo(Name = "AsTexture", Category = "OpenCV", Version = "DX11", Help = "Converts IPLImage to DX11 Texture", Tags = "")]
	public class AsDX11TextureNode : IPluginEvaluate, IDX11ResourceProvider, IDisposable
	{
		[Input("Image")]
		ISpread<CVImageLink> FPinInImage;

		[Output("Texture Out", IsSingle = true)]
		protected Pin<DX11Resource<DX11DynamicTexture2D>> FTextureOutput;

		private ProcessDestination<AsDX11TextureInstance> FProcessor;
		private bool FNeedsInit;

		private DX11DynamicTexture2D FTexture;

		public void Evaluate(int SpreadMax)
		{
			if (FProcessor == null)
				FProcessor = new ProcessDestination<AsDX11TextureInstance>(FPinInImage);

			if (FTextureOutput[0] == null) FTextureOutput[0] = new DX11Resource<DX11DynamicTexture2D>();

			FNeedsInit = FProcessor.CheckInputSize();
			for (var i = 0; i < FProcessor.SliceCount; i++)
				FNeedsInit |= FProcessor[i].NeedsTexture;
		}

		public void Update(IPluginIO pin, DX11RenderContext context)
		{
			if (FNeedsInit)
			{
				if (FProcessor.SliceCount > 0 && FProcessor.GetProcessor(0) != null)
				{
					FTexture = FProcessor.GetProcessor(0).CreateTexture(context);
				}
					
				
				if (FTextureOutput[0].Contains(context))
					FTextureOutput[0].Dispose(context);
			}

			if (FProcessor.SliceCount == 0)
				return;

			FProcessor.GetProcessor(0).UpdateTexture(FTexture);

			FTextureOutput[0][context] = FTexture;
		}

		public void Destroy(IPluginIO pin, DX11RenderContext context, bool force)
		{
			FTextureOutput[0].Dispose(context);
		}

		public void Dispose()
		{
			if (FProcessor != null)
				FProcessor.Dispose();

			FTextureOutput[0].Dispose();
		}
	}
}
