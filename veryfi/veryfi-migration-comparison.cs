/**
 * Veryfi to IronOCR Migration Comparison
 *
 * Side-by-side examples showing Veryfi (cloud) vs IronOCR (on-premise) implementations.
 * Use this file as a reference when migrating from Veryfi to IronOCR.
 *
 * KEY DIFFERENCES:
 * - Veryfi: Cloud processing, per-document costs, structured output
 * - IronOCR: Local processing, one-time license, raw OCR with pattern extraction
 *
 * MIGRATION BENEFITS:
 * - Data sovereignty: All processing stays on your infrastructure
 * - Cost predictability: One-time license vs per-document fees
 * - Offline capability: No internet required for processing
 * - Flexibility: Any document type, not just expenses
 *
 * Get IronOCR: https://ironsoftware.com/csharp/ocr/
 * Install: Install-Package IronOcr
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// ============================================================================
// EXAMPLE 1: RECEIPT EXTRACTION
// Veryfi cloud API vs IronOCR local processing
// ============================================================================

namespace VeryfiMigration.ReceiptExtraction
{
    // ----- BEFORE: Veryfi (Cloud Processing) -----
    /*
    using Veryfi;

    public class VeryfiReceiptProcessor
    {
        private readonly VeryfiClient _client;

        public VeryfiReceiptProcessor(string clientId, string clientSecret,
                                       string username, string apiKey)
        {
            // Four credentials required
            _client = new VeryfiClient(clientId, clientSecret, username, apiKey);
        }

        public async Task<ReceiptResult> ProcessReceiptAsync(string imagePath)
        {
            // ======================================================
            // DOCUMENT UPLOADED TO VERYFI CLOUD HERE
            // Cost: ~$0.05-0.15 per document
            // Data leaves your infrastructure
            // ======================================================
            var bytes = File.ReadAllBytes(imagePath);
            var response = await _client.ProcessDocumentAsync(bytes);

            // Veryfi returns pre-extracted structured data
            return new ReceiptResult
            {
                Vendor = response.Vendor?.Name,
                Date = response.Date,
                Total = response.Total,
                Tax = response.Tax,
                LineItems = response.LineItems?.Select(li => new LineItem
                {
                    Description = li.Description,
                    Quantity = li.Quantity,
                    Price = li.Price
                }).ToList()
            };
        }
    }
    */


    // ----- AFTER: IronOCR (Local Processing) -----
    using IronOcr;

    /// <summary>
    /// IronOCR receipt processor - all processing stays local.
    /// No data transmitted to any external service.
    /// No per-document costs after license purchase.
    /// </summary>
    public class IronOcrReceiptProcessor
    {
        private readonly IronTesseract _ocr;

        public IronOcrReceiptProcessor()
        {
            // No credentials needed - just license key
            _ocr = new IronTesseract();
        }

        public ReceiptResult ProcessReceipt(string imagePath)
        {
            // ======================================================
            // ALL PROCESSING HAPPENS LOCALLY
            // No cloud transmission
            // No per-document cost
            // ======================================================
            var result = _ocr.Read(imagePath);
            var text = result.Text;
            var lines = result.Lines.OrderBy(l => l.Y).ToList();

            // Extract fields using pattern matching
            return new ReceiptResult
            {
                Vendor = ExtractVendor(lines),
                Date = ExtractDate(text),
                Total = ExtractTotal(text),
                Tax = ExtractTax(text),
                LineItems = ExtractLineItems(lines)
            };
        }

        private string ExtractVendor(List<OcrLine> lines)
        {
            // Vendor typically first non-empty line
            return lines.Take(3)
                       .Select(l => l.Text.Trim())
                       .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t) && t.Length > 3);
        }

        private DateTime? ExtractDate(string text)
        {
            var patterns = new[]
            {
                @"\d{1,2}/\d{1,2}/\d{4}",
                @"\d{4}-\d{2}-\d{2}",
                @"\d{1,2}-\d{1,2}-\d{4}"
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
            var patterns = new[]
            {
                @"Total:?\s*\$?\s*([\d,]+\.?\d*)",
                @"Grand Total:?\s*\$?\s*([\d,]+\.?\d*)",
                @"Amount Due:?\s*\$?\s*([\d,]+\.?\d*)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success && decimal.TryParse(
                    match.Groups[1].Value.Replace(",", ""), out var total))
                {
                    return total;
                }
            }
            return null;
        }

        private decimal? ExtractTax(string text)
        {
            var match = Regex.Match(text, @"(?:Tax|Sales Tax):?\s*\$?\s*([\d,]+\.?\d*)",
                                    RegexOptions.IgnoreCase);
            if (match.Success && decimal.TryParse(
                match.Groups[1].Value.Replace(",", ""), out var tax))
            {
                return tax;
            }
            return null;
        }

        private List<LineItem> ExtractLineItems(List<OcrLine> lines)
        {
            var items = new List<LineItem>();
            var pricePattern = @"\$?([\d,]+\.\d{2})$";

            foreach (var line in lines)
            {
                var match = Regex.Match(line.Text.Trim(), pricePattern);
                if (match.Success)
                {
                    var description = line.Text.Substring(0, match.Index).Trim();
                    if (!string.IsNullOrWhiteSpace(description)
                        && !IsMetadataLine(description))
                    {
                        items.Add(new LineItem
                        {
                            Description = description,
                            Price = decimal.Parse(match.Groups[1].Value.Replace(",", ""))
                        });
                    }
                }
            }
            return items;
        }

        private bool IsMetadataLine(string text)
        {
            var skip = new[] { "subtotal", "total", "tax", "cash", "credit", "change" };
            return skip.Any(s => text.ToLower().Contains(s));
        }
    }
}


