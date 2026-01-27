/**
 * LEADTOOLS OCR to IronOCR Migration Examples
 *
 * This file demonstrates complete migration patterns from LEADTOOLS OCR
 * to IronOCR, covering all common scenarios including engine lifecycle,
 * license handling, basic OCR, zone-based extraction, multi-page documents,
 * error handling, and searchable PDF creation.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * Migration Resources:
 * - Documentation: https://ironsoftware.com/csharp/ocr/docs/
 * - Tutorials: https://ironsoftware.com/csharp/ocr/tutorials/
 */

using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

// ============================================================================
// MIGRATION SCENARIO 1: Engine Initialization
// LEADTOOLS requires complex startup sequence; IronOCR is ready immediately
// ============================================================================

namespace MigrationScenario1_EngineInitialization
{
    // BEFORE: LEADTOOLS - Full initialization sequence required
    // Requires: Leadtools, Leadtools.Ocr, Leadtools.Codecs NuGet packages

    namespace LeadtoolsBefore
    {
        using Leadtools;
        using Leadtools.Ocr;
        using Leadtools.Codecs;

        public class LeadtoolsOcrEngine : IDisposable
        {
            private IOcrEngine _ocrEngine;
            private RasterCodecs _codecs;
            private bool _isInitialized = false;

            // Constructor must perform complete initialization chain
            public LeadtoolsOcrEngine(string licPath, string keyPath, string runtimePath)
            {
                // Step 1: License validation (fails silently on some errors)
                RasterSupport.SetLicense(licPath, File.ReadAllText(keyPath));

                // Step 2: Initialize image codecs (required for any image loading)
                _codecs = new RasterCodecs();

                // Step 3: Create OCR engine with specific type
                // OcrEngineType.LEAD, OcrEngineType.Tesseract, or OcrEngineType.OmniPage
                _ocrEngine = OcrEngineManager.CreateEngine(OcrEngineType.LEAD);

                // Step 4: Engine startup - loads runtime into memory (500-2000ms)
                _ocrEngine.Startup(
                    _codecs,
                    null,  // DocumentWriterInstance (optional)
                    null,  // SpellChecker (optional)
                    runtimePath  // Path to OCR runtime files
                );

                _isInitialized = true;
            }

            public bool IsReady => _isInitialized && _ocrEngine.IsStarted;

            // Must explicitly shutdown before dispose
            public void Dispose()
            {
                if (_ocrEngine != null)
                {
                    // Step 1: Shutdown engine first (releases runtime)
                    if (_ocrEngine.IsStarted)
                    {
                        _ocrEngine.Shutdown();
                    }

                    // Step 2: Then dispose
                    _ocrEngine.Dispose();
                    _ocrEngine = null;
                }

                // Step 3: Dispose codecs
                _codecs?.Dispose();
                _codecs = null;

                _isInitialized = false;
            }
        }
    }

    // AFTER: IronOCR - No initialization required
    // Requires: IronOcr NuGet package

    namespace IronOcrAfter
    {
        using IronOcr;

        public class IronOcrEngine
        {
            // No constructor setup needed
            // No fields to manage
            // No disposal pattern required for engine

            public bool IsReady => true; // Always ready

            public string Process(string imagePath)
            {
                // Ready to use immediately
                return new IronTesseract().Read(imagePath).Text;
            }

            // No Dispose needed - IronTesseract is lightweight and stateless
        }
    }
}


// ============================================================================
// MIGRATION SCENARIO 2: License Handling
// LEADTOOLS uses file-based LIC+KEY; IronOCR uses simple string key
// ============================================================================

namespace MigrationScenario2_LicenseHandling
{
    // BEFORE: LEADTOOLS - File-based licensing with two files

    namespace LeadtoolsBefore
    {
        using Leadtools;

        public class LeadtoolsLicenseManager
        {
            // Must manage two files: LIC and KEY
            public void InitializeLicense()
            {
                // Option 1: Absolute paths (deployment dependent)
                string licPath = @"C:\LEADTOOLS\License\LEADTOOLS.LIC";
                string keyPath = @"C:\LEADTOOLS\License\LEADTOOLS.LIC.KEY";

                // Option 2: Relative paths (working directory dependent)
                licPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LEADTOOLS.LIC");
                keyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LEADTOOLS.LIC.KEY");

                // Must read key file content (not path)
                string key = File.ReadAllText(keyPath);

                // Set license - silent failure on some errors
                RasterSupport.SetLicense(licPath, key);

                // Verify license is valid
                if (!RasterSupport.IsLocked(RasterSupportType.Document))
                {
                    throw new InvalidOperationException("Document module not licensed");
                }
            }

