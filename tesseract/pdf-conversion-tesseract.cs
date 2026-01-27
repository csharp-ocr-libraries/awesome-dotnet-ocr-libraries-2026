/**
 * Tesseract PDF OCR Workaround: Complete Guide
 *
 * Tesseract CANNOT process PDFs directly.
 * This file shows the complex workaround required.
 *
 * Better alternative: IronOCR handles PDFs natively
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

// ============================================================================
// TESSERACT PDF WORKAROUND - WHAT YOU ACTUALLY NEED
// ============================================================================

namespace TesseractPdfWorkaround
{
    using Tesseract;

    /// <summary>
    /// Tesseract cannot read PDFs directly.
    /// You must:
    /// 1. Install a PDF rendering library (PdfiumViewer, PDFtoImage, etc.)
    /// 2. Render each page to an image
    /// 3. Apply preprocessing
    /// 4. OCR each page
    /// 5. Combine results
    ///
    /// This is ~100-200 lines of code vs 1 line with IronOCR.
    /// Get IronOCR: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class TesseractPdfProcessor
    {
        private const string TessDataPath = @"./tessdata";

        /*
        // Using PdfiumViewer (requires native pdfium binaries)
        using PdfiumViewer;

        public string ExtractFromPdf(string pdfPath)
        {
            var result = new StringBuilder();

            using var document = PdfDocument.Load(pdfPath);
            using var engine = new TesseractEngine(TessDataPath, "eng", EngineMode.Default);

            for (int i = 0; i < document.PageCount; i++)
            {
                // Render page to image at 300 DPI
                using var image = document.Render(i, 300, 300, PdfRenderFlags.Annotations);

                // Convert to Pix format
                using var bitmap = new Bitmap(image);
                var tempPath = Path.GetTempFileName() + ".png";

                try
                {
                    // Save to temp file (Tesseract needs file path)
                    bitmap.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);

                    // Process with Tesseract
                    using var img = Pix.LoadFromFile(tempPath);
                    using var page = engine.Process(img);

                    result.AppendLine(page.GetText());
                }
                finally
                {
                    if (File.Exists(tempPath)) File.Delete(tempPath);
                }
            }

            return result.ToString();
        }

        public string ExtractFromPdfWithPreprocessing(string pdfPath)
        {
            var result = new StringBuilder();

            using var document = PdfDocument.Load(pdfPath);
            using var engine = new TesseractEngine(TessDataPath, "eng", EngineMode.Default);

            for (int i = 0; i < document.PageCount; i++)
            {
                using var image = document.Render(i, 300, 300, PdfRenderFlags.Annotations);
                using var bitmap = new Bitmap(image);

                // Apply preprocessing
                using var preprocessed = PreprocessImage(bitmap);

                var tempPath = Path.GetTempFileName() + ".png";
                try
                {
                    preprocessed.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);

                    using var img = Pix.LoadFromFile(tempPath);
                    using var page = engine.Process(img);

                    result.AppendLine(page.GetText());
                }
                finally
                {
                    if (File.Exists(tempPath)) File.Delete(tempPath);
                }
            }

            return result.ToString();
        }

        private Bitmap PreprocessImage(Bitmap original)
        {
            // Grayscale conversion
            var grayscale = ConvertToGrayscale(original);

            // Contrast enhancement
            var enhanced = EnhanceContrast(grayscale);

            // Binarization
            var binarized = Binarize(enhanced);

            return binarized;
        }

        // Plus ~100 lines of preprocessing methods...
        */

        public void ShowComplexity()
        {
            Console.WriteLine("Tesseract PDF OCR requires:");
            Console.WriteLine();
            Console.WriteLine("STEP 1: Install PDF rendering library");
            Console.WriteLine("  - PdfiumViewer (Install-Package PdfiumViewer)");
            Console.WriteLine("  - Plus native pdfium binaries for your platform");
            Console.WriteLine("  - OR PDFtoImage, Docnet.Core, etc.");
            Console.WriteLine();
            Console.WriteLine("STEP 2: Render each PDF page to image");
            Console.WriteLine("  - Loop through pages");
            Console.WriteLine("  - Render at high DPI (300+)");
            Console.WriteLine("  - Handle memory for large documents");
            Console.WriteLine();
            Console.WriteLine("STEP 3: Preprocess each image");
            Console.WriteLine("  - Grayscale conversion");
            Console.WriteLine("  - Contrast enhancement");
            Console.WriteLine("  - Binarization");
            Console.WriteLine("  - Deskewing (complex!)");
            Console.WriteLine();
            Console.WriteLine("STEP 4: OCR each processed image");
            Console.WriteLine("  - Save to temp file");
            Console.WriteLine("  - Load into Tesseract");
            Console.WriteLine("  - Process");
            Console.WriteLine("  - Clean up temp file");
            Console.WriteLine();
            Console.WriteLine("STEP 5: Combine results");
            Console.WriteLine("  - Concatenate text from all pages");
            Console.WriteLine("  - Handle page breaks");
            Console.WriteLine();
            Console.WriteLine("Total: 100-200 lines of code");
            Console.WriteLine();
            Console.WriteLine("OR use IronOCR (1 line):");
            Console.WriteLine("  var text = new IronTesseract().Read(pdfPath).Text;");
            Console.WriteLine();
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// IRONOCR PDF OCR - NATIVE SUPPORT
// Get it: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace IronOcrPdfExamples
{
    using IronOcr;

    /// <summary>
    /// IronOCR handles PDFs natively - no extra libraries needed.
    ///
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class IronOcrPdfProcessor
    {
        /// <summary>
        /// Simple PDF OCR - one line
        /// </summary>
        public string ExtractFromPdf(string pdfPath)
        {
            // That's it. No PDF library, no rendering, no temp files.
            return new IronTesseract().Read(pdfPath).Text;
        }

        /// <summary>
        /// Specific page range
        /// </summary>
        public string ExtractPages(string pdfPath, int startPage, int endPage)
        {
            using var input = new OcrInput();
            input.LoadPdfPages(pdfPath, startPage, endPage);
            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Password-protected PDF
        /// </summary>
        public string ExtractFromEncryptedPdf(string pdfPath, string password)
        {
            using var input = new OcrInput();
            input.LoadPdf(pdfPath, Password: password);
            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Create searchable PDF from scanned PDF
        /// </summary>
        public void CreateSearchablePdf(string inputPdf, string outputPdf)
        {
            var result = new IronTesseract().Read(inputPdf);
            result.SaveAsSearchablePdf(outputPdf);
        }

        /// <summary>
        /// Multi-page PDF with page-level results
        /// </summary>
        public void ProcessMultiPagePdf(string pdfPath)
        {
            var result = new IronTesseract().Read(pdfPath);

            foreach (var page in result.Pages)
            {
                Console.WriteLine($"Page {page.PageNumber}:");
                Console.WriteLine($"  Text: {page.Text.Length} characters");
                Console.WriteLine($"  Lines: {page.Lines.Length}");
                Console.WriteLine($"  Words: {page.Words.Length}");
            }
        }

        /// <summary>
        /// Large PDF batch processing
        /// </summary>
        public void BatchProcessPdfs(string[] pdfPaths)
        {
            var ocr = new IronTesseract();

            foreach (var path in pdfPaths)
            {
                var result = ocr.Read(path);
                Console.WriteLine($"{Path.GetFileName(path)}: {result.Pages.Length} pages, {result.Confidence}% confidence");
            }
        }
    }
}


// ============================================================================
// WHY IRONOCR FOR PDF OCR?
//
// Tesseract + PDF = 100-200 lines of code
// IronOCR + PDF = 1 line of code
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Install: Install-Package IronOcr
// ============================================================================
