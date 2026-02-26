# Migrating from XImage.OCR to IronOCR

This guide is for .NET developers who are moving an existing XImage.OCR integration to [IronOCR](https://ironsoftware.com/csharp/ocr/). It covers the package consolidation process, namespace and API changes, and concrete code migration examples for the scenarios where XImage.OCR's fragmented architecture creates the most friction. No prior reading of the comparison article is required.

## Why Migrate from XImage.OCR

XImage.OCR is a commercial Tesseract wrapper from RasterEdge that distributes its functionality across a chain of coordinated NuGet packages. The architecture works at small scale but creates compounding maintenance costs as applications grow.

**Package Count Grows With Every Language.** Adding a language means adding a NuGet package. A five-language application carries six packages in its `.csproj`. A ten-language application carries eleven. Every package must be pinned to the same version as the core — a constraint that produces silent runtime failures when a developer updates only part of the chain. IronOCR delivers one package for all 125+ languages.

**Version Synchronization Is a Constant Risk.** `dotnet outdated` updates packages greedily. When `RasterEdge.XImage.OCR` advances to 12.5.0 but `XImage.OCR.Language.French` stays at 12.4.0, the error appears at runtime, not at build time, and the message rarely points to version sync as the cause. Teams running CI/CD pipelines learn to add explicit version pinning for every XImage.OCR package — overhead that serves no purpose beyond compensating for the fragmented model.

**No Built-In Preprocessing Caps Accuracy on Real Documents.** XImage.OCR passes images directly to the underlying Tesseract engine. A scan at 150 DPI with two degrees of skew goes into Tesseract unchanged. The accuracy ceiling on such input is 60–75% regardless of which Tesseract wrapper is in use. IronOCR ships a preprocessing pipeline — `Deskew()`, `DeNoise()`, `Contrast()`, `Binarize()`, `Sharpen()` — that corrects these problems before recognition runs.

**Structured Output Requires Manual Parsing.** XImage.OCR returns a plain string. Extracting word positions, line boundaries, or per-word confidence requires parsing that string yourself. IronOCR returns an `OcrResult` object with `Pages`, `Paragraphs`, `Lines`, `Words`, and per-character data with pixel coordinates and confidence scores built in.

**Output Formats Stop at Plain Text.** Writing a searchable PDF from an XImage.OCR result requires the RasterEdge PDF SDK — a second commercial purchase. IronOCR produces searchable PDFs via `result.SaveAsSearchablePdf()` with no additional dependencies.

**Cross-Platform Deployment Is Not Supported.** XImage.OCR targets Windows. Linux containers, macOS development environments, and cloud-native deployments on Azure or AWS require a different library. IronOCR runs on Windows, Linux, macOS, Docker, Azure App Service, and AWS Lambda from the same package.

### The Fundamental Problem

XImage.OCR requires one NuGet package per language. Ten languages means eleven packages, all version-locked to each other:

```xml
<!-- XImage.OCR: 11 packages to support 10 languages — every version must match -->
<PackageReference Include="RasterEdge.XImage.OCR" Version="12.4.0" />
<PackageReference Include="XImage.OCR.Language.English" Version="12.4.0" />
<PackageReference Include="XImage.OCR.Language.German" Version="12.4.0" />
<PackageReference Include="XImage.OCR.Language.French" Version="12.4.0" />
<PackageReference Include="XImage.OCR.Language.Spanish" Version="12.4.0" />
<PackageReference Include="XImage.OCR.Language.Italian" Version="12.4.0" />
<PackageReference Include="XImage.OCR.Language.Portuguese" Version="12.4.0" />
<PackageReference Include="XImage.OCR.Language.ChineseSimplified" Version="12.4.0" />
<PackageReference Include="XImage.OCR.Language.Japanese" Version="12.4.0" />
<PackageReference Include="XImage.OCR.Language.Korean" Version="12.4.0" />
<PackageReference Include="XImage.OCR.Language.Arabic" Version="12.4.0" />
```

IronOCR replaces the entire block with one line:

```xml
<!-- IronOCR: One package. 125+ languages. No version coordination. -->
<PackageReference Include="IronOcr" Version="2024.x.x" />
```

## IronOCR vs XImage.OCR: Feature Comparison

The table below covers the capabilities most relevant to the migration decision.

| Feature | XImage.OCR | IronOCR |
|---|---|---|
| **NuGet packages for English only** | 2 (core + language pack) | 1 |
| **NuGet packages for 10 languages** | 11 | 1 |
| **Version synchronization required** | Yes — all packages must match | No |
| **Languages available** | ~15 as separate packages | 125+ bundled |
| **Built-in preprocessing** | None | Deskew, DeNoise, Contrast, Binarize, Sharpen, Scale, Dilate, Erode, Invert |
| **Deep noise removal** | None | Yes (`DeepCleanBackgroundNoise()`) |
| **Native PDF input** | Requires RasterEdge PDF SDK | Yes (`input.LoadPdf()`) |
| **Searchable PDF output** | Requires RasterEdge PDF SDK | Yes (`result.SaveAsSearchablePdf()`) |
| **Multi-page TIFF input** | Limited | Yes (`input.LoadImageFrames()`) |
| **Byte array input** | Manual via MemoryStream | Yes (`input.LoadImage(bytes)`) |
| **Stream input** | Manual | Yes (`input.LoadImage(stream)`) |
| **Structured output** | Plain string | Pages, Paragraphs, Lines, Words, Characters with coordinates |
| **Per-word confidence scores** | Not available | Yes |
| **Barcode reading** | Not available | Yes (`ocr.Configuration.ReadBarCodes = true`) |
| **hOCR export** | Not available | Yes |
| **Thread safety** | Not thread-safe | Full thread safety |
| **Memory model (parallel)** | One handler instance per thread | Single shared instance |
| **Cross-platform** | Windows primarily | Windows, Linux, macOS, Docker, Azure, AWS |
| **.NET compatibility** | .NET Standard 2.0, .NET Framework 4.5+ | .NET Framework 4.6.2+, .NET Core, .NET 5/6/7/8/9 |
| **License type** | Commercial (RasterEdge) | Perpetual (Lite $749, Pro $1,499, Enterprise $2,999) |
| **Commercial support** | RasterEdge support | Yes, tiered by license |

## Quick Start: XImage.OCR to IronOCR Migration

### Step 1: Replace NuGet Packages

Remove all XImage.OCR packages. The number of commands matches the number of language packs you installed:

```bash
dotnet remove package RasterEdge.XImage.OCR
dotnet remove package XImage.OCR.Language.English
dotnet remove package XImage.OCR.Language.German
dotnet remove package XImage.OCR.Language.French
# Repeat for every language pack in your project
```

Install IronOCR from [NuGet](https://www.nuget.org/packages/IronOcr):

```bash
dotnet add package IronOcr
```

### Step 2: Update Namespaces

Replace the RasterEdge namespace imports with the single IronOCR namespace:

```csharp
// Before (XImage.OCR)
using RasterEdge.XImage.OCR;
using RasterEdge.Imaging.Basic;

// After (IronOCR)
using IronOcr;
```

### Step 3: Initialize License

Add license initialization once at application startup, before any OCR calls:

```csharp
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
```

Store the key in an environment variable or secrets manager rather than hardcoding it:

```csharp
IronOcr.License.LicenseKey = Environment.GetEnvironmentVariable("IRONOCR_LICENSE_KEY");
```

## Code Migration Examples

### Multi-Package Initialization Consolidation

The first migration task is collapsing the XImage.OCR initialization block — license activation, handler creation, and string-based language assignment — into the IronOCR equivalent.

**XImage.OCR Approach:**

```csharp
// Requires: RasterEdge.XImage.OCR + one XImage.OCR.Language.* package per language
// Language strings must exactly match installed package names or OCR fails at runtime

RasterEdge.XImage.OCR.License.LicenseManager.SetLicense("your-ximage-license-key");

var ocrHandler = new OCRHandler();

// String codes — typo "enh" instead of "eng" silently fails or throws at runtime
ocrHandler.Languages = new[] { "eng", "deu", "fra", "spa", "ita" };

// Process returns a plain string — no structure, no confidence
string extractedText = ocrHandler.Process("document.png");
Console.WriteLine(extractedText);
```

**IronOCR Approach:**

```csharp
// Requires: IronOcr (single package — all languages included)
IronOcr.License.LicenseKey = "YOUR-IRONOCR-LICENSE-KEY";

var ocr = new IronTesseract();

// Type-safe enum — compiler catches typos, no runtime surprises
ocr.Language = OcrLanguage.English + OcrLanguage.German +
               OcrLanguage.French + OcrLanguage.Spanish + OcrLanguage.Italian;

using var input = new OcrInput();
input.LoadImage("document.png");

var result = ocr.Read(input);
Console.WriteLine(result.Text);
Console.WriteLine($"Confidence: {result.Confidence}%");
```

The string-based language codes in XImage.OCR (`"eng"`, `"deu"`) fail at runtime when the corresponding NuGet package is absent or on the wrong version. The `OcrLanguage` enum in IronOCR makes invalid language combinations impossible to compile. The [IronTesseract setup guide](https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/) covers engine configuration options in full, and the [multiple languages how-to](https://ironsoftware.com/csharp/ocr/how-to/ocr-multiple-languages/) documents how primary and secondary language combinations work for mixed-language documents.

### Image Format Handling Unification

XImage.OCR handles each image source differently depending on format. Byte arrays, streams, and file paths each require slightly different code paths. IronOCR accepts all of them through the same `OcrInput` methods.

**XImage.OCR Approach:**

```csharp
// XImage.OCR: different handling per image source type
var ocrHandler = new OCRHandler();
ocrHandler.Language = "eng";

// File path — works directly
string resultFromFile = ocrHandler.Process("invoice.jpg");

// Byte array — must write to temp file first, then process
byte[] imageBytes = File.ReadAllBytes("invoice.jpg");
string tempPath = Path.GetTempFileName() + ".jpg";
File.WriteAllBytes(tempPath, imageBytes);
try
{
    string resultFromBytes = ocrHandler.Process(tempPath);
    Console.WriteLine(resultFromBytes);
}
finally
{
    File.Delete(tempPath);    // Manual cleanup — easy to forget
}

// Multi-page TIFF — must split frames manually
// No built-in TIFF frame iteration in base XImage.OCR
```

**IronOCR Approach:**

```csharp
// IronOCR: unified OcrInput accepts all source types identically
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

var ocr = new IronTesseract();

// File path
using (var input = new OcrInput())
{
    input.LoadImage("invoice.jpg");
    var result = ocr.Read(input);
    Console.WriteLine($"From file: {result.Text}");
}

// Byte array — no temp file needed
byte[] imageBytes = File.ReadAllBytes("invoice.jpg");
using (var input = new OcrInput())
{
    input.LoadImage(imageBytes);
    var result = ocr.Read(input);
    Console.WriteLine($"From bytes: {result.Text}");
}

// Multi-page TIFF — all frames processed in one call
using (var input = new OcrInput())
{
    input.LoadImageFrames("scanned-archive.tiff");
    var result = ocr.Read(input);
    Console.WriteLine($"TIFF pages: {result.Pages.Count}");
    foreach (var page in result.Pages)
        Console.WriteLine($"Page {page.PageNumber}: {page.Text}");
}
```

The temp-file pattern for byte arrays in XImage.OCR is a common source of disk bloat and leaked files in error paths. IronOCR's `LoadImage(byte[])` eliminates the intermediate file entirely. The [image input guide](https://ironsoftware.com/csharp/ocr/how-to/input-images/) and [TIFF/GIF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-tiff-gif/) cover every supported source type, including streams and multi-frame processing.

### Output Format Streamlining

XImage.OCR returns a plain string. Generating a searchable PDF requires a second RasterEdge product. IronOCR produces plain text, searchable PDFs, and structured data from the same result object with no additional packages.

**XImage.OCR Approach:**

```csharp
// XImage.OCR: plain text output only
// Searchable PDF requires purchasing the RasterEdge PDF SDK separately

var ocrHandler = new OCRHandler();
ocrHandler.Language = "eng";

string plainText = ocrHandler.Process("scanned-contract.jpg");

// To produce a searchable PDF from this text, you would need:
// 1. Purchase RasterEdge PDF SDK (separate commercial license)
// 2. Create a PDF document programmatically
// 3. Embed the extracted text as invisible text layer over the image
// 4. Manage the PDF document lifecycle manually
// No built-in path from OCR result to searchable PDF in XImage.OCR alone
Console.WriteLine(plainText);
```

**IronOCR Approach:**

```csharp
// IronOCR: plain text, searchable PDF, and structured data from one result
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

var ocr = new IronTesseract();

using var input = new OcrInput();
input.LoadImage("scanned-contract.jpg");

var result = ocr.Read(input);

// Plain text
Console.WriteLine(result.Text);

// Searchable PDF — no extra package required
result.SaveAsSearchablePdf("searchable-contract.pdf");

// Structured data: paragraphs with bounding box coordinates
foreach (var page in result.Pages)
{
    foreach (var paragraph in page.Paragraphs)
    {
        Console.WriteLine($"Paragraph at ({paragraph.X}, {paragraph.Y}): {paragraph.Text}");
    }
}

// Per-word confidence for quality gating
var lowConfidenceWords = result.Pages
    .SelectMany(p => p.Words)
    .Where(w => w.Confidence < 70)
    .ToList();

Console.WriteLine($"Words below 70% confidence: {lowConfidenceWords.Count}");
```

The `SaveAsSearchablePdf()` call embeds the recognized text as a hidden layer beneath the original image, making the document fully text-searchable without altering its visual appearance. The [searchable PDF how-to](https://ironsoftware.com/csharp/ocr/how-to/searchable-pdf/) covers page range options and DPI settings. For structured data extraction patterns, the [read results guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) documents the full `OcrResult` hierarchy including word coordinates and confidence access. The [searchable PDF example](https://ironsoftware.com/csharp/ocr/examples/make-pdf-searchable/) provides a complete working implementation.

### Batch Document Processing

XImage.OCR is not thread-safe. Each concurrent worker thread must create its own `OCRHandler` instance, multiplying memory consumption by the thread count. IronOCR uses a single shared instance across all threads.

**XImage.OCR Approach:**

```csharp
// XImage.OCR: one handler per thread — memory multiplies with concurrency
// 4 threads processing English documents: 4 x ~100MB = ~400MB for OCR alone
// 4 threads processing 5 languages: 4 x ~250MB = ~1GB just for OCR handlers

var results = new ConcurrentDictionary<string, string>();
string[] documentPaths = Directory.GetFiles("./incoming", "*.png");

Parallel.ForEach(documentPaths,
    new ParallelOptions { MaxDegreeOfParallelism = 4 },
    documentPath =>
    {
        // Each thread must create and dispose its own handler
        var ocrHandler = new OCRHandler();
        ocrHandler.Language = "eng";

        try
        {
            string text = ocrHandler.Process(documentPath);
            results[documentPath] = text;
        }
        finally
        {
            // Manual disposal required — no using statement support shown
            ocrHandler.Dispose();
        }
    });

foreach (var kvp in results)
    Console.WriteLine($"{Path.GetFileName(kvp.Key)}: {kvp.Value.Length} chars");
```

**IronOCR Approach:**

```csharp
// IronOCR: single IronTesseract instance shared across all threads
// Memory stays flat regardless of thread count
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

var ocr = new IronTesseract();    // Create once outside the parallel loop
var results = new ConcurrentDictionary<string, string>();
string[] documentPaths = Directory.GetFiles("./incoming", "*.png");

Parallel.ForEach(documentPaths, documentPath =>
{
    // OcrInput is created per thread — IronTesseract instance is shared
    using var input = new OcrInput();
    input.LoadImage(documentPath);
    input.Deskew();     // Preprocessing runs per-document, not per-thread engine
    input.DeNoise();

    var result = ocr.Read(input);
    results[documentPath] = result.Text;
});

foreach (var kvp in results)
    Console.WriteLine($"{Path.GetFileName(kvp.Key)}: {kvp.Value.Length} chars");
```

XImage.OCR's per-thread handler pattern means a four-thread batch job loading five languages carries roughly 1GB of OCR handler memory before processing a single document. IronOCR's shared instance keeps memory bounded at the single-instance footprint regardless of parallelism. The [multithreading example](https://ironsoftware.com/csharp/ocr/examples/csharp-tesseract-multithreading-for-speed/) demonstrates the pattern in full, and the [speed optimization guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-fast-configuration/) covers configuration tuning for throughput-focused batch workloads.

### Barcode and Text Combined Extraction

XImage.OCR has no barcode reading capability. Documents containing both text and barcodes require two separate libraries and two separate passes. IronOCR extracts both in a single read operation.

**XImage.OCR Approach:**

```csharp
// XImage.OCR: text only — barcodes require a separate library and second pass

var ocrHandler = new OCRHandler();
ocrHandler.Language = "eng";

// Pass 1: text extraction with XImage.OCR
string documentText = ocrHandler.Process("warehouse-label.png");
Console.WriteLine($"Text: {documentText}");

// Pass 2: barcode reading requires a completely separate library
// e.g., ZXing.Net, Dynamsoft Barcode Reader, or another commercial SDK
// - Additional NuGet package required
// - Additional license required
// - Additional code for result merging
// No combined text + barcode result object exists in XImage.OCR
```

**IronOCR Approach:**

```csharp
// IronOCR: text and barcodes from a single Read() call
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

var ocr = new IronTesseract();
ocr.Configuration.ReadBarCodes = true;    // Enable barcode extraction

using var input = new OcrInput();
input.LoadImage("warehouse-label.png");

var result = ocr.Read(input);

// Text and barcodes in one result object
Console.WriteLine($"Document text:\n{result.Text}");

if (result.Barcodes.Any())
{
    Console.WriteLine($"\nBarcodes found: {result.Barcodes.Count}");
    foreach (var barcode in result.Barcodes)
        Console.WriteLine($"  [{barcode.BarcodeType}] {barcode.Value}");
}
```

Setting `ReadBarCodes = true` adds barcode detection to the recognition pass without requiring a second library or a second read. The [barcode reading how-to](https://ironsoftware.com/csharp/ocr/how-to/barcodes/) and [barcode OCR example](https://ironsoftware.com/csharp/ocr/examples/csharp-ocr-barcodes/) cover the supported barcode formats and configuration options for mixed-content documents.

## XImage.OCR API to IronOCR Mapping Reference

| XImage.OCR | IronOCR Equivalent |
|---|---|
| `new OCRHandler()` | `new IronTesseract()` |
| `RasterEdge.XImage.OCR.License.LicenseManager.SetLicense("key")` | `IronOcr.License.LicenseKey = "key"` |
| `ocrHandler.Language = "eng"` | `ocr.Language = OcrLanguage.English` |
| `ocrHandler.Languages = new[] { "eng", "deu" }` | `ocr.Language = OcrLanguage.English + OcrLanguage.German` |
| `ocrHandler.Process(imagePath)` | `ocr.Read(input).Text` (after `input.LoadImage(path)`) |
| `ocrHandler.Process(image)` (from object) | `input.LoadImage(bytes)` or `input.LoadImage(stream)` |
| `ocrHandler.ProcessRegion(path, rect)` | `input.LoadImage(path, new CropRectangle(x, y, w, h))` |
| `ocrHandler.SetVariable("tessedit_char_whitelist", "0-9")` | `ocr.Configuration.WhiteListCharacters = "0123456789"` |
| `result` (plain string) | `result.Text` |
| `result.MeanConfidence` | `result.Confidence` |
| No equivalent | `result.Pages` / `result.Paragraphs` / `result.Lines` |
| No equivalent | `result.Words` (with `.X`, `.Y`, `.Confidence`) |
| No equivalent | `result.SaveAsSearchablePdf("output.pdf")` |
| No equivalent | `input.Deskew()` |
| No equivalent | `input.DeNoise()` |
| No equivalent | `input.Contrast()` |
| No equivalent | `input.Binarize()` |
| No equivalent | `input.Sharpen()` |
| No equivalent | `input.LoadImageFrames("file.tiff")` (multi-frame) |
| Requires RasterEdge PDF SDK | `input.LoadPdf(pdfPath)` |
| Requires RasterEdge PDF SDK | `result.SaveAsSearchablePdf("output.pdf")` |
| Not available | `ocr.Configuration.ReadBarCodes = true` |
| Per-thread `OCRHandler` instances | Single shared `IronTesseract` instance |

## Common Migration Issues and Solutions

### Issue 1: Runtime Failures After Partial Package Update

**XImage.OCR:** Running `dotnet outdated` or `dotnet restore` with a stale package cache can advance `RasterEdge.XImage.OCR` to a new version while leaving language packs at the previous version. The failure happens at runtime during the first OCR call with an error message that does not clearly identify version mismatch as the root cause. Finding the discrepancy requires checking all `PackageReference` entries manually.

**Solution:** After removing XImage.OCR packages and installing IronOCR, there is no version synchronization to maintain. The single `IronOcr` package carries everything. If you need language packs beyond bundled defaults, install `IronOcr.Languages.*` packages independently — they do not need to version-match the core:

```bash
dotnet add package IronOcr
dotnet add package IronOcr.Languages.Arabic     # Optional — only when needed
dotnet add package IronOcr.Languages.Japanese   # Optional — only when needed
```

### Issue 2: String Language Codes Cause Silent OCR Failures

**XImage.OCR:** Language codes are strings (`"eng"`, `"deu"`, `"fra"`). A typo in a language code — `"engg"`, `"ger"` instead of `"deu"` — either silently falls back to a default language or throws a runtime exception depending on the XImage.OCR version. Neither outcome is caught at compile time.

**Solution:** IronOCR uses the `OcrLanguage` enum. Invalid values are compile errors, not runtime surprises. Migrate string arrays to enum expressions:

```csharp
// Before (XImage.OCR) — typos compile fine, fail at runtime
ocrHandler.Languages = new[] { "eng", "deu", "fra" };

// After (IronOCR) — typos are compile errors
ocr.Language = OcrLanguage.English + OcrLanguage.German + OcrLanguage.French;
```

See the [multiple languages guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-multiple-languages/) for combining primary and secondary languages in documents with mixed-language content.

### Issue 3: Temp Files Left on Disk From Byte Array Processing

**XImage.OCR:** Processing images from byte arrays requires writing a temp file because `OCRHandler.Process()` accepts a file path, not a buffer. Exception paths that skip the `finally` block leave those temp files on disk. In high-throughput applications, this accumulates quickly.

**Solution:** `OcrInput.LoadImage()` accepts `byte[]` directly. No temp file is created:

```csharp
// Before (XImage.OCR) — temp file required
string tempPath = Path.GetTempFileName() + ".png";
File.WriteAllBytes(tempPath, imageBytes);
try { text = ocrHandler.Process(tempPath); }
finally { File.Delete(tempPath); }

// After (IronOCR) — direct byte array loading, no disk I/O
using var input = new OcrInput();
input.LoadImage(imageBytes);
var result = ocr.Read(input);
string text = result.Text;
```

### Issue 4: Memory Exhaustion Under Parallel Load

**XImage.OCR:** Parallel processing requires one `OCRHandler` per thread. Eight threads processing documents in five languages load eight separate engine instances, each carrying all five language packs. At roughly 50MB per language per instance, eight threads consume approximately 2GB of OCR engine memory alone before any document data enters the picture.

**Solution:** A single `IronTesseract` instance handles all threads. Create `OcrInput` per document (it is disposable and lightweight), reuse `IronTesseract` for the duration of the application:

```csharp
// Single instance — shared safely across all threads
var ocr = new IronTesseract();
ocr.Language = OcrLanguage.English + OcrLanguage.German +
               OcrLanguage.French + OcrLanguage.Spanish + OcrLanguage.Italian;

Parallel.ForEach(documentPaths, path =>
{
    using var input = new OcrInput();    // Per-document, lightweight
    input.LoadImage(path);
    var result = ocr.Read(input);        // Thread-safe call on shared instance
    ProcessResult(result.Text);
});
```

### Issue 5: CI/CD Pipeline Breaks After Partial Restore

**XImage.OCR:** A CI/CD agent with a warmed package cache often has some XImage.OCR language packs cached at an old version. When only the core package was updated in the project file, restore succeeds but the runtime loads mismatched assemblies. The build passes; the deployment fails.

**Solution:** After migration to IronOCR, the CI/CD pipeline restores one package. Add a validation step to confirm the expected version is present:

```bash
# In your CI pipeline — verify single package restore
dotnet restore
dotnet list package | grep IronOcr

# No version coordination logic needed — only one package to check
```

### Issue 6: Missing Structured Data for Downstream Parsing

**XImage.OCR:** Returns a plain string. Applications that need word positions, line groupings, or per-word confidence must parse the string using whitespace heuristics or custom logic. The accuracy of this parsing degrades on documents with multi-column layouts, tables, or rotated text.

**Solution:** IronOCR's `OcrResult` exposes the full document hierarchy directly. No string parsing needed:

```csharp
var result = ocr.Read(input);

// Direct access to structured data — no string manipulation
foreach (var page in result.Pages)
{
    foreach (var line in page.Lines)
    {
        // Line text, bounding box, and per-word data all available
        Console.WriteLine($"Line [{line.X},{line.Y}]: {line.Text}");

        foreach (var word in line.Words)
            Console.WriteLine($"  Word '{word.Text}' confidence: {word.Confidence}%");
    }
}
```

For the full structured data API, see the [read results how-to](https://ironsoftware.com/csharp/ocr/how-to/read-results/) and [OCR results features page](https://ironsoftware.com/csharp/ocr/features/ocr-results/).

## XImage.OCR Migration Checklist

### Pre-Migration

Audit the codebase to find all XImage.OCR touchpoints before making changes:

```bash
# Find all XImage.OCR namespace imports
grep -r "RasterEdge.XImage.OCR\|Yiigo.Image.Ocr\|XImage.OCR" --include="*.cs" .

# Find all OCRHandler usages
grep -r "OCRHandler\|ocrHandler" --include="*.cs" .

# Find all string-based language assignments
grep -r "\.Language\s*=\s*\"" --include="*.cs" .
grep -r "\.Languages\s*=\s*new\[\]" --include="*.cs" .

# Find all XImage.OCR package references in project files
grep -r "RasterEdge.XImage.OCR\|XImage.OCR.Language" --include="*.csproj" .

# Count distinct language packs installed
grep "XImage.OCR.Language" --include="*.csproj" -r . | wc -l
```

Note which image source types are in use (file paths, byte arrays, streams, TIFF), and identify any locations that use temp files for byte array processing. These are high-priority cleanup targets.

### Code Migration

1. Remove all `RasterEdge.XImage.OCR` and `XImage.OCR.Language.*` package references from every `.csproj` file
2. Add `IronOcr` package reference (`dotnet add package IronOcr`)
3. Replace `using RasterEdge.XImage.OCR` with `using IronOcr` in all files
4. Add `IronOcr.License.LicenseKey = ...` at application startup (once per process)
5. Replace `new OCRHandler()` with `new IronTesseract()`
6. Replace string language assignments (`"eng"`, `"deu"`) with `OcrLanguage` enum values
7. Replace `ocrHandler.Process(path)` with `input.LoadImage(path)` + `ocr.Read(input).Text`
8. Replace byte-array-to-temp-file patterns with `input.LoadImage(byte[])`
9. Replace multi-page TIFF manual frame splitting with `input.LoadImageFrames("file.tiff")`
10. Remove per-thread `OCRHandler` instantiation from `Parallel.ForEach` loops — use a single shared `IronTesseract` instance
11. Add preprocessing calls (`input.Deskew()`, `input.DeNoise()`) after each `LoadImage()` for documents from variable-quality sources
12. Replace plain string result handling with `result.Text` for text or `result.SaveAsSearchablePdf()` for PDF output
13. Replace `ocrHandler.SetVariable("tessedit_char_whitelist", ...)` with `ocr.Configuration.WhiteListCharacters = ...`
14. Update CI/CD pipeline: remove multi-package restore steps, remove version synchronization logic, verify single `IronOcr` package restore

### Post-Migration

- Confirm basic text extraction produces correct output from a known-good test image
- Verify multi-language documents return text for all configured languages
- Test byte array input paths produce correct output with no temp files created on disk
- Confirm multi-page TIFF documents return the correct page count in `result.Pages`
- Run parallel batch processing under load and measure peak memory — should be substantially lower than the XImage.OCR baseline
- Verify searchable PDF output opens correctly in Adobe Acrobat or a PDF viewer and that text is selectable
- Test preprocessing on a low-quality or skewed scan and compare extracted text accuracy against the XImage.OCR baseline
- Confirm license key initialization runs before the first OCR call and does not throw
- Validate that CI/CD restore succeeds in a clean environment with no cached packages
- Check structured data output (`result.Words`, `result.Paragraphs`) matches expected document layout

## Key Benefits of Migrating to IronOCR

**A Single Package Replaces an Entire Dependency Graph.** Every `XImage.OCR.Language.*` package, the core `RasterEdge.XImage.OCR` package, and the version synchronization overhead between them collapse into one `dotnet add package IronOcr` command. The `.csproj` entry count drops from eleven to one. The CI/CD restore step goes from a multi-package operation with eleven independent failure points to a single package restore. That simplification compounds: fewer packages to audit for security vulnerabilities, fewer entries to update when .NET compatibility changes, and no version coordination logic to maintain in automated update pipelines. The [IronOCR product page](https://ironsoftware.com/csharp/ocr/) and [documentation hub](https://ironsoftware.com/csharp/ocr/docs/) provide the full feature and deployment reference.

**Preprocessing Accuracy Improvements Are Immediate.** The migration is not a like-for-like replacement — it is an accuracy upgrade. Any document that XImage.OCR processed at degraded accuracy due to skew, noise, or low resolution now has a direct path to improvement via `input.Deskew()`, `input.DeNoise()`, and `input.Contrast()`. No external image processing library, no image processing expertise on the development team, no separate dependency to license and maintain. Adding three lines after `LoadImage()` recovers 20–35 percentage points of accuracy on scanned documents that were previously accepted as "good enough." The [image quality correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/) and the [preprocessing features page](https://ironsoftware.com/csharp/ocr/features/preprocessing/) cover each filter's effect on different document quality scenarios.

**Searchable PDFs and Structured Data Eliminate Second-SDK Costs.** The two most common requests from XImage.OCR users — searchable PDF output and word-level data with coordinates — both require additional RasterEdge products that incur separate commercial licenses. After migration, `result.SaveAsSearchablePdf()` produces archival-quality searchable documents with no extra packages, and `result.Pages`/`result.Words` provide structured data with bounding boxes and confidence scores. The functionality that cost two licenses now comes from one. Full output format documentation is on the [OCR results features page](https://ironsoftware.com/csharp/ocr/features/ocr-results/).

**Parallel Processing Scales Without Memory Penalties.** XImage.OCR's per-thread handler model makes scaling expensive. Doubling the thread count doubles the memory consumed by OCR engine instances. IronOCR's shared-instance model means memory stays bounded at the single-instance footprint regardless of parallelism. A server processing document batches at eight concurrent threads consumes the same OCR engine memory as a server processing one document at a time. This directly translates to lower hosting costs and higher throughput headroom on fixed infrastructure.

**Cross-Platform Deployment Opens Without Code Changes.** The same `IronOcr` package and the same application code run on Windows, Linux, macOS, Docker, Azure App Service, and AWS Lambda. No platform-conditional code, no platform-specific package variants, no per-environment deployment testing of the OCR layer. Teams that containerize workloads, run macOS development environments, or deploy to Linux-based cloud infrastructure gain immediate compatibility. The [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/), [Azure deployment guide](https://ironsoftware.com/csharp/ocr/get-started/azure/), and [Linux deployment guide](https://ironsoftware.com/csharp/ocr/get-started/linux/) document the setup for each target environment.

**125+ Languages Removes the Language Coverage Ceiling.** XImage.OCR tops out at approximately fifteen languages available as commercial packages. Standard tessdata distributions include over 100 languages at no cost. IronOCR bundles 125+ languages and exposes them through optional `IronOcr.Languages.*` packages that follow a clean installation pattern without the version-lock constraint. EU 24 official languages, all major CJK languages, Arabic, Hebrew, and specialized scripts are all available. The [languages index](https://ironsoftware.com/csharp/ocr/languages/) lists every supported language with its corresponding package name.
