# Migrating from PaddleOCR to IronOCR

This guide walks .NET developers through a complete migration from the `Sdcb.PaddleOCR` package family to [IronOCR](https://ironsoftware.com/csharp/ocr/). It covers the full replacement path: removing the multi-package PaddlePaddle stack, eliminating model file management and GPU configuration, and replacing the OpenCV-dependent inference pipeline with a single NuGet install. Each section is self-contained — no prior reading of the comparison article is required.

## Why Migrate from PaddleOCR

The `Sdcb.PaddleOCR` wrapper is a community-maintained bridge between PaddlePaddle's Python deep learning ecosystem and .NET. It does the job, but it carries the entire weight of that bridge with it — model files, native inference binaries, OpenCV for image loading, and optional CUDA infrastructure. For most .NET OCR workloads, that is infrastructure the project never needed.

**Three Model Directories Before a Single Character is Read.** PaddleOCR's inference pipeline chains three neural networks: a detection model, a direction classification model, and a recognition model. Each network is a separate directory of `.pdmodel` and `.pdiparams` files that must exist on disk before `new PaddleOcrAll(models)` compiles to a working engine. Whether those files arrive via an async download call — which connects to Baidu's `bj.bcebos.com` storage in China — or via a manually maintained `models/` directory tree, the developer is permanently responsible for model versioning. When `Sdcb.PaddleOCR` updates, the pre-downloaded models from the previous version may require re-download. IronOCR has no model files, no model directories, and no version synchronization problem. The engine is bundled inside the NuGet package.

**OpenCV Is Not Optional.** There is no path from a file path to PaddleOCR inference that bypasses OpenCvSharp. Every image, regardless of format, must pass through `Cv2.ImRead(path)` to become a `Mat` object before `ocr.Run(mat)` can accept it. That means two extra NuGet packages (`OpenCvSharp4` and `OpenCvSharp4.runtime.win`), platform-specific native DLLs in the deployment output, and an `apt-get install libopencv-dev` line in the Dockerfile. IronOCR accepts file paths, streams, byte arrays, and `System.Drawing.Bitmap` directly. The `Mat` intermediary does not exist.

**GPU Configuration Is a Multi-Day Project.** PaddleOCR's advertised GPU performance figures — 50-100ms per image versus 300-500ms on CPU — are real. Getting there requires NVIDIA drivers at a specific version, CUDA Toolkit 11.8 (not 12.x), cuDNN 8.6+ placed in the correct PATH locations, and a separate GPU runtime NuGet package. In Docker, the base image must be `nvidia/cuda:11.8.0-cudnn8-runtime-ubuntu22.04` and the host must have `nvidia-container-toolkit` installed. Teams without existing GPU infrastructure spend 2-8 hours on CUDA configuration per environment. IronOCR is engineered for CPU inference, delivers 150-300ms per image on standard hardware, and requires no GPU setup whatsoever.

**Deployment Artifacts Are 4-6x Larger.** The PaddleOCR deployment output includes `paddle_inference.dll` (~200MB), `paddle2onnx.dll` (~5MB), `opencv_world*.dll` files (~50MB combined), and the model directories (~21MB). A Docker image lands at approximately 1.5GB. An IronOCR deployment is approximately 80MB total; the Docker image lands at approximately 400MB. The difference compounds in CI/CD: every pipeline run that restores NuGet packages must either download models from Baidu or pull them from a separately maintained cache layer.

**No Searchable PDF Output.** PaddleOCR returns text regions from images and has no mechanism to embed recognized text back into a PDF as a searchable layer. Creating a searchable PDF from PaddleOCR output requires a third-party PDF library, page-by-page text layer injection, and coordinate remapping. IronOCR produces a fully searchable PDF with one line: `result.SaveAsSearchablePdf("output.pdf")`.

### The Fundamental Problem

PaddleOCR requires three model directories configured before inference can begin:

```csharp
// PaddleOCR: three model directories, all must exist and match the wrapper version
FullOcrModel models = new FullOcrModel(
    LocalDetectionModel.FromDirectory("models/ch_PP-OCRv4_det_infer"),       // ~5MB
    LocalClassificationModel.FromDirectory("models/ch_ppocr_mobile_v2.0_cls_infer"), // ~2MB
    LocalRecognitionModel.FromDirectory("models/ch_PP-OCRv4_rec_infer")      // ~15MB
);
using PaddleOcrAll ocr = new PaddleOcrAll(models);
using Mat mat = Cv2.ImRead("document.png");  // OpenCvSharp required for every image
PaddleOcrResult result = ocr.Run(mat);
```

IronOCR has no model files, no model directories, and no OpenCV dependency:

```csharp
// IronOCR: one package, zero model management
var ocr = new IronTesseract();
using var input = new OcrInput();
input.LoadImage("document.png");
var result = ocr.Read(input);
Console.WriteLine(result.Text);
```

## IronOCR vs PaddleOCR (.NET): Feature Comparison

The table below covers the dimensions that matter most during migration planning.

| Feature | PaddleOCR (Sdcb) | IronOCR |
|---|---|---|
| NuGet packages required | 4-5 | 1 |
| Model files required | Yes (3 directories, ~21MB) | No (bundled in package) |
| Model download source | Baidu servers (bj.bcebos.com) | NuGet restore (Iron Software) |
| OpenCV dependency | Required (OpenCvSharp4) | None |
| Image input | Via `Mat mat = Cv2.ImRead()` | Direct file path, stream, byte array |
| Native PDF input | No | Yes (`input.LoadPdf()`) |
| Searchable PDF output | No | Yes (`result.SaveAsSearchablePdf()`) |
| Multi-frame TIFF input | Manual per-frame loop | `input.LoadImageFrames()` |
| GPU support | Yes (CUDA 11.8 + cuDNN required) | CPU-optimized (no GPU needed) |
| Built-in preprocessing | No (neural network handles skew/noise) | Yes (Deskew, DeNoise, Contrast, Binarize, Sharpen) |
| Languages supported | 14 | 125+ |
| Language install method | `DownloadAsync()` per language model | `dotnet add package IronOcr.Languages.*` |
| Multi-language simultaneous | No (separate model per language) | Yes (`OcrLanguage.English + OcrLanguage.French`) |
| Structured output | `result.Regions` (spatial, unsorted) | Pages, Paragraphs, Lines, Words, Characters |
| Confidence scoring | Per-region float (0-1) | Per-word percentage (0-100) |
| Barcode reading | No | Yes (`ocr.Configuration.ReadBarCodes = true`) |
| hOCR export | No | Yes |
| Deployment size | 300-500MB | ~80MB |
| Docker image size | ~1.5GB (with CUDA base) | ~400MB |
| Cold start time | 3-5 seconds (model load) | Under 1 second |
| Cross-platform | Windows, Linux (partial) | Windows, Linux, macOS, Docker, Azure, AWS |
| .NET compatibility | .NET 6+ (community wrapper) | .NET Framework 4.6.2+, .NET 5/6/7/8/9 |
| Commercial support | Community / GitHub issues | Yes (Iron Software, with SLA) |
| License | Apache 2.0 (free) | Perpetual ($749 Lite / $1,499 Pro / $2,999 Enterprise) |

## Quick Start: PaddleOCR (.NET) to IronOCR Migration

### Step 1: Replace NuGet Packages

Remove all five PaddleOCR-related packages:

```bash
dotnet remove package Sdcb.PaddleOCR
dotnet remove package Sdcb.PaddleOCR.Models.Online
dotnet remove package Sdcb.PaddleInference.runtime.win64.mkl
dotnet remove package OpenCvSharp4
dotnet remove package OpenCvSharp4.runtime.win
```

If the GPU runtime was installed, remove it as well:

```bash
dotnet remove package Sdcb.PaddleInference.runtime.win64.cuda118
```

Install IronOCR from the [NuGet package page](https://www.nuget.org/packages/IronOcr):

```bash
dotnet add package IronOcr
```

### Step 2: Update Namespaces

Replace all PaddleOCR and OpenCvSharp namespace imports:

```csharp
// Before (PaddleOCR)
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models;
using Sdcb.PaddleOCR.Models.Online;
using Sdcb.PaddleInference;
using OpenCvSharp;

// After (IronOCR)
using IronOcr;
```

### Step 3: Initialize License

Add license initialization once at application startup, before any `IronTesseract` instance is created:

```csharp
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
```

A free trial key is available from the [IronOCR licensing page](https://ironsoftware.com/csharp/ocr/licensing/). The trial produces watermarked output and allows full feature testing before purchase.

## Code Migration Examples

### Local Model Path Configuration Elimination

Projects that pre-download PaddleOCR model files to avoid Baidu server connections at runtime must configure three separate directory paths. This configuration must be updated whenever the wrapper version changes.

**PaddleOCR Approach:**

```csharp
// Local model configuration — developer owns the directory structure
// Each wrapper update may require re-downloading model files
string modelsRoot = Path.Combine(AppContext.BaseDirectory, "models");

FullOcrModel models = new FullOcrModel(
    LocalDetectionModel.FromDirectory(
        Path.Combine(modelsRoot, "ch_PP-OCRv4_det_infer")),
    LocalClassificationModel.FromDirectory(
        Path.Combine(modelsRoot, "ch_ppocr_mobile_v2.0_cls_infer")),
    LocalRecognitionModel.FromDirectory(
        Path.Combine(modelsRoot, "ch_PP-OCRv4_rec_infer"))
);

// Fails at runtime if any of the three directories is missing or stale
using PaddleOcrAll ocr = new PaddleOcrAll(models)
{
    AllowRotateDetection = true,
    Enable180Classification = true
};

using Mat mat = Cv2.ImRead("document.png");
PaddleOcrResult result = ocr.Run(mat);
Console.WriteLine(result.Text);
```

**IronOCR Approach:**

```csharp
// No model directories, no path configuration, no version matching
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

var ocr = new IronTesseract();

using var input = new OcrInput();
input.LoadImage("document.png");

var result = ocr.Read(input);
Console.WriteLine(result.Text);
```

The `models/` directory tree, the three `FromDirectory()` calls, and the version-synchronization concern all disappear. The IronOCR engine is bundled inside the NuGet package at restore time and requires no runtime path resolution. See the [IronTesseract setup guide](https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/) for initialization options including license key placement in `appsettings.json`.

### Two-Stage Detection and Recognition Pipeline Consolidation

PaddleOCR's rotation and orientation pipeline is configured through properties on `PaddleOcrAll`. Replicating this behavior in IronOCR uses the `OcrInput` preprocessing methods, which handle the same document problems with a simpler call surface.

**PaddleOCR Approach:**

```csharp
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.Online;
using OpenCvSharp;

// Separate async initialization step — blocks startup for 3-5 seconds on cold run
FullOcrModel models = await OnlineFullModels.EnglishV4.DownloadAsync();

using PaddleOcrAll ocr = new PaddleOcrAll(models)
{
    AllowRotateDetection = true,        // Enables 0/90/180/270 degree rotation detection
    Enable180Classification = true      // Additional pass for upside-down text
};

// OpenCV Mat required — no direct file path support
using Mat mat = Cv2.ImRead("rotated-scan.png");

if (mat.Empty())
{
    throw new FileNotFoundException("Image could not be loaded by OpenCvSharp");
}

// Three neural network passes: detection → classification → recognition
PaddleOcrResult result = ocr.Run(mat);

// Regions arrive in spatial order, not reading order
// Manual sort required for top-to-bottom, left-to-right output
var orderedRegions = result.Regions
    .OrderBy(r => r.Rect.Center.Y)
    .ThenBy(r => r.Rect.Center.X);

foreach (var region in orderedRegions)
{
    Console.WriteLine($"{region.Text} (confidence: {region.Score:P1})");
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

var ocr = new IronTesseract();

using var input = new OcrInput();
input.LoadImage("rotated-scan.png");
input.Deskew();    // Corrects rotation and skew automatically

var result = ocr.Read(input);

// Output is already in reading order — no sort needed
foreach (var page in result.Pages)
{
    foreach (var line in page.Lines)
    {
        Console.WriteLine($"{line.Text} (confidence: {line.Confidence}%)");
    }
}
```

IronOCR's `Deskew()` method handles rotation detection as part of the preprocessing pipeline. The result's `Lines` collection is delivered in reading order by the Tesseract layout engine, eliminating the manual sort pattern. The [image orientation correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-orientation-correction/) documents the full range of rotation and deskew options.

### GPU and CPU Device Selection Removal

PaddleOCR applications that run GPU inference carry the largest migration surface: the GPU runtime NuGet package, the CUDA/cuDNN environment prerequisites, and the `PaddleDevice.Gpu()` configuration call. All of this is removed during migration.

**PaddleOCR Approach:**

```csharp
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.Online;
using Sdcb.PaddleInference;    // GPU configuration namespace
using OpenCvSharp;

// Prerequisites must exist on every deployment environment:
// - NVIDIA Driver 452.39+ (Windows) / 450.80.02+ (Linux)
// - CUDA Toolkit 11.8 (not 12.x — version must match exactly)
// - cuDNN 8.6.0+ placed in CUDA bin directory
// - dotnet add package Sdcb.PaddleInference.runtime.win64.cuda118

FullOcrModel models = await OnlineFullModels.ChineseV4.DownloadAsync();

// GPU device 0, 1000MB initial memory pool
// Throws native load exception if CUDA_PATH not set or cuDNN DLL missing
using PaddleOcrAll ocr = new PaddleOcrAll(models, PaddleDevice.Gpu(deviceId: 0))
{
    AllowRotateDetection = true,
    Enable180Classification = true
};

using Mat mat = Cv2.ImRead("scanned-batch.png");
PaddleOcrResult result = ocr.Run(mat);

Console.WriteLine($"Text regions: {result.Regions.Length}");
Console.WriteLine(result.Text);
```

**IronOCR Approach:**

*The IronOCR approach is identical to the example above — `IronTesseract` handles this scenario with the same API call. No GPU packages, no CUDA prerequisites, and no device selection are required. Replace `new PaddleOcrAll(models, PaddleDevice.Gpu(deviceId: 0))` with `new IronTesseract()` and remove all GPU-related configuration.*

IronOCR delivers 150-300ms per image on CPU — faster than PaddleOCR on CPU (300-500ms) and sufficient for the majority of web API and document pipeline workloads without any GPU infrastructure. For high-throughput scenarios, the [speed optimization guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-fast-configuration/) covers configuration options including thread management and page segmentation mode tuning.

### Structured Document Data Extraction

PaddleOCR returns a flat array of `PaddleOcrResultRegion` objects ordered spatially, not by reading flow. Extracting paragraph-level or line-level structure requires manual grouping logic based on bounding box proximity. IronOCR provides a hierarchical result tree with reading-order guaranteed.

**PaddleOCR Approach:**

```csharp
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.Online;
using OpenCvSharp;
using System.Collections.Generic;

FullOcrModel models = await OnlineFullModels.EnglishV4.DownloadAsync();
using PaddleOcrAll ocr = new PaddleOcrAll(models);
using Mat mat = Cv2.ImRead("invoice.png");

PaddleOcrResult result = ocr.Run(mat);

// No paragraph or line grouping — must implement manually
// Group regions into lines by proximity on the Y axis
var lineGroups = new Dictionary<int, List<PaddleOcrResultRegion>>();

foreach (var region in result.Regions)
{
    // Round Y center to nearest 15 pixels to approximate line grouping
    int lineKey = (int)(region.Rect.Center.Y / 15) * 15;

    if (!lineGroups.ContainsKey(lineKey))
        lineGroups[lineKey] = new List<PaddleOcrResultRegion>();

    lineGroups[lineKey].Add(region);
}

// Sort lines top to bottom, then regions left to right within each line
foreach (var line in lineGroups.OrderBy(kv => kv.Key))
{
    var lineText = string.Join(" ", line.Value
        .OrderBy(r => r.Rect.Center.X)
        .Select(r => r.Text));

    Console.WriteLine(lineText);
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

var ocr = new IronTesseract();

using var input = new OcrInput();
input.LoadImage("invoice.png");

var result = ocr.Read(input);

// Hierarchical structure: Pages → Paragraphs → Lines → Words → Characters
// All delivered in reading order by the layout engine
foreach (var page in result.Pages)
{
    Console.WriteLine($"Page {page.PageNumber} — {page.Words.Count} words");

    foreach (var paragraph in page.Paragraphs)
    {
        Console.WriteLine($"  Paragraph at ({paragraph.X}, {paragraph.Y}):");
        Console.WriteLine($"  {paragraph.Text}");
    }
}
```

The manual line-grouping approximation — rounding Y coordinates to a pixel bucket size — is replaced by the Tesseract layout engine's built-in paragraph segmentation. Bounding box coordinates are available at every level of the hierarchy through `paragraph.X`, `paragraph.Y`, `paragraph.Width`, and `paragraph.Height`. See the [structured results guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) and the [reading text from images tutorial](https://ironsoftware.com/csharp/ocr/tutorials/how-to-read-text-from-an-image-in-csharp-net/) for full result tree coverage.

### Searchable PDF Generation

PaddleOCR produces no PDF output. Generating a searchable PDF from PaddleOCR results requires a separate PDF library, manual coordinate mapping from `region.Rect` to PDF page units, and an invisible text layer injection. IronOCR generates a searchable PDF directly from the OCR result.

**PaddleOCR Approach:**

```csharp
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.Online;
using OpenCvSharp;
// Requires additional package: PdfSharp, iTextSharp, or similar
// Manual coordinate remapping from OpenCV pixel space to PDF point space

FullOcrModel models = await OnlineFullModels.EnglishV4.DownloadAsync();
using PaddleOcrAll ocr = new PaddleOcrAll(models);
using Mat mat = Cv2.ImRead("scanned-page.png");

PaddleOcrResult paddleResult = ocr.Run(mat);

// No built-in searchable PDF output — must build with external library
// region.Rect coordinates are in pixel space, PDF uses points (1 point = 1/72 inch)
// DPI conversion required for coordinate mapping
float dpiScale = 72.0f / 96.0f;  // Assuming 96 DPI source image

// ... hundreds of lines of PDF construction code using external library ...
// This is permanent maintenance, not a one-time cost
Console.WriteLine("Searchable PDF output requires external PDF library and coordinate mapping.");
```

**IronOCR Approach:**

```csharp
using IronOcr;

IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

var ocr = new IronTesseract();

using var input = new OcrInput();
input.LoadImage("scanned-page.png");
input.Deskew();
input.DeNoise();

var result = ocr.Read(input);

// Searchable PDF in one line — no external PDF library, no coordinate mapping
result.SaveAsSearchablePdf("searchable-output.pdf");

Console.WriteLine($"Searchable PDF created. Confidence: {result.Confidence}%");
```

The coordinate mapping problem — converting OpenCV pixel coordinates to PDF point space at the correct DPI — does not exist in IronOCR. The [searchable PDF guide](https://ironsoftware.com/csharp/ocr/how-to/searchable-pdf/) covers multi-page output, password-protected PDFs, and output quality settings. For teams digitizing scanned archives or building fax-to-searchable-PDF pipelines, this single method call replaces what would otherwise be a substantial integration project.

### Multi-Frame TIFF Batch Processing

Multi-page TIFF files appear frequently in document scanning workflows. PaddleOCR has no direct TIFF multi-frame support — each frame must be extracted individually using an external imaging library and loaded as a separate `Mat`. IronOCR handles multi-frame TIFFs natively.

**PaddleOCR Approach:**

```csharp
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.Online;
using OpenCvSharp;
using System.Drawing;  // For multi-frame TIFF extraction
using System.Drawing.Imaging;
using System.Text;

FullOcrModel models = await OnlineFullModels.EnglishV4.DownloadAsync();
using PaddleOcrAll ocr = new PaddleOcrAll(models);

var fullText = new StringBuilder();

// Must use System.Drawing to extract individual TIFF frames
// OpenCvSharp cannot enumerate TIFF frames directly
using var tiff = Image.FromFile("multipage-scan.tiff");
FrameDimension dimension = new FrameDimension(tiff.FrameDimensionsList[0]);
int frameCount = tiff.GetFrameCount(dimension);

for (int i = 0; i < frameCount; i++)
{
    tiff.SelectActiveFrame(dimension, i);

    // Save frame to temp file — OpenCvSharp needs a file path
    string tempPath = Path.GetTempFileName() + ".png";
    tiff.Save(tempPath, ImageFormat.Png);

    try
    {
        using Mat mat = Cv2.ImRead(tempPath);
        PaddleOcrResult result = ocr.Run(mat);
        fullText.AppendLine($"=== Frame {i + 1} ===");
        fullText.AppendLine(result.Text);
    }
    finally
    {
        File.Delete(tempPath);  // Must clean up temp files
    }
}

Console.WriteLine(fullText.ToString());
```

**IronOCR Approach:**

```csharp
using IronOcr;

IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

var ocr = new IronTesseract();

using var input = new OcrInput();
input.LoadImageFrames("multipage-scan.tiff");  // All frames in one call

var result = ocr.Read(input);

foreach (var page in result.Pages)
{
    Console.WriteLine($"=== Frame {page.PageNumber} ===");
    Console.WriteLine(page.Text);
}

// Optionally save the entire multi-frame result as searchable PDF
result.SaveAsSearchablePdf("multipage-searchable.pdf");
```

The frame extraction loop, `System.Drawing` dependency, temp file creation, and cleanup logic are all removed. IronOCR loads all frames in a single `LoadImageFrames()` call and exposes each frame as a `Page` in the result. The [TIFF and GIF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-tiff-gif/) covers multi-frame loading options, selective frame ranges, and memory considerations for large TIFF archives.

## PaddleOCR (.NET) API to IronOCR Mapping Reference

| PaddleOCR (Sdcb) | IronOCR | Notes |
|---|---|---|
| `Sdcb.PaddleOCR` | `IronOcr` | Namespace |
| `Sdcb.PaddleOCR.Models.Online` | N/A | No model acquisition namespace needed |
| `Sdcb.PaddleInference` | N/A | No inference backend namespace needed |
| `FullOcrModel` | N/A | No equivalent — models are bundled |
| `OnlineFullModels.ChineseV4.DownloadAsync()` | `dotnet add package IronOcr.Languages.ChineseSimplified` | Model acquisition replaced by NuGet |
| `LocalDetectionModel.FromDirectory(path)` | N/A | No model path management |
| `LocalClassificationModel.FromDirectory(path)` | N/A | No model path management |
| `LocalRecognitionModel.FromDirectory(path)` | N/A | No model path management |
| `new PaddleOcrAll(models)` | `new IronTesseract()` | Engine instantiation |
| `new PaddleOcrAll(models, PaddleDevice.Gpu(0))` | N/A | GPU device selection removed entirely |
| `PaddleDevice.Cpu()` | N/A | CPU is the only mode; no selection needed |
| `ocr.AllowRotateDetection = true` | `input.Deskew()` | Rotation correction |
| `ocr.Enable180Classification = true` | Automatic | Upside-down detection is built in |
| `Cv2.ImRead(path)` | `input.LoadImage(path)` | Image loading — no OpenCV required |
| `ocr.Run(mat)` | `ocr.Read(input)` | Execute OCR |
| `result.Text` | `result.Text` | Full document text string |
| `result.Regions` | `result.Pages[0].Lines` or `.Words` | Structured text regions |
| `region.Text` | `word.Text` / `line.Text` | Text content of a region |
| `region.Score` (float 0-1) | `word.Confidence` (int 0-100) | Confidence value — scale differs |
| `region.Rect.Center.X` | `word.X` | Horizontal position |
| `region.Rect.Center.Y` | `word.Y` | Vertical position |
| `region.Rect.Size.Width` | `word.Width` | Bounding box width |
| `region.Rect.Size.Height` | `word.Height` | Bounding box height |
| N/A | `input.LoadPdf(path)` | Native PDF input (no PaddleOCR equivalent) |
| N/A | `input.LoadImageFrames(path)` | Multi-frame TIFF (no PaddleOCR equivalent) |
| N/A | `result.SaveAsSearchablePdf(path)` | Searchable PDF output (no PaddleOCR equivalent) |

## Common Migration Issues and Solutions

### Issue 1: Confidence Scale Mismatch

**PaddleOCR:** Region confidence is a `float` from 0.0 to 1.0. A common threshold is `region.Score >= 0.8` to filter low-quality detections.

**Solution:** IronOCR confidence is an `int` percentage from 0 to 100. Multiply the PaddleOCR threshold by 100:

```csharp
// PaddleOCR: filter at 0.8
var highConfidence = result.Regions.Where(r => r.Score >= 0.8);

// IronOCR equivalent: filter at 80
var highConfidence = result.Pages
    .SelectMany(p => p.Words)
    .Where(w => w.Confidence >= 80);
```

Document-level confidence is available as `result.Confidence` for quick quality gating. The [confidence scores guide](https://ironsoftware.com/csharp/ocr/how-to/tesseract-result-confidence/) covers per-word and document-level thresholds.

### Issue 2: Reading Order Assumptions

**PaddleOCR:** `result.Regions` is ordered by detection sequence, not reading order. Any code that consumes `result.Text` expecting top-to-bottom, left-to-right output relies on the manual sort pattern used throughout PaddleOCR examples.

**Solution:** IronOCR's `result.Text` is already in reading order. Remove the manual sort. For cases where the sort was used to build a line-by-line output, use `result.Pages[0].Lines` directly:

```csharp
// PaddleOCR: manual sort required for reading order
var lines = result.Regions
    .OrderBy(r => r.Rect.Center.Y)
    .ThenBy(r => r.Rect.Center.X)
    .Select(r => r.Text);

// IronOCR: reading order is the default
var lines = result.Pages[0].Lines.Select(l => l.Text);
```

### Issue 3: OpenCV Mat Conversion Code

**PaddleOCR:** Some codebases contain helper methods that load images from streams or byte arrays by first writing to a temp file and then calling `Cv2.ImRead()`. These patterns exist because `Cv2.ImRead()` only accepts file paths.

**Solution:** IronOCR's `OcrInput` accepts streams and byte arrays directly. Delete the temp file intermediary:

```csharp
// PaddleOCR: stream → temp file → Mat → OCR
string tempPath = Path.GetTempFileName() + ".png";
using (var fs = File.Create(tempPath))
    await imageStream.CopyToAsync(fs);
using Mat mat = Cv2.ImRead(tempPath);
PaddleOcrResult result = ocr.Run(mat);
File.Delete(tempPath);

// IronOCR: stream → OCR (no temp file)
using var input = new OcrInput();
input.LoadImage(imageStream);
var result = ocr.Read(input);
```

The [stream input guide](https://ironsoftware.com/csharp/ocr/how-to/input-streams/) covers stream loading from HTTP responses, database blobs, and memory streams.

### Issue 4: Async Initialization Pattern Removal

**PaddleOCR:** The engine initialization is async because the model download involves network I/O. This forces async throughout the call chain, which can be problematic in synchronous contexts such as constructors or non-async event handlers.

**Solution:** IronOCR initialization is synchronous. `new IronTesseract()` does not perform I/O. Remove the `await` and the `async` modifier from any method whose only async operation was the model download:

```csharp
// PaddleOCR: async forced by model download
public async Task<string> ExtractTextAsync(string imagePath)
{
    FullOcrModel models = await OnlineFullModels.EnglishV4.DownloadAsync();
    using PaddleOcrAll ocr = new PaddleOcrAll(models);
    using Mat mat = Cv2.ImRead(imagePath);
    return ocr.Run(mat).Text;
}

// IronOCR: synchronous — no async required unless the caller needs it
public string ExtractText(string imagePath)
{
    var ocr = new IronTesseract();
    using var input = new OcrInput();
    input.LoadImage(imagePath);
    return ocr.Read(input).Text;
}
```

IronOCR also provides native async support via `ocr.ReadAsync(input)` when non-blocking execution is genuinely needed in an async context.

### Issue 5: Docker Build Step Cleanup

**PaddleOCR:** The Dockerfile contains `apt-get install libopencv-dev`, a `COPY models/ /app/models/` instruction, and often a model pre-download `RUN` step. The base image is frequently the NVIDIA CUDA image for GPU deployments.

**Solution:** Remove all PaddleOCR-specific Dockerfile instructions. The IronOCR Docker image requires no special base image and no model copy step:

```dockerfile
# PaddleOCR Dockerfile (remove all of this)
FROM nvidia/cuda:11.8.0-cudnn8-runtime-ubuntu22.04
RUN apt-get update && apt-get install -y libopencv-dev libgdiplus
COPY models/ /app/models/
COPY . /app

# IronOCR Dockerfile (clean)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY . /app
WORKDIR /app
ENTRYPOINT ["dotnet", "YourApp.dll"]
```

The resulting image drops from approximately 1.5GB to approximately 400MB. The [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/) covers Linux library requirements and multi-architecture builds.

### Issue 6: CI/CD Model Cache Invalidation

**PaddleOCR:** CI/CD pipelines that cache the NuGet restore step must separately manage model file caching. A common pattern is caching a `models/` folder between runs. When the wrapper version updates, the cache key changes and models must be re-downloaded from Baidu servers, adding 30-60 seconds to the pipeline.

**Solution:** IronOCR has no model cache directory. The only cache required is the standard NuGet package cache. No separate cache step, no cache invalidation on wrapper updates, no download from third-party servers during CI:

```yaml
# Remove from CI/CD pipeline:
# - name: Cache PaddleOCR models
#   uses: actions/cache@v3
#   with:
#     path: models/
#     key: paddleocr-models-${{ env.PADDLEOCR_VERSION }}

# IronOCR only needs standard NuGet caching:
- name: Cache NuGet packages
  uses: actions/cache@v3
  with:
    path: ~/.nuget/packages
    key: nuget-${{ hashFiles('**/*.csproj') }}
```

## PaddleOCR (.NET) Migration Checklist

### Pre-Migration

Audit the codebase to identify all PaddleOCR usage before making any changes:

```bash
# Find all PaddleOCR namespace imports
grep -rn "using Sdcb.PaddleOCR" --include="*.cs" .

# Find all OpenCvSharp imports (added as PaddleOCR dependency)
grep -rn "using OpenCvSharp" --include="*.cs" .

# Find all Mat usage patterns
grep -rn "Cv2\.ImRead\|new Mat\|Mat mat" --include="*.cs" .

# Find all async model download calls
grep -rn "DownloadAsync\|OnlineFullModels\|LocalDetectionModel" --include="*.cs" .

# Find all GPU device configuration
grep -rn "PaddleDevice\|EnableUseGpu\|cuda" --include="*.cs" .

# Find all result region access patterns
grep -rn "result\.Regions\|region\.Score\|region\.Rect" --include="*.cs" .

# Locate model directory references in configuration files
grep -rn "PP-OCRv4\|cls_infer\|det_infer\|rec_infer" --include="*.cs" --include="*.json" --include="*.yaml" .
```

Inventory the model directories and note total size. Identify which language models are in use (Chinese, English, Japanese, etc.) to determine which `IronOcr.Languages.*` packages to add. Note whether GPU configuration is present — those files have the most surface area to clean up.

### Code Migration

1. Remove `Sdcb.PaddleOCR`, `Sdcb.PaddleOCR.Models.Online`, `Sdcb.PaddleInference.runtime.*`, `OpenCvSharp4`, and `OpenCvSharp4.runtime.*` from the `.csproj` file
2. Add `IronOcr` to the `.csproj` file
3. Add `IronOcr.Languages.*` packages for each non-English language previously downloaded as a PaddleOCR model
4. Replace all `using Sdcb.PaddleOCR*` and `using OpenCvSharp` directives with `using IronOcr`
5. Add `IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";` at application startup
6. Replace `FullOcrModel models = await OnlineFullModels.*.DownloadAsync()` with nothing — remove the line entirely
7. Replace `new PaddleOcrAll(models)` with `new IronTesseract()`
8. Replace `new PaddleOcrAll(models, PaddleDevice.Gpu(deviceId: 0))` with `new IronTesseract()`
9. Replace `Mat mat = Cv2.ImRead(path)` with `var input = new OcrInput(); input.LoadImage(path);`
10. Replace `ocr.Run(mat)` with `ocr.Read(input)`
11. Replace `result.Regions` access with `result.Pages[0].Lines` or `result.Pages[0].Words`
12. Replace `region.Score >= threshold` with `word.Confidence >= threshold * 100`
13. Replace `region.Rect.Center.X / .Center.Y` with `word.X / word.Y`
14. Remove manual sort logic — IronOCR output is already in reading order
15. Remove `models/` directory and all model files from the repository and deployment scripts

### Post-Migration

- Verify `dotnet build` succeeds with zero references to `Sdcb.*`, `OpenCvSharp`, or `PaddleInference` in the build output
- Run OCR on the same representative document set used to validate PaddleOCR output and compare text accuracy
- Confirm confidence values are being read as integers 0-100 (not floats 0-1) at all filter points
- Verify reading order is correct without manual sorting — check multi-column and invoice layouts specifically
- Test the Docker image build completes without the CUDA base image or `apt-get install libopencv-dev`
- Confirm Docker image size is below 500MB
- Run the CI/CD pipeline end-to-end and verify no external downloads occur during the build
- Test air-gapped deployment: verify the application starts and processes documents with no outbound network connections
- For any multi-frame TIFF inputs, verify all frames are processed and the frame count matches the source file
- For any PDF inputs, verify `input.LoadPdf()` produces the same page count and text content as the previous PdfiumViewer-based conversion

## Key Benefits of Migrating to IronOCR

**Deployment Artifacts Shrink by 80 Percent.** The PaddleOCR deployment footprint — `paddle_inference.dll`, OpenCV DLLs, and three model directories — adds 300-500MB to every deployment target. After migration, the IronOCR deployment is approximately 80MB. Docker images drop from ~1.5GB to ~400MB. Container startup is faster, storage costs are lower, and deployment pipelines that previously transferred 500MB of artifacts now transfer 80MB.

**Cold Start Drops from Seconds to Milliseconds.** PaddleOCR loads three neural network model files from disk on first inference, adding a 3-5 second pause before the first call returns. In serverless functions, auto-scaling scenarios, or any context where new instances spin up on demand, that cold start is paid repeatedly. IronOCR's engine is bundled and initializes in under one second. The [basic OCR example](https://ironsoftware.com/csharp/ocr/examples/simple-csharp-ocr-tesseract/) demonstrates the initialization pattern.

**Language Coverage Expands from 14 to 125 Without Infrastructure Work.** PaddleOCR supports 14 languages. Adding any of the 111 languages IronOCR supports beyond PaddleOCR's ceiling is a single NuGet package addition per language — no model download, no directory management, no version synchronization. Teams whose document volume expands into new markets do not face a rewrite or a new infrastructure project to add Polish, Vietnamese, Greek, or Hebrew OCR support. The full [language catalog](https://ironsoftware.com/csharp/ocr/languages/) shows all 125+ available packs.

**Searchable PDF Output Requires One Line.** PaddleOCR returns text regions. Converting those regions into a searchable PDF layer requires a separate PDF library, pixel-to-point coordinate conversion, and invisible text injection code that becomes permanent maintenance. After migration, `result.SaveAsSearchablePdf("output.pdf")` replaces that entire subsystem. Scanned document archive workflows, fax-to-PDF pipelines, and document management integrations all benefit directly. The [searchable PDF how-to](https://ironsoftware.com/csharp/ocr/how-to/searchable-pdf/) and the [PDF data extraction blog post](https://ironsoftware.com/csharp/ocr/blog/using-ironocr/pdf-data-extraction-dotnet/) cover the full output options.

**No External Network Connections at Any Stage.** PaddleOCR connects to Baidu's `bj.bcebos.com` storage for model downloads. In environments where outbound connections are restricted — government networks, air-gapped systems, financial services infrastructure — that connection requires either a firewall exception or a pre-download workflow that adds CI/CD complexity. IronOCR makes no external connections at runtime. Models restore as part of `dotnet restore` from NuGet and are present in the deployment output. The [AWS deployment guide](https://ironsoftware.com/csharp/ocr/get-started/aws/) and [Azure deployment guide](https://ironsoftware.com/csharp/ocr/get-started/azure/) cover cloud-specific configuration for environments with network restrictions.

**One Commercial Support Contact for the Entire OCR Stack.** PaddleOCR issues span the `Sdcb.PaddleOCR` wrapper (community GitHub), the PaddlePaddle framework (Baidu), OpenCvSharp (community), and CUDA/cuDNN (NVIDIA). Each layer has a different support channel with no guarantee of response time. IronOCR is a single product from Iron Software with commercial email support and priority response tiers. The [IronOCR documentation hub](https://ironsoftware.com/csharp/ocr/docs/) consolidates all API documentation, how-to guides, and troubleshooting resources in one place.
