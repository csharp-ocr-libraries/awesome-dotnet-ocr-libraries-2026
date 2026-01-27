/**
 * Aspose.OCR vs IronOCR: PDF Processing Comparison
 *
 * This file demonstrates PDF OCR processing scenarios implemented in both
 * Aspose.OCR and IronOCR, highlighting API differences and complexity.
 *
 * Key difference: IronOCR has built-in PDF handling including password support.
 * Aspose.OCR requires Aspose.PDF (separate license) for password-protected PDFs.
 *
 * NuGet Packages:
 * - Aspose.OCR for Aspose examples
 * - Aspose.PDF for encrypted PDF handling (additional license)
 * - IronOcr for IronOCR examples
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// ============================================================================
// EXAMPLE 1: Basic PDF OCR
// ============================================================================

namespace BasicPdfOcr
{
    /// <summary>
    /// Aspose.OCR basic PDF text extraction.
    /// </summary>
    public class AsposeExample
    {
        public string ExtractFromPdf(string pdfPath)
        {
            var api = new Aspose.OCR.AsposeOcr();

            // DocumentRecognitionSettings for PDF processing
            var settings = new Aspose.OCR.DocumentRecognitionSettings
            {
                Language = Aspose.OCR.Language.Eng
            };

            // RecognizePdf returns list of results per page
            var results = api.RecognizePdf(pdfPath, settings);

            var text = new StringBuilder();
            foreach (var page in results)
            {
                text.AppendLine(page.RecognitionText);
            }

            return text.ToString();
        }
    }

    /// <summary>
    /// IronOCR basic PDF - direct Read support.
    /// </summary>
    public class IronOcrExample
    {
        public string ExtractFromPdf(string pdfPath)
        {
            // IronOCR reads PDFs directly - no special method needed
            return new IronOcr.IronTesseract().Read(pdfPath).Text;
        }

        public string ExtractFromPdfExplicit(string pdfPath)
        {
            // Or explicitly using OcrInput
            using var input = new IronOcr.OcrInput();
            input.LoadPdf(pdfPath);

            return new IronOcr.IronTesseract().Read(input).Text;
        }
    }
}


// ============================================================================
// EXAMPLE 2: Multi-Page PDF Handling
// ============================================================================

namespace MultiPagePdf
{
    public class PageResult
    {
        public int PageNumber { get; set; }
        public string Text { get; set; }
        public float Confidence { get; set; }
    }

    /// <summary>
    /// Aspose.OCR multi-page iteration.
    /// </summary>
    public class AsposeExample
    {
        public List<PageResult> ExtractAllPages(string pdfPath)
        {
            var api = new Aspose.OCR.AsposeOcr();
            var settings = new Aspose.OCR.DocumentRecognitionSettings();

            var results = api.RecognizePdf(pdfPath, settings);

            var pages = new List<PageResult>();
            int pageNum = 1;

            foreach (var result in results)
            {
                pages.Add(new PageResult
                {
                    PageNumber = pageNum++,
                    Text = result.RecognitionText,
                    Confidence = result.RecognitionAreasConfidence?.Average() ?? 0
                });
            }

            return pages;
        }

        public void ProcessPagesWithProgress(string pdfPath, Action<int, string> onPageProcessed)
        {
            var api = new Aspose.OCR.AsposeOcr();
            var settings = new Aspose.OCR.DocumentRecognitionSettings();

            var results = api.RecognizePdf(pdfPath, settings);

            int pageNum = 1;
            foreach (var result in results)
            {
                onPageProcessed(pageNum++, result.RecognitionText);
            }
        }
    }

    /// <summary>
    /// IronOCR multi-page - rich result model.
    /// </summary>
    public class IronOcrExample
    {
        public List<PageResult> ExtractAllPages(string pdfPath)
        {
            var result = new IronOcr.IronTesseract().Read(pdfPath);

            return result.Pages.Select((page, index) => new PageResult
            {
                PageNumber = index + 1,
                Text = page.Text,
                Confidence = (float)page.Confidence
            }).ToList();
        }

        public void ProcessPagesWithProgress(string pdfPath, Action<int, string> onPageProcessed)
        {
            var result = new IronOcr.IronTesseract().Read(pdfPath);

            foreach (var page in result.Pages)
            {
                onPageProcessed(page.PageNumber, page.Text);
            }
        }

        public void PrintDocumentStats(string pdfPath)
        {
            var result = new IronOcr.IronTesseract().Read(pdfPath);

            Console.WriteLine($"Total Pages: {result.Pages.Length}");
            Console.WriteLine($"Total Words: {result.Words.Length}");
            Console.WriteLine($"Overall Confidence: {result.Confidence:F1}%");

            foreach (var page in result.Pages)
            {
                Console.WriteLine($"  Page {page.PageNumber}: {page.Words.Length} words, {page.Confidence:F1}%");
            }
        }
    }
}


// ============================================================================
// EXAMPLE 3: Page Selection (Specific Pages Only)
// ============================================================================

namespace PageSelection
{
    /// <summary>
    /// Aspose.OCR page selection - 0-based indexing.
    /// </summary>
    public class AsposeExample
    {
        public string ExtractSpecificPages(string pdfPath, int startPage, int pageCount)
        {
            var api = new Aspose.OCR.AsposeOcr();

            // Note: Aspose uses 0-based page indexing
            var settings = new Aspose.OCR.DocumentRecognitionSettings
            {
                StartPage = startPage,  // 0-based: first page is 0
                PagesNumber = pageCount
            };

            var results = api.RecognizePdf(pdfPath, settings);

            var text = new StringBuilder();
            foreach (var result in results)
            {
                text.AppendLine(result.RecognitionText);
            }

            return text.ToString();
        }

        public string ExtractFirstPage(string pdfPath)
        {
            // Get only first page (0-based index)
            return ExtractSpecificPages(pdfPath, 0, 1);
        }

        public string ExtractPageRange(string pdfPath, int[] pageNumbers)
        {
            // Aspose doesn't support non-contiguous pages directly
            // Must process each page separately
            var api = new Aspose.OCR.AsposeOcr();
            var allText = new StringBuilder();

            foreach (var pageNum in pageNumbers)
            {
                var settings = new Aspose.OCR.DocumentRecognitionSettings
                {
                    StartPage = pageNum - 1,  // Convert to 0-based
                    PagesNumber = 1
                };

                var results = api.RecognizePdf(pdfPath, settings);
                foreach (var result in results)
                {
                    allText.AppendLine($"--- Page {pageNum} ---");
                    allText.AppendLine(result.RecognitionText);
                }
            }

            return allText.ToString();
        }
    }

    /// <summary>
    /// IronOCR page selection - 1-based indexing, flexible loading.
    /// </summary>
    public class IronOcrExample
    {
        public string ExtractSpecificPages(string pdfPath, int startPage, int endPage)
        {
            using var input = new IronOcr.OcrInput();

            // Note: IronOCR uses 1-based page indexing
            input.LoadPdfPages(pdfPath, startPage, endPage);

            return new IronOcr.IronTesseract().Read(input).Text;
        }

        public string ExtractFirstPage(string pdfPath)
        {
            // Get only first page (1-based index)
            return ExtractSpecificPages(pdfPath, 1, 1);
        }

        public string ExtractPageRange(string pdfPath, int[] pageNumbers)
        {
            using var input = new IronOcr.OcrInput();

            // IronOCR supports loading specific pages directly
            input.LoadPdfPages(pdfPath, pageNumbers);

            return new IronOcr.IronTesseract().Read(input).Text;
        }

        public string ExtractOddPagesOnly(string pdfPath, int totalPages)
        {
            using var input = new IronOcr.OcrInput();

            var oddPages = Enumerable.Range(1, totalPages)
                                     .Where(p => p % 2 == 1)
                                     .ToArray();

            input.LoadPdfPages(pdfPath, oddPages);

            return new IronOcr.IronTesseract().Read(input).Text;
        }
    }
}


// ============================================================================
// EXAMPLE 4: Password-Protected PDFs
// ============================================================================

namespace PasswordProtectedPdf
{
    /// <summary>
    /// Aspose.OCR cannot handle encrypted PDFs directly.
    /// Requires Aspose.PDF (separate license) to decrypt first.
    /// </summary>
    public class AsposeExample
    {
        public string ExtractFromProtectedPdf(string pdfPath, string password)
        {
            // Aspose.OCR alone CANNOT decrypt PDFs
            // You need Aspose.PDF (additional license cost)

            /*
            // Step 1: Decrypt with Aspose.PDF (requires separate license)
            using var pdfDoc = new Aspose.Pdf.Document(pdfPath, password);

            // Step 2: Convert pages to images (complex multi-step process)
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            var imageFiles = new List<string>();
            for (int i = 1; i <= pdfDoc.Pages.Count; i++)
            {
                var page = pdfDoc.Pages[i];
                var imagePath = Path.Combine(tempDir, $"page_{i}.png");

                using var stream = new FileStream(imagePath, FileMode.Create);
                var resolution = new Aspose.Pdf.Devices.Resolution(300);
                var pngDevice = new Aspose.Pdf.Devices.PngDevice(resolution);
                pngDevice.Process(page, stream);

                imageFiles.Add(imagePath);
            }

            // Step 3: OCR the images
            var api = new Aspose.OCR.AsposeOcr();
            var allText = new StringBuilder();

            foreach (var imagePath in imageFiles)
            {
                var result = api.RecognizeImage(imagePath, new Aspose.OCR.RecognitionSettings());
                allText.AppendLine(result.RecognitionText);
            }

            // Cleanup temp files
            Directory.Delete(tempDir, true);

            return allText.ToString();
            */

            throw new NotSupportedException(
                "Aspose.OCR requires Aspose.PDF (additional license) to handle password-protected PDFs. " +
                "This adds significant cost and complexity to your OCR workflow.");
        }
    }

    /// <summary>
    /// IronOCR has built-in password support - no additional license required.
    /// </summary>
    public class IronOcrExample
    {
        public string ExtractFromProtectedPdf(string pdfPath, string password)
        {
            // Built-in password parameter - no additional libraries or licenses
            using var input = new IronOcr.OcrInput();
            input.LoadPdf(pdfPath, Password: password);

            return new IronOcr.IronTesseract().Read(input).Text;
        }

        public string ExtractProtectedPdfPages(string pdfPath, string password, int[] pages)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadPdfPages(pdfPath, pages, Password: password);

            return new IronOcr.IronTesseract().Read(input).Text;
        }

        public bool TryExtractWithPassword(string pdfPath, string password, out string text)
        {
            try
            {
                using var input = new IronOcr.OcrInput();
                input.LoadPdf(pdfPath, Password: password);

                text = new IronOcr.IronTesseract().Read(input).Text;
                return true;
            }
            catch (Exception)
            {
                text = null;
                return false;
            }
        }
    }
}


