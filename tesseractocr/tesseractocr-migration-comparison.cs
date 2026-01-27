// TesseractOCR to IronOCR Migration Comparison
// This file provides side-by-side comparisons for common OCR patterns,
// demonstrating the migration path from TesseractOCR to IronOCR.
//
// Install TesseractOCR: dotnet add package TesseractOCR
//                       + download tessdata manually
// Install IronOCR:      dotnet add package IronOcr
//                       (that's it)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

// For demonstration, we show both namespaces
// In actual migration, you would replace one with the other
// using TesseractOCR;
// using TesseractOCR.Enums;
// using IronOcr;

namespace MigrationComparison
{
    // =========================================================================
    // EXAMPLE 1: Basic Text Extraction
    // =========================================================================

    /// <summary>
    /// TesseractOCR: Basic text extraction (BEFORE migration)
    /// </summary>
    public class TesseractOcrBasicExample
    {
        /*
        public string ExtractText(string imagePath)
        {
            // Requires tessdata folder with eng.traineddata
            // Downloaded separately from GitHub
            using var engine = new Engine(@"./tessdata", Language.English, EngineMode.Default);
            using var image = TesseractOCR.Pix.Image.LoadFromFile(imagePath);
            using var page = engine.Process(image);

            return page.Text;
        }
        */
    }

    /// <summary>
    /// IronOCR: Basic text extraction (AFTER migration)
    /// </summary>
    public class IronOcrBasicExample
    {
        /*
        public string ExtractText(string imagePath)
        {
            // No tessdata setup needed
            // Languages auto-download on first use
            var ocr = new IronTesseract();
            using var input = new OcrInput(imagePath);
            return ocr.Read(input).Text;
        }
        */
    }

    // Migration notes for Example 1:
    // - Remove tessdata folder and download scripts
    // - Replace Engine with IronTesseract
    // - Replace Pix.Image.LoadFromFile with OcrInput constructor
    // - Replace page.Text with result.Text

    // =========================================================================
    // EXAMPLE 2: PDF Processing
    // =========================================================================

    /// <summary>
    /// TesseractOCR: PDF processing (BEFORE migration)
    /// Requires external library (Docnet, PdfiumViewer, etc.)
    /// </summary>
    public class TesseractOcrPdfExample
    {
        /*
        // Requires: dotnet add package Docnet.Core
        using Docnet.Core;
        using Docnet.Core.Models;

        public string ExtractTextFromPdf(string pdfPath)
        {
            var allText = new StringBuilder();
            var tempFiles = new List<string>();

            try
            {
                using var library = DocLib.Instance;
                using var docReader = library.GetDocReader(pdfPath, new PageDimensions(200, 200));

                using var engine = new Engine(@"./tessdata", Language.English);

                for (int pageIndex = 0; pageIndex < docReader.GetPageCount(); pageIndex++)
                {
                    using var pageReader = docReader.GetPageReader(pageIndex);
                    var imageBytes = pageReader.GetImage();

                    // Save to temp file (TesseractOCR needs file path)
                    string tempPath = Path.GetTempFileName() + ".bmp";
                    tempFiles.Add(tempPath);
                    SaveImageBytes(imageBytes, pageReader.GetPageWidth(), pageReader.GetPageHeight(), tempPath);

                    using var image = TesseractOCR.Pix.Image.LoadFromFile(tempPath);
                    using var page = engine.Process(image);

                    allText.AppendLine($"--- Page {pageIndex + 1} ---");
                    allText.AppendLine(page.Text);
                }
            }
            finally
            {
                foreach (var temp in tempFiles)
                {
                    try { File.Delete(temp); } catch { }
                }
            }

            return allText.ToString();
        }

        private void SaveImageBytes(byte[] bytes, int width, int height, string path)
        {
            // Complex image saving logic here...
        }
        */
    }

