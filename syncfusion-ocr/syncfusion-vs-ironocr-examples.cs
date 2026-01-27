/**
 * Syncfusion PDF OCR vs IronOCR: Code Examples
 *
 * Compare Syncfusion's Tesseract-based OCR with IronOCR.
 * Syncfusion requires tessdata; IronOCR is self-contained.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * NuGet Packages:
 * - Syncfusion.PDF.OCR.Net.Core (Syncfusion)
 * - IronOcr (IronOCR) - https://www.nuget.org/packages/IronOcr/
 */

using System;
using System.Text;

// ============================================================================
// SYNCFUSION IMPLEMENTATION - Tesseract Under the Hood
// ============================================================================

namespace SyncfusionExamples
{
    /*
    using Syncfusion.OCRProcessor;
    using Syncfusion.Pdf;
    using Syncfusion.Pdf.Parsing;

    /// <summary>
    /// Syncfusion requires:
    /// 1. tessdata folder with traineddata files
    /// 2. Separate OCRProcessor initialization
    /// 3. Two-step: OCR then extract
    /// </summary>
    public class SyncfusionOcrService
    {
        private const string TessDataPath = @"tessdata/";

        /// <summary>
        /// PDF OCR with Syncfusion
        /// </summary>
        public string ExtractFromPdf(string pdfPath)
        {
            using var document = new PdfLoadedDocument(pdfPath);
            using var processor = new OCRProcessor(TessDataPath);

            // Step 1: Perform OCR
            processor.PerformOCR(document);

            // Step 2: Extract text
            var text = new StringBuilder();
            foreach (PdfLoadedPage page in document.Pages)
            {
                text.AppendLine(page.ExtractText());
            }

            return text.ToString();
        }
    }
    */

    public class SyncfusionPlaceholder
    {
        public void ShowRequirements()
        {
            Console.WriteLine("Syncfusion PDF OCR Requirements:");
            Console.WriteLine("1. Download tessdata files");
            Console.WriteLine("2. Configure tessdata path");
            Console.WriteLine("3. Two-step process: OCR then Extract");
            Console.WriteLine("4. Community license restrictions ($1M revenue)");
        }
    }
}


// ============================================================================
// IRONOCR - SIMPLER, NO TESSDATA
// Get it: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace IronOcrExamples
{
    using IronOcr;

    /// <summary>
    /// IronOCR requires no tessdata management.
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class IronOcrService
    {
        /// <summary>
        /// PDF OCR - one line, no tessdata
        /// </summary>
        public string ExtractFromPdf(string pdfPath)
        {
            return new IronTesseract().Read(pdfPath).Text;
        }

        /// <summary>
        /// Images work the same way
        /// </summary>
        public string ExtractFromImage(string imagePath)
        {
            return new IronTesseract().Read(imagePath).Text;
        }
    }
}


// ============================================================================
// COMPARISON
// ============================================================================

namespace Comparison
{
    public class SyncfusionVsIronOcrComparison
    {
        public void Compare()
        {
            Console.WriteLine("=== SYNCFUSION vs IRONOCR ===\n");

            Console.WriteLine("Feature          | Syncfusion     | IronOCR");
            Console.WriteLine("─────────────────────────────────────────────");
            Console.WriteLine("tessdata needed  | Yes            | No");
            Console.WriteLine("Image OCR        | Via PDF        | Native");
            Console.WriteLine("PDF OCR          | Yes            | Yes");
            Console.WriteLine("Password PDFs    | Complex        | Built-in");
            Console.WriteLine("Searchable PDF   | Manual         | Built-in");
            Console.WriteLine("Free tier        | $1M limit      | Trial");
            Console.WriteLine();
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// TRY IRONOCR TODAY
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// ============================================================================
