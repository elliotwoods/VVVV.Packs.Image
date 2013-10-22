#region usings
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;

using ThreadState = System.Threading.ThreadState;
using System.Collections.Generic;

using OpenNI;
using System.Runtime.InteropServices;

using VVVV.Nodes.OpenCV;
using System.Diagnostics;

#endregion usings

namespace VVVV.Nodes.OpenCV.OpenNI
{
	enum ImageNodeMode { RGB, IR };

	#region PluginInfo
	[PluginInfo(Name = "Images", Category = "OpenCV", Version = "OpenNI", Help = "OpenNI Image generator", Tags = "")]
	#endregion PluginInfo
	public class ImageNode : IPluginEvaluate, IDisposable
	{
        class ImageInstance : Listener, IDisposable
        {
            public string Status = "";

            public bool EnableWorld = true;
            public bool EnableImage = true;
			const int Width = 640;
			const int Height = 480;

			Object FLock = new Object();

            private Device FState;
            public Device State
            {
                set
                {
                    if (FState == value)
                        return;
                    if (FState != null)
                        FState.UnregisterListener(this);

                    FState = value;
                    FState.RegisterListener(this);
                }
            }
            
            void Listener.ContextInitialise()
            {
                Initialise();
            }

            void Listener.ContextUpdate()
            {
                this.Update();
            }


            ImageGenerator FRGBGenerator;
            IRGenerator FIRGenerator;

            public CVImageOutput Image = new CVImageOutput();
            public CVImageOutput Depth = new CVImageOutput();
            public CVImageOutput World = new CVImageOutput();

			ImageNodeMode FMode = ImageNodeMode.RGB;
            public ImageNodeMode Mode
			{
				get
				{
					return FMode;
				}
				set
				{
					FMode = value;
					if (this.FState != null)
						if (this.FState.Running == true)
							lock (FLock)
							{
								InitialiseImage();
							}
				}
			}

			Point3D[] FProjective = new Point3D[Width * Height];
			Size FSize = new Size(Width, Height);

            private void Initialise()
            {
				lock (FLock)
				{
					try
					{
						string messages = "";

						Depth.Image.Initialise(FSize, TColorFormat.L16);
						World.Image.Initialise(FSize, TColorFormat.RGB32F);
						messages += InitialiseImage();

						for (int x = 0; x < Width; x++)
							for (int y = 0; y < Height; y++)
							{
								FProjective[x + y * Width].X = x;
								FProjective[x + y * Width].Y = y;
							}

						Status = "OK";
					}
					catch (StatusException e)
					{
						Status = e.Message;
					}
				}
            }

			private string InitialiseImage()
			{
				string messages = "";

				MapOutputMode imageMode = new MapOutputMode();
				imageMode.XRes = FSize.Width;
				imageMode.YRes = FSize.Height;
				imageMode.FPS = 30;

				if (Mode == ImageNodeMode.RGB)
				{
					if (FIRGenerator != null)
					{
						FIRGenerator.StopGenerating();
						FIRGenerator.Dispose();
					}
					FRGBGenerator = new ImageGenerator(FState.Context);

					FRGBGenerator.MapOutputMode = imageMode;
					Image.Image.Initialise(FSize, TColorFormat.RGB8);

					if (FState.DepthGenerator.AlternativeViewpointCapability.IsViewpointSupported(FRGBGenerator))
					{
						FState.DepthGenerator.AlternativeViewpointCapability.SetViewpoint(FRGBGenerator);
					}
					else
					{
						messages += "AlternativeViewportCapability not supported\n";
					}
					FRGBGenerator.StartGenerating();
				}
				else
				{
					if (FRGBGenerator != null)
					{
						FRGBGenerator.StopGenerating();
						FRGBGenerator.Dispose();
					}
					FIRGenerator = new IRGenerator(FState.Context);
					FIRGenerator.MapOutputMode = imageMode;
					FIRGenerator.StartGenerating();

					Image.Image.Initialise(FSize, TColorFormat.L16);
				}

				return messages;
			}

