/**
 * GdPicture.NET to IronOCR Migration Examples
 *
 * This file demonstrates before/after patterns for migrating
 * from GdPicture.NET's integer ID-based resource management
 * to IronOCR's standard .NET patterns.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * Migration scenarios covered:
 * 1. Component initialization
 * 2. Resource configuration
 * 3. Basic OCR
 * 4. Image ID management
 * 5. Multi-image processing
 * 6. Status code handling
 * 7. Confidence scores
 * 8. Searchable PDF creation
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// ============================================================================
// SCENARIO 1: COMPONENT INITIALIZATION
// GdPicture requires multiple components; IronOCR is a single class
// ============================================================================

namespace MigrationScenario1_Initialization
{
    // BEFORE: GdPicture - Multiple components required
    namespace Before
    {
        using GdPicture14; // Version in namespace

        public class GdPictureInitialization
        {
            private LicenseManager _licenseManager;
            private GdPictureImaging _imaging;
            private GdPictureOCR _ocr;

            public GdPictureInitialization()
            {
                // Step 1: License registration (must be first)
                _licenseManager = new LicenseManager();
                _licenseManager.RegisterKEY("GDPICTURE-LICENSE-KEY");

                // Step 2: Initialize imaging component (for loading images)
                _imaging = new GdPictureImaging();

                // Step 3: Initialize OCR component (for text extraction)
                _ocr = new GdPictureOCR();

                // Step 4: Configure resource path (required!)
                _ocr.ResourceFolder = @"C:\GdPicture\Resources\OCR";
            }

            public void Dispose()
            {
                _ocr?.Dispose();
                _imaging?.Dispose();
            }
        }
    }

    // AFTER: IronOCR - Single class, no resource folder
    namespace After
    {
        using IronOcr;

        public class IronOcrInitialization
        {
            public IronOcrInitialization()
            {
                // One line license configuration (can be in Program.cs)
                IronOcr.License.LicenseKey = "IRONOCR-LICENSE-KEY";

                // No other initialization needed!
                // IronTesseract instances are created per-use
            }
        }
    }
}


// ============================================================================
// SCENARIO 2: RESOURCE CONFIGURATION
// GdPicture needs external tessdata files; IronOCR bundles resources
// ============================================================================

namespace MigrationScenario2_ResourceConfiguration
{
    // BEFORE: GdPicture - External resource folder required
    namespace Before
    {
        using GdPicture14;

        public class GdPictureResourceConfig
        {
            private GdPictureOCR _ocr;

            public void ConfigureOcr()
            {
                _ocr = new GdPictureOCR();

                // REQUIRED: Must point to folder containing traineddata files
                _ocr.ResourceFolder = @"C:\GdPicture\Resources\OCR";

                // Folder structure must exist:
                // C:\GdPicture\Resources\OCR\
                //   eng.traineddata  (~15MB)
                //   fra.traineddata  (~15MB)
                //   deu.traineddata  (~15MB)
                //   ... etc

                // Common errors if path is wrong:
                // - Silent failure with empty results
                // - Generic "OCR failed" error
                // - Works in dev, fails in production
            }
        }
    }

    // AFTER: IronOCR - Resources bundled, no configuration needed
    namespace After
    {
        using IronOcr;

        public class IronOcrResourceConfig
        {
            public void ConfigureOcr()
            {
                // No resource folder needed!
                // English is bundled in the NuGet package

                var ocr = new IronTesseract();

                // Additional languages: just install NuGet packages
                // Install-Package IronOcr.Languages.French
                // Install-Package IronOcr.Languages.German
                // Languages auto-download if not present
            }
        }
    }
}


// ============================================================================
// SCENARIO 3: BASIC OCR
// GdPicture requires 10+ lines; IronOCR needs 1 line
// ============================================================================

namespace MigrationScenario3_BasicOcr
{
    // BEFORE: GdPicture - Multi-step process
    namespace Before
    {
        using GdPicture14;

        public class GdPictureBasicOcr
        {
            private GdPictureImaging _imaging;
            private GdPictureOCR _ocr;

            public string ExtractText(string imagePath)
            {
                // Step 1: Load image into imaging component
                int imageId = _imaging.CreateGdPictureImageFromFile(imagePath);

                // Step 2: Check if load succeeded
                if (imageId == 0)
                {
                    throw new Exception($"Failed to load image: {_imaging.GetStat()}");
                }

                try
                {
                    // Step 3: Set image on OCR component
                    _ocr.SetImage(imageId);

                    // Step 4: Set language
                    _ocr.Language = "eng";

                    // Step 5: Run OCR
                    string resultId = _ocr.RunOCR();

                    // Step 6: Check if OCR succeeded
                    if (string.IsNullOrEmpty(resultId))
                    {
                        throw new Exception($"OCR failed: {_ocr.GetStat()}");
                    }

                    // Step 7: Get text from result
                    return _ocr.GetOCRResultText(resultId);
                }
                finally
                {
                    // Step 8: CRITICAL - Release image to prevent memory leak
                    _imaging.ReleaseGdPictureImage(imageId);
                }
            }
        }
    }

    // AFTER: IronOCR - One line
    namespace After
    {
        using IronOcr;

        public class IronOcrBasicOcr
        {
            public string ExtractText(string imagePath)
            {
                // All-in-one: load, process, extract, cleanup
                return new IronTesseract().Read(imagePath).Text;
            }

            // Or with more explicit control (still much simpler):
            public string ExtractTextExplicit(string imagePath)
            {
                var ocr = new IronTesseract();

                using var input = new OcrInput();
                input.LoadImage(imagePath);

                var result = ocr.Read(input);
                return result.Text;
                // Automatic cleanup via using statement
            }
        }
    }
}


// ============================================================================
// SCENARIO 4: IMAGE ID MANAGEMENT
// GdPicture's integer ID pattern vs standard .NET disposal
// ============================================================================

namespace MigrationScenario4_ImageIdManagement
{
    // BEFORE: GdPicture - Manual integer ID tracking
    namespace Before
    {
        using GdPicture14;

        public class GdPictureImageIdManagement
        {
            private GdPictureImaging _imaging;
            private GdPictureOCR _ocr;

            // CRITICAL: Every image ID must be released!
            public string ProcessSingleImage(string path)
            {
                int imageId = _imaging.CreateGdPictureImageFromFile(path);

                try
                {
                    _ocr.SetImage(imageId);
                    _ocr.Language = "eng";
                    var resultId = _ocr.RunOCR();
                    return _ocr.GetOCRResultText(resultId);
                }
                finally
                {
                    // MUST call this - no automatic cleanup
                    _imaging.ReleaseGdPictureImage(imageId);
                }
            }

            // What happens without cleanup:
            public void MemoryLeakExample(string[] paths)
            {
                foreach (var path in paths)
                {
                    int imageId = _imaging.CreateGdPictureImageFromFile(path);
                    _ocr.SetImage(imageId);
                    var resultId = _ocr.RunOCR();
                    Console.WriteLine(_ocr.GetOCRResultText(resultId));

                    // FORGOT: _imaging.ReleaseGdPictureImage(imageId);
                    // Memory grows with each iteration!
                    // Process eventually crashes
                }
            }
        }
    }

    // AFTER: IronOCR - Standard .NET using statements
    namespace After
    {
        using IronOcr;

        public class IronOcrImageManagement
        {
            // Automatic cleanup with using statement
            public string ProcessSingleImage(string path)
            {
                using var input = new OcrInput();
                input.LoadImage(path);

                return new IronTesseract().Read(input).Text;
                // Resources released when using block exits
            }

            // No memory leak risk
            public void SafeProcessing(string[] paths)
            {
                var ocr = new IronTesseract();

                foreach (var path in paths)
                {
                    using var input = new OcrInput();
                    input.LoadImage(path);

                    var result = ocr.Read(input);
                    Console.WriteLine(result.Text);
                    // Each iteration cleans up automatically
                }
            }
        }
    }
}


// ============================================================================
// SCENARIO 5: MULTI-IMAGE BATCH PROCESSING
// ID tracking for multiple images vs automatic management
// ============================================================================

namespace MigrationScenario5_MultiImageProcessing
{
    // BEFORE: GdPicture - Must track all IDs for cleanup
    namespace Before
    {
        using GdPicture14;

        public class GdPictureMultiImage
        {
            private GdPictureImaging _imaging;
            private GdPictureOCR _ocr;

            public Dictionary<string, string> ProcessBatch(string[] imagePaths)
            {
                var results = new Dictionary<string, string>();
                var imageIds = new List<int>(); // Must track all IDs

                try
                {
                    foreach (var path in imagePaths)
                    {
                        int imageId = _imaging.CreateGdPictureImageFromFile(path);

                        if (imageId != 0)
                        {
                            imageIds.Add(imageId); // Track for cleanup

                            _ocr.SetImage(imageId);
                            _ocr.Language = "eng";
                            var resultId = _ocr.RunOCR();

                            if (!string.IsNullOrEmpty(resultId))
                            {
                                results[path] = _ocr.GetOCRResultText(resultId);
                            }
                        }
                    }
                }
                finally
                {
                    // CRITICAL: Release ALL collected IDs
                    foreach (var id in imageIds)
                    {
                        _imaging.ReleaseGdPictureImage(id);
                    }
                }

                return results;
            }
        }
    }

    // AFTER: IronOCR - Automatic batch processing
    namespace After
    {
        using IronOcr;

        public class IronOcrMultiImage
        {
            public Dictionary<string, string> ProcessBatch(string[] imagePaths)
            {
                var ocr = new IronTesseract();
                var results = new Dictionary<string, string>();

                // Option 1: Process individually with automatic cleanup
                foreach (var path in imagePaths)
                {
                    using var input = new OcrInput();
                    input.LoadImage(path);
                    results[path] = ocr.Read(input).Text;
                }

                return results;
            }

            // Option 2: Load all into single input for batch processing
            public string ProcessAllTogether(string[] imagePaths)
            {
                var ocr = new IronTesseract();

                using var input = new OcrInput();

                foreach (var path in imagePaths)
                {
                    input.LoadImage(path);
                }

                // Process all at once - returns combined result
                return ocr.Read(input).Text;
            }
        }
    }
}


// ============================================================================
// SCENARIO 6: STATUS CODE HANDLING
// GdPictureStatus enum checking vs standard exceptions
// ============================================================================

namespace MigrationScenario6_StatusCodeHandling
{
    // BEFORE: GdPicture - Check GdPictureStatus after every operation
    namespace Before
    {
        using GdPicture14;

        public class GdPictureStatusHandling
        {
            private GdPictureImaging _imaging;
            private GdPictureOCR _ocr;

            public string ProcessWithStatusChecks(string path)
            {
                // Load image
                int imageId = _imaging.CreateGdPictureImageFromFile(path);

                // Check load status
                if (imageId == 0)
                {
                    GdPictureStatus status = _imaging.GetStat();

                    // Status codes are often generic
                    switch (status)
                    {
                        case GdPictureStatus.InvalidParameter:
                            throw new Exception("Invalid file path");
                        case GdPictureStatus.UnsupportedPixelFormat:
                            throw new Exception("Unsupported image format");
                        default:
                            throw new Exception($"Load failed: {status}");
                    }
                }

                try
                {
                    _ocr.SetImage(imageId);
                    _ocr.Language = "eng";

                    string resultId = _ocr.RunOCR();

                    // Check OCR status
                    if (string.IsNullOrEmpty(resultId))
                    {
                        GdPictureStatus ocrStatus = _ocr.GetStat();
                        throw new Exception($"OCR failed: {ocrStatus}");
                    }

                    return _ocr.GetOCRResultText(resultId);
                }
                finally
                {
                    _imaging.ReleaseGdPictureImage(imageId);
                }
            }
        }
    }

    // AFTER: IronOCR - Standard .NET exceptions
    namespace After
    {
        using IronOcr;

        public class IronOcrExceptionHandling
        {
            public string ProcessWithExceptions(string path)
            {
                try
                {
                    // Exceptions thrown for errors (standard .NET pattern)
                    return new IronTesseract().Read(path).Text;
                }
                catch (FileNotFoundException ex)
                {
                    // Standard .NET exception types
                    throw new Exception($"File not found: {ex.Message}");
                }
                catch (IronOcr.Exceptions.OcrException ex)
                {
                    // IronOCR-specific exceptions with clear messages
                    throw new Exception($"OCR error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    // Generic catch for unexpected issues
                    throw new Exception($"Unexpected error: {ex.Message}");
                }
            }
        }
    }
}


// ============================================================================
// SCENARIO 7: CONFIDENCE SCORES
// Different ways to access recognition confidence
// ============================================================================

namespace MigrationScenario7_ConfidenceScores
{
    // BEFORE: GdPicture - Result ID-based confidence access
    namespace Before
    {
        using GdPicture14;

        public class GdPictureConfidence
        {
            private GdPictureOCR _ocr;

            public (string text, float confidence) GetTextWithConfidence(int imageId)
            {
                _ocr.SetImage(imageId);
                _ocr.Language = "eng";

                string resultId = _ocr.RunOCR();

                if (string.IsNullOrEmpty(resultId))
                {
                    throw new Exception("OCR failed");
                }

                // Get overall confidence
                float confidence = _ocr.GetOCRResultConfidence(resultId);

                // Get text
                string text = _ocr.GetOCRResultText(resultId);

                return (text, confidence);
            }

            // Per-word confidence requires iteration
            public List<(string word, float confidence)> GetWordConfidences(int imageId)
            {
                var words = new List<(string, float)>();

                _ocr.SetImage(imageId);
                _ocr.Language = "eng";
                string resultId = _ocr.RunOCR();

                // Complex iteration through result blocks, lines, words
                int blockCount = _ocr.GetOCRResultBlockCount(resultId);

                for (int b = 0; b < blockCount; b++)
                {
                    int lineCount = _ocr.GetOCRResultBlockLineCount(resultId, b);

                    for (int l = 0; l < lineCount; l++)
                    {
                        int wordCount = _ocr.GetOCRResultBlockLineWordCount(resultId, b, l);

                        for (int w = 0; w < wordCount; w++)
                        {
                            string word = _ocr.GetOCRResultBlockLineWordText(resultId, b, l, w);
                            float conf = _ocr.GetOCRResultBlockLineWordConfidence(resultId, b, l, w);
                            words.Add((word, conf));
                        }
                    }
                }

                return words;
            }
        }
    }

    // AFTER: IronOCR - Direct property access
    namespace After
    {
        using IronOcr;

        public class IronOcrConfidence
        {
            public (string text, double confidence) GetTextWithConfidence(string imagePath)
            {
                var result = new IronTesseract().Read(imagePath);

                // Direct property access
                return (result.Text, result.Confidence);
            }

            // Per-word confidence is simple LINQ
            public List<(string word, double confidence)> GetWordConfidences(string imagePath)
            {
                var result = new IronTesseract().Read(imagePath);

                // Direct access to Words collection
                return result.Words
                    .Select(w => (w.Text, w.Confidence))
                    .ToList();
            }

            // Also available: Lines, Paragraphs, Blocks
            public void AccessAllLevels(string imagePath)
            {
                var result = new IronTesseract().Read(imagePath);

                // Character level
                foreach (var character in result.Characters)
                {
                    Console.WriteLine($"'{character.Text}' at ({character.X},{character.Y})");
                }

                // Word level
                foreach (var word in result.Words)
                {
                    Console.WriteLine($"'{word.Text}' confidence: {word.Confidence:P0}");
                }

                // Line level
                foreach (var line in result.Lines)
                {
                    Console.WriteLine($"Line: {line.Text}");
                }

                // Paragraph level
                foreach (var para in result.Paragraphs)
                {
                    Console.WriteLine($"Paragraph: {para.Text.Substring(0, 50)}...");
                }
            }
        }
    }
}


// ============================================================================
// SCENARIO 8: SEARCHABLE PDF CREATION
// Creating PDFs with embedded OCR text layer
// ============================================================================

namespace MigrationScenario8_SearchablePdf
{
    // BEFORE: GdPicture - Page-by-page OCR with PDF plugin
    namespace Before
    {
        using GdPicture14;

        public class GdPictureSearchablePdf
        {
            private GdPictureImaging _imaging;
            private GdPictureOCR _ocr;

            public void CreateSearchablePdf(string inputPdf, string outputPdf)
            {
                using var pdf = new GdPicturePDF();

                // Load PDF (requires PDF plugin license)
                var status = pdf.LoadFromFile(inputPdf, false);

                if (status != GdPictureStatus.OK)
                {
                    throw new Exception($"Failed to load PDF: {status}");
                }

                int pageCount = pdf.GetPageCount();

                // OCR each page
                for (int i = 1; i <= pageCount; i++)
                {
                    pdf.SelectPage(i);

                    // OcrPage adds text layer (requires OCR plugin)
                    var ocrStatus = pdf.OcrPage(
                        "eng",                           // Language
                        @"C:\GdPicture\Resources\OCR",   // Resource path
                        "",                              // No password
                        200                              // DPI
                    );

                    if (ocrStatus != GdPictureStatus.OK)
                    {
                        Console.WriteLine($"Warning: OCR failed on page {i}: {ocrStatus}");
                    }
                }

                // Save with linearization for web
                pdf.SaveToFile(outputPdf, true);
            }

            // From images to searchable PDF
            public void ImagesToSearchablePdf(string[] imagePaths, string outputPdf)
            {
                var imageIds = new List<int>();

                using var pdf = new GdPicturePDF();
                pdf.NewPDF();

                try
                {
                    foreach (var imagePath in imagePaths)
                    {
                        // Load image
                        int imageId = _imaging.CreateGdPictureImageFromFile(imagePath);

                        if (imageId != 0)
                        {
                            imageIds.Add(imageId);

                            // Add image as PDF page
                            pdf.AddImageFromGdPictureImage(imageId, false, true);
                        }
                    }

                    // OCR all pages
                    for (int i = 1; i <= pdf.GetPageCount(); i++)
                    {
                        pdf.SelectPage(i);
                        pdf.OcrPage("eng", @"C:\GdPicture\Resources\OCR", "", 200);
                    }

                    pdf.SaveToFile(outputPdf, true);
                }
                finally
                {
                    // Release all image IDs
                    foreach (var id in imageIds)
                    {
                        _imaging.ReleaseGdPictureImage(id);
                    }
                }
            }
        }
    }

    // AFTER: IronOCR - One-liner searchable PDF
    namespace After
    {
        using IronOcr;

        public class IronOcrSearchablePdf
        {
            public void CreateSearchablePdf(string inputPdf, string outputPdf)
            {
                // Load, OCR, and save as searchable - all automatic
                var result = new IronTesseract().Read(inputPdf);
                result.SaveAsSearchablePdf(outputPdf);
            }

            // From images to searchable PDF
            public void ImagesToSearchablePdf(string[] imagePaths, string outputPdf)
            {
                var ocr = new IronTesseract();

                using var input = new OcrInput();

                foreach (var imagePath in imagePaths)
                {
                    input.LoadImage(imagePath);
                }

                var result = ocr.Read(input);
                result.SaveAsSearchablePdf(outputPdf);
            }

            // Password-protected PDF - just works
            public void SearchableFromProtectedPdf(string inputPdf, string password, string outputPdf)
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
// MIGRATION SUMMARY
//
// Key differences:
// 1. Initialization: Multiple components vs single class
// 2. Resources: External folder vs bundled NuGet
// 3. Memory: Manual ID tracking vs automatic disposal
// 4. Errors: Status codes vs exceptions
// 5. Results: ID-based access vs direct properties
// 6. PDF: Separate plugin vs built-in
//
// Get IronOCR: https://ironsoftware.com/csharp/ocr/
// NuGet: Install-Package IronOcr
// ============================================================================