// ============================================================================
// EXAMPLE 2: INVOICE EXTRACTION
// Cloud vs local invoice processing
// ============================================================================

namespace VeryfiMigration.InvoiceExtraction
{
    // ----- BEFORE: Veryfi (Cloud Processing) -----
    /*
    public class VeryfiInvoiceProcessor
    {
        private readonly VeryfiClient _client;

        public async Task<InvoiceResult> ProcessInvoiceAsync(string pdfPath)
        {
            // Invoice data transmitted to Veryfi servers
            // Includes: bank details, vendor info, payment terms
            // Cost: ~$0.10-0.25 per invoice
            var bytes = File.ReadAllBytes(pdfPath);
            var response = await _client.ProcessDocumentAsync(bytes,
                                    categories: new[] { "invoices" });

            return new InvoiceResult
            {
                InvoiceNumber = response.InvoiceNumber,
                VendorName = response.Vendor?.Name,
                Total = response.Total,
                DueDate = response.DueDate,
                PaymentTerms = response.PaymentTerms
            };
        }
    }
    */


    // ----- AFTER: IronOCR (Local Processing) -----
    using IronOcr;

    /// <summary>
    /// Local invoice processing - no data leaves your infrastructure.
    /// </summary>
    public class IronOcrInvoiceProcessor
    {
        private readonly IronTesseract _ocr;

        public IronOcrInvoiceProcessor()
        {
            _ocr = new IronTesseract();
        }

        public InvoiceResult ProcessInvoice(string pdfPath)
        {
            // Process PDF locally - supports multi-page
            using var input = new OcrInput();
            input.LoadPdf(pdfPath);

            var result = _ocr.Read(input);
            var text = result.Text;

            return new InvoiceResult
            {
                InvoiceNumber = ExtractPattern(text, @"Invoice\s*#?\s*:?\s*(\w+[-\w]*)"),
                VendorName = result.Lines.FirstOrDefault()?.Text,
                Total = ExtractCurrency(text, @"(?:Total|Amount Due):?\s*\$?([\d,]+\.?\d*)"),
                DueDate = ExtractDate(text, @"Due\s*(?:Date)?:?\s*(\d{1,2}/\d{1,2}/\d{4})"),
                PaymentTerms = ExtractPattern(text, @"(?:Terms|Payment Terms):?\s*(\w+\s*\d+)")
            };
        }

        private string ExtractPattern(string text, string pattern)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }

        private decimal? ExtractCurrency(string text, string pattern)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && decimal.TryParse(
                match.Groups[1].Value.Replace(",", ""), out var amount))
            {
                return amount;
            }
            return null;
        }

        private DateTime? ExtractDate(string text, string pattern)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && DateTime.TryParse(match.Groups[1].Value, out var date))
                return date;
            return null;
        }
    }
}


// ============================================================================
// EXAMPLE 3: DATA HANDLING COMPARISON
// Cloud transmission vs local processing
// ============================================================================

