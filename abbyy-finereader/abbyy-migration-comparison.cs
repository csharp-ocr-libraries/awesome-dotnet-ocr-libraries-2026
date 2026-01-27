/**
 * ABBYY FineReader Engine to IronOCR Migration Examples
 *
 * Side-by-side before/after examples for migrating from ABBYY
 * FineReader Engine SDK to IronOCR.
 *
 * ABBYY FineReader Engine: Enterprise SDK ($4,999+ estimated)
 * - Contact ABBYY sales: https://www.abbyy.com/ocr-sdk/
 *
 * IronOCR: Modern .NET OCR library ($749-$2,999 one-time)
 * - NuGet: Install-Package IronOcr
 * - Download: https://ironsoftware.com/csharp/ocr/
 *
 * Migration Complexity: Medium (4-8 hours typical)
 * Key simplifications: Licensing, initialization, resource management
 */

using System;
using System.IO;
using System.Text;

namespace AbbyyMigration
{
    // ========================================================================
    // MIGRATION EXAMPLE 1: Engine Initialization
    // ========================================================================
    // ABBYY requires complex multi-step initialization
    // IronOCR requires no initialization

    namespace BeforeMigration
    {
        // ABBYY FineReader Engine initialization
        // Requires: ABBYY SDK installation + license files ($4,999+)

        /*
        using FREngine;

        public class AbbyyService : IDisposable
        {
            private IEngine _engine;
            private bool _isStarted;

            public AbbyyService()
            {
                // Step 1: Create engine loader
                var loader = new EngineLoader();

                // Step 2: Get engine with license paths
                // License path must contain valid ABBYY.lic and key files
                _engine = loader.GetEngineObject(
                    @"C:\Program Files\ABBYY SDK\FineReader Engine\Bin",
                    @"C:\Program Files\ABBYY SDK\License"
                );

                // Step 3: Load recognition profile
                _engine.LoadPredefinedProfile("DocumentConversion_Accuracy");

                // Step 4: Configure languages (optional but common)
                var langParams = _engine.CreateLanguageParams();
                langParams.Languages.Add("English");

                _isStarted = true;
            }

            public void Dispose()
            {
                // Explicit cleanup required
                _engine = null;
                _isStarted = false;
            }
        }
        */
    }

    namespace AfterMigration_Initialization
    {
        // IronOCR requires no special initialization
        // using IronOcr;

        public class OcrService
        {
            // No initialization code needed
            // No license files to manage
            // No engine lifecycle

            public OcrService()
            {
                // Optionally set license key (one line)
                // IronOcr.License.LicenseKey = "YOUR-KEY";

                // That's it - ready to use
            }
        }
    }

    // ========================================================================
    // MIGRATION EXAMPLE 2: Basic Text Extraction
    // ========================================================================
    // ABBYY: 15+ lines with explicit document handling
    // IronOCR: 1 line

    namespace BeforeMigration_TextExtraction
    {
        /*
        using FREngine;

        public class AbbyyTextExtractor
        {
            private IEngine _engine;

            public string ExtractText(string imagePath)
            {
                // Create document container
                var document = _engine.CreateFRDocument();

                try
                {
                    // Add image to document
                    document.AddImageFile(imagePath, null, null);

                    // Process the document (performs OCR)
                    document.Process(null);

                    // Extract text from processed document
                    return document.PlainText.Text;
                }
                finally
                {
                    // CRITICAL: Must close to prevent memory leak
                    document.Close();
                }
            }
        }
        */
    }

    namespace AfterMigration_TextExtraction
    {
        // using IronOcr;

        public class IronOcrTextExtractor
        {
            public string ExtractText(string imagePath)
            {
                // One line handles everything:
                // - Image loading
                // - OCR processing
                // - Text extraction
                // - Resource cleanup

                // return new IronTesseract().Read(imagePath).Text;

                return $"Install IronOcr: dotnet add package IronOcr";
            }
        }
    }

