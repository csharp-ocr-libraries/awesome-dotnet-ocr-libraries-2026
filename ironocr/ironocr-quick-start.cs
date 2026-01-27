/**
 * IronOCR Quick Start Guide
 *
 * Everything you need to get started with IronOCR in C#/.NET.
 *
 * INSTALLATION:
 * Install-Package IronOcr
 *
 * DOWNLOAD:
 * https://ironsoftware.com/csharp/ocr/
 *
 * DOCUMENTATION:
 * https://ironsoftware.com/csharp/ocr/docs/
 *
 * NUGET:
 * https://www.nuget.org/packages/IronOcr/
 */

using System;
using System.IO;
using IronOcr;

namespace IronOcrQuickStart
{
    // ========================================================================
    // BASIC OCR - GET STARTED IN 1 LINE
    // ========================================================================

    public class BasicOcr
    {
        /// <summary>
        /// The simplest possible OCR - one line of code
        /// </summary>
        public void SimpleOcr()
        {
            string text = new IronTesseract().Read("document.jpg").Text;
            Console.WriteLine(text);
        }

        /// <summary>
        /// With result object for more details
        /// </summary>
        public void OcrWithDetails()
        {
            var ocr = new IronTesseract();
            var result = ocr.Read("document.jpg");

            Console.WriteLine($"Text: {result.Text}");
            Console.WriteLine($"Confidence: {result.Confidence}%");
            Console.WriteLine($"Pages: {result.Pages.Length}");
        }
    }

    // ========================================================================
    // PDF OCR - NATIVE SUPPORT
    // ========================================================================

    public class PdfOcr
    {
        /// <summary>
        /// Direct PDF processing - no conversion needed
        /// </summary>
        public void SimplePdfOcr()
        {
            string text = new IronTesseract().Read("document.pdf").Text;
            Console.WriteLine(text);
        }

