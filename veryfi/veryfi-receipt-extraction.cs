/**
 * Veryfi Receipt Extraction Examples
 *
 * This file demonstrates Veryfi's cloud API patterns for expense document processing.
 * IMPORTANT: Every document processed with Veryfi is uploaded to their cloud servers.
 *
 * DATA PRIVACY WARNING:
 * - Receipt images transmitted to Veryfi's cloud infrastructure
 * - Sensitive financial data leaves your infrastructure:
 *   - Bank account numbers
 *   - Transaction amounts
 *   - Vendor relationships
 *   - Employee expense patterns
 *   - Payment card information
 *
 * COST WARNING:
 * - Each document incurs a charge (~$0.05-0.30 depending on document type)
 * - 50,000 receipts/month @ $0.10 = $5,000/month = $60,000/year
 *
 * For on-premise processing, see IronOCR alternative: https://ironsoftware.com/csharp/ocr/
 *
 * Install Veryfi: Install-Package Veryfi
 * Install IronOCR: Install-Package IronOcr
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace VeryfiReceiptExamples
{
    // ============================================================================
    // VERYFI CLIENT INITIALIZATION
    // Requires four separate credentials for authentication
    // ============================================================================

    /*
    using Veryfi;

    /// <summary>
    /// Veryfi client requires four credentials:
    /// 1. Client ID - Application identifier
    /// 2. Client Secret - Application secret key
    /// 3. Username - API user identifier
    /// 4. API Key - User-level API authentication
    ///
    /// All four must be securely stored and rotated regularly.
    /// </summary>
    public class VeryfiClientSetup
    {
        private readonly VeryfiClient _client;

        public VeryfiClientSetup(
            string clientId,
            string clientSecret,
            string username,
            string apiKey)
        {
            // Initialize client with all four credentials
            _client = new VeryfiClient(clientId, clientSecret, username, apiKey);
        }

        /// <summary>
        /// Alternative: Load credentials from secure configuration
        /// </summary>
        public VeryfiClientSetup(IConfiguration config)
        {
            _client = new VeryfiClient(
                config["Veryfi:ClientId"],
                config["Veryfi:ClientSecret"],
                config["Veryfi:Username"],
                config["Veryfi:ApiKey"]
            );
        }
    }
    */


    // ============================================================================
    // RECEIPT DOCUMENT PROCESSING
    // Documents are uploaded to Veryfi's cloud for processing
    // ============================================================================

    /*
    public class VeryfiReceiptService
    {
        private readonly VeryfiClient _client;

        public VeryfiReceiptService(VeryfiClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Process a receipt image through Veryfi's cloud API.
        ///
        /// PRIVACY NOTE: The receipt image is uploaded to Veryfi's servers.
        /// This includes any sensitive data visible on the receipt:
        /// - Vendor name and location
        /// - Purchase amounts
        /// - Payment method (last 4 digits of card)
        /// - Date and time of transaction
        /// - Items purchased
        ///
        /// COST: ~$0.05-0.15 per receipt
        /// </summary>
        public async Task<ReceiptData> ProcessReceiptAsync(string imagePath)
        {
            // STEP 1: Read the receipt image
            var imageBytes = File.ReadAllBytes(imagePath);

            // STEP 2: Convert to Base64 for transmission
            var base64Image = Convert.ToBase64String(imageBytes);

            // ======================================================
            // DOCUMENT UPLOADED TO VERYFI CLOUD HERE
            // Your receipt image leaves your infrastructure at this point
            // ======================================================
            var response = await _client.ProcessDocumentAsync(
                fileData: base64Image,
                fileName: Path.GetFileName(imagePath)
            );

            // Extract structured data from response
            return new ReceiptData
            {
                // Vendor information
                VendorName = response.Vendor?.Name,
                VendorAddress = response.Vendor?.Address,
                VendorPhone = response.Vendor?.PhoneNumber,

                // Transaction details
                Date = response.Date,
                Time = response.Time,
                Total = response.Total,
                Subtotal = response.Subtotal,
                Tax = response.Tax,
                Tip = response.Tip,

                // Payment information (sensitive!)
                PaymentType = response.Payment?.Type,
                CardLastFour = response.Payment?.Last4,

                // Line items
                LineItems = response.LineItems?.Select(li => new LineItem
                {
                    Description = li.Description,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice,
                    Total = li.Total,
                    SKU = li.SKU
                }).ToList(),

                // Confidence score
                Confidence = response.ConfidenceScore
            };
        }

        /// <summary>
        /// Batch process multiple receipts.
        ///
        /// COST CALCULATION:
        /// - 100 receipts @ $0.10 = $10 per batch
        /// - Daily batches of 100 = $300/month
        /// - Monthly totals can grow significantly
        /// </summary>
        public async Task<List<ReceiptData>> ProcessReceiptBatchAsync(string[] imagePaths)
        {
            var results = new List<ReceiptData>();

            foreach (var path in imagePaths)
            {
                try
                {
                    // Each call incurs per-document cost
                    // Each call transmits document to cloud
                    var receipt = await ProcessReceiptAsync(path);
                    results.Add(receipt);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to process {path}: {ex.Message}");
                }
            }

            return results;
        }
    }


    // ============================================================================
    // INVOICE DOCUMENT PROCESSING
    // Invoices may contain bank account numbers and payment terms
    // ============================================================================

    public class VeryfiInvoiceService
    {
        private readonly VeryfiClient _client;

        public VeryfiInvoiceService(VeryfiClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Process an invoice document.
        ///
        /// SENSITIVE DATA WARNING - Invoices typically contain:
        /// - Bank account numbers for payment
        /// - Routing numbers
        /// - Payment terms and credit arrangements
        /// - Vendor relationships
        /// - Purchase order numbers
        /// - Tax identification numbers
        ///
        /// COST: ~$0.10-0.25 per invoice
        /// </summary>
        public async Task<InvoiceData> ProcessInvoiceAsync(string pdfPath)
        {
            var pdfBytes = File.ReadAllBytes(pdfPath);

            // ======================================================
            // INVOICE UPLOADED TO VERYFI CLOUD HERE
            // All invoice data leaves your infrastructure
            // Including bank account numbers if present
            // ======================================================
            var response = await _client.ProcessDocumentAsync(
                fileData: pdfBytes,
                fileName: Path.GetFileName(pdfPath),
                categories: new[] { "invoices" }
            );

            return new InvoiceData
            {
                InvoiceNumber = response.InvoiceNumber,
                PurchaseOrderNumber = response.PurchaseOrderNumber,

                // Vendor details
                VendorName = response.Vendor?.Name,
                VendorAddress = response.Vendor?.Address,
                VendorTaxId = response.Vendor?.TaxId,  // SENSITIVE

                // Amounts
                Subtotal = response.Subtotal,
                Tax = response.Tax,
                Total = response.Total,

                // Dates
                InvoiceDate = response.Date,
                DueDate = response.DueDate,
                PaymentTerms = response.PaymentTerms,

                // Banking info (HIGHLY SENSITIVE if present)
                BankAccountNumber = response.BankAccount?.AccountNumber,
                BankRoutingNumber = response.BankAccount?.RoutingNumber,
                BankName = response.BankAccount?.BankName,

                // Line items
                LineItems = response.LineItems?.ToList()
            };
        }
    }


    // ============================================================================
    // BANK STATEMENT PROCESSING
    // Contains highly sensitive financial account information
    // ============================================================================

    public class VerfyBankStatementService
    {
        private readonly VeryfiClient _client;

        /// <summary>
        /// Process bank statement - EXTREMELY SENSITIVE.
        ///
        /// CRITICAL PRIVACY WARNING:
        /// Bank statements contain the most sensitive financial data:
        /// - Full account numbers
        /// - Transaction history
        /// - Balance information
        /// - Wire transfer details
        /// - Recurring payment patterns
        /// - Incoming/outgoing amounts
        ///
        /// COST: ~$0.15-0.30 per statement
        ///
        /// Consider carefully whether cloud processing is appropriate
        /// for bank statement data.
        /// </summary>
        public async Task<BankStatementData> ProcessBankStatementAsync(string pdfPath)
        {
            var pdfBytes = File.ReadAllBytes(pdfPath);

            // ======================================================
            // BANK STATEMENT UPLOADED TO VERYFI CLOUD HERE
            // Full financial history leaves your infrastructure
            // Account numbers, balances, transactions all transmitted
            // ======================================================
            var response = await _client.ProcessDocumentAsync(
                fileData: pdfBytes,
                categories: new[] { "bank_statements" }
            );

            return new BankStatementData
            {
                AccountNumber = response.AccountNumber,  // CRITICAL
                RoutingNumber = response.RoutingNumber,  // CRITICAL
                BankName = response.BankName,
                StatementPeriodStart = response.StartDate,
                StatementPeriodEnd = response.EndDate,
                OpeningBalance = response.OpeningBalance,
                ClosingBalance = response.ClosingBalance,
                Transactions = response.Transactions?.Select(t => new Transaction
                {
                    Date = t.Date,
                    Description = t.Description,
                    Amount = t.Amount,
                    Balance = t.RunningBalance
                }).ToList()
            };
        }
    }


    // ============================================================================
    // CHECK PROCESSING
    // Contains routing and account numbers
    // ============================================================================

    public class VeryfiCheckService
    {
        private readonly VeryfiClient _client;

        /// <summary>
        /// Process a check image.
        ///
        /// SENSITIVE DATA:
        /// - Account number (on check)
        /// - Routing number (on check)
        /// - Payee information
        /// - Amount
        /// - Payer signature (potential fraud vector)
        ///
        /// COST: ~$0.10-0.20 per check
        /// </summary>
        public async Task<CheckData> ProcessCheckAsync(string imagePath)
        {
            var imageBytes = File.ReadAllBytes(imagePath);

            // Check image with bank details leaves infrastructure
            var response = await _client.ProcessDocumentAsync(
                fileData: imageBytes,
                categories: new[] { "checks" }
            );

            return new CheckData
            {
                CheckNumber = response.CheckNumber,
                AccountNumber = response.AccountNumber,  // CRITICAL
                RoutingNumber = response.RoutingNumber,  // CRITICAL
                PayeeName = response.Payee,
                PayerName = response.Payer,
                Amount = response.Amount,
                Date = response.Date,
                Memo = response.Memo
            };
        }
    }


    // ============================================================================
    // ERROR HANDLING
    // Common Veryfi API errors and how to handle them
    // ============================================================================

    public class VeryfiErrorHandling
    {
        private readonly VeryfiClient _client;

        public async Task<ReceiptData> ProcessWithErrorHandlingAsync(string path)
        {
            try
            {
                var bytes = File.ReadAllBytes(path);
                var response = await _client.ProcessDocumentAsync(bytes);

                return MapToReceiptData(response);
            }
            catch (VeryfiApiException ex) when (ex.StatusCode == 401)
            {
                // Authentication failed - check credentials
                throw new Exception("Veryfi authentication failed. Verify all four credentials.", ex);
            }
            catch (VeryfiApiException ex) when (ex.StatusCode == 402)
            {
                // Payment required - out of credits or quota
                throw new Exception("Veryfi quota exceeded. Check billing and plan limits.", ex);
            }
            catch (VeryfiApiException ex) when (ex.StatusCode == 429)
            {
                // Rate limited - too many requests
                throw new Exception("Veryfi rate limit exceeded. Implement retry with backoff.", ex);
            }
            catch (VeryfiApiException ex) when (ex.StatusCode == 500)
            {
                // Server error - Veryfi infrastructure issue
                throw new Exception("Veryfi server error. Document processing unavailable.", ex);
            }
            catch (Exception ex)
            {
                // Network or other error
                throw new Exception($"Failed to process document: {ex.Message}", ex);
            }
        }
    }
    */


    // ============================================================================
    // DATA MODELS
    // ============================================================================

    public class ReceiptData
    {
        public string VendorName { get; set; }
        public string VendorAddress { get; set; }
        public string VendorPhone { get; set; }
        public DateTime? Date { get; set; }
        public string Time { get; set; }
        public decimal? Total { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? Tax { get; set; }
        public decimal? Tip { get; set; }
        public string PaymentType { get; set; }
        public string CardLastFour { get; set; }
        public List<LineItem> LineItems { get; set; }
        public double? Confidence { get; set; }
    }

    public class LineItem
    {
        public string Description { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? Total { get; set; }
        public string SKU { get; set; }
    }

    public class InvoiceData
    {
        public string InvoiceNumber { get; set; }
        public string PurchaseOrderNumber { get; set; }
        public string VendorName { get; set; }
        public string VendorAddress { get; set; }
        public string VendorTaxId { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? Tax { get; set; }
        public decimal? Total { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string PaymentTerms { get; set; }
        public string BankAccountNumber { get; set; }
        public string BankRoutingNumber { get; set; }
        public string BankName { get; set; }
        public List<object> LineItems { get; set; }
    }

    public class BankStatementData
    {
        public string AccountNumber { get; set; }
        public string RoutingNumber { get; set; }
        public string BankName { get; set; }
        public DateTime? StatementPeriodStart { get; set; }
        public DateTime? StatementPeriodEnd { get; set; }
        public decimal? OpeningBalance { get; set; }
        public decimal? ClosingBalance { get; set; }
        public List<Transaction> Transactions { get; set; }
    }

    public class Transaction
    {
        public DateTime? Date { get; set; }
        public string Description { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Balance { get; set; }
    }

    public class CheckData
    {
        public string CheckNumber { get; set; }
        public string AccountNumber { get; set; }
        public string RoutingNumber { get; set; }
        public string PayeeName { get; set; }
        public string PayerName { get; set; }
        public decimal? Amount { get; set; }
        public DateTime? Date { get; set; }
        public string Memo { get; set; }
    }
}


// ============================================================================
// FOR ON-PREMISE PROCESSING WITHOUT DATA TRANSMISSION:
//
// IronOCR processes documents entirely on your infrastructure.
// No document data leaves your servers.
// No per-document costs.
// Full offline capability.
//
// Download: https://ironsoftware.com/csharp/ocr/
// NuGet: Install-Package IronOcr
// Free Trial: https://ironsoftware.com/csharp/ocr/#trial-license
//
// See veryfi-migration-comparison.cs for side-by-side examples
// ============================================================================
