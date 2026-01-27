/**
 * Syncfusion OCR PDF Extraction Patterns
 *
 * Demonstrates PDF OCR workflows using Syncfusion's Essential Studio PDF library.
 * Note: Syncfusion OCR requires tessdata files (manual download) and is part of the
 * Essential Studio suite (1,600+ components).
 *
 * Install:
 *   dotnet add package Syncfusion.PDF.OCR.Net.Core
 *
 * tessdata Setup (REQUIRED):
 *   Download from: https://github.com/tesseract-ocr/tessdata_best
 *   Place in: /tessdata/eng.traineddata (and other languages as needed)
 *
 * License:
 *   - Community: <$1M revenue, <=5 devs, <=10 employees, never received >$3M funding
 *   - Commercial: $995-$1,595/developer/year for Essential Studio suite
 *
 * IronOCR Alternative: https://ironsoftware.com/csharp/ocr/
 *   - No tessdata required
 *   - Direct image OCR (no PDF conversion needed)
 *   - One-time perpetual licensing
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

// Syncfusion namespace imports
// using Syncfusion.OCRProcessor;
// using Syncfusion.Pdf;
// using Syncfusion.Pdf.Parsing;

namespace SyncfusionOcrPdfExamples
{
    // ============================================================================
    // BASIC PDF OCR EXTRACTION
    // ============================================================================

    /// <summary>
    /// Basic PDF OCR extraction workflow.
    /// Note: tessdata folder must exist with appropriate .traineddata files.
    /// </summary>
    public class BasicPdfExtraction
    {
        // Path to tessdata directory - must contain .traineddata files
        // This is a Tesseract dependency that Syncfusion does not bundle
        private const string TessDataPath = @"tessdata/";

        /*
        /// <summary>
        /// Extract text from a scanned PDF document.
        /// </summary>
        public string ExtractTextFromPdf(string pdfPath)
        {
            // Load the existing PDF document
            using var document = new PdfLoadedDocument(pdfPath);

            // Create OCR processor with tessdata path
            // IMPORTANT: tessdata files must be downloaded separately
            using var processor = new OCRProcessor(TessDataPath);

            // Configure language (must match .traineddata file)
            processor.Settings.Language = Languages.English;

            // Perform OCR - this adds a text layer to the PDF
            processor.PerformOCR(document);

            // Extract text from each page
            // Note: Two-step process - OCR then extract
            var text = new StringBuilder();
            foreach (PdfLoadedPage page in document.Pages)
            {
                string pageText = page.ExtractText();
                text.AppendLine(pageText);
            }

            return text.ToString();
        }
        */

        public void ShowWorkflowDescription()
        {
            Console.WriteLine("Syncfusion PDF OCR Workflow:");
            Console.WriteLine("1. Download tessdata files manually");
            Console.WriteLine("2. Load PDF with PdfLoadedDocument");
            Console.WriteLine("3. Create OCRProcessor with tessdata path");
            Console.WriteLine("4. Configure language settings");
            Console.WriteLine("5. Call PerformOCR(document)");
            Console.WriteLine("6. Iterate pages and call ExtractText()");
            Console.WriteLine();
            Console.WriteLine("Note: Community license requires <$1M revenue");
        }
    }

    // ============================================================================
    // MULTI-PAGE PDF PROCESSING WITH PROGRESS
    // ============================================================================

    /// <summary>
    /// Process multi-page PDFs with progress tracking.
    /// Demonstrates page-by-page OCR for large documents.
    /// </summary>
    public class MultiPagePdfProcessor
    {
        private const string TessDataPath = @"tessdata/";

        /*
        /// <summary>
        /// Process large PDF with page-by-page progress.
        /// Memory consideration: Process pages individually to avoid loading entire document.
        /// </summary>
        public IEnumerable<PageResult> ProcessWithProgress(string pdfPath, IProgress<int> progress)
        {
            using var document = new PdfLoadedDocument(pdfPath);
            using var processor = new OCRProcessor(TessDataPath);

            processor.Settings.Language = Languages.English;

            int totalPages = document.Pages.Count;
            var results = new List<PageResult>();

            for (int i = 0; i < totalPages; i++)
            {
                // Process single page
                var page = document.Pages[i] as PdfLoadedPage;

                // Create temporary document for single page OCR
                using var tempDoc = new PdfDocument();
                tempDoc.ImportPage(document, i);

                processor.PerformOCR(tempDoc);

                // Extract text from processed page
                var processedPage = tempDoc.Pages[0] as PdfLoadedPage;
                string text = processedPage?.ExtractText() ?? string.Empty;

                results.Add(new PageResult
                {
                    PageNumber = i + 1,
                    Text = text,
                    WordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
                });

                // Report progress
                progress?.Report((int)((i + 1) / (double)totalPages * 100));
            }

            return results;
        }

        /// <summary>
        /// Extract text from specific page range.
        /// </summary>
        public string ExtractPageRange(string pdfPath, int startPage, int endPage)
        {
            using var document = new PdfLoadedDocument(pdfPath);
            using var processor = new OCRProcessor(TessDataPath);

            processor.Settings.Language = Languages.English;

            var text = new StringBuilder();

            // Validate page range
            int totalPages = document.Pages.Count;
            startPage = Math.Max(0, startPage);
            endPage = Math.Min(totalPages - 1, endPage);

            for (int i = startPage; i <= endPage; i++)
            {
                var page = document.Pages[i] as PdfLoadedPage;

                // Create temp document for page
                using var tempDoc = new PdfDocument();
                tempDoc.ImportPage(document, i);

                processor.PerformOCR(tempDoc);

                var processedPage = tempDoc.Pages[0] as PdfLoadedPage;
                text.AppendLine($"--- Page {i + 1} ---");
                text.AppendLine(processedPage?.ExtractText() ?? string.Empty);
            }

            return text.ToString();
        }
        */

        public class PageResult
        {
            public int PageNumber { get; set; }
            public string Text { get; set; } = string.Empty;
            public int WordCount { get; set; }
        }
    }

    // ============================================================================
    // MULTI-LANGUAGE PDF OCR
    // ============================================================================

    /// <summary>
    /// Process PDFs containing multiple languages.
    /// IMPORTANT: Each language requires separate .traineddata file in tessdata folder.
    /// </summary>
    public class MultiLanguagePdfProcessor
    {
        private const string TessDataPath = @"tessdata/";

        /*
        /// <summary>
        /// Extract text from document with multiple languages.
        /// Requires: eng.traineddata, fra.traineddata, deu.traineddata in tessdata folder.
        /// </summary>
        public string ExtractMultiLanguage(string pdfPath, Languages languages)
        {
            using var document = new PdfLoadedDocument(pdfPath);
            using var processor = new OCRProcessor(TessDataPath);

            // Configure for multiple languages
            // tessdata folder must contain ALL specified language files
            processor.Settings.Language = languages;

            processor.PerformOCR(document);

            var text = new StringBuilder();
            foreach (PdfLoadedPage page in document.Pages)
            {
                text.AppendLine(page.ExtractText());
            }

            return text.ToString();
        }

        /// <summary>
        /// Example: English and French document.
        /// </summary>
        public string ExtractEnglishFrench(string pdfPath)
        {
            // Requires tessdata/eng.traineddata AND tessdata/fra.traineddata
            return ExtractMultiLanguage(pdfPath, Languages.English | Languages.French);
        }

        /// <summary>
        /// Example: German and English document.
        /// </summary>
        public string ExtractGermanEnglish(string pdfPath)
        {
            // Requires tessdata/deu.traineddata AND tessdata/eng.traineddata
            return ExtractMultiLanguage(pdfPath, Languages.German | Languages.English);
        }
        */

        public void ShowLanguageSetup()
        {
            Console.WriteLine("Multi-Language Setup (Syncfusion/Tesseract):");
            Console.WriteLine();
            Console.WriteLine("tessdata folder structure:");
            Console.WriteLine("  tessdata/");
            Console.WriteLine("    eng.traineddata  (15-50MB)");
            Console.WriteLine("    fra.traineddata  (15-50MB)");
            Console.WriteLine("    deu.traineddata  (15-50MB)");
            Console.WriteLine("    spa.traineddata  (15-50MB)");
            Console.WriteLine("    chi_sim.traineddata (50MB+)");
            Console.WriteLine();
            Console.WriteLine("Download from: https://github.com/tesseract-ocr/tessdata_best");
            Console.WriteLine();
            Console.WriteLine("Note: IronOCR bundles 125+ languages - no manual download needed");
        }
    }

    // ============================================================================
    // SEARCHABLE PDF CREATION
    // ============================================================================

    /// <summary>
    /// Convert scanned PDFs to searchable PDFs with text layer.
    /// </summary>
    public class SearchablePdfCreator
    {
        private const string TessDataPath = @"tessdata/";

        /*
        /// <summary>
        /// Create searchable PDF from scanned PDF.
        /// Adds invisible text layer over the image for search/copy functionality.
        /// </summary>
        public void CreateSearchablePdf(string inputPath, string outputPath)
        {
            using var document = new PdfLoadedDocument(inputPath);
            using var processor = new OCRProcessor(TessDataPath);

            processor.Settings.Language = Languages.English;

            // Perform OCR - adds text layer
            processor.PerformOCR(document);

            // Save modified PDF
            using var outputStream = new FileStream(outputPath, FileMode.Create);
            document.Save(outputStream);
        }

        /// <summary>
        /// Create searchable PDF with layout preservation.
        /// </summary>
        public void CreateSearchablePdfWithLayout(string inputPath, string outputPath)
        {
            using var document = new PdfLoadedDocument(inputPath);
            using var processor = new OCRProcessor(TessDataPath);

            processor.Settings.Language = Languages.English;
            // Layout settings may be available in newer versions
            // processor.Settings.PageSegmentMode = PageSegMode.AutoOsd;

            processor.PerformOCR(document);

            using var outputStream = new FileStream(outputPath, FileMode.Create);
            document.Save(outputStream);
        }
        */

        public void ShowSearchablePdfWorkflow()
        {
            Console.WriteLine("Creating Searchable PDF with Syncfusion:");
            Console.WriteLine("1. Load scanned PDF");
            Console.WriteLine("2. Configure OCR processor with tessdata path");
            Console.WriteLine("3. Call PerformOCR() - adds text layer");
            Console.WriteLine("4. Save document - text is now searchable/selectable");
        }
    }

    // ============================================================================
    // MEMORY MANAGEMENT FOR LARGE PDFs
    // ============================================================================

    /// <summary>
    /// Memory-efficient processing for large PDF documents.
    /// Critical for production systems handling high-page-count documents.
    /// </summary>
    public class LargePdfMemoryManager
    {
        private const string TessDataPath = @"tessdata/";

        /*
        /// <summary>
        /// Process large PDF in chunks to manage memory.
        /// Disposes resources after each chunk to prevent memory buildup.
        /// </summary>
        public async Task<string> ProcessLargePdfInChunks(
            string pdfPath,
            int chunkSize = 10)
        {
            var allText = new StringBuilder();

            // Get total page count
            int totalPages;
            using (var doc = new PdfLoadedDocument(pdfPath))
            {
                totalPages = doc.Pages.Count;
            }

            // Process in chunks
            for (int startPage = 0; startPage < totalPages; startPage += chunkSize)
            {
                int endPage = Math.Min(startPage + chunkSize - 1, totalPages - 1);

                // Process chunk
                string chunkText = await ProcessChunk(pdfPath, startPage, endPage);
                allText.AppendLine(chunkText);

                // Force garbage collection between chunks
                // Important for very large documents
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Optional: yield to prevent blocking
                await Task.Delay(10);
            }

            return allText.ToString();
        }

        private async Task<string> ProcessChunk(string pdfPath, int startPage, int endPage)
        {
            return await Task.Run(() =>
            {
                using var document = new PdfLoadedDocument(pdfPath);
                using var processor = new OCRProcessor(TessDataPath);

                processor.Settings.Language = Languages.English;

                var text = new StringBuilder();

                for (int i = startPage; i <= endPage; i++)
                {
                    // Create temporary single-page document
                    using var tempDoc = new PdfDocument();
                    tempDoc.ImportPage(document, i);

                    processor.PerformOCR(tempDoc);

                    var page = tempDoc.Pages[0] as PdfLoadedPage;
                    text.AppendLine(page?.ExtractText() ?? string.Empty);
                }

                return text.ToString();
            });
        }
        */

        public void ShowMemoryConsiderations()
        {
            Console.WriteLine("Memory Management for Large PDFs (Syncfusion):");
            Console.WriteLine();
            Console.WriteLine("Challenges:");
            Console.WriteLine("- OCR loads images into memory");
            Console.WriteLine("- Multi-page PDFs can exhaust memory");
            Console.WriteLine("- Tesseract has per-page memory overhead");
            Console.WriteLine();
            Console.WriteLine("Strategies:");
            Console.WriteLine("1. Process pages in chunks (10-20 pages)");
            Console.WriteLine("2. Dispose documents immediately after use");
            Console.WriteLine("3. Force GC between large operations");
            Console.WriteLine("4. Consider splitting very large PDFs");
            Console.WriteLine();
            Console.WriteLine("Memory per page: 50-200MB depending on resolution");
        }
    }

    // ============================================================================
    // ERROR HANDLING
    // ============================================================================

    /// <summary>
    /// Error handling patterns for Syncfusion OCR.
    /// Common errors include missing tessdata and license issues.
    /// </summary>
    public class OcrErrorHandler
    {
        private const string TessDataPath = @"tessdata/";

        /*
        /// <summary>
        /// Robust OCR with comprehensive error handling.
        /// </summary>
        public OcrResult ExtractWithErrorHandling(string pdfPath)
        {
            var result = new OcrResult();

            // Check tessdata exists
            if (!Directory.Exists(TessDataPath))
            {
                result.Success = false;
                result.Error = $"tessdata directory not found at: {TessDataPath}";
                result.ErrorType = OcrErrorType.TessDataMissing;
                return result;
            }

            // Check for English traineddata (most common)
            if (!File.Exists(Path.Combine(TessDataPath, "eng.traineddata")))
            {
                result.Success = false;
                result.Error = "eng.traineddata not found. Download from tessdata repository.";
                result.ErrorType = OcrErrorType.LanguageFileMissing;
                return result;
            }

            try
            {
                using var document = new PdfLoadedDocument(pdfPath);
                using var processor = new OCRProcessor(TessDataPath);

                processor.Settings.Language = Languages.English;
                processor.PerformOCR(document);

                var text = new StringBuilder();
                foreach (PdfLoadedPage page in document.Pages)
                {
                    text.AppendLine(page.ExtractText());
                }

                result.Success = true;
                result.Text = text.ToString();
            }
            catch (PdfException ex) when (ex.Message.Contains("password"))
            {
                result.Success = false;
                result.Error = "PDF is password protected";
                result.ErrorType = OcrErrorType.PasswordProtected;
            }
            catch (Exception ex) when (ex.Message.Contains("license"))
            {
                result.Success = false;
                result.Error = "License error - verify community license eligibility or commercial license";
                result.ErrorType = OcrErrorType.LicenseError;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                result.ErrorType = OcrErrorType.Unknown;
            }

            return result;
        }
        */

        public class OcrResult
        {
            public bool Success { get; set; }
            public string Text { get; set; } = string.Empty;
            public string Error { get; set; } = string.Empty;
            public OcrErrorType ErrorType { get; set; }
        }

        public enum OcrErrorType
        {
            None,
            TessDataMissing,
            LanguageFileMissing,
            PasswordProtected,
            LicenseError,
            Unknown
        }

        public void ShowCommonErrors()
        {
            Console.WriteLine("Common Syncfusion OCR Errors:");
            Console.WriteLine();
            Console.WriteLine("1. 'tessdata directory not found'");
            Console.WriteLine("   Fix: Download tessdata files and place in correct path");
            Console.WriteLine();
            Console.WriteLine("2. '{language}.traineddata not found'");
            Console.WriteLine("   Fix: Download specific language file from tessdata repo");
            Console.WriteLine();
            Console.WriteLine("3. License errors");
            Console.WriteLine("   Fix: Verify community license eligibility or commercial key");
            Console.WriteLine();
            Console.WriteLine("4. Out of memory");
            Console.WriteLine("   Fix: Process large PDFs in chunks");
        }
    }

    // ============================================================================
    // COMPARISON: SYNCFUSION vs IRONOCR
    // ============================================================================

    /// <summary>
    /// Side-by-side comparison showing the complexity difference.
    /// </summary>
    public class SyncfusionVsIronOcrComparison
    {
        public void ShowComparison()
        {
            Console.WriteLine("=== PDF OCR COMPARISON ===\n");

            Console.WriteLine("SYNCFUSION (Essential Studio):");
            Console.WriteLine("  1. Download tessdata files manually");
            Console.WriteLine("  2. Configure tessdata path");
            Console.WriteLine("  3. Load PDF with PdfLoadedDocument");
            Console.WriteLine("  4. Create OCRProcessor with tessdata path");
            Console.WriteLine("  5. Set language matching .traineddata files");
            Console.WriteLine("  6. Call PerformOCR(document)");
            Console.WriteLine("  7. Iterate pages calling ExtractText()");
            Console.WriteLine();
            Console.WriteLine("  License: $995-$1,595/dev/year (entire suite)");
            Console.WriteLine("  Community: <$1M revenue, <=5 devs, <=10 employees");
            Console.WriteLine();

            Console.WriteLine("IRONOCR:");
            Console.WriteLine("  1. var text = new IronTesseract().Read(pdfPath).Text;");
            Console.WriteLine();
            Console.WriteLine("  License: From $749 one-time");
            Console.WriteLine("  No tessdata management required");
            Console.WriteLine();

            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }
    }
}