    /// <summary>
    /// IronOCR: PDF processing (AFTER migration)
    /// Native PDF support - no external library needed
    /// </summary>
    public class IronOcrPdfExample
    {
        /*
        public string ExtractTextFromPdf(string pdfPath)
        {
            var ocr = new IronTesseract();
            using var input = new OcrInput();
            input.LoadPdf(pdfPath);
            return ocr.Read(input).Text;
        }

        // Password-protected PDFs - built-in support
        public string ExtractFromEncryptedPdf(string pdfPath, string password)
        {
            var ocr = new IronTesseract();
            using var input = new OcrInput();
            input.LoadPdf(pdfPath, Password: password);
            return ocr.Read(input).Text;
        }

        // Specific pages
        public string ExtractPages(string pdfPath, int startPage, int endPage)
        {
            var ocr = new IronTesseract();
            using var input = new OcrInput();
            input.LoadPdfPages(pdfPath, startPage, endPage);
            return ocr.Read(input).Text;
        }
        */
    }

    // Migration notes for Example 2:
    // - Remove Docnet.Core or PdfiumViewer package
    // - Remove all temp file logic
    // - Remove image conversion code
    // - Replace entire PDF rendering loop with single LoadPdf() call
    // - Password support is now built-in

    // =========================================================================
    // EXAMPLE 3: Image Preprocessing
    // =========================================================================

    /// <summary>
    /// TesseractOCR: Preprocessing (BEFORE migration)
    /// Requires external imaging library (ImageSharp, OpenCV, etc.)
    /// </summary>
    public class TesseractOcrPreprocessingExample
    {
        /*
        // Requires: dotnet add package SixLabors.ImageSharp
        using SixLabors.ImageSharp;
        using SixLabors.ImageSharp.Processing;

        public string ExtractWithPreprocessing(string imagePath)
        {
            // Load with ImageSharp for preprocessing
            using var image = Image.Load(imagePath);

            // Manual preprocessing - each step requires tuning
            image.Mutate(x => x.Grayscale());
            image.Mutate(x => x.Contrast(1.5f));         // Manual tuning
            image.Mutate(x => x.GaussianBlur(0.5f));     // Denoise
            image.Mutate(x => x.BinaryThreshold(0.5f)); // Manual threshold

            // Deskew requires complex angle detection algorithm
            // Not built into ImageSharp - need separate implementation
            // (~50-100 lines of Hough transform code)

            // Save preprocessed to temp file
            string tempPath = Path.GetTempFileName() + ".png";
            try
            {
                image.Save(tempPath);

                // Now process with TesseractOCR
                using var engine = new Engine(@"./tessdata", Language.English);
                using var pixImage = TesseractOCR.Pix.Image.LoadFromFile(tempPath);
                using var page = engine.Process(pixImage);

                return page.Text;
            }
            finally
            {
                File.Delete(tempPath);
            }
        }
        */
    }

    /// <summary>
    /// IronOCR: Preprocessing (AFTER migration)
    /// All preprocessing built-in, no external library needed
    /// </summary>
    public class IronOcrPreprocessingExample
    {
        /*
        public string ExtractWithPreprocessing(string imagePath)
        {
            var ocr = new IronTesseract();

            using var input = new OcrInput(imagePath);

            // All preprocessing built-in with intelligent defaults
            input.Deskew();           // Automatic angle detection
            input.DeNoise();          // Intelligent noise removal
            input.Contrast();         // Automatic contrast enhancement
            input.EnhanceResolution(300); // Upscale if needed

            return ocr.Read(input).Text;
        }

        // Or just let IronOCR auto-preprocess
        public string ExtractWithAutoPreprocessing(string imagePath)
        {
            var ocr = new IronTesseract();
            // Most preprocessing happens automatically
            return ocr.Read(imagePath).Text;
        }
        */
    }

    // Migration notes for Example 3:
    // - Remove ImageSharp or OpenCV dependency
    // - Remove temp file creation and cleanup
    // - Replace manual filter chain with built-in methods
    // - input.Deskew() replaces complex Hough transform code
    // - input.DeNoise() replaces blur/threshold tuning
    // - Auto-preprocessing often eliminates need for explicit calls

    // =========================================================================
    // EXAMPLE 4: Multi-Language OCR
    // =========================================================================

