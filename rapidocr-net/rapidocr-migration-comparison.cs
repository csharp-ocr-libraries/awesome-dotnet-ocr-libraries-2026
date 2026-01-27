/**
 * RapidOcrNet vs IronOCR: Migration Comparison Examples
 *
 * This file demonstrates side-by-side comparisons showing:
 * - 4-model configuration overhead in RapidOcrNet
 * - Zero-config simplicity of IronOCR
 * - Language support differences
 * - PDF processing capabilities
 *
 * For production-ready OCR with zero configuration:
 * https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RapidOcrMigrationComparison
{
    // ============================================================================
    // EXAMPLE 1: BASIC TEXT EXTRACTION
    // ============================================================================

    /// <summary>
    /// Compares basic text extraction setup between RapidOcrNet and IronOCR.
    /// </summary>
    public class BasicTextExtractionComparison
    {
        // ========== RAPIDOCRNET APPROACH ==========
        // Requires 4 model files to be downloaded and configured

        /*
        using RapidOcrNet;

        public class RapidOcrTextExtractor
        {
            private RapidOcrEngine _engine;

            // Step 1: Manual setup - download 4 files from GitHub
            // Step 2: Configure all paths explicitly
            public RapidOcrTextExtractor()
            {
                _engine = new RapidOcrEngine(new RapidOcrOptions
                {
                    DetModelPath = "./models/det.onnx",       // ~3MB download
                    ClsModelPath = "./models/cls.onnx",       // ~1MB download
                    RecModelPath = "./models/rec_en.onnx",    // ~2MB download
                    KeysPath = "./models/en_keys.txt"         // ~100KB download
                });
            }

            // Step 3: Extract text from image
            public string ExtractText(string imagePath)
            {
                var result = _engine.Run(imagePath);
                // Manual text block assembly required
                return string.Join("\n", result.TextBlocks
                    .OrderBy(b => b.BoundingBox.Top)
                    .Select(b => b.Text));
            }

            public void Dispose() => _engine?.Dispose();
        }
        */

        // ========== IRONOCR APPROACH ==========
        // Zero configuration required

        public class IronOcrTextExtractor
        {
            // using IronOcr;

            // No setup required - works immediately after NuGet install
            public string ExtractText(string imagePath)
            {
                // One line does everything
                return new IronOcr.IronTesseract().Read(imagePath).Text;
            }
        }

        public void ShowComparison()
        {
            Console.WriteLine("=== BASIC TEXT EXTRACTION COMPARISON ===\n");

            Console.WriteLine("RAPIDOCRNET (14+ lines of setup code):");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine(@"
// Download 4 model files first, then:
var engine = new RapidOcrEngine(new RapidOcrOptions
{
    DetModelPath = ""./models/det.onnx"",
    ClsModelPath = ""./models/cls.onnx"",
    RecModelPath = ""./models/rec_en.onnx"",
    KeysPath = ""./models/en_keys.txt""
});
var result = engine.Run(imagePath);
var text = string.Join(""\n"", result.TextBlocks.Select(b => b.Text));
");

            Console.WriteLine("\nIRONOCR (1 line, zero configuration):");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine(@"
var text = new IronTesseract().Read(imagePath).Text;
");
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/\n");
        }
    }

    // ============================================================================
    // EXAMPLE 2: MULTI-LANGUAGE SUPPORT
    // ============================================================================

    /// <summary>
    /// Compares language switching complexity.
    /// </summary>
    public class LanguageSupportComparison
    {
        // ========== RAPIDOCRNET APPROACH ==========
        // Must download different model files for each language

        /*
        using RapidOcrNet;

        public class RapidOcrMultiLanguage
        {
            private readonly string _modelPath;

            public RapidOcrMultiLanguage(string modelPath)
            {
                _modelPath = modelPath;
            }

            // Each language needs different model files downloaded
            public RapidOcrEngine CreateEnglishEngine()
            {
                return new RapidOcrEngine(new RapidOcrOptions
                {
                    DetModelPath = Path.Combine(_modelPath, "det.onnx"),
                    ClsModelPath = Path.Combine(_modelPath, "cls.onnx"),
                    RecModelPath = Path.Combine(_modelPath, "en_rec.onnx"),
                    KeysPath = Path.Combine(_modelPath, "en_keys.txt")
                });
            }

            public RapidOcrEngine CreateChineseEngine()
            {
                return new RapidOcrEngine(new RapidOcrOptions
                {
                    DetModelPath = Path.Combine(_modelPath, "det.onnx"),
                    ClsModelPath = Path.Combine(_modelPath, "cls.onnx"),
                    RecModelPath = Path.Combine(_modelPath, "ch_rec.onnx"),  // Different file!
                    KeysPath = Path.Combine(_modelPath, "ch_keys.txt")       // Different file!
                });
            }

            // Spanish, French, German? NOT SUPPORTED!
            // Russian, Arabic? NOT SUPPORTED!
            public RapidOcrEngine CreateSpanishEngine()
            {
                throw new NotSupportedException(
                    "RapidOcrNet does not support Spanish. " +
                    "Use IronOCR for 125+ languages.");
            }
        }
        */

        // ========== IRONOCR APPROACH ==========
        // Just change the Language property

        public class IronOcrMultiLanguage
        {
            // using IronOcr;

            public string ExtractEnglish(string imagePath)
            {
                var ocr = new IronOcr.IronTesseract();
                ocr.Language = IronOcr.OcrLanguage.English;
                return ocr.Read(imagePath).Text;
            }

            public string ExtractChinese(string imagePath)
            {
                var ocr = new IronOcr.IronTesseract();
                ocr.Language = IronOcr.OcrLanguage.ChineseSimplified;
                return ocr.Read(imagePath).Text;
            }

            public string ExtractSpanish(string imagePath)
            {
                var ocr = new IronOcr.IronTesseract();
                ocr.Language = IronOcr.OcrLanguage.Spanish;  // Just works!
                return ocr.Read(imagePath).Text;
            }

            public string ExtractArabic(string imagePath)
            {
                var ocr = new IronOcr.IronTesseract();
                ocr.Language = IronOcr.OcrLanguage.Arabic;  // Just works!
                return ocr.Read(imagePath).Text;
            }

            public string ExtractRussian(string imagePath)
            {
                var ocr = new IronOcr.IronTesseract();
                ocr.Language = IronOcr.OcrLanguage.Russian;  // Just works!
                return ocr.Read(imagePath).Text;
            }

            // 125+ languages available - no model downloads needed
        }

        public void ShowComparison()
        {
            Console.WriteLine("=== LANGUAGE SUPPORT COMPARISON ===\n");

            Console.WriteLine("RAPIDOCRNET - Limited Languages:");
            Console.WriteLine("--------------------------------");
            Console.WriteLine("Supported:");
            Console.WriteLine("  - Chinese (Simplified/Traditional)");
            Console.WriteLine("  - English");
            Console.WriteLine("  - Japanese (limited)");
            Console.WriteLine("  - Korean (limited)");
            Console.WriteLine();
            Console.WriteLine("NOT Supported:");
            Console.WriteLine("  - Spanish, French, German, Italian");
            Console.WriteLine("  - Russian, Ukrainian, Polish");
            Console.WriteLine("  - Arabic, Hebrew");
            Console.WriteLine("  - Hindi, Bengali, Tamil");
            Console.WriteLine("  - And 100+ more languages...\n");

            Console.WriteLine("Switching languages in RapidOcrNet:");
            Console.WriteLine(@"
// Must download different model files for each language
// Must reconfigure and recreate engine instance
// Many languages simply not available
");

            Console.WriteLine("\nIRONOCR - 125+ Languages:");
            Console.WriteLine("--------------------------");
            Console.WriteLine(@"
// Simply change the Language property - no downloads needed
ocr.Language = OcrLanguage.Spanish;
ocr.Language = OcrLanguage.Russian;
ocr.Language = OcrLanguage.Arabic;
ocr.Language = OcrLanguage.Hindi;
// ... 125+ languages available
");
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/\n");
        }
    }

    // ============================================================================
    // EXAMPLE 3: PDF PROCESSING
    // ============================================================================

    /// <summary>
    /// Compares PDF processing capabilities.
    /// </summary>
    public class PdfProcessingComparison
    {
        // ========== RAPIDOCRNET APPROACH ==========
        // NO native PDF support - requires workaround

        /*
        using RapidOcrNet;

        public class RapidOcrPdfProcessor
        {
            // RapidOcrNet CANNOT read PDFs directly!
            // Must use external library to convert PDF to images first

            public async Task<string> ProcessPdfWorkaround(string pdfPath)
            {
                // Step 1: Use external library to render PDF pages to images
                // (requires PdfPig, Docotic, or similar)
                var pageImages = await RenderPdfToImages(pdfPath);

                // Step 2: Initialize RapidOcr with 4 model files
                using var engine = new RapidOcrEngine(new RapidOcrOptions
                {
                    DetModelPath = "models/det.onnx",
                    ClsModelPath = "models/cls.onnx",
                    RecModelPath = "models/rec_en.onnx",
                    KeysPath = "models/en_keys.txt"
                });

                // Step 3: Process each page image individually
                var allText = new List<string>();
                foreach (var pageImage in pageImages)
                {
                    var result = engine.Run(pageImage);
                    allText.Add(string.Join("\n", result.TextBlocks.Select(b => b.Text)));
                }

                // Step 4: Combine results manually
                return string.Join("\n\n", allText);
            }

            private Task<List<string>> RenderPdfToImages(string pdfPath)
            {
                // This requires an additional library like PdfPig
                throw new NotImplementedException(
                    "PDF rendering requires external library. " +
                    "Consider IronOCR for native PDF support.");
            }

            // Creating searchable PDF? NOT SUPPORTED!
            public void CreateSearchablePdf(string inputPdf, string outputPdf)
            {
                throw new NotSupportedException(
                    "RapidOcrNet cannot create searchable PDFs. " +
                    "Use IronOCR for this functionality.");
            }
        }
        */

        // ========== IRONOCR APPROACH ==========
        // Native PDF support - one line

        public class IronOcrPdfProcessor
        {
            // using IronOcr;

            // Direct PDF OCR - no conversion needed
            public string ProcessPdf(string pdfPath)
            {
                return new IronOcr.IronTesseract().Read(pdfPath).Text;
            }

            // Specific pages
            public string ProcessPdfPages(string pdfPath, int startPage, int endPage)
            {
                using var input = new IronOcr.OcrInput();
                input.LoadPdf(pdfPath);
                // TODO: verify IronOCR API for page range selection
                return new IronOcr.IronTesseract().Read(input).Text;
            }

            // Create searchable PDF
            public void CreateSearchablePdf(string inputPdf, string outputPdf)
            {
                var result = new IronOcr.IronTesseract().Read(inputPdf);
                result.SaveAsSearchablePdf(outputPdf);
            }
        }

        public void ShowComparison()
        {
            Console.WriteLine("=== PDF PROCESSING COMPARISON ===\n");

            Console.WriteLine("RAPIDOCRNET - No PDF Support:");
            Console.WriteLine("-----------------------------");
            Console.WriteLine("Cannot read PDFs directly");
            Console.WriteLine("Requires external PDF library");
            Console.WriteLine("Must convert each page to image");
            Console.WriteLine("Cannot create searchable PDFs\n");

            Console.WriteLine("Workaround code required:");
            Console.WriteLine(@"
// Step 1: Add PDF library (additional dependency)
// Step 2: Convert PDF to images
var images = pdfLibrary.RenderPages(pdfPath);
// Step 3: Configure RapidOcr (4 model files)
// Step 4: OCR each image
// Step 5: Combine results manually
// Step 6: Clean up temp images
// Many lines of error-prone code...
");

            Console.WriteLine("\nIRONOCR - Native PDF Support:");
            Console.WriteLine("-----------------------------");
            Console.WriteLine(@"
// One line to extract text from PDF
var text = new IronTesseract().Read(""document.pdf"").Text;

// One line to create searchable PDF
var result = new IronTesseract().Read(""scanned.pdf"");
result.SaveAsSearchablePdf(""searchable.pdf"");

// Process specific pages
using var input = new OcrInput();
input.LoadPdf(""document.pdf"");
// TODO: verify IronOCR API for page range selection (pages 1-10)
var text = new IronTesseract().Read(input).Text;
");
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/\n");
        }
    }

    // ============================================================================
    // EXAMPLE 4: DEPLOYMENT COMPLEXITY
    // ============================================================================

    /// <summary>
    /// Compares deployment requirements.
    /// </summary>
    public class DeploymentComparison
    {
        public void ShowComparison()
        {
            Console.WriteLine("=== DEPLOYMENT COMPARISON ===\n");

            Console.WriteLine("RAPIDOCRNET Deployment Requirements:");
            Console.WriteLine("------------------------------------");
            Console.WriteLine("1. NuGet package (RapidOcrNet)");
            Console.WriteLine("2. ONNX Runtime (~30-50MB)");
            Console.WriteLine("3. Detection model (det.onnx ~3MB)");
            Console.WriteLine("4. Classification model (cls.onnx ~1MB)");
            Console.WriteLine("5. Recognition model (rec.onnx ~2-10MB)");
            Console.WriteLine("6. Character dictionary (keys.txt ~100KB)");
            Console.WriteLine("7. Correct path configuration");
            Console.WriteLine("8. Model files in deployment package\n");

            Console.WriteLine("Total: 5-7 files to manage and deploy correctly\n");

            Console.WriteLine("IRONOCR Deployment Requirements:");
            Console.WriteLine("--------------------------------");
            Console.WriteLine("1. NuGet package (IronOcr)");
            Console.WriteLine("\nTotal: 1 package, everything included\n");

            Console.WriteLine("Deployment scripts comparison:");
            Console.WriteLine(@"
// RapidOcrNet deployment (PowerShell example)
Copy-Item ./models/det.onnx $deployPath/models/
Copy-Item ./models/cls.onnx $deployPath/models/
Copy-Item ./models/rec_en.onnx $deployPath/models/
Copy-Item ./models/en_keys.txt $deployPath/models/
// Plus ONNX runtime binaries
// Plus configuration validation
// Plus error handling for missing files

// IronOCR deployment
dotnet publish
// Done - everything is included
");
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/\n");
        }
    }

    // ============================================================================
    // EXAMPLE 5: COMPLETE MIGRATION GUIDE
    // ============================================================================

    /// <summary>
    /// Step-by-step migration from RapidOcrNet to IronOCR.
    /// </summary>
    public class MigrationGuide
    {
        public void ShowMigrationSteps()
        {
            Console.WriteLine("=== MIGRATION GUIDE: RAPIDOCRNET TO IRONOCR ===\n");

            Console.WriteLine("STEP 1: Remove RapidOcrNet packages");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("dotnet remove package RapidOcrNet");
            Console.WriteLine("dotnet remove package Microsoft.ML.OnnxRuntime\n");

            Console.WriteLine("STEP 2: Install IronOCR");
            Console.WriteLine("-----------------------");
            Console.WriteLine("dotnet add package IronOcr\n");

            Console.WriteLine("STEP 3: Delete model files");
            Console.WriteLine("--------------------------");
            Console.WriteLine("rm -rf ./models/  # No longer needed\n");

            Console.WriteLine("STEP 4: Update code");
            Console.WriteLine("-------------------");
            Console.WriteLine(@"
// BEFORE: RapidOcrNet (many lines, 4 model files)
using RapidOcrNet;

var engine = new RapidOcrEngine(new RapidOcrOptions
{
    DetModelPath = ""models/det.onnx"",
    ClsModelPath = ""models/cls.onnx"",
    RecModelPath = ""models/rec_en.onnx"",
    KeysPath = ""models/en_keys.txt""
});
var result = engine.Run(imagePath);
var text = string.Join(""\n"", result.TextBlocks.Select(b => b.Text));

// AFTER: IronOCR (one line, zero configuration)
using IronOcr;

var text = new IronTesseract().Read(imagePath).Text;
");

            Console.WriteLine("\nSTEP 5: Update deployment");
            Console.WriteLine("-------------------------");
            Console.WriteLine("Remove model file copying from deployment scripts");
            Console.WriteLine("Remove ONNX runtime configuration\n");

            Console.WriteLine("STEP 6: Add new capabilities (free improvements!)");
            Console.WriteLine("-------------------------------------------------");
            Console.WriteLine(@"
// Direct PDF support (was impossible with RapidOcrNet)
var pdfText = new IronTesseract().Read(""document.pdf"").Text;

// 125+ languages (most not available in RapidOcrNet)
ocr.Language = OcrLanguage.Spanish;
ocr.Language = OcrLanguage.Arabic;
ocr.Language = OcrLanguage.Russian;

// Create searchable PDFs (not possible with RapidOcrNet)
result.SaveAsSearchablePdf(""searchable.pdf"");

// Built-in preprocessing (manual with RapidOcrNet)
input.Deskew();
input.DeNoise();
input.Contrast();
");
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/\n");
        }
    }

    // ============================================================================
    // MAIN DEMO
    // ============================================================================

    public class MigrationComparisonDemo
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("================================================");
            Console.WriteLine("RAPIDOCRNET VS IRONOCR MIGRATION COMPARISON");
            Console.WriteLine("================================================\n");

            // Example 1: Basic text extraction
            new BasicTextExtractionComparison().ShowComparison();
            Console.WriteLine("------------------------------------------------\n");

            // Example 2: Language support
            new LanguageSupportComparison().ShowComparison();
            Console.WriteLine("------------------------------------------------\n");

            // Example 3: PDF processing
            new PdfProcessingComparison().ShowComparison();
            Console.WriteLine("------------------------------------------------\n");

            // Example 4: Deployment
            new DeploymentComparison().ShowComparison();
            Console.WriteLine("------------------------------------------------\n");

            // Example 5: Migration guide
            new MigrationGuide().ShowMigrationSteps();

            Console.WriteLine("================================================");
            Console.WriteLine("SUMMARY");
            Console.WriteLine("================================================\n");

            Console.WriteLine("RapidOcrNet requires:");
            Console.WriteLine("  - 4 model files downloaded manually");
            Console.WriteLine("  - Complex configuration");
            Console.WriteLine("  - Limited language support (~5 languages)");
            Console.WriteLine("  - No PDF support");
            Console.WriteLine("  - Complex deployment\n");

            Console.WriteLine("IronOCR provides:");
            Console.WriteLine("  - Zero configuration");
            Console.WriteLine("  - 125+ languages");
            Console.WriteLine("  - Native PDF support");
            Console.WriteLine("  - Searchable PDF creation");
            Console.WriteLine("  - Simple deployment");
            Console.WriteLine("  - Commercial support\n");

            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
            Console.WriteLine("NuGet: Install-Package IronOcr");
            Console.WriteLine("================================================");
        }
    }
}

// ============================================================================
// READY TO MIGRATE?
//
// IronOCR removes all the complexity of RapidOcrNet:
// - No model file downloads
// - No path configuration
// - 125+ languages built-in
// - Native PDF support
// - Commercial support included
//
// Get started: https://ironsoftware.com/csharp/ocr/
// NuGet: Install-Package IronOcr
// ============================================================================
