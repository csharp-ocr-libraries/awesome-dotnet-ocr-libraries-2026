/**
 * Mindee vs IronOCR: Code Examples
 *
 * Compare cloud-based Mindee with on-premise IronOCR.
 * Mindee sends documents to their cloud; IronOCR stays local.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * NuGet Packages:
 * - Mindee (Mindee cloud SDK)
 * - IronOcr (IronOCR) - https://www.nuget.org/packages/IronOcr/
 */

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// ============================================================================
// MINDEE IMPLEMENTATION - Cloud Document Parsing
// Your documents are uploaded to Mindee's servers
// ============================================================================

namespace MindeeExamples
{
    /*
    using Mindee;
    using Mindee.Input;
    using Mindee.Product.Invoice;

    /// <summary>
    /// Mindee sends your documents to their cloud.
    /// Good for structured parsing, but data leaves your infrastructure.
    /// </summary>
    public class MindeeService
    {
        private readonly MindeeClient _client;

        public MindeeService(string apiKey)
        {
            _client = new MindeeClient(apiKey);
        }

        /// <summary>
        /// Parse invoice - document uploaded to Mindee
        /// </summary>
        public async Task<InvoiceData> ParseInvoiceAsync(string filePath)
        {
            // WARNING: Document is uploaded to Mindee cloud
            var inputSource = new LocalInputSource(filePath);
            var response = await _client.ParseAsync<InvoiceV4>(inputSource);

            var prediction = response.Document.Inference.Prediction;

            return new InvoiceData
            {
                InvoiceNumber = prediction.InvoiceNumber?.Value,
                Date = prediction.InvoiceDate?.Value,
                Total = prediction.TotalAmount?.Value,
                VendorName = prediction.SupplierName?.Value
            };
        }
    }

    public class InvoiceData
    {
        public string InvoiceNumber { get; set; }
        public DateTime? Date { get; set; }
        public decimal? Total { get; set; }
        public string VendorName { get; set; }
    }
    */
}


// ============================================================================
// IRONOCR - ON-PREMISE, BUILD YOUR OWN EXTRACTION
// Your data never leaves your infrastructure
// Get it: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace IronOcrExamples
{
    using IronOcr;
    using System.Linq;

    /// <summary>
    /// IronOCR processes locally - your data stays with you.
    /// Build structured extraction using OCR + patterns.
    ///
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class IronOcrInvoiceService
    {
        private readonly IronTesseract _ocr = new IronTesseract();

        /// <summary>
        /// Extract invoice fields locally
        /// </summary>
        public InvoiceData ParseInvoice(string imagePath)
        {
            // All processing on YOUR machine
            var result = _ocr.Read(imagePath);
            var text = result.Text;

            return new InvoiceData
            {
                InvoiceNumber = ExtractPattern(text, @"Invoice\s*#?\s*(\d+)"),
                Date = ExtractDate(text),
                Total = ExtractTotal(text),
                VendorName = GetFirstLine(result)
            };
        }

        private string ExtractPattern(string text, string pattern)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }

        private DateTime? ExtractDate(string text)
        {
            var patterns = new[] {
                @"\d{1,2}/\d{1,2}/\d{4}",
                @"\d{4}-\d{2}-\d{2}",
                @"\w+ \d{1,2}, \d{4}"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern);
                if (match.Success && DateTime.TryParse(match.Value, out var date))
                    return date;
            }

            return null;
        }

        private decimal? ExtractTotal(string text)
        {
            var match = Regex.Match(text, @"Total:?\s*\$?([\d,]+\.?\d*)", RegexOptions.IgnoreCase);
            if (match.Success && decimal.TryParse(match.Groups[1].Value.Replace(",", ""), out var total))
                return total;
            return null;
        }

        private string GetFirstLine(OcrResult result)
        {
            return result.Lines.FirstOrDefault()?.Text;
        }
    }

    public class InvoiceData
    {
        public string InvoiceNumber { get; set; }
        public DateTime? Date { get; set; }
        public decimal? Total { get; set; }
        public string VendorName { get; set; }
    }
}


// ============================================================================
// COMPARISON: CLOUD vs ON-PREMISE
// ============================================================================

namespace Comparison
{
    public class MindeeVsIronOcrComparison
    {
        public void Compare()
        {
            Console.WriteLine("=== MINDEE vs IRONOCR ===\n");

            Console.WriteLine("Feature          | Mindee         | IronOCR");
            Console.WriteLine("─────────────────────────────────────────────");
            Console.WriteLine("Data location    | Mindee cloud   | Your machine");
            Console.WriteLine("Internet needed  | Yes            | No");
            Console.WriteLine("Per-doc cost     | Yes            | No");
            Console.WriteLine("Pre-built parsing| Yes            | Build your own");
            Console.WriteLine("Custom docs      | Paid training  | Pattern matching");
            Console.WriteLine("HIPAA suitable   | Review needed  | Yes (local)");
            Console.WriteLine();

            Console.WriteLine("MINDEE: Great for quick prototypes with standard docs.");
            Console.WriteLine("IRONOCR: Better when data must stay local.");
            Console.WriteLine();
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// NEED ON-PREMISE DOCUMENT PARSING?
//
// IronOCR keeps your data local while giving you
// the building blocks for any document extraction.
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// ============================================================================
