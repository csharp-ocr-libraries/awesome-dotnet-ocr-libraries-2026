/**
 * IronOCR PDF Processing: Complete Guide
 *
 * Everything you need for PDF OCR with IronOCR.
 * Native support - no additional libraries required.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 * Documentation: https://ironsoftware.com/csharp/ocr/docs/
 */

using System;
using System.IO;
using IronOcr;

namespace IronOcrPdfExamples
{
    // ========================================================================
    // BASIC PDF OCR
    // ========================================================================

    public class BasicPdfOcr
    {
        /// <summary>
        /// Simple PDF OCR - one line
        /// </summary>
        public void SimplePdfOcr()
        {
            string text = new IronTesseract().Read("document.pdf").Text;
            Console.WriteLine(text);
        }

        /// <summary>
        /// PDF OCR with result object
        /// </summary>
        public void PdfOcrWithDetails()
        {
            var ocr = new IronTesseract();
            var result = ocr.Read("document.pdf");

            Console.WriteLine($"Text: {result.Text}");
            Console.WriteLine($"Confidence: {result.Confidence}%");
            Console.WriteLine($"Pages: {result.Pages.Length}");
        }

        /// <summary>
        /// Process multiple PDFs
        /// </summary>
        public void BatchPdfOcr(string[] pdfPaths)
        {
            var ocr = new IronTesseract();

            foreach (var path in pdfPaths)
            {
                var result = ocr.Read(path);
                Console.WriteLine($"{Path.GetFileName(path)}: {result.Text.Length} chars");
            }
        }
    }

    // ========================================================================
    // PAGE SELECTION
    // ========================================================================

    public class PdfPageSelection
    {
        /// <summary>
        /// Specific page range
        /// </summary>
        public void SpecificPageRange()
        {
            using var input = new OcrInput();
            input.LoadPdf("large-document.pdf");
            // TODO: verify IronOCR API for page range selection (pages 1-5)

            var result = new IronTesseract().Read(input);
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Single page
        /// </summary>
        public void SinglePage()
        {
            using var input = new OcrInput();
            input.LoadPdf("document.pdf");
            // TODO: verify IronOCR API for single page selection (page 3)

            var result = new IronTesseract().Read(input);
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// First N pages
        /// </summary>
        public void FirstNPages(string pdfPath, int n)
        {
            using var input = new OcrInput();
            input.LoadPdf(pdfPath);
            // TODO: verify IronOCR API for page range selection (first n pages)

            var result = new IronTesseract().Read(input);
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Process pages individually
        /// </summary>
        public void ProcessPagesIndividually(string pdfPath)
        {
            var ocr = new IronTesseract();
            var result = ocr.Read(pdfPath);

            foreach (var page in result.Pages)
            {
                Console.WriteLine($"=== Page {page.PageNumber} ===");
                Console.WriteLine($"Characters: {page.Text.Length}");
                Console.WriteLine($"Lines: {page.Lines.Length}");
                Console.WriteLine($"Words: {page.Words.Length}");
                Console.WriteLine(page.Text.Substring(0, Math.Min(200, page.Text.Length)));
                Console.WriteLine();
            }
        }
    }

    // ========================================================================
    // PASSWORD-PROTECTED PDFs
    // ========================================================================

    public class EncryptedPdfOcr
    {
        /// <summary>
        /// Open password-protected PDF
        /// </summary>
        public void ReadEncryptedPdf()
        {
            using var input = new OcrInput();
            input.LoadPdf("secure.pdf", Password: "secret123");

            var result = new IronTesseract().Read(input);
            Console.WriteLine(result.Text);
        }

        /// <summary>
        /// Try multiple passwords
        /// </summary>
        public string TryPasswords(string pdfPath, string[] passwords)
        {
            foreach (var password in passwords)
            {
                try
                {
                    using var input = new OcrInput();
                    input.LoadPdf(pdfPath, Password: password);

                    var result = new IronTesseract().Read(input);
                    return result.Text;
                }
                catch
                {
                    continue;
                }
            }

            throw new Exception("None of the passwords worked");
        }
    }

    // ========================================================================
    // SEARCHABLE PDF OUTPUT
    // ========================================================================

    public class SearchablePdfCreation
    {
        /// <summary>
        /// Create searchable PDF from scanned PDF
        /// </summary>
        public void CreateSearchablePdf()
        {
            var result = new IronTesseract().Read("scanned.pdf");
            result.SaveAsSearchablePdf("searchable-output.pdf");
        }

        /// <summary>
        /// Create searchable PDF from images
        /// </summary>
        public void CreateSearchablePdfFromImages(string[] imagePaths)
        {
            using var input = new OcrInput();
            foreach (var path in imagePaths)
            {
                input.LoadImage(path);
            }

            var result = new IronTesseract().Read(input);
            result.SaveAsSearchablePdf("combined-searchable.pdf");
        }

        /// <summary>
        /// Create searchable PDF with preprocessing
        /// </summary>
        public void CreateHighQualitySearchablePdf(string inputPdf)
        {
            using var input = new OcrInput();
            input.LoadPdf(inputPdf);
            input.Deskew();
            input.DeNoise();

            var result = new IronTesseract().Read(input);
            result.SaveAsSearchablePdf("high-quality-searchable.pdf");
        }
    }

    // ========================================================================
    // PDF WITH PREPROCESSING
    // ========================================================================

    public class PdfPreprocessing
    {
        /// <summary>
        /// Full preprocessing pipeline for scanned PDFs
        /// </summary>
        public string ProcessScannedPdf(string pdfPath)
        {
            using var input = new OcrInput();
            input.LoadPdf(pdfPath);

            // Apply preprocessing
            input.Deskew();              // Fix rotation
            input.DeNoise();             // Remove specks
            input.Contrast();            // Enhance contrast
            input.EnhanceResolution(300); // Ensure 300 DPI

            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// Process low-quality fax PDFs
        /// </summary>
        public string ProcessFaxPdf(string pdfPath)
        {
            using var input = new OcrInput();
            input.LoadPdf(pdfPath);

            input.DeNoise();             // Faxes are noisy
            input.Binarize();            // Convert to black/white
            input.Contrast();

            return new IronTesseract().Read(input).Text;
        }
    }

    // ========================================================================
    // PDF INPUT SOURCES
    // ========================================================================

    public class PdfInputSources
    {
        /// <summary>
        /// From file path
        /// </summary>
        public string FromFilePath(string pdfPath)
        {
            return new IronTesseract().Read(pdfPath).Text;
        }

        /// <summary>
        /// From byte array
        /// </summary>
        public string FromBytes(byte[] pdfBytes)
        {
            using var input = new OcrInput();
            input.LoadPdf(pdfBytes);
            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// From stream
        /// </summary>
        public string FromStream(Stream pdfStream)
        {
            using var input = new OcrInput();
            using var ms = new MemoryStream();
            pdfStream.CopyTo(ms);
            input.LoadPdf(ms.ToArray());
            return new IronTesseract().Read(input).Text;
        }
    }
}


// ============================================================================
// IRONOCR - THE BEST PDF OCR FOR .NET
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// PDF Guide: https://ironsoftware.com/csharp/ocr/tutorials/csharp-ocr-pdf/
// ============================================================================
