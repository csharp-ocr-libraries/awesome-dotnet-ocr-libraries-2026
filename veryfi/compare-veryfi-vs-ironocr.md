Veryfi sends your bank account numbers, routing numbers, transaction amounts, and vendor relationships to a third-party cloud server every time you call `_client.ProcessDocumentAsync(bytes)`. That is the architectural reality that defines the entire Veryfi evaluation: before a single line of structured JSON comes back, your expense documents—containing some of the most sensitive financial data in your organization—have already left your infrastructure. For many teams, that is the end of the evaluation.

## Understanding Veryfi

Veryfi is a cloud-based document intelligence API built exclusively for expense and financial document types. The product's core proposition is structured output: instead of raw OCR text requiring downstream parsing, a Veryfi API call returns pre-extracted fields—vendor name, total, line items, tax, payment card last four, and for invoices, bank account and routing numbers. Models are pre-trained on large volumes of financial documents and claim 99%+ accuracy on their supported document categories.

The architecture is pure cloud. Every document flows out of your application, through HTTPS to Veryfi's servers, through their AI/ML pipeline, and back as JSON. There is no on-premise deployment option, no air-gapped mode, and no way to process a document without transmitting it externally.

Key architectural characteristics:

- **Cloud-only processing:** Every document is uploaded to `api.veryfi.com` with no local processing path
- **Four-credential authentication:** `VeryfiClient` requires `clientId`, `clientSecret`, `username`, and `apiKey`—four secrets to manage, store securely, and rotate
- **Per-document pricing:** Receipts run approximately $0.05–$0.15 each; invoices $0.10–$0.25; bank statements $0.15–$0.30
- **Async-only API:** `ProcessDocumentAsync` is the primary method; there is no synchronous path
- **Specialist document scope:** Receipts, invoices, checks, bank statements, W-2s, and business cards; general documents, medical records, contracts, and technical drawings are outside reliable coverage
- **Proprietary JSON schema:** Veryfi's response format is specific to Veryfi; switching vendors requires rewriting extraction logic
- **Rate limiting:** High-volume processing requires backoff and retry logic to handle HTTP 429 responses

### Veryfi Receipt Processing

The Veryfi SDK exposes a single primary method for document processing:

```csharp
using Veryfi;

public class VeryfiReceiptService
{
    private readonly VeryfiClient _client;

    public VeryfiReceiptService(string clientId, string clientSecret,
                                string username, string apiKey)
    {
        // Four credentials required — four secrets to manage
        _client = new VeryfiClient(clientId, clientSecret, username, apiKey);
    }

    public async Task<ReceiptData> ProcessReceiptAsync(string imagePath)
    {
        // Receipt image leaves your infrastructure here
        // Cost: ~$0.05-0.15 per document
        var imageBytes = File.ReadAllBytes(imagePath);
        var base64Image = Convert.ToBase64String(imageBytes);

        var response = await _client.ProcessDocumentAsync(
            fileData: base64Image,
            fileName: Path.GetFileName(imagePath)
        );

        return new ReceiptData
        {
            VendorName   = response.Vendor?.Name,
            Total        = response.Total,
            Date         = response.Date,
            Tax          = response.Tax,
            PaymentType  = response.Payment?.Type,
            CardLastFour = response.Payment?.Last4,
            LineItems    = response.LineItems?.Select(li => new LineItem
            {
                Description = li.Description,
                Quantity    = li.Quantity,
                UnitPrice   = li.UnitPrice,
                Total       = li.Total
            }).ToList()
        };
    }
}
```

Invoices escalate the sensitivity. The same `ProcessDocumentAsync` pattern transmits documents that frequently contain bank account numbers, routing numbers, vendor tax IDs, and payment terms:

