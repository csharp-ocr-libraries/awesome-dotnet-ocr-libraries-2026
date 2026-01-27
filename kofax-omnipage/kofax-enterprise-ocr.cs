// =============================================================================
// Kofax OmniPage Enterprise SDK Usage Patterns
// =============================================================================
//
// This file demonstrates typical Kofax OmniPage SDK patterns for enterprise
// document processing. OmniPage is an enterprise-grade OCR suite requiring:
// - Sales contact and procurement process
// - Custom SDK installation (no NuGet)
// - License file configuration
// - Engine lifecycle management
//
// The IronOCR alternative is shown for comparison - demonstrating simpler
// patterns that achieve equivalent results with significantly less overhead.
//
// Note: OmniPage API patterns are illustrative based on enterprise SDK
// documentation patterns. Actual API may vary by version.
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;

// =============================================================================
// PART 1: Kofax OmniPage Enterprise Patterns (Conceptual)
// =============================================================================

namespace KofaxOmniPagePatterns
{
    /// <summary>
    /// Demonstrates Kofax OmniPage enterprise SDK initialization patterns.
    /// Enterprise SDKs typically require explicit lifecycle management.
    /// </summary>
    public class OmniPageEngineManager : IDisposable
    {
        private bool _isInitialized = false;
        private string _licensePath;

        // Enterprise SDK pattern: Explicit initialization required
        // - License file must be deployed to production servers
        // - Network license server validation common
        // - Activation keys tied to hardware/domains

        public void Initialize(string licensePath)
        {
            if (_isInitialized)
                throw new InvalidOperationException("Engine already initialized");

            _licensePath = licensePath;

            // OmniPage pattern: Validate license before any operations
            // This typically involves:
            // 1. Reading license file from disk
            // 2. Validating against license server (network call)
            // 3. Checking hardware fingerprint
            // 4. Verifying expiration date

            ValidateLicense(licensePath);

            // Initialize native components
            // OmniPage loads multiple DLLs for different recognition engines
            InitializeNativeComponents();

            _isInitialized = true;
        }

        private void ValidateLicense(string path)
        {
            // Enterprise license validation
            // In production, this contacts license server
            if (!File.Exists(path))
                throw new FileNotFoundException("License file not found", path);

            Console.WriteLine($"License validated from: {path}");
        }

        private void InitializeNativeComponents()
        {
            // Enterprise SDKs often have complex native dependencies
            // OmniPage requires multiple runtime components:
            // - OCR engine DLLs
            // - ICR handwriting recognition modules
            // - OMR mark recognition modules
            // - Language dictionaries (100MB+)
            // - Neural network models

            Console.WriteLine("Initializing OmniPage native components...");
            Console.WriteLine("Loading OCR engine...");
            Console.WriteLine("Loading ICR module...");
            Console.WriteLine("Loading language dictionaries...");
        }

        public void Shutdown()
        {
            if (!_isInitialized)
                return;

            // Enterprise pattern: Explicit cleanup required
            // Failure to call Shutdown can:
            // - Leave license locked
            // - Cause memory leaks
            // - Prevent other processes from using license

            Console.WriteLine("Releasing OmniPage resources...");
            Console.WriteLine("Releasing license...");

            _isInitialized = false;
        }

        public void Dispose()
        {
            Shutdown();
        }
    }

    /// <summary>
    /// Enterprise document processing with OmniPage patterns.
    /// Demonstrates the complexity of enterprise OCR workflows.
    /// </summary>
    public class EnterpriseDocumentProcessor
    {
        private readonly OmniPageEngineManager _engine;

        public EnterpriseDocumentProcessor(OmniPageEngineManager engine)
        {
            _engine = engine;
        }

