// =============================================================================
// Kofax OmniPage to IronOCR Migration Comparison
// =============================================================================
//
// This file provides side-by-side code examples showing how to migrate from
// Kofax OmniPage SDK patterns to IronOCR. Each section demonstrates a common
// OCR task with both approaches.
//
// Migration Benefits:
// - Eliminate enterprise sales process
// - Remove license file management
// - Simplify deployment (NuGet package)
// - Reduce code complexity
// - Lower total cost of ownership
//
// Note: OmniPage patterns are illustrative based on enterprise SDK documentation.
// Actual API may vary by version.
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

// =============================================================================
// MIGRATION 1: Basic Document OCR
// =============================================================================

namespace Migration1_BasicOcr
{
    /// <summary>
    /// Kofax OmniPage approach to basic document OCR.
    /// Requires engine lifecycle management and license configuration.
    /// </summary>
    public class KofaxBasicOcr
    {
        public string ExtractText(string imagePath)
        {
            // OmniPage Pattern: Engine lifecycle required
            // -------------------------------------------
            // Step 1: Initialize engine with license
            // Step 2: Create document container
            // Step 3: Add image to document
            // Step 4: Configure recognition settings
            // Step 5: Execute recognition
            // Step 6: Extract text
            // Step 7: Dispose document
            // Step 8: Shutdown engine

            string result = "";

            // Pseudo-code for OmniPage SDK pattern
            /*
            using (var engine = new OmniPageEngine())
            {
                // License file must exist and be valid
                engine.SetLicenseFile(@"C:\Program Files\OmniPage\license.lic");
                engine.Initialize();

                // Configure recognition parameters
                var settings = new RecognitionSettings();
                settings.Language = "English";
                settings.OutputFormat = OutputFormat.Text;
                settings.AccuracyMode = AccuracyMode.Normal;

                // Create document and add page
                var document = engine.CreateDocument();
                document.AddPage(imagePath);

                // Execute recognition
                document.Recognize(settings);

                // Extract text
                result = document.GetText();

                // Explicit cleanup required
                document.Dispose();
                engine.Shutdown();
            }
            */

            return result;
        }
    }

    /// <summary>
    /// IronOCR approach - dramatically simpler.
    /// </summary>
    public class IronOcrBasicOcr
    {
        public string ExtractText(string imagePath)
        {
            // IronOCR: No engine lifecycle, no license files

            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);

            var result = new IronOcr.IronTesseract().Read(input);
            return result.Text;

            // That's it. Three lines of actual code.
        }
    }
}

// =============================================================================
// MIGRATION 2: PDF Document Processing
// =============================================================================

namespace Migration2_PdfProcessing
{
    /// <summary>
    /// Kofax OmniPage PDF processing pattern.
    /// PDF support often requires additional modules or licensing.
    /// </summary>
    public class KofaxPdfProcessing
    {
        public string ExtractTextFromPdf(string pdfPath)
        {
            // OmniPage PDF Pattern:
            // - May require separate PDF module license
            // - Page-by-page processing common
            // - Resource management per page

            string allText = "";

            /*
            using (var engine = new OmniPageEngine())
            {
                engine.SetLicenseFile(licensePath);
                engine.Initialize();

                // Open PDF document
                var pdfDocument = engine.OpenPDF(pdfPath);

                // Process each page
                for (int i = 0; i < pdfDocument.PageCount; i++)
                {
                    var page = pdfDocument.GetPage(i);

                    var settings = new RecognitionSettings();
                    settings.Language = "English";

                    page.Recognize(settings);
                    allText += page.GetText() + "\n";

                    page.Dispose();
                }

                pdfDocument.Dispose();
                engine.Shutdown();
            }
            */

            return allText;
        }

        public void ConvertPdfToSearchable(string inputPdf, string outputPdf)
        {
            // OmniPage searchable PDF creation
            // Often requires specific output format configuration

            /*
            using (var engine = new OmniPageEngine())
            {
                engine.SetLicenseFile(licensePath);
                engine.Initialize();

                var pdfDocument = engine.OpenPDF(inputPdf);

                var outputSettings = new OutputSettings();
                outputSettings.Format = OutputFormat.SearchablePDF;
                outputSettings.Compression = PDFCompression.High;
                outputSettings.ImageQuality = 85;

                pdfDocument.RecognizeAll(recognitionSettings);
                pdfDocument.SaveAs(outputPdf, outputSettings);

                pdfDocument.Dispose();
                engine.Shutdown();
            }
            */
        }
    }

