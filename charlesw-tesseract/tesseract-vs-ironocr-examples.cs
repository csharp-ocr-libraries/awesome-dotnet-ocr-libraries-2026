/**
 * charlesw/Tesseract vs IronOCR: Complete Code Examples
 *
 * This file shows the REALITY of using raw Tesseract vs IronOCR.
 * Tesseract is free but requires significant development investment.
 * IronOCR handles complexity so you can ship faster.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * Tesseract NuGet: Install-Package Tesseract
 * IronOCR NuGet: Install-Package IronOcr
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

// ============================================================================
// TESSERACT BASIC USAGE - Looks Simple, But...
// ============================================================================

namespace TesseractBasicExamples
{
    using Tesseract;

    /// <summary>
    /// Basic Tesseract usage on CLEAN images.
    /// This works well - but most real documents aren't clean.
    /// </summary>
    public class BasicTesseractUsage
    {
        private const string TessDataPath = @"./tessdata";

        /// <summary>
        /// Simple case - works on clean 300 DPI scans
        /// </summary>
        public string ExtractText(string imagePath)
        {
            using var engine = new TesseractEngine(TessDataPath, "eng", EngineMode.Default);
            using var img = Pix.LoadFromFile(imagePath);
            using var page = engine.Process(img);

            return page.GetText();
        }
    }
}


// ============================================================================
// TESSERACT REALITY - What You Actually Need for Production
// ============================================================================

namespace TesseractProductionExamples
{
    using Tesseract;

    /// <summary>
    /// PRODUCTION Tesseract usage requires PREPROCESSING.
    /// Without preprocessing, results on real documents are poor.
    ///
    /// You must build:
    /// 1. Grayscale conversion
    /// 2. Contrast enhancement
    /// 3. Binarization
    /// 4. Deskewing
    /// 5. Noise removal
    /// 6. DPI normalization
    ///
    /// This is 100-300 lines of additional code.
    /// </summary>
    public class ProductionTesseractWithPreprocessing
    {
        private const string TessDataPath = @"./tessdata";

        /// <summary>
        /// Full preprocessing pipeline - required for production
        /// </summary>
        public string ExtractWithPreprocessing(string imagePath)
        {
            using var original = new Bitmap(imagePath);

            // Step 1: Grayscale
            using var grayscale = ConvertToGrayscale(original);

            // Step 2: Enhance contrast
            using var enhanced = EnhanceContrast(grayscale);

            // Step 3: Binarize
            using var binarized = Binarize(enhanced);

            // Step 4: Deskew (complex - Hough transform)
            using var deskewed = Deskew(binarized);

            // Step 5: Denoise
            using var denoised = Denoise(deskewed);

            // Step 6: Scale to 300 DPI
            using var scaled = ScaleToDpi(denoised, 300);

            // Step 7: Save to temp and process
            var tempPath = Path.GetTempFileName() + ".png";
            try
            {
                scaled.Save(tempPath, ImageFormat.Png);

                using var engine = new TesseractEngine(TessDataPath, "eng", EngineMode.Default);
                using var img = Pix.LoadFromFile(tempPath);
                using var page = engine.Process(img);

                return page.GetText();
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }

        // Each method is 10-30 lines of image processing code
        private Bitmap ConvertToGrayscale(Bitmap original)
        {
            // ~20 lines using ColorMatrix
            throw new NotImplementedException("Implement grayscale conversion");
        }

        private Bitmap EnhanceContrast(Bitmap image)
        {
            // ~15 lines of pixel manipulation
            throw new NotImplementedException("Implement contrast enhancement");
        }

        private Bitmap Binarize(Bitmap image)
        {
            // ~15 lines with adaptive thresholding
            throw new NotImplementedException("Implement binarization");
        }

        private Bitmap Deskew(Bitmap image)
        {
            // ~50+ lines with Hough transform or projection profile
            throw new NotImplementedException("Implement deskewing");
        }

        private Bitmap Denoise(Bitmap image)
        {
            // ~25 lines with median filter
            throw new NotImplementedException("Implement denoising");
        }

        private Bitmap ScaleToDpi(Bitmap image, int targetDpi)
        {
            // ~20 lines with interpolation
            throw new NotImplementedException("Implement DPI scaling");
        }
    }

    /// <summary>
    /// PDF processing with Tesseract requires ANOTHER library
    /// </summary>
    public class TesseractPdfProcessing
    {
        /// <summary>
        /// Tesseract CANNOT process PDFs directly.
        /// You need PdfiumViewer, PDFtoImage, or similar.
        /// </summary>
        public string ExtractFromPdf(string pdfPath)
        {
            // This is NOT Tesseract code - you need an additional library
            throw new NotImplementedException(@"
PDF processing requires:
1. Install PdfiumViewer (or PDFtoImage, Docnet, etc.)
2. Install platform-specific native binaries
3. Render each PDF page to image
4. Apply preprocessing pipeline
5. Process with Tesseract
6. Combine results

~50-100 additional lines of code
");
        }
    }
}


// ============================================================================
// IRONOCR - HANDLES ALL COMPLEXITY FOR YOU
// Get it: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace IronOcrExamples
{
    using IronOcr;

    /// <summary>
    /// IronOCR handles:
    /// - Automatic preprocessing
    /// - PDF support (native)
    /// - Password-protected PDFs
    /// - All tessdata bundled
    /// - Thread safety
    /// - Memory management
    ///
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class IronOcrSimple
    {
        /// <summary>
        /// This single line includes automatic preprocessing
        /// </summary>
        public string ExtractText(string imagePath)
        {
            return new IronTesseract().Read(imagePath).Text;
        }

        /// <summary>
        /// Native PDF support - no additional libraries
        /// </summary>
        public string ExtractFromPdf(string pdfPath)
        {
            return new IronTesseract().Read(pdfPath).Text;
        }

        /// <summary>
        /// Password PDFs - one parameter
        /// </summary>
        public string ExtractFromEncryptedPdf(string pdfPath, string password)
        {
            using var input = new OcrInput();
            input.LoadPdf(pdfPath, Password: password);
            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Explicit preprocessing when needed
        /// </summary>
        public string ExtractWithFilters(string imagePath)
        {
            using var input = new OcrInput();
            input.LoadImage(imagePath);
            input.Deskew();
            input.DeNoise();
            input.Contrast();
            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Create searchable PDF
        /// </summary>
        public void CreateSearchablePdf(string inputPdf, string outputPdf)
        {
            var result = new IronTesseract().Read(inputPdf);
            result.SaveAsSearchablePdf(outputPdf);
        }
    }
}


// ============================================================================
// COMPARISON: Development Time and Cost
// ============================================================================

namespace Comparison
{
    public class TesseractVsIronOcrComparison
    {
        public void CompareDevelopmentTime()
        {
            Console.WriteLine("=== DEVELOPMENT TIME COMPARISON ===\n");

            Console.WriteLine("TESSERACT (production-ready solution):");
            Console.WriteLine("  Preprocessing pipeline: 2-3 days");
            Console.WriteLine("  PDF handling: 1-2 days");
            Console.WriteLine("  Error handling: 1 day");
            Console.WriteLine("  Testing: 1-2 days");
            Console.WriteLine("  Total: 5-8 days minimum");
            Console.WriteLine();

            Console.WriteLine("IRONOCR (https://ironsoftware.com/csharp/ocr/):");
            Console.WriteLine("  Installation: 5 minutes");
            Console.WriteLine("  Implementation: 1 hour");
            Console.WriteLine("  Testing: 1 hour");
            Console.WriteLine("  Total: ~2 hours");
        }

        public void CompareTotalCost()
        {
            Console.WriteLine("=== TOTAL COST OF OWNERSHIP ===\n");

            Console.WriteLine("TESSERACT (\"free\"):");
            Console.WriteLine("  License: $0");
            Console.WriteLine("  Development time: 40+ hours × $100/hr = $4,000+");
            Console.WriteLine("  Ongoing maintenance: Variable");
            Console.WriteLine("  Total: $4,000+ (hidden in development)");
            Console.WriteLine();

            Console.WriteLine("IRONOCR:");
            Console.WriteLine("  License: $749-2,999 (one-time)");
            Console.WriteLine("  Development time: 2 hours × $100/hr = $200");
            Console.WriteLine("  Ongoing maintenance: Minimal");
            Console.WriteLine("  Total: $949-3,199");
            Console.WriteLine();

            Console.WriteLine("BOTTOM LINE: \"Free\" Tesseract often costs MORE.");
        }

        public void CompareAccuracy()
        {
            Console.WriteLine("=== ACCURACY COMPARISON ===\n");

            Console.WriteLine("Image Type               | Tesseract (raw) | IronOCR");
            Console.WriteLine("─────────────────────────────────────────────────────");
            Console.WriteLine("Clean 300 DPI scan       | 95%+           | 99%+");
            Console.WriteLine("Low-res 72 DPI           | 40-60%         | 95%+");
            Console.WriteLine("Skewed document          | 60-70%         | 98%+");
            Console.WriteLine("Noisy fax                | 30-50%         | 92%+");
            Console.WriteLine("Photo of document        | 20-40%         | 88%+");
            Console.WriteLine();
            Console.WriteLine("IronOCR applies automatic preprocessing.");
        }
    }
}


// ============================================================================
// READY TO SAVE TIME AND MONEY?
//
// Get IronOCR: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Free Trial: https://ironsoftware.com/csharp/ocr/#trial-license
// Documentation: https://ironsoftware.com/csharp/ocr/docs/
//
// Install now: Install-Package IronOcr
// ============================================================================