        /// <summary>
        /// Process a document with enterprise-grade settings.
        /// OmniPage supports extensive configuration options.
        /// </summary>
        public DocumentResult ProcessDocument(string imagePath, ProcessingOptions options)
        {
            // OmniPage enterprise pattern: Document-centric workflow
            // 1. Create document container
            // 2. Add pages (images)
            // 3. Configure recognition settings per page or document
            // 4. Execute recognition
            // 5. Extract results
            // 6. Dispose document resources

            Console.WriteLine($"Creating document from: {imagePath}");

            // Enterprise SDKs often have verbose configuration
            var settings = new RecognitionSettings
            {
                // Language configuration
                PrimaryLanguage = options.Language,
                SecondaryLanguages = options.AdditionalLanguages,

                // Recognition mode
                AccuracyMode = options.HighAccuracy ? "Maximum" : "Standard",
                SpeedPriority = options.FastMode ? "Speed" : "Accuracy",

                // Output format options
                PreserveLayout = options.PreserveFormatting,
                DetectTables = options.RecognizeTables,
                DetectColumns = true,

                // Advanced OCR settings
                DespeckleLevel = 2,
                ContrastEnhancement = true,
                AutoRotate = true,
                DeskewImage = true
            };

            // Execute recognition
            var result = ExecuteRecognition(imagePath, settings);

            return result;
        }

        private DocumentResult ExecuteRecognition(string imagePath, RecognitionSettings settings)
        {
            // Simulated recognition result
            return new DocumentResult
            {
                Text = "Extracted text would appear here",
                Confidence = 98.5,
                PageCount = 1,
                ProcessingTimeMs = 1500
            };
        }

        /// <summary>
        /// Process forms with handwriting (ICR) - OmniPage specialty.
        /// ICR is a key differentiator for enterprise OCR solutions.
        /// </summary>
        public FormResult ProcessHandwrittenForm(string imagePath, FormTemplate template)
        {
            // OmniPage ICR (Intelligent Character Recognition)
            // This is where enterprise solutions excel:
            // - Handwritten field recognition
            // - Form template matching
            // - Checkbox/bubble detection (OMR)
            // - Constrained character sets (numbers only, dates)

            Console.WriteLine("Processing form with ICR...");
            Console.WriteLine($"Applying template: {template.Name}");

            // Define extraction zones based on template
            var fields = new Dictionary<string, string>();

            foreach (var zone in template.Zones)
            {
                // OmniPage zone extraction
                // Each zone can have different settings:
                // - Recognition type (OCR, ICR, OMR, Barcode)
                // - Character constraints
                // - Validation rules

                Console.WriteLine($"Extracting zone: {zone.Name} ({zone.Type})");
                fields[zone.Name] = $"Extracted value for {zone.Name}";
            }

            return new FormResult
            {
                Fields = fields,
                ConfidenceScores = new Dictionary<string, double>()
            };
        }
    }

    // Supporting classes for OmniPage patterns

    public class ProcessingOptions
    {
        public string Language { get; set; } = "English";
        public string[] AdditionalLanguages { get; set; }
        public bool HighAccuracy { get; set; } = true;
        public bool FastMode { get; set; } = false;
        public bool PreserveFormatting { get; set; } = true;
        public bool RecognizeTables { get; set; } = true;
    }

    public class RecognitionSettings
    {
        public string PrimaryLanguage { get; set; }
        public string[] SecondaryLanguages { get; set; }
        public string AccuracyMode { get; set; }
        public string SpeedPriority { get; set; }
        public bool PreserveLayout { get; set; }
        public bool DetectTables { get; set; }
        public bool DetectColumns { get; set; }
        public int DespeckleLevel { get; set; }
        public bool ContrastEnhancement { get; set; }
        public bool AutoRotate { get; set; }
        public bool DeskewImage { get; set; }
    }

    public class DocumentResult
    {
        public string Text { get; set; }
        public double Confidence { get; set; }
        public int PageCount { get; set; }
        public long ProcessingTimeMs { get; set; }
    }

    public class FormTemplate
    {
        public string Name { get; set; }
        public List<FormZone> Zones { get; set; } = new List<FormZone>();
    }

    public class FormZone
    {
        public string Name { get; set; }
        public string Type { get; set; } // "OCR", "ICR", "OMR", "Barcode"
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class FormResult
    {
        public Dictionary<string, string> Fields { get; set; }
        public Dictionary<string, double> ConfidenceScores { get; set; }
    }
}

// =============================================================================
// PART 2: IronOCR Alternative - Simpler Approach
// =============================================================================

namespace IronOcrAlternative
{
    using IronOcr;

