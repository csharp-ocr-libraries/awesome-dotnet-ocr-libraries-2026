# GdPicture.NET OCR for .NET: Complete Developer Guide (2026)

*By [Jacob Mellor](https://ironsoftware.com/about-us/authors/jacobmellor/), CTO of Iron Software*

GdPicture.NET is a comprehensive document imaging SDK from ORPALIS that includes OCR among its extensive feature set. Having worked with document imaging SDKs for over a decade, I've found that GdPicture bundles OCR with PDF manipulation, barcode processing, image processing, and document management capabilities—offering broad functionality but introducing complexity for teams focused primarily on OCR.

## Table of Contents

1. [GdPicture.NET Platform Overview](#gdpicturenet-platform-overview)
2. [Plugin Architecture](#plugin-architecture)
3. [Pricing Analysis](#pricing-analysis)
4. [Image ID Management](#image-id-management)
5. [Resource Folder Configuration](#resource-folder-configuration)
6. [Version Namespace Issue](#version-namespace-issue)
7. [Technical Implementation](#technical-implementation)
8. [GdPicture vs IronOCR Comparison](#gdpicture-vs-ironocr-comparison)
9. [Performance Comparison](#performance-comparison)
10. [Migration Guide: GdPicture.NET to IronOCR](#migration-guide-gdpicturenet-to-ironocr)
11. [When to Use Each Option](#when-to-use-each-option)
12. [Code Examples](#code-examples)

---

## GdPicture.NET Platform Overview

### Platform Capabilities

GdPicture.NET positions itself as an all-in-one document SDK spanning multiple domains:

- **OCR** - Text extraction from images and scanned PDFs using Tesseract-based engine
- **PDF** - Creation, editing, rendering, manipulation, and digital signatures
- **Image processing** - Filters, transformations, format conversion, color management
- **Barcode** - Reading and generation (1D and 2D formats including QR, DataMatrix)
- **Document cleanup** - Deskew, despeckle, border removal, hole punch removal
- **Annotations** - Markup, review features, redaction capabilities
- **TWAIN/WIA scanning** - Direct scanner integration for document capture
- **DICOM support** - Medical imaging format processing
- **Document archiving** - PDF/A compliance and long-term preservation

### Module Breakdown

The SDK is organized into distinct modules, each with its own initialization requirements:

| Module | Primary Class | Purpose |
|--------|---------------|---------|
| Core Imaging | GdPictureImaging | Image loading, processing, format conversion |
| PDF Engine | GdPicturePDF | PDF creation, editing, rendering |
| OCR Engine | GdPictureOCR | Text recognition and extraction |
| 1D Barcode | GdPicture1DBarcode | Linear barcode reading/writing |
| 2D Barcode | GdPicture2DBarcode | 2D barcode reading/writing |
| Annotations | GdPictureAnnotations | Document markup and review |
| Document Imaging | GdPictureDocumentImaging | Advanced cleanup filters |

**Key consideration:** Even for basic OCR operations, you must instantiate multiple components (GdPictureImaging + GdPictureOCR) and manage their lifecycle separately.

### Architecture Considerations

GdPicture.NET uses a component-based architecture where operations flow through multiple classes:

```
Application
├── LicenseManager (required first)
├── GdPictureImaging (image loading/processing)
│   └── Image IDs (integer handles)
├── GdPicturePDF (PDF operations)
│   └── Image IDs from rendered pages
└── GdPictureOCR (OCR processing)
    └── Consumes Image IDs
```

This architecture differs from single-class OCR libraries. Developers must understand the relationship between components and properly manage resources flowing between them.

---

## Plugin Architecture

### Understanding Plugin-Based Pricing

GdPicture's most distinctive characteristic is its plugin-based architecture. Rather than licensing the entire SDK, you license individual capabilities as plugins that add to a core base license.

The plugin model works as follows:

```
Base GdPicture License (required)
├── OCR Plugin (additional cost)
├── PDF Plugin (additional cost)
├── Barcode 1D Plugin (additional cost)
├── Barcode 2D Plugin (additional cost)
├── Document Imaging Plugin (additional cost)
└── DICOM Plugin (additional cost)
```

### Plugin Dependencies

Some plugins require others:

| If you need... | You must license... |
|----------------|---------------------|
| OCR from images | Base + OCR Plugin |
| OCR from PDFs | Base + OCR Plugin + PDF Plugin |
| Searchable PDF creation | Base + OCR Plugin + PDF Plugin |
| Barcode recognition | Base + Barcode Plugin(s) |
| Document cleanup before OCR | Base + OCR Plugin + Document Imaging Plugin |

### Complexity for OCR-Only Use Cases

For teams that only need OCR functionality, the plugin architecture introduces several challenges:

1. **Multiple license keys** - Managing licenses for base + plugins
2. **Deployment overhead** - Including DLLs for modules you don't use
3. **Configuration complexity** - Setting up resource paths for each plugin
4. **Cost escalation** - Simple OCR requires purchasing components designed for full document imaging workflows

---

## Pricing Analysis

### License Structure

GdPicture uses traditional perpetual SDK licensing with annual maintenance:

| License Type | Description | Typical Base Cost |
|--------------|-------------|-------------------|
| Evaluation | 60-day trial, all features | Free |
| Standard | Desktop applications, 1 developer | ~$2,000+ |
| Professional | Server deployment, 1 developer | ~$4,000+ |
| Enterprise | Site license, multiple developers | ~$10,000+ |

*Pricing as of January 2026. Visit [GdPicture pricing page](https://www.gdpicture.com/pricing/) for current rates.*

### Plugin-Based Cost Breakdown

The total cost depends on which plugins you need:

| Component | Standard | Professional | Enterprise |
|-----------|----------|--------------|------------|
| Core GdPicture | ~$2,000 | ~$4,000 | ~$10,000 |
| OCR Plugin | ~$1,000 | ~$2,000 | ~$4,000 |
| PDF Plugin | ~$1,000 | ~$2,000 | ~$4,000 |
| Document Imaging | ~$500 | ~$1,000 | ~$2,000 |
| Annual Maintenance | ~20% of total | ~20% of total | ~20% of total |

### Cost Comparison: OCR Use Case

**Scenario:** Server application that needs to extract text from images and PDFs, create searchable PDFs.

**GdPicture.NET estimated costs:**
```
Professional Core License: ~$4,000
OCR Plugin: ~$2,000
PDF Plugin: ~$2,000 (needed for PDF input/output)
Subtotal: ~$8,000

Annual Maintenance (~20%): ~$1,600/year
3-year total: ~$8,000 + ($1,600 × 3) = ~$12,800

5-year total: ~$8,000 + ($1,600 × 5) = ~$16,000
```

**IronOCR estimated costs:**
```
Professional License: $2,999 (one-time)
Optional annual updates: $1,499/year

3-year total (with updates): ~$5,997
5-year total (with updates): ~$8,995

3-year total (perpetual, no updates): $2,999
5-year total (perpetual, no updates): $2,999
```

**Cost difference over 5 years: $7,000-$13,000** depending on update preferences.

### What You Pay For That You May Not Use

The plugin architecture means OCR-focused teams often pay for capabilities they don't need:

| GdPicture Capability | Relevant for OCR? |
|----------------------|-------------------|
| TWAIN/WIA scanning | No (most modern apps use file input) |
| Annotation system | No |
| Advanced PDF editing | Often no (just need OCR) |
| Barcode modules | Usually separate use case |
| DICOM support | Medical specialty only |

---

## Image ID Management

### The Integer ID Pattern

GdPicture.NET uses integer IDs to track images in memory. This is fundamentally different from object-oriented resource management in modern .NET:

```csharp
// GdPicture pattern
int imageId = imaging.CreateGdPictureImageFromFile(path);

// imageId is an integer handle, not an object
// You must track this ID and release it explicitly
```

### Memory Leak Risks

Every image loaded into GdPicture returns an integer ID that **must** be released:

```csharp
// CORRECT: With explicit cleanup
int imageId = imaging.CreateGdPictureImageFromFile(path);
try
{
    // Use the image...
}
finally
{
    imaging.ReleaseGdPictureImage(imageId); // CRITICAL
}

// WRONG: Missing cleanup - memory leak
int imageId = imaging.CreateGdPictureImageFromFile(path);
// Use the image...
// Forgot to call ReleaseGdPictureImage - memory leak!
```

### Tracking IDs Across Operations

When processing multiple images or PDF pages, ID tracking becomes complex:

```csharp
var imageIds = new List<int>();

try
{
    for (int i = 1; i <= pageCount; i++)
    {
        pdf.SelectPage(i);
        int pageImageId = pdf.RenderPageToGdPictureImage(200, false);

        if (pageImageId != 0)
        {
            imageIds.Add(pageImageId);
            // Process page...
        }
    }
}
finally
{
    // Must release ALL collected IDs
    foreach (var id in imageIds)
    {
        imaging.ReleaseGdPictureImage(id);
    }
}
```

### Contrast with Standard .NET Patterns

Modern .NET uses `IDisposable` and `using` statements for resource management:

```csharp
// IronOCR - Standard .NET pattern
using var input = new OcrInput();
input.LoadImage(imagePath);
var result = new IronTesseract().Read(input);
// Automatic cleanup via Dispose()
```

The `using` statement ensures resources are released even if exceptions occur. GdPicture's integer ID system predates these patterns and requires manual tracking.

---

## Resource Folder Configuration

### OCR Resource Files Requirement

GdPicture's OCR engine requires external resource files (tessdata-style language files):

```csharp
var ocr = new GdPictureOCR();
ocr.ResourceFolder = @"C:\GdPicture\OCR\Resources"; // REQUIRED

// Without this, OCR fails silently or with cryptic errors
```

### Deployment Considerations

The resource folder must be deployed alongside your application:

```
YourApplication/
├── bin/
│   ├── YourApp.exe
│   └── GdPicture14.dll
└── Resources/
    └── OCR/
        ├── eng.traineddata    (~15MB for English)
        ├── fra.traineddata    (~15MB for French)
        └── ... (more languages)
```

### Common Resource Path Issues

1. **Path doesn't exist at runtime** - Works in development, fails in production
2. **Relative vs absolute paths** - Confusion about working directory
3. **Missing language files** - OCR fails for unsupported languages
4. **File permissions** - Service accounts can't read resource folder

### Contrast with Bundled Resources

IronOCR bundles English language support directly in the NuGet package:

```csharp
// IronOCR - No resource folder needed
var result = new IronTesseract().Read("document.png");
// English language support is embedded

// Additional languages via NuGet packages
// Install-Package IronOcr.Languages.French
```

---

## Version Namespace Issue

### Namespace Contains Version Number

GdPicture includes the major version number in its namespace:

```csharp
using GdPicture14;  // Version 14
// or
using GdPicture13;  // Version 13
```

### Migration Impact

When upgrading GdPicture versions, you must update all namespace references:

```csharp
// Before upgrade (v14)
using GdPicture14;

var imaging = new GdPictureImaging();
var ocr = new GdPictureOCR();

// After upgrade (v15)
using GdPicture15;  // MUST change

var imaging = new GdPictureImaging();  // Same class name
var ocr = new GdPictureOCR();          // Same class name
```

This means:
- Every source file with GdPicture code needs modification
- Find-and-replace across entire codebase
- Build breaks if any file is missed
- Cannot have different parts of app on different versions

### Industry Standard Approach

Most modern .NET libraries use version-agnostic namespaces:

```csharp
using IronOcr;  // Same namespace across all versions
using Newtonsoft.Json;  // Same namespace across all versions
using Microsoft.EntityFrameworkCore;  // Same namespace
```

Version changes are handled through NuGet package versions, not namespace changes.

---

## Technical Implementation

### Basic Setup

```csharp
using GdPicture14; // Version number in namespace

public class GdPictureOcrService
{
    private GdPictureImaging _imaging;
    private GdPictureOCR _ocr;

    public GdPictureOcrService()
    {
        // Step 1: Register license key
        LicenseManager lm = new LicenseManager();
        lm.RegisterKEY("YOUR-LICENSE-KEY");

        // Step 2: Initialize imaging component
        _imaging = new GdPictureImaging();

        // Step 3: Initialize OCR component
        _ocr = new GdPictureOCR();

        // Step 4: Set OCR resource path
        _ocr.ResourceFolder = @"C:\GdPicture\OCR\Resources";
    }

    public string ExtractText(string imagePath)
    {
        // Load image into imaging component
        int imageId = _imaging.CreateGdPictureImageFromFile(imagePath);

        if (imageId == 0)
        {
            throw new Exception($"Failed to load image: {_imaging.GetStat()}");
        }

        try
        {
            // Set image source for OCR
            _ocr.SetImage(imageId);

            // Set language
            _ocr.Language = "eng";

            // Run OCR
            string resultId = _ocr.RunOCR();

            if (string.IsNullOrEmpty(resultId))
            {
                throw new Exception($"OCR failed: {_ocr.GetStat()}");
            }

            // Get text from result
            return _ocr.GetOCRResultText(resultId);
        }
        finally
        {
            // MUST release image resources
            _imaging.ReleaseGdPictureImage(imageId);
        }
    }

    public void Dispose()
    {
        _ocr?.Dispose();
        _imaging?.Dispose();
    }
}
```

### PDF OCR Processing

```csharp
public string ExtractFromPdf(string pdfPath)
{
    var text = new StringBuilder();

    using var pdfGdPicture = new GdPicturePDF();

    // Load PDF
    var status = pdfGdPicture.LoadFromFile(pdfPath, false);
    if (status != GdPictureStatus.OK)
    {
        throw new Exception($"Failed to load PDF: {status}");
    }

    int pageCount = pdfGdPicture.GetPageCount();

    for (int i = 1; i <= pageCount; i++)
    {
        // Select page
        pdfGdPicture.SelectPage(i);

        // Render page to image
        int imageId = pdfGdPicture.RenderPageToGdPictureImage(200, false);

        if (imageId != 0)
        {
            try
            {
                _ocr.SetImage(imageId);
                _ocr.Language = "eng";

                string resultId = _ocr.RunOCR();

                if (!string.IsNullOrEmpty(resultId))
                {
                    text.AppendLine(_ocr.GetOCRResultText(resultId));
                }
            }
            finally
            {
                _imaging.ReleaseGdPictureImage(imageId);
            }
        }
    }

    return text.ToString();
}
```

### Creating Searchable PDFs

```csharp
public void CreateSearchablePdf(string inputPdf, string outputPdf)
{
    using var pdfGdPicture = new GdPicturePDF();

    pdfGdPicture.LoadFromFile(inputPdf, false);

    // OCR each page and add text layer
    int pageCount = pdfGdPicture.GetPageCount();

    for (int i = 1; i <= pageCount; i++)
    {
        pdfGdPicture.SelectPage(i);
        pdfGdPicture.OcrPage("eng", @"C:\GdPicture\OCR\Resources", "", 200);
    }

    pdfGdPicture.SaveToFile(outputPdf, true);
}
```

---

## GdPicture vs IronOCR Comparison

### API Complexity

**GdPicture basic OCR:**
```csharp
// Initialize components
var lm = new LicenseManager();
lm.RegisterKEY(key);
var imaging = new GdPictureImaging();
var ocr = new GdPictureOCR();
ocr.ResourceFolder = resourcePath;

// Load and process
int imageId = imaging.CreateGdPictureImageFromFile(path);
ocr.SetImage(imageId);
ocr.Language = "eng";
string resultId = ocr.RunOCR();
string text = ocr.GetOCRResultText(resultId);

// Cleanup
imaging.ReleaseGdPictureImage(imageId);
```

**IronOCR equivalent:**
```csharp
// NuGet: Install-Package IronOcr
var text = new IronTesseract().Read(imagePath).Text;
```

### Feature Comparison

| Feature | GdPicture.NET | IronOCR |
|---------|---------------|---------|
| Setup complexity | High (resources, license, multiple components) | Minimal (single NuGet) |
| PDF support | Via GdPicturePDF (separate plugin) | Native (built-in) |
| Password PDFs | Yes (with PDF plugin) | Built-in |
| Preprocessing | Manual API calls | Automatic |
| Searchable PDF | Via OcrPage method | Built-in method |
| Resource files | Required (tessdata-like) | Bundled in NuGet |
| NuGet | Yes | Yes |
| On-premise | Yes | Yes |
| Thread safety | Manual instance management | Thread-safe by design |

### Resource Management Comparison

**GdPicture (manual ID management):**
```csharp
// Every image must be explicitly released
int imageId = imaging.CreateGdPictureImageFromFile(path);
try
{
    // Use image...
}
finally
{
    imaging.ReleaseGdPictureImage(imageId); // CRITICAL
}
```

**IronOCR (standard .NET patterns):**
```csharp
// NuGet: Install-Package IronOcr
using var input = new OcrInput();
input.LoadImage(path);
var result = new IronTesseract().Read(input);
// Automatic disposal with using statement
```

---

## Performance Comparison

### Initialization Time

| Scenario | GdPicture.NET | IronOCR |
|----------|---------------|---------|
| First initialization | ~500-800ms | ~100-200ms |
| Subsequent operations | ~50ms | ~30ms |
| Cold start (serverless) | ~1.5s | ~500ms |

GdPicture's longer initialization comes from loading multiple components (imaging + OCR + resources).

### Processing Speed

| Scenario | GdPicture.NET | IronOCR |
|----------|---------------|---------|
| Single image (300 DPI) | 400-700ms | 300-500ms |
| PDF page (rendered 200 DPI) | 500-900ms | 350-600ms |
| 10-page PDF | 5-9 seconds | 3-6 seconds |
| 100-page document | 50-90 seconds | 30-60 seconds |

### Memory Footprint

| Scenario | GdPicture.NET | IronOCR |
|----------|---------------|---------|
| Idle (initialized) | ~150MB | ~80MB |
| Single image processing | ~250MB peak | ~150MB peak |
| PDF (10 pages) | ~400MB peak | ~250MB peak |
| Batch (50 images) | ~800MB+ (with proper cleanup) | ~400MB |

GdPicture's higher memory usage reflects its comprehensive imaging toolkit loading modules you may not use.

### Memory Leak Potential

With GdPicture's ID-based system, forgetting cleanup calls causes memory leaks:

```csharp
// Memory leak scenario
for (int i = 0; i < 1000; i++)
{
    int imageId = imaging.CreateGdPictureImageFromFile(files[i]);
    ocr.SetImage(imageId);
    string resultId = ocr.RunOCR();
    results.Add(ocr.GetOCRResultText(resultId));
    // FORGOT: imaging.ReleaseGdPictureImage(imageId);
}
// Memory grows unbounded until process crashes
```

---

## Migration Guide: GdPicture.NET to IronOCR

### Why Migrate?

Developers migrate from GdPicture.NET to IronOCR for several reasons:

| Pain Point | GdPicture.NET | IronOCR Solution |
|------------|---------------|------------------|
| Plugin cost overhead | Pay for modules you don't use | Single package, all OCR features |
| Image ID management | Manual tracking, memory leak risk | Standard .NET disposal |
| Namespace versioning | Code changes on upgrades | Stable namespace |
| Resource folder setup | External files, deployment hassle | Bundled resources |
| API verbosity | 10+ lines for basic OCR | 1 line for basic OCR |

### Migration Complexity Assessment

| Complexity | Effort | When |
|------------|--------|------|
| Simple | 2-4 hours | Basic OCR only, few files |
| Medium | 4-8 hours | PDF processing, batch operations |
| Complex | 1-2 days | Custom preprocessing, high-volume |

### Package Changes

**Remove GdPicture packages:**
```bash
# Remove from your project
dotnet remove package GdPicture.NET
dotnet remove package GdPicture.NET.Imaging
dotnet remove package GdPicture.NET.PDF
dotnet remove package GdPicture.NET.OCR
```

**Add IronOCR:**
```bash
# Add IronOCR
dotnet add package IronOcr

# Optional: Additional languages
dotnet add package IronOcr.Languages.French
dotnet add package IronOcr.Languages.German
```

### Resource Folder Removal

**GdPicture requires:**
```
YourProject/
└── Resources/
    └── OCR/
        ├── eng.traineddata
        └── ... (other languages)
```

**IronOCR removes this need:**
- Delete the OCR resources folder
- Remove resource path configuration
- Languages bundled in NuGet or available as separate packages

### API Mapping Reference

| GdPicture.NET | IronOCR | Notes |
|---------------|---------|-------|
| `LicenseManager.RegisterKEY()` | `IronOcr.License.LicenseKey` | Single property |
| `GdPictureImaging` | Not needed | Handled internally |
| `GdPictureOCR` | `IronTesseract` | Main OCR class |
| `GdPicturePDF` | `OcrInput.LoadPdf()` | PDF loading built-in |
| `CreateGdPictureImageFromFile()` | `new OcrInput(path)` | Direct loading |
| `ReleaseGdPictureImage()` | `using` statement | Automatic disposal |
| `SetImage()` | `OcrInput.LoadImage()` | Fluent API |
| `RunOCR()` | `IronTesseract.Read()` | Returns result directly |
| `GetOCRResultText()` | `OcrResult.Text` | Property access |
| `GetOCRResultConfidence()` | `OcrResult.Confidence` | Property access |
| `GdPictureStatus` | Exceptions | Standard error handling |
| `ocr.ResourceFolder` | Not needed | Bundled resources |

### Image ID Management Migration

**Before (GdPicture - manual tracking):**
```csharp
public string ProcessImages(string[] paths)
{
    var results = new StringBuilder();
    var imageIds = new List<int>();

    try
    {
        foreach (var path in paths)
        {
            int imageId = _imaging.CreateGdPictureImageFromFile(path);
            if (imageId != 0)
            {
                imageIds.Add(imageId);
                _ocr.SetImage(imageId);
                _ocr.Language = "eng";
                var resultId = _ocr.RunOCR();
                results.AppendLine(_ocr.GetOCRResultText(resultId));
            }
        }
    }
    finally
    {
        foreach (var id in imageIds)
        {
            _imaging.ReleaseGdPictureImage(id);
        }
    }

    return results.ToString();
}
```

**After (IronOCR - automatic disposal):**
```csharp
// NuGet: Install-Package IronOcr
public string ProcessImages(string[] paths)
{
    var ocr = new IronTesseract();
    using var input = new OcrInput();

    foreach (var path in paths)
    {
        input.LoadImage(path);
    }

    return ocr.Read(input).Text;
}
```

### Step-by-Step Migration

**Step 1: Replace namespace imports**
```csharp
// Remove
using GdPicture14;

// Add
using IronOcr;
```

**Step 2: Remove component initialization**
```csharp
// Remove
LicenseManager lm = new LicenseManager();
lm.RegisterKEY("GDPICTURE-KEY");
_imaging = new GdPictureImaging();
_ocr = new GdPictureOCR();
_ocr.ResourceFolder = @"C:\Resources";

// Replace with
IronOcr.License.LicenseKey = "IRONOCR-KEY";
var ocr = new IronTesseract();
```

**Step 3: Simplify image loading**
```csharp
// Remove
int imageId = imaging.CreateGdPictureImageFromFile(path);
if (imageId == 0) throw new Exception(imaging.GetStat().ToString());

// Replace with
using var input = new OcrInput(path);
```

**Step 4: Simplify OCR execution**
```csharp
// Remove
_ocr.SetImage(imageId);
_ocr.Language = "eng";
string resultId = _ocr.RunOCR();
if (string.IsNullOrEmpty(resultId)) throw new Exception(_ocr.GetStat().ToString());
string text = _ocr.GetOCRResultText(resultId);

// Replace with
var result = ocr.Read(input);
string text = result.Text;
```

**Step 5: Remove explicit cleanup**
```csharp
// Remove
_imaging.ReleaseGdPictureImage(imageId);

// Not needed - using statement handles disposal
```

### Common Migration Issues

**Issue 1: Image ID tracking in loops**

GdPicture requires collecting all IDs for cleanup. IronOCR handles this automatically:

```csharp
// GdPicture - must track every ID
var ids = new List<int>();
foreach (var file in files)
{
    ids.Add(_imaging.CreateGdPictureImageFromFile(file));
}
// ... process ...
foreach (var id in ids) _imaging.ReleaseGdPictureImage(id);

// IronOCR - automatic
using var input = new OcrInput();
foreach (var file in files)
{
    input.LoadImage(file);
}
var result = ocr.Read(input);
```

**Issue 2: Status code checking vs exceptions**

GdPicture returns status codes; IronOCR throws exceptions:

```csharp
// GdPicture pattern
var status = pdf.LoadFromFile(path, false);
if (status != GdPictureStatus.OK)
{
    // Handle error
}

// IronOCR pattern
try
{
    using var input = new OcrInput();
    input.LoadPdf(path);
}
catch (Exception ex)
{
    // Standard .NET exception handling
}
```

**Issue 3: Confidence score access**

```csharp
// GdPicture
float confidence = _ocr.GetOCRResultConfidence(resultId);

// IronOCR
double confidence = result.Confidence;
// Or per-word: result.Words.Select(w => w.Confidence)
```

### Migration Checklist

- [ ] Backup existing codebase
- [ ] Remove GdPicture NuGet packages
- [ ] Add IronOcr NuGet package
- [ ] Update namespace imports (GdPicture14 to IronOcr)
- [ ] Remove LicenseManager, GdPictureImaging, GdPictureOCR initialization
- [ ] Add IronOcr.License.LicenseKey configuration
- [ ] Replace CreateGdPictureImageFromFile with OcrInput
- [ ] Replace RunOCR/GetOCRResultText with Read().Text
- [ ] Remove all ReleaseGdPictureImage calls
- [ ] Delete OCR resource folder
- [ ] Update error handling (status codes to exceptions)
- [ ] Test with production document samples
- [ ] Verify memory usage in long-running scenarios

---

## When to Use Each Option

### Choose GdPicture.NET When

**GdPicture makes sense if:**

- You need the **full document imaging toolkit** (scanning, annotation, PDF editing, barcode)
- **TWAIN/WIA scanner integration** is a core requirement
- You're building a **document management system** needing all features
- **DICOM support** (medical imaging) is required
- Your organization has an **existing GdPicture license** covering other features
- You need **advanced PDF editing** beyond just OCR text extraction

### Choose IronOCR When

**IronOCR is better if:**

- **OCR is your primary focus** and you don't need the full imaging toolkit
- You value **API simplicity** and reduced code complexity
- **Lower total cost of ownership** is important
- **Deployment simplicity** matters (no resource folders to manage)
- You want **standard .NET patterns** (using statements, exceptions)
- **Thread safety** out of the box is needed
- You're building a **microservice** or serverless function focused on OCR

### Decision Matrix

| Requirement | GdPicture.NET | IronOCR |
|-------------|---------------|---------|
| OCR only | Overkill | Better fit |
| OCR + PDF text extraction | Adequate | Better fit |
| OCR + full PDF editing | Good fit | PDF features limited |
| OCR + scanner integration | Good fit | No scanning |
| OCR + barcode in same workflow | Good fit | Separate library needed |
| Budget-constrained project | Higher cost | Lower cost |
| Rapid development | Complex API | Simple API |
| Legacy system integration | Depends | Modern patterns |

---

## Code Examples

Explore detailed code comparisons in the accompanying files:

- [GdPicture vs IronOCR Examples](./gdpicture-vs-ironocr-examples.cs) - Side-by-side comparison of basic operations
- [Migration Code Examples](./gdpicture-migration-examples.cs) - Before/after patterns for common scenarios
- [PDF OCR Processing](./gdpicture-pdf-ocr.cs) - PDF-specific comparisons with image ID management

---

## Security Considerations

Both are on-premise solutions with full data sovereignty:

| Aspect | GdPicture.NET | IronOCR |
|--------|---------------|---------|
| On-premise | Yes | Yes |
| Air-gapped | Yes | Yes |
| Cloud dependency | None | None |
| HIPAA suitable | Yes | Yes |
| Government deployable | Yes | Yes |

**For security, both are valid choices.** The decision comes down to complexity and feature requirements.

---

## Common GdPicture Pain Points

### 1. Resource Path Configuration

The OCR engine requires resource files:
- Must set `ResourceFolder` property
- Path must contain language data files
- Deployment must include these files
- Path issues cause silent failures

### 2. Image ID Management

Every operation uses integer image IDs:
- IDs must be tracked and released
- Leaks cause memory exhaustion
- No automatic disposal
- Error-prone manual management

### 3. Status Checking

Every operation requires status check:
```csharp
var status = pdfDoc.LoadFromFile(path, false);
if (status != GdPictureStatus.OK)
{
    // Handle error - but what went wrong?
}
```

Error messages are often generic, requiring documentation lookup.

### 4. Version Namespaces

GdPicture uses version-specific namespaces:
```csharp
using GdPicture14;  // Version 14
// Upgrading means changing all namespaces
```

---

## Conclusion

GdPicture.NET is a comprehensive document imaging platform with capable OCR functionality. For organizations needing PDF manipulation, scanning integration, barcode processing, and OCR in a single SDK, it offers consolidated functionality.

However, for development teams focused primarily on OCR:

- **Plugin cost overhead** - Pay for modules designed for full imaging workflows
- **Complexity overhead** - Multiple components, resource paths, image ID management
- **API verbosity** - Many steps for simple operations
- **Resource management** - Manual cleanup required, memory leak potential
- **Version namespaces** - Code changes required on SDK upgrades

IronOCR provides a focused alternative: one package, simple API, automatic resource management, and a pricing model aligned with pure OCR needs. For teams where OCR is the primary requirement, this focused approach typically delivers better developer experience with lower total cost of ownership.

---

## Resources

- [IronOCR Documentation](https://ironsoftware.com/csharp/ocr/docs/)
- [IronOCR Tutorials](https://ironsoftware.com/csharp/ocr/tutorials/)
- [Free Trial](https://ironsoftware.com/csharp/ocr/docs/license/trial/)
- [GdPicture.NET Documentation](https://www.gdpicture.com/guides/gdpicture/)

---

*Last verified: January 2026*
