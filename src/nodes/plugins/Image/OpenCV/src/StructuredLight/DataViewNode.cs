#region using
using System.ComponentModel.Composition;
using System.Drawing;
using System;
using System.Threading;
using System.Threading.Tasks;

using Emgu.CV;
using Emgu.CV.Structure;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using VVVV.CV.Core;
#endregion

namespace VVVV.CV.Nodes.StructuredLight
{
	public enum TDataSet { ProjectorInCamera, CameraInProjector, LuminanceInCamera }

	public class SpaceInstance : IStaticGeneratorInstance
	{
		ScanSet FScanSet = null;
		public ScanSet ScanSet
		{
			set
			{
				if (value != null)
				{
					FScanSet = value;
					ReAllocate();
					AddListeners();
				}
				else
				{
					if (FScanSet != null)
					{
						RemoveListeners();
						FScanSet = null;
					}
				}
			}
		}

		TDataSet FDataSetType = TDataSet.ProjectorInCamera;
		public TDataSet DataSetType
		{
			set
			{
				FDataSetType = value;
				ReAllocate();
			}
		}

		float FThreshold = 0.0f;
		public float Threshold
		{
			set
			{
				FThreshold = value;
				ReAllocate();
			}
		}

		public override void Allocate()
		{
			if (Allocated)
				lock (this)
				{
					switch (FDataSetType)
					{
						case TDataSet.ProjectorInCamera:
							FOutput.Image.Initialise(FScanSet.CameraSize, TColorFormat.RGBA32F);
							break;
						case TDataSet.CameraInProjector:
							FOutput.Image.Initialise(FScanSet.ProjectorSize, TColorFormat.RGBA32F);
							break;
						case TDataSet.LuminanceInCamera:
							FOutput.Image.Initialise(FScanSet.CameraSize, TColorFormat.L8);
							break;
					}
				}
		}

		public override bool NeedsThread()
		{
			return false;
		}

		bool Allocated
		{
			get
			{
				lock (this)
				{
					if (FScanSet == null)
						return false;
					else
						return FScanSet.Allocated;
				}
			}
		}

		public unsafe void UpdateData()
		{
			if (Allocated)
			{
				lock (this)
				{
					switch (FDataSetType)
					{
						case TDataSet.ProjectorInCamera:
							UpdateProjectorInCamera();
							break;
						case TDataSet.CameraInProjector:
							UpdateCameraInProjector();
							break;
						case TDataSet.LuminanceInCamera:
							UpdateLuminanceInCamera();
							break;
					}
				}

				FOutput.Send();
			}
		}
	
		unsafe void UpdateProjectorInCamera()
		{
			int PixelCount = FScanSet.CameraPixelCount;
			ulong width = (ulong) FScanSet.ProjectorSize.Width;
			ulong height = (ulong) FScanSet.ProjectorSize.Height;
			float floatWidth = (float)width;
			float floatHeight = (float)height;
			
			float threshold = FThreshold * 255.0f;
			float* p = (float*)FOutput.Data.ToPointer();

			fixed (ulong* projInCamFixed = &FScanSet.ProjectorInCamera[0])
			{
				ulong* projInCam = projInCamFixed;
				fixed (float* distanceFixed = &FScanSet.Distance[0])
				{
					float* distance = distanceFixed;
					for (int i = 0; i < PixelCount; i++)
					{
						*p++ = (float)(*projInCam % width) / floatWidth;
						*p++ = (float)(*projInCam / width) / floatHeight;
						*p++ = 0.0f;
						*p++ = Math.Abs(*distance++) > threshold ? 1.0f : 0.0f;
						projInCam++;
					}
				}
			}
		}

		[DllImport("msvcrt.dll")]
		private static unsafe extern void memset(void* dest, int c, int count);

