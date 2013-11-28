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
using System.Collections.Generic;
using System.Diagnostics;

namespace VVVV.Nodes.DeckLink
{
	#region PluginInfo
	[PluginInfo(Name = "VideoIn",
				Category = "DeckLink",
				Version = "EX9.Texture",
				Help = "Capture a video stream to a texture",
				Author = "elliotwoods",
				Credits = "Lumacoustics",
				Tags = "")]
	#endregion PluginInfo
	public class Template : DXTextureOutPluginBase, IPluginEvaluate, IDisposable
	{
		#region fields & pins

		[Input("Device")]
		IDiffSpread<DeviceRegister.DeviceIndex> FPinInDevice;

		[Input("Video mode")]
		IDiffSpread<_BMDDisplayMode> FPinInMode;

		[Input("Flags")]
		IDiffSpread<_BMDVideoInputFlags> FPinInFlags;

		[Input("Flush Streams", IsBang = true)]
		ISpread<bool> FPinInFlush;

		[Input("Wait For Frame", MinValue = 0, MaxValue = 1000 / 15, DimensionNames = new string[] { "ms" }, Visibility=PinVisibility.OnlyInspector)]
		ISpread<int> FPinInWaitForFrame;

		[Output("Frames Available")]
		ISpread<int> FPinOutFramesAvailable;

		[Output("Frame Received", IsBang=true)]
		ISpread<bool> FPinOutFrameReceived;

		[Output("Status")]
		ISpread<string> FStatus;

		List<Capture> FCaptures = new List<Capture>();
		#endregion fields & pins

		// import host and hand it to base constructor
		[ImportingConstructor()]
		public Template(IPluginHost host)
			: base(host)
		{
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			SetSliceCount(SpreadMax);
			FStatus.SliceCount = SpreadMax;
			FPinOutFramesAvailable.SliceCount = SpreadMax;
			FPinOutFrameReceived.SliceCount = SpreadMax;

			while (FCaptures.Count < SpreadMax)
			{
				FCaptures.Add(new Capture());
			}
			while (FCaptures.Count > SpreadMax)
			{
				FCaptures[FCaptures.Count - 1].Dispose();
				FCaptures.RemoveAt(FCaptures.Count - 1);
			}

			if (FPinInMode.IsChanged || FPinInDevice.IsChanged || FPinInFlags.IsChanged)
			{
				for (int i = 0; i < SpreadMax; i++)
					ReOpen(i);
				Reinitialize();
			}

			bool reinitialise = false;
			foreach (var capture in FCaptures)
			{
				reinitialise |= capture.Reinitialise;
			}
			if (reinitialise)
				Reinitialize();

			Update();

			for (int i = 0; i < SpreadMax; i++)
			{
				FPinOutFramesAvailable[i] = FCaptures[i].AvailableFrameCount;
			}
		}

		void ReOpen(int index)
		{
			try
			{
				FCaptures[index].Open(FPinInDevice[index], FPinInMode[index], FPinInFlags[index]);
				FStatus[index] = "OK";
			}

			catch (Exception e)
			{
				FStatus[index] = e.Message;
			}
		}

		protected override Texture CreateTexture(int Slice, Device device)
		{
			FCaptures[Slice].Reinitialised();
			bool isEx = device is DeviceEx;
			var pool = isEx ? Pool.Default : Pool.Managed;
			var usage = isEx ? Usage.Dynamic : Usage.None;
			return new Texture(device, Math.Max(FCaptures[Slice].Width / 2, 1), Math.Max(FCaptures[Slice].Height, 1), 1, usage, Format.A8R8G8B8, pool);
		}

		DateTime FLastUpdate = DateTime.Now;
		protected unsafe override void UpdateTexture(int Slice, Texture texture)
		{
			FPinOutFrameReceived[Slice] = false;

			if (!FCaptures[Slice].Ready)
				return;

			double timeout = FPinInWaitForFrame[Slice];
			if (timeout == 0)
			{
				if (!FCaptures[Slice].FreshData)
				return;
			} else {
				while(!FCaptures[Slice].FreshData) {
					if ((DateTime.Now - FLastUpdate).TotalMilliseconds > timeout) {
						return; //timeout occured
					} else {
						System.Threading.Thread.Sleep(1);
					}
				}
				FLastUpdate = DateTime.Now;
			}

			Surface srf = texture.GetSurfaceLevel(0);
			DataRectangle rect = srf.LockRectangle(LockFlags.Discard);

			try
			{
				FCaptures[Slice].Lock.AcquireReaderLock(500);
				try
				{
					rect.Data.WriteRange(FCaptures[Slice].Data, FCaptures[Slice].BytesPerFrame);
					FCaptures[Slice].Updated();
					FPinOutFrameReceived[Slice] = true;
				}
				catch
				{
					FStatus[Slice] = "Failure to upload texture.";
				}
				finally
				{
					srf.UnlockRectangle();
					FCaptures[Slice].Lock.ReleaseReaderLock();
				}
			}
			catch
			{
				FStatus[Slice] = "Failure to lock data for reading.";
			}
		}

		public void Dispose()
		{
			foreach (var capture in FCaptures)
				capture.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}
