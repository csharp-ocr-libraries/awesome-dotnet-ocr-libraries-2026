Azure Computer Vision's Read API costs $1.00 per 1,000 transactions, requires an Azure subscription to provision a Cognitive Services resource, and forces every OCR call through a three-step async dance: serialize your document to `BinaryData`, call `AnalyzeAsync`, then traverse nested blocks and lines to reconstruct the text. That is the minimum viable path — and every page of a multi-page PDF counts as a separate transaction. [IronOCR](https://ironsoftware.com/csharp/ocr/) collapses all of that to one synchronous method call, runs entirely within your infrastructure, and has no per-transaction meter.

## Understanding Azure Computer Vision

Azure Computer Vision is Microsoft's cloud-based cognitive service that exposes OCR through two primary APIs: the Image Analysis API (using `ImageAnalysisClient` with `VisualFeatures.Read`) for images, and Azure Form Recognizer's `DocumentAnalysisClient` for PDFs. Both are REST-backed services hosted in Azure data centers, accessed via the `Azure.AI.Vision.ImageAnalysis` and `Azure.AI.FormRecognizer.DocumentAnalysis` NuGet packages respectively.

Key architectural characteristics:

- **Cloud-first, always**: Every OCR operation transmits document data to Microsoft-managed servers over HTTPS. There is no local processing mode.
- **Subscription prerequisite**: Teams must create an Azure account, provision a Cognitive Services resource, obtain an endpoint URL, and generate an API key before writing a single line of OCR code.
- **Per-page transaction billing**: The Read API charges per image or per PDF page. A 50-page PDF is 50 transactions. Pricing starts at $1.00 per 1,000 transactions for the first million, dropping to $0.60 and then $0.40 at higher volumes.
- **Free tier ceiling**: 5,000 transactions per month on the free tier — enough for prototyping, not for production workloads.
- **Split service for PDFs**: Basic image OCR uses `ImageAnalysisClient`. Full PDF processing requires a separate service — Form Recognizer's `DocumentAnalysisClient` — with its own endpoint and configuration.
- **Async-only design**: All Read API calls are asynchronous. Local OCR can return results synchronously; cloud round-trips cannot. Every calling method in the chain must be `async`.
- **Rate limits**: The S1 tier caps at 10 transactions per second. High-volume batch processing requires either queuing logic or tier upgrades.
- **Error surface area**: Production code must handle HTTP 429 rate-limit responses, 5xx Azure service errors, network timeouts, authentication failures, and endpoint availability — each requiring separate retry logic.

### The Async Polling Pattern

The Read API's async requirement creates structural code consequences. Image analysis using `AnalyzeAsync` returns immediately but requires `await`; PDF processing through Form Recognizer requires `WaitUntil.Completed` to block until the operation finishes, or custom polling with `UpdateStatusAsync` for true async behavior. The result hierarchy then requires traversing blocks, lines, and words through nested loops:

```csharp
// Azure Computer Vision: image OCR
// Requires: Azure subscription + Cognitive Services resource + endpoint + API key
using Azure;
using Azure.AI.Vision.ImageAnalysis;

public class AzureOcrService
{
    private readonly ImageAnalysisClient _client;

    public AzureOcrService(string endpoint, string apiKey)
    {
        // Endpoint and key provisioned in Azure portal
        _client = new ImageAnalysisClient(
            new Uri(endpoint),
            new AzureKeyCredential(apiKey));
    }

    public async Task<string> ExtractTextAsync(string imagePath)
    {
        // Document is uploaded to Microsoft Azure
        using var stream = File.OpenRead(imagePath);
        var imageData = BinaryData.FromStream(stream);

        var result = await _client.AnalyzeAsync(
            imageData,
            VisualFeatures.Read);

        var text = new StringBuilder();
        foreach (var block in result.Value.Read.Blocks)
        {
            foreach (var line in block.Lines)
            {
                text.AppendLine(line.Text);
            }
        }

        return text.ToString();
    }
}
```