    /// <summary>
    /// TesseractOCR: Multi-language (BEFORE migration)
    /// </summary>
    public class TesseractOcrMultiLanguageExample
    {
        /*
        public string ExtractMultiLanguage(string imagePath)
        {
            // Requires manual download of each language:
            // curl -L -o tessdata/eng.traineddata https://github.com/tesseract-ocr/tessdata_best/raw/main/eng.traineddata
            // curl -L -o tessdata/fra.traineddata https://github.com/tesseract-ocr/tessdata_best/raw/main/fra.traineddata
            // curl -L -o tessdata/deu.traineddata https://github.com/tesseract-ocr/tessdata_best/raw/main/deu.traineddata

            // Check files exist before processing
            if (!File.Exists(@"./tessdata/fra.traineddata"))
            {
                throw new FileNotFoundException("fra.traineddata not found. Download from tessdata repository.");
            }

            using var engine = new Engine(@"./tessdata",
                Language.English | Language.French | Language.German,
                EngineMode.Default);

            using var image = TesseractOCR.Pix.Image.LoadFromFile(imagePath);
            using var page = engine.Process(image);

            return page.Text;
        }
        */
    }

    /// <summary>
    /// IronOCR: Multi-language (AFTER migration)
    /// Languages auto-download on first use
    /// </summary>
    public class IronOcrMultiLanguageExample
    {
        /*
        public string ExtractMultiLanguage(string imagePath)
        {
            var ocr = new IronTesseract();

            // Languages automatically download if not present
            ocr.Language = OcrLanguage.English + OcrLanguage.French + OcrLanguage.German;

            using var input = new OcrInput(imagePath);
            return ocr.Read(input).Text;
        }

        // Or install language packs via NuGet for offline/air-gapped environments:
        // dotnet add package IronOcr.Languages.French
        // dotnet add package IronOcr.Languages.German
        */
    }

    // Migration notes for Example 4:
    // - Delete tessdata folder with .traineddata files
    // - Remove manual download scripts
    // - Replace Language.X | Language.Y with OcrLanguage.X + OcrLanguage.Y
    // - Languages auto-download or install as NuGet packages

    // =========================================================================
    // EXAMPLE 5: Error Handling
    // =========================================================================

    /// <summary>
    /// TesseractOCR: Error handling (BEFORE migration)
    /// </summary>
    public class TesseractOcrErrorHandlingExample
    {
        /*
        public string ExtractWithErrorHandling(string imagePath)
        {
            try
            {
                using var engine = new Engine(@"./tessdata", Language.English, EngineMode.Default);
                using var image = TesseractOCR.Pix.Image.LoadFromFile(imagePath);
                using var page = engine.Process(image);
                return page.Text;
            }
            catch (TesseractOCR.Exceptions.TesseractException ex)
            {
                // Engine initialization failure
                // Common causes: tessdata path wrong, traineddata corrupted
                throw new InvalidOperationException($"Tesseract engine error: {ex.Message}", ex);
            }
            catch (DllNotFoundException ex)
            {
                // Native library not found
                // Common causes: Missing VC++ Redistributable, Linux missing packages
                throw new InvalidOperationException(
                    "Tesseract native libraries not found. " +
                    "Windows: Install VC++ Redistributable. " +
                    "Linux: apt-get install libtesseract-dev", ex);
            }
            catch (BadImageFormatException ex)
            {
                // Architecture mismatch (x86 vs x64)
                throw new InvalidOperationException(
                    "Architecture mismatch. Ensure project and Tesseract libraries match (x86/x64)", ex);
            }
            catch (OutOfMemoryException ex)
            {
                // Large image or memory leak from missing Dispose
                throw new InvalidOperationException(
                    "Out of memory. Image may be too large or memory leak from missing disposal", ex);
            }
        }
        */
    }

    /// <summary>
    /// IronOCR: Error handling (AFTER migration)
    /// Simpler error surface - most Tesseract issues don't apply
    /// </summary>
    public class IronOcrErrorHandlingExample
    {
        /*
        public string ExtractWithErrorHandling(string imagePath)
        {
            try
            {
                var ocr = new IronTesseract();
                using var input = new OcrInput(imagePath);
                var result = ocr.Read(input);

                // Check confidence for quality issues
                if (result.Confidence < 50)
                {
                    Console.WriteLine($"Warning: Low confidence ({result.Confidence}%). Consider preprocessing.");
                }

                return result.Text;
            }
            catch (IronOcr.Exceptions.OcrException ex)
            {
                // IronOCR-specific error (unified exception type)
                throw new InvalidOperationException($"OCR error: {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                // Standard file access errors
                throw new InvalidOperationException($"File error: {ex.Message}", ex);
            }

            // Note: No more TesseractException, DllNotFoundException, BadImageFormatException
            // IronOCR handles native dependencies internally
        }
        */
    }

