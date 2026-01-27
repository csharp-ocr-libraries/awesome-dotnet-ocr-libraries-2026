/**
 * Veryfi vs IronOCR: Code Examples
 *
 * Compare Veryfi's cloud document processing with IronOCR's on-premise solution.
 * Veryfi sends documents to their cloud; IronOCR processes locally.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * NuGet Packages:
 * - Veryfi (Cloud API SDK)
 * - IronOcr (IronOCR) - https://www.nuget.org/packages/IronOcr/
 */

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// ============================================================================
// VERYFI IMPLEMENTATION - Cloud Document Processing
// Your documents are uploaded to Veryfi's servers
// ============================================================================

namespace VeryfiExamples
{
    /*
    using Veryfi;

    /// <summary>
    /// Veryfi requires:
    /// 1. API credentials (client_id, client_secret, username, api_key)
    /// 2. Internet connection
    /// 3. Documents uploaded to their cloud
    /// 4. Per-document pricing
    /// </summary>
    public class VeryfiService
    {
        private readonly VeryfiClient _client;

        public VeryfiService(string clientId, string clientSecret, string username, string apiKey)
        {
            _client = new VeryfiClient(clientId, clientSecret, username, apiKey);
        }

        /// <summary>
        /// Process receipt - document goes to Veryfi cloud
        /// </summary>
        public async Task<ReceiptData> ProcessReceiptAsync(string imagePath)
        {
            // WARNING: Document uploaded to Veryfi servers
            var bytes = File.ReadAllBytes(imagePath);
            var response = await _client.ProcessDocumentAsync(bytes);

            return new ReceiptData
            {
                VendorName = response.Vendor?.Name,
                Total = response.Total,
                Date = response.Date,
                LineItems = response.LineItems?.Select(li => new LineItem
                {
                    Description = li.Description,
                    Quantity = li.Quantity,
                    Total = li.Total
                }).ToList()
            };
        }

        /// <summary>
        /// Process invoice
        /// </summary>
        public async Task<InvoiceData> ProcessInvoiceAsync(string imagePath)
        {
            var bytes = File.ReadAllBytes(imagePath);
            var response = await _client.ProcessDocumentAsync(bytes, new[] { "invoices" });

            return new InvoiceData
            {
                InvoiceNumber = response.InvoiceNumber,
                VendorName = response.Vendor?.Name,
                Total = response.Total,
                DueDate = response.DueDate
            };
        }
    }

    public class ReceiptData
    {
        public string VendorName { get; set; }
        public decimal? Total { get; set; }
        public DateTime? Date { get; set; }
        public List<LineItem> LineItems { get; set; }
    }

    public class LineItem
    {
        public string Description { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? Total { get; set; }
    }

    public class InvoiceData
    {
        public string InvoiceNumber { get; set; }
        public string VendorName { get; set; }
        public decimal? Total { get; set; }
        public DateTime? DueDate { get; set; }
    }
    */

