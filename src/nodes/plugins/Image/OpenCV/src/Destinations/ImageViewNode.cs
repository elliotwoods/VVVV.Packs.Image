#region usings
using System;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
using Emgu.CV.UI;
using VVVV.CV.Core;
#endregion usings

namespace VVVV.CV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "Inspektor",
	Category = "CV.Image",
	Help = "Render a CVImage using Emgu. Note : likely to be depreciated",
	Tags = "",
	AutoEvaluate = true)]
	#endregion PluginInfo
	public class RendererNode : UserControl, IPluginEvaluate, IDisposable
	{
		#region fields & pins

		[Input("Input")]
		ISpread<CVImageLink> FPinInInput;

		[Input("Slice", IsSingle = true)]
		ISpread<int> FPinInSlice;

		[Import()]
		ILogger FLogger;
		private ImageBox FImageBox;

		//gui controls

		#endregion fields & pins

		#region constructor and init

		public RendererNode()
		{
			//setup the gui
			InitializeComponent();
		}

		void InitializeComponent()
		{
			this.FImageBox = new Emgu.CV.UI.ImageBox();
			((System.ComponentModel.ISupportInitialize)(this.FImageBox)).BeginInit();
			this.SuspendLayout();
			// 
			// FImageBox
			// 
			this.FImageBox.Location = new System.Drawing.Point(0, 0);
			this.FImageBox.Name = "FImageBox";
			this.FImageBox.Size = this.Size;
			this.FImageBox.TabIndex = 2;
			this.FImageBox.TabStop = false;
			// 
			// ImageViewNode
			// 
			this.Controls.Add(this.FImageBox);
			this.Name = "ImageViewNode";
			this.Size = new System.Drawing.Size(531, 344);
			this.Resize += new System.EventHandler(this.ImageViewNode_Resize);
			((System.ComponentModel.ISupportInitialize)(this.FImageBox)).EndInit();
			this.ResumeLayout(false);
		}


		CVImageLink FInput = null;
		CVImage FImage = new CVImage();
		#endregion constructor and init

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (SpreadMax == 0)
			{
				RemoveListeners();
				return;
			}

			int slice = FPinInSlice[0];
			if (FInput != FPinInInput[slice])
			{
				FInput = FPinInInput[slice];
				AddListeners();
				if (FInput.Allocated)
				{
					UpdateAttributes(FInput.ImageAttributes);
					UpdateImage();
				}
			}
		}

		EventHandler FUpdate = null;
		EventHandler<ImageAttributesChangedEventArgs> FAttributesUpdate = null;

		void AddListeners()
		{
			RemoveListeners();

			FUpdate = new EventHandler(FInput_ImageUpdate);
			FAttributesUpdate = new EventHandler<ImageAttributesChangedEventArgs>(FInput_ImageAttributesUpdate);

			FInput.ImageUpdate += FUpdate;
			FInput.ImageAttributesUpdate += FAttributesUpdate;
		}

		void RemoveListeners()
		{
			if (FUpdate != null)
			{
				if (FInput != null)
				{
					FInput.ImageUpdate -= FUpdate;
					FInput.ImageAttributesUpdate -= FAttributesUpdate;
				}
				FUpdate = null;
				FAttributesUpdate = null;
			}
		}

		void FInput_ImageAttributesUpdate(object sender, ImageAttributesChangedEventArgs e)
		{
			UpdateAttributes(e.Attributes);
		}

		void FInput_ImageUpdate(object sender, EventArgs e)
		{
			UpdateImage();
		}

		void UpdateAttributes(CVImageAttributes attributes)
		{
			FImage.Initialise(attributes);
		}

		void UpdateImage()
		{
			if (!FInput.Allocated)
				return;
			FInput.GetImage(FImage);
			FImageBox.Image = FImage.GetImage();
		}

		private void ImageViewNode_Resize(object sender, EventArgs e)
		{
			FImageBox.Size = this.Size;
		}


		void IDisposable.Dispose()
		{
			RemoveListeners();
			FImageBox.Dispose();
		}
	}
}