            private unsafe void Update()
            {
				lock (FLock)
				{
					if (EnableImage)
					{

						if (Mode == ImageNodeMode.RGB)
						{
							byte* rgbs = (byte*)FRGBGenerator.ImageMapPtr.ToPointer();
							byte* rgbd = (byte*)Image.Image.Data.ToPointer();

							for (int i = 0; i < Width * Height; i++)
							{
								rgbd[2] = rgbs[0];
								rgbd[1] = rgbs[1];
								rgbd[0] = rgbs[2];
								rgbs += 3;
								rgbd += 3;
							}
						}
						else if (Mode == ImageNodeMode.IR)
						{
							Image.Image.SetPixels(FIRGenerator.IRMapPtr);
							ushort* dataFixed = (ushort*)FIRGenerator.IRMapPtr;
						}
						Image.Send();
					}

					Depth.Image.SetPixels(FState.DepthGenerator.DepthMapPtr);
					Depth.Send();

					if (EnableWorld)
					{
						fillWorld();
						World.Send();
					}
				}
            }

            private unsafe void fillWorld()
            {
                float* xyz = (float*)World.Data.ToPointer();
                ushort* d = (ushort*)Depth.Data.ToPointer();

                for (int i = 0; i < Width * Height; ++i)
                    FProjective[i].Z = *d++;

                Point3D[] xyzp = FState.DepthGenerator.ConvertProjectiveToRealWorld(FProjective);

                for (int i = 0; i < Width * Height; ++i, xyz += 3)
                {
                    xyz[0] = xyzp[i].X / 1000.0f;
                    xyz[1] = xyzp[i].Y / 1000.0f;
                    xyz[2] = xyzp[i].Z / 1000.0f;
                }
            }

            public void Dispose()
            {
                if (FState != null)
                    FState.UnregisterListener(this);
            }
        }

		[DllImport("msvcrt.dll", EntryPoint = "memcpy")]
		public unsafe static extern void CopyMemory(IntPtr pDest, IntPtr pSrc, int length);

		#region fields & pins
		[Input("Context")]
		ISpread<Device> FPinInContext;

		[Input("Mode")]
		IDiffSpread<ImageNodeMode> FPinInMode;

        [Input("Enable RGB", IsSingle = true, DefaultValue = 1, Visibility = PinVisibility.OnlyInspector)]
        IDiffSpread<bool> FPinInEnableRGB;

        [Input("Enable World", IsSingle = true, DefaultValue = 1, Visibility = PinVisibility.OnlyInspector)]
        IDiffSpread<bool> FPinInEnableWorld;

		[Output("Image")]
		ISpread<CVImageLink> FPinOutImageImage;

		[Output("Depth")]
		ISpread<CVImageLink> FPinOutImageDepth;

		[Output("World")]
		ISpread<CVImageLink> FPinOutImageWorld;

		[Output("Status")]
		ISpread<String> FPinOutStatus;

		[Import]
		ILogger FLogger;

        Spread<ImageInstance> FInstances = new Spread<ImageInstance>(0);
		#endregion fields & pins

		[ImportingConstructor]
		public ImageNode(IPluginHost host)
		{

		}

		public void Dispose()
		{
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
            SpreadMax = FPinInContext.SliceCount;

            CheckSliceCount(SpreadMax);

			if (FPinInMode.IsChanged)
				for (int i = 0; i < SpreadMax; i++)
					FInstances[i].Mode = FPinInMode[i];

            if (FPinInEnableWorld.IsChanged)
                for (int i = 0; i < SpreadMax; i++)
                    FInstances[i].EnableWorld = FPinInEnableWorld[i];

            if (FPinInEnableRGB.IsChanged)
                for (int i = 0; i < SpreadMax; i++)
                    FInstances[i].EnableImage = FPinInEnableRGB[i];

            for (int i = 0; i < SpreadMax; i++)
                FInstances[i].State = FPinInContext[i];

            for (int i=0; i<SpreadMax; i++)
                FPinOutStatus[i] = FInstances[i].Status;
		}

        void CheckSliceCount(int SpreadMax)
        {
            if (SpreadMax == FInstances.SliceCount)
                return;

            for (int i = FInstances.SliceCount; i < SpreadMax; i++)
                FInstances.Add(new ImageInstance());
            for (int i = SpreadMax; i < FInstances.SliceCount; i++)
            {
                FInstances[i] = null;
                FInstances.RemoveAt(i);
            }

            FPinOutImageDepth.SliceCount = SpreadMax;
            FPinOutImageImage.SliceCount = SpreadMax;
            FPinOutImageWorld.SliceCount = SpreadMax;
            FPinOutStatus.SliceCount = SpreadMax;

            for (int i = 0; i < SpreadMax; i++)
            {
                FPinOutImageDepth[i] = FInstances[i].Depth.Link;
                FPinOutImageImage[i] = FInstances[i].Image.Link;
                FPinOutImageWorld[i] = FInstances[i].World.Link;
            }
        }
	}
}
