using Emgu.CV;
using Emgu.CV.Structure;
using Tesseract;

namespace BlazorApp1.Data
{
    public class TesseractOcrService : IOCrService
    {
        public string performOcr(Image<Gray, byte> image)
        {
            string result;
            try
            {

                using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
                {

                    using (var img = PixConverter.ToPix(image.ToBitmap()))
                    {
                        using (var page = engine.Process(img))
                        {
                            result = page.GetText();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred in ApplyTesseract: " + ex.Message);
                result = string.Empty;
            }
            return result;
        }
    }
}
