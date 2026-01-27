/**
 * Syncfusion OCR to IronOCR Migration Comparison
 *
 * Side-by-side examples showing before/after migration patterns.
 * Syncfusion requires tessdata, suite licensing, and PDF-centric workflows.
 * IronOCR offers direct processing, built-in languages, and perpetual licensing.
 *
 * Migration Complexity: Low to Medium (1-4 hours depending on scope)
 *
 * SYNCFUSION INSTALL (before migration):
 *   dotnet add package Syncfusion.PDF.OCR.Net.Core
 *   + Manual tessdata download from https://github.com/tesseract-ocr/tessdata_best
 *
 * IRONOCR INSTALL (after migration):
 *   dotnet add package IronOcr
 *
 * IronOCR: https://ironsoftware.com/csharp/ocr/
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;

// ============================================================================
// MIGRATION EXAMPLE 1: BASIC PDF OCR
// Complexity: Low - Direct API mapping
// ============================================================================

namespace Migration_BasicPdfOcr
{
    // ----- BEFORE: Syncfusion -----

    /*
    using Syncfusion.OCRProcessor;
    using Syncfusion.Pdf;
    using Syncfusion.Pdf.Parsing;

    public class SyncfusionBasicOcr
    {
        // tessdata path must be configured and files downloaded manually
        private const string TessDataPath = @"tessdata/";

        public string ExtractTextFromPdf(string pdfPath)
        {
            // Step 1: Load PDF
            using var document = new PdfLoadedDocument(pdfPath);

            // Step 2: Create OCR processor with tessdata path
            // IMPORTANT: eng.traineddata must exist in tessdata folder
            using var processor = new OCRProcessor(TessDataPath);

            // Step 3: Configure language
            processor.Settings.Language = Languages.English;

            // Step 4: Perform OCR
            processor.PerformOCR(document);

            // Step 5: Extract text (separate step)
            var text = new StringBuilder();
            foreach (PdfLoadedPage page in document.Pages)
            {
                text.AppendLine(page.ExtractText());
            }

            return text.ToString();
        }
    }
    */

    // ----- AFTER: IronOCR -----

    using IronOcr;

    public class IronOcrBasicOcr
    {
        // No tessdata configuration needed
        // Languages are built into IronOCR

        public string ExtractTextFromPdf(string pdfPath)
        {
            // One line replaces 5+ steps
            return new IronTesseract().Read(pdfPath).Text;
        }
    }

    // Migration notes:
    // - Remove tessdata folder from project
    // - Remove tessdata path configuration
    // - Replace PdfLoadedDocument + OCRProcessor with single IronTesseract call
    // - Remove page iteration loop
}

// ============================================================================
// MIGRATION EXAMPLE 2: TESSDATA MANAGEMENT
// This entire pattern is eliminated with IronOCR
// ============================================================================

namespace Migration_TessdataManagement
{
    // ----- BEFORE: Syncfusion (manual tessdata management) -----

    /*
    public class SyncfusionTessdataSetup
    {
        // Must download files from: https://github.com/tesseract-ocr/tessdata_best
        // Each language file is 15-50MB
        // Must be deployed with application

        private const string TessDataPath = @"tessdata/";

        public bool ValidateTessdata()
        {
            // Check tessdata directory exists
            if (!Directory.Exists(TessDataPath))
            {
                Console.WriteLine("ERROR: tessdata directory not found");
                Console.WriteLine("Download from: https://github.com/tesseract-ocr/tessdata_best");
                return false;
            }

            // Check for required language files
            var requiredLanguages = new[] { "eng", "fra", "deu" };
            foreach (var lang in requiredLanguages)
            {
                string filePath = Path.Combine(TessDataPath, $"{lang}.traineddata");
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"ERROR: {lang}.traineddata not found");
                    return false;
                }
            }

            return true;
        }

        public void ShowDeploymentRequirements()
        {
            Console.WriteLine("Syncfusion tessdata deployment:");
            Console.WriteLine("1. Download .traineddata files for each language");
            Console.WriteLine("2. Add to project (Copy to Output Directory)");
            Console.WriteLine("3. Configure path in application");
            Console.WriteLine("4. Deploy tessdata folder with application");
            Console.WriteLine("5. Maintain tessdata versions separately from library");
        }
    }
    */

