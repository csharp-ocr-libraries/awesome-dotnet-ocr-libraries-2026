# Migrating from Klippa OCR to IronOCR

This guide is for .NET developers who have integrated Klippa's REST API and are moving to [IronOCR](https://ironsoftware.com/csharp/ocr/) for on-premise document processing. It covers the practical steps to remove the HTTP client infrastructure, eliminate JSON deserialization, and replace cloud-dependent document uploads with local OCR calls that never touch the network.

## Why Migrate from Klippa OCR

Klippa is a cloud-only document intelligence service with no .NET SDK. Every integration is a hand-built REST client. That architectural reality has downstream consequences that compound over the life of a production system.

**No NuGet Package Means You Own the Integration Layer.** There is nothing to install. The entry cost is writing an `HttpClient` wrapper, configuring `X-Auth-Key` authentication headers, building `MultipartFormDataContent` request bodies, deserializing Klippa's JSON response schema, and wiring up retry logic for transient failures. That is 2-4 days of plumbing before the first document is reliably processed in production. When Klippa updates their API schema, your deserialization code breaks and requires manual maintenance.

**Every Document Upload Is a Network Dependency.** Klippa processes documents exclusively on EU-hosted servers. Production outages at the Klippa end, elevated latency, or any interruption in outbound internet access from your application server halts document processing entirely. There is no fallback, no local mode, and no retry that resolves a cloud service being unavailable.

**Sensitive Documents Leave Your Infrastructure.** Financial documents — receipts with payment details, invoices with VAT numbers and amounts, identity documents with passport data — are transmitted to a third-party server on every API call. GDPR's data transfer provisions address some of this for EU-based processing, but audit scope still extends to Klippa's infrastructure, data retention policies, and sub-processors. For teams with healthcare, legal, financial services, or government contracts, "EU-hosted" does not satisfy the requirement that data not leave the organization.

**Per-Document Pricing Scales Without a Ceiling.** Klippa does not publish pricing. At any meaningful document volume — 10,000 receipts per month in an expense management system, 500 invoices per day in an AP automation workflow — the per-document billing model accrues costs that a perpetual license never would. The cost trajectory is tied directly to business growth, which is the opposite of what infrastructure spending should do.

**The Specialist Scope Breaks When Requirements Expand.** Klippa is trained on receipts, invoices, and identity documents. An application that starts as expense management rarely stays there. The first time a document type outside those three categories appears — a scanned employment agreement, a medical form, a technical drawing, a purchase order with non-standard layout — Klippa returns nothing useful. IronOCR processes any document that contains text, without category restrictions.

**Async-Only REST Calls Add Latency in Synchronous Contexts.** Every Klippa call is an async HTTP operation. A single document round-trip takes 500ms to 2000ms over the network. IronOCR processes the same document locally in 100-400ms without the async overhead in scenarios where synchronous processing fits the architecture better.

### The Fundamental Problem

Klippa has no SDK. OCR means constructing and sending an HTTP request, then deserializing JSON:

```csharp
// Klippa: 15+ lines of HTTP plumbing before you read a single character
var content = new MultipartFormDataContent();
content.Add(new ByteArrayContent(File.ReadAllBytes(imagePath)), "document", "receipt.jpg");
_client.DefaultRequestHeaders.Add("X-Auth-Key", _apiKey); // auth header — rotates, breaks, leaks

var response = await _client.PostAsync(
    "https://custom-ocr.klippa.com/api/v1/parseDocument", content);
response.EnsureSuccessStatusCode(); // throws on 4xx/5xx — no retry, document lost

var json = await response.Content.ReadAsStringAsync();
var parsed = JsonSerializer.Deserialize<KlippaResponse>(json); // your schema, your maintenance
var text = parsed?.Data?.ParsedDocument?.Text; // nullable chain — breaks when schema changes
```

IronOCR replaces all of it:

```csharp
// IronOCR: no HTTP, no auth headers, no JSON — just text
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
var text = new IronTesseract().Read(imagePath).Text;
```

## IronOCR vs Klippa OCR: Feature Comparison

