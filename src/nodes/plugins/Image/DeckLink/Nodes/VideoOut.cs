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

			public Instance(int deviceID, string modeString, uint textureHandle, EnumEntry format, EnumEntry usage)
			{
				IDeckLink device = null;
				WorkerThread.Singleton.PerformBlocking(() => {
					device = DeviceRegister.Singleton.GetDeviceHandle(deviceID);
				});

				try
				{
					IDeckLinkDisplayMode mode = null;
					int width = 0;
					int height = 0;
					WorkerThread.Singleton.PerformBlocking(() =>
					{
						mode = ModeRegister.Singleton.Modes[modeString];
						width = mode.GetWidth();
						height = mode.GetHeight();
					});

					this.Source = new Source(device, mode);
					this.ReadTexture = new ReadTexture(width, height, textureHandle, format, usage);
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
		IDiffSpread<DeviceRegister.DeviceIndex> FInDevice;

		[Input("Mode")]
		IDiffSpread<ModeRegister.ModeIndex> FInMode;

		[Input("Format", EnumName = "TextureFormat")]
		IDiffSpread<EnumEntry> FInFormat;

		[Input("Usage", EnumName = "TextureUsage")]
		IDiffSpread<EnumEntry> FInUsage;

		[Input("Handle")]
		IDiffSpread<uint> FInHandle;

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
			if (FInDevice.IsChanged || FInMode.IsChanged || FInFormat.IsChanged || FInUsage.IsChanged || FInHandle.IsChanged)
			{
				foreach(var slice in FInstances)
					slice.Dispose();

				FInstances.SliceCount = 0;
				FOutStatus.SliceCount = SpreadMax;

				for (int i=0; i<SpreadMax; i++)
				{
					try
					{
						if (FInDevice[i] == null)
							throw (new Exception("No device selected"));
						if (FInMode[i] == null)
							throw (new Exception("No mode selected"));

						FInstances.Add(new Instance(FInDevice[i].Index, FInMode[i].Index, FInHandle[i], FInFormat[i], FInUsage[i]));
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