    // ----- AFTER: IronOCR (no tessdata needed) -----

    using IronOcr;

    public class IronOcrNoTessdata
    {
        // Languages are built in - no tessdata folder needed
        // No manual downloads, no path configuration

        public string ProcessMultiLanguage(string pdfPath)
        {
            var ocr = new IronTesseract();

            // Languages available immediately - no download
            ocr.Language = OcrLanguage.English;
            ocr.AddSecondaryLanguage(OcrLanguage.French);
            ocr.AddSecondaryLanguage(OcrLanguage.German);

            return ocr.Read(pdfPath).Text;
        }

        public void ShowDeploymentRequirements()
        {
            Console.WriteLine("IronOCR deployment:");
            Console.WriteLine("1. Install NuGet package");
            Console.WriteLine("2. Deploy application");
            Console.WriteLine("That's it - languages are included.");
        }
    }

    // Migration notes:
    // - Delete entire tessdata folder from project
    // - Remove tessdata validation code
    // - Remove CI/CD tessdata copy steps
    // - No language file management needed
}

// ============================================================================
// MIGRATION EXAMPLE 3: IMAGE OCR (PDF-CENTRIC vs DIRECT)
// Complexity: Low-Medium - Pattern change from PDF conversion to direct
// ============================================================================

namespace Migration_ImageOcr
{
    // ----- BEFORE: Syncfusion (must convert image to PDF first) -----

    /*
    using Syncfusion.OCRProcessor;
    using Syncfusion.Pdf;
    using Syncfusion.Pdf.Graphics;
    using Syncfusion.Pdf.Parsing;

    public class SyncfusionImageOcr
    {
        private const string TessDataPath = @"tessdata/";

        // Syncfusion OCR is PDF-centric
        // Images must be converted to PDF, then OCR'd
        public string OcrFromImage(string imagePath)
        {
            // Step 1: Create a new PDF document
            using var pdfDoc = new PdfDocument();

            // Step 2: Add a page to contain the image
            var page = pdfDoc.Pages.Add();

            // Step 3: Load the image
            var image = new PdfBitmap(imagePath);

            // Step 4: Draw image on PDF page
            page.Graphics.DrawImage(image, 0, 0, page.Size.Width, page.Size.Height);

            // Step 5: Save PDF to memory stream
            using var stream = new MemoryStream();
            pdfDoc.Save(stream);
            stream.Position = 0;

            // Step 6: Load the PDF for OCR
            using var loadedDoc = new PdfLoadedDocument(stream);

            // Step 7: Create OCR processor
            using var processor = new OCRProcessor(TessDataPath);
            processor.Settings.Language = Languages.English;

            // Step 8: Perform OCR
            processor.PerformOCR(loadedDoc);

            // Step 9: Extract text
            var text = new StringBuilder();
            foreach (PdfLoadedPage p in loadedDoc.Pages)
            {
                text.AppendLine(p.ExtractText());
            }

            return text.ToString();
        }
    }
    */

    // ----- AFTER: IronOCR (direct image OCR) -----

    using IronOcr;

    public class IronOcrImageOcr
    {
        // IronOCR processes images directly - no PDF conversion
        public string OcrFromImage(string imagePath)
        {
            // One line - no intermediate PDF
            return new IronTesseract().Read(imagePath).Text;
        }

        // With preprocessing options
        public string OcrWithPreprocessing(string imagePath)
        {
            var ocr = new IronTesseract();

            using var input = new OcrInput();
            input.LoadImage(imagePath);

            // Built-in preprocessing (Syncfusion requires external library)
            input.Deskew();
            input.DeNoise();

            return ocr.Read(input).Text;
        }
    }

    // Migration notes:
    // - Remove PDF creation code for image OCR
    // - Remove PdfBitmap and Graphics code
    // - Remove MemoryStream intermediate step
    // - Single Read() call replaces 9 steps
}

// ============================================================================
// MIGRATION EXAMPLE 4: LICENSE HANDLING
// Complexity: Low - String vs suite registration
// ============================================================================

namespace Migration_LicenseHandling
{
    // ----- BEFORE: Syncfusion (suite license + community restrictions) -----