        /// <summary>
        /// Specific page range
        /// </summary>
        public void PdfPageRange()
        {
            using var input = new OcrInput();
            input.LoadPdf("large-document.pdf");
            // TODO: verify IronOCR API for page range selection

            var result = new IronTesseract().Read(input);
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Password-protected PDFs
        /// </summary>
        public void EncryptedPdf()
        {
            using var input = new OcrInput();
            input.LoadPdf("secure.pdf", Password: "secret123");

            var result = new IronTesseract().Read(input);
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Create searchable PDF from scanned PDF
        /// </summary>
        public void CreateSearchablePdf()
        {
            var result = new IronTesseract().Read("scanned.pdf");
            result.SaveAsSearchablePdf("searchable-output.pdf");
        }
    }

    // ========================================================================
    // IMAGE PREPROCESSING - AUTOMATIC OR MANUAL
    // ========================================================================

    public class Preprocessing
    {
        /// <summary>
        /// Automatic preprocessing (default) - IronOCR detects and fixes issues
        /// </summary>
        public void AutomaticPreprocessing()
        {
            // This automatically applies:
            // - Rotation correction
            // - Noise removal
            // - Contrast enhancement
            // - Resolution normalization
            var text = new IronTesseract().Read("low-quality-scan.jpg").Text;
        }

        /// <summary>
        /// Explicit preprocessing for challenging images
        /// </summary>
        public void ExplicitPreprocessing()
        {
            using var input = new OcrInput();
            input.LoadImage("problem-document.jpg");

            // Apply specific filters
            input.Deskew();              // Correct rotation
            input.DeNoise();             // Remove noise/specks
            input.Contrast();            // Enhance contrast
            input.Binarize();            // Convert to black/white
            input.EnhanceResolution(300); // Scale to 300 DPI

            var result = new IronTesseract().Read(input);
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// For inverted images (white text on dark background)
        /// </summary>
        public void InvertedImage()
        {
            using var input = new OcrInput();
            input.LoadImage("inverted.jpg");
            input.Invert();

            var result = new IronTesseract().Read(input);
            Console.WriteLine(result.Text);
        }
    }

    // ========================================================================
    // MULTI-LANGUAGE OCR
    // ========================================================================

    public class MultiLanguage
    {
        /// <summary>
        /// French OCR
        /// Install: Install-Package IronOcr.Languages.French
        /// </summary>
        public void FrenchOcr()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.French;

            var result = ocr.Read("french-document.jpg");
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Multiple languages in one document
        /// </summary>
        public void MultiLanguageDocument()
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.English;
            ocr.AddSecondaryLanguage(OcrLanguage.French);
            ocr.AddSecondaryLanguage(OcrLanguage.German);

            var result = ocr.Read("multilingual.jpg");
            Console.WriteLine(result.Text);
        }
    }

    // ========================================================================
    // STRUCTURED DATA EXTRACTION
    // ========================================================================

    public class StructuredExtraction
    {
        /// <summary>
        /// Access document structure
        /// </summary>
        public void DocumentStructure()
        {
            var result = new IronTesseract().Read("document.jpg");

            // Pages
            foreach (var page in result.Pages)
            {
                Console.WriteLine($"Page {page.PageNumber}: {page.Text.Length} chars");
            }

            // Paragraphs
            foreach (var paragraph in result.Paragraphs)
            {
                Console.WriteLine($"Paragraph: {paragraph.Text}");
            }

            // Lines
            foreach (var line in result.Lines)
            {
                Console.WriteLine($"Line at Y={line.Y}: {line.Text}");
            }

            // Words with positions
            foreach (var word in result.Words)
            {
                Console.WriteLine($"'{word.Text}' at ({word.X},{word.Y}) - {word.Confidence}%");
            }
        }

        /// <summary>
        /// Region-based OCR
        /// </summary>
        public void ExtractRegion()
        {
            using var input = new OcrInput();

            // Only read specific area
            var region = new System.Drawing.Rectangle(0, 0, 500, 100);
            input.LoadImage("document.jpg", region);

            var result = new IronTesseract().Read(input);
            Console.WriteLine($"Header: {result.Text}");
        }
    }

    // ========================================================================
    // BARCODE READING
    // ========================================================================

    public class BarcodeReading
    {
        /// <summary>
        /// Read barcodes during OCR
        /// </summary>
        public void ReadBarcodes()
        {
            var ocr = new IronTesseract();
            ocr.Configuration.ReadBarCodes = true;

            var result = ocr.Read("document-with-barcode.jpg");

            foreach (var barcode in result.Barcodes)
            {
                Console.WriteLine($"Barcode: {barcode.Value} ({barcode.Format})");
            }
        }
    }

    // ========================================================================
    // INPUT SOURCES
    // ========================================================================

    public class InputSources
    {
        /// <summary>
        /// All supported input types
        /// </summary>
        public void AllInputTypes()
        {
            var ocr = new IronTesseract();

            // From file path
            var result1 = ocr.Read("document.jpg");

            // From byte array
            byte[] bytes = File.ReadAllBytes("document.jpg");
            using var input2 = new OcrInput();
            input2.LoadImage(bytes);
            var result2 = ocr.Read(input2);

            // From stream
            using var stream = File.OpenRead("document.jpg");
            using var input3 = new OcrInput();
            input3.LoadImage(stream);
            var result3 = ocr.Read(input3);

            // From URL
            using var input4 = new OcrInput();
            input4.AddImage("https://example.com/document.jpg");
            var result4 = ocr.Read(input4);

            // From PDF
            var result5 = ocr.Read("document.pdf");
        }
    }

    // ========================================================================
    // LICENSE CONFIGURATION
    // ========================================================================

    public class LicenseSetup
    {
        /// <summary>
        /// Set license at application startup
        /// </summary>
        public void ConfigureLicense()
        {
            // Option 1: Direct assignment
            IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

            // Option 2: From environment variable
            IronOcr.License.LicenseKey = Environment.GetEnvironmentVariable("IRONOCR_LICENSE");

            // Option 3: From file
            IronOcr.License.LicenseKey = File.ReadAllText("license.txt");
        }
    }
}


// ============================================================================
// RESOURCES
//
// Download IronOCR: https://ironsoftware.com/csharp/ocr/
// NuGet Package: https://www.nuget.org/packages/IronOcr/
// Documentation: https://ironsoftware.com/csharp/ocr/docs/
// Tutorials: https://ironsoftware.com/csharp/ocr/tutorials/
// Free Trial: https://ironsoftware.com/csharp/ocr/#trial-license
// Pricing: https://ironsoftware.com/csharp/ocr/#pricing
//
// INSTALL NOW:
// Install-Package IronOcr
// ============================================================================
