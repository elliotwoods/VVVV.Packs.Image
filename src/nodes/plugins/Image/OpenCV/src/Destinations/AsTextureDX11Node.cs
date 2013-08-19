using System;
using FeralTic.DX11;
using FeralTic.DX11.Resources;
using VVVV.DX11;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.OpenCV
{
	[PluginInfo(Name = "AsTexture", Category = "OpenCV", Version = "DX11.Texture2D", Help = "Converts CVImage to DX11 Texture", Tags = "")]
	public class AsTextureDX11Node : IPluginEvaluate, IDX11ResourceProvider, IDisposable
	{
		[Input("Image")]
		Pin<CVImageLink> FPinInImage;

		[Output("Texture Out", IsSingle = true)]
		protected Pin<DX11Resource<DX11DynamicTexture2D>> FTextureOutput;

		private ProcessDestination<AsTextureDX11Instance> FProcessor;
		private bool FNeedsInit;

		private DX11DynamicTexture2D FTexture;

		public void Evaluate(int SpreadMax)
		{
			if (FProcessor == null)
				FProcessor = new ProcessDestination<AsTextureDX11Instance>(FPinInImage);

			if (FTextureOutput[0] == null) FTextureOutput[0] = new DX11Resource<DX11DynamicTexture2D>();

			FNeedsInit = FProcessor.CheckInputSize();
			for (var i = 0; i < FProcessor.SliceCount; i++)
				FNeedsInit |= FProcessor[i].NeedsTexture;
		}

		public void Update(IPluginIO pin, DX11RenderContext context)
		{
			if (!FPinInImage.PluginIO.IsConnected || FProcessor.SliceCount == 0)
				return;

            // why deal like this with textures smaller than 640x480 ?
            if (FPinInImage[0].ImageAttributes.Width < 640 && FPinInImage[0].ImageAttributes.Height < 480)
                return;

			if (FNeedsInit)
			{
				if (FProcessor.SliceCount > 0 && FProcessor.GetProcessor(0) != null)
				{
					FTexture = FProcessor.GetProcessor(0).CreateTexture(context);
				}
					
				
				if (FTextureOutput[0].Contains(context))
					FTextureOutput[0].Dispose(context);
			}

			if(FTexture == null) return;

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