    /// <summary>
    /// IronOCR PDF processing - native support, no additional modules.
    /// </summary>
    public class IronOcrPdfProcessing
    {
        public string ExtractTextFromPdf(string pdfPath)
        {
            // IronOCR: PDF support built-in, no extra license

            using var input = new IronOcr.OcrInput();
            input.LoadPdf(pdfPath);  // All pages loaded automatically

            var result = new IronOcr.IronTesseract().Read(input);
            return result.Text;  // All pages combined
        }

        public void ConvertPdfToSearchable(string inputPdf, string outputPdf)
        {
            // IronOCR searchable PDF creation

            using var input = new IronOcr.OcrInput();
            input.LoadPdf(inputPdf);

            var ocr = new IronOcr.IronTesseract();
            var result = ocr.Read(input);

            // Save as searchable PDF with text layer
            result.SaveAsSearchablePdf(outputPdf);
        }

        public void ProcessSpecificPages(string pdfPath, int startPage, int endPage)
        {
            // IronOCR: Selective page processing

            using var input = new IronOcr.OcrInput();
            input.LoadPdfPages(pdfPath, startPage, endPage);

            var result = new IronOcr.IronTesseract().Read(input);
            Console.WriteLine(result.Text);
        }
    }
}

// =============================================================================
// MIGRATION 3: Multi-Language OCR
// =============================================================================

namespace Migration3_MultiLanguage
{
    /// <summary>
    /// Kofax OmniPage multi-language configuration.
    /// </summary>
    public class KofaxMultiLanguage
    {
        public string ProcessMultiLanguageDocument(string imagePath)
        {
            // OmniPage multi-language pattern:
            // - Primary and secondary language configuration
            // - Dictionary loading per language
            // - May require additional language packs

            string result = "";

            /*
            using (var engine = new OmniPageEngine())
            {
                engine.SetLicenseFile(licensePath);
                engine.Initialize();

                // Load language dictionaries
                engine.LoadLanguageDictionary("English");
                engine.LoadLanguageDictionary("German");
                engine.LoadLanguageDictionary("French");

                var settings = new RecognitionSettings();
                settings.PrimaryLanguage = "English";
                settings.SecondaryLanguages = new[] { "German", "French" };
                settings.AutoDetectLanguage = true;

                var document = engine.CreateDocument();
                document.AddPage(imagePath);
                document.Recognize(settings);

                result = document.GetText();

                document.Dispose();
                engine.Shutdown();
            }
            */

            return result;
        }
    }

    /// <summary>
    /// IronOCR multi-language - language packs downloaded on demand.
    /// </summary>
    public class IronOcrMultiLanguage
    {
        public string ProcessMultiLanguageDocument(string imagePath)
        {
            // IronOCR: Multiple languages combined with + operator

            var ocr = new IronOcr.IronTesseract();

            // Combine languages - packs downloaded automatically if needed
            ocr.Language = IronOcr.OcrLanguage.English +
                          IronOcr.OcrLanguage.German +
                          IronOcr.OcrLanguage.French;

            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);

            var result = ocr.Read(input);
            return result.Text;
        }

        public string ProcessWithAutoDetect(string imagePath)
        {
            // IronOCR can process with language detection

            var ocr = new IronOcr.IronTesseract();

            // Use multiple languages for auto-detection
            ocr.Language = IronOcr.OcrLanguage.English +
                          IronOcr.OcrLanguage.German +
                          IronOcr.OcrLanguage.French +
                          IronOcr.OcrLanguage.Spanish;

            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);

            var result = ocr.Read(input);
            return result.Text;
        }
    }
}

// =============================================================================
// MIGRATION 4: Batch Document Processing
// =============================================================================