PDF processing escalates the complexity further — a separate `DocumentAnalysisClient` with its own endpoint, `AnalyzeDocumentAsync` with `WaitUntil.Completed`, and a different result shape using `result.Pages` and `page.Lines` accessing `.Content` instead of `.Text`.

## Understanding IronOCR

[IronOCR](https://ironsoftware.com/csharp/ocr/) is a commercial on-premise OCR library for .NET, delivered as a single NuGet package. It wraps an optimized Tesseract 5 engine with automatic preprocessing, native PDF support, and a synchronous API that requires no cloud credentials, no endpoint configuration, and no async plumbing.

Key characteristics:

- **Single NuGet deployment**: `dotnet add package IronOcr` installs everything — OCR engine, native binaries, and language data for English. No tessdata folders, no separate native library downloads.
- **Perpetual licensing**: $749 Lite / $1,499 Plus / $2,999 Professional / $5,999 Unlimited — one-time purchase, not a subscription. No per-document fees at any volume.
- **Local processing**: All OCR runs within your process. Documents never leave your infrastructure.
- **Automatic preprocessing**: Deskew, DeNoise, Contrast, Binarize, and resolution enhancement are applied automatically or via explicit filter calls on `OcrInput`.
- **Native PDF support**: `IronTesseract.Read("document.pdf")` handles PDFs directly, including password-protected files, without a separate service or additional NuGet package.
- **125+ languages**: Installed via separate language NuGet packages — `IronOcr.Languages.French`, `IronOcr.Languages.ChineseSimplified`, etc. — with no manual tessdata management.
- **Thread-safe**: `IronTesseract` is safe to use concurrently. Batch workloads can use `Parallel.ForEach` without additional synchronization.
- **Structured output**: `OcrResult` exposes `.Pages`, `.Paragraphs`, `.Lines`, `.Words`, and `.Barcodes` collections with per-element coordinates, confidence scores, and bounding rectangles.

## Feature Comparison

| Feature | Azure Computer Vision | IronOCR |
|---|---|---|
| **Processing location** | Microsoft Azure cloud | Local, on-premise |
| **Pricing model** | Per-transaction ($1.00/1K) | Perpetual license ($749+) |
| **Internet required** | Yes, always | No |
| **PDF support** | Via Form Recognizer (separate) | Built-in, native |
| **Setup complexity** | Azure account + resource + keys | NuGet install |
| **API pattern** | Async (cloud I/O) | Synchronous (local) |
| **Rate limits** | 10 TPS (S1) | Hardware-bound only |

### Detailed Feature Comparison

| Feature | Azure Computer Vision | IronOCR |
|---|---|---|
| **Setup and deployment** | | |
| NuGet install | Multiple packages | `dotnet add package IronOcr` |
| Credential configuration | Endpoint URL + API key | License key string |
| Azure subscription required | Yes | No |
| Internet connectivity required | Yes, every request | No |
| Air-gapped deployment | Impossible | Fully supported |
| Docker deployment | Requires outbound network | Self-contained |
| **OCR capabilities** | | |
| Image OCR | Yes (`AnalyzeAsync`) | Yes (`Read()`) |
| PDF OCR | Via Form Recognizer (extra service) | Native, built-in |
| Password-protected PDF | Via Form Recognizer | Single `Password:` parameter |
| Multi-page PDF (per-page billing) | Yes — each page = 1 transaction | No per-page cost |
| Searchable PDF output | Manual construction | `SaveAsSearchablePdf()` |
| Automatic preprocessing | Limited server-side | Deskew, DeNoise, Contrast, Binarize |
| Barcode reading during OCR | Limited | `ReadBarCodes = true` |
| Region-based OCR | Not directly (crop manually) | `CropRectangle` on `OcrInput` |
| **Language support** | | |
| Language count | 164+ | 125+ |
| Language installation | Service-level (cloud handles it) | NuGet language packs |
| Multiple simultaneous languages | Yes | Yes (`AddSecondaryLanguage`) |
| **Output and structure** | | |
| Plain text | Yes | Yes |
| Per-word bounding boxes | Polygon-based | Rectangle-based |
| Per-word confidence scores | Yes | Yes (0-100 scale) |
| Structured hierarchy | Blocks / Lines / Words | Pages / Paragraphs / Lines / Words |
| hOCR export | No | Yes (`SaveAsHocrFile`) |
| **Cost and compliance** | | |
| Per-document cost | $0.001 per page (Form Recognizer) | None |
| HIPAA-compliant deployment | Complex (BAA + cloud) | Straightforward (local only) |
| ITAR suitability | Not for controlled data | Fully on-premise |
| FedRAMP air-gapped | No | Yes |
| **Reliability** | | |
| Network failure modes | Yes | None |
| Rate limit errors | Yes (429 at 10 TPS) | None |
| Service availability SLA | 99.9% (Azure) | Your infrastructure |

## Cost Model

The transaction pricing gap between Azure Computer Vision and IronOCR becomes decisive at production volume. The cost calculator from the Azure source files shows the math precisely.

### Azure Computer Vision Approach

Azure bills per transaction using tiered pricing: $1.00 per 1,000 transactions for the first million, $0.60 for the next 9 million, and $0.40 beyond 10 million. Each PDF page is one transaction. A 10-page PDF is 10 billable calls.

```csharp
// Azure pricing tiers (per 1,000 transactions)
// Tier1: $1.00 (0-1M)
// Tier2: $0.60 (1M-10M)
// Tier3: $0.40 (10M+)

// Free tier: 5,000 transactions/month

// 10,000 single-page docs/month:
//   Billable: 5,000 transactions (after free tier)
//   Cost: $5.00/month = $60/year

// 50,000 single-page docs/month:
//   Billable: 45,000 transactions
//   Cost: $45.00/month = $540/year

// 100,000 single-page docs/month:
//   Billable: 95,000 transactions
//   Cost: $95.00/month = $1,140/year

// 10,000 x 5-page PDFs/month = 50,000 transactions
//   Cost: $45.00/month = $540/year
//   (Every page multiplies the bill)
```

At 50,000 documents per month, the IronOCR Lite license ($749) pays for itself in under two months of Azure savings. At 100,000 documents per month, IronOCR Professional ($2,999) pays back in under two and a half years — and then costs nothing more.

### IronOCR Approach

[IronOCR's pricing model](https://ironsoftware.com/csharp/ocr/licensing/) is a single number. Install the NuGet package, set the license key, and process any volume without a counter ticking:

```csharp
// Install: dotnet add package IronOcr
// License: one-time, perpetual

IronOcr.License.LicenseKey = Environment.GetEnvironmentVariable("IRONOCR_LICENSE");

var ocr = new IronTesseract();

// Process 1 document or 1 million — same cost
foreach (var path in documentPaths)
{
    var result = ocr.Read(path);
    Console.WriteLine($"Processed: {path}");
}

// Multi-page PDFs — no per-page billing
foreach (var path in pdfPaths)
{
    // 1 page or 100 pages, still no extra cost
    var result = ocr.Read(path);
    Console.WriteLine($"{path}: {result.Pages.Length} pages processed");
}
```

No metering, no usage tracking, no budget alerts needed. Predictable costs from day one. See the [reading text from images tutorial](https://ironsoftware.com/csharp/ocr/tutorials/how-to-read-text-from-an-image-in-csharp-net/) for a complete getting-started walkthrough.

## Data Sovereignty and Offline Capability

The compliance implications of cloud OCR are not theoretical. Every document processed by Azure Computer Vision crosses an organizational boundary. The Azure README documents the specific regulatory frameworks affected: HIPAA-covered entities, ITAR defense contractors, CMMC-certified organizations, GDPR-regulated European companies, and any operation in an air-gapped network.

### Azure Computer Vision Approach

Even with regional endpoint selection and a signed Business Associate Agreement, the data flow is fixed:

```csharp
// Azure: data flow for every OCR call
// 1. Your application reads the file
// 2. File is serialized to BinaryData
// 3. HTTPS transmission to Azure data center
// 4. Microsoft infrastructure processes the document
// 5. Result returned over HTTPS
// 6. You parse the result

using var stream = File.OpenRead(imagePath);
var imageData = BinaryData.FromStream(stream);  // Document in memory

// This call transmits your document to Azure
var result = await _client.AnalyzeAsync(
    imageData,           // Document leaves your network here
    VisualFeatures.Read);
```

An air-gapped network cannot reach the endpoint URL at all — Azure Computer Vision has no offline mode. For organizations running SCIFs, military installations, or isolated processing environments, the service is architecturally incompatible regardless of pricing.

### IronOCR Approach

IronOCR processes documents within the calling process. There is no outbound connection:

```csharp
// IronOCR: data never leaves your infrastructure
using IronOcr;

public class OnPremiseOcrService
{
    private readonly IronTesseract _ocr = new IronTesseract();

    public string ExtractText(string imagePath)
    {
        // Runs entirely in-process
        // No network call, no serialization to external endpoint
        var result = _ocr.Read(imagePath);
        return result.Text;
    }

    public string ExtractFromPdf(string pdfPath)
    {
        // PDF processed entirely on-premise, native support
        using var input = new OcrInput();
        input.LoadPdf(pdfPath);
        return _ocr.Read(input).Text;
    }

    public string ExtractFromEncryptedPdf(string pdfPath, string password)
    {
        // Encrypted PDFs also stay local
        using var input = new OcrInput();
        input.LoadPdf(pdfPath, Password: password);
        return _ocr.Read(input).Text;
    }
}
```

The Docker deployment removes outbound network requirements entirely:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN apt-get update && apt-get install -y libgdiplus
WORKDIR /app
COPY --from=build /app/publish .
ENV IRONOCR_LICENSE=your-key
# No Azure endpoint, no API key, no outbound network rules needed
ENTRYPOINT ["dotnet", "YourApp.dll"]
```

For HIPAA-covered entities, ITAR compliance, or FedRAMP air-gapped scenarios, IronOCR eliminates the entire category of third-party data processor risk. See the [Azure deployment guide](https://ironsoftware.com/csharp/ocr/get-started/azure/) for running IronOCR within Azure infrastructure while keeping documents local to the compute instance, and the [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/) for container configuration.

## Synchronous vs. Asynchronous API Design

The Azure Read API is async because it cannot be synchronous — cloud I/O has network latency. IronOCR processes locally and can return synchronously, which simplifies calling code, eliminates `async` propagation through the call stack, and removes the failure modes inherent in network I/O.

### Azure Computer Vision Approach

Every Azure OCR call requires `async`/`await`. Production code adds retry logic for 429 rate-limit errors and 5xx service errors. A minimal production implementation looks like this:

```csharp
public async Task<string> RobustExtractAsync(string imagePath)
{
    const int maxRetries = 3;
    int attempt = 0;

    while (attempt < maxRetries)
    {
        try
        {
            using var stream = File.OpenRead(imagePath);
            var imageData = BinaryData.FromStream(stream);

            var result = await _client.AnalyzeAsync(
                imageData,
                VisualFeatures.Read);

            return string.Join("\n",
                result.Value.Read.Blocks
                    .SelectMany(b => b.Lines)
                    .Select(l => l.Text));
        }
        catch (RequestFailedException ex) when (ex.Status == 429)
        {
            // Rate limited — Azure caps S1 at 10 TPS
            attempt++;
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }
        catch (RequestFailedException ex) when (ex.Status >= 500)
        {
            // Azure service error
            attempt++;
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
        catch (RequestFailedException ex)
        {
            // Client error — bad credential, invalid endpoint
            throw new Exception($"Azure OCR failed: {ex.Message}", ex);
        }
    }

    throw new Exception("Max retries exceeded for Azure OCR");
}
```

This is not defensive over-engineering — it is the minimum required for any Azure API call in production. Rate limits, service outages, and transient errors are real conditions any Azure consumer will encounter.

### IronOCR Approach

Local processing eliminates the network failure surface. The error handling scope narrows to file system and input validation:

```csharp
// No async required — local processing returns synchronously
public string ExtractText(string imagePath)
{
    var result = new IronTesseract().Read(imagePath);
    return result.Text;
}

// One line for simple cases
public string OneLineOcr(string imagePath)
{
    return new IronTesseract().Read(imagePath).Text;
}

// Confidence-aware extraction
public (string Text, double Confidence) ExtractWithConfidence(string imagePath)
{
    var result = new IronTesseract().Read(imagePath);
    return (result.Text, result.Confidence);
}
```

No `RequestFailedException`, no retry loops, no `Task.WhenAll` coordination for batch jobs. If you need async for integration with an async controller pipeline, `Task.Run(() => ocr.Read(path))` wraps the synchronous call without structural changes to the OCR logic itself. The [IronTesseract API reference](https://ironsoftware.com/csharp/ocr/object-reference/api/IronOcr.IronTesseract.html) covers the full synchronous interface. For workloads that genuinely need async patterns, IronOCR also provides a dedicated [async OCR guide](https://ironsoftware.com/csharp/ocr/how-to/async/).

## Credential and Endpoint Configuration

Azure Computer Vision requires provisioning infrastructure before the first test. IronOCR requires a NuGet install and optionally a license key string.

### Azure Computer Vision Approach

The Azure setup sequence before writing any OCR code:

1. Create an Azure account if one does not exist.
2. Navigate to the Azure portal and create a Cognitive Services resource (or an Azure AI Services resource).
3. Select a pricing tier and region.
4. Copy the endpoint URL (format: `https://your-resource.cognitiveservices.azure.com/`).
5. Copy one of the two API keys.
6. Store both values securely — in environment variables, Azure Key Vault, or `appsettings.json` (non-production only).
7. Install `Azure.AI.Vision.ImageAnalysis` via NuGet.
8. Initialize `ImageAnalysisClient` with the endpoint and credential.

For PDF processing, repeat steps 2-8 for a Form Recognizer resource with a different NuGet package (`Azure.AI.FormRecognizer`) and a different client class (`DocumentAnalysisClient`).

The `appsettings.json` stores endpoint and key:

```json
{
  "Azure": {
    "ComputerVision": {
      "Endpoint": "https://your-resource.cognitiveservices.azure.com/",
      "ApiKey": "your-api-key"
    }
  }
}
```

Rotating API keys, handling credential expiration, and managing endpoint URLs across environments (dev, staging, production) are ongoing operational tasks with no equivalent in local processing.

### IronOCR Approach

The [IronTesseract setup guide](https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/) reduces setup to two steps:

```bash
dotnet add package IronOcr
```

```csharp
// Set once at application startup — environment variable recommended
IronOcr.License.LicenseKey = Environment.GetEnvironmentVariable("IRONOCR_LICENSE");

// Then use immediately — no endpoint, no credential rotation, no portal setup
var text = new IronTesseract().Read("document.jpg").Text;
```

The license key is a static string. It does not expire per request, does not require rotation, and does not call home for validation after activation. Deployment environments need no outbound firewall rules for OCR to function.

## API Mapping Reference

| Azure Computer Vision | IronOCR Equivalent |
|---|---|
| `ImageAnalysisClient` | `IronTesseract` |
| `new AzureKeyCredential(apiKey)` | `IronOcr.License.LicenseKey = key` |
| `client.AnalyzeAsync(data, VisualFeatures.Read)` | `ocr.Read(imagePath)` |
| `BinaryData.FromStream(stream)` | `input.LoadImage(stream)` |
| `result.Value.Read.Blocks` | `result.Paragraphs` |
| `block.Lines` | `result.Lines` |
| `line.Text` | `line.Text` |
| `line.Words` | `result.Words` |
| `word.Confidence` | `word.Confidence` (0-100 scale vs Azure's 0-1) |
| `word.BoundingPolygon` | `word.X`, `word.Y`, `word.Width`, `word.Height` |
| `DocumentAnalysisClient` | `IronTesseract` + `OcrInput` |
| `AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-read", stream)` | `ocr.Read(input)` with `input.LoadPdf(path)` |
| `operation.Value.Pages` | `result.Pages` |
| `page.Lines` / `line.Content` | `result.Lines` / `line.Text` |
| `RequestFailedException` (429 retry) | Not applicable — no rate limits |
| `RequestFailedException` (500 retry) | Not applicable — no service errors |

## When Teams Consider Moving from Azure Computer Vision to IronOCR

### Compliance Requirements Block Cloud Processing

A healthcare ISV building a document management system starts on Azure Computer Vision because it is fast to integrate. Then the first enterprise customer arrives — a hospital system with a HIPAA security officer who asks two questions: "Where does our PHI go?" and "Can you show us the Business Associate Agreement covering that third party?" Azure has a BAA, but the answer to the first question — "Microsoft data centers" — triggers a lengthy security review, a request for Microsoft's audit reports, and a compliance timeline that delays the contract. Switching to IronOCR removes the question entirely. PHI never leaves the customer's environment. The compliance scope stays within the customer's own organization.

### Volume Growth Makes Per-Transaction Pricing Untenable

An operations team launches an invoice processing pipeline at 5,000 documents per month — comfortably within Azure's free tier. Six months later, the pipeline processes 80,000 invoices per month, averaging 3 pages each. That is 240,000 transactions. At $1.00 per 1,000 transactions (after the 5,000 free), the monthly bill is $235 and the annual cost is $2,820. At that annual rate, IronOCR Professional ($2,999) pays for itself in 13 months and costs nothing in year two. Teams that project even moderate volume growth reach break-even quickly, after which every additional document is savings.

### Network Latency Affects Processing SLAs

A document processing service targets a 2-second end-to-end SLA. Azure Computer Vision adds 200-800ms of network latency for same-region calls and 500-2000ms for cross-region deployments — before the OCR computation itself. Under load, the 10 TPS rate limit on S1 forces queuing, which inflates latency further. IronOCR processes a single 300 DPI image in 100-400ms on commodity server hardware with no queue, no rate cap, and no network hop. The SLA becomes predictable because it depends only on hardware, not on Azure service health or network conditions.

### Air-Gapped Infrastructure Requirements

Defense contractors, intelligence agencies, and critical infrastructure operators run workloads on networks with no internet connectivity by design. Azure Computer Vision is technically incompatible with these environments — the endpoint cannot be reached. Teams in these sectors need a library that deploys as a self-contained binary, operates without any outbound connection, and passes security reviews that explicitly prohibit cloud data transmission. IronOCR's [Linux deployment](https://ironsoftware.com/csharp/ocr/get-started/linux/) and Docker support make it deployable in restricted environments without modification.

### Simplifying Multi-Environment Deployment

A team managing dev, staging, and production environments for a SaaS application carries three Azure Cognitive Services resources, three sets of API keys, and three endpoint URLs — each requiring secure storage, rotation policies, and environment-specific configuration. Every deployment environment needs outbound network access to Azure. IronOCR reduces the per-environment configuration to one environment variable (`IRONOCR_LICENSE`), eliminates the network access requirement, and removes the operational overhead of credential management across environments.

## Common Migration Considerations

### Async to Synchronous Pattern

Azure Consumer code is `async` by necessity. IronOCR does not require async, but the transition is mechanical. Replace `async Task<string>` return types with `string`, remove `await` and `async` keywords, and delete the retry loop. If the calling method is an ASP.NET controller or service that must remain async, wrap the IronOCR call in `Task.Run`:

```csharp
// Before: Azure async chain
public async Task<string> ReadTextAsync(string imagePath)
{
    using var stream = File.OpenRead(imagePath);
    var data = BinaryData.FromStream(stream);
    var result = await _client.AnalyzeAsync(data, VisualFeatures.Read);
    return string.Join("\n", result.Value.Read.Blocks
        .SelectMany(b => b.Lines)
        .Select(l => l.Text));
}

// After: IronOCR synchronous
public string ReadText(string imagePath)
{
    return new IronTesseract().Read(imagePath).Text;
}

// If async signature must be preserved for interface compatibility
public Task<string> ReadTextAsync(string imagePath)
{
    return Task.Run(() => new IronTesseract().Read(imagePath).Text);
}
```

### Confidence Scale Normalization

Azure Computer Vision returns word confidence as a `float` between 0 and 1. IronOCR returns confidence as a `double` on a 0-100 scale. Any code that thresholds on Azure confidence values needs adjustment:

```csharp
// Azure: confidence is 0.0 - 1.0
foreach (var word in line.Words)
{
    if (word.Confidence > 0.85f) { /* high confidence */ }
}

// IronOCR: confidence is 0 - 100
var result = new IronTesseract().Read(imagePath);
foreach (var word in result.Words)
{
    if (word.Confidence > 85.0) { /* equivalent threshold */ }
}
Console.WriteLine($"Overall: {result.Confidence}%");
```

The [OcrResult API reference](https://ironsoftware.com/csharp/ocr/object-reference/api/IronOcr.OcrResult.html) documents all result properties including the confidence scale. The [confidence scores how-to guide](https://ironsoftware.com/csharp/ocr/how-to/tesseract-result-confidence/) covers threshold selection and per-element interpretation.

### PDF Processing Service Consolidation

Azure splits image OCR and PDF OCR across two separate services with separate clients, NuGet packages, and endpoint configurations. Migrating means consolidating both paths into a single `IronTesseract` instance. The `OcrInput.LoadPdf` method accepts a file path, stream, or byte array, with an optional `Password` parameter for encrypted files — no second client required:

```csharp
// Before: Two separate Azure clients for images vs PDFs
// Image: ImageAnalysisClient + AnalyzeAsync
// PDF:   DocumentAnalysisClient + AnalyzeDocumentAsync(WaitUntil.Completed, ...)

// After: One IronTesseract instance handles both
var ocr = new IronTesseract();

// Image
var imageResult = ocr.Read("document.jpg");

// PDF (same client, same Read method)
using var pdfInput = new OcrInput();
pdfInput.LoadPdf("document.pdf");
var pdfResult = ocr.Read(pdfInput);

// Password-protected PDF
using var encInput = new OcrInput();
encInput.LoadPdf("secured.pdf", Password: "secret");
var encResult = ocr.Read(encInput);

// Searchable PDF output — no manual construction
pdfResult.SaveAsSearchablePdf("searchable-output.pdf");
```

The [PDF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-pdfs/) covers page range selection, stream input, and the native PDF rendering pipeline. For image inputs, see the [image input guide](https://ironsoftware.com/csharp/ocr/how-to/input-images/).

### Structured Data Extraction Without Form Recognizer

Teams using Form Recognizer's prebuilt models (invoices, receipts, identity documents) for field extraction need to replicate that extraction logic in IronOCR using region-based OCR with `CropRectangle`. The positional extraction is explicit rather than model-based:

```csharp
// Form Recognizer extracted named fields automatically
// IronOCR: define extraction zones for known document layouts
var ocr = new IronTesseract();

// Define regions matching the document template
var vendorZone   = new CropRectangle(0,   0,   300, 100);
var invoiceDate  = new CropRectangle(400, 0,   200, 50);
var totalAmount  = new CropRectangle(400, 500, 200, 100);

string vendor, date, total;

using (var input = new OcrInput())
{
    input.LoadImage("invoice.jpg", vendorZone);
    vendor = ocr.Read(input).Text.Trim();
}

using (var input = new OcrInput())
{
    input.LoadImage("invoice.jpg", invoiceDate);
    date = ocr.Read(input).Text.Trim();
}

using (var input = new OcrInput())
{
    input.LoadImage("invoice.jpg", totalAmount);
    total = ocr.Read(input).Text.Trim();
}
```

The [region-based OCR guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-region-of-an-image/) covers `CropRectangle` usage in detail. For invoice-specific workflows, the [invoice OCR tutorial](https://ironsoftware.com/csharp/ocr/blog/using-ironocr/invoice-ocr-csharp-tutorial/) demonstrates the full extraction pattern.

## Additional IronOCR Capabilities

Beyond the comparison points above, IronOCR provides capabilities that Azure Computer Vision does not expose through its standard OCR API:

- **[Scanned document processing](https://ironsoftware.com/csharp/ocr/how-to/read-scanned-document/)**: The full preprocessing pipeline — deskew, denoise, contrast, binarization, sharpen — is applied before the OCR engine sees the image, improving accuracy on scans that return empty or low-confidence results from cloud APIs.
- **[Progress tracking for long documents](https://ironsoftware.com/csharp/ocr/how-to/progress-tracking/)**: Subscribe to progress events during multi-page PDF processing — useful for long-running batch jobs with UI feedback requirements.
- **[Computer vision preprocessing](https://ironsoftware.com/csharp/ocr/how-to/computer-vision/)**: Deep learning-based preprocessing for challenging documents such as photos taken at angles or under inconsistent lighting.

## .NET Compatibility and Future Readiness

IronOCR targets .NET 8, .NET 9, .NET Standard 2.0, and .NET Framework 4.6.2 through 4.8 from a single NuGet package. It supports Windows x64, Windows x86, Linux x64, macOS (Intel and Apple Silicon), and ARM64 — covering the full range of modern .NET deployment targets including Azure App Service, AWS Lambda, Docker containers, and on-premise Linux servers. Azure Computer Vision's .NET SDK (`Azure.AI.Vision.ImageAnalysis`) also maintains modern .NET compatibility, but the cloud architecture means compatibility with the language runtime is secondary to compatibility with the Azure endpoint, which is versioned and updated independently of the SDK. IronOCR ships language and engine updates through NuGet, keeping the update model consistent with the rest of the .NET ecosystem.

## Conclusion

Azure Computer Vision is a capable OCR service for teams already operating within the Azure ecosystem whose documents carry no regulatory restrictions on cloud transmission, and whose volume fits within free-tier or low-volume paid tiers. The async API is functional, the accuracy on standard documents is reliable, and the Form Recognizer prebuilt models reduce development effort for structured document types like invoices and receipts.

The cost model, however, does not scale. At 50,000 documents per month, IronOCR Lite pays for itself in under two months of saved Azure transaction fees. The per-page billing for multi-page PDFs compounds the cost. Every operational year beyond break-even is money that does not go to Microsoft. For any team projecting growth beyond 10,000 documents per month, the long-term economics favor a perpetual on-premise license.

The data sovereignty argument is more absolute. If PHI, ITAR-controlled data, attorney-client privileged communications, or any document category that cannot legally or contractually cross an organizational boundary flows through your OCR pipeline, Azure Computer Vision is excluded from the design — not disadvantaged, excluded. IronOCR's local processing model handles those workloads without architectural compromise.

The async polling complexity is real overhead. The retry logic, the rate-limit handling, the network failure modes, and the split between `ImageAnalysisClient` and `DocumentAnalysisClient` for images versus PDFs all add code that has no OCR value — it is cloud-integration code. IronOCR's synchronous `Read()` method handles images and PDFs with identical code, no async propagation required, and no retry logic needed. For teams who want to spend engineering effort on their application rather than on cloud API plumbing, that simplicity has compounding value over the life of a project.
