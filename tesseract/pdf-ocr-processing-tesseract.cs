/**
 * PDF OCR Processing: Tesseract .NET vs IronOCR
 *
 * This example demonstrates OCR on PDF documents - a critical enterprise use case
 * that highlights significant differences between Tesseract and IronOCR.
 *
 * Key Insight:
 * - Tesseract CANNOT process PDFs directly - it's image-only
 * - You need additional libraries to convert PDF pages to images first
 * - IronOCR handles PDFs natively with no additional dependencies
 *
 * Additional Libraries Required for Tesseract:
 * - PdfiumViewer, PDFtoImage, or Docnet.Core for PDF rendering
 * - GhostScript (native dependency) for some solutions
 * - ImageMagick (native dependency) alternative
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

// ============================================================================
// TESSERACT PDF PROCESSING - Requires External PDF Library
// ============================================================================

namespace TesseractPdfProcessing
{
    using Tesseract;

    /// <summary>
    /// PDF OCR with Tesseract requires a multi-step process:
    /// 1. Convert PDF to images (using external library)
    /// 2. Preprocess each image
    /// 3. Run Tesseract on each image
    /// 4. Combine results
    /// </summary>
    public class PdfToImageToOcr
    {
        private const string TessDataPath = @"./tessdata";

        /// <summary>
        /// Process PDF using PdfiumViewer for PDF-to-image conversion.
        /// Requires: PdfiumViewer NuGet package + pdfium native binaries
        /// </summary>
        public static string ExtractFromPdfWithPdfium(string pdfPath)
        {
            /*
            // NuGet: PdfiumViewer, PdfiumViewer.Native.x64 (or x86)
            // Native pdfium.dll must be deployed with application

            using PdfiumViewer;

            var results = new List<string>();

            using (var document = PdfDocument.Load(pdfPath))
            {
                using (var engine = new TesseractEngine(TessDataPath, "eng", EngineMode.Default))
                {
                    for (int pageIndex = 0; pageIndex < document.PageCount; pageIndex++)
                    {
                        // Render page to image at 300 DPI
                        using (var pageImage = document.Render(pageIndex, 300, 300,
                            PdfRenderFlags.CorrectFromDpi))
                        {
                            // Save to temp file (Tesseract requires file path)
                            string tempPath = Path.GetTempFileName() + ".png";
                            try
                            {
                                pageImage.Save(tempPath);

                                using (var img = Pix.LoadFromFile(tempPath))
                                {
                                    using (var page = engine.Process(img))
                                    {
                                        results.Add(page.GetText());
                                    }
                                }
                            }
                            finally
                            {
                                File.Delete(tempPath);
                            }
                        }
                    }
                }
            }

            return string.Join("\n\n--- Page Break ---\n\n", results);
            */

            throw new NotImplementedException(
                "Requires PdfiumViewer + native pdfium.dll. " +
                "See comments for implementation details.");
        }

        /// <summary>
        /// Process PDF using PDFtoImage library.
        /// Requires: PDFtoImage NuGet package
        /// </summary>
        public static string ExtractFromPdfWithPdfToImage(string pdfPath)
        {
            /*
            // NuGet: PDFtoImage

            using PDFtoImage;

            var results = new List<string>();

            // Get page count
            int pageCount = Conversion.GetPageCount(pdfPath);

            using (var engine = new TesseractEngine(TessDataPath, "eng", EngineMode.Default))
            {
                for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
                {
                    // Render page to SKBitmap
                    using (var bitmap = Conversion.ToImage(pdfPath, pageIndex, dpi: 300))
                    {
                        // Convert SkiaSharp bitmap to byte array
                        using (var data = bitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
                        {
                            byte[] imageBytes = data.ToArray();

                            using (var img = Pix.LoadFromMemory(imageBytes))
                            {
                                using (var page = engine.Process(img))
                                {
                                    results.Add(page.GetText());
                                }
                            }
                        }
                    }
                }
            }

            return string.Join("\n\n--- Page Break ---\n\n", results);
            */

            throw new NotImplementedException(
                "Requires PDFtoImage NuGet package. " +
                "See comments for implementation details.");
        }

        /// <summary>
        /// Process PDF using Docnet.Core (Docnet).
        /// Requires: Docnet.Core NuGet package
        /// </summary>
        public static string ExtractFromPdfWithDocnet(string pdfPath)
        {
            /*
            // NuGet: Docnet.Core

            using Docnet.Core;
            using Docnet.Core.Models;

            var results = new List<string>();

            using (var library = DocLib.Instance)
            {
                using (var docReader = library.GetDocReader(pdfPath,
                    new PageDimensions(1.0)))  // Scale factor
                {
                    using (var engine = new TesseractEngine(TessDataPath, "eng", EngineMode.Default))
                    {
                        for (int pageIndex = 0; pageIndex < docReader.GetPageCount(); pageIndex++)
                        {
                            using (var pageReader = docReader.GetPageReader(pageIndex))
                            {
                                // Get raw image bytes
                                var bytes = pageReader.GetImage();
                                int width = pageReader.GetPageWidth();
                                int height = pageReader.GetPageHeight();

                                // Convert BGRA to Bitmap
                                using (var bitmap = new Bitmap(width, height,
                                    System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                                {
                                    var bmpData = bitmap.LockBits(
                                        new Rectangle(0, 0, width, height),
                                        System.Drawing.Imaging.ImageLockMode.WriteOnly,
                                        bitmap.PixelFormat);

                                    System.Runtime.InteropServices.Marshal.Copy(
                                        bytes, 0, bmpData.Scan0, bytes.Length);

                                    bitmap.UnlockBits(bmpData);

                                    // Save to temp file
                                    string tempPath = Path.GetTempFileName() + ".png";
                                    try
                                    {
                                        bitmap.Save(tempPath);

                                        using (var img = Pix.LoadFromFile(tempPath))
                                        {
                                            using (var page = engine.Process(img))
                                            {
                                                results.Add(page.GetText());
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        File.Delete(tempPath);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return string.Join("\n\n--- Page Break ---\n\n", results);
            */

            throw new NotImplementedException(
                "Requires Docnet.Core NuGet package. " +
                "See comments for implementation details.");
        }
    }

    /// <summary>
    /// PDF OCR with GhostScript backend.
    /// Requires GhostScript installed on the system.
    /// </summary>
    public class GhostScriptPdfOcr
    {
        private const string TessDataPath = @"./tessdata";

        /// <summary>
        /// Convert PDF to images using GhostScript command line.
        /// Requires: GhostScript installed on system
        /// </summary>
        public static string ExtractWithGhostScript(string pdfPath)
        {
            /*
            // GhostScript must be installed:
            // Windows: Download from https://www.ghostscript.com/
            // Linux: apt-get install ghostscript
            // macOS: brew install ghostscript

            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                // Convert PDF to PNG images using GhostScript
                var gsPath = @"C:\Program Files\gs\gs10.02.1\bin\gswin64c.exe"; // Windows
                // Linux/macOS: gsPath = "gs";

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = gsPath,
                    Arguments = $"-dBATCH -dNOPAUSE -sDEVICE=png16m -r300 " +
                                $"-sOutputFile=\"{tempDir}\\page_%03d.png\" \"{pdfPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(startInfo))
                {
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                        throw new Exception("GhostScript conversion failed");
                }

                // OCR each page
                var results = new List<string>();
                var pageFiles = Directory.GetFiles(tempDir, "page_*.png");
                Array.Sort(pageFiles);

                using (var engine = new TesseractEngine(TessDataPath, "eng", EngineMode.Default))
                {
                    foreach (var pageFile in pageFiles)
                    {
                        using (var img = Pix.LoadFromFile(pageFile))
                        {
                            using (var page = engine.Process(img))
                            {
                                results.Add(page.GetText());
                            }
                        }
                    }
                }

                return string.Join("\n\n--- Page Break ---\n\n", results);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
            */

            throw new NotImplementedException(
                "Requires GhostScript installed on system. " +
                "See comments for implementation details.");
        }
    }

    /// <summary>
    /// Password-protected PDF handling with Tesseract.
    /// </summary>
    public class PasswordProtectedPdf
    {
        /// <summary>
        /// Tesseract cannot handle encrypted PDFs at all.
        /// You need a PDF library that supports decryption.
        /// </summary>
        public static string ExtractFromProtectedPdf(string pdfPath, string password)
        {
            /*
            // Using iTextSharp (AGPL) or iText 7 (commercial)
            // Or PDFsharp (MIT)

            // Step 1: Decrypt PDF
            // Step 2: Convert to images
            // Step 3: Run Tesseract on images

            // This is complex and requires proper licensing considerations
            */

            throw new NotImplementedException(
                "Requires PDF library with encryption support (iText, PDFsharp). " +
                "Tesseract cannot decrypt PDFs.");
        }
    }
}