namespace Migration4_BatchProcessing
{
    /// <summary>
    /// Kofax OmniPage batch processing - enterprise workflow pattern.
    /// </summary>
    public class KofaxBatchProcessing
    {
        public Dictionary<string, string> ProcessBatch(string[] imagePaths)
        {
            // OmniPage batch pattern:
            // - Single engine instance for batch
            // - Per-document resource management
            // - Progress callbacks common
            // - Error handling per document

            var results = new Dictionary<string, string>();

            /*
            using (var engine = new OmniPageEngine())
            {
                engine.SetLicenseFile(licensePath);
                engine.Initialize();

                var settings = new RecognitionSettings();
                settings.Language = "English";

                foreach (var imagePath in imagePaths)
                {
                    try
                    {
                        var document = engine.CreateDocument();
                        document.AddPage(imagePath);
                        document.Recognize(settings);

                        results[imagePath] = document.GetText();

                        document.Dispose();
                    }
                    catch (RecognitionException ex)
                    {
                        results[imagePath] = $"Error: {ex.Message}";
                    }
                }

                engine.Shutdown();
            }
            */

            return results;
        }
    }

    /// <summary>
    /// IronOCR batch processing - parallel processing supported.
    /// </summary>
    public class IronOcrBatchProcessing
    {
        public Dictionary<string, string> ProcessBatch(string[] imagePaths)
        {
            // IronOCR: Simple batch processing

            var results = new Dictionary<string, string>();
            var ocr = new IronOcr.IronTesseract();

            foreach (var imagePath in imagePaths)
            {
                try
                {
                    using var input = new IronOcr.OcrInput();
                    input.LoadImage(imagePath);

                    var result = ocr.Read(input);
                    results[imagePath] = result.Text;
                }
                catch (Exception ex)
                {
                    results[imagePath] = $"Error: {ex.Message}";
                }
            }

            return results;
        }

        public async Task<Dictionary<string, string>> ProcessBatchParallelAsync(string[] imagePaths)
        {
            // IronOCR supports parallel processing

            var results = new Dictionary<string, string>();
            var tasks = new List<Task>();

            foreach (var imagePath in imagePaths)
            {
                var path = imagePath; // Capture for closure
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        using var input = new IronOcr.OcrInput();
                        input.LoadImage(path);

                        var result = new IronOcr.IronTesseract().Read(input);

                        lock (results)
                        {
                            results[path] = result.Text;
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (results)
                        {
                            results[path] = $"Error: {ex.Message}";
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);
            return results;
        }
    }
}

// =============================================================================
// MIGRATION 5: Image Preprocessing
// =============================================================================

namespace Migration5_Preprocessing
{
    /// <summary>
    /// Kofax OmniPage image preprocessing configuration.
    /// </summary>
    public class KofaxPreprocessing
    {
        public string ProcessWithPreprocessing(string imagePath)
        {
            // OmniPage preprocessing:
            // - Explicit preprocessing configuration
            // - Multiple preprocessing stages

            string result = "";

            /*
            using (var engine = new OmniPageEngine())
            {
                engine.SetLicenseFile(licensePath);
                engine.Initialize();

                // Configure preprocessing
                var preprocessSettings = new PreprocessingSettings();
                preprocessSettings.AutoRotate = true;
                preprocessSettings.Deskew = true;
                preprocessSettings.DespeckleLevel = 2;
                preprocessSettings.ContrastEnhancement = true;
                preprocessSettings.NoiseReduction = true;
                preprocessSettings.BorderRemoval = true;

                var recognitionSettings = new RecognitionSettings();
                recognitionSettings.Language = "English";
                recognitionSettings.Preprocessing = preprocessSettings;

                var document = engine.CreateDocument();
                document.AddPage(imagePath);
                document.Recognize(recognitionSettings);

                result = document.GetText();

                document.Dispose();
                engine.Shutdown();
            }
            */

            return result;
        }
    }

    /// <summary>
    /// IronOCR preprocessing - automatic and configurable.
    /// </summary>
    public class IronOcrPreprocessing
    {
        public string ProcessWithAutoPreprocessing(string imagePath)
        {
            // IronOCR: Automatic preprocessing by default

            var ocr = new IronOcr.IronTesseract();

            // Preprocessing happens automatically
            // Rotation, deskew, noise reduction all included

            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);

            var result = ocr.Read(input);
            return result.Text;
        }

        public string ProcessWithExplicitPreprocessing(string imagePath)
        {
            // IronOCR: Explicit preprocessing control when needed

            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);

            // Apply specific preprocessing filters
            input.Deskew();              // Straighten rotated images
            input.DeNoise();             // Remove visual noise
            input.Contrast();            // Enhance contrast
            input.Binarize();            // Convert to black and white
            input.Dilate();              // Thicken text
            input.Erode();               // Thin text

