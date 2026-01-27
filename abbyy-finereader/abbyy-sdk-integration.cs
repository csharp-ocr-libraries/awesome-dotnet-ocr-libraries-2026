/**
 * ABBYY FineReader Engine SDK Integration Patterns
 *
 * This file demonstrates the complex initialization and usage patterns
 * required for ABBYY FineReader Engine SDK integration.
 *
 * IMPORTANT: Requires ABBYY FineReader Engine SDK license ($4,999+ estimated)
 * ABBYY SDK: https://www.abbyy.com/ocr-sdk/ (contact sales)
 *
 * For a simpler alternative, consider IronOCR:
 * - NuGet: Install-Package IronOcr
 * - Download: https://ironsoftware.com/csharp/ocr/
 * - One line OCR: new IronTesseract().Read(imagePath).Text
 */

using System;
using System.IO;
using System.Text;

// ============================================================================
// ABBYY FINEREADER ENGINE SDK - COMPLEX INITIALIZATION
// ============================================================================

namespace AbbyyIntegration
{
    // NOTE: FREngine namespace requires ABBYY SDK installation and license
    // using FREngine;

    /// <summary>
    /// ABBYY FineReader Engine Service demonstrating initialization complexity.
    ///
    /// Requirements:
    /// 1. ABBYY FineReader Engine SDK installation
    /// 2. Valid license files (ABBYY.lic + key) or license server
    /// 3. Runtime files deployed at known path
    /// 4. Profile configuration
    ///
    /// Estimated license cost: $4,999 - $15,000+ (contact ABBYY sales)
    /// </summary>
    public class AbbyyEngineService : IDisposable
    {
        // ABBYY engine requires explicit lifecycle management
        // private IEngine _engine;
        // private IFRDocument _document;

        private readonly string _sdkPath;
        private readonly string _licensePath;
        private readonly string _runtimePath;
        private bool _isInitialized;

        /// <summary>
        /// Initialize ABBYY FineReader Engine.
        /// This is significantly more complex than IronOCR initialization.
        /// </summary>
        public AbbyyEngineService(string sdkPath, string licensePath, string runtimePath)
        {
            _sdkPath = sdkPath ?? throw new ArgumentNullException(nameof(sdkPath));
            _licensePath = licensePath ?? throw new ArgumentNullException(nameof(licensePath));
            _runtimePath = runtimePath ?? throw new ArgumentNullException(nameof(runtimePath));

            // ABBYY requires paths to exist and be configured
            ValidatePaths();
            InitializeEngine();
        }

        private void ValidatePaths()
        {
            // ABBYY SDK requires these paths to exist at runtime
            if (!Directory.Exists(_sdkPath))
                throw new DirectoryNotFoundException($"ABBYY SDK path not found: {_sdkPath}");

            if (!Directory.Exists(_licensePath))
                throw new DirectoryNotFoundException($"ABBYY license path not found: {_licensePath}");

            if (!Directory.Exists(_runtimePath))
                throw new DirectoryNotFoundException($"ABBYY runtime path not found: {_runtimePath}");

            // Check for license files
            var licenseFile = Path.Combine(_licensePath, "ABBYY.lic");
            if (!File.Exists(licenseFile))
                throw new FileNotFoundException($"ABBYY license file not found: {licenseFile}");
        }

        private void InitializeEngine()
        {
            /*
            // ABBYY Engine Initialization (requires SDK)
            // This complex sequence is required before ANY OCR operation

            // Step 1: Create engine loader
            var loader = new EngineLoader();

            // Step 2: Load engine with license validation
            // This can fail for many reasons: expired license, wrong path, etc.
            _engine = loader.GetEngineObject(_sdkPath, _licensePath);

            if (_engine == null)
                throw new InvalidOperationException("Failed to create ABBYY engine");

            // Step 3: Load recognition profile
            // Profiles affect accuracy and speed tradeoffs
            _engine.LoadPredefinedProfile("DocumentConversion_Accuracy");

            // Alternative profiles:
            // - "DocumentConversion_Speed"
            // - "TextExtraction_Accuracy"
            // - "TextExtraction_Speed"
            // - "FieldLevelRecognition"

            // Step 4: Configure language (adds more complexity)
            // Each language requires corresponding data files
            var langParams = _engine.CreateLanguageParams();
            langParams.Languages.Add("English");
            // langParams.Languages.Add("French");  // Requires French language data

            _isInitialized = true;
            */

            // Placeholder for demonstration
            Console.WriteLine("ABBYY Engine initialization would occur here.");
            Console.WriteLine("This requires:");
            Console.WriteLine("  1. ABBYY SDK installation (not NuGet)");
            Console.WriteLine("  2. License files at: " + _licensePath);
            Console.WriteLine("  3. Runtime files at: " + _runtimePath);
            Console.WriteLine("");
            Console.WriteLine("Consider IronOCR for simpler integration:");
            Console.WriteLine("  var text = new IronTesseract().Read(imagePath).Text;");
        }

