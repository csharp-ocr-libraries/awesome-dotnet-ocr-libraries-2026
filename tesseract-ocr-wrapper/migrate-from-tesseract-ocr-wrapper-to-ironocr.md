# Migrating from Tesseract OCR Wrapper to IronOCR

This guide is for .NET developers who are currently using the `TesseractOCR` NuGet package and need a clear, step-by-step path to IronOCR. It covers the specific gaps that drive migration — incomplete API coverage and inconsistent error reporting — and provides before-and-after code for the scenarios where those gaps cause the most friction in production applications.

## Why Migrate from Tesseract OCR Wrapper

The `TesseractOCR` package (published by community developer Oachkatzlschwoaf) solves the basic problem of exposing the Tesseract engine as a managed .NET API. For proof-of-concept work, it is adequate. For production systems that need reliable error signals, multiple output formats, and a complete API surface, the wrapper's design choices become blockers.

**Incomplete API Surface.** The wrapper exposes text extraction and an aggregate confidence float. Word-level data, bounding boxes, line-level traversal, and paragraph-level grouping are absent from the public API. Applications that need to know where on the page a value appears — invoice field extraction, redaction pipelines, document analysis — have no path forward within the wrapper. Adding a second library to parse hOCR from raw Tesseract adds integration work that compounds over time.

**Silent Failure on Bad Input.** When the Tesseract engine encounters a degraded image, an unsupported format, or an internal processing error, the wrapper returns an empty string from `page.GetText()` rather than throwing a catchable managed exception. The calling code receives an empty result that is indistinguishable from a legitimate blank page. Automated pipelines that process thousands of documents per day can silently drop data for months before an audit reveals the problem.

**No Searchable PDF Output.** The wrapper produces plain text. Converting that text into a searchable PDF — a standard compliance requirement in legal, healthcare, and financial services — requires a separate PDF library, manual text layer assembly, and page coordinate calculations. That integration runs to 150-300 lines and must be maintained independently.

**No Native PDF Input.** Every codebase using the wrapper that processes PDFs contains a PDF-to-image rasterization layer: typically PdfiumViewer, Ghostscript, or PDFsharp calling a rendering API to convert each PDF page to a bitmap before feeding it to the engine. That dependency adds complexity, introduces a quality-loss step from the intermediate rasterization, and requires its own deployment configuration.

**No Multi-Format Input Handling.** The wrapper's primary input path is a file path string passed to `Pix.Image.LoadFromFile`. Stream-based and byte-array-based input — common in ASP.NET applications receiving uploaded files — require writing the bytes to a temporary file first, then passing that path to the engine, then cleaning up the temporary file. That pattern is error-prone and unnecessary.

**Engine Configuration Rigidity.** The wrapper exposes a subset of Tesseract's engine configuration options. Page segmentation mode is accessible, but configuration for resolution normalization, output type, and recognition parameters requires working at a lower abstraction level than the wrapper provides.

### The Fundamental Problem

The wrapper's error contract is undefined. A call that appears to succeed can silently discard the result:

```csharp
// TesseractOCR: no way to tell failure from "no text on this page"
using var engine = new Engine(@"./tessdata", Language.English);
using var img = Pix.Image.LoadFromFile(imagePath);
using var page = engine.Process(img);

var text = page.Text; // returns "" on engine failure — same as blank page
// Caller cannot distinguish OCR failure from legitimate empty result
```

IronOCR throws on engine failure and exposes a numeric confidence score on every successful result:

```csharp
// IronOCR: failures throw, low-confidence results are detectable
var result = new IronTesseract().Read(imagePath);
// result.Confidence is 0-100; a score below 10 signals a processing problem
// An engine failure throws IronOcrException — never returns a silent empty string
Console.WriteLine($"Text: {result.Text}, Confidence: {result.Confidence}%");
```

## IronOCR vs Tesseract OCR Wrapper: Feature Comparison

The table below covers the capabilities that matter most for production document processing applications.

