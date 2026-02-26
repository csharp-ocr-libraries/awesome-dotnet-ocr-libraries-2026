# Migrating from Azure Computer Vision OCR to IronOCR

This guide walks .NET developers through replacing Azure Computer Vision OCR with [IronOCR](https://ironsoftware.com/csharp/ocr/), an on-premise OCR library that processes documents locally without cloud infrastructure. It covers the mechanical steps of swapping the NuGet package and namespaces, translating the Azure async polling model to synchronous local calls, and handling the specific patterns — dependency injection wiring, Form Recognizer polling loops, multi-page TIFF processing, and batch throughput — that require the most attention during migration.

## Why Migrate from Azure Computer Vision OCR

The case for migration is not abstract. Teams typically reach the decision after encountering one or more concrete operational problems with Azure Computer Vision in production.

**Endpoint and API key management never ends.** Every deployment environment — development, staging, production, disaster recovery — requires a provisioned Azure Cognitive Services resource, an endpoint URL, and at least one API key. Keys must be rotated. Endpoints change when resources move regions. Every environment needs outbound firewall rules to reach `cognitiveservices.azure.com`. The operational surface area grows with each environment and developer on the team. IronOCR replaces all of that with a single string license key set once at application startup, with no rotation schedule and no outbound network requirement.

**Per-page billing punishes multi-page documents.** Azure Computer Vision counts every PDF page as a separate transaction. A 20-page contract is 20 billable calls. At $1.00 per 1,000 transactions, a team processing 50,000 multi-page documents per month at an average of 4 pages each generates 200,000 transactions — $195 per month after the free tier, $2,340 per year. That is the break-even point against IronOCR Lite ($749) in fewer than four months, after which every additional page costs nothing.

**Async propagation spreads through the entire call stack.** Azure Computer Vision cannot return synchronously — cloud I/O has network latency. The `async`/`await` requirement on `AnalyzeAsync` forces every calling method to be async, propagating the pattern from the service layer up through controllers, background workers, and any synchronous code that must be refactored to accommodate it. Form Recognizer's polling-based operations compound this: `WaitUntil.Completed` blocks the thread, and true non-blocking behavior requires managing `UpdateStatusAsync` polling loops manually.

**Documents leave the network on every call.** For teams processing HIPAA-covered health information, ITAR-controlled defense documents, attorney-client privileged communications, or any document category subject to data residency rules, the mandatory cloud transmission is an architectural incompatibility, not a tradeoff. There is no Azure Computer Vision mode that avoids transmitting document content to Microsoft data centers.

**Rate limits create throughput ceilings.** The S1 tier of Azure Computer Vision caps at 10 transactions per second. A batch job processing 3,600 images per hour hits the ceiling exactly. Exceeding it returns HTTP 429 responses, requiring retry logic with exponential backoff in every calling path. IronOCR's throughput ceiling is the hosting hardware — no service-imposed cap, no retry infrastructure required.

**Image OCR and PDF OCR require two separate services.** Standard image OCR uses `ImageAnalysisClient` from `Azure.AI.Vision.ImageAnalysis`. Full PDF processing requires `DocumentAnalysisClient` from `Azure.AI.FormRecognizer.DocumentAnalysis` — a different NuGet package, a different Azure resource, a different endpoint, and a different result schema. Every application that processes both images and PDFs carries this doubled configuration overhead. IronOCR handles both with `IronTesseract.Read()` and a single `OcrInput` loader.

### The Fundamental Problem

```csharp
// Azure: endpoint URL + API key + async + nested block traversal — before a single character
var client = new ImageAnalysisClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
using var stream = File.OpenRead(imagePath);
var result = await client.AnalyzeAsync(BinaryData.FromStream(stream), VisualFeatures.Read);
var text = string.Join("\n", result.Value.Read.Blocks.SelectMany(b => b.Lines).Select(l => l.Text));

// IronOCR: no endpoint, no key rotation, no async, no traversal
var text = new IronTesseract().Read(imagePath).Text;
```

## IronOCR vs Azure Computer Vision OCR: Feature Comparison

The table below covers the capabilities most relevant to teams evaluating this migration.

| Feature | Azure Computer Vision OCR | IronOCR |
|---|---|---|
| Processing location | Microsoft Azure cloud | Local, on-premise |
| Internet required | Yes, every request | No |
| Azure subscription required | Yes | No |
| Pricing model | Per-transaction ($1.00 per 1,000) | Perpetual license (from $749) |
| Per-page billing on multi-page PDFs | Yes — each page = 1 transaction | No per-page cost |
| Free tier | 5,000 transactions/month | Trial mode (watermarked) |
| Image OCR API | `AnalyzeAsync` (async only) | `Read()` (synchronous) |
| PDF OCR | Separate Form Recognizer service | Built-in, same `Read()` call |
| Password-protected PDF | Via Form Recognizer | `input.LoadPdf(path, Password: "x")` |
| Searchable PDF output | Manual construction | `result.SaveAsSearchablePdf()` |
| Multi-page TIFF | Not supported | `input.LoadImageFrames()` |
| Automatic image preprocessing | Opaque server-side, not configurable | Deskew, DeNoise, Contrast, Binarize, Sharpen, Scale |
| Deep noise removal | No | `input.DeepCleanBackgroundNoise()` |
| Barcode reading during OCR | Separate Image Analysis feature | `ocr.Configuration.ReadBarCodes = true` |
| Region-based OCR | Not directly (manual crop before upload) | `CropRectangle` on `OcrInput` |
| Rate limits | 10 TPS on S1 tier | Hardware-bound only |
| Retry logic required | Yes (HTTP 429, 5xx) | No |
| Air-gapped deployment | Impossible | Fully supported |
| Languages supported | 164+ (server-managed) | 125+ (NuGet language packs) |
| Multi-language simultaneous | Yes | Yes (`OcrLanguage.French + OcrLanguage.German`) |
| Word bounding boxes | Polygon (variable vertex count) | Rectangle (x, y, width, height) |
| Confidence scoring | Per-word float (0.0–1.0) | Per-word and overall (0–100 scale) |
| hOCR export | No | `result.SaveAsHocrFile()` |
| Structured output hierarchy | Blocks / Lines / Words | Pages / Paragraphs / Lines / Words / Characters |
| .NET compatibility | .NET Standard 2.0+ | .NET Framework 4.6.2+, .NET Core, .NET 5–9 |
| Cross-platform | Windows, Linux, macOS (via cloud) | Windows, Linux, macOS, Docker, ARM64 |
| Commercial support | Azure support plans | IronOCR support included with license |

## Quick Start: Azure Computer Vision OCR to IronOCR Migration

### Step 1: Replace NuGet Package

Remove the Azure Computer Vision package:

```bash
dotnet remove package Azure.AI.Vision.ImageAnalysis
```

If the project also uses Form Recognizer for PDF processing, remove that package too:

```bash
dotnet remove package Azure.AI.FormRecognizer
```

Install IronOCR from [NuGet](https://www.nuget.org/packages/IronOcr):

```bash
dotnet add package IronOcr
```

### Step 2: Update Namespaces

```csharp
// Before (Azure Computer Vision)
using Azure;
using Azure.AI.Vision.ImageAnalysis;
// For PDF processing:
// using Azure.AI.FormRecognizer.DocumentAnalysis;

// After (IronOCR)
using IronOcr;
```

### Step 3: Initialize License

Add the license key once at application startup, before any OCR call:

```csharp
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
```

Store the key in an environment variable in production:

```csharp
IronOcr.License.LicenseKey = Environment.GetEnvironmentVariable("IRONOCR_LICENSE");
```

## Code Migration Examples

### Replacing Dependency-Injected Azure Client Configuration

Teams that follow the recommended Azure SDK pattern register `ImageAnalysisClient` in the DI container using `IOptions<AzureComputerVisionOptions>` or direct `IConfiguration` binding. This wiring pulls endpoint URLs and API keys from `appsettings.json` and requires outbound network configuration in every deployment environment.

**Azure Computer Vision Approach:**

```csharp
// appsettings.json binds to this class
public class AzureComputerVisionOptions
{
    public string Endpoint { get; set; }   // "https://your-resource.cognitiveservices.azure.com/"
    public string ApiKey   { get; set; }   // rotated periodically
}

// Program.cs / Startup.cs
services.Configure<AzureComputerVisionOptions>(
    configuration.GetSection("AzureComputerVision"));

services.AddSingleton<ImageAnalysisClient>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<AzureComputerVisionOptions>>().Value;
    return new ImageAnalysisClient(
        new Uri(opts.Endpoint),
        new AzureKeyCredential(opts.ApiKey));
});

services.AddScoped<IOcrService, AzureOcrService>();
```

```csharp
// AzureOcrService.cs
public class AzureOcrService : IOcrService
{
    private readonly ImageAnalysisClient _client;

    public AzureOcrService(ImageAnalysisClient client)
    {
        _client = client;
    }

    public async Task<string> ReadAsync(string imagePath)
    {
        using var stream = File.OpenRead(imagePath);
        var data = BinaryData.FromStream(stream);
        var result = await _client.AnalyzeAsync(data, VisualFeatures.Read);

        return string.Join("\n",
            result.Value.Read.Blocks
                .SelectMany(b => b.Lines)
                .Select(l => l.Text));
    }
}
```

**IronOCR Approach:**

```csharp
// Program.cs / Startup.cs
// One-time license key — no endpoint, no credential class, no options binding
IronOcr.License.LicenseKey = Environment.GetEnvironmentVariable("IRONOCR_LICENSE");

// Register IronTesseract as a singleton — it is thread-safe
services.AddSingleton<IronTesseract>();
services.AddScoped<IOcrService, IronOcrService>();
```

```csharp
// IronOcrService.cs
public class IronOcrService : IOcrService
{
    private readonly IronTesseract _ocr;

    public IronOcrService(IronTesseract ocr)
    {
        _ocr = ocr;
    }

    public string Read(string imagePath)
    {
        return _ocr.Read(imagePath).Text;
    }
}
```

The DI wiring drops from two configuration classes (options + client factory) to a single `AddSingleton<IronTesseract>()` call. The `appsettings.json` Azure section, key vault references, and outbound firewall rules for `cognitiveservices.azure.com` are all removed. See the [IronTesseract setup guide](https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/) for configuration options available on the singleton instance.

### Eliminating the Form Recognizer Polling Loop

Form Recognizer's `AnalyzeDocumentAsync` returns a `LongRunningOperation`. `WaitUntil.Completed` blocks the calling thread until the cloud job finishes — typically 2–10 seconds per document. For non-blocking behavior, teams write a `UpdateStatusAsync` polling loop with a delay between polls, adding 30–50 lines of infrastructure code that has no OCR logic in it.

**Azure Computer Vision Approach:**

```csharp
// DocumentAnalysisClient — separate from ImageAnalysisClient, separate resource
public class FormRecognizerPdfService
{
    private readonly DocumentAnalysisClient _client;

    public FormRecognizerPdfService(string endpoint, string apiKey)
    {
        _client = new DocumentAnalysisClient(
            new Uri(endpoint),
            new AzureKeyCredential(apiKey));
    }

    // Blocking wait — thread is held for the duration of cloud processing
    public async Task<string> ExtractPdfTextBlocking(string pdfPath)
    {
        using var stream = File.OpenRead(pdfPath);

        var operation = await _client.AnalyzeDocumentAsync(
            WaitUntil.Completed,  // blocks until Azure finishes
            "prebuilt-read",
            stream);

        var docResult = operation.Value;
        var sb = new StringBuilder();
        foreach (var page in docResult.Pages)
        {
            foreach (var line in page.Lines)
            {
                sb.AppendLine(line.Content);  // .Content, not .Text
            }
        }
        return sb.ToString();
    }

    // True async — manual polling loop required
    public async Task<string> ExtractPdfTextNonBlocking(string pdfPath)
    {
        using var stream = File.OpenRead(pdfPath);

        var operation = await _client.AnalyzeDocumentAsync(
            WaitUntil.Started,  // returns immediately, not complete yet
            "prebuilt-read",
            stream);

        // Poll every 500ms until the operation finishes
        while (!operation.HasCompleted)
        {
            await Task.Delay(500);
            await operation.UpdateStatusAsync();
        }

        var docResult = operation.Value;
        var sb = new StringBuilder();
        foreach (var page in docResult.Pages)
        {
            foreach (var line in page.Lines)
            {
                sb.AppendLine(line.Content);
            }
        }
        return sb.ToString();
    }
}
```

**IronOCR Approach:**

```csharp
// One class handles both images and PDFs — no second client or second resource
public class IronOcrDocumentService
{
    private readonly IronTesseract _ocr;

    public IronOcrDocumentService(IronTesseract ocr)
    {
        _ocr = ocr;
    }

    // Synchronous — returns immediately when local processing completes
    public string ExtractPdfText(string pdfPath)
    {
        using var input = new OcrInput();
        input.LoadPdf(pdfPath);
        return _ocr.Read(input).Text;
    }

    // Specific page range — no per-page billing penalty
    public string ExtractPageRange(string pdfPath, int startPage, int endPage)
    {
        using var input = new OcrInput();
        input.LoadPdfPages(pdfPath, startPage, endPage);
        return _ocr.Read(input).Text;
    }

    // If an async signature is required by an interface or controller
    public Task<string> ExtractPdfTextAsync(string pdfPath)
    {
        return Task.Run(() => ExtractPdfText(pdfPath));
    }
}
```

The polling loop and its delay logic disappear entirely. `LoadPdfPages` handles page range selection — no separate per-page call, no transaction counting. The [PDF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-pdfs/) covers stream input, byte array loading, and page range parameters in full detail.

### Mapping Azure Word-Level Results to IronOCR Structured Output

Azure Computer Vision returns word bounding boxes as polygons with a variable number of vertices — typically four, but not guaranteed. The result hierarchy is `Blocks → Lines → Words`, and confidence is a `float` on a 0.0–1.0 scale. Code that reads word positions must handle the polygon vertex array and normalize the confidence scale for any threshold comparisons.

**Azure Computer Vision Approach:**

```csharp
public async Task<List<WordResult>> ExtractWordPositionsAsync(string imagePath)
{
    using var stream = File.OpenRead(imagePath);
    var imageData = BinaryData.FromStream(stream);

    var response = await _client.AnalyzeAsync(imageData, VisualFeatures.Read);
    var words = new List<WordResult>();

    foreach (var block in response.Value.Read.Blocks)
    {
        foreach (var line in block.Lines)
        {
            foreach (var word in line.Words)
            {
                // BoundingPolygon is a list of ImagePoint — variable vertex count
                var polygon = word.BoundingPolygon;
                int minX = polygon.Min(p => p.X);
                int minY = polygon.Min(p => p.Y);
                int maxX = polygon.Max(p => p.X);
                int maxY = polygon.Max(p => p.Y);

                words.Add(new WordResult
                {
                    Text        = word.Text,
                    // Azure confidence: 0.0 to 1.0 — multiply by 100 for comparison
                    Confidence  = (double)word.Confidence * 100.0,
                    X           = minX,
                    Y           = minY,
                    Width       = maxX - minX,
                    Height      = maxY - minY
                });
            }
        }
    }
    return words;
}

public record WordResult(string Text, double Confidence, int X, int Y, int Width, int Height);
```

**IronOCR Approach:**

```csharp
public List<WordResult> ExtractWordPositions(string imagePath)
{
    var result = new IronTesseract().Read(imagePath);
    var words = new List<WordResult>();

    foreach (var page in result.Pages)
    {
        foreach (var line in page.Lines)
        {
            foreach (var word in line.Words)
            {
                // Rectangle-based bounding box — no polygon math required
                // Confidence is already 0–100, matching the converted Azure scale
                words.Add(new WordResult
                {
                    Text       = word.Text,
                    Confidence = word.Confidence,   // 0–100, no conversion needed
                    X          = word.X,
                    Y          = word.Y,
                    Width      = word.Width,
                    Height     = word.Height
                });
            }
        }
    }
    return words;
}

// Filter to only high-confidence words — common post-processing pattern
public IEnumerable<string> ExtractHighConfidenceWords(string imagePath, double threshold = 80.0)
{
    var result = new IronTesseract().Read(imagePath);
    return result.Words
        .Where(w => w.Confidence >= threshold)
        .Select(w => w.Text);
}

public record WordResult(string Text, double Confidence, int X, int Y, int Width, int Height);
```

The polygon-to-rectangle conversion disappears. Confidence values match directly once the Azure 0.0–1.0 values are multiplied by 100 — any existing threshold logic needs that one adjustment. The [structured data output guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) documents the complete hierarchy and coordinate properties. For the confidence scoring model specifically, see the [confidence scores guide](https://ironsoftware.com/csharp/ocr/how-to/tesseract-result-confidence/).

### Multi-Page TIFF Processing Without Cloud Upload

Azure Computer Vision's `ImageAnalysisClient` accepts single images. Multi-frame TIFF files — common in document scanning workflows, fax archives, and medical imaging pipelines — require either splitting the TIFF into individual images before upload (one transaction per frame) or switching to Form Recognizer with its separate configuration. Neither path is clean.

**Azure Computer Vision Approach:**

```csharp
// Azure does not support multi-frame TIFF directly via ImageAnalysisClient
// Must split frames manually and upload each as a separate transaction
public async Task<string> ExtractMultiFrameTiffAsync(string tiffPath)
{
    // Load TIFF using System.Drawing or a third-party library
    using var bitmap = new System.Drawing.Bitmap(tiffPath);
    int frameCount = bitmap.GetFrameCount(
        System.Drawing.Imaging.FrameDimension.Page);

    var allText = new StringBuilder();

    for (int i = 0; i < frameCount; i++)
    {
        // Select frame, save to temporary PNG, upload to Azure
        bitmap.SelectActiveFrame(System.Drawing.Imaging.FrameDimension.Page, i);

        using var ms = new MemoryStream();
        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        ms.Position = 0;

        // Each frame = 1 Azure transaction = $0.001
        var imageData = BinaryData.FromStream(ms);
        var result = await _client.AnalyzeAsync(imageData, VisualFeatures.Read);

        foreach (var block in result.Value.Read.Blocks)
            foreach (var line in block.Lines)
                allText.AppendLine(line.Text);
    }

    return allText.ToString();
}
```

**IronOCR Approach:**

```csharp
// IronOCR handles multi-frame TIFF natively — single call, no frame splitting
public string ExtractMultiFrameTiff(string tiffPath)
{
    using var input = new OcrInput();
    input.LoadImageFrames(tiffPath);  // all frames loaded automatically
    var result = new IronTesseract().Read(input);
    return result.Text;
}

// Access per-page data for frame-level reporting
public void ExtractTiffWithPageStats(string tiffPath)
{
    using var input = new OcrInput();
    input.LoadImageFrames(tiffPath);

    var ocr = new IronTesseract();
    var result = ocr.Read(input);

    Console.WriteLine($"Total frames processed: {result.Pages.Length}");
    foreach (var page in result.Pages)
    {
        Console.WriteLine($"Frame {page.PageNumber}: " +
            $"{page.Words.Length} words, " +
            $"confidence {page.Confidence:F1}%");
    }
}

// Combine with preprocessing for scanned TIFF archives
public string ExtractLowQualityTiffArchive(string tiffPath)
{
    using var input = new OcrInput();
    input.LoadImageFrames(tiffPath);
    input.Deskew();
    input.DeNoise();
    input.Contrast();

    var result = new IronTesseract().Read(input);
    return result.Text;
}
```

The per-frame transaction cost drops to zero. The `System.Drawing` frame extraction loop and its temporary PNG serialization step are eliminated entirely. For fax archive workflows where document quality varies, the [TIFF and GIF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-tiff-gif/) covers frame selection options, and the [image quality correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/) documents the full preprocessing filter set.

### Parallel Batch Processing Without Rate-Limit Queuing

Azure Computer Vision's S1 tier caps throughput at 10 transactions per second. Batch jobs that exceed this rate receive HTTP 429 responses. Production implementations require a rate-limiting wrapper, a semaphore, or a queuing layer to stay within the cap. The IronOCR API is thread-safe by design — create one `IronTesseract` instance per thread and run with `Parallel.ForEach`.

**Azure Computer Vision Approach:**

```csharp
// Must throttle to 10 TPS to avoid 429 errors on S1 tier
public class ThrottledAzureBatchProcessor
{
    private readonly ImageAnalysisClient _client;
    // Semaphore limits concurrent Azure calls to stay under 10 TPS
    private readonly SemaphoreSlim _throttle = new SemaphoreSlim(10, 10);

    public async Task<Dictionary<string, string>> ProcessBatchAsync(
        IEnumerable<string> imagePaths)
    {
        var results = new ConcurrentDictionary<string, string>();
        var tasks = imagePaths.Select(async path =>
        {
            await _throttle.WaitAsync();
            try
            {
                using var stream = File.OpenRead(path);
                var data = BinaryData.FromStream(stream);
                var response = await _client.AnalyzeAsync(data, VisualFeatures.Read);

                var text = string.Join("\n",
                    response.Value.Read.Blocks
                        .SelectMany(b => b.Lines)
                        .Select(l => l.Text));

                results[path] = text;

                // Respect 1-second window for the 10 TPS ceiling
                await Task.Delay(100);
            }
            catch (RequestFailedException ex) when (ex.Status == 429)
            {
                // Rate limited despite throttling — back off and retry
                await Task.Delay(2000);
                // Re-queue or log failure — simplified here
                results[path] = string.Empty;
            }
            finally
            {
                _throttle.Release();
            }
        });

        await Task.WhenAll(tasks);
        return new Dictionary<string, string>(results);
    }
}
```

**IronOCR Approach:**

```csharp
// No rate limiting needed — throughput is hardware-bound
public Dictionary<string, string> ProcessBatch(IEnumerable<string> imagePaths)
{
    var results = new ConcurrentDictionary<string, string>();

    Parallel.ForEach(imagePaths, imagePath =>
    {
        // Each thread gets its own IronTesseract instance — fully thread-safe
        var ocr = new IronTesseract();
        var result = ocr.Read(imagePath);
        results[imagePath] = result.Text;
    });

    return new Dictionary<string, string>(results);
}

// With controlled parallelism for memory-constrained environments
public Dictionary<string, string> ProcessBatchControlled(
    IEnumerable<string> imagePaths, int maxDegreeOfParallelism = 4)
{
    var results = new ConcurrentDictionary<string, string>();
    var options = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };

    Parallel.ForEach(imagePaths, options, imagePath =>
    {
        var ocr = new IronTesseract();
        var result = ocr.Read(imagePath);
        results[imagePath] = result.Text;
    });

    return new Dictionary<string, string>(results);
}
```

The semaphore, the 100ms delay, and the HTTP 429 catch block are all removed. Parallelism is limited only by CPU cores and available memory, not by a service tier. The [multithreading example](https://ironsoftware.com/csharp/ocr/examples/csharp-tesseract-multithreading-for-speed/) shows the full pattern with timing comparisons, and the [speed optimization guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-fast-configuration/) covers engine configuration tuning for batch workloads.

### Preprocessing Low-Quality Scans That Azure Rejects

Azure Computer Vision performs server-side image enhancement, but it is opaque and not configurable. Documents that are too skewed, too noisy, or too low-contrast return low-confidence results or empty text with no way to intervene. IronOCR exposes the preprocessing pipeline directly on `OcrInput`.

**Azure Computer Vision Approach:**

```csharp
// No client-side preprocessing API — must preprocess externally before upload
public async Task<string> ExtractFromLowQualityScanAsync(string imagePath)
{
    // Option 1: Accept whatever Azure returns (may be empty or low-quality)
    using var stream = File.OpenRead(imagePath);
    var imageData = BinaryData.FromStream(stream);

    var result = await _client.AnalyzeAsync(imageData, VisualFeatures.Read);

    // No way to know if the server applied enhancement
    // No confidence on the overall result — only per-word
    var text = string.Join("\n",
        result.Value.Read.Blocks
            .SelectMany(b => b.Lines)
            .Select(l => l.Text));

    if (string.IsNullOrWhiteSpace(text))
    {
        // Option 2: Apply external preprocessing using System.Drawing, SkiaSharp,
        // or ImageMagick, re-serialize to stream, re-upload — second billable transaction
        throw new Exception("Azure returned empty result; manual preprocessing needed");
    }

    return text;
}
```

**IronOCR Approach:**

```csharp
// Preprocessing is part of the same call — no re-upload, no second transaction
public string ExtractFromLowQualityScan(string imagePath)
{
    using var input = new OcrInput();
    input.LoadImage(imagePath);

    // Correct common scanning defects before OCR
    input.Deskew();           // Fix rotated documents
    input.DeNoise();          // Remove scanner noise
    input.Contrast();         // Improve contrast for faded documents
    input.Binarize();         // Convert to black and white

    var ocr = new IronTesseract();
    var result = ocr.Read(input);

    Console.WriteLine($"Confidence after preprocessing: {result.Confidence:F1}%");
    return result.Text;
}

// For severely degraded documents
public string ExtractFromDegradedDocument(string imagePath)
{
    using var input = new OcrInput();
    input.LoadImage(imagePath);
    input.DeepCleanBackgroundNoise();  // Deep learning-based noise removal
    input.Deskew();
    input.Scale(150);                  // Upscale for better character resolution

    var result = new IronTesseract().Read(input);
    return result.Text;
}
```

The external preprocessing dependency — `System.Drawing`, SkiaSharp, or ImageMagick — is removed. The re-upload and second transaction cost disappear. The preprocessing pipeline is part of the `OcrInput` lifecycle, so it is applied before the OCR engine sees the image. The [image filters tutorial](https://ironsoftware.com/csharp/ocr/tutorials/c-sharp-ocr-image-filters/) walks through each filter with before/after accuracy comparisons.

## Azure Computer Vision OCR API to IronOCR Mapping Reference

| Azure Computer Vision | IronOCR Equivalent |
|---|---|
| `ImageAnalysisClient` | `IronTesseract` |
| `new AzureKeyCredential(apiKey)` | `IronOcr.License.LicenseKey = key` |
| `new Uri(endpoint)` | Not required |
| `client.AnalyzeAsync(data, VisualFeatures.Read)` | `ocr.Read(imagePath)` |
| `BinaryData.FromStream(stream)` | `input.LoadImage(stream)` |
| `BinaryData.FromBytes(bytes)` | `input.LoadImage(bytes)` |
| `result.Value.Read.Blocks` | `result.Pages[i].Paragraphs` |
| `block.Lines` | `result.Pages[i].Lines` |
| `line.Text` | `line.Text` |
| `line.Words` | `line.Words` |
| `word.Text` | `word.Text` |
| `word.Confidence` (0.0–1.0 float) | `word.Confidence` (0–100 double) |
| `word.BoundingPolygon` | `word.X`, `word.Y`, `word.Width`, `word.Height` |
| `DocumentAnalysisClient` | `IronTesseract` + `OcrInput` |
| `AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-read", stream)` | `ocr.Read(input)` with `input.LoadPdf(path)` |
| `operation.Value.Pages` | `result.Pages` |
| `page.Lines[i].Content` | `result.Lines[i].Text` |
| `UpdateStatusAsync()` polling loop | Not required — synchronous result |
| `RequestFailedException` (status 429) | Not applicable — no rate limits |
| `RequestFailedException` (status 5xx) | Not applicable — no service errors |
| `VisualFeatures.Read` enum flag | Implicit — `Read()` always extracts text |
| Form Recognizer `prebuilt-read` model | Built-in OCR engine (no model selection) |
| Azure endpoint URL in `appsettings.json` | Not required |
| API key rotation procedures | Not required |

## Common Migration Issues and Solutions

### Issue 1: Async Interface Contracts That Cannot Change

**Azure Computer Vision:** Service interfaces often declare `Task<string>` return types because Azure mandates async. Calling code, controllers, and background workers are all written as async methods. Switching to IronOCR removes the need for async in the OCR layer, but changing every interface signature is not always feasible in a large codebase.

**Solution:** Wrap the synchronous IronOCR call in `Task.Run` to satisfy the existing interface without cascading refactors:

```csharp
// Existing interface — do not change it
public interface IOcrService
{
    Task<string> ReadAsync(string imagePath);
}

// New IronOCR implementation — fulfills the contract
public class IronOcrService : IOcrService
{
    private readonly IronTesseract _ocr;

    public IronOcrService(IronTesseract ocr) => _ocr = ocr;

    public Task<string> ReadAsync(string imagePath)
    {
        // Task.Run offloads to thread pool — no await chain needed
        return Task.Run(() => _ocr.Read(imagePath).Text);
    }
}
```

This is a valid intermediate step. The [async OCR guide](https://ironsoftware.com/csharp/ocr/how-to/async/) covers IronOCR's built-in async support for scenarios where full async integration is preferred.

### Issue 2: Confidence Threshold Logic Producing Wrong Results

**Azure Computer Vision:** Azure returns word confidence as a `float` between 0.0 and 1.0. Existing filtering code uses thresholds like `word.Confidence > 0.85f`. After migration, these comparisons always evaluate to false because IronOCR confidence is 0–100, not 0–1.

**Solution:** Multiply existing Azure thresholds by 100 when updating filtering logic:

```csharp
// Before: Azure threshold (0.0 - 1.0 scale)
var highConfidenceWords = azureWords
    .Where(w => w.Confidence > 0.85f)
    .Select(w => w.Text);

// After: IronOCR threshold (0 - 100 scale)
var result = new IronTesseract().Read(imagePath);
var highConfidenceWords = result.Words
    .Where(w => w.Confidence > 85.0)
    .Select(w => w.Text);

// Overall document confidence — also on 0-100 scale
if (result.Confidence < 70.0)
{
    // Document may need preprocessing or manual review
}
```

### Issue 3: Form Recognizer Prebuilt Model Field Extraction Has No Direct IronOCR Equivalent

**Azure Computer Vision:** Form Recognizer's prebuilt invoice and receipt models extract named fields automatically — `InvoiceTotal`, `VendorName`, `InvoiceDate` — without specifying where those fields appear on the page. The extraction logic is embedded in the Azure model.

**Solution:** Replace model-based field extraction with region-based OCR using `CropRectangle`. This requires knowing the document layout, but most real-world deployments already have fixed templates:

```csharp
var ocr = new IronTesseract();

// Define extraction zones for a known invoice template
var headerZone    = new CropRectangle(50,  40,  400, 60);
var totalZone     = new CropRectangle(350, 600, 250, 50);
var dateZone      = new CropRectangle(400, 100, 200, 40);

string header, total, date;

using (var input = new OcrInput())
{
    input.LoadImage("invoice.jpg", headerZone);
    header = ocr.Read(input).Text.Trim();
}

using (var input = new OcrInput())
{
    input.LoadImage("invoice.jpg", totalZone);
    total = ocr.Read(input).Text.Trim();
}

using (var input = new OcrInput())
{
    input.LoadImage("invoice.jpg", dateZone);
    date = ocr.Read(input).Text.Trim();
}
```

The [region-based OCR guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-region-of-an-image/) covers coordinate system details and multi-region batching.

### Issue 4: Missing hOCR and Structured Export

**Azure Computer Vision:** Azure does not provide hOCR output. Teams that need standardized layout data for downstream document analysis tools extract bounding boxes manually from the Azure response and construct their own output format.

**Solution:** IronOCR produces standards-compliant hOCR in one call:

```csharp
var result = new IronTesseract().Read("document.jpg");

// Write hOCR file — recognized by most document analysis tools
result.SaveAsHocrFile("document.hocr");

// Searchable PDF — alternative for archive and search indexing workflows
result.SaveAsSearchablePdf("document-searchable.pdf");
```

### Issue 5: Azure SDK Version Conflicts with Other Azure Packages

**Azure Computer Vision:** Projects that use multiple Azure SDK packages (`Azure.Storage.Blobs`, `Azure.Identity`, `Azure.KeyVault.Secrets`) can encounter version conflicts between `Azure.Core` transitive dependencies. The Azure SDK's monorepo versioning policy helps but does not eliminate all conflicts, particularly when mixing GA and preview SDK versions.

**Solution:** Removing `Azure.AI.Vision.ImageAnalysis` and `Azure.AI.FormRecognizer` eliminates those SDK packages from the dependency tree. If the project uses Azure only for OCR, the entire `Azure.*` dependency set is removed. If other Azure services remain, the reduced package count lowers the surface area for conflicts:

```bash
# Remove only the OCR-related Azure packages
dotnet remove package Azure.AI.Vision.ImageAnalysis
dotnet remove package Azure.AI.FormRecognizer

# Verify remaining Azure packages have no new conflicts
dotnet restore
dotnet build
```

### Issue 6: Scanned Document Quality Below Azure Acceptance Threshold

**Azure Computer Vision:** Very low-resolution images (below ~150 DPI) or severely skewed scans return minimal or empty text from Azure's server-side pipeline with no feedback about what enhancement was attempted. The caller has no way to improve the result without external preprocessing.

**Solution:** Use IronOCR's preprocessing pipeline to prepare the image before OCR:

```csharp
using var input = new OcrInput();
input.LoadImage("low-quality-scan.jpg");
input.Deskew();
input.DeNoise();
input.Scale(200);       // Upscale to improve character resolution
input.Contrast();
input.Sharpen();

var result = new IronTesseract().Read(input);
Console.WriteLine($"Extraction confidence: {result.Confidence:F1}%");
```

The [image orientation correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-orientation-correction/) covers deskew and rotation detection specifically.

## Azure Computer Vision OCR Migration Checklist

### Pre-Migration

Locate all Azure Computer Vision and Form Recognizer usage in the codebase:

```bash
# Find all Azure OCR-related using statements
grep -r "Azure.AI.Vision.ImageAnalysis" --include="*.cs" .
grep -r "Azure.AI.FormRecognizer" --include="*.cs" .
grep -r "ImageAnalysisClient" --include="*.cs" .
grep -r "DocumentAnalysisClient" --include="*.cs" .
grep -r "AnalyzeAsync" --include="*.cs" .
grep -r "AnalyzeDocumentAsync" --include="*.cs" .
grep -r "VisualFeatures.Read" --include="*.cs" .
grep -r "WaitUntil.Completed" --include="*.cs" .
grep -r "UpdateStatusAsync" --include="*.cs" .
grep -r "AzureKeyCredential" --include="*.cs" .
```

Identify configuration files containing Azure OCR endpoints and keys:

```bash
grep -r "cognitiveservices.azure.com" --include="*.json" .
grep -r "AzureComputerVision\|FormRecognizer" --include="*.json" .
grep -r "ComputerVision\|FormRecognizer" --include="appsettings*.json" .
```

Inventory items before coding begins:

- Count of classes implementing Azure OCR service patterns
- Count of async method chains that propagate from OCR calls
- Identify any word confidence thresholds using the 0.0–1.0 Azure scale
- Identify Form Recognizer prebuilt model usage (invoice, receipt, identity) requiring region-based replacement
- Identify multi-frame TIFF inputs currently split for per-frame upload
- Check Docker and CI/CD configurations for outbound Azure network rules that will no longer be needed

### Code Migration

1. Remove `Azure.AI.Vision.ImageAnalysis` NuGet package from all projects that use it
2. Remove `Azure.AI.FormRecognizer` NuGet package from all projects that use it
3. Run `dotnet add package IronOcr` in each affected project
4. Add `IronOcr.License.LicenseKey = Environment.GetEnvironmentVariable("IRONOCR_LICENSE")` at application startup
5. Replace `using Azure; using Azure.AI.Vision.ImageAnalysis;` with `using IronOcr;`
6. Register `IronTesseract` as a singleton in DI (`services.AddSingleton<IronTesseract>()`)
7. Remove `AzureComputerVisionOptions` or equivalent configuration classes; remove the `appsettings.json` Azure OCR sections
8. Replace `ImageAnalysisClient` constructor injection with `IronTesseract` injection in all service classes
9. Replace `AnalyzeAsync(BinaryData.FromStream(stream), VisualFeatures.Read)` calls with `ocr.Read(imagePath)` or `ocr.Read(input)` with `OcrInput` as appropriate
10. Remove all `UpdateStatusAsync` polling loops and `WaitUntil.Completed` patterns; replace `DocumentAnalysisClient` with `IronTesseract` + `OcrInput.LoadPdf()`
11. Update word confidence threshold comparisons: multiply all Azure 0.0–1.0 values by 100
12. Replace polygon bounding box access (`word.BoundingPolygon.Min/Max`) with direct `word.X`, `word.Y`, `word.Width`, `word.Height` properties
13. Replace multi-frame TIFF per-frame upload loops with `input.LoadImageFrames(tiffPath)`
14. Convert Form Recognizer prebuilt model field extraction to `CropRectangle`-based region OCR for known document templates
15. Remove `RequestFailedException` catch blocks for HTTP 429 and 5xx; simplify error handling to file system and input validation exceptions only

### Post-Migration

- Verify plain text extraction output matches or improves on Azure results for a representative sample of 20+ documents
- Confirm multi-page PDF processing produces text for all pages without per-page billing counters in monitoring
- Test multi-frame TIFF processing returns the same page count as the source frame count
- Validate word confidence values fall in the 0–100 range and that threshold comparisons behave correctly
- Verify word bounding box coordinates align correctly with the source image coordinate system
- Run the batch processing path and confirm it completes without rate-limit errors or semaphore contention
- Test in an environment with no outbound internet access to confirm no Azure endpoint calls occur
- Confirm Docker and container deployments start successfully without outbound firewall rules for `cognitiveservices.azure.com`
- Check that DI container construction succeeds and `IronTesseract` singleton is correctly resolved
- Verify the `IRONOCR_LICENSE` environment variable is set in all deployment environments and that OCR produces licensed (non-watermarked) output

## Key Benefits of Migrating to IronOCR

**Cost becomes predictable.** Azure Computer Vision's per-transaction meter runs continuously. A single month of high document volume can exceed the annual cost of an IronOCR license. After migration, the OCR budget is a fixed line item. Processing volume can grow — due to business expansion, bulk reprocessing, or temporary spikes — without generating a larger invoice. The [IronOCR licensing page](https://ironsoftware.com/csharp/ocr/licensing/) shows tier options from $749 for a single-developer license to $2,999 for unlimited developers.

**The application stack becomes simpler.** Removing `Azure.AI.Vision.ImageAnalysis` and `Azure.AI.FormRecognizer` eliminates two NuGet packages, two Azure resource configurations, two sets of credentials, and the entire async polling infrastructure. Service classes that previously required an `async Task<string>` signature because of cloud I/O become synchronous methods. Error handling narrows from network failures, rate limits, and service availability to file system and input validation. Every future developer working in this codebase encounters less code to understand.

**Document data stays within the organization's control.** After migration, OCR processing is a local operation. Documents do not cross an organizational boundary, do not appear in Azure telemetry, and are not subject to Microsoft's data retention or processing policies. HIPAA-covered entities, ITAR contractors, GDPR-regulated organizations, and any team with data residency requirements can process documents without compliance review of a cloud third party. The [Linux deployment guide](https://ironsoftware.com/csharp/ocr/get-started/linux/) and [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/) show how to deploy IronOCR in restricted environments.

**Batch throughput scales to hardware.** The 10 TPS ceiling from Azure's S1 tier is a hard limit that requires queuing, throttling, or tier upgrades to work around. After migration, concurrent OCR jobs saturate available CPU cores without any service-imposed cap. A four-core server can run four parallel `IronTesseract` instances simultaneously. The [multithreading example](https://ironsoftware.com/csharp/ocr/examples/csharp-tesseract-multithreading-for-speed/) demonstrates the pattern and shows throughput scaling with core count.

**Preprocessing defects are solvable in code.** Azure Computer Vision's server-side image enhancement is a black box. When a scan returns empty or low-confidence output, the only options are to accept it or preprocess externally before re-uploading at additional cost. IronOCR's `OcrInput` pipeline exposes deskew, denoise, contrast, binarization, scale, sharpen, and deep noise removal as first-class methods. Problematic scan types become tunable parameters. The [preprocessing features page](https://ironsoftware.com/csharp/ocr/features/preprocessing/) lists the complete filter set with guidance on which filters address which scan defects.