// ============================================================================
// IRONOCR - SIMPLER ALTERNATIVE
// Get IronOCR: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace IronOcrPdfExamples
{
    using IronOcr;

    /// <summary>
    /// IronOCR equivalent showing dramatically simpler PDF OCR.
    /// No tessdata required - all languages built in.
    /// </summary>
    public class IronOcrPdfExtraction
    {
        /// <summary>
        /// PDF OCR in one line - no tessdata, no setup.
        /// </summary>
        public string ExtractFromPdf(string pdfPath)
        {
            return new IronTesseract().Read(pdfPath).Text;
        }

        /// <summary>
        /// Multi-page with progress.
        /// </summary>
        public string ExtractWithPageInfo(string pdfPath)
        {
            var ocr = new IronTesseract();
            using var input = new OcrInput();
            input.LoadPdf(pdfPath);

            var result = ocr.Read(input);

            var text = new StringBuilder();
            for (int i = 0; i < result.Pages.Length; i++)
            {
                text.AppendLine($"--- Page {i + 1} ---");
                text.AppendLine(result.Pages[i].Text);
            }

            return text.ToString();
        }

        /// <summary>
        /// Multi-language - languages are built-in.
        /// </summary>
        public string ExtractMultiLanguage(string pdfPath)
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.English;
            ocr.AddSecondaryLanguage(OcrLanguage.French);
            ocr.AddSecondaryLanguage(OcrLanguage.German);

            return ocr.Read(pdfPath).Text;
        }

        /// <summary>
        /// Create searchable PDF.
        /// </summary>
        public void CreateSearchablePdf(string inputPath, string outputPath)
        {
            var ocr = new IronTesseract();
            using var input = new OcrInput();
            input.LoadPdf(inputPath);

            var result = ocr.Read(input);
            result.SaveAsSearchablePdf(outputPath);
        }
    }
}

// ============================================================================
// TRY IRONOCR - NO TESSDATA REQUIRED
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// ============================================================================