| Feature | Tesseract OCR Wrapper | IronOCR |
|---|---|---|
| NuGet package | `TesseractOCR` + manual tessdata + native binary | `IronOcr` (all dependencies bundled) |
| License | Apache 2.0 (free) | Commercial ($749–$2,999 perpetual) |
| Engine version | Depends on bundled native binary | Optimized Tesseract 5 (bundled) |
| Plain text output | Yes (`page.Text`) | Yes (`result.Text`) |
| Searchable PDF output | No | Yes (`result.SaveAsSearchablePdf()`) |
| hOCR export | No | Yes (`result.SaveAsHocrFile()`) |
| Structured word/line/paragraph data | No | Yes (with bounding box coordinates) |
| Per-word confidence scores | No | Yes (`word.Confidence`) |
| Aggregate confidence | Yes (`page.GetMeanConfidence()`, float 0–1) | Yes (`result.Confidence`, double 0–100) |
| Consistent error handling | No (empty string on failure) | Yes (managed exceptions throughout) |
| Native PDF input | No | Yes |
| Password-protected PDF input | No | Yes |
| Multi-page TIFF input | Limited | Yes |
| Stream and byte-array input | No direct support | Yes (`input.LoadImage(stream)`, `input.LoadImage(bytes)`) |
| Automatic deskew | No | Yes |
| Automatic denoise | No | Yes |
| Automatic contrast enhancement | No | Yes |
| Binarization | No | Yes |
| Barcode reading during OCR | No | Yes (`ocr.Configuration.ReadBarCodes = true`) |
| Region-based OCR | No exposed API | Yes (`CropRectangle`) |
| Thread safety | Limited | Full (one `IronTesseract` instance per thread) |
| Cross-platform deployment | Requires native binary configuration | Windows, Linux, macOS, Docker, Azure, AWS |
| .NET version support | Varies with wrapper version | .NET Framework 4.6.2+, .NET Core, .NET 5/6/7/8/9 |
| Commercial support | None | Yes (email, priority on higher tiers) |

## Quick Start: Tesseract OCR Wrapper to IronOCR Migration

### Step 1: Replace NuGet Package

Remove the existing package:

```bash
dotnet remove package TesseractOCR
```

