/**
 * LEADTOOLS OCR vs IronOCR: Code Examples
 *
 * Compare the complexity of LEADTOOLS document imaging SDK with IronOCR.
 * LEADTOOLS requires extensive setup; IronOCR is one NuGet package.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * NuGet Packages Compared:
 * - Leadtools, Leadtools.Ocr, Leadtools.Codecs (LEADTOOLS)
 * - IronOcr (IronOCR) - https://www.nuget.org/packages/IronOcr/
 */

using System;
using System.IO;
using System.Text;

// ============================================================================
// LEADTOOLS IMPLEMENTATION - Complex Setup Required
// ============================================================================

namespace LeadtoolsExamples
{
    using Leadtools;
    using Leadtools.Ocr;
    using Leadtools.Codecs;

    /// <summary>
    /// LEADTOOLS OCR requires:
    /// 1. License file (.LIC) and key
    /// 2. RasterCodecs initialization
    /// 3. OCR engine initialization with runtime path
    /// 4. Engine startup before use
    /// 5. Manual image loading and disposal
    /// 6. Engine shutdown on cleanup
    /// </summary>
    public class LeadtoolsOcrService : IDisposable
    {
        private IOcrEngine _ocrEngine;
        private RasterCodecs _codecs;

        public LeadtoolsOcrService()
        {
            // Step 1: Set license (complex file-based licensing)
            RasterSupport.SetLicense(
                @"C:\LEADTOOLS\License\LEADTOOLS.LIC",
                File.ReadAllText(@"C:\LEADTOOLS\License\LEADTOOLS.LIC.KEY"));

            // Step 2: Initialize image codecs
            _codecs = new RasterCodecs();

            // Step 3: Create OCR engine
            _ocrEngine = OcrEngineManager.CreateEngine(OcrEngineType.LEAD);

            // Step 4: Start engine with runtime files path
            _ocrEngine.Startup(
                _codecs,
                null,
                null,
                @"C:\LEADTOOLS\OCR\OcrRuntime" // Runtime files required
            );
        }

        /// <summary>
        /// Extract text - requires multiple steps
        /// </summary>
        public string ExtractText(string imagePath)
        {
            // Load image into LEADTOOLS format
            using var image = _codecs.Load(imagePath);

            // Create OCR document
            using var document = _ocrEngine.DocumentManager.CreateDocument();

            // Add page to document
            var page = document.Pages.AddPage(image, null);

            // Must call Recognize explicitly
            page.Recognize(null);

            // Get text
            return page.GetText(-1);
        }

        /// <summary>
        /// PDF processing requires iteration
        /// </summary>
        public string ExtractFromPdf(string pdfPath)
        {
            var text = new StringBuilder();
            var pdfInfo = _codecs.GetInformation(pdfPath, true);

            using var document = _ocrEngine.DocumentManager.CreateDocument();

            for (int i = 1; i <= pdfInfo.TotalPages; i++)
            {
                using var pageImage = _codecs.Load(pdfPath, 0,
                    CodecsLoadByteOrder.BgrOrGray, i, i);

                var page = document.Pages.AddPage(pageImage, null);
                page.Recognize(null);
                text.AppendLine(page.GetText(-1));
            }

            return text.ToString();
        }

        public void Dispose()
        {
            _ocrEngine?.Shutdown(); // Must shutdown
            _ocrEngine?.Dispose();
            _codecs?.Dispose();
        }
    }
}


// ============================================================================
// IRONOCR IMPLEMENTATION - Simple, One NuGet Package
// Get it: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace IronOcrExamples
{
    using IronOcr;

    /// <summary>
    /// IronOCR requires:
    /// 1. Install-Package IronOcr
    /// Done.
    ///
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class IronOcrService
    {
        /// <summary>
        /// Extract text - one line
        /// </summary>
        public string ExtractText(string imagePath)
        {
            return new IronTesseract().Read(imagePath).Text;
        }

        /// <summary>
        /// PDF processing - native support
        /// </summary>
        public string ExtractFromPdf(string pdfPath)
        {
            return new IronTesseract().Read(pdfPath).Text;
        }

        /// <summary>
        /// Password PDF - built-in
        /// </summary>
        public string ExtractFromEncryptedPdf(string pdfPath, string password)
        {
            using var input = new OcrInput();
            input.LoadPdf(pdfPath, Password: password);
            return new IronTesseract().Read(input).Text;
        }
    }
}


// ============================================================================
// COMPARISON: Why Choose IronOCR Over LEADTOOLS
// Learn more: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace Comparison
{
    public class LeadtoolsVsIronOcrComparison
    {
        public void CompareSetup()
        {
            Console.WriteLine("=== SETUP COMPARISON ===\n");

            Console.WriteLine("LEADTOOLS:");
            Console.WriteLine("  1. Purchase license");
            Console.WriteLine("  2. Download SDK (~500MB+)");
            Console.WriteLine("  3. Install license files (.LIC + .KEY)");
            Console.WriteLine("  4. Configure runtime files path");
            Console.WriteLine("  5. Add multiple NuGet packages");
            Console.WriteLine("  6. Initialize RasterCodecs");
            Console.WriteLine("  7. Create and start OCR engine");
            Console.WriteLine("  8. Shutdown engine on dispose");
            Console.WriteLine("  Setup lines: ~20+");
            Console.WriteLine();

            Console.WriteLine("IRONOCR (https://ironsoftware.com/csharp/ocr/):");
            Console.WriteLine("  1. Install-Package IronOcr");
            Console.WriteLine("  2. var text = new IronTesseract().Read(path).Text;");
            Console.WriteLine("  Setup lines: 1");
        }

        public void ComparePricing()
        {
            Console.WriteLine("=== PRICING COMPARISON ===\n");

            Console.WriteLine("LEADTOOLS:");
            Console.WriteLine("  Per-developer: $3,000-15,000+/year");
            Console.WriteLine("  5 developers × 3 years: $45,000-225,000");
            Console.WriteLine();

            Console.WriteLine("IRONOCR (https://ironsoftware.com/csharp/ocr/#licensing):");
            Console.WriteLine("  Lite: $749 one-time");
            Console.WriteLine("  Professional: $2,999 one-time");
            Console.WriteLine("  Unlimited: $5,999 one-time");
        }
    }
}


// ============================================================================
// READY TO SWITCH?
//
// Get IronOCR: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Documentation: https://ironsoftware.com/csharp/ocr/docs/
// ============================================================================
