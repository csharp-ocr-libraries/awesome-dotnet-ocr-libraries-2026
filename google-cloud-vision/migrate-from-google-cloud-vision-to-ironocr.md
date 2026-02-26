# Migrating from Google Cloud Vision OCR to IronOCR

This guide walks .NET developers through replacing Google Cloud Vision with [IronOCR](https://ironsoftware.com/csharp/ocr/) as a drop-in on-premise OCR engine. It covers credential removal, Protobuf annotation parsing replacement, batch annotation simplification, and multi-page document processing — the four structural changes that account for the majority of migration work.

## Why Migrate from Google Cloud Vision OCR

The decision to migrate almost always starts with one of two realizations: a compliance audit blocks cloud document transmission, or the operational surface area of managing GCP credentials, GCS buckets, async polling, and per-image billing becomes more expensive than the OCR itself.

**Service Account JSON Key Lifecycle.** Every deployment of your application — development machine, CI/CD pipeline, staging server, production server, Docker container, Kubernetes pod — requires the same service account JSON key file. The file contains an RSA private key. It must never enter source control, must be rotated on a schedule, must be protected with file-system permissions, and must be updated across all environments simultaneously when rotation occurs. One compromised key grants API access until it is manually revoked in the GCP Console. IronOCR replaces this entire operational surface with a single license key string set once at application startup.

**Per-Request Billing at Scale.** At $1.50 per 1,000 images and $0.0015 per PDF page, costs are invisible during development and painful in production. A document processing pipeline handling 200,000 pages per month costs $300 per month in API fees alone, before GCS storage charges and egress costs. That $300 repeats every month indefinitely. IronOCR's perpetual license converts OCR from a metered operating expense into a fixed capital line item that costs nothing to run in year two or year three.

**GCS Async Pipeline for PDFs.** Google Cloud Vision does not accept PDFs as direct API input. The full pipeline requires a second NuGet package (`Google.Cloud.Storage.V1`), a provisioned GCS bucket, an async upload, an `AsyncBatchAnnotateFilesAsync` call, a polling loop, JSON output parsing from GCS, and a cleanup step. That pipeline spans 50-plus lines of code before any text has been extracted. IronOCR reads a PDF in three lines, synchronously, with no external dependencies.

**Protobuf Symbol Concatenation.** The `DOCUMENT_TEXT_DETECTION` response stores text at the symbol level inside a Protobuf hierarchy of Pages, Blocks, Paragraphs, Words, and Symbols. Reading paragraph text requires iterating five nested loops and calling `.SelectMany(w => w.Symbols).Select(s => s.Text)`. IronOCR returns paragraph text as `paragraph.Text` — a typed string property.

**1,800 Requests per Minute Default Quota.** Batch workloads above the default quota receive `StatusCode.ResourceExhausted` responses that stall the pipeline for 60 seconds per exceedance. Increasing the quota requires a GCP Console request and Google's approval. IronOCR processes locally at the speed of available CPU cores — there is no quota to manage, no approval to seek, and no retry logic required for rate limiting.

**No Offline or Air-Gapped Support.** Google Cloud Vision requires internet connectivity to Google's endpoints. Air-gapped networks, classified data centers, and industrial control systems cannot use it at any level of architectural complexity. IronOCR runs with zero outbound network connectivity after the initial license validation.

### The Fundamental Problem

Google Cloud Vision requires a JSON key file on disk before the first line of OCR code runs:

```csharp
// Google Cloud Vision: JSON key file deployed to every server before this line works
// GOOGLE_APPLICATION_CREDENTIALS="/etc/secrets/service-account.json" must be set
// Key contains RSA private key — rotate manually, revoke if compromised
_client = ImageAnnotatorClient.Create();
var image = Image.FromFile("document.jpg");
var response = _client.DetectText(image);   // document leaves your infrastructure
string text = response[0].Description;
```

IronOCR starts with one string and runs entirely on the local machine:

```csharp
// IronOCR: one string at startup, no key files, no environment variables
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
string text = new IronTesseract().Read("document.jpg").Text;  // local, no cloud
```

## IronOCR vs Google Cloud Vision OCR: Feature Comparison

The table below maps features directly for teams building the business case for migration.

| Feature | Google Cloud Vision OCR | IronOCR |
|---|---|---|
| Processing location | Google Cloud (remote) | On-premise (local) |
| Authentication | Service account JSON key + env variable | License key string |
| PDF input | Upload to GCS + async API | `input.LoadPdf()` direct |
| Password-protected PDF | Not supported | `LoadPdf(path, Password: "...")` |
| Multi-page TIFF input | Limited | `input.LoadImageFrames()` |
| Searchable PDF output | Not available | `result.SaveAsSearchablePdf()` |
| Structured data access | Protobuf: Pages > Blocks > Paragraphs > Words > Symbols | `result.Paragraphs`, `result.Lines`, `result.Words` (typed .NET objects) |
| Paragraph text property | No — requires symbol concatenation | `paragraph.Text` direct property |
| Confidence scores | Per symbol (requires loop) | `result.Confidence`, `word.Confidence` |
| Automatic image preprocessing | None (ML handles it) | Deskew, DeNoise, Contrast, Binarize, Sharpen |
| Region-based OCR | No native crop | `CropRectangle` on `OcrInput` |
| Barcode reading | Separate API feature | `ocr.Configuration.ReadBarCodes = true` |
| Rate limits | 1,800 requests/minute default | None (CPU-bound) |
| Offline / air-gapped | No | Yes |
| Per-document cost | $1.50 per 1,000 images; $0.0015/PDF page | None (perpetual license) |
| Languages supported | ~50 | 125+ |
| FedRAMP authorization | Not authorized | Not applicable (on-premise) |
| HIPAA compliance path | Business Associate Agreement required | No third-party data handling |
| .NET Framework support | .NET Standard 2.0+ | .NET Framework 4.6.2+ and .NET 5/6/7/8/9 |
| NuGet packages required | `Google.Cloud.Vision.V1` + `Google.Cloud.Storage.V1` | `IronOcr` only |
| Pricing model | Per-request metered billing | Perpetual ($749 Lite / $1,499 Pro / $2,999 Enterprise) |

## Quick Start: Google Cloud Vision OCR to IronOCR Migration

### Step 1: Replace NuGet Package

Remove the Google Cloud packages:

```bash
dotnet remove package Google.Cloud.Vision.V1
dotnet remove package Google.Cloud.Storage.V1
```

Install IronOCR from the [NuGet package page](https://www.nuget.org/packages/IronOcr):

```bash
dotnet add package IronOcr
```

### Step 2: Update Namespaces

Replace the Google Cloud namespaces with the IronOcr namespace:

```csharp
// Before (Google Cloud Vision)
using Google.Cloud.Vision.V1;
using Google.Cloud.Storage.V1;
using Google.Protobuf;
using Grpc.Core;

// After (IronOCR)
using IronOcr;
```

### Step 3: Initialize License

Add license initialization once at application startup, before any `IronTesseract` instance is created:

```csharp
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
```

In production, read the key from an environment variable or secrets manager:

```csharp
IronOcr.License.LicenseKey = Environment.GetEnvironmentVariable("IRONOCR_LICENSE")
    ?? throw new InvalidOperationException("IRONOCR_LICENSE environment variable not set.");
```

## Code Migration Examples

### Eliminating Service Account Credential Configuration

The Google Cloud Vision client initialization looks like a single line but requires extensive prerequisite infrastructure. Every developer who joins the project, every deployment environment, and every CI/CD pipeline needs the full credential configuration in place before the constructor completes without throwing.

**Google Cloud Vision Approach:**

```csharp
using Google.Cloud.Vision.V1;

// Prerequisites before this class can be instantiated:
// 1. GCP project created and Vision API enabled in GCP Console
// 2. Service account created with roles/cloudvision.user IAM role
// 3. JSON key file downloaded to every server that runs this code
// 4. GOOGLE_APPLICATION_CREDENTIALS env var pointing to the JSON file
// 5. JSON file excluded from source control via .gitignore
// 6. Key rotation schedule established (recommended: 90 days)
// 7. Separate credentials per environment (dev/staging/prod)

public class DocumentOcrService
{
    private readonly ImageAnnotatorClient _client;
    private readonly string _projectId;

    public DocumentOcrService(string projectId)
    {
        _projectId = projectId;
        // Throws RpcException(StatusCode.PermissionDenied) if any prerequisite is missing
        _client = ImageAnnotatorClient.Create();
    }

    public string ReadDocument(string imagePath)
    {
        var image = Image.FromFile(imagePath);
        var response = _client.DetectText(image);
        return response.Count > 0 ? response[0].Description : string.Empty;
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

// Prerequisites: set the license key once at app startup
// No JSON files, no environment variables beyond the key, no GCP Console configuration
// No key rotation, no IAM roles, no per-environment credential sets

public class DocumentOcrService
{
    private readonly IronTesseract _ocr;

    public DocumentOcrService()
    {
        // IronTesseract is ready immediately — no external validation required
        _ocr = new IronTesseract();
    }

    public string ReadDocument(string imagePath)
    {
        return _ocr.Read(imagePath).Text;
    }
}
```

The operational difference is concrete: Google Cloud Vision generates five categories of `RpcException` at runtime — `PermissionDenied`, `ResourceExhausted`, `Unavailable`, `DeadlineExceeded`, and `Unauthenticated` — each representing a different infrastructure failure mode. IronOCR's failure modes are `IOException` (file not found or locked) and `OcrException` (processing failure). See the [IronTesseract setup guide](https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/) for configuration options and the [IronOCR product page](https://ironsoftware.com/csharp/ocr/licensing/) for licensing details.

### Replacing Batch Annotation Requests with Multiple Feature Types

Google Cloud Vision supports batching multiple images into a single `BatchAnnotateImagesRequest`, each image configured with a list of `Feature` types. This pattern is used when a single call needs to collect both `TEXT_DETECTION` and `DOCUMENT_TEXT_DETECTION` results, or when submitting many images to minimize round-trip overhead. The Protobuf response requires matching each `AnnotateImageResponse` back to its original request by index.

**Google Cloud Vision Approach:**

```csharp
using Google.Cloud.Vision.V1;
using System.Collections.Generic;

public class BatchAnnotationService
{
    private readonly ImageAnnotatorClient _client;

    public BatchAnnotationService()
    {
        _client = ImageAnnotatorClient.Create();
    }

    public List<string> BatchAnnotateImages(string[] imagePaths)
    {
        // Build one request per image with TEXT_DETECTION feature
        var requests = imagePaths.Select(path => new AnnotateImageRequest
        {
            Image = Image.FromFile(path),
            Features =
            {
                new Feature { Type = Feature.Types.Type.TextDetection },
                new Feature { Type = Feature.Types.Type.DocumentTextDetection }
            }
        }).ToList();

        // Single round-trip for all images in the batch
        var batchResponse = _client.BatchAnnotateImages(requests);

        // Match responses back to requests by index
        var results = new List<string>();
        for (int i = 0; i < batchResponse.Responses.Count; i++)
        {
            var response = batchResponse.Responses[i];
            if (response.Error != null)
            {
                // Per-item error in batch — must handle individually
                results.Add($"Error on {imagePaths[i]}: {response.Error.Message}");
                continue;
            }

            // Prefer DOCUMENT_TEXT_DETECTION full text if available
            var fullText = response.FullTextAnnotation?.Text
                ?? response.TextAnnotations.FirstOrDefault()?.Description
                ?? string.Empty;
            results.Add(fullText);
        }

        return results;
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

public class BatchAnnotationService
{
    public List<string> BatchAnnotateImages(string[] imagePaths)
    {
        var results = new ConcurrentDictionary<int, string>();

        // Parallel processing — no batch size limit, no network round-trips
        Parallel.For(0, imagePaths.Length, i =>
        {
            var ocr = new IronTesseract();   // thread-safe: one instance per thread
            results[i] = ocr.Read(imagePaths[i]).Text;
        });

        // Reconstruct in original order
        return Enumerable.Range(0, imagePaths.Length)
            .Select(i => results[i])
            .ToList();
    }

    public OcrResult BatchAsDocument(string[] imagePaths)
    {
        // Load all images into a single OcrInput for combined document output
        using var input = new OcrInput();
        foreach (var path in imagePaths)
            input.LoadImage(path);

        return new IronTesseract().Read(input);
    }
}
```

IronOCR runs the batch in parallel across CPU cores with zero network overhead. There is no `BatchSize` cap, no response-index matching, and no per-item error handling for network or credential failures. For workloads that combine multiple images into a single logical document — scanned multi-page forms submitted as individual JPEGs, for example — `BatchAsDocument` loads all images into one `OcrInput` and returns a unified `OcrResult` with `result.Pages` indexed to each input image. The [multithreading example](https://ironsoftware.com/csharp/ocr/examples/csharp-tesseract-multithreading-for-speed/) shows performance benchmarks for parallel processing.

### Migrating Protobuf Word-Level Annotation Extraction

Google Cloud Vision's word-level bounding box and confidence data requires navigating the full Protobuf hierarchy: Pages, then Blocks, then Paragraphs, then Words, then Symbols. Extracting word text requires concatenating Symbols from each Word — the `Word` object has no direct `.Text` property. Bounding box coordinates are stored as a `BoundingPoly` with a list of `Vertices` rather than as discrete X, Y, Width, Height fields.

**Google Cloud Vision Approach:**

```csharp
using Google.Cloud.Vision.V1;
using System.Collections.Generic;

public record WordAnnotation(string Text, int X, int Y, int Width, int Height, float Confidence);

public class WordLevelExtractor
{
    private readonly ImageAnnotatorClient _client;

    public WordLevelExtractor()
    {
        _client = ImageAnnotatorClient.Create();
    }

    public List<WordAnnotation> ExtractWordAnnotations(string imagePath)
    {
        var image = Image.FromFile(imagePath);
        var annotation = _client.DetectDocumentText(image);

        var words = new List<WordAnnotation>();

        // Navigate: Pages -> Blocks -> Paragraphs -> Words
        foreach (var page in annotation.Pages)
        {
            foreach (var block in page.Blocks)
            {
                foreach (var paragraph in block.Paragraphs)
                {
                    foreach (var word in paragraph.Words)
                    {
                        // Word.Text does not exist — must concatenate Symbols
                        var text = string.Concat(word.Symbols.Select(s => s.Text));

                        // BoundingPoly has Vertices, not X/Y/Width/Height
                        var vertices = word.BoundingBox.Vertices;
                        int x = vertices[0].X;
                        int y = vertices[0].Y;
                        int width = vertices.Count > 1 ? vertices[1].X - vertices[0].X : 0;
                        int height = vertices.Count > 2 ? vertices[2].Y - vertices[0].Y : 0;

                        words.Add(new WordAnnotation(text, x, y, width, height, word.Confidence));
                    }
                }
            }
        }

        return words;
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;
using System.Collections.Generic;

public record WordAnnotation(string Text, int X, int Y, int Width, int Height, double Confidence);

public class WordLevelExtractor
{
    private readonly IronTesseract _ocr;

    public WordLevelExtractor()
    {
        _ocr = new IronTesseract();
    }

    public List<WordAnnotation> ExtractWordAnnotations(string imagePath)
    {
        var result = _ocr.Read(imagePath);

        // Words are a flat collection — no hierarchy traversal, no symbol concatenation
        return result.Words.Select(w => new WordAnnotation(
            Text:       w.Text,         // direct string property
            X:          w.X,
            Y:          w.Y,
            Width:      w.Width,
            Height:     w.Height,
            Confidence: w.Confidence    // double, no conversion needed
        )).ToList();
    }
}
```

The symbol concatenation loop in the Google Cloud Vision version is not a design choice — it is required by the Protobuf schema. `Word.Text` is not a property in the response object. Every team that uses the word-level API writes an equivalent loop. IronOCR's `OcrResult.Words` collection exposes `Text`, `X`, `Y`, `Width`, `Height`, and `Confidence` as first-class properties. The [read results guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) documents the complete property set available at every granularity level.

### Multi-Page TIFF Processing to Searchable PDF Output

Google Cloud Vision treats multi-page TIFF files as a sequential series of images, each requiring a separate API call. There is no single API call that accepts a TIFF and returns structured output for all frames. Producing a searchable PDF from Google Cloud Vision results requires a separate PDF generation library — the API returns text only.

**Google Cloud Vision Approach:**

```csharp
using Google.Cloud.Vision.V1;
using System.Collections.Generic;
using System.Drawing;          // for TIFF frame extraction
using System.Drawing.Imaging;

public class TiffProcessingService
{
    private readonly ImageAnnotatorClient _client;

    public TiffProcessingService()
    {
        _client = ImageAnnotatorClient.Create();
    }

    public List<string> ProcessMultiPageTiff(string tiffPath)
    {
        var pageTexts = new List<string>();

        // Load TIFF and extract frames manually using System.Drawing
        using var tiff = System.Drawing.Image.FromFile(tiffPath);
        var frameDimension = new FrameDimension(tiff.FrameDimensionsList[0]);
        int frameCount = tiff.GetFrameCount(frameDimension);

        for (int i = 0; i < frameCount; i++)
        {
            tiff.SelectActiveFrame(frameDimension, i);

            // Save each frame to a temp file — Vision API does not accept TIFF frames directly
            var tempPath = Path.Combine(Path.GetTempPath(), $"tiff-frame-{i}.jpg");
            tiff.Save(tempPath, ImageFormat.Jpeg);

            // One API call per frame — each call = one unit of quota
            var visionImage = Google.Cloud.Vision.V1.Image.FromFile(tempPath);
            var response = _client.DetectText(visionImage);
            pageTexts.Add(response.FirstOrDefault()?.Description ?? string.Empty);

            File.Delete(tempPath);
        }

        // Producing a searchable PDF requires a separate library (e.g., iTextSharp)
        // Google Cloud Vision has no PDF output capability
        return pageTexts;
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public class TiffProcessingService
{
    private readonly IronTesseract _ocr;

    public TiffProcessingService()
    {
        _ocr = new IronTesseract();
    }

    public string ProcessMultiPageTiff(string tiffPath)
    {
        using var input = new OcrInput();
        // LoadImageFrames handles all TIFF frames in one call — no temp files, no frame loop
        input.LoadImageFrames(tiffPath);

        var result = _ocr.Read(input);
        return result.Text;
    }

    public void ProcessMultiPageTiffToSearchablePdf(string tiffPath, string outputPdfPath)
    {
        using var input = new OcrInput();
        input.LoadImageFrames(tiffPath);

        var result = _ocr.Read(input);

        // Google Cloud Vision has no equivalent — this single call produces a searchable PDF
        result.SaveAsSearchablePdf(outputPdfPath);
    }

    public void ProcessLowQualityTiff(string tiffPath, string outputPdfPath)
    {
        using var input = new OcrInput();
        input.LoadImageFrames(tiffPath);

        // Preprocessing before OCR improves accuracy on degraded scans
        input.Deskew();
        input.DeNoise();
        input.Contrast();

        var result = _ocr.Read(input);
        result.SaveAsSearchablePdf(outputPdfPath);
    }
}
```

The Google Cloud Vision version requires System.Drawing for frame extraction, temporary JPEG files written to disk, one API call per frame (consuming quota proportionally to frame count), and a separate PDF library to produce any output beyond plain text. IronOCR handles multi-frame TIFF natively through `LoadImageFrames`, processes all frames in a single `Read` call, and produces searchable PDF output through `SaveAsSearchablePdf`. For scanned archival TIFF documents, the preprocessing pipeline in `ProcessLowQualityTiff` addresses the most common quality issues in a single pass. See [TIFF and GIF input](https://ironsoftware.com/csharp/ocr/how-to/input-tiff-gif/) and [searchable PDF output](https://ironsoftware.com/csharp/ocr/how-to/searchable-pdf/) for the complete API.

### Rate-Limited Batch Migration with Progress Tracking

At production scale, Google Cloud Vision's 1,800-requests-per-minute default quota requires either throttling or retry logic with exponential backoff. Processing 5,000 documents in a single overnight job will exceed the quota multiple times. Each quota exceedance blocks the pipeline for a mandatory 60-second wait. IronOCR has no rate limit — the pipeline is limited only by CPU cores and available threads.

**Google Cloud Vision Approach:**

```csharp
using Google.Cloud.Vision.V1;
using Grpc.Core;
using System.Collections.Generic;

public class ThrottledBatchProcessor
{
    private readonly ImageAnnotatorClient _client;
    private const int MaxRequestsPerMinute = 1800;
    private const int RetryDelayMs = 60_000;

    public ThrottledBatchProcessor()
    {
        _client = ImageAnnotatorClient.Create();
    }

    public async Task<Dictionary<string, string>> ProcessWithThrottlingAsync(
        string[] imagePaths,
        IProgress<(int completed, int total)> progress)
    {
        var results = new Dictionary<string, string>();
        int completed = 0;

        foreach (var path in imagePaths)
        {
            bool succeeded = false;
            while (!succeeded)
            {
                try
                {
                    var image = Google.Cloud.Vision.V1.Image.FromFile(path);
                    var response = _client.DetectText(image);
                    results[path] = response.FirstOrDefault()?.Description ?? string.Empty;
                    succeeded = true;
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.ResourceExhausted)
                {
                    // Rate limit exceeded — wait and retry
                    await Task.Delay(RetryDelayMs);
                }
            }

            progress.Report((++completed, imagePaths.Length));
        }

        return results;
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class BatchProcessor
{
    public Dictionary<string, string> ProcessBatch(
        string[] imagePaths,
        IProgress<(int completed, int total)> progress)
    {
        var results = new ConcurrentDictionary<string, string>();
        int completed = 0;

        // No rate limits — full parallelism, no retry logic needed
        Parallel.ForEach(imagePaths, imagePath =>
        {
            var ocr = new IronTesseract();
            results[imagePath] = ocr.Read(imagePath).Text;
            progress.Report((Interlocked.Increment(ref completed), imagePaths.Length));
        });

        return new Dictionary<string, string>(results);
    }

    public void ProcessBatchToSearchablePdfs(
        string[] imagePaths,
        string outputDirectory)
    {
        Parallel.ForEach(imagePaths, imagePath =>
        {
            var ocr = new IronTesseract();
            var result = ocr.Read(imagePath);

            var outputPath = Path.Combine(
                outputDirectory,
                Path.GetFileNameWithoutExtension(imagePath) + "-searchable.pdf");

            result.SaveAsSearchablePdf(outputPath);
        });
    }
}
```

The Google Cloud Vision version is sequential because parallel requests would multiply the rate limit exposure. Each `ResourceExhausted` exception adds a full 60-second stall. A batch of 5,000 documents that hits the quota 10 times adds 10 minutes of idle waiting. IronOCR's version parallelizes across all available cores with no waiting. For long-running batches, the [progress tracking API](https://ironsoftware.com/csharp/ocr/how-to/progress-tracking/) provides built-in progress callbacks without requiring manual `Interlocked.Increment` wiring. For image quality issues in batch scans, the [image quality correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/) covers the preprocessing pipeline that can be added before each `Read` call.

## Google Cloud Vision OCR API to IronOCR Mapping Reference

| Google Cloud Vision | IronOCR | Notes |
|---|---|---|
| `ImageAnnotatorClient.Create()` | `new IronTesseract()` | Client initialization; no credential file needed |
| `Image.FromFile(path)` | `_ocr.Read(path)` or `input.LoadImage(path)` | Direct path reading available on `IronTesseract` |
| `_client.DetectText(image)` | `_ocr.Read(path).Text` | `TEXT_DETECTION` equivalent |
| `_client.DetectDocumentText(image)` | `_ocr.Read(path)` | `DOCUMENT_TEXT_DETECTION` equivalent; mode is automatic |
| `response[0].Description` | `result.Text` | Full document text |
| `TextAnnotation` | `OcrResult` | Top-level result container |
| `annotation.Text` | `result.Text` | Full text string |
| `annotation.Pages[i]` | `result.Pages[i]` | Per-page access |
| `page.Blocks[i].Paragraphs[j]` | `result.Paragraphs[i]` | IronOCR exposes paragraphs as a flat collection |
| `paragraph.Words.SelectMany(w => w.Symbols).Select(s => s.Text)` | `paragraph.Text` | Direct string property; no symbol iteration |
| `word.BoundingBox.Vertices` | `word.X`, `word.Y`, `word.Width`, `word.Height` | Discrete int properties instead of vertex list |
| `word.Confidence` | `word.Confidence` | Per-word confidence score |
| `page.Confidence` | `result.Confidence` | Overall result confidence |
| `Feature.Types.Type.DocumentTextDetection` | Automatic | IronOCR auto-selects processing mode |
| `BatchAnnotateImagesRequest` | `Parallel.ForEach` + `new IronTesseract()` per thread | Parallel local processing; no batch size cap |
| `_client.BatchAnnotateImages(requests)` | `new IronTesseract().Read(input)` with multi-image `OcrInput` | Single call for multi-image input |
| `AsyncBatchAnnotateFilesAsync()` | `input.LoadPdf(); _ocr.Read(input)` | PDF processing is synchronous; no GCS required |
| `StorageClient.Create()` | Not needed | No GCS dependency |
| `storageClient.UploadObjectAsync()` | Not needed | PDFs load directly from local path or stream |
| `operation.PollUntilCompletedAsync()` | Not needed | Processing is synchronous |
| `RpcException (StatusCode.ResourceExhausted)` | Not applicable | No rate limits |
| `RpcException (StatusCode.PermissionDenied)` | Not applicable | No runtime authentication |
| `GOOGLE_APPLICATION_CREDENTIALS` env var | `IronOcr.License.LicenseKey` | String assignment, not file path |

## Common Migration Issues and Solutions

### Issue 1: Constructor Throws Without GOOGLE_APPLICATION_CREDENTIALS

**Google Cloud Vision:** `ImageAnnotatorClient.Create()` throws `RpcException` with `StatusCode.Unauthenticated` or `StatusCode.PermissionDenied` if the environment variable is not set or points to an invalid file. This failure occurs at startup, not at the first API call, which means the entire application fails to initialize if credentials are missing in any one environment.

**Solution:** After removing the Google Cloud Vision packages, delete all references to `GOOGLE_APPLICATION_CREDENTIALS` from your environment configurations, CI/CD pipeline secrets, Kubernetes secrets, and Docker Compose files. Replace with a single `IRONOCR_LICENSE` environment variable:

```csharp
// Remove this from every deployment environment:
// GOOGLE_APPLICATION_CREDENTIALS=/path/to/service-account.json

// Add this once at application startup:
IronOcr.License.LicenseKey = Environment.GetEnvironmentVariable("IRONOCR_LICENSE")
    ?? throw new InvalidOperationException("IRONOCR_LICENSE environment variable is required.");
```

### Issue 2: Protobuf Symbol Concatenation Code Breaks After Namespace Removal

**Google Cloud Vision:** Every location in your codebase where paragraph or word text was extracted using `.SelectMany(w => w.Symbols).Select(s => s.Text)` will produce compile errors after the `Google.Cloud.Vision.V1` namespace is removed. These calls are spread across whatever helper or service classes consumed the API response.

**Solution:** Search for all `SelectMany` and `w.Symbols` patterns in your codebase and replace them with direct property access on IronOCR result objects. The [read results how-to guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) covers every available property on `OcrResult`, `OcrResult.Page`, `OcrResult.Paragraph`, `OcrResult.Line`, and `OcrResult.Word`:

```bash
# Find all Protobuf symbol concatenation patterns
grep -rn "\.Symbols\." --include="*.cs" .
grep -rn "SelectMany.*Symbols" --include="*.cs" .
grep -rn "w\.Symbols\.Select" --include="*.cs" .
```

Replace each occurrence:

```csharp
// Before: symbol concatenation required by Protobuf schema
var text = string.Join("", paragraph.Words.SelectMany(w => w.Symbols).Select(s => s.Text));

// After: direct property on OcrResult.Paragraph
var text = paragraph.Text;
```

### Issue 3: PDF Processing Code Does Not Compile After Storage.V1 Removal

**Google Cloud Vision:** After removing `Google.Cloud.Storage.V1`, all code that references `StorageClient`, `UploadObjectAsync`, `DeleteObjectAsync`, `AsyncAnnotateFileRequest`, `GcsSource`, `GcsDestination`, and `PollUntilCompletedAsync` will fail to compile. This code may span multiple service classes and typically represents the largest single block of changes.

**Solution:** Delete the entire GCS pipeline. Replace the 50-plus line async method with the IronOCR three-line equivalent. For code that maintained an async signature for caller compatibility, wrap with `Task.Run`:

```csharp
// Delete: StorageClient, GCS upload, AsyncBatchAnnotateFilesAsync,
//         PollUntilCompletedAsync, output download, DeleteObjectAsync

// Replace with:
public async Task<string> ProcessPdfAsync(string pdfPath)
{
    return await Task.Run(() =>
    {
        using var input = new OcrInput();
        input.LoadPdf(pdfPath);
        return new IronTesseract().Read(input).Text;
    });
}
```

For new code, use the [native async OCR support](https://ironsoftware.com/csharp/ocr/how-to/async/) instead of the `Task.Run` wrapper. The [PDF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-pdfs/) covers page range selection and password-protected PDF loading.

### Issue 4: Rate Limit Retry Logic Is No Longer Needed

**Google Cloud Vision:** Any code that catches `RpcException` with `StatusCode.ResourceExhausted` and implements a wait-and-retry pattern was written to handle the 1,800-requests-per-minute quota. This retry logic may be embedded in middleware, pipeline steps, or batch processing loops.

**Solution:** Remove all retry logic associated with quota errors. IronOCR processes locally with no external quota. The error handler contract changes from five `RpcException` cases to two:

```csharp
// Remove: all RpcException handlers for ResourceExhausted, PermissionDenied,
//         Unavailable, DeadlineExceeded, Unauthenticated

// IronOCR error surface:
try
{
    var result = new IronTesseract().Read(imagePath);
    if (result.Confidence < 50)
        input.DeNoise(); // add preprocessing for low-confidence results
    return result.Text;
}
catch (IOException ex)
{
    // File not found or locked
    throw new InvalidOperationException($"Cannot read: {imagePath}", ex);
}
catch (IronOcr.Exceptions.OcrException ex)
{
    // Processing failure — not a transient network error
    throw new InvalidOperationException($"OCR failed: {ex.Message}", ex);
}
```

### Issue 5: Multi-Page TIFF Requires Frame Extraction Loop

**Google Cloud Vision:** Existing TIFF processing code likely extracts frames using `System.Drawing.Image`, saves each frame as a JPEG to a temp directory, submits each JPEG as a separate API call, and deletes the temp files afterward. This pattern consumes one quota unit per frame and may leave orphaned temp files on crash.

**Solution:** Replace the frame extraction loop and temp file management with `input.LoadImageFrames()`. The entire System.Drawing frame loop is deleted:

```csharp
// Remove: System.Drawing frame extraction, temp file writes, per-frame API calls

// Replace with:
using var input = new OcrInput();
input.LoadImageFrames(tiffPath);   // all frames, no temp files
var result = new IronTesseract().Read(input);
```

See the [TIFF and GIF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-tiff-gif/) for multi-frame processing options including frame range selection.

### Issue 6: BoundingPoly Vertex Calculations Break

**Google Cloud Vision:** Code that read bounding box coordinates from `annotation.BoundingPoly.Vertices` calculated X, Y, Width, and Height from vertex index arithmetic: `vertices[0].X` for X, `vertices[1].X - vertices[0].X` for Width, `vertices[2].Y - vertices[0].Y` for Height. After migration, these expressions have no equivalent in IronOCR because `Vertices` does not exist.

**Solution:** Replace vertex arithmetic with direct int properties. No calculation is needed:

```csharp
// Before: vertex index arithmetic
int x = word.BoundingBox.Vertices[0].X;
int y = word.BoundingBox.Vertices[0].Y;
int width  = word.BoundingBox.Vertices[1].X - word.BoundingBox.Vertices[0].X;
int height = word.BoundingBox.Vertices[2].Y - word.BoundingBox.Vertices[0].Y;

// After: direct properties
int x      = word.X;
int y      = word.Y;
int width  = word.Width;
int height = word.Height;
```

## Google Cloud Vision OCR Migration Checklist

### Pre-Migration Tasks

Audit the codebase to identify every Google Cloud Vision dependency before making any changes:

```bash
# Find all Google Cloud Vision namespace imports
grep -rn "using Google.Cloud.Vision" --include="*.cs" .
grep -rn "using Google.Cloud.Storage" --include="*.cs" .
grep -rn "using Grpc.Core" --include="*.cs" .

# Find ImageAnnotatorClient usage
grep -rn "ImageAnnotatorClient" --include="*.cs" .

# Find GCS pipeline code
grep -rn "StorageClient\|UploadObjectAsync\|DeleteObjectAsync" --include="*.cs" .
grep -rn "AsyncBatchAnnotateFilesAsync\|PollUntilCompleted" --include="*.cs" .

# Find Protobuf symbol concatenation
grep -rn "\.Symbols\." --include="*.cs" .
grep -rn "SelectMany.*Symbols" --include="*.cs" .

# Find BoundingPoly vertex calculations
grep -rn "BoundingPoly\|BoundingBox\.Vertices" --include="*.cs" .

# Find rate limit retry handlers
grep -rn "ResourceExhausted\|StatusCode\." --include="*.cs" .

# Find environment variable references
grep -rn "GOOGLE_APPLICATION_CREDENTIALS" .
```

Inventory notes to complete before starting:
- List all GCS buckets created for OCR input/output — schedule cleanup after migration
- Document the service account email so it can be disabled in GCP Console post-migration
- Identify all environments where `GOOGLE_APPLICATION_CREDENTIALS` is configured
- Note any code that reads GCP project IDs or bucket names from configuration — remove after migration

### Code Update Tasks

1. Remove `Google.Cloud.Vision.V1` NuGet package from all projects
2. Remove `Google.Cloud.Storage.V1` NuGet package from all projects
3. Install `IronOcr` NuGet package in all projects that perform OCR
4. Add `IronOcr.License.LicenseKey` initialization at application startup
5. Replace all `using Google.Cloud.Vision.V1` imports with `using IronOcr`
6. Replace all `using Google.Cloud.Storage.V1` and `using Grpc.Core` imports
7. Replace `ImageAnnotatorClient.Create()` with `new IronTesseract()`
8. Delete all GCS pipeline methods (`StorageClient`, `UploadObjectAsync`, async annotation, polling, download, delete)
9. Replace `input.LoadPdf()` for all PDF processing paths (removing the async GCS orchestration)
10. Replace all Protobuf symbol concatenation loops with direct `.Text` property access on `OcrResult` objects
11. Replace `BoundingPoly.Vertices` index calculations with `word.X`, `word.Y`, `word.Width`, `word.Height`
12. Remove all `RpcException` catch blocks for `ResourceExhausted`, `PermissionDenied`, `Unavailable`, `DeadlineExceeded`, and `Unauthenticated`
13. Replace per-frame TIFF loop with `input.LoadImageFrames()`
14. Convert sequential batch loops to `Parallel.ForEach` with per-thread `IronTesseract` instances
15. Remove `GOOGLE_APPLICATION_CREDENTIALS` from all environment configurations, CI/CD pipelines, Docker Compose files, and Kubernetes secrets

### Post-Migration Testing

- Verify that no `RpcException` or `GoogleApiException` types remain referenced in error handling code
- Confirm `GOOGLE_APPLICATION_CREDENTIALS` is absent from all deployment environment configurations
- Run the OCR pipeline on the same set of sample documents used in production and compare text output quality
- Test PDF processing on documents previously handled by the GCS async pipeline and confirm identical text output
- Test password-protected PDFs with `input.LoadPdf(path, Password: "...")` — previously not supported
- Test multi-page TIFF processing using `input.LoadImageFrames()` and verify all frames are processed
- Run the batch processor on a representative sample and confirm output quality matches previous results
- Confirm `result.Confidence` values are within acceptable range for your document corpus
- Verify searchable PDF output using `result.SaveAsSearchablePdf()` for documents that previously required a separate PDF library
- Run the application in an environment with no outbound internet connectivity and confirm OCR works correctly

## Key Benefits of Migrating to IronOCR

**Credential Surface Reduced to Zero Files.** After migration, there are no JSON key files, no GCS bucket configurations, no IAM roles, no service accounts, and no `GOOGLE_APPLICATION_CREDENTIALS` environment variables in your infrastructure. The entire credential surface is one environment variable containing a license key string. Key rotation, which was a mandatory periodic operation with Google Cloud Vision, is no longer a concept that applies. For teams that operate in multiple regions or cloud providers, the reduction in deployment configuration complexity is immediate.

**PDF and TIFF Processing Without External Dependencies.** The GCS async pipeline and the System.Drawing TIFF frame loop are deleted entirely. `input.LoadPdf()` and `input.LoadImageFrames()` are the replacements — both synchronous, both local, both three lines from call to result. Password-protected PDFs, which were impossible with Google Cloud Vision, work with a single additional parameter. The [PDF OCR guide](https://ironsoftware.com/csharp/ocr/how-to/input-pdfs/) and the [TIFF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-tiff-gif/) cover the full input API.

**Batch Processing at CPU Speed.** Removing the 1,800-requests-per-minute quota and the mandatory 60-second retry waits means batch jobs that were previously rate-constrained now run at the speed of available processor cores. A machine with 16 cores processes 16 documents simultaneously with zero external approval. The `Parallel.ForEach` pattern with per-thread `IronTesseract` instances is the direct replacement for the throttled sequential loop. The [speed optimization guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-fast-configuration/) covers engine configuration options that tune throughput for specific document types.

**Structured Data Without Protobuf.** Every `OcrResult` exposes `Text`, `Confidence`, `Pages`, `Paragraphs`, `Lines`, `Words`, and `Characters` as typed .NET properties with no Protobuf namespace dependency, no symbol concatenation, and no vertex arithmetic for bounding boxes. Code that previously required 20-line nested loops to extract paragraph text reduces to `result.Paragraphs.Select(p => p.Text)`. For use cases that need word-level positioning for document layout analysis, `word.X`, `word.Y`, `word.Width`, and `word.Height` are available directly. The [OCR results features page](https://ironsoftware.com/csharp/ocr/features/ocr-results/) documents every property in the result model.

**Searchable PDF Output Built In.** Google Cloud Vision returns text only — producing a searchable PDF required a separate PDF generation library, adding another NuGet dependency, another API to learn, and additional licensing to evaluate. IronOCR's `result.SaveAsSearchablePdf(outputPath)` produces a fully searchable PDF from any OCR result in one line. For document archival workflows and legal discovery pipelines, this eliminates an entire dependency. The [searchable PDF example](https://ironsoftware.com/csharp/ocr/examples/make-pdf-searchable/) demonstrates the pattern end-to-end.

**Data Sovereignty for Regulated Industries.** Documents processed by IronOCR never leave the server. For HIPAA-covered healthcare records, ITAR-controlled technical data, CMMC-scoped defense contractor materials, attorney-client privileged legal documents, and PCI-DSS-in-scope financial records, the on-premise architecture removes the third-party data processor category from compliance scope entirely. There is no Business Associate Agreement to negotiate, no DPA to execute, and no Google data retention policy to review. The [IronOCR documentation hub](https://ironsoftware.com/csharp/ocr/docs/) covers deployment configurations for Docker, Linux, Azure, and AWS environments where data residency requirements apply.
