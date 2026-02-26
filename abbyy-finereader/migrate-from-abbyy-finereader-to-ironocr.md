# Migrating from ABBYY FineReader to IronOCR

This guide walks .NET developers through every step of replacing ABBYY FineReader Engine SDK with [IronOCR](https://ironsoftware.com/csharp/ocr/). It covers the mechanical steps of removing COM dependencies and SDK installer artifacts, maps ABBYY's API to IronOCR equivalents, and provides before/after code examples for the patterns most commonly found in production ABBYY integrations. The migration targets teams that have decided the enterprise cost and deployment complexity of ABBYY no longer aligns with their project requirements.

## Why Migrate from ABBYY FineReader

ABBYY FineReader Engine is a capable OCR platform, but its architecture was designed for enterprise Windows shops with dedicated infrastructure teams. When a .NET team's actual workload is invoice processing, contract digitization, or scanned form extraction, that architecture becomes a liability rather than an asset.

**COM Interop Debt Compounds Over Time.** Every ABBYY integration in .NET runs through a COM interop layer. COM objects require explicit lifecycle management: create, initialize, process, then close in a `finally` block or the process leaks memory. Every code path that touches ABBYY carries this pattern. Over two or three years of feature additions, that lifecycle ceremony propagates through service classes, background workers, and request handlers. The result is 30-50% boilerplate in every OCR-related class that disappears entirely when you switch to `IronTesseract`.

**SDK Installer Blocks Modern Deployment Patterns.** ABBYY deploys via a Windows SDK installer that places binaries, language data, runtime files, and license files at hard-coded paths. Containerizing a service that uses ABBYY requires either baking a 300+ MB custom base image from that installer output or mounting volumes with license files at startup. Neither approach fits a standard Kubernetes or cloud-native pipeline. IronOCR is a NuGet package: the same `dotnet restore` that pulls every other dependency pulls the complete OCR engine.

**Per-Page Licensing Turns Volume Into a Cost Center.** ABBYY's volume-based licensing models charge per page processed above included thresholds. An application that processes 50,000 documents per month at launch and reaches 500,000 two years later has its OCR costs growing in direct proportion to its success. IronOCR charges a flat fee for the license — a team processing two million pages a month pays exactly the same license cost as one processing two thousand.

**Language Data Requires Manual Deployment Coordination.** ABBYY language packs live as files in the SDK runtime directory. Adding a language means identifying the correct data files, copying them to the right path on every deployment target, and updating CI/CD scripts to include them. On IronOCR, adding French is `dotnet add package IronOcr.Languages.French` — the package manager handles the rest.

**License File Failures Hit Production Without Warning.** ABBYY licenses live as `.lic` and `.key` files that must be present at specific disk paths when `loader.GetEngineObject()` runs. If those files are missing on a new production server — wrong deployment script, failed file copy, permissions issue — the call throws at startup. An expired license fails the same way. IronOCR's [licensing](https://ironsoftware.com/csharp/ocr/licensing/) is a string key assigned in startup code, storable in any secrets manager, with validation checked by `IronOcr.License.IsValidLicense` before the application accepts traffic.

**Thread Safety Requires a Single Shared Engine Instance.** ABBYY's engine is not trivially thread-safe for concurrent `CreateFRDocument` calls from multiple threads. Production implementations use locking strategies or processor pools. IronOCR's `IronTesseract` is stateless: spin up one instance per thread, run recognition concurrently without locks, dispose when done.

### The Fundamental Problem

```csharp
// ABBYY: COM loader + license path validation + profile load — before a single pixel of OCR runs
var loader = new EngineLoader();
var engine = loader.GetEngineObject(
    @"C:\Program Files\ABBYY SDK\FineReader Engine\Bin",  // Breaks on every new machine
    @"C:\Program Files\ABBYY SDK\License"                 // Fails if .lic file is missing
);
engine.LoadPredefinedProfile("DocumentConversion_Accuracy");
var langParams = engine.CreateLanguageParams();
langParams.Languages.Add("English");
```

```csharp
// IronOCR: the entire initialization
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
var ocr = new IronTesseract();
```

## IronOCR vs ABBYY FineReader: Feature Comparison

The following table covers the capabilities relevant to teams evaluating this migration.

| Feature | ABBYY FineReader Engine | IronOCR |
|---|---|---|
| **Installation** | SDK installer (Windows) | `dotnet add package IronOcr` |
| **Acquisition** | Contact sales (4-12 weeks) | Self-service NuGet |
| **Licensing Model** | Enterprise, per-server or per-page | Perpetual, $749-$2,999 one-time |
| **License Management** | `.lic` + `.key` files on disk | String key in code or environment variable |
| **.NET Integration** | COM interop | Native .NET |
| **COM Dependency** | Yes | No |
| **Thread Safety** | Requires locking strategy | Full (one `IronTesseract` per thread) |
| **Languages Supported** | 190+ | 125+ |
| **Language Installation** | Runtime data files at SDK path | NuGet language packages |
| **PDF Input** | Yes (via `CreatePDFFile`) | Yes (native, `input.LoadPdf()`) |
| **Searchable PDF Output** | Yes (export pipeline) | Yes (`result.SaveAsSearchablePdf()`) |
| **Automatic Preprocessing** | Profile-based | Built-in (Deskew, DeNoise, Contrast, Binarize, Sharpen) |
| **Region-Based OCR** | Zone objects (`CreateZone`, `SetBounds`) | `CropRectangle` parameter |
| **Barcode Reading** | Yes | Yes (`ocr.Configuration.ReadBarCodes = true`) |
| **Cross-Platform** | Windows, Linux, macOS | Windows, Linux, macOS, Docker, Azure, AWS |
| **Docker Deployment** | Custom base image required | Standard .NET base image + `libgdiplus` |
| **Confidence Scoring** | Yes | Yes (`result.Confidence`) |
| **Time to First OCR Result** | 4-12 weeks (procurement) | Same day |

## Quick Start: ABBYY FineReader to IronOCR Migration

### Step 1: Replace NuGet Package

ABBYY FineReader Engine has no NuGet package. Remove it by uninstalling the SDK and removing the manual assembly reference from your project file:

```xml
<!-- Remove these lines from your .csproj -->
<Reference Include="FREngine">
  <HintPath>C:\Program Files\ABBYY SDK\FineReader Engine\Bin\FREngine.dll</HintPath>
</Reference>
```

Then remove the `FREngine.dll` COM interop reference from Visual Studio's References node, or delete the corresponding entry from your project file directly. Install IronOCR from [NuGet](https://www.nuget.org/packages/IronOcr):

```bash
dotnet add package IronOcr
```

### Step 2: Update Namespaces

```csharp
// Before (ABBYY)
using FREngine;
using ABBYY.FineReader;

// After (IronOCR)
using IronOcr;
```

### Step 3: Initialize License

Add this once at application startup, before any OCR calls:

```csharp
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
```

Store the key in an environment variable or secrets manager for production deployments:

```csharp
IronOcr.License.LicenseKey = Environment.GetEnvironmentVariable("IRONOCR_LICENSE_KEY");
```

## Code Migration Examples

### Engine Lifecycle in a Windows Service vs Stateless IronTesseract

ABBYY's engine initialization ceremony belongs in a service wrapper because the `EngineLoader` and `IEngine` objects are expensive to create. Most production integrations wrap the engine in a singleton service with explicit startup and teardown methods.

**ABBYY FineReader Approach:**

```csharp
using FREngine;

public class DocumentOcrService : IHostedService, IDisposable
{
    private IEngine _engine;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Step 1: Create loader — requires COM interop registration
        var loader = new EngineLoader();

        // Step 2: Load engine from SDK path — throws if license files are missing
        _engine = loader.GetEngineObject(
            @"C:\Program Files\ABBYY SDK\FineReader Engine\Bin",
            @"C:\Program Files\ABBYY SDK\License"
        );

        // Step 3: Load profile before any recognition work
        _engine.LoadPredefinedProfile("DocumentConversion_Accuracy");

        return Task.CompletedTask;
    }

    public string ProcessDocument(string imagePath)
    {
        // Document must be created and destroyed per call
        var document = _engine.CreateFRDocument();
        try
        {
            document.AddImageFile(imagePath, null, null);
            document.Process(null);
            return document.PlainText.Text;
        }
        finally
        {
            document.Close(); // Memory leaks if omitted
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _engine = null; // COM cleanup
        return Task.CompletedTask;
    }

    public void Dispose() => _engine = null;
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public class DocumentOcrService
{
    // No startup, no shutdown, no COM lifecycle
    // IronTesseract is stateless — create per call or reuse per thread

    public string ProcessDocument(string imagePath)
    {
        return new IronTesseract().Read(imagePath).Text;
    }
}
```

`IronTesseract` has no engine lifecycle. It initializes internally on first use and requires no explicit shutdown. The hosted service wrapper, the `IEngine` field, and the `StartAsync`/`StopAsync` methods all disappear. If the application processes documents concurrently, each thread creates its own `IronTesseract` instance — no locking required. The [IronTesseract setup guide](https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/) covers configuration options including `TesseractVersion` and `Configuration` properties.

### Recognition Language Setup

ABBYY language configuration involves creating a `LanguageParams` object, adding language name strings that must match installed data files, and associating those params with the engine before any document is processed. Each additional language requires the corresponding data files deployed at the runtime path.

**ABBYY FineReader Approach:**

```csharp
using FREngine;

// Engine must already be initialized with sdkPath and licensePath
private void ConfigureLanguages(IEngine engine, string[] languageCodes)
{
    // Create language parameters object
    var langParams = engine.CreateLanguageParams();

    // Add each language — string names must match installed data file names
    // Missing data file causes runtime failure
    foreach (var lang in languageCodes)
    {
        langParams.Languages.Add(lang);  // e.g., "English", "French", "German"
    }

    // Language params are associated at the profile level, not per-document
    // Changing languages requires reloading profile or reinitializing engine
    engine.LoadPredefinedProfile("DocumentConversion_Accuracy");
}

public string RecognizeFrenchDocument(IEngine engine, string imagePath)
{
    var langParams = engine.CreateLanguageParams();
    langParams.Languages.Add("French");  // Requires FrenchLanguage data files at runtime path

    var document = engine.CreateFRDocument();
    try
    {
        document.AddImageFile(imagePath, null, null);
        document.Process(null);
        return document.PlainText.Text;
    }
    finally
    {
        document.Close();
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

// Single language — install IronOcr.Languages.French via NuGet first
var ocr = new IronTesseract();
ocr.Language = OcrLanguage.French;
var result = ocr.Read("french-document.jpg");
Console.WriteLine(result.Text);

// Multiple simultaneous languages — operator overload, no data file management
var multiOcr = new IronTesseract();
multiOcr.Language = OcrLanguage.French + OcrLanguage.German + OcrLanguage.English;
var multiResult = multiOcr.Read("multilingual-contract.jpg");
Console.WriteLine(multiResult.Text);
```

Language packs install as standard NuGet packages (`dotnet add package IronOcr.Languages.French`). No data files to deploy manually, no path configuration, no engine reinitialization when switching languages. The [multiple languages guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-multiple-languages/) covers combining languages and the [languages index](https://ironsoftware.com/csharp/ocr/languages/) lists all 125+ available packs.

### Multi-Frame TIFF Processing

ABBYY processes multi-page TIFF files by iterating frames and adding each frame as a separate document page. The frame count must be retrieved from the TIFF object, then each frame is added to the document container individually.

**ABBYY FineReader Approach:**

```csharp
using FREngine;

public string ProcessMultiFrameTiff(IEngine engine, string tiffPath)
{
    var document = engine.CreateFRDocument();

    try
    {
        // Must add each frame individually — no automatic multi-frame handling
        // Page count requires reading the TIFF metadata before processing
        var imageInfo = engine.CreateImageInfo();
        imageInfo.LoadImageFile(tiffPath);
        int frameCount = imageInfo.FrameCount;

        for (int i = 0; i < frameCount; i++)
        {
            // Each frame added with its frame index via image processing params
            var imgParams = engine.CreateImageProcessingParams();
            imgParams.FrameIndex = i;
            document.AddImageFile(tiffPath, imgParams, null);
        }

        document.Process(null);
        return document.PlainText.Text;
    }
    finally
    {
        document.Close();
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

// LoadImageFrames handles multi-frame TIFF automatically
using var input = new OcrInput();
input.LoadImageFrames("scanned-batch.tiff");

var ocr = new IronTesseract();
var result = ocr.Read(input);

// Per-page results accessible directly
foreach (var page in result.Pages)
{
    Console.WriteLine($"Frame {page.PageNumber}: {page.Text.Length} characters");
    Console.WriteLine(page.Text);
}
```

`OcrInput.LoadImageFrames` reads every frame in a multi-page TIFF without manual iteration. The result provides per-page access through `result.Pages`, including text, coordinate data, and confidence per frame. The [TIFF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-tiff-gif/) covers both multi-frame TIFF and animated GIF handling.

### Parallel Batch Processing

ABBYY's COM-based engine is not safe to call `CreateFRDocument` on concurrently from multiple threads without a synchronization strategy. Production batch processors typically maintain a pool of engine instances or serialize access through a lock. Either approach adds infrastructure that IronOCR eliminates.

**ABBYY FineReader Approach:**

```csharp
using FREngine;
using System.Collections.Concurrent;
using System.Threading;

public class AbbyyBatchProcessor
{
    // Pool required because engine is not safely concurrent
    private readonly SemaphoreSlim _engineLock = new SemaphoreSlim(1, 1);
    private IEngine _engine;

    public async Task<Dictionary<string, string>> ProcessBatchAsync(string[] imagePaths)
    {
        var results = new ConcurrentDictionary<string, string>();

        // Must serialize — one document at a time through single engine
        foreach (var imagePath in imagePaths)
        {
            await _engineLock.WaitAsync();
            try
            {
                var document = _engine.CreateFRDocument();
                try
                {
                    document.AddImageFile(imagePath, null, null);
                    document.Process(null);
                    results[imagePath] = document.PlainText.Text;
                }
                finally
                {
                    document.Close();
                }
            }
            finally
            {
                _engineLock.Release();
            }
        }

        return new Dictionary<string, string>(results);
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;
using System.Collections.Concurrent;
using System.Threading.Tasks;

public class OcrBatchProcessor
{
    public Dictionary<string, string> ProcessBatch(string[] imagePaths)
    {
        var results = new ConcurrentDictionary<string, string>();

        // IronTesseract is thread-safe — one instance per thread, fully parallel
        Parallel.ForEach(imagePaths, imagePath =>
        {
            var ocr = new IronTesseract();  // Each thread owns its instance
            var result = ocr.Read(imagePath);
            results[imagePath] = result.Text;
        });

        return new Dictionary<string, string>(results);
    }
}
```

Each `IronTesseract` instance is independent. `Parallel.ForEach` saturates available CPU cores without any shared state, locks, or serialization. The ABBYY version processes documents sequentially despite the async wrapper; the IronOCR version processes them truly in parallel. The [multithreading example](https://ironsoftware.com/csharp/ocr/examples/csharp-tesseract-multithreading-for-speed/) demonstrates this pattern with timing comparisons. For higher-level throughput control, see the [speed optimization guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-fast-configuration/).

### Document Export Pipeline

ABBYY supports multiple export formats through its `Export` method with `FileExportFormatEnum` values. Exporting to DOCX, RTF, or plain text requires creating format-specific export parameter objects, then calling `document.Export` with the appropriate enum value and parameters object.

**ABBYY FineReader Approach:**

```csharp
using FREngine;

public class AbbyyExporter
{
    private IEngine _engine;

    public void ExportToMultipleFormats(string imagePath, string outputDir)
    {
        var document = _engine.CreateFRDocument();

        try
        {
            document.AddImageFile(imagePath, null, null);
            document.Process(null);

            string baseName = Path.GetFileNameWithoutExtension(imagePath);

            // Export as plain text
            document.Export(
                Path.Combine(outputDir, baseName + ".txt"),
                FileExportFormatEnum.FEF_TextUnicodeDefaults,
                null
            );

            // Export as searchable PDF (requires PDF export params)
            var pdfParams = _engine.CreatePDFExportParams();
            pdfParams.Scenario = PDFExportScenarioEnum.PDES_Balanced;
            pdfParams.UseOriginalPaperSize = true;
            document.Export(
                Path.Combine(outputDir, baseName + ".pdf"),
                FileExportFormatEnum.FEF_PDF,
                pdfParams
            );

            // Export as DOCX
            var docxParams = _engine.CreateDOCXExportParams();
            document.Export(
                Path.Combine(outputDir, baseName + ".docx"),
                FileExportFormatEnum.FEF_DOCX,
                docxParams
            );
        }
        finally
        {
            document.Close();
        }
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public class OcrExporter
{
    public void ExportToMultipleFormats(string imagePath, string outputDir)
    {
        var ocr = new IronTesseract();
        var result = ocr.Read(imagePath);
        string baseName = Path.GetFileNameWithoutExtension(imagePath);

        // Plain text — direct property access
        File.WriteAllText(
            Path.Combine(outputDir, baseName + ".txt"),
            result.Text
        );

        // Searchable PDF — one method call, no parameter objects
        result.SaveAsSearchablePdf(
            Path.Combine(outputDir, baseName + ".pdf")
        );

        // hOCR format — for document management systems
        result.SaveAsHocrFile(
            Path.Combine(outputDir, baseName + ".hocr")
        );
    }
}
```

IronOCR's `OcrResult` exposes `.Text` directly and provides output methods without parameter objects or format enums. The `SaveAsSearchablePdf` call handles PDF export in one line versus ABBYY's three-step parameter/export sequence. The [searchable PDF guide](https://ironsoftware.com/csharp/ocr/how-to/searchable-pdf/) covers page range options and compression settings. The [hOCR export guide](https://ironsoftware.com/csharp/ocr/how-to/html-hocr-export/) covers the HOCR format for systems that consume position-aware OCR output.

## ABBYY FineReader API to IronOCR Mapping Reference

| ABBYY FineReader Engine | IronOCR Equivalent |
|---|---|
| `new EngineLoader()` | Not required |
| `loader.GetEngineObject(sdkPath, licensePath)` | `new IronTesseract()` |
| `engine.LoadPredefinedProfile("...")` | Not required (handled internally) |
| `engine.CreateLanguageParams()` | Not required |
| `langParams.Languages.Add("French")` | `ocr.Language = OcrLanguage.French` |
| `langParams.Languages.Add("English")` + `langParams.Languages.Add("German")` | `ocr.Language = OcrLanguage.English + OcrLanguage.German` |
| `engine.CreateFRDocument()` | `new OcrInput()` |
| `engine.CreateFRDocumentFromImage(path, null)` | `ocr.Read(path)` |
| `document.AddImageFile(path, null, null)` | `input.LoadImage(path)` |
| `imageInfo.LoadImageFile(tiff)` + `frameCount` loop | `input.LoadImageFrames(tiff)` |
| `engine.CreatePDFFile()` then `pdfFile.Open(path, null, null)` | `input.LoadPdf(path)` |
| `document.Process(null)` | `ocr.Read(input)` |
| `document.PlainText.Text` | `result.Text` |
| `frDocument.Pages[i].PlainText.Text` | `result.Pages[i].Text` |
| `page.Layout.Blocks` + `BlockTypeEnum.BT_Table` check | `result.Pages` + word coordinate data |
| `block.GetAsTableBlock()` | `result.Pages[i].Lines` (with coordinates) |
| `engine.CreatePDFExportParams()` | Not required |
| `document.Export(path, FEF_PDF, params)` | `result.SaveAsSearchablePdf(path)` |
| `document.Export(path, FEF_TextUnicodeDefaults, null)` | `File.WriteAllText(path, result.Text)` |
| `engine.CreateDOCXExportParams()` + Export | Not directly supported |
| `document.Close()` | Handled by `using` on `OcrInput` |
| `_engine.GetLicenseInfo().ExpirationDate` | `IronOcr.License.IsValidLicense` |
| License files (`ABBYY.lic`, `ABBYY.key`) | `IronOcr.License.LicenseKey = "key"` |
| `engine.CreateZone()` + `zone.SetBounds(x, y, w, h)` | `new CropRectangle(x, y, width, height)` |

## Common Migration Issues and Solutions

### Issue 1: COM Registration Errors After Removing SDK

**ABBYY:** After removing `FREngine.dll` from project references, the build may still fail with `Could not load type 'FREngine.EngineLoader'` or COM interop errors from classes that retained the old namespace.

**Solution:** Search for all `FREngine` and `ABBYY.FineReader` usages before removing the reference. Any class that implements `IDisposable` specifically to null out an `IEngine` field needs its disposal logic replaced with `using` blocks on `OcrInput`:

```csharp
// Before: explicit Close in finally
var document = _engine.CreateFRDocument();
try { document.Process(null); }
finally { document.Close(); }

// After: using pattern on OcrInput
using var input = new OcrInput();
input.LoadImage(imagePath);
var result = new IronTesseract().Read(input);
```

### Issue 2: Recognition Profile Has No Equivalent

**ABBYY:** Code that calls `engine.LoadPredefinedProfile("DocumentConversion_Speed")` or `engine.LoadPredefinedProfile("FieldLevelRecognition")` uses ABBYY-specific profiles to balance accuracy against throughput. There is no IronOCR equivalent property named `Profile`.

**Solution:** IronOCR exposes the same tradeoffs through `IronTesseract.Configuration`. For speed optimization, set `ocr.Configuration.TesseractVersion = TesseractVersion.Tesseract5` (default) and reduce preprocessing filters. For maximum accuracy, add the full preprocessing pipeline:

```csharp
// Speed-optimized
var ocr = new IronTesseract();
// No preprocessing — fastest path
var result = ocr.Read("clean-document.jpg");

// Accuracy-optimized for difficult inputs
var ocr = new IronTesseract();
using var input = new OcrInput();
input.LoadImage("degraded-scan.jpg");
input.Deskew();
input.DeNoise();
input.Contrast();
input.Binarize();
var result = ocr.Read(input);
```

The [image quality correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/) explains which filters address which input quality problems. The [speed optimization guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-fast-configuration/) covers configuration properties that reduce processing time on clean documents.

### Issue 3: License File Deployment Step Remains in CI/CD

**ABBYY:** Build pipelines typically contain a step that copies `ABBYY.lic` and `ABBYY.key` from a secure store to the deployment target. After migration, teams sometimes forget to remove this step, leaving dead deployment code that references paths that no longer exist.

**Solution:** Remove the license file copy step entirely. Replace it with an environment variable injection step:

```yaml
# Remove these CI/CD steps after migration:
# - name: Copy ABBYY license files
#   run: |
#     cp $SECRETS_PATH/ABBYY.lic $DEPLOY_PATH/License/
#     cp $SECRETS_PATH/ABBYY.key $DEPLOY_PATH/License/

# Add this instead (environment variable injection):
# - name: Set IronOCR license
#   env:
#     IRONOCR_LICENSE_KEY: ${{ secrets.IRONOCR_LICENSE }}
```

And in application startup:

```csharp
IronOcr.License.LicenseKey = Environment.GetEnvironmentVariable("IRONOCR_LICENSE_KEY")
    ?? throw new InvalidOperationException("IRONOCR_LICENSE_KEY not set");
```

### Issue 4: Engine Not Thread-Safe — Existing Locking Code

**ABBYY:** Applications that call ABBYY from multiple threads typically contain `SemaphoreSlim`, `lock` statements, or thread-local engine instances to avoid COM threading issues. This synchronization code is specific to ABBYY's threading model.

**Solution:** Delete all synchronization code wrapping ABBYY calls. IronOCR's `IronTesseract` is safe to instantiate per-thread:

```csharp
// Remove all of this:
// private readonly SemaphoreSlim _engineLock = new SemaphoreSlim(1, 1);
// await _engineLock.WaitAsync();
// try { ... } finally { _engineLock.Release(); }

// Replace with:
Parallel.ForEach(documents, doc =>
{
    var ocr = new IronTesseract(); // One per thread — no lock needed
    results[doc.Id] = ocr.Read(doc.Path).Text;
});
```

### Issue 5: `CreateImageInfo` / `FrameCount` Pattern for TIFF

**ABBYY:** Code that reads frame counts from TIFF files using `engine.CreateImageInfo()` and `imageInfo.LoadImageFile()` before looping through frames has no direct equivalent in IronOCR because `OcrInput.LoadImageFrames` handles frame enumeration internally.

**Solution:** Delete the frame-counting loop entirely:

```csharp
// Remove:
// var imageInfo = engine.CreateImageInfo();
// imageInfo.LoadImageFile(tiffPath);
// for (int i = 0; i < imageInfo.FrameCount; i++) { document.AddImageFile(...) }

// Replace with:
using var input = new OcrInput();
input.LoadImageFrames("multi-page-scan.tiff");
var result = new IronTesseract().Read(input);
// result.Pages contains one entry per TIFF frame
```

### Issue 6: DOCX Export Has No Direct Equivalent

**ABBYY:** `document.Export(path, FileExportFormatEnum.FEF_DOCX, docxParams)` produces a Word document. IronOCR does not produce DOCX output directly.

**Solution:** IronOCR produces searchable PDFs and structured text data. For workflows that required DOCX output, the practical migration path is to produce a searchable PDF and convert it downstream, or extract structured text and write it to a DOCX using a library such as Open XML SDK:

```csharp
// IronOCR to searchable PDF (closest equivalent)
var result = new IronTesseract().Read(inputPath);
result.SaveAsSearchablePdf(outputPath.Replace(".docx", ".pdf"));

// Or extract structured text for downstream DOCX generation
foreach (var paragraph in result.Paragraphs)
{
    Console.WriteLine(paragraph.Text);
    // Write to DOCX via Open XML SDK or similar
}
```

The [read results guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) covers accessing paragraphs, lines, words, and character-level coordinate data for downstream processing.

## ABBYY FineReader Migration Checklist

### Pre-Migration Tasks

Audit the codebase before making any changes:

```bash
# Find all files using ABBYY namespaces
grep -r "using FREngine" --include="*.cs" .
grep -r "using ABBYY" --include="*.cs" .

# Find engine lifecycle patterns
grep -r "EngineLoader\|GetEngineObject\|LoadPredefinedProfile" --include="*.cs" .

# Find document lifecycle patterns
grep -r "CreateFRDocument\|document\.Close\|AddImageFile\|document\.Process" --include="*.cs" .

# Find language configuration
grep -r "CreateLanguageParams\|langParams\.Languages" --include="*.cs" .

# Find export calls
grep -r "FileExportFormatEnum\|CreatePDFExportParams\|document\.Export" --include="*.cs" .

# Find license file references in CI/CD and deployment scripts
grep -r "ABBYY\.lic\|ABBYY\.key" .
```

Document every class that holds an `IEngine` or `IFRDocument` field. Note which export formats are in use — DOCX output requires an alternative approach (see Issue 6 above).

### Code Update Tasks

1. Remove `FREngine.dll` reference from all `.csproj` files
2. Run `dotnet add package IronOcr` in each project that used ABBYY
3. Add `IronOcr.License.LicenseKey = ...` at application startup (`Program.cs` or startup class)
4. Install language NuGet packages for each non-English language (`dotnet add package IronOcr.Languages.French`, etc.)
5. Delete all `EngineLoader`, `GetEngineObject`, and `LoadPredefinedProfile` calls
6. Delete all `CreateLanguageParams` and `langParams.Languages.Add` calls
7. Replace `engine.CreateFRDocument()` + `document.AddImageFile()` + `document.Process()` with `new IronTesseract().Read(path)`
8. Replace multi-frame TIFF loops with `input.LoadImageFrames(tiffPath)`
9. Replace `document.PlainText.Text` with `result.Text`
10. Replace `frDocument.Pages[i].PlainText.Text` with `result.Pages[i].Text`
11. Replace `document.Export(..., FEF_PDF, pdfParams)` with `result.SaveAsSearchablePdf(path)`
12. Replace all `document.Close()` calls with `using` blocks on `OcrInput`
13. Delete `SemaphoreSlim` and locking code that serialized ABBYY engine access
14. Replace `engine.CreateZone()` / `zone.SetBounds()` / `page.Zones.Add()` with `new CropRectangle(x, y, width, height)` passed to `input.LoadImage()`
15. Remove license file copy steps from CI/CD pipelines
16. Update Docker images — remove SDK installation layer, add `libgdiplus` for Linux targets

### Post-Migration Testing

- Verify text extraction output on a representative sample of each document type (invoices, contracts, scanned forms)
- Confirm multi-page TIFF processing returns the same number of pages as ABBYY produced frames
- Test multi-language documents against the same inputs used for ABBYY baseline comparison
- Verify searchable PDF output is text-searchable in Adobe Reader and browser PDF viewers
- Run the parallel batch processor with the production concurrency level and confirm no exceptions
- Check `result.Confidence` on known-good documents to establish a baseline threshold for quality gates
- Test license key initialization from environment variable in the staging deployment environment
- Verify the Docker image builds and runs OCR without the ABBYY SDK volume mount
- Confirm CI/CD pipeline completes without the license file copy step
- Run a memory profiler on the batch processor to confirm no `OcrInput` objects are leaking (verify `using` placement)

## Key Benefits of Migrating to IronOCR

**Deployment Complexity Drops by an Order of Magnitude.** Every ABBYY deployment required SDK installation, license file placement, runtime path configuration, and validation that files were at the correct paths before the application would start. IronOCR deploys as a NuGet dependency. `dotnet publish` produces a self-contained artifact with the OCR engine included. The [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/) and [Azure setup guide](https://ironsoftware.com/csharp/ocr/get-started/azure/) show the complete configuration — both fit on a single page.

**COM Interop Is Gone.** Removing the COM layer eliminates an entire category of runtime failures: COM registration errors on new machines, apartment threading mismatches, `RCW` lifecycle bugs, and the 15-25 lines of `try/finally` boilerplate that every ABBYY document processing call required. The codebase shrinks. The error surface shrinks with it.

**Volume Growth No Longer Triggers Budget Reviews.** IronOCR's perpetual license covers unlimited document volume. An application that processes 10,000 documents per month in year one and 2,000,000 per month in year three carries the same OCR licensing cost. There are no per-page counters, no overage invoices, no volume tier renegotiations. The [licensing page](https://ironsoftware.com/csharp/ocr/licensing/) shows all tiers — the Professional license at $2,999 covers ten developers processing any volume on any number of deployment targets.

**Cross-Platform Deployment Opens New Infrastructure Options.** The ABBYY COM layer requires Windows. Teams that wanted to move document processing to Linux containers for cost or density reasons were blocked. IronOCR runs identically on Windows, Linux, and macOS from the same NuGet package. Migrating from ABBYY removes the Windows constraint from the OCR tier of the application stack. The [Linux deployment guide](https://ironsoftware.com/csharp/ocr/get-started/linux/) and [AWS deployment guide](https://ironsoftware.com/csharp/ocr/get-started/aws/) cover the complete setup for each environment.

**Parallel Throughput Is Available Without Infrastructure Work.** The locking strategies that serialized ABBYY engine access are gone. `IronTesseract` instances are independent: spin up one per thread, run `Parallel.ForEach` across a document batch, get results. Throughput scales with available CPU cores without any additional code. The [multithreading example](https://ironsoftware.com/csharp/ocr/examples/csharp-tesseract-multithreading-for-speed/) demonstrates wall-clock improvements on multi-core hardware.

**Language Configuration Is a Package Reference.** Adding German or Japanese OCR support to an ABBYY integration meant identifying data files, deploying them to runtime paths on every target machine, and handling failures when files were missing. With IronOCR, `dotnet add package IronOcr.Languages.German` adds the language pack as a versioned, reproducible NuGet dependency. The package manager ensures the data is present on every build. The [custom language pack guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-custom-language/) covers training and deploying custom language models for specialized domains.