// ============================================================================
// EXAMPLE 5: Creating Searchable PDFs
// ============================================================================

namespace SearchablePdfOutput
{
    /// <summary>
    /// Aspose.OCR searchable PDF creation.
    /// </summary>
    public class AsposeExample
    {
        public void ConvertScannedPdfToSearchable(string inputPdf, string outputPdf)
        {
            var api = new Aspose.OCR.AsposeOcr();
            var settings = new Aspose.OCR.DocumentRecognitionSettings();

            // Recognize all pages
            var results = api.RecognizePdf(inputPdf, settings);

            // Save with text layer embedded
            api.SaveMultipageDocument(
                outputPdf,
                Aspose.OCR.SaveFormat.Pdf,
                results.ToList());
        }

        public void CreateSearchablePdfFromImages(string[] imagePaths, string outputPdf)
        {
            var api = new Aspose.OCR.AsposeOcr();
            var settings = new Aspose.OCR.RecognitionSettings();

            var allResults = new List<Aspose.OCR.RecognitionResult>();

            foreach (var path in imagePaths)
            {
                var result = api.RecognizeImage(path, settings);
                allResults.Add(result);
            }

            // Combine into single searchable PDF
            api.SaveMultipageDocument(
                outputPdf,
                Aspose.OCR.SaveFormat.Pdf,
                allResults);
        }

