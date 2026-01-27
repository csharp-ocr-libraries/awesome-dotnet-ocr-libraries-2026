/**
 * ABBYY FineReader Engine vs IronOCR: Code Examples
 *
 * Compare enterprise ABBYY with IronOCR.
 * ABBYY is powerful but expensive; IronOCR offers similar features at lower cost.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * ABBYY SDK: Requires separate licensing agreement
 * IronOCR NuGet: https://www.nuget.org/packages/IronOcr/
 */

using System;

// ============================================================================
// ABBYY FINEREADER ENGINE - Enterprise OCR
// ============================================================================

namespace AbbyyExamples
{
    /*
    using FREngine;

    /// <summary>
    /// ABBYY FineReader Engine requires:
    /// 1. SDK license (contact sales - $$$)
    /// 2. Engine installation
    /// 3. Complex initialization
    /// </summary>
    public class AbbyyOcrService
    {
        private readonly IEngine _engine;

        public AbbyyOcrService(string projectId)
        {
            // Complex initialization
            var loader = new EngineLoader();
            _engine = loader.GetEngineObject(projectId);
        }

        /// <summary>
        /// Extract text with ABBYY
        /// </summary>
        public string ExtractText(string imagePath)
        {
            var frDocument = _engine.CreateFRDocumentFromImage(imagePath, null);
            frDocument.Process(null);

            string result = "";
            for (int i = 0; i < frDocument.Pages.Count; i++)
            {
                result += frDocument.Pages[i].PlainText.Text + "\n";
            }

            return result;
        }

        /// <summary>
        /// Table extraction
        /// </summary>
        public void ExtractTables(string imagePath)
        {
            var frDocument = _engine.CreateFRDocumentFromImage(imagePath, null);
            frDocument.Process(null);

            foreach (IFRPage page in frDocument.Pages)
            {
                foreach (IBlock block in page.Layout.Blocks)
                {
                    if (block.Type == BlockTypeEnum.BT_Table)
                    {
                        var table = block.GetAsTableBlock();
                        // Process table cells...
                    }
                }
            }
        }
    }
    */