Install IronOCR from [NuGet](https://www.nuget.org/packages/IronOcr):

```bash
dotnet add package IronOcr
```

If your project uses multiple languages, install the relevant language packs:

```bash
dotnet add package IronOcr.Languages.French
dotnet add package IronOcr.Languages.German
```

### Step 2: Update Namespaces

Replace the old namespace references with the IronOCR namespace:

```csharp
// Before (Tesseract OCR Wrapper)
using TesseractOCR;
using TesseractOCR.Enums;

// After (IronOCR)
using IronOcr;
```

### Step 3: Initialize License

Add the license key call once at application startup, before any OCR operations run:

```csharp
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
```

A free trial key is available from the [IronOCR licensing page](https://ironsoftware.com/csharp/ocr/licensing/) and allows full functionality during evaluation.

## Code Migration Examples

### Replacing Silent Failures with Reliable Error Handling

The wrapper's error behavior is the migration trigger most teams encounter first. An automated pipeline runs for weeks, then an audit reveals that a percentage of records contain no data — not because the documents were blank, but because the engine silently failed on certain image conditions.

**Tesseract OCR Wrapper Approach:**

```csharp
using TesseractOCR;

public class DocumentProcessor
{
    private readonly string _tessDataPath = @"./tessdata";

    public string ProcessDocument(string imagePath)
    {
        using var engine = new Engine(_tessDataPath, Language.English);
        using var img = Pix.Image.LoadFromFile(imagePath);
        using var page = engine.Process(img);

        // Empty string on engine failure — indistinguishable from blank page
        // No exception thrown, no confidence signal, no recovery path
        var text = page.Text;

        // Caller cannot tell if this is "" because:
        // - The document is genuinely blank
        // - The image format was not supported
        // - The engine encountered an internal error
        // - The tessdata was corrupted or version-mismatched
        return text;
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public class DocumentProcessor
{
    public string ProcessDocument(string imagePath)
    {
        try
        {
            var result = new IronTesseract().Read(imagePath);

            // Confidence below threshold means the result is unreliable
            if (result.Confidence < 15)
            {
                // Route to human review queue — do not silently write empty data
                throw new InvalidOperationException(
                    $"OCR confidence too low ({result.Confidence:F1}%) for: {imagePath}");
            }

            return result.Text;
        }
        catch (IronOcrException ex)
        {
            // Engine failures are typed exceptions — never silent empty strings
            // Log and rethrow with context so the pipeline can flag the document
            throw new ApplicationException(
                $"OCR engine failure processing '{imagePath}': {ex.Message}", ex);
        }
    }
}
```

Every failure mode surfaces as a catchable, typed exception. Low-quality results expose their confidence score so the calling code can decide whether to retry with preprocessing, route to manual review, or reject the input. No silent data loss.

For the full confidence scoring API, see the [confidence scores how-to guide](https://ironsoftware.com/csharp/ocr/how-to/tesseract-result-confidence/).

### Expanding Output from Plain Text to a Document Archive Pipeline

A common requirement in document management is converting scanned archives — paper contracts, invoices, fax records — into searchable PDFs that document management systems can index. The wrapper produces plain text and nothing else. Building a searchable PDF from that output requires a PDF library, manual text overlay, per-page coordinate calculations, and font metric handling.

**Tesseract OCR Wrapper Approach:**

```csharp
using TesseractOCR;
// Also requires: a PDF library (PDFsharp, iText, or similar)
// Also requires: a PDF rasterizer (PdfiumViewer or Ghostscript) to convert input PDFs to images

public class ArchivePipeline
{
    private readonly string _tessDataPath = @"./tessdata";

    public string ExtractText(string imagePath)
    {
        using var engine = new Engine(_tessDataPath, Language.English);
        using var img = Pix.Image.LoadFromFile(imagePath);
        using var page = engine.Process(img);

        return page.Text; // Plain text only — searchable PDF requires a separate pipeline
    }

    // To create a searchable PDF from this text, you would need:
    // 1. Load the original image as a PDF page background
    // 2. Map character positions back to image coordinates
    // 3. Overlay an invisible text layer using a PDF library
    // 4. Handle multi-page documents with per-page iteration
    // That is approximately 150-300 lines of additional code
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public class ArchivePipeline
{
    // Single method handles the full document archive pipeline
    public void ProcessArchive(string[] inputPaths, string outputDirectory)
    {
        var ocr = new IronTesseract();

        foreach (var inputPath in inputPaths)
        {
            var result = ocr.Read(inputPath);

            // Plain text for full-text search indexing
            var textPath = Path.Combine(outputDirectory,
                Path.GetFileNameWithoutExtension(inputPath) + ".txt");
            File.WriteAllText(textPath, result.Text);

            // Searchable PDF — invisible text layer aligned to original scan
            var pdfPath = Path.Combine(outputDirectory,
                Path.GetFileNameWithoutExtension(inputPath) + "-searchable.pdf");
            result.SaveAsSearchablePdf(pdfPath);
        }
    }

    // Input can be scanned image files or existing PDFs — same API
    public void ProcessScannedPdf(string scannedPdfPath, string outputPath)
    {
        var result = new IronTesseract().Read(scannedPdfPath);
        result.SaveAsSearchablePdf(outputPath);
    }
}
```

The same `Read()` call accepts both image files and PDF documents. The `SaveAsSearchablePdf()` call produces a standard, indexable PDF file with a correctly positioned invisible text layer. No PDF library dependency, no coordinate calculation, no text overlay assembly.

The [searchable PDF output guide](https://ironsoftware.com/csharp/ocr/how-to/searchable-pdf/) and the [searchable PDF example](https://ironsoftware.com/csharp/ocr/examples/make-pdf-searchable/) cover multi-page and batch scenarios.

### Simplifying Engine Configuration for Batch Processing

The wrapper requires a new `Engine` instance per OCR call, and that instance takes a tessdata filesystem path as a required constructor argument. In a batch processing scenario processing thousands of documents, this means resolving and validating the tessdata path at every instantiation — and the overhead of engine initialization at each call site.

**Tesseract OCR Wrapper Approach:**

```csharp
using TesseractOCR;

public class BatchOcrService
{
    // tessdata path must be configured correctly in every environment
    private readonly string _tessDataPath;

    public BatchOcrService(string tessDataPath)
    {
        // Path validation deferred to runtime — no early error on misconfiguration
        _tessDataPath = tessDataPath;
    }

    public IEnumerable<string> ProcessBatch(IEnumerable<string> imagePaths)
    {
        var results = new List<string>();

        foreach (var path in imagePaths)
        {
            // New engine created per document — tessdata path re-resolved each time
            using var engine = new Engine(_tessDataPath, Language.English);
            using var img = Pix.Image.LoadFromFile(path);
            using var page = engine.Process(img);

            results.Add(page.Text);
        }

        return results;
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public class BatchOcrService
{
    // One IronTesseract instance for the lifetime of the service
    // Thread-safe — can be registered as a singleton in DI
    private readonly IronTesseract _ocr;

    public BatchOcrService()
    {
        _ocr = new IronTesseract();
        // Optional: tune for batch throughput
        _ocr.Configuration.TesseractVersion = TesseractVersion.Tesseract5;
    }

    public IEnumerable<string> ProcessBatch(IEnumerable<string> imagePaths)
    {
        // Reuse the initialized engine — no tessdata path re-resolution per call
        return imagePaths.Select(path => _ocr.Read(path).Text).ToList();
    }

    // Parallel batch processing — IronTesseract is thread-safe with separate instances
    public IEnumerable<string> ProcessBatchParallel(string[] imagePaths)
    {
        var results = new string[imagePaths.Length];

        Parallel.For(0, imagePaths.Length, i =>
        {
            // Separate instance per thread — thread-safe by design
            var ocr = new IronTesseract();
            results[i] = ocr.Read(imagePaths[i]).Text;
        });

        return results;
    }
}
```

Engine initialization carries startup overhead. Reusing the `IronTesseract` instance across sequential calls eliminates that overhead. For parallel workloads, the pattern is one instance per thread — each instance is independently initialized and safe to use concurrently. No locks, no shared state.

See the [multithreading example](https://ironsoftware.com/csharp/ocr/examples/csharp-tesseract-multithreading-for-speed/) for a complete parallel batch processing implementation.

### Handling Multi-Format Input Without Temporary Files

ASP.NET applications receiving uploaded files have the document as a stream or byte array. The wrapper's primary input path is a filesystem path — meaning the application must write the uploaded bytes to a temporary file, pass that path to the engine, and then delete the temporary file. That pattern is fragile and adds I/O overhead for every request.

**Tesseract OCR Wrapper Approach:**

```csharp
using TesseractOCR;

public class UploadOcrController
{
    private readonly string _tessDataPath = @"./tessdata";

    public async Task<string> ProcessUpload(Stream uploadStream)
    {
        // Must write to temp file — no direct stream input path in the wrapper
        var tempPath = Path.GetTempFileName();
        try
        {
            using (var fileStream = File.Create(tempPath))
            {
                await uploadStream.CopyToAsync(fileStream);
            }

            using var engine = new Engine(_tessDataPath, Language.English);
            using var img = Pix.Image.LoadFromFile(tempPath); // file path required
            using var page = engine.Process(img);

            return page.Text;
        }
        finally
        {
            // Cleanup — if this throws, temp file leaks
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public class UploadOcrController
{
    public string ProcessUpload(Stream uploadStream)
    {
        // Direct stream input — no temporary file, no I/O overhead, no cleanup
        using var input = new OcrInput();
        input.LoadImage(uploadStream);
        return new IronTesseract().Read(input).Text;
    }

    public string ProcessUploadBytes(byte[] imageBytes)
    {
        // Byte array input — works directly from memory
        using var input = new OcrInput();
        input.LoadImage(imageBytes);
        return new IronTesseract().Read(input).Text;
    }

    public string ProcessMultiPageTiff(Stream tiffStream)
    {
        // Multi-frame TIFF — all frames processed in one call
        using var input = new OcrInput();
        input.LoadImageFrames(tiffStream);
        return new IronTesseract().Read(input).Text;
    }
}
```

`OcrInput` accepts streams, byte arrays, file paths, and multi-frame TIFFs through a unified loading API. There is no temporary file, no I/O overhead, and no cleanup logic. The `using` block on `OcrInput` handles resource disposal correctly.

The [stream input guide](https://ironsoftware.com/csharp/ocr/how-to/input-streams/) and the [image input guide](https://ironsoftware.com/csharp/ocr/how-to/input-images/) cover all supported input sources including memory-mapped files and network streams.

### Extracting Structured Data for Document Analysis

The wrapper returns the full document as a single string from `page.Text`. Applications that need to identify specific fields — invoice amounts, dates, line items — must parse that string with heuristics or regular expressions without any spatial context. There is no API for accessing individual words with their positions on the page.

**Tesseract OCR Wrapper Approach:**

```csharp
using TesseractOCR;
using System.Text.RegularExpressions;

public class InvoiceFieldExtractor
{
    private readonly string _tessDataPath = @"./tessdata";

    public Dictionary<string, string> ExtractFields(string imagePath)
    {
        using var engine = new Engine(_tessDataPath, Language.English);
        using var img = Pix.Image.LoadFromFile(imagePath);
        using var page = engine.Process(img);

        var fullText = page.Text;

        // Must parse the full string — no spatial context available
        // Pattern matching is fragile across different invoice layouts
        var fields = new Dictionary<string, string>();

        var totalMatch = Regex.Match(fullText, @"Total[:\s]+\$?([\d,]+\.\d{2})");
        if (totalMatch.Success)
            fields["Total"] = totalMatch.Groups[1].Value;

        var dateMatch = Regex.Match(fullText, @"Date[:\s]+(\d{1,2}/\d{1,2}/\d{4})");
        if (dateMatch.Success)
            fields["Date"] = dateMatch.Groups[1].Value;

        return fields;
        // No spatial fallback when text patterns fail — the data is lost
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public class InvoiceFieldExtractor
{
    public Dictionary<string, string> ExtractFields(string imagePath)
    {
        var result = new IronTesseract().Read(imagePath);
        var fields = new Dictionary<string, string>();

        // Traverse structured result — words carry position and confidence
        foreach (var page in result.Pages)
        {
            foreach (var paragraph in page.Paragraphs)
            {
                var paraText = paragraph.Text.Trim();

                // Spatial proximity: find words near known label positions
                if (paraText.StartsWith("Total", StringComparison.OrdinalIgnoreCase))
                {
                    fields["Total"] = paraText;
                    // paragraph.X, paragraph.Y give position for layout validation
                }

                if (paraText.StartsWith("Invoice Date", StringComparison.OrdinalIgnoreCase))
                {
                    fields["Date"] = paraText;
                }
            }
        }

        // Flag low-confidence extractions for review rather than silently accepting them
        var lowConfidenceWords = result.Pages
            .SelectMany(p => p.Paragraphs)
            .SelectMany(para => para.Words)
            .Where(w => w.Confidence < 50)
            .Select(w => w.Text)
            .ToList();

        if (lowConfidenceWords.Any())
            fields["_LowConfidenceWarning"] = string.Join(", ", lowConfidenceWords);

        return fields;
    }
}
```

The `result.Pages[].Paragraphs[].Words[]` hierarchy exposes position (`X`, `Y`, `Width`, `Height`) and confidence for every word. Extraction logic that previously relied on fragile string parsing can use spatial proximity — knowing that a value appears to the right of, or immediately below, a known label on the page.

The [read results guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) documents the full hierarchy with code examples for common extraction patterns.

## Tesseract OCR Wrapper API to IronOCR Mapping Reference

| Tesseract OCR Wrapper | IronOCR Equivalent |
|---|---|
| `new Engine(tessDataPath, Language.English)` | `new IronTesseract()` (no path needed) |
| `new Engine(tessDataPath, "eng+fra")` | `ocr.Language = OcrLanguage.English; ocr.AddSecondaryLanguage(OcrLanguage.French)` |
| `Pix.Image.LoadFromFile(imagePath)` | `input.LoadImage(imagePath)` |
| `engine.Process(img)` | `ocr.Read(input)` or `ocr.Read(imagePath)` |
| `page.Text` | `result.Text` |
| `page.GetMeanConfidence()` (float 0–1) | `result.Confidence` (double 0–100) |
| No equivalent — stream input requires temp file | `input.LoadImage(stream)` |
| No equivalent — byte input requires temp file | `input.LoadImage(byteArray)` |
| No equivalent — PDF not supported | `input.LoadPdf(pdfPath)` |
| No equivalent — PDF not supported | `input.LoadPdf(pdfPath, Password: "secret")` |
| No equivalent — multi-frame TIFF limited | `input.LoadImageFrames(tiffPath)` |
| No equivalent — no output formats beyond text | `result.SaveAsSearchablePdf(outputPath)` |
| No equivalent — no hOCR output | `result.SaveAsHocrFile(outputPath)` |
| No equivalent — no structured data | `result.Pages[i].Paragraphs[j].Words[k]` |
| No equivalent — no word coordinates | `word.X`, `word.Y`, `word.Width`, `word.Height` |
| No equivalent — no per-word confidence | `word.Confidence` |
| No equivalent — no preprocessing | `input.Deskew()`, `input.DeNoise()`, `input.Contrast()` |
| No equivalent — no region selection | `input.LoadImage(path, new CropRectangle(x, y, w, h))` |
| No equivalent — no barcode support | `ocr.Configuration.ReadBarCodes = true; result.Barcodes` |
| `TesseractException` (inconsistent) | `IronOcrException` (consistent, always thrown on failure) |

Full class and method documentation is in the [IronTesseract API reference](https://ironsoftware.com/csharp/ocr/object-reference/api/IronOcr.IronTesseract.html) and [OcrResult API reference](https://ironsoftware.com/csharp/ocr/object-reference/api/IronOcr.OcrResult.html).

## Common Migration Issues and Solutions

### Issue 1: Empty String Results Disappear After Migration

**Tesseract OCR Wrapper:** Code that checked `if (string.IsNullOrEmpty(result))` to detect both failures and blank pages will behave differently after migration. IronOCR throws on failure rather than returning empty, so the empty-string check no longer catches engine failures.

**Solution:** Separate the two concerns. Use a `try/catch` for engine failures and check `result.Confidence` for quality filtering:

```csharp
try
{
    var result = new IronTesseract().Read(imagePath);
    if (result.Confidence < 10)
    {
        // Genuinely unreadable or blank — route to review
        return string.Empty;
    }
    return result.Text;
}
catch (IronOcrException)
{
    // Engine failure — log and handle separately from blank pages
    return null; // or rethrow
}
```

### Issue 2: Confidence Scale Changed

**Tesseract OCR Wrapper:** `page.GetMeanConfidence()` returns a `float` between 0 and 1. Code that thresholds on values like `0.7f` will fire on every IronOCR result.

**Solution:** `result.Confidence` in IronOCR is a `double` expressed as a percentage (0 to 100). Update threshold comparisons by multiplying the old value by 100:

```csharp
// Before (TesseractOCR): if (confidence < 0.7f)
// After (IronOCR):
if (result.Confidence < 70)
{
    // Below 70% confidence
}
```

### Issue 3: Language String Format Changed

**Tesseract OCR Wrapper:** Languages are specified as a `+`-delimited string in the `Engine` constructor: `"eng+fra+deu"`. The relevant `.traineddata` files must exist in the tessdata directory at that exact path.

**Solution:** Install language NuGet packages and use the `OcrLanguage` enum. Remove the tessdata directory from deployment:

```csharp
// dotnet add package IronOcr.Languages.French
// dotnet add package IronOcr.Languages.German

var ocr = new IronTesseract();
ocr.Language = OcrLanguage.English;
ocr.AddSecondaryLanguage(OcrLanguage.French);
ocr.AddSecondaryLanguage(OcrLanguage.German);
```

The [multiple languages guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-multiple-languages/) lists all 125+ available language packages.

### Issue 4: Tessdata Path Configuration Missing

**Tesseract OCR Wrapper:** The `Engine` constructor requires a tessdata filesystem path as its first argument. This path is typically stored in configuration and injected at runtime. After migration, that configuration key is unused.

**Solution:** Remove the tessdata path from configuration files and deployment scripts. Delete the tessdata directory from the repository and deployment artifacts. Remove the path parameter from the `Engine` constructor call — IronOCR resolves language data from installed NuGet packages automatically:

```csharp
// Before: new Engine(configuration["TessDataPath"], Language.English)
// After:
var ocr = new IronTesseract(); // language resolved from NuGet package
ocr.Language = OcrLanguage.English;
```

### Issue 5: PDF Input Requires Rasterization Layer Removal

**Tesseract OCR Wrapper:** PDF processing requires a rasterization library (PdfiumViewer, Ghostscript, or similar) to convert each page to a bitmap before passing it to the engine. That library is now unnecessary.

**Solution:** Remove the PDF rasterization library and replace the entire convert-then-OCR pipeline with a direct IronOCR call:

```csharp
// Before: rasterize each PDF page to bitmap, OCR each bitmap, collect results
// After:
using var input = new OcrInput();
input.LoadPdf("document.pdf");
var result = new IronTesseract().Read(input);
Console.WriteLine(result.Text);
```

The [PDF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-pdfs/) covers page range selection and password-protected PDFs.

### Issue 6: No Temporary File Needed for Stream Input

**Tesseract OCR Wrapper:** Uploading a file to an ASP.NET controller and OCR-ing the uploaded stream required writing bytes to a temp file, OCR-ing from the file path, and then deleting the temp file. That pattern leaves orphaned temp files if the OCR call throws.

**Solution:** Load directly from the stream using `OcrInput`:

```csharp
// Before: write to temp, OCR, delete temp
// After:
public async Task<string> OcrUpload(IFormFile file)
{
    using var stream = file.OpenReadStream();
    using var input = new OcrInput();
    input.LoadImage(stream);
    return new IronTesseract().Read(input).Text;
}
```

No temporary file, no cleanup logic, no orphaned files on exception.

## Tesseract OCR Wrapper Migration Checklist

### Pre-Migration

Audit the codebase for all usage of the wrapper before writing any new code:

```bash
# Find all files using the TesseractOCR namespace
grep -r "using TesseractOCR" --include="*.cs" .

# Find Engine constructor calls — these carry the tessdata path
grep -rn "new Engine(" --include="*.cs" .

# Find tessdata path configuration references
grep -rn "tessdata" --include="*.cs" .
grep -rn "tessdata" --include="*.json" .
grep -rn "tessdata" --include="*.xml" .

# Find all page.Text and page.GetText() calls — the primary output pattern
grep -rn "page\.Text\|page\.GetText()" --include="*.cs" .

# Find GetMeanConfidence calls — confidence scale will change
grep -rn "GetMeanConfidence" --include="*.cs" .

# Find PDF rasterization libraries that can be removed after migration
grep -rn "PdfiumViewer\|Ghostscript\|PDFsharp" --include="*.cs" .
grep -rn "PdfiumViewer\|Ghostscript\|PdfSharp" --include="*.csproj" .
```

Document the results before writing any code. Note how many call sites use the tessdata path, how many use confidence scoring, and whether any code relies on empty-string returns to detect failures.

### Code Migration

1. Remove the `TesseractOCR` NuGet package from the project file.
2. Install `IronOcr` via `dotnet add package IronOcr`.
3. Install language packs for each language previously downloaded as `.traineddata` files.
4. Add `IronOcr.License.LicenseKey = "YOUR-KEY";` at application startup.
5. Replace all `using TesseractOCR;` and `using TesseractOCR.Enums;` directives with `using IronOcr;`.
6. Replace every `new Engine(tessDataPath, language)` instantiation with `new IronTesseract()`.
7. Replace `Pix.Image.LoadFromFile(path)` and `engine.Process(img)` with `ocr.Read(path)` or an `OcrInput`-based call.
8. Replace `page.Text` and `page.GetText()` with `result.Text`.
9. Update confidence threshold comparisons: multiply old `float` thresholds by 100 for the `double` percentage scale.
10. Replace `+`-delimited language strings with `ocr.Language` and `ocr.AddSecondaryLanguage()` calls.
11. Replace empty-string failure detection with `try/catch IronOcrException`.
12. Replace temporary-file patterns for stream input with `input.LoadImage(stream)`.
13. Remove PDF rasterization library references where IronOCR's `input.LoadPdf()` replaces the rasterization step.
14. Remove the tessdata directory from deployment artifacts and configuration files.
15. Register `IronTesseract` as a singleton in the DI container for sequential workloads; use one instance per thread for parallel workloads.

### Post-Migration

- Confirm that OCR results on previously passing test images match or exceed the quality of the wrapper's output.
- Verify that engine failures now throw `IronOcrException` rather than returning empty strings.
- Confirm that confidence scores are in the 0–100 range and that threshold comparisons use the updated scale.
- Test multi-language documents to verify that language NuGet packages are installed and recognized correctly.
- Test stream and byte-array input paths to confirm no temporary files are created.
- Test PDF input directly (without rasterization) and confirm page count and text content are correct.
- Test searchable PDF output in a PDF viewer and confirm that text search returns results aligned to the original scan.
- Run the batch processing path and verify throughput with a reused `IronTesseract` instance.
- Confirm that the tessdata directory has been removed from deployment and that the application starts correctly without it.
- Run a load test against any ASP.NET endpoints that perform OCR to verify thread safety with per-request instances.

## Key Benefits of Migrating to IronOCR

**A Defined Error Contract.** After migration, every OCR failure produces a catchable, typed exception with a meaningful message. The silent empty-string failure mode is gone. Pipelines that previously required external quality validation logic — checking file sizes, running image analysis, comparing character counts — can rely on IronOCR's exception model and confidence scores instead.

**Output Format Coverage Without Additional Libraries.** The `OcrResult` object that comes back from every `Read()` call supports plain text, searchable PDF, and hOCR export without any additional package. [Searchable PDF generation](https://ironsoftware.com/csharp/ocr/how-to/searchable-pdf/) for compliance archives, and hOCR export for accessibility pipelines, become two lines of code rather than a multi-library integration project.

**Structured Data for Document Intelligence.** The complete word hierarchy — pages, paragraphs, lines, words, characters — with bounding box coordinates and per-word confidence is available on every result object. Invoice extractors, redaction tools, and form processors that previously parsed flat strings with fragile regular expressions gain spatial context that makes field identification layout-independent. The [OCR results feature page](https://ironsoftware.com/csharp/ocr/features/ocr-results/) covers the full data model.

**Native PDF and Multi-Format Input.** The PDF rasterization library and its associated configuration disappear from the dependency graph. Streams and byte arrays load directly into `OcrInput` without temporary files. Multi-frame TIFFs process in a single call. The input handling code that surrounded the wrapper — format detection, temp file management, cleanup logic — is replaced by a unified loading API.

**Deployment Without Environment Configuration.** The tessdata directory, the native binary version check, and the platform-specific binary deployment steps are gone. IronOCR bundles its engine and language data in the NuGet package. Deployment to [Docker](https://ironsoftware.com/csharp/ocr/get-started/docker/), [Linux](https://ironsoftware.com/csharp/ocr/get-started/linux/), [Azure](https://ironsoftware.com/csharp/ocr/get-started/azure/), or [AWS](https://ironsoftware.com/csharp/ocr/get-started/aws/) requires no environment-specific configuration beyond the one-line library dependency.

**Commercial Support and Predictable Licensing.** The wrapper is community-maintained with no support contract. IronOCR provides email support, a staffed documentation team, and regular releases with .NET version compatibility guarantees. The perpetual license model — starting at $749 for the Lite tier — means no per-page billing surprises and no subscription renewals that block access to new .NET versions. The investment in the license is typically recovered within the first iteration that eliminates the integration work the wrapper's gaps require.
