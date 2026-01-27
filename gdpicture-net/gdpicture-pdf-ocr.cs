/**
 * GdPicture.NET PDF OCR Processing Comparison
 *
 * This file demonstrates PDF OCR processing differences between
 * GdPicture.NET and IronOCR, focusing on areas where GdPicture's
 * image ID management is most critical.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * PDF OCR scenarios covered:
 * 1. Basic PDF OCR with proper cleanup
 * 2. Page-by-page processing
 * 3. Memory leak demonstration
 * 4. Searchable PDF creation
 * 5. Password-protected PDFs
 * 6. Batch PDF processing
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// ============================================================================
// SCENARIO 1: BASIC PDF OCR WITH PROPER CLEANUP
// GdPicture requires image ID tracking for each rendered page
// ============================================================================

namespace PdfOcr_BasicProcessing
{
    // GDPICTURE: Complex flow with multiple cleanup points
    namespace GdPictureApproach
    {
        using GdPicture14;

        public class GdPicturePdfOcr
        {
            private GdPictureImaging _imaging;
            private GdPictureOCR _ocr;

            public GdPicturePdfOcr()
            {
                var lm = new LicenseManager();
                lm.RegisterKEY("LICENSE-KEY");

                _imaging = new GdPictureImaging();
                _ocr = new GdPictureOCR();
                _ocr.ResourceFolder = @"C:\GdPicture\Resources\OCR";
            }

            public string ExtractTextFromPdf(string pdfPath)
            {
                var text = new StringBuilder();

                // GdPicturePDF requires separate PDF plugin license
                using var pdf = new GdPicturePDF();

                // Load PDF - check status
                GdPictureStatus status = pdf.LoadFromFile(pdfPath, false);

                if (status != GdPictureStatus.OK)
                {
                    throw new Exception($"Failed to load PDF: {status}");
                }

                int pageCount = pdf.GetPageCount();

                for (int i = 1; i <= pageCount; i++)
                {
                    // Select page
                    pdf.SelectPage(i);

                    // Render page to image - returns integer ID
                    // 200 DPI is typical for OCR quality
                    int imageId = pdf.RenderPageToGdPictureImage(200, false);

                    // Check if render succeeded
                    if (imageId == 0)
                    {
                        Console.WriteLine($"Warning: Failed to render page {i}");
                        continue;
                    }

                    try
                    {
                        // Set image for OCR
                        _ocr.SetImage(imageId);
                        _ocr.Language = "eng";

                        // Run OCR
                        string resultId = _ocr.RunOCR();

                        if (!string.IsNullOrEmpty(resultId))
                        {
                            text.AppendLine($"--- Page {i} ---");
                            text.AppendLine(_ocr.GetOCRResultText(resultId));
                        }
                    }
                    finally
                    {
                        // CRITICAL: Release the rendered page image
                        // Each page render creates a new image in memory
                        // Without this, memory grows with each page
                        _imaging.ReleaseGdPictureImage(imageId);
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

    // IRONOCR: Simple one-liner
    namespace IronOcrApproach
    {
        using IronOcr;

        public class IronOcrPdfOcr
        {
            public string ExtractTextFromPdf(string pdfPath)
            {
                // One line - handles page iteration, cleanup, etc.
                return new IronTesseract().Read(pdfPath).Text;
            }

            // With explicit page access if needed
            public string ExtractTextWithPageInfo(string pdfPath)
            {
                var result = new IronTesseract().Read(pdfPath);
                var text = new StringBuilder();

                foreach (var page in result.Pages)
                {
                    text.AppendLine($"--- Page {page.PageNumber} ---");
                    text.AppendLine(page.Text);
                }

                return text.ToString();
            }
        }
    }
}


// ============================================================================
// SCENARIO 2: PAGE-BY-PAGE PROCESSING WITH SELECTIVE PAGES
// Processing specific page ranges
// ============================================================================

namespace PdfOcr_SelectivePages
{
    // GDPICTURE: Manual page iteration with cleanup
    namespace GdPictureApproach
    {
        using GdPicture14;

        public class GdPicturePageSelection
        {
            private GdPictureImaging _imaging;
            private GdPictureOCR _ocr;

            public Dictionary<int, string> ProcessPages(string pdfPath, int[] pageNumbers)
            {
                var results = new Dictionary<int, string>();
                var processedImageIds = new List<int>();

                using var pdf = new GdPicturePDF();
                pdf.LoadFromFile(pdfPath, false);

                try
                {
                    foreach (int pageNum in pageNumbers)
                    {
                        // Validate page number
                        if (pageNum < 1 || pageNum > pdf.GetPageCount())
                        {
                            Console.WriteLine($"Invalid page number: {pageNum}");
                            continue;
                        }

                        pdf.SelectPage(pageNum);
                        int imageId = pdf.RenderPageToGdPictureImage(200, false);

                        if (imageId != 0)
                        {
                            processedImageIds.Add(imageId);

                            _ocr.SetImage(imageId);
                            _ocr.Language = "eng";
                            var resultId = _ocr.RunOCR();

                            if (!string.IsNullOrEmpty(resultId))
                            {
                                results[pageNum] = _ocr.GetOCRResultText(resultId);
                            }
                        }
                    }
                }
                finally
                {
                    // Release ALL rendered page images
                    foreach (var id in processedImageIds)
                    {
                        _imaging.ReleaseGdPictureImage(id);
                    }
                }

                return results;
            }
        }
    }

    // IRONOCR: Built-in page selection
    namespace IronOcrApproach
    {
        using IronOcr;

        public class IronOcrPageSelection
        {
            public Dictionary<int, string> ProcessPages(string pdfPath, int[] pageNumbers)
            {
                var ocr = new IronTesseract();

                using var input = new OcrInput();
                input.LoadPdfPages(pdfPath, pageNumbers);

                var result = ocr.Read(input);
                var results = new Dictionary<int, string>();

                foreach (var page in result.Pages)
                {
                    results[page.PageNumber] = page.Text;
                }

                return results;
            }

            // Or with page range
            public string ProcessPageRange(string pdfPath, int startPage, int endPage)
            {
                using var input = new OcrInput();

                // Load specific page range
                for (int i = startPage; i <= endPage; i++)
                {
                    input.LoadPdfPages(pdfPath, new[] { i });
                }

                return new IronTesseract().Read(input).Text;
            }
        }
    }
}


// ============================================================================
// SCENARIO 3: MEMORY LEAK DEMONSTRATION
// What happens when cleanup is forgotten
// ============================================================================

namespace PdfOcr_MemoryLeakDemo
{
    // GDPICTURE: Memory leak example and correct pattern
    namespace GdPictureApproach
    {
        using GdPicture14;

        public class GdPictureMemoryIssues
        {
            private GdPictureImaging _imaging;
            private GdPictureOCR _ocr;

            // BAD: Memory leak - will crash on large PDFs
            public void ProcessPdfWithLeak(string pdfPath)
            {
                using var pdf = new GdPicturePDF();
                pdf.LoadFromFile(pdfPath, false);

                for (int i = 1; i <= pdf.GetPageCount(); i++)
                {
                    pdf.SelectPage(i);

                    // Each render creates ~10-50MB memory allocation
                    int imageId = pdf.RenderPageToGdPictureImage(200, false);

                    _ocr.SetImage(imageId);
                    _ocr.Language = "eng";
                    var resultId = _ocr.RunOCR();
                    Console.WriteLine(_ocr.GetOCRResultText(resultId));

                    // PROBLEM: No ReleaseGdPictureImage call!
                    // Memory accumulates with each page
                    // 100-page PDF at 200 DPI = 1-5GB leaked memory
                }
            }

            // CORRECT: With proper cleanup
            public void ProcessPdfCorrectly(string pdfPath)
            {
                using var pdf = new GdPicturePDF();
                pdf.LoadFromFile(pdfPath, false);

                for (int i = 1; i <= pdf.GetPageCount(); i++)
                {
                    pdf.SelectPage(i);
                    int imageId = pdf.RenderPageToGdPictureImage(200, false);

                    if (imageId != 0)
                    {
                        try
                        {
                            _ocr.SetImage(imageId);
                            _ocr.Language = "eng";
                            var resultId = _ocr.RunOCR();
                            Console.WriteLine(_ocr.GetOCRResultText(resultId));
                        }
                        finally
                        {
                            // CORRECT: Release immediately after processing
                            _imaging.ReleaseGdPictureImage(imageId);
                        }
                    }
                }
            }
        }
    }

    // IRONOCR: No memory leak risk
    namespace IronOcrApproach
    {
        using IronOcr;

        public class IronOcrNoLeakRisk
        {
            public void ProcessPdf(string pdfPath)
            {
                // Automatic memory management
                // using statement ensures cleanup even on exceptions
                using var input = new OcrInput();
                input.LoadPdf(pdfPath);

                var result = new IronTesseract().Read(input);

                foreach (var page in result.Pages)
                {
                    Console.WriteLine(page.Text);
                }
                // Resources released automatically when using block exits
            }

            // Even simpler - no explicit input object
            public void ProcessPdfSimplest(string pdfPath)
            {
                var result = new IronTesseract().Read(pdfPath);

                foreach (var page in result.Pages)
                {
                    Console.WriteLine(page.Text);
                }
                // Internal resources managed automatically
            }
        }
    }
}


// ============================================================================
// SCENARIO 4: SEARCHABLE PDF CREATION
// Adding OCR text layer to scanned PDFs
// ============================================================================

namespace PdfOcr_SearchablePdf
{
    // GDPICTURE: OcrPage method for in-place OCR
    namespace GdPictureApproach
    {
        using GdPicture14;

        public class GdPictureSearchable
        {
            public void MakeSearchable(string inputPdf, string outputPdf)
            {
                using var pdf = new GdPicturePDF();

                GdPictureStatus status = pdf.LoadFromFile(inputPdf, false);

                if (status != GdPictureStatus.OK)
                {
                    throw new Exception($"Load failed: {status}");
                }

                int pageCount = pdf.GetPageCount();

                for (int i = 1; i <= pageCount; i++)
                {
                    pdf.SelectPage(i);

                    // OcrPage adds text layer to page
                    // Requires: OCR plugin + resource folder
                    GdPictureStatus ocrStatus = pdf.OcrPage(
                        "eng",                           // Language
                        @"C:\GdPicture\Resources\OCR",   // Resource path
                        "",                              // No secondary language
                        200                              // DPI for OCR
                    );

                    if (ocrStatus != GdPictureStatus.OK)
                    {
                        Console.WriteLine($"Warning: Page {i} OCR failed: {ocrStatus}");
                    }
                }

                // Save with PDF/A compliance if needed
                pdf.SaveToFile(outputPdf, true); // true = linearize
            }
        }
    }

    // IRONOCR: One method call
    namespace IronOcrApproach
    {
        using IronOcr;

        public class IronOcrSearchable
        {
            public void MakeSearchable(string inputPdf, string outputPdf)
            {
                var result = new IronTesseract().Read(inputPdf);
                result.SaveAsSearchablePdf(outputPdf);
            }

            // With options
            public void MakeSearchableWithOptions(string inputPdf, string outputPdf)
            {
                var ocr = new IronTesseract();
                ocr.Language = OcrLanguage.English;

                using var input = new OcrInput();
                input.LoadPdf(inputPdf);

                // Optional: Apply preprocessing
                input.Deskew();
                input.DeNoise();

                var result = ocr.Read(input);

                // Save as searchable PDF
                result.SaveAsSearchablePdf(outputPdf);
            }
        }
    }
}


// ============================================================================
// SCENARIO 5: PASSWORD-PROTECTED PDFS
// GdPicture requires separate handling; IronOCR has built-in support
// ============================================================================

namespace PdfOcr_PasswordProtected
{
    // GDPICTURE: Password passed to LoadFromFile
    namespace GdPictureApproach
    {
        using GdPicture14;

        public class GdPictureProtectedPdf
        {
            private GdPictureImaging _imaging;
            private GdPictureOCR _ocr;

            public string ExtractFromProtectedPdf(string pdfPath, string password)
            {
                var text = new StringBuilder();

                using var pdf = new GdPicturePDF();

                // Password must be provided at load time
                // Second parameter is user password, third is owner password
                GdPictureStatus status = pdf.LoadFromFile(pdfPath, password);

                if (status != GdPictureStatus.OK)
                {
                    if (status == GdPictureStatus.PasswordNeeded)
                    {
                        throw new Exception("Incorrect password");
                    }
                    throw new Exception($"Load failed: {status}");
                }

                // Standard processing from here
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
                            var resultId = _ocr.RunOCR();

                            if (!string.IsNullOrEmpty(resultId))
                            {
                                text.AppendLine(_ocr.GetOCRResultText(resultId));
                            }
                        }
                        finally
                        {
                            _imaging.ReleaseGdPictureImage(imageId);
                        }
                    }
                }

                return text.ToString();
            }
        }
    }

    // IRONOCR: Simple password parameter
    namespace IronOcrApproach
    {
        using IronOcr;

        public class IronOcrProtectedPdf
        {
            public string ExtractFromProtectedPdf(string pdfPath, string password)
            {
                using var input = new OcrInput();

                // Password as named parameter
                input.LoadPdf(pdfPath, Password: password);

                return new IronTesseract().Read(input).Text;
            }

            // Make protected PDF searchable
            public void MakeProtectedPdfSearchable(string inputPdf, string password, string outputPdf)
            {
                using var input = new OcrInput();
                input.LoadPdf(inputPdf, Password: password);

                var result = new IronTesseract().Read(input);
                result.SaveAsSearchablePdf(outputPdf);
            }
        }
    }
}


// ============================================================================
// SCENARIO 6: BATCH PDF PROCESSING
// Processing multiple PDFs with resource management
// ============================================================================

namespace PdfOcr_BatchProcessing
{
    // GDPICTURE: Complex resource tracking across files
    namespace GdPictureApproach
    {
        using GdPicture14;

        public class GdPictureBatchPdf
        {
            private GdPictureImaging _imaging;
            private GdPictureOCR _ocr;

            public Dictionary<string, string> ProcessPdfBatch(string[] pdfPaths)
            {
                var results = new Dictionary<string, string>();

                foreach (var pdfPath in pdfPaths)
                {
                    var fileImageIds = new List<int>();

                    using var pdf = new GdPicturePDF();

                    if (pdf.LoadFromFile(pdfPath, false) != GdPictureStatus.OK)
                    {
                        results[pdfPath] = "ERROR: Failed to load";
                        continue;
                    }

                    try
                    {
                        var text = new StringBuilder();
                        int pageCount = pdf.GetPageCount();

                        for (int i = 1; i <= pageCount; i++)
                        {
                            pdf.SelectPage(i);
                            int imageId = pdf.RenderPageToGdPictureImage(200, false);

                            if (imageId != 0)
                            {
                                fileImageIds.Add(imageId);

                                _ocr.SetImage(imageId);
                                _ocr.Language = "eng";
                                var resultId = _ocr.RunOCR();

                                if (!string.IsNullOrEmpty(resultId))
                                {
                                    text.AppendLine(_ocr.GetOCRResultText(resultId));
                                }
                            }
                        }

                        results[pdfPath] = text.ToString();
                    }
                    finally
                    {
                        // Release all image IDs for this file
                        foreach (var id in fileImageIds)
                        {
                            _imaging.ReleaseGdPictureImage(id);
                        }
                    }
                }

                return results;
            }
        }
    }

    // IRONOCR: Simple parallel processing
    namespace IronOcrApproach
    {
        using IronOcr;
        using System.Threading.Tasks;
        using System.Collections.Concurrent;

        public class IronOcrBatchPdf
        {
            public Dictionary<string, string> ProcessPdfBatch(string[] pdfPaths)
            {
                var results = new Dictionary<string, string>();
                var ocr = new IronTesseract(); // Thread-safe

                foreach (var pdfPath in pdfPaths)
                {
                    try
                    {
                        results[pdfPath] = ocr.Read(pdfPath).Text;
                    }
                    catch (Exception ex)
                    {
                        results[pdfPath] = $"ERROR: {ex.Message}";
                    }
                }

                return results;
            }

            // Parallel processing (thread-safe)
            public ConcurrentDictionary<string, string> ProcessPdfBatchParallel(string[] pdfPaths)
            {
                var results = new ConcurrentDictionary<string, string>();
                var ocr = new IronTesseract(); // Single instance, thread-safe

                Parallel.ForEach(pdfPaths, pdfPath =>
                {
                    try
                    {
                        results[pdfPath] = ocr.Read(pdfPath).Text;
                    }
                    catch (Exception ex)
                    {
                        results[pdfPath] = $"ERROR: {ex.Message}";
                    }
                });

                return results;
            }
        }
    }
}


// ============================================================================
// SUMMARY: PDF OCR COMPLEXITY COMPARISON
//
// GdPicture.NET PDF OCR requires:
// - PDF plugin license (separate from OCR plugin)
// - Manual page iteration
// - Image ID tracking for each rendered page
// - Explicit cleanup to prevent memory leaks
// - Status code checking at every step
//
// IronOCR PDF OCR provides:
// - Built-in PDF support (no separate license)
// - Automatic page handling
// - Automatic memory management
// - Exception-based error handling
// - Password support as parameter
//
// Get IronOCR: https://ironsoftware.com/csharp/ocr/
// NuGet: Install-Package IronOcr
// ============================================================================
