# Migrating from OCR.space to IronOCR

This guide walks .NET developers through replacing OCR.space's REST API integration with [IronOCR](https://ironsoftware.com/csharp/ocr/), a native .NET library delivered as a single NuGet package. It covers the package swap, namespace cleanup, and four concrete code migration scenarios that are specific to the REST-to-local transition: multipart upload elimination, base64 encoding removal, OCR engine selection replacement, and structured data extraction. Developers who have read the Phase 1 comparison article will find this guide focused on the mechanical steps of the migration itself rather than the feature comparison.

## Why Migrate from OCR.space

OCR.space fills a genuine niche: zero-cost experimentation for developers who want to test OCR in an afternoon without installing anything. The problem is that the free tier is designed for prototyping, not production. Once a .NET application moves toward real document volumes, compliance requirements, or team development, every characteristic of the OCR.space integration works against the application.

**No NuGet package means no SDK and no IntelliSense.** OCR.space provides a REST endpoint and documentation. The .NET integration — HTTP client construction, request serialization, response deserialization, error handling, and retry logic — is entirely the developer's responsibility. This is not a minor inconvenience. The minimum viable client is 80+ lines of infrastructure code before the first business-logic method is written. That code is undifferentiated across every OCR.space integration in every .NET codebase, and it accumulates bugs and maintenance burden over time.

**Rate limits impose artificial ceilings on production applications.** The free tier enforces 60 requests per minute and 500 requests per day per IP address. Both limits are hard walls. An application that exceeds 500 requests between midnight and the next midnight receives error responses until the counter resets. Production systems running in shared office networks or shared CI/CD environments can exhaust the daily quota before business hours end.

**Documents leave your infrastructure on every call.** OCR.space has no on-premise deployment option. Every request transmits the document — invoices, medical records, contracts, identity documents — to OCR.space's cloud servers. HIPAA, GDPR, and internal data classification policies that prohibit third-party transmission of sensitive documents make OCR.space architecturally incompatible, regardless of contractual controls.

**The free tier produces watermarked searchable PDFs.** Applications that generate searchable PDF output as a deliverable — document archival systems, compliance platforms, client-facing document portals — cannot use OCR.space's free tier for this purpose. The watermark is embedded in the output PDF and cannot be removed without a paid plan.

**Subscription pricing grows with volume; the OCR.space PRO tier at $144 per year crosses IronOCR's $749 perpetual entry price before year six.** Teams projecting document volume growth past the free tier threshold face compounding subscription costs against a fixed perpetual license. The $749 Lite license covers one developer and one deployment location with no per-request charges at any volume. See the [IronOCR licensing page](https://ironsoftware.com/csharp/ocr/licensing/) for tier details.

### The Fundamental Problem

OCR.space requires you to build a complete HTTP client before processing a single document:

```csharp
// OCR.space: 80+ lines of infrastructure before business logic
public class OcrSpaceApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly SemaphoreSlim _rateLimiter; // You implement this

    public OcrSpaceApiClient(string apiKey)
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(120);
        _rateLimiter = new SemaphoreSlim(60, 60); // Free tier: 60/min
    }
    // ... 70+ more lines of HTTP plumbing follow
}
```

IronOCR is a NuGet package. The entire client is already written:

```csharp
// IronOCR: no client to build, no rate limiter to manage
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
var result = new IronTesseract().Read("document.jpg");
Console.WriteLine(result.Text);
```

## IronOCR vs OCR.space: Feature Comparison

The table below maps OCR.space concepts and constraints directly to IronOCR equivalents.