    // ========================================================================
    // MIGRATION EXAMPLE 3: PDF Processing
    // ========================================================================
    // ABBYY: Complex page-by-page handling
    // IronOCR: Native PDF support

    namespace BeforeMigration_PdfProcessing
    {
        /*
        using FREngine;

        public class AbbyyPdfProcessor
        {
            private IEngine _engine;

            public string ProcessPdf(string pdfPath)
            {
                var document = _engine.CreateFRDocument();
                var results = new StringBuilder();

                try
                {
                    // Open PDF file
                    var pdfFile = _engine.CreatePDFFile();
                    pdfFile.Open(pdfPath, null, null);

                    // Process each page individually
                    for (int i = 0; i < pdfFile.PageCount; i++)
                    {
                        // Add page to document
                        document.AddImageFile(
                            pdfPath,
                            null,
                            _engine.CreatePDFExportParams()
                        );
                    }

                    // Process all pages
                    document.Process(null);

                    // Extract text
                    results.Append(document.PlainText.Text);

                    return results.ToString();
                }
                finally
                {
                    document.Close();
                }
            }
        }
        */
    }

    namespace AfterMigration_PdfProcessing
    {
        // using IronOcr;

        public class IronOcrPdfProcessor
        {
            public string ProcessPdf(string pdfPath)
            {
                // IronOCR handles PDF natively
                // No page-by-page loops needed

                // using var input = new OcrInput();
                // input.LoadPdf(pdfPath);
                // return new IronTesseract().Read(input).Text;

                return $"Install IronOcr: dotnet add package IronOcr";
            }

            public string ProcessPdfWithPasswordProtection(string pdfPath, string password)
            {
                // IronOCR supports password-protected PDFs directly

                // using var input = new OcrInput();
                // input.LoadPdf(pdfPath, Password: password);
                // return new IronTesseract().Read(input).Text;

                return $"Password-protected PDF support built-in";
            }
        }
    }

    // ========================================================================
    // MIGRATION EXAMPLE 4: License Management
    // ========================================================================
    // ABBYY: File-based licensing with potential server requirements
    // IronOCR: Simple string-based licensing

    namespace BeforeMigration_Licensing
    {
        /*
        public class AbbyyLicenseManager
        {
            // ABBYY requires license files at specific paths
            private const string LicenseFile = @"C:\ABBYY\License\ABBYY.lic";
            private const string LicenseKey = @"C:\ABBYY\License\ABBYY.key";

            public bool ValidateLicense()
            {
                // Check files exist
                if (!File.Exists(LicenseFile))
                {
                    Console.WriteLine("License file not found!");
                    return false;
                }

                if (!File.Exists(LicenseKey))
                {
                    Console.WriteLine("License key file not found!");
                    return false;
                }

                // License server connection may also be required
                // depending on licensing model

                return true;
            }

            public void DeployLicense(string targetPath)
            {
                // Must copy license files to deployment
                File.Copy(LicenseFile, Path.Combine(targetPath, "ABBYY.lic"));
                File.Copy(LicenseKey, Path.Combine(targetPath, "ABBYY.key"));
            }
        }
        */
    }

    namespace AfterMigration_Licensing
    {
        // using IronOcr;

        public class IronOcrLicenseManager
        {
            public void SetLicense()
            {
                // Option 1: Direct string assignment
                // IronOcr.License.LicenseKey = "IRONOCR-LICENSE-KEY";

                // Option 2: Environment variable
                // IronOcr.License.LicenseKey = Environment.GetEnvironmentVariable("IRONOCR_KEY");

                // Option 3: Configuration file
                // var key = Configuration["IronOcr:LicenseKey"];
                // IronOcr.License.LicenseKey = key;

                // No files to deploy, no server to configure
            }
        }
    }

    // ========================================================================
    // MIGRATION EXAMPLE 5: Batch Processing
    // ========================================================================
    // ABBYY: Requires careful document lifecycle per image
    // IronOCR: Simple iteration with automatic cleanup

