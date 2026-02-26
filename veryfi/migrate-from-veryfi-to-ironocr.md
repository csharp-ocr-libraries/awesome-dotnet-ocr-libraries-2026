# Migrating from Veryfi to IronOCR

This guide walks .NET developers through replacing Veryfi's cloud document processing API with [IronOCR](https://ironsoftware.com/csharp/ocr/), a local OCR library. It covers the package swap, namespace cleanup, and four complete code migration examples focused on the patterns most commonly built around Veryfi: client initialization, region-based field extraction, expense categorization with structured data, and webhook replacement. No prior reading of the comparison article is required.

## Why Migrate from Veryfi

Financial documents flow through Veryfi's pipeline in one direction: out of your infrastructure and into theirs. That architectural fact drives most migrations. Here are the specific pain points that push teams to make the switch.

**Every document call transmits sensitive financial data to a third-party server.** Receipts carry card last-four digits and vendor relationships. Invoices carry bank account numbers, routing numbers, and vendor tax IDs. Bank statements carry full transaction histories. With Veryfi, every single `ProcessDocumentAsync` call uploads those bytes to `api.veryfi.com`, processes them on Veryfi's infrastructure, and returns JSON. Your control over that data ends the moment the HTTP request is sent.

**Four credentials are required and must be kept in sync across every environment.** `VeryfiClient` requires `clientId`, `clientSecret`, `username`, and `apiKey`—four separate secrets to store in configuration, rotate on schedule, inject into CI/CD pipelines, and audit for exposure. A single credential leak breaks authentication for every document processed across the entire application. IronOCR requires one license key string.

**Per-document pricing compounds without ceiling.** Receipts cost approximately $0.05–$0.15 each, invoices $0.10–$0.25, bank statements $0.15–$0.30. At 50,000 documents per month, that is $5,000–$15,000 per month in metered billing with no reduction in year two or year three. The IronOCR Professional license at $2,999 covers unlimited documents on a perpetual basis—the break-even against $5,000 monthly Veryfi spend is under three weeks.

**The API is async-only because the underlying work is remote.** `ProcessDocumentAsync` is not async because processing is computationally long; it is async because the document must travel to a server, queue behind other requests, complete inference, and return a response over the network. Latency is non-deterministic. HTTP 429 rate limiting requires retry logic. HTTP 402 payment failures halt batch processing entirely. HTTP 500 errors on Veryfi's infrastructure take your workflow down with them.

**Veryfi's document scope ends at the expense document boundary.** The trained models return structured fields reliably for receipts, invoices, checks, bank statements, W-2s, and business cards. Outside that list—general business documents, contracts, medical records, shipping documents, custom internal forms—results degrade or require paid custom model training. Organizations that adopt Veryfi for expense automation typically discover within 6–12 months that other teams need OCR for documents Veryfi was not designed to handle.

**Veryfi's proprietary JSON schema couples all extraction logic to a single vendor.** Every line of code that reads `response.Vendor?.Name`, `response.BankAccount?.RoutingNumber`, or `response.LineItems` is code that only works with Veryfi. Switching vendors—or switching to local OCR—means rewriting all extraction logic from scratch.

### The Fundamental Problem

```csharp
// Veryfi: financial data leaves your infrastructure on every call
var client = new VeryfiClient(clientId, clientSecret, username, apiKey); // 4 secrets
var bytes = File.ReadAllBytes("invoice-with-routing-number.pdf");
var response = await client.ProcessDocumentAsync(bytes); // bank details transmitted
var routingNumber = response.BankAccount?.RoutingNumber; // arrived via Veryfi cloud
```

```csharp
// IronOCR: routing numbers never leave your server
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY"; // 1 key
var ocr = new IronTesseract();
using var input = new OcrInput();
input.LoadPdf("invoice-with-routing-number.pdf"); // processed locally
var result = ocr.Read(input);
var routingNumber = Regex.Match(result.Text, @"Routing\s*#?\s*:?\s*(\d{9})").Groups[1].Value;
```

## IronOCR vs Veryfi: Feature Comparison

The table below maps capabilities across both products to support technical evaluation.

