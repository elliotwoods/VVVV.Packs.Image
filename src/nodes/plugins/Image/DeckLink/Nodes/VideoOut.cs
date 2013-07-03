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

			public Instance(int deviceID, _BMDDisplayMode mode, _BMDVideoOutputFlags flags, uint textureHandle, Format format, Usage usage)
			{
				IDeckLink device = null;
				WorkerThread.Singleton.PerformBlocking(() => {
					device = DeviceRegister.Singleton.GetDeviceHandle(deviceID);
				});

				try
				{
					this.Source = new Source(device, mode, flags);
					this.ReadTexture = new ReadTexture(this.Source.Width, this.Source.Height, textureHandle, format, usage);
					this.FBuffer = new byte[this.ReadTexture.BufferLength];
					this.Source.NewFrame += Source_NewFrame;
				}
				catch
				{
					if (this.Source != null)
						this.Source.Dispose();
					if (this.ReadTexture != null)
						this.ReadTexture.Dispose();
					if (this.FBuffer != null)
						this.FBuffer = null;
					throw;
				}
			}

			void Source_NewFrame(IntPtr data)
			{
				try
				{
					this.ReadTexture.ReadBack(this.FBuffer);
					Marshal.Copy(this.FBuffer, 0, data, this.FBuffer.Length / 2);
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
		IDiffSpread<int> FInDevice;

		[Input("Video mode")]
		IDiffSpread<_BMDDisplayMode> FInMode;

		[Input("Flags")]
		IDiffSpread<ISpread<_BMDVideoOutputFlags>> FInFlags;

        [Input("Handle")]
        IDiffSpread<uint> FInHandle;

		[Input("Format", EnumName = "TextureFormat")]
		IDiffSpread<EnumEntry> FInFormat;

		[Input("Usage", EnumName = "TextureUsage")]
		IDiffSpread<EnumEntry> FInUsage;

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
			if (FInDevice.IsChanged || FInFlags.IsChanged || FInMode.IsChanged || FInFormat.IsChanged || FInUsage.IsChanged || FInHandle.IsChanged)
			{
				foreach(var slice in FInstances)
					slice.Dispose();

				FInstances.SliceCount = 0;
				FOutStatus.SliceCount = SpreadMax;

				for (int i=0; i<SpreadMax; i++)
				{
					_BMDVideoOutputFlags flags = _BMDVideoOutputFlags.bmdVideoOutputFlagDefault;
					if (FInFlags.SliceCount > 0)
					{
						for (int j=0; j<FInFlags[i].SliceCount; j++)
						{
							if (j==0)
								flags = FInFlags[i][j];
							else
								flags |= FInFlags[i][j];
						}
					}
					try
					{
						Format format;
						if (FInFormat[i].Name == "INTZ")
							format = D3DX.MakeFourCC((byte)'I', (byte)'N', (byte)'T', (byte)'Z');
						else if (FInFormat[i].Name == "RAWZ")
							format = D3DX.MakeFourCC((byte)'R', (byte)'A', (byte)'W', (byte)'Z');
						else if (FInFormat[i].Name == "RESZ")
							format = D3DX.MakeFourCC((byte)'R', (byte)'E', (byte)'S', (byte)'Z');
						else
							format = (Format)Enum.Parse(typeof(Format), FInFormat[i], true);

						var usage = Usage.Dynamic;
						if (FInUsage[i].Index == (int)(TextureType.RenderTarget))
							usage = Usage.RenderTarget;
						else if (FInUsage[i].Index == (int)(TextureType.DepthStencil))
							usage = Usage.DepthStencil;

						FInstances.Add(new Instance(FInDevice[i], FInMode[i], flags, FInHandle[i], format, usage));
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
			GC.SuppressFinalize(this);
		}
	}
}
