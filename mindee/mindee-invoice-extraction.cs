/**
 * Mindee Invoice Extraction Examples
 *
 * This file demonstrates Mindee's invoice and receipt parsing APIs.
 * IMPORTANT: All documents are uploaded to Mindee's cloud servers.
 *
 * Data Privacy Notice:
 * - Bank account numbers transmitted to Mindee
 * - Transaction amounts visible to Mindee
 * - Vendor/customer names exposed
 * - Tax IDs and addresses transmitted
 *
 * Get IronOCR for local processing: https://ironsoftware.com/csharp/ocr/
 * NuGet: Install-Package IronOcr
 *
 * NuGet Packages Required:
 * - Mindee (cloud SDK) - https://www.nuget.org/packages/Mindee/
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// ============================================================================
// MINDEE INVOICE EXTRACTION
// Documents are uploaded to Mindee cloud for processing
// ============================================================================

namespace MindeeInvoiceExamples
{
    /*
    using Mindee;
    using Mindee.Http;
    using Mindee.Input;
    using Mindee.Product.Invoice;
    using Mindee.Product.Receipt;
    using Mindee.Product.FinancialDocument;

    /// <summary>
    /// Demonstrates Mindee invoice extraction with data privacy annotations.
    /// Every method that calls Mindee sends your document to external servers.
    /// </summary>
    public class MindeeInvoiceService
    {
        private readonly MindeeClient _client;

        public MindeeInvoiceService(string apiKey)
        {
            // API key authenticates with Mindee cloud
            _client = new MindeeClient(apiKey);
        }

        // ========================================================================
        // BASIC INVOICE PARSING
        // ========================================================================

        /// <summary>
        /// Parse an invoice document.
        /// WARNING: Document is uploaded to Mindee cloud servers.
        /// Sensitive data transmitted: vendor name, bank details, amounts, tax IDs
        /// </summary>
        public async Task<InvoiceResult> ParseInvoiceAsync(string filePath)
        {
            // *** DOCUMENT UPLOAD OCCURS HERE ***
            // The entire invoice image/PDF leaves your infrastructure
            var inputSource = new LocalInputSource(filePath);

            // This call transmits your document to Mindee
            var response = await _client.ParseAsync<InvoiceV4>(inputSource);

            var prediction = response.Document.Inference.Prediction;

            // Map Mindee response to our domain object
            return new InvoiceResult
            {
                // Document identification
                InvoiceNumber = prediction.InvoiceNumber?.Value,
                InvoiceDate = prediction.InvoiceDate?.Value,
                DueDate = prediction.DueDate?.Value,

                // Vendor information (TRANSMITTED TO MINDEE)
                VendorName = prediction.SupplierName?.Value,
                VendorAddress = prediction.SupplierAddress?.Value,
                VendorPhone = prediction.SupplierPhoneNumber?.Value,
                VendorEmail = prediction.SupplierEmail?.Value,
                // TAX ID transmitted to third party
                VendorTaxId = prediction.SupplierCompanyRegistrations?
                    .FirstOrDefault()?.Value,

                // Customer information (TRANSMITTED TO MINDEE)
                CustomerName = prediction.CustomerName?.Value,
                CustomerAddress = prediction.CustomerAddress?.Value,
                CustomerCompanyRegistration = prediction.CustomerCompanyRegistrations?
                    .FirstOrDefault()?.Value,

                // Financial data (HIGHLY SENSITIVE - TRANSMITTED)
                Subtotal = prediction.TotalNet?.Value,
                TaxAmount = prediction.TotalTax?.Value,
                TotalAmount = prediction.TotalAmount?.Value,
                Currency = prediction.Locale?.Currency,

                // Payment information (VERY SENSITIVE - TRANSMITTED)
                // Bank account details leave your infrastructure
                PaymentDetails = prediction.SupplierPaymentDetails?
                    .Select(pd => new PaymentDetail
                    {
                        Iban = pd.Iban,           // Bank account transmitted
                        Swift = pd.Swift,         // Bank identifier transmitted
                        RoutingNumber = pd.RoutingNumber,
                        AccountNumber = pd.AccountNumber
                    }).ToList(),

                // Line items (business intelligence exposed)
                LineItems = prediction.LineItems?
                    .Select(li => new InvoiceLineItem
                    {
                        Description = li.Description,
                        Quantity = li.Quantity,
                        UnitPrice = li.UnitPrice,
                        TotalAmount = li.TotalAmount,
                        TaxRate = li.TaxRate,
                        ProductCode = li.ProductCode
                    }).ToList(),

                // Confidence scores
                OverallConfidence = CalculateAverageConfidence(prediction)
            };
        }

        // ========================================================================
        // RECEIPT PARSING
        // ========================================================================

        /// <summary>
        /// Parse a receipt document.
        /// WARNING: Receipt data uploaded to Mindee including:
        /// - Merchant information
        /// - Transaction amounts
        /// - Potentially partial credit card numbers
        /// - Location data (store address)
        /// </summary>
        public async Task<ReceiptResult> ParseReceiptAsync(string filePath)
        {
            // *** DOCUMENT UPLOAD OCCURS HERE ***
            var inputSource = new LocalInputSource(filePath);

            // Receipt image transmitted to Mindee cloud
            var response = await _client.ParseAsync<ReceiptV5>(inputSource);

            var prediction = response.Document.Inference.Prediction;

            return new ReceiptResult
            {
                // Merchant data (location tracking exposure)
                MerchantName = prediction.SupplierName?.Value,
                MerchantAddress = prediction.SupplierAddress?.Value,
                MerchantPhone = prediction.SupplierPhoneNumber?.Value,

                // Transaction data (financial exposure)
                Date = prediction.Date?.Value,
                Time = prediction.Time?.Value,
                TotalAmount = prediction.TotalAmount?.Value,
                TotalTax = prediction.TotalTax?.Value,
                TotalNet = prediction.TotalNet?.Value,
                Tip = prediction.Tip?.Value,

                // Category classification
                Category = prediction.Category?.Value,
                Subcategory = prediction.Subcategory?.Value,

                // Line items
                LineItems = prediction.LineItems?
                    .Select(li => new ReceiptLineItem
                    {
                        Description = li.Description,
                        Quantity = li.Quantity,
                        TotalAmount = li.TotalAmount
                    }).ToList()
            };
        }

        // ========================================================================
        // BATCH PROCESSING
        // ========================================================================

        /// <summary>
        /// Process multiple invoices.
        /// WARNING: Each document is uploaded to Mindee.
        /// Total data exposure multiplied by document count.
        /// </summary>
        public async Task<List<InvoiceResult>> ProcessInvoiceBatchAsync(
            IEnumerable<string> filePaths)
        {
            var results = new List<InvoiceResult>();

            foreach (var path in filePaths)
            {
                try
                {
                    // Each iteration uploads a document
                    var result = await ParseInvoiceAsync(path);
                    results.Add(result);
                }
                catch (MindeeException ex)
                {
                    // API error - document may still have been received
                    Console.WriteLine($"Mindee error for {path}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {path}: {ex.Message}");
                }

                // Respect rate limits
                await Task.Delay(100);
            }

            return results;
        }

        // ========================================================================
        // ERROR HANDLING AND CONFIDENCE CHECKING
        // ========================================================================

        /// <summary>
        /// Parse with confidence validation.
        /// Low confidence may indicate need for manual review.
        /// </summary>
        public async Task<InvoiceResult> ParseWithConfidenceCheckAsync(
            string filePath,
            double minimumConfidence = 0.85)
        {
            var result = await ParseInvoiceAsync(filePath);

            // Check critical field confidence
            if (result.OverallConfidence < minimumConfidence)
            {
                Console.WriteLine($"Warning: Low confidence ({result.OverallConfidence:P0})");
                Console.WriteLine("Document may require manual review");
            }

            return result;
        }

        /// <summary>
        /// Parse with retry logic for transient failures.
        /// Network issues don't mean document wasn't received by Mindee.
        /// </summary>
        public async Task<InvoiceResult> ParseWithRetryAsync(
            string filePath,
            int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await ParseInvoiceAsync(filePath);
                }
                catch (HttpRequestException ex) when (attempt < maxRetries)
                {
                    Console.WriteLine($"Attempt {attempt} failed: {ex.Message}");
                    // Note: Document may have been partially uploaded
                    await Task.Delay(1000 * attempt);
                }
            }

            throw new Exception($"Failed after {maxRetries} attempts");
        }

        // ========================================================================
        // FINANCIAL DOCUMENT (COMBINED INVOICE/RECEIPT)
        // ========================================================================

        /// <summary>
        /// Parse any financial document (invoice or receipt).
        /// Mindee determines document type automatically.
        /// </summary>
        public async Task<FinancialDocumentResult> ParseFinancialDocumentAsync(
            string filePath)
        {
            var inputSource = new LocalInputSource(filePath);

            // Document uploaded for type detection and parsing
            var response = await _client.ParseAsync<FinancialDocumentV1>(inputSource);

            var prediction = response.Document.Inference.Prediction;

            return new FinancialDocumentResult
            {
                DocumentType = prediction.DocumentType?.Value,
                InvoiceNumber = prediction.InvoiceNumber?.Value,
                Date = prediction.Date?.Value,
                TotalAmount = prediction.TotalAmount?.Value,
                VendorName = prediction.SupplierName?.Value,
                CustomerName = prediction.CustomerName?.Value
            };
        }

        // ========================================================================
        // HELPER METHODS
        // ========================================================================

        private double CalculateAverageConfidence(InvoiceV4Document prediction)
        {
            var confidences = new List<double>();

            if (prediction.InvoiceNumber?.Confidence.HasValue == true)
                confidences.Add(prediction.InvoiceNumber.Confidence.Value);
            if (prediction.InvoiceDate?.Confidence.HasValue == true)
                confidences.Add(prediction.InvoiceDate.Confidence.Value);
            if (prediction.TotalAmount?.Confidence.HasValue == true)
                confidences.Add(prediction.TotalAmount.Confidence.Value);
            if (prediction.SupplierName?.Confidence.HasValue == true)
                confidences.Add(prediction.SupplierName.Confidence.Value);

            return confidences.Any() ? confidences.Average() : 0;
        }
    }

    // ========================================================================
    // DATA MODELS
    // ========================================================================

    public class InvoiceResult
    {
        // Document identification
        public string InvoiceNumber { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }

        // Vendor (supplier) information
        public string VendorName { get; set; }
        public string VendorAddress { get; set; }
        public string VendorPhone { get; set; }
        public string VendorEmail { get; set; }
        public string VendorTaxId { get; set; }

        // Customer (buyer) information
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerCompanyRegistration { get; set; }

        // Financial totals
        public decimal? Subtotal { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? TotalAmount { get; set; }
        public string Currency { get; set; }

        // Payment details (HIGHLY SENSITIVE)
        public List<PaymentDetail> PaymentDetails { get; set; }

        // Line items
        public List<InvoiceLineItem> LineItems { get; set; }

        // Confidence
        public double OverallConfidence { get; set; }
    }

    public class PaymentDetail
    {
        public string Iban { get; set; }
        public string Swift { get; set; }
        public string RoutingNumber { get; set; }
        public string AccountNumber { get; set; }
    }

    public class InvoiceLineItem
    {
        public string Description { get; set; }
        public double? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? TotalAmount { get; set; }
        public double? TaxRate { get; set; }
        public string ProductCode { get; set; }
    }

    public class ReceiptResult
    {
        public string MerchantName { get; set; }
        public string MerchantAddress { get; set; }
        public string MerchantPhone { get; set; }
        public DateTime? Date { get; set; }
        public string Time { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? TotalTax { get; set; }
        public decimal? TotalNet { get; set; }
        public decimal? Tip { get; set; }
        public string Category { get; set; }
        public string Subcategory { get; set; }
        public List<ReceiptLineItem> LineItems { get; set; }
    }

    public class ReceiptLineItem
    {
        public string Description { get; set; }
        public double? Quantity { get; set; }
        public decimal? TotalAmount { get; set; }
    }

    public class FinancialDocumentResult
    {
        public string DocumentType { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime? Date { get; set; }
        public decimal? TotalAmount { get; set; }
        public string VendorName { get; set; }
        public string CustomerName { get; set; }
    }
    */
}


// ============================================================================
// DATA PRIVACY SUMMARY
// ============================================================================
//
// When using Mindee, the following sensitive data is transmitted to their servers:
//
// INVOICES:
// - Bank account numbers (IBAN, account number, routing number)
// - Vendor/customer names and addresses
// - Tax identification numbers (EIN, VAT)
// - Transaction amounts (reveals pricing, margins)
// - Line item details (products, quantities, unit prices)
//
// RECEIPTS:
// - Merchant information (reveals where you/employees spend)
// - Transaction amounts
// - Location data (store addresses)
// - Purchase patterns (expense tracking)
// - Time stamps (behavioral data)
//
// CONSIDER LOCAL PROCESSING WITH IRONOCR:
// - Data never leaves your infrastructure
// - No third-party access to sensitive documents
// - No per-page costs
// - Full control over processing
//
// Get IronOCR: https://ironsoftware.com/csharp/ocr/
// NuGet: Install-Package IronOcr
// ============================================================================