// ============================================================================
// IRONOCR PDF PROCESSING - Native Support, Zero Dependencies
// ============================================================================

namespace IronOcrPdfProcessing
{
    using IronOcr;

    /// <summary>
    /// IronOCR handles PDFs natively with no additional libraries.
    /// Supports both image-based and mixed (text + image) PDFs.
    /// </summary>
    public class NativePdfOcr
    {
        /// <summary>
        /// Basic PDF OCR - one line of code.
        /// </summary>
        public static string ExtractFromPdf(string pdfPath)
        {
            // IronOCR handles PDF internally - no conversion needed
            var result = new IronTesseract().Read(pdfPath);
            return result.Text;
        }

        /// <summary>
        /// Extract from specific pages.
        /// </summary>
        public static string ExtractFromPages(string pdfPath, int[] pageNumbers)
        {
            using (var input = new OcrInput())
            {
                // Page numbers are 1-based in IronOCR
                input.LoadPdf(pdfPath, PageSelection: pageNumbers);

                var result = new IronTesseract().Read(input);
                return result.Text;
            }
        }

        /// <summary>
        /// Extract from page range.
        /// </summary>
        public static string ExtractPageRange(string pdfPath, int startPage, int endPage)
        {
            using (var input = new OcrInput())
            {
                input.LoadPdfPages(pdfPath, startPage, endPage);

                var result = new IronTesseract().Read(input);
                return result.Text;
            }
        }

