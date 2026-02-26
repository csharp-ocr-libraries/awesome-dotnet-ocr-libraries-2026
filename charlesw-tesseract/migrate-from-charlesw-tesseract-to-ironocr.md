# Migrating from Charlesw Tesseract to IronOCR

This guide walks .NET developers through migrating from the `charlesw/tesseract` NuGet package (`Tesseract`) to [IronOCR](https://ironsoftware.com/csharp/ocr/). The migration centers on one specific problem: the native binary deployment model that the charlesw wrapper imposes and the platform-conditional code that model forces developers to write. Teams that have fought `DllNotFoundException` in CI, wrestled with Leptonica library paths on Linux, or written OS-detection blocks that have nothing to do with OCR will find this guide shows exactly what disappears after switching.

## Why Migrate from Charlesw Tesseract

The archived `charlesw/tesseract` package gets new projects into trouble not because the API is bad, but because the deployment model it requires was designed around assumptions that do not hold in modern .NET infrastructure. Here is what drives migration decisions:

**Native Binary Deployment Per Platform.** The `Tesseract` NuGet package ships platform-specific native binaries: `tesseract50.dll` for Windows x64, a separate build for x86, `libtesseract.so` for Linux x64. Those binaries must land in the correct location at runtime for the P/Invoke calls to succeed. On a developer workstation the SDK copies them automatically. In a Docker container, an ARM64 build agent, or an Azure App Service with a non-standard application root, they do not. Every new deployment target becomes a debugging session.

**Leptonica as a Hidden Dependency.** Tesseract's image loading is handled by the Leptonica library, which ships as its own set of native DLLs alongside the Tesseract binaries. On Windows, `leptonica-1.82.0.dll` must be present in the output directory. On Linux, the Leptonica shared library must either be bundled or installed as a system package. Debian-based Docker images without `libleptonica-dev` fail at `Pix.LoadFromFile()` with an unhelpful native exception, and fixing it requires knowing which system package resolves the dependency.

**Platform-Conditional Code in Application Logic.** The combination of native binary loading and tessdata path resolution forces developers to write `RuntimeInformation.IsOSPlatform()` checks, environment variable detection for container contexts, and path-building logic that varies by target. None of that code is OCR logic. It is deployment plumbing that exists solely because the package's binary management is incomplete.

**Archived Package With No Remediation Path.** The repository has been archived since 2021. When a system package update on a Linux host changes the Leptonica ABI, or when a new .NET runtime changes native binary loading behavior, there is no version to update to. The only options are forking the native build pipeline or replacing the library.

**Tesseract 4.1.1 Engine Freeze.** The package wraps Tesseract 4.1.1. Tesseract 5's rewritten LSTM model delivers meaningfully higher accuracy on degraded documents. That upgrade is not available through the charlesw package — it requires switching libraries.

**Confidence Handling Without a Standard Pattern.** The charlesw wrapper exposes `page.GetMeanConfidence()` as a float between 0 and 1, but applying confidence thresholds at the word or character level requires the iterator pattern with `iter.GetConfidence(PageIteratorLevel.Word)`. There is no standard filtering API; every team implements their own threshold logic differently.

### The Fundamental Problem

The charlesw wrapper requires platform-specific native binary configuration before OCR can run:

```csharp
// charlesw Tesseract: OS detection required just to find native DLLs
// DllNotFoundException on any platform where binaries do not resolve
if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", "/app/lib");
}
var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
using var img = Pix.LoadFromFile(imagePath); // Requires leptonica native DLL
using var page = engine.Process(img);
return page.GetText();
```

IronOCR has no native binary configuration:

```csharp
// IronOCR: no path management, no OS detection, no leptonica dependency
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
var text = new IronTesseract().Read(imagePath).Text;
```

## IronOCR vs Charlesw Tesseract: Feature Comparison

The following table covers the capabilities relevant to teams evaluating this migration:

| Feature | Charlesw Tesseract | IronOCR |
|---|---|---|
| **Maintenance status** | Archived (no updates since 2021) | Actively maintained |
| **Tesseract engine version** | 4.1.1 (frozen) | 5 (current, optimized) |
| **License** | Apache 2.0 (free) | Commercial ($749–$2,999 perpetual) |
| **NuGet installation** | `Tesseract` | `IronOcr` |
| **Native binary management** | Manual per-platform DLL deployment | Bundled, zero configuration |
| **Leptonica dependency** | Requires `leptonica-1.82.0.dll` / `libleptonica-dev` | Not applicable (handled internally) |
| **Tessdata management** | Manual download and `.csproj` copy entry | NuGet language packs |
| **Platform-conditional code** | Required for multi-target deployment | Not required |
| **Docker deployment** | Requires explicit tessdata `COPY` + Leptonica `apt-get` | Standard .NET container requirements only |
| **ARM64 support** | Unconfirmed post-archive | Bundled, validated |
| **Image input formats** | TIFF, PNG, BMP, JPG (via Leptonica) | JPG, PNG, BMP, TIFF, GIF, and more |
| **Multi-page TIFF** | Manual frame iteration | `input.LoadImageFrames()` |
| **Native PDF input** | No (requires secondary library) | Yes |
| **Searchable PDF output** | No | Yes (`result.SaveAsSearchablePdf()`) |
| **Built-in preprocessing** | None | Deskew, DeNoise, Contrast, Binarize, Sharpen, Scale, Dilate, Erode, Invert |
| **Confidence filtering API** | Manual iterator with `GetConfidence()` | `result.Confidence`, `word.Confidence` |
| **Structured results** | Iterator pattern (`ResultIterator`) | Direct collections (Pages, Paragraphs, Lines, Words) |
| **Barcode reading** | No | Yes (during OCR pass) |
| **Region-based OCR** | No | Yes (`CropRectangle`) |
| **Thread safety** | Caller responsibility | Built-in |
| **125+ language packs** | Manual tessdata downloads | `dotnet add package IronOcr.Languages.*` |
| **Cross-platform .NET** | Yes (.NET Standard 2.0) | Yes (.NET Framework 4.6.2+, .NET 5/6/7/8/9) |
| **Security patch cadence** | None (archived) | Regular releases |

## Quick Start: Charlesw Tesseract to IronOCR Migration

### Step 1: Replace NuGet Package

Remove the charlesw Tesseract package:

```bash
dotnet remove package Tesseract
```

Install IronOCR from [NuGet](https://www.nuget.org/packages/IronOcr):

```bash
dotnet add package IronOcr
```

### Step 2: Update Namespaces

```csharp
// Before (charlesw Tesseract)
using Tesseract;

// After (IronOCR)
using IronOcr;
```

### Step 3: Initialize License

Add this call once at application startup, before any OCR operations run:

```csharp
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
```

A free trial license is available at the [IronOCR licensing page](https://ironsoftware.com/csharp/ocr/licensing/). The trial removes the watermark from output and enables full API access.

## Code Migration Examples

### Removing Native Binary Path Configuration

The most common initialization pattern in charlesw/tesseract projects is a factory or helper class that builds the tessdata path and configures native library loading per environment. This code exists purely because of the wrapper's deployment model.

**Charlesw Tesseract Approach:**

```csharp
// A realistic factory found in production charlesw/Tesseract projects
public static class OcrEngineFactory
{
    private static string GetTessDataPath()
    {
        // Different path per environment — all wrong until explicitly configured
        if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
            return "/app/tessdata";                                    // Docker
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return Path.Combine(AppContext.BaseDirectory, "tessdata"); // Linux bare metal
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "/usr/local/share/tessdata";                        // macOS Homebrew install
        return @".\tessdata";                                          // Windows dev machine
    }

    public static TesseractEngine Create(string language = "eng")
    {
        // If leptonica-1.82.0.dll is not in output directory: DllNotFoundException at this line
        // If tessdata folder is missing: TesseractException at engine construction
        return new TesseractEngine(GetTessDataPath(), language, EngineMode.Default);
    }
}

// Call site
using var engine = OcrEngineFactory.Create();
using var img = Pix.LoadFromFile("invoice.jpg");
using var page = engine.Process(img);
Console.WriteLine(page.GetText());
```

**IronOCR Approach:**

```csharp
// IronOCR: no factory, no path logic, no OS detection
// Runs identically on Windows, Linux, macOS, and ARM64
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

var result = new IronTesseract().Read("invoice.jpg");
Console.WriteLine(result.Text);
```

The entire `OcrEngineFactory` class deletes. The platform-conditional path logic, the `DOTNET_RUNNING_IN_CONTAINER` check, and the Leptonica DLL dependency all disappear with it. Every environment — developer workstation, CI agent, Docker container, cloud VM — executes the same two lines. The [IronTesseract setup guide](https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/) covers configuration options when defaults need adjustment, but for most deployments none are required.

### Leptonica Image Conversion Replacement

The charlesw wrapper uses Leptonica's `Pix` type as its image representation. Any code that manipulates images before OCR must convert through `Pix`, which requires the Leptonica native DLL to be loaded and operational. Replacing this pattern with `OcrInput` eliminates the Leptonica dependency entirely.

**Charlesw Tesseract Approach:**

```csharp
// Pix is Leptonica's image type — requires leptonica native DLL
// Converting from System.Drawing.Bitmap requires a temp file round-trip
public string ProcessInMemoryImage(Bitmap bitmap)
{
    // No direct Bitmap → Pix conversion; must write to temp file
    var tempPath = Path.Combine(Path.GetTempPath(), $"ocr_{Guid.NewGuid()}.png");
    try
    {
        bitmap.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);

        using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
        using var pix = Pix.LoadFromFile(tempPath);   // Leptonica file I/O
        using var page = engine.Process(pix);

        return page.GetText();
    }
    finally
    {
        if (File.Exists(tempPath)) File.Delete(tempPath);
    }
}
```

**IronOCR Approach:**

```csharp
// OcrInput accepts byte arrays and streams — no temp file, no Leptonica
public string ProcessInMemoryImage(byte[] imageBytes)
{
    using var input = new OcrInput();
    input.LoadImage(imageBytes);   // Direct byte array loading

    var result = new IronTesseract().Read(input);
    return result.Text;
}

// Or from a stream — same pattern
public string ProcessFromStream(Stream imageStream)
{
    using var input = new OcrInput();
    input.LoadImage(imageStream);

    var result = new IronTesseract().Read(input);
    return result.Text;
}
```

The temp file round-trip disappears. No file is written to disk, no Leptonica DLL is invoked for the conversion, and there is no `finally` block to clean up. The [image input guide](https://ironsoftware.com/csharp/ocr/how-to/input-images/) and [stream input guide](https://ironsoftware.com/csharp/ocr/how-to/input-streams/) document all supported input sources, including loading from URLs and memory-mapped files.

### Confidence Threshold Filtering

The charlesw wrapper exposes confidence at two levels: `page.GetMeanConfidence()` for the whole page, and `iter.GetConfidence(PageIteratorLevel.Word)` for individual words. Filtering low-confidence words out of output requires managing an iterator loop manually. IronOCR exposes confidence directly on result objects, making threshold logic a LINQ expression.

**Charlesw Tesseract Approach:**

```csharp
// Word-level confidence filtering requires iterator boilerplate
public List<string> ExtractHighConfidenceWords(string imagePath, float minConfidence = 0.8f)
{
    var highConfidenceWords = new List<string>();

    using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
    using var img = Pix.LoadFromFile(imagePath);
    using var page = engine.Process(img);

    // Page-level confidence only: fine-grained requires the iterator
    Console.WriteLine($"Page confidence: {page.GetMeanConfidence():P1}");

    using var iter = page.GetIterator();
    iter.Begin();
    do
    {
        if (iter.IsAtBeginningOf(PageIteratorLevel.Word))
        {
            var wordText = iter.GetText(PageIteratorLevel.Word)?.Trim();
            var wordConf  = iter.GetConfidence(PageIteratorLevel.Word) / 100f; // Returns 0-100
            if (!string.IsNullOrEmpty(wordText) && wordConf >= minConfidence)
                highConfidenceWords.Add(wordText);
        }
    } while (iter.Next(PageIteratorLevel.Para, PageIteratorLevel.Word));

    return highConfidenceWords;
}
```

**IronOCR Approach:**

```csharp
// Confidence is a property on each result object — no iterator required
public List<string> ExtractHighConfidenceWords(string imagePath, double minConfidence = 80.0)
{
    var result = new IronTesseract().Read(imagePath);

    Console.WriteLine($"Page confidence: {result.Confidence}%");

    // LINQ directly on the word collection — no iterator state management
    return result.Pages
        .SelectMany(p => p.Lines)
        .SelectMany(l => l.Words)
        .Where(w => w.Confidence >= minConfidence && !string.IsNullOrWhiteSpace(w.Text))
        .Select(w => w.Text)
        .ToList();
}
```

The iterator state machine is gone. Confidence values in IronOCR are on a 0–100 scale consistently, no division by 100 required. The [confidence scores guide](https://ironsoftware.com/csharp/ocr/how-to/tesseract-result-confidence/) covers per-word, per-line, and per-page confidence access patterns. The [read results guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) shows how to navigate the full structured result hierarchy.

### Multi-Page TIFF Batch Processing

TIFF files with multiple frames are common in document scanning workflows. The charlesw wrapper has no built-in multi-frame TIFF support; each frame must be extracted manually before processing. IronOCR handles multi-frame TIFFs natively with a single load call.

**Charlesw Tesseract Approach:**

```csharp
// charlesw/Tesseract has no multi-frame TIFF support
// Each frame must be extracted via System.Drawing before OCR can run
public string ProcessMultiFrameTiff(string tiffPath)
{
    var fullText = new StringBuilder();

    using var tiffImage = Image.FromFile(tiffPath);
    var frameCount = tiffImage.GetFrameCount(FrameDimension.Page);

    using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);

    for (int i = 0; i < frameCount; i++)
    {
        tiffImage.SelectActiveFrame(FrameDimension.Page, i);

        // Must save each frame as a temp file for Pix to load
        var tempPath = Path.Combine(Path.GetTempPath(), $"tiff_frame_{i}.png");
        try
        {
            tiffImage.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);

            using var pix  = Pix.LoadFromFile(tempPath);
            using var page = engine.Process(pix);
            fullText.AppendLine(page.GetText());
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    return fullText.ToString();
}
```

**IronOCR Approach:**

```csharp
// LoadImageFrames handles multi-frame TIFFs natively — no frame extraction loop
public string ProcessMultiFrameTiff(string tiffPath)
{
    using var input = new OcrInput();
    input.LoadImageFrames(tiffPath);   // All frames loaded in one call

    var result = new IronTesseract().Read(input);

    // Pages maps directly to TIFF frames
    foreach (var page in result.Pages)
        Console.WriteLine($"Frame {page.PageNumber}: {page.Words.Count()} words");

    return result.Text;
}
```

The temp-file extraction loop and the per-frame disposal chain are gone. Frame count detection via `FrameDimension.Page` disappears. IronOCR maps TIFF frames to `OcrResult.Pages`, so per-frame text access requires no extra iteration logic. The [TIFF/GIF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-tiff-gif/) covers additional options for frame selection and partial TIFF processing.

### Searchable PDF Generation

The charlesw wrapper produces only text output. Converting a scanned document to a searchable PDF — a common requirement for document management systems — requires a secondary PDF library (IronPDF, PdfSharp, or similar) to overlay the extracted text onto the original image pages. IronOCR produces searchable PDFs in one method call, with no secondary library.

**Charlesw Tesseract Approach:**

```csharp
// charlesw/Tesseract produces text only.
// Creating a searchable PDF requires a second library and significant code.
// The pattern below is representative — actual implementation varies by PDF library.
public void CreateSearchablePdf(string imagePath, string outputPdfPath)
{
    // Step 1: Extract text from image
    string extractedText;
    using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
    using var img = Pix.LoadFromFile(imagePath);
    using var page = engine.Process(img);
    extractedText = page.GetText();

    // Step 2: Build a PDF with the image as background and text overlay
    // Requires a separate PDF library (not shown — 50-100+ additional lines)
    // The text layer must be positioned to match the original image layout
    // Word-level coordinates from the iterator are needed for accurate alignment
    throw new NotImplementedException(
        "Searchable PDF generation requires a separate PDF library. " +
        "Add PdfSharp, IronPDF, or similar, then implement text layer overlay.");
}
```

**IronOCR Approach:**

```csharp
// SaveAsSearchablePdf produces a PDF/A-compatible searchable document
// No secondary library, no text overlay code, no coordinate mapping
public void CreateSearchablePdf(string imagePath, string outputPdfPath)
{
    var result = new IronTesseract().Read(imagePath);
    result.SaveAsSearchablePdf(outputPdfPath);
    Console.WriteLine($"Searchable PDF saved: {outputPdfPath}");
}

// Same API works for multi-page TIFF or existing PDF input
public void MakePdfSearchable(string scannedPdfPath, string outputPdfPath)
{
    var result = new IronTesseract().Read(scannedPdfPath);
    result.SaveAsSearchablePdf(outputPdfPath);
}
```

`SaveAsSearchablePdf()` embeds the OCR text as an invisible layer aligned to the recognized words, making the document full-text searchable without altering its visual appearance. The [searchable PDF how-to guide](https://ironsoftware.com/csharp/ocr/how-to/searchable-pdf/) covers page range selection and compression options. A working example is available at the [searchable PDF example page](https://ironsoftware.com/csharp/ocr/examples/make-pdf-searchable/).

## Charlesw Tesseract API to IronOCR Mapping Reference

| Charlesw Tesseract | IronOCR Equivalent |
|---|---|
| `new TesseractEngine(tessDataPath, "eng", EngineMode.Default)` | `new IronTesseract()` |
| `Pix.LoadFromFile(imagePath)` | `input.LoadImage(imagePath)` |
| `Pix.LoadFromMemory(bytes)` | `input.LoadImage(imageBytes)` |
| `engine.Process(pix)` | `ocr.Read(input)` |
| `page.GetText()` | `result.Text` |
| `page.GetMeanConfidence()` | `result.Confidence` (0–100 scale) |
| `page.GetIterator()` | `result.Pages`, `result.Words` (direct collections) |
| `iter.GetText(PageIteratorLevel.Word)` | `word.Text` |
| `iter.GetConfidence(PageIteratorLevel.Word)` | `word.Confidence` |
| `iter.TryGetBoundingBox(PageIteratorLevel.Word, out var b)` | `word.X`, `word.Y`, `word.Width`, `word.Height` |
| `iter.GetText(PageIteratorLevel.Para)` | `paragraph.Text` |
| `iter.IsAtBeginningOf(PageIteratorLevel.Block)` | `page.Paragraphs` (iterate directly) |
| `EngineMode.Default` | Automatic (Tesseract 5 LSTM default) |
| `EngineMode.TesseractOnly` | `ocr.Configuration.PageSegmentationMode` |
| Manual tessdata `.traineddata` file | `dotnet add package IronOcr.Languages.French` |
| `TessDataPath` constant + `.csproj` copy entry | Not applicable — bundled |
| `Pix.LoadFromFile()` via Leptonica DLL | `input.LoadImage()` — no native DLL required |
| Platform `GetTessDataPath()` method | Not applicable — eliminated |
| `leptonica-1.82.0.dll` / `libleptonica-dev` | Not applicable — no Leptonica dependency |
| Manual temp-file frame extraction for TIFF | `input.LoadImageFrames(tiffPath)` |
| No searchable PDF output | `result.SaveAsSearchablePdf(outputPath)` |
| `new TesseractEngine()` per thread | One `IronTesseract` — thread-safe |

## Common Migration Issues and Solutions

### Issue 1: DllNotFoundException for Leptonica or Tesseract Binaries

**Charlesw Tesseract:** `System.DllNotFoundException: Unable to load DLL 'leptonica-1.82.0': The specified module could not be found.` This exception fires when the Leptonica native DLL is not in the expected location. It is common in fresh Docker containers, CI agents, or any environment where the NuGet package's `runtimes/` folder did not copy correctly.

**Solution:** Remove the `Tesseract` package. Install `IronOcr`. IronOCR bundles all native binaries internally and does not P/Invoke into system Leptonica. The exception cannot occur because there is no external Leptonica dependency:

```bash
dotnet remove package Tesseract
dotnet add package IronOcr
```

No `apt-get install libleptonica-dev` required. No `<CopyToOutputDirectory>` entries for native DLLs.

### Issue 2: Tessdata Path Errors After Deployment

**Charlesw Tesseract:** `Tesseract.TesseractException: Failed to initialise tesseract engine.` This fires when `TessDataPath` does not resolve at runtime. It compiles without error, fails only at runtime, and the failure path depends on the deployment environment.

**Solution:** The tessdata path concept does not exist in IronOCR. Delete the constant, delete the `CopyToOutputDirectory` XML in `.csproj`, and delete the factory method that builds it. Language data is distributed as NuGet packages:

```bash
# Replace this manual tessdata file management:
#   tessdata/eng.traineddata  (15 MB, manually downloaded)
#   tessdata/fra.traineddata  (15 MB, manually downloaded)
#   .csproj <CopyToOutputDirectory> entry

# With NuGet packages:
dotnet add package IronOcr.Languages.French
```

The [multiple languages guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-multiple-languages/) shows how to configure multi-language recognition after adding language packages.

### Issue 3: Container Build Breaks When Base Image Updates

**Charlesw Tesseract:** The Dockerfile includes `apt-get install -y libleptonica-dev` to satisfy the Leptonica native dependency. When the base image moves from Debian Bullseye to Bookworm, or when the Leptonica package name changes across distributions, the build breaks with an apt error. Fixing it requires knowing which package name to use on the new distribution.

**Solution:** Remove the Leptonica `apt-get` line entirely. IronOCR on Linux requires only the standard `libgdiplus` package that any .NET application using System.Drawing already needs:

```dockerfile
# Before: Leptonica explicit install — breaks on base image updates
RUN apt-get update && apt-get install -y libleptonica-dev

# After: standard .NET Linux requirement only
RUN apt-get update && apt-get install -y libgdiplus
```

The [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/) provides tested Dockerfile templates for common base images. No charlesw-specific infrastructure code is required.

### Issue 4: Iterator Pattern Breaks on Empty or Whitespace Pages

**Charlesw Tesseract:** The `ResultIterator` returns `null` from `iter.GetText()` on some page segments, requiring explicit null checks throughout the loop. Missing a null check causes `NullReferenceException` on blank pages or images with no recognizable text.

**Solution:** IronOCR result collections are never null. Empty pages return empty collections. Check text content rather than null references:

```csharp
// Before: null checks required at every iterator level
var wordText = iter.GetText(PageIteratorLevel.Word);
if (wordText != null && wordText.Trim().Length > 0)
    results.Add(wordText.Trim());

// After: collection is safe to enumerate; check content as needed
foreach (var word in result.Pages.SelectMany(p => p.Lines).SelectMany(l => l.Words))
{
    if (!string.IsNullOrWhiteSpace(word.Text))
        results.Add(word.Text);
}
```

### Issue 5: Thread Safety Violations Under Load

**Charlesw Tesseract:** `TesseractEngine` is not thread-safe. Sharing one instance across concurrent requests in an ASP.NET application causes access violations or corrupted results. The standard fix is creating one engine per thread, but this is not obvious from the API and the error messages when it goes wrong are cryptic native exceptions.

**Solution:** `IronTesseract` is thread-safe. One instance can serve concurrent requests, or for maximum throughput, create one per thread in a `Parallel.ForEach` — both patterns work without modification:

```csharp
// Thread-safe parallel processing — IronTesseract handles concurrent access
var results = new System.Collections.Concurrent.ConcurrentBag<string>();
Parallel.ForEach(imageFiles, imagePath =>
{
    var ocr    = new IronTesseract();
    var result = ocr.Read(imagePath);
    results.Add(result.Text);
});
```

The [async OCR guide](https://ironsoftware.com/csharp/ocr/how-to/async/) covers async patterns for ASP.NET Core controllers where thread-blocking is not acceptable.

### Issue 6: ARM64 Binary Missing at Runtime

**Charlesw Tesseract:** On AWS Graviton (Linux ARM64) or Apple Silicon CI agents, the archived package may not ship an ARM64 native binary. The failure is a `DllNotFoundException` or a `BadImageFormatException` at engine creation — a runtime error on a platform that the package has no path to supporting.

**Solution:** IronOCR ships validated ARM64 binaries for both Linux and macOS. Deploy to ARM64 with no code changes. The [Linux deployment guide](https://ironsoftware.com/csharp/ocr/get-started/linux/) and [macOS deployment guide](https://ironsoftware.com/csharp/ocr/get-started/mac/) confirm the supported runtime identifiers.

## Charlesw Tesseract Migration Checklist

### Pre-Migration

Audit the codebase to identify all patterns that will change:

```bash
# Find all references to Tesseract namespace (engine creation, Pix usage, iterator usage)
grep -rn "using Tesseract" --include="*.cs" .

# Find TesseractEngine instantiation points
grep -rn "TesseractEngine" --include="*.cs" .

# Find Pix usage (Leptonica image type)
grep -rn "Pix\." --include="*.cs" .

# Find tessdata path constants and methods
grep -rn "tessdata\|TessDataPath\|traineddata" --include="*.cs" .

# Find platform-conditional deployment code
grep -rn "IsOSPlatform\|DOTNET_RUNNING_IN_CONTAINER\|LD_LIBRARY_PATH" --include="*.cs" .

# Find iterator pattern usage
grep -rn "GetIterator\|ResultIterator\|PageIteratorLevel" --include="*.cs" .

# Find confidence calls
grep -rn "GetMeanConfidence\|GetConfidence" --include="*.cs" .

# Find .csproj tessdata copy entries
grep -rn "tessdata" --include="*.csproj" .
```

Note which deployment environments the project targets (Docker, Linux, ARM64, Azure, AWS) — these are the environments where charlesw/tesseract requires the most configuration that IronOCR eliminates.

### Code Migration

1. Run `dotnet remove package Tesseract` to uninstall the charlesw wrapper
2. Run `dotnet add package IronOcr` to install IronOCR
3. Add `IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";` at application startup
4. Replace all `using Tesseract;` statements with `using IronOcr;`
5. Delete the tessdata path constant and any method that builds the path per environment
6. Remove all `RuntimeInformation.IsOSPlatform()` blocks written for tessdata path selection
7. Remove `<CopyToOutputDirectory>` entries for tessdata files from all `.csproj` files
8. Delete tessdata `.traineddata` files from source control or deployment artifact stores
9. Add `dotnet add package IronOcr.Languages.*` for each language previously deployed as a `.traineddata` file
10. Replace `TesseractEngine` + `Pix.LoadFromFile()` + `engine.Process()` chains with `new IronTesseract().Read()`
11. Replace all `Pix.LoadFromFile()` and `Pix.LoadFromMemory()` calls with `input.LoadImage()`
12. Replace all `page.GetText()` calls with `result.Text`
13. Replace iterator-based word/line extraction with direct collection access on `result.Pages`
14. Replace `iter.GetConfidence()` threshold logic with LINQ on `result.Words` or `result.Lines`
15. Remove `libleptonica-dev` / `leptonica-1.82.0.dll` from Dockerfiles and deployment scripts

### Post-Migration

Verify the following after completing code updates:

- OCR runs successfully on Windows without any native DLL errors
- OCR runs successfully in a Docker Linux container without any `apt-get` changes beyond `libgdiplus`
- OCR produces text output on ARM64 if that platform is in the deployment matrix
- Multi-page TIFF files return text from all frames, not just the first
- Confidence filtering returns the same logical set of high-confidence words as the previous iterator implementation
- Language-specific documents (French, German, etc.) recognize correctly after language NuGet packages are installed
- Parallel OCR operations complete without exceptions or corrupted output
- Searchable PDF output is generated where the previous implementation returned text only
- CI/CD pipeline builds without any tessdata download steps or Leptonica install commands
- Smoke test against the same image corpus used to validate the previous implementation

## Key Benefits of Migrating to IronOCR

**Self-Contained Deployment Model.** After migration, the OCR dependency is fully described by a single NuGet package reference. No tessdata files in source control, no `CopyToOutputDirectory` entries, no native DLL deployment steps, no Leptonica system packages. CI/CD pipelines that previously required multi-step artifact management reduce to `dotnet publish`. The deployment-related code that accumulated to support the charlesw wrapper is gone permanently.

**Platform Portability Without Conditional Logic.** The same application binary runs on Windows x64, Linux x64, Linux ARM64, macOS x64, and macOS ARM64 without modification. Teams adding an ARM64 deployment target — whether AWS Graviton, Apple Silicon CI, or Raspberry Pi — do not write new platform detection code. The [Linux deployment guide](https://ironsoftware.com/csharp/ocr/get-started/linux/) and [AWS deployment guide](https://ironsoftware.com/csharp/ocr/get-started/aws/) confirm the tested configurations.

**Tesseract 5 Accuracy With Built-In Preprocessing.** The jump from Tesseract 4.1.1 to Tesseract 5 improves recognition on degraded documents. IronOCR adds automatic preprocessing on top of the engine upgrade, applying deskew, denoise, contrast normalization, and binarization before the engine processes each image. Documents that previously required a custom preprocessing pipeline to reach acceptable accuracy thresholds now reach those thresholds without additional code. The [image quality correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/) documents explicit preprocessing options for cases that need tuning beyond the defaults.

**Direct Result Navigation Replaces Iterator Boilerplate.** The charlesw iterator pattern — `GetIterator()`, `Begin()`, `Next()`, `IsAtBeginningOf()`, null checks throughout — is replaced by simple collections. Words, lines, paragraphs, and pages are properties on the result object. Confidence-based filtering is a LINQ expression. Code that extracted word-level data previously required 15–30 lines of iterator management; the IronOCR equivalent is 2–3 lines. The [OCR results features page](https://ironsoftware.com/csharp/ocr/features/ocr-results/) summarizes the full structured output model.

**Searchable PDF Output Without a Secondary Library.** `result.SaveAsSearchablePdf()` produces a PDF with a text layer aligned to recognized words, requiring no secondary PDF library. Document management systems that ingest searchable PDFs no longer require a separate PDF generation step. The same result object that provides extracted text also writes the searchable file, keeping document processing pipelines to a single library dependency.

**Active Maintenance and Security Patch Coverage.** IronOCR receives regular updates tracking Tesseract 5 model improvements, .NET runtime compatibility validation, and security patch coverage for the underlying C++ engine. The dependency no longer carries the risk profile of an archived package — compliance reviews do not produce findings for a missing security patch path. As .NET 10 reaches general availability through 2026, the [IronOCR documentation hub](https://ironsoftware.com/csharp/ocr/docs/) will reflect current compatibility without requiring workarounds or forks.
