/**
 * PaddleSharp/PaddleOCR vs IronOCR: Code Examples
 *
 * Compare deep learning PaddleOCR with IronOCR.
 * PaddleSharp requires model management; IronOCR is self-contained.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * NuGet Packages Compared:
 * - Sdcb.PaddleOCR, OpenCvSharp4 (PaddleSharp)
 * - IronOcr (IronOCR) - https://www.nuget.org/packages/IronOcr/
 */

using System;
using System.Linq;

// ============================================================================
// PADDLESHARP IMPLEMENTATION - Deep Learning, Complex Setup
// ============================================================================

namespace PaddleSharpExamples
{
    /*
    using Sdcb.PaddleOCR;
    using Sdcb.PaddleOCR.Models;
    using OpenCvSharp;

    /// <summary>
    /// PaddleSharp requires:
    /// 1. Multiple NuGet packages
    /// 2. Model downloads (detection, classification, recognition)
    /// 3. OpenCV dependency
    /// 4. CUDA setup for GPU (optional)
    /// </summary>
    public class PaddleOcrService
    {
        private readonly PaddleOcrAll _ocr;

        public PaddleOcrService()
        {
            // Download and configure models
            var detModel = LocalFullModels.ChineseV3.DetectionModel;
            var recModel = LocalFullModels.ChineseV3.RecognitionModel;
            var clsModel = LocalFullModels.ChineseV3.ClassifierModel;

            _ocr = new PaddleOcrAll(detModel, clsModel, recModel);
        }

        /// <summary>
        /// Extract text - requires OpenCV for image loading
        /// </summary>
        public string ExtractText(string imagePath)
        {
            using var image = Cv2.ImRead(imagePath);
            var result = _ocr.Run(image);

            return string.Join("\n", result.Regions
                .OrderBy(r => r.Rect.Center.Y)
                .ThenBy(r => r.Rect.Center.X)
                .Select(r => r.Text));
        }
    }
    */

    /// <summary>
    /// Placeholder showing the complexity
    /// </summary>
    public class PaddleOcrPlaceholder
    {
        public void ShowSetupComplexity()
        {
            Console.WriteLine("PaddleSharp Setup Requirements:");
            Console.WriteLine("1. Install Sdcb.PaddleOCR");
            Console.WriteLine("2. Install OpenCvSharp4");
            Console.WriteLine("3. Install OpenCvSharp4.runtime.win (platform-specific)");
            Console.WriteLine("4. Download detection model (~3MB)");
            Console.WriteLine("5. Download classification model (~2MB)");
            Console.WriteLine("6. Download recognition model (~10MB)");
            Console.WriteLine("7. Configure model paths");
            Console.WriteLine("8. Handle OpenCV image loading");
        }
    }
}


// ============================================================================
// IRONOCR - ZERO MODEL MANAGEMENT
// Get it: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace IronOcrExamples
{
    using IronOcr;

    /// <summary>
    /// IronOCR requires:
    /// 1. Install-Package IronOcr
    /// Done. No models, no OpenCV, no configuration.
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
        /// PDF support included
        /// </summary>
        public string ExtractFromPdf(string pdfPath)
        {
            return new IronTesseract().Read(pdfPath).Text;
        }

        /// <summary>
        /// 125+ languages via NuGet packages
        /// </summary>
        public string ExtractChinese(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.ChineseSimplified;
            return ocr.Read(imagePath).Text;
        }
    }
}


// ============================================================================
// COMPARISON
// ============================================================================

namespace Comparison
{
    public class PaddleVsIronOcrComparison
    {
        public void Compare()
        {
            Console.WriteLine("=== PADDLESHARP vs IRONOCR ===\n");

            Console.WriteLine("Feature          | PaddleSharp    | IronOCR");
            Console.WriteLine("─────────────────────────────────────────────");
            Console.WriteLine("NuGet packages   | 3-4            | 1");
            Console.WriteLine("Model management | Required       | None");
            Console.WriteLine("PDF support      | Via conversion | Native");
            Console.WriteLine("Languages        | ~10            | 125+");
            Console.WriteLine("GPU support      | Yes (CUDA)     | CPU optimized");
            Console.WriteLine("Password PDFs    | No             | Yes");
            Console.WriteLine();
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// TRY IRONOCR - SIMPLER, MORE FEATURES
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Install: Install-Package IronOcr
// ============================================================================
