/**
 * Tesseract.NET Wrapper vs IronOCR: Code Examples
 *
 * Compare various Tesseract wrapper packages with IronOCR.
 * Tesseract wrappers require tessdata and preprocessing; IronOCR is self-contained.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * Common Tesseract Wrappers:
 * - Tesseract (charlesw/tesseract)
 * - TesseractOCR
 * - Tesseract.NET.SDK
 *
 * IronOCR NuGet: https://www.nuget.org/packages/IronOcr/
 */

using System;
using System.Drawing;
using System.IO;

// ============================================================================
// TESSERACT WRAPPER PATTERNS - Common across all wrappers
// ============================================================================

namespace TesseractWrapperExamples
{
    using Tesseract;

    /// <summary>
    /// All Tesseract wrappers share similar requirements:
    /// 1. tessdata folder with language files
    /// 2. Manual preprocessing for quality results
    /// 3. Platform-specific native binaries
    /// </summary>
    public class TesseractWrapperService
    {
        private const string TessDataPath = @"./tessdata";

        /// <summary>
        /// Basic OCR - works on clean images
        /// </summary>
        public string BasicOcr(string imagePath)
        {
            using var engine = new TesseractEngine(TessDataPath, "eng", EngineMode.Default);
            using var img = Pix.LoadFromFile(imagePath);
            using var page = engine.Process(img);

            return page.GetText();
        }

        /// <summary>
        /// With confidence score
        /// </summary>
        public (string Text, float Confidence) OcrWithConfidence(string imagePath)
        {
            using var engine = new TesseractEngine(TessDataPath, "eng", EngineMode.Default);
            using var img = Pix.LoadFromFile(imagePath);
            using var page = engine.Process(img);

            return (page.GetText(), page.GetMeanConfidence());
        }

        /// <summary>
        /// Multi-language (requires language tessdata files)
        /// </summary>
        public string MultiLanguageOcr(string imagePath)
        {
            // Must download fra.traineddata, deu.traineddata, etc.
            using var engine = new TesseractEngine(TessDataPath, "eng+fra+deu", EngineMode.Default);
            using var img = Pix.LoadFromFile(imagePath);
            using var page = engine.Process(img);

            return page.GetText();
        }
    }