The table below compares the two libraries across the dimensions that matter most for a production migration decision.

| Feature | Klippa OCR | IronOCR |
|---|---|---|
| Deployment model | Cloud-only (EU servers) | On-premise, fully local |
| .NET SDK / NuGet package | None | `IronOcr` NuGet package |
| Internet required | Yes, on every call | Never |
| Document data leaves network | Always | Never |
| General-purpose OCR | No (receipts, invoices, IDs only) | Yes (any document type) |
| Authentication setup | `X-Auth-Key` HTTP header | `IronOcr.License.LicenseKey` string |
| HTTP client required | Yes | No |
| Response deserialization | Manual JSON parsing | Typed `OcrResult` object |
| Retry/timeout logic | Hand-rolled | Not needed (local call) |
| Offline / air-gapped support | No | Yes |
| PDF input | Yes (cloud) | Yes (native, local) |
| Multi-page TIFF input | Unknown | Yes |
| Image input formats | JPG, PNG (cloud) | JPG, PNG, BMP, TIFF, GIF, and more |
| Stream and byte array input | No SDK | Yes |
| Automatic image preprocessing | Cloud-side (opaque) | Yes (Deskew, DeNoise, Contrast, Binarize, Sharpen) |
| Structured output: word coordinates | No | Yes |
| Confidence scores per word | No | Yes |
| Searchable PDF output | No | Yes |
| Barcode reading during OCR | No | Yes |
| Multi-language support | Limited to trained document types | 125+ languages |
| Thread safety | N/A (HTTP calls) | Yes (one `IronTesseract` per thread) |
| Cross-platform deployment | REST-agnostic | Windows, Linux, macOS, Docker, Azure, AWS |
| HIPAA / ITAR / air-gapped compliance | No | Yes |
| Pricing model | Per-document SaaS (unpublished rates) | Perpetual license from $749 |
| Per-page cost at scale | Yes, unbounded | None |

## Quick Start: Klippa OCR to IronOCR Migration

### Step 1: Replace NuGet Package

Klippa has no official NuGet package. Remove the HTTP client dependencies that exist solely to support the Klippa integration:

```bash
# Remove Klippa-related packages (if installed for REST support)
dotnet remove package Newtonsoft.Json
dotnet remove package System.Net.Http.Json
```

