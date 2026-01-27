/**
 * Basic Text Extraction: Tesseract .NET vs IronOCR
 *
 * This example demonstrates fundamental OCR text extraction using various
 * Tesseract .NET wrappers compared to IronOCR's streamlined approach.
 *
 * Key Differences:
 * - Tesseract requires external tessdata files and engine initialization
 * - IronOCR includes all language data and initializes automatically
 * - Tesseract needs explicit Pix/image handling; IronOCR accepts any image format
 *
 * NuGet Packages Required:
 * - Tesseract (charlesw): Tesseract version 5.2.0+
 * - IronOCR: IronOcr version 2024.x+
 */

using System;
using System.IO;
using System.Drawing;

// ============================================================================
// TESSERACT APPROACH (charlesw/Tesseract)
// ============================================================================

namespace TesseractExamples
{
    using Tesseract;

    /// <summary>
    /// Basic text extraction using charlesw/Tesseract wrapper.
    /// Requires tessdata folder with trained language files.
    /// </summary>
    public class BasicTesseractExtraction
    {
        // Path to tessdata folder - MUST be configured for each deployment
        private const string TessDataPath = @"./tessdata";

        /// <summary>
        /// Simple text extraction from an image file.
        /// </summary>
        public static string ExtractText(string imagePath)
        {
            // Validate tessdata exists - common deployment issue
            if (!Directory.Exists(TessDataPath))
            {
                throw new DirectoryNotFoundException(
                    $"Tessdata folder not found at {TessDataPath}. " +
                    "Download trained data from https://github.com/tesseract-ocr/tessdata");
            }

            // Initialize Tesseract engine - must specify language
            using (var engine = new TesseractEngine(TessDataPath, "eng", EngineMode.Default))
            {
                // Load image as Pix format (Leptonica)
                using (var img = Pix.LoadFromFile(imagePath))
                {
                    // Perform OCR
                    using (var page = engine.Process(img))
                    {
                        return page.GetText();
                    }
                }
            }
        }

        /// <summary>
        /// Extract text with confidence score.
        /// </summary>
        public static (string Text, float Confidence) ExtractTextWithConfidence(string imagePath)
        {
            using (var engine = new TesseractEngine(TessDataPath, "eng", EngineMode.Default))
            {
                using (var img = Pix.LoadFromFile(imagePath))
                {
                    using (var page = engine.Process(img))
                    {
                        return (page.GetText(), page.GetMeanConfidence());
                    }
                }
            }
        }

        /// <summary>
        /// Multi-language OCR requires separate engine initialization or combined traineddata.
        /// </summary>
        public static string ExtractMultiLanguage(string imagePath, string[] languages)
        {
            // Languages must be combined with '+' character
            string langString = string.Join("+", languages); // e.g., "eng+fra+deu"

            // Each language requires its .traineddata file in tessdata folder
            foreach (var lang in languages)
            {
                var trainedDataPath = Path.Combine(TessDataPath, $"{lang}.traineddata");
                if (!File.Exists(trainedDataPath))
                {
                    throw new FileNotFoundException(
                        $"Language data not found: {trainedDataPath}");
                }
            }

            using (var engine = new TesseractEngine(TessDataPath, langString, EngineMode.Default))
            {
                using (var img = Pix.LoadFromFile(imagePath))
                {
                    using (var page = engine.Process(img))
                    {
                        return page.GetText();
                    }
                }
            }
        }

        /// <summary>
        /// Extract text from specific region of image.
        /// </summary>
        public static string ExtractFromRegion(string imagePath, Rectangle region)
        {
            using (var engine = new TesseractEngine(TessDataPath, "eng", EngineMode.Default))
            {
                using (var img = Pix.LoadFromFile(imagePath))
                {
                    // Create Rect from System.Drawing.Rectangle
                    var tessRect = new Rect(region.X, region.Y, region.Width, region.Height);

                    using (var page = engine.Process(img, tessRect))
                    {
                        return page.GetText();
                    }
                }
            }
        }