        public void SaveAsDocx(string inputPdf, string outputDocx)
        {
            var api = new Aspose.OCR.AsposeOcr();
            var settings = new Aspose.OCR.DocumentRecognitionSettings();

            var results = api.RecognizePdf(inputPdf, settings);

            // Save as Word document
            api.SaveMultipageDocument(
                outputDocx,
                Aspose.OCR.SaveFormat.Docx,
                results.ToList());
        }
    }

    /// <summary>
    /// IronOCR searchable PDF - direct method with more options.
    /// </summary>
    public class IronOcrExample
    {
        public void ConvertScannedPdfToSearchable(string inputPdf, string outputPdf)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadPdf(inputPdf);

            var result = new IronOcr.IronTesseract().Read(input);

            // Direct save with original image + text layer
            result.SaveAsSearchablePdf(outputPdf);
        }

        public void CreateSearchablePdfFromImages(string[] imagePaths, string outputPdf)
        {
            using var input = new IronOcr.OcrInput();
            foreach (var path in imagePaths)
            {
                input.LoadImage(path);
            }

            var result = new IronOcr.IronTesseract().Read(input);
            result.SaveAsSearchablePdf(outputPdf);
        }

        public void SaveAsHtml(string inputPdf, string outputHtml)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadPdf(inputPdf);

