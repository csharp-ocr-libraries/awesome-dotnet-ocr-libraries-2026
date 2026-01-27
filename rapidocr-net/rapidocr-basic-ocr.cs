/**
 * RapidOcrNet Basic OCR Examples
 *
 * This file demonstrates the 4-model configuration overhead required
 * by RapidOcrNet compared to zero-config alternatives like IronOCR.
 *
 * RapidOcrNet requires manual download and configuration of:
 * 1. Detection model (det.onnx)
 * 2. Classification model (cls.onnx)
 * 3. Recognition model (rec.onnx)
 * 4. Character dictionary (keys.txt)
 *
 * For zero-configuration OCR, try IronOCR:
 * https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RapidOcrNetExamples
{
    // ============================================================================
    // PART 1: MODEL SETUP AND CONFIGURATION (REQUIRED)
    // ============================================================================

    /// <summary>
    /// Model configuration helper for RapidOcrNet.
    /// You MUST download and configure these files before any OCR can work.
    /// </summary>
    public class RapidOcrModelSetup
    {
        // All 4 files must be present and paths must be correct
        private const string ModelDirectory = "./models";
        private const string DetectionModel = "det.onnx";      // ~3MB download
        private const string ClassificationModel = "cls.onnx";  // ~1MB download
        private const string RecognitionModel = "rec_en.onnx"; // ~2-10MB download
        private const string CharacterKeys = "en_keys.txt";     // ~100KB download

        /// <summary>
        /// Validates that all required model files are present.
        /// Run this before attempting any OCR operations.
        /// </summary>
        public static bool ValidateModelFiles()
        {
            var requiredFiles = new[]
            {
                Path.Combine(ModelDirectory, DetectionModel),
                Path.Combine(ModelDirectory, ClassificationModel),
                Path.Combine(ModelDirectory, RecognitionModel),
                Path.Combine(ModelDirectory, CharacterKeys)
            };

            var missingFiles = requiredFiles.Where(f => !File.Exists(f)).ToList();

            if (missingFiles.Any())
            {
                Console.WriteLine("ERROR: Missing required model files:");
                foreach (var file in missingFiles)
                {
                    Console.WriteLine($"  - {file}");
                }
                Console.WriteLine();
                Console.WriteLine("Download models from: https://github.com/RapidAI/RapidOCR");
                Console.WriteLine("Or use IronOCR for zero-config OCR: https://ironsoftware.com/csharp/ocr/");
                return false;
            }

            Console.WriteLine("All model files validated successfully.");
            return true;
        }

        /// <summary>
        /// Shows model download instructions for different languages.
        /// Each language requires different model files.
        /// </summary>
        public static void ShowModelDownloadInstructions(string language)
        {
            Console.WriteLine($"=== MODEL DOWNLOAD INSTRUCTIONS FOR {language.ToUpper()} ===\n");

            switch (language.ToLower())
            {
                case "english":
                case "en":
                    Console.WriteLine("Required files for English OCR:");
                    Console.WriteLine("  1. det.onnx - Text detection model");
                    Console.WriteLine("  2. cls.onnx - Direction classifier");
                    Console.WriteLine("  3. en_rec.onnx - English recognition model (~2MB)");
                    Console.WriteLine("  4. en_keys.txt - English character dictionary");
                    break;

                case "chinese":
                case "zh":
                    Console.WriteLine("Required files for Chinese OCR:");
                    Console.WriteLine("  1. det.onnx - Text detection model");
                    Console.WriteLine("  2. cls.onnx - Direction classifier");
                    Console.WriteLine("  3. ch_rec.onnx - Chinese recognition model (~10MB)");
                    Console.WriteLine("  4. ch_keys.txt - Chinese character dictionary (6000+ chars)");
                    break;

                case "japanese":
                case "ja":
                    Console.WriteLine("Required files for Japanese OCR (LIMITED SUPPORT):");
                    Console.WriteLine("  1. det.onnx - Text detection model");
                    Console.WriteLine("  2. cls.onnx - Direction classifier");
                    Console.WriteLine("  3. japan_rec.onnx - Japanese recognition model");
                    Console.WriteLine("  4. japan_keys.txt - Japanese character dictionary");
                    Console.WriteLine("\n  WARNING: Japanese support is experimental/community-contributed.");
                    break;

                default:
                    Console.WriteLine($"Language '{language}' may not be supported.");
                    Console.WriteLine("RapidOcrNet primarily supports: Chinese, English, Japanese, Korean");
                    Console.WriteLine();
                    Console.WriteLine("For 125+ languages, consider IronOCR:");
                    Console.WriteLine("https://ironsoftware.com/csharp/ocr/");
                    break;
            }

            Console.WriteLine("\nDownload source: https://github.com/RapidAI/RapidOCR/releases");
        }
    }

    // ============================================================================
    // PART 2: BASIC OCR OPERATIONS
    // ============================================================================

    /// <summary>
    /// Basic OCR service using RapidOcrNet.
    /// Demonstrates the 4-model configuration requirement.
    /// </summary>
    public class RapidOcrBasicService
    {
        /*
        // Uncomment when RapidOcrNet is installed and models are downloaded

        using RapidOcrNet;

        private readonly RapidOcrEngine _engine;

        /// <summary>
        /// Constructor requires explicit model paths for all 4 files.
        /// Compare to IronOCR: new IronTesseract() - no configuration needed.
        /// </summary>
        public RapidOcrBasicService(string modelPath)
        {
            // All 4 paths are REQUIRED - missing any one will fail
            _engine = new RapidOcrEngine(new RapidOcrOptions
            {
                DetModelPath = Path.Combine(modelPath, "det.onnx"),
                ClsModelPath = Path.Combine(modelPath, "cls.onnx"),
                RecModelPath = Path.Combine(modelPath, "rec_en.onnx"),
                KeysPath = Path.Combine(modelPath, "en_keys.txt"),

                // Optional configuration
                UseGpu = false,  // Requires CUDA and different NuGet package
                NumThreads = 4   // CPU threads for inference
            });
        }

        /// <summary>
        /// Extract text from an image file.
        /// </summary>
        public string ExtractText(string imagePath)
        {
            if (!File.Exists(imagePath))
                throw new FileNotFoundException($"Image not found: {imagePath}");

            var result = _engine.Run(imagePath);

            // Results come as text blocks with bounding boxes
            // Must be manually sorted and joined
            return string.Join("\n", result.TextBlocks
                .OrderBy(b => b.BoundingBox.Top)
                .ThenBy(b => b.BoundingBox.Left)
                .Select(b => b.Text));
        }

        /// <summary>
        /// Extract text with confidence scores.
        /// </summary>
        public IEnumerable<(string Text, float Confidence, Rectangle Bounds)> ExtractWithConfidence(string imagePath)
        {
            var result = _engine.Run(imagePath);

            foreach (var block in result.TextBlocks.OrderBy(b => b.BoundingBox.Top))
            {
                yield return (
                    block.Text,
                    block.Confidence,
                    new Rectangle(
                        (int)block.BoundingBox.Left,
                        (int)block.BoundingBox.Top,
                        (int)block.BoundingBox.Width,
                        (int)block.BoundingBox.Height
                    )
                );
            }
        }

        public void Dispose()
        {
            _engine?.Dispose();
        }
        */

        public void ShowConfigurationExample()
        {
            Console.WriteLine("=== RAPIDOCRNET CONFIGURATION EXAMPLE ===\n");
            Console.WriteLine("// RapidOcrNet requires 4 model files:");
            Console.WriteLine(@"
var engine = new RapidOcrEngine(new RapidOcrOptions
{
    DetModelPath = ""models/det.onnx"",      // Detection model
    ClsModelPath = ""models/cls.onnx"",      // Classification model
    RecModelPath = ""models/rec_en.onnx"",   // Recognition model
    KeysPath = ""models/en_keys.txt""        // Character dictionary
});
");
            Console.WriteLine("\n// Compare to IronOCR (zero configuration):");
            Console.WriteLine(@"
var text = new IronTesseract().Read(""image.png"").Text;
");
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }
    }

    // ============================================================================
    // PART 3: LANGUAGE-SPECIFIC CONFIGURATION
    // ============================================================================

    /// <summary>
    /// Demonstrates the complexity of switching languages in RapidOcrNet.
    /// Each language requires different model files to be downloaded.
    /// </summary>
    public class RapidOcrLanguageService
    {
        private readonly string _modelBasePath;

        public RapidOcrLanguageService(string modelBasePath)
        {
            _modelBasePath = modelBasePath;
        }

        /*
        using RapidOcrNet;

        /// <summary>
        /// Create engine configured for English text.
        /// Requires: det.onnx, cls.onnx, en_rec.onnx, en_keys.txt
        /// </summary>
        public RapidOcrEngine CreateEnglishEngine()
        {
            return new RapidOcrEngine(new RapidOcrOptions
            {
                DetModelPath = Path.Combine(_modelBasePath, "det.onnx"),
                ClsModelPath = Path.Combine(_modelBasePath, "cls.onnx"),
                RecModelPath = Path.Combine(_modelBasePath, "en_rec.onnx"),
                KeysPath = Path.Combine(_modelBasePath, "en_keys.txt")
            });
        }

        /// <summary>
        /// Create engine configured for Chinese text.
        /// Requires: det.onnx, cls.onnx, ch_rec.onnx, ch_keys.txt
        /// Note: Chinese recognition model is larger (~10MB vs ~2MB for English)
        /// </summary>
        public RapidOcrEngine CreateChineseEngine()
        {
            return new RapidOcrEngine(new RapidOcrOptions
            {
                DetModelPath = Path.Combine(_modelBasePath, "det.onnx"),
                ClsModelPath = Path.Combine(_modelBasePath, "cls.onnx"),
                RecModelPath = Path.Combine(_modelBasePath, "ch_rec.onnx"),
                KeysPath = Path.Combine(_modelBasePath, "ch_keys.txt")
            });
        }

        /// <summary>
        /// Create engine for mixed Chinese/English documents.
        /// Still requires Chinese models - English is embedded in Chinese dictionary.
        /// </summary>
        public RapidOcrEngine CreateMixedCjkEnglishEngine()
        {
            // Use Chinese models which include basic English support
            return CreateChineseEngine();
        }
        */

        /// <summary>
        /// Shows why language switching is complex in RapidOcrNet.
        /// </summary>
        public void ShowLanguageComplexity()
        {
            Console.WriteLine("=== RAPIDOCRNET LANGUAGE COMPLEXITY ===\n");

            Console.WriteLine("To switch languages in RapidOcrNet, you must:");
            Console.WriteLine("  1. Download different model files");
            Console.WriteLine("  2. Update configuration paths");
            Console.WriteLine("  3. Recreate the engine instance\n");

            Console.WriteLine("Language-specific files needed:");
            Console.WriteLine("  English:  en_rec.onnx + en_keys.txt");
            Console.WriteLine("  Chinese:  ch_rec.onnx + ch_keys.txt");
            Console.WriteLine("  Japanese: japan_rec.onnx + japan_keys.txt (limited)");
            Console.WriteLine("  Korean:   korean_rec.onnx + korean_keys.txt (limited)\n");

            Console.WriteLine("Languages NOT SUPPORTED by RapidOcrNet:");
            Console.WriteLine("  - Spanish, French, German, Italian, Portuguese");
            Console.WriteLine("  - Russian, Ukrainian, other Cyrillic");
            Console.WriteLine("  - Arabic, Hebrew");
            Console.WriteLine("  - Hindi, Bengali, Tamil, and other Indic scripts");
            Console.WriteLine("  - Thai, Vietnamese, Indonesian\n");

            Console.WriteLine("For 125+ languages with simple API, use IronOCR:");
            Console.WriteLine(@"
var ocr = new IronTesseract();
ocr.Language = OcrLanguage.Spanish;       // Just change the enum
ocr.Language = OcrLanguage.Russian;       // No model download needed
ocr.Language = OcrLanguage.Arabic;        // Works immediately
var text = ocr.Read(""document.png"").Text;
");
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }
    }

    // ============================================================================
    // PART 4: PDF PROCESSING (WORKAROUND REQUIRED)
    // ============================================================================

    /// <summary>
    /// Demonstrates that RapidOcrNet has NO native PDF support.
    /// PDF processing requires external libraries and manual conversion.
    /// </summary>
    public class RapidOcrPdfWorkaround
    {
        /*
        // PDF processing with RapidOcrNet requires:
        // 1. A PDF rendering library (PdfPig, iTextSharp, etc.)
        // 2. Converting each page to an image
        // 3. Running OCR on each image
        // 4. Combining results yourself

        public async Task<string> ExtractTextFromPdfWorkaround(string pdfPath)
        {
            // Step 1: Convert PDF to images (requires external library)
            var pageImages = await ConvertPdfToImages(pdfPath);

            // Step 2: Create RapidOcr engine (requires 4 model files)
            using var engine = new RapidOcrEngine(new RapidOcrOptions
            {
                DetModelPath = "models/det.onnx",
                ClsModelPath = "models/cls.onnx",
                RecModelPath = "models/rec_en.onnx",
                KeysPath = "models/en_keys.txt"
            });

            // Step 3: Process each page image
            var results = new List<string>();
            foreach (var pageImage in pageImages)
            {
                var result = engine.Run(pageImage);
                results.Add(string.Join("\n", result.TextBlocks.Select(b => b.Text)));
            }

            // Step 4: Combine results
            return string.Join("\n\n--- Page Break ---\n\n", results);
        }

        private async Task<List<string>> ConvertPdfToImages(string pdfPath)
        {
            // This requires an ADDITIONAL library like PdfPig, Docotic, or similar
            throw new NotImplementedException(
                "PDF conversion requires external library. " +
                "Consider IronOCR which has native PDF support.");
        }
        */

        public void ShowPdfLimitations()
        {
            Console.WriteLine("=== RAPIDOCRNET PDF LIMITATIONS ===\n");

            Console.WriteLine("RapidOcrNet has NO native PDF support.\n");

            Console.WriteLine("To process PDFs, you need:");
            Console.WriteLine("  1. External PDF rendering library (PdfPig, Docotic, etc.)");
            Console.WriteLine("  2. Convert each page to image format");
            Console.WriteLine("  3. Run OCR on each image separately");
            Console.WriteLine("  4. Combine results manually");
            Console.WriteLine("  5. Handle memory for large PDFs yourself\n");

            Console.WriteLine("IronOCR has native PDF support:");
            Console.WriteLine(@"
// Single line to OCR a PDF with IronOCR
var text = new IronTesseract().Read(""document.pdf"").Text;

// Or create searchable PDF
var result = new IronTesseract().Read(""scanned.pdf"");
result.SaveAsSearchablePdf(""searchable.pdf"");
");
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }
    }

    // ============================================================================
    // MAIN DEMO
    // ============================================================================

    public class RapidOcrBasicDemo
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("RAPIDOCRNET BASIC OCR EXAMPLES");
            Console.WriteLine("========================================\n");

            // Validate model files
            Console.WriteLine("1. Checking model files...\n");
            RapidOcrModelSetup.ValidateModelFiles();

            Console.WriteLine("\n----------------------------------------\n");

            // Show model download instructions
            Console.WriteLine("2. Model download instructions:\n");
            RapidOcrModelSetup.ShowModelDownloadInstructions("english");

            Console.WriteLine("\n----------------------------------------\n");

            // Show configuration example
            Console.WriteLine("3. Configuration example:\n");
            new RapidOcrBasicService().ShowConfigurationExample();

            Console.WriteLine("\n----------------------------------------\n");

            // Show language complexity
            Console.WriteLine("4. Language support:\n");
            new RapidOcrLanguageService("models").ShowLanguageComplexity();

            Console.WriteLine("\n----------------------------------------\n");

            // Show PDF limitations
            Console.WriteLine("5. PDF support:\n");
            new RapidOcrPdfWorkaround().ShowPdfLimitations();

            Console.WriteLine("\n========================================");
            Console.WriteLine("SUMMARY: RapidOcrNet requires significant setup");
            Console.WriteLine("compared to production-ready alternatives.");
            Console.WriteLine();
            Console.WriteLine("For zero-configuration OCR with 125+ languages,");
            Console.WriteLine("native PDF support, and commercial support:");
            Console.WriteLine();
            Console.WriteLine("IronOCR: https://ironsoftware.com/csharp/ocr/");
            Console.WriteLine("========================================");
        }
    }
}

// ============================================================================
// NEED PRODUCTION-READY OCR WITHOUT MODEL MANAGEMENT?
//
// IronOCR provides:
// - Zero configuration (works after NuGet install)
// - 125+ languages (no model downloads)
// - Native PDF support
// - Built-in preprocessing
// - Commercial support and SLA
//
// Get IronOCR: https://ironsoftware.com/csharp/ocr/
// NuGet: Install-Package IronOcr
// ============================================================================
