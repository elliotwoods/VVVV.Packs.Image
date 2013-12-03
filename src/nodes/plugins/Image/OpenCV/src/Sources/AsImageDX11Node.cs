using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using VVVV.CV.Core;
using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V1;
using System.Drawing;
using System.Runtime.InteropServices;

using FeralTic.DX11.Resources;
using VVVV.DX11;
using SlimDX.DXGI;
using VVVV.Core.Logging;
using SlimDX.Direct3D11;
using SlimDX;

namespace VVVV.CV.Nodes
{	
	[PluginInfo(Name = "AsImage",
				Category = "CV.Image",
				Version = "DX11.Texture2D",
				Help = "Converts DX11.Texture2D to CVImageLink",
				Tags = "")]
	public unsafe class AsImageDX11Node : IPluginEvaluate, IDX11ResourceDataRetriever, IDisposable
	{
		[Input("Input")]
		Pin<DX11Resource<DX11Texture2D>> FInput;

		[Output("Output")]
		ISpread<CVImageLink> FOutput;

		[Import()]
		protected IPluginHost FHost;

		[Import()]
		protected ILogger FLogger;

		Spread<AsImageDX11Instance> FInstances = new Spread<AsImageDX11Instance>();

		public void Evaluate(int SpreadMax)
		{
			try
			{
				if (FInput.PluginIO.IsConnected)
				{
					if (this.RenderRequest != null) { this.RenderRequest(this, this.FHost); }

					if (this.AssignedContext == null) { return; }

					var device = this.AssignedContext.Device;
					var context = this.AssignedContext;

					FInstances.ResizeAndDispose(SpreadMax);
					FOutput.SliceCount = SpreadMax;
					for (int i = 0; i < SpreadMax; i++)
					{
						FInstances[i].Process(this.FInput[i], this.AssignedContext);
						FOutput[i] = FInstances[i].Output;
					}
				}
			}
			catch (Exception e)
			{
				FLogger.Log(e);
			}
		}
					
		public FeralTic.DX11.DX11RenderContext AssignedContext
		{
			get;
			set;
		}

		public event DX11RenderRequestDelegate RenderRequest;

		public void Dispose()
		{
			FInstances.ResizeAndDispose(0);
		}
	}
}