            var result = new IronOcr.IronTesseract().Read(input);

            // Save as HTML with formatting preserved
            result.SaveAsHocrFile(outputHtml);
        }

        public void ExportToCsv(string inputPdf, string outputCsv)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadPdf(inputPdf);

            var result = new IronOcr.IronTesseract().Read(input);

            // Export structured data if detected
            File.WriteAllText(outputCsv, result.Text);
        }
    }
}


// ============================================================================
// EXAMPLE 6: Large PDF Optimization
// ============================================================================

namespace LargePdfProcessing
{
    /// <summary>
    /// Aspose.OCR large PDF handling.
    /// </summary>
    public class AsposeExample
    {
        public void ProcessLargePdfInChunks(string pdfPath, int pagesPerChunk, Action<int, string> onChunkComplete)
        {
            var api = new Aspose.OCR.AsposeOcr();

            // Get total pages (requires loading whole PDF metadata)
            int totalPages = GetPdfPageCount(pdfPath);
            int currentPage = 0;

            while (currentPage < totalPages)
            {
                var settings = new Aspose.OCR.DocumentRecognitionSettings
                {
                    StartPage = currentPage,
                    PagesNumber = Math.Min(pagesPerChunk, totalPages - currentPage)
                };

                var results = api.RecognizePdf(pdfPath, settings);

                var chunkText = new StringBuilder();
                foreach (var result in results)
                {
                    chunkText.AppendLine(result.RecognitionText);
                }

                onChunkComplete(currentPage / pagesPerChunk + 1, chunkText.ToString());

                currentPage += pagesPerChunk;

                // Manual memory cleanup for large PDFs
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private int GetPdfPageCount(string pdfPath)
        {
            // Would need Aspose.PDF or another library to get page count
            // without processing entire document
            throw new NotImplementedException(
                "Requires Aspose.PDF to get page count without full processing");
        }

        public void ProcessWithLowMemory(string pdfPath, int dpi = 150)
        {
            var api = new Aspose.OCR.AsposeOcr();

            // Lower DPI = less memory, but lower accuracy
            var settings = new Aspose.OCR.DocumentRecognitionSettings
            {
                // No built-in DPI control for PDFs in Aspose.OCR
                // Memory optimization is limited
            };

            var results = api.RecognizePdf(pdfPath, settings);
            // Process results...
        }
    }

    /// <summary>
    /// IronOCR large PDF - streaming and optimization options.
    /// </summary>
    public class IronOcrExample
    {
        public void ProcessLargePdfInChunks(string pdfPath, int pagesPerChunk, Action<int, string> onChunkComplete)
        {
            // Get page count using IronPdf (bundled) or load and count
            using var info = new IronOcr.OcrInput();
            info.LoadPdf(pdfPath);
            int totalPages = info.GetPages().Length;

            for (int start = 1; start <= totalPages; start += pagesPerChunk)
            {
                int end = Math.Min(start + pagesPerChunk - 1, totalPages);

                using var input = new IronOcr.OcrInput();
                input.LoadPdfPages(pdfPath, Enumerable.Range(start, end - start + 1).ToArray());

                var result = new IronOcr.IronTesseract().Read(input);

                onChunkComplete((start - 1) / pagesPerChunk + 1, result.Text);

                // OcrInput disposes automatically, freeing memory
            }
        }

        public void ProcessWithLowMemory(string pdfPath, int dpi = 150)
        {
            var ocr = new IronOcr.IronTesseract();

            using var input = new IronOcr.OcrInput();
            input.LoadPdf(pdfPath);

            // Control DPI for memory vs accuracy tradeoff
            input.TargetDpi = dpi;  // Lower = less memory

            var result = ocr.Read(input);
            // Process results...
        }

        public void ProcessWithTimeout(string pdfPath, TimeSpan timeout)
        {
            var ocr = new IronOcr.IronTesseract();
            ocr.Configuration.ReadTimeoutSeconds = (int)timeout.TotalSeconds;

            using var input = new IronOcr.OcrInput();
            input.LoadPdf(pdfPath);

            try
            {
                var result = ocr.Read(input);
                Console.WriteLine($"Processed in under {timeout.TotalSeconds}s");
            }
            catch (TimeoutException)
            {
                Console.WriteLine("PDF processing exceeded timeout");
            }
        }

        public async void ProcessPagesParallel(string pdfPath, int maxConcurrency = 4)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadPdf(pdfPath);

            var pages = input.GetPages();

            // IronOCR handles parallel processing internally
            // but you can also manually parallelize page processing

            var results = new System.Collections.Concurrent.ConcurrentBag<string>();

            await System.Threading.Tasks.Parallel.ForEachAsync(
                pages,
                new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = maxConcurrency },
                async (page, ct) =>
                {
                    using var pageInput = new IronOcr.OcrInput();
                    pageInput.AddPage(page);

                    var result = new IronOcr.IronTesseract().Read(pageInput);
                    results.Add(result.Text);
                });

            Console.WriteLine($"Processed {pages.Length} pages with {maxConcurrency} threads");
        }
    }
}


