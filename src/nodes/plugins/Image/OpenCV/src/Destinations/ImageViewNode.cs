using System;
using System.Windows.Forms;
using Emgu.CV.UI;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.OpenCV
{
	[PluginInfo(Name = "Preview", Category = "OpenCV", Help = "Preview an EmguCV Image", Author = "elliotwoods", AutoEvaluate = true)]
	public class PreviewNode : UserControl, IPluginEvaluate, IDisposable
	{
		[Input("Input")]
		ISpread<CVImageLink> FPinInInput;

		[Input("Slice", IsSingle = true)]
		ISpread<int> FPinInSlice;

		private ImageBox FImageBox;

		#region constructor and init
		public PreviewNode()
		{
			//setup the gui
			InitializeComponent();
		}

		void InitializeComponent()
		{
			FImageBox = new ImageBox();
			((System.ComponentModel.ISupportInitialize)(FImageBox)).BeginInit();
			SuspendLayout();

			// 
			// FImageBox
			// 
			FImageBox.Location = new System.Drawing.Point(0, 0);
			FImageBox.Name = "FImageBox";
			FImageBox.Size = Size;
			FImageBox.TabIndex = 2;
			FImageBox.TabStop = false;

			// 
			// ImageViewNode
			// 
			Controls.Add(FImageBox);
			Name = "PreviewNode";
			Size = new System.Drawing.Size(531, 344);
			Resize += ImageViewNode_Resize;
			((System.ComponentModel.ISupportInitialize)(FImageBox)).EndInit();
			ResumeLayout(false);
		}


		CVImageLink FInput;
		readonly CVImage FImage = new CVImage();
		#endregion constructor and init

		public void Evaluate(int spreadMax)
		{
			if (spreadMax == 0)
			{
				RemoveListeners();
				return;
			}

			var slice = FPinInSlice[0];
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

		EventHandler FUpdate;
		EventHandler<ImageAttributesChangedEventArgs> FAttributesUpdate;

		void AddListeners()
		{
			RemoveListeners();

			FUpdate = FInput_ImageUpdate;
			FAttributesUpdate = FInput_ImageAttributesUpdate;

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
			FImageBox.Size = Size;
		}


		void IDisposable.Dispose()
		{
			RemoveListeners();
			FImageBox.Dispose();
		}
	}
}