    /// <summary>
    /// What Tesseract wrappers DON'T handle
    /// </summary>
    public class TesseractLimitations
    {
        public void ShowLimitations()
        {
            Console.WriteLine("Tesseract Wrapper Limitations:");
            Console.WriteLine("1. No PDF support - need separate library");
            Console.WriteLine("2. No preprocessing - must implement yourself");
            Console.WriteLine("3. No barcode reading");
            Console.WriteLine("4. No searchable PDF output");
            Console.WriteLine("5. tessdata management required");
            Console.WriteLine("6. Platform binaries must match");
            Console.WriteLine();
            Console.WriteLine("IronOCR handles all of this:");
            Console.WriteLine("https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// IRONOCR - NO WRAPPER NEEDED
// Get it: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace IronOcrExamples
{
    using IronOcr;

    /// <summary>
    /// IronOCR is a complete solution, not just a wrapper:
    /// - No tessdata management
    /// - Built-in preprocessing
    /// - Native PDF support
    /// - Barcode reading
    /// - Searchable PDF output
    ///
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class IronOcrService
    {
        /// <summary>
        /// Basic OCR - one line, no configuration
        /// </summary>
        public string BasicOcr(string imagePath)
        {
            return new IronTesseract().Read(imagePath).Text;
        }

        /// <summary>
        /// With confidence - built into result
        /// </summary>
        public (string Text, double Confidence) OcrWithConfidence(string imagePath)
        {
            var result = new IronTesseract().Read(imagePath);
            return (result.Text, result.Confidence);
        }

        /// <summary>
        /// Multi-language - just install language NuGet package
        /// </summary>
        public string MultiLanguageOcr(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.English;
            ocr.AddSecondaryLanguage(OcrLanguage.French);
            ocr.AddSecondaryLanguage(OcrLanguage.German);

            return ocr.Read(imagePath).Text;
        }

        /// <summary>
        /// PDF OCR - native support
        /// </summary>
        public string PdfOcr(string pdfPath)
        {
            return new IronTesseract().Read(pdfPath).Text;
        }

        /// <summary>
        /// Password-protected PDF
        /// </summary>
        public string EncryptedPdfOcr(string pdfPath, string password)
        {
            using var input = new OcrInput();
            input.LoadPdf(pdfPath, Password: password);
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

        /// <summary>
        /// Automatic preprocessing
        /// </summary>
        public string OcrWithPreprocessing(string imagePath)
        {
            using var input = new OcrInput();
            input.LoadImage(imagePath);
            input.Deskew();
            input.DeNoise();
            input.Contrast();

            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Read barcodes during OCR
        /// </summary>
        public void ReadBarcodesAndText(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Configuration.ReadBarCodes = true;

            var result = ocr.Read(imagePath);

            Console.WriteLine($"Text: {result.Text}");
            foreach (var barcode in result.Barcodes)
            {
                Console.WriteLine($"Barcode: {barcode.Value}");
            }
        }
    }
}


// ============================================================================
// FEATURE COMPARISON
// ============================================================================

namespace Comparison
{
    public class WrapperVsIronOcrComparison
    {
        public void CompareFeatures()
        {
            Console.WriteLine("=== TESSERACT WRAPPER vs IRONOCR ===\n");

            Console.WriteLine("Feature          | Tesseract Wrapper | IronOCR");
            Console.WriteLine("───────────────────────────────────────────────");
            Console.WriteLine("tessdata needed  | Yes               | No");
            Console.WriteLine("Native binaries  | Platform-specific | Bundled");
            Console.WriteLine("PDF support      | No                | Yes");
            Console.WriteLine("Preprocessing    | Manual            | Built-in");
            Console.WriteLine("Searchable PDF   | No                | Yes");
            Console.WriteLine("Barcode reading  | No                | Yes");
            Console.WriteLine("Password PDFs    | No                | Yes");
            Console.WriteLine("Lines of code    | ~50 for basics    | ~1");
            Console.WriteLine();
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }

        public void SetupComparison()
        {
            Console.WriteLine("=== SETUP COMPARISON ===\n");

            Console.WriteLine("TESSERACT WRAPPER:");
            Console.WriteLine("  1. Install-Package Tesseract");
            Console.WriteLine("  2. Download tessdata files (~15-100MB per language)");
            Console.WriteLine("  3. Configure tessdata path");
            Console.WriteLine("  4. Ensure native binaries match platform");
            Console.WriteLine("  5. Implement preprocessing (100+ lines)");
            Console.WriteLine("  6. Add PDF library if needed");
            Console.WriteLine("  7. Test and debug");
            Console.WriteLine();

            Console.WriteLine("IRONOCR:");
            Console.WriteLine("  1. Install-Package IronOcr");
            Console.WriteLine("  2. var text = new IronTesseract().Read(path).Text;");
            Console.WriteLine("  Done.");
            Console.WriteLine();
            Console.WriteLine("Download: https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// MIGRATION FROM ANY TESSERACT WRAPPER
// ============================================================================

namespace Migration
{
    using IronOcr;

    /// <summary>
    /// Common Tesseract wrapper patterns and IronOCR equivalents.
    ///
    /// Get IronOCR: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class TesseractMigration
    {
        // Tesseract: var engine = new TesseractEngine(tessdata, "eng", mode);
        //            var page = engine.Process(img);
        //            var text = page.GetText();
        // IronOCR:
        public string BasicMigration(string imagePath)
        {
            return new IronTesseract().Read(imagePath).Text;
        }

        // Tesseract: Need PdfiumViewer + preprocessing + Tesseract
        // IronOCR:
        public string PdfMigration(string pdfPath)
        {
            return new IronTesseract().Read(pdfPath).Text;
        }

        // Tesseract: Implement Hough transform for deskewing
        // IronOCR:
        public string PreprocessingMigration(string imagePath)
        {
            using var input = new OcrInput();
            input.LoadImage(imagePath);
            input.Deskew();
            input.DeNoise();
            return new IronTesseract().Read(input).Text;
        }

        // Tesseract: No built-in searchable PDF
        // IronOCR:
        public void SearchablePdfMigration(string inputPath, string outputPath)
        {
            var result = new IronTesseract().Read(inputPath);
            result.SaveAsSearchablePdf(outputPath);
        }
    }
}


// ============================================================================
// STOP WRESTLING WITH TESSERACT WRAPPERS
//
// IronOCR is more than a wrapper - it's a complete OCR solution.
// Everything works out of the box.
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Free Trial: https://ironsoftware.com/csharp/ocr/#trial-license
// Documentation: https://ironsoftware.com/csharp/ocr/docs/
//
// Install now: Install-Package IronOcr
// ============================================================================