            // Deployment requires copying these files:
            // - LEADTOOLS.LIC
            // - LEADTOOLS.LIC.KEY
            // And ensuring paths resolve correctly in:
            // - Development (bin/Debug)
            // - Production server
            // - Docker containers
            // - Azure App Service
        }
    }

    // AFTER: IronOCR - String-based licensing

    namespace IronOcrAfter
    {
        using IronOcr;

        public class IronOcrLicenseManager
        {
            // Single string key - store anywhere
            public void InitializeLicense()
            {
                // Option 1: Direct key (development)
                IronOcr.License.LicenseKey = "IRONSUITE.YOUR-LICENSE-KEY";

                // Option 2: Environment variable (production recommended)
                IronOcr.License.LicenseKey =
                    Environment.GetEnvironmentVariable("IRONOCR_LICENSE");

                // Option 3: Configuration file
                // var key = Configuration["IronOcr:LicenseKey"];
                // IronOcr.License.LicenseKey = key;

                // Verify license
                bool isLicensed = IronOcr.License.IsLicensed;
            }

            // No files to deploy or manage
            // Key can be stored in:
            // - Environment variables (Docker, Azure, AWS)
            // - appsettings.json
            // - Azure Key Vault
            // - AWS Secrets Manager
        }
    }
}


// ============================================================================
// MIGRATION SCENARIO 3: Basic OCR Operation
// LEADTOOLS requires document/page abstraction; IronOCR is direct
// ============================================================================

namespace MigrationScenario3_BasicOcr
{
    // BEFORE: LEADTOOLS - Multiple steps for simple OCR

    namespace LeadtoolsBefore
    {
        using Leadtools;
        using Leadtools.Ocr;
        using Leadtools.Codecs;

        public class LeadtoolsOcr
        {
            private IOcrEngine _engine;
            private RasterCodecs _codecs;

            public LeadtoolsOcr(IOcrEngine engine, RasterCodecs codecs)
            {
                _engine = engine;
                _codecs = codecs;
            }

            public string ExtractText(string imagePath)
            {
                // Step 1: Load image using codecs
                using var image = _codecs.Load(imagePath);

                // Step 2: Create document container
                using var document = _engine.DocumentManager.CreateDocument();

                // Step 3: Add page to document
                var page = document.Pages.AddPage(image, null);

                // Step 4: Explicitly recognize
                page.Recognize(null);

                // Step 5: Extract text
                return page.GetText(-1);
            }
        }
    }

    // AFTER: IronOCR - Single line operation

    namespace IronOcrAfter
    {
        using IronOcr;

        public class IronOcrService
        {
            public string ExtractText(string imagePath)
            {
                // Everything in one line
                return new IronTesseract().Read(imagePath).Text;
            }

            // Or with reusable instance for batch processing
            private readonly IronTesseract _ocr = new IronTesseract();

            public string ExtractTextReusable(string imagePath)
            {
                return _ocr.Read(imagePath).Text;
            }
        }
    }
}


// ============================================================================
// MIGRATION SCENARIO 4: Zone-Based OCR
// LEADTOOLS uses OcrZone class; IronOCR uses CropRectangle
// ============================================================================

namespace MigrationScenario4_ZoneBasedOcr
{
    // BEFORE: LEADTOOLS - Zone configuration with OcrZone

    namespace LeadtoolsBefore
    {
        using Leadtools;
        using Leadtools.Ocr;
        using Leadtools.Codecs;

        public class LeadtoolsZoneOcr
        {
            private IOcrEngine _engine;
            private RasterCodecs _codecs;

