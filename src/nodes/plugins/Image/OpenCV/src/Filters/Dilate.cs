#region using
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using System;
using VVVV.Utils.VColor;

#endregion

namespace VVVV.Nodes.OpenCV
{
	public class DilateInstance : IFilterInstance
	{
		private int FIterations = 1;
		public int Iterations
		{
			set
			{
				if (value < 0)
					value = 0;
				if (value > 64)
					value = 64;

				FIterations = value;
			}
		}

		public override void Allocate()
		{
			FOutput.Image.Initialise(FInput.Image.ImageAttributes);
		}

		public override void Process()
		{
			if (!FInput.LockForReading())
				return;
			CvInvoke.cvDilate(FInput.CvMat, FOutput.CvMat, IntPtr.Zero, FIterations);
			FInput.ReleaseForReading();

			FOutput.Send();
		}

	}

	#region PluginInfo
	[PluginInfo(Name = "Dilate", Category = "OpenCV", Version = "", Help = "Inflate features in image, i.e. grow noise", Author = "elliotwoods", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class DilateNode : IFilterNode<DilateInstance>
	{
		[Input("Iterations", MinValue = 0, MaxValue = 64, DefaultValue = 1)]
		IDiffSpread<int> FIterations;

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			if (FIterations.IsChanged || SpreadChanged)
			{
				for (int i = 0; i < InstanceCount; i++)
				{
					FProcessor[i].Iterations = FIterations[i];
					FProcessor[i].FlagForProcess();
				}
			}
		}
	}
}
