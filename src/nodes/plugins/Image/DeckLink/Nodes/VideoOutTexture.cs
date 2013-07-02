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
    public enum TextureType
    {
        None,
        RenderTarget,
        DepthStencil,
        Dynamic
    }

    #region PluginInfo
    [PluginInfo(Name = "TextureIn",
                Category = "DeckLink",
                Version = "EX9.Texture",
                Help = "Given a texture handle, will push graphic to DeckLink device", Tags = "")]
    #endregion PluginInfo
    public class EX9_TextureSharedReaderNode : DXTextureOutPluginBase, IPluginEvaluate
    {
        #region fields & pins
#pragma warning disable 0649
        [Input("Width", DefaultValue = 64)]
        IDiffSpread<int> FWidthIn;

        [Input("Height", DefaultValue = 64)]
        IDiffSpread<int> FHeightIn;

        [Input("Format", EnumName = "TextureFormat")]
        IDiffSpread<EnumEntry> FFormat;

        [Input("Usage", EnumName = "TextureUsage")]
        IDiffSpread<EnumEntry> FUsage;

        [Input("Handle")]
        IDiffSpread<uint> FHandleIn;

        [Import()]
        ILogger FLogger;
#pragma warning restore

        //track the current texture slice
        int FCurrentSlice;
        Surface offscreen;

        #endregion fields & pins

        // import host and hand it to base constructor
        [ImportingConstructor()]
        public EX9_TextureSharedReaderNode(IPluginHost host)
            : base(host)
        {
        }

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            SetSliceCount(SpreadMax);

            //recreate texture if any input changed
            if (FUsage.IsChanged || FFormat.IsChanged || FWidthIn.IsChanged || FHeightIn.IsChanged || FHandleIn.IsChanged)
            {
                Reinitialize();
            }
            this.Update();
        }

        //this method gets called, when Reinitialize() was called in evaluate,
        //or a graphics device asks for its data
        protected override Texture CreateTexture(int slice, Device device)
        {
            int p = unchecked((int)FHandleIn[slice]);
            IntPtr share = new IntPtr(p);
            Format format;
            if (FFormat[slice].Name == "INTZ")
                format = D3DX.MakeFourCC((byte)'I', (byte)'N', (byte)'T', (byte)'Z');
            else if (FFormat[slice].Name == "RAWZ")
                format = D3DX.MakeFourCC((byte)'R', (byte)'A', (byte)'W', (byte)'Z');
            else if (FFormat[slice].Name == "RESZ")
                format = D3DX.MakeFourCC((byte)'R', (byte)'E', (byte)'S', (byte)'Z');
            else
                format = (Format)Enum.Parse(typeof(Format), FFormat[slice], true);

            Texture texture = null;
            try
            {
                var usage = Usage.Dynamic;
                if (FUsage[slice].Index == (int)(TextureType.RenderTarget))
                    usage = Usage.RenderTarget;
                else if (FUsage[slice].Index == (int)(TextureType.DepthStencil))
                    usage = Usage.DepthStencil;

                var width = Math.Max(FWidthIn[slice], 1);
                var height = Math.Max(FHeightIn[slice], 1);
                texture = new Texture(device, width, height, 1, usage, format, Pool.Default, ref share);

                if (offscreen != null)
                    offscreen.Dispose();
                offscreen = null;

                var description = texture.GetLevelDescription(0);
                var format2 = description.Format; // sometimes it's changed by this point
                offscreen = Surface.CreateOffscreenPlainEx((DeviceEx)device, width, height, format2, Pool.SystemMemory, Usage.None);
            }
            catch (Exception e)
            {
                FLogger.Log(LogType.Debug, e.Message + " Handle: " + FHandleIn[slice] + " Format: " + format.ToString());
            }
            return texture;
        }

        //this method gets called, when Update() was called in evaluate,
        //or a graphics device asks for its texture, here you fill the texture with the actual data
        //this is called for each renderer, careful here with multiscreen setups, in that case
        //calculate the pixels in evaluate and just copy the data to the device texture here
        unsafe protected override void UpdateTexture(int Slice, Texture texture)
        {
            FCurrentSlice = Slice;

            int width = FWidthIn[0];
            int height = FHeightIn[0];

            texture.Device.GetRenderTargetData(texture.GetSurfaceLevel(0), offscreen);

            var rect = offscreen.LockRectangle(LockFlags.ReadOnly);
            try
            {
                var data = new byte[width * height * 4];
                rect.Data.Read(data, 0, data.Length);
            }
            catch
            {
            }
            finally
            {
                offscreen.UnlockRectangle();
            }
        }
    }
}
