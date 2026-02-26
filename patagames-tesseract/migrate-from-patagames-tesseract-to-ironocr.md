# Migrating from Patagames Tesseract.NET SDK to IronOCR

This guide walks .NET developers through a complete migration from Patagames Tesseract.NET SDK to [IronOCR](https://ironsoftware.com/csharp/ocr/). It covers the mechanical API translation, the cross-platform deployment unlock that drives most migrations, and the practical code changes required to move a production OCR pipeline from a Windows-only commercial wrapper to a library that runs on Windows, Linux, macOS, Docker, Azure, and AWS without modification.

## Why Migrate from Patagames Tesseract.NET SDK

The majority of teams that evaluate Patagames for replacement are not dissatisfied with OCR accuracy. They hit a deployment wall — a Linux container target, a cloud migration project, or a CI pipeline on Ubuntu — and discover that the Windows-only native binary simply has no path forward on that platform. That single constraint drives the rest of the migration evaluation.

**Windows-Only Deployment Blocks the Modern .NET Stack.** Patagames ships Windows native binaries for its Tesseract engine wrapper. There are no Linux x64, macOS, or ARM runtime packages. The `OcrApi` class loads a Windows DLL at runtime; on any other operating system, the application fails to start. Add the `System.Drawing.Bitmap` dependency, which Microsoft has formally marked as unsupported for new cross-platform development, and the library is incompatible with the default deployment model of every cloud provider and container orchestrator.

**Paying Commercial Rates for a Free Engine, Without Cross-Platform Access.** The Tesseract engine underlying Patagames is open source and free. Free community wrappers such as `tesseractocr` also ship pre-built Windows binaries today, which removes the primary convenience argument Patagames historically offered. A commercial license for Patagames buys a marginally cleaner API surface over raw Tesseract, but it does not add preprocessing, PDF support, searchable PDF output, or cross-platform deployment — the four capabilities that define a complete OCR library in 2026.

**Opaque Pricing Makes Budget Planning Impossible.** Patagames does not publish license prices. Evaluating the library requires a sales contact before any cost comparison can be made. IronOCR pricing starts at $749 for a perpetual single-developer Lite license with one year of updates included. Teams can evaluate cost versus capability without a sales process. See the [IronOCR licensing page](https://ironsoftware.com/csharp/ocr/licensing/) for full tier details.

**Raw Tesseract Variables Leak Through the API.** Setting page segmentation mode in Patagames requires calling `api.SetVariable("tessedit_pageseg_mode", "3")` — a raw string-based Tesseract variable assignment with no IntelliSense, no compile-time checking, and no discoverability. Misspell the variable name and the call silently does nothing. IronOCR wraps every Tesseract configuration option in strongly typed properties on `IronTesseract.Configuration`.

**No Structured Output Beyond a Flat String.** Patagames `GetTextFromImage` returns a single string. There is no access to word boundaries, line groupings, paragraph structure, or per-word confidence scores. Applications that need to extract specific fields from forms or validate OCR accuracy on a word-by-word basis have no foundation to build on with the Patagames API.

**CI/CD Pipelines Break at the Linux Step.** Modern .NET development teams run CI on Linux — GitHub Actions, GitLab CI, and Azure DevOps all default to Linux-based runners. A project referencing `Tesseract.Net.SDK` will either fail to build the native binary reference or fail at runtime during integration tests. Every test run requires a Windows-specific CI runner or a workaround that mocks the OCR layer entirely.

### The Fundamental Problem

Patagames targets Windows only. The moment your deployment target changes, the library cannot follow:

```csharp
// Patagames: Windows DLL loads; fails on Linux container or macOS developer machine
using var api = OcrApi.Create();
api.Init(@"./tessdata", "eng"); // tessdata path — must be manually managed in every environment
using var bitmap = new Bitmap(imagePath); // System.Drawing — unsupported on non-Windows targets
return api.GetTextFromImage(bitmap);
```

```csharp
// IronOCR: same code runs on Windows, Linux, macOS, Docker, Azure, AWS
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
var text = new IronTesseract().Read(imagePath).Text;
```

No tessdata directory. No native DLL path. No platform conditionals. The NuGet dependency graph resolves the correct runtime for each target automatically.

## IronOCR vs Patagames Tesseract.NET SDK: Feature Comparison

The following table covers the capabilities relevant to teams currently running Patagames in production.

| Feature | Patagames Tesseract.NET SDK | IronOCR |
|---|---|---|
| **Windows support** | Yes | Yes |
| **Linux support** | No | Yes |
| **macOS support** | No | Yes |
| **Docker deployment** | No | Yes |
| **Azure App Service** | No | Yes |
| **AWS Lambda** | No | Yes |
| **NuGet package** | `Tesseract.Net.SDK` | `IronOcr` |
| **License model** | Commercial (contact for price) | Perpetual ($749–$2,999, public) |
| **OCR engine** | Tesseract (open source) | Optimized Tesseract 5 (bundled) |
| **Tessdata management** | Manual directory with `.traineddata` files | NuGet language packages |
| **Automatic preprocessing** | None | Deskew, DeNoise, Contrast, Binarize, Sharpen, Scale, Dilate, Erode |
| **Deep background noise removal** | None | Yes (`DeepCleanBackgroundNoise()`) |
| **Native PDF input** | No (external renderer required) | Yes |
| **Multi-page TIFF input** | Limited | Yes (`input.LoadImageFrames()`) |
| **Searchable PDF output** | No | Yes (`result.SaveAsSearchablePdf()`) |
| **hOCR export** | No | Yes |
| **Languages supported** | Tesseract tessdata files | 125+ via NuGet packages |
| **Multi-language simultaneous** | Yes (string concatenation) | Yes (strongly typed `OcrLanguage` enum) |
| **Region-based OCR** | No | Yes (`CropRectangle`) |
| **Barcode reading** | No | Yes |
| **Structured output** | Flat string only | Pages, Paragraphs, Lines, Words, Characters with coordinates |
| **Per-word confidence scores** | No | Yes |
| **Page segmentation config** | Raw `SetVariable` string call | Strongly typed `Configuration.PageSegmentationMode` |
| **System.Drawing dependency** | Required | Optional |
| **Thread safety** | Standard Tesseract limits | Full (create `IronTesseract` per thread) |
| **Commercial support** | Yes | Yes |
| **NuGet downloads** | Limited | 5.3M+ |

## Quick Start: Patagames Tesseract.NET SDK to IronOCR Migration

### Step 1: Replace NuGet Package

Remove Patagames Tesseract.NET SDK:

```bash
dotnet remove package Tesseract.Net.SDK
```

Install IronOCR from [NuGet](https://www.nuget.org/packages/IronOcr):

```bash
dotnet add package IronOcr
```

For language support beyond English, install the corresponding language package:

```bash
dotnet add package IronOcr.Languages.French
dotnet add package IronOcr.Languages.German
```

### Step 2: Update Namespaces

Replace Patagames namespaces with the IronOCR namespace:

```csharp
// Before (Patagames)
using Patagames.Ocr;
using Patagames.Ocr.Enums;
using System.Drawing;

// After (IronOCR)
using IronOcr;
```

### Step 3: Initialize License

Add license initialization at application startup (before the first `IronTesseract` call):

```csharp
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
```

A free trial license is available at [ironsoftware.com/csharp/ocr/](https://ironsoftware.com/csharp/ocr/) to begin migration testing without purchase.

## Code Migration Examples

### Batch Folder Processing

Phase 1 showed single-image extraction. Production Patagames deployments typically initialize an `OcrApi` inside a loop, calling `api.Init()` on every iteration — which re-loads the tessdata and reinitializes the Tesseract engine for each file. That pattern compounds the initialization cost across hundreds of documents.

**Patagames Approach:**

```csharp
// NuGet: Tesseract.Net.SDK
using Patagames.Ocr;
using System.Drawing;
using System.IO;

public class PatagamesBatchProcessor
{
    private const string TessDataPath = @"./tessdata";

    public Dictionary<string, string> ProcessFolder(string folderPath)
    {
        var results = new Dictionary<string, string>();

        foreach (var file in Directory.GetFiles(folderPath, "*.jpg"))
        {
            // OcrApi re-initialized per file — tessdata loaded each time
            using var api = OcrApi.Create();
            api.Init(TessDataPath, "eng");

            using var bitmap = new Bitmap(file);
            var text = api.GetTextFromImage(bitmap);

            results[Path.GetFileName(file)] = text;
        }

        return results;
    }
}
```

**IronOCR Approach:**

```csharp
// NuGet: IronOcr
using IronOcr;
using System.IO;

IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

public class IronOcrBatchProcessor
{
    public Dictionary<string, string> ProcessFolder(string folderPath)
    {
        var results = new Dictionary<string, string>();
        // Single engine instance — initialized once, reused across all files
        var ocr = new IronTesseract();

        foreach (var file in Directory.GetFiles(folderPath, "*.jpg"))
        {
            var result = ocr.Read(file);
            results[Path.GetFileName(file)] = result.Text;
        }

        return results;
    }
}
```

IronOCR's `IronTesseract` instance holds engine state across calls. Reusing one instance for an entire batch eliminates per-file initialization overhead and removes the tessdata path dependency entirely. For parallel batch processing across multiple CPU cores, see the [multithreading example](https://ironsoftware.com/csharp/ocr/examples/csharp-tesseract-multithreading-for-speed/) — create one `IronTesseract` per thread rather than sharing a single instance.

### Page Segmentation Mode Migration

Patagames exposes page segmentation mode through a raw `SetVariable` call with a string key and an integer value cast to string. No IntelliSense, no enum validation, no documentation hint at the call site. A single digit controls whether Tesseract treats the input as a single block of text, a column, a word, or a single character — and there is no feedback when the variable name is mistyped.

**Patagames Approach:**

```csharp
// NuGet: Tesseract.Net.SDK
using Patagames.Ocr;
using Patagames.Ocr.Enums;
using System.Drawing;

public string OcrSingleLineField(string imagePath)
{
    using var api = OcrApi.Create();
    api.Init(@"./tessdata", "eng");

    // Raw variable string — no IntelliSense, no validation
    // PageSegmentationMode.SingleLine == 7
    api.SetVariable("tessedit_pageseg_mode", "7");

    // Additional variable to suppress dictionary output
    api.SetVariable("tessedit_char_whitelist", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz -.:/");

    using var bitmap = new Bitmap(imagePath);
    return api.GetTextFromImage(bitmap);
}
```

**IronOCR Approach:**

```csharp
// NuGet: IronOcr
using IronOcr;

IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

public string OcrSingleLineField(string imagePath)
{
    var ocr = new IronTesseract();

    // Strongly typed enum — discoverable through IntelliSense
    ocr.Configuration.PageSegmentationMode = TesseractPageSegmentationMode.SingleLine;

    var result = ocr.Read(imagePath);
    return result.Text;
}
```

Every Tesseract configuration option that Patagames exposes through `SetVariable` has a direct strongly typed equivalent in `IronTesseract.Configuration`. The migration is a mechanical substitution of string literals for named enum values. See the [IronTesseract API reference](https://ironsoftware.com/csharp/ocr/object-reference/api/IronOcr.IronTesseract.html) for the complete configuration surface. The [reading specific documents guide](https://ironsoftware.com/csharp/ocr/tutorials/read-specific-document/) covers when to apply each page segmentation mode on different document types.

### Result Iterator Pattern Replacement

Patagames returns a flat string from `GetTextFromImage`. Extracting individual words, their bounding boxes, or their confidence scores from Patagames output requires writing a parser on top of the returned string — or accessing the underlying Tesseract result iterator API directly through interop. Neither approach is reliable or maintainable. IronOCR exposes a fully structured `OcrResult` with native access to every level of the document hierarchy.

**Patagames Approach:**

```csharp
// NuGet: Tesseract.Net.SDK
using Patagames.Ocr;
using System.Drawing;

public void ExtractWordsWithPositions(string imagePath)
{
    using var api = OcrApi.Create();
    api.Init(@"./tessdata", "eng");

    using var bitmap = new Bitmap(imagePath);
    // Flat string only — no word positions, no confidence, no line grouping
    var text = api.GetTextFromImage(bitmap);

    // Only option: split on whitespace and hope line breaks survive
    var words = text.Split(new[] { ' ', '\n', '\r' },
        StringSplitOptions.RemoveEmptyEntries);

    foreach (var word in words)
    {
        // No X, Y, Width, Height — position information is lost
        Console.WriteLine(word);
    }
}
```

**IronOCR Approach:**

```csharp
// NuGet: IronOcr
using IronOcr;

IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

public void ExtractWordsWithPositions(string imagePath)
{
    var result = new IronTesseract().Read(imagePath);

    foreach (var page in result.Pages)
    {
        Console.WriteLine($"Page {page.PageNumber}: {page.Width}x{page.Height}px");

        foreach (var paragraph in page.Paragraphs)
        {
            Console.WriteLine($"  Paragraph at ({paragraph.X}, {paragraph.Y})");
            Console.WriteLine($"  Text: {paragraph.Text}");
        }

        foreach (var word in page.Words)
        {
            // Bounding box, confidence, and text for every word
            Console.WriteLine($"  Word: '{word.Text}' at ({word.X},{word.Y}) " +
                              $"size {word.Width}x{word.Height} " +
                              $"confidence {word.Confidence:F1}%");
        }
    }

    Console.WriteLine($"Overall confidence: {result.Confidence:F1}%");
}
```

The full `OcrResult` structure — pages, paragraphs, lines, words, and characters — eliminates the need for any post-processing parser. Word coordinates enable field extraction by position, which is the foundation of invoice processing, form OCR, and table extraction. See the [structured results guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) for the full hierarchy and the [confidence scores guide](https://ironsoftware.com/csharp/ocr/how-to/tesseract-result-confidence/) for filtering low-confidence words.

### Multi-Page TIFF Processing

Patagames accepts a `System.Drawing.Bitmap`. A multi-frame TIFF contains multiple embedded images, but `System.Drawing.Bitmap` does not automatically enumerate frames — you must use `Image.SelectActiveFrame()` to step through them manually and pass each frame bitmap to `GetTextFromImage` in a loop. The frame enumeration API is not obvious and the error messages when it fails are not descriptive.

**Patagames Approach:**

```csharp
// NuGet: Tesseract.Net.SDK
using Patagames.Ocr;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

public string ProcessMultiPageTiff(string tiffPath)
{
    using var api = OcrApi.Create();
    api.Init(@"./tessdata", "eng");

    var sb = new StringBuilder();

    using var tiffImage = Image.FromFile(tiffPath);
    var frameCount = tiffImage.GetFrameCount(FrameDimension.Page);

    for (int i = 0; i < frameCount; i++)
    {
        // Manual frame selection — FrameDimension.Page required
        tiffImage.SelectActiveFrame(FrameDimension.Page, i);

        using var frameBitmap = new Bitmap(tiffImage);
        var pageText = api.GetTextFromImage(frameBitmap);
        sb.AppendLine(pageText);
    }

    return sb.ToString();
}
```

**IronOCR Approach:**

```csharp
// NuGet: IronOcr
using IronOcr;

IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

public string ProcessMultiPageTiff(string tiffPath)
{
    using var input = new OcrInput();
    // LoadImageFrames handles all frames automatically
    input.LoadImageFrames(tiffPath);

    // Optional: apply preprocessing to all frames at once
    input.Deskew();
    input.DeNoise();

    var result = new IronTesseract().Read(input);

    // Per-page access if needed
    foreach (var page in result.Pages)
    {
        Console.WriteLine($"Page {page.PageNumber}: {page.Words.Length} words");
    }

    return result.Text;
}
```

`OcrInput.LoadImageFrames()` handles the frame enumeration internally and applies preprocessing to each frame in the pipeline. The `System.Drawing` frame selection ceremony disappears entirely. See the [TIFF and GIF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-tiff-gif/) for additional options including single-frame selection when only specific pages are needed.

### PDF Input Without an External Renderer

Patagames has no native PDF support. A Patagames-based PDF OCR pipeline requires an external PDF rendering library — PdfiumViewer, iTextSharp, or PdfSharp — to convert each page to a `Bitmap` before passing it to `GetTextFromImage`. That external dependency adds package management overhead, a separate licensing consideration, and a secondary failure point. The rendering quality also varies across libraries, which affects OCR accuracy independently of the Tesseract engine.

**Patagames Approach:**

```csharp
// NuGet: Tesseract.Net.SDK + PdfiumViewer (external dependency)
using Patagames.Ocr;
using PdfiumViewer; // separate NuGet package required
using System.Drawing;
using System.Text;

public string OcrPdfDocument(string pdfPath)
{
    using var api = OcrApi.Create();
    api.Init(@"./tessdata", "eng");

    var sb = new StringBuilder();

    // External renderer required — Patagames has no PDF support
    using var pdfDoc = PdfDocument.Load(pdfPath);

    for (int page = 0; page < pdfDoc.PageCount; page++)
    {
        // Render at 300 DPI for acceptable OCR accuracy
        using var img = pdfDoc.Render(page, 300, 300, false);
        using var bitmap = new Bitmap(img);

        var pageText = api.GetTextFromImage(bitmap);
        sb.AppendLine(pageText);
    }

    return sb.ToString();
}
```

**IronOCR Approach:**

```csharp
// NuGet: IronOcr only — no external PDF renderer
using IronOcr;

IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

public string OcrPdfDocument(string pdfPath)
{
    using var input = new OcrInput();
    input.LoadPdf(pdfPath);

    // Preprocessing applies to every page in one call
    input.Deskew();

    var ocr = new IronTesseract();
    var result = ocr.Read(input);

    // Full per-page structured access
    foreach (var page in result.Pages)
    {
        Console.WriteLine($"Page {page.PageNumber}: {page.Paragraphs.Length} paragraphs");
    }

    return result.Text;
}
```

One NuGet package replaces two. The rendering step disappears. The [PDF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-pdfs/) covers single-page, multi-page, and password-protected PDFs. For the searchable PDF output workflow — producing a Ctrl+F-searchable document from a scanned PDF — the [searchable PDF guide](https://ironsoftware.com/csharp/ocr/how-to/searchable-pdf/) and the [searchable PDF example](https://ironsoftware.com/csharp/ocr/examples/make-pdf-searchable/) show the full pipeline in five lines.

## Patagames Tesseract.NET SDK API to IronOCR Mapping Reference

| Patagames Tesseract.NET SDK | IronOCR Equivalent |
|---|---|
| `Tesseract.Net.SDK` (NuGet package) | `IronOcr` (NuGet package) |
| `Patagames.Ocr` (namespace) | `IronOcr` (namespace) |
| `Patagames.Ocr.Enums` (namespace) | `IronOcr` (namespace) |
| `OcrApi.Create()` | `new IronTesseract()` |
| `api.Init(tessDataPath, "eng")` | `ocr.Language = OcrLanguage.English` (no path) |
| `api.Init(path, "eng+fra+deu")` | `ocr.Language = OcrLanguage.English + OcrLanguage.French + OcrLanguage.German` |
| `api.GetTextFromImage(bitmap)` | `ocr.Read(imagePath).Text` |
| `api.SetVariable("tessedit_pageseg_mode", "7")` | `ocr.Configuration.PageSegmentationMode = TesseractPageSegmentationMode.SingleLine` |
| `api.SetVariable(key, value)` (any raw variable) | `ocr.Configuration.[TypedProperty]` |
| `new Bitmap(imagePath)` (input preparation) | `input.LoadImage(imagePath)` |
| `OcrBitmap.FromFile(path)` | `input.LoadImage(path)` |
| No multi-frame TIFF support | `input.LoadImageFrames(tiffPath)` |
| No PDF input | `input.LoadPdf(pdfPath)` or `ocr.Read(pdfPath)` |
| No searchable PDF | `result.SaveAsSearchablePdf("output.pdf")` |
| No preprocessing | `input.Deskew()`, `input.DeNoise()`, `input.Contrast()`, `input.Binarize()` |
| No region OCR | `input.LoadImage(path, new CropRectangle(x, y, w, h))` |
| No barcode reading | `ocr.Configuration.ReadBarCodes = true` |
| Flat string result only | `result.Pages`, `result.Lines`, `result.Words`, `result.Paragraphs` |
| No per-word confidence | `result.Words[i].Confidence`, `result.Confidence` |
| `PageSegmentationMode` enum | `TesseractPageSegmentationMode` enum |
| No hOCR export | Result `.ToHOcrString()` output |
| Windows x64/x86 only | Windows, Linux, macOS, Docker, Azure, AWS |

## Common Migration Issues and Solutions

### Issue 1: Tessdata Directory Missing in New Environment

**Patagames:** The `api.Init(@"./tessdata", "eng")` call fails at runtime if the `tessdata` directory is absent or the `eng.traineddata` file is missing. In containerized environments, this is a deployment-time failure with no build-time warning. Teams deploying to Docker frequently discover this after the image is already pushed.

**Solution:** IronOCR removes the tessdata directory concept entirely. Install language data as NuGet packages:

```bash
dotnet add package IronOcr.Languages.English
```

The language data resolves at build time and is included in `dotnet publish` output automatically. There is no path to get wrong and no deployment checklist item for language files.

### Issue 2: System.Drawing.Bitmap Fails on Linux

**Patagames:** The `System.Drawing.Bitmap` constructor throws `TypeInitializationException` or `PlatformNotSupportedException` on Linux unless `libgdiplus` is installed as a system package. Even with `libgdiplus` present, behavior is inconsistent across distributions. Microsoft explicitly recommends against using `System.Drawing` on non-Windows platforms in new development.

**Solution:** IronOCR accepts file paths, byte arrays, and streams directly. The `System.Drawing` dependency is not required:

```csharp
// Replace this pattern:
using var bitmap = new Bitmap(imagePath); // fails without libgdiplus on Linux
api.GetTextFromImage(bitmap);

// With:
var result = new IronTesseract().Read(imagePath); // no System.Drawing required
```

See the [image input guide](https://ironsoftware.com/csharp/ocr/how-to/input-images/) for all supported input types including byte arrays and streams.

### Issue 3: Silent SetVariable Failures

**Patagames:** `api.SetVariable("tessedit_pageseg_mode", someValue)` returns `bool` but most callers discard the return value. When a variable name is misspelled or an unsupported value is passed, Tesseract silently applies a default and continues. The resulting accuracy degradation is difficult to trace back to the configuration call.

**Solution:** IronOCR configuration properties are strongly typed. An invalid assignment produces a compiler error, not a silent runtime default:

```csharp
// Compile-time safety — no silent failures
var ocr = new IronTesseract();
ocr.Configuration.PageSegmentationMode = TesseractPageSegmentationMode.SingleBlock;
ocr.Configuration.TesseractVersion = TesseractVersion.Tesseract5; // also strongly typed
```

### Issue 4: OcrApi Initialized Inside a Loop

**Patagames:** Teams that initialize `OcrApi` inside a processing loop incur tessdata loading overhead on every iteration. The typical pattern — `OcrApi.Create()` and `api.Init()` inside a `foreach` — is correct from a thread-isolation perspective but expensive when processing hundreds of documents.

**Solution:** Create one `IronTesseract` per thread and reuse it across all documents assigned to that thread. The instance is stateless between `.Read()` calls:

```csharp
// One instance, many reads — engine initialized once
var ocr = new IronTesseract();
ocr.Language = OcrLanguage.English;

foreach (var file in imageFiles)
{
    var text = ocr.Read(file).Text;
    ProcessText(text);
}
```

For parallel batch workloads, create one instance per task. See the [speed optimization guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-fast-configuration/) for throughput tuning options including `IronTesseract.Configuration.TesseractVersion` and reading-speed presets.

### Issue 5: No Linux Docker Base Image Works

**Patagames:** There is no Linux-compatible Patagames binary. Any attempt to run a Patagames-based application in a Linux Docker container fails. The only workaround is a Windows-based container (`FROM mcr.microsoft.com/windows/servercore`), which is significantly larger, slower to pull, and incompatible with most Kubernetes configurations that use Linux node pools.

**Solution:** IronOCR supports standard Linux base images. The [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/) covers the exact Dockerfile configuration:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
# IronOCR resolves the Linux native runtime from NuGet automatically
ENTRYPOINT ["dotnet", "MyApp.dll"]
```

No Windows container required. No separate binary distribution. The same Docker image runs on any Linux-based container host.

### Issue 6: PDF OCR Requires Two NuGet Packages

**Patagames:** Adding PDF OCR to a Patagames application requires a second NuGet package for PDF rendering (PdfiumViewer, iTextSharp.LGPLv2.Core, or similar). Each adds its own licensing terms, update cadence, and potential compatibility issues. When the PDF renderer version and the Patagames version conflict, both teams must be engaged to resolve it.

**Solution:** IronOCR handles PDF input natively with no second package. Remove the PDF renderer dependency entirely:

```bash
# Remove the PDF rendering shim
dotnet remove package PdfiumViewer

# IronOCR handles PDF natively
var result = new IronTesseract().Read("document.pdf");
```

## Patagames Tesseract.NET SDK Migration Checklist

### Pre-Migration Tasks

Audit the codebase to identify all Patagames references before starting:

```bash
# Find all files using Patagames namespaces
grep -r "Patagames.Ocr" --include="*.cs" . -l

# Find all OcrApi usage patterns
grep -r "OcrApi\|GetTextFromImage\|api\.Init\|SetVariable" --include="*.cs" .

# Find tessdata path references
grep -r "tessdata\|TessDataPath\|traineddata" --include="*.cs" .

# Find System.Drawing.Bitmap usage tied to OCR
grep -r "new Bitmap\|System\.Drawing" --include="*.cs" . -l

# Find any PDF rendering libraries used to feed Patagames
grep -r "PdfiumViewer\|iTextSharp\|PdfSharp" --include="*.csproj" .
```

Document: total count of `OcrApi.Create()` call sites, number of distinct `api.Init()` language configurations, location of tessdata directory in each deployment environment, and any preprocessing code written in `System.Drawing` or `ImageSharp` that wraps Patagames calls.

### Code Update Tasks

1. Remove `Tesseract.Net.SDK` NuGet package reference from all projects.
2. Remove any PDF rendering NuGet packages (PdfiumViewer, iTextSharp, etc.) used solely to feed Patagames.
3. Install `IronOcr` NuGet package.
4. Install `IronOcr.Languages.English` and any other required language packages.
5. Add `IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY"` at application startup.
6. Replace `using Patagames.Ocr;` and `using Patagames.Ocr.Enums;` with `using IronOcr;`.
7. Replace each `OcrApi.Create()` + `api.Init(path, lang)` block with `new IronTesseract()` + `ocr.Language = OcrLanguage.[Language]`.
8. Replace each `api.GetTextFromImage(bitmap)` call with `ocr.Read(imagePath).Text` (removing the `Bitmap` constructor).
9. Replace each `api.SetVariable("tessedit_pageseg_mode", value)` call with the typed `ocr.Configuration.PageSegmentationMode = TesseractPageSegmentationMode.[Value]`.
10. Remove all `System.Drawing.Bitmap` instantiation that existed solely to pass images to Patagames.
11. Replace PDF rendering loops (if present) with `input.LoadPdf(pdfPath)`.
12. Replace multi-frame TIFF loops using `Image.SelectActiveFrame()` with `input.LoadImageFrames(tiffPath)`.
13. Replace any custom preprocessing code (System.Drawing resize, contrast, threshold) with the equivalent `OcrInput` filter calls.
14. Remove the tessdata directory from all deployment manifests, Dockerfiles, and CI copy steps.
15. Update integration tests to run on Linux CI runners (GitHub Actions ubuntu-latest, etc.) to verify cross-platform behavior.

### Post-Migration Testing

- Run the full test suite on Linux (not just Windows) to confirm the cross-platform deployment unlock is working.
- Verify that OCR accuracy is equal to or better than the Patagames baseline on the same set of test images.
- Confirm that multi-language documents produce correct output using the `OcrLanguage` enum approach.
- Test PDF input directly without the external rendering library and compare output accuracy against the old rendered-bitmap path.
- Validate multi-frame TIFF processing produces the same page count and text content as the previous frame enumeration loop.
- Confirm that the tessdata directory is absent from the deployment artifact and no runtime path errors occur.
- Run a Docker build targeting `linux/amd64` and execute at least one OCR call inside the container.
- Verify that the CI pipeline (GitHub Actions, GitLab CI, Azure DevOps) completes successfully on its default Linux runner.
- Check that confidence scores are available on the result and that any confidence-based filtering logic works as expected.
- Confirm that the license key initialization runs before the first `IronTesseract` instance is created in production startup code.

## Key Benefits of Migrating to IronOCR

**Cross-Platform Deployment Without Code Changes.** After migration, the same binary runs on Windows Server, Ubuntu Docker containers, macOS developer machines, Azure App Service on Linux, and AWS Lambda. There are no platform conditionals, no runtime identifier flags, and no separate deployment artifacts per operating system. A cloud migration that was previously blocked by the Windows-only OCR library becomes a standard container deployment. The [Linux](https://ironsoftware.com/csharp/ocr/get-started/linux/), [Docker](https://ironsoftware.com/csharp/ocr/get-started/docker/), [Azure](https://ironsoftware.com/csharp/ocr/get-started/azure/), and [AWS](https://ironsoftware.com/csharp/ocr/get-started/aws/) deployment guides cover production configurations for each target.

**Tessdata Management Disappears from Operations.** The tessdata directory — its location, its contents, its presence in every environment — no longer exists as an operational concern. Language data is a NuGet dependency resolved at build time. It appears in `dotnet publish` output automatically. There are no deployment runbooks to update when adding a new language, no Docker layer to invalidate when tessdata files change, and no missing-tessdata production incident to investigate.

**Structured Output Replaces String Parsing.** Applications that previously parsed the flat string from `GetTextFromImage` to extract fields, validate content, or compute confidence now access that data directly from `OcrResult`. Word coordinates, line boundaries, paragraph groupings, and per-word confidence scores are first-class properties. Field extraction by bounding box — the foundation of invoice processing and form OCR — is a direct `CropRectangle` call rather than a fragile substring search.

**Built-In Preprocessing Replaces Custom Image Pipelines.** Any preprocessing code written to compensate for Patagames' lack of built-in filters can be replaced with `OcrInput` method calls. Deskewing, noise removal, contrast enhancement, binarization, and resolution normalization are one-line operations. Teams that spent 20-40 hours building and tuning a `System.Drawing` preprocessing pipeline can replace it with five method calls and redirect that maintenance effort elsewhere. See the [preprocessing features overview](https://ironsoftware.com/csharp/ocr/features/preprocessing/) for the full filter catalog.

**Native PDF Support Removes a Dependency Class.** PDF rendering libraries added solely to bridge Patagames' PDF gap are eliminated. A production OCR system that previously required coordinating updates across three packages — `Tesseract.Net.SDK`, a PDF renderer, and their shared `System.Drawing` dependency — now has one OCR package with no bridging dependencies. PDF input, including password-protected and multi-page documents, is a first-class input type. For compliance and records management use cases, `result.SaveAsSearchablePdf()` produces text-layer PDF output in a single call without additional libraries.

**Transparent Pricing and Commercial Support.** IronOCR's $749 perpetual Lite license covers one developer and one deployment location with one year of updates included. The pricing is public, the tier structure is clear, and commercial support is available without an enterprise contract. Teams that were paying Patagames rates for Windows-only Tesseract wrapping gain cross-platform deployment, preprocessing, PDF support, and 125+ languages — while moving to a pricing model where the cost is known before the evaluation is complete. See [IronOCR licensing](https://ironsoftware.com/csharp/ocr/licensing/) for full tier details and the [IronOCR product page](https://ironsoftware.com/csharp/ocr/) for a free trial license.
