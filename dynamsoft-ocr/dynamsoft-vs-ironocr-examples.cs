/**
 * Dynamsoft OCR vs IronOCR: Code Examples
 *
 * Compare Dynamsoft's OCR SDK with IronOCR.
 * Both offer on-premise OCR; IronOCR has simpler API.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * NuGet Packages:
 * - Dynamsoft.DotNet.OCR (Dynamsoft)
 * - IronOcr (IronOCR) - https://www.nuget.org/packages/IronOcr/
 */

using System;

// ============================================================================
// DYNAMSOFT OCR IMPLEMENTATION
// ============================================================================

namespace DynamsoftExamples
{
    /*
    using Dynamsoft.OCR;

    /// <summary>
    /// Dynamsoft OCR requires:
    /// 1. SDK license
    /// 2. Specific initialization pattern
    /// 3. Manual resource management
    /// </summary>
    public class DynamsoftOcrService : IDisposable
    {
        private readonly DynamsoftLabelRecognizer _recognizer;

        public DynamsoftOcrService(string licenseKey)
        {
            DynamsoftLabelRecognizer.InitLicense(licenseKey);
            _recognizer = new DynamsoftLabelRecognizer();
        }

        public string ExtractText(string imagePath)
        {
            var results = _recognizer.RecognizeByFile(imagePath, "");
            string text = "";

            foreach (var result in results)
            {
                foreach (var lineResult in result.LineResults)
                {
                    text += lineResult.Text + "\n";
                }
            }

            return text;
        }

        public void Dispose()
        {
            _recognizer?.Dispose();
        }
    }
    */

    public class DynamsoftPlaceholder
    {
        public void ShowInfo()
        {
            Console.WriteLine("Dynamsoft OCR:");
            Console.WriteLine("1. Primarily focused on barcode/label reading");
            Console.WriteLine("2. OCR is secondary feature");
            Console.WriteLine("3. Good for structured text (labels, IDs)");
            Console.WriteLine("4. Less suited for full document OCR");
            Console.WriteLine();
            Console.WriteLine("For full document OCR, try IronOCR:");
            Console.WriteLine("https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// IRONOCR - FULL DOCUMENT OCR
// Get it: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace IronOcrExamples
{
    using IronOcr;

    /// <summary>
    /// IronOCR is designed for full document OCR:
    /// - Multi-page PDFs
    /// - Complex layouts
    /// - Mixed content (text + images)
    /// - Handwriting recognition
    ///
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class IronOcrService
    {
        /// <summary>
        /// Full page text extraction
        /// </summary>
        public string ExtractFullPage(string imagePath)
        {
            return new IronTesseract().Read(imagePath).Text;
        }

        /// <summary>
        /// Multi-page PDF processing
        /// </summary>
        public string ProcessMultiPagePdf(string pdfPath)
        {
            var result = new IronTesseract().Read(pdfPath);

            foreach (var page in result.Pages)
            {
                Console.WriteLine($"Page {page.PageNumber}: {page.Text.Length} characters");
            }

            return result.Text;
        }

        /// <summary>
        /// Specific regions (for labels, barcodes, etc.)
        /// </summary>
        public string ExtractLabelRegion(string imagePath, int x, int y, int w, int h)
        {
            using var input = new OcrInput();
            var region = new System.Drawing.Rectangle(x, y, w, h);
            input.LoadImage(imagePath, region);

            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Combined text and barcode reading
        /// </summary>
        public void ReadTextAndBarcodes(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Configuration.ReadBarCodes = true;

            var result = ocr.Read(imagePath);

            Console.WriteLine("Text:");
            Console.WriteLine(result.Text);

            Console.WriteLine("\nBarcodes:");
            foreach (var barcode in result.Barcodes)
            {
                Console.WriteLine($"  {barcode.Format}: {barcode.Value}");
            }
        }

        /// <summary>
        /// Complex document with preprocessing
        /// </summary>
        public string ProcessComplexDocument(string imagePath)
        {
            using var input = new OcrInput();
            input.LoadImage(imagePath);
            input.Deskew();
            input.DeNoise();
            input.Contrast();

            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Word-level extraction with positions
        /// </summary>
        public void ExtractWordsWithPositions(string imagePath)
        {
            var result = new IronTesseract().Read(imagePath);

            foreach (var word in result.Words)
            {
                Console.WriteLine($"'{word.Text}' at ({word.X},{word.Y}) - Confidence: {word.Confidence}%");
            }
        }
    }
}


// ============================================================================
// COMPARISON
// ============================================================================

namespace Comparison
{
    public class DynamsoftVsIronOcrComparison
    {
        public void Compare()
        {
            Console.WriteLine("=== DYNAMSOFT vs IRONOCR ===\n");

            Console.WriteLine("Feature          | Dynamsoft      | IronOCR");
            Console.WriteLine("─────────────────────────────────────────────");
            Console.WriteLine("Primary focus    | Labels/Barcodes| Full documents");
            Console.WriteLine("PDF support      | Limited        | Native");
            Console.WriteLine("Document OCR     | Basic          | Advanced");
            Console.WriteLine("Preprocessing    | Limited        | Built-in");
            Console.WriteLine("Searchable PDF   | No             | Yes");
            Console.WriteLine("Languages        | Limited        | 125+");
            Console.WriteLine("Barcode reading  | Excellent      | Good");
            Console.WriteLine();
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }

        public void UseCaseComparison()
        {
            Console.WriteLine("=== USE CASE COMPARISON ===\n");

            Console.WriteLine("DYNAMSOFT excels at:");
            Console.WriteLine("  - Barcode scanning");
            Console.WriteLine("  - Label recognition");
            Console.WriteLine("  - Structured text extraction");
            Console.WriteLine();

            Console.WriteLine("IRONOCR excels at:");
            Console.WriteLine("  - Full document OCR");
            Console.WriteLine("  - PDF processing");
            Console.WriteLine("  - Multi-page documents");
            Console.WriteLine("  - Complex layouts");
            Console.WriteLine("  - Handwriting recognition");
            Console.WriteLine("  - Searchable PDF creation");
            Console.WriteLine();
            Console.WriteLine("Download: https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// NEED FULL DOCUMENT OCR?
//
// IronOCR handles complete documents, not just labels.
// PDFs, multi-page scans, complex layouts - all supported.
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Free Trial: https://ironsoftware.com/csharp/ocr/#trial-license
//
// Install: Install-Package IronOcr
// ============================================================================
