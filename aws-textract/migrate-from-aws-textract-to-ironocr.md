# Migrating from AWS Textract to IronOCR

This guide walks through a complete migration from AWS Textract to [IronOCR](https://ironsoftware.com/csharp/ocr/) for .NET developers who need local document processing without cloud dependencies. It covers the credential teardown, async pipeline removal, S3 elimination, and the specific code replacements required to move each Textract operation to its IronOCR equivalent.

## Why Migrate from AWS Textract

AWS Textract is a capable managed service, but its architecture imposes costs — financial and operational — that grow as your document workload grows. Teams building serious document processing pipelines eventually encounter all of these friction points.

**AWS Credential Infrastructure Compounds Every Deployment.** Every environment that runs Textract code needs AWS credentials: IAM user or role, access key and secret, region configuration, and — for PDF processing — S3 bucket permissions on top. That is five distinct configuration steps before the first page processes. Production deployments require credential rotation policies, secure injection into Docker containers or Kubernetes pods, and monitoring for `AccessDeniedException` when IAM policy drift causes silent failures. IronOCR requires one string: a license key, set once at startup.

**Per-Page Pricing Has No Ceiling.** The $0.0015 per-page base rate for `DetectDocumentText` is the floor, not the cost. Any document with tables triggers `AnalyzeDocument` at $0.015 per page — ten times the base rate. Forms add another $0.05 per page. A mixed document workflow at 100,000 pages per month with table extraction runs $18,000 per year and that number resets every January. The IronOCR Professional license costs $2,999 once. After that, the per-page cost is zero regardless of volume.

**The Async PDF Pipeline Is a Maintenance Liability.** Processing any multi-page PDF through Textract requires five distinct phases: S3 upload, `StartDocumentTextDetection`, polling loop, paginated result retrieval, and S3 cleanup. Each phase is an independent failure mode requiring its own error handling, retry strategy, and timeout management. Teams that ship this pipeline into production consistently report spending more time maintaining it than they spent building it. IronOCR reads a PDF with two lines of code.

**No Internet Access Means No Textract.** Docker containers in isolated network segments, on-premise servers, air-gapped research environments, and industrial systems with outbound traffic restrictions all share one characteristic: Textract is unavailable. IronOCR installs as a NuGet package and operates without any network calls after the initial package download.

**Data Leaves Your Infrastructure on Every Call.** Every OCR operation with Textract transmits document content to Amazon's servers. For healthcare organizations processing PHI, defense contractors handling CUI, legal teams processing privileged communications, and financial institutions processing payment card images, this is not a configuration option — it is an architectural constraint that may prohibit Textract entirely. IronOCR processes on your hardware. There is no cloud mode to disable; local processing is the only mode.

**Rate Limits Constrain Batch Throughput.** The default `StartDocumentTextDetection` TPS limit is 5 requests per second. Batch jobs processing hundreds of documents require `SemaphoreSlim` throttling, exponential backoff on `ProvisionedThroughputExceededException`, and rate-replenishment timers. Requesting a TPS increase requires a formal AWS support case with no guaranteed outcome. IronOCR processes as fast as the local CPU allows — a 16-core server processes 16 documents concurrently with a plain `Parallel.ForEach`, no service tier negotiation required.

### The Fundamental Problem

Every Textract deployment starts with this ceremony — before a single page can be processed:

```csharp
// Textract: five environment variables, an IAM role, and an S3 bucket
// — all required before the first OCR call

// Environment must have:
//   AWS_ACCESS_KEY_ID=AKIA...
//   AWS_SECRET_ACCESS_KEY=...
//   AWS_DEFAULT_REGION=us-east-1
//   IAM role: textract:DetectDocumentText, textract:AnalyzeDocument
//   IAM role: s3:PutObject, s3:DeleteObject (for any PDF processing)
//   S3 bucket: my-textract-staging-bucket (with lifecycle policy to prevent cost accumulation)

public class TextractOcrService
{
    private readonly AmazonTextractClient _textractClient;
    private readonly AmazonS3Client _s3Client; // required for PDFs

    public TextractOcrService()
    {
        // Fails with AmazonClientException if credentials not found in chain
        _textractClient = new AmazonTextractClient(Amazon.RegionEndpoint.USEast1);
        _s3Client = new AmazonS3Client(Amazon.RegionEndpoint.USEast1);
    }
}
```

IronOCR replaces the entire credential chain with a single assignment:

```csharp
// IronOCR: one line, one string, done
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

// Or from environment (no IAM, no rotation, no S3)
IronOcr.License.LicenseKey = Environment.GetEnvironmentVariable("IRONOCR_LICENSE");
```

## IronOCR vs AWS Textract: Feature Comparison

The following table maps the capabilities of both libraries across the dimensions most relevant to migration decisions.

| Feature | AWS Textract | IronOCR |
|---|---|---|
| **Processing Location** | Amazon cloud (mandatory) | Local / on-premise only |
| **Internet Required** | Always | Never |
| **Credential Setup** | IAM user/role + optional S3 bucket | Single license key string |
| **Multi-Page PDF** | Requires S3 staging + async job | Direct synchronous `input.LoadPdf()` |
| **Password-Protected PDF** | Not supported | `input.LoadPdf(path, Password: "x")` |
| **Multi-Frame TIFF** | Not supported | `input.LoadImageFrames("multi.tiff")` |
| **Async Polling Required** | Yes (for all PDFs) | No — synchronous by default |
| **Rate Limits** | 5 TPS default (StartDocumentTextDetection) | None — CPU-bound only |
| **Per-Page Cost** | $0.0015–$0.065 per page | $0 after license purchase |
| **Cost at 100K pages/month (tables)** | $18,000/year | $0/year (after $2,999 license) |
| **Automatic Preprocessing** | Internal, not configurable | Deskew, DeNoise, Contrast, Binarize, Sharpen, Scale |
| **Searchable PDF Output** | Not available | `result.SaveAsSearchablePdf()` |
| **hOCR Export** | Not available | `result.SaveAsHocrFile()` |
| **Barcode Reading** | Not available | `ocr.Configuration.ReadBarCodes = true` |
| **Region-Based OCR** | Via AnalyzeDocument + block relationships | `CropRectangle(x, y, width, height)` |
| **Structured Results** | Flat `List<Block>` filtered by `BlockType` | Typed `Pages`, `Paragraphs`, `Lines`, `Words` |
| **Languages Supported** | English-dominant (select additional) | 125+ language packs via NuGet |
| **Multi-Language Simultaneous** | Not supported | `OcrLanguage.French + OcrLanguage.German` |
| **Air-Gapped Deployment** | Not possible | Fully supported |
| **HIPAA Without BAA** | Not possible | No external processor — on-premise only |
| **Docker Deployment** | Requires injected AWS credentials | No credentials — plain NuGet package |
| **Cross-Platform** | AWS-managed (Linux only) | Windows, Linux, macOS, Docker, Azure, AWS EC2 |
| **Licensing Model** | Per-page metered billing (perpetual spend) | Perpetual one-time ($749 / $1,499 / $2,999) |

## Quick Start: AWS Textract to IronOCR Migration

### Step 1: Replace NuGet Package

Remove the AWS SDK packages:

```bash
dotnet remove package AWSSDK.Textract
dotnet remove package AWSSDK.S3
dotnet remove package AWSSDK.Core
```

Install IronOCR from [NuGet](https://www.nuget.org/packages/IronOcr):

```bash
dotnet add package IronOcr
```

### Step 2: Update Namespaces

Replace all AWS namespace imports with the single IronOCR namespace:

```csharp
// Before (AWS Textract)
using Amazon.Textract;
using Amazon.Textract.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;

// After (IronOCR)
using IronOcr;
```

### Step 3: Initialize License

Add license initialization once at application startup. Remove all AWS credential environment variable setup:

```csharp
// Remove from startup:
//   AWS_ACCESS_KEY_ID environment variable
//   AWS_SECRET_ACCESS_KEY environment variable
//   AWS_DEFAULT_REGION environment variable
//   ~/.aws/credentials file entries
//   IAM role bindings on the execution context

// Add instead:
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
```

## Code Migration Examples

### Replacing the AmazonTextractClient Constructor and IAM Setup

The most visible migration change is the client initialization. Textract requires a dependency-injected client with underlying credential resolution — meaning both the client and all its dependencies must be pre-configured in every deployment environment. Any misconfiguration fails silently until the first OCR call throws `AmazonClientException`.

**AWS Textract Approach:**

```csharp
// Requires: NuGet AWSSDK.Textract, AWSSDK.S3
// Requires: AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, AWS_DEFAULT_REGION in environment
// Requires: IAM policy with textract:* and s3:PutObject, s3:DeleteObject

using Amazon.S3;
using Amazon.Textract;

public class DocumentOcrService
{
    private readonly AmazonTextractClient _textractClient;
    private readonly AmazonS3Client _s3Client;
    private readonly string _stagingBucket;

    public DocumentOcrService(IConfiguration config)
    {
        // Credential chain: environment → ~/.aws/credentials → EC2 instance profile
        // Fails if none found — runtime exception, not compile-time
        _textractClient = new AmazonTextractClient(Amazon.RegionEndpoint.USEast1);
        _s3Client = new AmazonS3Client(Amazon.RegionEndpoint.USEast1);
        _stagingBucket = config["Textract:StagingBucket"]; // bucket must exist and have permissions
    }

    public async Task<string> ReadImageAsync(string imagePath)
    {
        var bytes = File.ReadAllBytes(imagePath);
        var response = await _textractClient.DetectDocumentTextAsync(
            new DetectDocumentTextRequest
            {
                Document = new Document { Bytes = new MemoryStream(bytes) }
            });

        return string.Join("\n", response.Blocks
            .Where(b => b.BlockType == BlockType.LINE)
            .Select(b => b.Text));
    }
}
```

**IronOCR Approach:**

```csharp
// Requires: NuGet IronOcr
// Requires: one license key string — nothing else

using IronOcr;

public class DocumentOcrService
{
    private readonly IronTesseract _ocr;

    public DocumentOcrService()
    {
        IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY"; // or set at Program.cs startup
        _ocr = new IronTesseract();
    }

    public string ReadImage(string imagePath)
    {
        // No credential chain, no region, no bucket — just read
        return _ocr.Read(imagePath).Text;
    }
}
```

The entire AWS credential infrastructure — IAM roles, environment variables, `~/.aws/credentials` files, S3 bucket lifecycle policies, and region configuration — is removed. The `DocumentOcrService` constructor goes from a 3-dependency injection site with runtime failure modes to a single license assignment. For details on deployment patterns, see the [IronTesseract setup guide](https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/).

### Replacing StartDocumentTextDetection with TIFF Multi-Frame Processing

Textract's async job API (`StartDocumentTextDetection`) is required for any multi-page document. A common pattern in document digitization pipelines is receiving scanned documents as multi-frame TIFFs — one frame per scanned page. Textract cannot accept TIFF input directly at all; the document must be converted to PDF and uploaded to S3 before a job can start. IronOCR reads multi-frame TIFFs natively.

**AWS Textract Approach:**

```csharp
// Textract cannot accept TIFF directly — requires PDF conversion + S3 + async job
// This shows the conversion overhead plus the minimum async pipeline

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Textract;
using Amazon.Textract.Model;

public async Task<string> ProcessScannedTiffAsync(string tiffPath, string s3Bucket)
{
    // Step 0: Convert TIFF to PDF first (Textract does not accept TIFF for async jobs)
    // Requires a separate PDF conversion library — not shown here
    var pdfPath = Path.ChangeExtension(tiffPath, ".pdf");
    ConvertTiffToPdf(tiffPath, pdfPath); // external dependency required

    // Step 1: Upload converted PDF to S3
    var s3Key = $"staging/{Guid.NewGuid()}.pdf";
    using (var stream = File.OpenRead(pdfPath))
    {
        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = s3Bucket,
            Key = s3Key,
            InputStream = stream
        });
    }

    try
    {
        // Step 2: Start async detection job
        var startResp = await _textractClient.StartDocumentTextDetectionAsync(
            new StartDocumentTextDetectionRequest
            {
                DocumentLocation = new DocumentLocation
                {
                    S3Object = new S3Object { Bucket = s3Bucket, Name = s3Key }
                }
            });

        // Step 3: Poll every 5 seconds — can block for 30–120 seconds on large files
        GetDocumentTextDetectionResponse pollResp;
        do
        {
            await Task.Delay(5000);
            pollResp = await _textractClient.GetDocumentTextDetectionAsync(
                new GetDocumentTextDetectionRequest { JobId = startResp.JobId });
        } while (pollResp.JobStatus == JobStatus.IN_PROGRESS);

        if (pollResp.JobStatus != JobStatus.SUCCEEDED)
            throw new Exception($"Job failed: {pollResp.StatusMessage}");

        // Step 4: Collect paginated results
        var text = new StringBuilder();
        string token = null;
        do
        {
            var page = await _textractClient.GetDocumentTextDetectionAsync(
                new GetDocumentTextDetectionRequest
                {
                    JobId = startResp.JobId,
                    NextToken = token
                });
            foreach (var block in page.Blocks.Where(b => b.BlockType == BlockType.LINE))
                text.AppendLine(block.Text);
            token = page.NextToken;
        } while (token != null);

        return text.ToString();
    }
    finally
    {
        // Step 5: Clean up S3 — orphaned objects accumulate storage costs
        await _s3Client.DeleteObjectAsync(s3Bucket, s3Key);
        File.Delete(pdfPath); // clean up intermediate PDF
    }
}
```

**IronOCR Approach:**

```csharp
// IronOCR reads multi-frame TIFF directly — no conversion, no S3, no polling

using IronOcr;

public string ProcessScannedTiff(string tiffPath)
{
    using var input = new OcrInput();
    input.LoadImageFrames(tiffPath); // reads all frames natively

    var ocr = new IronTesseract();
    var result = ocr.Read(input);

    // Access per-page results if needed
    foreach (var page in result.Pages)
        Console.WriteLine($"Page {page.PageNumber}: {page.Words.Length} words detected");

    return result.Text;
}
```

The TIFF-to-PDF conversion step, the S3 upload, the async polling loop, the paginated result accumulation, and the S3 cleanup are all eliminated. The IronOCR implementation reads frames directly and provides per-page access through `result.Pages` without any intermediate format conversion. The [TIFF and GIF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-tiff-gif/) covers frame selection for cases where only a subset of frames are needed.

### Replacing S3 Staging with Searchable PDF Output

A common Textract use case is converting scanned PDF archives into searchable form. The Textract approach requires uploading each PDF to S3, running the async job, collecting the extracted text, then using a separate PDF library to embed that text as a searchable layer. IronOCR performs the scan and produces a searchable PDF in a single call.

**AWS Textract Approach:**

```csharp
// Textract extracts text but cannot produce searchable PDFs
// Requires: S3 upload + async job + separate PDF library to embed the text layer

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Textract;
using Amazon.Textract.Model;

public async Task<string> ExtractTextForSearchableLayerAsync(
    string scannedPdfPath,
    string s3Bucket)
{
    // Must upload to S3 — cannot pass PDF bytes directly for async jobs
    var key = $"ocr-input/{Guid.NewGuid()}.pdf";

    await using (var fs = File.OpenRead(scannedPdfPath))
    {
        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = s3Bucket,
            Key = key,
            InputStream = fs,
            ContentType = "application/pdf"
        });
    }

    string jobId;
    try
    {
        var startResp = await _textractClient.StartDocumentTextDetectionAsync(
            new StartDocumentTextDetectionRequest
            {
                DocumentLocation = new DocumentLocation
                {
                    S3Object = new S3Object { Bucket = s3Bucket, Name = key }
                }
            });
        jobId = startResp.JobId;
    }
    catch
    {
        await _s3Client.DeleteObjectAsync(s3Bucket, key);
        throw;
    }

    // Poll — minimum 5s wait; typical 15–60s for a 10-page PDF
    GetDocumentTextDetectionResponse pollResp;
    int pollAttempts = 0;
    const int maxAttempts = 120; // 10 minute timeout

    do
    {
        await Task.Delay(5000);
        pollResp = await _textractClient.GetDocumentTextDetectionAsync(
            new GetDocumentTextDetectionRequest { JobId = jobId });

        if (++pollAttempts >= maxAttempts)
            throw new TimeoutException("Textract job timed out after 10 minutes");

    } while (pollResp.JobStatus == JobStatus.IN_PROGRESS);

    await _s3Client.DeleteObjectAsync(s3Bucket, key); // cleanup regardless of outcome

    if (pollResp.JobStatus != JobStatus.SUCCEEDED)
        throw new Exception($"Textract job status: {pollResp.JobStatus} — {pollResp.StatusMessage}");

    // Return extracted text — caller must use a separate library to produce searchable PDF
    return string.Join("\n", pollResp.Blocks
        .Where(b => b.BlockType == BlockType.LINE)
        .Select(b => b.Text));

    // Caller still needs: iTextSharp, PdfSharp, or similar to embed text layer
}
```

**IronOCR Approach:**

```csharp
// IronOCR reads the PDF and produces a searchable PDF output directly
// No S3, no polling, no external PDF library needed

using IronOcr;

public void ConvertToSearchablePdf(string scannedPdfPath, string outputPath)
{
    var ocr = new IronTesseract();

    using var input = new OcrInput();
    input.LoadPdf(scannedPdfPath);

    var result = ocr.Read(input);

    // Embed extracted text as searchable layer in one call
    result.SaveAsSearchablePdf(outputPath);

    Console.WriteLine($"Pages processed: {result.Pages.Length}");
    Console.WriteLine($"Overall confidence: {result.Confidence:F1}%");
}
```

The polling timeout logic, S3 upload-and-cleanup pattern, and the external PDF library dependency are all gone. `SaveAsSearchablePdf` embeds the recognized text directly into the output file, making every scanned page full-text searchable without a second library. The [searchable PDF guide](https://ironsoftware.com/csharp/ocr/how-to/searchable-pdf/) covers page selection, compression options, and metadata embedding. For the broader context of PDF OCR pipelines, see the [PDF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-pdfs/).

### Replacing AnalyzeDocument Block Graph Traversal with Structured Page Results

Textract's `AnalyzeDocument` with `TABLES` and `FORMS` returns a flat `List<Block>` where every element — lines, words, cells, table headers, form keys, form values — is the same `Block` type distinguished only by `BlockType` and linked by `RelationshipType.CHILD` ID arrays. Reconstructing a document's logical structure requires traversing this relationship graph. IronOCR returns a typed hierarchy: pages, paragraphs, lines, words, each with coordinate access.

**AWS Textract Approach:**

```csharp
// AnalyzeDocument with TABLES returns blocks that must be graph-traversed
// to determine which words belong to which paragraphs

using Amazon.Textract;
using Amazon.Textract.Model;

public async Task<List<DocumentSection>> ExtractDocumentStructureAsync(string imagePath)
{
    var bytes = File.ReadAllBytes(imagePath);

    var response = await _textractClient.AnalyzeDocumentAsync(
        new AnalyzeDocumentRequest
        {
            Document = new Document { Bytes = new MemoryStream(bytes) },
            FeatureTypes = new List<string> { "TABLES", "FORMS" }
            // Note: AnalyzeDocument (TABLES) costs $0.015/page — 10x DetectDocumentText
        });

    var sections = new List<DocumentSection>();

    // Filter LINE blocks for paragraph reconstruction
    // LINE blocks are not grouped into paragraphs — must infer from Y proximity
    var lineBlocks = response.Blocks
        .Where(b => b.BlockType == BlockType.LINE)
        .OrderBy(b => b.Geometry?.BoundingBox?.Top ?? 0)
        .ToList();

    // Group lines into pseudo-paragraphs by vertical gap heuristic
    var currentParagraph = new List<Block>();
    float? lastBottom = null;
    const float paragraphGapThreshold = 0.02f; // 2% of page height

    foreach (var line in lineBlocks)
    {
        var top = line.Geometry?.BoundingBox?.Top ?? 0;
        var height = line.Geometry?.BoundingBox?.Height ?? 0;

        if (lastBottom.HasValue && (top - lastBottom.Value) > paragraphGapThreshold)
        {
            if (currentParagraph.Any())
            {
                sections.Add(new DocumentSection
                {
                    Text = string.Join(" ", currentParagraph.Select(b => b.Text)),
                    LineCount = currentParagraph.Count,
                    // Confidence: average of all LINE block confidence values
                    Confidence = (float)currentParagraph.Average(b => b.Confidence ?? 0)
                });
                currentParagraph.Clear();
            }
        }

        currentParagraph.Add(line);
        lastBottom = top + height;
    }

    if (currentParagraph.Any())
        sections.Add(new DocumentSection
        {
            Text = string.Join(" ", currentParagraph.Select(b => b.Text)),
            LineCount = currentParagraph.Count,
            Confidence = (float)currentParagraph.Average(b => b.Confidence ?? 0)
        });

    return sections;
}

public class DocumentSection
{
    public string Text { get; set; }
    public int LineCount { get; set; }
    public float Confidence { get; set; }
}
```

**IronOCR Approach:**

```csharp
// IronOCR returns a typed hierarchy — paragraphs are first-class objects
// No block graph, no heuristic gap detection, no confidence averaging

using IronOcr;

public List<DocumentSection> ExtractDocumentStructure(string imagePath)
{
    var ocr = new IronTesseract();
    var result = ocr.Read(imagePath);

    var sections = new List<DocumentSection>();

    foreach (var page in result.Pages)
    {
        foreach (var paragraph in page.Paragraphs)
        {
            sections.Add(new DocumentSection
            {
                Text = paragraph.Text,
                LineCount = paragraph.Lines.Length,
                // Coordinate access: exact pixel position on the page
                LocationX = paragraph.X,
                LocationY = paragraph.Y,
                Width = paragraph.Width,
                Height = paragraph.Height,
                Confidence = paragraph.Confidence
            });
        }
    }

    return sections;
}

public class DocumentSection
{
    public string Text { get; set; }
    public int LineCount { get; set; }
    public int LocationX { get; set; }
    public int LocationY { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double Confidence { get; set; }
}
```

Paragraphs, lines, and words are directly accessible as typed collections with `.X`, `.Y`, `.Width`, `.Height`, and `.Confidence` properties. No BoundingBox normalization, no gap threshold heuristics, no block type filtering. The [read results guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) documents the full `OcrResult` hierarchy including character-level coordinate access. For invoice and receipt workflows that depend on this structure, see the [invoice OCR tutorial](https://ironsoftware.com/csharp/ocr/blog/using-ironocr/invoice-ocr-csharp-tutorial/).

### Replacing Rate-Limited Batch Processing with Unconstrained Parallel Execution

Batch image processing is where Textract's TPS limits produce the most visible operational cost. The `DetectDocumentText` synchronous API is rate-limited; sending more than 5 requests per second produces `ProvisionedThroughputExceededException`. Correct batch handling requires `SemaphoreSlim` throttling, retry logic with backoff, and careful accounting for in-flight requests. IronOCR has no rate limit — throughput is bounded only by available CPU cores.

**AWS Textract Approach:**

```csharp
// Batch processing requires SemaphoreSlim throttle + retry on rate limit exceeded

using Amazon.Textract;
using Amazon.Textract.Model;
using System.Collections.Concurrent;

public async Task<Dictionary<string, string>> ProcessImageBatchAsync(
    IReadOnlyList<string> imagePaths,
    IProgress<(int Completed, int Total)> progress = null)
{
    var results = new ConcurrentDictionary<string, string>();
    var semaphore = new SemaphoreSlim(5); // 5 concurrent — Textract default TPS limit
    var tasks = new List<Task>();
    int completed = 0;

    foreach (var path in imagePaths)
    {
        await semaphore.WaitAsync();

        tasks.Add(Task.Run(async () =>
        {
            try
            {
                string text = null;
                int retryCount = 0;

                while (text == null && retryCount < 3)
                {
                    try
                    {
                        var bytes = await File.ReadAllBytesAsync(path);
                        var response = await _textractClient.DetectDocumentTextAsync(
                            new DetectDocumentTextRequest
                            {
                                Document = new Document { Bytes = new MemoryStream(bytes) }
                            });

                        text = string.Join("\n", response.Blocks
                            .Where(b => b.BlockType == BlockType.LINE)
                            .Select(b => b.Text));
                    }
                    catch (AmazonTextractException ex)
                        when (ex.ErrorCode == "ProvisionedThroughputExceededException")
                    {
                        // Exponential backoff on rate limit — blocks the entire thread
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
                        retryCount++;
                    }
                }

                results[path] = text ?? string.Empty;
                var done = Interlocked.Increment(ref completed);
                progress?.Report((done, imagePaths.Count));
            }
            finally
            {
                semaphore.Release();
            }
        }));
    }

    await Task.WhenAll(tasks);
    return results.ToDictionary(x => x.Key, x => x.Value);
}
```

**IronOCR Approach:**

```csharp
// No rate limits, no semaphore, no retry logic — Parallel.ForEach saturates all cores

using IronOcr;
using System.Collections.Concurrent;

public Dictionary<string, string> ProcessImageBatch(
    IReadOnlyList<string> imagePaths,
    IProgress<(int Completed, int Total)> progress = null)
{
    var ocr = new IronTesseract();
    var results = new ConcurrentDictionary<string, string>();
    int completed = 0;

    Parallel.ForEach(imagePaths, path =>
    {
        var result = ocr.Read(path);
        results[path] = result.Text;

        var done = Interlocked.Increment(ref completed);
        progress?.Report((done, imagePaths.Count));
    });

    return results.ToDictionary(x => x.Key, x => x.Value);
}
```

The `SemaphoreSlim`, the retry loop, the exponential backoff, and the `ProvisionedThroughputExceededException` catch blocks are all removed. `IronTesseract` is thread-safe and a single instance shared across threads handles the full parallel load without locks. On a machine with 8 cores, this processes 8 documents simultaneously with no configuration; on 32 cores, 32 simultaneously. The [multithreading example](https://ironsoftware.com/csharp/ocr/examples/csharp-tesseract-multithreading-for-speed/) shows the complete pattern and the [speed optimization guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-fast-configuration/) covers engine configuration tuning for throughput-focused deployments.

### Replacing AnalyzeDocument Form Extraction with Region-Based CropRectangle OCR

Textract's `AnalyzeDocument` with `FORMS` returns `KEY_VALUE_SET` blocks linked by `RelationshipType.VALUE` relationships. Reconstructing form field key-value pairs requires filtering by `EntityTypes.Contains("KEY")`, following `VALUE` relationship IDs, then fetching child `WORD` blocks to assemble the text. For fixed-format forms where field positions are known, IronOCR's `CropRectangle` extracts each field directly without relationship graph traversal — and costs zero per page.

**AWS Textract Approach:**

```csharp
// FORMS mode costs $0.05/page — the most expensive Textract tier
// Relationship graph traversal required to reconstruct key-value pairs

using Amazon.Textract;
using Amazon.Textract.Model;

public async Task<Dictionary<string, string>> ExtractInvoiceFieldsAsync(string imagePath)
{
    var bytes = File.ReadAllBytes(imagePath);

    // $0.05/page for FORMS analysis
    var response = await _textractClient.AnalyzeDocumentAsync(
        new AnalyzeDocumentRequest
        {
            Document = new Document { Bytes = new MemoryStream(bytes) },
            FeatureTypes = new List<string> { "FORMS" }
        });

    var allBlocks = response.Blocks;
    var fields = new Dictionary<string, string>();

    // Find KEY blocks only
    var keyBlocks = allBlocks.Where(b =>
        b.BlockType == BlockType.KEY_VALUE_SET &&
        b.EntityTypes?.Contains("KEY") == true);

    foreach (var keyBlock in keyBlocks)
    {
        // Assemble key text from CHILD WORD blocks
        var keyText = GetTextFromBlock(allBlocks, keyBlock);

        // Find associated VALUE block via VALUE relationship
        var valueBlockId = keyBlock.Relationships?
            .FirstOrDefault(r => r.Type == RelationshipType.VALUE)?
            .Ids.FirstOrDefault();

        if (valueBlockId == null) continue;

        var valueBlock = allBlocks.FirstOrDefault(b => b.Id == valueBlockId);
        if (valueBlock == null) continue;

        var valueText = GetTextFromBlock(allBlocks, valueBlock);
        fields[keyText.TrimEnd(':')] = valueText;
    }

    return fields;
}

private string GetTextFromBlock(IList<Block> allBlocks, Block block)
{
    if (block.Relationships == null) return string.Empty;

    var childWordIds = block.Relationships
        .Where(r => r.Type == RelationshipType.CHILD)
        .SelectMany(r => r.Ids);

    return string.Join(" ", allBlocks
        .Where(b => b.BlockType == BlockType.WORD && childWordIds.Contains(b.Id))
        .Select(b => b.Text));
}
```

**IronOCR Approach:**

```csharp
// CropRectangle extracts each field zone directly — no relationship graph
// Works for any fixed-format form where field positions are known

using IronOcr;

// Define the field zones for your form template once
private static readonly Dictionary<string, CropRectangle> InvoiceFieldZones =
    new Dictionary<string, CropRectangle>
    {
        { "InvoiceNumber", new CropRectangle(450, 80,  300, 35) },
        { "InvoiceDate",   new CropRectangle(450, 120, 300, 35) },
        { "VendorName",    new CropRectangle(50,  160, 400, 35) },
        { "TotalAmount",   new CropRectangle(450, 680, 200, 35) },
        { "DueDate",       new CropRectangle(450, 720, 200, 35) }
    };

public Dictionary<string, string> ExtractInvoiceFields(string imagePath)
{
    var ocr = new IronTesseract();
    var fields = new Dictionary<string, string>();

    foreach (var zone in InvoiceFieldZones)
    {
        using var input = new OcrInput();
        input.LoadImage(imagePath, zone.Value); // load only the defined region

        var result = ocr.Read(input);
        fields[zone.Key] = result.Text.Trim();
    }

    return fields;
}
```

The `KEY_VALUE_SET` block filtering, the `RelationshipType.VALUE` traversal, and the `GetTextFromBlock` helper are eliminated. Each field reads exactly the pixel region that contains it — no more, no less. The [region-based OCR guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-region-of-an-image/) covers `CropRectangle` coordinates and how to define zones from template measurements. For invoice-specific workflows, the [invoice OCR blog post](https://ironsoftware.com/csharp/ocr/blog/using-ironocr/invoice-ocr-csharp-tutorial/) and [receipt scanning tutorial](https://ironsoftware.com/csharp/ocr/blog/using-ironocr/receipt-scanning-api-tutorial/) demonstrate full field extraction pipelines.

## AWS Textract API to IronOCR Mapping Reference

| AWS Textract | IronOCR Equivalent |
|---|---|
| `AmazonTextractClient` | `IronTesseract` |
| `AmazonS3Client` | Not required |
| `DetectDocumentTextRequest` | `OcrInput` with `input.LoadImage()` |
| `DetectDocumentTextResponse` | `OcrResult` |
| `AnalyzeDocumentRequest` (TABLES/FORMS) | `OcrInput` with optional `CropRectangle` |
| `StartDocumentTextDetectionRequest` | `OcrInput` with `input.LoadPdf()` — synchronous, no job start |
| `GetDocumentTextDetectionRequest` | Not applicable — results are immediate |
| `Document.Bytes` | `input.LoadImage(byteArray)` or `input.LoadImage(stream)` |
| `S3Object` (document staging) | File path or stream — no staging required |
| `Block` (`BlockType.LINE`) | `result.Lines` (typed collection) |
| `Block` (`BlockType.WORD`) | `result.Words` (typed collection) |
| `Block` (`BlockType.TABLE`) | `result.Words` grouped by coordinate proximity |
| `Block` (`BlockType.KEY_VALUE_SET`) | `CropRectangle` region extraction per field |
| `Block.Confidence` | `word.Confidence` / `result.Confidence` |
| `Block.Geometry.BoundingBox` | `word.X`, `word.Y`, `word.Width`, `word.Height` |
| `RelationshipType.CHILD` traversal | Direct property access on typed result objects |
| `JobStatus.IN_PROGRESS` | Not applicable — no async state |
| `JobStatus.SUCCEEDED` | Not applicable — synchronous return |
| `response.NextToken` (pagination) | Not applicable — results not paginated |
| `ProvisionedThroughputExceededException` | Not applicable — no TPS limits |
| `AccessDeniedException` | Not applicable — no credential chain |
| `input.LoadImageFrames()` | Multi-frame TIFF direct support (no Textract equivalent) |
| `result.SaveAsSearchablePdf()` | Searchable PDF output (no Textract equivalent) |

## Common Migration Issues and Solutions

### Issue 1: Credentials Misconfigured in New Deployment Environments

**AWS Textract:** The `AmazonTextractClient` constructor resolves credentials from a chain: environment variables, `~/.aws/credentials`, EC2 instance profile, ECS task role. In a new Docker container or CI environment, if none of these are configured, the client constructs without error but every API call throws `AmazonClientException: No credentials specified`. The failure is invisible until runtime.

**Solution:** Remove the credential chain entirely. Set the IronOCR license key from an environment variable that is simpler to configure and carries no IAM permissions to manage:

```csharp
// Single environment variable — no IAM, no rotation, no chain resolution
var key = Environment.GetEnvironmentVariable("IRONOCR_LICENSE");
if (string.IsNullOrEmpty(key))
    throw new InvalidOperationException("IRONOCR_LICENSE environment variable is not set");

IronOcr.License.LicenseKey = key;
```

### Issue 2: Async Call Sites Throughout the Codebase

**AWS Textract:** Because the entire SDK is async-only (`DetectDocumentTextAsync`, `StartDocumentTextDetectionAsync`, `GetDocumentTextDetectionAsync`), all Textract call sites propagate `async Task<T>` through the call stack. Migrating to IronOCR's synchronous API requires a decision at each site.

**Solution:** For background processing services and controller actions, IronOCR's synchronous calls are safe on background threads. Wrap in `Task.Run` only when the existing call chain must remain async:

```csharp
// If the call site must remain async (e.g., an ASP.NET controller action):
public async Task<IActionResult> ProcessUpload(IFormFile file)
{
    using var ms = new MemoryStream();
    await file.CopyToAsync(ms);

    // Offload synchronous OCR to thread pool — keeps controller non-blocking
    var text = await Task.Run(() =>
    {
        var ocr = new IronTesseract();
        using var input = new OcrInput();
        input.LoadImage(ms.ToArray());
        return ocr.Read(input).Text;
    });

    return Ok(text);
}
```

For batch processors and background services that are already running on non-UI threads, call `ocr.Read()` synchronously — `Task.Run` adds overhead without benefit in those contexts. The [async OCR guide](https://ironsoftware.com/csharp/ocr/how-to/async/) covers the recommended patterns.

### Issue 3: S3 Bucket Cleanup Logic Scattered Through Code

**AWS Textract:** Any code that uploads to S3 for Textract processing must also delete from S3 after the job completes. This cleanup frequently lives in `finally` blocks and is sometimes omitted on error paths, causing orphaned objects that accumulate storage costs. During migration, S3 cleanup code is easy to overlook because it is not conceptually related to OCR.

**Solution:** The entire S3 upload-and-cleanup pattern has no IronOCR equivalent. Delete all `PutObjectAsync`, `DeleteObjectAsync`, and S3-related `try/finally` blocks entirely. IronOCR reads from local paths or streams directly:

```csharp
// Before: upload → job → poll → extract → cleanup (50+ lines)
// After: two lines, no cleanup required

using var input = new OcrInput();
input.LoadPdf(localPdfPath);
var text = new IronTesseract().Read(input).Text;
```

Decommission the S3 staging bucket after migration. The [PDF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-pdfs/) and [stream input guide](https://ironsoftware.com/csharp/ocr/how-to/input-streams/) cover every input pattern that replaces S3-staged document access.

### Issue 4: Block Graph Traversal Code Has No Direct Equivalent

**AWS Textract:** Code that traverses `Block.Relationships` to reconstruct table cells, form fields, or paragraph groupings from `RelationshipType.CHILD` ID arrays is the most time-consuming code to migrate. There is no one-to-one replacement because IronOCR returns typed collections instead of a relationship graph.

**Solution:** Replace each traversal pattern with the appropriate typed property. Relationship ID lookups become direct property access:

```csharp
// Before: filter by BlockType + traverse relationship IDs
var paragraphText = string.Join(" ", allBlocks
    .Where(b => b.BlockType == BlockType.WORD &&
                childIds.Contains(b.Id))
    .Select(b => b.Text));

// After: direct paragraph text (no filtering, no relationship lookup)
foreach (var page in result.Pages)
    foreach (var para in page.Paragraphs)
        Console.WriteLine(para.Text); // already assembled
```

For table reconstruction that depended on `BlockType.CELL` and `RowIndex`/`ColumnIndex` properties, use word coordinate grouping on `result.Words`. The [table reading guide](https://ironsoftware.com/csharp/ocr/how-to/read-table-in-document/) covers the coordinate-based approach.

### Issue 5: ProvisionedThroughputExceededException Catch Blocks

**AWS Textract:** Robust batch processing code catches `AmazonTextractException` with `ErrorCode == "ProvisionedThroughputExceededException"` and implements retry with backoff. This error code is a Textract-specific concept with no IronOCR equivalent.

**Solution:** Delete all `ProvisionedThroughputExceededException` catch blocks. IronOCR has no TPS limits. Replace the entire throttled batch pattern with `Parallel.ForEach`:

```csharp
// Delete: SemaphoreSlim, retry loops, ProvisionedThroughputExceededException handlers
// Replace with:
Parallel.ForEach(imagePaths, path =>
{
    var result = new IronTesseract().Read(path);
    results[path] = result.Text;
});
```

### Issue 6: Region Configuration Baked Into Client Constructors

**AWS Textract:** `new AmazonTextractClient(Amazon.RegionEndpoint.USEast1)` hardcodes a region. Multi-region deployments require multiple client instances or dynamic region selection. When Textract adds support in new regions, code must be updated.

**Solution:** IronOCR has no region concept. Remove all `Amazon.RegionEndpoint` references. The `IronTesseract` constructor takes no network configuration — processing is local. For multi-region deployments on AWS infrastructure where each region runs its own compute, each IronOCR instance processes documents locally within that compute boundary with no cross-region calls.

## AWS Textract Migration Checklist

### Pre-Migration Tasks

Audit all Textract and S3 SDK usage in the codebase:

```bash
grep -rn "Amazon.Textract\|AmazonTextractClient\|DetectDocumentText" --include="*.cs" .
grep -rn "StartDocumentTextDetection\|GetDocumentTextDetection\|JobStatus" --include="*.cs" .
grep -rn "AmazonS3Client\|PutObjectRequest\|DeleteObjectAsync" --include="*.cs" .
grep -rn "BlockType\|RelationshipType\|KEY_VALUE_SET" --include="*.cs" .
grep -rn "ProvisionedThroughputExceededException\|AmazonTextractException" --include="*.cs" .
grep -rn "AWSSDK\|Amazon.RegionEndpoint" --include="*.csproj" .
```

Inventory the following before writing any replacement code:

- Count all files containing `AmazonTextractClient` — each is a replacement target
- List all `AnalyzeDocument` call sites — note whether `TABLES`, `FORMS`, or both are used
- Identify all async polling loops (`do { await Task.Delay } while JobStatus == IN_PROGRESS`)
- Find all S3 cleanup `finally` blocks — these are deleted wholesale, not replaced
- Count all `ProvisionedThroughputExceededException` catch blocks — deleted, not replaced
- Note all `BlockType.TABLE`, `BlockType.CELL`, `BlockType.KEY_VALUE_SET` traversal logic — each needs structured output replacement

### Code Update Tasks

1. Remove `AWSSDK.Textract`, `AWSSDK.S3`, and `AWSSDK.Core` from all `.csproj` files
2. Run `dotnet add package IronOcr` in each project that performs OCR
3. Add `IronOcr.License.LicenseKey = ...` once at application startup (`Program.cs` or equivalent)
4. Remove all `using Amazon.Textract;`, `using Amazon.Textract.Model;`, `using Amazon.S3;`, `using Amazon.Runtime;` statements
5. Add `using IronOcr;` to each file that previously imported Textract namespaces
6. Replace `AmazonTextractClient` constructor calls with `new IronTesseract()`
7. Replace `AmazonS3Client` instantiation and all `PutObjectAsync` / `DeleteObjectAsync` calls with direct file path or stream reads
8. Replace all `DetectDocumentTextAsync` calls: `var text = ocr.Read(path).Text`
9. Replace all `StartDocumentTextDetectionAsync` + polling loops with `input.LoadPdf(path)` + `ocr.Read(input)`
10. Replace all multi-frame TIFF pipelines (TIFF → PDF → S3 → async) with `input.LoadImageFrames(tiffPath)`
11. Replace `BlockType.LINE` / `BlockType.WORD` filter chains with `result.Lines` / `result.Words`
12. Replace `BlockType.KEY_VALUE_SET` relationship traversal with `CropRectangle` zone extraction
13. Delete all `ProvisionedThroughputExceededException` catch blocks and `SemaphoreSlim` throttles
14. Replace throttled batch patterns with `Parallel.ForEach` using a shared `IronTesseract` instance
15. Remove all AWS credential environment variable configuration from deployment manifests and CI pipelines
16. Decommission S3 staging buckets after all processing code is migrated and verified

### Post-Migration Testing

- Verify the application starts cleanly with only `IRONOCR_LICENSE` set and all AWS environment variables removed
- Run OCR on the same sample images previously processed by Textract and compare extracted text for accuracy
- Process a multi-page PDF directly and verify all pages are extracted without S3 or async polling
- Process a multi-frame TIFF and verify page count matches Textract's output for the same document
- Test batch processing with a set of 50+ images and confirm no `ProvisionedThroughputExceededException` equivalent occurs
- Run the application in a Docker container with no outbound internet access and confirm OCR completes successfully
- Verify searchable PDF output opens correctly in Adobe Reader and is full-text searchable
- Confirm that removing `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` from the deployment environment does not break anything
- Test any form or invoice extraction workflows against known output from Textract to validate field accuracy
- Measure processing latency for a representative document set and confirm elimination of the minimum 5-second polling wait

## Key Benefits of Migrating to IronOCR

**Infrastructure Consolidation to a Single NuGet Package.** Three AWS SDK packages, an S3 bucket with lifecycle policies, IAM roles across every deployment environment, and AWS credential rotation procedures are replaced by one NuGet package. The `.csproj` change is `dotnet remove package AWSSDK.Textract` followed by `dotnet add package IronOcr`. Every downstream consequence of AWS credential management — security audits, rotation schedules, environment variable hygiene, Docker secrets configuration — disappears with it.

**Predictable Total Cost of Ownership.** The per-page billing model produces variable costs that scale with document volume indefinitely. After migrating to IronOCR, the cost per additional page processed is zero regardless of volume. A team processing 200,000 pages per month at the Textract `AnalyzeDocument` rate for tables spends $36,000 per year on OCR alone. The IronOCR Enterprise license at $2,999 recovers that cost in less than 31 days of avoided billing. See the [IronOCR licensing page](https://ironsoftware.com/csharp/ocr/licensing/) for full tier details and SaaS redistribution terms.

**Synchronous Processing Eliminates Five-Phase Async Complexity.** The S3 upload, job start, polling loop, paginated result collection, and cleanup finally block represent five independent failure modes in the Textract PDF pipeline. After migration, a PDF reads in two lines. Error handling reduces to a single try/catch around the `ocr.Read()` call. The polling timeout logic, the `do/while JobStatus == IN_PROGRESS` loop, the `nextToken` pagination accumulator — none of these concepts exist in the IronOCR codebase. Maintenance burden drops in proportion to the code that was removed.

**Complete Deployment Flexibility.** Textract requires internet access on every OCR call. IronOCR requires internet access only to download the NuGet package at install time. After that, it operates in air-gapped networks, Docker containers with no outbound rules, on-premise servers, and AWS EC2 instances without Textract access. The same binary that runs in production on Windows Server also runs in a Linux container. The [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/) and [Linux deployment guide](https://ironsoftware.com/csharp/ocr/get-started/linux/) cover the specific configuration for containerized environments that were previously blocked from using Textract.

**Data Sovereignty Achieved by Architecture, Not Configuration.** IronOCR has no network transmission mode to disable — local processing is the only mode. Documents processed through IronOCR never leave the host machine. For healthcare applications processing PHI, defense applications processing CUI, and financial applications processing payment card images, this is a compliance posture that requires no BAA negotiation, no regional data residency configuration, and no audit of Amazon's data handling policies. The compliance scope is your own infrastructure boundary, which you already control. The [IronOCR product page](https://ironsoftware.com/csharp/ocr/) provides the full feature summary and deployment documentation for teams completing a compliance review.

**Configurable Preprocessing for Document Quality Control.** Textract's internal preprocessing is not accessible or configurable. When a scanned document produces poor results, the only recourse is to accept the output or rescan. IronOCR exposes the full preprocessing pipeline: `Deskew()`, `DeNoise()`, `Contrast()`, `Binarize()`, `Sharpen()`, `Scale()`, `Dilate()`, `Erode()`, and `DeepCleanBackgroundNoise()`. Teams that moved documents from Textract because of low-confidence results on difficult scans can address the root cause directly with filters matched to the specific defect — rotation, noise, low contrast, or insufficient resolution. The [preprocessing features page](https://ironsoftware.com/csharp/ocr/features/preprocessing/) and [image quality correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/) document the available filters and their appropriate use cases.