    /*
    public class SyncfusionLicenseSetup
    {
        public void InitializeLicense()
        {
            // Syncfusion requires suite-wide license registration
            // Community license restrictions:
            // - Less than $1M annual revenue
            // - 5 or fewer developers
            // - 10 or fewer total employees
            // - Never received more than $3M in outside capital
            // - Not a government organization
            //
            // IMPORTANT: Audit risk if you exceed thresholds

            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("YOUR-SYNCFUSION-KEY");
        }

        public void CheckCommunityEligibility()
        {
            Console.WriteLine("Syncfusion Community License Requirements:");
            Console.WriteLine("- Annual revenue < $1,000,000 USD");
            Console.WriteLine("- Developers <= 5");
            Console.WriteLine("- Total employees <= 10");
            Console.WriteLine("- Total outside funding < $3,000,000");
            Console.WriteLine("- Not a government entity");
            Console.WriteLine();
            Console.WriteLine("WARNING: You may be audited for compliance");
            Console.WriteLine("WARNING: Exceeding thresholds requires immediate commercial license");
        }
    }
    */

    // ----- AFTER: IronOCR (simple key, perpetual option) -----

    using IronOcr;

    public class IronOcrLicenseSetup
    {
        public void InitializeLicense()
        {
            // Simple string key
            // No revenue restrictions
            // No employee count limits
            // No audit risk
            // Perpetual licensing available

            IronOcr.License.LicenseKey = "YOUR-IRONOCR-KEY";
        }

        public void LicenseOptions()
        {
            Console.WriteLine("IronOCR License Options:");
            Console.WriteLine("- Lite: $749 (one-time, perpetual)");
            Console.WriteLine("- Plus: $1,499 (one-time, perpetual)");
            Console.WriteLine("- Professional: $2,999 (one-time, perpetual)");
            Console.WriteLine("- Enterprise: Contact for pricing");
            Console.WriteLine();
            Console.WriteLine("No revenue restrictions");
            Console.WriteLine("No employee count limits");
            Console.WriteLine("No audit risk");
        }
    }

    // Migration notes:
    // - Replace Syncfusion license registration with IronOcr.License.LicenseKey
    // - No community license compliance tracking needed
    // - No annual renewal required (perpetual option)
}

// ============================================================================
// MIGRATION EXAMPLE 5: MULTI-PAGE PROCESSING
// Complexity: Low - Loop pattern to single call
// ============================================================================

namespace Migration_MultiPageProcessing
{
    // ----- BEFORE: Syncfusion (manual page iteration) -----

    /*
    using Syncfusion.OCRProcessor;
    using Syncfusion.Pdf.Parsing;

    public class SyncfusionMultiPage
    {
        private const string TessDataPath = @"tessdata/";

        public Dictionary<int, string> ProcessAllPages(string pdfPath)
        {
            var results = new Dictionary<int, string>();

            using var document = new PdfLoadedDocument(pdfPath);
            using var processor = new OCRProcessor(TessDataPath);

            processor.Settings.Language = Languages.English;
            processor.PerformOCR(document);

            // Must iterate pages manually
            int pageNumber = 1;
            foreach (PdfLoadedPage page in document.Pages)
            {
                results[pageNumber] = page.ExtractText();
                pageNumber++;
            }

            return results;
        }

        public string GetSpecificPage(string pdfPath, int pageIndex)
        {
            using var document = new PdfLoadedDocument(pdfPath);
            using var processor = new OCRProcessor(TessDataPath);

            processor.Settings.Language = Languages.English;

            // Must still OCR entire document
            processor.PerformOCR(document);

            // Then extract specific page
            if (pageIndex < document.Pages.Count)
            {
                var page = document.Pages[pageIndex] as PdfLoadedPage;
                return page?.ExtractText() ?? string.Empty;
            }

            return string.Empty;
        }
    }
    */

    // ----- AFTER: IronOCR (automatic page handling) -----

    using IronOcr;

    public class IronOcrMultiPage
    {
        public Dictionary<int, string> ProcessAllPages(string pdfPath)
        {
            var ocr = new IronTesseract();
            var result = ocr.Read(pdfPath);

            var results = new Dictionary<int, string>();
            for (int i = 0; i < result.Pages.Length; i++)
            {
                results[i + 1] = result.Pages[i].Text;
            }

            return results;
        }