            public string ExtractFromRegion(string imagePath, int x, int y, int width, int height)
            {
                using var image = _codecs.Load(imagePath);
                using var document = _engine.DocumentManager.CreateDocument();

                var page = document.Pages.AddPage(image, null);

                // Clear auto-detected zones
                page.Zones.Clear();

                // Create and configure zone
                var zone = new OcrZone
                {
                    Bounds = new LeadRect(x, y, width, height),
                    ZoneType = OcrZoneType.Text,
                    CharacterFilters = OcrZoneCharacterFilters.None,
                    RecognitionModule = OcrZoneRecognitionModule.Auto
                };

                page.Zones.Add(zone);
                page.Recognize(null);

                return page.GetText(-1);
            }

            // Multiple zones
            public Dictionary<string, string> ExtractMultipleRegions(
                string imagePath,
                Dictionary<string, Rectangle> regions)
            {
                var results = new Dictionary<string, string>();

                using var image = _codecs.Load(imagePath);
                using var document = _engine.DocumentManager.CreateDocument();

                var page = document.Pages.AddPage(image, null);
                page.Zones.Clear();

                foreach (var region in regions)
                {
                    var zone = new OcrZone
                    {
                        Bounds = new LeadRect(
                            region.Value.X,
                            region.Value.Y,
                            region.Value.Width,
                            region.Value.Height),
                        ZoneType = OcrZoneType.Text
                    };
                    page.Zones.Add(zone);
                }

                page.Recognize(null);

                // Extract text per zone (by index)
                for (int i = 0; i < regions.Count; i++)
                {
                    results[regions.Keys.ElementAt(i)] = page.Zones[i].Text;
                }

                return results;
            }
        }
    }

    // AFTER: IronOCR - CropRectangle approach

    namespace IronOcrAfter
    {
        using IronOcr;

        public class IronOcrZoneService
        {
            public string ExtractFromRegion(string imagePath, int x, int y, int width, int height)
            {
                var ocr = new IronTesseract();

                using var input = new OcrInput();
                input.LoadImage(imagePath, new Rectangle(x, y, width, height));

                return ocr.Read(input).Text;
            }

            // Alternative: CropRectangle for existing input
            public string ExtractFromRegionAlternative(string imagePath, Rectangle region)
            {
                var ocr = new IronTesseract();

                using var input = new OcrInput(imagePath);
                input.CropRectangle = new CropRectangle(
                    region.X, region.Y, region.Width, region.Height);

                return ocr.Read(input).Text;
            }

            // Multiple regions - process each separately
            public Dictionary<string, string> ExtractMultipleRegions(
                string imagePath,
                Dictionary<string, Rectangle> regions)
            {
                var ocr = new IronTesseract();
                var results = new Dictionary<string, string>();

                foreach (var region in regions)
                {
                    using var input = new OcrInput();
                    input.LoadImage(imagePath, region.Value);

                    results[region.Key] = ocr.Read(input).Text;
                }

                return results;
            }
        }
    }
}


// ============================================================================
// MIGRATION SCENARIO 5: Multi-Page Document Processing
// LEADTOOLS requires page-by-page iteration; IronOCR handles automatically
// ============================================================================

namespace MigrationScenario5_MultiPageDocument
{
    // BEFORE: LEADTOOLS - Manual page iteration with disposal

    namespace LeadtoolsBefore
    {
        using Leadtools;
        using Leadtools.Ocr;
        using Leadtools.Codecs;

        public class LeadtoolsMultiPageOcr
        {
            private IOcrEngine _engine;
            private RasterCodecs _codecs;

            public string ProcessMultiPageTiff(string tiffPath)
            {
                var result = new StringBuilder();

                // Get page count
                var info = _codecs.GetInformation(tiffPath, true);
                int pageCount = info.TotalPages;

                using var document = _engine.DocumentManager.CreateDocument();

                for (int pageNum = 1; pageNum <= pageCount; pageNum++)
                {
                    // Load specific page (must dispose each!)
                    using var pageImage = _codecs.Load(
                        tiffPath,
                        0,
                        CodecsLoadByteOrder.BgrOrGray,
                        pageNum,
                        pageNum);

                    var page = document.Pages.AddPage(pageImage, null);
                    page.Recognize(null);
                    result.AppendLine(page.GetText(-1));
                }

                return result.ToString();
            }