| Feature | Veryfi | IronOCR |
|---|---|---|
| **Processing location** | Veryfi cloud servers | Your infrastructure |
| **Deployment model** | Cloud API only | On-premise, Docker, Azure, AWS, Linux |
| **Offline support** | No | Yes |
| **Internet required** | Yes (every document) | No |
| **Data leaves infrastructure** | Yes (every call) | Never |
| **HIPAA-compatible without BAA** | No | Yes |
| **Air-gapped environment support** | Not possible | Fully supported |
| **Pricing model** | Per-document ($0.05–$0.30) | Perpetual license ($749–$2,999) |
| **Credentials required** | 4 (clientId, clientSecret, username, apiKey) | 1 license key |
| **Synchronous API** | No (async only) | Yes |
| **Rate limiting** | Yes (HTTP 429) | None |
| **Document scope** | Receipts, invoices, checks, bank statements, W-2s, business cards | Any document type |
| **Custom document types** | Paid model training required | Any layout via regex/pattern extraction |
| **PDF input** | Yes (byte upload) | Yes (native, local) |
| **Searchable PDF output** | No | Yes (`result.SaveAsSearchablePdf()`) |
| **Region-based OCR** | No | Yes (`CropRectangle`) |
| **Barcode reading** | No | Yes (same OCR pass) |
| **Structured result access** | Pre-parsed JSON fields | Pages, paragraphs, lines, words with coordinates |
| **Confidence scoring** | Per-field (proprietary) | Per-word and overall (`result.Confidence`) |
| **125+ language support** | Limited | Yes (NuGet language packs) |
| **Thread-safe parallel processing** | HTTP concurrency limits apply | Full (one `IronTesseract` per thread) |
| **Unit testing without mocks** | Requires HTTP mocking | Direct local testing |

## Quick Start: Veryfi to IronOCR Migration

### Step 1: Replace NuGet Package

Remove the Veryfi SDK:

```bash
dotnet remove package Veryfi
```