        public string GetAllText(string pdfPath)
        {
            // One line for all pages
            return new IronTesseract().Read(pdfPath).Text;
        }

        public string GetSpecificPage(string pdfPath, int pageNumber)
        {
            using var input = new OcrInput();
            // Load only the page needed
            input.LoadPdfPages(pdfPath, new[] { pageNumber });

            return new IronTesseract().Read(input).Text;
        }
    }

    // Migration notes:
    // - Remove page iteration loops for simple text extraction
    // - Use result.Pages[] for page-specific access
    // - LoadPdfPages() for selective page processing (more efficient)
}

// ============================================================================
// MIGRATION EXAMPLE 6: ERROR HANDLING
// Complexity: Low - Exception types change
// ============================================================================

namespace Migration_ErrorHandling
{
    // ----- BEFORE: Syncfusion (tessdata and license errors common) -----

    /*
    using Syncfusion.OCRProcessor;
    using Syncfusion.Pdf.Parsing;

    public class SyncfusionErrorHandling
    {
        private const string TessDataPath = @"tessdata/";

        public string ProcessWithErrorHandling(string pdfPath)
        {
            // Must check for tessdata before processing
            if (!Directory.Exists(TessDataPath))
            {
                throw new InvalidOperationException(
                    "tessdata directory not found. Download from tessdata repository.");
            }

            if (!File.Exists(Path.Combine(TessDataPath, "eng.traineddata")))
            {
                throw new InvalidOperationException(
                    "eng.traineddata not found. Download English language file.");
            }

            try
            {
                using var document = new PdfLoadedDocument(pdfPath);
                using var processor = new OCRProcessor(TessDataPath);

                processor.Settings.Language = Languages.English;
                processor.PerformOCR(document);

                var text = new StringBuilder();
                foreach (PdfLoadedPage page in document.Pages)
                {
                    text.AppendLine(page.ExtractText());
                }
                return text.ToString();
            }
            catch (Exception ex) when (ex.Message.Contains("license"))
            {
                // License errors may occur if:
                // - Community license thresholds exceeded
                // - Commercial license expired
                // - License key invalid
                throw new InvalidOperationException(
                    "Syncfusion license error. Check community eligibility or commercial license.", ex);
            }
        }
    }
    */

    // ----- AFTER: IronOCR (simplified error handling) -----

    using IronOcr;

    public class IronOcrErrorHandling
    {
        // No tessdata checks needed - languages built in
        // No community license compliance checks

        public string ProcessWithErrorHandling(string pdfPath)
        {
            try
            {
                return new IronTesseract().Read(pdfPath).Text;
            }
            catch (FileNotFoundException ex)
            {
                throw new ArgumentException($"PDF file not found: {pdfPath}", ex);
            }
            catch (Exception ex) when (ex.Message.Contains("license"))
            {
                throw new InvalidOperationException(
                    "IronOCR license required. Get trial: https://ironsoftware.com/csharp/ocr/", ex);
            }
        }
    }

    // Migration notes:
    // - Remove tessdata existence checks
    // - Remove language file validation
    // - No community license compliance validation needed
}

// ============================================================================
// MIGRATION CHECKLIST
// ============================================================================

