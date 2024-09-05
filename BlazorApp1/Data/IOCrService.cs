using Emgu.CV;
using Emgu.CV.Structure;

namespace BlazorApp1.Data
{
    public interface IOCrService
    {
        string performOcr(Image<Gray, byte> image);
    }
}