    namespace BeforeMigration_BatchProcessing
    {
        /*
        using FREngine;

        public class AbbyyBatchProcessor
        {
            private IEngine _engine;

            public void ProcessBatch(string[] imagePaths, string outputDir)
            {
                foreach (var imagePath in imagePaths)
                {
                    // Each image requires document creation
                    var document = _engine.CreateFRDocument();

                    try
                    {
                        document.AddImageFile(imagePath, null, null);
                        document.Process(null);

                        var outputPath = Path.Combine(
                            outputDir,
                            Path.GetFileNameWithoutExtension(imagePath) + ".txt"
                        );

                        File.WriteAllText(outputPath, document.PlainText.Text);
                    }
                    finally
                    {
                        // CRITICAL: Close each document
                        // Memory leak if forgotten
                        document.Close();
                    }
                }
            }
        }
        */
    }

    namespace AfterMigration_BatchProcessing
    {
        // using IronOcr;

        public class IronOcrBatchProcessor
        {
            public void ProcessBatch(string[] imagePaths, string outputDir)
            {
                // Reusable OCR instance
                // var ocr = new IronTesseract();

                foreach (var imagePath in imagePaths)
                {
                    // Simple iteration, automatic cleanup
                    // var text = ocr.Read(imagePath).Text;

                    var outputPath = Path.Combine(
                        outputDir,
                        Path.GetFileNameWithoutExtension(imagePath) + ".txt"
                    );

                    // File.WriteAllText(outputPath, text);
                }
            }

            public void ProcessBatchWithPreprocessing(string[] imagePaths, string outputDir)
            {
                // IronOCR includes automatic preprocessing
                // var ocr = new IronTesseract();
                // ocr.Configuration.ReadBarCodes = true;

                // using var input = new OcrInput();
                // foreach (var path in imagePaths)
                // {
                //     input.LoadImage(path);
                // }

                // Built-in filters:
                // input.Deskew();
                // input.DeNoise();
                // input.EnhanceResolution();

                // var result = ocr.Read(input);
            }
        }
    }

    // ========================================================================
    // MIGRATION EXAMPLE 6: Searchable PDF Output
    // ========================================================================
    // ABBYY: Export with format parameters
    // IronOCR: Direct method call

    namespace BeforeMigration_SearchablePdf
    {
        /*
        using FREngine;

        public class AbbyySearchablePdfCreator
        {
            private IEngine _engine;

            public void CreateSearchablePdf(string inputImage, string outputPdf)
            {
                var document = _engine.CreateFRDocument();

                try
                {
                    // Add and process
                    document.AddImageFile(inputImage, null, null);
                    document.Process(null);

                    // Configure export parameters
                    var exportParams = _engine.CreatePDFExportParams();
                    exportParams.Scenario = PDFExportScenarioEnum.PDES_Balanced;
                    exportParams.UseOriginalPaperSize = true;

                    // Export to searchable PDF
                    document.Export(
                        outputPdf,
                        FileExportFormatEnum.FEF_PDF,
                        exportParams
                    );
                }
                finally
                {
                    document.Close();
                }
            }
        }
        */
    }

    namespace AfterMigration_SearchablePdf
    {
        // using IronOcr;

        public class IronOcrSearchablePdfCreator
        {
            public void CreateSearchablePdf(string inputImage, string outputPdf)
            {
                // IronOCR: Direct method call
                // var result = new IronTesseract().Read(inputImage);
                // result.SaveAsSearchablePdf(outputPdf);
            }

            public void CreateSearchablePdfFromMultipleImages(string[] inputImages, string outputPdf)
            {
                // using var input = new OcrInput();
                // foreach (var image in inputImages)
                // {
                //     input.LoadImage(image);
                // }

                // var result = new IronTesseract().Read(input);
                // result.SaveAsSearchablePdf(outputPdf);
            }
        }
    }

    // ========================================================================
    // MIGRATION SUMMARY
    // ========================================================================

