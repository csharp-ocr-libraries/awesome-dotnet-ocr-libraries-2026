/**
 * RapidOCR.NET vs IronOCR: Code Examples
 *
 * Compare open-source RapidOCR with IronOCR.
 * RapidOCR is based on PaddleOCR; IronOCR is production-ready.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * RapidOCR: Open-source PaddleOCR wrapper
 * IronOCR NuGet: https://www.nuget.org/packages/IronOcr/
 */

using System;

// ============================================================================
// RAPIDOCR.NET IMPLEMENTATION
// ============================================================================

namespace RapidOcrExamples
{
    /*
    using RapidOCR;

    /// <summary>
    /// RapidOCR.NET wraps PaddleOCR deep learning models.
    /// Requires model files and configuration.
    /// </summary>
    public class RapidOcrService
    {
        private readonly RapidOCREngine _engine;

        public RapidOcrService()
        {
            // Requires model files
            _engine = new RapidOCREngine(new RapidOCROptions
            {
                DetModelPath = "models/det.onnx",
                RecModelPath = "models/rec.onnx",
                ClsModelPath = "models/cls.onnx",
                KeysPath = "models/keys.txt"
            });
        }

        public string ExtractText(string imagePath)
        {
            var result = _engine.Run(imagePath);
            return string.Join("\n", result.TextBlocks.Select(b => b.Text));
        }

        public void Dispose()
        {
            _engine?.Dispose();
        }
    }
    */

    public class RapidOcrPlaceholder
    {
        public void ShowRequirements()
        {
            Console.WriteLine("RapidOCR.NET Requirements:");
            Console.WriteLine("1. Download ONNX model files (~30-50MB)");
            Console.WriteLine("2. Configure model paths");
            Console.WriteLine("3. ONNX Runtime dependency");
            Console.WriteLine("4. Limited PDF support");
            Console.WriteLine("5. Primarily Chinese/English focused");
            Console.WriteLine();
            Console.WriteLine("For production-ready OCR, try IronOCR:");
            Console.WriteLine("https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// IRONOCR - PRODUCTION READY
// Get it: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace IronOcrExamples
{
    using IronOcr;

    /// <summary>
    /// IronOCR is production-ready out of the box:
    /// - No model management
    /// - 125+ languages
    /// - Native PDF support
    /// - Built-in preprocessing
    ///
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class IronOcrService
    {
        /// <summary>
        /// Simple extraction - no model configuration
        /// </summary>
        public string ExtractText(string imagePath)
        {
            return new IronTesseract().Read(imagePath).Text;
        }

        /// <summary>
        /// PDF OCR - native support
        /// </summary>
        public string ExtractFromPdf(string pdfPath)
        {
            return new IronTesseract().Read(pdfPath).Text;
        }

        /// <summary>
        /// 125+ languages - just add NuGet package
        /// </summary>
        public string ChineseOcr(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.ChineseSimplified;
            return ocr.Read(imagePath).Text;
        }

        /// <summary>
        /// Japanese OCR
        /// </summary>
        public string JapaneseOcr(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Japanese;
            return ocr.Read(imagePath).Text;
        }

        /// <summary>
        /// Korean OCR
        /// </summary>
        public string KoreanOcr(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.Korean;
            return ocr.Read(imagePath).Text;
        }

        /// <summary>
        /// Mixed CJK and English
        /// </summary>
        public string MixedCjkEnglishOcr(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.ChineseSimplified;
            ocr.AddSecondaryLanguage(OcrLanguage.English);

            return ocr.Read(imagePath).Text;
        }

        /// <summary>
        /// With preprocessing for better results
        /// </summary>
        public string HighQualityOcr(string imagePath)
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
        public void CreateSearchablePdf(string inputPath, string outputPath)
        {
            var result = new IronTesseract().Read(inputPath);
            result.SaveAsSearchablePdf(outputPath);
        }
    }
}


// ============================================================================
// COMPARISON
// ============================================================================

namespace Comparison
{
    public class RapidOcrVsIronOcrComparison
    {
        public void Compare()
        {
            Console.WriteLine("=== RAPIDOCR vs IRONOCR ===\n");

            Console.WriteLine("Feature          | RapidOCR       | IronOCR");
            Console.WriteLine("─────────────────────────────────────────────");
            Console.WriteLine("Model management | Required       | None");
            Console.WriteLine("Languages        | CJK focused    | 125+");
            Console.WriteLine("PDF support      | Limited        | Native");
            Console.WriteLine("Preprocessing    | Manual         | Built-in");
            Console.WriteLine("Searchable PDF   | No             | Yes");
            Console.WriteLine("Installation     | Complex        | NuGet");
            Console.WriteLine("Support          | Community      | Commercial");
            Console.WriteLine();
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }

        public void LanguageSupport()
        {
            Console.WriteLine("=== LANGUAGE SUPPORT ===\n");

            Console.WriteLine("RAPIDOCR:");
            Console.WriteLine("  - Chinese (Simplified/Traditional)");
            Console.WriteLine("  - English");
            Console.WriteLine("  - Japanese (limited)");
            Console.WriteLine("  - Korean (limited)");
            Console.WriteLine();

            Console.WriteLine("IRONOCR (125+ languages):");
            Console.WriteLine("  - All major European languages");
            Console.WriteLine("  - All CJK languages");
            Console.WriteLine("  - Arabic, Hebrew, Hindi");
            Console.WriteLine("  - Cyrillic languages");
            Console.WriteLine("  - And many more...");
            Console.WriteLine();
            Console.WriteLine("Download: https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// NEED PRODUCTION-READY OCR?
//
// IronOCR works out of the box. No model management required.
// 125+ languages, PDF support, commercial support included.
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Install: Install-Package IronOcr
// ============================================================================