            public List<string> ProcessMultipleImages(string[] imagePaths)
            {
                var results = new List<string>();

                using var document = _engine.DocumentManager.CreateDocument();

                foreach (var path in imagePaths)
                {
                    // Each image must be loaded and disposed
                    using var image = _codecs.Load(path);

                    var page = document.Pages.AddPage(image, null);
                    page.Recognize(null);
                    results.Add(page.GetText(-1));
                }

                return results;
            }
        }
    }

    // AFTER: IronOCR - Automatic multi-page handling

    namespace IronOcrAfter
    {
        using IronOcr;

        public class IronOcrMultiPageService
        {
            public string ProcessMultiPageTiff(string tiffPath)
            {
                var ocr = new IronTesseract();

                using var input = new OcrInput();
                input.LoadImage(tiffPath); // Automatically loads all pages

                return ocr.Read(input).Text;
            }

            public List<string> ProcessMultipleImages(string[] imagePaths)
            {
                var ocr = new IronTesseract();
                var results = new List<string>();

                using var input = new OcrInput();
                foreach (var path in imagePaths)
                {
                    input.LoadImage(path);
                }

                var ocrResult = ocr.Read(input);

                // Access individual page results
                foreach (var page in ocrResult.Pages)
                {
                    results.Add(page.Text);
                }

                return results;
            }

            // Alternative: Process in parallel (thread-safe)
            public Dictionary<string, string> ProcessParallel(string[] imagePaths)
            {
                var ocr = new IronTesseract(); // Thread-safe, reusable
                var results = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

                Parallel.ForEach(imagePaths, path =>
                {
                    var text = ocr.Read(path).Text;
                    results[path] = text;
                });

                return results.ToDictionary(x => x.Key, x => x.Value);
            }
        }
    }
}


// ============================================================================
// MIGRATION SCENARIO 6: Engine Lifecycle (Startup/Shutdown)
// LEADTOOLS requires explicit lifecycle; IronOCR is automatic
// ============================================================================

namespace MigrationScenario6_EngineLifecycle
{
    // BEFORE: LEADTOOLS - Manual lifecycle management

    namespace LeadtoolsBefore
    {
        using Leadtools;
        using Leadtools.Ocr;
        using Leadtools.Codecs;

        public class LeadtoolsLifecycleService : IDisposable
        {
            private IOcrEngine _engine;
            private RasterCodecs _codecs;

            public void Initialize(string runtimePath)
            {
                _codecs = new RasterCodecs();
                _engine = OcrEngineManager.CreateEngine(OcrEngineType.LEAD);

                // Startup loads engine into memory (expensive operation)
                _engine.Startup(_codecs, null, null, runtimePath);
            }

            public bool IsStarted => _engine?.IsStarted ?? false;

            public string Process(string path)
            {
                if (!IsStarted)
                    throw new InvalidOperationException("Engine not started");

                using var image = _codecs.Load(path);
                using var doc = _engine.DocumentManager.CreateDocument();
                var page = doc.Pages.AddPage(image, null);
                page.Recognize(null);
                return page.GetText(-1);
            }

            // Can shutdown and restart (e.g., to change engine type)
            public void Restart(OcrEngineType newType)
            {
                if (_engine.IsStarted)
                {
                    _engine.Shutdown();
                }
                _engine.Dispose();

                _engine = OcrEngineManager.CreateEngine(newType);
                _engine.Startup(_codecs, null, null, _runtimePath);
            }

            public void Dispose()
            {
                // Order matters: shutdown before dispose
                if (_engine?.IsStarted == true)
                {
                    _engine.Shutdown();
                }
                _engine?.Dispose();
                _codecs?.Dispose();
            }
        }
    }

    // AFTER: IronOCR - No lifecycle to manage

    namespace IronOcrAfter
    {
        using IronOcr;

        public class IronOcrService
        {
            // No initialization needed
            // No shutdown needed
            // No dispose needed for engine

            public string Process(string path)
            {
                // Always ready to use
                return new IronTesseract().Read(path).Text;
            }

            // For performance, can reuse instance (thread-safe)
            private readonly IronTesseract _ocr = new IronTesseract();

            public string ProcessReusable(string path)
            {
                return _ocr.Read(path).Text;
            }

