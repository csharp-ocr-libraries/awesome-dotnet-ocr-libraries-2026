# Migrating from Tesseract to IronOCR

This guide provides a direct migration path from the charlesw `Tesseract` NuGet package to [IronOCR](https://ironsoftware.com/csharp/ocr/). It covers the specific steps required to eliminate tessdata folder management, replace `TesseractEngine` and `Pix` initialization patterns, add a built-in preprocessing pipeline, and unlock native PDF support — without duplicating the material already examined in the comparison article for this library.

## Why Migrate from Tesseract

The charlesw `Tesseract` package exposes genuine OCR capability, and its 8 million NuGet downloads prove it. The friction is not in the engine — it is in the infrastructure you must build around the engine before you can ship production-quality output. Four specific pain points drive most migration decisions.

**Tessdata folder management compounds with every environment.** Before a single word can be recognized, the tessdata path must exist, be populated with the correct `.traineddata` files for every language your application needs, and be accessible at exactly the path passed to `TesseractEngine`. That means separate folder setup for development machines, CI builds, staging servers, production hosts, and Docker containers. A missing file throws `TesseractException: Failed to initialise tesseract engine` at runtime — after deployment — with a message that does not always identify which file is absent. Every new environment is another opportunity for this failure.

**Tesseract 4.1.1 is the end of the road.** The charlesw wrapper is pinned to Tesseract 4.1.1, released in 2019. Tesseract 5.x introduced LSTM model improvements that produce measurably better accuracy on certain document types. That version is not available through this package, and the wrapper's maintenance cadence has slowed considerably since 2021. Teams that care about accuracy parity with current Tesseract releases have no upgrade path through the charlesw wrapper.

**No preprocessing means no reliability on real-world documents.** Tesseract expects clean, high-resolution, properly oriented input. It applies no built-in correction for skew, noise, low DPI, or color backgrounds. Building the preprocessing pipeline manually — grayscale conversion, contrast enhancement, binarization, median noise filtering, deskew — runs to approximately 180 lines of code using `System.Drawing.Common` (Windows-only) or requires pulling in `OpenCvSharp4` for a proper Hough-transform deskew. That pipeline must then be maintained as new document sources introduce edge cases.

**PDF is an afterthought requiring a second dependency chain.** Contracts, invoices, bank statements, and compliance documents arrive as PDFs. Tesseract cannot open a PDF. Bridging the gap requires a separate PDF rendering library — PdfiumViewer, PDFtoImage, or Docnet.Core — each with its own native binaries, platform-specific deployment steps, and license considerations. GhostScript introduces AGPL licensing implications. Password-protected PDFs add yet another library. Teams managing three separate native dependency chains across multiple environments reach a maintenance threshold that prompts a direct evaluation of single-package alternatives.

**Non-thread-safe engine design limits parallel throughput.** A `TesseractEngine` instance cannot be shared across threads. The standard parallel processing pattern creates one engine per thread, loading 40-100 MB of language model data per instance. Eight parallel threads means 320-800 MB of engine initialization overhead before any documents are processed. This is not a bug — it is the intended usage of a thread-unsafe API — but the memory cost is real and compounds as batch sizes grow.

### The Fundamental Problem

Every Tesseract application starts the same way: specifying a tessdata path that must be correct on every machine the application runs on.

**Tesseract Approach:**
```csharp
// TessDataPath must exist and be populated — breaks on first clean deployment
private const string TessDataPath = @"./tessdata";

public static string ExtractText(string imagePath)
{
    // Runtime failure if eng.traineddata is missing from TessDataPath
    if (!Directory.Exists(TessDataPath))
        throw new DirectoryNotFoundException(
            $"Tessdata not found at {TessDataPath}. " +
            "Download from https://github.com/tesseract-ocr/tessdata");

    using var engine = new TesseractEngine(TessDataPath, "eng", EngineMode.Default);
    using var img   = Pix.LoadFromFile(imagePath);  // Leptonica Pix object
    using var page  = engine.Process(img);
    return page.GetText();
}
```

**IronOCR Approach:**
```csharp
// No tessdata folder. No path. No file check. Just OCR.
var text = new IronTesseract().Read("document.jpg").Text;
```

The entire `TessDataPath` constant, the `Directory.Exists` guard, the `Pix` object, and the three-level `using` nesting disappear. Language data is embedded in the NuGet package.

## IronOCR vs Tesseract: Feature Comparison

The following table covers the capabilities that matter most during migration decisions.

| Feature | Tesseract (charlesw) | IronOCR |
|---|---|---|
| **NuGet package** | `Tesseract` | `IronOcr` |
| **Tesseract engine version** | 4.1.1 (2019, pinned) | Optimized Tesseract 5.x |
| **Tessdata management** | Manual folder + file download | Bundled — zero configuration |
| **Language packs** | Manual `.traineddata` download | NuGet package per language |
| **Languages available** | 100+ (manual) | 125+ (NuGet) |
| **Multi-language simultaneous** | `"eng+fra+deu"` string | `OcrLanguage.French + OcrLanguage.German` |
| **Image preprocessing** | Manual (~180 lines) | Built-in one-line methods |
| **Deskew** | Manual (Hough transform needed) | `input.Deskew()` |
| **DeNoise** | Manual (median filter) | `input.DeNoise()` |
| **Contrast / Binarize** | Manual pixel iteration | `input.Contrast()`, `input.Binarize()` |
| **Deep noise removal** | Not available | `input.DeepCleanBackgroundNoise()` |
| **PDF input** | None — requires external library | Native (scanned, digital, mixed) |
| **Password-protected PDF** | Requires decryption library | `input.LoadPdf(path, Password: "...")` |
| **Multi-page TIFF** | Manual frame iteration | `input.LoadImageFrames()` |
| **Searchable PDF output** | Not supported | `result.SaveAsSearchablePdf()` |
| **Structured result access** | `ResultIterator` loop | `result.Pages`, `.Paragraphs`, `.Words` |
| **Thread safety** | Not thread-safe | Thread-safe single instance |
| **Barcode reading** | Not supported | `ocr.Configuration.ReadBarCodes = true` |
| **Cross-platform** | Native DLLs required per platform | Single NuGet, all platforms |
| **Docker deployment** | apt-get + tessdata COPY steps | No additional steps |
| **Licensing** | Apache 2.0 (free) | Perpetual ($749 Lite / $1,499 Pro / $2,999 Enterprise) |
| **Commercial support** | Community only | Yes (email + priority tiers) |

## Quick Start: Tesseract to IronOCR Migration

### Step 1: Replace NuGet Package

Remove the charlesw Tesseract wrapper:

```bash
dotnet remove package Tesseract
```

Install IronOCR from [NuGet](https://www.nuget.org/packages/IronOcr):

```bash
dotnet add package IronOcr
```

Language packs install as separate packages when needed:

```bash
dotnet add package IronOcr.Languages.French
dotnet add package IronOcr.Languages.German
```

### Step 2: Update Namespaces

Replace the Tesseract namespace with the IronOcr namespace:

```csharp
// Before
using Tesseract;

// After
using IronOcr;
```

### Step 3: Initialize License

Add license initialization once at application startup, before any `IronTesseract` calls:

```csharp
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
```

A free trial runs without a key during development. Production deployments require a valid key from the [licensing page](https://ironsoftware.com/csharp/ocr/licensing/).

## Code Migration Examples

### Tessdata Path Elimination and Engine Initialization

The most immediate change is removing `TesseractEngine` initialization and all the tessdata validation code that surrounds it.

**Tesseract Approach:**
```csharp
// Every class that uses OCR must handle this initialization block
private const string TessDataPath = @"./tessdata";

public string RecognizeInvoiceNumber(string imagePath)
{
    // Check tessdata presence — missing file = silent runtime failure
    foreach (var lang in new[] { "eng" })
    {
        if (!File.Exists(Path.Combine(TessDataPath, $"{lang}.traineddata")))
            throw new FileNotFoundException(
                $"Missing {lang}.traineddata. " +
                "Download from https://github.com/tesseract-ocr/tessdata");
    }

    using var engine = new TesseractEngine(TessDataPath, "eng", EngineMode.Default);

    // Pix is a Leptonica wrapper type — not a standard .NET image
    using var img  = Pix.LoadFromFile(imagePath);
    using var page = engine.Process(img);

    string text = page.GetText();
    float  conf = page.GetMeanConfidence();

    return conf > 0.7f ? text : string.Empty;
}
```

**IronOCR Approach:**
```csharp
using IronOcr;

public string RecognizeInvoiceNumber(string imagePath)
{
    var result = new IronTesseract().Read(imagePath);

    // Confidence property returns 0-100 double
    return result.Confidence > 70 ? result.Text : string.Empty;
}
```

The `FileNotFoundException` guard, the tessdata constant, the `Pix` object, and the three-level nesting are gone. `IronTesseract` is constructed without arguments because language data is embedded. See the [IronTesseract setup guide](https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/) for configuration options when you need non-default behavior, and the [confidence scores guide](https://ironsoftware.com/csharp/ocr/how-to/tesseract-result-confidence/) for the full confidence API.

### Multi-Page TIFF Processing with Preprocessing Pipeline

Multi-frame TIFF files — common in scanned document archives and fax systems — require explicit frame iteration with Tesseract. IronOCR loads all frames in one call and applies the preprocessing pipeline uniformly.

**Tesseract Approach:**
```csharp
using Tesseract;
using System.Drawing;
using System.Drawing.Imaging;

private const string TessDataPath = @"./tessdata";

public static string ExtractFromMultiPageTiff(string tiffPath)
{
    var allText = new System.Text.StringBuilder();

    using var engine = new TesseractEngine(TessDataPath, "eng", EngineMode.Default);
    using var tiffImage = Image.FromFile(tiffPath);

    int frameCount = tiffImage.GetFrameCount(FrameDimension.Page);

    for (int i = 0; i < frameCount; i++)
    {
        tiffImage.SelectActiveFrame(FrameDimension.Page, i);

        // Must save each frame to disk — Pix.LoadFromFile requires a path
        string tempPath = Path.GetTempFileName() + ".png";
        try
        {
            tiffImage.Save(tempPath, ImageFormat.Png);

            using var img  = Pix.LoadFromFile(tempPath);
            using var page = engine.Process(img);
            allText.AppendLine(page.GetText());
        }
        finally
        {
            File.Delete(tempPath); // Uncleaned temp files fill disk on failure
        }
    }

    return allText.ToString();
}
```

**IronOCR Approach:**
```csharp
using IronOcr;

public static string ExtractFromMultiPageTiff(string tiffPath)
{
    using var input = new OcrInput();
    input.LoadImageFrames(tiffPath);  // Loads all frames at once
    input.Deskew();                   // Applied to every frame uniformly
    input.DeNoise();

    var result = new IronTesseract().Read(input);
    return result.Text;
}
```

No frame iteration. No temp file creation. No cleanup logic. The preprocessing pipeline applies to every frame without an additional loop. The [TIFF and GIF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-tiff-gif/) covers multi-frame handling in detail, including selective frame ranges for large archive files.

### Searchable PDF Generation

Converting a scanned PDF into a searchable PDF requires Tesseract to render each page to an image (via an external PDF library), run OCR, then reconstruct a PDF with a text layer — a multi-library, multi-step process. IronOCR handles input, OCR, and output in a single pipeline.

**Tesseract Approach:**
```csharp
// Requires: PdfiumViewer + Tesseract + a PDF writer library (iText, PdfSharp)
// Each library adds its own native dependencies and license considerations

using Tesseract;
// using PdfiumViewer;  // Comment: must add NuGet + deploy native pdfium.dll
// using iText.Kernel.Pdf;  // Comment: AGPL or commercial license required

private const string TessDataPath = @"./tessdata";

public static void CreateSearchablePdf(string inputPdfPath, string outputPdfPath)
{
    // Step 1: Render PDF pages to images (requires PdfiumViewer)
    // Step 2: Run OCR on each image (Tesseract)
    // Step 3: Write text positions back into PDF (requires iText or PDFsharp)
    //
    // Total: ~150 lines across three libraries
    // Native binaries required: tesseract*.dll, leptonica*.dll, pdfium.dll
    // License risk: iText is AGPL unless you purchase a commercial license

    throw new NotImplementedException(
        "Requires PdfiumViewer + Tesseract + a PDF writer. " +
        "No single-package solution exists with this stack.");
}
```

**IronOCR Approach:**
```csharp
using IronOcr;

public static void CreateSearchablePdf(string inputPdfPath, string outputPdfPath)
{
    using var input = new OcrInput();
    input.LoadPdf(inputPdfPath);
    input.Deskew();    // Correct scanned page skew before OCR
    input.DeNoise();   // Remove scanner artifacts

    var result = new IronTesseract().Read(input);
    result.SaveAsSearchablePdf(outputPdfPath);
}
```

One method call produces the searchable PDF with an embedded text layer. No external PDF library, no native pdfium binary, no license entanglement with AGPL dependencies. The [searchable PDF how-to guide](https://ironsoftware.com/csharp/ocr/how-to/searchable-pdf/) documents the output format, and the [PDF OCR example](https://ironsoftware.com/csharp/ocr/examples/csharp-pdf-ocr/) walks through a complete scanned document pipeline. For broader context on what IronOCR can do with PDF input, the [PDF OCR use case page](https://ironsoftware.com/csharp/ocr/use-case/pdf-ocr-csharp/) covers production architecture patterns.

### Structured Data Extraction from Scanned Documents

Tesseract exposes word-level data through `ResultIterator`, which requires a `do/while` loop with manual bounding box extraction. IronOCR exposes a document hierarchy — pages, paragraphs, lines, words — as strongly-typed collections with coordinates already populated.

**Tesseract Approach:**
```csharp
using Tesseract;

private const string TessDataPath = @"./tessdata";

public static void ExtractStructuredData(string imagePath)
{
    using var engine = new TesseractEngine(TessDataPath, "eng", EngineMode.Default);
    using var img    = Pix.LoadFromFile(imagePath);
    using var page   = engine.Process(img);
    using var iter   = page.GetIterator();

    iter.Begin();
    do
    {
        if (iter.IsAtBeginningOf(PageIteratorLevel.Para))
            Console.WriteLine("-- New Paragraph --");

        if (iter.TryGetBoundingBox(PageIteratorLevel.Word, out var bounds))
        {
            string word       = iter.GetText(PageIteratorLevel.Word);
            float  confidence = iter.GetConfidence(PageIteratorLevel.Word);
            Console.WriteLine(
                $"Word: '{word?.Trim()}' " +
                $"at ({bounds.X1},{bounds.Y1})-({bounds.X2},{bounds.Y2}) " +
                $"conf={confidence:P0}");
        }
    }
    while (iter.Next(PageIteratorLevel.Word));
}
```

**IronOCR Approach:**
```csharp
using IronOcr;

public static void ExtractStructuredData(string imagePath)
{
    var result = new IronTesseract().Read(imagePath);

    foreach (var page in result.Pages)
    {
        Console.WriteLine($"Page {page.PageNumber} — confidence: {result.Confidence}%");

        foreach (var paragraph in page.Paragraphs)
        {
            Console.WriteLine($"  Paragraph at ({paragraph.X},{paragraph.Y}):");
            Console.WriteLine($"  {paragraph.Text}");

            foreach (var word in paragraph.Words)
            {
                Console.WriteLine(
                    $"    Word: '{word.Text}' " +
                    $"at ({word.X},{word.Y}) " +
                    $"size {word.Width}x{word.Height} " +
                    $"conf={word.Confidence:P0}");
            }
        }
    }
}
```

The `ResultIterator` loop disappears entirely. The document hierarchy is a set of enumerable collections — no iterator state, no manual level tracking, no bounding box extraction by output parameter. Each word object carries its own coordinates and confidence. The [read results guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) documents every level of the hierarchy, and the [OcrResult API reference](https://ironsoftware.com/csharp/ocr/object-reference/api/IronOcr.OcrResult.html) lists all available properties.

### Multi-Language OCR Without Tessdata File Management

Adding a language to a Tesseract application means downloading a `.traineddata` file, placing it in the tessdata folder, updating every deployment manifest that includes that folder, and modifying the engine initialization string. With IronOCR, it is a single NuGet package reference.

**Tesseract Approach:**
```csharp
using Tesseract;

private const string TessDataPath = @"./tessdata";

public static string ExtractFromEuropeanDocument(string imagePath)
{
    // Before this call works, these files must exist:
    // ./tessdata/eng.traineddata  (~15 MB, from GitHub)
    // ./tessdata/fra.traineddata  (~15 MB, from GitHub)
    // ./tessdata/deu.traineddata  (~15 MB, from GitHub)
    // ./tessdata/spa.traineddata  (~15 MB, from GitHub)
    // Total: ~60 MB to download, version-match, and deploy to every environment

    foreach (var lang in new[] { "eng", "fra", "deu", "spa" })
    {
        if (!File.Exists(Path.Combine(TessDataPath, $"{lang}.traineddata")))
            throw new FileNotFoundException(
                $"Download {lang}.traineddata from " +
                "https://github.com/tesseract-ocr/tessdata " +
                $"and place in {TessDataPath}");
    }

    // Language string is a concatenation — order affects recognition priority
    using var engine = new TesseractEngine(TessDataPath, "eng+fra+deu+spa", EngineMode.Default);
    using var img    = Pix.LoadFromFile(imagePath);
    using var page   = engine.Process(img);
    return page.GetText();
}
```

**IronOCR Approach:**
```csharp
// Install language packs once per project:
// dotnet add package IronOcr.Languages.French
// dotnet add package IronOcr.Languages.German
// dotnet add package IronOcr.Languages.Spanish
using IronOcr;

public static string ExtractFromEuropeanDocument(string imagePath)
{
    var ocr = new IronTesseract();
    ocr.Language = OcrLanguage.English;
    ocr.AddSecondaryLanguage(OcrLanguage.French);
    ocr.AddSecondaryLanguage(OcrLanguage.German);
    ocr.AddSecondaryLanguage(OcrLanguage.Spanish);

    return ocr.Read(imagePath).Text;
}
```

The tessdata folder, the file-existence loop, the path concatenation string, and the deployment manifest updates are all replaced by `PackageReference` lines in the `.csproj`. Adding a language to Docker means one additional `dotnet add package` — not a Dockerfile `COPY` step. The [multiple languages guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-multiple-languages/) covers the full 125+ language catalog and CJK character sets, and the [languages index](https://ironsoftware.com/csharp/ocr/languages/) lists every available language pack.

## Tesseract API to IronOCR Mapping Reference

| Tesseract (charlesw) | IronOCR |
|---|---|
| `new TesseractEngine(tessDataPath, "eng", EngineMode.Default)` | `new IronTesseract()` |
| `Pix.LoadFromFile(path)` | `input.LoadImage(path)` or `ocr.Read(path)` |
| `Pix.LoadFromMemory(bytes)` | `input.LoadImage(bytes)` |
| `engine.Process(img)` | `ocr.Read(input)` |
| `page.GetText()` | `result.Text` |
| `page.GetMeanConfidence()` | `result.Confidence` |
| `page.GetHOCRText(0)` | `result.SaveAsHocrFile(path)` |
| `engine.Process(img, tessRect)` | `input.LoadImage(path, new CropRectangle(x, y, w, h))` |
| `page.GetIterator()` | `result.Pages` / `result.Paragraphs` / `result.Words` |
| `iter.GetText(PageIteratorLevel.Word)` | `result.Words[i].Text` |
| `iter.GetConfidence(PageIteratorLevel.Word)` | `result.Words[i].Confidence` |
| `iter.TryGetBoundingBox(PageIteratorLevel.Word, out bounds)` | `word.X`, `word.Y`, `word.Width`, `word.Height` |
| `"eng+fra+deu"` language string | `ocr.AddSecondaryLanguage(OcrLanguage.French)` |
| Tessdata folder + `.traineddata` files | NuGet language package (`IronOcr.Languages.French`) |
| N/A — requires PdfiumViewer or similar | `input.LoadPdf(path)` |
| N/A — requires decryption library | `input.LoadPdf(path, Password: "secret")` |
| N/A — requires iText or PDFsharp | `result.SaveAsSearchablePdf(outputPath)` |
| N/A — manual System.Drawing pipeline | `input.Deskew()`, `input.DeNoise()`, `input.Binarize()` |
| N/A — per-thread engine in `Parallel.ForEach` | Single `IronTesseract` shared across all threads |
| N/A — not supported | `ocr.Configuration.ReadBarCodes = true` |

## Common Migration Issues and Solutions

### Issue 1: Tessdata Path Reference Remains After Migration

**Tesseract:** `TessDataPath` constants, `Directory.Exists(TessDataPath)` guards, and `File.Exists(Path.Combine(TessDataPath, lang + ".traineddata"))` checks appear throughout the codebase and in project files as `<Content Include="tessdata\**">` build items.

**Solution:** Search for all occurrences and remove them along with the tessdata folder itself:

```bash
# Find all tessdata references in source
grep -r "TessDataPath\|tessdata\|traineddata" --include="*.cs" .
grep -r "tessdata" --include="*.csproj" .
```

After removing the path constants and file guards, delete the tessdata folder from the project. Remove any `<Content Include="tessdata\**" CopyToOutputDirectory="..." />` lines from `.csproj` files. Dockerfile `COPY ./tessdata` lines and `ENV TESSDATA_PREFIX` environment variable declarations are also safe to remove.

### Issue 2: Pix Object Type Cannot Be Resolved

**Tesseract:** `Pix` is a Leptonica image wrapper type from the `Tesseract` namespace. References appear in variable declarations (`using var img = Pix.LoadFromFile(...)`), method signatures that accept `Pix` parameters, and any code that calls `Pix.LoadFromMemory()` or `Pix.LoadFromBitmap()`.

**Solution:** Replace `Pix.LoadFromFile(path)` with `input.LoadImage(path)` on an `OcrInput` instance. Replace `Pix.LoadFromMemory(bytes)` with `input.LoadImage(bytes)`. The `OcrInput` class accepts file paths, byte arrays, streams, and `System.Drawing.Bitmap` objects directly. No conversion to an intermediate wrapper type is required. See the [image input guide](https://ironsoftware.com/csharp/ocr/how-to/input-images/) and [stream input guide](https://ironsoftware.com/csharp/ocr/how-to/input-streams/) for the full set of accepted input types.

### Issue 3: ResultIterator Loop Pattern Has No Direct Equivalent

**Tesseract:** Code that iterates `ResultIterator` with `iter.Begin()`, `iter.Next(PageIteratorLevel.Word)`, and `iter.TryGetBoundingBox()` is the standard pattern for word-level or character-level extraction. This pattern requires tracking iterator state and level transitions manually.

**Solution:** Replace the iterator loop with LINQ over `result.Words`, `result.Pages`, or the appropriate collection level:

```csharp
// Before: iterator loop
using var iter = page.GetIterator();
iter.Begin();
do
{
    if (iter.TryGetBoundingBox(PageIteratorLevel.Word, out var bounds))
    {
        string text = iter.GetText(PageIteratorLevel.Word);
        // process text and bounds
    }
}
while (iter.Next(PageIteratorLevel.Word));

// After: enumerable collection
var result = new IronTesseract().Read(imagePath);
foreach (var word in result.Words)
{
    // word.Text, word.X, word.Y, word.Width, word.Height, word.Confidence
}
```

For paragraph-level access — which has no clean analog in the Tesseract iterator — use `result.Pages[i].Paragraphs`. The [read results guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) documents all available levels.

### Issue 4: PDF Library Code Must Be Removed Entirely

**Tesseract:** Any code that converts PDF pages to images before passing them to Tesseract — PdfiumViewer `document.Render()` loops, PDFtoImage `Conversion.ToImage()` calls, Docnet.Core `GetPageReader()` patterns, or GhostScript process invocations — exists only to work around Tesseract's inability to open PDFs. These classes, loops, temp file patterns, and native binary deployments are all scaffolding around the real requirement.

**Solution:** Delete the PDF rendering code entirely. Replace the entire render-then-OCR block with `input.LoadPdf(path)`:

```csharp
// Before: ~50-150 lines of PdfiumViewer + Tesseract + temp file management
// After:
using var input = new OcrInput();
input.LoadPdf("document.pdf");
input.Deskew();
input.DeNoise();
var result = new IronTesseract().Read(input);
```

Remove PdfiumViewer, PDFtoImage, and Docnet.Core package references from the `.csproj`. Remove native binary deployments (`pdfium.dll`, GhostScript executables) from build scripts and Dockerfiles. The [PDF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-pdfs/) covers page range selection and password-protected PDFs.

### Issue 5: Parallel Processing Engine-Per-Thread Pattern

**Tesseract:** The standard pattern for safe parallel OCR creates a new `TesseractEngine` inside the `Parallel.ForEach` body because a single engine is not thread-safe. This loads the full language model per thread.

**Solution:** Create `IronTesseract` once before the loop and reference it inside:

```csharp
// Before: engine per thread, 40-100 MB per language model, times thread count
Parallel.ForEach(files, file =>
{
    using var engine = new TesseractEngine(TessDataPath, "eng", EngineMode.Default);
    using var img    = Pix.LoadFromFile(file);
    using var page   = engine.Process(img);
    results[file] = page.GetText();
});

// After: single engine, thread-safe, shared pool
var ocr = new IronTesseract();
Parallel.ForEach(files, file =>
{
    var result = ocr.Read(file);
    results[file] = result.Text;
});
```

The thread-safety change also eliminates the `using` disposal pattern from inside the loop body, which was necessary only to ensure each per-thread engine was released promptly.

### Issue 6: EngineMode Enum Has No Direct Mapping

**Tesseract:** `EngineMode.Default`, `EngineMode.TesseractOnly`, and `EngineMode.LstmOnly` appear in `TesseractEngine` constructors to select whether Tesseract uses the legacy engine, LSTM, or both. The charlesw wrapper exposes these modes because Tesseract 4.x retained both engines.

**Solution:** IronOCR uses the Tesseract 5 LSTM engine exclusively, which is the high-accuracy configuration. No `EngineMode` parameter exists because there is no legacy engine to fall back to. Remove the `EngineMode` argument when translating the constructor call. For throughput-versus-accuracy tuning, use `ocr.Configuration.PageSegmentationMode` and consult the [speed optimization guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-fast-configuration/).

## Tesseract Migration Checklist

### Pre-Migration

Audit the codebase for all Tesseract and tessdata references:

```bash
# Find all using directives for the Tesseract namespace
grep -rn "using Tesseract" --include="*.cs" .

# Find TesseractEngine constructors
grep -rn "TesseractEngine\|TessDataPath\|tessdata" --include="*.cs" .

# Find Pix object usage
grep -rn "Pix\." --include="*.cs" .

# Find ResultIterator usage
grep -rn "GetIterator\|ResultIterator\|PageIteratorLevel" --include="*.cs" .

# Find PDF rendering libraries added for Tesseract
grep -rn "PdfiumViewer\|PDFtoImage\|Docnet\|GhostScript" --include="*.cs" .

# Find tessdata references in project files
grep -rn "tessdata\|traineddata" --include="*.csproj" .

# Find tessdata references in Dockerfiles
grep -rn "tessdata\|TESSDATA_PREFIX\|libtesseract" Dockerfile* .
```

Inventory results to estimate migration scope:
- Count files with `using Tesseract` to determine how many classes require changes
- Identify which PDF rendering library is in use (PdfiumViewer, PDFtoImage, Docnet.Core, GhostScript)
- Note which languages are referenced in `TesseractEngine` constructor strings to determine which IronOCR language NuGet packages to add

### Code Migration

1. Remove `Tesseract` NuGet package reference from all `.csproj` files
2. Remove PDF rendering library NuGet references added solely for Tesseract support (PdfiumViewer, PDFtoImage, Docnet.Core)
3. Install `IronOcr` NuGet package
4. Install required language NuGet packages (`IronOcr.Languages.French`, etc.)
5. Add `IronOcr.License.LicenseKey = "YOUR-KEY";` at application startup
6. Replace `using Tesseract;` with `using IronOcr;` in all affected files
7. Remove `TessDataPath` constants and all `Directory.Exists` / `File.Exists` tessdata guards
8. Replace `new TesseractEngine(...)` with `new IronTesseract()`
9. Replace `Pix.LoadFromFile(path)` with `input.LoadImage(path)` on an `OcrInput` instance
10. Replace `Pix.LoadFromMemory(bytes)` with `input.LoadImage(bytes)`
11. Replace `engine.Process(img)` with `ocr.Read(input)`
12. Replace `page.GetText()` with `result.Text`
13. Replace `page.GetMeanConfidence()` with `result.Confidence`
14. Replace `ResultIterator` loops with enumeration over `result.Words` or `result.Pages[i].Paragraphs`
15. Replace PDF rendering loops with `input.LoadPdf(path)` — delete the rendering library code entirely
16. Replace `"eng+fra+deu"` language strings with `ocr.AddSecondaryLanguage(OcrLanguage.X)` calls
17. Delete the tessdata folder and its build `<Content Include="...">` project items
18. Remove native binary deployment steps from build scripts and Dockerfiles (tessdata COPY, TESSDATA_PREFIX ENV, apt-get libtesseract-dev)

### Post-Migration

- Verify basic text extraction on the same sample images used during development with the Tesseract wrapper
- Confirm confidence scores are reasonable (70%+ for clean documents, 85%+ for high-quality scans)
- Test multi-page TIFF input produces the correct number of pages in `result.Pages`
- Verify PDF input reads scanned PDFs without requiring PdfiumViewer or any external library
- Test password-protected PDF reading with `input.LoadPdf(path, Password: "...")` against a known encrypted file
- Confirm searchable PDF output opens in Adobe Reader and supports text search
- Test parallel processing: create one `IronTesseract` instance before a `Parallel.ForEach` loop and confirm no thread-safety exceptions
- Verify each language pack produces correct output for the target language document set
- Run Docker build without `COPY tessdata` and `apt-get libtesseract-dev` — confirm the container starts and processes documents
- Confirm the tessdata folder and native DLL files are absent from the published output directory
- Check that no `TesseractException` or `System.DllNotFoundException` appears in logs after removing native binary references

## Key Benefits of Migrating to IronOCR

**Deployment shrinks to a single package.** The tessdata folder, platform-specific native libraries (`tesseract50.dll`, `leptonica-1.82.0.dll`, `libtesseract.so.5`), and any PDF rendering native binaries are gone from the deployment artifact. Adding a new environment — a Linux container, an AWS Lambda function, a macOS developer machine — requires no platform-specific setup steps. The [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/) and [Linux deployment guide](https://ironsoftware.com/csharp/ocr/get-started/linux/) confirm the process: install the package, add the license key, run. No apt-get, no COPY, no environment variables.

**Language additions take seconds, not minutes.** Adding Spanish OCR support goes from "download `spa.traineddata`, place in tessdata folder, update the deployment manifest, verify path in the engine constructor" to `dotnet add package IronOcr.Languages.Spanish` and `ocr.AddSecondaryLanguage(OcrLanguage.Spanish)`. The same two steps work on every platform. Teams supporting 10+ languages — common in multinational document processing workflows — see this compress from hours of ongoing maintenance to minutes of one-time setup. Browse the full catalog at the [languages index](https://ironsoftware.com/csharp/ocr/languages/).

**PDF workflows need no external libraries.** The requirement to deploy and maintain PdfiumViewer native binaries, manage pdfium.dll bitness for 32/64-bit environments, handle AGPL license considerations from GhostScript, and write page-by-page render loops disappears. `input.LoadPdf()` reads scanned PDFs, digital PDFs, mixed content PDFs, and password-protected PDFs natively. `result.SaveAsSearchablePdf()` produces a searchable output without involving any secondary library. The full round-trip — load a scanned PDF, deskew and denoise, OCR, save searchable output — is under 10 lines of code. See the [searchable PDFs blog post](https://ironsoftware.com/csharp/ocr/blog/using-ironocr/searchable-pdfs-with-ironocr/) for production pipeline patterns.

**Preprocessing is built in, not built by you.** The approximately 180 lines of manual preprocessing code — grayscale color matrix, pixel-iteration contrast enhancement, median-filter noise removal, Hough-transform deskew, DPI scaling — become a sequence of one-line method calls: `input.Deskew()`, `input.DeNoise()`, `input.Contrast()`, `input.Binarize()`. For most real-world documents, the default read applies intelligent automatic preprocessing with no explicit filter calls at all. The [image quality correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/) and [image filters tutorial](https://ironsoftware.com/csharp/ocr/tutorials/c-sharp-ocr-image-filters/) cover the full filter catalog.

**Tesseract 5 accuracy is available immediately.** The charlesw wrapper is pinned to Tesseract 4.1.1. IronOCR ships an optimized Tesseract 5 LSTM engine with no action required on your part. Teams that have measured accuracy degradation on difficult document types — low-DPI scans, faxes, handprinted forms — gain the Tesseract 5 improvements the moment they switch packages. The accuracy delta is most noticeable on documents where LSTM recognition outperforms the legacy engine, which is the majority of real-world OCR workloads.

**Commercial support replaces community troubleshooting.** The charlesw wrapper is a community-maintained open-source project with no guaranteed response times and no SLA. IronOCR provides email support, priority support on higher tiers, and a commercially maintained codebase with regular .NET compatibility updates. For teams with production SLAs on document processing pipelines, that support model matters. The [IronOCR product page](https://ironsoftware.com/csharp/ocr/) and [documentation hub](https://ironsoftware.com/csharp/ocr/docs/) cover the full feature set and deployment options.
