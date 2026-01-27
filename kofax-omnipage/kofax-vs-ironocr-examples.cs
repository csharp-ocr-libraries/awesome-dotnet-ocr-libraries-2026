/**
 * Kofax OmniPage vs IronOCR: Code Examples
 *
 * Compare enterprise Kofax OmniPage with IronOCR.
 * Kofax is expensive enterprise software; IronOCR offers accessible pricing.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * Kofax: Enterprise licensing, sales engagement required
 * IronOCR NuGet: https://www.nuget.org/packages/IronOcr/
 */

using System;

// ============================================================================
// KOFAX OMNIPAGE IMPLEMENTATION - Enterprise Document Processing
// ============================================================================

namespace KofaxExamples
{
    /*
    using Kofax.OmniPage;

    /// <summary>
    /// Kofax OmniPage requirements:
    /// 1. Enterprise sales engagement
    /// 2. Complex SDK installation
    /// 3. License server configuration
    /// 4. Significant development investment
    /// </summary>
    public class KofaxOcrService
    {
        private readonly OmniPageEngine _engine;

        public KofaxOcrService(string licenseKey)
        {
            _engine = new OmniPageEngine();
            _engine.Initialize(licenseKey);
        }

        public string ExtractText(string imagePath)
        {
            var document = _engine.LoadDocument(imagePath);
            document.Recognize();
            return document.GetText();
        }

        public void Dispose()
        {
            _engine?.Terminate();
        }
    }
    */

    public class KofaxPlaceholder
    {
        public void ShowRequirements()
        {
            Console.WriteLine("Kofax OmniPage Requirements:");
            Console.WriteLine("1. Contact Kofax sales team");
            Console.WriteLine("2. Enterprise agreement negotiation");
            Console.WriteLine("3. SDK installation (complex)");
            Console.WriteLine("4. License server setup");
            Console.WriteLine("5. Significant cost ($10,000+/year)");
            Console.WriteLine("6. Long deployment timeline");
            Console.WriteLine();
            Console.WriteLine("For accessible OCR, try IronOCR:");
            Console.WriteLine("https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// IRONOCR - ENTERPRISE FEATURES WITHOUT ENTERPRISE COMPLEXITY
// Get it: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace IronOcrExamples
{
    using IronOcr;

    /// <summary>
    /// IronOCR provides enterprise-grade OCR with:
    /// - Clear pricing (no sales calls)
    /// - NuGet installation
    /// - Same-day deployment
    /// - Excellent accuracy
    ///
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class IronOcrService
    {
        /// <summary>
        /// Simple text extraction
        /// </summary>
        public string ExtractText(string imagePath)
        {
            return new IronTesseract().Read(imagePath).Text;
        }

        /// <summary>
        /// High-quality OCR with preprocessing
        /// </summary>
        public string HighQualityOcr(string imagePath)
        {
            using var input = new OcrInput();
            input.LoadImage(imagePath);
            input.Deskew();
            input.DeNoise();
            input.EnhanceResolution(300);

            return new IronTesseract().Read(input).Text;
        }

        /// <summary>
        /// PDF processing - built-in
        /// </summary>
        public string ProcessPdf(string pdfPath)
        {
            return new IronTesseract().Read(pdfPath).Text;
        }

        /// <summary>
        /// Batch processing
        /// </summary>
        public void BatchProcess(string[] imagePaths)
        {
            var ocr = new IronTesseract();

            foreach (var path in imagePaths)
            {
                var result = ocr.Read(path);
                Console.WriteLine($"{path}: {result.Text.Length} chars, {result.Confidence}% confidence");
            }
        }

        /// <summary>
        /// Create searchable PDF archive
        /// </summary>
        public void CreateSearchableArchive(string inputPdf, string outputPdf)
        {
            var result = new IronTesseract().Read(inputPdf);
            result.SaveAsSearchablePdf(outputPdf);
        }

        /// <summary>
        /// Multi-language document processing
        /// </summary>
        public string ProcessMultiLanguage(string imagePath)
        {
            var ocr = new IronTesseract();
            ocr.Language = OcrLanguage.English;
            ocr.AddSecondaryLanguage(OcrLanguage.French);
            ocr.AddSecondaryLanguage(OcrLanguage.German);
            ocr.AddSecondaryLanguage(OcrLanguage.Spanish);

            return ocr.Read(imagePath).Text;
        }

        /// <summary>
        /// Structured data extraction
        /// </summary>
        public void ExtractStructuredData(string imagePath)
        {
            var result = new IronTesseract().Read(imagePath);

            Console.WriteLine($"Total confidence: {result.Confidence}%");
            Console.WriteLine($"Pages: {result.Pages.Length}");
            Console.WriteLine($"Paragraphs: {result.Paragraphs.Length}");
            Console.WriteLine($"Lines: {result.Lines.Length}");
            Console.WriteLine($"Words: {result.Words.Length}");
        }
    }
}


// ============================================================================
// COMPARISON
// ============================================================================

namespace Comparison
{
    public class KofaxVsIronOcrComparison
    {
        public void Compare()
        {
            Console.WriteLine("=== KOFAX OMNIPAGE vs IRONOCR ===\n");

            Console.WriteLine("Feature          | Kofax          | IronOCR");
            Console.WriteLine("─────────────────────────────────────────────");
            Console.WriteLine("Pricing          | Enterprise     | Published");
            Console.WriteLine("Cost             | $10,000+/year  | $749-2,999 once");
            Console.WriteLine("Sales required   | Yes            | No");
            Console.WriteLine("Installation     | Complex        | NuGet");
            Console.WriteLine("Deployment time  | Weeks          | Minutes");
            Console.WriteLine("PDF support      | Yes            | Yes");
            Console.WriteLine("Searchable PDF   | Yes            | Yes");
            Console.WriteLine("Languages        | 120+           | 125+");
            Console.WriteLine();
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }

        public void TotalCostOfOwnership()
        {
            Console.WriteLine("=== TOTAL COST OF OWNERSHIP ===\n");

            Console.WriteLine("KOFAX OMNIPAGE (3-year):");
            Console.WriteLine("  - License: $10,000-50,000/year");
            Console.WriteLine("  - Implementation: $5,000-20,000");
            Console.WriteLine("  - Training: $2,000-5,000");
            Console.WriteLine("  - Maintenance: $3,000-10,000/year");
            Console.WriteLine("  - 3-year total: $50,000-200,000+");
            Console.WriteLine();

            Console.WriteLine("IRONOCR (3-year):");
            Console.WriteLine("  - License: $749-2,999 (one-time)");
            Console.WriteLine("  - Implementation: $500-2,000");
            Console.WriteLine("  - Training: Self-service docs");
            Console.WriteLine("  - Maintenance: Optional upgrade");
            Console.WriteLine("  - 3-year total: $1,249-5,000");
            Console.WriteLine();
            Console.WriteLine("Download: https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// ENTERPRISE OCR WITHOUT ENTERPRISE PRICING
//
// IronOCR delivers professional-grade results at accessible prices.
// No sales calls, no enterprise agreements, no hassle.
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Free Trial: https://ironsoftware.com/csharp/ocr/#trial-license
// Pricing: https://ironsoftware.com/csharp/ocr/#pricing
//
// Install: Install-Package IronOcr
// ============================================================================
