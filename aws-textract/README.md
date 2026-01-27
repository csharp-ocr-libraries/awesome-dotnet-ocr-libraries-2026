# AWS Textract for .NET: Complete Developer Guide (2026)

*By [Jacob Mellor](https://ironsoftware.com/about-us/authors/jacobmellor/), CTO of Iron Software*

AWS Textract is Amazon's cloud-based document analysis service that extracts text, tables, and form data from images and PDFs. Like all cloud OCR services, Textract requires careful evaluation of data security, regulatory compliance, and operational considerations before adoption in enterprise environments. Having integrated OCR into hundreds of enterprise systems, I've seen both the strengths and significant limitations of cloud-dependent approaches.

## Table of Contents

1. [Critical Security Considerations](#critical-security-considerations)
2. [AWS Textract Overview](#aws-textract-overview)
3. [Security and Compliance Analysis](#security-and-compliance-analysis)
4. [AWS Textract Pricing](#aws-textract-pricing)
5. [Implementing AWS Textract in .NET](#implementing-aws-textract-in-net)
6. [On-Premise Alternative: IronOCR](#on-premise-alternative-ironocr)
7. [Migration Guide: AWS Textract to IronOCR](#migration-guide-aws-textract-to-ironocr)
8. [Performance Comparison](#performance-comparison)
9. [When to Use Each Option](#when-to-use-each-option)
10. [Code Examples](#code-examples)
11. [Additional Resources](#additional-resources)

---

## Critical Security Considerations

**AWS Textract sends all documents to Amazon's cloud for processing.**

This fundamental architecture decision has significant implications:

- Your documents travel to and are processed on AWS infrastructure
- Data crosses organizational security boundaries
- AWS personnel may have access for support/debugging
- You depend on AWS's security practices and compliance certifications
- Data residency controls are limited to AWS region selection

**Organizations handling the following should carefully evaluate alternatives:**

- Protected Health Information (PHI/ePHI)
- Personally Identifiable Information (PII)
- Financial records and payment data
- Legal documents with privilege concerns
- Government classified or controlled information
- Defense contractor ITAR/EAR controlled data
- Trade secrets and intellectual property

---

## AWS Textract Overview

### Core Capabilities

AWS Textract offers several analysis types:

| Feature | Description | Use Case |
|---------|-------------|----------|
| DetectDocumentText | Basic text extraction | Simple OCR needs |
| AnalyzeDocument | Tables and forms extraction | Structured documents |
| AnalyzeExpense | Invoice/receipt parsing | Accounts payable |
| AnalyzeID | ID document extraction | Identity verification |
| StartDocumentTextDetection | Async for large documents | Batch processing |

### Feature Deep Dive

**AnalyzeExpense:** Purpose-built for invoice and receipt processing. Extracts vendor name, invoice number, dates, line items, taxes, and totals. Works well for standardized business documents but requires AWS cloud processing.

**AnalyzeID:** Extracts data from ID documents like driver's licenses and passports. Returns structured data including name, address, date of birth, and document number. Useful for KYC workflows but raises data sovereignty concerns for identity documents.

**StartDocumentAnalysis (Async):** Required for multi-page PDFs. Documents must be staged in S3, processing is polled for completion, and results retrieved separately. Adds significant architectural complexity.

### AWS Textract Architecture

```
Your .NET Application
    |
    v
AWS SDK for .NET
    |
    v (HTTPS)
S3 Bucket (optional staging)
    |
    v
AWS Textract Service
    |
    +-- us-east-1
    +-- us-west-2
    +-- eu-west-1
    +-- (other regions)
    |
    v (processed)
Results returned / stored
```

### Operational Complexity

Working with AWS Textract introduces several operational dependencies:

**Credential Management:** IAM users, roles, access keys, and rotation policies. Credentials must be securely stored and rotated regularly.

**S3 Dependency:** Large documents and multi-page PDFs require S3 staging. This means additional bucket management, lifecycle policies, and access controls.

**Network Dependency:** Every OCR operation requires internet connectivity and AWS service availability. Network latency directly impacts processing time.

**Rate Limits:** Textract enforces transaction-per-second (TPS) limits that vary by operation type and account. Batch processing must implement throttling and retry logic.

---

## Security and Compliance Analysis

### Government and Military Considerations

**Federal Agencies:**

AWS GovCloud provides FedRAMP High authorization, but:
- Documents still leave your infrastructure
- Processing happens on AWS-managed servers
- Audit scope includes AWS as processor
- Not suitable for classified data

**Defense Contractors (DFARS/CMMC):**

| Requirement | AWS Textract Impact |
|-------------|---------------------|
| CMMC Level 1 | May be acceptable with controls |
| CMMC Level 2 | Requires careful BAA review |
| CMMC Level 3 | Likely problematic for CUI |
| ITAR-controlled | Prohibited without special authorization |
| Classified | Absolutely prohibited |

**Air-Gapped Deployments:**

AWS Textract cannot function in air-gapped environments. Any network restrictions block the service entirely.

### Healthcare Compliance

**HIPAA Considerations:**

While AWS offers HIPAA BAA:
- PHI still leaves your security perimeter
- AWS becomes a Business Associate
- Breach notification complexity increases
- Minimum necessary principle applies

**Many healthcare organizations find on-premise OCR preferable** for processing medical records, insurance documents, and patient information.

### Financial Services

**Regulatory Frameworks:**

| Regulation | Concern |
|------------|---------|
| GLBA | Customer financial data protection |
| SOX | Data integrity and audit trail |
| PCI DSS | Payment card images prohibited |
| State banking laws | Data residency requirements |

### Data Residency

AWS Textract is available in limited regions:

| Region | Data Residency |
|--------|----------------|
| us-east-1 | United States |
| us-west-2 | United States |
| eu-west-1 | Ireland |
| eu-west-2 | United Kingdom |
| ap-southeast-1 | Singapore |
| ap-southeast-2 | Australia |

**For organizations with strict data residency requirements**, this limited regional availability may be insufficient.

---

## AWS Textract Pricing

### Current Pricing (2026)

**DetectDocumentText (basic OCR):**
| Volume | Price per Page |
|--------|----------------|
| First 1M pages/month | $0.0015 |
| 1M+ pages/month | $0.0006 |

**AnalyzeDocument (tables/forms):**
| Feature | Price per Page |
|---------|----------------|
| Tables | $0.015 |
| Forms | $0.05 |
| Tables + Forms | $0.065 |

**AnalyzeExpense:**
$0.01 per page

**AnalyzeID:**
$0.025 per page

*Pricing as of January 2026. Visit [AWS Textract pricing page](https://aws.amazon.com/textract/pricing/) for current rates.*

### Cost Calculation Example

**Scenario:** Processing 100,000 invoices/month with table extraction

```
AWS Textract:
  AnalyzeDocument (Tables+Forms): 100,000 x $0.065 = $6,500/month
  Annual cost: $78,000

IronOCR Alternative:
  Unlimited license: $5,999 one-time
  Year 1: $5,999
  Year 2+: $0

  Savings over 3 years: $228,001
```

### Hidden Costs

1. **S3 Storage** - Large documents may require S3 staging
2. **Data Transfer** - Egress charges for results
3. **Development** - SDK complexity, error handling
4. **Operations** - AWS monitoring, cost management
5. **Compliance** - Legal review, audit preparation

### Long-Term Cost Projection

| Time Period | AWS Textract (100K pages/mo, tables) | IronOCR Professional |
|-------------|--------------------------------------|----------------------|
| Year 1 | $78,000 | $2,999 |
| Year 2 | $78,000 | $0 |
| Year 3 | $78,000 | $0 |
| 3-Year Total | $234,000 | $2,999 |
| 5-Year Total | $390,000 | $2,999 |

For high-volume document processing, the cost differential is substantial.

---

## Implementing AWS Textract in .NET

### Basic Setup

```csharp
using Amazon.Textract;
using Amazon.Textract.Model;

public class TextractService
{
    private readonly AmazonTextractClient _client;

    public TextractService()
    {
        _client = new AmazonTextractClient(Amazon.RegionEndpoint.USEast1);
    }

    public async Task<string> DetectTextAsync(string imagePath)
    {
        // WARNING: Image data is sent to AWS
        var imageBytes = File.ReadAllBytes(imagePath);

        var request = new DetectDocumentTextRequest
        {
            Document = new Document
            {
                Bytes = new MemoryStream(imageBytes)
            }
        };

        var response = await _client.DetectDocumentTextAsync(request);

        var text = new StringBuilder();
        foreach (var block in response.Blocks.Where(b => b.BlockType == BlockType.LINE))
        {
            text.AppendLine(block.Text);
        }

        return text.ToString();
    }
}
```

### Table and Form Extraction

```csharp
public async Task<AnalysisResult> AnalyzeDocumentAsync(string imagePath)
{
    var imageBytes = File.ReadAllBytes(imagePath);

    var request = new AnalyzeDocumentRequest
    {
        Document = new Document
        {
            Bytes = new MemoryStream(imageBytes)
        },
        FeatureTypes = new List<string> { "TABLES", "FORMS" }
    };

    var response = await _client.AnalyzeDocumentAsync(request);

    return new AnalysisResult
    {
        Text = ExtractText(response.Blocks),
        Tables = ExtractTables(response.Blocks),
        Forms = ExtractForms(response.Blocks)
    };
}

private List<List<string>> ExtractTables(List<Block> blocks)
{
    // Complex block relationship parsing required
    // Tables are represented as cell blocks with relationships
    // Significant code required to reconstruct table structure
    throw new NotImplementedException("Complex implementation required");
}
```

### Large Document Processing (Async API)

```csharp
public async Task<string> ProcessLargeDocumentAsync(string s3Bucket, string s3Key)
{
    // Start async job
    var startRequest = new StartDocumentTextDetectionRequest
    {
        DocumentLocation = new DocumentLocation
        {
            S3Object = new S3Object
            {
                Bucket = s3Bucket,
                Name = s3Key
            }
        }
    };

    var startResponse = await _client.StartDocumentTextDetectionAsync(startRequest);
    var jobId = startResponse.JobId;

    // Poll for completion
    GetDocumentTextDetectionResponse getResponse;
    do
    {
        await Task.Delay(5000);

        getResponse = await _client.GetDocumentTextDetectionAsync(
            new GetDocumentTextDetectionRequest { JobId = jobId });

    } while (getResponse.JobStatus == JobStatus.IN_PROGRESS);

    if (getResponse.JobStatus != JobStatus.SUCCEEDED)
    {
        throw new Exception($"Textract job failed: {getResponse.StatusMessage}");
    }

    // Extract text from results
    var text = new StringBuilder();
    foreach (var block in getResponse.Blocks.Where(b => b.BlockType == BlockType.LINE))
    {
        text.AppendLine(block.Text);
    }

    return text.ToString();
}
```

---

## On-Premise Alternative: IronOCR

For organizations where cloud processing is unacceptable, IronOCR provides comprehensive OCR without external data transmission.

### Equivalent Implementation

```csharp
using IronOcr;

public class OnPremiseOcrService
{
    private readonly IronTesseract _ocr;

    public OnPremiseOcrService()
    {
        _ocr = new IronTesseract();
    }

    public string DetectText(string imagePath)
    {
        // All processing happens locally
        return _ocr.Read(imagePath).Text;
    }

    public DocumentAnalysis AnalyzeDocument(string imagePath)
    {
        var result = _ocr.Read(imagePath);

        return new DocumentAnalysis
        {
            Text = result.Text,
            Confidence = result.Confidence,
            Lines = result.Lines.Select(l => new LineInfo
            {
                Text = l.Text,
                BoundingBox = new BoundingBox(l.X, l.Y, l.Width, l.Height)
            }).ToList(),
            Tables = ExtractTablesFromLayout(result)
        };
    }

    private List<TableData> ExtractTablesFromLayout(OcrResult result)
    {
        // IronOCR provides word positions for table reconstruction
        var tables = new List<TableData>();

        // Group lines by Y position for row detection
        var rows = result.Lines
            .GroupBy(l => l.Y / 20) // 20px tolerance for row grouping
            .OrderBy(g => g.Key)
            .ToList();

        // Analyze column structure from word positions
        // ... table extraction logic

        return tables;
    }

    public string ProcessLargeDocument(string pdfPath)
    {
        // No S3 staging, no async polling, no job management
        using var input = new OcrInput();
        input.LoadPdf(pdfPath);

        return _ocr.Read(input).Text;
    }
}
```

### Feature Comparison

| Feature | AWS Textract | IronOCR |
|---------|--------------|---------|
| Basic OCR | Yes | Yes |
| Table extraction | Yes (complex API) | Via positioning |
| Form extraction | Yes (complex API) | Via zones |
| PDF support | Yes (via S3) | Native |
| Encrypted PDF | No | Built-in |
| On-premise | No | Yes |
| Air-gapped | No | Yes |
| Async API | Required for large docs | Not needed |
| S3 dependency | For large docs | None |
| Per-page cost | $0.0015+ | None (licensed) |

---

## Migration Guide: AWS Textract to IronOCR

This section provides a comprehensive, step-by-step migration path from AWS Textract to IronOCR for .NET applications.

### Why Migrate from AWS Textract?

Before investing time in migration, understand the common motivations:

#### Strong Migration Candidates

| Symptom | Root Cause | IronOCR Solution |
|---------|------------|------------------|
| High monthly costs | Per-page pricing compounds | One-time license fee |
| S3 staging complexity | Async API requirements | Direct file processing |
| Data sovereignty concerns | Cloud processing mandatory | Local processing |
| Network dependency issues | Internet required for every call | Offline capable |
| Rate limit throttling | AWS TPS limits | No throttling |
| Credential management burden | IAM complexity | No credentials needed |

#### Pain Points Addressed

**1. Per-Page Costs:** AWS Textract's pricing model becomes expensive at scale. Processing 100,000 pages monthly with table extraction costs $6,500/month. IronOCR eliminates per-page fees entirely.

**2. S3 Dependency:** Multi-page PDFs require S3 staging, introducing bucket management, access policies, and lifecycle rules. IronOCR processes PDFs directly from disk.

**3. Async Complexity:** Large documents require start job, poll status, retrieve results patterns. IronOCR provides synchronous processing regardless of document size.

**4. Network Latency:** Every Textract call includes network round-trip time. Local processing eliminates this overhead.

**5. Credential Rotation:** AWS credentials require secure storage and regular rotation. IronOCR uses a simple license key.

### Migration Complexity Assessment

| Migration Type | Estimated Time | Description |
|---------------|----------------|-------------|
| Simple | 2-4 hours | Basic DetectDocumentText usage |
| Medium | 4-8 hours | AnalyzeDocument with tables/forms |
| Complex | 1-2 days | Async processing, S3 integration, batch workflows |

### Step 1: Package Changes

#### Remove AWS Dependencies

```xml
<!-- Remove from .csproj -->
<PackageReference Include="AWSSDK.Textract" Version="*" />
<PackageReference Include="AWSSDK.S3" Version="*" />
<PackageReference Include="AWSSDK.Core" Version="*" />
```

#### Add IronOCR

```xml
<!-- Add to .csproj -->
<PackageReference Include="IronOcr" Version="2024.*" />
```

#### NuGet Commands

```powershell
# Remove AWS packages
Uninstall-Package AWSSDK.Textract
Uninstall-Package AWSSDK.S3

# Add IronOCR
Install-Package IronOcr
```

### Step 2: API Mapping Reference

#### Core Classes

| AWS Textract | IronOCR | Notes |
|--------------|---------|-------|
| `AmazonTextractClient` | `IronTesseract` | Main OCR engine |
| `DetectDocumentTextRequest` | `OcrInput` | Input container |
| `DetectDocumentTextResponse` | `OcrResult` | Results container |
| `Block` (LINE) | `OcrResult.Lines` | Text lines |
| `Block` (WORD) | `OcrResult.Words` | Individual words |
| `S3Object` | File path string | Direct file access |

#### Method Mapping

| AWS Textract Method | IronOCR Equivalent |
|--------------------|-------------------|
| `DetectDocumentTextAsync(request)` | `ocr.Read(path)` |
| `AnalyzeDocumentAsync(request)` | `ocr.Read(input)` with result processing |
| `StartDocumentTextDetectionAsync` | `ocr.Read(input)` (no async needed) |
| `GetDocumentTextDetectionAsync` | N/A (results immediate) |

### Step 3: Code Migration Examples

#### Basic Text Extraction

**Before (AWS Textract):**
```csharp
using Amazon.Textract;
using Amazon.Textract.Model;

public async Task<string> ExtractTextAsync(string imagePath)
{
    var client = new AmazonTextractClient(RegionEndpoint.USEast1);
    var imageBytes = File.ReadAllBytes(imagePath);

    var request = new DetectDocumentTextRequest
    {
        Document = new Document
        {
            Bytes = new MemoryStream(imageBytes)
        }
    };

    var response = await client.DetectDocumentTextAsync(request);

    return string.Join("\n", response.Blocks
        .Where(b => b.BlockType == BlockType.LINE)
        .Select(b => b.Text));
}
```

**After (IronOCR):**
```csharp
using IronOcr;

public string ExtractText(string imagePath)
{
    var ocr = new IronTesseract();
    return ocr.Read(imagePath).Text;
}
```

**Key Changes:**
- No AWS credentials required
- No async/await needed
- No byte array conversion
- No block filtering
- 90% less code

#### Table Extraction

**Before (AWS Textract):**
```csharp
public async Task<List<TableData>> ExtractTablesAsync(string imagePath)
{
    var client = new AmazonTextractClient(RegionEndpoint.USEast1);
    var imageBytes = File.ReadAllBytes(imagePath);

    var request = new AnalyzeDocumentRequest
    {
        Document = new Document { Bytes = new MemoryStream(imageBytes) },
        FeatureTypes = new List<string> { "TABLES" }
    };

    var response = await client.AnalyzeDocumentAsync(request);

    // Complex table reconstruction from blocks
    var tables = new List<TableData>();
    var tableBlocks = response.Blocks.Where(b => b.BlockType == BlockType.TABLE);

    foreach (var tableBlock in tableBlocks)
    {
        var table = new TableData();
        var cellBlocks = response.Blocks
            .Where(b => b.BlockType == BlockType.CELL &&
                        tableBlock.Relationships?.Any(r =>
                            r.Type == RelationshipType.CHILD &&
                            r.Ids.Contains(b.Id)) == true);

        foreach (var cell in cellBlocks)
        {
            // Complex cell relationship parsing...
            // Get child word blocks for cell text...
        }
        tables.Add(table);
    }

    return tables;
}
```

**After (IronOCR):**
```csharp
public List<TableData> ExtractTables(string imagePath)
{
    var ocr = new IronTesseract();
    var result = ocr.Read(imagePath);

    // Use word positions to reconstruct tables
    var tables = new List<TableData>();
    var words = result.Words.OrderBy(w => w.Y).ThenBy(w => w.X);

    // Group by row (Y position within tolerance)
    var rows = words.GroupBy(w => w.Y / 15);

    foreach (var row in rows)
    {
        // Analyze column structure from X positions
        var cells = row.OrderBy(w => w.X).ToList();
        // Build table row...
    }

    return tables;
}
```

#### PDF Processing (Multi-Page)

**Before (AWS Textract with S3):**
```csharp
public async Task<string> ProcessPdfAsync(string pdfPath)
{
    // Step 1: Upload to S3
    var s3Client = new AmazonS3Client(RegionEndpoint.USEast1);
    var bucketName = "my-textract-bucket";
    var key = $"uploads/{Guid.NewGuid()}.pdf";

    using (var fileStream = File.OpenRead(pdfPath))
    {
        await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucketName,
            Key = key,
            InputStream = fileStream
        });
    }

    // Step 2: Start async Textract job
    var textractClient = new AmazonTextractClient(RegionEndpoint.USEast1);
    var startResponse = await textractClient.StartDocumentTextDetectionAsync(
        new StartDocumentTextDetectionRequest
        {
            DocumentLocation = new DocumentLocation
            {
                S3Object = new S3Object { Bucket = bucketName, Name = key }
            }
        });

    // Step 3: Poll for completion
    GetDocumentTextDetectionResponse getResponse;
    do
    {
        await Task.Delay(5000);
        getResponse = await textractClient.GetDocumentTextDetectionAsync(
            new GetDocumentTextDetectionRequest { JobId = startResponse.JobId });
    } while (getResponse.JobStatus == JobStatus.IN_PROGRESS);

    // Step 4: Handle pagination
    var allText = new StringBuilder();
    string nextToken = null;
    do
    {
        var pageResponse = await textractClient.GetDocumentTextDetectionAsync(
            new GetDocumentTextDetectionRequest
            {
                JobId = startResponse.JobId,
                NextToken = nextToken
            });

        foreach (var block in pageResponse.Blocks.Where(b => b.BlockType == BlockType.LINE))
        {
            allText.AppendLine(block.Text);
        }
        nextToken = pageResponse.NextToken;
    } while (nextToken != null);

    // Step 5: Clean up S3
    await s3Client.DeleteObjectAsync(bucketName, key);

    return allText.ToString();
}
```

**After (IronOCR):**
```csharp
public string ProcessPdf(string pdfPath)
{
    var ocr = new IronTesseract();
    using var input = new OcrInput();
    input.LoadPdf(pdfPath);
    return ocr.Read(input).Text;
}
```

**Complexity Reduction:**
- No S3 bucket management
- No credential configuration
- No async job polling
- No pagination handling
- No cleanup required
- 50+ lines reduced to 5

### Step 4: Removing AWS Dependencies

#### Configuration Cleanup

Remove AWS configuration from your project:

```
# Delete these files if Textract-only:
appsettings.json AWS sections
~/.aws/credentials (or remove Textract-specific profiles)
Environment variables: AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY
```

#### IAM Cleanup

If migrating away from AWS entirely:
- Delete IAM users created for Textract
- Remove Textract permissions from roles
- Delete S3 buckets used for document staging

#### Docker Cleanup

**Before (AWS Textract):**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# AWS SDK requires these environment variables
ENV AWS_ACCESS_KEY_ID=your_key
ENV AWS_SECRET_ACCESS_KEY=your_secret
ENV AWS_DEFAULT_REGION=us-east-1

WORKDIR /app
COPY . .
ENTRYPOINT ["dotnet", "YourApp.dll"]
```

**After (IronOCR):**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# No AWS configuration needed
WORKDIR /app
COPY . .
ENTRYPOINT ["dotnet", "YourApp.dll"]
```

### Step 5: Common Migration Issues

#### Issue 1: Async to Sync Pattern Change

**Problem:** Existing code uses async/await with Textract.

**Solution:** IronOCR operations are synchronous but fast. For UI responsiveness, wrap in Task.Run:

```csharp
// If you need async for UI thread
public async Task<string> ExtractTextAsync(string path)
{
    return await Task.Run(() => new IronTesseract().Read(path).Text);
}
```

#### Issue 2: Block-Based Result Structure

**Problem:** Code expects Textract's Block hierarchy with relationships.

**Solution:** IronOCR provides direct access to hierarchical results:

```csharp
// Textract pattern
var lines = response.Blocks.Where(b => b.BlockType == BlockType.LINE);
var words = response.Blocks.Where(b => b.BlockType == BlockType.WORD);

// IronOCR pattern
var result = ocr.Read(path);
var lines = result.Lines;
var words = result.Words;
var paragraphs = result.Paragraphs;
```

#### Issue 3: Confidence Score Differences

**Problem:** Textract confidence is per-block; IronOCR confidence is overall.

**Solution:** Access per-element confidence in IronOCR:

```csharp
var result = ocr.Read(path);

// Overall confidence
double overall = result.Confidence;

// Per-word confidence
foreach (var word in result.Words)
{
    Console.WriteLine($"'{word.Text}': {word.Confidence}%");
}
```

#### Issue 4: S3 Event Triggers

**Problem:** Existing workflow uses S3 uploads to trigger Textract.

**Solution:** Replace with file system monitoring or queue-based processing:

```csharp
// Watch folder for new documents
var watcher = new FileSystemWatcher(uploadFolder, "*.pdf");
watcher.Created += (s, e) =>
{
    var text = new IronTesseract().Read(e.FullPath).Text;
    // Process result...
};
watcher.EnableRaisingEvents = true;
```

### Migration Checklist

#### Pre-Migration
- [ ] Inventory all Textract usage points in codebase
- [ ] Document S3 buckets used for staging
- [ ] Identify IAM users/roles for Textract access
- [ ] Create test document corpus
- [ ] Baseline current accuracy and performance metrics
- [ ] Obtain IronOCR license

#### Migration
- [ ] Remove AWS NuGet packages
- [ ] Add IronOCR NuGet package
- [ ] Update namespace imports
- [ ] Replace DetectDocumentText calls
- [ ] Replace AnalyzeDocument calls
- [ ] Remove S3 upload/download code
- [ ] Remove async job polling code
- [ ] Update error handling
- [ ] Configure IronOCR license

#### Post-Migration
- [ ] Run validation test suite
- [ ] Compare accuracy metrics
- [ ] Compare performance metrics
- [ ] Delete S3 staging buckets
- [ ] Remove IAM Textract permissions
- [ ] Remove AWS credentials
- [ ] Update deployment pipelines
- [ ] Update monitoring/alerting
- [ ] Document cost savings

---

## Performance Comparison

| Metric | AWS Textract | IronOCR |
|--------|--------------|---------|
| Single page (network included) | 500-2000ms | 100-400ms |
| Batch (100 pages) | 30-60s | 15-40s |
| Large PDF (async job) | Minutes | Direct processing |
| Rate limit | 5-15 TPS | No limit |
| Latency variability | High (network) | Low (local) |

### Latency Considerations

AWS Textract latency includes:
- Network round-trip to AWS region
- S3 upload time (for large documents)
- Queue wait time (during high demand)
- Processing time
- Result download time

IronOCR latency is purely processing time with no network overhead.

### Throughput Scaling

**AWS Textract:**
- Default TPS limits require throttling
- Burst capacity limited
- Must implement retry logic
- Cost increases linearly with volume

**IronOCR:**
- Limited only by local CPU/memory
- Parallel processing built-in
- No throttling required
- Cost fixed regardless of volume

---

## When to Use Each Option

### Choose AWS Textract When

- **Serverless architecture** - Lambda functions with occasional OCR needs
- **Already using AWS heavily** - Existing S3 workflows, IAM expertise
- **Specialized features needed** - AnalyzeExpense for invoices, AnalyzeID for identity documents
- **Low volume** - Under 10,000 pages/month where per-page cost is acceptable
- **Data security is not primary concern** - Non-sensitive public documents

### Choose IronOCR When

- **Data must remain on-premise** - Healthcare, finance, government, legal
- **Regulatory compliance required** - HIPAA, GDPR, CMMC, ITAR
- **Air-gapped deployment** - No internet connectivity available
- **High volume processing** - Cost savings at scale
- **Predictable costs needed** - Fixed licensing vs variable usage
- **Low latency required** - No network overhead acceptable
- **Simplified architecture** - No AWS dependency desired

### Decision Framework

| Factor | AWS Textract | IronOCR |
|--------|--------------|---------|
| Monthly volume < 10K pages | Acceptable | Preferred |
| Monthly volume > 50K pages | Expensive | Preferred |
| Air-gapped environment | Not possible | Required |
| HIPAA/PHI data | Requires BAA | Full control |
| ITAR-controlled data | Prohibited | Preferred |
| Serverless architecture | Native fit | Requires compute |
| Need AnalyzeExpense | Built-in | Custom implementation |
| Need AnalyzeID | Built-in | Custom implementation |

---

## Code Examples

Complete working examples demonstrating various scenarios:

- [AWS Textract vs IronOCR Examples](./aws-textract-vs-ironocr-examples.cs) - Side-by-side comparison code
- [Migration Examples](./aws-textract-migration-examples.cs) - Before/after migration patterns
- [Async Processing Comparison](./aws-textract-async-processing.cs) - Complexity comparison for large documents

---

## Additional Resources

- [AWS Textract Documentation](https://docs.aws.amazon.com/textract/)
- [AWS Textract .NET SDK](https://docs.aws.amazon.com/sdkfornet/)
- [IronOCR Documentation](https://ironsoftware.com/csharp/ocr/docs/)
- [IronOCR Tutorials](https://ironsoftware.com/csharp/ocr/tutorials/)

---

*Last verified: January 2026*
