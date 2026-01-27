/**
 * Mindee to IronOCR Migration Comparison Examples
 *
 * Side-by-side before/after examples showing migration patterns
 * from Mindee cloud API to IronOCR local processing.
 *
 * KEY MIGRATION BENEFITS:
 * - Data stays on your infrastructure (no cloud transmission)
 * - No per-page pricing (unlimited processing)
 * - Works offline (no internet dependency)
 * - Process any document type (not limited to Mindee's pre-built APIs)
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * NuGet Packages:
 * - Mindee (Mindee cloud SDK) - https://www.nuget.org/packages/Mindee/
 * - IronOcr (IronOCR) - https://www.nuget.org/packages/IronOcr/
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// ============================================================================
// MIGRATION EXAMPLE 1: INVOICE EXTRACTION
// Cloud API vs Local OCR + Pattern Matching
// ============================================================================

namespace MigrationExamples
{
    // ------------------------------------------------------------------------
    // BEFORE: Mindee Cloud Invoice Extraction
    // Document is uploaded to Mindee servers
    // ------------------------------------------------------------------------

    /*
    using Mindee;
    using Mindee.Input;
    using Mindee.Product.Invoice;

    public class MindeeInvoiceProcessor
    {
        private readonly MindeeClient _client;

        public MindeeInvoiceProcessor(string apiKey)
        {
            _client = new MindeeClient(apiKey);
        }

        /// <summary>
        /// CLOUD PROCESSING: Document uploaded to Mindee
        /// - Bank account numbers transmitted
        /// - Vendor/customer names exposed
        /// - Transaction amounts visible to Mindee
        /// </summary>
        public async Task<InvoiceData> ExtractInvoiceAsync(string filePath)
        {
            // Document leaves your infrastructure HERE
            var inputSource = new LocalInputSource(filePath);
            var response = await _client.ParseAsync<InvoiceV4>(inputSource);

            var prediction = response.Document.Inference.Prediction;

            return new InvoiceData
            {
                InvoiceNumber = prediction.InvoiceNumber?.Value,
                Date = prediction.InvoiceDate?.Value,
                VendorName = prediction.SupplierName?.Value,
                CustomerName = prediction.CustomerName?.Value,
                Subtotal = prediction.TotalNet?.Value,
                Tax = prediction.TotalTax?.Value,
                Total = prediction.TotalAmount?.Value,
                LineItems = prediction.LineItems?.Select(li => new LineItem
                {
                    Description = li.Description,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice,
                    Amount = li.TotalAmount
                }).ToList()
            };
        }
    }
    */

    // ------------------------------------------------------------------------
    // AFTER: IronOCR Local Invoice Extraction
    // Document never leaves your infrastructure
    // ------------------------------------------------------------------------

    using IronOcr;

    public class IronOcrInvoiceProcessor
    {
        private readonly IronTesseract _ocr;

        public IronOcrInvoiceProcessor()
        {
            _ocr = new IronTesseract();
        }

        /// <summary>
        /// LOCAL PROCESSING: All data stays on your machine
        /// - Bank account numbers never transmitted
        /// - Full data sovereignty
        /// - No per-page costs
        /// </summary>
        public InvoiceData ExtractInvoice(string filePath)
        {
            // ALL processing happens locally
            var result = _ocr.Read(filePath);
            var text = result.Text;

            return new InvoiceData
            {
                InvoiceNumber = ExtractInvoiceNumber(text),
                Date = ExtractDate(text),
                VendorName = ExtractVendorName(result),
                CustomerName = ExtractCustomerName(text),
                Subtotal = ExtractAmount(text, @"Subtotal\s*:?\s*\$?([\d,]+\.?\d*)"),
                Tax = ExtractAmount(text, @"Tax\s*:?\s*\$?([\d,]+\.?\d*)"),
                Total = ExtractAmount(text, @"Total\s*(?:Due)?\s*:?\s*\$?([\d,]+\.?\d*)"),
                LineItems = ExtractLineItems(result)
            };
        }

        private string ExtractInvoiceNumber(string text)
        {
            var patterns = new[]
            {
                @"Invoice\s*#?\s*:?\s*([A-Z0-9]+-?\d+)",
                @"Invoice\s*Number\s*:?\s*([A-Z0-9]+-?\d+)",
                @"Inv\s*No\.?\s*:?\s*(\w+\d+)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                    return match.Groups[1].Value;
            }

            return null;
        }

        private DateTime? ExtractDate(string text)
        {
            var patterns = new[]
            {
                @"Date\s*:?\s*(\d{1,2}[/-]\d{1,2}[/-]\d{2,4})",
                @"Invoice\s*Date\s*:?\s*(\d{1,2}[/-]\d{1,2}[/-]\d{2,4})",
                @"Date\s*:?\s*(\w+\s+\d{1,2},?\s+\d{4})"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success && DateTime.TryParse(match.Groups[1].Value, out var date))
                    return date;
            }

            return null;
        }

        private string ExtractVendorName(OcrResult result)
        {
            // Vendor name typically in top portion
            // TODO: verify IronOCR API for page height
            var maxY = result.Lines.Max(l => l.Y + l.Height);
            var topLines = result.Lines
                .Where(l => l.Y < maxY * 0.15)
                .OrderBy(l => l.Y)
                .ToList();

            // First substantial line is often company name
            return topLines.FirstOrDefault(l => l.Text.Length > 5)?.Text;
        }

        private string ExtractCustomerName(string text)
        {
            var patterns = new[]
            {
                @"Bill\s*To\s*:?\s*\n?(.+)",
                @"Customer\s*:?\s*\n?(.+)",
                @"Ship\s*To\s*:?\s*\n?(.+)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var name = match.Groups[1].Value.Trim();
                    // Get first line only
                    return name.Split('\n').FirstOrDefault()?.Trim();
                }
            }

            return null;
        }

        private decimal? ExtractAmount(string text, string pattern)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var value = match.Groups[1].Value.Replace(",", "");
                if (decimal.TryParse(value, out var amount))
                    return amount;
            }

            return null;
        }

        private List<LineItem> ExtractLineItems(OcrResult result)
        {
            var items = new List<LineItem>();

            // Pattern: Description Qty UnitPrice Amount
            var linePattern = @"^(.{10,50}?)\s+(\d+)\s+\$?([\d,]+\.?\d*)\s+\$?([\d,]+\.?\d*)$";

            foreach (var line in result.Lines)
            {
                var match = Regex.Match(line.Text, linePattern);
                if (match.Success)
                {
                    items.Add(new LineItem
                    {
                        Description = match.Groups[1].Value.Trim(),
                        Quantity = double.Parse(match.Groups[2].Value),
                        UnitPrice = decimal.Parse(match.Groups[3].Value.Replace(",", "")),
                        Amount = decimal.Parse(match.Groups[4].Value.Replace(",", ""))
                    });
                }
            }

            return items;
        }
    }


    // ============================================================================
    // MIGRATION EXAMPLE 2: RECEIPT EXTRACTION
    // ============================================================================

    // ------------------------------------------------------------------------
    // BEFORE: Mindee Cloud Receipt Processing
    // ------------------------------------------------------------------------

    /*
    public class MindeeReceiptProcessor
    {
        private readonly MindeeClient _client;

        public async Task<ReceiptData> ExtractReceiptAsync(string filePath)
        {
            // Receipt uploaded to Mindee cloud
            // Merchant details, amounts, location data exposed
            var inputSource = new LocalInputSource(filePath);
            var response = await _client.ParseAsync<ReceiptV5>(inputSource);

            var prediction = response.Document.Inference.Prediction;

            return new ReceiptData
            {
                MerchantName = prediction.SupplierName?.Value,
                MerchantAddress = prediction.SupplierAddress?.Value,
                Date = prediction.Date?.Value,
                Time = prediction.Time?.Value,
                Total = prediction.TotalAmount?.Value,
                Tax = prediction.TotalTax?.Value
            };
        }
    }
    */

    // ------------------------------------------------------------------------
    // AFTER: IronOCR Local Receipt Processing
    // ------------------------------------------------------------------------

    public class IronOcrReceiptProcessor
    {
        private readonly IronTesseract _ocr;

        public IronOcrReceiptProcessor()
        {
            _ocr = new IronTesseract();
        }

        /// <summary>
        /// LOCAL PROCESSING: Receipt data never leaves your system
        /// - Merchant patterns stay private
        /// - Employee expense data protected
        /// - No location tracking exposure
        /// </summary>
        public ReceiptData ExtractReceipt(string filePath)
        {
            var result = _ocr.Read(filePath);
            var text = result.Text;

            return new ReceiptData
            {
                MerchantName = ExtractMerchantName(result),
                MerchantAddress = ExtractAddress(text),
                Date = ExtractDate(text),
                Time = ExtractTime(text),
                Total = ExtractTotal(text),
                Tax = ExtractTax(text),
                Items = ExtractItems(result)
            };
        }

        private string ExtractMerchantName(OcrResult result)
        {
            // Merchant name usually at top of receipt
            return result.Lines.FirstOrDefault()?.Text;
        }

        private string ExtractAddress(string text)
        {
            // Address pattern with street number
            var match = Regex.Match(text, @"(\d+\s+[\w\s]+(?:St|Ave|Rd|Blvd|Dr|Lane|Way)\.?)",
                RegexOptions.IgnoreCase);
            return match.Success ? match.Value : null;
        }

        private DateTime? ExtractDate(string text)
        {
            var match = Regex.Match(text, @"(\d{1,2}[/-]\d{1,2}[/-]\d{2,4})");
            if (match.Success && DateTime.TryParse(match.Value, out var date))
                return date;
            return null;
        }

        private string ExtractTime(string text)
        {
            var match = Regex.Match(text, @"(\d{1,2}:\d{2}(?::\d{2})?\s*(?:AM|PM)?)",
                RegexOptions.IgnoreCase);
            return match.Success ? match.Value : null;
        }

        private decimal? ExtractTotal(string text)
        {
            var patterns = new[]
            {
                @"Total\s*:?\s*\$?([\d,]+\.?\d*)",
                @"Amount\s*Due\s*:?\s*\$?([\d,]+\.?\d*)",
                @"Grand\s*Total\s*:?\s*\$?([\d,]+\.?\d*)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success && decimal.TryParse(
                    match.Groups[1].Value.Replace(",", ""), out var total))
                    return total;
            }

            return null;
        }

        private decimal? ExtractTax(string text)
        {
            var match = Regex.Match(text, @"Tax\s*:?\s*\$?([\d,]+\.?\d*)",
                RegexOptions.IgnoreCase);
            if (match.Success && decimal.TryParse(
                match.Groups[1].Value.Replace(",", ""), out var tax))
                return tax;
            return null;
        }

        private List<ReceiptItem> ExtractItems(OcrResult result)
        {
            var items = new List<ReceiptItem>();

            // Pattern: Item name followed by price
            var itemPattern = @"^(.+?)\s+\$?([\d,]+\.\d{2})$";

            foreach (var line in result.Lines)
            {
                var match = Regex.Match(line.Text.Trim(), itemPattern);
                if (match.Success)
                {
                    items.Add(new ReceiptItem
                    {
                        Description = match.Groups[1].Value.Trim(),
                        Amount = decimal.Parse(match.Groups[2].Value.Replace(",", ""))
                    });
                }
            }

            return items;
        }
    }


    // ============================================================================
    // MIGRATION EXAMPLE 3: BATCH PROCESSING
    // Cost comparison: Per-page vs Unlimited
    // ============================================================================

    // ------------------------------------------------------------------------
    // BEFORE: Mindee Batch Processing
    // Each document = API call = Cost
    // ------------------------------------------------------------------------

    /*
    public class MindeeBatchProcessor
    {
        private readonly MindeeClient _client;

        /// <summary>
        /// COST IMPACT: 1000 invoices = 1000 API calls
        /// At $0.05/page (Starter tier overage): $50
        /// At $0.10/page (Pro tier overage): $100
        /// </summary>
        public async Task<List<InvoiceData>> ProcessBatchAsync(
            IEnumerable<string> files)
        {
            var results = new List<InvoiceData>();

            foreach (var file in files)
            {
                // Each call:
                // 1. Uploads document to Mindee
                // 2. Incurs per-page charge
                // 3. Requires internet connectivity
                var result = await ProcessSingleInvoiceAsync(file);
                results.Add(result);

                // Must respect rate limits
                await Task.Delay(100);
            }

            return results;
        }
    }
    */

    // ------------------------------------------------------------------------
    // AFTER: IronOCR Batch Processing
    // Unlimited processing, no per-document cost
    // ------------------------------------------------------------------------

    public class IronOcrBatchProcessor
    {
        private readonly IronTesseract _ocr;
        private readonly IronOcrInvoiceProcessor _processor;

        public IronOcrBatchProcessor()
        {
            _ocr = new IronTesseract();
            _processor = new IronOcrInvoiceProcessor();
        }

        /// <summary>
        /// COST IMPACT: 1000 invoices = $0 additional
        /// - One-time license covers unlimited processing
        /// - No rate limits
        /// - No internet required
        /// - Process in parallel for speed
        /// </summary>
        public List<InvoiceData> ProcessBatch(IEnumerable<string> files)
        {
            var results = new List<InvoiceData>();

            // Can parallelize without rate limit concerns
            Parallel.ForEach(files, file =>
            {
                var result = _processor.ExtractInvoice(file);
                lock (results)
                {
                    results.Add(result);
                }
            });

            return results;
        }

        /// <summary>
        /// Process with progress reporting
        /// </summary>
        public IEnumerable<InvoiceData> ProcessBatchWithProgress(
            IEnumerable<string> files,
            IProgress<int> progress)
        {
            var fileList = files.ToList();
            var processed = 0;

            foreach (var file in fileList)
            {
                yield return _processor.ExtractInvoice(file);

                processed++;
                progress?.Report((int)((double)processed / fileList.Count * 100));
            }
        }
    }


    // ============================================================================
    // MIGRATION EXAMPLE 4: OFFLINE PROCESSING
    // Cloud dependency vs Local capability
    // ============================================================================

    // ------------------------------------------------------------------------
    // BEFORE: Mindee - Internet Required
    // ------------------------------------------------------------------------

    /*
    public class MindeeOfflineIssue
    {
        private readonly MindeeClient _client;

        /// <summary>
        /// OFFLINE: Not possible
        /// - Every call requires internet
        /// - Network outages halt processing
        /// - Air-gapped environments not supported
        /// - Latency added to every operation
        /// </summary>
        public async Task<InvoiceData> ProcessInvoiceAsync(string file)
        {
            // This will throw if no internet
            try
            {
                var inputSource = new LocalInputSource(file);
                var response = await _client.ParseAsync<InvoiceV4>(inputSource);
                // ...
            }
            catch (HttpRequestException ex)
            {
                // No offline fallback available
                throw new Exception("Mindee requires internet connectivity", ex);
            }
        }
    }
    */

    // ------------------------------------------------------------------------
    // AFTER: IronOCR - Full Offline Support
    // ------------------------------------------------------------------------

    public class IronOcrOfflineCapable
    {
        private readonly IronTesseract _ocr;
        private readonly IronOcrInvoiceProcessor _processor;

        public IronOcrOfflineCapable()
        {
            _ocr = new IronTesseract();
            _processor = new IronOcrInvoiceProcessor();
        }

        /// <summary>
        /// OFFLINE: Fully supported
        /// - All processing local
        /// - No network dependency
        /// - Air-gapped deployment ready
        /// - Field operations supported
        /// - No latency from network calls
        /// </summary>
        public InvoiceData ProcessInvoice(string file)
        {
            // Works with no internet, always
            return _processor.ExtractInvoice(file);
        }

        /// <summary>
        /// Deployment in restricted environments
        /// </summary>
        public class AirGappedDeployment
        {
            public void ProcessInAirGappedEnvironment()
            {
                // IronOCR works in:
                // - Government secure facilities
                // - Military installations
                // - Financial trading floors
                // - Healthcare facilities
                // - Manufacturing plants
                // - Ships, aircraft, remote locations

                var ocr = new IronTesseract();
                var result = ocr.Read("classified_document.pdf");

                // Data never leaves the secure perimeter
                Console.WriteLine($"Processed: {result.Text.Length} characters");
            }
        }
    }


    // ============================================================================
    // MIGRATION EXAMPLE 5: CUSTOM DOCUMENT TYPES
    // Pre-built API limitation vs Universal processing
    // ============================================================================

    // ------------------------------------------------------------------------
    // BEFORE: Mindee - Limited to Pre-built APIs
    // ------------------------------------------------------------------------

    /*
    public class MindeeCustomDocumentLimitation
    {
        /// <summary>
        /// MINDEE LIMITATION: Only pre-built document types
        /// - Invoices: Yes
        /// - Receipts: Yes
        /// - Passports: Yes
        /// - Contracts: NO - requires custom training
        /// - Medical forms: NO - requires custom training
        /// - Custom documents: Requires Enterprise plan + training
        /// </summary>
        public void ShowLimitations()
        {
            // These work:
            // client.ParseAsync<InvoiceV4>(input);
            // client.ParseAsync<ReceiptV5>(input);
            // client.ParseAsync<PassportV1>(input);

            // These DON'T exist without custom training:
            // client.ParseAsync<ContractV1>(input);     // Not available
            // client.ParseAsync<MedicalFormV1>(input);  // Not available
            // client.ParseAsync<ShippingLabelV1>(input); // Not available
        }
    }
    */

    // ------------------------------------------------------------------------
    // AFTER: IronOCR - Process Any Document Type
    // ------------------------------------------------------------------------

    public class IronOcrUniversalDocumentProcessor
    {
        private readonly IronTesseract _ocr;

        public IronOcrUniversalDocumentProcessor()
        {
            _ocr = new IronTesseract();
        }

        /// <summary>
        /// IRONOCR ADVANTAGE: Any document type with same approach
        /// Build extraction logic once, adapt patterns for each type
        /// </summary>

        // INVOICES - Same approach as Mindee covers
        public InvoiceData ProcessInvoice(string file)
        {
            var result = _ocr.Read(file);
            return ExtractInvoiceFields(result);
        }

        // CONTRACTS - Mindee doesn't support without custom training
        public ContractData ProcessContract(string file)
        {
            var result = _ocr.Read(file);
            return new ContractData
            {
                PartyA = ExtractPattern(result.Text, @"between\s+(.+?)\s+and"),
                PartyB = ExtractPattern(result.Text, @"and\s+(.+?)\s*\("),
                EffectiveDate = ExtractDate(result.Text, @"Effective\s+Date\s*:?\s*(.+)"),
                TermLength = ExtractPattern(result.Text, @"Term\s*:?\s*(.+)"),
                ContractValue = ExtractCurrency(result.Text, @"Value\s*:?\s*\$?([\d,]+)")
            };
        }

        // MEDICAL FORMS - Mindee doesn't support without custom training
        public MedicalFormData ProcessMedicalForm(string file)
        {
            var result = _ocr.Read(file);
            return new MedicalFormData
            {
                PatientName = ExtractPattern(result.Text, @"Patient\s*Name\s*:?\s*(.+)"),
                DateOfBirth = ExtractDate(result.Text, @"DOB\s*:?\s*(.+)"),
                MRN = ExtractPattern(result.Text, @"MRN\s*:?\s*(\w+)"),
                Diagnosis = ExtractPattern(result.Text, @"Diagnosis\s*:?\s*(.+)"),
                Provider = ExtractPattern(result.Text, @"Provider\s*:?\s*(.+)")
            };
        }

        // SHIPPING LABELS - Mindee doesn't support without custom training
        public ShippingLabelData ProcessShippingLabel(string file)
        {
            var result = _ocr.Read(file);
            return new ShippingLabelData
            {
                TrackingNumber = ExtractPattern(result.Text, @"Tracking\s*#?\s*:?\s*(\w+)"),
                Sender = ExtractAddress(result.Text, "From"),
                Recipient = ExtractAddress(result.Text, "To"),
                Weight = ExtractPattern(result.Text, @"Weight\s*:?\s*([\d.]+\s*\w+)")
            };
        }

        // HANDWRITTEN NOTES - With ICR mode
        public string ProcessHandwrittenNotes(string file)
        {
            _ocr.Configuration.ReadBarCodes = false;
            // IronOCR handles handwriting
            var result = _ocr.Read(file);
            return result.Text;
        }

        // Helper methods
        private string ExtractPattern(string text, string pattern)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        private DateTime? ExtractDate(string text, string pattern)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && DateTime.TryParse(match.Groups[1].Value, out var date))
                return date;
            return null;
        }

        private decimal? ExtractCurrency(string text, string pattern)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && decimal.TryParse(
                match.Groups[1].Value.Replace(",", ""), out var value))
                return value;
            return null;
        }

        private string ExtractAddress(string text, string label)
        {
            var pattern = $@"{label}\s*:?\s*\n?(.+?\n.+?\n.+?)(?:\n|$)";
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        private InvoiceData ExtractInvoiceFields(OcrResult result)
        {
            // Reuse invoice extraction logic
            return new InvoiceData
            {
                InvoiceNumber = ExtractPattern(result.Text, @"Invoice\s*#?\s*:?\s*(\w+)"),
                Total = ExtractCurrency(result.Text, @"Total\s*:?\s*\$?([\d,]+\.?\d*)")
            };
        }
    }


    // ============================================================================
    // DATA MODELS
    // ============================================================================

    public class InvoiceData
    {
        public string InvoiceNumber { get; set; }
        public DateTime? Date { get; set; }
        public string VendorName { get; set; }
        public string CustomerName { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? Tax { get; set; }
        public decimal? Total { get; set; }
        public List<LineItem> LineItems { get; set; } = new List<LineItem>();
    }

    public class LineItem
    {
        public string Description { get; set; }
        public double? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? Amount { get; set; }
    }

    public class ReceiptData
    {
        public string MerchantName { get; set; }
        public string MerchantAddress { get; set; }
        public DateTime? Date { get; set; }
        public string Time { get; set; }
        public decimal? Total { get; set; }
        public decimal? Tax { get; set; }
        public List<ReceiptItem> Items { get; set; } = new List<ReceiptItem>();
    }

    public class ReceiptItem
    {
        public string Description { get; set; }
        public decimal? Amount { get; set; }
    }

    public class ContractData
    {
        public string PartyA { get; set; }
        public string PartyB { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public string TermLength { get; set; }
        public decimal? ContractValue { get; set; }
    }

    public class MedicalFormData
    {
        public string PatientName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string MRN { get; set; }
        public string Diagnosis { get; set; }
        public string Provider { get; set; }
    }

    public class ShippingLabelData
    {
        public string TrackingNumber { get; set; }
        public string Sender { get; set; }
        public string Recipient { get; set; }
        public string Weight { get; set; }
    }
}


// ============================================================================
// MIGRATION SUMMARY
// ============================================================================
//
// BEFORE (Mindee):
// - Documents uploaded to cloud
// - Per-page costs
// - Internet required
// - Limited to pre-built APIs
// - Vendor lock-in
// - Rate limits
//
// AFTER (IronOCR):
// - All processing local
// - Unlimited processing (licensed)
// - Works offline
// - Any document type
// - You own the code
// - No rate limits
//
// MIGRATION STEPS:
// 1. Install-Package IronOcr (remove Mindee)
// 2. Build extraction patterns for your document types
// 3. Remove API key configuration
// 4. Delete network egress rules for Mindee
// 5. Test accuracy with your documents
//
// Get IronOCR: https://ironsoftware.com/csharp/ocr/
// ============================================================================
