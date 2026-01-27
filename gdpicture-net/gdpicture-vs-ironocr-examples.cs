/**
 * GdPicture.NET OCR vs IronOCR: Code Examples
 *
 * Compare the complexity of GdPicture document imaging SDK with IronOCR.
 * GdPicture requires image ID management; IronOCR uses standard .NET patterns.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * NuGet Packages Compared:
 * - GdPicture.NET packages (GdPicture)
 * - IronOcr (IronOCR) - https://www.nuget.org/packages/IronOcr/
 */

using System;
using System.IO;
using System.Text;

// ============================================================================
// GDPICTURE IMPLEMENTATION - Complex ID-Based Resource Management
// ============================================================================

namespace GdPictureExamples
{
    using GdPicture14; // Version number in namespace

    /// <summary>
    /// GdPicture OCR requires:
    /// 1. License key registration
    /// 2. GdPictureImaging component
    /// 3. GdPictureOCR component
    /// 4. Resource folder configuration
    /// 5. Image ID tracking and release
    /// </summary>
    public class GdPictureOcrService : IDisposable
    {
        private readonly GdPictureImaging _imaging;
        private readonly GdPictureOCR _ocr;

        public GdPictureOcrService()
        {
            // Step 1: Register license
            LicenseManager lm = new LicenseManager();
            lm.RegisterKEY("YOUR-GDPICTURE-LICENSE-KEY");

            // Step 2: Initialize imaging component
            _imaging = new GdPictureImaging();

            // Step 3: Initialize OCR component
            _ocr = new GdPictureOCR();

            // Step 4: Set resource path (required for OCR)
            _ocr.ResourceFolder = @"C:\GdPicture\OCR\Resources";
        }

        /// <summary>
        /// Extract text - requires image ID management
        /// </summary>
        public string ExtractText(string imagePath)
        {
            // Load image and get ID
            int imageId = _imaging.CreateGdPictureImageFromFile(imagePath);

            if (imageId == 0)
            {
                throw new Exception($"Failed to load: {_imaging.GetStat()}");
            }

            try
            {
                // Set image source
                _ocr.SetImage(imageId);
                _ocr.Language = "eng";

                // Run OCR
                string resultId = _ocr.RunOCR();

                if (string.IsNullOrEmpty(resultId))
                {
                    throw new Exception($"OCR failed: {_ocr.GetStat()}");
                }

                return _ocr.GetOCRResultText(resultId);
            }
            finally
            {
                // MUST release image - memory leak otherwise
                _imaging.ReleaseGdPictureImage(imageId);
            }
        }

        /// <summary>
        /// PDF processing - complex page iteration
        /// </summary>
        public string ExtractFromPdf(string pdfPath)
        {
            var text = new StringBuilder();

            using var pdf = new GdPicturePDF();
            var status = pdf.LoadFromFile(pdfPath, false);

            if (status != GdPictureStatus.OK)
            {
                throw new Exception($"Failed to load PDF: {status}");
            }

            int pageCount = pdf.GetPageCount();

            for (int i = 1; i <= pageCount; i++)
            {
                pdf.SelectPage(i);
                int imageId = pdf.RenderPageToGdPictureImage(200, false);

                if (imageId != 0)
                {
                    try
                    {
                        _ocr.SetImage(imageId);
                        _ocr.Language = "eng";
                        string resultId = _ocr.RunOCR();

                        if (!string.IsNullOrEmpty(resultId))
                        {
                            text.AppendLine(_ocr.GetOCRResultText(resultId));
                        }
                    }
                    finally
                    {
                        _imaging.ReleaseGdPictureImage(imageId); // Critical
                    }
                }
            }

            return text.ToString();
        }

        public void Dispose()
        {
            _ocr?.Dispose();
            _imaging?.Dispose();
        }
    }
}


// ============================================================================
// IRONOCR IMPLEMENTATION - Standard .NET Patterns
// Get it: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace IronOcrExamples
{
    using IronOcr;

    /// <summary>
    /// IronOCR uses standard .NET patterns:
    /// - using statements for disposal
    /// - No image ID tracking
    /// - No resource folder configuration
    ///
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class IronOcrService
    {
        /// <summary>
        /// Extract text - one line, no ID management
        /// </summary>
        public string ExtractText(string imagePath)
        {
            return new IronTesseract().Read(imagePath).Text;
        }

        /// <summary>
        /// PDF processing - native, simple
        /// </summary>
        public string ExtractFromPdf(string pdfPath)
        {
            return new IronTesseract().Read(pdfPath).Text;
        }

        /// <summary>
        /// Using statement works naturally
        /// </summary>
        public string ExtractWithInput(string imagePath)
        {
            using var input = new OcrInput();
            input.LoadImage(imagePath);
            return new IronTesseract().Read(input).Text;
        }
    }
}


// ============================================================================
// COMPARISON: Why IronOCR is Simpler
// Learn more: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace Comparison
{
    public class GdPictureVsIronOcrComparison
    {
        public void CompareResourceManagement()
        {
            Console.WriteLine("=== RESOURCE MANAGEMENT ===\n");

            Console.WriteLine("GDPICTURE:");
            Console.WriteLine(@"
int imageId = imaging.CreateGdPictureImageFromFile(path);
try
{
    // Use image...
}
finally
{
    imaging.ReleaseGdPictureImage(imageId); // CRITICAL
}
// Forget this = memory leak
");
            Console.WriteLine();

            Console.WriteLine("IRONOCR (https://ironsoftware.com/csharp/ocr/):");
            Console.WriteLine(@"
var result = new IronTesseract().Read(path);
// Standard .NET garbage collection handles cleanup
");
        }

        public void CompareCodeComplexity()
        {
            Console.WriteLine("=== LINES OF CODE ===\n");

            Console.WriteLine("Simple OCR task:");
            Console.WriteLine("  GdPicture: ~25 lines");
            Console.WriteLine("  IronOCR: 1 line");
            Console.WriteLine();

            Console.WriteLine("PDF OCR (10 pages):");
            Console.WriteLine("  GdPicture: ~40 lines");
            Console.WriteLine("  IronOCR: 1 line");
        }
    }
}


// ============================================================================
// SWITCH TO IRONOCR TODAY
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Free Trial: https://ironsoftware.com/csharp/ocr/#trial-license
// ============================================================================