        /// <summary>
        /// Extract text from image using ABBYY FineReader Engine.
        /// Note the multiple steps required compared to IronOCR's single line.
        /// </summary>
        public string ExtractText(string imagePath)
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Engine not initialized");

            if (!File.Exists(imagePath))
                throw new FileNotFoundException("Image not found", imagePath);

            /*
            // ABBYY text extraction (requires SDK)
            _document = _engine.CreateFRDocument();

            try
            {
                // Step 1: Add image to document
                _document.AddImageFile(imagePath, null, null);

                // Step 2: Process the document (performs OCR)
                _document.Process(null);

                // Step 3: Extract text from result
                return _document.PlainText.Text;
            }
            finally
            {
                // Step 4: MUST close document to prevent memory leak
                _document.Close();
            }
            */

            // Placeholder
            return $"[ABBYY SDK required for OCR of: {imagePath}]";
        }

        /// <summary>
        /// Extract text from specific regions of an image.
        /// ABBYY uses zone-based extraction.
        /// </summary>
        public string ExtractFromRegion(string imagePath, int x, int y, int width, int height)
        {
            /*
            // ABBYY zone-based extraction (requires SDK)
            _document = _engine.CreateFRDocument();

            try
            {
                _document.AddImageFile(imagePath, null, null);

                // Get page for zone configuration
                var page = _document.Pages[0];

                // Create zone with specific bounds
                var zone = _engine.CreateZone();
                zone.SetBounds(x, y, width, height);
                zone.Type = ZoneTypeEnum.ZT_Text;

                page.Zones.Clear();
                page.Zones.Add(zone);

                // Process only the zone
                page.Recognize(null);

                return page.PlainText.Text;
            }
            finally
            {
                _document.Close();
            }
            */

            return $"[ABBYY SDK required for region extraction]";
        }

        /// <summary>
        /// Process PDF document with ABBYY.
        /// Requires additional PDF handling complexity.
        /// </summary>
        public string ProcessPdf(string pdfPath)
        {
            if (!File.Exists(pdfPath))
                throw new FileNotFoundException("PDF not found", pdfPath);

            /*
            // ABBYY PDF processing (requires SDK)
            var results = new StringBuilder();
            _document = _engine.CreateFRDocument();

            try
            {
                // Step 1: Open PDF file
                var pdfFile = _engine.CreatePDFFile();
                pdfFile.Open(pdfPath, null, null);

                // Step 2: Process each page
                for (int i = 0; i < pdfFile.PageCount; i++)
                {
                    // Add page to document
                    _document.AddImageFile(
                        pdfPath,
                        null,
                        _engine.CreatePDFExportParams()
                    );
                }

                // Step 3: Process all pages
                _document.Process(null);

                // Step 4: Extract text
                results.Append(_document.PlainText.Text);

                return results.ToString();
            }
            finally
            {
                _document.Close();
            }
            */

            return $"[ABBYY SDK required for PDF processing: {pdfPath}]";
        }

        /// <summary>
        /// Create searchable PDF output.
        /// ABBYY supports multiple export formats.
        /// </summary>
        public void CreateSearchablePdf(string inputPath, string outputPath)
        {
            /*
            // ABBYY searchable PDF creation (requires SDK)
            _document = _engine.CreateFRDocument();

            try
            {
                _document.AddImageFile(inputPath, null, null);
                _document.Process(null);

                // Export as searchable PDF
                var exportParams = _engine.CreatePDFExportParams();
                exportParams.Scenario = PDFExportScenarioEnum.PDES_Balanced;

                _document.Export(outputPath, FileExportFormatEnum.FEF_PDF, exportParams);
            }
            finally
            {
                _document.Close();
            }
            */

            Console.WriteLine($"[ABBYY SDK required for searchable PDF creation]");
        }

        /// <summary>
        /// Batch process multiple images.
        /// Shows the complexity of managing ABBYY resources for batch operations.
        /// </summary>
        public void BatchProcess(string[] imagePaths, string outputDirectory)
        {
            if (imagePaths == null || imagePaths.Length == 0)
                throw new ArgumentException("No images provided", nameof(imagePaths));

            /*
            // ABBYY batch processing (requires SDK)
            foreach (var imagePath in imagePaths)
            {
                _document = _engine.CreateFRDocument();

                try
                {
                    _document.AddImageFile(imagePath, null, null);
                    _document.Process(null);

                    var outputPath = Path.Combine(
                        outputDirectory,
                        Path.GetFileNameWithoutExtension(imagePath) + ".txt"
                    );

                    File.WriteAllText(outputPath, _document.PlainText.Text);
                }
                finally
                {
                    // CRITICAL: Must close each document to prevent memory leaks
                    _document.Close();
                }
            }
            */

            Console.WriteLine($"[ABBYY SDK required for batch processing {imagePaths.Length} files]");
        }