            // No restart concept needed - just create new instance if config changes
            public string ProcessWithDifferentConfig(string path)
            {
                var ocr = new IronTesseract();
                ocr.Language = OcrLanguage.German;
                return ocr.Read(path).Text;
            }
        }
    }
}


// ============================================================================
// MIGRATION SCENARIO 7: Error Handling
// LEADTOOLS uses status codes and exceptions; IronOCR uses standard exceptions
// ============================================================================

namespace MigrationScenario7_ErrorHandling
{
    // BEFORE: LEADTOOLS - Various error types

    namespace LeadtoolsBefore
    {
        using Leadtools;
        using Leadtools.Ocr;
        using Leadtools.Codecs;

        public class LeadtoolsErrorHandling
        {
            public string SafeExtract(string imagePath)
            {
                try
                {
                    // License errors
                    if (RasterSupport.IsLocked(RasterSupportType.Document))
                    {
                        throw new InvalidOperationException("OCR not licensed");
                    }

                    // Engine state errors
                    if (!_engine.IsStarted)
                    {
                        throw new InvalidOperationException("Engine not started");
                    }

                    using var image = _codecs.Load(imagePath);
                    using var doc = _engine.DocumentManager.CreateDocument();
                    var page = doc.Pages.AddPage(image, null);
                    page.Recognize(null);

                    // Check recognition status
                    var status = page.RecognizeStatus;
                    if (status != OcrPageRecognizeStatus.Done)
                    {
                        throw new Exception($"Recognition incomplete: {status}");
                    }

                    return page.GetText(-1);
                }
                catch (RasterException ex)
                {
                    // LEADTOOLS-specific exceptions
                    switch (ex.Code)
                    {
                        case RasterExceptionCode.FileNotFound:
                            throw new FileNotFoundException("Image not found", imagePath, ex);
                        case RasterExceptionCode.InvalidFormat:
                            throw new FormatException("Invalid image format", ex);
                        default:
                            throw new Exception($"LEADTOOLS error: {ex.Code}", ex);
                    }
                }
                catch (OcrException ex)
                {
                    // OCR-specific errors
                    throw new Exception($"OCR error: {ex.Message}", ex);
                }
            }
        }
    }

    // AFTER: IronOCR - Standard .NET exception patterns

    namespace IronOcrAfter
    {
        using IronOcr;

        public class IronOcrErrorHandling
        {
            public string SafeExtract(string imagePath)
            {
                try
                {
                    var ocr = new IronTesseract();
                    var result = ocr.Read(imagePath);

                    // Check confidence for quality
                    if (result.Confidence < 50)
                    {
                        Console.WriteLine($"Low confidence: {result.Confidence}%");
                    }

                    return result.Text;
                }
                catch (FileNotFoundException ex)
                {
                    // Standard .NET exception
                    throw new FileNotFoundException("Image not found", imagePath, ex);
                }
                catch (IronOcr.Exceptions.OcrException ex)
                {
                    // IronOCR-specific but inherits from standard exception
                    throw new Exception($"OCR failed: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    // General handling
                    throw new Exception($"Processing failed: {ex.Message}", ex);
                }
            }

            // Confidence-based validation
            public (string Text, double Confidence) ExtractWithValidation(string imagePath)
            {
                var result = new IronTesseract().Read(imagePath);

                if (result.Confidence < 30)
                {
                    throw new Exception("OCR quality too low for reliable extraction");
                }

                return (result.Text, result.Confidence);
            }
        }
    }
}


// ============================================================================
// MIGRATION SCENARIO 8: Searchable PDF Creation
// LEADTOOLS requires complex flow; IronOCR is single method call
// ============================================================================

namespace MigrationScenario8_SearchablePdf
{
    // BEFORE: LEADTOOLS - Multi-step PDF creation

    namespace LeadtoolsBefore
    {
        using Leadtools;
        using Leadtools.Ocr;
        using Leadtools.Codecs;
        using Leadtools.Document.Writer;

        public class LeadtoolsSearchablePdf
        {
            private IOcrEngine _engine;
            private RasterCodecs _codecs;

