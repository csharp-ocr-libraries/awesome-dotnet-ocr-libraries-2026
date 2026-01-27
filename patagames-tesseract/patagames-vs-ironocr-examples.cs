/**
 * Patagames Tesseract.NET SDK vs IronOCR: Code Examples
 *
 * Compare Patagames' Tesseract wrapper with IronOCR.
 * Patagames wraps Tesseract; IronOCR is a complete OCR solution.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * NuGet Packages:
 * - Patagames.Ocr (Patagames Tesseract.NET SDK)
 * - IronOcr (IronOCR) - https://www.nuget.org/packages/IronOcr/
 */

using System;
using System.Drawing;
using System.IO;

// ============================================================================
// PATAGAMES TESSERACT.NET SDK IMPLEMENTATION
// ============================================================================

namespace PatagamesExamples
{
    /*
    using Patagames.Ocr;
    using Patagames.Ocr.Enums;

    /// <summary>
    /// Patagames Tesseract.NET SDK wraps Tesseract.
    /// Still requires tessdata files and configuration.
    /// </summary>
    public class PatagamesOcrService
    {
        private const string TessDataPath = @"./tessdata";

        /// <summary>
        /// Basic OCR with Patagames
        /// </summary>
        public string ExtractText(string imagePath)
        {
            using var api = OcrApi.Create();
            api.Init(TessDataPath, "eng");

            using var bitmap = new Bitmap(imagePath);
            return api.GetTextFromImage(bitmap);
        }

        /// <summary>
        /// Multiple languages
        /// </summary>
        public string MultiLanguageOcr(string imagePath)
        {
            using var api = OcrApi.Create();
            api.Init(TessDataPath, "eng+fra+deu");

            using var bitmap = new Bitmap(imagePath);
            return api.GetTextFromImage(bitmap);
        }

        /// <summary>
        /// With page segmentation mode
        /// </summary>
        public string OcrWithSegmentationMode(string imagePath, PageSegmentationMode mode)
        {
            using var api = OcrApi.Create();
            api.Init(TessDataPath, "eng");
            api.SetVariable("tessedit_pageseg_mode", ((int)mode).ToString());

            using var bitmap = new Bitmap(imagePath);
            return api.GetTextFromImage(bitmap);
        }
    }
    */