Install IronOCR from [NuGet](https://www.nuget.org/packages/IronOcr):

```bash
dotnet add package IronOcr
```

### Step 2: Update Namespaces

Replace Veryfi namespaces with the IronOCR namespace:

```csharp
// Before (Veryfi)
using Veryfi;
using Veryfi.Models;

// After (IronOCR)
using IronOcr;
using System.Text.RegularExpressions;
```

### Step 3: Initialize License

Add this line once at application startup, before any OCR call:

```csharp
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
```

## Code Migration Examples

### Document Processing Client Replacement

Veryfi services are built around constructor injection of `VeryfiClient`. The four-credential constructor is a natural seam for dependency injection, but it creates four secrets that must be managed and rotated. Replacing this with IronOCR consolidates the credentials to a single license key and moves instantiation of the processing engine into the service class itself.

**Veryfi Approach:**

```csharp
using Veryfi;
using Microsoft.Extensions.Configuration;

public class ExpenseDocumentService
{
    private readonly VeryfiClient _client;

    // Four credentials injected — four secrets to manage, store, rotate
    public ExpenseDocumentService(IConfiguration config)
    {
        _client = new VeryfiClient(
            config["Veryfi:ClientId"],       // secret 1
            config["Veryfi:ClientSecret"],   // secret 2
            config["Veryfi:Username"],       // secret 3
            config["Veryfi:ApiKey"]          // secret 4
        );
    }

    public async Task<string> GetVendorNameAsync(string documentPath)
    {
        var bytes = File.ReadAllBytes(documentPath);
        // Document uploaded to Veryfi on this call
        var response = await _client.ProcessDocumentAsync(bytes);
        return response.Vendor?.Name;
    }

    public async Task<decimal?> GetTotalAsync(string documentPath)
    {
        var bytes = File.ReadAllBytes(documentPath);
        var response = await _client.ProcessDocumentAsync(bytes);
        return response.Total;
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;
using System.Text.RegularExpressions;

public class ExpenseDocumentService
{
    private readonly IronTesseract _ocr;

    // One license key — set once at startup, not per-instance
    public ExpenseDocumentService()
    {
        _ocr = new IronTesseract();
    }

    public string GetVendorName(string documentPath)
    {
        // All processing local — document bytes never leave this server
        var result = _ocr.Read(documentPath);

        // Vendor is typically the first non-whitespace line on a receipt
        return result.Pages[0].Paragraphs
            .OrderBy(p => p.Y)
            .Select(p => p.Text.Trim())
            .FirstOrDefault(t => t.Length > 3);
    }

    public decimal? GetTotal(string documentPath)
    {
        var result = _ocr.Read(documentPath);
        var match = Regex.Match(result.Text,
            @"(?:Total|Grand Total|Amount Due):?\s*\$?\s*([\d,]+\.\d{2})",
            RegexOptions.IgnoreCase);
        return match.Success
            ? decimal.Parse(match.Groups[1].Value.Replace(",", ""))
            : (decimal?)null;
    }
}
```

The constructor change eliminates four configuration entries from every environment: `appsettings.json`, Docker secrets, Azure Key Vault references, and CI/CD pipeline variables. The `IronTesseract` instance is reusable across multiple calls on the same thread. See the [IronTesseract setup guide](https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/) for singleton registration patterns in ASP.NET Core dependency injection containers.

### Receipt Field Extraction with Region-Based OCR

Veryfi extracts receipt fields by running its trained ML models over the entire document image and returning a pre-structured JSON response. IronOCR's equivalent is region-based OCR using `CropRectangle`, which targets specific zones of the receipt image—header zone for vendor, footer zone for totals—rather than running a full-page pass and searching the output for patterns. This is faster for known layouts and more accurate when the area of interest is well-defined.

**Veryfi Approach:**

```csharp
using Veryfi;

public class ReceiptFieldExtractor
{
    private readonly VeryfiClient _client;

    public ReceiptFieldExtractor(VeryfiClient client)
    {
        _client = client;
    }

    public async Task<(string Vendor, decimal? Total, decimal? Tax)>
        ExtractReceiptFieldsAsync(string imagePath)
    {
        var bytes = File.ReadAllBytes(imagePath);

        // Full document uploaded — Veryfi's ML returns structured fields
        var response = await _client.ProcessDocumentAsync(bytes);

        return (
            Vendor: response.Vendor?.Name,
            Total:  response.Total,
            Tax:    response.Tax
        );
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;
using System.Text.RegularExpressions;

public class ReceiptFieldExtractor
{
    private readonly IronTesseract _ocr = new IronTesseract();

    public (string Vendor, decimal? Total, decimal? Tax)
        ExtractReceiptFields(string imagePath)
    {
        // Region 1: Header zone — vendor name typically in top 15% of receipt
        var headerRegion = new CropRectangle(0, 0, 800, 150);
        using var headerInput = new OcrInput();
        headerInput.LoadImage(imagePath, headerRegion);
        headerInput.Deskew();
        var headerResult = _ocr.Read(headerInput);

        // Region 2: Footer zone — totals typically in bottom 20% of receipt
        var footerRegion = new CropRectangle(0, 650, 800, 200);
        using var footerInput = new OcrInput();
        footerInput.LoadImage(imagePath, footerRegion);
        footerInput.DeNoise();
        var footerResult = _ocr.Read(footerInput);

        var vendor = headerResult.Pages[0].Paragraphs
            .OrderBy(p => p.Y)
            .Select(p => p.Text.Trim())
            .FirstOrDefault(t => t.Length > 3);

        var footerText = footerResult.Text;

        var totalMatch = Regex.Match(footerText,
            @"(?:Total|Grand Total):?\s*\$?\s*([\d,]+\.\d{2})",
            RegexOptions.IgnoreCase);

        var taxMatch = Regex.Match(footerText,
            @"(?:Tax|Sales Tax|VAT):?\s*\$?\s*([\d,]+\.\d{2})",
            RegexOptions.IgnoreCase);

        return (
            Vendor: vendor,
            Total: totalMatch.Success
                ? decimal.Parse(totalMatch.Groups[1].Value.Replace(",", ""))
                : (decimal?)null,
            Tax: taxMatch.Success
                ? decimal.Parse(taxMatch.Groups[1].Value.Replace(",", ""))
                : (decimal?)null
        );
    }
}
```

`CropRectangle` takes `(x, y, width, height)` in pixels. Processing only the header and footer zones is faster than a full-page read and avoids false matches from line-item amounts in the receipt body. The [region-based OCR guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-region-of-an-image/) covers coordinate measurement strategies for variable-size documents, and the [region crop example](https://ironsoftware.com/csharp/ocr/examples/net-tesseract-content-area-rectangle-crop/) shows the full pattern.

### Expense Categorization with Structured Paragraph Data

Veryfi returns `response.LineItems` as a pre-structured array of objects with `Description`, `Quantity`, `UnitPrice`, and `Total` already parsed. IronOCR provides the equivalent through `result.Pages[0].Paragraphs` and `result.Lines`, which expose each text block with its X/Y coordinates. Expense categorization logic—deciding whether a line item is a meal, travel, supply, or software charge—operates on the same text either way. The difference is that with IronOCR, the categorization logic is yours to own, tune, and extend without a paid ML retraining cycle.

**Veryfi Approach:**

```csharp
using Veryfi;

public class ExpenseCategorizer
{
    private readonly VeryfiClient _client;

    public ExpenseCategorizer(VeryfiClient client)
    {
        _client = client;
    }

    public async Task<Dictionary<string, decimal>> CategorizeExpensesAsync(string receiptPath)
    {
        var bytes = File.ReadAllBytes(receiptPath);
        var response = await _client.ProcessDocumentAsync(bytes);

        var categories = new Dictionary<string, decimal>();

        // Line items arrive pre-parsed from Veryfi's ML pipeline
        foreach (var item in response.LineItems ?? Enumerable.Empty<dynamic>())
        {
            var category = response.Category ?? "Uncategorized";
            var amount   = (decimal)(item.Total ?? 0m);

            if (!categories.ContainsKey(category))
                categories[category] = 0m;

            categories[category] += amount;
        }

        return categories;
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;
using System.Text.RegularExpressions;

public class ExpenseCategorizer
{
    private readonly IronTesseract _ocr = new IronTesseract();

    // Keyword-based categorization — tune these for your expense policy
    private static readonly Dictionary<string, string[]> CategoryKeywords = new()
    {
        ["Meals & Entertainment"] = new[] { "restaurant", "cafe", "coffee", "lunch", "dinner", "food", "bar" },
        ["Travel"]                = new[] { "airline", "hotel", "uber", "lyft", "taxi", "parking", "gas", "fuel" },
        ["Office Supplies"]       = new[] { "staples", "office depot", "paper", "ink", "toner", "supplies" },
        ["Software & Subscriptions"] = new[] { "adobe", "microsoft", "github", "aws", "azure", "slack" }
    };

    public Dictionary<string, decimal> CategorizeExpenses(string receiptPath)
    {
        var result = _ocr.Read(receiptPath);

        // Use paragraph coordinates to isolate line items
        // Line items typically appear in the middle vertical band of the receipt
        var lineItemParagraphs = result.Pages[0].Paragraphs
            .Where(p => p.Y > 150 && p.Y < 650) // skip header/footer regions
            .OrderBy(p => p.Y)
            .ToList();

        var categories = new Dictionary<string, decimal>();
        var pricePattern = new Regex(@"\$?([\d,]+\.\d{2})$");
        var vendorText   = result.Text.ToLower();

        // Determine top-level category from vendor name
        var topCategory = "Uncategorized";
        foreach (var (cat, keywords) in CategoryKeywords)
        {
            if (keywords.Any(kw => vendorText.Contains(kw)))
            {
                topCategory = cat;
                break;
            }
        }

        // Extract individual line item amounts
        foreach (var para in lineItemParagraphs)
        {
            var priceMatch = pricePattern.Match(para.Text.Trim());
            if (!priceMatch.Success)
                continue;

            if (!decimal.TryParse(priceMatch.Groups[1].Value.Replace(",", ""), out var amount))
                continue;

            // Classify individual items where keywords appear in the description
            var itemCategory = topCategory;
            var descriptionText = para.Text.ToLower();
            foreach (var (cat, keywords) in CategoryKeywords)
            {
                if (keywords.Any(kw => descriptionText.Contains(kw)))
                {
                    itemCategory = cat;
                    break;
                }
            }

            if (!categories.ContainsKey(itemCategory))
                categories[itemCategory] = 0m;

            categories[itemCategory] += amount;
        }

        return categories;
    }
}
```

The `Paragraphs` collection provides the `Y` coordinate of each text block, which makes it straightforward to isolate the vertical zone where line items appear on a standard receipt layout. The [structured data access guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) explains the full hierarchy of `Pages`, `Paragraphs`, `Lines`, `Words`, and `Characters` with their coordinate properties. For receipts with poor scan quality—crumpled paper, low-contrast thermal printing—the [image quality correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/) covers preprocessing filters that improve accuracy before the categorization logic runs.

### Webhook Elimination and Synchronous Batch Replacement

At high document volumes, Veryfi recommends webhook-based notification over polling. The pattern requires a publicly accessible HTTPS endpoint, a webhook secret for signature verification, a queue to hold results until the webhook fires, and retry logic for missed deliveries. This is significant infrastructure for what is ultimately a workaround for the fact that cloud OCR is slow relative to local processing. IronOCR processes synchronously. There is no async gap to bridge with a webhook.

**Veryfi Approach:**

```csharp
using Veryfi;
using Microsoft.AspNetCore.Mvc;

// Veryfi webhook receiver — required for high-volume reliable processing
[ApiController]
[Route("webhooks")]
public class VeryfiWebhookController : ControllerBase
{
    private readonly IDocumentResultQueue _queue;

    public VeryfiWebhookController(IDocumentResultQueue queue)
    {
        _queue = queue;
    }

    [HttpPost("veryfi")]
    public IActionResult ReceiveWebhook([FromBody] VeryfiWebhookPayload payload,
                                        [FromHeader(Name = "X-Veryfi-Token")] string token)
    {
        // Validate webhook signature — prevents spoofed payloads
        if (!IsValidSignature(token, payload))
            return Unauthorized();

        // Enqueue result for async downstream consumption
        _queue.Enqueue(new DocumentResult
        {
            DocumentId = payload.Id,
            Vendor     = payload.Data?.Vendor?.Name,
            Total      = payload.Data?.Total
        });

        return Ok();
    }

    private bool IsValidSignature(string token, VeryfiWebhookPayload payload) =>
        // HMAC validation against webhook secret — infrastructure requirement
        token == ComputeHmac(payload, Environment.GetEnvironmentVariable("VERYFI_WEBHOOK_SECRET"));
}

// Document batch submission — fire and forget, results arrive via webhook
public class VeryfiDocumentBatchSubmitter
{
    private readonly VeryfiClient _client;

    public async Task SubmitBatchAsync(string[] documentPaths)
    {
        foreach (var path in documentPaths)
        {
            var bytes = File.ReadAllBytes(path);
            // Submit — result arrives asynchronously via webhook, not here
            await _client.ProcessDocumentAsync(bytes);
        }
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

// No webhook controller needed — results are synchronous and local
public class DocumentBatchProcessor
{
    // IronTesseract is thread-safe when one instance is created per thread
    public List<DocumentResult> ProcessBatch(string[] documentPaths)
    {
        var results = new ConcurrentBag<DocumentResult>();

        Parallel.ForEach(documentPaths, documentPath =>
        {
            // One IronTesseract per thread — thread-safe pattern
            var ocr    = new IronTesseract();
            var result = ocr.Read(documentPath);

            results.Add(new DocumentResult
            {
                FilePath   = documentPath,
                Vendor     = ExtractVendor(result),
                Total      = ExtractTotal(result.Text),
                Confidence = result.Confidence,
                // Result is available immediately — no queue, no webhook
                ProcessedAt = DateTime.UtcNow
            });
        });

        return results.OrderBy(r => r.FilePath).ToList();
    }

    private string ExtractVendor(OcrResult result)
    {
        // Vendor: first substantive paragraph ordered by vertical position
        return result.Pages[0].Paragraphs
            .OrderBy(p => p.Y)
            .Select(p => p.Text.Trim())
            .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t) && t.Length > 3);
    }

    private decimal? ExtractTotal(string text)
    {
        var match = Regex.Match(text,
            @"(?:Total|Grand Total|Amount Due):?\s*\$?\s*([\d,]+\.\d{2})",
            RegexOptions.IgnoreCase);
        return match.Success
            ? decimal.Parse(match.Groups[1].Value.Replace(",", ""))
            : (decimal?)null;
    }
}

public class DocumentResult
{
    public string FilePath   { get; set; }
    public string Vendor     { get; set; }
    public decimal? Total    { get; set; }
    public double Confidence { get; set; }
    public DateTime ProcessedAt { get; set; }
}
```

Removing the webhook layer eliminates the HTTPS endpoint, the webhook secret rotation requirement, the result queue, the HMAC validation logic, and the retry configuration. The entire downstream plumbing exists only because Veryfi's results arrive asynchronously from a remote server. With IronOCR, `Parallel.ForEach` replaces all of it. The [multithreading example](https://ironsoftware.com/csharp/ocr/examples/csharp-tesseract-multithreading-for-speed/) demonstrates the per-thread `IronTesseract` pattern in detail, and the [async OCR guide](https://ironsoftware.com/csharp/ocr/how-to/async/) covers `Task.Run` integration for UI responsiveness. The [speed optimization guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-fast-configuration/) covers instance configuration for maximum throughput on batch workloads.

## Veryfi API to IronOCR Mapping Reference

| Veryfi | IronOCR Equivalent |
|---|---|
| `new VeryfiClient(clientId, clientSecret, username, apiKey)` | `new IronTesseract()` + `IronOcr.License.LicenseKey = "key"` |
| `_client.ProcessDocumentAsync(bytes)` | `ocr.Read(filePath)` or `ocr.Read(ocrInput)` |
| `_client.ProcessDocumentAsync(bytes, categories: new[] { "invoices" })` | `input.LoadPdf(path); ocr.Read(input)` |
| `_client.ProcessDocumentAsync(bytes, categories: new[] { "bank_statements" })` | `input.LoadPdf(path); ocr.Read(input)` |
| `response.Vendor?.Name` | First paragraph ordered by `p.Y` from `result.Pages[0].Paragraphs` |
| `response.Total` | `Regex.Match(result.Text, @"Total:?\s*\$?([\d,]+\.\d{2})")` |
| `response.Tax` | `Regex.Match(result.Text, @"Tax:?\s*\$?([\d,]+\.\d{2})")` |
| `response.Date` | `Regex.Match(result.Text, @"\d{1,2}/\d{1,2}/\d{4}")` |
| `response.LineItems` | `result.Pages[0].Paragraphs` filtered by Y coordinate range |
| `response.InvoiceNumber` | `Regex.Match(result.Text, @"Invoice\s*#?\s*:?\s*(\w+[-\w]*)")` |
| `response.BankAccount?.AccountNumber` | `Regex.Match(result.Text, @"Account\s*#?\s*:?\s*(\d{4,})")` |
| `response.BankAccount?.RoutingNumber` | `Regex.Match(result.Text, @"Routing\s*#?\s*:?\s*(\d{9})")` |
| `response.ConfidenceScore` | `result.Confidence` (overall) or `word.Confidence` (per-word) |
| `response.Payment?.Last4` | `Regex.Match(result.Text, @"\*{4}\s*(\d{4})")` |
| `VeryfiApiException` (401/402/429/500) | Standard .NET exceptions — no HTTP error codes for local processing |
| Base64 encoding before upload | Not required — `ocr.Read(filePath)` accepts file paths directly |
| `response.Category` | Custom keyword matching against `result.Text` |
| Webhook payload deserialization | Not required — `ocr.Read()` returns result synchronously |
| `ProcessDocumentAsync` with retry/backoff | Not required — no rate limits on local processing |

## Common Migration Issues and Solutions

### Issue 1: Missing Pre-Parsed Fields

**Veryfi:** `response.Vendor?.Name`, `response.Total`, and `response.LineItems` arrive as structured fields from a pre-trained ML model. No extraction logic is required on the client side.

**Solution:** Write Regex patterns for each field your application uses. The migration effort is typically 8–24 hours depending on how many distinct document layouts you process. For common receipt and invoice patterns, the [invoice OCR tutorial](https://ironsoftware.com/csharp/ocr/blog/using-ironocr/invoice-ocr-csharp-tutorial/) and [receipt scanning tutorial](https://ironsoftware.com/csharp/ocr/blog/using-ironocr/receipt-scanning-api-tutorial/) provide full extraction pattern implementations.

```csharp
// Map each Veryfi field to a Regex extraction
private static readonly Dictionary<string, string> FieldPatterns = new()
{
    ["InvoiceNumber"] = @"Invoice\s*#?\s*:?\s*(\w+[-\w]*)",
    ["PurchaseOrder"]  = @"(?:PO|P\.O\.|Purchase Order)\s*#?\s*:?\s*(\w+)",
    ["DueDate"]        = @"Due\s*(?:Date)?:?\s*(\d{1,2}/\d{1,2}/\d{4})",
    ["PaymentTerms"]   = @"(?:Terms|Net)\s*:?\s*(\w+\s*\d+)"
};

public string ExtractField(string text, string fieldName)
{
    if (!FieldPatterns.TryGetValue(fieldName, out var pattern))
        return null;
    var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
    return match.Success ? match.Groups[1].Value.Trim() : null;
}
```

### Issue 2: Async Method Signatures Throughout the Codebase

**Veryfi:** `ProcessDocumentAsync` is async at the Veryfi SDK level. Teams typically propagate `async`/`await` through every calling method up the call stack, meaning service classes, controllers, and background jobs all carry `async Task<T>` signatures.

**Solution:** IronOCR's `Read()` is synchronous. Existing `async` method signatures can be preserved by wrapping with `Task.Run` during the transition period. This avoids mass signature changes across the codebase while still eliminating the cloud dependency.

```csharp
// Preserve async signature during transition — no codebase-wide refactor needed
public async Task<string> GetVendorNameAsync(string documentPath)
{
    return await Task.Run(() =>
    {
        var result = _ocr.Read(documentPath);
        return result.Pages[0].Paragraphs
            .OrderBy(p => p.Y)
            .Select(p => p.Text.Trim())
            .FirstOrDefault(t => t.Length > 3);
    });
}
```

### Issue 3: Credential Configuration Scattered Across Environments

**Veryfi:** Four credentials (`Veryfi:ClientId`, `Veryfi:ClientSecret`, `Veryfi:Username`, `Veryfi:ApiKey`) appear in `appsettings.json`, environment variable blocks in Docker Compose files, GitHub Actions secrets, Azure Key Vault references, and CI/CD pipeline configurations.

**Solution:** Search and remove all four credential entries from every environment. Add a single `IRONOCR_LICENSE_KEY` environment variable. Load it at startup.

```bash
# Find all Veryfi credential references
grep -r "Veryfi:ClientId\|Veryfi:ClientSecret\|Veryfi:Username\|Veryfi:ApiKey" \
    --include="*.json" --include="*.yml" --include="*.yaml" --include="*.env" .
```

```csharp
// Load from environment at startup
IronOcr.License.LicenseKey = Environment.GetEnvironmentVariable("IRONOCR_LICENSE_KEY")
    ?? throw new InvalidOperationException("IRONOCR_LICENSE_KEY not set");
```

### Issue 4: Scan Quality Issues Not Previously Visible

**Veryfi:** Cloud processing includes server-side image enhancement before ML inference runs. Low-quality receipt scans—crumpled paper, faded thermal printing, skewed phone photos—were corrected silently before field extraction.

**Solution:** Apply IronOCR's preprocessing pipeline explicitly. `Deskew()`, `DeNoise()`, and `Contrast()` cover the majority of real-world receipt scan quality problems.

```csharp
using var input = new OcrInput();
input.LoadImage("receipt-phone-photo.jpg");
input.Deskew();        // correct rotation from angled phone capture
input.DeNoise();       // remove compression artifacts
input.Contrast();      // improve faded thermal print
input.Sharpen();       // recover edge detail

var result = _ocr.Read(input);
```

The [image quality correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/) and [image filters tutorial](https://ironsoftware.com/csharp/ocr/tutorials/c-sharp-ocr-image-filters/) cover which filters to apply for specific scan degradation patterns.

### Issue 5: High-Volume Batch Processing Throughput

**Veryfi:** Rate limits throttle document submission velocity. HTTP 429 responses require exponential backoff logic. Throughput is bounded by Veryfi's per-plan rate limit, not by your hardware.

**Solution:** IronOCR is bounded only by CPU cores. Use `Parallel.ForEach` with one `IronTesseract` instance per thread. On an 8-core server, throughput scales roughly linearly with core count.

```csharp
// One IronTesseract per thread — do not share instances across threads
Parallel.ForEach(
    documentPaths,
    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
    path =>
    {
        var ocr    = new IronTesseract();
        var result = ocr.Read(path);
        SaveResult(path, result.Text, result.Confidence);
    });
```

### Issue 6: Proprietary JSON Schema Locked to Veryfi

**Veryfi:** All extraction code reads from Veryfi's response schema: `response.Vendor?.Name`, `response.LineItems`, `response.BankAccount?.RoutingNumber`. This code works only with Veryfi's SDK. Any field name change in a Veryfi API update breaks application code.

**Solution:** IronOCR extraction uses standard .NET `System.Text.RegularExpressions.Regex` against plain text. The patterns are portable, testable without mocking any SDK, and under your control. Unit tests run without any network connection.

```csharp
// Extraction logic that is fully portable and unit-testable
[Fact]
public void ExtractsRoutingNumberFromInvoiceText()
{
    const string sampleText = "Routing Number: 021000021\nAccount: 1234567890";
    var match = Regex.Match(sampleText, @"Routing\s*(?:Number)?:?\s*(\d{9})",
                            RegexOptions.IgnoreCase);
    Assert.True(match.Success);
    Assert.Equal("021000021", match.Groups[1].Value);
}
```

## Veryfi Migration Checklist

### Pre-Migration Tasks

Audit the codebase to inventory all Veryfi usage before touching any code:

```bash
# Find all Veryfi using statements
grep -rn "using Veryfi" --include="*.cs" .

# Find all VeryfiClient instantiations
grep -rn "VeryfiClient\|ProcessDocumentAsync" --include="*.cs" .

# Find all Veryfi response field accesses
grep -rn "response\.Vendor\|response\.Total\|response\.LineItems\|response\.BankAccount" --include="*.cs" .

# Find all credential configuration references
grep -r "Veryfi:ClientId\|Veryfi:ClientSecret\|Veryfi:Username\|Veryfi:ApiKey" \
    --include="*.json" --include="*.yml" --include="*.yaml" --include="*.env" .

# Find all webhook-related code
grep -rn "VeryfiWebhook\|X-Veryfi-Token\|webhook" --include="*.cs" .
```

Record the total count of `ProcessDocumentAsync` call sites, the list of response fields accessed per call site, and the list of environments containing Veryfi credentials.

### Code Update Tasks

1. Remove the `Veryfi` NuGet package from all projects in the solution.
2. Install `IronOcr` NuGet package to all projects that previously referenced `Veryfi`.
3. Add `IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";` to application startup (before any OCR call).
4. Replace all `using Veryfi;` and `using Veryfi.Models;` statements with `using IronOcr;`.
5. Replace `VeryfiClient` constructor injection with `IronTesseract` field initialization.
6. Remove all four Veryfi credential entries from every `appsettings.json`, `appsettings.*.json`, and secrets configuration file.
7. Convert `ProcessDocumentAsync(bytes)` calls to `ocr.Read(filePath)` or `ocr.Read(ocrInput)`.
8. Replace `response.Vendor?.Name` accesses with paragraph-ordered text extraction from `result.Pages[0].Paragraphs`.
9. Replace `response.Total`, `response.Tax`, `response.InvoiceNumber`, and other structured field accesses with Regex patterns against `result.Text`.
10. Replace `response.LineItems` iteration with Y-coordinate-filtered `result.Pages[0].Paragraphs` iteration.
11. Delete webhook controller classes and remove webhook endpoint registrations.
12. Remove webhook secret environment variables from all environments.
13. Add `OcrInput` with preprocessing (`Deskew()`, `DeNoise()`, `Contrast()`) for scanned image inputs.
14. Replace single-threaded sequential loops with `Parallel.ForEach` using one `IronTesseract` per thread.
15. Add `IRONOCR_LICENSE_KEY` to all environment variable configurations and CI/CD secret stores.

### Post-Migration Testing

- Verify that no Veryfi network calls appear in HTTP traffic logs after migration deployment.
- Confirm that extracted vendor names match expected values across a sample set of 20–50 receipts.
- Confirm that extracted totals match expected values within $0.01 tolerance for the same sample set.
- Verify that invoice number extraction succeeds for each invoice format in the document corpus.
- Test batch processing throughput against the baseline Veryfi throughput to confirm rate-limit removal.
- Run the full test suite without any network connection to confirm zero cloud dependency.
- Confirm that `result.Confidence` scores exceed 80% for clean document scans; below 80% indicates a preprocessing step should be added.
- Verify that all four Veryfi credentials have been removed from every environment (dev, staging, production).
- Confirm that webhook endpoints return 404 or have been removed from the routing table.
- Test behavior on low-quality receipt scans (crumpled, faded, skewed) with the preprocessing pipeline active.

## Key Benefits of Migrating to IronOCR

**Financial documents processed locally are documents that cannot be breached at a third party.** After migration, bank account numbers extracted from invoices, routing numbers parsed from checks, and transaction histories read from bank statements are all processed on your hardware. No third-party security incident, subprocessor data access, or Veryfi infrastructure breach can expose documents that never left your servers.

**Per-document costs drop to zero the day the migration deploys.** At 50,000 documents per month, the $5,000–$15,000 monthly Veryfi line item disappears. The one-time IronOCR Professional license at $2,999 is recovered in the first week of the first month. At higher volumes, the savings compound every year without any volume discount negotiation or contract renewal.

**Processing throughput scales with hardware, not with a vendor's rate limits.** HTTP 429 responses, plan-level throughput caps, and seasonal overage charges are architectural artifacts of cloud APIs. With IronOCR, adding CPU cores increases throughput proportionally. A batch of 10,000 receipts processes on your timeline, not on Veryfi's rate limit schedule.

**Any document type processes with the same API.** The organization no longer needs a second OCR tool when HR requests onboarding form processing, legal needs contract text extraction, or operations needs shipping document data. `ocr.Read()` handles all of them. The [reading text from images tutorial](https://ironsoftware.com/csharp/ocr/tutorials/how-to-read-text-from-an-image-in-csharp-net/) and the [specialized document guides](https://ironsoftware.com/csharp/ocr/features/specialized/) cover the full range of document formats IronOCR handles.

**Extraction logic becomes a first-class part of the codebase.** Regex patterns are in source control, reviewable in pull requests, testable in unit tests without mocking any SDK, and tunable based on production feedback. When Veryfi's pre-trained model returns an incorrect vendor name, there is nothing to tune. When IronOCR's extraction pattern returns an incorrect vendor name, the fix is a one-line regex change with a unit test. The [IronOCR licensing page](https://ironsoftware.com/csharp/ocr/licensing/) covers tier options including the SaaS subscription path for teams that prefer annual billing over perpetual purchase.

**Deployment footprint shrinks to a single NuGet package that runs anywhere.** IronOCR installs as one package with no external dependencies, no native binary management, and no tessdata folder configuration. The same package reference resolves on Windows, Linux, macOS, Docker, Azure App Service, and AWS Lambda without platform-conditional code. See the [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/) and [Linux deployment guide](https://ironsoftware.com/csharp/ocr/get-started/linux/) for containerized environments where Veryfi's network egress requirement is a deployment blocker.
