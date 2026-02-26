# Migrating from RapidOCR.NET to IronOCR

This guide covers the complete migration path from RapidOCR.NET (`RapidOcrNet`) to [IronOCR](https://ironsoftware.com/csharp/ocr/) for .NET developers who need to eliminate ONNX model file management from their OCR pipeline. It walks through package replacement, code translation, and the operational changes that follow when external model dependencies are removed entirely.

## Why Migrate from RapidOCR.NET

RapidOCR.NET works — for a narrow set of use cases, in controlled environments, where someone has already solved the model distribution problem. When any of those conditions change, the library's architectural constraints become engineering costs.

**ONNX Model Files Are a Deployment Artifact, Not a Package.** RapidOCR.NET requires four external files — `det.onnx`, `cls.onnx`, `rec.onnx`, and a character dictionary — before a single character can be recognized. These files are not bundled in the NuGet package. They live on GitHub release pages, require manual download, require explicit path configuration in code, and require custom MSBuild rules to copy on build. Every new developer, every CI pipeline, every deployment environment repeats that ceremony.

**Language Switching Means File Replacement, Not Configuration.** Changing from English OCR to Chinese OCR in RapidOCR.NET requires downloading a different recognition model and a different character dictionary, then rebuilding the engine instance. Spanish, French, German, Russian, Arabic, and more than 100 other languages have no available model in the RapidOCR model catalog at all. An application that needs to process documents across a mix of languages has no viable path inside RapidOCR.NET for the unsupported ones.

**Model Version Updates Require Manual Intervention.** When the upstream RapidOCR project releases improved model weights, teams must download new files, replace them in every environment, validate paths, and redeploy. There is no package restore step that handles this automatically. In a multi-environment setup with development, staging, and production, that propagation is a manual operation each time.

**The ONNX Runtime Dependency Adds Platform Complexity.** RapidOCR.NET depends on `Microsoft.ML.OnnxRuntime`, a package with platform-specific native binaries. CPU and GPU variants require different packages. A container image built for `linux/amd64` requires different binaries than one built for `linux/arm64`. Each deployment target needs validation that the correct runtime variant is present and compatible with the installed model files.

**Cold-Start Latency and Memory Footprint Are Fixed Costs.** Loading the three ONNX models at startup takes 2–5 seconds and holds 300–500 MB in memory for the duration of the process. That cost is paid regardless of OCR volume, making the library a poor fit for serverless functions, lightweight containers, or low-traffic services where the startup penalty is disproportionate to throughput.

**No Commercial Support Path.** RapidOCR.NET is maintained by a single community developer under Apache 2.0 license. Production incidents — ONNX Runtime version conflicts, inference failures on unusual image formats, memory growth under sustained load — go to a GitHub issues queue with no guaranteed response timeline and no SLA.

### The Fundamental Problem

Three ONNX model files plus a character dictionary, all downloaded separately, all configured by path:

```csharp
// RapidOcrNet: 4 external files required before any OCR can execute
var engine = new RapidOcrEngine(new RapidOcrOptions
{
    DetModelPath = "./models/det.onnx",     // ~3 MB — downloaded from GitHub
    ClsModelPath = "./models/cls.onnx",     // ~1 MB — downloaded from GitHub
    RecModelPath = "./models/rec_en.onnx",  // ~2-10 MB — language-specific download
    KeysPath     = "./models/en_keys.txt"   // character dictionary — language-specific
});
```

IronOCR has no model files, no path configuration, and no download step:

```csharp
// IronOCR: install the NuGet package, write one line
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
var text = new IronTesseract().Read("document.jpg").Text;
```

## IronOCR vs RapidOCR.NET: Feature Comparison

IronOCR and RapidOCR.NET overlap on basic image OCR. The gap opens on every surrounding concern.

| Feature | RapidOCR.NET | IronOCR |
|---|---|---|
| NuGet installation | Yes (`RapidOcrNet`) | Yes (`IronOcr`) |
| External model files required | Yes (4 files, manual download) | No |
| Path configuration required | Yes | No |
| MSBuild copy rules required | Yes | No |
| Works immediately after NuGet install | No | Yes |
| ONNX Runtime dependency | Yes (~30–50 MB) | No |
| Languages supported | ~5 (CJK + English only) | 125+ via NuGet language packs |
| Language switching | File swap + engine rebuild | Property assignment |
| European language support | No | Yes (30+) |
| Arabic / Hebrew support | No | Yes |
| Cyrillic (Russian, Ukrainian) support | No | Yes |
| Native PDF input | No | Yes |
| Password-protected PDF input | No | Yes |
| Searchable PDF output | No | Yes |
| Multi-page TIFF input | No | Yes |
| Stream and byte array input | Limited | Yes |
| Built-in image preprocessing | No | Yes (automatic + manual filters) |
| Deskew / DeNoise / Contrast filters | No | Yes |
| Structured output (paragraphs, lines, words) | Partial (blocks only) | Yes, with coordinates |
| Per-word confidence scores | Yes (per block) | Yes |
| Barcode reading during OCR | No | Yes |
| hOCR export | No | Yes |
| Thread-safe parallel processing | Limited | Yes (one instance per thread) |
| Cross-platform deployment | Requires ONNX Runtime binaries per platform | Yes (Windows, Linux, macOS, Docker) |
| Docker deployment | Manual model COPY instructions required | Out of the box |
| Cold-start overhead | 2–5 seconds (model loading) | Minimal |
| Commercial support | No | Yes |
| License | Apache 2.0 (free) | Perpetual ($749 Lite, $1,499 Pro, $2,999 Enterprise) |

## Quick Start: RapidOCR.NET to IronOCR Migration

### Step 1: Replace NuGet Package

Remove RapidOCR.NET and the ONNX Runtime dependency:

```bash
dotnet remove package RapidOcrNet
dotnet remove package Microsoft.ML.OnnxRuntime
```

Install IronOCR from [NuGet](https://www.nuget.org/packages/IronOcr):

```bash
dotnet add package IronOcr
```

### Step 2: Update Namespaces

Replace the RapidOCR.NET namespace with the IronOCR namespace:

```csharp
// Before (RapidOCR.NET)
using RapidOcrNet;

// After (IronOCR)
using IronOcr;
```

### Step 3: Initialize License

Add license initialization at application startup, before any `IronTesseract` calls:

```csharp
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
```

A free trial key is available from the [IronOCR licensing page](https://ironsoftware.com/csharp/ocr/licensing/).

## Code Migration Examples

### ONNX Model Path Configuration Removal

The most mechanical change in this migration is deleting the `RapidOcrOptions` configuration block and replacing it with a zero-argument constructor.

**RapidOCR.NET Approach:**

```csharp
using RapidOcrNet;

// Startup validation — written because a missing model crashes at runtime, not at install
private static void EnsureModelsPresent(string modelDir)
{
    var required = new[]
    {
        Path.Combine(modelDir, "det.onnx"),
        Path.Combine(modelDir, "cls.onnx"),
        Path.Combine(modelDir, "rec_en.onnx"),
        Path.Combine(modelDir, "en_keys.txt")
    };

    var missing = required.Where(f => !File.Exists(f)).ToList();
    if (missing.Any())
        throw new FileNotFoundException(
            $"Missing model files: {string.Join(", ", missing)}\n" +
            "Download from: https://github.com/RapidAI/RapidOCR/releases");
}

// Engine factory — called once at startup, held for lifetime of service
public RapidOcrEngine CreateEngine(string modelDir)
{
    EnsureModelsPresent(modelDir);
    return new RapidOcrEngine(new RapidOcrOptions
    {
        DetModelPath = Path.Combine(modelDir, "det.onnx"),
        ClsModelPath = Path.Combine(modelDir, "cls.onnx"),
        RecModelPath = Path.Combine(modelDir, "rec_en.onnx"),
        KeysPath     = Path.Combine(modelDir, "en_keys.txt"),
        UseGpu       = false,
        NumThreads   = Environment.ProcessorCount
    });
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

// No model validation, no path configuration, no GPU flags
// IronTesseract is thread-safe; create one per thread or on demand
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
var ocr = new IronTesseract();
```

The entire `EnsureModelsPresent` validation method, the `RapidOcrOptions` configuration object, and the engine factory class can be deleted. There are no model files to validate because IronOCR ships its engine internally as part of the NuGet package. The [IronTesseract setup guide](https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/) covers initialization options and license key placement in detail.

### Detection, Classification, and Recognition Pipeline Consolidation

RapidOCR.NET runs a three-stage ONNX pipeline — detection, direction classification, then recognition — and returns an unordered flat list of text blocks that the caller must sort and assemble. IronOCR exposes a single `.Read()` call backed by its internal Tesseract 5 engine, returning structured output with reading order already applied.

**RapidOCR.NET Approach:**

```csharp
using RapidOcrNet;

public class InvoiceTextExtractor
{
    private readonly RapidOcrEngine _engine;

    public InvoiceTextExtractor(string modelDir)
    {
        // Three separate ONNX models run in sequence on every call
        _engine = new RapidOcrEngine(new RapidOcrOptions
        {
            DetModelPath = Path.Combine(modelDir, "det.onnx"),   // Stage 1: detect text regions
            ClsModelPath = Path.Combine(modelDir, "cls.onnx"),   // Stage 2: classify direction
            RecModelPath = Path.Combine(modelDir, "rec_en.onnx"),// Stage 3: recognize characters
            KeysPath     = Path.Combine(modelDir, "en_keys.txt")
        });
    }

    public string ExtractInvoiceText(string imagePath)
    {
        var result = _engine.Run(imagePath);

        // Blocks are unordered — must sort by vertical position, then horizontal
        var orderedBlocks = result.TextBlocks
            .OrderBy(b => b.BoundingBox.Top)
            .ThenBy(b => b.BoundingBox.Left)
            .ToList();

        // Manual assembly — no paragraph or line structure
        return string.Join(Environment.NewLine,
            orderedBlocks.Select(b => b.Text));
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public class InvoiceTextExtractor
{
    private readonly IronTesseract _ocr = new IronTesseract();

    public string ExtractInvoiceText(string imagePath)
    {
        // Single call — detection, recognition, reading order all internal
        var result = _ocr.Read(imagePath);
        return result.Text; // Already in reading order
    }

    public IEnumerable<string> ExtractInvoiceParagraphs(string imagePath)
    {
        var result = _ocr.Read(imagePath);
        // Structured paragraphs with coordinates — no sorting or assembly needed
        foreach (var page in result.Pages)
            foreach (var paragraph in page.Paragraphs)
                yield return paragraph.Text;
    }
}
```

The three-stage pipeline is entirely internal to IronOCR. The `result.TextBlocks` list with its manual `OrderBy` chain collapses to `result.Text`. For callers that needed bounding-box data from `TextBlocks`, the `result.Pages[i].Paragraphs`, `.Lines`, and `.Words` collections provide equivalent coordinates through a structured API. The [read results how-to](https://ironsoftware.com/csharp/ocr/how-to/read-results/) and [OCR results features page](https://ironsoftware.com/csharp/ocr/features/ocr-results/) document the full structured output model.

### Custom Model Loading Replacement

Applications that need to switch OCR configurations at runtime — for example, routing documents through different recognition parameters based on document type — must rebuild the entire `RapidOcrEngine` in RapidOCR.NET because configuration is constructor-bound. IronOCR exposes engine configuration as properties that can be adjusted per-read on a single instance.

**RapidOCR.NET Approach:**

```csharp
using RapidOcrNet;

public class DocumentRouter
{
    private readonly string _modelDir;

    public DocumentRouter(string modelDir) => _modelDir = modelDir;

    // Must create separate engine instances per configuration
    // Each engine holds ~300-500 MB of loaded model weights
    private RapidOcrEngine BuildEnglishEngine() =>
        new RapidOcrEngine(new RapidOcrOptions
        {
            DetModelPath = Path.Combine(_modelDir, "det.onnx"),
            ClsModelPath = Path.Combine(_modelDir, "cls.onnx"),
            RecModelPath = Path.Combine(_modelDir, "en_rec.onnx"),
            KeysPath     = Path.Combine(_modelDir, "en_keys.txt")
        });

    private RapidOcrEngine BuildChineseEngine() =>
        new RapidOcrEngine(new RapidOcrOptions
        {
            DetModelPath = Path.Combine(_modelDir, "det.onnx"),
            ClsModelPath = Path.Combine(_modelDir, "cls.onnx"),
            RecModelPath = Path.Combine(_modelDir, "ch_rec.onnx"),  // separate download
            KeysPath     = Path.Combine(_modelDir, "ch_keys.txt")   // separate download
        });

    public string ProcessDocument(string imagePath, string language)
    {
        // Rebuild engine for each language — model reload cost on every switch
        using var engine = language == "chinese"
            ? BuildChineseEngine()
            : BuildEnglishEngine();

        var result = engine.Run(imagePath);
        return string.Join("\n", result.TextBlocks
            .OrderBy(b => b.BoundingBox.Top)
            .Select(b => b.Text));
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public class DocumentRouter
{
    // One instance handles all languages — language is a property, not a constructor param
    private readonly IronTesseract _ocr = new IronTesseract();

    public string ProcessDocument(string imagePath, string language)
    {
        // Language switch requires no model reload, no rebuild
        _ocr.Language = language switch
        {
            "chinese"  => OcrLanguage.ChineseSimplified,
            "japanese" => OcrLanguage.Japanese,
            "arabic"   => OcrLanguage.Arabic,
            "russian"  => OcrLanguage.Russian,
            _          => OcrLanguage.English
        };

        return _ocr.Read(imagePath).Text;
    }
}
```

No engine rebuild, no model reload, no separate download per language. Language packs for non-English targets install via NuGet — `dotnet add package IronOcr.Languages.ChineseSimplified` — and the restore step handles deployment automatically. The [multiple languages how-to](https://ironsoftware.com/csharp/ocr/how-to/ocr-multiple-languages/) covers language pack installation and the [languages index](https://ironsoftware.com/csharp/ocr/languages/) lists all 125+ available packs.

### Batch Processing Migration

RapidOCR.NET has no thread safety guarantees on a single `RapidOcrEngine` instance. Batch processing requires either a single-threaded queue or per-thread engine instantiation, each carrying its own 300–500 MB model footprint. IronOCR is explicitly thread-safe: create one `IronTesseract` per thread and run them concurrently without locks.

**RapidOCR.NET Approach:**

```csharp
using RapidOcrNet;

public class BatchOcrProcessor
{
    private readonly string _modelDir;

    public BatchOcrProcessor(string modelDir) => _modelDir = modelDir;

    // Thread-pool processing — each thread needs its own engine copy
    // 4 threads × 300-500 MB model footprint = 1.2-2 GB RAM minimum
    public Dictionary<string, string> ProcessBatch(IReadOnlyList<string> imagePaths)
    {
        var results = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

        Parallel.ForEach(imagePaths, new ParallelOptions { MaxDegreeOfParallelism = 4 },
            imagePath =>
            {
                // Each thread must create its own engine — not safe to share
                using var engine = new RapidOcrEngine(new RapidOcrOptions
                {
                    DetModelPath = Path.Combine(_modelDir, "det.onnx"),
                    ClsModelPath = Path.Combine(_modelDir, "cls.onnx"),
                    RecModelPath = Path.Combine(_modelDir, "rec_en.onnx"),
                    KeysPath     = Path.Combine(_modelDir, "en_keys.txt")
                });

                var result = engine.Run(imagePath);
                results[imagePath] = string.Join("\n",
                    result.TextBlocks
                          .OrderBy(b => b.BoundingBox.Top)
                          .Select(b => b.Text));
            });

        return new Dictionary<string, string>(results);
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public class BatchOcrProcessor
{
    // Thread-safe: create IronTesseract per thread, no shared state required
    public Dictionary<string, string> ProcessBatch(IReadOnlyList<string> imagePaths)
    {
        var results = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

        Parallel.ForEach(imagePaths, imagePath =>
        {
            // Lightweight construction — no model loading overhead per thread
            var ocr = new IronTesseract();
            var result = ocr.Read(imagePath);
            results[imagePath] = result.Text;
        });

        return new Dictionary<string, string>(results);
    }
}
```

The per-thread `RapidOcrEngine` instantiation disappears. IronOCR thread instances are lightweight — no external model load on construction. The [multithreading example](https://ironsoftware.com/csharp/ocr/examples/csharp-tesseract-multithreading-for-speed/) demonstrates concurrent processing patterns for high-throughput pipelines.

### Multi-Frame TIFF Processing

RapidOCR.NET accepts only single image files. Processing a multi-page TIFF — the standard format for fax-received documents and scanned archives — requires splitting it into individual frames with a separate imaging library, saving those frames to temporary files, running `engine.Run()` on each, and cleaning up afterward. IronOCR handles multi-frame TIFF natively through `OcrInput.LoadImageFrames`.

**RapidOCR.NET Approach:**

```csharp
using RapidOcrNet;
// Also requires: SixLabors.ImageSharp or System.Drawing for TIFF frame extraction

public class TiffOcrProcessor
{
    private readonly RapidOcrEngine _engine;

    public TiffOcrProcessor(string modelDir)
    {
        _engine = new RapidOcrEngine(new RapidOcrOptions
        {
            DetModelPath = Path.Combine(modelDir, "det.onnx"),
            ClsModelPath = Path.Combine(modelDir, "cls.onnx"),
            RecModelPath = Path.Combine(modelDir, "rec_en.onnx"),
            KeysPath     = Path.Combine(modelDir, "en_keys.txt")
        });
    }

    public string ProcessMultiPageTiff(string tiffPath)
    {
        var pageTexts = new List<string>();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // External library required to split TIFF frames
            var framePaths = SplitTiffIntoFrames(tiffPath, tempDir); // not in RapidOcrNet

            foreach (var framePath in framePaths)
            {
                var result = _engine.Run(framePath);
                pageTexts.Add(string.Join("\n",
                    result.TextBlocks
                          .OrderBy(b => b.BoundingBox.Top)
                          .Select(b => b.Text)));
            }
        }
        finally
        {
            // Clean up temp frame files
            Directory.Delete(tempDir, recursive: true);
        }

        return string.Join("\n\n", pageTexts);
    }

    private IEnumerable<string> SplitTiffIntoFrames(string tiffPath, string outputDir)
    {
        // Requires external library — implementation depends on what is installed
        throw new NotImplementedException("Add SixLabors.ImageSharp or similar");
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public class TiffOcrProcessor
{
    private readonly IronTesseract _ocr = new IronTesseract();

    public string ProcessMultiPageTiff(string tiffPath)
    {
        using var input = new OcrInput();
        input.LoadImageFrames(tiffPath); // All frames loaded — no external library needed

        var result = _ocr.Read(input);
        return result.Text; // Pages assembled in order automatically
    }

    public IEnumerable<(int PageNumber, string Text, double Confidence)> ProcessTiffWithPageData(string tiffPath)
    {
        using var input = new OcrInput();
        input.LoadImageFrames(tiffPath);

        var result = _ocr.Read(input);

        foreach (var page in result.Pages)
            yield return (page.PageNumber, page.Text, page.Confidence);
    }
}
```

No external imaging library, no temporary files, no cleanup logic. `LoadImageFrames` reads all TIFF frames into the `OcrInput` pipeline in a single call. The [TIFF and GIF input how-to](https://ironsoftware.com/csharp/ocr/how-to/input-tiff-gif/) covers frame selection, page range filtering, and memory-efficient handling of large multi-frame documents.

### Structured Data Extraction from Scanned Forms

RapidOCR.NET returns text blocks with bounding boxes but no higher-level document structure — no concept of paragraphs, lines, or words. Extracting individual fields from a scanned form requires writing coordinate intersection logic against the raw block list. IronOCR provides a structured result tree down to the character level, with coordinates at every level.

**RapidOCR.NET Approach:**

```csharp
using RapidOcrNet;

public class FormFieldExtractor
{
    private readonly RapidOcrEngine _engine;

    public FormFieldExtractor(string modelDir)
    {
        _engine = new RapidOcrEngine(new RapidOcrOptions
        {
            DetModelPath = Path.Combine(modelDir, "det.onnx"),
            ClsModelPath = Path.Combine(modelDir, "cls.onnx"),
            RecModelPath = Path.Combine(modelDir, "rec_en.onnx"),
            KeysPath     = Path.Combine(modelDir, "en_keys.txt")
        });
    }

    // Extract text within a defined region by filtering block coordinates manually
    public string ExtractFieldByRegion(string imagePath, float regionLeft, float regionTop,
                                        float regionRight, float regionBottom)
    {
        var result = _engine.Run(imagePath);

        // Filter blocks whose bounding box intersects the target region
        var blocksInRegion = result.TextBlocks
            .Where(b =>
                b.BoundingBox.Left   < regionRight  &&
                b.BoundingBox.Right  > regionLeft   &&
                b.BoundingBox.Top    < regionBottom &&
                b.BoundingBox.Bottom > regionTop)
            .OrderBy(b => b.BoundingBox.Top)
            .ThenBy(b => b.BoundingBox.Left);

        return string.Join(" ", blocksInRegion.Select(b => b.Text));
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public class FormFieldExtractor
{
    private readonly IronTesseract _ocr = new IronTesseract();

    // Use CropRectangle to OCR only the target region — no post-filter needed
    public string ExtractFieldByRegion(string imagePath, int x, int y, int width, int height)
    {
        var region = new CropRectangle(x, y, width, height);

        using var input = new OcrInput();
        input.LoadImage(imagePath, region);

        return _ocr.Read(input).Text;
    }

    // Extract all fields with their coordinates from a full-page scan
    public IEnumerable<(string Text, int X, int Y, double Confidence)> ExtractAllWords(string imagePath)
    {
        var result = _ocr.Read(imagePath);

        foreach (var page in result.Pages)
            foreach (var word in page.Words)
                yield return (word.Text, word.X, word.Y, word.Confidence);
    }
}
```

`CropRectangle` confines OCR to the exact region of interest, which is faster and more accurate than running full-page OCR and filtering results after the fact. Per-word coordinates and confidence values are available directly on `result.Pages[i].Words` without any manual bounding-box intersection code. The [region-based OCR how-to](https://ironsoftware.com/csharp/ocr/how-to/ocr-region-of-an-image/) and the [crop rectangle example](https://ironsoftware.com/csharp/ocr/examples/net-tesseract-content-area-rectangle-crop/) cover this pattern in detail.

## RapidOCR.NET API to IronOCR Mapping Reference

| RapidOCR.NET | IronOCR Equivalent |
|---|---|
| `using RapidOcrNet` | `using IronOcr` |
| `new RapidOcrEngine(new RapidOcrOptions { ... })` | `new IronTesseract()` |
| `RapidOcrOptions.DetModelPath` | Not needed — bundled internally |
| `RapidOcrOptions.ClsModelPath` | Not needed — bundled internally |
| `RapidOcrOptions.RecModelPath` | Not needed — bundled internally |
| `RapidOcrOptions.KeysPath` | Not needed — bundled internally |
| `RapidOcrOptions.UseGpu` | Not applicable — CPU-optimized internally |
| `RapidOcrOptions.NumThreads` | Use `Parallel.ForEach` with one `IronTesseract` per thread |
| `engine.Run(imagePath)` | `ocr.Read(imagePath)` |
| `engine.Dispose()` | `using var ocr = new IronTesseract()` |
| `result.TextBlocks` | `result.Pages[i].Words` / `.Lines` / `.Paragraphs` |
| `result.TextBlocks[i].Text` | `result.Words[i].Text` |
| `result.TextBlocks[i].Confidence` | `result.Words[i].Confidence` |
| `result.TextBlocks[i].BoundingBox.Top` | `result.Words[i].Y` |
| `result.TextBlocks[i].BoundingBox.Left` | `result.Words[i].X` |
| Manual `OrderBy(b => b.BoundingBox.Top)` sort | Not needed — `result.Text` is in reading order |
| `string.Join("\n", result.TextBlocks.Select(b => b.Text))` | `result.Text` |
| Language file swap (download different model) | `ocr.Language = OcrLanguage.French` |
| Engine rebuild for language change | Not needed — set `ocr.Language` per call |
| PDF-to-image + `engine.Run()` loop | `ocr.Read("document.pdf")` |
| Multi-frame TIFF manual frame split | `input.LoadImageFrames("document.tiff")` |
| No searchable PDF capability | `result.SaveAsSearchablePdf("output.pdf")` |
| No barcode capability | `ocr.Configuration.ReadBarCodes = true` |

## Common Migration Issues and Solutions

### Issue 1: Models Directory Still Exists After Migration

**RapidOCR.NET:** The `models/` directory in the project contains `det.onnx`, `cls.onnx`, `rec_en.onnx`, and `en_keys.txt`, along with MSBuild `<Content>` entries that copy them on build. After switching to IronOCR, this directory and those entries remain and still inflate the build output.

**Solution:** Delete the `models/` directory, remove the corresponding `<ItemGroup>` from `.csproj`, and remove any startup validation logic that checked for missing files. Also remove the `Microsoft.ML.OnnxRuntime` NuGet reference if it was installed separately. The published output of a .NET application using IronOCR contains no external model files.

```xml
<!-- Remove this entire block from .csproj -->
<ItemGroup>
  <Content Include="models\**\*.*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

### Issue 2: Per-Thread Engine Construction Pattern

**RapidOCR.NET:** Parallel processing code that created a new `RapidOcrEngine` per thread to avoid shared-state problems carried a significant memory cost: each engine instance loaded 300–500 MB of ONNX model weights independently.

**Solution:** IronOCR `IronTesseract` instances are thread-safe and lightweight. Create one per thread in a `Parallel.ForEach` without worrying about a per-instance model load cost. The IronOCR approach is identical to the Batch Processing Migration example above — `IronTesseract` handles this scenario with the same per-thread construction pattern, but without the 300–500 MB model load cost that each `RapidOcrEngine` instance carried. The [multithreading example](https://ironsoftware.com/csharp/ocr/examples/csharp-tesseract-multithreading-for-speed/) shows the standard pattern for high-throughput pipelines.

### Issue 3: Language Not Supported Exception

**RapidOCR.NET:** Code that routed non-CJK documents through RapidOCR.NET — or attempted to build an engine with a non-existent Spanish/French/German model — would throw at runtime with a file-not-found error or produce empty results.

**Solution:** Install the appropriate language pack NuGet package and set `ocr.Language` to the target `OcrLanguage` enum value. No model download, no engine rebuild, no additional code path for each language:

```bash
dotnet add package IronOcr.Languages.Spanish
dotnet add package IronOcr.Languages.French
dotnet add package IronOcr.Languages.Arabic
```

```csharp
var ocr = new IronTesseract();
ocr.Language = OcrLanguage.Spanish;
var result = ocr.Read("spanish-document.jpg");
```

The [custom language packs guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-custom-language/) covers advanced language configuration beyond the standard 125+ packs.

### Issue 4: Text Block Sorting Logic Breaks After Migration

**RapidOCR.NET:** Because `result.TextBlocks` was an unordered flat list, codebases typically contained `.OrderBy(b => b.BoundingBox.Top).ThenBy(b => b.BoundingBox.Left)` chains scattered throughout result-processing code.

**Solution:** Delete this sorting logic entirely. `result.Text` in IronOCR is already assembled in natural reading order. For code that also consumed bounding-box coordinates from the sorted blocks, replace the block reference with `result.Pages[i].Words[j]`:

```csharp
// Before: manual sort + coordinate extraction
var sorted = result.TextBlocks
    .OrderBy(b => b.BoundingBox.Top)
    .ThenBy(b => b.BoundingBox.Left);

foreach (var block in sorted)
    Console.WriteLine($"{block.Text} at ({block.BoundingBox.Left}, {block.BoundingBox.Top})");

// After: structured access, already in order
foreach (var page in result.Pages)
    foreach (var word in page.Words)
        Console.WriteLine($"{word.Text} at ({word.X}, {word.Y})");
```

### Issue 5: CI/CD Pipeline Fails After Model Files Are Removed

**RapidOCR.NET:** Build pipelines that cached or fetched the `models/` directory as a separate step — either from an artifact store, a shared S3 bucket, or a Git LFS repository — will fail when those steps find nothing to restore after the migration.

**Solution:** Remove the model file fetch and cache steps from the CI pipeline entirely. IronOCR's engine is restored as part of the standard `dotnet restore` step. No additional pipeline stages are required. For containerized deployments, remove any `COPY models/ ./models/` Docker instructions — the IronOCR [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/) documents the one required system package (`libgdiplus` on Debian/Ubuntu images) and nothing more.

### Issue 6: ONNX Runtime Version Conflicts After Partial Migration

**RapidOCR.NET:** Applications that also use other ONNX-based ML packages (ML.NET, ONNX object detection, etc.) may have had `Microsoft.ML.OnnxRuntime` pinned to a specific version for RapidOCR.NET compatibility. Removing RapidOCR.NET can expose version conflicts in those other packages.

**Solution:** Remove `Microsoft.ML.OnnxRuntime` from the explicit package list. IronOCR has no ONNX Runtime dependency, so removing the RapidOCR.NET reference eliminates the version pin entirely. Other ML packages that genuinely require ONNX Runtime can then resolve their own compatible version through standard NuGet dependency resolution without the RapidOCR.NET constraint.

## RapidOCR.NET Migration Checklist

### Pre-Migration Tasks

Audit the codebase for all RapidOCR.NET usage before making changes:

```bash
# Find all files that reference RapidOcrNet
grep -r "RapidOcrNet\|RapidOcrEngine\|RapidOcrOptions" --include="*.cs" .

# Find model path configuration
grep -r "DetModelPath\|ClsModelPath\|RecModelPath\|KeysPath" --include="*.cs" .

# Find MSBuild model copy entries
grep -r "det\.onnx\|cls\.onnx\|rec.*\.onnx\|keys\.txt" --include="*.csproj" .

# Find model validation logic
grep -r "ValidateModel\|models/" --include="*.cs" .

# Find ONNX Runtime references
grep -r "OnnxRuntime\|Microsoft\.ML" --include="*.csproj" .

# Find language-switching patterns (multiple engine instances per language)
grep -r "CreateEnglishEngine\|CreateChineseEngine\|rec_en\|ch_rec\|en_keys\|ch_keys" --include="*.cs" .
```

Inventory the results: note every place an engine is created, every place model paths are configured, every place text blocks are sorted, and every place PDF-to-image conversion feeds into `engine.Run()`.

### Code Update Tasks

1. Remove `RapidOcrNet` NuGet package reference from all `.csproj` files.
2. Remove `Microsoft.ML.OnnxRuntime` NuGet package reference from all `.csproj` files.
3. Install `IronOcr` NuGet package.
4. Install language pack NuGet packages for any non-English languages the application requires.
5. Delete the `models/` directory from the project and repository.
6. Remove `<Content Include="models\**\*.*">` MSBuild entries from all `.csproj` files.
7. Remove startup model validation methods (the `EnsureModelsPresent`-style methods).
8. Replace `using RapidOcrNet` with `using IronOcr` in all source files.
9. Replace `new RapidOcrEngine(new RapidOcrOptions { ... })` with `new IronTesseract()`.
10. Replace `engine.Run(imagePath)` with `ocr.Read(imagePath)`.
11. Replace `result.TextBlocks` assembly chains (`.OrderBy().Select(b => b.Text)`) with `result.Text`.
12. Replace coordinate-filter field extraction with `CropRectangle` region input.
13. Replace per-thread engine construction with per-thread `IronTesseract` construction.
14. Replace language-specific engine factory methods with `ocr.Language = OcrLanguage.X` assignments.
15. Remove PDF-to-image conversion code and replace with direct `ocr.Read("file.pdf")` calls.
16. Remove multi-frame TIFF frame-splitting code and replace with `input.LoadImageFrames("file.tiff")`.
17. Add `IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY"` at application startup.
18. Remove model file fetch and cache steps from CI/CD pipeline definitions.
19. Remove ONNX model `COPY` instructions from Dockerfiles.

### Post-Migration Testing

- Verify that all existing image OCR paths return text with accuracy equal to or better than RapidOCR.NET output.
- Confirm `result.Text` reading order matches the expected field sequence for each document type.
- Test language-switched reads for every `OcrLanguage` value the application uses.
- Run the parallel batch processor and confirm no thread contention errors or stale-result issues.
- Verify multi-frame TIFF processing returns the correct number of pages with correct per-page text.
- Test form field extraction via `CropRectangle` against the expected coordinate regions.
- Confirm the `models/` directory is absent from build output and deployment packages.
- Run the CI pipeline end-to-end and confirm no model fetch steps remain.
- Build and run a Docker container and confirm no `COPY models/` layer or file-not-found errors at startup.
- Test a startup time measurement to verify cold-start latency has decreased.

## Key Benefits of Migrating to IronOCR

**Deployment Is Now Deterministic.** `dotnet restore` and `dotnet publish` produce a complete, working OCR deployment with no external file dependencies. The same NuGet restore that installs the package version installs everything the engine needs to run. There are no model files to version separately, no CI cache steps to configure, and no deployment validation scripts to maintain. The pipeline is as simple as any other .NET package dependency.

**Language Coverage Scales With Business Requirements.** Adding support for a new document language means running `dotnet add package IronOcr.Languages.X` and setting `ocr.Language`. There is no upstream model availability check, no model download, and no engine refactor. Teams that start with English OCR and later need to process German contracts, Arabic invoices, or Russian purchase orders extend coverage without touching application architecture. All [125+ language packs](https://ironsoftware.com/csharp/ocr/languages/) follow the same installation pattern.

**Structured Output Eliminates Coordinate Assembly Code.** The `result.Pages`, `.Paragraphs`, `.Lines`, `.Words`, and `.Characters` hierarchy replaces the flat `TextBlocks` list and the sorting logic that worked around its lack of structure. Code that extracted reading-order text by sorting block coordinates is deleted. Code that needed per-word bounding boxes gets them from `word.X`, `word.Y`, `word.Width`, `word.Height` with no intersection filtering. The [OCR results features page](https://ironsoftware.com/csharp/ocr/features/ocr-results/) documents the full output model.

**PDF and TIFF Processing Require No External Libraries.** The two most common document formats beyond single-image JPGs — multi-page PDFs and multi-frame TIFFs — are handled natively by IronOCR. Every external library that was added to the dependency tree to support `engine.Run()` with PDF or TIFF input can be removed. Net result: fewer packages to update, fewer version compatibility surfaces, and simpler project files. The [PDF input how-to](https://ironsoftware.com/csharp/ocr/how-to/input-pdfs/) and [TIFF input how-to](https://ironsoftware.com/csharp/ocr/how-to/input-tiff-gif/) cover both formats in detail.

**Production Incidents Have a Support Path.** Commercial licenses include direct email support with a contact point for issues that cannot wait for a GitHub issue response. Teams with SLA obligations or business-critical document processing pipelines can escalate incidents to engineers who maintain the library rather than waiting for a community response. The [IronOCR documentation hub](https://ironsoftware.com/csharp/ocr/docs/) provides reference documentation alongside that support path.

**The $749 Perpetual License Is a One-Time Cost.** There is no per-page pricing, no per-transaction billing, and no annual renewal that re-opens the cost conversation. Development teams that priced out the engineering hours spent on model management, PDF conversion workarounds, CI pipeline maintenance, and unsupported-language escalations consistently find the comparison favorable against the license cost.
