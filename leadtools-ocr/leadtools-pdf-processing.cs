/**
 * LEADTOOLS vs IronOCR: PDF Processing Comparison
 *
 * PDF processing is where LEADTOOLS API complexity is most visible.
 * This file compares PDF OCR workflows between LEADTOOLS and IronOCR,
 * demonstrating the significant code reduction possible with IronOCR.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * Key Differences:
 * - LEADTOOLS: Page-by-page iteration, manual image conversion, explicit disposal
 * - IronOCR: Native PDF support, automatic page handling, simple API
 */

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

// ============================================================================
// SCENARIO 1: Basic PDF OCR
// LEADTOOLS requires loading PDF pages as images; IronOCR handles natively
// ============================================================================

namespace PdfProcessing_BasicOcr
{
    // BEFORE: LEADTOOLS - Load PDF pages as images, iterate manually

    namespace LeadtoolsBefore
    {
        using Leadtools;
        using Leadtools.Ocr;
        using Leadtools.Codecs;

        public class LeadtoolsPdfOcr
        {
            private IOcrEngine _engine;
            private RasterCodecs _codecs;

            public LeadtoolsPdfOcr(IOcrEngine engine, RasterCodecs codecs)
            {
                _engine = engine;
                _codecs = codecs;
            }

            public string ExtractTextFromPdf(string pdfPath)
            {
                var text = new StringBuilder();

                // Step 1: Get PDF information
                var pdfInfo = _codecs.GetInformation(pdfPath, true);
                int totalPages = pdfInfo.TotalPages;

                // Step 2: Create document container
                using var document = _engine.DocumentManager.CreateDocument();

                // Step 3: Iterate through each page
                for (int pageNum = 1; pageNum <= totalPages; pageNum++)
                {
                    // Step 4: Load specific page as raster image
                    // IMPORTANT: Must dispose each page image!
                    using var pageImage = _codecs.Load(
                        pdfPath,
                        0,  // bitsPerPixel (0 = default)
                        CodecsLoadByteOrder.BgrOrGray,
                        pageNum,  // firstPage
                        pageNum   // lastPage
                    );

                    // Step 5: Add page to OCR document
                    var page = document.Pages.AddPage(pageImage, null);

                    // Step 6: Recognize the page
                    page.Recognize(null);

                    // Step 7: Extract text
                    text.AppendLine($"--- Page {pageNum} ---");
                    text.AppendLine(page.GetText(-1));
                }

                return text.ToString();
            }
        }
    }

    // AFTER: IronOCR - Native PDF support

    namespace IronOcrAfter
    {
        using IronOcr;

        public class IronOcrPdfService
        {
            public string ExtractTextFromPdf(string pdfPath)
            {
                var ocr = new IronTesseract();

                using var input = new OcrInput();
                input.LoadPdf(pdfPath);  // Automatically handles all pages

                return ocr.Read(input).Text;
            }