        /// <summary>
        /// Password-protected PDF - built-in support.
        /// </summary>
        public static string ExtractFromProtectedPdf(string pdfPath, string password)
        {
            using (var input = new OcrInput())
            {
                // Password support is built-in
                input.LoadPdf(pdfPath, Password: password);

                var result = new IronTesseract().Read(input);
                return result.Text;
            }
        }

        /// <summary>
        /// Process PDF with preprocessing.
        /// </summary>
        public static string ExtractWithPreprocessing(string pdfPath)
        {
            using (var input = new OcrInput())
            {
                input.LoadPdf(pdfPath);

                // Apply preprocessing to PDF pages automatically
                input.Deskew();
                input.DeNoise();
                input.EnhanceResolution();

                var result = new IronTesseract().Read(input);
                return result.Text;
            }
        }
    }

    /// <summary>
    /// Advanced PDF features in IronOCR.
    /// </summary>
    public class AdvancedPdfFeatures
    {
        /// <summary>
        /// Extract text page-by-page with metadata.
        /// </summary>
        public static void ExtractWithPageInfo(string pdfPath)
        {
            var ocr = new IronTesseract();

            using (var input = new OcrInput())
            {
                input.LoadPdf(pdfPath);
                var result = ocr.Read(input);

                Console.WriteLine($"Total pages: {result.Pages.Length}");
                Console.WriteLine($"Overall confidence: {result.Confidence:P1}");
                Console.WriteLine();

                foreach (var page in result.Pages)
                {
                    Console.WriteLine($"Page {page.PageNumber}:");
                    Console.WriteLine($"  Dimensions: {page.Width} x {page.Height}");
                    Console.WriteLine($"  Confidence: {page.Confidence:P1}");
                    Console.WriteLine($"  Words: {page.Words.Length}");
                    Console.WriteLine($"  Text preview: {page.Text.Substring(0, Math.Min(100, page.Text.Length))}...");
                    Console.WriteLine();
                }
            }
        }

