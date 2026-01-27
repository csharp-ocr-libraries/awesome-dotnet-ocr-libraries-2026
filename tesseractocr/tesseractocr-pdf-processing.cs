// TesseractOCR PDF Processing Examples
// Install: dotnet add package TesseractOCR
//          dotnet add package Docnet.Core (for PDF rendering)
//
// IMPORTANT: TesseractOCR does NOT support PDF natively.
// You must use an external library to render PDF pages to images first.
//
// This file demonstrates the complexity of PDF processing with TesseractOCR
// compared to solutions with native PDF support like IronOCR.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TesseractOCR;
using TesseractOCR.Enums;

// External PDF library required - this adds licensing complexity and dependency management
// Options:
// - Docnet.Core (MIT license, shown here)
// - PdfiumViewer (Apache 2.0)
// - iTextSharp (AGPL - requires commercial license for most uses)
// - PDFsharp (MIT)
using Docnet.Core;
using Docnet.Core.Models;

namespace TesseractOcrPdfExamples
{
    /// <summary>
    /// PDF OCR service using TesseractOCR + Docnet.
    /// This demonstrates the complexity of PDF processing without native support.
    ///
    /// Contrast with IronOCR:
    ///   var result = new IronTesseract().Read(new OcrInput("document.pdf"));
    ///
    /// That single line replaces ~100 lines of code below.
    /// </summary>
    public class PdfOcrService : IDisposable
    {
        private readonly string _tessDataPath;
        private readonly string _tempDirectory;
        private bool _disposed;

        public PdfOcrService(string tessDataPath = "./tessdata")
        {
            _tessDataPath = tessDataPath;
            _tempDirectory = Path.Combine(Path.GetTempPath(), "TesseractOcrPdf");
            Directory.CreateDirectory(_tempDirectory);
        }