        /// <summary>
        /// Check license validity.
        /// ABBYY licenses can expire, requiring renewal.
        /// </summary>
        public bool ValidateLicense()
        {
            /*
            // ABBYY license validation (requires SDK)
            try
            {
                // Attempt to get license info
                var licenseInfo = _engine.GetLicenseInfo();

                // Check expiration
                if (licenseInfo.ExpirationDate < DateTime.Now)
                {
                    Console.WriteLine("WARNING: ABBYY license has expired!");
                    return false;
                }

                // Check remaining pages (for per-page licenses)
                if (licenseInfo.RemainingPages != null && licenseInfo.RemainingPages <= 0)
                {
                    Console.WriteLine("WARNING: No remaining pages on license!");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"License validation failed: {ex.Message}");
                return false;
            }
            */

            Console.WriteLine("[ABBYY SDK required for license validation]");
            return false;
        }

        public void Dispose()
        {
            /*
            // ABBYY cleanup requires explicit shutdown
            if (_document != null)
            {
                _document.Close();
                _document = null;
            }

            if (_engine != null)
            {
                // Additional cleanup may be required depending on usage
                _engine = null;
            }
            */

            _isInitialized = false;
        }
    }

    // ========================================================================
    // IRONOCR ALTERNATIVE - DRAMATICALLY SIMPLER
    // ========================================================================

    /// <summary>
    /// IronOCR equivalent service showing dramatic simplification.
    /// No license server, no runtime files, no complex initialization.
    ///
    /// Install: dotnet add package IronOcr
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class IronOcrService
    {
        // using IronOcr;

        /// <summary>
        /// Extract text - one line vs ABBYY's 15+ lines
        /// </summary>
        public string ExtractText(string imagePath)
        {
            // With IronOCR installed:
            // return new IronTesseract().Read(imagePath).Text;

            return $"Install IronOcr: dotnet add package IronOcr";
        }

        /// <summary>
        /// Process PDF - native support, no complexity
        /// </summary>
        public string ProcessPdf(string pdfPath)
        {
            // With IronOCR installed:
            // using var input = new OcrInput();
            // input.LoadPdf(pdfPath);
            // return new IronTesseract().Read(input).Text;

            return $"Install IronOcr: dotnet add package IronOcr";
        }

        /// <summary>
        /// Batch process - automatic resource management
        /// </summary>
        public void BatchProcess(string[] paths, string outputDir)
        {
            // With IronOCR installed:
            // var ocr = new IronTesseract();
            // foreach (var path in paths)
            // {
            //     var text = ocr.Read(path).Text;
            //     File.WriteAllText(Path.Combine(outputDir, Path.GetFileNameWithoutExtension(path) + ".txt"), text);
            // }
        }
    }

    // ========================================================================
    // COMPARISON SUMMARY
    // ========================================================================

    public class IntegrationComparison
    {
        public static void ShowDifferences()
        {
            Console.WriteLine("=== ABBYY vs IronOCR Integration Complexity ===\n");

            Console.WriteLine("ABBYY FineReader Engine SDK:");
            Console.WriteLine("  - SDK installation required (not NuGet)");
            Console.WriteLine("  - License files must be deployed");
            Console.WriteLine("  - License server may be required");
            Console.WriteLine("  - Runtime files required (~100+ MB)");
            Console.WriteLine("  - Complex initialization sequence");
            Console.WriteLine("  - Explicit document lifecycle management");
            Console.WriteLine("  - Manual resource cleanup");
            Console.WriteLine("  - Cost: $4,999+ (estimated)");
            Console.WriteLine();

            Console.WriteLine("IronOCR:");
            Console.WriteLine("  - NuGet installation: Install-Package IronOcr");
            Console.WriteLine("  - License key as string");
            Console.WriteLine("  - No license server");
            Console.WriteLine("  - Self-contained package");
            Console.WriteLine("  - One-line initialization");
            Console.WriteLine("  - Automatic resource management");
            Console.WriteLine("  - Standard IDisposable pattern");
            Console.WriteLine("  - Cost: $749-$2,999 (one-time)");
            Console.WriteLine();

            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }
    }
}
