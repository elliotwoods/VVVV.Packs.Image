using Emgu.CV;
using Emgu.CV.Structure;

using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.CV.Core;

namespace VVVV.CV.Nodes
{
    [FilterInstance("WithinRange", Version = "HSV", Author = "alg", Help = "Check if value is in target HSV range")]
    public class WithinRangeHsvInstance : IFilterInstance
    {
        [Input("Minimum", DefaultValues = new double[] {0, 0, 0}, MinValue = 0, MaxValue = 1)] public Vector3D Minimum;

        [Input("Maximum", DefaultValues = new double[] {1, 1, 1}, MinValue = 0, MaxValue = 1)] public Vector3D Maximum;

        private double FMult = byte.MaxValue;

        public override void Allocate()
        {
            FOutput.Image.Initialise(FInput.Image.ImageAttributes.Size, TColorFormat.L8);

            FMult = FInput.ImageAttributes.BytesPerPixel > 4 ? float.MaxValue : byte.MaxValue;
        }

        public override void Process()
        {
            if (!FInput.LockForReading()) return;

            CvInvoke.cvInRangeS(FInput.CvMat, new MCvScalar(Minimum.x*FMult, Minimum.y*FMult, Minimum.z*FMult),
                new MCvScalar(Maximum.x*FMult, Maximum.y*FMult, Maximum.z*FMult), FOutput.CvMat);
            FInput.ReleaseForReading();

            FOutput.Send();
        }
    }
}