    public class VeryfiPlaceholder
    {
        public void ShowConsiderations()
        {
            Console.WriteLine("Veryfi Considerations:");
            Console.WriteLine("1. Documents uploaded to Veryfi cloud");
            Console.WriteLine("2. Per-document pricing (adds up fast)");
            Console.WriteLine("3. Requires internet connection");
            Console.WriteLine("4. Great for quick prototyping");
            Console.WriteLine("5. May not meet compliance requirements");
            Console.WriteLine();
            Console.WriteLine("For on-premise OCR, try IronOCR:");
            Console.WriteLine("https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// IRONOCR - ON-PREMISE DOCUMENT PROCESSING
// Your data stays on your infrastructure
// Get it: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace IronOcrExamples
{
    using IronOcr;
    using System.Linq;

    /// <summary>
    /// IronOCR processes locally - no cloud required.
    /// Build your own extraction logic with OCR + pattern matching.
    ///
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class IronOcrReceiptService
    {
        private readonly IronTesseract _ocr = new IronTesseract();

        /// <summary>
        /// Extract receipt data - all processing on YOUR machine
        /// </summary>
        public ReceiptData ProcessReceipt(string imagePath)
        {
            var result = _ocr.Read(imagePath);
            var text = result.Text;

            return new ReceiptData
            {
                VendorName = GetFirstLine(result),
                Total = ExtractTotal(text),
                Date = ExtractDate(text),
                RawText = text
            };
        }

        /// <summary>
        /// Extract invoice data locally
        /// </summary>
        public InvoiceData ProcessInvoice(string imagePath)
        {
            var result = _ocr.Read(imagePath);
            var text = result.Text;

            return new InvoiceData
            {
                InvoiceNumber = ExtractPattern(text, @"Invoice\s*#?\s*:?\s*(\w+)"),
                VendorName = GetFirstLine(result),
                Total = ExtractTotal(text),
                DueDate = ExtractDueDate(text),
                RawText = text
            };
        }

        private string GetFirstLine(OcrResult result)
        {
            return result.Lines.FirstOrDefault()?.Text;
        }

        private decimal? ExtractTotal(string text)
        {
            var patterns = new[]
            {
                @"Total:?\s*\$?([\d,]+\.?\d*)",
                @"Grand Total:?\s*\$?([\d,]+\.?\d*)",
                @"Amount Due:?\s*\$?([\d,]+\.?\d*)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success && decimal.TryParse(match.Groups[1].Value.Replace(",", ""), out var total))
                    return total;
            }
            return null;
        }

        private DateTime? ExtractDate(string text)
        {
            var patterns = new[]
            {
                @"\d{1,2}/\d{1,2}/\d{4}",
                @"\d{4}-\d{2}-\d{2}",
                @"\w+ \d{1,2},? \d{4}"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern);
                if (match.Success && DateTime.TryParse(match.Value, out var date))
                    return date;
            }
            return null;
        }

        private DateTime? ExtractDueDate(string text)
        {
            var match = Regex.Match(text, @"Due Date:?\s*(\d{1,2}/\d{1,2}/\d{4})", RegexOptions.IgnoreCase);
            if (match.Success && DateTime.TryParse(match.Groups[1].Value, out var date))
                return date;
            return ExtractDate(text);
        }

        private string ExtractPattern(string text, string pattern)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }
    }

    public class ReceiptData
    {
        public string VendorName { get; set; }
        public decimal? Total { get; set; }
        public DateTime? Date { get; set; }
        public string RawText { get; set; }
    }

    public class InvoiceData
    {
        public string InvoiceNumber { get; set; }
        public string VendorName { get; set; }
        public decimal? Total { get; set; }
        public DateTime? DueDate { get; set; }
        public string RawText { get; set; }
    }
}


// ============================================================================
// COMPARISON: CLOUD vs ON-PREMISE
// ============================================================================

namespace Comparison
{
    public class VeryfiVsIronOcrComparison
    {
        public void Compare()
        {
            Console.WriteLine("=== VERYFI vs IRONOCR ===\n");

            Console.WriteLine("Feature          | Veryfi         | IronOCR");
            Console.WriteLine("─────────────────────────────────────────────");
            Console.WriteLine("Data location    | Veryfi cloud   | Your machine");
            Console.WriteLine("Internet needed  | Yes            | No");
            Console.WriteLine("Per-doc cost     | Yes            | No");
            Console.WriteLine("Pre-built ML     | Yes            | OCR + patterns");
            Console.WriteLine("Custom docs      | Limited        | Full control");
            Console.WriteLine("HIPAA suitable   | Review needed  | Yes (local)");
            Console.WriteLine("Air-gapped       | No             | Yes");
            Console.WriteLine();
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }

        public void SecurityConsiderations()
        {
            Console.WriteLine("=== SECURITY COMPARISON ===\n");

            Console.WriteLine("VERYFI (Cloud):");
            Console.WriteLine("  - Documents leave your infrastructure");
            Console.WriteLine("  - Third-party handles your data");
            Console.WriteLine("  - SOC 2 compliant");
            Console.WriteLine("  - May not meet all compliance needs");
            Console.WriteLine();

            Console.WriteLine("IRONOCR (On-Premise):");
            Console.WriteLine("  - All processing stays local");
            Console.WriteLine("  - No data transmission");
            Console.WriteLine("  - HIPAA, GDPR, ITAR compatible");
            Console.WriteLine("  - Works in air-gapped networks");
            Console.WriteLine();
            Console.WriteLine("Download IronOCR: https://ironsoftware.com/csharp/ocr/");
        }

        public void CostAnalysis()
        {
            Console.WriteLine("=== COST ANALYSIS (1000 docs/month) ===\n");

            Console.WriteLine("VERYFI:");
            Console.WriteLine("  - $0.10+ per document");
            Console.WriteLine("  - 1000 docs = $100+/month");
            Console.WriteLine("  - Annual: $1,200+");
            Console.WriteLine();

            Console.WriteLine("IRONOCR:");
            Console.WriteLine("  - One-time license: $749-2,999");
            Console.WriteLine("  - Unlimited documents");
            Console.WriteLine("  - Pays for itself in months");
            Console.WriteLine();
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// WANT ON-PREMISE DOCUMENT PROCESSING?
//
// IronOCR keeps your data local while giving you powerful OCR capabilities.
// Build custom extraction logic that fits your exact needs.
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Free Trial: https://ironsoftware.com/csharp/ocr/#trial-license
// Documentation: https://ironsoftware.com/csharp/ocr/docs/
//
// Install: Install-Package IronOcr
// ============================================================================