| Feature | OCR.space | IronOCR |
|---|---|---|
| **NuGet package** | None — REST API only | `IronOcr` — native .NET |
| **SDK / IntelliSense** | None — manual JSON | Full — typed API |
| **Custom models required** | No | No |
| **Processing location** | OCR.space cloud servers | Local — in-process |
| **Internet dependency** | Required for every call | None |
| **Air-gapped deployment** | Not supported | Fully supported |
| **Rate limits** | 60/min, 500/day (free) | None |
| **File size limit** | 5 MB (free tier) | Available memory only |
| **PDF input** | Yes (limited, 5 MB) | Yes — native, no size limit |
| **Searchable PDF output** | Watermarked on free tier | Clean output, all tiers |
| **Automatic preprocessing** | Server-side, no developer control | Deskew, DeNoise, Contrast, Binarize, Sharpen |
| **Language support** | ~25 languages | 125+ via NuGet language packs |
| **Multi-language per document** | Not supported | Yes — `OcrLanguage.French + OcrLanguage.German` |
| **Structured output (words, lines)** | Plain text only | Pages, paragraphs, lines, words with coordinates |
| **Word-level confidence scores** | Not available | Yes — `word.Confidence` |
| **Region-based OCR** | Not supported | Yes — `CropRectangle` |
| **Barcode reading** | Not supported | Yes — `ReadBarCodes = true` |
| **Searchable PDF generation** | Watermarked (free), clean (paid) | Clean output — all license tiers |
| **HIPAA / GDPR compatibility** | Risk — data transmitted externally | Yes — no external data transmission |
| **Pricing model** | Monthly subscription | One-time perpetual |
| **Entry price** | $12/month ($144/year) | $749 one-time |
| **.NET compatibility** | `HttpClient` — any .NET | .NET 4.6.2+, .NET 5/6/7/8/9 |
| **Cross-platform deployment** | Requires outbound internet | Windows, Linux, macOS, Docker, Azure, AWS |

## Quick Start: OCR.space to IronOCR Migration

### Step 1: Replace NuGet Package

OCR.space has no NuGet package to uninstall. Remove all OCR.space-related infrastructure code from the project: the `HttpClient` wrapper class, the `SemaphoreSlim` rate limiter, the custom result models, and the custom exception types. All of these are replaced by IronOCR's NuGet package.

