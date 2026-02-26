# Migrating from TesseractOCR to IronOCR

This guide walks .NET developers through a complete migration from the TesseractOCR NuGet package (the Sicos1977/Kees van Spelde fork) to [IronOCR](https://ironsoftware.com/csharp/ocr/). It covers the full replacement path: removing external preprocessing dependencies, enabling native PDF input and searchable PDF output, updating namespaces and API calls, and verifying the migrated integration. No prior reading of the comparison article is required.

## Why Migrate from TesseractOCR

TesseractOCR is an actively maintained community wrapper that targets modern .NET and bundles Tesseract 5 native libraries. Upgrading to it from older wrappers solves framework compatibility. It does not solve the architectural gaps that sit below the wrapper layer. When those gaps surface in production, the migration conversation starts.

**Preprocessing lives entirely outside the library.** TesseractOCR calls `engine.Process(image)` on whatever pixels you supply. A skewed scan, a low-contrast fax, a phone photo of a receipt — all of them go to the Tesseract engine raw. Recovering usable output requires adding SixLabors.ImageSharp, SkiaSharp, or a similar imaging library, writing manual filter chains with parameters tuned per document type, and routing the preprocessed image through a temp file because `TesseractOCR.Pix.Image` expects a file path. Deskew is not available in standard .NET imaging libraries at all — it requires implementing a Hough transform angle detection algorithm from scratch, typically 50 to 100 additional lines. This is not a one-time setup cost; it recurs every time a new document type enters the pipeline.

**PDF input requires a second library and a temp-file pipeline.** TesseractOCR processes images, not PDFs. Every PDF workflow requires an additional package — Docnet.Core, PdfiumViewer, or similar — to render PDF pages to BGRA byte arrays, a helper method to convert those bytes to a format TesseractOCR can read, and temp file creation and cleanup logic wrapping the entire loop. The result is approximately 100 lines of infrastructure code surrounding every PDF OCR operation. Password-protected PDFs require a third library (iTextSharp with AGPL licensing, or PDFsharp) just to decrypt before processing.

**Searchable PDF output has no path.** Teams that need to produce machine-readable PDFs from scanned documents — a common requirement for document management, archiving, and compliance workflows — find that TesseractOCR provides no mechanism for it. There is no `SaveAsSearchablePdf()`, no hOCR-to-PDF pipeline, no output format beyond extracted text. Adding this capability requires either a separate PDF library or abandoning TesseractOCR entirely.

**TIFF multi-frame documents require a manual page loop.** Multi-page TIFF files, common in fax workflows and document scanners, have no native multi-frame handling in TesseractOCR. Extracting all frames requires loading the TIFF with an external library, iterating frames, saving each to a temp file, and feeding each temp file through the OCR engine separately.

**The community size limits practical support.** TesseractOCR has approximately 200,000 NuGet downloads. Stack Overflow, blog posts, and GitHub issue threads about .NET Tesseract wrappers overwhelmingly reference the charlesw API — `TesseractEngine`, `Pix.LoadFromFile` — not the Sicos1977 API. Real-world troubleshooting for TesseractOCR-specific problems hits this wall quickly.

### The Fundamental Problem

TesseractOCR has no preprocessing and no PDF support. Every production document workflow ends up requiring external libraries just to reach the point where OCR can run:

```csharp
// TesseractOCR: three packages, a temp file, and manual byte conversion
// just to OCR one PDF page — before any preprocessing
// dotnet add package TesseractOCR
// dotnet add package Docnet.Core
// dotnet add package SixLabors.ImageSharp   (preprocessing)

using var library = DocLib.Instance;
using var docReader = library.GetDocReader(pdfPath, new PageDimensions(200, 200));
using var pageReader = docReader.GetPageReader(0);
var bytes = pageReader.GetImage(); // BGRA — not a format Pix.Image accepts directly

string tempPath = Path.GetTempFileName() + ".png";
SaveBgraAsPng(bytes, pageReader.GetPageWidth(), pageReader.GetPageHeight(), tempPath);
// ^ 30+ line helper method needed here

using var engine = new Engine(@"./tessdata", Language.English, EngineMode.Default);
using var image = TesseractOCR.Pix.Image.LoadFromFile(tempPath);
using var page = engine.Process(image);
string text = page.Text;
File.Delete(tempPath); // hope this succeeds
```

```csharp
// IronOCR: one package, three lines, preprocessing automatic
// dotnet add package IronOcr

var ocr = new IronTesseract();
using var input = new OcrInput();
input.LoadPdf(pdfPath);
string text = ocr.Read(input).Text;
```

## IronOCR vs TesseractOCR: Feature Comparison

The table below maps the capabilities that matter most during migration evaluation.

| Feature | TesseractOCR | IronOCR |
|---|---|---|
| NuGet package | `TesseractOCR` | `IronOcr` |
| .NET compatibility | .NET 6.0, 7.0, 8.0 | .NET Framework 4.6.2+, .NET Core, .NET 5/6/7/8/9 |
| License | Apache 2.0 (free) | Commercial (perpetual, from $749) |
| Tessdata management | Required (manual download from GitHub) | Not required (bundled internally) |
| Built-in preprocessing | None | Deskew, DeNoise, Contrast, Binarize, Sharpen, Scale, Dilate, Erode, Invert |
| Deep background noise removal | No | Yes (`DeepCleanBackgroundNoise()`) |
| Native PDF input | No (requires Docnet.Core or similar) | Yes (`input.LoadPdf()`) |
| Password-protected PDF | No (requires third library to decrypt) | Yes (single `Password` parameter) |
| Searchable PDF output | No | Yes (`result.SaveAsSearchablePdf()`) |
| Multi-frame TIFF input | No (requires external frame extraction) | Yes (`input.LoadImageFrames()`) |
| Stream and byte array input | No (requires temp file intermediary) | Yes (direct `LoadImage(stream)`, `LoadImage(bytes)`) |
| Thread safety | No (one engine instance per thread) | Yes (single `IronTesseract` shared across threads) |
| Region-based OCR | No | Yes (`CropRectangle`) |
| Barcode reading during OCR | No | Yes (`ocr.Configuration.ReadBarCodes = true`) |
| Structured output (pages, words, coordinates) | No (flat text string only) | Yes (`Pages`, `Paragraphs`, `Lines`, `Words` with X/Y) |
| Confidence scoring | Document-level float (0.0–1.0) | Document and word-level double (0–100) |
| hOCR export | No | Yes |
| 125+ language NuGet packs | No | Yes |
| Cross-platform deployment | Windows, Linux, macOS | Windows, Linux, macOS, Docker, Azure, AWS |
| Commercial support | No (single volunteer maintainer) | Yes (email, SLA options) |

## Quick Start: TesseractOCR to IronOCR Migration

### Step 1: Replace NuGet Package

Remove TesseractOCR and any libraries added to support it:

```bash
dotnet remove package TesseractOCR
dotnet remove package Docnet.Core
dotnet remove package SixLabors.ImageSharp
```

Install IronOCR from [NuGet](https://www.nuget.org/packages/IronOcr):

```bash
dotnet add package IronOcr
```

### Step 2: Update Namespaces

Replace all TesseractOCR namespace imports with IronOcr:

```csharp
// Before (TesseractOCR)
using TesseractOCR;
using TesseractOCR.Enums;

// After (IronOCR)
using IronOcr;
```

### Step 3: Initialize License

Add license initialization once at application startup, before any OCR call:

```csharp
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
```

A free trial license is available from the [IronOCR licensing page](https://ironsoftware.com/csharp/ocr/licensing/) for evaluation.

## Code Migration Examples

### Replacing the External Preprocessing Pipeline

TesseractOCR requires an external imaging library for every document quality improvement. The code below shows the pattern that teams write when document quality is variable — grayscale conversion, contrast adjustment, noise reduction, and a temp file write before OCR can run. Deskew (correcting a tilted scan) is not available in standard .NET imaging libraries and requires a separate algorithm.

**TesseractOCR Approach:**

```csharp
// Requires: dotnet add package SixLabors.ImageSharp
// Manual preprocessing — parameters must be tuned per document type
// Deskew is NOT in ImageSharp — requires custom Hough transform (~50-100 lines)

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using TesseractOCR;
using TesseractOCR.Enums;

public string ExtractFromLowQualityScan(string imagePath)
{
    using var image = Image.Load(imagePath);

    image.Mutate(x => x.Grayscale());
    image.Mutate(x => x.Contrast(1.5f));          // manual tuning required
    image.Mutate(x => x.GaussianBlur(0.5f));      // noise reduction approximation
    image.Mutate(x => x.BinaryThreshold(0.5f));   // threshold requires per-doc adjustment

    // Deskew omitted — no built-in support, ~80 lines of additional code

    string tempPath = Path.GetTempFileName() + ".png";
    try
    {
        image.Save(tempPath);

        using var engine = new Engine(@"./tessdata", Language.English, EngineMode.Default);
        using var pix = TesseractOCR.Pix.Image.LoadFromFile(tempPath);
        using var page = engine.Process(pix);

        return page.Text;
    }
    finally
    {
        File.Delete(tempPath);
    }
}
```

**IronOCR Approach:**

```csharp
// No external imaging library
// No temp file — OcrInput accepts a path, stream, or byte array directly
// Deskew is built in — automatic angle detection and correction

using IronOcr;

public string ExtractFromLowQualityScan(string imagePath)
{
    using var input = new OcrInput();
    input.LoadImage(imagePath);
    input.Deskew();           // automatic angle correction
    input.DeNoise();          // intelligent noise removal
    input.Contrast();         // automatic contrast enhancement
    input.Binarize();         // clean black-and-white conversion

    var ocr = new IronTesseract();
    return ocr.Read(input).Text;
}
```

Removing the ImageSharp dependency eliminates the tuning cycle entirely. The `OcrInput` preprocessing pipeline applies algorithms calibrated for document OCR — no guessing at contrast multipliers or blur radii. The [image filters tutorial](https://ironsoftware.com/csharp/ocr/tutorials/c-sharp-ocr-image-filters/) and [image quality correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/) cover every available filter with parameter options for cases where the defaults need adjustment.

### Replacing Multi-Frame TIFF Processing

Fax documents, document scanner output, and archival files frequently arrive as multi-page TIFF files. TesseractOCR has no multi-frame support — each frame must be extracted with an external library, saved to disk, and fed through the engine one at a time. IronOCR loads the entire TIFF in a single call.

**TesseractOCR Approach:**

```csharp
// Requires: dotnet add package SixLabors.ImageSharp
// Manual frame extraction — every frame becomes a temp file on disk

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Tiff;
using TesseractOCR;
using TesseractOCR.Enums;

public string ExtractFromMultiPageTiff(string tiffPath)
{
    var allText = new System.Text.StringBuilder();
    var tempFiles = new List<string>();

    try
    {
        using var image = Image.Load(tiffPath);
        using var engine = new Engine(@"./tessdata", Language.English, EngineMode.Default);

        for (int frameIndex = 0; frameIndex < image.Frames.Count; frameIndex++)
        {
            // Clone frame and save to temp file — no in-memory path
            using var frameImage = image.Frames.CloneFrame(frameIndex);
            string tempPath = Path.GetTempFileName() + ".png";
            tempFiles.Add(tempPath);
            frameImage.SaveAsPng(tempPath);

            using var pix = TesseractOCR.Pix.Image.LoadFromFile(tempPath);
            using var page = engine.Process(pix);

            allText.AppendLine($"=== Frame {frameIndex + 1} ===");
            allText.AppendLine(page.Text);
        }
    }
    finally
    {
        foreach (var f in tempFiles)
            try { File.Delete(f); } catch { }
    }

    return allText.ToString();
}
```

**IronOCR Approach:**

```csharp
// No external library for frame extraction
// All frames processed in one Read() call — no manual loop required

using IronOcr;

public string ExtractFromMultiPageTiff(string tiffPath)
{
    var ocr = new IronTesseract();
    using var input = new OcrInput();
    input.LoadImageFrames(tiffPath);   // loads all frames automatically
    var result = ocr.Read(input);

    // Access per-page text if needed
    foreach (var page in result.Pages)
        Console.WriteLine($"Frame {page.PageNumber}: {page.Text}");

    return result.Text;
}
```

The frame extraction loop, temp file list, the `try`/`finally` cleanup block — all of that goes away. For a 20-page fax TIFF, this replaces approximately 40 lines with 6. The [TIFF and GIF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-tiff-gif/) covers multi-frame loading options including selective frame ranges.

### Generating Searchable PDF Output

This scenario has no migration path in TesseractOCR — it simply cannot be done. Scanned PDFs that need to become machine-readable, text-selectable documents (for search indexing, accessibility, or archiving) require producing a searchable PDF output. TesseractOCR produces extracted text only. IronOCR produces the searchable PDF directly.

**TesseractOCR Approach:**

```csharp
// No path available — TesseractOCR cannot produce any PDF output.
// The closest workaround requires a separate PDF library (iTextSharp AGPL,
// or similar) to overlay extracted text onto the original PDF manually.
// This is 150-300 lines of additional code and introduces AGPL license concerns.

// The best available output from TesseractOCR:
using var engine = new Engine(@"./tessdata", Language.English, EngineMode.Default);
using var pix = TesseractOCR.Pix.Image.LoadFromFile("scanned-page.png");
using var page = engine.Process(pix);

string extractedText = page.Text; // flat string — no PDF output possible
File.WriteAllText("output.txt", extractedText);
// Cannot produce a searchable PDF — no API exists for this
```

**IronOCR Approach:**

```csharp
// Native searchable PDF output — no additional library required
// Input can be a scanned image, a scanned PDF, or a multi-page TIFF

using IronOcr;

public void CreateSearchablePdf(string scannedPdfPath, string outputPath)
{
    var ocr = new IronTesseract();
    using var input = new OcrInput();
    input.LoadPdf(scannedPdfPath);
    input.Deskew();     // improve accuracy before generating the output
    input.DeNoise();

    var result = ocr.Read(input);
    result.SaveAsSearchablePdf(outputPath);   // searchable, text-selectable PDF
}
```

The `SaveAsSearchablePdf()` call embeds OCR text into the PDF as an invisible layer behind the original scanned image. The document remains visually identical but becomes fully searchable, selectable, and indexable. The [searchable PDF guide](https://ironsoftware.com/csharp/ocr/how-to/searchable-pdf/) covers the full API, and the [searchable PDF example](https://ironsoftware.com/csharp/ocr/examples/make-pdf-searchable/) shows the complete working pattern.

### Replacing Byte-Array Input and Eliminating Temp Files

TesseractOCR's `Pix.Image` API accepts a file path. When image data arrives as a byte array — from a database, an HTTP multipart upload, a memory cache — TesseractOCR forces a write to a temporary file before processing. IronOCR's `OcrInput` accepts byte arrays and streams directly, removing the temp-file step entirely.

**TesseractOCR Approach:**

```csharp
// TesseractOCR.Pix.Image has no byte[] or Stream overload
// Every in-memory image must be written to disk before processing

using TesseractOCR;
using TesseractOCR.Enums;

public string ExtractFromBytes(byte[] imageBytes)
{
    // Force a disk write just to satisfy the file-path API
    string tempPath = Path.GetTempFileName() + ".png";

    try
    {
        File.WriteAllBytes(tempPath, imageBytes);

        using var engine = new Engine(@"./tessdata", Language.English, EngineMode.Default);
        using var pix = TesseractOCR.Pix.Image.LoadFromFile(tempPath);
        using var page = engine.Process(pix);

        return page.Text;
    }
    finally
    {
        // Risk: if an exception fires between WriteAllBytes and Delete,
        // temp files accumulate on the server disk
        if (File.Exists(tempPath))
            File.Delete(tempPath);
    }
}
```

**IronOCR Approach:**

```csharp
// OcrInput accepts byte arrays and streams natively
// No disk write, no temp file cleanup, no cleanup failure risk

using IronOcr;

public string ExtractFromBytes(byte[] imageBytes)
{
    var ocr = new IronTesseract();
    using var input = new OcrInput();
    input.LoadImage(imageBytes);   // direct byte array — no temp file
    return ocr.Read(input).Text;
}

public string ExtractFromStream(Stream imageStream)
{
    var ocr = new IronTesseract();
    using var input = new OcrInput();
    input.LoadImage(imageStream);  // direct stream — no intermediate buffer
    return ocr.Read(input).Text;
}
```

In web applications processing uploaded documents, the temp-file pattern accumulates disk usage under load and introduces race conditions if cleanup code throws. The [stream input guide](https://ironsoftware.com/csharp/ocr/how-to/input-streams/) and [image input guide](https://ironsoftware.com/csharp/ocr/how-to/input-images/) cover every supported input format including `MemoryStream`, `byte[]`, `Bitmap`, and file path.

### Word-Level Confidence Filtering with Structured Data

TesseractOCR returns a single document-level confidence score (`page.MeanConfidence`, a float from 0.0 to 1.0) and a flat text string. There is no per-word confidence, no word positioning, and no structural hierarchy. Building a workflow that flags uncertain words, extracts specific regions, or maps text to document coordinates requires switching to a fundamentally different output model.

**TesseractOCR Approach:**

```csharp
// Only document-level confidence available
// No word coordinates, no structural hierarchy

using TesseractOCR;
using TesseractOCR.Enums;

public void ProcessWithConfidence(string imagePath)
{
    using var engine = new Engine(@"./tessdata", Language.English, EngineMode.Default);
    using var pix = TesseractOCR.Pix.Image.LoadFromFile(imagePath);
    using var page = engine.Process(pix);

    float docConfidence = page.MeanConfidence; // 0.0 to 1.0 for the whole document

    if (docConfidence >= 0.7f)
        Console.WriteLine($"Accepted ({docConfidence:P0}): {page.Text}");
    else
        Console.WriteLine($"Rejected ({docConfidence:P0}): document needs preprocessing");

    // No way to identify WHICH words are uncertain
    // No word coordinates available
}
```

**IronOCR Approach:**

```csharp
// Per-word confidence and coordinate data
// Filter individual uncertain words without discarding the whole document

using IronOcr;

public void ProcessWithWordLevelConfidence(string imagePath)
{
    var ocr = new IronTesseract();
    using var input = new OcrInput();
    input.LoadImage(imagePath);
    var result = ocr.Read(input);

    Console.WriteLine($"Document confidence: {result.Confidence}%");

    // Iterate words and flag those below threshold
    foreach (var page in result.Pages)
    {
        foreach (var word in page.Words)
        {
            if (word.Confidence < 70)
            {
                // Low-confidence word — log position for review
                Console.WriteLine(
                    $"Low confidence word '{word.Text}' ({word.Confidence}%) " +
                    $"at X:{word.X} Y:{word.Y}");
            }
        }
    }

    // Extract only high-confidence text
    var reliableWords = result.Pages
        .SelectMany(p => p.Words)
        .Where(w => w.Confidence >= 70)
        .Select(w => w.Text);

    Console.WriteLine(string.Join(" ", reliableWords));
}
```

Per-word confidence filtering is essential for invoice processing, form extraction, and any workflow where acting on uncertain text is worse than flagging it for review. The [confidence scores guide](https://ironsoftware.com/csharp/ocr/how-to/tesseract-result-confidence/) covers the full scoring model, and the [read results guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) documents the complete structured output hierarchy.

## TesseractOCR API to IronOCR Mapping Reference

| TesseractOCR | IronOCR | Notes |
|---|---|---|
| `new Engine(tessDataPath, Language.English, EngineMode.Default)` | `new IronTesseract()` | No tessdata path; no EngineMode selection needed |
| `TesseractOCR.Pix.Image.LoadFromFile(path)` | `input.LoadImage(path)` | Also accepts `byte[]` and `Stream` |
| `engine.Process(pixImage)` | `ocr.Read(input)` | Returns `OcrResult` instead of `Page` |
| `page.Text` | `result.Text` | Identical semantics |
| `page.MeanConfidence` (0.0–1.0 float) | `result.Confidence` (0–100 double) | Scale differs — update threshold comparisons |
| `Language.English \| Language.French` | `OcrLanguage.English + OcrLanguage.French` | Addition operator, not bitwise OR |
| `EngineMode.Default` | N/A | IronOCR selects mode internally |
| `EngineMode.LstmOnly` | N/A | Automatic |
| `TesseractOCR.Exceptions.TesseractException` | `IronOcr.Exceptions.OcrException` | Fewer exception types to handle |
| `DllNotFoundException` (native missing) | Not applicable | IronOCR bundles its native dependencies |
| `BadImageFormatException` (arch mismatch) | Not applicable | Handled internally |
| External `Image.Mutate(x => x.Grayscale())` | `input.Binarize()` | Built-in, no external library |
| External `Image.Mutate(x => x.Contrast(...))` | `input.Contrast()` | Automatic calibration |
| External Hough transform deskew | `input.Deskew()` | Built-in, one method call |
| External `GaussianBlur` noise filter | `input.DeNoise()` | Intelligent noise removal |
| `DocLib.GetDocReader(pdfPath, ...)` | `input.LoadPdf(pdfPath)` | No Docnet.Core needed |
| `docReader.GetPageReader(i).GetImage()` + temp file | `input.LoadPdf(pdfPath)` | Entire loop replaced |
| `input.LoadPdf(encrypted, Password: "...")` | Single parameter — no third library needed | |
| N/A (no PDF output) | `result.SaveAsSearchablePdf(outputPath)` | No equivalent in TesseractOCR |
| N/A (no frame support) | `input.LoadImageFrames(tiffPath)` | Multi-frame TIFF in one call |
| N/A (file path only) | `input.LoadImage(stream)` / `input.LoadImage(bytes)` | Eliminates temp file pattern |
| Per-thread `Engine` instances | Single `IronTesseract` shared across threads | Thread-safe by design |
| `page.MeanConfidence` (document only) | `word.Confidence` per word | Word-level scoring available |

## Common Migration Issues and Solutions

### Issue 1: Confidence Threshold Values Break After Migration

**TesseractOCR:** `page.MeanConfidence` returns a float in the range 0.0 to 1.0. Code commonly checks `if (confidence >= 0.7f)` to accept results.

**Solution:** IronOCR reports confidence as a double on a 0–100 scale. Multiply all existing threshold values by 100. A threshold of `0.7f` becomes `70.0`. Document-level confidence is at `result.Confidence`; word-level confidence is at `word.Confidence` inside `result.Pages[n].Words`.

```csharp
// Before (TesseractOCR): page.MeanConfidence >= 0.7f
// After (IronOCR):
var result = new IronTesseract().Read("document.png");
if (result.Confidence >= 70.0)
{
    Console.WriteLine(result.Text);
}
```

### Issue 2: Temp Directory Fills Up After Migration Attempt

**TesseractOCR:** Code written around the `Pix.Image.LoadFromFile()` constraint frequently creates temp files that are cleaned up in `finally` blocks. If the `finally` block itself throws, or if the application is forcibly terminated, temp files accumulate.

**Solution:** Replace all `File.WriteAllBytes(tempPath, bytes)` + `Pix.Image.LoadFromFile(tempPath)` patterns with `input.LoadImage(bytes)` or `input.LoadImage(stream)`. Once no code creates temp files, the cleanup logic and the directory creation for temp storage can be deleted entirely. Search for `GetTempFileName`, `GetTempPath`, and `SaveBgraAsPng` to find all occurrences.

```bash
grep -rn "GetTempFileName\|GetTempPath\|SaveBgraAsPng" --include="*.cs" .
```

```csharp
// Before: byte[] → temp file → Pix.Image.LoadFromFile
// After: byte[] → OcrInput directly
using var input = new OcrInput();
input.LoadImage(imageBytes);   // no disk write
var result = ocr.Read(input);
```

See the [image input guide](https://ironsoftware.com/csharp/ocr/how-to/input-images/) for all supported input formats.

### Issue 3: Language Operator Change Causes Compiler Error

**TesseractOCR:** Multi-language OCR uses bitwise OR on a flags enum: `Language.English | Language.French`. This is a `[Flags]` enum pattern.

**Solution:** IronOCR uses the addition operator: `OcrLanguage.English + OcrLanguage.French`. These look similar but are different operators. A find-and-replace for `Language.` to `OcrLanguage.` combined with `|` to `+` inside language expressions handles the majority of cases. Verify that any runtime-built language combinations also use `+`.

```csharp
// Before (TesseractOCR):
var engine = new Engine(@"./tessdata",
    Language.English | Language.French | Language.German,
    EngineMode.Default);

// After (IronOCR):
var ocr = new IronTesseract();
ocr.Language = OcrLanguage.English + OcrLanguage.French + OcrLanguage.German;
```

### Issue 4: Docnet and ImageSharp Packages Still Referenced After Uninstall

**TesseractOCR:** Projects using TesseractOCR for PDF workflows typically have Docnet.Core as a direct dependency, and SixLabors.ImageSharp or SkiaSharp for preprocessing. After switching to IronOCR, these packages frequently remain in the `.csproj` because the using statements have not been fully removed.

**Solution:** After removing the packages from `.csproj`, search for any remaining `using Docnet.Core`, `using SixLabors.ImageSharp`, and related namespace references. If `using` statements reference namespaces that no longer exist in the dependency tree, the compiler will flag them — but only if the `dotnet remove package` commands were actually run.

```bash
grep -rn "using Docnet\|using SixLabors\|using SkiaSharp" --include="*.cs" .
```

Remove the identified files' references, then delete the preprocessing helper methods (`SaveBgraAsPng`, `ApplyGrayscale`, `ApplyThreshold`, and similar) that served the old pipeline.

### Issue 5: Docker Image Size Increases After Migration

**TesseractOCR:** Some Docker configurations install Tesseract via `apt-get install tesseract-ocr tesseract-ocr-eng` as a system package, then reference those system binaries. This adds approximately 30-80MB to the image depending on language packs.

**Solution:** IronOCR bundles its own Tesseract binaries inside the NuGet package. The `apt-get install tesseract-ocr` line in the Dockerfile is no longer needed and should be removed. Language packs also come from NuGet, not from `apt-get install tesseract-ocr-fra`. The [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/) provides validated base image configurations and the exact packages required for IronOCR to run in a container.

```dockerfile
# Remove these lines after migration:
# RUN apt-get install -y tesseract-ocr tesseract-ocr-eng tesseract-ocr-fra
# COPY ./tessdata /app/tessdata
```

### Issue 6: `TesseractException` and `DllNotFoundException` Catch Blocks Become Unreachable

**TesseractOCR:** Robust TesseractOCR integrations catch `TesseractOCR.Exceptions.TesseractException`, `DllNotFoundException` (for missing native binaries), and `BadImageFormatException` (for architecture mismatches). These exception types are defensive responses to the instability of tessdata and native binary deployment.

**Solution:** IronOCR bundles native dependencies and manages initialization internally. `DllNotFoundException` and `BadImageFormatException` do not apply. Remove those catch blocks. The exception surface reduces to `IronOcr.Exceptions.OcrException` for OCR failures and standard `IOException` for file access problems.

```csharp
// Before: five exception types to handle
catch (TesseractOCR.Exceptions.TesseractException ex) { ... }
catch (DllNotFoundException ex) { ... }
catch (BadImageFormatException ex) { ... }
catch (OutOfMemoryException ex) { ... }

// After: two exception types
catch (IronOcr.Exceptions.OcrException ex) { ... }
catch (IOException ex) { ... }
```

## TesseractOCR Migration Checklist

### Pre-Migration Tasks

Audit all TesseractOCR usage points in the codebase:

```bash
grep -rn "using TesseractOCR" --include="*.cs" .
grep -rn "new Engine(" --include="*.cs" .
grep -rn "Pix\.Image\.LoadFromFile\|engine\.Process\|page\.Text\|MeanConfidence" --include="*.cs" .
grep -rn "Language\." --include="*.cs" .
```

Identify all supporting infrastructure that will be removed:

```bash
grep -rn "using Docnet\|using SixLabors\|GetTempFileName\|SaveBgraAsPng" --include="*.cs" .
grep -rn "tessdata" --include="*.cs" .
grep -rn "tessdata" --include="*.csproj" .
grep -rn "tessdata" Dockerfile 2>/dev/null || true
```

Document current accuracy baseline on a representative sample of documents before migration so post-migration quality can be verified.

### Code Update Tasks

1. Run `dotnet remove package TesseractOCR`
2. Run `dotnet remove package Docnet.Core` (if present)
3. Run `dotnet remove package SixLabors.ImageSharp` (if added for preprocessing)
4. Run `dotnet add package IronOcr`
5. Add `IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY"` at application startup
6. Replace `using TesseractOCR` and `using TesseractOCR.Enums` with `using IronOcr`
7. Replace `new Engine(tessDataPath, Language.English, EngineMode.Default)` with `new IronTesseract()`
8. Replace `TesseractOCR.Pix.Image.LoadFromFile(path)` with `input.LoadImage(path)` on an `OcrInput` instance
9. Replace `engine.Process(pixImage)` with `ocr.Read(input)`
10. Replace `page.Text` with `result.Text`
11. Update confidence threshold comparisons — multiply all 0.0–1.0 values by 100 for the IronOCR 0–100 scale
12. Replace `Language.X | Language.Y` with `OcrLanguage.X + OcrLanguage.Y`
13. Delete all preprocessing helper methods (`SaveBgraAsPng`, manual filter chains, temp file logic)
14. Replace Docnet PDF rendering loops with `input.LoadPdf(path)` or `input.LoadPdfPages(path, start, end)`
15. Replace multi-frame TIFF loops with `input.LoadImageFrames(tiffPath)`
16. Replace `File.WriteAllBytes(tempPath, bytes)` + `LoadFromFile(tempPath)` with `input.LoadImage(bytes)`
17. Update catch blocks — remove `TesseractException`, `DllNotFoundException`, `BadImageFormatException`
18. Remove tessdata folder from project output directory configuration and Docker images

### Post-Migration Testing

- Confirm `dotnet build` produces zero compiler errors and zero unreachable-catch warnings
- Run OCR against the pre-migration accuracy baseline sample and compare results
- Verify multi-page TIFF files produce the correct number of extracted pages
- Confirm searchable PDF output opens in a PDF viewer with selectable text
- Test byte-array and stream input paths from the application's actual data sources
- Verify word-level confidence values are in the 0–100 range (not 0.0–1.0)
- Run parallel processing tests to confirm no per-thread engine allocation warnings
- Deploy to the target environment (Docker, Azure, Linux) and confirm IronOCR initializes without `DllNotFoundException`
- Verify no tessdata folder or `.traineddata` file is referenced anywhere in deployment scripts

## Key Benefits of Migrating to IronOCR

**Preprocessing becomes a one-line configuration, not a 100-line dependency.** After migration, `input.Deskew()`, `input.DeNoise()`, and `input.Contrast()` replace an external imaging library, manual parameter tuning, and the temp file write that connected the two. Phone photos, skewed scans, and low-contrast faxes — the document types that previously required a dedicated preprocessing engineer — produce reliable output from the built-in pipeline. The [preprocessing features page](https://ironsoftware.com/csharp/ocr/features/preprocessing/) lists every available filter.

**PDF is a first-class input and output format.** The Docnet dependency, the BGRA-to-PNG conversion helper, the temp file management loop, the third library for password-protected files — all of that goes away. Any PDF that arrives in the system goes directly into `input.LoadPdf()`. Any scanned document that needs to become searchable goes out through `result.SaveAsSearchablePdf()`. The entire PDF pipeline that required 100+ lines in TesseractOCR becomes a handful of method calls. Explore the [PDF OCR use case page](https://ironsoftware.com/csharp/ocr/use-case/pdf-ocr-csharp/) for the full range of supported PDF workflows.

**Structured output replaces flat text strings.** `result.Pages`, `result.Paragraphs`, `result.Lines`, and `result.Words` expose the document structure with per-element coordinates and per-word confidence scores. Workflows that previously required parsing heuristics to find specific fields — invoice numbers, dates, amounts — can use word-level coordinates and confidence filtering instead. This is the foundation for building reliable form extraction and document processing pipelines on top of IronOCR's [OCR results features](https://ironsoftware.com/csharp/ocr/features/ocr-results/).

**Deployment stops requiring tessdata orchestration.** The tessdata folder, the curl download scripts, the Docker `COPY ./tessdata` layer, the CI/CD cache configuration for `.traineddata` files — all of that disappears. Languages ship as NuGet packages, versioned, restored with the rest of the project dependencies, and deployed identically whether the target is a developer workstation, a Docker container, an Azure App Service, or an AWS Lambda. The [Azure deployment guide](https://ironsoftware.com/csharp/ocr/get-started/azure/) and [Linux deployment guide](https://ironsoftware.com/csharp/ocr/get-started/linux/) provide validated configurations for production environments.

**The licensing model is predictable.** TesseractOCR is free, but the infrastructure it requires is not — developer time for preprocessing implementation, PDF library evaluation, tessdata deployment scripting, and ongoing maintenance of the external dependency chain. IronOCR's perpetual license ($749 Lite, $1,499 Professional, $2,999 Enterprise) is a one-time cost that replaces weeks of infrastructure work and eliminates the recurring maintenance surface. Commercial support with a guaranteed response path replaces reliance on a single volunteer maintainer's GitHub issue queue.