        /// <summary>
        /// Word-by-word extraction with bounding boxes.
        /// </summary>
        public static void ExtractWordsWithPositions(string imagePath)
        {
            using (var engine = new TesseractEngine(TessDataPath, "eng", EngineMode.Default))
            {
                using (var img = Pix.LoadFromFile(imagePath))
                {
                    using (var page = engine.Process(img))
                    {
                        using (var iter = page.GetIterator())
                        {
                            iter.Begin();
                            do
                            {
                                if (iter.TryGetBoundingBox(PageIteratorLevel.Word, out var bounds))
                                {
                                    string word = iter.GetText(PageIteratorLevel.Word);
                                    float confidence = iter.GetConfidence(PageIteratorLevel.Word);

                                    Console.WriteLine($"Word: '{word?.Trim()}' " +
                                        $"at ({bounds.X1},{bounds.Y1})-({bounds.X2},{bounds.Y2}) " +
                                        $"Confidence: {confidence:P1}");
                                }
                            } while (iter.Next(PageIteratorLevel.Word));
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Memory stream handling for Tesseract - more complex than file-based.
    /// </summary>
    public class TesseractStreamHandling
    {
        private const string TessDataPath = @"./tessdata";

        /// <summary>
        /// Extract text from byte array (common in web applications).
        /// Tesseract doesn't directly support byte arrays - must use Pix.LoadFromMemory.
        /// </summary>
        public static string ExtractFromBytes(byte[] imageData)
        {
            using (var engine = new TesseractEngine(TessDataPath, "eng", EngineMode.Default))
            {
                // Pix.LoadFromMemory expects specific image formats
                using (var img = Pix.LoadFromMemory(imageData))
                {
                    using (var page = engine.Process(img))
                    {
                        return page.GetText();
                    }
                }
            }
        }

        /// <summary>
        /// Extract from Stream - requires conversion to byte array first.
        /// </summary>
        public static string ExtractFromStream(Stream imageStream)
        {
            using (var ms = new MemoryStream())
            {
                imageStream.CopyTo(ms);
                return ExtractFromBytes(ms.ToArray());
            }
        }
    }
}


// ============================================================================
// IRONOCR APPROACH - Simplified, Production-Ready
// ============================================================================

namespace IronOcrExamples
{
    using IronOcr;

    /// <summary>
    /// Basic text extraction using IronOCR.
    /// No external dependencies, no tessdata management, automatic preprocessing.
    /// </summary>
    public class BasicIronOcrExtraction
    {
        /// <summary>
        /// Simple text extraction - one line of meaningful code.
        /// Language data is embedded, no external files required.
        /// </summary>
        public static string ExtractText(string imagePath)
        {
            var result = new IronTesseract().Read(imagePath);
            return result.Text;
        }

        /// <summary>
        /// Extract text with confidence score.
        /// </summary>
        public static (string Text, double Confidence) ExtractTextWithConfidence(string imagePath)
        {
            var result = new IronTesseract().Read(imagePath);
            return (result.Text, result.Confidence);
        }

        /// <summary>
        /// Multi-language OCR - just add language packs via NuGet.
        /// No manual tessdata management required.
        /// </summary>
        public static string ExtractMultiLanguage(string imagePath)
        {
            var ocr = new IronTesseract();

            // Languages installed via NuGet: IronOcr.Languages.French, etc.
            ocr.Language = OcrLanguage.English;
            ocr.AddSecondaryLanguage(OcrLanguage.French);
            ocr.AddSecondaryLanguage(OcrLanguage.German);

            var result = ocr.Read(imagePath);
            return result.Text;
        }

        /// <summary>
        /// Extract text from specific region.
        /// Uses System.Drawing.Rectangle directly - no conversion needed.
        /// </summary>
        public static string ExtractFromRegion(string imagePath, Rectangle region)
        {
            var ocr = new IronTesseract();

            using (var input = new OcrInput())
            {
                // CropRectangle works with standard .NET types
                input.LoadImage(imagePath, new CropRectangle(region));
                var result = ocr.Read(input);
                return result.Text;
            }
        }

        /// <summary>
        /// Word-by-word extraction with positions - cleaner API.
        /// </summary>
        public static void ExtractWordsWithPositions(string imagePath)
        {
            var result = new IronTesseract().Read(imagePath);

            foreach (var word in result.Words)
            {
                Console.WriteLine($"Word: '{word.Text}' " +
                    $"at ({word.X},{word.Y}) size ({word.Width}x{word.Height}) " +
                    $"Confidence: {word.Confidence:P1}");
            }
        }

        /// <summary>
        /// Paragraph-level extraction for document structure.
        /// </summary>
        public static void ExtractParagraphs(string imagePath)
        {
            var result = new IronTesseract().Read(imagePath);

            int paragraphNumber = 1;
            foreach (var paragraph in result.Paragraphs)
            {
                Console.WriteLine($"Paragraph {paragraphNumber++}:");
                Console.WriteLine(paragraph.Text);
                Console.WriteLine($"Confidence: {paragraph.Confidence:P1}");
                Console.WriteLine();
            }
        }
    }

    /// <summary>
    /// Stream and byte array handling - seamless in IronOCR.
    /// </summary>
    public class IronOcrStreamHandling
    {
        /// <summary>
        /// Extract from byte array - direct support.
        /// </summary>
        public static string ExtractFromBytes(byte[] imageData)
        {
            using (var input = new OcrInput())
            {
                input.LoadImage(imageData);
                return new IronTesseract().Read(input).Text;
            }
        }

        /// <summary>
        /// Extract from Stream - direct support.
        /// </summary>
        public static string ExtractFromStream(Stream imageStream)
        {
            using (var input = new OcrInput())
            {
                input.LoadImage(imageStream);
                return new IronTesseract().Read(input).Text;
            }
        }

        /// <summary>
        /// Extract from URL - built-in support.
        /// </summary>
        public static string ExtractFromUrl(string imageUrl)
        {
            using (var input = new OcrInput())
            {
                input.LoadImageFromUrl(imageUrl);
                return new IronTesseract().Read(input).Text;
            }
        }
    }
}


// ============================================================================
// COMPARISON: Side-by-Side Implementation
// ============================================================================

namespace ComparisonExamples
{
    using System.Diagnostics;

    /// <summary>
    /// Direct comparison of the same task in both libraries.
    /// </summary>
    public class SideBySideComparison
    {
        /// <summary>
        /// Compare basic extraction - setup and execution.
        /// </summary>
        public static void CompareBasicExtraction(string imagePath)
        {
            Console.WriteLine("=== TESSERACT (charlesw) ===");
            Console.WriteLine("Setup required:");
            Console.WriteLine("  1. Download tessdata files (~15-50MB per language)");
            Console.WriteLine("  2. Configure tessdata path in application");
            Console.WriteLine("  3. Ensure correct permissions on tessdata folder");
            Console.WriteLine("  4. Handle missing traineddata exceptions");
            Console.WriteLine();

            // Tesseract extraction
            var sw = Stopwatch.StartNew();
            try
            {
                using (var engine = new Tesseract.TesseractEngine(@"./tessdata", "eng",
                    Tesseract.EngineMode.Default))
                {
                    using (var img = Tesseract.Pix.LoadFromFile(imagePath))
                    {
                        using (var page = engine.Process(img))
                        {
                            sw.Stop();
                            Console.WriteLine($"Text: {page.GetText().Substring(0, 100)}...");
                            Console.WriteLine($"Time: {sw.ElapsedMilliseconds}ms");
                            Console.WriteLine($"Confidence: {page.GetMeanConfidence():P1}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("Common issues: missing tessdata, wrong path, unsupported format");
            }

            Console.WriteLine();
            Console.WriteLine("=== IRONOCR ===");
            Console.WriteLine("Setup required:");
            Console.WriteLine("  1. Install NuGet package");
            Console.WriteLine("  2. (Optional) Add license key for production");
            Console.WriteLine();

            // IronOCR extraction
            sw.Restart();
            try
            {
                var result = new IronOcr.IronTesseract().Read(imagePath);
                sw.Stop();
                Console.WriteLine($"Text: {result.Text.Substring(0, 100)}...");
                Console.WriteLine($"Time: {sw.ElapsedMilliseconds}ms");
                Console.WriteLine($"Confidence: {result.Confidence:P1}");
            }
            catch (Exception ex)
            {
                sw.Stop();
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Compare error handling requirements.
        /// </summary>
        public static void CompareErrorHandling()
        {
            Console.WriteLine("=== ERROR HANDLING COMPARISON ===");
            Console.WriteLine();
            Console.WriteLine("Tesseract common exceptions to handle:");
            Console.WriteLine("  - TesseractException: Engine initialization failed");
            Console.WriteLine("  - FileNotFoundException: Missing traineddata file");
            Console.WriteLine("  - DirectoryNotFoundException: tessdata folder missing");
            Console.WriteLine("  - InvalidOperationException: Unsupported image format");
            Console.WriteLine("  - AccessViolationException: Native library issues");
            Console.WriteLine();
            Console.WriteLine("IronOCR exception handling:");
            Console.WriteLine("  - IronOcrException: All OCR errors wrapped in single type");
            Console.WriteLine("  - Clear error messages with troubleshooting guidance");
            Console.WriteLine("  - No native library crashes - managed code throughout");
        }
    }
}


// ============================================================================
// DEPLOYMENT CONSIDERATIONS
// ============================================================================

namespace DeploymentExamples
{
    /// <summary>
    /// Demonstrates deployment complexity differences.
    /// </summary>
    public class DeploymentComparison
    {
        /// <summary>
        /// Tesseract deployment checklist.
        /// </summary>
        public static void TesseractDeploymentChecklist()
        {
            Console.WriteLine("=== TESSERACT DEPLOYMENT CHECKLIST ===");
            Console.WriteLine();
            Console.WriteLine("Required files to deploy:");
            Console.WriteLine("  [ ] tessdata/ folder with .traineddata files");
            Console.WriteLine("  [ ] leptonica native libraries (platform-specific)");
            Console.WriteLine("  [ ] tesseract native libraries (platform-specific)");
            Console.WriteLine();
            Console.WriteLine("Platform-specific native libraries:");
            Console.WriteLine("  Windows x64: tesseract50.dll, leptonica-1.82.0.dll");
            Console.WriteLine("  Windows x86: tesseract50.dll (32-bit), leptonica-1.82.0.dll (32-bit)");
            Console.WriteLine("  Linux x64: libtesseract.so.5, liblept.so.5");
            Console.WriteLine("  macOS: libtesseract.dylib, liblept.dylib");
            Console.WriteLine();
            Console.WriteLine("Configuration requirements:");
            Console.WriteLine("  - Set TESSDATA_PREFIX environment variable");
            Console.WriteLine("  - Ensure native library path is in system PATH");
            Console.WriteLine("  - Verify file permissions for tessdata folder");
            Console.WriteLine();
            Console.WriteLine("Docker considerations:");
            Console.WriteLine("  - Install tesseract-ocr package in Dockerfile");
            Console.WriteLine("  - Copy traineddata files to container");
            Console.WriteLine("  - Set TESSDATA_PREFIX in container environment");
        }

        /// <summary>
        /// IronOCR deployment - simplified.
        /// </summary>
        public static void IronOcrDeploymentChecklist()
        {
            Console.WriteLine("=== IRONOCR DEPLOYMENT CHECKLIST ===");
            Console.WriteLine();
            Console.WriteLine("Required: NuGet package only");
            Console.WriteLine();
            Console.WriteLine("Platform support (automatic):");
            Console.WriteLine("  [x] Windows x64 - included");
            Console.WriteLine("  [x] Windows x86 - included");
            Console.WriteLine("  [x] Linux x64 - included");
            Console.WriteLine("  [x] macOS - included");
            Console.WriteLine("  [x] Azure App Service - included");
            Console.WriteLine("  [x] AWS Lambda - included");
            Console.WriteLine("  [x] Docker - works out of box");
            Console.WriteLine();
            Console.WriteLine("Language data:");
            Console.WriteLine("  - English included by default");
            Console.WriteLine("  - Additional languages via NuGet packages");
            Console.WriteLine("  - No external file management required");
            Console.WriteLine();
            Console.WriteLine("Docker: Use any .NET base image, no additional packages needed");
        }
    }
}
