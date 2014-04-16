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
        [Input("Minimum", DefaultValues = new double[] {0, 0, 0}, MinValue = 0, MaxValue = 1)] 
        public Vector3D Minimum;

        [Input("Maximum", DefaultValues = new double[] {1, 1, 1}, MinValue = 0, MaxValue = 1)] 
        public Vector3D Maximum;

        [Input("Pass Original", DefaultBoolean = false, IsToggle = true, IsSingle = true)] 
        public bool PassOriginal;

        private readonly CVImage FHsvImage = new CVImage();
        private readonly CVImage FBuffer = new CVImage();


        private double FMult = byte.MaxValue;

        public override void Allocate()
        {
            FHsvImage.Initialise(FInput.ImageAttributes.Size, TColorFormat.HSV32F);
            FBuffer.Initialise(FInput.ImageAttributes.Size, TColorFormat.L8);
            
            FOutput.Image.Initialise(FInput.Image.ImageAttributes);

            FMult = FInput.ImageAttributes.BytesPerPixel > 4 ? float.MaxValue : byte.MaxValue;
        }

        public override void Process()
        {
            if (!FInput.LockForReading()) return;

            FInput.GetImage(FHsvImage);
            
            FInput.ReleaseForReading();

            CvInvoke.cvInRangeS(FHsvImage.CvMat, new MCvScalar(Minimum.x * FMult, Minimum.y * FMult, Minimum.z * FMult),
                    new MCvScalar(Maximum.x * FMult, Maximum.y * FMult, Maximum.z * FMult), FBuffer.CvMat);

            if (PassOriginal)
            {
                FOutput.Image.SetImage(FInput.Image);

                CvInvoke.cvNot(FBuffer.CvMat, FBuffer.CvMat);
                CvInvoke.cvSet(FOutput.Image.CvMat, new MCvScalar(0.0), FBuffer.CvMat);

                FOutput.Send();
            }
            else
            {
                FOutput.Send(FBuffer);
            }
        }
    }
}