namespace VeryfiMigration.DataHandling
{
    /// <summary>
    /// Demonstrates the fundamental data handling difference:
    /// - Veryfi: Document bytes leave your infrastructure
    /// - IronOCR: All processing happens locally
    /// </summary>
    public class DataHandlingComparison
    {
        // ----- VERYFI: Cloud Transmission -----
        /*
        public async Task ProcessWithVeryfi(string documentPath)
        {
            var bytes = File.ReadAllBytes(documentPath);

            // AT THIS POINT:
            // - Document bytes are sent over HTTPS to Veryfi servers
            // - Document may be stored temporarily or permanently
            // - Document is processed by Veryfi's infrastructure
            // - You have no control over where/how data is handled
            // - Any breach at Veryfi exposes your documents

            await _veryfiClient.ProcessDocumentAsync(bytes);
        }
        */

        // ----- IRONOCR: Local Processing -----
        public void ProcessWithIronOcr(string documentPath)
        {
            var ocr = new IronTesseract();
            var result = ocr.Read(documentPath);

            // AT THIS POINT:
            // - Document never left your infrastructure
            // - Processing happened on your hardware
            // - No network transmission occurred
            // - You maintain complete data control
            // - No third-party access to your documents

            Console.WriteLine($"Processed locally: {result.Text.Length} characters");
        }

        /// <summary>
        /// Sensitive document processing - shows why local matters
        /// </summary>
        public void ProcessSensitiveDocument(string bankStatementPath)
        {
            var ocr = new IronTesseract();
            var result = ocr.Read(bankStatementPath);

            // Bank statement data stays on your server:
            // - Account numbers never transmitted
            // - Transaction history remains private
            // - Balance information protected
            // - Full audit trail under your control

            var accountNumber = ExtractAccountNumber(result.Text);
            var balance = ExtractBalance(result.Text);

            // Process locally without any cloud dependency
            Console.WriteLine("Bank statement processed securely on-premise");
        }

        private string ExtractAccountNumber(string text)
        {
            var match = Regex.Match(text, @"Account\s*#?\s*:?\s*(\d{4,})", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }

        private decimal? ExtractBalance(string text)
        {
            var match = Regex.Match(text, @"Balance:?\s*\$?([\d,]+\.?\d*)", RegexOptions.IgnoreCase);
            if (match.Success && decimal.TryParse(match.Groups[1].Value.Replace(",", ""), out var bal))
                return bal;
            return null;
        }
    }
}


// ============================================================================
// EXAMPLE 4: COST MODEL COMPARISON
// Per-document API vs unlimited local OCR
// ============================================================================

namespace VeryfiMigration.CostComparison
{
    /// <summary>
    /// Cost comparison between Veryfi's per-document model and IronOCR's license model.
    /// </summary>
    public class CostAnalysis
    {
        public void CalculateCosts(int monthlyDocuments, int years)
        {
            // ----- VERYFI COSTS -----
            decimal veryfiPerDoc = 0.10m;  // Average across document types
            decimal veryfiMonthly = monthlyDocuments * veryfiPerDoc;
            decimal veryfiAnnual = veryfiMonthly * 12;
            decimal veryfiTotal = veryfiAnnual * years;

            Console.WriteLine("=== VERYFI (Per-Document Pricing) ===");
            Console.WriteLine($"Monthly documents: {monthlyDocuments:N0}");
            Console.WriteLine($"Per-document cost: ${veryfiPerDoc:F2}");
            Console.WriteLine($"Monthly cost: ${veryfiMonthly:N0}");
            Console.WriteLine($"Annual cost: ${veryfiAnnual:N0}");
            Console.WriteLine($"{years}-Year total: ${veryfiTotal:N0}");
            Console.WriteLine();

            // ----- IRONOCR COSTS -----
            decimal ironOcrLicense = 3999m;  // Professional license
            decimal ironOcrTotal = ironOcrLicense;  // One-time

            Console.WriteLine("=== IRONOCR (One-Time License) ===");
            Console.WriteLine($"Monthly documents: {monthlyDocuments:N0} (unlimited)");
            Console.WriteLine($"Per-document cost: $0.00");
            Console.WriteLine($"License cost: ${ironOcrLicense:N0} (one-time)");
            Console.WriteLine($"{years}-Year total: ${ironOcrTotal:N0}");
            Console.WriteLine();

            // ----- SAVINGS -----
            decimal savings = veryfiTotal - ironOcrTotal;
            decimal savingsPercent = (savings / veryfiTotal) * 100;

            Console.WriteLine("=== SAVINGS ===");
            Console.WriteLine($"{years}-Year savings: ${savings:N0}");
            Console.WriteLine($"Savings percentage: {savingsPercent:F1}%");
            Console.WriteLine($"Break-even: {Math.Ceiling(ironOcrLicense / veryfiMonthly)} months");
        }

