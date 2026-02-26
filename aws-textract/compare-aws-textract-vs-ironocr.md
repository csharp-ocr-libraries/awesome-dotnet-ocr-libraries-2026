At $0.0015 per page for basic text detection, AWS Textract looks cheap until the invoice arrives: 10,000 pages per month costs $180 per year, 100,000 pages per month costs $1,800 per year, and those charges never stop. Every document your application processes leaves your network, travels to an Amazon data center, gets processed by Amazon infrastructure, and the bill compounds indefinitely. For teams evaluating OCR options in .NET, the question is not just whether Textract produces accurate results — it does — but whether the per-page cost model, mandatory cloud transmission, and async polling architecture for multi-page documents match what your application actually needs.

## Understanding AWS Textract

AWS Textract is Amazon's managed document analysis service, accessible via the AWS SDK for .NET through the `AWSSDK.Textract` NuGet package. It operates as a cloud API: your application sends document data to Amazon's infrastructure and receives structured results. The service requires an AWS account, IAM credentials with Textract permissions, and an internet connection for every single OCR operation.

Textract exposes several distinct analysis modes, each priced separately:

- **DetectDocumentText:** Basic text extraction at $0.0015 per page (first 1 million pages per month)
- **AnalyzeDocument (Tables):** Structured table extraction at $0.015 per page — ten times the base rate
- **AnalyzeDocument (Forms):** Key-value form extraction at $0.05 per page — over thirty times the base rate
- **AnalyzeExpense:** Invoice and receipt parsing at $0.01 per page
- **AnalyzeID:** Identity document extraction at $0.025 per page
- **StartDocumentTextDetection / StartDocumentAnalysis:** Asynchronous API required for any multi-page PDF, mandating an S3 staging bucket, job polling, and result pagination

The result model uses a flat list of `Block` objects with relationship IDs that must be traversed to reconstruct tables, forms, or any structured output. A simple table extraction requires iterating `BlockType.TABLE` blocks, finding child `BlockType.CELL` blocks via `RelationshipType.CHILD` relationship IDs, then fetching `BlockType.WORD` blocks for each cell's text. This relationship graph model handles complex document structures, but it is not lightweight.

### The S3-Async Pipeline

Single-image OCR via `DetectDocumentTextAsync` can pass document bytes directly in the request. Multi-page PDFs cannot. Any PDF requires the full asynchronous pipeline:

```csharp
// AWS Textract: Multi-page PDF requires S3 + async job
public async Task<string> ProcessPdfAsync(string pdfPath)
{
    // Step 1: Upload to S3 — credentials for two services required
    var key = $"uploads/{Guid.NewGuid()}.pdf";
    using (var fileStream = File.OpenRead(pdfPath))
    {
        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = fileStream
        });
    }

    try
    {
        // Step 2: Start async Textract job
        var startResponse = await _textractClient.StartDocumentTextDetectionAsync(
            new StartDocumentTextDetectionRequest
            {
                DocumentLocation = new DocumentLocation
                {
                    S3Object = new S3Object { Bucket = _bucketName, Name = key }
                }
            });

        var jobId = startResponse.JobId;

        // Step 3: Poll every 5 seconds until complete
        GetDocumentTextDetectionResponse getResponse;
        do
        {
            await Task.Delay(5000);
            getResponse = await _textractClient.GetDocumentTextDetectionAsync(
                new GetDocumentTextDetectionRequest { JobId = jobId });
        } while (getResponse.JobStatus == JobStatus.IN_PROGRESS);

        if (getResponse.JobStatus != JobStatus.SUCCEEDED)
            throw new Exception($"Textract job failed: {getResponse.StatusMessage}");

        // Step 4: Paginate through result blocks
        var allText = new StringBuilder();
        string nextToken = null;
        do
        {
            var pageResponse = await _textractClient.GetDocumentTextDetectionAsync(
                new GetDocumentTextDetectionRequest
                {
                    JobId = jobId,
                    NextToken = nextToken
                });

            foreach (var block in pageResponse.Blocks.Where(b => b.BlockType == BlockType.LINE))
                allText.AppendLine(block.Text);

            nextToken = pageResponse.NextToken;
        } while (nextToken != null);

        return allText.ToString();
    }
    finally
    {
        // Step 5: Always clean up S3
        await _s3Client.DeleteObjectAsync(_bucketName, key);
    }
}
```

