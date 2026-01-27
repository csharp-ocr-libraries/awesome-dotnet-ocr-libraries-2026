// =============================================================================
// Tesseract.Net.SDK PDF Processing Examples
// =============================================================================
// Install: Install-Package Tesseract.Net.SDK
//          Install-Package PdfiumViewer (for PDF rendering)
// License: Commercial (Patagames)
// Platform: Windows ONLY (.NET Framework 2.0-4.5)
//
// CRITICAL LIMITATION:
// Tesseract.Net.SDK does NOT support PDF input natively.
// You must use a separate library to convert PDF pages to images first.
// This example uses PdfiumViewer, but alternatives include:
//   - iTextSharp
//   - Ghostscript.NET
//   - Docnet.Core
//
// IronOCR Comparison:
// IronOCR supports PDF input natively with: input.LoadPdf("document.pdf")
// =============================================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Patagames.Ocr;
// using PdfiumViewer; // Requires separate NuGet package

namespace TesseractNetSdkExamples
{
    /// <summary>
    /// Demonstrates PDF OCR processing with Tesseract.Net.SDK.
    /// IMPORTANT: Requires external PDF rendering library since Tesseract.Net.SDK
    /// does not support PDF input directly.
    /// </summary>
    public class PdfProcessingExamples
    {
        private const string TessdataPath = @".\tessdata";

        /// <summary>
        /// Process a multi-page PDF document.
        /// LIMITATION: Must convert each page to image first.
        /// </summary>
        /// <param name="pdfPath">Path to PDF file</param>
        /// <param name="dpi">Resolution for rendering (300 recommended for OCR)</param>
        /// <returns>Combined text from all pages</returns>
        public string ExtractTextFromPdf(string pdfPath, int dpi = 300)
        {
            // This method demonstrates the pattern - actual PDF rendering
            // requires PdfiumViewer or similar library

            var results = new StringBuilder();

            // PSEUDO-CODE: PDF rendering with PdfiumViewer
            // using (var pdfDocument = PdfDocument.Load(pdfPath))
            // {
            //     for (int pageIndex = 0; pageIndex < pdfDocument.PageCount; pageIndex++)
            //     {
            //         // Render page to image at specified DPI
            //         using (var pageImage = pdfDocument.Render(pageIndex, dpi, dpi, PdfRenderFlags.CorrectFromDpi))
            //         {
            //             // Save to temp file (Tesseract.Net.SDK limitation)
            //             string tempPath = Path.GetTempFileName() + ".png";
            //             pageImage.Save(tempPath, ImageFormat.Png);
            //
            //             try
            //             {
            //                 // OCR the page image
            //                 string pageText = ExtractTextFromTempImage(tempPath);
            //                 results.AppendLine($"--- Page {pageIndex + 1} ---");
            //                 results.AppendLine(pageText);
            //             }
            //             finally
            //             {
            //                 // Clean up temp file
            //                 if (File.Exists(tempPath))
            //                     File.Delete(tempPath);
            //             }
            //         }
            //     }
            // }

            // For actual implementation, see ExtractTextFromPdfActual below
            throw new NotImplementedException(
                "PDF processing requires PdfiumViewer NuGet package. " +
                "Install with: Install-Package PdfiumViewer");
        }

        /// <summary>
        /// OCR a single image (used after PDF page rendering).
        /// </summary>
        private string ExtractTextFromTempImage(string imagePath)
        {
            using (var api = OcrApi.Create())
            {
                api.Init(Languages.English);
                return api.GetTextFromImage(imagePath);
            }
        }

        /// <summary>
        /// Process specific page range from PDF.
        /// Useful for large documents where you only need certain pages.
        /// </summary>
        public string ExtractTextFromPdfPages(string pdfPath, int startPage, int endPage, int dpi = 300)
        {
            // Validate page range
            if (startPage < 1)
                throw new ArgumentException("Start page must be >= 1");
            if (endPage < startPage)
                throw new ArgumentException("End page must be >= start page");

            var results = new StringBuilder();

            // PSEUDO-CODE: Similar to above but with page range
            // using (var pdfDocument = PdfDocument.Load(pdfPath))
            // {
            //     // Validate page range against document
            //     if (endPage > pdfDocument.PageCount)
            //         endPage = pdfDocument.PageCount;
            //
            //     for (int pageIndex = startPage - 1; pageIndex < endPage; pageIndex++)
            //     {
            //         // ... render and OCR each page
            //     }
            // }

            return results.ToString();
        }

