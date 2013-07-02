#region usings
using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;

using SlimDX;
using SlimDX.Direct3D9;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.EX9;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Utils.SlimDX;

#endregion usings

//here you can change the vertex type
using VertexType = VVVV.Utils.SlimDX.TexturedVertex;
using DeckLinkAPI;

namespace VVVV.Nodes.DeckLink
{
    #region PluginInfo
    [PluginInfo(Name = "VideoOut",
                Category = "DeckLink",
                Version = "EX9.Texture",
                Help = "Given a texture handle, will push graphic to DeckLink device", Tags = "")]
    #endregion PluginInfo
    public class VideoOut : IPluginEvaluate, IDisposable
    {
		class Instance : IDisposable
		{
			public Source Source;
			public ReadTexture ReadTexture;
			byte[] FBuffer;

			public Instance(IDeckLink device, _BMDDisplayMode mode, _BMDVideoOutputFlags flags, uint textureHandle)
			{
				this.Source = new Source(device, mode, flags);
				this.ReadTexture = new ReadTexture(this.Source.Width, this.Source.Height, textureHandle);
				this.FBuffer = new byte[this.ReadTexture.BufferLength];

				this.Source.NewFrame += Source_NewFrame;
			}

			void Source_NewFrame(IntPtr data)
			{
				try
				{
					this.ReadTexture.ReadBack(this.FBuffer);
					Marshal.Copy(this.FBuffer, 0, data, this.FBuffer.Length);
				}
				catch(Exception e)
				{
				}
			}

			public void Dispose()
			{
				this.Source.Dispose();
				this.ReadTexture.Dispose();
				this.FBuffer = null;
			}
		}

        #region fields & pins
#pragma warning disable 0649
		[Input("Device")]
		IDiffSpread<IDeckLink> FInDevice;

		[Input("Video mode")]
		IDiffSpread<_BMDDisplayMode> FInMode;

		[Input("Flags")]
		IDiffSpread<ISpread<_BMDVideoOutputFlags>> FInFlags;

        [Input("Usage", EnumName = "TextureUsage")]
        IDiffSpread<EnumEntry> FInUsage;

        [Input("Handle")]
        IDiffSpread<uint> FInHandle;

		[Output("Width", DefaultValue = 64)]
		ISpread<int> FOutWidth;

		[Output("Height", DefaultValue = 64)]
		ISpread<int> FOutHeight;

		//[Output("Format", EnumName = "TextureFormat")]
		//ISpread<EnumEntry> FOutFormat;

		[Output("Status")]
		ISpread<string> FOutStatus;

        [Import()]
        ILogger FLogger;
#pragma warning restore

        //track the current texture slice
		Spread<Instance> FInstances = new Spread<Instance>();
        #endregion fields & pins

        [ImportingConstructor()]
		public VideoOut(IPluginHost host)
        {
        }

        public void Evaluate(int SpreadMax)
        {
			if (FInDevice.IsChanged || FInFlags.IsChanged || FInMode.IsChanged)
			{
				foreach(var slice in FInstances)
					slice.Dispose();

				FInstances.SliceCount = 0;
				FOutStatus.SliceCount = SpreadMax;

				for (int i=0; i<SpreadMax; i++)
				{
					_BMDVideoOutputFlags flags = _BMDVideoOutputFlags.bmdVideoOutputFlagDefault;
					for (int j=0; j<FInFlags[i].SliceCount; j++)
					{
						if (j==0)
							flags = FInFlags[i][j];
						else
							flags |= FInFlags[i][j];
					}
					try
					{
						FInstances.Add(new Instance(FInDevice[i], FInMode[i], flags, FInHandle[i]));
						FOutStatus[i] = "OK";
					}
					catch(Exception e)
					{
						FOutStatus[i] = e.Message;
					}

				}
			}
        }

		public void Dispose()
		{
			foreach (var slice in FInstances)
				slice.Dispose();
		}
	}
}
