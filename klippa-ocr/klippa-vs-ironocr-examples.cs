/**
 * Klippa OCR vs IronOCR: Code Examples
 *
 * Compare Klippa's cloud document processing with IronOCR.
 * Klippa is cloud-based; IronOCR is on-premise.
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * Klippa: Cloud API for receipts/invoices
 * IronOCR NuGet: https://www.nuget.org/packages/IronOcr/
 */

using System;
using System.Net.Http;
using System.Threading.Tasks;

// ============================================================================
// KLIPPA OCR IMPLEMENTATION - Cloud Document Processing
// ============================================================================

namespace KlippaExamples
{
    /// <summary>
    /// Klippa is a cloud-based document processing API.
    /// Specialized for receipts, invoices, and identity documents.
    /// Data is processed on their servers.
    /// </summary>
    public class KlippaService
    {
        private readonly HttpClient _client;
        private readonly string _apiKey;

        public KlippaService(string apiKey)
        {
            _apiKey = apiKey;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("X-Auth-Key", apiKey);
        }

        /*
        public async Task<ReceiptData> ProcessReceiptAsync(string imagePath)
        {
            // Document uploaded to Klippa cloud
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(File.ReadAllBytes(imagePath)), "document", "receipt.jpg");

            var response = await _client.PostAsync("https://custom-ocr.klippa.com/api/v1/parseDocument", content);
            var json = await response.Content.ReadAsStringAsync();

            // Parse response...
            return new ReceiptData();
        }
        */

        public void ShowConsiderations()
        {
            Console.WriteLine("Klippa Considerations:");
            Console.WriteLine("1. Cloud-only - no on-premise option");
            Console.WriteLine("2. Per-document pricing");
            Console.WriteLine("3. Specialized for receipts/invoices");
            Console.WriteLine("4. Requires internet connection");
            Console.WriteLine("5. Documents sent to EU data centers");
            Console.WriteLine();
            Console.WriteLine("For on-premise OCR, use IronOCR:");
            Console.WriteLine("https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// IRONOCR - ON-PREMISE ALTERNATIVE
// Get it: https://ironsoftware.com/csharp/ocr/
// ============================================================================

namespace IronOcrExamples
{
    using IronOcr;
    using System.Text.RegularExpressions;
    using System.Linq;

    /// <summary>
    /// IronOCR processes locally with no cloud dependency.
    /// Build your own receipt/invoice extraction logic.
    ///
    /// Download: https://ironsoftware.com/csharp/ocr/
    /// </summary>
    public class IronOcrReceiptService
    {
        private readonly IronTesseract _ocr = new IronTesseract();

        /// <summary>
        /// Extract receipt text locally
        /// </summary>
        public string ExtractReceiptText(string imagePath)
        {
            return _ocr.Read(imagePath).Text;
        }

        /// <summary>
        /// Parse receipt data with pattern matching
        /// </summary>
        public ReceiptData ParseReceipt(string imagePath)
        {
            var result = _ocr.Read(imagePath);
            var text = result.Text;

            return new ReceiptData
            {
                MerchantName = result.Lines.FirstOrDefault()?.Text,
                Total = ExtractTotal(text),
                Date = ExtractDate(text),
                RawText = text
            };
        }

        /// <summary>
        /// Invoice processing
        /// </summary>
        public InvoiceData ParseInvoice(string imagePath)
        {
            var result = _ocr.Read(imagePath);
            var text = result.Text;

            return new InvoiceData
            {
                InvoiceNumber = ExtractPattern(text, @"Invoice\s*#?\s*:?\s*(\w+)"),
                VendorName = result.Lines.FirstOrDefault()?.Text,
                Total = ExtractTotal(text),
                DueDate = ExtractDate(text)
            };
        }

        /// <summary>
        /// Batch receipt processing - unlimited
        /// </summary>
        public void BatchProcessReceipts(string[] imagePaths)
        {
            foreach (var path in imagePaths)
            {
                var data = ParseReceipt(path);
                Console.WriteLine($"{path}: {data.MerchantName} - ${data.Total}");
            }
        }

        private decimal? ExtractTotal(string text)
        {
            var patterns = new[]
            {
                @"Total:?\s*\$?([\d,]+\.?\d*)",
                @"Amount:?\s*\$?([\d,]+\.?\d*)",
                @"Sum:?\s*\$?([\d,]+\.?\d*)"
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
            var patterns = new[] { @"\d{1,2}/\d{1,2}/\d{4}", @"\d{4}-\d{2}-\d{2}" };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern);
                if (match.Success && DateTime.TryParse(match.Value, out var date))
                    return date;
            }
            return null;
        }

        private string ExtractPattern(string text, string pattern)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }
    }

    public class ReceiptData
    {
        public string MerchantName { get; set; }
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
    }
}


// ============================================================================
// COMPARISON
// ============================================================================

namespace Comparison
{
    public class KlippaVsIronOcrComparison
    {
        public void Compare()
        {
            Console.WriteLine("=== KLIPPA vs IRONOCR ===\n");

            Console.WriteLine("Feature          | Klippa         | IronOCR");
            Console.WriteLine("─────────────────────────────────────────────");
            Console.WriteLine("Deployment       | Cloud only     | On-premise");
            Console.WriteLine("Data location    | EU cloud       | Your machine");
            Console.WriteLine("Pricing          | Per document   | One-time");
            Console.WriteLine("Internet needed  | Yes            | No");
            Console.WriteLine("Receipt parsing  | Built-in       | DIY + OCR");
            Console.WriteLine("General OCR      | Limited        | Full");
            Console.WriteLine("PDF support      | Yes            | Yes");
            Console.WriteLine("Compliance       | GDPR           | Any");
            Console.WriteLine();
            Console.WriteLine("Get IronOCR: https://ironsoftware.com/csharp/ocr/");
        }
    }
}


// ============================================================================
// NEED ON-PREMISE DOCUMENT PROCESSING?
//
// IronOCR keeps your data local.
// Build custom extraction for any document type.
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: https://www.nuget.org/packages/IronOcr/
// Install: Install-Package IronOcr
// ============================================================================