    /// <summary>
    /// IronOCR equivalent functionality with dramatically simpler API.
    /// No license files, no engine lifecycle, no native component management.
    /// </summary>
    public class SimpleDocumentProcessor
    {
        /// <summary>
        /// Process a document - complete implementation in minimal code.
        /// </summary>
        public string ProcessDocument(string imagePath)
        {
            // IronOCR: No engine initialization required
            // No license files to deploy
            // No shutdown procedures

            var ocr = new IronTesseract();

            // Multi-language support built-in
            ocr.Language = OcrLanguage.English;

            // Preprocessing happens automatically
            // Auto-rotation, deskew, contrast enhancement included

            using var input = new OcrInput();
            input.LoadImage(imagePath);

            var result = ocr.Read(input);
            return result.Text;
        }

        /// <summary>
        /// Process PDF documents - native support, no separate modules.
        /// </summary>
        public string ProcessPdf(string pdfPath)
        {
            // IronOCR handles PDFs natively
            // No additional PDF modules required
            // No separate licensing for PDF processing

            using var input = new OcrInput();
            input.LoadPdf(pdfPath);

            var result = new IronTesseract().Read(input);
            return result.Text;
        }

        /// <summary>
        /// MRZ (Machine Readable Zone) processing - built-in.
        /// Passport/ID processing without separate MRZ module.
        /// </summary>
        public string ProcessMrz(string passportImagePath)
        {
            // IronOCR includes MRZ recognition
            // No separate license for travel document processing

            using var input = new OcrInput();
            input.LoadImage(passportImagePath);

            var ocr = new IronTesseract();
            var result = ocr.Read(input);

            // MRZ data extraction
            // IronOCR can extract the two-line MRZ codes
            return result.Text;
        }

        /// <summary>
        /// Barcode/QR code reading - included at no extra cost.
        /// OmniPage requires separate barcode modules.
        /// </summary>
        public string[] ReadBarcodes(string imagePath)
        {
            // IronOCR includes barcode scanning
            // No add-on licensing required

            using var input = new OcrInput();
            input.LoadImage(imagePath);

            var result = new IronTesseract().Read(input);

            // Access barcodes detected during OCR
            var barcodes = new List<string>();
            foreach (var barcode in result.Barcodes)
            {
                barcodes.Add(barcode.Value);
            }

            return barcodes.ToArray();
        }
    }

    /// <summary>
    /// IronOCR licensing - simple key-based system.
    /// </summary>
    public static class LicenseSetup
    {
        public static void Configure()
        {
            // IronOCR licensing options:

            // Option 1: Direct key assignment
            IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

            // Option 2: Environment variable (recommended for production)
            // Set IRONOCR_LICENSE_KEY environment variable
            // IronOCR reads it automatically

            // Option 3: App.config or Web.config
            // <add key="IronOcr.License.LicenseKey" value="YOUR-KEY"/>

            // No license files to deploy
            // No hardware fingerprinting
            // No license server connections
            // No annual validation requirements
        }
    }
}

// =============================================================================
// PART 3: Enterprise Integration Comparison
// =============================================================================

namespace EnterpriseComparison
{
    /// <summary>
    /// Side-by-side comparison of enterprise integration patterns.
    /// </summary>
    public static class IntegrationComparison
    {
        /// <summary>
        /// OmniPage enterprise deployment checklist.
        /// </summary>
        public static string[] OmniPageDeploymentSteps => new[]
        {
            "1. Complete sales process (4-12 weeks)",
            "2. Receive SDK installer from Tungsten",
            "3. Install SDK on development machines",
            "4. Configure license files (per-machine or floating)",
            "5. Set up license server (if using network licensing)",
            "6. Install runtime components on production servers",
            "7. Deploy license files to production",
            "8. Configure firewall for license server communication",
            "9. Set up monitoring for license availability",
            "10. Document shutdown procedures for license release"
        };

        /// <summary>
        /// IronOCR deployment checklist.
        /// </summary>
        public static string[] IronOcrDeploymentSteps => new[]
        {
            "1. Install NuGet package: dotnet add package IronOcr",
            "2. Add license key to configuration",
            "3. Deploy application"
        };
    }
}