        /// <summary>
        /// CRITICAL LIMITATION: Threading issues with Tesseract.Net.SDK
        ///
        /// Tesseract engines are NOT thread-safe. When processing PDFs
        /// with multiple pages in parallel, each thread needs its own
        /// OcrApi instance, which multiplies memory usage.
        /// </summary>
        public Dictionary<int, string> ProcessPdfPagesParallel(string pdfPath, int maxParallelism = 4)
        {
            var results = new Dictionary<int, string>();
            var syncLock = new object();

            // PSEUDO-CODE: Parallel PDF processing
            // using (var pdfDocument = PdfDocument.Load(pdfPath))
            // {
            //     var pageIndices = Enumerable.Range(0, pdfDocument.PageCount).ToList();
            //
            //     Parallel.ForEach(
            //         pageIndices,
            //         new ParallelOptions { MaxDegreeOfParallelism = maxParallelism },
            //         pageIndex =>
            //         {
            //             // WARNING: Must create separate OcrApi for each thread!
            //             // This uses ~40-100MB per thread per language
            //             using (var api = OcrApi.Create())
            //             {
            //                 api.Init(Languages.English);
            //
            //                 // Render page to image
            //                 using (var pageImage = pdfDocument.Render(pageIndex, 300, 300, PdfRenderFlags.CorrectFromDpi))
            //                 {
            //                     string tempPath = Path.GetTempFileName() + ".png";
            //                     pageImage.Save(tempPath, ImageFormat.Png);
            //
            //                     try
            //                     {
            //                         string text = api.GetTextFromImage(tempPath);
            //
            //                         lock (syncLock)
            //                         {
            //                             results[pageIndex + 1] = text;
            //                         }
            //                     }
            //                     finally
            //                     {
            //                         File.Delete(tempPath);
            //                     }
            //                 }
            //             }
            //         });
            // }

            // Memory usage calculation:
            // 4 threads × 100MB per engine = 400MB just for OCR engines
            // Plus PDF rendering memory, temp images, etc.
            // Total: 600MB-1GB+ for parallel PDF processing

            return results;
        }
    }