            // With per-page access
            public string ExtractTextWithPageNumbers(string pdfPath)
            {
                var ocr = new IronTesseract();

                using var input = new OcrInput();
                input.LoadPdf(pdfPath);

                var result = ocr.Read(input);
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
// SCENARIO 2: Password-Protected PDFs
// LEADTOOLS needs separate PDF module; IronOCR handles built-in
// ============================================================================

namespace PdfProcessing_PasswordProtected
{
    // BEFORE: LEADTOOLS - Requires Leadtools.Pdf module (additional license)

    namespace LeadtoolsBefore
    {
        using Leadtools;
        using Leadtools.Ocr;
        using Leadtools.Codecs;
        using Leadtools.Pdf;  // Additional module required!

        public class LeadtoolsEncryptedPdfOcr
        {
            private IOcrEngine _engine;
            private RasterCodecs _codecs;

            public string ExtractFromEncryptedPdf(string pdfPath, string password)
            {
                // Method 1: Using PDFFile class (requires Leadtools.Pdf)
                var pdfFile = new PDFFile(pdfPath);
                pdfFile.Password = password;

                // Or use codec options for password
                var loadOptions = new CodecsLoadOptions();
                loadOptions.Pdf.Password = password;

                // Apply options to codecs
                _codecs.Options.Pdf.Load.Password = password;

                var text = new StringBuilder();
                var pdfInfo = _codecs.GetInformation(pdfPath, true);

                using var document = _engine.DocumentManager.CreateDocument();

                for (int i = 1; i <= pdfInfo.TotalPages; i++)
                {
                    using var pageImage = _codecs.Load(pdfPath, 0,
                        CodecsLoadByteOrder.BgrOrGray, i, i);

                    var page = document.Pages.AddPage(pageImage, null);
                    page.Recognize(null);
                    text.AppendLine(page.GetText(-1));
                }

                return text.ToString();
            }
        }
    }

    // AFTER: IronOCR - Built-in password support

    namespace IronOcrAfter
    {
        using IronOcr;

        public class IronOcrEncryptedPdfService
        {
            public string ExtractFromEncryptedPdf(string pdfPath, string password)
            {
                var ocr = new IronTesseract();

                using var input = new OcrInput();
                input.LoadPdf(pdfPath, Password: password);  // Password parameter

                return ocr.Read(input).Text;
            }

            // Alternative: TryLoad pattern for error handling
            public (bool Success, string Text) TryExtractFromEncryptedPdf(
                string pdfPath, string password)
            {
                try
                {
                    using var input = new OcrInput();
                    input.LoadPdf(pdfPath, Password: password);

                    var result = new IronTesseract().Read(input);
                    return (true, result.Text);
                }
                catch (Exception)
                {
                    return (false, null);
                }
            }
        }
    }
}


// ============================================================================
// SCENARIO 3: Searchable PDF Output
// LEADTOOLS requires DocumentWriter configuration; IronOCR is one method
// ============================================================================

namespace PdfProcessing_SearchableOutput
{
    // BEFORE: LEADTOOLS - Configure DocumentWriter for PDF output

    namespace LeadtoolsBefore
    {
        using Leadtools;
        using Leadtools.Ocr;
        using Leadtools.Codecs;
        using Leadtools.Document.Writer;

        public class LeadtoolsSearchablePdfCreator
        {
            private IOcrEngine _engine;
            private RasterCodecs _codecs;

            public void CreateSearchablePdf(string inputPdfPath, string outputPdfPath)
            {
                // Load and OCR the input PDF
                var pdfInfo = _codecs.GetInformation(inputPdfPath, true);

                using var document = _engine.DocumentManager.CreateDocument();

                for (int i = 1; i <= pdfInfo.TotalPages; i++)
                {
                    using var pageImage = _codecs.Load(inputPdfPath, 0,
                        CodecsLoadByteOrder.BgrOrGray, i, i);

                    var page = document.Pages.AddPage(pageImage, null);
                    page.Recognize(null);
                }

                // Configure PDF output options
                var pdfOptions = new PdfDocumentOptions
                {
                    DocumentType = PdfDocumentType.Pdf,
                    ImageOverText = true,  // Image visible, text layer underneath
                    Linearized = false,
                    Title = Path.GetFileNameWithoutExtension(inputPdfPath)
                };

                // Apply options to document writer
                _engine.DocumentWriterInstance.SetOptions(
                    DocumentFormat.Pdf,
                    pdfOptions);

                // Save as searchable PDF
                document.Save(outputPdfPath, DocumentFormat.Pdf, null);
            }

            public void CreatePdfACompliant(string inputPath, string outputPath)
            {
                // Similar process but with PDF/A options
                var pdfInfo = _codecs.GetInformation(inputPath, true);

                using var document = _engine.DocumentManager.CreateDocument();

                for (int i = 1; i <= pdfInfo.TotalPages; i++)
                {
                    using var pageImage = _codecs.Load(inputPath, 0,
                        CodecsLoadByteOrder.BgrOrGray, i, i);

                    var page = document.Pages.AddPage(pageImage, null);
                    page.Recognize(null);
                }

                var pdfOptions = new PdfDocumentOptions
                {
                    DocumentType = PdfDocumentType.PdfA,  // PDF/A compliance
                    ImageOverText = true
                };

                _engine.DocumentWriterInstance.SetOptions(DocumentFormat.Pdf, pdfOptions);
                document.Save(outputPath, DocumentFormat.Pdf, null);
            }
        }
    }

    // AFTER: IronOCR - Single method call

    namespace IronOcrAfter
    {
        using IronOcr;

        public class IronOcrSearchablePdfService
        {
            public void CreateSearchablePdf(string inputPdfPath, string outputPdfPath)
            {
                var ocr = new IronTesseract();

                using var input = new OcrInput();
                input.LoadPdf(inputPdfPath);

                var result = ocr.Read(input);
                result.SaveAsSearchablePdf(outputPdfPath);
            }

            // Get as byte array (for streaming, memory processing)
            public byte[] CreateSearchablePdfBytes(string inputPdfPath)
            {
                using var input = new OcrInput();
                input.LoadPdf(inputPdfPath);

                return new IronTesseract().Read(input).SaveAsSearchablePdfBytes();
            }

            // Process and return both text and searchable PDF
            public (string Text, byte[] PdfBytes) ProcessPdf(string inputPath)
            {
                using var input = new OcrInput();
                input.LoadPdf(inputPath);

                var result = new IronTesseract().Read(input);

                return (result.Text, result.SaveAsSearchablePdfBytes());
            }
        }
    }
}


// ============================================================================
// SCENARIO 4: Multi-Page PDF Batch Processing
// LEADTOOLS requires careful memory management; IronOCR handles automatically
// ============================================================================

namespace PdfProcessing_BatchProcessing
{
    // BEFORE: LEADTOOLS - Manual memory management critical

    namespace LeadtoolsBefore
    {
        using Leadtools;
        using Leadtools.Ocr;
        using Leadtools.Codecs;

        public class LeadtoolsPdfBatchProcessor
        {
            private IOcrEngine _engine;
            private RasterCodecs _codecs;

            public Dictionary<string, string> ProcessMultiplePdfs(string[] pdfPaths)
            {
                var results = new Dictionary<string, string>();

                foreach (var pdfPath in pdfPaths)
                {
                    var text = new StringBuilder();
                    var pdfInfo = _codecs.GetInformation(pdfPath, true);

                    // Create fresh document for each PDF
                    using var document = _engine.DocumentManager.CreateDocument();

                    for (int i = 1; i <= pdfInfo.TotalPages; i++)
                    {
                        // CRITICAL: Dispose each page image
                        using var pageImage = _codecs.Load(pdfPath, 0,
                            CodecsLoadByteOrder.BgrOrGray, i, i);

                        var page = document.Pages.AddPage(pageImage, null);
                        page.Recognize(null);
                        text.AppendLine(page.GetText(-1));
                    }

                    results[pdfPath] = text.ToString();

                    // Force garbage collection between large PDFs
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                return results;
            }

            // Large PDF - page at a time to control memory
            public IEnumerable<string> ProcessLargePdfStreaming(string pdfPath)
            {
                var pdfInfo = _codecs.GetInformation(pdfPath, true);

                for (int i = 1; i <= pdfInfo.TotalPages; i++)
                {
                    // Process one page at a time
                    using var document = _engine.DocumentManager.CreateDocument();
                    using var pageImage = _codecs.Load(pdfPath, 0,
                        CodecsLoadByteOrder.BgrOrGray, i, i);

                    var page = document.Pages.AddPage(pageImage, null);
                    page.Recognize(null);

                    yield return page.GetText(-1);

                    // Explicit cleanup
                    document.Pages.Clear();
                }
            }
        }
    }

    // AFTER: IronOCR - Automatic memory management

    namespace IronOcrAfter
    {
        using IronOcr;

        public class IronOcrPdfBatchProcessor
        {
            public Dictionary<string, string> ProcessMultiplePdfs(string[] pdfPaths)
            {
                var ocr = new IronTesseract();
                var results = new Dictionary<string, string>();

                foreach (var pdfPath in pdfPaths)
                {
                    using var input = new OcrInput();
                    input.LoadPdf(pdfPath);

                    results[pdfPath] = ocr.Read(input).Text;
                }

                return results;
            }

            // Parallel processing (thread-safe)
            public Dictionary<string, string> ProcessMultiplePdfsParallel(string[] pdfPaths)
            {
                var ocr = new IronTesseract();  // Thread-safe, reusable
                var results = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

                Parallel.ForEach(pdfPaths, pdfPath =>
                {
                    using var input = new OcrInput();
                    input.LoadPdf(pdfPath);

                    results[pdfPath] = ocr.Read(input).Text;
                });

                return results.ToDictionary(x => x.Key, x => x.Value);
            }

            // Large PDF - page-by-page access if needed
            public IEnumerable<string> ProcessLargePdfStreaming(string pdfPath)
            {
                var ocr = new IronTesseract();

                using var input = new OcrInput();
                input.LoadPdf(pdfPath);

                var result = ocr.Read(input);

                foreach (var page in result.Pages)
                {
                    yield return page.Text;
                }
            }

            // Specific page range
            public string ProcessPageRange(string pdfPath, int startPage, int endPage)
            {
                var ocr = new IronTesseract();

                using var input = new OcrInput();
                input.LoadPdfPages(pdfPath, startPage, endPage);

                return ocr.Read(input).Text;
            }
        }
    }
}


// ============================================================================
// SCENARIO 5: PDF with Preprocessing
// LEADTOOLS: Separate preprocessing pass; IronOCR: Inline filters
// ============================================================================

namespace PdfProcessing_WithPreprocessing
{
    // BEFORE: LEADTOOLS - Manual preprocessing of each page

    namespace LeadtoolsBefore
    {
        using Leadtools;
        using Leadtools.Ocr;
        using Leadtools.Codecs;
        using Leadtools.ImageProcessing;

        public class LeadtoolsPdfWithPreprocessing
        {
            private IOcrEngine _engine;
            private RasterCodecs _codecs;

            public string ProcessLowQualityPdf(string pdfPath)
            {
                var text = new StringBuilder();
                var pdfInfo = _codecs.GetInformation(pdfPath, true);

                using var document = _engine.DocumentManager.CreateDocument();

                for (int i = 1; i <= pdfInfo.TotalPages; i++)
                {
                    using var pageImage = _codecs.Load(pdfPath, 0,
                        CodecsLoadByteOrder.BgrOrGray, i, i);

                    // Apply preprocessing commands
                    // Deskew
                    var deskewCommand = new DeskewCommand();
                    deskewCommand.Run(pageImage);

                    // Despeckle (noise removal)
                    var despeckleCommand = new DespeckleCommand();
                    despeckleCommand.Run(pageImage);

                    // Convert to grayscale if not already
                    if (pageImage.BitsPerPixel > 8)
                    {
                        var grayscaleCommand = new GrayscaleCommand(8);
                        grayscaleCommand.Run(pageImage);
                    }

                    // Auto binarize
                    var binarizeCommand = new AutoBinarizeCommand();
                    binarizeCommand.Run(pageImage);

                    var page = document.Pages.AddPage(pageImage, null);
                    page.Recognize(null);
                    text.AppendLine(page.GetText(-1));
                }

                return text.ToString();
            }
        }
    }

    // AFTER: IronOCR - Built-in preprocessing

    namespace IronOcrAfter
    {
        using IronOcr;

        public class IronOcrPdfWithPreprocessing
        {
            public string ProcessLowQualityPdf(string pdfPath)
            {
                var ocr = new IronTesseract();

                using var input = new OcrInput();
                input.LoadPdf(pdfPath);

                // Apply preprocessing to all pages at once
                input.Deskew();           // Auto-detect and fix rotation
                input.DeNoise();          // Remove scanner artifacts
                input.EnhanceResolution(300);  // Upscale if needed

                return ocr.Read(input).Text;
            }

            // Full preprocessing pipeline for difficult scans
            public string ProcessDifficultPdf(string pdfPath)
            {
                var ocr = new IronTesseract();

                using var input = new OcrInput();
                input.LoadPdf(pdfPath);

                // Comprehensive preprocessing
                input.Deskew();
                input.DeNoise();
                input.Binarize();
                input.Contrast();
                input.EnhanceResolution(300);

                var result = ocr.Read(input);

                Console.WriteLine($"Confidence: {result.Confidence}%");
                return result.Text;
            }
        }
    }
}


// ============================================================================
// SCENARIO 6: Large Document Handling
// Memory-efficient patterns for processing large multi-page PDFs
// ============================================================================

namespace PdfProcessing_LargeDocuments
{
    // BEFORE: LEADTOOLS - Must process page-at-a-time to avoid memory issues

    namespace LeadtoolsBefore
    {
        using Leadtools;
        using Leadtools.Ocr;
        using Leadtools.Codecs;

        public class LeadtoolsLargePdfHandler
        {
            private IOcrEngine _engine;
            private RasterCodecs _codecs;

            // For very large PDFs (100+ pages), process in chunks
            public void ProcessLargePdfToFile(string pdfPath, string outputPath)
            {
                var pdfInfo = _codecs.GetInformation(pdfPath, true);
                int totalPages = pdfInfo.TotalPages;
                int chunkSize = 10;  // Process 10 pages at a time

                using var writer = new StreamWriter(outputPath);

                for (int startPage = 1; startPage <= totalPages; startPage += chunkSize)
                {
                    int endPage = Math.Min(startPage + chunkSize - 1, totalPages);

                    // Create fresh document for each chunk
                    using var document = _engine.DocumentManager.CreateDocument();

                    for (int pageNum = startPage; pageNum <= endPage; pageNum++)
                    {
                        using var pageImage = _codecs.Load(pdfPath, 0,
                            CodecsLoadByteOrder.BgrOrGray, pageNum, pageNum);

                        var page = document.Pages.AddPage(pageImage, null);
                        page.Recognize(null);
                        writer.WriteLine(page.GetText(-1));
                    }

                    // Clear and collect between chunks
                    document.Pages.Clear();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    // Report progress
                    Console.WriteLine($"Processed pages {startPage}-{endPage} of {totalPages}");
                }
            }

            // Memory monitoring during processing
            public string ProcessWithMemoryMonitoring(string pdfPath)
            {
                var text = new StringBuilder();
                var pdfInfo = _codecs.GetInformation(pdfPath, true);

                long baselineMemory = GC.GetTotalMemory(true);

                using var document = _engine.DocumentManager.CreateDocument();

                for (int i = 1; i <= pdfInfo.TotalPages; i++)
                {
                    using var pageImage = _codecs.Load(pdfPath, 0,
                        CodecsLoadByteOrder.BgrOrGray, i, i);

                    var page = document.Pages.AddPage(pageImage, null);
                    page.Recognize(null);
                    text.AppendLine(page.GetText(-1));

                    long currentMemory = GC.GetTotalMemory(false);
                    if (currentMemory - baselineMemory > 500_000_000)  // 500MB threshold
                    {
                        // Force cleanup
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }

                return text.ToString();
            }
        }
    }

    // AFTER: IronOCR - Automatic memory management, or page range for control

    namespace IronOcrAfter
    {
        using IronOcr;

        public class IronOcrLargePdfHandler
        {
            // Standard processing - IronOCR manages memory
            public string ProcessLargePdf(string pdfPath)
            {
                var ocr = new IronTesseract();

                using var input = new OcrInput();
                input.LoadPdf(pdfPath);

                return ocr.Read(input).Text;
            }

            // Chunked processing for very large documents if needed
            public void ProcessLargePdfToFile(string pdfPath, string outputPath)
            {
                var ocr = new IronTesseract();

                // Get page count first
                using var testInput = new OcrInput();
                testInput.LoadPdf(pdfPath);
                int totalPages = testInput.GetPages().Count();

                using var writer = new StreamWriter(outputPath);
                int chunkSize = 20;

                for (int startPage = 1; startPage <= totalPages; startPage += chunkSize)
                {
                    int endPage = Math.Min(startPage + chunkSize - 1, totalPages);

                    using var input = new OcrInput();
                    input.LoadPdfPages(pdfPath, startPage, endPage);

                    var result = ocr.Read(input);
                    writer.WriteLine(result.Text);

                    Console.WriteLine($"Processed pages {startPage}-{endPage} of {totalPages}");
                }
            }

            // Streaming with async support
            public async IAsyncEnumerable<string> ProcessPdfStreamingAsync(string pdfPath)
            {
                var ocr = new IronTesseract();

                using var input = new OcrInput();
                input.LoadPdf(pdfPath);

                var result = ocr.Read(input);

                foreach (var page in result.Pages)
                {
                    yield return page.Text;
                    await Task.Yield();  // Allow other async operations
                }
            }
        }
    }
}


// ============================================================================
// SUMMARY: PDF Processing Comparison
// ============================================================================

/*
 * LEADTOOLS PDF Processing:
 * - Requires loading each page as RasterImage
 * - Manual page iteration with explicit disposal
 * - Password PDFs need separate Leadtools.Pdf module
 * - Searchable PDF creation requires DocumentWriter configuration
 * - Memory management is developer responsibility
 * - Large PDFs need careful chunking
 *
 * IronOCR PDF Processing:
 * - Native PDF loading with LoadPdf()
 * - Automatic page handling
 * - Built-in password support
 * - One-method searchable PDF creation
 * - Automatic memory management
 * - Thread-safe parallel processing
 *
 * Code Reduction for Typical PDF Workflow:
 * - LEADTOOLS: 30-50 lines
 * - IronOCR: 3-5 lines
 */


// ============================================================================
// READY TO SIMPLIFY YOUR PDF PROCESSING?
//
// Get IronOCR: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// PDF Tutorial: https://ironsoftware.com/csharp/ocr/tutorials/csharp-tesseract-ocr/
// ============================================================================
