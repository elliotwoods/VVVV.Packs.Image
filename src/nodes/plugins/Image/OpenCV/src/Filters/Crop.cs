//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Emgu.CV;
//using VVVV.PluginInterfaces.V2;

//namespace VVVV.CV.Nodes
//{
//	class CropInstance : IFilterInstance
//	{
//		public int Top = 0;
//		public int Left = 0;
//		public int Width = 16;
//		public int Height = 16;



//		public override void Process()
//		{
//			if (FOutput.Image.Width != Width || FOutput.Image.Height != Height)
//			{
//				FOutput.Image.Initialise(new System.Drawing.Size(Width, Height), FInput.ImageAttributes.ColourFormat);
//			}

//			FInput.Image.
//			CvInvoke.cvCopy(FInput.CvMat, FOutput.CvMat, IntPtr.Zero);
//			FOutput.Send();
//		}
//	}

//	#region PluginInfo
//	[PluginInfo(Name = "Crop", Category = "CV.Image", Version = "", Help = "Crop a sub-portion of an image", Author = "elliotwoods", Credits = "", Tags = "")]
//	#endregion PluginInfo
//	class CropNode : IFilterNode<CropInstance>
//	{
//		[Input("Top")]
//		IDiffSpread<int> FInTop;

//		[Input("Left")]
//		IDiffSpread<int> FInLeft;

//		[Input("Width", DefaultValue=16)]
//		IDiffSpread<int> FInWidth;

//		[Input("Height", DefaultValue=16)]
//		IDiffSpread<int> FInHeight;

//		protected override void Update(int InstanceCount, bool SpreadChanged)
//		{
//			if (FInTop.IsChanged)
//				for (int i = 0; i < InstanceCount; i++)
//					FProcessor[i].Top = FInTop[i];

//			if (FInLeft.IsChanged)
//				for (int i = 0; i < InstanceCount; i++)
//					FProcessor[i].Left = FInLeft[i];

//			if (FInWidth.IsChanged)
//				for (int i = 0; i < InstanceCount; i++)
//					FProcessor[i].Width = FInWidth[i];

//			if (FInHeight.IsChanged)
//				for (int i = 0; i < InstanceCount; i++)
//					FProcessor[i].Height = FInHeight[i];
//		}
//	}
//}