Install IronOCR from the [IronOCR NuGet page](https://www.nuget.org/packages/IronOcr):

```bash
dotnet add package IronOcr
```

### Step 2: Update Namespaces

Remove OCR.space HTTP and JSON namespaces. Add the IronOCR namespace:

```csharp
// Before (OCR.space — manually written infrastructure)
using System.Net.Http;
using System.Text.Json;
using System.Threading;

// After (IronOCR)
using IronOcr;
```

### Step 3: Initialize License

Add license initialization once at application startup — not per request:

```csharp
// Program.cs or application startup
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
```

## Code Migration Examples

### Replacing MultipartFormDataContent File Uploads

OCR.space requires building `MultipartFormDataContent` with the file bytes and API key, then POST-ing to the cloud endpoint. The document leaves your infrastructure on every call.

**OCR.space Approach:**

```csharp
// MultipartFormDataContent: file upload to cloud on every request
public async Task<string> UploadAndExtract(string imagePath)
{
    using var content = new MultipartFormDataContent();
    var imageBytes = File.ReadAllBytes(imagePath);

    // Document is transmitted to OCR.space servers here
    content.Add(new ByteArrayContent(imageBytes), "file", Path.GetFileName(imagePath));
    content.Add(new StringContent(_apiKey), "apikey");
    content.Add(new StringContent("eng"), "language");
    content.Add(new StringContent("2"), "OCREngine"); // Select Engine 2

    var response = await _httpClient.PostAsync("https://api.ocr.space/parse/image", content);
    response.EnsureSuccessStatusCode();

    string json = await response.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(json);

    // Navigate JSON tree manually — no typed result
    return doc.RootElement
        .GetProperty("ParsedResults")[0]
        .GetProperty("ParsedText")
        .GetString() ?? string.Empty;
}
```

**IronOCR Approach:**

```csharp
// OcrInput replaces the entire upload + JSON pipeline
public string ExtractFromFile(string imagePath)
{
    using var input = new OcrInput();
    input.LoadImage(imagePath); // Stays local — no network call

    var ocr = new IronTesseract();
    var result = ocr.Read(input);

    return result.Text; // Typed property — no JSON navigation
}
```

`OcrInput` is the local replacement for `MultipartFormDataContent`. It accepts file paths, byte arrays, streams, and multi-page TIFFs through a consistent API. The `HttpClient`, API key injection, and JSON navigation disappear entirely. The [image input how-to](https://ironsoftware.com/csharp/ocr/how-to/input-images/) covers every supported input format.

### Eliminating Base64 Encoding

When OCR.space integrations use the `base64Image` form parameter instead of the file upload parameter, the code reads the file to bytes, encodes to Base64, constructs a data URI string, and embeds it in `FormUrlEncodedContent`. IronOCR accepts raw bytes directly with no encoding step.

**OCR.space Approach:**

```csharp
// base64Image parameter: read → encode → embed in form → POST → parse
public async Task<string> ExtractViaBase64(string imagePath)
{
    byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
    string base64Image = Convert.ToBase64String(imageBytes); // Mandatory encoding step

    // Embed as data URI — adds 33% overhead to payload size
    string mimeType = "image/png";
    string dataUri = $"data:{mimeType};base64,{base64Image}";

    var formContent = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("apikey", _apiKey),
        new KeyValuePair<string, string>("base64Image", dataUri),
        new KeyValuePair<string, string>("language", "eng"),
        new KeyValuePair<string, string>("isOverlayRequired", "false")
    });

    var response = await _httpClient.PostAsync("https://api.ocr.space/parse/image", formContent);
    string json = await response.Content.ReadAsStringAsync();

    using var doc = JsonDocument.Parse(json);
    return doc.RootElement
        .GetProperty("ParsedResults")[0]
        .GetProperty("ParsedText")
        .GetString() ?? string.Empty;
}
```

**IronOCR Approach:**

```csharp
// LoadImage(bytes): raw bytes accepted directly — no encoding
public string ExtractFromBytes(byte[] imageBytes)
{
    using var input = new OcrInput();
    input.LoadImage(imageBytes); // No Base64, no data URI, no overhead

    var result = new IronTesseract().Read(input);
    return result.Text;
}
```

The Base64 encoding step does not exist in IronOCR because there is no HTTP transport layer. Raw bytes go directly into `OcrInput.LoadImage()`. The data URI overhead — Base64 encoding inflates payload size by approximately 33% — also disappears. The [stream input guide](https://ironsoftware.com/csharp/ocr/how-to/input-streams/) shows the same pattern for `Stream` inputs, which is useful when the bytes originate from an upload handler or memory buffer rather than a file.

### Replacing OCR Engine Selection with Image Preprocessing

OCR.space exposes two OCR engines via the `OCREngine` form parameter: Engine 1 is faster with lower accuracy on complex layouts; Engine 2 is slower with higher accuracy on most document types. Developers select the engine per call based on document characteristics. IronOCR runs a single optimized Tesseract 5 engine but exposes explicit preprocessing filters that address the root cause — document quality — rather than switching between engine modes.

**OCR.space Approach:**

```csharp
// OCREngine parameter: binary choice, no control over why accuracy differs
public async Task<string> ExtractWithEngineSelection(
    string imagePath,
    bool useHighAccuracyEngine = true)
{
    byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
    string base64 = Convert.ToBase64String(imageBytes);

    var formContent = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("apikey", _apiKey),
        new KeyValuePair<string, string>("base64Image", $"data:image/png;base64,{base64}"),
        new KeyValuePair<string, string>("language", "eng"),
        // Engine 1 = faster, Engine 2 = higher accuracy — binary choice only
        new KeyValuePair<string, string>("OCREngine", useHighAccuracyEngine ? "2" : "1"),
        new KeyValuePair<string, string>("scale", "true"),
        new KeyValuePair<string, string>("detectOrientation", "true")
    });

    var response = await _httpClient.PostAsync("https://api.ocr.space/parse/image", formContent);
    string json = await response.Content.ReadAsStringAsync();

    using var doc = JsonDocument.Parse(json);
    return doc.RootElement
        .GetProperty("ParsedResults")[0]
        .GetProperty("ParsedText")
        .GetString() ?? string.Empty;
}
```

**IronOCR Approach:**

```csharp
// Preprocessing pipeline: fix the document, not the engine selection
public string ExtractWithPreprocessing(string imagePath)
{
    using var input = new OcrInput();
    input.LoadImage(imagePath);

    // Apply filters that match the document's specific quality issues
    input.Deskew();         // Correct rotation — replaces detectOrientation
    input.DeNoise();        // Remove noise from fax/photocopier artifacts
    input.Contrast();       // Enhance contrast on low-quality scans
    input.Scale(200);       // Upscale small or low-DPI images

    var ocr = new IronTesseract();
    var result = ocr.Read(input);

    Console.WriteLine($"Confidence: {result.Confidence}%"); // No equivalent in OCR.space
    return result.Text;
}
```

The OCR.space `OCREngine` parameter is a proxy for document quality — when Engine 1 fails on a document, developers switch to Engine 2 hoping the different algorithm compensates. IronOCR's preprocessing pipeline addresses the quality problem directly: `Deskew()` corrects skewed scans, `DeNoise()` handles fax artifacts, and `Contrast()` recovers text from low-contrast photocopies. The result's `Confidence` property quantifies extraction quality, which `OCREngine` switching cannot provide. The [image quality correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/) and [filter wizard](https://ironsoftware.com/csharp/ocr/how-to/filter-wizard/) document each filter's effect on different document types.

### Multi-Language OCR Without Per-Call Language Switching

OCR.space accepts one `language` parameter per API call. Documents containing mixed languages require separate calls for each language, with results merged manually. IronOCR processes multiple languages simultaneously in a single read operation using the `+` operator on `OcrLanguage` values.

**OCR.space Approach:**

```csharp
// OCR.space: one language per call — multi-language requires multiple requests
public async Task<string> ExtractMultiLanguage(string imagePath)
{
    // First pass: English
    string englishText = await ExtractWithLanguage(imagePath, "eng");

    // Second pass: French (consumes another rate-limit slot, another API call)
    string frenchText = await ExtractWithLanguage(imagePath, "fre");

    // Manually merge results — no way to know which text belongs to which language
    return $"{englishText}\n{frenchText}";
}

private async Task<string> ExtractWithLanguage(string imagePath, string langCode)
{
    byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
    string base64 = Convert.ToBase64String(imageBytes);

    var content = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("apikey", _apiKey),
        new KeyValuePair<string, string>("base64Image", $"data:image/png;base64,{base64}"),
        new KeyValuePair<string, string>("language", langCode) // One language per call
    });

    var response = await _httpClient.PostAsync("https://api.ocr.space/parse/image", content);
    string json = await response.Content.ReadAsStringAsync();

    using var doc = JsonDocument.Parse(json);
    return doc.RootElement
        .GetProperty("ParsedResults")[0]
        .GetProperty("ParsedText")
        .GetString() ?? string.Empty;
}
```

**IronOCR Approach:**

```csharp
// IronOCR: multiple languages in a single read — one pass, correct output
public string ExtractMultiLanguage(string imagePath)
{
    var ocr = new IronTesseract();

    // Combine languages with + operator — processed simultaneously
    ocr.Language = OcrLanguage.English + OcrLanguage.French + OcrLanguage.German;

    var result = ocr.Read(imagePath);
    return result.Text; // Correctly interleaved multilingual output
}
```

OCR.space's single-language-per-call constraint forces developers to make N API calls for an N-language document and guess at how to reconcile the results. IronOCR combines language models into a single engine pass, which produces correctly interleaved output without post-processing. Language packs install as NuGet packages — `IronOcr.Languages.French`, `IronOcr.Languages.German`, and so on — and work offline. The [multiple languages how-to](https://ironsoftware.com/csharp/ocr/how-to/ocr-multiple-languages/) covers pack installation and the `+` operator syntax for all 125+ supported languages.

### Structured Data Extraction with Word Coordinates

OCR.space returns plain text from `ParsedResults[0].ParsedText`. There is no word-level data, no bounding boxes, no line boundaries, and no per-element confidence scores. Applications that need to locate specific fields — a date in the upper-right corner of an invoice, a total in the bottom-right cell of a table — have no structured foundation to build on from OCR.space's response. IronOCR provides a full document hierarchy: pages, paragraphs, lines, words, and characters, each with pixel coordinates and confidence scores.

**OCR.space Approach:**

```csharp
// OCR.space: plain text only — no structure, no coordinates
public async Task<string> ExtractInvoiceFields(string invoicePath)
{
    byte[] invoiceBytes = await File.ReadAllBytesAsync(invoicePath);
    string base64 = Convert.ToBase64String(invoiceBytes);

    var content = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("apikey", _apiKey),
        new KeyValuePair<string, string>("base64Image", $"data:application/pdf;base64,{base64}"),
        new KeyValuePair<string, string>("filetype", "PDF"),
        new KeyValuePair<string, string>("language", "eng"),
        // isOverlayRequired=true returns word boxes, but only as raw JSON coordinates
        new KeyValuePair<string, string>("isOverlayRequired", "true")
    });

    var response = await _httpClient.PostAsync("https://api.ocr.space/parse/image", content);
    string json = await response.Content.ReadAsStringAsync();

    // Navigate deeply-nested JSON to find word boxes — no typed models
    using var doc = JsonDocument.Parse(json);
    var overlay = doc.RootElement
        .GetProperty("ParsedResults")[0]
        .GetProperty("TextOverlay");

    // Parse word coordinate arrays manually — fragile JSON path traversal
    var wordData = new List<(string word, int x, int y)>();
    foreach (var line in overlay.GetProperty("Lines").EnumerateArray())
    {
        foreach (var word in line.GetProperty("Words").EnumerateArray())
        {
            string wordText = word.GetProperty("WordText").GetString() ?? "";
            int left = word.GetProperty("Left").GetInt32();
            int top = word.GetProperty("Top").GetInt32();
            wordData.Add((wordText, left, top));
        }
    }

    // Reconstruct full text from raw JSON — still no typed result
    return string.Join(" ", wordData.Select(w => w.word));
}
```

**IronOCR Approach:**

```csharp
// IronOCR: full document hierarchy — typed, no JSON, no coordinates parsing
public void ExtractInvoiceFields(string invoicePath)
{
    var ocr = new IronTesseract();
    var result = ocr.Read(invoicePath);

    // Access the full document hierarchy — all strongly typed
    foreach (var page in result.Pages)
    {
        foreach (var paragraph in page.Paragraphs)
        {
            Console.WriteLine($"Paragraph at ({paragraph.X}, {paragraph.Y}): {paragraph.Text}");
        }

        foreach (var word in page.Words)
        {
            // Word-level confidence — identify low-quality extractions
            if (word.Confidence < 70)
                Console.WriteLine($"Low confidence word '{word.Text}' at ({word.X}, {word.Y})");
        }
    }

    // Or use region-based OCR to target specific invoice zones directly
    var totalRegion = new CropRectangle(400, 700, 200, 50); // Bottom-right total field
    using var input = new OcrInput();
    input.LoadImage(invoicePath, totalRegion);
    string totalText = ocr.Read(input).Text;
    Console.WriteLine($"Invoice total: {totalText}");
}
```

The OCR.space `isOverlayRequired=true` flag returns JSON word coordinates, but the response structure requires navigating nested JSON arrays with string-keyed property access — no typed model, no IntelliSense, and fragile path traversal that breaks if the response structure changes. IronOCR's `result.Pages`, `result.Words`, and `result.Lines` are typed .NET objects. The `CropRectangle` approach targets specific document regions directly rather than extracting the full document and filtering by coordinates afterward. The [read results how-to](https://ironsoftware.com/csharp/ocr/how-to/read-results/) and [region-based OCR guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-region-of-an-image/) cover both patterns in detail.

## OCR.space API to IronOCR Mapping Reference

| OCR.space Concept | IronOCR Equivalent |
|---|---|
| No NuGet package | `dotnet add package IronOcr` |
| `HttpClient` construction | Not needed — no HTTP layer |
| `SemaphoreSlim` rate limiter | Not needed — no rate limits |
| `FormUrlEncodedContent` / `MultipartFormDataContent` | `OcrInput` |
| `base64Image` data URI parameter | `input.LoadImage(bytes)` |
| `file` upload parameter | `input.LoadImage(path)` |
| `apikey` header / form field | `IronOcr.License.LicenseKey` (once at startup) |
| `language` parameter (one per call) | `ocr.Language = OcrLanguage.English + OcrLanguage.French` |
| `OCREngine=1` (fast) | Default engine (optimized Tesseract 5) |
| `OCREngine=2` (high accuracy) | `input.Deskew(); input.DeNoise(); input.Contrast();` |
| `scale=true` parameter | `input.Scale(200)` |
| `detectOrientation=true` parameter | `input.Deskew()` |
| `isOverlayRequired=true` parameter | `result.Pages[n].Words` (always available, typed) |
| `isCreateSearchablePdf=true` parameter | `result.SaveAsSearchablePdf("output.pdf")` |
| `filetype=PDF` parameter | `input.LoadPdf(path)` |
| `ParsedResults[0].ParsedText` | `result.Text` |
| `ParsedResults[n]` (per-page text) | `result.Pages[n].Text` |
| `TextOverlay.Lines[n].Words[n].WordText` | `result.Pages[n].Words[n].Text` |
| `TextOverlay.Lines[n].Words[n].Left/Top` | `result.Pages[n].Words[n].X / .Y` |
| `IsErroredOnProcessing` JSON flag | Standard `Exception` with message |
| `FileParseExitCode` per-page flag | Standard `Exception` with message |
| HTTP 429 Too Many Requests | Not applicable — no rate limits |
| Custom `OcrResult` POCO (user-defined) | `IronOcr.OcrResult` (provided by NuGet) |
| Custom `OcrSpaceException` (user-defined) | Standard .NET exception types |

## Common Migration Issues and Solutions

### Issue 1: Async Code That Existed Only for HTTP

**OCR.space:** Every OCR call is `async` because it involves an HTTP round trip to the cloud. Service methods, controller actions, and background jobs were made async to avoid blocking the thread on the network wait.

**Solution:** IronOCR's `Read()` method is synchronous. Remove `async`/`await` from methods that were async purely because OCR.space required it. In ASP.NET Core contexts where non-blocking execution matters, wrap the synchronous call in `Task.Run()` or use the async patterns documented in the [async OCR guide](https://ironsoftware.com/csharp/ocr/how-to/async/). Do not reflexively add `await` to IronOCR calls — it is not required and adds unnecessary overhead in non-web contexts.

```csharp
// Before: async because OCR.space required network I/O
public async Task<string> ProcessDocumentAsync(string path)
{
    return await _ocrSpaceClient.ExtractTextAsync(path); // Network wait
}

// After: synchronous — no network, no async needed
public string ProcessDocument(string path)
{
    return _ocr.Read(path).Text; // Local execution
}
```

### Issue 2: API Key Storage and Rotation Infrastructure

**OCR.space:** The API key must be injected into every request. Teams typically store it in `appsettings.json` or environment variables, inject it through `IOptions<T>` or constructor injection, and rotate it when exposed. Key rotation requires updating every deployment environment and restarting the application.

**Solution:** The IronOCR license key is set once at startup and never referenced again during execution. Remove the per-request key injection pattern. Remove the `IOptions<OcrSpaceSettings>` configuration class. The key initialization pattern is one line:

```csharp
// Startup.cs or Program.cs — once only
IronOcr.License.LicenseKey = Environment.GetEnvironmentVariable("IRONOCR_LICENSE_KEY");
```

There is no per-request credential injection, no key rotation procedure, and no risk of accidentally logging the key in request traces.

### Issue 3: File Size Pre-Validation Logic

**OCR.space:** The free tier rejects files over 5 MB with an error response. Production code adds a file size check before every request to avoid wasting a rate-limit slot on a call that will fail:

```csharp
// OCR.space: pre-validation to avoid wasting quota on large files
var fileInfo = new FileInfo(filePath);
if (fileInfo.Length > 5 * 1024 * 1024)
    throw new InvalidOperationException("File exceeds 5MB free tier limit.");
```

**Solution:** Delete this check entirely. IronOCR's `OcrInput.LoadPdf()` and `OcrInput.LoadImage()` have no size limit beyond available system memory. The artificial 5 MB threshold exists only because OCR.space's free tier imposes it for server capacity reasons. A 50 MB scanned PDF loads the same way as a 500 KB one.

### Issue 4: JSON Response Navigation Fragility

**OCR.space:** Response parsing relies on navigating `JsonDocument` with string-keyed property access. Code like `doc.RootElement.GetProperty("ParsedResults")[0].GetProperty("ParsedText")` throws `KeyNotFoundException` if the response shape changes and `IndexOutOfRangeException` if `ParsedResults` is empty. Both require try-catch guards or null checks throughout.

**Solution:** IronOCR returns a typed `OcrResult` object. The `.Text` property is always a `string` — never null, never missing. If OCR produces no output (blank page, unreadable image), `result.Text` is an empty string. There is no JSON to navigate and no property-path fragility to guard against. For confidence-based filtering, `result.Confidence` returns a `double` that you compare directly:

```csharp
// IronOCR: typed result — no JSON path fragility
var result = new IronTesseract().Read("document.jpg");

if (result.Confidence < 50)
    Console.WriteLine("Low confidence — consider preprocessing");
else
    Console.WriteLine(result.Text);
```

The [confidence scores how-to](https://ironsoftware.com/csharp/ocr/how-to/tesseract-result-confidence/) covers per-word and per-document confidence thresholds.

### Issue 5: Shared-IP Rate Limit Exhaustion in CI/CD

**OCR.space:** CI/CD pipelines that run integration tests against OCR.space use the same outbound IP address as the development office network. Free tier accounts share a 500-requests-per-day limit per IP. A pipeline that processes 200 test documents per run can exhaust the daily quota before the first developer runs a manual test. Teams work around this by mocking OCR.space responses in tests, which defeats the purpose of integration testing.

**Solution:** IronOCR processes locally. The test suite calls `new IronTesseract().Read(testImagePath).Text` directly — no mocking required, no quota to exhaust, no network dependency. Integration tests run in CI/CD with the same real OCR results as production without any rate-limit management or test isolation patterns.

### Issue 6: `IDisposable` Pattern from HttpClient Management

**OCR.space:** The `HttpClient` wrapper class implements `IDisposable` to release the HTTP connection pool. Every consumer of the OCR service must either inject a singleton, use `using` blocks, or register it with the DI container's disposal lifecycle. Forgetting to dispose causes socket exhaustion under load.

**Solution:** `IronTesseract` does not manage network connections. It does not implement `IDisposable`. Create one instance per thread (or per request in ASP.NET), call `.Read()`, and let the GC collect it. The `OcrInput` class implements `IDisposable` and should be wrapped in `using` blocks when preprocessing is applied, but the primary `IronTesseract` class needs no lifecycle management. Remove the `IDisposable` implementation from your OCR service wrapper and simplify the DI registration from scoped/transient with disposal to a simple factory or singleton.

## OCR.space Migration Checklist

### Pre-Migration Tasks

Audit the codebase to identify all OCR.space integration points:

```bash
# Find all OCR.space HTTP calls
grep -rn "ocr.space" --include="*.cs" .

# Find all base64 encoding related to OCR
grep -rn "base64Image\|Convert.ToBase64String" --include="*.cs" .

# Find rate limiter and retry logic
grep -rn "SemaphoreSlim\|TooManyRequests\|exponential" --include="*.cs" .

# Find JSON parsing for OCR responses
grep -rn "ParsedResults\|IsErroredOnProcessing\|FileParseExitCode" --include="*.cs" .

# Find custom OCR models and exception types
grep -rn "OcrSpaceException\|OcrResult\b" --include="*.cs" .

# Find API key configuration
grep -rn "apikey\|OcrSpaceApiKey\|ocr_space" --include="*.cs" --include="*.json" .
```

Document the list of files containing OCR.space code. Note which methods are `async` solely because of OCR.space's HTTP dependency — these can be made synchronous after migration.

### Code Update Tasks

1. Install the `IronOcr` NuGet package: `dotnet add package IronOcr`
2. Add `IronOcr.License.LicenseKey = "..."` to application startup
3. Delete the `OcrSpaceApiClient` class and all supporting infrastructure
4. Delete the custom `OcrResult` POCO (replaced by `IronOcr.OcrResult`)
5. Delete the custom `OcrSpaceException` class (replaced by standard .NET exceptions)
6. Delete the `SemaphoreSlim` rate limiter and associated Task.Delay logic
7. Remove all `Convert.ToBase64String()` calls used for OCR image encoding
8. Replace `FormUrlEncodedContent` / `MultipartFormDataContent` construction with `OcrInput`
9. Replace `_httpClient.PostAsync(...)` calls with `new IronTesseract().Read(input)`
10. Replace `JsonDocument` parsing of `ParsedResults[0].ParsedText` with `result.Text`
11. Replace `TextOverlay` JSON coordinate parsing with `result.Pages[n].Words`
12. Replace `OCREngine` parameter switching with appropriate preprocessing filters
13. Replace `language` parameter strings with `OcrLanguage` enum values
14. Remove file size pre-validation checks (5 MB limit no longer applies)
15. Convert `async Task<string>` OCR methods to synchronous `string` where HTTP was the only async reason
16. Remove OCR.space API key from configuration files and environment variable setup

### Post-Migration Testing

- Verify text extraction produces equivalent or higher accuracy on the same test documents
- Confirm large files (over 5 MB) process without errors
- Test multi-language documents with `OcrLanguage.English + OcrLanguage.French` and verify interleaved output
- Run the CI/CD pipeline with real OCR calls — confirm no rate-limit errors at any document volume
- Validate searchable PDF output has no watermarks
- Check that previously async controller actions still respond correctly after synchronous conversion
- Test that air-gapped or network-restricted deployment environments process documents without errors
- Confirm `result.Confidence` values are acceptable on documents that previously required `OCREngine=2`
- Verify `result.Pages[n].Words` coordinates match expected field positions in structured documents
- Test that the application startup license initialization succeeds before the first OCR call

## Key Benefits of Migrating to IronOCR

**The 80+ line infrastructure tax disappears.** Every OCR.space integration ships an HTTP client, a rate limiter, a JSON deserializer, custom exception types, and custom result models. None of that code does anything the application actually needs — it exists to compensate for OCR.space's absent SDK. After migration, that code is deleted. The OCR surface area in the codebase shrinks to `new IronTesseract().Read(path).Text` at the call site and one license initialization line at startup.

**Document processing speed becomes a function of local hardware.** OCR.space introduces network latency, OCR.space server queue depth, and geographic round-trip time into every processing operation. IronOCR executes in-process. A local workstation processes documents faster than any cloud API at any throughput level, without the 60-requests-per-minute cap that serializes batch processing. Parallel processing with `Parallel.ForEach` across multiple `IronTesseract` instances scales with CPU cores — see the [multithreading example](https://ironsoftware.com/csharp/ocr/examples/csharp-tesseract-multithreading-for-speed/).

**Sensitive documents stay inside your infrastructure permanently.** After migration, medical records, financial documents, legal contracts, and identity documents never leave the application server. Compliance reviews for HIPAA, GDPR, SOC 2, and internal data classification policies no longer need to include OCR.space's data handling practices in their scope. The audit surface shrinks to your own infrastructure. The [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/) and [Azure deployment guide](https://ironsoftware.com/csharp/ocr/get-started/azure/) cover deploying IronOCR in containerized and cloud environments that require data residency compliance.

**Structured output enables document intelligence applications.** OCR.space's `ParsedText` string is the end of the road for document analysis. IronOCR's `result.Pages`, `result.Words`, and `result.Lines` with coordinates and per-word confidence scores enable applications to locate specific fields, validate extraction quality, extract table data, and build downstream document intelligence pipelines. Capabilities that required building custom layout analysis on top of OCR.space's plain-text output become direct API calls. The [table extraction guide](https://ironsoftware.com/csharp/ocr/how-to/read-table-in-document/) and [scanned document processing guide](https://ironsoftware.com/csharp/ocr/how-to/read-scanned-document/) demonstrate what this structured foundation enables.

**Cost becomes fixed and predictable at any volume.** OCR.space's free tier covers 25,000 requests per month. Above that, subscription costs scale with usage. IronOCR's $749 Lite perpetual license carries no per-document charge at any volume. A team processing 100,000 documents per month pays the same license fee as a team processing 1,000 documents per month. Budget forecasting for document-processing applications becomes a fixed annual cost rather than a variable line item that grows with business success. The [IronOCR product page](https://ironsoftware.com/csharp/ocr/) includes a free trial that lets teams validate accuracy on their specific document types before purchasing.
