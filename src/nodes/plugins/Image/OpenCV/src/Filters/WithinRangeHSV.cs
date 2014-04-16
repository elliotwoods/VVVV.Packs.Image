using Emgu.CV;
using Emgu.CV.Structure;

using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.CV.Core;

namespace VVVV.CV.Nodes
{
    [FilterInstance("WithinRange", Version = "HSV", Author = "alg", Help = "check if color is in target HSV range")]
    public class WithinRangeHsvInstance : IFilterInstance
    {
        [Input("Minimum", DefaultValues = new double[] {0, 0, 0}, MinValue = 0, MaxValue = 1)] 
        public Vector3D Minimum;

        [Input("Maximum", DefaultValues = new double[] {1, 1, 1}, MinValue = 0, MaxValue = 1)] 
        public Vector3D Maximum;

        [Input("Pass Original", DefaultBoolean = false, IsToggle = true, IsSingle = true)] 
        public bool PassOriginal;

        [Input("Raw Range", DefaultBoolean = false, IsToggle = true, IsSingle = true, Visibility = PinVisibility.OnlyInspector)]
        public bool RawRange;

        private readonly CVImage FHsvImage = new CVImage();
        private readonly CVImage FBuffer = new CVImage();


        private double FRangeMult = byte.MaxValue;

        public override void Allocate()
        {
            FHsvImage.Initialise(FInput.ImageAttributes.Size, TColorFormat.HSV32F);
            FBuffer.Initialise(FInput.ImageAttributes.Size, TColorFormat.L8);
            
            FOutput.Image.Initialise(FInput.Image.ImageAttributes);

            FRangeMult = FInput.ImageAttributes.BytesPerPixel > 4 ? float.MaxValue : byte.MaxValue;
        }

        public override void Process()
        {
            if (!FInput.LockForReading()) return;

            FInput.GetImage(FHsvImage);
            
            FInput.ReleaseForReading();

            var mult = FRangeMult;
            if (RawRange) mult = 1;

            CvInvoke.cvInRangeS(FHsvImage.CvMat, new MCvScalar(Minimum.x * mult, Minimum.y * mult, Minimum.z * mult),
                    new MCvScalar(Maximum.x * mult, Maximum.y * mult, Maximum.z * mult), FBuffer.CvMat);

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