            public void CreateSearchablePdf(string imagePath, string outputPdfPath)
            {
                using var image = _codecs.Load(imagePath);
                using var document = _engine.DocumentManager.CreateDocument();

                var page = document.Pages.AddPage(image, null);
                page.Recognize(null);

                // Configure PDF output options
                var pdfOptions = new PdfDocumentOptions
                {
                    DocumentType = PdfDocumentType.Pdf,
                    ImageOverText = true, // Keep original image with hidden text layer
                    Linearized = false,
                    Title = "Searchable PDF"
                };

                // Apply options
                _engine.DocumentWriterInstance.SetOptions(
                    DocumentFormat.Pdf,
                    pdfOptions);

                // Save document
                document.Save(
                    outputPdfPath,
                    DocumentFormat.Pdf,
                    null);
            }

            public void CreateFromMultipleImages(string[] imagePaths, string outputPdfPath)
            {
                using var document = _engine.DocumentManager.CreateDocument();

                foreach (var path in imagePaths)
                {
                    using var image = _codecs.Load(path);
                    var page = document.Pages.AddPage(image, null);
                    page.Recognize(null);
                }

                var pdfOptions = new PdfDocumentOptions
                {
                    DocumentType = PdfDocumentType.PdfA,
                    ImageOverText = true
                };

                _engine.DocumentWriterInstance.SetOptions(DocumentFormat.Pdf, pdfOptions);
                document.Save(outputPdfPath, DocumentFormat.Pdf, null);
            }
        }
    }

    // AFTER: IronOCR - Single method call

    namespace IronOcrAfter
    {
        using IronOcr;

        public class IronOcrSearchablePdfService
        {
            public void CreateSearchablePdf(string imagePath, string outputPdfPath)
            {
                var result = new IronTesseract().Read(imagePath);
                result.SaveAsSearchablePdf(outputPdfPath);
            }

            public void CreateFromMultipleImages(string[] imagePaths, string outputPdfPath)
            {
                var ocr = new IronTesseract();

                using var input = new OcrInput();
                foreach (var path in imagePaths)
                {
                    input.LoadImage(path);
                }

                var result = ocr.Read(input);
                result.SaveAsSearchablePdf(outputPdfPath);
            }

            // Alternative: Get PDF bytes without saving to file
            public byte[] CreateSearchablePdfBytes(string imagePath)
            {
                var result = new IronTesseract().Read(imagePath);
                return result.SaveAsSearchablePdfBytes();
            }

            // PDF/A compliance for archival
            public void CreatePdfACompliant(string imagePath, string outputPath)
            {
                var ocr = new IronTesseract();
                var result = ocr.Read(imagePath);

                // SaveAsSearchablePdf creates PDF with text layer
                // For PDF/A specific compliance, use IronPdf integration
                result.SaveAsSearchablePdf(outputPath);
            }
        }
    }
}


// ============================================================================
// QUICK REFERENCE: API MAPPING
// ============================================================================

/*
 * LEADTOOLS → IronOCR API Mapping:
 *
 * | LEADTOOLS                           | IronOCR                      |
 * |-------------------------------------|------------------------------|
 * | RasterSupport.SetLicense()          | License.LicenseKey =         |
 * | RasterCodecs                        | OcrInput                     |
 * | OcrEngineManager.CreateEngine()     | new IronTesseract()          |
 * | engine.Startup()                    | (not needed)                 |
 * | engine.Shutdown()                   | (not needed)                 |
 * | _codecs.Load(path)                  | new OcrInput(path)           |
 * | DocumentManager.CreateDocument()    | (not needed)                 |
 * | document.Pages.AddPage()            | input.LoadImage()            |
 * | page.Recognize()                    | ocr.Read()                   |
 * | page.GetText()                      | result.Text                  |
 * | OcrZone                             | CropRectangle                |
 * | page.RecognizeStatus                | result.Confidence            |
 * | document.Save(path, Pdf)            | result.SaveAsSearchablePdf() |
 *
 * Key Differences:
 * 1. No engine lifecycle (Startup/Shutdown)
 * 2. No codec initialization
 * 3. No document container abstraction
 * 4. Single NuGet package
 * 5. String-based licensing
 * 6. Thread-safe by default
 */


// ============================================================================
// READY TO MIGRATE?
//
// Get IronOCR: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Documentation: https://ironsoftware.com/csharp/ocr/docs/
// Migration Guide: https://ironsoftware.com/csharp/ocr/tutorials/
// ============================================================================
