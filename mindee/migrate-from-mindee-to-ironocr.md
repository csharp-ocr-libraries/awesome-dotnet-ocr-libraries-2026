# Migrating from Mindee to IronOCR

Migrating from Mindee to [IronOCR](https://ironsoftware.com/csharp/ocr/) moves document processing off external cloud servers and onto your own infrastructure — eliminating per-page billing, third-party data transmission, and network availability as a runtime dependency. The sections below cover package replacement, namespace updates, and four practical before-and-after code scenarios: invoice zone extraction, multi-page receipt PDF processing, financial document preprocessing, and searchable PDF archiving.

## Why Migrate from Mindee

Mindee solves a real problem elegantly: submit a document, receive structured JSON fields. For teams where transmitting financial documents to an external API is acceptable and volume stays moderate, it delivers fast results. The migration trigger usually arrives when one of several conditions changes.

**Financial Data Crosses a Compliance Boundary.** Mindee's `ParseAsync<InvoiceV4>` uploads the complete document to external servers. IBAN codes, routing numbers, vendor EINs, and itemized line items travel over the public internet and are processed on hardware outside your control. SOC 2 Type II certification confirms Mindee follows security practices; it does not keep your documents inside your security perimeter. When a compliance review, a new customer contract, or a data residency regulation draws the line at third-party cloud processing, Mindee's architecture becomes disqualifying regardless of its extraction accuracy.

**Per-Page Costs Scale Against You.** The Starter plan provides 1,000 pages per month at $49/month. The Pro plan provides 5,000 pages per month at $499/month. Multi-page PDFs count every page. Development runs count against quota. A team processing 4,000 invoices per month pays $499/month — $5,988/year — with that figure resetting every year and scaling upward with any volume growth. At 24,000 pages per month you are negotiating Enterprise pricing. IronOCR's perpetual license at $1,499 (Professional tier) covers unlimited document volume and recovers its cost against Mindee Pro in under four months.

**Async Polling Propagates Through Your Stack.** Every Mindee operation is asynchronous because every operation is a network request. That mandatory async chain cascades upward through controllers, service layers, and worker classes. When the network is unavailable or Mindee's service experiences an outage, the cascade results in failure with no local fallback. IronOCR processes synchronously — local CPU work — and wraps in `Task.Run` where async interfaces are required for architectural consistency.

**The Pre-Built API Catalog Has a Hard Ceiling.** Mindee covers `InvoiceV4`, `ReceiptV5`, `PassportV1`, and a small catalog of other document types. Any document type outside that list requires Enterprise plan custom model training: sample collection, labeling, training time, and lead time before the API is available. Every new document type your business requires is a separate vendor engagement. IronOCR processes any document type using the same `IronTesseract().Read()` call; adding a new document type means writing extraction patterns, not negotiating a training contract.

**API Key Management Adds Operational Surface.** Mindee credentials must be stored securely server-side, rotated periodically, and protected from exposure. Network egress rules must permit outbound HTTPS to Mindee endpoints. Retry logic must handle `HttpRequestException` and `MindeeException` from the inevitable network failures. Removing Mindee removes all of that operational surface: no credentials, no egress rules, no retry wrappers around network I/O.

**Air-Gapped and Restricted Environments Are Excluded.** Government contractors, defense subcontractors, healthcare networks with strict outbound internet controls, and industrial systems deployed in facilities without reliable internet connectivity cannot use Mindee as designed. IronOCR runs identically in Docker containers without outbound access, in Azure Functions with egress restrictions, and in fully air-gapped environments. There is no cloud dependency at runtime. See the [IronOCR licensing page](https://ironsoftware.com/csharp/ocr/licensing/) for deployment tier details.

### The Fundamental Problem

Every financial document Mindee processes crosses a network boundary you do not control:

```csharp
// Mindee: invoice with bank details leaves your infrastructure on this line
var response = await _client.ParseAsync<InvoiceV4>(new LocalInputSource("invoice.pdf"));
// IBAN, routing number, EIN, line items — all transmitted to Mindee cloud
```

```csharp
// IronOCR: same invoice, same result, zero data transmission
var result = new IronTesseract().Read("invoice.pdf");
// All processing local — bank details never leave your machine
```

## IronOCR vs Mindee: Feature Comparison

The following table captures the architectural and capability differences that drive migration decisions.

| Feature | Mindee | IronOCR |
|---|---|---|
| **Processing location** | Mindee cloud servers | Local machine / your infrastructure |
| **Internet required at runtime** | Always | Never |
| **Data transmission** | Full document uploaded per call | None |
| **Per-document cost** | Yes (per page, plan-based) | No (perpetual license) |
| **Starting price** | $49/month (1,000 pages) | $749 one-time (Lite) |
| **Offline / air-gapped support** | Not supported | Full support |
| **On-premise deployment** | Not available | Only option |
| **Invoice parsing** | Pre-built structured API (`InvoiceV4`) | OCR + region-based or regex extraction |
| **Receipt parsing** | Pre-built structured API (`ReceiptV5`) | OCR + extraction patterns |
| **Arbitrary document types** | Not supported without custom training | Full support, any document |
| **Custom model training** | Enterprise plan + lead time | Not required |
| **API call pattern** | Async mandatory (network I/O) | Synchronous (async optional) |
| **Rate limiting** | Plan-dependent | None |
| **PDF input** | Yes | Yes (native, no conversion) |
| **Multi-page PDF processing** | Yes | Yes |
| **Searchable PDF output** | No | Yes (`result.SaveAsSearchablePdf()`) |
| **Image preprocessing** | None exposed | Deskew, DeNoise, Contrast, Binarize, Sharpen, Scale |
| **Region-based extraction** | Field coordinates in response | `CropRectangle` on input |
| **125+ language support** | Limited | Yes (bundled, no tessdata management) |
| **Barcode reading** | No | Yes (concurrent with OCR) |
| **Structured word coordinates** | No | Yes (Pages, Paragraphs, Lines, Words) |
| **Thread safety** | N/A (network I/O per call) | Built-in |
| **Cross-platform** | Cloud (platform-independent) | Windows, Linux, macOS, Docker, Azure, AWS |
| **.NET compatibility** | .NET Standard 2.0+ | .NET Framework 4.6.2+, .NET 5/6/7/8/9 |
| **Commercial support** | Yes | Yes |

## Quick Start: Mindee to IronOCR Migration

### Step 1: Replace NuGet Package

Remove the Mindee package:

```bash
dotnet remove package Mindee
```

Install IronOCR from [NuGet](https://www.nuget.org/packages/IronOcr):

```bash
dotnet add package IronOcr
```

### Step 2: Update Namespaces

Replace the Mindee namespace block with the IronOCR namespace:

```csharp
// Before (Mindee)
using Mindee;
using Mindee.Input;
using Mindee.Product.Invoice;
using Mindee.Product.Receipt;
using Mindee.Product.FinancialDocument;

// After (IronOCR)
using IronOcr;
```

### Step 3: Initialize License

Add the license key at application startup (before the first OCR call):

```csharp
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
```

## Code Migration Examples

### Invoice Field Extraction Using Document Zones

Mindee returns pre-parsed fields at known locations because its server-side model knows where invoice numbers, dates, and totals typically appear. The IronOCR equivalent uses `CropRectangle` to read specific zones of the document, matching the spatial awareness of Mindee's extraction without transmitting the document.

**Mindee Approach:**

```csharp
using Mindee;
using Mindee.Input;
using Mindee.Product.Invoice;

public class MindeeZoneExtractor
{
    private readonly MindeeClient _client;

    public MindeeZoneExtractor(string apiKey)
    {
        _client = new MindeeClient(apiKey);
    }

    public async Task<(string invoiceNumber, string supplierName, decimal? total)>
        ExtractKeyFieldsAsync(string filePath)
    {
        // Document transmitted to Mindee — spatial field detection happens server-side
        var inputSource = new LocalInputSource(filePath);
        var response    = await _client.ParseAsync<InvoiceV4>(inputSource);
        var prediction  = response.Document.Inference.Prediction;

        // Fields arrive pre-parsed; no zone configuration required on your end
        return (
            prediction.InvoiceNumber?.Value,
            prediction.SupplierName?.Value,
            prediction.TotalAmount?.Value
        );
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;
using System.Text.RegularExpressions;

public class IronOcrZoneExtractor
{
    private readonly IronTesseract _ocr = new IronTesseract();

    public (string invoiceNumber, string supplierName, string total)
        ExtractKeyFields(string filePath)
    {
        // Zone 1: supplier name — top 12% of document, full width
        var headerRegion = new CropRectangle(0, 0, 850, 120);
        using var headerInput = new OcrInput();
        headerInput.LoadImage(filePath, headerRegion);
        var supplierResult = _ocr.Read(headerInput);

        // Zone 2: invoice number and date — upper-right quadrant
        var metaRegion = new CropRectangle(450, 80, 400, 100);
        using var metaInput = new OcrInput();
        metaInput.LoadImage(filePath, metaRegion);
        var metaResult = _ocr.Read(metaInput);

        // Zone 3: totals block — bottom-right 20%
        var totalsRegion = new CropRectangle(500, 800, 350, 200);
        using var totalsInput = new OcrInput();
        totalsInput.LoadImage(filePath, totalsRegion);
        var totalsResult = _ocr.Read(totalsInput);

        var invoiceNumber = Regex.Match(
            metaResult.Text,
            @"Invoice\s*#?\s*:?\s*([A-Z0-9\-]+)",
            RegexOptions.IgnoreCase).Groups[1].Value;

        var total = Regex.Match(
            totalsResult.Text,
            @"Total\s*(?:Due)?\s*:?\s*\$?([\d,]+\.?\d*)",
            RegexOptions.IgnoreCase).Groups[1].Value;

        // Supplier name is the first substantial text line in the header zone
        var supplierName = supplierResult.Text
            .Split('\n')
            .FirstOrDefault(l => l.Trim().Length > 4)
            ?.Trim();

        return (invoiceNumber, supplierName, total);
    }
}
```

Zone-based OCR matches the intent of Mindee's spatial field detection: read specific areas of the document rather than applying regex to the entire page. `CropRectangle(x, y, width, height)` takes pixel coordinates, so tuning requires measuring against a representative sample invoice. The [region-based OCR guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-region-of-an-image/) covers coordinate measurement and zone overlap strategies. For invoices with variable layouts, combining zone extraction with full-page regex on `result.Text` produces the most reliable results.

### Multi-Page Expense Report PDF Processing

Mindee accepts a PDF file and processes each page as a discrete document unit, returning a structured result per page. IronOCR reads multi-page PDFs natively through `OcrInput.LoadPdf()`, processing all pages in a single call and returning `result.Pages` with per-page content and word coordinates.

**Mindee Approach:**

```csharp
using Mindee;
using Mindee.Input;
using Mindee.Product.Receipt;

public class MindeeExpenseReportProcessor
{
    private readonly MindeeClient _client;

    public MindeeExpenseReportProcessor(string apiKey)
    {
        _client = new MindeeClient(apiKey);
    }

    public async Task<List<ExpenseSummary>> ProcessExpenseReportAsync(string pdfPath)
    {
        var summaries = new List<ExpenseSummary>();

        // Mindee processes per-document; each page of a PDF is a separate upload
        // For a 10-page expense report: 10 API calls, 10 pages billed
        var inputSource = new LocalInputSource(pdfPath);
        var response    = await _client.ParseAsync<ReceiptV5>(inputSource);

        var prediction = response.Document.Inference.Prediction;

        summaries.Add(new ExpenseSummary
        {
            MerchantName  = prediction.SupplierName?.Value,
            Date          = prediction.Date?.Value?.ToString(),
            TotalAmount   = prediction.TotalAmount?.Value ?? 0m,
            Category      = prediction.Category?.Value
        });

        return summaries;
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;
using System.Text.RegularExpressions;

public class IronOcrExpenseReportProcessor
{
    private readonly IronTesseract _ocr = new IronTesseract();

    public List<ExpenseSummary> ProcessExpenseReport(string pdfPath)
    {
        // Load entire multi-page PDF in one call — no per-page upload
        using var input = new OcrInput();
        input.LoadPdf(pdfPath);    // all pages loaded locally

        var result    = _ocr.Read(input);
        var summaries = new List<ExpenseSummary>();

        // Each page maps to one receipt in the expense report
        foreach (var page in result.Pages)
        {
            var pageText = page.Text;

            // Skip pages without receipt content
            if (!pageText.Contains("Total", StringComparison.OrdinalIgnoreCase))
                continue;

            var merchantName = page.Lines
                .FirstOrDefault(l => l.Text.Trim().Length > 4)
                ?.Text.Trim();

            var totalMatch = Regex.Match(
                pageText,
                @"(?:Total|Amount\s+Due|Grand\s+Total)\s*:?\s*\$?([\d,]+\.\d{2})",
                RegexOptions.IgnoreCase);

            var dateMatch = Regex.Match(
                pageText,
                @"\b(\d{1,2}[/-]\d{1,2}[/-]\d{2,4})\b");

            var categoryMatch = Regex.Match(
                pageText,
                @"(?:Category|Dept|Department)\s*:?\s*(.+)",
                RegexOptions.IgnoreCase);

            summaries.Add(new ExpenseSummary
            {
                PageNumber   = page.PageNumber,
                MerchantName = merchantName,
                Date         = dateMatch.Success ? dateMatch.Value : null,
                TotalAmount  = totalMatch.Success
                    ? decimal.Parse(totalMatch.Groups[1].Value.Replace(",", ""))
                    : 0m,
                Category     = categoryMatch.Success
                    ? categoryMatch.Groups[1].Value.Trim()
                    : null,
                Confidence   = page.Words.Any()
                    ? page.Words.Average(w => (double)w.Confidence)
                    : 0
            });
        }

        return summaries;
    }
}

public class ExpenseSummary
{
    public int    PageNumber   { get; set; }
    public string MerchantName { get; set; }
    public string Date         { get; set; }
    public decimal TotalAmount { get; set; }
    public string Category     { get; set; }
    public double Confidence   { get; set; }
}
```

Processing a 10-page expense report PDF with Mindee produces 10 API calls and 10 billed pages. IronOCR processes the same file in one local call with no per-page cost and no network round-trip per page. The `result.Pages` collection provides per-page text, word-level bounding boxes, and line positions for layout-aware extraction. See the [PDF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-pdfs/) for password-protected PDF loading and page range selection, and the [structured read results guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) for working with word coordinates.

### Scanned Invoice Preprocessing Pipeline

Mindee's cloud processing applies its own image normalization before running field extraction. The quality of that normalization is opaque — you have no control over it and no visibility into what preprocessing ran. When a scanned invoice has skew, shadow, or low contrast, Mindee returns lower confidence scores with no mechanism for you to improve the scan before submission. IronOCR exposes the preprocessing pipeline explicitly, letting you apply exactly the corrections a given document needs before recognition runs.

**Mindee Approach:**

```csharp
using Mindee;
using Mindee.Input;
using Mindee.Product.Invoice;

public class MindeeScanProcessor
{
    private readonly MindeeClient _client;

    public MindeeScanProcessor(string apiKey)
    {
        _client = new MindeeClient(apiKey);
    }

    public async Task<ScanResult> ProcessScannedInvoiceAsync(string scanPath)
    {
        // Mindee applies internal normalization — no developer control over preprocessing
        var inputSource = new LocalInputSource(scanPath);
        var response    = await _client.ParseAsync<InvoiceV4>(inputSource);
        var prediction  = response.Document.Inference.Prediction;

        return new ScanResult
        {
            InvoiceNumber = prediction.InvoiceNumber?.Value,
            Total         = prediction.TotalAmount?.Value,
            // Per-field confidence: only proxy for scan quality
            InvoiceNumberConfidence = prediction.InvoiceNumber?.Confidence,
            TotalConfidence         = prediction.TotalAmount?.Confidence
        };
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;
using System.Text.RegularExpressions;

public class IronOcrScanProcessor
{
    private readonly IronTesseract _ocr = new IronTesseract();

    public ScanResult ProcessScannedInvoice(string scanPath)
    {
        // First pass: read without preprocessing
        var quickResult = _ocr.Read(scanPath);

        OcrResult result;

        if (quickResult.Confidence < 75)
        {
            // Low confidence — apply full preprocessing pipeline
            using var input = new OcrInput();
            input.LoadImage(scanPath);
            input.Deskew();       // correct page rotation up to ±45 degrees
            input.DeNoise();      // remove scanner artifacts and speckle
            input.Contrast();     // normalize contrast for faded or overexposed scans
            input.Sharpen();      // sharpen blurred text edges

            result = _ocr.Read(input);
        }
        else
        {
            result = quickResult;
        }

        var invoiceNumber = Regex.Match(
            result.Text,
            @"Invoice\s*(?:Number|#|No\.?)?\s*:?\s*([A-Z0-9\-]+)",
            RegexOptions.IgnoreCase).Groups[1].Value;

        var total = Regex.Match(
            result.Text,
            @"(?:Total|Amount\s+Due)\s*:?\s*\$?([\d,]+\.\d{2})",
            RegexOptions.IgnoreCase).Groups[1].Value;

        return new ScanResult
        {
            InvoiceNumber    = invoiceNumber,
            Total            = total,
            DocumentConfidence = result.Confidence,
            PreprocessingApplied = quickResult.Confidence < 75
        };
    }
}

public class ScanResult
{
    public string InvoiceNumber        { get; set; }
    public string Total                { get; set; }
    public double DocumentConfidence   { get; set; }
    public bool   PreprocessingApplied { get; set; }
}
```

The two-pass pattern — quick read first, preprocessing on low confidence — avoids the overhead of full preprocessing on clean scans. For the scan quality scenarios most common in accounts payable (slightly skewed flatbed scans, fax artifacts, photocopied invoices), `Deskew()` and `DeNoise()` together recover most of the recognition accuracy. The [image quality correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/) documents every available filter with guidance on which combinations work best for different scan defects. The [image orientation correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-orientation-correction/) covers auto-rotation for documents scanned upside down.

### Searchable PDF Archiving for Financial Documents

Mindee does not produce any PDF output. Its architecture is input-only: submit a document, receive JSON fields. Teams archiving scanned invoices in a searchable format must maintain that pipeline separately from Mindee's extraction. IronOCR generates searchable PDFs directly from the same recognition pass used for field extraction — no additional library, no second processing step.

**Mindee Approach:**

```csharp
using Mindee;
using Mindee.Input;
using Mindee.Product.Invoice;

public class MindeeArchiveProcessor
{
    private readonly MindeeClient _client;

    public MindeeArchiveProcessor(string apiKey)
    {
        _client = new MindeeClient(apiKey);
    }

    public async Task ArchiveInvoiceAsync(string scanPath, string archivePath)
    {
        // Extract fields via Mindee (document transmitted to cloud)
        var inputSource = new LocalInputSource(scanPath);
        var response    = await _client.ParseAsync<InvoiceV4>(inputSource);
        var prediction  = response.Document.Inference.Prediction;

        // Mindee returns no PDF output — searchable PDF requires a separate library
        // (e.g., iTextSharp, PdfSharp, or a separate OCR library)
        // The scan at scanPath is the only PDF available for archiving;
        // it remains image-only with no embedded text layer
        Console.WriteLine($"Fields extracted. Searchable PDF: not available from Mindee.");
        Console.WriteLine($"Invoice: {prediction.InvoiceNumber?.Value}");
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;
using System.Text.RegularExpressions;

public class IronOcrArchiveProcessor
{
    private readonly IronTesseract _ocr = new IronTesseract();

    public ArchiveResult ArchiveInvoice(string scanPath, string archiveOutputPath)
    {
        // Apply preprocessing before recognition and archiving
        using var input = new OcrInput();
        input.LoadPdf(scanPath);   // works with both PDF scans and image files
        input.Deskew();
        input.DeNoise();

        var result = _ocr.Read(input);

        // Extract fields from the same recognition pass
        var invoiceNumber = Regex.Match(
            result.Text,
            @"Invoice\s*#?\s*:?\s*([A-Z0-9\-]+)",
            RegexOptions.IgnoreCase).Groups[1].Value;

        var vendor = result.Pages.FirstOrDefault()?.Lines
            .FirstOrDefault(l => l.Text.Trim().Length > 4)
            ?.Text.Trim();

        var totalMatch = Regex.Match(
            result.Text,
            @"Total\s*(?:Due)?\s*:?\s*\$?([\d,]+\.\d{2})",
            RegexOptions.IgnoreCase);

        // Write searchable PDF to archive in one call — no separate library needed
        result.SaveAsSearchablePdf(archiveOutputPath);

        return new ArchiveResult
        {
            InvoiceNumber    = invoiceNumber,
            VendorName       = vendor,
            Total            = totalMatch.Success ? totalMatch.Groups[1].Value : null,
            ArchivePath      = archiveOutputPath,
            DocumentConfidence = result.Confidence,
            PageCount        = result.Pages.Count()
        };
    }
}

public class ArchiveResult
{
    public string InvoiceNumber      { get; set; }
    public string VendorName         { get; set; }
    public string Total              { get; set; }
    public string ArchivePath        { get; set; }
    public double DocumentConfidence { get; set; }
    public int    PageCount          { get; set; }
}
```

`result.SaveAsSearchablePdf()` embeds the recognized text layer directly into the output PDF, producing a file where every word is selectable and searchable in any PDF viewer or enterprise content management system. Field extraction and archiving run in the same pass — one `_ocr.Read(input)` call provides both `result.Text` for pattern matching and `result.SaveAsSearchablePdf()` for the archive. The [searchable PDF guide](https://ironsoftware.com/csharp/ocr/how-to/searchable-pdf/) covers output options including HOCR export and the [PDF OCR use case page](https://ironsoftware.com/csharp/ocr/use-case/pdf-ocr-csharp/) covers production deployment patterns for document archiving pipelines.

### Removing the FinancialDocumentV1 Custom Endpoint

Mindee's `FinancialDocumentV1` API handles both invoices and receipts by auto-detecting document type. Teams using it eliminate branching logic at the cost of higher cloud dependency — every document regardless of type is uploaded. Replacing it with IronOCR uses word-level positional data to detect document structure and route extraction logic without any cloud call.

**Mindee Approach:**

```csharp
using Mindee;
using Mindee.Input;
using Mindee.Product.FinancialDocument;

public class MindeeFinancialRouter
{
    private readonly MindeeClient _client;

    public MindeeFinancialRouter(string apiKey)
    {
        _client = new MindeeClient(apiKey);
    }

    public async Task<FinancialSummary> ProcessFinancialDocumentAsync(string filePath)
    {
        // All financial documents uploaded for auto-detection and parsing
        var inputSource = new LocalInputSource(filePath);
        var response    = await _client.ParseAsync<FinancialDocumentV1>(inputSource);
        var prediction  = response.Document.Inference.Prediction;

        return new FinancialSummary
        {
            DocumentType  = prediction.DocumentType?.Value,  // "INVOICE" or "RECEIPT"
            InvoiceNumber = prediction.InvoiceNumber?.Value,
            Date          = prediction.Date?.Value?.ToString(),
            TotalAmount   = prediction.TotalAmount?.Value,
            SupplierName  = prediction.SupplierName?.Value,
            CustomerName  = prediction.CustomerName?.Value
        };
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;
using System.Text.RegularExpressions;

public class IronOcrFinancialRouter
{
    private readonly IronTesseract _ocr = new IronTesseract();

    public FinancialSummary ProcessFinancialDocument(string filePath)
    {
        var result = _ocr.Read(filePath);
        var text   = result.Text;

        // Detect document type using structural keywords from word data
        var isInvoice = DetectInvoice(result);

        if (isInvoice)
        {
            return new FinancialSummary
            {
                DocumentType  = "INVOICE",
                InvoiceNumber = Regex.Match(text,
                    @"Invoice\s*#?\s*:?\s*([A-Z0-9\-]+)",
                    RegexOptions.IgnoreCase).Groups[1].Value,
                Date          = Regex.Match(text,
                    @"(?:Invoice\s+)?Date\s*:?\s*(\d{1,2}[/-]\d{1,2}[/-]\d{2,4})",
                    RegexOptions.IgnoreCase).Groups[1].Value,
                TotalAmount   = ParseAmount(text,
                    @"Total\s*(?:Due|Amount)?\s*:?\s*\$?([\d,]+\.\d{2})"),
                SupplierName  = ExtractTopLine(result),
                CustomerName  = Regex.Match(text,
                    @"Bill\s+To\s*:?\s*\n?(.+)",
                    RegexOptions.IgnoreCase).Groups[1].Value.Trim()
            };
        }
        else
        {
            // Receipt structure: merchant at top, no "Bill To" section
            return new FinancialSummary
            {
                DocumentType = "RECEIPT",
                Date         = Regex.Match(text,
                    @"\b(\d{1,2}[/-]\d{1,2}[/-]\d{2,4})\b").Value,
                TotalAmount  = ParseAmount(text,
                    @"(?:Total|Amount\s+Due|Grand\s+Total)\s*:?\s*\$?([\d,]+\.\d{2})"),
                SupplierName = ExtractTopLine(result)
            };
        }
    }

    private bool DetectInvoice(OcrResult result)
    {
        // Invoice indicators: "Invoice", "Bill To", "Due Date", line items with quantities
        var text = result.Text;
        var invoiceSignals = new[] { "Invoice", "Bill To", "Due Date", "Purchase Order" };
        return invoiceSignals.Any(s =>
            text.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private string ExtractTopLine(OcrResult result)
    {
        return result.Pages
            .FirstOrDefault()
            ?.Lines
            .FirstOrDefault(l => l.Text.Trim().Length > 4)
            ?.Text.Trim();
    }

    private decimal? ParseAmount(string text, string pattern)
    {
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        if (match.Success &&
            decimal.TryParse(match.Groups[1].Value.Replace(",", ""), out var val))
            return val;
        return null;
    }
}

public class FinancialSummary
{
    public string   DocumentType  { get; set; }
    public string   InvoiceNumber { get; set; }
    public string   Date          { get; set; }
    public decimal? TotalAmount   { get; set; }
    public string   SupplierName  { get; set; }
    public string   CustomerName  { get; set; }
}
```

The document type detection logic replaces Mindee's server-side `DocumentType` field with a simple keyword scan against `result.Text`. For the vast majority of financial documents, the presence of "Invoice" or "Bill To" unambiguously identifies invoices; the absence identifies receipts. Word-level position data in `result.Pages` enables more sophisticated layout-based detection if your document set includes edge cases. The [read results guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) explains the full object model including `Words`, `Lines`, and `Paragraphs` with their bounding coordinates.

## Mindee API to IronOCR Mapping Reference

| Mindee | IronOCR Equivalent |
|---|---|
| `new MindeeClient(apiKey)` | `new IronTesseract()` — no API key required |
| `new LocalInputSource(filePath)` | `new OcrInput()` + `input.LoadImage(filePath)` or `ocr.Read(filePath)` |
| `_client.ParseAsync<InvoiceV4>(input)` | `_ocr.Read(filePath)` + regex extraction on `result.Text` |
| `_client.ParseAsync<ReceiptV5>(input)` | `_ocr.Read(filePath)` + receipt extraction patterns |
| `_client.ParseAsync<PassportV1>(input)` | `_ocr.Read(filePath)` + MRZ zone extraction via `CropRectangle` |
| `_client.ParseAsync<FinancialDocumentV1>(input)` | `_ocr.Read(filePath)` + document-type detection on `result.Text` |
| `response.Document.Inference.Prediction` | `result.Text`, `result.Pages`, `result.Lines`, `result.Words` |
| `prediction.InvoiceNumber?.Value` | `Regex.Match(result.Text, @"Invoice\s*#?\s*:?\s*([A-Z0-9\-]+)")` |
| `prediction.SupplierName?.Value` | Top-line of first page via `result.Pages[0].Lines.First()` |
| `prediction.TotalAmount?.Value` | `Regex.Match(result.Text, @"Total\s*:?\s*\$?([\d,]+\.\d{2})")` |
| `prediction.LineItems` | `result.Lines` filtered by line-item regex patterns |
| `prediction.SupplierPaymentDetails[].Iban` | `Regex.Match(result.Text, @"IBAN\s*:?\s*([\w\s]+)")` |
| `prediction.InvoiceDate?.Value` | `Regex.Match(result.Text, @"Date\s*:?\s*(\d{1,2}[/-]\d{1,2}[/-]\d{4})")` |
| `field.Confidence` | `result.Confidence` (document-level) or `word.Confidence` (per-word) |
| `catch (MindeeException ex)` | No equivalent — no network errors at recognition time |
| `await Task.Delay(100)` (rate limit) | Not required — no rate limits |
| `input.LoadPdf(path)` | `ocrInput.LoadPdf(path)` — same operation, fully local |
| API key in configuration | License key via `IronOcr.License.LicenseKey = "..."` — no rotation needed |

## Common Migration Issues and Solutions

### Issue 1: CropRectangle Coordinates Do Not Match Document Layout

**Mindee:** Spatial field coordinates are handled server-side. Mindee's model adapts to different invoice layouts without any coordinate configuration from the developer.

**Solution:** Measure zone coordinates against a representative sample of documents from your vendor set. Open a sample invoice in any image editor, read the pixel dimensions, and define `CropRectangle(x, y, width, height)` accordingly. For invoices with variable layouts, read the full document first and use the word position data to locate zones dynamically:

```csharp
var result  = _ocr.Read("invoice.jpg");
var allWords = result.Pages.First().Words;

// Find the word "Total" and read the region 50px to its right
var totalLabel = allWords.FirstOrDefault(w =>
    w.Text.Equals("Total", StringComparison.OrdinalIgnoreCase));

if (totalLabel != null)
{
    var valueRegion = new CropRectangle(
        totalLabel.X + totalLabel.Width + 5,
        totalLabel.Y - 5,
        200,
        totalLabel.Height + 10);

    using var valueInput = new OcrInput();
    valueInput.LoadImage("invoice.jpg", valueRegion);
    var valueResult = _ocr.Read(valueInput);
    Console.WriteLine($"Total: {valueResult.Text.Trim()}");
}
```

The [region-based OCR example](https://ironsoftware.com/csharp/ocr/examples/net-tesseract-content-area-rectangle-crop/) shows this dynamic zone pattern in a complete working example.

### Issue 2: Async Call Sites Break After Removing Mindee

**Mindee:** The entire Mindee API surface is async because it is network I/O. Controllers and service methods built on `await _client.ParseAsync<T>()` are async throughout the call chain.

**Solution:** IronOCR's `Read()` method is synchronous. Existing async call sites work immediately by wrapping the synchronous call in `Task.Run`:

```csharp
// Existing async signature preserved for callers
public async Task<string> ExtractInvoiceNumberAsync(string filePath)
{
    return await Task.Run(() =>
    {
        var result = new IronTesseract().Read(filePath);
        return Regex.Match(result.Text,
            @"Invoice\s*#?\s*:?\s*([A-Z0-9\-]+)",
            RegexOptions.IgnoreCase).Groups[1].Value;
    });
}
```

For high-throughput ASP.NET scenarios, review the [async OCR guide](https://ironsoftware.com/csharp/ocr/how-to/async/) before deciding between `Task.Run` wrappers and the native async path.

### Issue 3: Extraction Accuracy Drops on Low-Quality Scans

**Mindee:** Mindee's preprocessing is internal and automatic. Scan quality problems reduce field confidence scores with no developer intervention available before resubmission.

**Solution:** Apply IronOCR's preprocessing pipeline before recognition. The combination of `Deskew()` and `DeNoise()` recovers most accuracy on the typical flatbed scan defects seen in accounts payable workflows:

```csharp
using var input = new OcrInput();
input.LoadImage("faded-invoice.jpg");
input.Deskew();
input.DeNoise();
input.Contrast();    // for faded or low-contrast thermal paper receipts
input.Binarize();    // convert to clean black-and-white before recognition
var result = new IronTesseract().Read(input);
```

For severely degraded scans, `input.DeepCleanBackgroundNoise()` applies a heavier background removal pass. The [image quality correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/) documents thresholds and the [filter wizard](https://ironsoftware.com/csharp/ocr/how-to/filter-wizard/) helps identify which filters improve accuracy on a given document sample.

### Issue 4: API Key and Network Egress Configuration Removed

**Mindee:** The API key stored in app settings, the network egress rules permitting outbound HTTPS to `api.mindee.net`, and the retry logic wrapping `HttpRequestException` are all required infrastructure.

**Solution:** All three are removed together. The Mindee API key entry is deleted from configuration. The network egress rule is revoked. The `try/catch (HttpRequestException)` block is removed. The only remaining credential is the IronOCR license key, set once at startup:

```csharp
// appsettings.json: remove "Mindee:ApiKey"
// Firewall: revoke egress rule for api.mindee.net
// Startup.cs or Program.cs:
IronOcr.License.LicenseKey = Configuration["IronOcr:LicenseKey"];
// No rotation schedule, no secure storage beyond standard config secret management
```

### Issue 5: Multi-Page PDF Page Count Billing

**Mindee:** A 12-page vendor statement uploaded to Mindee consumes 12 pages against the monthly quota. At Pro tier overage rates, that single document incurs $1.20 in additional cost beyond the monthly allotment if you are near the limit.

**Solution:** IronOCR processes multi-page PDFs with zero per-page cost. Load the entire document and iterate pages:

```csharp
using var input = new OcrInput();
input.LoadPdf("vendor-statement.pdf");   // 12 pages — no billing unit increment
var result = new IronTesseract().Read(input);

foreach (var page in result.Pages)
    Console.WriteLine($"Page {page.PageNumber}: {page.Text.Length} characters recognized");
```

### Issue 6: FinancialDocumentV1 Dependency Cannot Be Substituted With a Single Pattern

**Mindee:** `FinancialDocumentV1` auto-detects document type and extracts fields without the calling code branching on document type.

**Solution:** Build a lightweight detector using keyword presence. Financial documents split cleanly into invoices (contain "Invoice", "Bill To", or "Due Date") and receipts (contain "Receipt", "Thank You", or lack the invoice markers). Two extraction methods plus a three-line detector replaces the Mindee API call without sacrificing accuracy on well-formed documents:

```csharp
var result  = _ocr.Read(filePath);
var summary = result.Text.IndexOf("Invoice", StringComparison.OrdinalIgnoreCase) >= 0
    ? ExtractInvoiceFields(result)
    : ExtractReceiptFields(result);
```

## Mindee Migration Checklist

### Pre-Migration

Audit all Mindee usage across the codebase:

```bash
grep -r "using Mindee" --include="*.cs" .
grep -r "MindeeClient\|ParseAsync\|InvoiceV4\|ReceiptV5\|PassportV1\|FinancialDocumentV1" --include="*.cs" .
grep -r "LocalInputSource\|MindeeException" --include="*.cs" .
grep -r "Mindee:ApiKey\|mindee_api_key\|MINDEE_API_KEY" --include="*.json" --include="*.env" --include="*.yml" .
```

Inventory findings:
- Count unique call sites using `ParseAsync<T>`
- Note which document types are used (`InvoiceV4`, `ReceiptV5`, `PassportV1`, `FinancialDocumentV1`)
- Identify all async method chains that propagate from Mindee calls
- Locate API key references in configuration files and secrets vaults
- Identify retry logic wrapping Mindee network calls
- Identify network egress rules permitting outbound traffic to Mindee endpoints

### Code Migration

1. Remove the `Mindee` NuGet package from the project file
2. Run `dotnet add package IronOcr` to install IronOCR
3. Add `IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY"` to application startup
4. Remove all `using Mindee`, `using Mindee.Input`, and `using Mindee.Product.*` directives
5. Replace `MindeeClient` constructor injection with `IronTesseract` instantiation or DI registration
6. Replace each `ParseAsync<InvoiceV4>` call with `_ocr.Read(filePath)` plus invoice extraction methods
7. Replace each `ParseAsync<ReceiptV5>` call with `_ocr.Read(filePath)` plus receipt extraction methods
8. Replace each `ParseAsync<PassportV1>` call with zone-based OCR using `CropRectangle` on the MRZ area
9. Replace each `ParseAsync<FinancialDocumentV1>` call with the document-type detector + routing pattern
10. Remove all `await Task.Delay(...)` rate-limit compliance lines
11. Remove all `catch (MindeeException)` and `catch (HttpRequestException)` blocks from OCR paths
12. Add `OcrInput` preprocessing calls (`Deskew`, `DeNoise`, `Contrast`) for scanned document paths
13. Add `result.SaveAsSearchablePdf(archivePath)` to any document archiving paths
14. Remove Mindee API key from all configuration files and secrets vaults
15. Update integration tests to run synchronously without network mocking

### Post-Migration

- Verify extraction accuracy against a 20-30 document sample set covering each document type
- Confirm `result.Confidence` values above 80 on representative clean scans; apply preprocessing if below
- Test preprocessing pipeline (`Deskew`, `DeNoise`) on skewed and degraded scan samples
- Verify multi-page PDFs produce correct `result.Pages.Count` and per-page content
- Confirm `result.SaveAsSearchablePdf()` output is searchable in Adobe Acrobat and system PDF viewers
- Test all zone-based extraction (`CropRectangle`) against document layout variations in your vendor set
- Run batch processing without `Task.Delay` calls and verify no threading issues
- Confirm application starts and runs correctly with no outbound internet access
- Verify license key initialization runs before the first `_ocr.Read()` call in each entry point
- Check that no Mindee API key references remain in configuration, environment variables, or secrets

## Key Benefits of Migrating to IronOCR

**Complete Data Sovereignty Over Financial Documents.** After migration, vendor IBAN codes, routing numbers, customer EINs, and itemized line items never leave your infrastructure. Compliance scope covers your organization only. Processor agreements with third-party AI vendors are removed from your audit trail. Teams operating under GLBA, CMMC, FedRAMP, or data residency requirements can process financial documents without the compliance review that external cloud transmission requires.

**Predictable Cost Regardless of Volume.** The IronOCR Lite license at $749 covers one developer with unlimited document processing. Scaling from 500 invoices per month to 50,000 invoices per month costs nothing additional. Year-end processing spikes, batch remediation runs, and development test cycles do not increment any billing counter. The three-year total cost of ownership for a team processing 5,000 pages per month drops from approximately $17,964 (Mindee Pro) to $1,499–$2,999 (IronOCR perpetual license). See the full [IronOCR product page](https://ironsoftware.com/csharp/ocr/) for tier details.

**Extraction Logic You Own Permanently.** IronOCR extraction patterns are plain C# code in your repository. They are version-controlled, reviewable, testable, and deployable independently of any external vendor's release schedule or API versioning decisions. When Mindee releases `InvoiceV5` and deprecates `InvoiceV4`, that is a migration deadline imposed by the vendor. When you maintain extraction patterns, improvement is a `git commit`.

**Preprocessing Control for Real-World Scan Quality.** Accounts payable operations receive invoices from dozens or hundreds of vendors, each scanned with different equipment at different settings. IronOCR's preprocessing pipeline — `Deskew()`, `DeNoise()`, `Contrast()`, `Sharpen()`, `Binarize()` — addresses the specific defects in each scan category. A thermal receipt photograph has different defects than a flatbed-scanned multi-page invoice; you apply the appropriate filters for each path. Mindee's preprocessing is invisible and non-configurable. See the [preprocessing features page](https://ironsoftware.com/csharp/ocr/features/preprocessing/) for the complete filter catalog.

**Searchable PDF Archiving in One Step.** Every invoice processed through IronOCR can be archived as a searchable PDF with `result.SaveAsSearchablePdf()`. The recognized text layer is embedded in the output file, making every word full-text searchable in document management systems, SharePoint libraries, and enterprise content repositories. Mindee provides no PDF output at all; searchable archiving previously required a separate library, a separate processing pass, and additional maintenance. After migration, extraction and archiving collapse into one `_ocr.Read(input)` call.

**Deployment Everywhere Without Architecture Changes.** IronOCR deploys on Windows, Linux, macOS, Docker, Azure App Service, AWS Lambda, and Google Cloud Run using the same NuGet package and the same initialization. Air-gapped environments, factory floor systems, and government secure facilities all work without modification. The [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/), [Azure guide](https://ironsoftware.com/csharp/ocr/get-started/azure/), and [AWS guide](https://ironsoftware.com/csharp/ocr/get-started/aws/) cover environment-specific configuration for each platform.
