# Migrating from Windows.Media.Ocr to IronOCR

This guide provides a step-by-step migration path for .NET developers moving from Windows.Media.Ocr to [IronOCR](https://ironsoftware.com/csharp/ocr/). It covers namespace removal, project file changes, code migration examples for the patterns that arise most frequently during migration, and a practical checklist for validating the completed transition.

## Why Migrate from Windows.Media.Ocr (UWP/WinRT OCR)

Windows.Media.Ocr works well inside its boundaries. Those boundaries are narrow, and projects routinely grow past them. The reasons teams migrate fall into predictable categories.

**The Windows TFM Blocks Every Non-Windows Target.** The project file must declare a `net*-windows*` Target Framework Moniker before the `Windows.Media.Ocr` namespace even resolves at compile time. That declaration is not a runtime flag — it is a build constraint that propagates to every project referencing yours. A shared OCR service library, a web API, a background worker deployed to Linux — all of them inherit the constraint. Removing it means removing Windows.Media.Ocr.

**Language Availability Is Determined at Runtime by the OS, Not at Build Time by the Developer.** `OcrEngine.TryCreateFromLanguage` returns null when the requested language pack is absent from the host machine. The developer cannot install a language pack from code, bundle one with the application binary, or provide a fallback model. In automated environments — build agents, CI runners, minimal cloud VMs, containers — language packs are rarely installed. Production failures caused by a missing language pack are not reproducible by looking at the code; they require inspecting the OS configuration of the target machine.

**No Preprocessing Means No Recovery Path for Sub-Optimal Input.** The API accepts a `SoftwareBitmap` and produces text. Image quality improvement between those two points is entirely the developer's responsibility, using separate Windows Imaging Component APIs that are themselves Windows-only. Mobile phone photographs, misaligned flatbed scans, and photocopied documents degrade accuracy silently, with no built-in mechanism to diagnose or improve the result.

**PDF Is the Most Common Document Format in Enterprise Workflows.** Windows.Media.Ocr has no PDF input path. Processing a scanned PDF requires an external renderer, page-by-page rasterization, and manual result assembly. That renderer adds a dependency, licensing considerations, and a separate failure surface — exactly the complexity a "free and built-in" library was supposed to avoid.

**Server-Side Deployment Is Structurally Unsupported.** Windows.Media.Ocr targets client applications. Running it on Windows Server requires the Desktop Experience feature pack, which increases VM cost and infrastructure complexity. Docker deployment is impossible. Azure Functions on Linux, AWS Lambda, and any Linux-based container workload simply cannot reference the API.

**The WinRT Async Stack Is Incompatible with Standard .NET Patterns.** Six or more chained `await` calls — `StorageFile`, stream, `BitmapDecoder`, `SoftwareBitmap`, null-check, `RecognizeAsync` — are required before a single character is read. Integrating that chain into a background service, a Parallel.ForEach loop, or a standard ASP.NET controller is awkward. The WinRT `IAsyncOperation` machinery sits underneath it, and the interaction with .NET's `Task` model creates subtle edge cases in non-UI contexts.

### The Fundamental Problem

Language availability in Windows.Media.Ocr is a runtime unknown that cannot be resolved at deploy time:

```csharp
// Windows.Media.Ocr: language availability decided by OS admin, not the developer
// Returns null on any machine without the language pack installed
var engine = OcrEngine.TryCreateFromLanguage(
    new Windows.Globalization.Language("ja-JP"));

if (engine == null)
    throw new InvalidOperationException(
        "Japanese OCR unavailable — install the Japanese language pack in Windows Settings.");
// No recovery path. No bundled model. No fallback.
```

```csharp
// IronOCR: language availability is a NuGet package, not an OS configuration
// dotnet add package IronOcr.Languages.Japanese
var ocr = new IronTesseract();
ocr.Language = OcrLanguage.Japanese;
var result = ocr.Read("invoice.jpg"); // Works on any OS, any machine
Console.WriteLine(result.Text);
```

## IronOCR vs Windows.Media.Ocr (UWP/WinRT OCR): Feature Comparison

The table below covers the full capability surface relevant to migration decisions.

| Feature | Windows.Media.Ocr | IronOCR |
|---|---|---|
| **Platform: Windows 10/11** | Yes | Yes |
| **Platform: Windows Server** | Limited (Desktop Experience required) | Yes |
| **Platform: Linux** | No | Yes |
| **Platform: macOS** | No | Yes |
| **Platform: Docker containers** | No | Yes |
| **Platform: Azure Functions (Linux)** | No | Yes |
| **Platform: AWS Lambda** | No | Yes |
| **Project TFM requirement** | `net*-windows*` required | None (standard TFMs) |
| **Installation** | Windows built-in (no NuGet) | Single NuGet package (`IronOcr`) |
| **Image input (JPG, PNG, BMP)** | Yes (via WinRT pipeline) | Yes |
| **PDF input** | No | Yes (native) |
| **Multi-page TIFF input** | No | Yes |
| **Stream and byte array input** | No (StorageFile only) | Yes |
| **Language source** | OS-installed language packs | 125+ bundled NuGet packages |
| **Language portability** | No (machine-dependent) | Yes (deploy with application) |
| **Multi-language simultaneous** | No | Yes |
| **Preprocessing: deskew** | No | Yes (`input.Deskew()`) |
| **Preprocessing: denoise** | No | Yes (`input.DeNoise()`) |
| **Preprocessing: contrast** | No | Yes (`input.Contrast()`) |
| **Preprocessing: binarize** | No | Yes (`input.Binarize()`) |
| **Searchable PDF output** | No | Yes (`result.SaveAsSearchablePdf()`) |
| **Per-word confidence scores** | No | Yes (`word.Confidence`) |
| **Structured output (paragraphs, lines, words)** | Lines only | Pages, Paragraphs, Lines, Words, Characters |
| **Barcode reading during OCR** | No | Yes |
| **Region-based OCR** | No | Yes (`CropRectangle`) |
| **Synchronous OCR path** | No | Yes |
| **Thread-safe parallel processing** | Limited | Full |
| **Commercial support** | No (Windows platform team) | Yes |
| **Licensing model** | Free (Windows built-in) | Perpetual ($749 Lite, $1,499 Pro, $2,999 Enterprise) |

## Quick Start: Windows.Media.Ocr (UWP/WinRT OCR) to IronOCR Migration

### Step 1: Replace NuGet Package

Windows.Media.Ocr has no NuGet package — it is part of the Windows Runtime and resolves through the Windows TFM. Removing it means removing the Windows-specific namespace references and, where possible, the Windows TFM from the project file.

Remove the Windows.Media.Ocr namespaces from all source files:

```bash
# Audit all files referencing Windows OCR namespaces
grep -r "Windows.Media.Ocr\|Windows.Graphics.Imaging\|Windows.Storage" --include="*.cs" .
```

Install IronOCR:

```bash
dotnet add package IronOcr
```

The [IronOcr NuGet package](https://www.nuget.org/packages/IronOcr) targets `net6.0`, `net7.0`, `net8.0`, and `net9.0` without platform-specific TFMs. After removing the Windows OCR namespaces, update the `<TargetFramework>` in the project file from `net8.0-windows10.0.19041.0` to `net8.0` (or the appropriate version), provided no other WinRT APIs remain in the project.

### Step 2: Update Namespaces

Replace the three Windows OCR namespaces with a single IronOCR namespace:

```csharp
// Before (Windows.Media.Ocr)
using Windows.Media.Ocr;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Globalization;

// After (IronOCR)
using IronOcr;
```

### Step 3: Initialize License

Add the license initialization call once at application startup — in `Program.cs`, `Startup.cs`, or the application host builder:

```csharp
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
```

A free trial key is available from the [IronOCR licensing page](https://ironsoftware.com/csharp/ocr/licensing/) and removes the trial watermark for evaluation purposes.

## Code Migration Examples

### Replacing the WinRT Async Chain in a Background Service

Windows.Media.Ocr requires a minimum of six chained async operations before recognition begins. In a background service that processes a document queue, that chain runs inside a loop — and the `SoftwareBitmap` disposal, null-checking, and WinRT `IAsyncOperation` interop add friction at every iteration.

**Windows.Media.Ocr Approach:**

```csharp
// Windows.Media.Ocr: full async chain required per document
// Requires net8.0-windows10.0.19041.0 TFM — cannot deploy to Linux workers
public async Task<List<string>> ProcessQueueAsync(IEnumerable<string> imagePaths)
{
    var engine = OcrEngine.TryCreateFromUserProfileLanguages();
    if (engine == null)
        throw new InvalidOperationException("No OCR language pack installed on this machine.");

    var results = new List<string>();

    foreach (var path in imagePaths)
    {
        // Each document: 4 async steps before RecognizeAsync
        var file = await StorageFile.GetFileFromPathAsync(path);
        using var stream = await file.OpenAsync(FileAccessMode.Read);
        var decoder = await BitmapDecoder.CreateAsync(stream);
        var bitmap = await decoder.GetSoftwareBitmapAsync();

        var ocrResult = await engine.RecognizeAsync(bitmap);
        results.Add(ocrResult.Text);

        bitmap.Dispose();
    }

    return results;
}
```

**IronOCR Approach:**

```csharp
// IronOCR: one call per document, no WinRT, no SoftwareBitmap, no null checks
// Runs on Windows, Linux, macOS, Docker — same binary, no TFM change
public List<string> ProcessQueue(IEnumerable<string> imagePaths)
{
    var results = new List<string>();

    foreach (var path in imagePaths)
    {
        var result = new IronTesseract().Read(path);
        results.Add(result.Text);
    }

    return results;
}
```

The IronOCR version eliminates the `StorageFile` round-trip, the `BitmapDecoder`, the `SoftwareBitmap` lifecycle, and the null-check guard. For async-native services, [IronOCR provides an async path](https://ironsoftware.com/csharp/ocr/how-to/async/) that integrates cleanly into `Task`-based pipelines without WinRT interop overhead. The [IronTesseract setup guide](https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/) covers instance lifecycle recommendations for high-throughput queue scenarios.

### Eliminating SoftwareBitmap Conversion for In-Memory Image Data

Applications that already have image data in memory — from a network download, a database blob, or a camera capture callback — must convert that data into a `SoftwareBitmap` before Windows.Media.Ocr can process it. That conversion path goes through `BitmapDecoder`, which requires a stream, which means copying the byte array into a `MemoryStream`. IronOCR accepts byte arrays and streams directly.

**Windows.Media.Ocr Approach:**

```csharp
// Windows.Media.Ocr: byte array must travel through WinRT stream → BitmapDecoder → SoftwareBitmap
public async Task<string> RecognizeFromBytesAsync(byte[] imageBytes)
{
    var engine = OcrEngine.TryCreateFromUserProfileLanguages();
    if (engine == null)
        throw new InvalidOperationException("No OCR language available.");

    // Copy byte array into InMemoryRandomAccessStream (WinRT type)
    using var ras = new Windows.Storage.Streams.InMemoryRandomAccessStream();
    using var writer = new Windows.Storage.Streams.DataWriter(ras);
    writer.WriteBytes(imageBytes);
    await writer.StoreAsync();
    ras.Seek(0);

    var decoder = await BitmapDecoder.CreateAsync(ras);
    var bitmap = await decoder.GetSoftwareBitmapAsync();

    var result = await engine.RecognizeAsync(bitmap);
    bitmap.Dispose();
    return result.Text;
}
```

**IronOCR Approach:**

```csharp
// IronOCR: byte array loads directly into OcrInput — no conversion, no WinRT types
public string RecognizeFromBytes(byte[] imageBytes)
{
    using var input = new OcrInput();
    input.LoadImage(imageBytes); // direct byte array load

    var result = new IronTesseract().Read(input);
    return result.Text;
}
```

The Windows.Media.Ocr path requires `InMemoryRandomAccessStream` — a WinRT type that cannot be instantiated outside of Windows — plus `DataWriter`, `BitmapDecoder`, and `SoftwareBitmap`. The IronOCR path uses `OcrInput.LoadImage(byte[])` and produces the result in two lines. See the [stream input guide](https://ironsoftware.com/csharp/ocr/how-to/input-streams/) for `Stream`-based loading patterns, which follow the same simplicity as byte array input.

### Multi-Language Document Processing Without OS Coordination

A multilingual invoice pipeline that must recognize English, French, and German text in a single pass faces an architectural dead-end with Windows.Media.Ocr. The API allows only one language per engine instance. Processing a mixed-language document requires either a best-guess single-language engine or running recognition three times and merging results — neither of which produces reliable output.

**Windows.Media.Ocr Approach:**

```csharp
// Windows.Media.Ocr: one language per engine, no simultaneous multi-language support
// Each language requires a separate language pack installed on the machine
public async Task<string> RecognizeMultiLanguageAsync(SoftwareBitmap bitmap)
{
    // Must pick ONE language — no simultaneous recognition
    var engine = OcrEngine.TryCreateFromLanguage(
        new Windows.Globalization.Language("en-US"));
    if (engine == null)
        throw new InvalidOperationException("English language pack not installed.");

    // French and German text on the same document will be misrecognized
    var result = await engine.RecognizeAsync(bitmap);
    return result.Text;
}
```

**IronOCR Approach:**

```csharp
// IronOCR: simultaneous multi-language recognition in a single pass
// Language packs are NuGet packages — no OS coordination required
// dotnet add package IronOcr.Languages.French
// dotnet add package IronOcr.Languages.German
public string RecognizeMultiLanguage(string documentPath)
{
    var ocr = new IronTesseract();
    ocr.Language = OcrLanguage.English;
    ocr.Language = OcrLanguage.English + OcrLanguage.French + OcrLanguage.German;

    var result = ocr.Read(documentPath);

    // Structured output: walk paragraphs with location data
    foreach (var page in result.Pages)
    {
        foreach (var paragraph in page.Paragraphs)
        {
            Console.WriteLine($"[{paragraph.X},{paragraph.Y}] {paragraph.Text}");
        }
    }

    return result.Text;
}
```

IronOCR combines language models in a single recognition pass, eliminating the need to guess which language a given region uses. The [multi-language OCR guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-multiple-languages/) covers language pack installation and the `OcrLanguage` enum values for all 125+ supported languages. The [languages index](https://ironsoftware.com/csharp/ocr/languages/) lists the full catalog including CJK scripts, Arabic, Hebrew, Devanagari, and Cyrillic families.

### Enabling Server-Side OCR with Parallel Processing

Windows.Media.Ocr cannot run in a server context on Linux, cannot be called from a standard ASP.NET Core controller on a cross-platform host, and has undefined behavior when called from non-UI threads in server scenarios. A team moving an OCR endpoint from a Windows-only desktop application to a scalable web API hits all three constraints simultaneously.

**Windows.Media.Ocr Approach:**

```csharp
// Windows.Media.Ocr: cannot run on Linux, Docker, or Azure Functions on Linux
// UWP/WinRT assumptions about thread context cause failures in ASP.NET pipelines
// The entire approach below is non-deployable outside Windows with Desktop Experience

[HttpPost("ocr")]
public async Task<IActionResult> RecognizeDocument(IFormFile file)
{
    // WinRT requires STA thread context in some scenarios — not guaranteed in ASP.NET
    // Cannot deploy this controller to a Linux App Service plan
    using var stream = file.OpenReadStream();
    // InMemoryRandomAccessStream is a WinRT type — does not exist on Linux
    // var ras = new InMemoryRandomAccessStream(); // compile error on net8.0 TFM
    return StatusCode(503, "Windows-only — cannot deploy cross-platform.");
}
```

**IronOCR Approach:**

```csharp
// IronOCR: ASP.NET Core controller running on Linux, Docker, or Windows — same code
[HttpPost("ocr")]
public async Task<IActionResult> RecognizeDocument(IFormFile file)
{
    if (file == null || file.Length == 0)
        return BadRequest("No file provided.");

    using var memoryStream = new MemoryStream();
    await file.CopyToAsync(memoryStream);
    var imageBytes = memoryStream.ToArray();

    using var input = new OcrInput();
    input.LoadImage(imageBytes);
    input.Deskew();   // straighten uploaded scans automatically
    input.DeNoise();  // remove mobile camera noise

    var result = new IronTesseract().Read(input);

    return Ok(new
    {
        Text = result.Text,
        Confidence = result.Confidence,
        Pages = result.Pages.Count
    });
}
```

This controller deploys to Linux App Service, Docker, and AWS Lambda without modification. The [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/) covers the single `apt-get` dependency required on the Linux base image. The [Azure deployment guide](https://ironsoftware.com/csharp/ocr/get-started/azure/) and [AWS guide](https://ironsoftware.com/csharp/ocr/get-started/aws/) walk through cloud-specific configuration.

### Generating Searchable PDFs from Scanned Archives

Windows.Media.Ocr produces plain text strings. It has no output format beyond `OcrResult.Text` and the line geometry in `OcrResult.Lines`. Converting a scanned archive into searchable PDFs — a common requirement for document management systems and compliance workflows — requires a third library to construct the PDF output layer. IronOCR produces searchable PDFs natively.

**Windows.Media.Ocr Approach:**

```csharp
// Windows.Media.Ocr: plain text output only
// Searchable PDF requires external PDF library + manual text layer construction
public async Task<string> GetTextOnlyAsync(SoftwareBitmap bitmap)
{
    var engine = OcrEngine.TryCreateFromUserProfileLanguages();
    if (engine == null)
        throw new InvalidOperationException("No OCR language available.");

    var result = await engine.RecognizeAsync(bitmap);

    // result.Text is all you get
    // Producing a searchable PDF requires an entirely separate library
    return result.Text;
}
```

**IronOCR Approach:**

```csharp
// IronOCR: searchable PDF output is one method call on OcrResult
public void ProcessScannedArchive(IEnumerable<string> pdfPaths, string outputDirectory)
{
    foreach (var sourcePdf in pdfPaths)
    {
        var ocr = new IronTesseract();

        using var input = new OcrInput();
        input.LoadPdf(sourcePdf);   // native PDF input — no external renderer
        input.Deskew();             // correct scan misalignment per page
        input.DeNoise();            // remove scanner speckle

        var result = ocr.Read(input);

        var outputFileName = Path.Combine(
            outputDirectory,
            Path.GetFileNameWithoutExtension(sourcePdf) + "-searchable.pdf");

        result.SaveAsSearchablePdf(outputFileName);

        Console.WriteLine($"Processed: {sourcePdf} → {outputFileName} " +
                          $"({result.Pages.Count} pages, {result.Confidence:F1}% confidence)");
    }
}
```

The `SaveAsSearchablePdf` call embeds a text layer over the original scanned image, preserving visual fidelity while enabling full-text search and Ctrl+F within any PDF viewer. The [searchable PDF how-to guide](https://ironsoftware.com/csharp/ocr/how-to/searchable-pdf/) covers options for font embedding, text layer positioning, and multi-page output. The [PDF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-pdfs/) covers password-protected PDFs and page range selection for large archives.

### Extracting Structured Data with Word-Level Coordinates

Windows.Media.Ocr exposes `OcrResult.Lines` with line-level text and bounding rectangles. Per-word geometry exists in `OcrLine.Words` with `OcrWord.BoundingRect`, but there are no paragraphs, no confidence scores, and no character-level data. For form field extraction or invoice line item parsing, the line geometry is insufficient — paragraph boundaries and word confidence scores are required to distinguish structured fields from surrounding text.

**Windows.Media.Ocr Approach:**

```csharp
// Windows.Media.Ocr: line-level geometry, no paragraph grouping, no confidence scores
public async Task<List<string>> ExtractLineTextAsync(SoftwareBitmap bitmap)
{
    var engine = OcrEngine.TryCreateFromUserProfileLanguages();
    if (engine == null)
        throw new InvalidOperationException("No OCR language available.");

    var result = await engine.RecognizeAsync(bitmap);

    var lineTexts = new List<string>();
    foreach (var line in result.Lines)
    {
        // Line text + word bounding rects — no paragraph grouping, no confidence
        lineTexts.Add(line.Text);
    }
    return lineTexts;
}
```

**IronOCR Approach:**

```csharp
// IronOCR: full hierarchy — pages, paragraphs, lines, words, characters
// Each element carries coordinates and confidence for downstream validation
public void ExtractStructuredData(string documentPath)
{
    var result = new IronTesseract().Read(documentPath);

    Console.WriteLine($"Overall confidence: {result.Confidence:F1}%");

    foreach (var page in result.Pages)
    {
        Console.WriteLine($"\n--- Page {page.PageNumber} ---");

        foreach (var paragraph in page.Paragraphs)
        {
            Console.WriteLine($"Paragraph at ({paragraph.X},{paragraph.Y}): {paragraph.Text}");

            // Filter words below confidence threshold for validation workflows
            var lowConfidence = paragraph.Words
                .Where(w => w.Confidence < 70)
                .ToList();

            if (lowConfidence.Any())
            {
                Console.WriteLine($"  Low-confidence words: " +
                    string.Join(", ", lowConfidence.Select(w => $"'{w.Text}' ({w.Confidence:F0}%)")));
            }
        }
    }
}
```

The structured result model — `Pages`, `Paragraphs`, `Lines`, `Words`, `Characters` — provides the coordinate and confidence data needed for form field extraction, invoice parsing, and document layout analysis. The [read results guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) documents the full `OcrResult` object graph. The [confidence score guide](https://ironsoftware.com/csharp/ocr/how-to/tesseract-result-confidence/) explains how to use per-word confidence values to flag uncertain extractions for human review.

## Windows.Media.Ocr API to IronOCR Mapping Reference

| Windows.Media.Ocr | IronOCR |
|---|---|
| `OcrEngine.TryCreateFromLanguage(lang)` | `new IronTesseract()` + `ocr.Language = OcrLanguage.X` |
| `OcrEngine.TryCreateFromUserProfileLanguages()` | `new IronTesseract()` (English default; no null return) |
| `engine.RecognizeAsync(softwareBitmap)` | `ocr.Read("image.jpg")` or `ocr.Read(ocrInput)` |
| `StorageFile.GetFileFromPathAsync(path)` | `ocr.Read("path")` directly (no file handle needed) |
| `file.OpenAsync(FileAccessMode.Read)` | Eliminated — `OcrInput` loads directly |
| `BitmapDecoder.CreateAsync(stream)` | `input.LoadImage(stream)` via `OcrInput` |
| `decoder.GetSoftwareBitmapAsync()` | Eliminated — no `SoftwareBitmap` in IronOCR |
| `SoftwareBitmap` (WinRT type) | Eliminated — `OcrInput` accepts bytes, streams, file paths |
| `InMemoryRandomAccessStream` (WinRT type) | `new MemoryStream()` + `input.LoadImage(stream)` |
| `OcrResult.Text` | `OcrResult.Text` |
| `OcrResult.Lines` | `OcrResult.Lines` (also `Pages`, `Paragraphs`, `Words`, `Characters`) |
| `OcrLine.Text` | `OcrResult.Lines[i].Text` |
| `OcrLine.Words` | `OcrResult.Words` or `page.Paragraphs[i].Words` |
| `OcrWord.BoundingRect` | `word.X`, `word.Y`, `word.Width`, `word.Height` |
| No equivalent | `result.Confidence` (overall) / `word.Confidence` (per-word) |
| No equivalent | `result.SaveAsSearchablePdf("output.pdf")` |
| No equivalent | `input.LoadPdf("document.pdf")` |
| No equivalent | `input.Deskew()`, `input.DeNoise()`, `input.Contrast()` |
| No equivalent | `ocr.Language = OcrLanguage.A + OcrLanguage.B` (simultaneous) |
| No equivalent | `ocr.Configuration.ReadBarCodes = true` |
| No equivalent | `input.LoadImage(byteArray)` |

## Common Migration Issues and Solutions

### Issue 1: Project File Still Requires Windows TFM After Migration

**Windows.Media.Ocr:** The `<TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>` declaration is required for the WinRT types to resolve. Removing Windows.Media.Ocr references without checking for other WinRT dependencies in the same project can leave the TFM in place, preventing cross-platform builds.

**Solution:** After removing Windows OCR namespace references, search the project for any remaining WinRT API usage before changing the TFM:

```bash
# Find remaining WinRT API usage before removing the Windows TFM
grep -r "Windows\." --include="*.cs" .
grep -r "WinRT\|IAsyncOperation\|StorageFile\|SoftwareBitmap" --include="*.cs" .
```

If no WinRT references remain, update the project file:

```xml
<!-- Before -->
<TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>

<!-- After -->
<TargetFramework>net8.0</TargetFramework>
```

If other WinRT features (Windows notifications, shell integration, XAML) remain in use, abstract the OCR call behind an interface and provide platform-specific implementations rather than removing the TFM project-wide.

### Issue 2: Null Engine Checks Have No IronOCR Equivalent

**Windows.Media.Ocr:** Every call to `TryCreateFromLanguage` and `TryCreateFromUserProfileLanguages` can return null. All existing code contains null-check guard clauses that throw or branch on a null engine.

**Solution:** IronOCR throws structured exceptions on initialization failures rather than returning null. Remove the null-check guard clauses. Wrap in a standard try/catch if you need to surface initialization errors to a caller:

```csharp
// Before: null-check pattern
var engine = OcrEngine.TryCreateFromUserProfileLanguages();
if (engine == null)
    throw new InvalidOperationException("OCR unavailable.");

// After: no null — IronTesseract throws if misconfigured
try
{
    var result = new IronTesseract().Read("document.jpg");
}
catch (IronOcr.Exceptions.OcrException ex)
{
    // structured exception with diagnostic message
    logger.LogError("OCR failed: {Message}", ex.Message);
}
```

### Issue 3: SoftwareBitmap Parameters in Existing Method Signatures

**Windows.Media.Ocr:** Utility methods, services, and repository classes may accept `SoftwareBitmap` as a parameter type. Those method signatures cannot compile when the Windows TFM is removed.

**Solution:** Replace `SoftwareBitmap` parameters with `byte[]` or `Stream`. IronOCR's `OcrInput` accepts both directly. The call sites that previously constructed a `SoftwareBitmap` can pass their underlying data instead:

```csharp
// Before: SoftwareBitmap parameter — cannot compile cross-platform
public async Task<string> RecognizeAsync(SoftwareBitmap bitmap) { ... }

// After: byte array parameter — compiles on all platforms
public string Recognize(byte[] imageBytes)
{
    using var input = new OcrInput();
    input.LoadImage(imageBytes);
    return new IronTesseract().Read(input).Text;
}
```

### Issue 4: Async-Only Callers Cannot Use Synchronous IronOCR Directly

**Windows.Media.Ocr:** Every recognition call is `async`. Callers throughout the codebase use `await` and return `Task<string>`. Switching to IronOCR's synchronous `Read` method inside an `async` method works but may introduce blocking calls in contexts where `async` was architectural.

**Solution:** IronOCR provides an async path for callers that need it. Use `Task.Run` for CPU-bound wrapping in existing async methods, or use the native async API:

```csharp
// Option A: wrap synchronous call in Task.Run for async callers
public async Task<string> RecognizeAsync(string imagePath)
{
    return await Task.Run(() => new IronTesseract().Read(imagePath).Text);
}

// Option B: IronOCR async path
// See: https://ironsoftware.com/csharp/ocr/how-to/async/
```

The [async OCR guide](https://ironsoftware.com/csharp/ocr/how-to/async/) documents the built-in async API for contexts where fire-and-forget or progress-reporting patterns are needed.

### Issue 5: Windows Language Tag Format Does Not Map Directly

**Windows.Media.Ocr:** Languages are specified using BCP-47 string tags passed to `Windows.Globalization.Language("fr-FR")`. Those string tags have no direct equivalent in IronOCR.

**Solution:** Map BCP-47 language tags to the `OcrLanguage` enum. The mapping is straightforward for common languages:

```csharp
// Before: BCP-47 string tags
var engine = OcrEngine.TryCreateFromLanguage(
    new Windows.Globalization.Language("fr-FR"));

// After: OcrLanguage enum
var ocr = new IronTesseract();
ocr.Language = OcrLanguage.French;
// Also: OcrLanguage.German, OcrLanguage.Japanese, OcrLanguage.Arabic, etc.
```

The full mapping is available in the [IronOCR language catalog](https://ironsoftware.com/csharp/ocr/languages/). For languages not listed in the main enum, [custom language pack support](https://ironsoftware.com/csharp/ocr/how-to/ocr-custom-language/) covers loading `.traineddata` files directly.

### Issue 6: FileAccessMode.Read Has No Replacement

**Windows.Media.Ocr:** `file.OpenAsync(FileAccessMode.Read)` is a WinRT-specific file open pattern. The `FileAccessMode` enum does not exist in standard .NET.

**Solution:** Replace with standard `System.IO.File.ReadAllBytes` or `FileStream`. `OcrInput` accepts both:

```csharp
// Before: WinRT file access
using var stream = await file.OpenAsync(FileAccessMode.Read);

// After: standard .NET
var imageBytes = File.ReadAllBytes(imagePath);
using var input = new OcrInput();
input.LoadImage(imageBytes);
```

## Windows.Media.Ocr (UWP/WinRT OCR) Migration Checklist

### Pre-Migration Tasks

Audit the codebase before making changes:

```bash
# Find all Windows OCR namespace usages
grep -rn "using Windows.Media.Ocr" --include="*.cs" .
grep -rn "using Windows.Graphics.Imaging" --include="*.cs" .
grep -rn "using Windows.Storage" --include="*.cs" .
grep -rn "using Windows.Globalization" --include="*.cs" .

# Find WinRT type usages
grep -rn "OcrEngine\|SoftwareBitmap\|BitmapDecoder\|StorageFile" --include="*.cs" .
grep -rn "TryCreateFromLanguage\|TryCreateFromUserProfileLanguages\|RecognizeAsync" --include="*.cs" .
grep -rn "InMemoryRandomAccessStream\|DataWriter\|FileAccessMode" --include="*.cs" .

# Find project files with Windows TFM
grep -rn "net.*-windows" --include="*.csproj" .

# Count files requiring changes
grep -rl "Windows.Media.Ocr\|Windows.Graphics.Imaging\|SoftwareBitmap" --include="*.cs" . | wc -l
```

Record the number of files affected, the language tags in use (`"en-US"`, `"fr-FR"`, etc.), and whether any WinRT types appear in public method signatures (these require API surface changes in addition to internal rewrites).

### Code Update Tasks

1. Install the `IronOcr` NuGet package: `dotnet add package IronOcr`
2. Add the license initialization call in `Program.cs` or `Startup.cs`: `IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";`
3. Remove `using Windows.Media.Ocr;` from all source files
4. Remove `using Windows.Graphics.Imaging;` from all source files
5. Remove `using Windows.Storage;` from all source files
6. Remove `using Windows.Globalization;` from all source files
7. Add `using IronOcr;` to all files that perform OCR
8. Replace every `OcrEngine.TryCreateFromLanguage(new Language("xx-XX"))` call with `new IronTesseract()` and set `ocr.Language = OcrLanguage.X`
9. Replace every `OcrEngine.TryCreateFromUserProfileLanguages()` call with `new IronTesseract()`
10. Remove all null-check guard clauses on engine creation results
11. Replace `SoftwareBitmap` parameters in method signatures with `byte[]` or `Stream`
12. Replace `StorageFile` + `BitmapDecoder` + `SoftwareBitmap` construction chains with `OcrInput.LoadImage(path)`, `OcrInput.LoadImage(bytes)`, or `OcrInput.LoadImage(stream)`
13. Replace `engine.RecognizeAsync(bitmap)` with `ocr.Read(path)` or `ocr.Read(input)`
14. Replace `InMemoryRandomAccessStream` and `DataWriter` usage with `MemoryStream`
15. Replace Windows BCP-47 language tag strings with `OcrLanguage` enum values; install required language NuGet packages
16. Update `<TargetFramework>` in `.csproj` files to remove the `-windowsX.Y.Z` suffix where no other WinRT APIs remain

### Post-Migration Testing

- Confirm the project compiles targeting `net8.0` (or your target version) without the Windows TFM suffix
- Confirm the project compiles and runs on a Linux environment or Docker container using `mcr.microsoft.com/dotnet/aspnet:8.0`
- Verify OCR output text matches expected results for each document type in the test suite
- Verify all previously supported languages produce correct output using the IronOCR language NuGet packages
- Verify multi-language documents produce correct results in a single recognition pass
- Confirm no `NullReferenceException` or `InvalidOperationException` occurs at engine initialization on machines without Windows language packs installed
- Verify `result.Confidence` values are within expected ranges for clean and low-quality input documents
- If the application generates documents, verify `SaveAsSearchablePdf` output opens correctly in a PDF viewer and supports text search
- Run any existing parallel or multi-threaded processing paths and confirm thread safety under load
- Deploy to the target environment (Docker, Azure App Service, AWS, Linux server) and execute at least one full end-to-end OCR operation

## Key Benefits of Migrating to IronOCR

**Cross-Platform Deployment Becomes a Configuration Decision, Not a Code Rewrite.** After migration, the OCR component runs identically on Windows, Linux, macOS, Docker, and every major cloud provider. Moving an OCR workload from a Windows VM to a Linux container to reduce hosting cost is a deployment operation. The [Linux deployment guide](https://ironsoftware.com/csharp/ocr/get-started/linux/) and [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/) cover the one-line dependency addition required on Linux base images.

**Language Support Travels with the Application Binary.** Language packs install as NuGet packages and version-pin with the IronOCR package. The set of languages your application can recognize is defined in the project file and is identical on every machine — developer workstation, CI runner, staging server, and production host. No OS administrator coordination, no Group Policy exception, no runtime null-check.

**OCR Accuracy Improves Without External Tooling.** The preprocessing pipeline — `Deskew`, `DeNoise`, `Contrast`, `Binarize`, `Sharpen`, `Scale` — runs inside IronOCR before the recognition engine sees the image. Documents that produced degraded results with Windows.Media.Ocr due to scan misalignment or noise improve without adding external image processing dependencies. The [image quality correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/) and [filter wizard](https://ironsoftware.com/csharp/ocr/how-to/filter-wizard/) help identify the right filter combination for each document type.

**PDF Workflows Consolidate Into a Single Library.** The external PDF renderer that was required to bridge Windows.Media.Ocr and PDF input is no longer needed. Scanned PDF archives process through the same `IronTesseract.Read` call as images. Searchable PDF output is a method on the result object. The two-library architecture disappears, along with its version management, licensing overhead, and deployment surface.

**Structured Output Enables Document Intelligence Pipelines.** The `OcrResult` hierarchy — `Pages`, `Paragraphs`, `Lines`, `Words`, `Characters` — with per-element coordinates and confidence scores provides the data required for invoice field extraction, form parsing, and document classification. Windows.Media.Ocr's line-level output is insufficient for these workflows. With IronOCR, confidence-filtered word extraction, paragraph boundary detection, and coordinate-based field mapping are first-class features without additional libraries.

**Perpetual Licensing Replaces an Unbounded Infrastructure Dependency.** The cost of maintaining Windows language pack installation across a heterogeneous fleet of machines, Windows Server Desktop Experience licensing, and Windows-only CI infrastructure is real but diffuse — it shows up in IT tickets and infrastructure budgets, not as a line item in the OCR budget. A $749 IronOCR Lite license eliminates that overhead for a single-developer project. The $1,499 Professional license covers ten developers. Both are one-time purchases with one year of updates included.