		unsafe void UpdateCameraInProjector()
		{
			int PixelCount = FScanSet.CameraPixelCount;
			int width = FScanSet.CameraSize.Width;
			int height = FScanSet.CameraSize.Height;
			float floatWidth = (float)width;
			float floatHeight = (float)height;
			
			float threshold = FThreshold * 255.0f;
			float* pixels = (float*)FOutput.Data.ToPointer();
			

			//clear all
			memset((void*)pixels, 0, (int)FOutput.Image.ImageAttributes.BytesPerFrame);

			fixed (ulong* camInProjFixed = &FScanSet.CameraInProjector[0])
			{
				fixed (ulong* projInCamFixed = &FScanSet.ProjectorInCamera[0])
				{
					ulong* projInCamPtr = projInCamFixed;
					fixed (float* distanceFixed = &FScanSet.Distance[0])
					{
						long start = DateTime.Now.Ticks;
						
						float* distancePtr = distanceFixed;
						Parallel.For(0, PixelCount, i =>
						{
							if (distancePtr[i] > threshold)
							{
								float* pixel;
								ulong projInCam = projInCamPtr[i]; /// An index of a camera pixel, index is a projector pixel's index

								//index of this projector pixel
								pixel = pixels + (int)(projInCam) * 4;

								*pixel++ = (float)(i % width) / floatWidth;
								*pixel++ = (float)(i / width) / floatHeight;
								*pixel++ = 0.0f;
								*pixel++ = 1.0f;
							}
						});

						long parallel = DateTime.Now.Ticks - start;

						for (int i = 0; i < PixelCount; i++)
						{
							if (distancePtr[i] > threshold)
							{
								float* pixel;
								ulong projInCam = projInCamPtr[i]; /// An index of a camera pixel, index is a projector pixel's index

								//index of this projector pixel
								pixel = pixels + (int)(projInCam) * 4;

								*pixel++ = (float)(i % width) / floatWidth;
								*pixel++ = (float)(i / width) / floatHeight;
								*pixel++ = 0.0f;
								*pixel++ = 1.0f;
							}
						}

						long sequential = DateTime.Now.Ticks - (start + parallel);
						Debug.Print("parallel = " + parallel.ToString() + ", sequential = " + sequential.ToString());
					}
				}
			}
		}

		unsafe void UpdateLuminanceInCamera()
		{
			Marshal.Copy(FScanSet.Luminance, 0, FOutput.Data, FScanSet.CameraPixelCount);
		}

		void AddListeners()
		{
			RemoveListeners();

			lock (this)
			{
				FScanSet.UpdateAttributes += new EventHandler(FScanSet_UpdateAttributes);
				FScanSet.UpdateData += new EventHandler(FScanSet_UpdateData);
			}
		}

		void RemoveListeners()
		{
			lock (this)
			{
				FScanSet.UpdateAttributes -= FScanSet_UpdateAttributes;
				FScanSet.UpdateData -= FScanSet_UpdateData;
			}
		}

		void FScanSet_UpdateData(object sender, EventArgs e)
		{
			UpdateData();
		}

		void FScanSet_UpdateAttributes(object sender, EventArgs e)
		{
			ReAllocate();
			UpdateData();
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "Space", Category = "CV.StructuredLight", Help = "Preview structured light data", Author = "elliotwoods", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class SpaceNode : IGeneratorNode<SpaceInstance>
	{
		#region fields & pins
		[Input("Input")]
		IDiffSpread<ScanSet> FPinInInput;

		[Input("Threshold", MinValue=0, MaxValue=1)]
		IDiffSpread<float> FPinInThreshold;

		[Input("Dataset")]
		IDiffSpread<TDataSet> FPinInDataSetType;

		[Import()]
		ILogger FLogger;

		CVImage FOutput = new CVImage();
		ScanSet FScanSet;
		bool FFirstRun = true;

		bool FDataUpdated = false;
		bool FAttributesUpdated = false;
		bool FAllocated = false;
		#endregion fields&pins

		[ImportingConstructor()]
		public SpaceNode()
		{

		}

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			bool needsInit = false;
			bool needsCalc = false;

			if (FPinInDataSetType.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].DataSetType = FPinInDataSetType[i];
				needsInit = true;
			}

			if (FPinInInput.IsChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
					FProcessor[i].ScanSet = FPinInInput[i];
				needsInit = true;
			}

			if (FPinInThreshold.IsChanged)
			{
				for (int i=0; i<InstanceCount; i++)
					FProcessor[i].Threshold = FPinInThreshold[i];
				needsCalc = true;
			}

			if (needsInit)
				foreach (var process in FProcessor)
					process.ReAllocate();

			if (needsCalc)
				foreach (var process in FProcessor)
					process.UpdateData();
		}

	}
}