    public class AbbyyPlaceholder
    {
        public void ShowRequirements()
        {
            Console.WriteLine("ABBYY FineReader Engine Requirements:");
            Console.WriteLine("1. Contact ABBYY sales for SDK license");
            Console.WriteLine("2. Enterprise pricing ($10,000+ annually)");
            Console.WriteLine("3. Install FineReader Engine locally");
            Console.WriteLine("4. Configure license server");
            Console.WriteLine("5. Complex API initialization");
            Console.WriteLine();
            Console.WriteLine("Consider IronOCR instead:");
            Console.WriteLine("https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// IRONOCR - SIMILAR FEATURES, SIMPLER API, LOWER COST
// Get it: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace IronOcrExamples
{
    using IronOcr;
    using System.Linq;

    /// <summary>
    /// IronOCR provides enterprise features without enterprise complexity:
    /// - One NuGet package
    /// - Clear pricing (no sales calls)
    /// - Same-day deployment
    ///
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class IronOcrService
    {
        /// <summary>
        /// Simple text extraction - one line
        /// </summary>
        public string ExtractText(string imagePath)
        {
            return new IronTesseract().Read(imagePath).Text;
        }

        /// <summary>
        /// PDF OCR - native support
        /// </summary>
        public string ExtractFromPdf(string pdfPath)
        {
            return new IronTesseract().Read(pdfPath).Text;
        }

        /// <summary>
        /// Structured data access
        /// </summary>
        public void ExtractStructuredData(string imagePath)
        {
            var result = new IronTesseract().Read(imagePath);

            // Pages
            foreach (var page in result.Pages)
            {
                Console.WriteLine($"Page {page.PageNumber}");
            }

            // Paragraphs
            foreach (var para in result.Paragraphs)
            {
                Console.WriteLine($"Paragraph: {para.Text}");
            }

            // Words with positions
            foreach (var word in result.Words)
            {
                Console.WriteLine($"'{word.Text}' at ({word.X},{word.Y})");
            }
        }

        /// <summary>
        /// Create searchable PDF
        /// </summary>
        public void CreateSearchablePdf(string inputPath, string outputPath)
        {
            var result = new IronTesseract().Read(inputPath);
            result.SaveAsSearchablePdf(outputPath);
        }

        /// <summary>
        /// Multi-language support - 125+ languages
        /// </summary>
        public string ExtractMultiLanguage(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.English;
            ocr.AddSecondaryLanguage(OcrLanguage.French);
            ocr.AddSecondaryLanguage(OcrLanguage.German);

            return ocr.Read(imagePath).Text;
        }
    }
}


// ============================================================================
// COMPARISON: ABBYY vs IRONOCR
// ============================================================================

namespace Comparison
{
    public class AbbyyVsIronOcrComparison
    {
        public void Compare()
        {
            Console.WriteLine("=== ABBYY FINEREADER vs IRONOCR ===\n");

            Console.WriteLine("Feature          | ABBYY          | IronOCR");
            Console.WriteLine("─────────────────────────────────────────────");
            Console.WriteLine("Pricing          | Sales call     | Published");
            Console.WriteLine("Annual cost      | $10,000+       | $749-2,999");
            Console.WriteLine("Installation     | Complex        | NuGet");
            Console.WriteLine("PDF support      | Yes            | Yes");
            Console.WriteLine("Languages        | 190+           | 125+");
            Console.WriteLine("Searchable PDF   | Yes            | Yes");
            Console.WriteLine("On-premise       | Yes            | Yes");
            Console.WriteLine("Time to deploy   | Days/weeks     | Minutes");
            Console.WriteLine();
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }

        public void PricingBreakdown()
        {
            Console.WriteLine("=== PRICING COMPARISON ===\n");

            Console.WriteLine("ABBYY FineReader Engine:");
            Console.WriteLine("  - Enterprise SDK license required");
            Console.WriteLine("  - Typically $10,000-50,000+ annually");
            Console.WriteLine("  - Volume licensing adds cost");
            Console.WriteLine("  - Support contracts extra");
            Console.WriteLine();

            Console.WriteLine("IronOCR (https://ironsoftware.com/csharp/ocr/):");
            Console.WriteLine("  - Lite: $749 (one-time)");
            Console.WriteLine("  - Professional: $1,499 (one-time)");
            Console.WriteLine("  - Unlimited: $2,999 (one-time)");
            Console.WriteLine("  - Free trial available");
            Console.WriteLine();

            Console.WriteLine("BOTTOM LINE: IronOCR costs less than one year of ABBYY.");
        }
    }
}


// ============================================================================
// MIGRATION FROM ABBYY TO IRONOCR
// ============================================================================

namespace Migration
{
    using IronOcr;

    /// <summary>
    /// Common ABBYY patterns and their IronOCR equivalents.
    /// Migration is straightforward for most use cases.
    ///
    /// Get IronOCR: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class AbbyyMigration
    {
        // ABBYY: var frDocument = engine.CreateFRDocumentFromImage(path, null);
        // IronOCR:
        public string BasicOcr(string imagePath)
        {
            return new IronTesseract().Read(imagePath).Text;
        }

        // ABBYY: Complex table extraction with block types
        // IronOCR: Structured result with lines/words/positions
        public void StructuredExtraction(string imagePath)
        {
            var result = new IronTesseract().Read(imagePath);

            foreach (var line in result.Lines)
            {
                Console.WriteLine($"Line at Y={line.Y}: {line.Text}");
            }
        }

        // ABBYY: frDocument.Export(path, ExportFormatEnum.EFF_PDF);
        // IronOCR:
        public void ExportToPdf(string inputPath, string outputPath)
        {
            var result = new IronTesseract().Read(inputPath);
            result.SaveAsSearchablePdf(outputPath);
        }
    }
}


// ============================================================================
// READY TO SWITCH FROM ABBYY?
//
// IronOCR provides enterprise-grade OCR without enterprise complexity.
// Same-day deployment, clear pricing, excellent support.
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Free Trial: https://ironsoftware.com/csharp/ocr/#trial-license
// Documentation: https://ironsoftware.com/csharp/ocr/docs/
//
// Install: Install-Package IronOcr
// ============================================================================