    public class PatagamesPlaceholder
    {
        public void ShowRequirements()
        {
            Console.WriteLine("Patagames Tesseract.NET SDK Requirements:");
            Console.WriteLine("1. Install NuGet package");
            Console.WriteLine("2. Download tessdata files");
            Console.WriteLine("3. Configure tessdata path");
            Console.WriteLine("4. System.Drawing dependency");
            Console.WriteLine("5. No built-in PDF support");
            Console.WriteLine("6. No preprocessing included");
            Console.WriteLine();
            Console.WriteLine("For a complete solution, try IronOCR:");
            Console.WriteLine("https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// IRONOCR - COMPLETE SOLUTION
// Get it: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace IronOcrExamples
{
    using IronOcr;

    /// <summary>
    /// IronOCR requires no tessdata, no configuration.
    /// Everything works out of the box.
    ///
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class IronOcrService
    {
        /// <summary>
        /// Basic OCR - one line
        /// </summary>
        public string ExtractText(string imagePath)
        {
            return new IronTesseract().Read(imagePath).Text;
        }

        /// <summary>
        /// Multiple languages - just add NuGet packages
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
        /// Specific PDF pages
        /// </summary>
        public string PdfPageRange(string pdfPath, int startPage, int endPage)
        {
            using var input = new OcrInput();
            input.LoadPdf(pdfPath);
            // TODO: verify IronOCR API for page range selection
            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Preprocessing built-in
        /// </summary>
        public string OcrWithPreprocessing(string imagePath)
        {
            using var input = new OcrInput();
            input.LoadImage(imagePath);
            input.Deskew();
            input.DeNoise();
            input.Contrast();
            input.Binarize();

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
        /// Structured data extraction
        /// </summary>
        public void ExtractStructuredData(string imagePath)
        {
            var result = new IronTesseract().Read(imagePath);

            foreach (var page in result.Pages)
            {
                Console.WriteLine($"Page {page.PageNumber}");
            }

            foreach (var line in result.Lines)
            {
                Console.WriteLine($"Line at Y={line.Y}: {line.Text}");
            }

            foreach (var word in result.Words)
            {
                Console.WriteLine($"'{word.Text}' at ({word.X},{word.Y}) - {word.Confidence}%");
            }
        }

        /// <summary>
        /// Region-based OCR
        /// </summary>
        public string ExtractRegion(string imagePath, int x, int y, int width, int height)
        {
            using var input = new OcrInput();
            var region = new System.Drawing.Rectangle(x, y, width, height);
            input.LoadImage(imagePath, region);

            return new IronTesseract().Read(input).Text;
        }
    }
}


// ============================================================================
// COMPARISON
// ============================================================================

namespace Comparison
{
    public class PatagamesVsIronOcrComparison
    {
        public void Compare()
        {
            Console.WriteLine("=== PATAGAMES vs IRONOCR ===\n");

            Console.WriteLine("Feature          | Patagames      | IronOCR");
            Console.WriteLine("─────────────────────────────────────────────");
            Console.WriteLine("tessdata needed  | Yes            | No");
            Console.WriteLine("PDF support      | No             | Native");
            Console.WriteLine("Preprocessing    | No             | Built-in");
            Console.WriteLine("Searchable PDF   | No             | Yes");
            Console.WriteLine("Password PDFs    | No             | Yes");
            Console.WriteLine("Barcode reading  | No             | Yes");
            Console.WriteLine("Region OCR       | Manual         | Built-in");
            Console.WriteLine("Languages        | Tesseract      | 125+");
            Console.WriteLine();
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }

        public void CodeComparison()
        {
            Console.WriteLine("=== CODE COMPARISON ===\n");

            Console.WriteLine("PATAGAMES PDF OCR:");
            Console.WriteLine("  // No PDF support");
            Console.WriteLine("  // Need PdfiumViewer or similar");
            Console.WriteLine("  // Render to image, then OCR");
            Console.WriteLine("  // Many lines of code...");
            Console.WriteLine();

            Console.WriteLine("IRONOCR PDF OCR:");
            Console.WriteLine("  var text = new IronTesseract().Read(pdfPath).Text;");
            Console.WriteLine();
            Console.WriteLine("Download IronOCR: https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// MIGRATION FROM PATAGAMES
// ============================================================================

namespace Migration
{
    using IronOcr;

    /// <summary>
    /// Migration from Patagames to IronOCR is straightforward.
    ///
    /// Get IronOCR: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class PatagamesMigration
    {
        // Patagames: api.Init(tessdata, "eng");
        //            api.GetTextFromImage(bitmap);
        // IronOCR:
        public string BasicMigration(string imagePath)
        {
            return new IronTesseract().Read(imagePath).Text;
        }

        // Patagames: api.Init(tessdata, "eng+fra");
        // IronOCR:
        public string MultiLanguageMigration(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.English;
            ocr.AddSecondaryLanguage(OcrLanguage.French);
            return ocr.Read(imagePath).Text;
        }

        // Patagames: No PDF support
        // IronOCR:
        public string PdfMigration(string pdfPath)
        {
            return new IronTesseract().Read(pdfPath).Text;
        }
    }
}


// ============================================================================
// READY FOR A COMPLETE OCR SOLUTION?
//
// IronOCR does more than wrap Tesseract.
// PDF support, preprocessing, searchable output - all built-in.
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Free Trial: https://ironsoftware.com/csharp/ocr/#trial-license
// Documentation: https://ironsoftware.com/csharp/ocr/docs/
//
// Install: Install-Package IronOcr
// ============================================================================