// ============================================================================
// EXAMPLE 7: PDF Quality Enhancement Before OCR
// ============================================================================

namespace PdfQualityEnhancement
{
    /// <summary>
    /// Aspose.OCR PDF preprocessing options.
    /// </summary>
    public class AsposeExample
    {
        public string EnhancedPdfOcr(string pdfPath)
        {
            var api = new Aspose.OCR.AsposeOcr();

            // Build preprocessing filter chain
            var filters = new Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter();
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.AutoSkew());
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.AutoDenoising());
            filters.Add(Aspose.OCR.Models.PreprocessingFilters.PreprocessingFilter.ContrastCorrectionFilter());

            var settings = new Aspose.OCR.DocumentRecognitionSettings
            {
                PreprocessingFilters = filters
            };

            var results = api.RecognizePdf(pdfPath, settings);

            var text = new StringBuilder();
            foreach (var result in results)
            {
                text.AppendLine(result.RecognitionText);
            }

            return text.ToString();
        }
    }

    /// <summary>
    /// IronOCR PDF preprocessing - fluent API.
    /// </summary>
    public class IronOcrExample
    {
        public string EnhancedPdfOcr(string pdfPath)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadPdf(pdfPath);

            // Apply preprocessing to improve quality
            input.Deskew()         // Fix rotation
                 .DeNoise()        // Remove speckles
                 .Contrast()       // Enhance contrast
                 .Sharpen();       // Sharpen text

            return new IronOcr.IronTesseract().Read(input).Text;
        }

        public string ProcessPoorQualityPdf(string pdfPath)
        {
            using var input = new IronOcr.OcrInput();
            input.LoadPdf(pdfPath);

            // Aggressive cleanup for poor scans
            input.DeNoise()
                 .Invert()          // If white text on black
                 .Dilate()          // Thicken thin text
                 .Scale(200);       // Upscale for better recognition

            return new IronOcr.IronTesseract().Read(input).Text;
        }
    }
}
