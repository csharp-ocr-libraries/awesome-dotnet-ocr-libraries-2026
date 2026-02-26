# Migrating from TesseractOcrMaui to IronOCR

This guide walks through a complete migration from TesseractOcrMaui to [IronOCR](https://ironsoftware.com/csharp/ocr/), with practical before-and-after code for every step. It targets developers who have already decided to move beyond the MAUI platform constraint and need a systematic path to a library that runs identically in mobile apps, server-side APIs, background workers, and cloud functions. No prior reading of the comparison article is required.

## Why Migrate from TesseractOcrMaui

TesseractOcrMaui was written to solve a real gap: existing .NET Tesseract wrappers could not resolve mobile platform interop on their own. For a pure MAUI prototype with no server footprint, it fills that gap. The problems surface the moment the product grows past that narrow scope.

**MAUI-Only Target Frameworks Prevent Code Sharing.** TesseractOcrMaui ships targets for `net8.0-ios`, `net8.0-android`, and `net8.0-windows` — all MAUI platform monikers. The package contains no `net8.0`, no `netstandard2.1`, no server-compatible target. Referencing it from a class library, an ASP.NET Core project, or an Azure Function produces a compile error. There is no workaround: the package is architecturally incapable of running outside a MAUI host. Every time the OCR requirement appears in a non-MAUI context, a second library must be introduced and maintained in parallel.

**Mandatory MAUI Dependency Injection Coupling.** The `AddTesseractOcr()` call in `MauiProgram.cs` wires `ITesseract` to the MAUI service provider. There is no factory method, no static entry point, and no constructor outside that DI graph. This means OCR logic cannot be extracted into a portable class library — every class that takes `ITesseract` in its constructor is locked to the MAUI application host for its entire lifetime.

**No PDF Input at Any Level.** PDF documents are the most common format for scanned contracts, invoices, and identity documents. TesseractOcrMaui throws `NotSupportedException` on any PDF input. Processing a PDF requires adding a separate PDF rendering library, writing page-by-page image extraction, managing temporary files in the device cache, and cleaning them up after each call. That is 100+ lines of infrastructure code before a single OCR call executes — and it still only works on MAUI.

**No Built-In Preprocessing for Real-World Images.** Mobile cameras produce images with rotation, sensor noise, and inconsistent DPI across device models. TesseractOcrMaui passes images directly to the Tesseract engine with zero preprocessing. Teams that need better accuracy must add SkiaSharp or ImageSharp, implement deskewing and denoising algorithms manually, write temporary file management, and test it all across iOS and Android device variants. Most skip it. Accuracy on real mobile captures suffers as a result.

**Single-Developer Maintenance Risk on a Production Dependency.** TesseractOcrMaui is maintained by one developer. There is no company behind it, no SLA, no security patch commitment, and no escalation path beyond a GitHub issue. For production applications in regulated industries — finance, healthcare, legal — a volunteer-maintained library with roughly 33,900 total NuGet downloads is not an acceptable dependency.

### The Fundamental Problem

TesseractOcrMaui compiles only inside a MAUI project. The moment another project type needs OCR, the architecture breaks:

```csharp
// TesseractOcrMaui: wired to MAUI host — cannot escape to a shared library
// This code compiles only inside a .NET MAUI application
public class OcrService
{
    private readonly ITesseract _tesseract; // resolved from MAUI DI — no other source exists

    public OcrService(ITesseract tesseract) { _tesseract = tesseract; }

    public async Task<string> ReadAsync(string imagePath)
    {
        await _tesseract.InitAsync("eng"); // traineddata must be bundled as MauiAsset
        var result = await _tesseract.RecognizeTextAsync(imagePath);
        return result.Success ? result.RecognizedText : string.Empty;
    }
    // Cannot reference this class from ASP.NET Core, Azure Functions, or Docker
}
```

```csharp
// IronOCR: plain instantiable class — compiles in any .NET project type
public class OcrService
{
    private readonly IronTesseract _ocr = new IronTesseract(); // no DI, no MAUI host

    public string Read(string imagePath)
    {
        using var input = new OcrInput();
        input.LoadImage(imagePath);
        return _ocr.Read(input).Text;
    }
    // Place this in a netstandard2.1 library — reference from MAUI, API, and Functions together
}
```

## IronOCR vs TesseractOcrMaui: Feature Comparison

The table below covers capability differences relevant to teams evaluating this migration.

| Feature | TesseractOcrMaui | IronOCR |
|---|---|---|
| .NET MAUI (iOS) | Yes | Yes (`IronOcr.iOS`) |
| .NET MAUI (Android) | Yes | Yes (`IronOcr.Android`) |
| .NET MAUI (Windows) | Yes | Yes |
| ASP.NET Core | No | Yes |
| Azure Functions | No | Yes |
| AWS Lambda | No | Yes |
| Docker / Linux containers | No | Yes |
| Console applications | No | Yes |
| WPF / WinForms | No | Yes |
| Shared .NET class library | No | Yes |
| PDF input (native) | No | Yes |
| Password-protected PDF input | No | Yes |
| Stream input | No | Yes |
| Byte array input | No | Yes |
| Multi-page TIFF input | No | Yes |
| Searchable PDF output | No | Yes |
| hOCR export | No | Yes |
| Automatic deskew | No | Yes |
| Automatic denoising | No | Yes |
| Contrast enhancement | No | Yes |
| Binarization | No | Yes |
| Region-based OCR | No | Yes |
| Barcode reading during OCR | No | Yes |
| Word-level coordinates | No | Yes |
| Multi-language simultaneous | No | Yes |
| Languages supported | Manually bundled traineddata | 125+ via NuGet packages |
| Thread safety | Manual | Built-in |
| Commercial support | None (single developer) | Yes (Iron Software) |
| Licensing | Apache 2.0 (free) | Perpetual from $749 |
| NuGet downloads | ~33,900 | 5.3M+ |

## Quick Start: TesseractOcrMaui to IronOCR Migration

### Step 1: Replace NuGet Package

Remove TesseractOcrMaui from the MAUI project:

```bash
dotnet remove package TesseractOcrMaui
```

Install IronOCR. For MAUI projects, add the platform-specific packages alongside the core package:

```bash
dotnet add package IronOcr
dotnet add package IronOcr.Android   # MAUI Android target
dotnet add package IronOcr.iOS       # MAUI iOS target
```

For server-side projects (ASP.NET Core, Azure Functions, console):

```bash
dotnet add package IronOcr
```

The [IronOCR NuGet package](https://www.nuget.org/packages/IronOcr) page lists all available platform packages.

### Step 2: Update Namespaces

Replace TesseractOcrMaui namespaces with the IronOcr namespace:

```csharp
// Before (TesseractOcrMaui)
using TesseractOcrMaui;
using TesseractOcrMaui.Results;
using Microsoft.Maui.Storage;

// After (IronOCR)
using IronOcr;
```

### Step 3: Initialize License

Add license initialization at application startup. In a MAUI app this goes in `MauiProgram.cs`; in ASP.NET Core it goes in `Program.cs`:

```csharp
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
```

## Code Migration Examples

### Replacing MAUI Dependency Injection Registration

TesseractOcrMaui requires registering the OCR engine through the MAUI service provider. Removing that registration is the first architectural step, because it is what locks all subsequent OCR code to the MAUI host.

**TesseractOcrMaui Approach:**

```csharp
// MauiProgram.cs — OCR engine registered here; nowhere else resolves it
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // Binds OCR to MAUI DI — no standalone path exists after this
        builder.Services.AddTesseractOcr();

        return builder.Build();
    }
}

// Any class that needs OCR must receive ITesseract from the MAUI container
public class InvoicePageViewModel
{
    private readonly ITesseract _tesseract;

    public InvoicePageViewModel(ITesseract tesseract)
    {
        _tesseract = tesseract; // fails to construct outside MAUI host
    }

    public async Task<string> ScanInvoiceAsync(string imagePath)
    {
        await _tesseract.InitAsync("eng");
        var result = await _tesseract.RecognizeTextAsync(imagePath);
        return result.RecognizedText ?? string.Empty;
    }
}
```

**IronOCR Approach:**

```csharp
// MauiProgram.cs — license only; no DI registration needed
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // One-line initialization — works for all project types
        IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

        return builder.Build();
    }
}

// No constructor injection needed — IronTesseract instantiates directly
public class InvoicePageViewModel
{
    public string ScanInvoice(string imagePath)
    {
        var ocr = new IronTesseract();
        using var input = new OcrInput();
        input.LoadImage(imagePath);
        return ocr.Read(input).Text;
    }
}
```

Removing `AddTesseractOcr()` eliminates the MAUI DI coupling. The `IronTesseract` class has a public parameterless constructor and carries no platform dependency — it can be instantiated anywhere. See the [IronTesseract setup guide](https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/) for initialization options including engine mode and language configuration.

### Moving OCR Logic to a Shared Class Library

With TesseractOcrMaui, sharing OCR logic across project types is structurally impossible. With IronOCR, the migration path is straightforward: extract the service into a `.NET Standard 2.1` or `net8.0` class library and reference it from every project in the solution.

**TesseractOcrMaui Approach:**

```csharp
// This service CANNOT be extracted to a shared library.
// It compiles only in a project that references TesseractOcrMaui,
// which only has MAUI platform targets.
//
// Result: every non-MAUI project must use a different OCR library,
// duplicating language config, error handling, and accuracy tuning.

public class DocumentOcrService
{
    private readonly ITesseract _tesseract; // MAUI DI only

    public DocumentOcrService(ITesseract tesseract)
    {
        _tesseract = tesseract;
    }

    public async Task<string> ProcessDocumentAsync(string imagePath)
    {
        await _tesseract.InitAsync("eng");
        var result = await _tesseract.RecognizeTextAsync(imagePath);
        return result.Success ? result.RecognizedText : string.Empty;
    }
    // Server team writes their own version using a different library
    // Two codebases, two accuracy profiles, two maintenance tracks
}
```

**IronOCR Approach:**

```csharp
// Place this in: MyCompany.OcrCore (net8.0 or netstandard2.1 class library)
// Reference from: MyCompany.MauiApp, MyCompany.Api, MyCompany.BatchWorker

using IronOcr;

namespace MyCompany.OcrCore
{
    public class DocumentOcrService
    {
        private readonly IronTesseract _ocr;

        public DocumentOcrService()
        {
            _ocr = new IronTesseract();
        }

        public string ProcessDocument(string imagePath)
        {
            using var input = new OcrInput();
            input.LoadImage(imagePath);
            input.Deskew();
            input.DeNoise();
            return _ocr.Read(input).Text;
        }

        public string ProcessDocumentFromBytes(byte[] imageData)
        {
            using var input = new OcrInput();
            input.LoadImage(imageData);
            input.Deskew();
            input.DeNoise();
            return _ocr.Read(input).Text;
        }

        public string ProcessDocumentFromStream(Stream imageStream)
        {
            using var input = new OcrInput();
            input.LoadImage(imageStream);
            return _ocr.Read(input).Text;
        }
    }
}
```

One class library, one set of tests, one accuracy profile. The MAUI app calls `ProcessDocument(photoPath)`, the ASP.NET Core API calls `ProcessDocumentFromBytes(uploadedBytes)`, and the Azure Function calls `ProcessDocumentFromStream(blobStream)` — all backed by the same implementation. The [stream input guide](https://ironsoftware.com/csharp/ocr/how-to/input-streams/) and [image input guide](https://ironsoftware.com/csharp/ocr/how-to/input-images/) document all `OcrInput` loading variants.

### Enabling Server-Side OCR in ASP.NET Core

TesseractOcrMaui cannot be referenced from an ASP.NET Core project. Teams that add a document upload endpoint are forced to reach for a different library entirely. IronOCR runs in ASP.NET Core without any configuration changes beyond the license key.

**TesseractOcrMaui Approach:**

```csharp
// ASP.NET Core Web API — TesseractOcrMaui CANNOT be used here.
// The package has no net8.0 or netstandard target.
// Referencing it produces: "The given project does not support targeting net8.0-ios/android/windows."
//
// Team is forced to add a second OCR library — Tesseract charlesw wrapper,
// a cloud API, or another solution — creating a split codebase.

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    // Cannot inject ITesseract here — no MAUI host, no MAUI DI container
    // Must use a completely different OCR library for server-side processing
}
```

**IronOCR Approach:**

```csharp
// ASP.NET Core — IronOCR works without modification
using IronOcr;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    [HttpPost("extract-text")]
    public async Task<IActionResult> ExtractText(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var ocr = new IronTesseract();
        using var input = new OcrInput();

        // Load directly from the upload stream — no temp files
        using var stream = file.OpenReadStream();

        if (file.ContentType == "application/pdf")
            input.LoadPdf(stream);
        else
            input.LoadImage(stream);

        input.Deskew();
        input.DeNoise();

        var result = ocr.Read(input);

        return Ok(new
        {
            text = result.Text,
            confidence = result.Confidence,
            pageCount = result.Pages.Count()
        });
    }

    [HttpPost("extract-text-batch")]
    public async Task<IActionResult> ExtractTextBatch(List<IFormFile> files)
    {
        var results = new List<object>();

        // Thread-safe: create one IronTesseract per thread
        await Parallel.ForEachAsync(files, async (file, ct) =>
        {
            var ocr = new IronTesseract();
            using var input = new OcrInput();
            using var stream = file.OpenReadStream();
            input.LoadImage(stream);
            var result = ocr.Read(input);

            lock (results)
            {
                results.Add(new { file = file.FileName, text = result.Text });
            }
        });

        return Ok(results);
    }
}
```

The same code deploys to IIS, Kestrel, or a Linux Docker container unchanged. The [ASP.NET OCR guide](https://ironsoftware.com/csharp/ocr/use-case/asp-net-ocr/) covers middleware configuration and the [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/) documents the Linux container setup.

### Eliminating Platform-Specific Handler Code

TesseractOcrMaui's MAUI-target-only architecture forces developers to write platform-conditional code when trying to integrate OCR into multi-target solutions. IronOCR removes the need for platform conditionals because the same package resolves correctly on every target.

**TesseractOcrMaui Approach:**

```csharp
// Attempting to share OCR logic across MAUI and non-MAUI targets
// requires platform-conditional compilation — a maintenance hazard

#if ANDROID || IOS || WINDOWS
// Only compile this block in MAUI targets
// Non-MAUI targets cannot reference TesseractOcrMaui at all
using TesseractOcrMaui;

public class PlatformOcrHandler
{
    private readonly ITesseract _tesseract;

    public PlatformOcrHandler(ITesseract tesseract)
    {
        _tesseract = tesseract;
    }

    public async Task<string> ProcessAsync(string imagePath)
    {
        await _tesseract.InitAsync("eng");
        var r = await _tesseract.RecognizeTextAsync(imagePath);
        return r.RecognizedText ?? string.Empty;
    }
}
#else
// Server targets need a completely different implementation
public class PlatformOcrHandler
{
    public string ProcessAsync(string imagePath)
    {
        // Duplicate logic using a different library
        throw new PlatformNotSupportedException("Use server OCR library here");
    }
}
#endif
```

**IronOCR Approach:**

```csharp
// One implementation — no conditional compilation, no duplicate logic
using IronOcr;

public class PlatformOcrHandler
{
    // This class compiles identically for:
    // net8.0-android, net8.0-ios, net8.0-windows (MAUI targets)
    // net8.0 (server targets)
    // netstandard2.1 (shared library targets)

    public string Process(string imagePath)
    {
        var ocr = new IronTesseract();
        using var input = new OcrInput();
        input.LoadImage(imagePath);
        input.Deskew();
        return ocr.Read(input).Text;
    }
}

// Multi-target .csproj — no conditional package references needed
// <TargetFrameworks>net8.0;net8.0-android;net8.0-ios</TargetFrameworks>
// IronOcr resolves correctly for all three targets from one package reference
```

Platform conditionals in OCR handling code indicate an architectural split that compounds over time. Every language configuration change, every preprocessing adjustment, every confidence threshold tweak must be applied in both branches. IronOCR makes the split unnecessary. The [.NET OCR library overview](https://ironsoftware.com/csharp/ocr/use-case/net-ocr-library/) covers multi-target project structure in detail.

### Structured Data Extraction with Word Coordinates

TesseractOcrMaui exposes only `result.RecognizedText` and a top-level confidence score. Extracting individual words with their bounding boxes — required for form field validation, document parsing, or highlight overlays — is not possible. IronOCR exposes a full document object model: pages, paragraphs, lines, words, and characters, each with pixel coordinates.

**TesseractOcrMaui Approach:**

```csharp
// TesseractOcrMaui: flat text string only — no structure, no coordinates
public class TesseractMauiFormParser
{
    private readonly ITesseract _tesseract;

    public TesseractMauiFormParser(ITesseract tesseract)
    {
        _tesseract = tesseract;
    }

    public async Task<Dictionary<string, string>> ParseFormAsync(string imagePath)
    {
        await _tesseract.InitAsync("eng");
        var result = await _tesseract.RecognizeTextAsync(imagePath);

        // result.RecognizedText is one flat string — no field positions
        // Parsing requires fragile line-splitting and regex heuristics
        var fields = new Dictionary<string, string>();
        var lines = result.RecognizedText?.Split('\n') ?? Array.Empty<string>();

        foreach (var line in lines)
        {
            // Hope the layout stays consistent enough to parse
            var parts = line.Split(':');
            if (parts.Length == 2)
                fields[parts[0].Trim()] = parts[1].Trim();
        }

        return fields;
        // No way to validate against expected field positions
        // No confidence per word — only document-level confidence
    }
}
```

**IronOCR Approach:**

```csharp
// IronOCR: full document structure with bounding boxes per word
using IronOcr;

public class IronOcrFormParser
{
    public List<WordLocation> ExtractWordsWithPositions(string imagePath)
    {
        var ocr = new IronTesseract();
        using var input = new OcrInput();
        input.LoadImage(imagePath);

        var result = ocr.Read(input);
        var wordLocations = new List<WordLocation>();

        foreach (var page in result.Pages)
        {
            foreach (var word in page.Words)
            {
                wordLocations.Add(new WordLocation
                {
                    Text = word.Text,
                    Confidence = word.Confidence,
                    X = word.X,
                    Y = word.Y,
                    Width = word.Width,
                    Height = word.Height
                });
            }
        }

        return wordLocations;
    }

    public FormData ParseStructuredForm(string imagePath)
    {
        var ocr = new IronTesseract();
        using var input = new OcrInput();
        input.LoadImage(imagePath);
        input.Deskew();

        var result = ocr.Read(input);
        var form = new FormData();

        foreach (var page in result.Pages)
        {
            foreach (var paragraph in page.Paragraphs)
            {
                // Use Y coordinate to identify form regions
                if (paragraph.Y < 200)
                    form.HeaderText += paragraph.Text + " ";
                else if (paragraph.Y > 800)
                    form.FooterText += paragraph.Text + " ";
                else
                    form.BodyLines.Add(paragraph.Text);
            }
        }

        form.OverallConfidence = result.Confidence;
        return form;
    }
}

public class WordLocation
{
    public string Text { get; set; }
    public float Confidence { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public class FormData
{
    public string HeaderText { get; set; } = string.Empty;
    public string FooterText { get; set; } = string.Empty;
    public List<string> BodyLines { get; set; } = new();
    public float OverallConfidence { get; set; }
}
```

Word coordinates enable validation against known form templates, confidence-based flagging for human review, and highlight overlays in document viewer UIs. The [structured results guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) documents the full `OcrResult` object model including character-level access and the [confidence scores guide](https://ironsoftware.com/csharp/ocr/how-to/tesseract-result-confidence/) covers per-word confidence filtering patterns.

### Background Processing with Native Async and Progress Tracking

TesseractOcrMaui exposes an async API (`RecognizeTextAsync`) but only within the MAUI application context. Long-running batch jobs must run in a background service, Azure Function, or worker process — none of which TesseractOcrMaui can target. IronOCR provides native async support that works in any hosted service.

**TesseractOcrMaui Approach:**

```csharp
// Background processing is impossible with TesseractOcrMaui.
// IHostedService runs in a server context — TesseractOcrMaui has no server target.
// The MAUI async API exists, but there is nowhere to run it outside the MAUI app host.

public class DocumentBatchWorker : BackgroundService
{
    // ITesseract cannot be injected here — no MAUI DI in a hosted service
    // Attempting to reference TesseractOcrMaui will fail to compile:
    // error: Package TesseractOcrMaui does not support target net8.0
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        throw new PlatformNotSupportedException(
            "TesseractOcrMaui has no server target. Use a different OCR library.");
    }
}
```

**IronOCR Approach:**

```csharp
// IronOCR: hosted service background batch processor
using IronOcr;
using Microsoft.Extensions.Hosting;

public class DocumentBatchWorker : BackgroundService
{
    private readonly ILogger<DocumentBatchWorker> _logger;
    private readonly string _inputFolder;
    private readonly string _outputFolder;

    public DocumentBatchWorker(ILogger<DocumentBatchWorker> logger, IConfiguration config)
    {
        _logger = logger;
        _inputFolder = config["Ocr:InputFolder"];
        _outputFolder = config["Ocr:OutputFolder"];
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var pendingFiles = Directory.GetFiles(_inputFolder, "*.pdf")
                .Concat(Directory.GetFiles(_inputFolder, "*.jpg"))
                .ToList();

            if (pendingFiles.Count > 0)
            {
                _logger.LogInformation("Processing {Count} documents.", pendingFiles.Count);

                // Thread-safe parallel processing — one IronTesseract per thread
                await Parallel.ForEachAsync(pendingFiles,
                    new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = stoppingToken },
                    async (filePath, ct) =>
                    {
                        await ProcessDocumentAsync(filePath, ct);
                    });
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ProcessDocumentAsync(string filePath, CancellationToken ct)
    {
        try
        {
            var ocr = new IronTesseract();
            using var input = new OcrInput();

            if (Path.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                input.LoadPdf(filePath);
            else
                input.LoadImage(filePath);

            input.Deskew();
            input.DeNoise();

            var result = await Task.Run(() => ocr.Read(input), ct);

            // Produce searchable PDF from the same OCR pass
            var outputPath = Path.Combine(_outputFolder,
                Path.GetFileNameWithoutExtension(filePath) + "_searchable.pdf");
            result.SaveAsSearchablePdf(outputPath);

            File.Delete(filePath); // move from input queue
            _logger.LogInformation("Processed {File}: {Confidence:F1}% confidence.", filePath, result.Confidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {File}.", filePath);
        }
    }
}
```

The worker registers in `Program.cs` with `builder.Services.AddHostedService<DocumentBatchWorker>()` and runs in any .NET 8 host — Windows Service, Linux systemd unit, Docker container, or Azure Container App. The [async OCR guide](https://ironsoftware.com/csharp/ocr/how-to/async/) covers async patterns and the [searchable PDF guide](https://ironsoftware.com/csharp/ocr/how-to/searchable-pdf/) documents the `SaveAsSearchablePdf` output options.

## TesseractOcrMaui API to IronOCR Mapping Reference

| TesseractOcrMaui | IronOCR Equivalent |
|---|---|
| `dotnet add package TesseractOcrMaui` | `dotnet add package IronOcr` |
| `builder.Services.AddTesseractOcr()` | Remove entirely — no registration required |
| `ITesseract` (injected) | `new IronTesseract()` (direct instantiation) |
| `_tesseract.InitAsync("eng")` | `ocr.Language = OcrLanguage.English;` (or omit for default English) |
| `_tesseract.RecognizeTextAsync(imagePath)` | `ocr.Read(input)` |
| `result.RecognizedText` | `result.Text` |
| `result.Success` | Exception-based; no boolean flag |
| `result.Status` | `catch (Exception ex)` message |
| `result.Confidence` | `result.Confidence` (also per-word) |
| `TesseractOcrMaui.Results.RecognitionResult` | `IronOcr.OcrResult` |
| `<MauiAsset>` traineddata bundle | `dotnet add package IronOcr.Languages.French` |
| `Resources/Raw/tessdata/eng.traineddata` | Remove — language data is inside NuGet package |
| `FileSystem.OpenAppPackageFileAsync()` (for traineddata) | Remove — not needed |
| No PDF support | `input.LoadPdf(path)` or `input.LoadPdf(stream)` |
| No preprocessing | `input.Deskew()`, `input.DeNoise()`, `input.Binarize()`, `input.Contrast()` |
| No searchable PDF output | `result.SaveAsSearchablePdf(outputPath)` |
| No word coordinates | `result.Pages[0].Words[i].X`, `.Y`, `.Width`, `.Height` |
| No per-word confidence | `result.Pages[0].Words[i].Confidence` |
| `net8.0-ios` target only | `net8.0` + `IronOcr.iOS` package |
| `net8.0-android` target only | `net8.0` + `IronOcr.Android` package |

## Common Migration Issues and Solutions

### Issue 1: AddTesseractOcr Cannot Be Removed Without Breaking Dependent Classes

**TesseractOcrMaui:** Every class that performs OCR receives `ITesseract` through constructor injection. Removing `AddTesseractOcr()` immediately breaks those constructors with a DI resolution exception.

**Solution:** Remove the constructor parameter and replace it with direct `IronTesseract` instantiation. If the project uses a DI container and you want to keep the injectable pattern, register `IronTesseract` manually:

```csharp
// Option A: Direct instantiation (recommended for most cases)
public class ScanPageViewModel
{
    public string ScanDocument(string imagePath)
    {
        var ocr = new IronTesseract();
        using var input = new OcrInput();
        input.LoadImage(imagePath);
        return ocr.Read(input).Text;
    }
}

// Option B: Register IronTesseract in DI if your architecture requires it
// In MauiProgram.cs or Program.cs:
builder.Services.AddSingleton<IronTesseract>();

// Then inject normally:
public class ScanPageViewModel
{
    private readonly IronTesseract _ocr;
    public ScanPageViewModel(IronTesseract ocr) { _ocr = ocr; }

    public string ScanDocument(string imagePath)
    {
        using var input = new OcrInput();
        input.LoadImage(imagePath);
        return _ocr.Read(input).Text;
    }
}
```

### Issue 2: Traineddata Files Missing After Package Removal

**TesseractOcrMaui:** The `Resources/Raw/tessdata/` folder, the `.traineddata` files inside it, and the `<MauiAsset>` declarations in the `.csproj` all need to be removed. Leaving them causes build warnings and inflates the app bundle with unused files.

**Solution:** Delete the tessdata folder, remove the `<MauiAsset>` entries, and uninstall any language that was manually downloaded. Install the equivalent IronOCR language pack instead:

```bash
# Delete traineddata assets
rm -rf Resources/Raw/tessdata

# Remove from .csproj (delete the MauiAsset ItemGroup):
# <ItemGroup>
#   <MauiAsset Include="Resources\Raw\tessdata\*.traineddata" />
# </ItemGroup>

# Install IronOCR language pack (if non-English language was needed)
dotnet add package IronOcr.Languages.French
dotnet add package IronOcr.Languages.German
```

Language packages from IronOCR are resolved at build time and bundled without any manual file management. The [multiple languages guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-multiple-languages/) documents all available packages and simultaneous multi-language configuration.

### Issue 3: InitAsync Must Be Called Before Every RecognizeTextAsync

**TesseractOcrMaui:** The `ITesseract.InitAsync(language)` call must precede every `RecognizeTextAsync` call. Teams often add `_isInitialized` guard flags, double-checked locking, or semaphores to avoid repeated initialization. All of that code becomes dead code after migration.

**Solution:** `IronTesseract` has no initialization step. Language is set once on the instance. Remove all `InitAsync` calls, all `_isInitialized` flags, and all initialization guard logic:

```csharp
// Before: initialization guard required before every OCR call
private bool _isInitialized = false;
private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);

public async Task<string> GetTextAsync(string imagePath)
{
    await _initLock.WaitAsync();
    try
    {
        if (!_isInitialized)
        {
            await _tesseract.InitAsync("eng");
            _isInitialized = true;
        }
    }
    finally { _initLock.Release(); }

    var result = await _tesseract.RecognizeTextAsync(imagePath);
    return result.RecognizedText ?? string.Empty;
}

// After: no initialization, no guard, no semaphore
public string GetText(string imagePath)
{
    var ocr = new IronTesseract();
    using var input = new OcrInput();
    input.LoadImage(imagePath);
    return ocr.Read(input).Text;
}
```

### Issue 4: result.Success Check Pattern Must Be Replaced

**TesseractOcrMaui:** The `RecognizeTextAsync` return value carries a `Success` boolean and a `Status` string. Code that checks `if (!result.Success)` and reads `result.Status` for error information needs to be rewritten.

**Solution:** IronOCR uses standard .NET exception semantics. Replace success-flag checks with try/catch. On success, `.Text` is always populated (empty string if no text was found):

```csharp
// Before: success-flag pattern
var result = await _tesseract.RecognizeTextAsync(imagePath);
if (!result.Success)
{
    logger.LogError("OCR failed: {Status}", result.Status);
    return string.Empty;
}
return result.RecognizedText ?? string.Empty;

// After: exception pattern
try
{
    var ocr = new IronTesseract();
    using var input = new OcrInput();
    input.LoadImage(imagePath);
    var result = ocr.Read(input);
    return result.Text; // empty string if no text found — never null
}
catch (Exception ex)
{
    logger.LogError(ex, "OCR failed for {Path}.", imagePath);
    return string.Empty;
}
```

### Issue 5: MAUI-Only Target Framework in Shared Library Projects

**TesseractOcrMaui:** A class library that references `TesseractOcrMaui` automatically inherits its platform constraint. The library's `<TargetFramework>` must be set to a MAUI moniker (`net8.0-android`, `net8.0-ios`, or `net8.0-windows`), which prevents it from being referenced by server projects.

**Solution:** Change the class library target to `net8.0` or `netstandard2.1` and reference `IronOcr` instead. The library now resolves correctly from any consuming project:

```xml
<!-- Before: locked to MAUI target because TesseractOcrMaui has no net8.0 target -->
<TargetFramework>net8.0-android</TargetFramework>
<PackageReference Include="TesseractOcrMaui" Version="*" />

<!-- After: universal target — referenced from MAUI, API, worker, and Functions -->
<TargetFramework>net8.0</TargetFramework>
<PackageReference Include="IronOcr" Version="*" />
```

### Issue 6: PDF Processing Requires a Second Library to Be Removed

**TesseractOcrMaui:** Teams that implemented PDF support added a second library (PDFium, PdfPig, or a cloud renderer) to convert PDF pages to images before passing them to `RecognizeTextAsync`. After migrating to IronOCR, that second library and all its page-rendering code can be deleted.

**Solution:** Remove the PDF rendering library and replace the entire page-extraction pipeline with `input.LoadPdf()`:

```csharp
// Before: PDF library + manual temp file management (50+ lines)
using var pdfDoc = PdfDocument.Open(pdfPath);
var results = new List<string>();
foreach (var page in pdfDoc.GetPages())
{
    var tempImagePath = Path.Combine(FileSystem.CacheDirectory, $"page_{page.Number}.png");
    RenderPageToImage(page, tempImagePath, dpi: 300);
    await _tesseract.InitAsync("eng");
    var r = await _tesseract.RecognizeTextAsync(tempImagePath);
    results.Add(r.RecognizedText ?? string.Empty);
    File.Delete(tempImagePath);
}
return string.Join("\n", results);

// After: native PDF support — 5 lines
var ocr = new IronTesseract();
using var input = new OcrInput();
input.LoadPdf(pdfPath);
var result = ocr.Read(input);
return result.Text;
```

The [PDF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-pdfs/) covers page range selection, password-protected PDFs, and stream-based loading.

## TesseractOcrMaui Migration Checklist

### Pre-Migration Tasks

Audit the codebase to inventory all TesseractOcrMaui usage before changing any code:

```bash
# Find all files that reference TesseractOcrMaui namespaces
grep -r "TesseractOcrMaui" --include="*.cs" .

# Find all ITesseract injection points
grep -r "ITesseract" --include="*.cs" .

# Find all AddTesseractOcr registrations
grep -r "AddTesseractOcr" --include="*.cs" .

# Find all InitAsync calls
grep -r "InitAsync" --include="*.cs" .

# Find all RecognizeTextAsync calls
grep -r "RecognizeTextAsync" --include="*.cs" .

# Find traineddata asset declarations in project files
grep -r "tessdata" --include="*.csproj" .

# Find MauiAsset traineddata declarations
grep -r "MauiAsset" --include="*.csproj" .

# Identify projects with MAUI-only target frameworks that hold OCR logic
grep -r "net8.0-android\|net8.0-ios\|net8.0-windows" --include="*.csproj" .
```

Note every class that takes `ITesseract` in a constructor — those constructors will change. Note every project file that declares `<MauiAsset>` for traineddata — those declarations will be deleted. Identify whether a PDF rendering library is present and whether it is used exclusively for OCR preprocessing.

### Code Update Tasks

1. Run `dotnet remove package TesseractOcrMaui` in every project that references it
2. Run `dotnet add package IronOcr` in every project that will perform OCR
3. Run `dotnet add package IronOcr.Android` in MAUI projects targeting Android
4. Run `dotnet add package IronOcr.iOS` in MAUI projects targeting iOS
5. Run `dotnet add package IronOcr.Languages.*` for any non-English language previously bundled as traineddata
6. Add `IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";` at application startup in each entry-point project
7. Delete `Resources/Raw/tessdata/` and all `.traineddata` files from MAUI projects
8. Remove all `<MauiAsset Include="Resources\Raw\tessdata\*.traineddata" />` lines from `.csproj` files
9. Remove `builder.Services.AddTesseractOcr()` from all `MauiProgram.cs` files
10. Replace all `using TesseractOcrMaui;` and `using TesseractOcrMaui.Results;` with `using IronOcr;`
11. Remove `ITesseract` constructor parameters from all service and view-model classes
12. Replace `await _tesseract.InitAsync("eng")` calls with `ocr.Language = OcrLanguage.English;` if needed (default is English)
13. Replace `await _tesseract.RecognizeTextAsync(imagePath)` with `ocr.Read(input)` using an `OcrInput` instance
14. Replace `result.RecognizedText` with `result.Text`
15. Replace `if (!result.Success)` checks with try/catch blocks
16. If a PDF rendering library was added solely to support TesseractOcrMaui, remove it and replace page-extraction code with `input.LoadPdf()`
17. Change any MAUI-only target framework in class libraries that held OCR logic to `net8.0` or `netstandard2.1`

### Post-Migration Testing

- Verify that OCR produces text from a JPEG image captured by the device camera on both iOS and Android targets
- Verify that OCR produces text from the same image loaded via `byte[]` in the server-side API endpoint
- Confirm the shared class library compiles and runs identically when referenced from both MAUI and ASP.NET Core projects
- Test that PDF input works end-to-end without any temporary file creation
- Verify that `SaveAsSearchablePdf` output is indexable in a PDF viewer
- Confirm confidence scores are present on `result.Confidence` and on `page.Words[i].Confidence`
- Test that the MAUI app produces an error-free startup log with no traineddata file-not-found exceptions
- Verify that the `Resources/Raw/tessdata/` folder is absent from the MAUI app bundle in release builds
- Run a parallel batch job with 10 or more documents to confirm thread safety
- Confirm that `InitAsync` removal has not left orphaned semaphore or `_isInitialized` state variables in any service class

## Key Benefits of Migrating to IronOCR

**One Codebase Across the Entire Product.** After migration, every project in the solution — MAUI mobile app, ASP.NET Core API, Azure Function, background worker — calls the same `DocumentOcrService` class from the same shared library. Language configuration, preprocessing settings, and accuracy tuning happen in one place. When a new document type requires a new preprocessing filter, the change is made once and takes effect everywhere.

**Server-Side Deployment Without Rewriting.** IronOCR deploys to Linux containers, Windows Server, Azure App Service, AWS Lambda, and any other .NET 8 runtime target without modifications. The same `IronTesseract` instance that processes mobile camera captures processes server-side PDF uploads. The [Azure deployment guide](https://ironsoftware.com/csharp/ocr/get-started/azure/) and [AWS deployment guide](https://ironsoftware.com/csharp/ocr/get-started/aws/) document the platform-specific configuration steps.

**PDF Processing Without a Second Library.** Native PDF input through `input.LoadPdf()` eliminates the PDF rendering library, the page-by-page image extraction loop, the temporary file management, and the cleanup code that TesseractOcrMaui's architecture required. Scanned PDF contracts, invoices, and identity documents load in one line. The same OCR pass that extracts text can produce a searchable PDF with `result.SaveAsSearchablePdf()` — a capability that TesseractOcrMaui cannot provide at any level.

**Preprocessing That Handles Real Mobile Images.** `input.Deskew()`, `input.DeNoise()`, `input.Binarize()`, and `input.Sharpen()` are single-method calls that apply calibrated image corrections before the Tesseract engine sees the data. Teams that were accepting 40–60% accuracy on low-light mobile captures without preprocessing commonly see 85–90%+ after adding a three-filter pipeline. No SkiaSharp, no ImageSharp, no algorithm implementation required. The [image quality correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/) documents every available filter and when to apply it.

**Commercial Support With a Defined Escalation Path.** Iron Software provides email support for all IronOCR license tiers and priority phone and chat support at the Professional and Enterprise levels. When a platform update breaks native library resolution on a specific Android API level — the kind of failure that TesseractOcrMaui's GitHub issue queue handles on volunteer time — there is an actual engineering team with a response obligation. Perpetual licensing starts at $749 for the Lite tier; the [licensing page](https://ironsoftware.com/csharp/ocr/licensing/) lists all tiers and their included support levels.

**125+ Languages Through NuGet Without App Bundle Inflation.** TesseractOcrMaui bundles traineddata files inside the MAUI app — each language adds 10–50 MB to the app download size. IronOCR language packs install through NuGet and are included only in server-side builds or in platform builds where they are explicitly referenced. Mobile app bundles stay lean; server-side builds get the full language set. Adding a new language is one `dotnet add package` command with no project file changes and no file management. The full [languages catalog](https://ironsoftware.com/csharp/ocr/languages/) lists all 125+ available packs.
