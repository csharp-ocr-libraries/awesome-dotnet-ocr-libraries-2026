# Azure Computer Vision OCR for .NET: Complete Developer Guide (2026)

Azure Computer Vision is Microsoft's cloud-based OCR service that processes images and documents through Azure's infrastructure. While offering powerful recognition capabilities, the cloud-first architecture raises significant considerations for enterprise security, data sovereignty, and regulatory compliance that .NET developers must carefully evaluate. For on-premise alternatives, see [IronOCR](https://ironsoftware.com/csharp/ocr/).

## Critical Security Warning for Enterprise Customers

**Before implementing Azure Computer Vision, organizations must understand:**

When you use Azure Computer Vision for OCR, your documents are transmitted to and processed on Microsoft's cloud servers. This has profound implications for:

- **HIPAA-covered entities** processing protected health information
- **Government contractors** handling controlled unclassified information (CUI)
- **Defense contractors** managing ITAR-controlled data
- **Financial institutions** processing customer financial records
- **Legal firms** handling privileged communications
- **Any organization** with data residency requirements

This guide examines these concerns in depth and provides alternatives for security-conscious deployments.

## Azure Computer Vision Overview

### What Azure Computer Vision Offers

Azure Computer Vision's OCR capabilities include:

- **Read API** - Optimized for text-heavy documents
- **Image Analysis API** - Extracts text along with object detection
- **Form Recognizer** - Pre-built models for invoices, receipts, IDs
- **Custom models** - Train on your specific document types
- **Handwriting recognition** - Print and cursive script
- **Multi-language support** - 164+ languages

### Architecture: How Your Data Flows

Understanding the data flow is critical for security assessment:

```
Your Application (.NET)
    │
    ▼
Azure SDK / REST API call
    │
    ▼ (HTTPS encrypted)
Azure Computer Vision Endpoint
    │
    ├── East US data center
    ├── West Europe data center
    ├── Southeast Asia data center
    └── (other regions)
    │
    ▼
Microsoft OCR Processing
    │
    ▼
Result returned to your application
```

**Key Security Considerations:**

1. Your document data travels over the internet to Microsoft servers
2. Documents are processed on Microsoft-managed infrastructure
3. Data may be cached, logged, or retained per Microsoft's policies
4. You have limited visibility into the processing environment
5. Data crosses organizational boundaries

## Data Sovereignty and Compliance Deep Dive

### Who Should NOT Use Azure Computer Vision

Based on security requirements, these organizations should carefully consider alternatives:

#### Government Agencies (Federal, State, Local)

**Federal agencies** subject to:
- **FedRAMP** - While Azure has FedRAMP authorization, data still leaves your environment
- **FISMA** - Federal Information Security Modernization Act requires careful data handling
- **NIST 800-171** - Controlled Unclassified Information (CUI) protection

**State and local governments** with:
- Data residency requirements
- Citizen privacy mandates
- State-specific security regulations

#### Defense and Intelligence

**Defense contractors** handling:
- **ITAR** - International Traffic in Arms Regulations prohibit certain data exports
- **EAR** - Export Administration Regulations for dual-use technologies
- **CMMC** - Cybersecurity Maturity Model Certification requirements
- **Classified information** - Even if using Azure Government

**Air-gapped environments:**
- SCIFs (Sensitive Compartmented Information Facilities)
- Isolated networks without internet connectivity
- Military installations with network restrictions

#### Healthcare

**HIPAA-covered entities** must consider:
- Business Associate Agreement (BAA) requirements
- Minimum necessary standard - is cloud OCR necessary?
- Risk analysis obligations under HIPAA Security Rule
- State-specific healthcare privacy laws (e.g., California CMIA)

While Microsoft offers HIPAA BAA for Azure:
- Data still leaves your environment
- You inherit Microsoft's security posture
- Breach notification becomes complex with third parties

#### Financial Services

**Banks, credit unions, investment firms:**
- **GLBA** - Gramm-Leach-Bliley Act requirements
- **SOX** - Sarbanes-Oxley data integrity requirements
- **PCI DSS** - If processing payment card images
- State banking regulations
- SEC/FINRA recordkeeping requirements

#### Legal

**Law firms and legal departments:**
- Attorney-client privilege concerns
- Work product doctrine protection
- Ethical obligations regarding client data
- E-discovery and litigation hold requirements

### Geographic Data Residency Requirements

Many jurisdictions have strict data localization laws:

| Region | Regulation | Requirement |
|--------|------------|-------------|
| European Union | GDPR | May require data processing within EU |
| Germany | BDSG | Stricter than GDPR for certain data |
| Russia | Federal Law No. 242-FZ | Data must be stored in Russia |
| China | PIPL/CSL | Data localization for certain categories |
| Australia | Privacy Act | Consider data location for sensitive data |
| Canada | PIPEDA + Provincial | Provincial requirements may apply |
| Brazil | LGPD | Similar to GDPR requirements |

**Azure's regional endpoints don't fully solve this:**

Even with a regional endpoint (e.g., West Europe), you still:
- Send data outside your organization
- Rely on Microsoft's data handling claims
- Have limited audit capability
- Cannot guarantee data doesn't cross borders for processing

## Azure Computer Vision Pricing Analysis

### Current Pricing (2026)

| Tier | Price per 1,000 Images | Included Features |
|------|------------------------|-------------------|
| Free | $0 (5,000/month limit) | Read API only |
| S1 | $1.50 | Read API |
| S2 | $2.50 | Read + Image Analysis |
| S3 | $4.00 | All features |

**Form Recognizer pricing:**

| Feature | Price per Page |
|---------|----------------|
| Read (prebuilt) | $0.001 |
| Layout | $0.01 |
| Invoice/Receipt | $0.01 |
| Custom model | $0.01 |

*Pricing as of January 2026. Visit [Azure Computer Vision pricing page](https://azure.microsoft.com/pricing/details/cognitive-services/computer-vision/) for current rates.*

### Hidden Costs

Beyond per-transaction pricing, consider:

**1. Network Egress**
```
Sending images to Azure:
  - 1 MB average image × 10,000 documents/month = 10 GB
  - Egress from your infrastructure varies by provider
```

**2. Development and Integration**
- Azure SDK learning curve
- Authentication/authorization setup
- Error handling for cloud APIs
- Retry logic and rate limiting

**3. Operational Overhead**
- Azure subscription management
- Cost monitoring and alerting
- API version updates
- Regional endpoint management

**4. Compliance Costs**
- Legal review of Microsoft agreements
- Security assessments
- Audit preparation
- Compliance documentation

### Total Cost of Ownership Example

**Scenario:** Mid-size company processing 50,000 documents/month

```
Azure Computer Vision:
  OCR processing:     $75/month (50K × $1.50/1000)
  Form processing:    $500/month (50K × $0.01)
  Network costs:      ~$10/month
  DevOps overhead:    ~$200/month (monitoring, management)
  Compliance review:  ~$2,000/year (amortized: $167/month)
  ─────────────────────────────────────────
  Monthly total:      ~$952
  Annual total:       ~$11,424

IronOCR (on-premise alternative):
  License:            $2,999 one-time (Professional)
  No per-document cost
  No network costs
  Simpler compliance
  ─────────────────────────────────────────
  Year 1:            $2,999
  Year 2+:           $0 (perpetual license)
  3-year total:      $2,999

  Savings over 3 years: $31,273
```

## Implementing Azure Computer Vision in .NET

### Basic Implementation

Despite security concerns, here's how to implement Azure Computer Vision if you proceed:

```csharp
using Azure;
using Azure.AI.Vision.ImageAnalysis;

public class AzureOcrService
{
    private readonly ImageAnalysisClient _client;

    public AzureOcrService(string endpoint, string apiKey)
    {
        _client = new ImageAnalysisClient(
            new Uri(endpoint),
            new AzureKeyCredential(apiKey));
    }

    public async Task<string> ExtractTextAsync(string imagePath)
    {
        // WARNING: This sends your image to Azure
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

### PDF Processing with Azure

```csharp
using Azure.AI.FormRecognizer.DocumentAnalysis;

public class AzureDocumentService
{
    private readonly DocumentAnalysisClient _client;

    public AzureDocumentService(string endpoint, string apiKey)
    {
        _client = new DocumentAnalysisClient(
            new Uri(endpoint),
            new AzureKeyCredential(apiKey));
    }

    public async Task<string> ExtractFromPdfAsync(string pdfPath)
    {
        // WARNING: PDF is uploaded to Azure
        using var stream = File.OpenRead(pdfPath);

        var operation = await _client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            "prebuilt-read",
            stream);

        var result = operation.Value;
        var text = new StringBuilder();

        foreach (var page in result.Pages)
        {
            foreach (var line in page.Lines)
            {
                text.AppendLine(line.Content);
            }
        }

        return text.ToString();
    }
}
```

### Error Handling for Cloud APIs

```csharp
public async Task<string> RobustExtractTextAsync(string imagePath)
{
    const int maxRetries = 3;
    int retryCount = 0;

    while (retryCount < maxRetries)
    {
        try
        {
            return await ExtractTextAsync(imagePath);
        }
        catch (RequestFailedException ex) when (ex.Status == 429)
        {
            // Rate limited - wait and retry
            retryCount++;
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
        }
        catch (RequestFailedException ex) when (ex.Status >= 500)
        {
            // Server error - retry
            retryCount++;
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
        catch (RequestFailedException ex)
        {
            // Client error - don't retry
            throw new OcrException($"Azure OCR failed: {ex.Message}", ex);
        }
    }

    throw new OcrException("Max retries exceeded for Azure OCR");
}
```

## On-Premise Alternative: IronOCR

For organizations where cloud OCR is not acceptable, IronOCR provides equivalent functionality without data leaving your infrastructure.

### Security Comparison

| Aspect | Azure Computer Vision | IronOCR |
|--------|----------------------|---------|
| Data location | Microsoft Azure servers | Your infrastructure |
| Network transmission | Required | None |
| Third-party access | Yes (Microsoft) | No |
| Air-gapped deployment | Impossible | Fully supported |
| Compliance audit | Complex (includes Microsoft) | Simple (your org only) |
| Data retention | Per Microsoft policy | Your control |
| Breach liability | Shared with Microsoft | Your control |

### Equivalent Implementation in IronOCR

```csharp
using IronOcr;

public class OnPremiseOcrService
{
    private readonly IronTesseract _ocr;

    public OnPremiseOcrService()
    {
        _ocr = new IronTesseract();
    }

    public string ExtractText(string imagePath)
    {
        // All processing happens locally - no data leaves your infrastructure
        var result = _ocr.Read(imagePath);
        return result.Text;
    }

    public string ExtractFromPdf(string pdfPath)
    {
        // PDF processed entirely on-premise
        using var input = new OcrInput();
        input.LoadPdf(pdfPath);

        return _ocr.Read(input).Text;
    }

    public string ExtractFromPdf(string pdfPath, string password)
    {
        // Even encrypted PDFs stay local
        using var input = new OcrInput();
        input.LoadPdf(pdfPath, Password: password);

        return _ocr.Read(input).Text;
    }
}
```

### Feature Comparison

| Feature | Azure Computer Vision | IronOCR |
|---------|----------------------|---------|
| Basic OCR | Yes | Yes |
| PDF support | Yes | Yes (native) |
| Password PDFs | Via Form Recognizer | Built-in |
| Handwriting | Yes | Yes |
| Tables | Via Form Recognizer | Built-in |
| Barcodes | Limited | Full support |
| 130+ languages | Yes | 125+ |
| Searchable PDF output | Manual | Built-in |
| Preprocessing | Limited | Extensive |
| Custom training | Yes (paid) | N/A |
| Offline operation | No | Yes |
| No internet required | No | Yes |

## Migration: Azure Computer Vision to IronOCR

If you've started with Azure and need to move to an on-premise solution:

### API Mapping

| Azure Computer Vision | IronOCR |
|----------------------|---------|
| `ImageAnalysisClient` | `IronTesseract` |
| `AnalyzeAsync()` | `Read()` |
| `ReadResult.Blocks` | `OcrResult.Paragraphs` |
| `ReadResult.Lines` | `OcrResult.Lines` |
| `DocumentAnalysisClient` | `IronTesseract` + `OcrInput` |
| `AnalyzeDocumentAsync()` | `Read()` with PDF input |

### Migration Code Example

**Before (Azure):**
```csharp
var client = new ImageAnalysisClient(
    new Uri(endpoint),
    new AzureKeyCredential(apiKey));

var result = await client.AnalyzeAsync(
    BinaryData.FromStream(imageStream),
    VisualFeatures.Read);

foreach (var line in result.Value.Read.Blocks.SelectMany(b => b.Lines))
{
    Console.WriteLine(line.Text);
}
```

**After (IronOCR):**
```csharp
using var input = new OcrInput();
input.LoadImage(imageStream);

var result = new IronTesseract().Read(input);

foreach (var line in result.Lines)
{
    Console.WriteLine(line.Text);
}
```

### What You Gain from Migration

1. **Complete data sovereignty** - Documents never leave your infrastructure
2. **Simplified compliance** - No third-party processor to audit
3. **Cost predictability** - One-time license vs. per-transaction
4. **No internet dependency** - Works in air-gapped environments
5. **Faster processing** - No network latency
6. **Reduced attack surface** - No cloud API credentials to protect

## Azure Government and Azure Stack

### Azure Government

Microsoft offers Azure Government for US federal, state, and local government:

- FedRAMP High authorized
- CJIS compliant
- IRS 1075 compliant

**However:**
- Data still leaves your infrastructure
- Still processed on Microsoft (albeit government-only) servers
- Still requires internet connectivity
- Additional cost premium
- May not satisfy all agency requirements

### Azure Stack Hub (On-Premise)

Azure Stack Hub brings Azure services on-premise:

**Limitations for OCR:**
- Not all Azure services available on Stack
- Significant infrastructure investment
- Complex setup and maintenance
- Per-service availability varies
- May not include latest Computer Vision features

**For most organizations seeking on-premise OCR, a purpose-built library like IronOCR is simpler and more cost-effective than deploying Azure Stack.**

## Performance Considerations

### Latency Comparison

| Scenario | Azure Computer Vision | IronOCR |
|----------|----------------------|---------|
| Single image (local) | 200-800ms | 100-400ms |
| Single image (cross-region) | 500-2000ms | 100-400ms |
| Batch (100 images) | 20-40 seconds | 10-30 seconds |
| Large PDF (50 pages) | 30-60 seconds | 20-40 seconds |

*Azure times include network latency; IronOCR is local processing only*

### Throughput Limits

**Azure Computer Vision:**
- Free tier: 20 calls/minute
- S1: 10 transactions/second
- S2: 10 transactions/second
- S3: 10 transactions/second
- Custom tiers available for higher throughput

**IronOCR:**
- No artificial limits
- Throughput limited only by your hardware
- Scales with CPU cores
- Multiple instances possible

### Reliability

**Azure Computer Vision:**
- 99.9% SLA (standard)
- Dependent on:
  - Your internet connection
  - Azure regional availability
  - API service health

**IronOCR:**
- No external dependencies
- Reliability = your infrastructure reliability
- No network failure modes

## Conclusion: Making the Right Choice

### Choose Azure Computer Vision If:

- Your data has no sensitivity or regulatory restrictions
- Internet connectivity is reliable and acceptable
- You need custom model training capabilities
- Per-transaction pricing fits your volume
- Cloud vendor dependency is acceptable

### Choose IronOCR If:

- Data must stay within your infrastructure
- You have regulatory compliance requirements (HIPAA, ITAR, GDPR, etc.)
- Air-gapped or restricted network deployment is required
- Predictable one-time licensing is preferred
- You need to eliminate cloud vendor dependency
- Government or military customer requirements apply
- Data sovereignty is non-negotiable

For security-conscious enterprises, government agencies, healthcare providers, and organizations handling sensitive data, the on-premise approach eliminates an entire category of risk that cloud OCR inherently introduces.

## Migration Guide: Azure Computer Vision to IronOCR

This section provides a complete roadmap for migrating .NET applications from Azure Computer Vision to IronOCR, enabling on-premise document processing without cloud dependency.

### Why Migrate from Azure Computer Vision?

Organizations typically migrate for these reasons:

1. **Data Sovereignty** - Regulatory requirements prohibit data leaving your infrastructure
2. **Security** - Eliminate cloud transmission of sensitive documents
3. **Cost Reduction** - One-time licensing vs. ongoing per-transaction fees
4. **Air-Gapped Deployment** - Military, government, or isolated environments
5. **Latency** - Eliminate network round-trip for faster processing
6. **Reliability** - Remove internet connectivity as a failure point

### Migration Complexity Assessment

| Scenario | Effort | Primary Challenge |
|----------|--------|-------------------|
| Basic OCR (Read API) | 1-2 hours | Minimal - API is similar |
| Form Recognizer | 4-8 hours | Rebuild structured extraction |
| Custom models | 1-2 weeks | No direct equivalent - use preprocessing |
| High-volume batch | 4-8 hours | Threading model differences |

### Phase 1: Package Migration

#### Remove Azure Packages

```bash
dotnet remove package Azure.AI.Vision.ImageAnalysis
dotnet remove package Azure.AI.FormRecognizer
dotnet remove package Azure.Identity
```

#### Add IronOCR

```bash
dotnet add package IronOcr
```

If you need additional languages:
```bash
dotnet add package IronOcr.Languages.French
dotnet add package IronOcr.Languages.German
```

### Phase 2: Configuration Changes

#### Remove Azure Configuration

**appsettings.json (remove):**
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

#### Add IronOCR Configuration

**appsettings.json (add):**
```json
{
  "IronOCR": {
    "LicenseKey": "${IRONOCR_LICENSE}"
  }
}
```

**Startup configuration:**
```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Azure removed - no cloud configuration needed

    // IronOCR license
    IronOcr.License.LicenseKey = Configuration["IronOCR:LicenseKey"]
        ?? Environment.GetEnvironmentVariable("IRONOCR_LICENSE");
}
```

### Phase 3: API Migration

#### Read API Migration

**Azure Computer Vision (Read API):**
```csharp
using Azure;
using Azure.AI.Vision.ImageAnalysis;

public class AzureOcrService
{
    private readonly ImageAnalysisClient _client;

    public AzureOcrService(string endpoint, string apiKey)
    {
        _client = new ImageAnalysisClient(
            new Uri(endpoint),
            new AzureKeyCredential(apiKey));
    }

    public async Task<string> ReadTextAsync(string imagePath)
    {
        using var stream = File.OpenRead(imagePath);
        var data = BinaryData.FromStream(stream);

        var result = await _client.AnalyzeAsync(
            data,
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

**IronOCR equivalent:**
```csharp
using IronOcr;

public class OcrService
{
    private readonly IronTesseract _ocr;

    public OcrService()
    {
        _ocr = new IronTesseract();
    }

    public string ReadText(string imagePath)
    {
        // No async needed - local processing is fast
        var result = _ocr.Read(imagePath);
        return result.Text;
    }

    // If you need async for compatibility:
    public Task<string> ReadTextAsync(string imagePath)
    {
        return Task.Run(() => ReadText(imagePath));
    }
}
```

#### Document Analysis Migration

**Azure Document Analysis (Form Recognizer):**
```csharp
using Azure.AI.FormRecognizer.DocumentAnalysis;

public async Task<DocumentInfo> AnalyzeDocumentAsync(string pdfPath)
{
    using var stream = File.OpenRead(pdfPath);

    var operation = await _documentClient.AnalyzeDocumentAsync(
        WaitUntil.Completed,
        "prebuilt-read",
        stream);

    var result = operation.Value;

    return new DocumentInfo
    {
        Text = string.Join("\n", result.Pages
            .SelectMany(p => p.Lines)
            .Select(l => l.Content)),
        PageCount = result.Pages.Count
    };
}
```

**IronOCR equivalent:**
```csharp
using IronOcr;

public DocumentInfo AnalyzeDocument(string pdfPath)
{
    using var input = new OcrInput();
    input.LoadPdf(pdfPath);

    var result = _ocr.Read(input);

    return new DocumentInfo
    {
        Text = result.Text,
        PageCount = result.Pages.Length,
        Lines = result.Lines.Select(l => new LineInfo
        {
            Text = l.Text,
            X = l.X,
            Y = l.Y,
            Width = l.Width,
            Height = l.Height
        }).ToList()
    };
}
```

### Phase 4: Structured Data Extraction

If you used Form Recognizer for invoices, receipts, or IDs, use zone-based extraction:

```csharp
public InvoiceData ExtractInvoiceWithZones(string imagePath)
{
    var ocr = new IronTesseract();
    var invoice = new InvoiceData();

    // Define zones for specific fields
    var vendorZone = new CropRectangle(0, 0, 300, 100);
    var dateZone = new CropRectangle(400, 0, 200, 50);
    var totalZone = new CropRectangle(400, 500, 200, 100);

    // Extract each zone
    using (var input = new OcrInput())
    {
        input.LoadImage(imagePath, vendorZone);
        invoice.VendorName = ocr.Read(input).Text.Trim();
    }

    using (var input = new OcrInput())
    {
        input.LoadImage(imagePath, dateZone);
        invoice.Date = ParseDate(ocr.Read(input).Text);
    }

    using (var input = new OcrInput())
    {
        input.LoadImage(imagePath, totalZone);
        invoice.Total = ParseCurrency(ocr.Read(input).Text);
    }

    return invoice;
}
```

### Phase 5: Async to Sync Migration

Azure Computer Vision is async-first due to network I/O. IronOCR processes locally, so sync is often preferred.

**Azure async pattern:**
```csharp
public async Task ProcessDocumentsAsync(string[] paths)
{
    var tasks = paths.Select(p => ProcessDocumentAsync(p));
    await Task.WhenAll(tasks);
}
```

**IronOCR sync (often faster):**
```csharp
public void ProcessDocuments(string[] paths)
{
    // Parallel processing without async overhead
    Parallel.ForEach(paths, path =>
    {
        var result = new IronTesseract().Read(path);
        SaveResult(path, result);
    });
}

// Or batch input for internal optimization
public void ProcessDocumentsBatch(string[] paths)
{
    var ocr = new IronTesseract();

    using var input = new OcrInput();
    foreach (var path in paths)
        input.LoadImage(path);

    var result = ocr.Read(input);
    // Process result.Pages
}
```

### Phase 6: Error Handling Migration

#### Remove Azure Error Patterns

```csharp
try
{
    var result = await _client.AnalyzeAsync(data, VisualFeatures.Read);
}
catch (RequestFailedException ex) when (ex.Status == 429)
{
    // Rate limit - retry logic
}
catch (RequestFailedException ex) when (ex.Status >= 500)
{
    // Azure service error
}
catch (AuthenticationFailedException)
{
    // Credential error
}
```

#### Add IronOCR Error Patterns

```csharp
try
{
    var result = _ocr.Read(imagePath);

    // Check for quality issues
    if (result.Confidence < 50)
    {
        _logger.LogWarning("Low confidence OCR result: {Confidence}%", result.Confidence);
    }
}
catch (IronOcr.Exceptions.IronOcrInputException ex)
{
    // Invalid input (corrupted image, unsupported format)
    _logger.LogError(ex, "Invalid input: {Path}", imagePath);
}
catch (IronOcr.Exceptions.IronOcrLicenseException ex)
{
    // License issue
    _logger.LogError(ex, "License error - check IronOCR license");
}
```

### Phase 7: Deployment Updates

#### Simplified IronOCR Deployment

**Docker example:**

```dockerfile
# Before: Azure dependencies
FROM mcr.microsoft.com/dotnet/aspnet:8.0
# Required Azure SDK configuration
# Required network access to Azure endpoints

# After: Self-contained
FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN apt-get update && apt-get install -y libgdiplus
WORKDIR /app
COPY --from=build /app/publish .
ENV IRONOCR_LICENSE=your-key
ENTRYPOINT ["dotnet", "YourApp.dll"]
```

**Benefits:**
- No outbound network requirements
- Works in air-gapped environments
- Simpler container image
- No Azure connectivity testing needed

### What You Gain from Migration

1. **Complete data sovereignty** - Documents never leave your infrastructure
2. **Simplified compliance** - No third-party processor to audit
3. **Cost predictability** - One-time license vs. per-transaction
4. **No internet dependency** - Works in air-gapped environments
5. **Faster processing** - No network latency
6. **Reduced attack surface** - No cloud API credentials to protect

### Migration Checklist

#### Pre-Migration
- [ ] Document current Azure Computer Vision usage
- [ ] Capture baseline results for test documents
- [ ] Identify custom models or Form Recognizer usage
- [ ] Calculate cost savings projections
- [ ] Obtain IronOCR license

#### Migration
- [ ] Remove Azure NuGet packages
- [ ] Add IronOCR package
- [ ] Update configuration (remove Azure, add IronOCR)
- [ ] Migrate OCR service code
- [ ] Convert async patterns if needed
- [ ] Update error handling
- [ ] Migrate structured extraction logic

#### Post-Migration
- [ ] Run accuracy validation tests
- [ ] Run performance benchmarks
- [ ] Update deployment configurations
- [ ] Remove Azure resources (cost savings)
- [ ] Update monitoring and logging
- [ ] Update documentation

### Common Migration Issues

#### Issue 1: Missing Language Support

**Problem:** Azure supported a specific language, IronOCR default package doesn't include it.

**Solution:**
```bash
dotnet add package IronOcr.Languages.Arabic
dotnet add package IronOcr.Languages.Japanese
```

#### Issue 2: Different Confidence Scales

**Problem:** Azure returns confidence 0-1, IronOCR returns 0-100.

**Solution:**
```csharp
// If your code expects 0-1 scale:
double normalizedConfidence = result.Confidence / 100.0;
```

#### Issue 3: Custom Model Training

**Problem:** Azure Form Recognizer custom models don't have direct equivalent.

**Solution:** Use zone-based extraction and pattern matching:
```csharp
// Instead of custom model, define extraction zones
var zones = new Dictionary<string, CropRectangle>
{
    ["FieldA"] = new CropRectangle(x, y, width, height),
    ["FieldB"] = new CropRectangle(x2, y2, width2, height2)
};

foreach (var (field, zone) in zones)
{
    using var input = new OcrInput();
    input.LoadImage(imagePath, zone);
    extractedData[field] = ocr.Read(input).Text;
}
```

## Additional Resources

- [Azure Computer Vision Documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/computer-vision/)
- [IronOCR on NuGet](https://www.nuget.org/packages/IronOcr) - On-premise alternative
- [IronOCR Documentation](https://ironsoftware.com/csharp/ocr/docs/)
- [HIPAA and Cloud Computing Guidance](https://www.hhs.gov/hipaa/for-professionals/special-topics/cloud-computing/index.html)
- [NIST 800-171 Requirements](https://csrc.nist.gov/publications/detail/sp/800-171/rev-2/final)

**Compare Other Cloud OCR Services:**
- [AWS Textract](../aws-textract/) - Amazon's document analysis service
- [Google Cloud Vision](../google-cloud-vision/) - Google's OCR API
- [OCR.space](../ocrspace/) - Freemium cloud OCR (no enterprise SLA)

---

*Last verified: January 2026*