            var result = new IronOcr.IronTesseract().Read(input);
            return result.Text;
        }

        public string ProcessLowQualityDocument(string imagePath)
        {
            // IronOCR: Handling degraded documents

            using var input = new IronOcr.OcrInput();
            input.LoadImage(imagePath);

            // Aggressive preprocessing for poor quality scans
            input.DeNoise();
            input.Sharpen();
            input.Contrast();
            input.Deskew();

            // Use higher DPI for better accuracy on poor scans
            input.TargetDPI = 300;

            var ocr = new IronOcr.IronTesseract();
            ocr.Configuration.TesseractVersion = IronOcr.TesseractVersion.Tesseract5;

            var result = ocr.Read(input);
            return result.Text;
        }
    }
}

// =============================================================================
// MIGRATION 6: License and Deployment
// =============================================================================

namespace Migration6_Deployment
{
    /// <summary>
    /// Kofax OmniPage license management patterns.
    /// </summary>
    public static class KofaxLicenseManagement
    {
        // OmniPage license management complexity:
        //
        // 1. License File Deployment
        //    - License.lic file must be deployed to each server
        //    - File path must be accessible to application
        //    - File permissions must allow read access
        //
        // 2. Network/Floating License
        //    - License server installation required
        //    - Firewall configuration for license server ports
        //    - License checkout/checkin at runtime
        //    - Monitoring for license availability
        //
        // 3. Per-Page Licensing
        //    - Page counting mechanism
        //    - Usage reporting to license server
        //    - Billing reconciliation
        //
        // 4. Activation
        //    - Hardware fingerprinting
        //    - Online activation required
        //    - Reactivation on hardware changes
        //
        // 5. Maintenance
        //    - Annual maintenance fees
        //    - Version updates tied to maintenance
        //    - Support access tied to maintenance

        public static void InitializeWithLicense(string licensePath)
        {
            /*
            // Check license file exists
            if (!File.Exists(licensePath))
                throw new LicenseException("License file not found");

            // Check license file permissions
            try
            {
                using var stream = File.OpenRead(licensePath);
            }
            catch (UnauthorizedAccessException)
            {
                throw new LicenseException("Cannot read license file - check permissions");
            }

            // Initialize engine with license
            var engine = new OmniPageEngine();
            engine.SetLicenseFile(licensePath);

            try
            {
                engine.Initialize(); // May contact license server
            }
            catch (LicenseValidationException ex)
            {
                // Handle: expired, invalid, concurrent limit, etc.
                throw new LicenseException($"License validation failed: {ex.Message}");
            }
            */
        }
    }

    /// <summary>
    /// IronOCR license management - simple key-based system.
    /// </summary>
    public static class IronOcrLicenseManagement
    {
        public static void ConfigureLicense()
        {
            // IronOCR: Simple license key
            // No files to deploy, no servers to configure

            // Option 1: Direct assignment
            IronOcr.License.LicenseKey = "IRONSOFTWARE-LICENSE-KEY";

            // Option 2: Environment variable (recommended)
            // Set IRONOCR_LICENSE_KEY in environment
            // IronOCR reads automatically - no code changes needed

            // Option 3: Configuration file
            // Add to appsettings.json:
            // { "IronOcr": { "LicenseKey": "YOUR-KEY" } }

            // Benefits:
            // - No license files to deploy
            // - No license server to maintain
            // - No firewall configuration
            // - No hardware fingerprinting
            // - No annual maintenance required (perpetual license)
            // - Works offline
        }

        public static void ValidateLicenseOptionally()
        {
            // Check license status (optional)
            bool isLicensed = IronOcr.License.IsLicensed;
            Console.WriteLine($"IronOCR Licensed: {isLicensed}");

            // IronOCR works in trial mode without license
            // Trial: Watermarks applied, but full functionality available
            // Licensed: No watermarks, production ready
        }
    }
}

// =============================================================================
// MIGRATION 7: Complete Migration Example
// =============================================================================