namespace Migration_Checklist
{
    public class MigrationChecklist
    {
        public void ShowChecklist()
        {
            Console.WriteLine("=== SYNCFUSION TO IRONOCR MIGRATION CHECKLIST ===\n");

            Console.WriteLine("PACKAGE CHANGES:");
            Console.WriteLine("[ ] Remove Syncfusion.PDF.OCR.Net.Core");
            Console.WriteLine("[ ] Remove Syncfusion.Pdf.Net.Core");
            Console.WriteLine("[ ] Remove any other Syncfusion dependencies");
            Console.WriteLine("[ ] Add IronOcr package");
            Console.WriteLine();

            Console.WriteLine("CODE CHANGES:");
            Console.WriteLine("[ ] Replace Syncfusion license registration with IronOcr.License.LicenseKey");
            Console.WriteLine("[ ] Replace OCRProcessor with IronTesseract");
            Console.WriteLine("[ ] Replace PdfLoadedDocument with OcrInput (for PDF)");
            Console.WriteLine("[ ] Replace page iteration with result.Text or result.Pages[]");
            Console.WriteLine("[ ] Remove tessdata path configuration");
            Console.WriteLine("[ ] Update language configuration to OcrLanguage enum");
            Console.WriteLine("[ ] Remove PDF conversion for image OCR");
            Console.WriteLine();

            Console.WriteLine("DEPLOYMENT CHANGES:");
            Console.WriteLine("[ ] Remove tessdata folder from project");
            Console.WriteLine("[ ] Update .csproj to remove tessdata copy rules");
            Console.WriteLine("[ ] Update CI/CD to remove tessdata handling");
            Console.WriteLine("[ ] Remove tessdata from Docker images");
            Console.WriteLine();

            Console.WriteLine("TESTING:");
            Console.WriteLine("[ ] Test PDF OCR functionality");
            Console.WriteLine("[ ] Test image OCR functionality");
            Console.WriteLine("[ ] Test multi-language scenarios");
            Console.WriteLine("[ ] Test searchable PDF creation");
            Console.WriteLine("[ ] Verify license key works");
            Console.WriteLine();

            Console.WriteLine("CLEANUP:");
            Console.WriteLine("[ ] Delete Syncfusion license files");
            Console.WriteLine("[ ] Remove community license compliance documentation");
            Console.WriteLine("[ ] Cancel Syncfusion subscription if no other components used");
        }

        public void ShowBenefitsAfterMigration()
        {
            Console.WriteLine("\n=== BENEFITS AFTER MIGRATION ===\n");

            Console.WriteLine("CODE REDUCTION:");
            Console.WriteLine("- PDF OCR: 15+ lines -> 1 line");
            Console.WriteLine("- Image OCR: 20+ lines -> 1 line");
            Console.WriteLine("- No tessdata validation code");
            Console.WriteLine("- No page iteration loops");
            Console.WriteLine();

            Console.WriteLine("DEPLOYMENT SIMPLIFICATION:");
            Console.WriteLine("- No tessdata folder (saves 50-500MB+)");
            Console.WriteLine("- Single DLL deployment");
            Console.WriteLine("- Smaller Docker images");
            Console.WriteLine("- Simpler CI/CD pipelines");
            Console.WriteLine();

            Console.WriteLine("COST REDUCTION (example: 5 devs, 3 years):");
            Console.WriteLine("- Syncfusion: $14,925 - $23,925");
            Console.WriteLine("- IronOCR: $2,999 (Professional, perpetual)");
            Console.WriteLine("- Savings: $12,000 - $21,000");
            Console.WriteLine();

            Console.WriteLine("RISK ELIMINATION:");
            Console.WriteLine("- No community license audit risk");
            Console.WriteLine("- No growth-triggered licensing events");
            Console.WriteLine("- No annual renewal requirements");
            Console.WriteLine("- No employee count compliance tracking");
        }
    }
}

// ============================================================================
// COMPARISON SUMMARY
// ============================================================================

namespace Comparison_Summary
{
    public class ComparisonSummary
    {
        public void ShowSummary()
        {
            Console.WriteLine("=== SYNCFUSION OCR vs IRONOCR ===\n");

            Console.WriteLine("Feature          | Syncfusion        | IronOCR");
            Console.WriteLine("─────────────────────────────────────────────────────");
            Console.WriteLine("tessdata needed  | Yes (manual)      | No (built-in)");
            Console.WriteLine("Image OCR        | Via PDF convert   | Direct");
            Console.WriteLine("PDF OCR          | Yes               | Yes");
            Console.WriteLine("Languages        | 60+ (download)    | 125+ (built-in)");
            Console.WriteLine("Preprocessing    | Manual            | Automatic");
            Console.WriteLine("License model    | Annual suite      | Perpetual option");
            Console.WriteLine("Community tier   | Yes ($1M limit)   | Trial only");
            Console.WriteLine("Audit risk       | Yes               | No");
            Console.WriteLine("Code complexity  | 15+ lines         | 1 line");
            Console.WriteLine();

            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
            Console.WriteLine("NuGet: https://www.nuget.org/packages/IronOcr/");
        }
    }
}

// ============================================================================
// TRY IRONOCR - SIMPLER, NO TESSDATA, PERPETUAL LICENSING
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// ============================================================================