```csharp
public async Task<InvoiceData> ProcessInvoiceAsync(string pdfPath)
{
    var pdfBytes = File.ReadAllBytes(pdfPath);

    // Invoice uploaded to Veryfi — banking details included
    var response = await _client.ProcessDocumentAsync(
        fileData: pdfBytes,
        fileName: Path.GetFileName(pdfPath),
        categories: new[] { "invoices" }
    );

    return new InvoiceData
    {
        InvoiceNumber    = response.InvoiceNumber,
        VendorName       = response.Vendor?.Name,
        VendorTaxId      = response.Vendor?.TaxId,       // transmitted
        Total            = response.Total,
        DueDate          = response.DueDate,
        PaymentTerms     = response.PaymentTerms,
        BankAccountNumber = response.BankAccount?.AccountNumber, // transmitted
        BankRoutingNumber = response.BankAccount?.RoutingNumber  // transmitted
    };
}
```

The bank statement endpoint is the most sensitive. Calling `ProcessDocumentAsync` with `categories: new[] { "bank_statements" }` transmits full account numbers, routing numbers, complete transaction history, opening and closing balances, and wire transfer details to Veryfi's infrastructure.

## Understanding IronOCR

[IronOCR](https://ironsoftware.com/csharp/ocr/) is a commercial on-premise OCR library for .NET that processes every document locally. No document bytes leave your infrastructure. The library wraps an optimized Tesseract 5 engine with automatic preprocessing, native PDF support, and 125+ language packs—all delivered as a single NuGet package with a perpetual license.

Key characteristics:

- **On-premise by design:** All processing happens on your hardware; no network calls for OCR
- **Automatic preprocessing:** Deskew, DeNoise, Contrast, Binarize, and EnhanceResolution applied automatically without explicit configuration
- **Native PDF input:** Read scanned and digital PDFs directly via `LoadPdf()`—no separate PDF library required
- **Perpetual licensing:** $749 Lite / $1,499 Plus / $2,999 Professional / $5,999 Unlimited—pay once, process unlimited documents
- **Structured result access:** Words, lines, paragraphs, and pages with X/Y coordinates and per-word confidence scores
- **Thread-safe and parallelizable:** `IronTesseract` instances can be used across threads without additional synchronization
- **125+ languages:** Installed as NuGet packages, no tessdata folder management required
- **Cross-platform:** Windows, Linux, macOS, Docker, AWS, Azure—one package covers all targets

## Feature Comparison

| Feature | Veryfi | IronOCR |
|---|---|---|
| **Data location** | Veryfi cloud servers | Your infrastructure |
| **Deployment model** | Cloud API only | On-premise |
| **Pricing model** | Per-document ($0.05–$0.30) | Perpetual license ($749–$5,999) |
| **Offline support** | No | Yes |
| **Document scope** | Expense documents only | Any document type |
| **Setup** | 4 API credentials | 1 license key |

### Detailed Feature Comparison

| Feature | Veryfi | IronOCR |
|---|---|---|
| **Data Privacy** | | |
| Documents leave infrastructure | Yes (every call) | Never |
| Air-gapped deployment | Not possible | Fully supported |
| On-premise processing | Not possible | Default behavior |
| HIPAA compliant (no BAA required) | No (BAA required) | Yes |
| ITAR/CMMC compatible | Complex | Yes |
| **Document Support** | | |
| Receipt processing | Yes (specialized) | Yes (via OCR + extraction) |
| Invoice processing | Yes (specialized) | Yes (via OCR + extraction) |
| Bank statement processing | Yes (specialized) | Yes (via OCR + extraction) |
| General business documents | Poor results | Yes |
| Medical records | Not supported | Yes |
| Legal contracts | Limited | Yes |
| Custom/arbitrary document types | Paid training required | Any type |
| Multi-page PDFs | Yes | Yes |
| **Technical** | | |
| Requires internet | Yes (every document) | No |
| Synchronous API available | No | Yes |
| Image preprocessing | Automatic (cloud) | Automatic (local) |
| PDF input (native) | Yes | Yes |
| Searchable PDF output | No | Yes |
| Barcode reading | No | Yes |
| Region-based OCR | No | Yes |
| Confidence scores | Per-field | Per-word and overall |
| **Development** | | |
| Credentials to manage | 4 (clientId, clientSecret, username, apiKey) | 1 (license key) |
| Unit testing complexity | Requires mocking HTTP | Direct local testing |
| Error handling | HTTP status codes + VeryfiApiException | Local .NET exceptions |
| Language support | Limited | 125+ languages |
| **Cost** | | |
| Cost ceiling | None without contractual cap | License price |
| Cost at high volume | Scales linearly | Flat |

## Data Privacy for Financial Documents

Financial documents processed by Veryfi include the most sensitive data categories in any organization. This is not a theoretical concern—it is a concrete architectural fact about how the product works.

### Veryfi Approach

Every Veryfi API call transmits document bytes over HTTPS to `api.veryfi.com`. The data extracted from those documents—account numbers, routing numbers, transaction histories, vendor tax IDs, payment card details, and spending patterns—is processed by Veryfi's infrastructure. Data residency, retention policies, subprocessor chains, and breach response procedures are all governed by Veryfi's terms, not yours.

```csharp
// Bank statement processing — critical sensitivity
public async Task<BankStatementData> ProcessBankStatementAsync(string pdfPath)
{
    var pdfBytes = File.ReadAllBytes(pdfPath);

    // AT THIS POINT: full financial history leaves your infrastructure
    // Account numbers, transaction history, balances — all transmitted
    var response = await _client.ProcessDocumentAsync(
        fileData: pdfBytes,
        categories: new[] { "bank_statements" }
    );

    return new BankStatementData
    {
        AccountNumber  = response.AccountNumber,  // critical — transmitted
        RoutingNumber  = response.RoutingNumber,  // critical — transmitted
        OpeningBalance = response.OpeningBalance,
        ClosingBalance = response.ClosingBalance,
        Transactions   = response.Transactions?.Select(t => new Transaction
        {
            Date        = t.Date,
            Description = t.Description,
            Amount      = t.Amount,
            Balance     = t.RunningBalance
        }).ToList()
    };
}
```

Veryfi holds SOC 2 Type II, GDPR, and HIPAA (with Business Associate Agreement) certifications. Those certifications confirm that Veryfi follows their stated security practices. They do not mean your data stays under your control, that you control data residency, or that Veryfi's subprocessors have no access. For organizations under CMMC Level 2+, ITAR, SOX, or GLBA, adding Veryfi as a data processor introduces compliance scope that requires legal review and ongoing oversight.

### IronOCR Approach

[IronOCR](https://ironsoftware.com/csharp/ocr/) processes documents with zero network transmission. The OCR engine runs on your hardware. Bank statements, invoices with routing numbers, and checks with account numbers never leave your server:

```csharp
using IronOcr;
using System.Text.RegularExpressions;

IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

public class LocalFinancialDocumentProcessor
{
    private readonly IronTesseract _ocr = new IronTesseract();

    public BankStatementResult ProcessBankStatement(string pdfPath)
    {
        // All processing on your infrastructure
        // No network calls — no external access
        using var input = new OcrInput();
        input.LoadPdf(pdfPath);

        var result = _ocr.Read(input);
        var text   = result.Text;

        // Account numbers stay on your server
        return new BankStatementResult
        {
            AccountNumber = ExtractAccountNumber(text),
            Balance       = ExtractBalance(text),
            RawText       = text,
            Confidence    = result.Confidence
        };
    }

    private string ExtractAccountNumber(string text)
    {
        var match = Regex.Match(text,
            @"Account\s*#?\s*:?\s*(\d{4,})",
            RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private decimal? ExtractBalance(string text)
    {
        var match = Regex.Match(text,
            @"Balance:?\s*\$?([\d,]+\.?\d*)",
            RegexOptions.IgnoreCase);
        if (match.Success && decimal.TryParse(
            match.Groups[1].Value.Replace(",", ""), out var bal))
            return bal;
        return null;
    }
}
```

For compliance-heavy environments, local processing eliminates third-party audit scope entirely. No BAA with IronOCR. No subprocessor disclosures. No data residency questions. See the [IronTesseract setup guide](https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/) for deployment configuration across Windows, Linux, Docker, and air-gapped environments.

## Specialist Scope vs. Generalist OCR

Veryfi's narrow document focus is both its greatest strength and its most significant limitation. The pre-trained models deliver genuinely fast, structured results for the specific document types they were built for. The problem arises the moment your organization needs OCR outside that list.

### Veryfi Approach

Veryfi works well within its lane. Submit a receipt image and the response contains vendor, total, tax, payment method, and line items—no regex required on your end. But the scope ends at the boundary of expense and financial documents. The README documents this directly: general business documents, medical records, technical drawings, multi-language documents, and custom form types outside the trained set produce poor results or require paid custom model training.

Real organizations rarely process only one document category. HR departments process employee onboarding forms. Legal teams need contract text. Operations teams handle shipping documents and logistics forms. Each category outside Veryfi's scope forces a second solution into the stack:

```csharp
// Veryfi: works for invoices, fails for shipping manifests
var invoiceResponse = await _client.ProcessDocumentAsync(
    fileData: invoiceBytes,
    categories: new[] { "invoices" }
);

// For anything outside the supported list, you need a second tool
// Contracts: limited support
// Medical forms: not supported
// Shipping documents: not supported
// Custom forms: paid training required
```

### IronOCR Approach

[IronOCR](https://ironsoftware.com/csharp/ocr/) processes any document type. The extraction logic is yours to write, which means it handles exactly the fields your documents contain—not a pre-trained model's guess at what fields are common across all expense documents. The same `IronTesseract` instance processes receipts, contracts, medical forms, shipping documents, and custom internal forms:

```csharp
using IronOcr;

var ocr = new IronTesseract();

// Receipts
var receiptResult = ocr.Read("receipt.jpg");
var receiptTotal  = ExtractTotal(receiptResult.Text);

// Contracts — not in Veryfi's scope
using var contractInput = new OcrInput();
contractInput.LoadPdf("contract.pdf");
var contractResult = ocr.Read(contractInput);
var parties        = ExtractContractParties(contractResult.Text);

// Medical forms — requires HIPAA BAA with Veryfi; stays local with IronOCR
var medicalResult = ocr.Read("patient-intake-form.jpg");
var patientName   = medicalResult.Lines.FirstOrDefault()?.Text;

// Shipping documents
var shipResult    = ocr.Read("bill-of-lading.jpg");
var trackingNum   = ExtractPattern(shipResult.Text, @"Tracking\s*#?\s*:?\s*(\w+)");
```

For invoice and receipt fields that follow consistent patterns, IronOCR's [structured data extraction](https://ironsoftware.com/csharp/ocr/how-to/read-results/) provides word-level positioning that makes extraction logic precise. The `result.Lines` collection, ordered by Y position, reliably surfaces vendor names from receipt headers. The [image quality correction filters](https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/) handle the crumpled, faded, or photographed receipts that real expense workflows produce.

## Per-Document Cloud Cost vs. Perpetual License

The cost difference between Veryfi and IronOCR compounds with every document processed. Veryfi's per-document model means costs grow in exact proportion to volume with no ceiling unless you negotiate one contractually.

### Veryfi Approach

Veryfi charges per document. The rate depends on document type and volume tier: approximately $0.05–$0.15 for receipts, $0.10–$0.25 for invoices, and $0.15–$0.30 for bank statements. Processing 50,000 receipts per month at $0.10 per document costs $5,000 per month—$60,000 per year—with no reduction in subsequent years regardless of how long you have been a customer.

```csharp
// This method call costs money — every time
// 50,000 calls/month @ $0.10 = $5,000/month
var response = await _client.ProcessDocumentAsync(bytes);
```

Hidden costs compound the total: rate limiting at HTTP 429 forces retry logic; API version changes require SDK updates; security review of the data processing agreement adds legal time; and overage charges appear when seasonal volume exceeds plan estimates.

### IronOCR Approach

IronOCR licensing is a one-time purchase. The Professional license at $2,999 covers 10 developers, processes unlimited documents per month, and requires no renewal to continue processing. Year 2 costs $0 for processing. Year 5 costs $0 for processing.

```csharp
// Configure once at startup — no per-call cost
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

var ocr = new IronTesseract();

// This processes 50,000 documents/month at $0 marginal cost
foreach (var documentPath in documentBatch)
{
    var result = ocr.Read(documentPath);
    ProcessExtractedData(result.Text, result.Lines);
}
```

The 3-year comparison at 50,000 documents per month: Veryfi totals $180,000; IronOCR totals $2,999 plus an estimated 8–16 hours of development time to build extraction patterns. Break-even against the $2,999 Professional license at $5,000/month Veryfi spend: under one month.

For teams currently spending on cloud OCR, the [IronOCR licensing page](https://ironsoftware.com/csharp/ocr/licensing/) covers tier details and the SaaS subscription option available for teams that prefer annual billing.

## Webhook and Async Processing vs. Synchronous Local Results

Veryfi's cloud architecture imposes an async-only processing model. The document travels to a remote server, gets processed by a remote pipeline, and the response arrives after a network round-trip. For high-volume batch processing, Veryfi recommends webhook-based notification rather than polling. IronOCR processes synchronously by default, with no network dependency in the loop.

### Veryfi Approach

All Veryfi processing is asynchronous because it is cloud-based. The `ProcessDocumentAsync` method initiates an HTTP call, waits for Veryfi's pipeline to complete, and returns the result. Total latency includes network transmission, queue time on Veryfi's infrastructure, model inference, and response transmission.

```csharp
// Veryfi error categories reflect cloud dependencies
public async Task<ReceiptData> ProcessWithHandlingAsync(string path)
{
    try
    {
        var bytes    = File.ReadAllBytes(path);
        var response = await _client.ProcessDocumentAsync(bytes);
        return MapToReceiptData(response);
    }
    catch (VeryfiApiException ex) when (ex.StatusCode == 401)
    {
        // Credential failure — check all four credentials
        throw new Exception("Authentication failed. Verify clientId, clientSecret, username, apiKey.", ex);
    }
    catch (VeryfiApiException ex) when (ex.StatusCode == 402)
    {
        // Plan quota exceeded — processing halted until billing resolved
        throw new Exception("Quota exceeded. Check plan limits and billing.", ex);
    }
    catch (VeryfiApiException ex) when (ex.StatusCode == 429)
    {
        // Rate limited — requires exponential backoff implementation
        throw new Exception("Rate limit exceeded. Implement retry with backoff.", ex);
    }
    catch (VeryfiApiException ex) when (ex.StatusCode == 500)
    {
        // Veryfi infrastructure failure — your processing is down
        throw new Exception("Veryfi server error. Processing unavailable.", ex);
    }
}
```

Network outages halt processing. Veryfi infrastructure incidents halt processing. Rate limits halt processing. Each of these is an external dependency your application cannot control.

### IronOCR Approach

IronOCR processes synchronously with zero network dependency. The result is available immediately after `Read()` returns, with no polling, no webhook configuration, and no retry logic for network failures.

```csharp
using IronOcr;

var ocr = new IronTesseract();

// Synchronous — result immediately available
// No network, no quota, no rate limits
var result = ocr.Read("receipt.jpg");

Console.WriteLine(result.Text);
Console.WriteLine($"Confidence: {result.Confidence}%");

// Word-level positioning for precise field extraction
foreach (var line in result.Lines)
{
    Console.WriteLine($"Y={line.Y}: {line.Text}");
}
```

For applications that need async behavior for UI responsiveness, IronOCR also supports [async OCR](https://ironsoftware.com/csharp/ocr/how-to/async/) via `Task.Run` wrapping or the async API surface. The difference is that async in IronOCR is a concurrency pattern, not a requirement imposed by a remote call. A network outage at 2 AM does not stop your batch process.

For receipt scanning workflows, the [receipt scanning tutorial](https://ironsoftware.com/csharp/ocr/blog/using-ironocr/receipt-scanning-api-tutorial/) and [invoice OCR guide](https://ironsoftware.com/csharp/ocr/blog/using-ironocr/invoice-ocr-csharp-tutorial/) cover full extraction pattern implementations. [Confidence scores](https://ironsoftware.com/csharp/ocr/how-to/tesseract-result-confidence/) provide per-word reliability signals that help flag low-quality scans before committing extracted data to a downstream system.

## API Mapping Reference

| Veryfi | IronOCR Equivalent |
|---|---|
| `new VeryfiClient(clientId, clientSecret, username, apiKey)` | `new IronTesseract()` + `IronOcr.License.LicenseKey = "key"` |
| `_client.ProcessDocumentAsync(bytes)` | `ocr.Read(filePath)` or `ocr.Read(ocrInput)` |
| `_client.ProcessDocumentAsync(bytes, categories: new[] { "invoices" })` | `input.LoadPdf(pdfPath); ocr.Read(input)` |
| `response.Vendor?.Name` | `result.Lines.FirstOrDefault()?.Text` |
| `response.Total` | `ExtractTotal(result.Text)` via Regex |
| `response.Date` | `ExtractDate(result.Text)` via Regex |
| `response.LineItems` | `result.Lines` with positional extraction |
| `response.InvoiceNumber` | `Regex.Match(result.Text, @"Invoice\s*#?\s*:?\s*(\w+)")` |
| `response.ConfidenceScore` | `result.Confidence` |
| `response.Payment?.Last4` | `Regex.Match(result.Text, @"\d{4}$")` |
| `response.BankAccount?.AccountNumber` | `Regex.Match(result.Text, @"Account\s*#?\s*:?\s*(\d+)")` |
| `VeryfiApiException` (HTTP 401/402/429/500) | Standard .NET exceptions (local, no network codes) |
| Base64 encoding before upload | Not required — `ocr.Read(filePath)` accepts file paths directly |
| Four credentials in configuration | Single license key |

## When Teams Consider Moving from Veryfi to IronOCR

### Data Sovereignty Becomes Non-Negotiable

The most common trigger for evaluating IronOCR is a compliance audit or legal review that surfaces Veryfi as a data processor for sensitive financial records. A HIPAA assessment finds that expense documents containing PHI require a Business Associate Agreement with Veryfi and ongoing audit oversight. A CMMC Level 2 assessment identifies cloud-transmitted financial documents as a potential gap. A legal team advises against sending client matter expense data to a third party. In each scenario, the path to compliance is eliminating the external data transmission—which means replacing Veryfi with on-premise OCR. IronOCR's local processing model satisfies these requirements by default: no BAA, no subprocessor chain, no third-party audit scope.

### Document Types Expand Beyond Expenses

Organizations that adopt Veryfi for expense automation often discover within 6–12 months that adjacent teams need OCR for documents outside Veryfi's training set. HR needs onboarding forms processed. Legal needs contract text extracted. Operations needs bill-of-lading data captured. Each new document category either produces poor results from Veryfi's general-purpose endpoint, requires paid custom model training, or forces a second OCR solution into the infrastructure. A single IronOCR license handles all of these with the same API. The [read-specific-document tutorial](https://ironsoftware.com/csharp/ocr/tutorials/read-specific-document/) covers extraction strategies for structured form layouts.

### Per-Document Costs Cross the Business Case Threshold

Teams processing 25,000 or more documents per month typically find the IronOCR payback period under three months. At 50,000 documents per month, Veryfi costs $60,000 per year; the IronOCR Professional license at $2,999 is paid for in under 20 days of Veryfi spend. Finance teams running total cost of ownership analysis on per-document cloud pricing inevitably arrive at this comparison. The migration effort—building regex extraction patterns for the specific fields the team needs—is typically 8–24 hours depending on document complexity and the number of distinct document types in the workflow.

### Offline and Disconnected Environments

Field service organizations, remote work sites, mobile expense capture apps, and air-gapped government environments share a common requirement: OCR must work without internet. Veryfi is architecturally incapable of satisfying this requirement—every document processing call fails without a network connection. IronOCR processes offline by design. A field technician running a mobile expense app, a manufacturing plant with a firewalled network, and a defense contractor in a SCIF can all run IronOCR without modification. The [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/) and [Linux deployment guide](https://ironsoftware.com/csharp/ocr/get-started/linux/) cover containerized environments where network egress is restricted.

### Vendor Lock-In and Schema Dependency

Veryfi's JSON response schema is proprietary. Every line of extraction code that reads vendor name, total, routing number, or line item fields is code that only works with Veryfi. Switching to a competing cloud API, or to an on-premise solution, requires rewriting all extraction logic. Organizations that negotiate multi-year contracts with Veryfi discover this dependency when pricing changes or the product roadmap shifts. IronOCR extraction logic reads from raw text using standard .NET Regex—portable, not coupled to any vendor's schema.

## Common Migration Considerations

### Replacing Structured Output with Pattern Extraction

Veryfi returns pre-parsed fields; IronOCR returns raw OCR text. The migration work is writing the extraction patterns that Veryfi was doing in their cloud. For receipts and invoices with consistent layouts, this is straightforward:

```csharp
using IronOcr;
using System.Text.RegularExpressions;

var ocr    = new IronTesseract();
var result = ocr.Read("receipt.jpg");
var text   = result.Text;

// Vendor: first non-empty line, ordered by Y position
var vendor = result.Lines
    .OrderBy(l => l.Y)
    .Select(l => l.Text.Trim())
    .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t) && t.Length > 3);

// Total: pattern matching against common receipt labels
decimal? total = null;
foreach (var pattern in new[] {
    @"Total:?\s*\$?\s*([\d,]+\.?\d*)",
    @"Grand Total:?\s*\$?\s*([\d,]+\.?\d*)",
    @"Amount Due:?\s*\$?\s*([\d,]+\.?\d*)" })
{
    var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
    if (match.Success && decimal.TryParse(
        match.Groups[1].Value.Replace(",", ""), out var t))
    {
        total = t;
        break;
    }
}

// Tax
var taxMatch = Regex.Match(text,
    @"(?:Tax|Sales Tax|VAT):?\s*\$?\s*([\d,]+\.?\d*)",
    RegexOptions.IgnoreCase);
```

For receipts with unusual layouts or faded printing, IronOCR's preprocessing filters improve raw accuracy before extraction runs. See the [image quality correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/) and [image color correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-color-correction/) for handling real-world receipt scan quality.

### Handling the Async-to-Sync Transition

Veryfi's entire API surface is async. IronOCR's primary path is synchronous. Existing async method signatures in application code can be preserved by wrapping:

```csharp
// Preserve async signature during transition
public async Task<ReceiptResult> ProcessReceiptAsync(string path)
{
    // Run IronOCR on a background thread
    return await Task.Run(() =>
    {
        var result = _ocr.Read(path);
        return new ReceiptResult
        {
            Vendor = result.Lines.FirstOrDefault()?.Text,
            Total  = ExtractTotal(result.Text),
            Date   = ExtractDate(result.Text)
        };
    });
}
```

For high-throughput scenarios, IronOCR is thread-safe and multiple `IronTesseract` instances can process documents in parallel without synchronization. The [speed optimization guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-fast-configuration/) covers instance reuse patterns and batch loading strategies that minimize processing time at scale.

### Credential Cleanup

Veryfi requires four credentials: `ClientId`, `ClientSecret`, `Username`, and `ApiKey`. All four must be removed from configuration files, environment variables, key vaults, and CI/CD pipelines after migration. IronOCR requires a single license key string. The migration checklist: remove all four Veryfi secrets from every environment, add a single `IRONOCR_LICENSE` environment variable, and update deployment pipelines to remove Veryfi credential injection.

### PDF Processing for Invoices

Veryfi accepts PDF bytes via `ProcessDocumentAsync`. IronOCR processes PDFs natively through `OcrInput.LoadPdf()`:

```csharp
// Multi-page invoice PDF — all pages processed locally
using var input = new OcrInput();
input.LoadPdf("invoice.pdf");

var result       = new IronTesseract().Read(input);
var invoiceText  = result.Text;
var invoiceNumber = Regex.Match(invoiceText,
    @"Invoice\s*#?\s*:?\s*(\w+[-\w]*)",
    RegexOptions.IgnoreCase).Groups[1].Value;
```

For invoice workflows that produce searchable PDF archives, IronOCR's `SaveAsSearchablePdf()` method generates PDF/A-compatible output from scanned input—a capability Veryfi's cloud API does not provide. See the [searchable PDF guide](https://ironsoftware.com/csharp/ocr/how-to/searchable-pdf/) for implementation details.

## Additional IronOCR Capabilities

Beyond the core comparison areas, IronOCR provides capabilities that are entirely outside Veryfi's scope:

- **[Table extraction](https://ironsoftware.com/csharp/ocr/how-to/read-table-in-document/):** Structured table reading from documents with grid layouts, applicable to bank statements and itemized invoices
- **[Handwriting recognition](https://ironsoftware.com/csharp/ocr/how-to/read-handwritten-image/):** Process handwritten notes, signatures, and handwritten form fields that Veryfi's pre-trained models are not designed to handle
- **[MICR/cheque reading](https://ironsoftware.com/csharp/ocr/how-to/read-micr-cheque/):** Dedicated MICR line extraction for check processing workflows, keeping routing and account number extraction on-premise
- **[hOCR export](https://ironsoftware.com/csharp/ocr/how-to/html-hocr-export/):** Export OCR results as hOCR (HTML with position data) for downstream document analysis pipelines
- **[Progress tracking](https://ironsoftware.com/csharp/ocr/how-to/progress-tracking/):** Monitor processing progress for long-running batch jobs without polling a remote API
- **[Passport and ID reading](https://ironsoftware.com/csharp/ocr/how-to/read-passport/):** Extract machine-readable zone data from passports and identity documents for onboarding and KYC workflows
- **[AWS and Azure deployment](https://ironsoftware.com/csharp/ocr/get-started/aws/):** Deploy to cloud infrastructure while keeping document processing inside your own cloud account—no external SaaS data processor added to your compliance scope

## .NET Compatibility and Future Readiness

[IronOCR](https://ironsoftware.com/csharp/ocr/) targets .NET 8, .NET 9, and the forthcoming .NET 10, as well as .NET Standard 2.0+ for legacy compatibility. It runs on Windows x64/x86, Linux x64, macOS, and ARM64—covering every modern .NET deployment target including Docker containers, Azure App Service, AWS Lambda, and on-premise Linux servers. The single-package deployment model means no native binary management across environments; the same NuGet reference resolves correctly on every supported platform. Veryfi's C# SDK targets .NET Standard and functions as an HTTP client wrapper, meaning its "compatibility" is primarily a function of the HTTP stack rather than OCR engine depth—a distinction that becomes significant when platform constraints or network restrictions are in play.

## Conclusion

Veryfi solves a specific problem quickly: submit an expense document, receive structured JSON fields without writing parsing logic. For small-scale expense automation in environments where cloud data transmission is acceptable, that proposition has genuine value. The friction appears when volume grows past the point where per-document costs exceed a perpetual license in weeks, when document types expand beyond Veryfi's trained categories, when compliance requirements make third-party financial document processing impractical, or when the application simply needs to work without internet access.

The four-credential authentication model, the async-only API, the rate limiting, the HTTP 402 payment failures that halt batch processing, and the proprietary JSON schema that couples all extraction logic to a single vendor—these are not edge-case concerns. They are daily operational realities for teams running Veryfi in production at scale.

IronOCR requires writing extraction patterns that Veryfi handles automatically. That is a real development cost: expect 8–24 hours to build regex extraction for receipts and invoices, depending on document variety. But that investment is made once. The resulting code runs on your infrastructure, processes any document type, operates offline, and costs nothing per document. Bank statements, routing numbers, account numbers, and transaction histories never leave your server.

For teams in financial services, healthcare, defense contracting, or legal services—any domain where sending expense documents to a third-party cloud is a compliance conversation—the IronOCR path eliminates that conversation entirely. For teams currently spending $5,000–$20,000 per month on Veryfi, the arithmetic is straightforward. For teams that need general-purpose OCR beyond expense documents, the choice is clear: a cloud specialist or a local general-purpose library are not equivalent tools, and the one-time license cost of the latter buys unbounded flexibility that the former cannot provide at any per-document price.