namespace Migration7_CompleteExample
{
    /// <summary>
    /// Complete before/after migration example showing a realistic
    /// document processing service migration from OmniPage to IronOCR.
    /// </summary>
    public class DocumentOcrServiceComparison
    {
        // ===================================================================
        // BEFORE: Kofax OmniPage Service (Conceptual)
        // ===================================================================
        /*
        public class KofaxDocumentService : IDisposable
        {
            private OmniPageEngine _engine;
            private bool _initialized = false;

            public KofaxDocumentService(string licensePath)
            {
                _engine = new OmniPageEngine();
                _engine.SetLicenseFile(licensePath);
                _engine.Initialize();
                _initialized = true;
            }

            public string ProcessDocument(string imagePath)
            {
                if (!_initialized)
                    throw new InvalidOperationException("Engine not initialized");

                var settings = new RecognitionSettings();
                settings.Language = "English";
                settings.AccuracyMode = AccuracyMode.Normal;

                var document = _engine.CreateDocument();
                try
                {
                    document.AddPage(imagePath);
                    document.Recognize(settings);
                    return document.GetText();
                }
                finally
                {
                    document.Dispose();
                }
            }

            public void Dispose()
            {
                if (_initialized)
                {
                    _engine.Shutdown();
                    _initialized = false;
                }
            }
        }
        */

        // ===================================================================
        // AFTER: IronOCR Service
        // ===================================================================

        public class IronOcrDocumentService
        {
            private readonly IronOcr.IronTesseract _ocr;

            public IronOcrDocumentService()
            {
                // No license file path needed
                // No initialization required
                _ocr = new IronOcr.IronTesseract();
                _ocr.Language = IronOcr.OcrLanguage.English;
            }

            public string ProcessDocument(string imagePath)
            {
                using var input = new IronOcr.OcrInput();
                input.LoadImage(imagePath);

                var result = _ocr.Read(input);
                return result.Text;
            }

            public string ProcessPdf(string pdfPath)
            {
                using var input = new IronOcr.OcrInput();
                input.LoadPdf(pdfPath);

                var result = _ocr.Read(input);
                return result.Text;
            }

            // No Dispose needed - no unmanaged resources
        }
    }

    /// <summary>
    /// Migration checklist - steps to migrate from OmniPage to IronOCR.
    /// </summary>
    public static class MigrationChecklist
    {
        public static void PrintMigrationSteps()
        {
            Console.WriteLine("=== Kofax OmniPage to IronOCR Migration Checklist ===");
            Console.WriteLine();
            Console.WriteLine("1. PREPARATION");
            Console.WriteLine("   [ ] Document current OmniPage usage patterns");
            Console.WriteLine("   [ ] Identify all OmniPage DLL references");
            Console.WriteLine("   [ ] List license file locations");
            Console.WriteLine("   [ ] Inventory language packs in use");
            Console.WriteLine();
            Console.WriteLine("2. DEPENDENCY CHANGES");
            Console.WriteLine("   [ ] Remove OmniPage SDK DLL references");
            Console.WriteLine("   [ ] Uninstall OmniPage runtime components (optional)");
            Console.WriteLine("   [ ] Add NuGet package: dotnet add package IronOcr");
            Console.WriteLine();
            Console.WriteLine("3. CODE MIGRATION");
            Console.WriteLine("   [ ] Replace using statements");
            Console.WriteLine("   [ ] Remove engine initialization code");
            Console.WriteLine("   [ ] Remove engine shutdown code");
            Console.WriteLine("   [ ] Simplify document processing methods");
            Console.WriteLine("   [ ] Update PDF processing (if applicable)");
            Console.WriteLine("   [ ] Migrate language configuration");
            Console.WriteLine();
            Console.WriteLine("4. LICENSE MIGRATION");
            Console.WriteLine("   [ ] Remove OmniPage license files from deployment");
            Console.WriteLine("   [ ] Add IronOCR license key to configuration");
            Console.WriteLine("   [ ] Remove license server configuration (if applicable)");
            Console.WriteLine();
            Console.WriteLine("5. TESTING");
            Console.WriteLine("   [ ] Test basic OCR functionality");
            Console.WriteLine("   [ ] Test PDF processing");
            Console.WriteLine("   [ ] Test multi-language support");
            Console.WriteLine("   [ ] Verify accuracy meets requirements");
            Console.WriteLine("   [ ] Performance benchmark comparison");
            Console.WriteLine();
            Console.WriteLine("6. DEPLOYMENT");
            Console.WriteLine("   [ ] Deploy updated application");
            Console.WriteLine("   [ ] Verify license key is configured");
            Console.WriteLine("   [ ] Remove OmniPage from servers (optional)");
            Console.WriteLine("   [ ] Monitor for any issues");
        }
    }
}
