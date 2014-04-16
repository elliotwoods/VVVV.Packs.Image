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
using VVVV.CV.Core;

#endregion

namespace VVVV.CV.Nodes
{
	#region Interface
	public abstract class CMPInstance : IFilterInstance
	{
		[Input("Input 2")]
		public double Threshold = 0.5;

		protected CVImage Buffer = new CVImage();

        [Input("Pass Original", DefaultBoolean = false, IsToggle = true, IsSingle = true)]
	    public bool PassOriginal;

	    [Input("Raw Range", DefaultBoolean = false, IsToggle = true, IsSingle = true, Visibility = PinVisibility.OnlyInspector)] 
        public bool RawRange;

        protected double RangeMult = byte.MaxValue;

		public override void Allocate()
		{
			Buffer.Initialise(FInput.ImageAttributes.Size, TColorFormat.L8);

            RangeMult = FInput.ImageAttributes.BytesPerPixel > 4 ? float.MaxValue : byte.MaxValue;
		}

		public override void Process()
		{
			if (FInput.ImageAttributes.ChannelCount == 1)
			{
				if (!FInput.LockForReading())
					return;
				try
				{
					Compare(FInput.CvMat);
				}
				finally
				{
					FInput.ReleaseForReading();
				}
			}
			else
			{
				FInput.GetImage(Buffer);
				Compare(Buffer.CvMat);
			}
				
            if (PassOriginal)
			{
                FOutput.Image.SetImage(FInput.Image);

				CvInvoke.cvNot(Buffer.CvMat, Buffer.CvMat);
				CvInvoke.cvSet(FOutput.Image.CvMat, new MCvScalar(0.0), Buffer.CvMat);
				FOutput.Send();
			}
			else
				FOutput.Send(Buffer);
		}

		protected abstract void Compare(IntPtr CvMat);
	}
	#endregion

	#region Instances
	[FilterInstance("GT", Help = "Greater than")]
	public class GTInstance : CMPInstance
	{
		protected override void Compare(IntPtr CvMat)
		{
            var value = Threshold * RangeMult;
            if (RawRange) value = Threshold;
			
            CvInvoke.cvCmpS(CvMat, value, Buffer.CvMat, CMP_TYPE.CV_CMP_GT);
		}
	}

	[FilterInstance("LT", Help = "Less than")]
	public class LTInstance : CMPInstance
	{
		protected override void Compare(IntPtr CvMat)
		{
            var value = Threshold * RangeMult;
            if (RawRange) value = Threshold;
			
            CvInvoke.cvCmpS(CvMat, value, Buffer.CvMat, CMP_TYPE.CV_CMP_LT);
		}
	}

	[FilterInstance("EQ", Help = "Equal to")]
	public class EQInstance : CMPInstance
	{
		protected override void Compare(IntPtr CvMat)
		{
		    var value = Threshold*RangeMult;
		    if (RawRange) value = Threshold;
			
            CvInvoke.cvCmpS(CvMat, value, Buffer.CvMat, CMP_TYPE.CV_CMP_EQ);
		}
	}
	#endregion
}