        public void ShowCostScenarios()
        {
            Console.WriteLine("\n=== COST SCENARIOS (3-Year) ===\n");

            var scenarios = new[]
            {
                (name: "Small Business", docs: 5000),
                (name: "Medium Business", docs: 50000),
                (name: "Enterprise", docs: 200000)
            };

            foreach (var (name, docs) in scenarios)
            {
                Console.WriteLine($"--- {name}: {docs:N0} docs/month ---");
                CalculateCosts(docs, 3);
                Console.WriteLine();
            }
        }
    }
}


// ============================================================================
// EXAMPLE 5: OFFLINE PROCESSING
// Cloud dependency vs full offline support
// ============================================================================

namespace VeryfiMigration.OfflineProcessing
{
    /// <summary>
    /// IronOCR works fully offline - no internet required.
    /// Veryfi requires internet for every document.
    /// </summary>
    public class OfflineCapability
    {
        // ----- VERYFI: No Offline Support -----
        /*
        public async Task VeryfiOfflineAttempt(string path)
        {
            // This will FAIL without internet
            // Veryfi requires cloud connectivity for every document

            try
            {
                await _client.ProcessDocumentAsync(File.ReadAllBytes(path));
            }
            catch (HttpRequestException)
            {
                // No network = no processing
                throw new Exception("Veryfi requires internet connection");
            }
        }
        */

        // ----- IRONOCR: Full Offline Support -----
        public string ProcessOffline(string path)
        {
            // Works without any network connection
            var ocr = new IronTesseract();
            var result = ocr.Read(path);

            // Processing happens entirely on local machine
            return result.Text;
        }

        /// <summary>
        /// Field worker scenario - process documents without internet.
        /// </summary>
        public List<ExpenseReport> ProcessFieldExpenses(string[] imagePaths)
        {
            var ocr = new IronTesseract();
            var reports = new List<ExpenseReport>();

            foreach (var path in imagePaths)
            {
                // No internet required - perfect for:
                // - Remote job sites
                // - Field service technicians
                // - Mobile applications
                // - Air-gapped environments

                var result = ocr.Read(path);
                reports.Add(new ExpenseReport
                {
                    ImagePath = path,
                    ExtractedText = result.Text,
                    ProcessedAt = DateTime.Now,
                    ProcessedLocally = true
                });
            }

            return reports;
        }
    }
}


// ============================================================================
// EXAMPLE 6: CUSTOM DOCUMENT TYPES
// Veryfi limitations vs IronOCR flexibility
// ============================================================================

namespace VeryfiMigration.CustomDocuments
{
    /// <summary>
    /// Veryfi is optimized for expense documents only.
    /// IronOCR handles any document type.
    /// </summary>
    public class DocumentFlexibility
    {
        private readonly IronTesseract _ocr;

        public DocumentFlexibility()
        {
            _ocr = new IronTesseract();
        }

        // ----- VERYFI LIMITATIONS -----
        /*
        // Veryfi only handles:
        // - Receipts
        // - Invoices
        // - Checks
        // - Bank statements
        // - W-2s
        // - Business cards

        // These document types get POOR RESULTS or require paid training:
        // - General business documents
        // - Contracts (limited)
        // - Medical records
        // - Technical documents
        // - Forms
        // - ID cards (specialized API)
        */

        // ----- IRONOCR: Any Document -----

        /// <summary>
        /// Process any document type with IronOCR.
        /// </summary>
        public DocumentResult ProcessAnyDocument(string path)
        {
            var result = _ocr.Read(path);

            return new DocumentResult
            {
                Text = result.Text,
                Confidence = result.Confidence,
                PageCount = result.Pages.Count(),
                Words = result.Words.Count()
            };
        }

        /// <summary>
        /// Contract processing - not supported well by Veryfi.
        /// </summary>
        public ContractData ProcessContract(string pdfPath)
        {
            using var input = new OcrInput();
            input.LoadPdf(pdfPath);

            var result = _ocr.Read(input);
            var text = result.Text;

            return new ContractData
            {
                FullText = text,
                Parties = ExtractParties(text),
                EffectiveDate = ExtractDate(text, @"Effective Date:?\s*(\d{1,2}/\d{1,2}/\d{4})"),
                ExpirationDate = ExtractDate(text, @"(?:Expiration|End) Date:?\s*(\d{1,2}/\d{1,2}/\d{4})"),
                ContractValue = ExtractCurrency(text, @"(?:Contract|Total) (?:Value|Amount):?\s*\$?([\d,]+\.?\d*)")
            };
        }

