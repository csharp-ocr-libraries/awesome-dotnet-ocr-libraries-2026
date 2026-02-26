# Migrating from PaddleSharp OCR to IronOCR

This guide walks .NET developers through a full migration from PaddleSharp OCR (`Sdcb.PaddleOCR`) to [IronOCR](https://ironsoftware.com/csharp/ocr/). It covers replacing inference session management, eliminating the OpenCV preprocessing dependency, removing backend selection logic for CPU, GPU, and OpenVINO, and migrating table recognition workflows. Each section provides before-and-after code drawn from PaddleSharp-specific patterns that do not appear in generic OCR comparisons.

## Why Migrate from PaddleSharp OCR

PaddleSharp exposes a deep learning inference pipeline at the application layer. That architecture gives you access to PaddlePaddle model performance but demands that your application manage what would otherwise be infrastructure concerns. The following pain points drive most .NET teams to look for alternatives.

**Inference Backend Configuration Is Application Code.** Choosing between CPU, GPU, and OpenVINO backends in PaddleSharp requires constructing and configuring `PaddleConfig` objects, selecting the right native runtime NuGet package for the deployment target, and conditionally branching your initialization code based on the hardware available at runtime. This logic lives in your application, not in the library, and it breaks when the target environment changes.

**OpenCV Is a Required Dependency for Image Input.** PaddleSharp cannot accept a file path or a stream directly. Every image passes through OpenCV's `Cv2.ImRead()` before it reaches the OCR engine. That forces `OpenCvSharp4` and a platform-specific `OpenCvSharp4.runtime.*` package into your dependency graph. Updating one platform runtime without the other causes runtime failures that are difficult to reproduce across environments.

**Inference Session Lifetime Requires Explicit Design.** `PaddleOcrAll` loads three model binaries from disk at construction time. That cost — measurable in hundreds of milliseconds — means the object cannot be instantiated per-request. Teams must design a lifecycle strategy: singleton, pooled, or scoped. In ASP.NET Core, this typically means a registered service with careful thread-safety analysis, because `PaddleOcrAll` shares underlying native state.

**Table Recognition Requires Separate Model Downloads.** Structured document extraction in PaddleSharp requires a dedicated table recognition model in addition to the standard three-stage detection/classification/recognition pipeline. That model is a fourth file to download, version, and configure. There is no unified API surface — table recognition uses a distinct code path with its own result type.

**No Searchable PDF Output.** PaddleSharp produces text strings. It cannot write searchable PDF files. Teams that need to archive scanned documents as text-searchable PDFs must integrate a separate PDF library, manage that additional dependency, and write a conversion layer. The output format gap is complete: no hOCR, no structured searchable PDF, no text-layer overlay.

**The Upstream Dependency Chain Is Not Owned by the .NET Community.** PaddleSharp wraps Baidu's PaddlePaddle inference framework. Model format changes between PaddleOCR versions have broken the .NET binding layer in the past. Most issue tracking, documentation, and release discussion happens in Chinese. For a .NET team without Mandarin speakers monitoring upstream projects, breaking changes arrive without warning.

### The Fundamental Problem

Selecting and initializing a backend in PaddleSharp requires configuration code that belongs in infrastructure, not in OCR logic:

```csharp
// PaddleSharp: Backend selection sprawls into application startup
// Simplified — see Sdcb.PaddleInference documentation for full API
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;

// CPU-only deployment
var config = PaddleConfig.FromModelDir("models/det");
config.SetCpuMathLibraryNumThreads(4);

// GPU deployment — different package, different init path
// var config = PaddleConfig.FromModelDir("models/det");
// config.EnableGpu(500, 0);  // memoryMB, deviceId

// OpenVINO deployment — third conditional branch
// config.EnableMkldnn();

// Application code now owns the hardware topology decision
```

```csharp
// IronOCR: No backend selection. No config objects. Zero hardware decisions.
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

var result = new IronTesseract().Read("document.jpg");
Console.WriteLine(result.Text);
// Runs on CPU, Linux, Docker, or ARM without a code change
```

## IronOCR vs PaddleSharp OCR: Feature Comparison

Here is a direct capability comparison across the dimensions that matter most during migration:

| Feature | PaddleSharp OCR | IronOCR |
|---|---|---|
| NuGet packages required | 3–4 minimum | 1 |
| Image input method | OpenCV `Cv2.ImRead()` | Direct path, stream, or byte array |
| PDF input (native) | No | Yes |
| Password-protected PDF | No | Yes |
| Multi-page TIFF | Via OpenCV | Native |
| Searchable PDF output | No | Yes (`result.SaveAsSearchablePdf()`) |
| hOCR export | No | Yes |
| Backend selection (CPU/GPU/OpenVINO) | Manual `PaddleConfig` | Automatic |
| Preprocessing pipeline | Manual OpenCV operations | Built-in (`Deskew`, `DeNoise`, `Contrast`, etc.) |
| Inference session lifecycle management | Manual (expensive construction) | Lightweight `IronTesseract` |
| Table recognition model | Separate download and code path | `input.LoadImage()` + structured result |
| Languages supported | ~10–20 | 125+ |
| Language installation | Model file download | NuGet package |
| Multi-language simultaneous | Limited | Yes (`OcrLanguage.French + OcrLanguage.German`) |
| Region-based OCR | No built-in | `CropRectangle` |
| Barcode reading during OCR | No | Yes (`ocr.Configuration.ReadBarCodes = true`) |
| Confidence scores | Per-region | Per-word, per-line, per-page |
| Structured output hierarchy | Flat regions list | Pages → Paragraphs → Lines → Words → Characters |
| Cross-platform deployment | Complex (platform runtime packages) | Single NuGet, all platforms |
| Docker deployment | Multiple layers, runtime packages | Single layer |
| Commercial support | GitHub issues (primarily Chinese) | Email support |
| License model | Apache 2.0 | Perpetual ($749 Lite, $1,499 Pro, $2,999 Enterprise) |

## Quick Start: PaddleSharp OCR to IronOCR Migration

### Step 1: Replace NuGet Package

Remove PaddleSharp and its OpenCV dependency:

```bash
dotnet remove package Sdcb.PaddleOCR
dotnet remove package Sdcb.PaddleInference
dotnet remove package OpenCvSharp4
dotnet remove package OpenCvSharp4.runtime.win
```

Install IronOCR from [NuGet](https://www.nuget.org/packages/IronOcr):

```bash
dotnet add package IronOcr
```

### Step 2: Update Namespaces

Replace PaddleSharp namespaces with the single IronOCR namespace:

```csharp
// Before (PaddleSharp)
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models;
using Sdcb.PaddleInference;
using OpenCvSharp;

// After (IronOCR)
using IronOcr;
```

### Step 3: Initialize License

Add license initialization once at application startup — in `Program.cs`, `Startup.cs`, or your composition root:

```csharp
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
```

## Code Migration Examples

### Inference Session Lifecycle Replacement

PaddleSharp's `PaddleOcrAll` is expensive to construct because it loads three model binaries synchronously at instantiation. Production applications must treat it as a long-lived object, which drives a specific dependency injection pattern. The disposal chain also requires attention because the underlying native resources must be released in the correct order.

**PaddleSharp OCR Approach:**

```csharp
// Simplified — see Sdcb.PaddleOCR documentation for full API
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models;
using OpenCvSharp;
using Microsoft.Extensions.DependencyInjection;

// Expensive: loads 3 model files from disk on construction (~300–800ms)
public class PaddleOcrEngine : IDisposable
{
    private readonly PaddleOcrAll _ocr;
    private bool _disposed;

    public PaddleOcrEngine()
    {
        var detModel = LocalFullModels.ChineseV3.DetectionModel;
        var clsModel = LocalFullModels.ChineseV3.ClassifierModel;
        var recModel = LocalFullModels.ChineseV3.RecognitionModel;

        // Must be singleton — cannot afford per-request construction
        _ocr = new PaddleOcrAll(detModel, clsModel, recModel);
    }

    public string Read(string imagePath)
    {
        using var mat = Cv2.ImRead(imagePath); // OpenCV required even for a file path
        var result = _ocr.Run(mat);
        return string.Join(" ", result.Regions.Select(r => r.Text));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _ocr?.Dispose();
            _disposed = true;
        }
    }
}

// Startup.cs — forced singleton because of construction cost
services.AddSingleton<PaddleOcrEngine>();
```

**IronOCR Approach:**

```csharp
using IronOcr;
using Microsoft.Extensions.DependencyInjection;

// IronTesseract has lightweight initialization — no model loading on construction
public class OcrEngine
{
    public string Read(string imagePath)
    {
        return new IronTesseract().Read(imagePath).Text;
    }
}

// Flexible registration — singleton, scoped, or transient all work
services.AddTransient<OcrEngine>();

// Or skip the wrapper entirely and inject IronTesseract directly
services.AddTransient<IronTesseract>();
```

The shift from forced-singleton to flexible lifetime is significant. PaddleSharp's construction cost locks your service lifetime decision; IronOCR lets you choose based on your application's threading and request isolation needs. The [IronTesseract setup guide](https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/) covers configuration options that apply at the instance level.

### OpenCV Preprocessing Pipeline Migration

PaddleSharp teams with low-quality scans typically build an OpenCV preprocessing pipeline before invoking the OCR engine. That pipeline requires knowledge of OpenCV's API surface, which is substantially larger than what any OCR preprocessing task actually needs. Common operations — deskew, denoise, contrast stretch — require multiple `Mat` operations and careful memory management with `using` blocks to prevent native memory leaks.

**PaddleSharp OCR Approach:**

```csharp
// Simplified — see OpenCvSharp documentation for full API
using OpenCvSharp;
using Sdcb.PaddleOCR;

public string ReadWithPreprocessing(string imagePath, PaddleOcrAll ocr)
{
    using var original = Cv2.ImRead(imagePath);

    // Step 1: Grayscale conversion
    using var gray = new Mat();
    Cv2.CvtColor(original, gray, ColorConversionCodes.BGR2GRAY);

    // Step 2: Denoise (Gaussian blur to reduce noise)
    using var denoised = new Mat();
    Cv2.GaussianBlur(gray, denoised, new Size(3, 3), 0);

    // Step 3: Adaptive threshold for binarization
    using var binary = new Mat();
    Cv2.AdaptiveThreshold(denoised, binary, 255,
        AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 11, 2);

    // Step 4: Deskew — requires custom rotation detection logic (not shown)
    // Several dozen lines of custom Mat operations

    var result = ocr.Run(binary);
    return string.Join(" ", result.Regions.Select(r => r.Text));
    // Each Mat must be disposed; missing a using block leaks native memory
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public string ReadWithPreprocessing(string imagePath)
{
    using var input = new OcrInput();
    input.LoadImage(imagePath);

    // Named operations replace OpenCV knowledge requirements
    input.Deskew();
    input.DeNoise();
    input.Contrast();
    input.Binarize();

    var result = new IronTesseract().Read(input);
    return result.Text;
    // OcrInput implements IDisposable; using block handles cleanup
}
```

No `Mat` allocations. No knowledge of adaptive threshold parameters. No custom deskew rotation math. The same preprocessing pipeline that required 30–50 lines of OpenCV code becomes four method calls. The [image quality correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/) documents every available filter with before-and-after examples. For documents with heavy background noise, `input.DeepCleanBackgroundNoise()` goes further than `DeNoise()` without any additional parameters.

For teams whose preprocessing requirements are non-standard, the [filter wizard](https://ironsoftware.com/csharp/ocr/how-to/filter-wizard/) provides an interactive tool for evaluating combinations of filters on your specific document types before committing to code.

### Backend Selection Elimination

PaddleSharp exposes the inference backend as an application-level concern. A deployment that needs to run on a CPU-only cloud VM uses different initialization code than one targeting a GPU workstation or an Intel OpenVINO-capable edge device. That conditional logic typically ends up in application startup code, environment variable checks, or feature flags — infrastructure work that has nothing to do with reading text from images.

**PaddleSharp OCR Approach:**

```csharp
// Simplified — see Sdcb.PaddleInference documentation for full API
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;

public PaddleOcrAll CreateOcrEngine(string backendMode)
{
    // Each backend requires a different NuGet runtime package installed
    switch (backendMode)
    {
        case "gpu":
            // Requires: Sdcb.PaddleInference.runtime.win64.cuda
            // Requires: CUDA toolkit + cuDNN installed on host
            var gpuConfig = PaddleConfig.FromModelDir("models/");
            gpuConfig.EnableGpu(500, deviceId: 0); // Simplified
            break;

        case "openvino":
            // Requires: Sdcb.PaddleInference.runtime.win64.mkl
            var oviConfig = PaddleConfig.FromModelDir("models/");
            oviConfig.EnableMkldnn(); // Simplified
            break;

        default:
            // CPU-only — still requires platform-specific runtime package
            var cpuConfig = PaddleConfig.FromModelDir("models/");
            cpuConfig.SetCpuMathLibraryNumThreads(Environment.ProcessorCount);
            break;
    }

    // Backend-specific config passed to model constructors — Simplified
    var detModel = LocalFullModels.ChineseV3.DetectionModel;
    var clsModel = LocalFullModels.ChineseV3.ClassifierModel;
    var recModel = LocalFullModels.ChineseV3.RecognitionModel;
    return new PaddleOcrAll(detModel, clsModel, recModel);
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

// No backend selection. No switch statement. No environment variable check.
// The same code runs on CPU-only VMs, GPU workstations, and ARM devices.
public IronTesseract CreateOcrEngine()
{
    return new IronTesseract();
}

// Parallel processing across CPU cores — no GPU configuration required
public IEnumerable<string> ReadBatch(IEnumerable<string> imagePaths)
{
    var results = new System.Collections.Concurrent.ConcurrentBag<string>();
    Parallel.ForEach(imagePaths, path =>
    {
        var result = new IronTesseract().Read(path);
        results.Add(result.Text);
    });
    return results;
}
```

The `Parallel.ForEach` pattern here is thread-safe out of the box. Each `IronTesseract` instance is independent with no shared native state. For teams whose PaddleSharp deployment spends time managing backend conditionals, that simplification is also a deployment reliability improvement — the same build artifact runs everywhere without hardware detection code. The [speed optimization guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-fast-configuration/) covers configuration options for throughput-sensitive scenarios.

### Table Recognition Migration

Table extraction in PaddleSharp requires a dedicated table recognition model — a fourth model file beyond the standard detection, classification, and recognition set. The table model uses a separate API call and returns its own result structure. Teams building invoice, form, or spreadsheet processing pipelines maintain two parallel initialization paths and two result-parsing strategies.

**PaddleSharp OCR Approach:**

```csharp
// Simplified — see Sdcb.PaddleOCR documentation for full API
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models;
using OpenCvSharp;

public class TableRecognitionService
{
    // Standard OCR engine — 3 models
    private readonly PaddleOcrAll _textOcr;

    // Table engine — 4th model, separate initialization
    // private readonly PaddleOcrTable _tableOcr; // Simplified

    public TableRecognitionService()
    {
        var detModel = LocalFullModels.ChineseV3.DetectionModel;
        var clsModel = LocalFullModels.ChineseV3.ClassifierModel;
        var recModel = LocalFullModels.ChineseV3.RecognitionModel;
        _textOcr = new PaddleOcrAll(detModel, clsModel, recModel);

        // Table model: separate download, separate version tracking
        // var tableModel = LocalFullModels.TableEnV2.Model; // Simplified
        // _tableOcr = new PaddleOcrTable(tableModel); // Simplified
    }

    public void ProcessDocument(string imagePath)
    {
        using var image = Cv2.ImRead(imagePath);

        // Text extraction path
        var textResult = _textOcr.Run(image);
        var text = string.Join(" ", textResult.Regions.Select(r => r.Text));

        // Table extraction path — different API, different result structure
        // var tableResult = _tableOcr.Run(image); // Simplified
        // foreach (var cell in tableResult.Cells) { ... } // Simplified
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public class TableRecognitionService
{
    // One engine handles both text and table regions
    public void ProcessDocument(string imagePath)
    {
        var ocr = new IronTesseract();
        var result = ocr.Read(imagePath);

        // Structured hierarchy: pages → paragraphs → lines → words
        foreach (var page in result.Pages)
        {
            foreach (var paragraph in page.Paragraphs)
            {
                Console.WriteLine($"Block at ({paragraph.X},{paragraph.Y}): {paragraph.Text}");
            }
        }

        Console.WriteLine($"Full document text: {result.Text}");
    }
}
```

For documents where the table structure itself must be extracted as rows and columns, IronOCR provides dedicated table extraction capability:

```csharp
using IronOcr;

var ocr = new IronTesseract();
using var input = new OcrInput();
input.LoadImage("invoice-with-table.jpg");

var result = ocr.Read(input);

// Access structured page layout for table region extraction
foreach (var page in result.Pages)
{
    foreach (var line in page.Lines)
    {
        // Lines within a table region preserve spatial ordering
        Console.WriteLine($"Row text: {line.Text} | Y position: {line.Y}");
        foreach (var word in line.Words)
        {
            Console.WriteLine($"  Cell: '{word.Text}' at X={word.X}");
        }
    }
}
```

One model download eliminated. One initialization path eliminated. The structured result hierarchy in IronOCR — with word-level X/Y coordinates — provides the positional data needed to reconstruct table rows and columns without a separate recognition model. The [table reading guide](https://ironsoftware.com/csharp/ocr/how-to/read-table-in-document/) and the [read results guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) cover the full structured output API.

### Searchable PDF Output from Scanned Documents

PaddleSharp produces text strings and nothing else. Building a document archive that makes scanned PDFs text-searchable requires integrating a separate PDF library, writing a text-overlay layer, and managing two libraries in concert. Teams that have accepted that constraint often find it is the migration trigger — the effort of the two-library integration exceeds the effort of switching OCR providers.

**PaddleSharp OCR Approach:**

```csharp
// Simplified — PaddleSharp has no PDF output. Requires a separate PDF library.
// Example of what teams typically build:

// using Sdcb.PaddleOCR;
// using SomePdfLibrary; // Third dependency to produce searchable PDF

public void ArchiveScannedDocument(string imagePath, string outputPdfPath)
{
    // Step 1: OCR via PaddleSharp — produces text only
    // var text = _ocr.Run(Cv2.ImRead(imagePath));

    // Step 2: Build a PDF with text overlay using a separate PDF library
    // Requires: text positions mapped to PDF coordinate space
    // Requires: image embedded as background
    // Requires: invisible text layer positioned over image
    // ~50–100 lines of PDF construction code
    throw new NotImplementedException("Requires a separate PDF library");
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public void ArchiveScannedDocument(string imagePath, string outputPdfPath)
{
    using var input = new OcrInput();
    input.LoadImage(imagePath);
    input.Deskew();   // Straighten scan before archiving
    input.DeNoise();  // Clean up scan artifacts

    var ocr = new IronTesseract();
    var result = ocr.Read(input);

    // One call: OCR + searchable PDF with text layer + image background
    result.SaveAsSearchablePdf(outputPdfPath);
}

// Multi-page document — same pattern
public void ArchiveMultiPageDocument(string[] imageFiles, string outputPdfPath)
{
    using var input = new OcrInput();
    foreach (var file in imageFiles)
        input.LoadImage(file);

    var result = new IronTesseract().Read(input);
    result.SaveAsSearchablePdf(outputPdfPath);
}
```

No PDF library. No coordinate mapping. No text layer positioning. The searchable PDF output format in IronOCR embeds an invisible text layer over the original image, producing a file that is both visually faithful to the scanned document and fully text-searchable. The [searchable PDF how-to](https://ironsoftware.com/csharp/ocr/how-to/searchable-pdf/) covers page selection, quality options, and metadata control.

## PaddleSharp OCR API to IronOCR Mapping Reference

| PaddleSharp OCR | IronOCR |
|---|---|
| `Sdcb.PaddleOCR` (namespace) | `IronOcr` (namespace) |
| `Sdcb.PaddleInference` (namespace) | Not needed — auto-configured |
| `PaddleOcrAll` | `IronTesseract` |
| `new PaddleOcrAll(det, cls, rec)` | `new IronTesseract()` |
| `LocalFullModels.ChineseV3.DetectionModel` | No equivalent — no model selection |
| `LocalFullModels.ChineseV3.ClassifierModel` | No equivalent — no model selection |
| `LocalFullModels.ChineseV3.RecognitionModel` | No equivalent — no model selection |
| `PaddleConfig.FromModelDir()` | No equivalent — no config object |
| `config.EnableGpu(memMB, deviceId)` | No equivalent — backend is automatic |
| `config.EnableMkldnn()` | No equivalent — backend is automatic |
| `config.SetCpuMathLibraryNumThreads(n)` | No equivalent — managed internally |
| `Cv2.ImRead(path)` (OpenCV load) | `input.LoadImage(path)` |
| `ocr.Run(mat)` | `ocr.Read(input)` or `ocr.Read("file.jpg")` |
| `result.Regions` | `result.Pages[0].Words` or `result.Pages[0].Lines` |
| `region.Text` | `word.Text`, `line.Text`, `paragraph.Text` |
| `region.Rect.Center.X/.Y` | `word.X`, `word.Y` |
| `region.Score` (confidence) | `word.Confidence`, `result.Confidence` |
| Model-level language swap | `ocr.Language = OcrLanguage.French` |
| Table model (separate download) | Built-in structured result hierarchy |
| `Cv2.CvtColor(..., GRAY)` | `input.Binarize()` or `input.Contrast()` |
| `Cv2.GaussianBlur(...)` | `input.DeNoise()` |
| No searchable PDF output | `result.SaveAsSearchablePdf("output.pdf")` |

## Common Migration Issues and Solutions

### Issue 1: OpenCV Dependency Fails to Unload

**PaddleSharp OCR:** `OpenCvSharp4.runtime.win` and similar platform-specific runtime packages install unmanaged native DLLs. These DLLs can prevent proper cleanup in some hosting scenarios — particularly IIS app pool recycling — and cause assembly load failures when the wrong platform runtime package is referenced at build time. Removing them requires both the NuGet package removal and clearing any cached native binaries in the output directory.

**Solution:** After removing the `OpenCvSharp4` and `OpenCvSharp4.runtime.*` packages, clean the build output directory before rebuilding:

```bash
dotnet remove package OpenCvSharp4
dotnet remove package OpenCvSharp4.runtime.win
dotnet clean
dotnet build
```

IronOCR bundles its native dependencies internally and handles the unmanaged lifecycle. No platform-specific runtime package selection is required. The [IronTesseract setup guide](https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/) documents the platform requirements that IronOCR handles automatically.

### Issue 2: Model Files Left on Disk After Migration

**PaddleSharp OCR:** Model files downloaded by PaddleSharp (detection, classification, recognition, and any table models) are typically stored in a `models/` directory relative to the application or in a configured path. These files are not removed when the NuGet package is uninstalled. In a Docker image, they add unnecessary layer size. In a deployment pipeline, stale model files at old paths can cause startup failures if any remnant initialization code references them.

**Solution:** Explicitly remove model directories as part of the migration. Audit the startup configuration for any path references:

```bash
# Locate model directory references in application code
grep -r "LocalFullModels\|ModelPath\|models/" --include="*.cs" .
grep -r "DetectionModel\|RecognitionModel\|ClassifierModel" --include="*.cs" .
```

Once model references are removed and IronOCR is initialized, delete the model directory from the repository and Docker build context.

### Issue 3: Singleton Lifetime Assumption Breaks After Migration

**PaddleSharp OCR:** `PaddleOcrAll` was registered as a singleton because its construction cost made per-request instantiation impractical. Migration code that lifts IronOCR into the same singleton registration may introduce unnecessary state sharing across requests. While `IronTesseract` is thread-safe when used concurrently, it is not necessary to share a single instance — each instance is independent.

**Solution:** Evaluate whether the singleton registration serves a purpose beyond performance. For most ASP.NET Core applications, transient registration is the cleaner choice with IronOCR:

```csharp
// PaddleSharp — forced singleton due to construction cost
services.AddSingleton<PaddleOcrAll>(sp =>
{
    var det = LocalFullModels.ChineseV3.DetectionModel;  // Simplified
    var cls = LocalFullModels.ChineseV3.ClassifierModel; // Simplified
    var rec = LocalFullModels.ChineseV3.RecognitionModel; // Simplified
    return new PaddleOcrAll(det, cls, rec);
});

// IronOCR — transient works; no expensive construction
services.AddTransient<IronTesseract>();
```

For high-throughput batch scenarios where you want explicit instance reuse, a singleton or pooled pattern still works — but it is a performance choice, not a correctness requirement.

### Issue 4: Result Region Ordering No Longer Required

**PaddleSharp OCR:** `result.Regions` returns detected text regions in detection order, which does not necessarily match reading order (left-to-right, top-to-bottom). Teams typically apply a sort by `.Rect.Center.Y` then `.Rect.Center.X` before joining region text — a pattern that appears in almost every PaddleSharp text extraction implementation. Migrating this pattern literally to IronOCR produces redundant code.

**Solution:** IronOCR returns results in reading order by default. Remove the sort:

```csharp
// PaddleSharp — manual reading-order sort required
var text = string.Join("\n", result.Regions
    .OrderBy(r => r.Rect.Center.Y)
    .ThenBy(r => r.Rect.Center.X)
    .Select(r => r.Text));

// IronOCR — result.Text is already in reading order; no sort needed
var text = result.Text;

// For word-level access with position, use the structured hierarchy directly
foreach (var word in result.Pages[0].Words)
{
    Console.WriteLine($"{word.Text} at ({word.X},{word.Y})");
}
```

### Issue 5: Backend-Conditional Packages Break Restore

**PaddleSharp OCR:** Some PaddleSharp setups conditionally reference different `Sdcb.PaddleInference.runtime.*` packages based on the target environment (CUDA for GPU, MKL for OpenVINO, CPU-only). This sometimes appears as `.csproj` conditions or as separate project files per deployment target. The resulting build matrix breaks CI pipelines when the wrong package set is restored.

**Solution:** After removing PaddleSharp packages, audit the `.csproj` file for conditional `PackageReference` blocks referencing any `Sdcb.*` or `OpenCvSharp*` packages and remove them entirely:

```bash
grep -n "Sdcb\|OpenCvSharp\|PaddleInference" *.csproj
```

IronOCR uses a single `IronOcr` package reference with no platform conditionals. The same package restores correctly on Windows, Linux, and macOS.

### Issue 6: Table Result Structure Has No Direct Equivalent

**PaddleSharp OCR:** `PaddleOcrTable` returns a cell-based structure with row and column indices per recognized cell. Code that consumes this structure typically builds a two-dimensional array indexed by `(row, column)`. IronOCR does not provide an identical cell-index structure — it provides word and line coordinates that require spatial grouping to reconstruct a cell grid.

**Solution:** Reconstruct table structure from IronOCR word coordinates using Y-position grouping for rows and X-position sorting for columns. For common table formats, the [table reading how-to](https://ironsoftware.com/csharp/ocr/how-to/read-table-in-document/) provides a spatial grouping approach. For structured invoices with known field positions, [region-based OCR](https://ironsoftware.com/csharp/ocr/how-to/ocr-region-of-an-image/) with `CropRectangle` is a cleaner pattern than full-page table extraction:

```csharp
using IronOcr;

// Target specific table cells by region instead of full-page table detection
var totalAmountRegion = new CropRectangle(400, 600, 200, 30); // x, y, width, height
using var input = new OcrInput();
input.LoadImage("invoice.jpg", totalAmountRegion);

var result = new IronTesseract().Read(input);
Console.WriteLine($"Total: {result.Text}");
```

## PaddleSharp OCR Migration Checklist

### Pre-Migration Tasks

Audit all PaddleSharp references in the codebase before removing packages:

```bash
# Find all PaddleSharp and PaddleInference usages
grep -rn "Sdcb\.PaddleOCR\|Sdcb\.PaddleInference" --include="*.cs" .

# Find OpenCV usages that will need replacement
grep -rn "OpenCvSharp\|Cv2\.\|using var.*Mat\b" --include="*.cs" .

# Find model path references and configuration
grep -rn "LocalFullModels\|ModelDir\|DetectionModel\|RecognitionModel\|ClassifierModel" --include="*.cs" .

# Find backend selection logic
grep -rn "EnableGpu\|EnableMkldnn\|PaddleConfig\|SetCpuMath" --include="*.cs" .

# Find table recognition usages
grep -rn "PaddleOcrTable\|TableModel\|table.*ocr\|ocr.*table" --include="*.cs" .

# Find result region access patterns that need updating
grep -rn "\.Regions\b\|Rect\.Center\|region\.Text" --include="*.cs" .
```

Inventory the model files on disk and note their paths. Inventory all deployment targets and whether any have GPU or OpenVINO-specific NuGet conditionals in `.csproj`. Note any services registered as singleton due to PaddleSharp construction cost.

### Code Update Tasks

1. Remove all `Sdcb.PaddleOCR`, `Sdcb.PaddleInference`, `OpenCvSharp4`, and `OpenCvSharp4.runtime.*` NuGet packages from every project file.
2. Install `IronOcr` NuGet package.
3. Install language NuGet packages for required languages (e.g., `IronOcr.Languages.ChineseSimplified`).
4. Add `IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";` to application startup.
5. Replace all `using Sdcb.PaddleOCR`, `using Sdcb.PaddleInference`, and `using OpenCvSharp` statements with `using IronOcr`.
6. Replace `PaddleOcrAll` instantiation and model loading with `new IronTesseract()`.
7. Delete all `PaddleConfig` backend selection blocks (CPU, GPU, OpenVINO conditionals).
8. Replace `Cv2.ImRead(path)` calls with `input.LoadImage(path)` using `OcrInput`.
9. Replace OpenCV preprocessing operations (`CvtColor`, `GaussianBlur`, `Threshold`, etc.) with `OcrInput` filter methods (`Deskew()`, `DeNoise()`, `Contrast()`, `Binarize()`).
10. Replace `ocr.Run(mat)` calls with `ocr.Read(input)`.
11. Replace `result.Regions` enumeration with `result.Pages`, `result.Pages[n].Lines`, or `result.Pages[n].Words`.
12. Remove `.OrderBy(r => r.Rect.Center.Y).ThenBy(r => r.Rect.Center.X)` sort chains — reading order is automatic.
13. Replace `PaddleOcrTable` initialization and result parsing with `OcrInput` region-based targeting or coordinate-based word grouping.
14. Add `result.SaveAsSearchablePdf(path)` anywhere a searchable PDF archive is required.
15. Reassess service lifetime registrations: singleton registrations driven by PaddleSharp construction cost can typically become transient or scoped.
16. Delete model files from disk and remove model directories from Docker build contexts.
17. Remove any `.csproj` conditional `PackageReference` blocks for platform-specific Paddle or OpenCV runtime packages.

### Post-Migration Testing

- Verify text extraction output matches or exceeds PaddleSharp output on a representative sample of 20–30 documents from each document type in the pipeline.
- Confirm no `OpenCvSharp`-related assembly load exceptions in application startup logs.
- Test deployment on each target platform (Windows, Linux, Docker) using the same build artifact — no platform-specific package selection should be needed.
- Verify that documents previously requiring manual result sorting produce correctly ordered text via `result.Text`.
- Confirm searchable PDF output files are text-searchable in Adobe Acrobat Reader or a PDF viewer of your choice.
- Run the application under load to confirm that `IronTesseract` instances created per-request do not produce memory pressure comparable to per-request `PaddleOcrAll` construction.
- Verify that language packs installed as NuGet packages restore correctly in CI without additional file deployment steps.
- Test any table extraction scenarios against expected row/column structure using the region-based or coordinate-grouping approach.
- Confirm that application startup time decreases after eliminating `PaddleOcrAll` singleton construction from the startup path.

## Key Benefits of Migrating to IronOCR

**One Package Replaces a Four-Package Stack.** After migration, the OCR dependency footprint is a single `IronOcr` NuGet reference. The four-package stack — `Sdcb.PaddleOCR`, `Sdcb.PaddleInference`, `OpenCvSharp4`, and a platform-specific runtime — becomes one entry in the project file. Dependency audits, license scans, and vulnerability monitoring now cover one surface instead of four.

**Deployment Artifacts Are Uniform Across Environments.** The backend selection conditionals — CPU versus GPU versus OpenVINO — are gone. The same build artifact deploys to a developer laptop, a CI runner, a Linux container, and a cloud VM without any environment-specific package selection or initialization branching. Docker images shrink because there are no model files to `COPY` and no platform runtime packages to install.

**Document Archive Pipelines No Longer Require a Second Library.** `result.SaveAsSearchablePdf()` eliminates the PDF library dependency that most PaddleSharp teams had added to produce searchable archives. The OCR pass and the searchable PDF write are a single API call. For teams processing thousands of scanned documents per day, that simplification removes an entire class of inter-library version conflicts. The [searchable PDFs blog post](https://ironsoftware.com/csharp/ocr/blog/using-ironocr/searchable-pdfs-with-ironocr/) covers production-scale considerations.

**Service Lifetime Decisions Reflect Application Requirements, Not Library Constraints.** `IronTesseract` has lightweight construction. The forced-singleton pattern driven by PaddleSharp's expensive model loading is no longer necessary. Services can be scoped per-request in ASP.NET Core, creating cleaner isolation between concurrent users and removing shared-state threading concerns. For more on deployment options, see the [ASP.NET OCR use case page](https://ironsoftware.com/csharp/ocr/use-case/asp-net-ocr/).

**Language Expansion Is a Package Install, Not a Research Project.** The [125+ language catalog](https://ironsoftware.com/csharp/ocr/languages/) covers European, Asian, Middle Eastern, and specialized scripts as NuGet packages. Adding French, German, Arabic, or Japanese to a pipeline that started as Chinese-only is `dotnet add package IronOcr.Languages.French` and one configuration line. No model file sourcing, no upstream availability research, no manual file deployment.

**Preprocessing Is Part of the OCR API.** The OpenCV knowledge that PaddleSharp preprocessing required — understanding filter kernels, managing `Mat` disposal, selecting adaptive threshold parameters — is no longer a prerequisite for OCR work. `OcrInput` provides named operations with sensible defaults. Teams that were not OpenCV specialists but were maintaining OpenCV preprocessing code can delete that code without replacing it. The [preprocessing features page](https://ironsoftware.com/csharp/ocr/features/preprocessing/) lists every available filter with documentation on when to apply each.