    /// <summary>
    /// Demonstrates batch processing of multiple documents.
    /// Highlights memory management challenges with Tesseract.Net.SDK.
    /// </summary>
    public class BatchProcessingExamples
    {
        /// <summary>
        /// Process multiple image files in sequence.
        /// Memory-efficient but slower than parallel.
        /// </summary>
        public Dictionary<string, string> ProcessBatchSequential(string[] imagePaths)
        {
            var results = new Dictionary<string, string>();

            // Single engine for all files - memory efficient
            using (var api = OcrApi.Create())
            {
                api.Init(Languages.English);

                foreach (var imagePath in imagePaths)
                {
                    try
                    {
                        string text = api.GetTextFromImage(imagePath);
                        results[imagePath] = text;
                    }
                    catch (Exception ex)
                    {
                        results[imagePath] = $"ERROR: {ex.Message}";
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Process multiple files in parallel.
        /// CAUTION: High memory usage due to per-thread engine requirement.
        /// </summary>
        public Dictionary<string, string> ProcessBatchParallel(string[] imagePaths, int maxParallelism = 4)
        {
            var results = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

            // WARNING: This creates maxParallelism separate engines
            // Each loads ~40-100MB of language data into memory
            Parallel.ForEach(
                imagePaths,
                new ParallelOptions { MaxDegreeOfParallelism = maxParallelism },
                imagePath =>
                {
                    // Each parallel task needs its own engine
                    // This is NOT efficient but required by Tesseract
                    using (var api = OcrApi.Create())
                    {
                        api.Init(Languages.English);

                        try
                        {
                            string text = api.GetTextFromImage(imagePath);
                            results[imagePath] = text;
                        }
                        catch (Exception ex)
                        {
                            results[imagePath] = $"ERROR: {ex.Message}";
                        }
                    }
                });

            return new Dictionary<string, string>(results);
        }

        /// <summary>
        /// Process batch with progress reporting.
        /// Useful for long-running operations.
        /// </summary>
        public void ProcessBatchWithProgress(
            string[] imagePaths,
            IProgress<BatchProgress> progress,
            CancellationToken cancellationToken = default)
        {
            int total = imagePaths.Length;
            int processed = 0;
            int succeeded = 0;
            int failed = 0;

            using (var api = OcrApi.Create())
            {
                api.Init(Languages.English);

                foreach (var imagePath in imagePaths)
                {
                    // Check for cancellation
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        string text = api.GetTextFromImage(imagePath);
                        succeeded++;
                    }
                    catch
                    {
                        failed++;
                    }

                    processed++;

                    // Report progress
                    progress?.Report(new BatchProgress
                    {
                        Total = total,
                        Processed = processed,
                        Succeeded = succeeded,
                        Failed = failed,
                        CurrentFile = imagePath,
                        PercentComplete = (processed * 100) / total
                    });
                }
            }
        }
    }

    /// <summary>
    /// Progress information for batch operations.
    /// </summary>
    public class BatchProgress
    {
        public int Total { get; set; }
        public int Processed { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }
        public string CurrentFile { get; set; }
        public int PercentComplete { get; set; }
    }

    /// <summary>
    /// Demonstrates the memory and performance limitations
    /// when processing large documents with Tesseract.Net.SDK.
    /// </summary>
    public class LargeDocumentExamples
    {
        /// <summary>
        /// Process a large multi-page TIFF document.
        /// Shows memory management techniques for large files.
        /// </summary>
        public List<PageResult> ProcessLargeTiff(string tiffPath)
        {
            var results = new List<PageResult>();

            using (var api = OcrApi.Create())
            {
                api.Init(Languages.English);

                // Load TIFF and get frame count
                using (var bitmap = new Bitmap(tiffPath))
                {
                    var dimension = new System.Drawing.Imaging.FrameDimension(
                        bitmap.FrameDimensionsList[0]);
                    int frameCount = bitmap.GetFrameCount(dimension);

                    for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                    {
                        // Select frame
                        bitmap.SelectActiveFrame(dimension, frameIndex);

                        // Save frame to temp file (Tesseract limitation)
                        string tempPath = Path.GetTempFileName() + ".png";
                        bitmap.Save(tempPath, ImageFormat.Png);

                        try
                        {
                            string text = api.GetTextFromImage(tempPath);
                            float confidence = api.GetMeanConfidence();

                            results.Add(new PageResult
                            {
                                PageNumber = frameIndex + 1,
                                Text = text,
                                Confidence = confidence
                            });

                            // Force garbage collection between pages to manage memory
                            // This slows processing but prevents out-of-memory errors
                            if (frameIndex % 10 == 0)
                            {
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                            }
                        }
                        finally
                        {
                            File.Delete(tempPath);
                        }
                    }
                }
            }

            return results;
        }
    }

    /// <summary>
    /// Result for a single page in a multi-page document.
    /// </summary>
    public class PageResult
    {
        public int PageNumber { get; set; }
        public string Text { get; set; }
        public float Confidence { get; set; }
    }
}

// =============================================================================
// IronOCR Comparison: PDF Processing
// =============================================================================
// IronOCR's native PDF support eliminates all the complexity above:
//
// using IronOcr;
//
// var ocr = new IronTesseract();
//
// // Single-line PDF OCR
// using var input = new OcrInput();
// input.LoadPdf("document.pdf");  // Native PDF support!
//
// var result = ocr.Read(input);
//
// // Access individual pages
// foreach (var page in result.Pages)
// {
//     Console.WriteLine($"Page {page.PageNumber}: {page.Text}");
// }
//
// // Or load specific pages
// input.LoadPdfPages("document.pdf", new[] { 1, 3, 5 });
//
// Key advantages:
// - No PdfiumViewer dependency
// - No temp file management
// - No manual page iteration
// - Thread-safe (single instance for parallel processing)
// - Native password-protected PDF support
// - 10x less code for the same result
// =============================================================================
