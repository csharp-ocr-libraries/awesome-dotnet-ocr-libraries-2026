// TesseractOCR Basic OCR Examples
// Install: dotnet add package TesseractOCR
//
// CRITICAL: You must also download tessdata files separately:
// curl -L -o tessdata/eng.traineddata https://github.com/tesseract-ocr/tessdata_best/raw/main/eng.traineddata
//
// This file demonstrates basic TesseractOCR usage patterns with proper
// disposal, memory management, and common error handling.

using System;
using System.IO;
using TesseractOCR;
using TesseractOCR.Enums;

namespace TesseractOcrExamples
{
    /// <summary>
    /// Basic OCR service using TesseractOCR.
    /// Demonstrates proper initialization, processing, and disposal patterns.
    /// </summary>
    public class BasicOcrService
    {
        private readonly string _tessDataPath;

        public BasicOcrService(string tessDataPath = "./tessdata")
        {
            _tessDataPath = tessDataPath;
            ValidateTessData();
        }

        /// <summary>
        /// Validates that tessdata folder and required files exist.
        /// This is a common source of errors - always validate before processing.
        /// </summary>
        private void ValidateTessData()
        {
            if (!Directory.Exists(_tessDataPath))
            {
                throw new DirectoryNotFoundException(
                    $"tessdata folder not found at: {_tessDataPath}\n" +
                    "Download traineddata files from: https://github.com/tesseract-ocr/tessdata_best");
            }

            string engTrainedData = Path.Combine(_tessDataPath, "eng.traineddata");
            if (!File.Exists(engTrainedData))
            {
                throw new FileNotFoundException(
                    $"eng.traineddata not found in {_tessDataPath}\n" +
                    "Download from: https://github.com/tesseract-ocr/tessdata_best/raw/main/eng.traineddata");
            }
        }

        /// <summary>
        /// Basic text extraction from a single image.
        /// Uses proper disposal pattern to prevent memory leaks.
        /// </summary>
        public string ExtractText(string imagePath)
        {
            // IMPORTANT: Always use 'using' statements with TesseractOCR
            // The native Tesseract library has memory that must be explicitly released

            using var engine = new Engine(_tessDataPath, Language.English, EngineMode.Default);
            using var image = TesseractOCR.Pix.Image.LoadFromFile(imagePath);
            using var page = engine.Process(image);

            // Extract text before page is disposed
            return page.Text;
        }

        /// <summary>
        /// Extract text with confidence score.
        /// Confidence helps determine if results are trustworthy.
        /// </summary>
        public (string Text, float Confidence) ExtractTextWithConfidence(string imagePath)
        {
            using var engine = new Engine(_tessDataPath, Language.English, EngineMode.Default);
            using var image = TesseractOCR.Pix.Image.LoadFromFile(imagePath);
            using var page = engine.Process(image);

            // MeanConfidence returns 0.0-1.0 (multiply by 100 for percentage)
            // Below 0.6 (60%) indicates poor quality or preprocessing needed
            return (page.Text, page.MeanConfidence);
        }

        /// <summary>
        /// Multiple language support example.
        /// Requires corresponding .traineddata files in tessdata folder.
        /// </summary>
        public string ExtractMultiLanguage(string imagePath, params Language[] languages)
        {
            // Combine languages using bitwise OR
            Language combinedLanguages = languages[0];
            for (int i = 1; i < languages.Length; i++)
            {
                combinedLanguages |= languages[i];
            }

            using var engine = new Engine(_tessDataPath, combinedLanguages, EngineMode.Default);
            using var image = TesseractOCR.Pix.Image.LoadFromFile(imagePath);
            using var page = engine.Process(image);

            return page.Text;
        }
    }

    /// <summary>
    /// Demonstrates engine reuse for batch processing.
    /// Creating a new engine per image is wasteful - reuse when possible.
    /// WARNING: Engine is NOT thread-safe. Use one engine per thread.
    /// </summary>
    public class BatchOcrService : IDisposable
    {
        private readonly Engine _engine;
        private bool _disposed;

        public BatchOcrService(string tessDataPath = "./tessdata")
        {
            // Engine initialization is expensive (~500ms)
            // Create once and reuse for multiple images
            _engine = new Engine(tessDataPath, Language.English, EngineMode.Default);
        }

        /// <summary>
        /// Process multiple images using a single engine instance.
        /// More efficient than creating engine per image.
        /// </summary>
        public Dictionary<string, string> ProcessBatch(string[] imagePaths)
        {
            var results = new Dictionary<string, string>();

            foreach (var path in imagePaths)
            {
                try
                {
                    using var image = TesseractOCR.Pix.Image.LoadFromFile(path);
                    using var page = _engine.Process(image);

                    results[path] = page.Text;
                }
                catch (Exception ex)
                {
                    results[path] = $"Error: {ex.Message}";
                }
            }

            return results;
        }