        /// <summary>
        /// Create searchable PDF from scanned PDF.
        /// </summary>
        public static void CreateSearchablePdf(string inputPdf, string outputPdf)
        {
            var ocr = new IronTesseract();

            using (var input = new OcrInput())
            {
                input.LoadPdf(inputPdf);
                var result = ocr.Read(input);

                // Save as searchable PDF with text layer
                result.SaveAsSearchablePdf(outputPdf);

                Console.WriteLine($"Searchable PDF created: {outputPdf}");
            }
        }

        /// <summary>
        /// Process large PDFs with memory management.
        /// </summary>
        public static void ProcessLargePdf(string pdfPath, int batchSize = 10)
        {
            var ocr = new IronTesseract();
            var allText = new System.Text.StringBuilder();

            using (var input = new OcrInput())
            {
                input.LoadPdf(pdfPath);
                int totalPages = input.PageCount;

                // Process in batches to manage memory
                for (int i = 0; i < totalPages; i += batchSize)
                {
                    int endPage = Math.Min(i + batchSize, totalPages);
                    Console.WriteLine($"Processing pages {i + 1} to {endPage}...");

                    using (var batchInput = new OcrInput())
                    {
                        batchInput.LoadPdfPages(pdfPath, i + 1, endPage);
                        var result = ocr.Read(batchInput);
                        allText.AppendLine(result.Text);
                    }

                    // Force garbage collection between batches
                    GC.Collect();
                }
            }

            Console.WriteLine($"Total text extracted: {allText.Length} characters");
        }