        /// <summary>
        /// Medical form processing - HIPAA concerns with Veryfi.
        /// </summary>
        public MedicalFormData ProcessMedicalForm(string imagePath)
        {
            // Medical data stays LOCAL - no HIPAA concerns about cloud processing
            var result = _ocr.Read(imagePath);
            var text = result.Text;

            return new MedicalFormData
            {
                PatientName = ExtractPattern(text, @"Patient Name:?\s*(.+)"),
                DateOfBirth = ExtractDate(text, @"(?:DOB|Date of Birth):?\s*(\d{1,2}/\d{1,2}/\d{4})"),
                MRN = ExtractPattern(text, @"(?:MRN|Medical Record):?\s*(\w+)"),
                Diagnosis = ExtractPattern(text, @"Diagnosis:?\s*(.+)")
            };
        }

        /// <summary>
        /// Shipping document processing - not in Veryfi's scope.
        /// </summary>
        public ShippingData ProcessShippingDocument(string imagePath)
        {
            var result = _ocr.Read(imagePath);
            var text = result.Text;

            return new ShippingData
            {
                TrackingNumber = ExtractPattern(text, @"(?:Tracking|Track):?\s*#?\s*(\w+)"),
                ShipFrom = ExtractAddress(text, "Ship From"),
                ShipTo = ExtractAddress(text, "Ship To"),
                Weight = ExtractPattern(text, @"Weight:?\s*([\d.]+\s*(?:lbs?|kg))")
            };
        }

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
                match.Groups[1].Value.Replace(",", ""), out var amount))
                return amount;
            return null;
        }

        private List<string> ExtractParties(string text)
        {
            var parties = new List<string>();
            var matches = Regex.Matches(text, @"(?:Party|Between|And)\s*:?\s*([A-Z][^,\n]+)",
                                        RegexOptions.IgnoreCase);
            foreach (Match m in matches)
            {
                parties.Add(m.Groups[1].Value.Trim());
            }
            return parties;
        }

        private string ExtractAddress(string text, string label)
        {
            var pattern = $@"{label}:?\s*(.+?)(?=Ship|$)";
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }
    }
}


// ============================================================================
// SHARED DATA MODELS
// ============================================================================

namespace VeryfiMigration
{
    public class ReceiptResult
    {
        public string Vendor { get; set; }
        public DateTime? Date { get; set; }
        public decimal? Total { get; set; }
        public decimal? Tax { get; set; }
        public List<LineItem> LineItems { get; set; }
    }

    public class LineItem
    {
        public string Description { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? Price { get; set; }
    }

    public class InvoiceResult
    {
        public string InvoiceNumber { get; set; }
        public string VendorName { get; set; }
        public decimal? Total { get; set; }
        public DateTime? DueDate { get; set; }
        public string PaymentTerms { get; set; }
    }

    public class ExpenseReport
    {
        public string ImagePath { get; set; }
        public string ExtractedText { get; set; }
        public DateTime ProcessedAt { get; set; }
        public bool ProcessedLocally { get; set; }
    }

    public class DocumentResult
    {
        public string Text { get; set; }
        public double Confidence { get; set; }
        public int PageCount { get; set; }
        public int Words { get; set; }
    }

    public class ContractData
    {
        public string FullText { get; set; }
        public List<string> Parties { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public decimal? ContractValue { get; set; }
    }

    public class MedicalFormData
    {
        public string PatientName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string MRN { get; set; }
        public string Diagnosis { get; set; }
    }

    public class ShippingData
    {
        public string TrackingNumber { get; set; }
        public string ShipFrom { get; set; }
        public string ShipTo { get; set; }
        public string Weight { get; set; }
    }
}


// ============================================================================
// MIGRATION COMPLETE - NEXT STEPS
//
// 1. Remove Veryfi NuGet package: Uninstall-Package Veryfi
// 2. Add IronOCR NuGet package: Install-Package IronOcr
// 3. Configure IronOCR license key
// 4. Replace Veryfi client calls with IronOCR Read()
// 5. Build field extraction patterns for your document types
// 6. Delete Veryfi credentials from all environments
// 7. Update deployment pipelines
// 8. Track cost savings
//
// Download IronOCR: https://ironsoftware.com/csharp/ocr/
// Documentation: https://ironsoftware.com/csharp/ocr/docs/
// Free Trial: https://ironsoftware.com/csharp/ocr/#trial-license
// ============================================================================