    // Migration notes for Example 5:
    // - Remove TesseractException catch blocks
    // - Remove DllNotFoundException handling (IronOCR bundles natives)
    // - Remove BadImageFormatException handling (auto-detected)
    // - Replace with IronOcr.Exceptions.OcrException
    // - Use Confidence property to detect quality issues

    // =========================================================================
    // EXAMPLE 6: Thread-Safe Batch Processing
    // =========================================================================

    /// <summary>
    /// TesseractOCR: Thread-safe processing (BEFORE migration)
    /// Engine is NOT thread-safe - must create per thread
    /// </summary>
    public class TesseractOcrThreadSafeExample
    {
        /*
        public Dictionary<string, string> ProcessBatch(string[] imagePaths)
        {
            var results = new ConcurrentDictionary<string, string>();

            // WARNING: TesseractEngine is NOT thread-safe
            // Each thread must create its own engine instance
            // Memory: 4 threads x ~50MB = 200MB+ for engines alone

            Parallel.ForEach(
                imagePaths,
                new ParallelOptions { MaxDegreeOfParallelism = 4 },
                imagePath =>
                {
                    // Per-thread engine instance (expensive: ~500ms init, ~50MB memory)
                    using var engine = new Engine(@"./tessdata", Language.English, EngineMode.Default);
                    using var image = TesseractOCR.Pix.Image.LoadFromFile(imagePath);
                    using var page = engine.Process(image);

                    results[imagePath] = page.Text;
                });

            return new Dictionary<string, string>(results);
        }
        */
    }

    /// <summary>
    /// IronOCR: Thread-safe processing (AFTER migration)
    /// IronTesseract IS thread-safe - single instance for all threads
    /// </summary>
    public class IronOcrThreadSafeExample
    {
        /*
        public Dictionary<string, string> ProcessBatch(string[] imagePaths)
        {
            var results = new ConcurrentDictionary<string, string>();

            // IronTesseract is thread-safe - create once, use everywhere
            // Memory: Single instance shared across all threads
            var ocr = new IronTesseract();

            Parallel.ForEach(imagePaths, imagePath =>
            {
                using var input = new OcrInput(imagePath);
                results[imagePath] = ocr.Read(input).Text;
            });

            return new Dictionary<string, string>(results);
        }

        // Or use built-in batch processing
        public string ProcessBatchSimple(string[] imagePaths)
        {
            var ocr = new IronTesseract();

            using var input = new OcrInput();
            foreach (var path in imagePaths)
            {
                input.LoadImage(path);
            }

            // Internal parallelization handles optimization
            var result = ocr.Read(input);
            return result.Text;
        }
        */
    }

    // Migration notes for Example 6:
    // - Remove per-thread engine creation
    // - Create single IronTesseract instance outside parallel loop
    // - Memory usage drops significantly (4x engines -> 1 instance)
    // - Consider using OcrInput batch loading for simpler code

    // =========================================================================
    // EXAMPLE 7: Memory Management
    // =========================================================================

    /// <summary>
    /// TesseractOCR: Memory management (BEFORE migration)
    /// Requires careful attention to disposal
    /// </summary>
    public class TesseractOcrMemoryExample : IDisposable
    {
        /*
        private bool _disposed;

        public void ProcessManyDocuments(string[] paths)
        {
            // Pattern: Reuse engine (saves init time) but requires Dispose
            using var engine = new Engine(@"./tessdata", Language.English, EngineMode.Default);

            foreach (var path in paths)
            {
                // CRITICAL: Every Pix and Page must be disposed
                // Missing 'using' here causes native memory leak
                using var image = TesseractOCR.Pix.Image.LoadFromFile(path);
                using var page = engine.Process(image);

                Console.WriteLine(page.Text);
            }

            // For long-running processes, periodic GC may help
            if (paths.Length > 100)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
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
                // Engine, Pix, Page all have native resources
                // Failure to dispose properly leaks memory
                _disposed = true;
            }
        }
        */
    }