This is the minimum viable implementation for reliable PDF processing — five distinct phases, two AWS service clients, and cleanup logic in a `finally` block. The complete production version with proper error handling, rate limit retry logic, and timeout management runs 150-300 lines.

## Understanding IronOCR

[IronOCR](https://ironsoftware.com/csharp/ocr/) is a commercial .NET OCR library that runs entirely on your infrastructure. It wraps an optimized Tesseract 5 engine with automatic image preprocessing, native PDF support, and a synchronous API that produces results directly without external service calls or staging steps.

Key characteristics of the IronOCR architecture:

- **Local processing only:** No document data leaves the machine running your application
- **Single NuGet package:** `dotnet add package IronOcr` installs everything including native binaries
- **Automatic preprocessing:** Deskew, denoise, contrast enhancement, binarization, and resolution scaling happen automatically on poor-quality inputs
- **Native PDF support:** Reads PDFs directly via file path or stream without S3 staging or async jobs
- **Thread-safe:** A single `IronTesseract` instance handles concurrent requests across threads without contention
- **Perpetual licensing:** $749 Lite / $1,499 Plus / $2,999 Professional / $5,999 Unlimited — one payment, no per-page charges, no usage metering
- **125+ language packs:** Installed as separate NuGet packages, loaded locally, no network calls

## Feature Comparison

| Feature | AWS Textract | IronOCR |
|---|---|---|
| **Processing location** | Amazon cloud (mandatory) | Local / on-premise |
| **Multi-page PDF** | Requires S3 + async job | Direct synchronous call |
| **Cost model** | $0.0015–$0.065 per page | Perpetual license, no per-page fee |
| **Internet required** | Always | Never |
| **Credential setup** | IAM user/role + optional S3 | Single license key string |
| **Air-gapped deployment** | Not possible | Fully supported |
| **Encrypted PDF support** | Not supported | Built-in (password parameter) |

### Detailed Feature Comparison

| Feature | AWS Textract | IronOCR |
|---|---|---|
| **Text Extraction** | | |
| Basic OCR (images) | Yes — `DetectDocumentTextAsync` | Yes — `ocr.Read(path)` |
| Multi-page PDF | Requires S3 + async polling | Direct `input.LoadPdf(path)` |
| Password-protected PDF | Not supported | `input.LoadPdf(path, Password: "x")` |
| Stream input | Yes (byte array in request) | Yes — `input.LoadImage(stream)` |
| **Structured Extraction** | | |
| Table extraction | `AnalyzeDocument` + block graph traversal | Word position-based reconstruction |
| Form field extraction | `AnalyzeDocument` + KEY_VALUE_SET blocks | Region-based `CropRectangle` zones |
| Line-level results | `Block` filtering by `BlockType.LINE` | `result.Lines` direct collection |
| Word-level with coordinates | `Block` filtering by `BlockType.WORD` | `result.Words` with `.X`, `.Y`, `.Width` |
| Confidence scores | Per-block confidence | Per-word and overall `result.Confidence` |
| **Processing Model** | | |
| Synchronous (images) | Yes (single page only) | Yes (all document types) |
| Asynchronous | Required for PDFs | Optional — `Task.Run()` wrapper |
| Batch processing | Requires rate limit management (5 TPS default) | Unconstrained `Parallel.ForEach` |
| **Preprocessing** | | |
| Auto deskew | Not exposed | `input.Deskew()` |
| Noise removal | Internal (not configurable) | `input.DeNoise()` |
| Contrast enhancement | Internal (not configurable) | `input.Contrast()` |
| Resolution enhancement | Internal (not configurable) | `input.EnhanceResolution(300)` |
| Binarization | Internal | `input.Binarize()` |
| **Output Formats** | | |
| Plain text | Yes | Yes |
| Searchable PDF | No | `result.SaveAsSearchablePdf(path)` |
| hOCR | No | `result.SaveAsHocrFile(path)` |
| Structured JSON | Via block serialization | `result.Words` / `result.Lines` |
| **Deployment** | | |
| On-premise | No | Yes |
| Air-gapped | No | Yes |
| Docker | Yes (with AWS credentials injected) | Yes (no credentials required) |
| AWS Lambda | Native | Supported |
| Azure | Yes | Yes |
| Linux | Yes (AWS-managed) | Yes — `get-started/linux/` |
| **Compliance** | | |
| HIPAA | Requires BAA with AWS | No external processor |
| GDPR | Data crosses to AWS regions | Data stays in-boundary |
| ITAR | Prohibited without special authorization | Fully on-premise |
| Air-gapped / CMMC Level 3 | Not possible | Supported |

## Cost at Scale

The per-page pricing model is the defining structural constraint of AWS Textract. $0.0015 per page sounds trivial in isolation. Across a real document workflow, it is not.

### AWS Textract Approach

```csharp
// Every call to this method costs money — per page, permanently
public async Task<string> DetectTextAsync(string imagePath)
{
    var imageBytes = File.ReadAllBytes(imagePath);  // Image leaves your network

    var request = new DetectDocumentTextRequest
    {
        Document = new Document
        {
            Bytes = new MemoryStream(imageBytes)
        }
    };

    var response = await _client.DetectDocumentTextAsync(request);  // $0.0015

    return string.Join("\n", response.Blocks
        .Where(b => b.BlockType == BlockType.LINE)
        .Select(b => b.Text));
}
```

Basic text detection at $0.0015 per page represents the floor. If your workflow uses `AnalyzeDocument` with table extraction, the rate rises to $0.015 per page — a 10x multiplier. Forms extraction adds another $0.05 per page. A document containing tables and form fields costs $0.065 per page, which puts 100,000 pages per month at $6,500 — $78,000 per year — with no upper bound and no way to pay ahead.

Three-year total cost at 100,000 pages per month with table extraction: $234,000, and the meter is still running.

### IronOCR Approach

```csharp
// One license. No per-page cost. Same code handles 1 page or 1,000,000.
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

var text = new IronTesseract().Read("document.jpg").Text;
```

The $2,999 Professional license covers 10 developers, unlimited projects, and unlimited page volume. After year one, the ongoing cost for pages processed is zero. For a team processing 50,000 pages per month at the basic Textract rate, the IronOCR license pays for itself in 40 days.

The [IronOCR licensing page](https://ironsoftware.com/csharp/ocr/licensing/) covers tier details, SaaS subscription options for usage-based billing scenarios, and OEM redistribution terms.

## Data Sovereignty and Compliance

AWS Textract's architecture makes one guarantee impossible: that your documents stay within your infrastructure. Every OCR operation transmits document content to Amazon's servers.

### AWS Textract Approach

```csharp
// This code sends PHI, legal documents, financial records — whatever is in
// the file — to Amazon Web Services infrastructure
public async Task<string> ProcessSensitiveDocumentAsync(string documentPath)
{
    var imageBytes = File.ReadAllBytes(documentPath);

    // Data crosses your security perimeter here
    var request = new DetectDocumentTextRequest
    {
        Document = new Document
        {
            Bytes = new MemoryStream(imageBytes)
        }
    };

    // Amazon processes it; you receive text back
    var response = await _client.DetectDocumentTextAsync(request);

    return string.Join("\n", response.Blocks
        .Where(b => b.BlockType == BlockType.LINE)
        .Select(b => b.Text));
}
```

AWS offers a HIPAA Business Associate Agreement for covered entities, and GovCloud regions provide FedRAMP High authorization. These frameworks do not change the fundamental architecture: documents leave your infrastructure for every operation. For ITAR-controlled technical data, this is not a compliance nuance — it is a prohibition. For CMMC Level 3 workloads with CUI, cloud transmission requires specific authorizations most defense contractors do not hold. For air-gapped systems — research networks, industrial control environments, classified facilities — Textract is simply unavailable.

AWS Textract is available in six regions: `us-east-1`, `us-west-2`, `eu-west-1`, `eu-west-2`, `ap-southeast-1`, and `ap-southeast-2`. Organizations with data residency requirements outside these regions have no compliant option.

### IronOCR Approach

```csharp
// IronOCR: document bytes never leave this process
public string ProcessSensitiveDocument(string documentPath)
{
    // Processes entirely on local hardware — no network call
    var ocr = new IronTesseract();
    return ocr.Read(documentPath).Text;
}
```

Because IronOCR executes locally, it fits naturally into healthcare workflows processing PHI, legal document systems handling privileged communications, financial applications handling payment card images, and defense contractor pipelines processing CUI. There is no external processor to audit, no BAA to negotiate, no data residency constraint to satisfy. The compliance scope is your organization's own infrastructure.

For teams deploying on AWS infrastructure but needing local processing, IronOCR [runs on AWS EC2 and Lambda](https://ironsoftware.com/csharp/ocr/get-started/aws/) without any dependency on Textract — the processing happens within your own AWS account boundary rather than Amazon's managed service.

## Async Polling vs. Synchronous Processing

The architectural split between Textract's synchronous (single-image) and asynchronous (multi-page PDF) APIs is not a minor API detail. It shapes how services are built, how errors are handled, and how much code maintainers must read and reason about.

### AWS Textract Approach

```csharp
// Full production-grade async processor for Textract PDF handling
public class TextractAsyncProcessor
{
    private readonly AmazonTextractClient _textractClient;
    private readonly AmazonS3Client _s3Client;
    private readonly string _bucketName;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _maxWaitTime = TimeSpan.FromMinutes(10);

    public async Task<DocumentResult> ProcessDocumentAsync(
        string localFilePath,
        CancellationToken cancellationToken = default)
    {
        var s3Key = $"textract-uploads/{Guid.NewGuid()}{Path.GetExtension(localFilePath)}";

        try
        {
            // Phase 1: Upload to S3
            await UploadToS3Async(localFilePath, s3Key, cancellationToken);

            // Phase 2: Start Textract job
            var jobId = await StartTextractJobAsync(s3Key, cancellationToken);

            // Phase 3: Poll until complete (up to 10 minutes)
            var pollResult = await PollForCompletionAsync(jobId, cancellationToken);

            if (!pollResult.Success)
                throw new Exception($"Textract job failed: {pollResult.ErrorMessage}");

            // Phase 4: Retrieve paginated results
            return await GetAllResultsAsync(jobId, cancellationToken);
        }
        finally
        {
            // Phase 5: S3 cleanup — must succeed or storage costs accumulate
            await DeleteFromS3Async(s3Key, cancellationToken);
        }
    }

    private async Task<(bool Success, string ErrorMessage)> PollForCompletionAsync(
        string jobId, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        int pollCount = 0;

        while (DateTime.UtcNow - startTime < _maxWaitTime)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await _textractClient.GetDocumentTextDetectionAsync(
                new GetDocumentTextDetectionRequest { JobId = jobId }, cancellationToken);

            pollCount++;

            switch (response.JobStatus)
            {
                case JobStatus.SUCCEEDED: return (true, null);
                case JobStatus.FAILED: return (false, response.StatusMessage ?? "Unknown error");
                case JobStatus.IN_PROGRESS:
                    await Task.Delay(_pollInterval, cancellationToken);
                    break;
                default:
                    throw new Exception($"Unknown job status: {response.JobStatus}");
            }
        }

        return (false, "Job timed out");
    }
}
```

This is not boilerplate that can be generated and forgotten. When a Textract job fails mid-flight, the S3 cleanup must still run. When a job times out after 10 minutes, the caller needs a clean error. When the network drops during polling, the retry strategy must not create duplicate jobs. Each of these failure modes requires explicit handling — the structure shown above is the minimum responsible implementation.

Batch processing adds another layer: Textract's default `StartDocumentTextDetection` TPS limit is 5 requests per second. Processing 100 documents requires a `SemaphoreSlim` throttle, a rate-replenishment timer, and retry logic for `ProvisionedThroughputExceededException`.

### IronOCR Approach

```csharp
// IronOCR: same synchronous API regardless of document type or size
public string ProcessDocument(string filePath)
{
    using var input = new OcrInput();

    if (Path.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        input.LoadPdf(filePath);
    else
        input.LoadImage(filePath);

    return new IronTesseract().Read(input).Text;
}
```

There is no polling loop, no job ID tracking, no S3 bucket, no result pagination. The same code handles a single JPEG and a 200-page PDF. Processing completes or throws — no intermediate "in progress" state to manage. For batch processing, [IronOCR is thread-safe](https://ironsoftware.com/csharp/ocr/examples/csharp-tesseract-multithreading-for-speed/) and a single `IronTesseract` instance handles `Parallel.ForEach` without locks or semaphores.

The [IronTesseract setup guide](https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/) covers configuration, and the [PDF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-pdfs/) documents page range selection, password-protected PDFs, and stream-based input for PDFs retrieved from databases or HTTP responses.

## Credential Management Overhead

Starting an OCR operation with AWS Textract involves IAM configuration before a single page is processed.

### AWS Textract Approach

Before calling `DetectDocumentTextAsync`, a developer must:

1. Create an AWS account or obtain access to an existing one
2. Create an IAM user or role with `textract:DetectDocumentText` and `textract:AnalyzeDocument` permissions
3. Generate and securely store access key ID and secret access key
4. Configure credential resolution — environment variables, AWS credentials file, or EC2 instance profile
5. If processing PDFs: create an S3 bucket, configure bucket policy, add `s3:PutObject` and `s3:DeleteObject` permissions
6. Implement credential rotation policies to meet security standards
7. Store credentials securely in each deployment environment — Docker secrets, Kubernetes secrets, AWS Secrets Manager, or CI/CD pipeline variables

```csharp
// Every environment needs these configured before this constructor succeeds
public TextractOcrService()
{
    // Reads credentials from environment, ~/.aws/credentials, or IAM role
    _client = new AmazonTextractClient(Amazon.RegionEndpoint.USEast1);
}
```

When credentials expire, rotate, or are misconfigured, every OCR call fails with `AmazonTextractException` carrying `ErrorCode == "AccessDeniedException"`. In a production system, this means implementing specific catch blocks for credential failures and monitoring for IAM policy drift.

### IronOCR Approach

```csharp
// One-time setup at application startup
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

// Or from environment — recommended for deployments
IronOcr.License.LicenseKey = Environment.GetEnvironmentVariable("IRONOCR_LICENSE");
```

The license key is a static string. It does not expire mid-operation, does not require rotation, and carries no permissions to manage. A Docker container that processes documents does not need injected AWS credentials, an IAM role bound to an execution context, or network access to AWS STS for token refresh.

The complete credential overhead reduction when moving from Textract to IronOCR: three NuGet packages removed (`AWSSDK.Textract`, `AWSSDK.S3`, `AWSSDK.Core`), all `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY` / `AWS_DEFAULT_REGION` environment variables removed, and IAM roles and S3 bucket configurations decommissioned. The [image input guide](https://ironsoftware.com/csharp/ocr/how-to/input-images/) and [stream input guide](https://ironsoftware.com/csharp/ocr/how-to/input-streams/) cover the full range of input methods that replace Textract's byte-array and S3-object document models.

## API Mapping Reference

| AWS Textract API | IronOCR Equivalent |
|---|---|
| `AmazonTextractClient` | `IronTesseract` |
| `AmazonS3Client` | Not required |
| `DetectDocumentTextRequest` | `OcrInput` |
| `DetectDocumentTextResponse` | `OcrResult` |
| `AnalyzeDocumentRequest` | `OcrInput` with `CropRectangle` for zones |
| `StartDocumentTextDetectionRequest` | `OcrInput` — synchronous, no start needed |
| `GetDocumentTextDetectionRequest` | Not required — results immediate |
| `Document.Bytes` | `input.LoadImage(bytes)` or `input.LoadImage(stream)` |
| `S3Object` (document staging) | File path string or stream |
| `Block` (`BlockType.LINE`) | `result.Lines` |
| `Block` (`BlockType.WORD`) | `result.Words` |
| `Block` (`BlockType.TABLE`) | Word position grouping via `result.Words` |
| `Block` (`BlockType.KEY_VALUE_SET`) | `CropRectangle` region extraction |
| `Block.Confidence` | `word.Confidence` / `result.Confidence` |
| `JobStatus.SUCCEEDED` | Not applicable — synchronous return |
| `JobStatus.IN_PROGRESS` | Not applicable — no async state |
| `response.NextToken` (pagination) | Not applicable — results not paginated |
| `ProvisionedThroughputExceededException` | Not applicable — no TPS limits |
| `client.DetectDocumentTextAsync(request)` | `ocr.Read(path)` |
| `client.AnalyzeDocumentAsync(request)` | `ocr.Read(input)` |
| `client.StartDocumentTextDetectionAsync(request)` | `ocr.Read(input)` |
| `client.GetDocumentTextDetectionAsync(request)` | Not applicable |

## When Teams Consider Moving from AWS Textract to IronOCR

### When the Monthly Bill Becomes a Budget Line Item

Teams that started with Textract at low volume often encounter a specific moment: the AWS bill for OCR processing appears in a quarterly budget review and someone asks whether this cost is fixed. It is not. At 100,000 pages per month with table extraction, the annual Textract cost is $18,000 for basic OCR or up to $78,000 with `AnalyzeDocument`. The IronOCR Professional license at $2,999 one-time recovers that cost in weeks. Teams processing 50,000 pages per month at the basic Textract rate hit the break-even point against IronOCR's entry-level Lite license in under two months.

### When a Compliance Requirement Blocks Cloud Processing

Healthcare organizations implementing document digitization workflows frequently discover mid-project that HIPAA PHI cannot flow through cloud services without a BAA and additional legal review, or that their security team prohibits cloud transmission entirely. Defense contractors handling technical drawings, specifications, or any CUI face ITAR and CMMC constraints that exclude AWS Textract from consideration. Legal firms processing privileged communications have similar concerns. These are not theoretical compliance edge cases — they appear regularly in procurement reviews, security audits, and contract negotiations. IronOCR processes locally, so the compliance question for document data reduces to whether your own infrastructure is in scope, not whether Amazon's infrastructure is in scope.

### When the Async PDF Complexity Exceeds Its Value

The five-phase S3-async pipeline — upload, start job, poll, paginate results, clean up — is not technically difficult to implement. It is difficult to maintain, test, and operate. Every phase is a failure point. S3 upload failures require retry logic. Textract job failures require distinguishing transient from permanent errors. Polling timeouts require timeout handling separate from cancellation. Result pagination requires accumulating state across multiple API calls. S3 cleanup failures require alerting because orphaned objects accumulate costs. Teams that have shipped this pipeline into production typically spend more ongoing engineering time maintaining it than they spent building it. The IronOCR equivalent — `input.LoadPdf(path)` followed by `ocr.Read(input)` — eliminates all five phases and their associated failure modes.

### When Deployment Environments Lack Internet Access

Docker containers running in isolated network segments, on-premise servers without outbound internet, air-gapped research environments, and industrial systems with strict network controls all share one characteristic: AWS Textract is not available. IronOCR installs as a standard NuGet package and operates without any network calls after installation. Teams running .NET applications in these environments have no Textract option and need a library that processes locally. The [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/) and [Linux deployment guide](https://ironsoftware.com/csharp/ocr/get-started/linux/) cover the specific configuration for containerized environments.

### When Rate Limit Throttling Disrupts Batch Workflows

The default `StartDocumentTextDetection` TPS limit is 5 requests per second. `DetectDocumentText` synchronous calls are also rate-limited. Batch jobs processing hundreds or thousands of documents must implement `SemaphoreSlim` throttling, exponential backoff on `ProvisionedThroughputExceededException`, and rate-replenishment timers. AWS supports TPS limit increase requests, but they require justification, review, and are not guaranteed. IronOCR processes as fast as local CPU allows — a 32-core server processes 32 documents concurrently without throttle configuration or service tier negotiation.

## Common Migration Considerations

### Replacing the Block Graph with Direct Collections

Textract represents all results as a flat `List<Block>` where lines, words, cells, tables, and key-value pairs are distinguished by `BlockType` and linked by relationship ID arrays. IronOCR provides direct typed collections.

```csharp
// Textract: filter flat block list by type
var lines = response.Blocks.Where(b => b.BlockType == BlockType.LINE);
var words = response.Blocks.Where(b => b.BlockType == BlockType.WORD);

// IronOCR: direct access to typed collections
var result = ocr.Read(imagePath);
var lines = result.Lines;   // IEnumerable<OcrResult.OcrResultLine>
var words = result.Words;   // IEnumerable<OcrResult.OcrResultWord>
foreach (var word in result.Words)
    Console.WriteLine($"'{word.Text}' at ({word.X},{word.Y}) confidence {word.Confidence}%");
```

The [structured results guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) covers `result.Pages`, `result.Paragraphs`, `result.Lines`, `result.Words`, and coordinate access for building layout-aware document processing.

### Replacing S3-Staged PDF Processing with Direct LoadPdf

Any Textract code that uploads to S3 before starting a detection job can be replaced with a direct PDF load. No staging bucket, no upload timing, no cleanup logic.

```csharp
// Textract: upload to S3 → start job → poll → paginate → cleanup (50+ lines)
// IronOCR equivalent:
public string ProcessPdf(string pdfPath)
{
    var ocr = new IronTesseract();
    using var input = new OcrInput();
    input.LoadPdf(pdfPath);
    return ocr.Read(input).Text;
}

// Specific page ranges (no Textract equivalent without async job per range)
public string ProcessPdfPages(string pdfPath, int startPage, int endPage)
{
    var ocr = new IronTesseract();
    using var input = new OcrInput();
    input.LoadPdfPages(pdfPath, startPage, endPage);
    return ocr.Read(input).Text;
}
```

### Adding Preprocessing for Documents That Produced Low Confidence in Textract

Textract's preprocessing is internal and not configurable. When a scanned document produces poor results, the only options are retrying or accepting low-confidence output. IronOCR exposes the preprocessing pipeline directly.

```csharp
// For documents that returned low-confidence results from Textract
using var input = new OcrInput();
input.LoadImage("low-quality-scan.jpg");

input.Deskew();              // Fix rotation from scanner misalignment
input.DeNoise();             // Remove scanner noise artifacts
input.Contrast();            // Boost faint text
input.EnhanceResolution(300); // Scale to optimal OCR resolution

var result = new IronTesseract().Read(input);
Console.WriteLine($"Confidence: {result.Confidence}%");
```

The [image quality correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/) and [image filters tutorial](https://ironsoftware.com/csharp/ocr/tutorials/c-sharp-ocr-image-filters/) document the full preprocessing pipeline and combinations that work best for specific document types. For confidence score interpretation and per-element confidence access, the [confidence scores guide](https://ironsoftware.com/csharp/ocr/how-to/tesseract-result-confidence/) covers the `result.Confidence` property and per-word confidence values.

### Handling the Async-to-Synchronous Pattern Change

Existing Textract code is necessarily `async Task<T>` throughout because the SDK is async-only. IronOCR operations are synchronous. For application code that already has an async call chain, wrap the IronOCR call in `Task.Run` to keep the async boundary.

```csharp
// Preserves async call site for minimal refactoring
public async Task<string> ExtractTextAsync(string path)
{
    return await Task.Run(() => new IronTesseract().Read(path).Text);
}
```

This is a convenience wrapper, not a requirement. For server-side processing where the calling code is already on a background thread, the synchronous call is preferred directly.

## Additional IronOCR Capabilities

Beyond the comparison points above, IronOCR provides capabilities that have no AWS Textract equivalent:

- **[Barcode reading during OCR](https://ironsoftware.com/csharp/ocr/how-to/barcodes/):** Set `ocr.Configuration.ReadBarCodes = true` and barcodes in the document are extracted alongside text in one pass — no separate barcode scanning step
- **[Progress tracking for long jobs](https://ironsoftware.com/csharp/ocr/how-to/progress-tracking/):** Subscribe to progress events for multi-page processing without polling an external service
- **[Scanned document processing](https://ironsoftware.com/csharp/ocr/how-to/read-scanned-document/):** Optimized pipeline for typical office scanner output including duplex scans and mixed-orientation pages
- **[Multi-language simultaneous extraction](https://ironsoftware.com/csharp/ocr/languages/):** Combine language packs at read time — `OcrLanguage.French + OcrLanguage.German` — with no API tier change
- **[Passport and ID reading](https://ironsoftware.com/csharp/ocr/how-to/read-passport/):** Dedicated pipeline for machine-readable zones on identity documents, extracting structured fields without manual region definition

## .NET Compatibility and Future Readiness

IronOCR targets .NET 8 and .NET 9, with active compatibility for .NET Standard 2.0 projects and .NET Framework 4.6.2 through 4.8. The library ships native binaries for Windows x64, Windows x86, Linux x64, and macOS via a single NuGet package — no runtime identifier switching or platform-specific package references. AWS Textract's `AWSSDK.Textract` package supports the same modern .NET targets, but the deployment model carries the full AWS SDK dependency tree, IAM credential infrastructure, and the architectural constraints documented throughout this article. IronOCR maintains active development with regular releases tracking Tesseract 5 engine updates and .NET runtime advances, including compatibility with .NET 10 when released.

## Conclusion

AWS Textract and IronOCR solve the same problem — extracting text from documents in .NET applications — with fundamentally incompatible architectural assumptions. Textract assumes documents can leave your network, that cloud service costs scale linearly with volume, and that multi-page PDFs justify a five-phase async pipeline with S3 staging. IronOCR assumes documents stay where they are processed, that license costs should be decoupled from volume, and that PDF processing should require the same three lines of code as image processing.

The cost arithmetic is the clearest dividing line. At 10,000 pages per month, Textract's basic OCR costs $180 per year — less than IronOCR's cheapest license tier for that single year. At 50,000 pages per month, the annual Textract cost for basic OCR ($900) approaches IronOCR's Lite license ($749) and the crossover arrives quickly as volume grows. At 100,000 pages per month with table extraction, the three-year Textract cost ($234,000) dwarfs even the Unlimited IronOCR license ($5,999) by a factor of 40. The opening math holds: the per-page model adds up fast, and it never stops.

Data sovereignty is the second structural constraint. For healthcare, legal, financial, and government workloads, the question of where documents are processed is not a preference — it is a compliance requirement. IronOCR processes locally by design, not by configuration. There is no "local mode" to enable; local processing is the only mode. That makes the compliance answer simple: your documents stay in your infrastructure because there is nowhere else for them to go.

For teams evaluating OCR at genuine scale, or operating in environments where document data cannot leave internal infrastructure, IronOCR's documentation provides the complete API reference, deployment guides for Docker, AWS, Azure, and Linux, and tutorials covering the full range of OCR use cases from basic image reading to searchable PDF generation and multi-language extraction.