        /// <summary>
        /// Process with quality filtering.
        /// Skips results below confidence threshold.
        /// </summary>
        public Dictionary<string, string> ProcessBatchWithQualityFilter(
            string[] imagePaths,
            float minimumConfidence = 0.6f)
        {
            var results = new Dictionary<string, string>();

            foreach (var path in imagePaths)
            {
                try
                {
                    using var image = TesseractOCR.Pix.Image.LoadFromFile(path);
                    using var page = _engine.Process(image);

                    if (page.MeanConfidence >= minimumConfidence)
                    {
                        results[path] = page.Text;
                    }
                    else
                    {
                        results[path] = $"[Low confidence: {page.MeanConfidence:P0}] {page.Text}";
                    }
                }
                catch (Exception ex)
                {
                    results[path] = $"Error: {ex.Message}";
                }
            }

            return results;
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
                if (disposing)
                {
                    // CRITICAL: Engine must be disposed to release native memory
                    _engine?.Dispose();
                }
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Thread-safe batch processing with TesseractOCR.
    /// Since Engine is not thread-safe, we create one engine per thread.
    /// This is memory-intensive but necessary for parallel processing.
    /// </summary>
    public class ThreadSafeOcrService
    {
        private readonly string _tessDataPath;
        private readonly int _maxDegreeOfParallelism;

        public ThreadSafeOcrService(string tessDataPath = "./tessdata", int maxDegreeOfParallelism = 4)
        {
            _tessDataPath = tessDataPath;
            _maxDegreeOfParallelism = maxDegreeOfParallelism;
        }

        /// <summary>
        /// Parallel processing with per-thread engine instances.
        /// Memory usage = _maxDegreeOfParallelism * engine memory (~40-100MB each)
        /// </summary>
        public ConcurrentDictionary<string, string> ProcessParallel(string[] imagePaths)
        {
            var results = new ConcurrentDictionary<string, string>();

            // WARNING: Each thread creates its own Engine
            // This is required because TesseractEngine is NOT thread-safe
            // Memory overhead: 4 threads x 50MB = 200MB+ just for engines

            Parallel.ForEach(
                imagePaths,
                new ParallelOptions { MaxDegreeOfParallelism = _maxDegreeOfParallelism },
                imagePath =>
                {
                    // Per-thread engine - required for thread safety
                    using var engine = new Engine(_tessDataPath, Language.English, EngineMode.Default);
                    using var image = TesseractOCR.Pix.Image.LoadFromFile(imagePath);
                    using var page = engine.Process(image);

                    results[imagePath] = page.Text;
                });

            return results;
        }
    }

    /// <summary>
    /// Common error handling patterns for TesseractOCR.
    /// </summary>
    public class ErrorHandlingExamples
    {
        public void HandleCommonErrors(string imagePath)
        {
            try
            {
                using var engine = new Engine("./tessdata", Language.English, EngineMode.Default);
                using var image = TesseractOCR.Pix.Image.LoadFromFile(imagePath);
                using var page = engine.Process(image);

                Console.WriteLine(page.Text);
            }
            catch (TesseractOCR.Exceptions.TesseractException ex)
            {
                // TesseractOCR-specific exceptions
                // Usually indicates engine initialization or processing failure
                Console.WriteLine($"Tesseract error: {ex.Message}");

                // Common causes:
                // - tessdata path incorrect
                // - traineddata file corrupted
                // - Tesseract/tessdata version mismatch
            }
            catch (DllNotFoundException ex)
            {
                // Native library not found
                Console.WriteLine($"Native library error: {ex.Message}");

                // Common causes:
                // - Missing Visual C++ Redistributable (Windows)
                // - Missing shared libraries (Linux: apt-get install libtesseract-dev)
                // - Architecture mismatch (x86 vs x64)
            }
            catch (FileNotFoundException ex)
            {
                // Image file not found
                Console.WriteLine($"File not found: {ex.Message}");
            }
            catch (OutOfMemoryException ex)
            {
                // Large image or memory leak
                Console.WriteLine($"Memory error: {ex.Message}");

                // Common causes:
                // - Very large image (>100MP)
                // - Missing Dispose() calls causing memory leak
                // - Too many parallel engines
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
        }
    }

    // Example usage
    class Program
    {
        static void Main(string[] args)
        {
            // Basic usage
            var basicService = new BasicOcrService("./tessdata");
            var text = basicService.ExtractText("document.png");
            Console.WriteLine($"Extracted text:\n{text}");

            // With confidence
            var (extractedText, confidence) = basicService.ExtractTextWithConfidence("document.png");
            Console.WriteLine($"Confidence: {confidence:P0}");

            // Batch processing (engine reuse)
            using var batchService = new BatchOcrService("./tessdata");
            var batchResults = batchService.ProcessBatch(new[] { "doc1.png", "doc2.png", "doc3.png" });

            // Parallel processing (memory-intensive)
            var parallelService = new ThreadSafeOcrService("./tessdata", maxDegreeOfParallelism: 4);
            var parallelResults = parallelService.ProcessParallel(new[] { "doc1.png", "doc2.png" });
        }
    }
}