Install IronOCR from [NuGet](https://www.nuget.org/packages/IronOcr):

```bash
dotnet add package IronOcr
```

### Step 2: Update Namespaces

Remove the HTTP and JSON namespaces that Klippa integration required. Add the single IronOCR namespace:

```csharp
// Before (Klippa integration)
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

// After (IronOCR)
using IronOcr;
```

### Step 3: Initialize License

Add license initialization once at application startup — in `Program.cs`, `Startup.cs`, or before the first OCR call:

```csharp
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
```

## Code Migration Examples

### Replacing the HTTP Client Service Class

Klippa integration requires a full service class wrapping HTTP infrastructure. There is no way to avoid this because there is no SDK.

**Klippa Approach:**

```csharp
// Klippa: entire service class just to send one HTTP request
public class KlippaOcrService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "https://custom-ocr.klippa.com/api/v1";

    public KlippaOcrService(string apiKey)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("X-Auth-Key", apiKey);
        _httpClient.Timeout = TimeSpan.FromSeconds(30); // network timeout required
    }

    public async Task<string> ReadDocumentTextAsync(string filePath)
    {
        using var form = new MultipartFormDataContent();
        var fileBytes = await File.ReadAllBytesAsync(filePath);
        form.Add(new ByteArrayContent(fileBytes), "document", Path.GetFileName(filePath));

        var response = await _httpClient.PostAsync($"{_baseUrl}/parseDocument", form);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        // navigate Klippa's nested JSON schema
        return doc.RootElement
            .GetProperty("data")
            .GetProperty("parsed_document")
            .GetProperty("text")
            .GetString() ?? string.Empty;
    }

    public void Dispose() => _httpClient.Dispose();
}
```

**IronOCR Approach:**

```csharp
// IronOCR: no HTTP, no JSON navigation, no Dispose plumbing
public class OcrService
{
    private readonly IronTesseract _ocr = new IronTesseract();

    public string ReadDocumentText(string filePath)
    {
        return _ocr.Read(filePath).Text;
    }
}

// At startup:
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

// Usage — identical call site, different internals:
var service = new OcrService();
var text = service.ReadDocumentText("invoice.jpg"); // local, synchronous, zero network
```

The Klippa service class exists entirely because the API requires HTTP infrastructure. The IronOCR equivalent collapses to a single `Read()` call. Timeouts, authentication headers, and disposal patterns all disappear because there is no network. See the [IronTesseract setup guide](https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/) for initialization options and the [basic OCR example](https://ironsoftware.com/csharp/ocr/examples/simple-csharp-ocr-tesseract/) for working code.

### Eliminating Multipart Form Upload

Klippa receives documents as multipart form uploads. The upload code is mechanical but fragile: file reads, content type headers, boundary construction, and upload size management.

**Klippa Approach:**

```csharp
// Klippa: multipart upload — every document is an HTTP form POST
public async Task<KlippaResult> UploadAndParseAsync(
    string filePath, string documentType = "financial")
{
    using var form = new MultipartFormDataContent();

    // read file into memory — entire document in RAM before upload
    var fileBytes = await File.ReadAllBytesAsync(filePath);
    var byteContent = new ByteArrayContent(fileBytes);
    byteContent.Headers.ContentType =
        new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

    form.Add(byteContent, "document", Path.GetFileName(filePath));
    form.Add(new StringContent(documentType), "DocumentType");

    // document leaves your server here
    var response = await _httpClient.PostAsync(
        "https://custom-ocr.klippa.com/api/v1/parseDocument", form);

    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"Klippa API error: {response.StatusCode} — {error}");
    }

    var json = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<KlippaResult>(json,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
}
```

**IronOCR Approach:**

```csharp
// IronOCR: load from file path, byte array, or stream — no upload, no form
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

// From file path
using var input = new OcrInput();
input.LoadImage("invoice.jpg");
var result = new IronTesseract().Read(input);

// From byte array (same bytes Klippa was uploading)
byte[] fileBytes = await File.ReadAllBytesAsync("invoice.jpg");
using var inputFromBytes = new OcrInput();
inputFromBytes.LoadImage(fileBytes);
var resultFromBytes = new IronTesseract().Read(inputFromBytes);

Console.WriteLine(result.Text);
```

The `MultipartFormDataContent` construction, content-type headers, and the upload itself are all gone. IronOCR reads directly from the file path, from a byte array, or from a `Stream` — the same data that Klippa was transmitting to the cloud stays local. The [image input guide](https://ironsoftware.com/csharp/ocr/how-to/input-images/) covers all supported input formats, and the [stream input guide](https://ironsoftware.com/csharp/ocr/how-to/input-streams/) covers the memory-stream path for documents that arrive as byte arrays from upstream processes.

### Replacing JSON Response Deserialization

Klippa returns a nested JSON structure. Navigating that structure requires either a matching C# model or inline `JsonDocument` traversal — both of which break when Klippa changes their response schema.

**Klippa Approach:**

```csharp
// Klippa: deserialization model — breaks when API schema changes
public class KlippaResponse
{
    [JsonPropertyName("data")]
    public KlippaData Data { get; set; }
}

public class KlippaData
{
    [JsonPropertyName("parsed_document")]
    public KlippaParsedDocument ParsedDocument { get; set; }
}

public class KlippaParsedDocument
{
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    [JsonPropertyName("merchant")]
    public string Merchant { get; set; }

    [JsonPropertyName("date")]
    public string Date { get; set; }
}

// Usage: navigate the nullable chain every time
public async Task<string> GetExtractedTextAsync(string imagePath)
{
    var klippaResult = await UploadAndParseAsync(imagePath);
    // every property access is nullable — schema drift breaks this silently
    return klippaResult?.Data?.ParsedDocument?.Text ?? string.Empty;
}
```

**IronOCR Approach:**

```csharp
// IronOCR: typed result object — no JSON schema, no nullable chains
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

var ocr = new IronTesseract();
var result = ocr.Read("invoice.jpg");

// Direct property access — no deserialization, no nullable navigation
string fullText   = result.Text;
double confidence = result.Confidence;
int pageCount     = result.Pages.Count();

// Structured data: lines and words with coordinates
foreach (var page in result.Pages)
{
    foreach (var line in page.Lines)
    {
        Console.WriteLine($"Line: '{line.Text}' at Y={line.Y}");
    }
}
```

`OcrResult` is a typed .NET object. There is no JSON to parse, no model class to maintain, and no risk of schema drift breaking production deserialization. The [read results guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) documents the complete `OcrResult` object model including word coordinates, confidence scores, and structured page hierarchy. For invoice-specific field extraction patterns built on top of `OcrResult`, the [invoice OCR tutorial](https://ironsoftware.com/csharp/ocr/blog/using-ironocr/invoice-ocr-csharp-tutorial/) covers end-to-end extraction logic.

### Removing Error Handling and Retry Infrastructure

Klippa integration over HTTP requires comprehensive error handling for every failure mode a network call can produce: timeouts, 4xx responses, 5xx responses, rate limits, and partial JSON. Teams running production integrations add retry policies using Polly or custom logic. That infrastructure disappears when the network call disappears.

**Klippa Approach:**

```csharp
// Klippa: retry policy required — cloud calls fail unpredictably
public async Task<string> ReadWithRetryAsync(string filePath, int maxRetries = 3)
{
    var delay = TimeSpan.FromSeconds(1);

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            using var form = new MultipartFormDataContent();
            form.Add(
                new ByteArrayContent(await File.ReadAllBytesAsync(filePath)),
                "document",
                Path.GetFileName(filePath));

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var response = await _httpClient.PostAsync(
                "https://custom-ocr.klippa.com/api/v1/parseDocument", form, cts.Token);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                // rate limited — back off and retry
                await Task.Delay(delay * attempt);
                continue;
            }

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cts.Token);
            var parsed = JsonSerializer.Deserialize<KlippaResponse>(json);
            return parsed?.Data?.ParsedDocument?.Text ?? string.Empty;
        }
        catch (HttpRequestException) when (attempt < maxRetries)
        {
            await Task.Delay(delay * attempt); // exponential backoff
        }
        catch (TaskCanceledException) when (attempt < maxRetries)
        {
            await Task.Delay(delay * attempt); // timeout — retry
        }
    }

    throw new InvalidOperationException($"Klippa API failed after {maxRetries} attempts");
}
```

**IronOCR Approach:**

```csharp
// IronOCR: no network, no retry policy needed — local call either works or throws once
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

public string ReadDocument(string filePath)
{
    // No retry loop. No CancellationTokenSource. No HTTP status checks.
    // No rate limit handling. No partial-JSON guards.
    var result = new IronTesseract().Read(filePath);
    return result.Text;
}
```

The entire retry infrastructure — the loop, the delay calculation, the `CancellationTokenSource`, the HTTP status code branching, the `TaskCanceledException` catch block — exists solely because of the network. Remove the network call and all of it disappears. A local OCR call fails fast with a typed exception if the input file is missing or unreadable, and succeeds otherwise. The [speed optimization guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-fast-configuration/) covers IronOCR performance tuning if throughput is a concern after migration.

### Processing Multi-Page PDFs Without Cloud Upload

Klippa accepts PDF uploads through the same `parseDocument` endpoint. Multi-page PDFs still leave your network. IronOCR reads PDFs natively, in-process, with page-by-page result access.

**Klippa Approach:**

```csharp
// Klippa: PDF upload — entire document transmitted, results depend on cloud availability
public async Task<List<string>> ExtractPdfPagesAsync(string pdfPath)
{
    var pages = new List<string>();

    // Klippa parses the entire PDF server-side and returns combined results
    // You cannot control per-page processing or access raw page text
    using var form = new MultipartFormDataContent();
    form.Add(
        new ByteArrayContent(await File.ReadAllBytesAsync(pdfPath)),
        "document",
        Path.GetFileName(pdfPath));

    var response = await _httpClient.PostAsync(
        "https://custom-ocr.klippa.com/api/v1/parseDocument", form);
    response.EnsureSuccessStatusCode();

    var json = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<KlippaResponse>(json);
    // Klippa returns the combined parsed text — no per-page breakdown in basic API
    pages.Add(result?.Data?.ParsedDocument?.Text ?? string.Empty);

    return pages;
}
```

**IronOCR Approach:**

```csharp
// IronOCR: native PDF OCR with per-page structured access — no upload
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

using var input = new OcrInput();
input.LoadPdf("multi-page-invoice.pdf"); // reads locally — no HTTP

var ocr = new IronTesseract();
var result = ocr.Read(input);

// Per-page access — not available from Klippa's combined response
foreach (var page in result.Pages)
{
    Console.WriteLine($"Page {page.PageNumber}: {page.Lines.Count()} lines");
    Console.WriteLine(page.Text);
}

// Or produce a searchable PDF from the scanned original
result.SaveAsSearchablePdf("searchable-output.pdf");
```

IronOCR reads PDF files natively without any conversion step. Each page is individually accessible with its full line, word, and character hierarchy. The `SaveAsSearchablePdf()` call produces a text-layer PDF from a scanned document — a capability Klippa does not offer. The [PDF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-pdfs/) covers loading options, and the [searchable PDF guide](https://ironsoftware.com/csharp/ocr/how-to/searchable-pdf/) covers output options including PDF/A for archival compliance.

## Klippa OCR API to IronOCR Mapping Reference

Klippa is a REST API, not a typed SDK. The mapping below translates Klippa's integration surface to IronOCR equivalents.

| Klippa Concept | IronOCR Equivalent |
|---|---|
| `HttpClient` with `X-Auth-Key` header | `IronTesseract` instance — no authentication setup |
| `MultipartFormDataContent` | `OcrInput.LoadImage(path)` or `OcrInput.LoadPdf(path)` |
| `POST /api/v1/parseDocument` | `IronTesseract.Read(input)` |
| `await _client.PostAsync(...)` | `ocr.Read(input)` — synchronous, no `await` needed |
| `response.EnsureSuccessStatusCode()` | Not needed — no HTTP response |
| `JsonSerializer.Deserialize<KlippaResponse>(json)` | Typed `OcrResult` — no deserialization |
| `KlippaResponse.Data.ParsedDocument.Text` | `OcrResult.Text` |
| `KlippaResponse.Data.ParsedDocument.Amount` | Custom regex on `OcrResult.Text` or `OcrResult.Lines` |
| `KlippaResponse.Data.ParsedDocument.Merchant` | `OcrResult.Pages[0].Lines[0].Text` |
| Retry loop with `Task.Delay` | Not needed — no network failure mode |
| `CancellationTokenSource(TimeSpan.FromSeconds(30))` | Not needed — local execution |
| Rate limit handling (HTTP 429) | Not needed — no rate limits |
| Cloud document routing to EU servers | Local in-process execution |
| `KlippaService.Dispose()` / `HttpClient.Dispose()` | `OcrInput` disposal via `using` statement |
| Structured JSON response fields | `OcrResult.Text` + `OcrResult.Pages` + `OcrResult.Words` |
| SaaS API subscription | `IronOcr.License.LicenseKey` string — perpetual |

## Common Migration Issues and Solutions

### Issue 1: Async-Only Call Sites After HTTP Removal

**Klippa:** All Klippa integration is async because HTTP calls require it. Controllers, services, and background workers throughout your codebase call `await ProcessDocumentAsync(...)`. Removing the HTTP call means the `await` is no longer needed, but the `async` method signatures remain.

**Solution:** IronOCR provides both synchronous and async APIs. For call sites that must stay async (ASP.NET Core controllers, background services with `CancellationToken`), use `ReadAsync`:

```csharp
// Keep async method signatures — switch the implementation
public async Task<string> ProcessDocumentAsync(
    string filePath, CancellationToken cancellationToken = default)
{
    // Previously: await _httpClient.PostAsync(...)
    // Now: local call, same awaitable pattern
    var ocr = new IronTesseract();
    var result = await ocr.ReadAsync(filePath);
    return result.Text;
}
```

The [async OCR guide](https://ironsoftware.com/csharp/ocr/how-to/async/) covers `ReadAsync` and `CancellationToken` integration for ASP.NET Core and hosted service patterns.

### Issue 2: Dependency Injection Registration

**Klippa:** The `KlippaService` class is registered in DI as a singleton or scoped service and wraps `HttpClient`. Removing it means updating the DI registration and all injection points.

**Solution:** Register `IronTesseract` as a singleton (it is thread-safe) and inject it directly, or create a thin wrapper that mirrors your existing service interface:

```csharp
// In Program.cs or Startup.cs
builder.Services.AddSingleton<IronTesseract>();

// Or wrap for interface compatibility
builder.Services.AddSingleton<IOcrService, IronOcrService>();

public class IronOcrService : IOcrService
{
    private readonly IronTesseract _ocr;
    public IronOcrService(IronTesseract ocr) => _ocr = ocr;

    public string ReadDocument(string path) => _ocr.Read(path).Text;
}
```

One `IronTesseract` instance registered as a singleton handles concurrent requests. Each call to `Read()` is thread-safe.

### Issue 3: Structured Field Extraction Without Pre-Parsed JSON

**Klippa:** Klippa returns `amount`, `merchant`, `date`, and `vat_amount` as typed JSON properties. Migrating to IronOCR means those fields no longer arrive pre-parsed.

**Solution:** IronOCR's `OcrResult` provides the raw text and word-level coordinates to build equivalent extraction. For documents with predictable layouts, region-based OCR targets specific fields directly:

```csharp
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

// Target specific layout regions instead of relying on pre-parsed cloud fields
var totalRegion   = new CropRectangle(350, 580, 250, 50); // bottom-right total area
var merchantRegion = new CropRectangle(50, 30, 400, 60);  // top header area

using var merchantInput = new OcrInput();
merchantInput.LoadImage("receipt.jpg", merchantRegion);
var merchantName = new IronTesseract().Read(merchantInput).Text.Trim();

using var totalInput = new OcrInput();
totalInput.LoadImage("receipt.jpg", totalRegion);
var totalText = new IronTesseract().Read(totalInput).Text.Trim();
```

The [region-based OCR guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-region-of-an-image/) covers `CropRectangle` usage in detail. For full extraction patterns across receipt and invoice layouts, the [receipt scanning tutorial](https://ironsoftware.com/csharp/ocr/blog/using-ironocr/receipt-scanning-api-tutorial/) provides complete working code.

### Issue 4: Documents Arriving as Streams from Upstream Services

**Klippa:** Klippa receives documents as multipart form uploads — file bytes wrapped in HTTP form content. If your application receives documents as streams from S3, Azure Blob Storage, or internal APIs, you were reading the stream to bytes and then uploading those bytes to Klippa.

**Solution:** IronOCR accepts `Stream` objects directly. The byte conversion step disappears:

```csharp
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

// Stream from S3, Azure Blob, or any upstream source
public async Task<string> ProcessDocumentStreamAsync(Stream documentStream)
{
    using var input = new OcrInput();
    input.LoadImage(documentStream); // accepts Stream directly

    var ocr = new IronTesseract();
    var result = ocr.Read(input);
    return result.Text;
}
```

No `ReadAllBytes`, no `MultipartFormDataContent` construction, no HTTP POST. The stream goes directly into `OcrInput`. The [stream input guide](https://ironsoftware.com/csharp/ocr/how-to/input-streams/) covers stream types and disposal patterns.

### Issue 5: Integration Tests That Depend on HTTP Mocking

**Klippa:** Integration tests for Klippa code mock `HttpClient` or use HTTP interceptors (e.g., `WireMock`, `MockHttp`) to simulate API responses. Those tests mock the HTTP layer, not the OCR logic.

**Solution:** IronOCR tests use real documents with known expected output. No mocking infrastructure needed. Tests run offline:

```csharp
[Fact]
public void ReadDocument_ReturnsExpectedText()
{
    IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
    var ocr = new IronTesseract();

    // Use a real test fixture — no HTTP mocking, runs fully offline
    var result = ocr.Read("test-fixtures/sample-invoice.jpg");

    Assert.Contains("Invoice", result.Text, StringComparison.OrdinalIgnoreCase);
    Assert.True(result.Confidence > 70);
}
```

Tests that previously required a live Klippa connection or a complex HTTP mock setup now run in CI without network access.

### Issue 6: Low-Quality Documents That Klippa Enhanced Server-Side

**Klippa:** Cloud processing applies image enhancement before recognition. Developers never configure this — it happens automatically on Klippa's servers. When migrating, documents that Klippa handled silently may produce lower accuracy without explicit preprocessing in IronOCR.

**Solution:** Apply IronOCR's preprocessing filters explicitly. The filter set mirrors what cloud services apply server-side:

```csharp
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

using var input = new OcrInput();
input.LoadImage("low-quality-scan.jpg");
input.Deskew();    // fix rotation from camera or scanner
input.DeNoise();   // remove compression noise
input.Contrast();  // boost faded ink
input.Binarize();  // clean background for clearer character edges

var result = new IronTesseract().Read(input);
Console.WriteLine($"Confidence: {result.Confidence}%");
```

The [image quality correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/) covers all preprocessing filters and the order in which to apply them for different document degradation types.

## Klippa OCR Migration Checklist

### Pre-Migration Tasks

Audit your codebase to locate all Klippa-specific code before removing anything:

```bash
# Find all files containing Klippa HTTP integration code
grep -r "X-Auth-Key" --include="*.cs" .
grep -r "klippa.com" --include="*.cs" .
grep -r "KlippaService\|KlippaResponse\|KlippaResult\|KlippaData" --include="*.cs" .

# Find all files with MultipartFormDataContent (likely Klippa upload code)
grep -r "MultipartFormDataContent" --include="*.cs" .

# Find all JSON deserialization models that map to Klippa response fields
grep -r "parsed_document\|vat_amount\|merchant\|X-Auth-Key" --include="*.cs" .

# Find all async methods that wrap Klippa calls
grep -r "ParseDocumentAsync\|ProcessReceiptAsync\|UploadAndParseAsync" --include="*.cs" .

# Find test files with HTTP mocks for Klippa
grep -r "MockHttp\|WireMock\|klippa" --include="*.cs" .
```

Inventory notes:
- Record every class that wraps `HttpClient` for Klippa calls
- List all JSON deserialization model classes (`KlippaResponse`, `KlippaParsedDocument`, etc.)
- Document all field mappings that consume Klippa's pre-parsed JSON properties
- Note any Polly retry policies or custom retry loops built for Klippa

### Code Update Tasks

1. Install `IronOcr` NuGet package (`dotnet add package IronOcr`)
2. Add `IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY"` to application startup
3. Remove `System.Net.Http`, `System.Text.Json`, `Newtonsoft.Json` imports from Klippa service files
4. Delete `KlippaService` class (or replace its body with `IronTesseract` calls, keeping the interface)
5. Register `IronTesseract` as a singleton in the DI container
6. Replace `MultipartFormDataContent` upload blocks with `OcrInput.LoadImage()` or `OcrInput.LoadPdf()`
7. Delete JSON response model classes (`KlippaResponse`, `KlippaData`, `KlippaParsedDocument`)
8. Replace nullable JSON navigation chains (`.Data?.ParsedDocument?.Text`) with `result.Text`
9. Remove retry loops and `CancellationTokenSource` timeouts from Klippa call sites
10. Remove rate limit handling (HTTP 429 catch blocks)
11. Replace `await ProcessDocumentAsync(...)` with `await ocr.ReadAsync(...)` or synchronous `ocr.Read(...)`
12. Add `OcrInput` preprocessing filters (`Deskew`, `DeNoise`, `Contrast`) for low-quality document inputs
13. Replace HTTP mock test infrastructure with real document fixture tests
14. Delete Polly retry policies or custom retry middleware scoped to Klippa calls

### Post-Migration Testing

- Verify text extraction output matches expected content from known test documents
- Confirm confidence scores exceed acceptable threshold (typically 70%+) for production document types
- Test PDF input: load multi-page PDFs natively and verify per-page text access via `result.Pages`
- Test stream input: pass `MemoryStream` and verify `OcrInput.LoadImage(stream)` produces correct output
- Verify preprocessing filters improve accuracy on low-quality scans compared to unprocessed baseline
- Confirm DI-injected `IronTesseract` singleton handles concurrent requests without contention
- Run integration tests offline (no network connection) — all tests should pass without cloud access
- Verify searchable PDF output with `result.SaveAsSearchablePdf("output.pdf")` for scanned document flows
- Test `ReadAsync` in ASP.NET Core controller context with `CancellationToken` propagation
- Confirm `using var input = new OcrInput()` disposal pattern does not leak memory under sustained load

## Key Benefits of Migrating to IronOCR

**Data Sovereignty From Day One.** After migration, sensitive financial documents, identity scans, and confidential invoices never leave your infrastructure. There is no third-party processor in the audit scope, no data retention policy to review, and no data transfer agreement to maintain. HIPAA, ITAR, CMMC, and FedRAMP constraints that previously made Klippa problematic are satisfied by default. Deployment on [Docker](https://ironsoftware.com/csharp/ocr/get-started/docker/), [AWS](https://ironsoftware.com/csharp/ocr/get-started/aws/), or [Azure](https://ironsoftware.com/csharp/ocr/get-started/azure/) keeps everything inside your own infrastructure boundary.

**Infrastructure Complexity Eliminated.** The service class, the HTTP client, the form upload code, the JSON models, the retry policy, the timeout configuration — all of it existed to wrap a network call. Remove the network call and all of it goes with it. The resulting codebase is smaller, easier to read, and has fewer failure modes. A single `IronTesseract` instance injected through DI replaces the entire HTTP integration layer.

**Predictable Cost Regardless of Volume.** A perpetual IronOCR license at $749 (Lite), $1,499 (Professional), or $2,999 (Enterprise) covers unlimited document processing. Processing 500 documents per month or 500,000 per month costs the same. The per-document billing dynamic that made Klippa expensive at scale is structurally absent. The [IronOCR licensing page](https://ironsoftware.com/csharp/ocr/licensing/) details all tiers and what each includes.

**Document Scope Without Boundaries.** IronOCR processes any document that contains text. Scanned contracts, technical drawings, medical forms, purchase orders, handwritten notes, screenshots, TIFF archives — all handled by the same `Read()` call with the same API. The specialist scope restriction that required a second system for documents outside Klippa's trained categories is gone. One library, one integration point, any document type.

**Offline and Restricted Network Environments Now Supported.** Applications deployed in banking networks, government systems, edge environments, or any infrastructure with restricted outbound egress run exactly as they do in open environments. There is no connectivity check, no health ping to a cloud endpoint, and no degraded mode when the internet is unavailable. Air-gapped deployments work without modification. The [Linux deployment guide](https://ironsoftware.com/csharp/ocr/get-started/linux/) and [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/) cover the containerized and server-side deployment paths for these environments.

**Full Control Over Image Enhancement.** Cloud preprocessing was a black box — Klippa applied it, you observed the results, you had no parameters to tune. IronOCR's preprocessing pipeline is explicit and composable: `Deskew()`, `DeNoise()`, `Contrast()`, `Binarize()`, `Sharpen()`, `Scale()`, `Dilate()`, `DeepCleanBackgroundNoise()`. Each filter is optional and ordered. Accuracy improvements are measurable, reproducible, and under your control. The [image quality correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/) and [preprocessing features page](https://ironsoftware.com/csharp/ocr/features/preprocessing/) cover the full filter catalog with guidance on when to apply each.