        /// <summary>
        /// Extract structured data from PDF tables.
        /// </summary>
        public static void ExtractTables(string pdfPath)
        {
            var ocr = new IronTesseract();

            using (var input = new OcrInput())
            {
                input.LoadPdf(pdfPath);
                var result = ocr.Read(input);

                // Access table data
                foreach (var page in result.Pages)
                {
                    // Lines can be used to detect table rows
                    foreach (var line in page.Lines)
                    {
                        // Words in a line represent potential table cells
                        var cells = line.Words;

                        // Use X coordinates to detect columns
                        Console.WriteLine($"Line at Y={line.Y}: {line.Text}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Mixed PDF handling (native text + scanned images).
    /// </summary>
    public class MixedPdfProcessing
    {
        /// <summary>
        /// Handle PDFs with both native text and scanned pages.
        /// </summary>
        public static string ProcessMixedPdf(string pdfPath)
        {
            // IronOCR automatically handles mixed PDFs:
            // - Extracts native text from text layers
            // - Applies OCR to image-only pages
            // - Combines results seamlessly

            var result = new IronTesseract().Read(pdfPath);
            return result.Text;
        }

        /// <summary>
        /// Check if PDF page needs OCR.
        /// </summary>
        public static void AnalyzePdfPages(string pdfPath)
        {
            using (var input = new OcrInput())
            {
                input.LoadPdf(pdfPath);
                var ocr = new IronTesseract();
                var result = ocr.Read(input);

                foreach (var page in result.Pages)
                {
                    bool hasNativeText = !string.IsNullOrWhiteSpace(page.Text);
                    Console.WriteLine($"Page {page.PageNumber}: " +
                        $"{(hasNativeText ? "Has text" : "Image only")} " +
                        $"(Confidence: {page.Confidence:P1})");
                }
            }
        }
    }
}


// ============================================================================
// COMPARISON: PDF Processing Complexity
// ============================================================================

namespace PdfProcessingComparison
{
    /// <summary>
    /// Direct comparison of PDF processing approaches.
    /// </summary>
    public class ComplexityComparison
    {
        /// <summary>
        /// Compare dependencies and setup.
        /// </summary>
        public static void CompareDependencies()
        {
            Console.WriteLine("=== PDF PROCESSING DEPENDENCIES ===");
            Console.WriteLine();

            Console.WriteLine("TESSERACT APPROACH:");
            Console.WriteLine("  Required packages:");
            Console.WriteLine("    1. Tesseract (NuGet)");
            Console.WriteLine("    2. tessdata files (external download)");
            Console.WriteLine("    3. PDF library (choose one):");
            Console.WriteLine("       - PdfiumViewer + pdfium native binaries");
            Console.WriteLine("       - PDFtoImage (SkiaSharp-based)");
            Console.WriteLine("       - Docnet.Core");
            Console.WriteLine("       - GhostScript (system install)");
            Console.WriteLine();
            Console.WriteLine("  Deployment files:");
            Console.WriteLine("    - tesseract native libraries");
            Console.WriteLine("    - leptonica native libraries");
            Console.WriteLine("    - tessdata folder (~15MB per language)");
            Console.WriteLine("    - pdfium.dll or GhostScript");
            Console.WriteLine();
            Console.WriteLine("  License considerations:");
            Console.WriteLine("    - Tesseract: Apache 2.0");
            Console.WriteLine("    - PdfiumViewer: BSD");
            Console.WriteLine("    - iText: AGPL or commercial");
            Console.WriteLine("    - GhostScript: AGPL or commercial");
            Console.WriteLine();

            Console.WriteLine("IRONOCR APPROACH:");
            Console.WriteLine("  Required packages:");
            Console.WriteLine("    1. IronOcr (NuGet)");
            Console.WriteLine();
            Console.WriteLine("  Deployment files:");
            Console.WriteLine("    - IronOcr.dll");
            Console.WriteLine("    - (all dependencies bundled)");
            Console.WriteLine();
            Console.WriteLine("  License:");
            Console.WriteLine("    - Commercial (Lite, Plus, Professional, Unlimited)");
            Console.WriteLine("    - Free trial available");
        }

        /// <summary>
        /// Compare code complexity.
        /// </summary>
        public static void CompareCodeComplexity()
        {
            Console.WriteLine("=== PDF OCR CODE COMPLEXITY ===");
            Console.WriteLine();

            Console.WriteLine("TESSERACT: Basic PDF OCR");
            Console.WriteLine("  ~50-100 lines of code including:");
            Console.WriteLine("  - PDF library initialization");
            Console.WriteLine("  - Page iteration");
            Console.WriteLine("  - Image conversion/rendering");
            Console.WriteLine("  - Temp file management");
            Console.WriteLine("  - Tesseract engine setup");
            Console.WriteLine("  - Error handling");
            Console.WriteLine("  - Resource cleanup");
            Console.WriteLine();

            Console.WriteLine("IRONOCR: Basic PDF OCR");
            Console.WriteLine("  1 line of code:");
            Console.WriteLine("  var text = new IronTesseract().Read(\"document.pdf\").Text;");
        }

        /// <summary>
        /// Compare feature support.
        /// </summary>
        public static void CompareFeatures()
        {
            Console.WriteLine("=== PDF FEATURE COMPARISON ===");
            Console.WriteLine();
            Console.WriteLine("Feature                        | Tesseract | IronOCR");
            Console.WriteLine("────────────────────────────────────────────────────────");
            Console.WriteLine("Basic PDF OCR                  | Via lib   | Native");
            Console.WriteLine("Password-protected PDFs        | Via lib   | Native");
            Console.WriteLine("Page selection                 | Manual    | Native");
            Console.WriteLine("Mixed PDFs (text + image)      | Complex   | Automatic");
            Console.WriteLine("Create searchable PDF          | Manual    | Native");
            Console.WriteLine("PDF/A compliance               | No        | Yes");
            Console.WriteLine("Table extraction               | No        | Built-in");
            Console.WriteLine("Barcode in PDF                 | No        | Built-in");
            Console.WriteLine("Large PDF (100+ pages)         | Memory    | Optimized");
            Console.WriteLine("Multi-threading                | Complex   | Built-in");
        }
    }

    /// <summary>
    /// Enterprise PDF processing scenarios.
    /// </summary>
    public class EnterpriseScenarios
    {
        /// <summary>
        /// Scenario: Invoice processing pipeline.
        /// </summary>
        public static void InvoiceProcessingPipeline()
        {
            Console.WriteLine("=== ENTERPRISE SCENARIO: Invoice Processing ===");
            Console.WriteLine();
            Console.WriteLine("Requirements:");
            Console.WriteLine("  - Process 10,000+ invoices/day");
            Console.WriteLine("  - Mixed PDF types (scanned, digital, mixed)");
            Console.WriteLine("  - Some password-protected");
            Console.WriteLine("  - Extract structured data (amounts, dates, vendors)");
            Console.WriteLine("  - Multi-language support");
            Console.WriteLine();

            Console.WriteLine("TESSERACT IMPLEMENTATION:");
            Console.WriteLine("  Development time: 3-6 weeks");
            Console.WriteLine("  Challenges:");
            Console.WriteLine("    - Build robust PDF-to-image pipeline");
            Console.WriteLine("    - Handle various PDF encryption types");
            Console.WriteLine("    - Implement multi-threading safely");
            Console.WriteLine("    - Memory management for large batches");
            Console.WriteLine("    - Preprocessing optimization per document type");
            Console.WriteLine("    - Error recovery and retry logic");
            Console.WriteLine();

            Console.WriteLine("IRONOCR IMPLEMENTATION:");
            Console.WriteLine("  Development time: 1-2 days");
            Console.WriteLine("  All requirements met with built-in features");
        }
    }
}


// ============================================================================
// SECURITY: Air-Gapped PDF Processing
// ============================================================================

namespace SecurityScenarios
{
    /// <summary>
    /// PDF processing for secure/classified environments.
    /// </summary>
    public class AirGappedProcessing
    {
        /// <summary>
        /// Both solutions support fully offline PDF processing.
        /// </summary>
        public static void OfflineCapabilities()
        {
            Console.WriteLine("=== AIR-GAPPED PDF PROCESSING ===");
            Console.WriteLine();
            Console.WriteLine("Government/Military Requirements:");
            Console.WriteLine("  [x] No internet connectivity required");
            Console.WriteLine("  [x] All processing on-premise");
            Console.WriteLine("  [x] No data transmission to cloud");
            Console.WriteLine("  [x] Auditable code paths");
            Console.WriteLine();

            Console.WriteLine("TESSERACT for Air-Gapped:");
            Console.WriteLine("  Challenges:");
            Console.WriteLine("  - Multiple native libraries to audit");
            Console.WriteLine("  - tessdata files downloaded from GitHub");
            Console.WriteLine("  - PDF library adds more dependencies");
            Console.WriteLine("  - Complex supply chain to verify");
            Console.WriteLine();

            Console.WriteLine("IRONOCR for Air-Gapped:");
            Console.WriteLine("  Advantages:");
            Console.WriteLine("  - Single NuGet package");
            Console.WriteLine("  - Signed assemblies");
            Console.WriteLine("  - No external file downloads");
            Console.WriteLine("  - Simpler security audit");
            Console.WriteLine("  - Commercial support with SLA");
        }
    }
}


// ============================================================================
// OUTPUT FORMAT OPTIONS
// ============================================================================

namespace OutputFormats
{
    using IronOcr;

    /// <summary>
    /// IronOCR can output to various formats from PDF input.
    /// </summary>
    public class PdfOutputOptions
    {
        /// <summary>
        /// All output format options from PDF OCR.
        /// </summary>
        public static void DemonstrateOutputFormats(string pdfPath)
        {
            var ocr = new IronTesseract();

            using (var input = new OcrInput())
            {
                input.LoadPdf(pdfPath);
                var result = ocr.Read(input);

                // Plain text
                string text = result.Text;

                // Searchable PDF (text layer added)
                result.SaveAsSearchablePdf("output_searchable.pdf");

                // hOCR format (HTML with positioning)
                string hocr = result.SaveAsHocrFile("output.hocr");

                // Each page as separate image
                int pageNum = 1;
                foreach (var page in result.Pages)
                {
                    // Page can be saved as image
                    // page.ToBitmap().Save($"page_{pageNum++}.png");
                }

                Console.WriteLine("Output formats generated:");
                Console.WriteLine("  - Plain text: result.Text");
                Console.WriteLine("  - Searchable PDF: SaveAsSearchablePdf()");
                Console.WriteLine("  - hOCR (HTML): SaveAsHocrFile()");
                Console.WriteLine("  - Individual page images");
            }
        }
    }
}