    /// <summary>
    /// IronOCR: Memory management (AFTER migration)
    /// Simplified - GC-friendly implementation
    /// </summary>
    public class IronOcrMemoryExample
    {
        /*
        public void ProcessManyDocuments(string[] paths)
        {
            var ocr = new IronTesseract();

            foreach (var path in paths)
            {
                // OcrInput uses standard IDisposable pattern
                using var input = new OcrInput(path);
                var result = ocr.Read(input);
                Console.WriteLine(result.Text);
            }

            // No manual GC needed - IronOCR manages memory internally
        }

        // Even simpler - no using statements required if not reusing input
        public void ProcessManyDocumentsSimple(string[] paths)
        {
            var ocr = new IronTesseract();

            foreach (var path in paths)
            {
                // Read() can take path directly
                var result = ocr.Read(path);
                Console.WriteLine(result.Text);
            }
        }
        */
    }

    // Migration notes for Example 7:
    // - Remove complex Dispose patterns
    // - Remove GC.Collect() hints
    // - 'using' for OcrInput is recommended but not strictly required
    // - IronTesseract doesn't need disposal

    // =========================================================================
    // COMPLETE MIGRATION CHECKLIST
    // =========================================================================

    /// <summary>
    /// Summary of migration changes
    /// </summary>
    public static class MigrationChecklist
    {
        /*
        PRE-MIGRATION:
        [ ] Inventory all TesseractOCR usage points
        [ ] Document current accuracy metrics
        [ ] Create test documents for validation
        [ ] Obtain IronOCR license key

        PACKAGE CHANGES:
        [ ] Remove: TesseractOCR package
        [ ] Remove: Docnet.Core or PdfiumViewer (if used for PDF)
        [ ] Remove: ImageSharp or OpenCV (if used for preprocessing)
        [ ] Remove: tessdata folder
        [ ] Add: IronOcr package
        [ ] Add: Optional language packages (IronOcr.Languages.*)

        CODE CHANGES:
        [ ] Update namespaces: TesseractOCR -> IronOcr
        [ ] Replace Engine with IronTesseract
        [ ] Replace Pix.Image.LoadFromFile with OcrInput
        [ ] Replace page.Text with result.Text
        [ ] Replace page.MeanConfidence with result.Confidence (note: scale may differ)
        [ ] Replace Language enum flags (|) with OcrLanguage addition (+)
        [ ] Remove manual preprocessing code
        [ ] Remove PDF rendering loops
        [ ] Simplify threading (single IronTesseract instance)
        [ ] Update error handling (TesseractException -> OcrException)

        POST-MIGRATION:
        [ ] Run test suite
        [ ] Compare accuracy metrics
        [ ] Verify no remaining TesseractOCR references
        [ ] Update deployment scripts (remove tessdata deployment)
        [ ] Update Docker files (remove apt-get for tesseract)
        [ ] Update documentation
        */
    }

    // Entry point for examples
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("TesseractOCR to IronOCR Migration Examples");
            Console.WriteLine("==========================================");
            Console.WriteLine();
            Console.WriteLine("This file contains commented code showing side-by-side comparisons.");
            Console.WriteLine("Uncomment the TesseractOCR or IronOCR sections to test each approach.");
            Console.WriteLine();
            Console.WriteLine("Key migration benefits:");
            Console.WriteLine("- Remove tessdata folder and download scripts");
            Console.WriteLine("- Remove external PDF library (Docnet, PdfiumViewer)");
            Console.WriteLine("- Remove external preprocessing library (ImageSharp)");
            Console.WriteLine("- Simplify threading (shared IronTesseract instance)");
            Console.WriteLine("- Native PDF and password-protected PDF support");
            Console.WriteLine("- Built-in preprocessing (deskew, denoise, contrast)");
            Console.WriteLine("- Auto-downloading languages");
        }
    }
}