    public class MigrationSummary
    {
        public static void ShowMigrationChecklist()
        {
            Console.WriteLine("=== ABBYY to IronOCR Migration Checklist ===\n");

            Console.WriteLine("Step 1: Remove ABBYY Dependencies");
            Console.WriteLine("  [ ] Remove FREngine.dll reference");
            Console.WriteLine("  [ ] Uninstall ABBYY FineReader Engine SDK");
            Console.WriteLine("  [ ] Remove license files from deployment");
            Console.WriteLine("  [ ] Remove runtime files directory");
            Console.WriteLine("  [ ] Decommission license server (if applicable)");
            Console.WriteLine();

            Console.WriteLine("Step 2: Add IronOCR");
            Console.WriteLine("  [ ] Install-Package IronOcr");
            Console.WriteLine("  [ ] Set license key: IronOcr.License.LicenseKey = \"KEY\";");
            Console.WriteLine();

            Console.WriteLine("Step 3: Update Code Patterns");
            Console.WriteLine("  [ ] Remove engine loader initialization");
            Console.WriteLine("  [ ] Remove profile loading");
            Console.WriteLine("  [ ] Replace CreateFRDocument with OcrInput");
            Console.WriteLine("  [ ] Replace Process() + PlainText.Text with Read().Text");
            Console.WriteLine("  [ ] Remove explicit Close() calls");
            Console.WriteLine("  [ ] Update PDF handling to LoadPdf()");
            Console.WriteLine("  [ ] Update searchable PDF to SaveAsSearchablePdf()");
            Console.WriteLine();

            Console.WriteLine("Step 4: Verify and Clean Up");
            Console.WriteLine("  [ ] Test all OCR scenarios");
            Console.WriteLine("  [ ] Remove ABBYY-specific error handling");
            Console.WriteLine("  [ ] Update CI/CD pipeline");
            Console.WriteLine("  [ ] Update deployment scripts");
            Console.WriteLine("  [ ] Cancel ABBYY maintenance contract");
            Console.WriteLine();

            Console.WriteLine("Estimated Time: 4-8 hours");
            Console.WriteLine("Estimated Cost Savings: $10,000-50,000+ over 3 years");
            Console.WriteLine();
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }

        public static void ShowCodeComparison()
        {
            Console.WriteLine("=== Code Comparison ===\n");

            Console.WriteLine("ABBYY FineReader Engine (20+ lines):");
            Console.WriteLine("─────────────────────────────────────");
            Console.WriteLine("var loader = new EngineLoader();");
            Console.WriteLine("var engine = loader.GetEngineObject(sdkPath, licensePath);");
            Console.WriteLine("engine.LoadPredefinedProfile(\"DocumentConversion_Accuracy\");");
            Console.WriteLine("var document = engine.CreateFRDocument();");
            Console.WriteLine("try {");
            Console.WriteLine("    document.AddImageFile(imagePath, null, null);");
            Console.WriteLine("    document.Process(null);");
            Console.WriteLine("    var text = document.PlainText.Text;");
            Console.WriteLine("} finally {");
            Console.WriteLine("    document.Close();");
            Console.WriteLine("}");
            Console.WriteLine();

            Console.WriteLine("IronOCR (1 line):");
            Console.WriteLine("─────────────────");
            Console.WriteLine("var text = new IronTesseract().Read(imagePath).Text;");
            Console.WriteLine();

            Console.WriteLine("Lines reduced: 95%");
            Console.WriteLine("Cost reduced: ~95%");
            Console.WriteLine("Deployment simplified: 100%");
        }
    }
}

// ============================================================================
// READY TO MIGRATE FROM ABBYY?
//
// IronOCR provides enterprise-grade OCR without enterprise complexity.
// Same-day deployment, clear pricing, excellent support.
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Free Trial: https://ironsoftware.com/csharp/ocr/docs/license/trial/
// Documentation: https://ironsoftware.com/csharp/ocr/docs/
//
// Install: Install-Package IronOcr
// ============================================================================