        /// <summary>
        /// Extract text from all pages of a PDF.
        /// This requires:
        /// 1. Loading PDF with external library
        /// 2. Rendering each page to an image
        /// 3. Saving image to temp file (TesseractOCR needs file path)
        /// 4. OCR processing each image
        /// 5. Cleaning up temp files
        ///
        /// IronOCR equivalent: 3 lines of code
        /// </summary>
        public string ExtractTextFromPdf(string pdfPath, int dpi = 200)
        {
            if (!File.Exists(pdfPath))
            {
                throw new FileNotFoundException($"PDF not found: {pdfPath}");
            }

            var allText = new StringBuilder();
            var tempFiles = new List<string>();

            try
            {
                // Step 1: Load PDF with Docnet
                using var library = DocLib.Instance;
                using var docReader = library.GetDocReader(
                    pdfPath,
                    new PageDimensions(dpi, dpi));

                int pageCount = docReader.GetPageCount();
                Console.WriteLine($"Processing {pageCount} pages...");

                // Step 2: Process each page
                using var engine = new Engine(_tessDataPath, Language.English, EngineMode.Default);

                for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
                {
                    Console.WriteLine($"Processing page {pageIndex + 1}/{pageCount}");

                    // Step 2a: Render PDF page to image bytes
                    using var pageReader = docReader.GetPageReader(pageIndex);
                    var width = pageReader.GetPageWidth();
                    var height = pageReader.GetPageHeight();
                    var imageBytes = pageReader.GetImage();

                    if (imageBytes == null || imageBytes.Length == 0)
                    {
                        allText.AppendLine($"--- Page {pageIndex + 1}: [Empty or failed to render] ---");
                        continue;
                    }

                    // Step 2b: Save to temp file (TesseractOCR requires file path)
                    // This is inefficient but necessary with the TesseractOCR API
                    string tempPath = Path.Combine(_tempDirectory, $"page_{pageIndex}_{Guid.NewGuid()}.png");
                    tempFiles.Add(tempPath);

                    // Convert BGRA bytes to PNG
                    SaveBgraAsPng(imageBytes, width, height, tempPath);

                    // Step 2c: OCR the page image
                    try
                    {
                        using var image = TesseractOCR.Pix.Image.LoadFromFile(tempPath);
                        using var page = engine.Process(image);

                        allText.AppendLine($"--- Page {pageIndex + 1} (Confidence: {page.MeanConfidence:P0}) ---");
                        allText.AppendLine(page.Text);
                        allText.AppendLine();
                    }
                    catch (Exception ex)
                    {
                        allText.AppendLine($"--- Page {pageIndex + 1}: [OCR Error: {ex.Message}] ---");
                    }
                }
            }
            finally
            {
                // Step 5: Clean up temp files
                foreach (var tempFile in tempFiles)
                {
                    try
                    {
                        if (File.Exists(tempFile))
                        {
                            File.Delete(tempFile);
                        }
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }

            return allText.ToString();
        }

        /// <summary>
        /// Extract text from specific page range.
        /// Memory management becomes important for large PDFs.
        /// </summary>
        public string ExtractTextFromPageRange(string pdfPath, int startPage, int endPage, int dpi = 200)
        {
            var allText = new StringBuilder();
            var tempFiles = new List<string>();

            try
            {
                using var library = DocLib.Instance;
                using var docReader = library.GetDocReader(pdfPath, new PageDimensions(dpi, dpi));

                int pageCount = docReader.GetPageCount();

                // Validate page range
                startPage = Math.Max(0, startPage);
                endPage = Math.Min(pageCount - 1, endPage);

                using var engine = new Engine(_tessDataPath, Language.English, EngineMode.Default);

                for (int pageIndex = startPage; pageIndex <= endPage; pageIndex++)
                {
                    using var pageReader = docReader.GetPageReader(pageIndex);
                    var width = pageReader.GetPageWidth();
                    var height = pageReader.GetPageHeight();
                    var imageBytes = pageReader.GetImage();

                    if (imageBytes == null) continue;

                    string tempPath = Path.Combine(_tempDirectory, $"page_{pageIndex}_{Guid.NewGuid()}.png");
                    tempFiles.Add(tempPath);

                    SaveBgraAsPng(imageBytes, width, height, tempPath);

                    using var image = TesseractOCR.Pix.Image.LoadFromFile(tempPath);
                    using var page = engine.Process(image);

                    allText.AppendLine($"--- Page {pageIndex + 1} ---");
                    allText.AppendLine(page.Text);
                    allText.AppendLine();
                }
            }
            finally
            {
                CleanupTempFiles(tempFiles);
            }

            return allText.ToString();
        }

        /// <summary>
        /// Process large PDFs with memory-efficient streaming.
        /// For PDFs with 100+ pages, processing all at once may cause OutOfMemoryException.
        /// </summary>
        public IEnumerable<(int PageNumber, string Text, float Confidence)> ProcessLargePdfStreaming(
            string pdfPath,
            int dpi = 150) // Lower DPI for large PDFs to reduce memory
        {
            using var library = DocLib.Instance;
            using var docReader = library.GetDocReader(pdfPath, new PageDimensions(dpi, dpi));

            int pageCount = docReader.GetPageCount();

            // Create engine once for all pages
            using var engine = new Engine(_tessDataPath, Language.English, EngineMode.Default);

            for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                string tempPath = Path.Combine(_tempDirectory, $"stream_page_{Guid.NewGuid()}.png");

                try
                {
                    using var pageReader = docReader.GetPageReader(pageIndex);
                    var width = pageReader.GetPageWidth();
                    var height = pageReader.GetPageHeight();
                    var imageBytes = pageReader.GetImage();

                    if (imageBytes == null)
                    {
                        yield return (pageIndex + 1, "[Empty page]", 0);
                        continue;
                    }

                    SaveBgraAsPng(imageBytes, width, height, tempPath);

                    using var image = TesseractOCR.Pix.Image.LoadFromFile(tempPath);
                    using var page = engine.Process(image);

                    yield return (pageIndex + 1, page.Text, page.MeanConfidence);
                }
                finally
                {
                    // Clean up temp file immediately after processing each page
                    try { File.Delete(tempPath); } catch { }
                }

                // Force garbage collection periodically for large PDFs
                if (pageIndex > 0 && pageIndex % 10 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
        }

        /// <summary>
        /// Helper method to save BGRA byte array as PNG.
        /// This is necessary because Docnet returns raw pixel data,
        /// but TesseractOCR expects a file path to a standard image format.
        ///
        /// In production, you would use a proper imaging library.
        /// This simplified version creates a basic BMP file that most systems can read.
        /// </summary>
        private void SaveBgraAsPng(byte[] bgraData, int width, int height, string outputPath)
        {
            // For production use, use ImageSharp or SkiaSharp for proper image saving
            // This is a simplified BMP writer for demonstration

            // Simple approach: Write as 32-bit BMP (widely compatible)
            using var fs = new FileStream(outputPath.Replace(".png", ".bmp"), FileMode.Create);
            using var writer = new BinaryWriter(fs);

            // BMP Header
            int rowSize = (width * 4 + 3) & ~3; // 4-byte aligned rows
            int imageSize = rowSize * height;
            int fileSize = 54 + imageSize;

            // File header (14 bytes)
            writer.Write((byte)'B');
            writer.Write((byte)'M');
            writer.Write(fileSize);
            writer.Write((short)0); // Reserved
            writer.Write((short)0); // Reserved
            writer.Write(54); // Pixel data offset

            // Info header (40 bytes)
            writer.Write(40); // Header size
            writer.Write(width);
            writer.Write(-height); // Negative for top-down
            writer.Write((short)1); // Planes
            writer.Write((short)32); // Bits per pixel
            writer.Write(0); // Compression (none)
            writer.Write(imageSize);
            writer.Write(2835); // X pixels per meter
            writer.Write(2835); // Y pixels per meter
            writer.Write(0); // Colors in table
            writer.Write(0); // Important colors

            // Pixel data (BGRA order matches BMP format)
            writer.Write(bgraData);

            // Update output path to use .bmp extension
            if (outputPath.EndsWith(".png"))
            {
                // Rename if caller expected PNG
                File.Move(outputPath.Replace(".png", ".bmp"), outputPath, true);
            }
        }

        private void CleanupTempFiles(List<string> tempFiles)
        {
            foreach (var tempFile in tempFiles)
            {
                try
                {
                    if (File.Exists(tempFile)) File.Delete(tempFile);
                    // Also try BMP variant
                    var bmpPath = tempFile.Replace(".png", ".bmp");
                    if (File.Exists(bmpPath)) File.Delete(bmpPath);
                }
                catch { }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                // Clean up temp directory on dispose
                try
                {
                    if (Directory.Exists(_tempDirectory))
                    {
                        var files = Directory.GetFiles(_tempDirectory);
                        foreach (var file in files)
                        {
                            try { File.Delete(file); } catch { }
                        }
                    }
                }
                catch { }

                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Comparison: The same PDF processing in IronOCR.
    /// This demonstrates the dramatic difference in complexity.
    /// </summary>
    public static class IronOcrPdfComparison
    {
        /*
        // IronOCR PDF processing - the entire implementation:

        using IronOcr;

        public static string ExtractTextFromPdf(string pdfPath)
        {
            var ocr = new IronTesseract();
            using var input = new OcrInput();
            input.LoadPdf(pdfPath);
            return ocr.Read(input).Text;
        }

        // That's it. 5 lines vs ~200 lines above.
        // Plus:
        // - No external PDF library needed
        // - No temp file management
        // - No memory management concerns
        // - No BGRA to image conversion
        // - Native password-protected PDF support:
        //   input.LoadPdf("encrypted.pdf", Password: "secret");
        //
        // - Create searchable PDFs:
        //   result.SaveAsSearchablePdf("output.pdf");
        */
    }

    /// <summary>
    /// Password-protected PDF handling with TesseractOCR.
    /// This is NOT supported - you need yet another library.
    /// </summary>
    public static class PasswordProtectedPdfExample
    {
        /*
        // TesseractOCR + Docnet cannot handle password-protected PDFs directly.
        // You need to:
        // 1. Use a PDF library that supports decryption (e.g., iTextSharp, PDFsharp)
        // 2. Decrypt/remove password first
        // 3. Save decrypted PDF
        // 4. Then process with the code above
        //
        // Example with iTextSharp (AGPL license - requires commercial license):

        using iTextSharp.text.pdf;

        public static string ProcessEncryptedPdf(string pdfPath, string password, string tessDataPath)
        {
            // Step 1: Decrypt PDF
            string decryptedPath = Path.GetTempFileName() + ".pdf";

            try
            {
                PdfReader.unethicalreading = true; // Required for some encrypted PDFs
                using var reader = new PdfReader(pdfPath, Encoding.UTF8.GetBytes(password));
                using var fs = new FileStream(decryptedPath, FileMode.Create);
                using var stamper = new PdfStamper(reader, fs);
                // Password removed, PDF saved

                // Step 2: Now process with TesseractOCR
                using var service = new PdfOcrService(tessDataPath);
                return service.ExtractTextFromPdf(decryptedPath);
            }
            finally
            {
                if (File.Exists(decryptedPath))
                {
                    File.Delete(decryptedPath);
                }
            }
        }

        // IronOCR equivalent:
        // input.LoadPdf("encrypted.pdf", Password: "secret");
        // One line, built-in, no additional library or license.
        */
    }

    // Example usage
    class Program
    {
        static void Main(string[] args)
        {
            string pdfPath = "document.pdf";
            string tessDataPath = "./tessdata";

            Console.WriteLine("PDF OCR with TesseractOCR (requires external library)");
            Console.WriteLine("=====================================================");

            using var pdfService = new PdfOcrService(tessDataPath);

            // Process entire PDF
            string text = pdfService.ExtractTextFromPdf(pdfPath);
            Console.WriteLine(text);

            // Process specific pages
            string pages2to5 = pdfService.ExtractTextFromPageRange(pdfPath, 1, 4); // 0-indexed
            Console.WriteLine(pages2to5);

            // Stream large PDFs
            foreach (var (pageNum, pageText, confidence) in pdfService.ProcessLargePdfStreaming(pdfPath))
            {
                Console.WriteLine($"Page {pageNum} ({confidence:P0}): {pageText.Substring(0, Math.Min(100, pageText.Length))}...");
            }
        }
    }
}
