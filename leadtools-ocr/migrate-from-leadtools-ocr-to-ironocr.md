# Migrating from LEADTOOLS OCR to IronOCR

This guide walks .NET developers through a complete migration from LEADTOOLS OCR to [IronOCR](https://ironsoftware.com/csharp/ocr/). It covers every step from NuGet package replacement to full code migration, with before-and-after examples for the patterns most affected by LEADTOOLS' initialization ceremony, multi-namespace structure, and file-based license deployment model.

## Why Migrate from LEADTOOLS OCR

LEADTOOLS OCR ships inside an enterprise imaging platform with roots going back to 1990, and the API reflects that lineage. The minimum working configuration requires four NuGet packages, four namespaces, two physical license files deployed to a known path, a codec layer initialized before the engine, an engine type selection from three options, and a blocking startup call that loads runtime binaries into memory. All of that runs before a single character is recognized. Teams migrating to IronOCR eliminate that entire layer — one package, one namespace, one line to set a license key.

**File-Based License Deployment Breaks in Containers.** LEADTOOLS requires two physical files — `LEADTOOLS.LIC` and `LEADTOOLS.LIC.KEY` — readable at a specific path on every machine that runs the application. In Docker containers, those files must either be baked into the image (exposing them in layer history) or mounted at runtime (requiring volume coordination in every orchestration environment). Azure Functions and AWS Lambda have no mechanism for license file deployment without workarounds. CI/CD pipelines need the files present at the exact path used by the startup call, or the application throws before processing a single document. IronOCR replaces both files with a string that fits in an environment variable, a Kubernetes secret, or an Azure Key Vault reference.

**Four Packages for One Task.** A working LEADTOOLS OCR project requires `Leadtools`, `Leadtools.Ocr`, `Leadtools.Codecs`, and `Leadtools.Forms.DocumentWriters` at minimum. Password-protected PDF support requires an additional `Leadtools.Pdf` module that may not be included in the bundle purchased. Each package must be present, compatible, and versioned together. IronOCR ships as one NuGet package. All capabilities — native PDF input, searchable PDF output, preprocessing, barcode reading — are included.

**The Engine Lifecycle Creates a Maintenance Surface.** LEADTOOLS wraps the OCR engine in an `IDisposable` service class not for idiomatic .NET reasons, but because the `Shutdown()` call must precede `Dispose()` or the application produces errors. Production implementations of LEADTOOLS batch processors commonly include `GC.Collect()` / `GC.WaitForPendingFinalizers()` calls between document chunks to compensate for `RasterImage` instances that accumulate. IronOCR uses standard `using` blocks. `OcrInput` is the only object that needs disposal.

**Bundle Confusion Surfaces in Production.** LEADTOOLS does not publish pricing publicly. The Document Imaging SDK — the tier that includes OCR — runs an estimated $3,000–$8,000 per developer per year. Teams that purchased the OCR module without the PDF module discover the gap when `RasterSupport.IsLocked()` returns true against password-protected documents in production. The OmniPage engine accuracy tier requires a separate Kofax license agreement on top of the LEADTOOLS purchase. IronOCR licenses all features at each tier. There is no secondary vendor relationship, no separate module for encrypted documents.

**Initialization Cost Affects Cold Starts.** `engine.Startup()` is a blocking call that loads runtime files into memory. In serverless environments where cold-start latency matters — Azure Functions, AWS Lambda — a 500–2000 ms blocking initialization before any recognition work occurs is a structural problem. IronOCR uses lazy initialization. The `IronTesseract` instance initializes on first use, and subsequent calls within the same process pay no initialization cost at all.

### The Fundamental Problem

LEADTOOLS requires four namespaces, four NuGet packages, and a mandatory startup ceremony before a character is recognized:

```csharp
// LEADTOOLS: Four namespaces, four packages, six steps before recognition
using Leadtools;
using Leadtools.Ocr;
using Leadtools.Codecs;
using Leadtools.Forms.DocumentWriters;

RasterSupport.SetLicense(licPath, File.ReadAllText(keyPath)); // Step 1: two files on disk
var codecs = new RasterCodecs();                              // Step 2: codec layer
var engine = OcrEngineManager.CreateEngine(OcrEngineType.LEAD); // Step 3: engine factory
engine.Startup(codecs, null, null, runtimePath);              // Step 4: blocking startup
// ... still need to create document, add page, call Recognize(), extract text
```

```csharp
// IronOCR: One namespace, one package, one line
using IronOcr;

IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
var text = new IronTesseract().Read("document.jpg").Text;
```

## IronOCR vs LEADTOOLS OCR: Feature Comparison

The following table maps capabilities directly between the two libraries.

| Feature | LEADTOOLS OCR | IronOCR |
|---|---|---|
| **NuGet packages required** | 4 minimum (more for PDF) | 1 (`IronOcr`) |
| **License mechanism** | `.LIC` + `.LIC.KEY` file pair | String key |
| **License deployment** | Files on every production machine | Environment variable or config |
| **Pricing model** | $3,000–$15,000+/developer/year (estimated) | $749–$2,999 one-time perpetual |
| **Engine initialization** | Manual `Startup()` with runtime path | Automatic, lazy |
| **Engine shutdown** | Manual `Shutdown()` before `Dispose()` | Not required |
| **Codec layer** | `RasterCodecs` required for all image loading | Not required |
| **PDF input** | Page-by-page rasterization loop | Native `LoadPdf()` |
| **Password-protected PDF** | Requires separate `Leadtools.Pdf` module | Built-in `Password` parameter |
| **Searchable PDF output** | `DocumentWriter` + `PdfDocumentOptions` + `document.Save()` | `result.SaveAsSearchablePdf()` |
| **Preprocessing** | Separate command classes (`DeskewCommand`, `DespeckleCommand`, etc.) | Built-in filter methods on `OcrInput` |
| **Multi-page TIFF** | Manual frame iteration with `CodecsLoadByteOrder` | `input.LoadImageFrames()` |
| **Structured output** | Page and zone level | Page, paragraph, line, word, character with coordinates |
| **Confidence scoring** | `OcrPageRecognizeStatus` enum | `result.Confidence` as percentage |
| **Barcode reading** | Separate LEADTOOLS Barcode module | Built-in (`ReadBarCodes = true`) |
| **Thread safety** | Requires careful management | Full (one `IronTesseract` per thread) |
| **Languages supported** | 60–120 (engine dependent) | 125+ via NuGet language packages |
| **Language deployment** | tessdata files or engine-bundled files | NuGet package per language |
| **Cross-platform** | Supported with per-platform runtime configuration | Windows, Linux, macOS, Docker, Azure, AWS |
| **Docker deployment** | `.LIC`/`.KEY` files must be mounted or baked | Standard `dotnet publish` |
| **Commercial support** | Yes | Yes |

## Quick Start: LEADTOOLS OCR to IronOCR Migration

### Step 1: Replace NuGet Package

Remove all LEADTOOLS packages:

```bash
dotnet remove package Leadtools
dotnet remove package Leadtools.Ocr
dotnet remove package Leadtools.Codecs
dotnet remove package Leadtools.Forms.DocumentWriters
dotnet remove package Leadtools.Pdf
```

Install IronOCR from [NuGet](https://www.nuget.org/packages/IronOcr):

```bash
dotnet add package IronOcr
```

### Step 2: Update Namespaces

Replace all LEADTOOLS `using` directives with a single IronOCR import:

```csharp
// Before (LEADTOOLS)
using Leadtools;
using Leadtools.Ocr;
using Leadtools.Codecs;
using Leadtools.Forms.DocumentWriters;

// After (IronOCR)
using IronOcr;
```

### Step 3: Initialize License

Remove all `RasterSupport.SetLicense()` calls and the `.LIC` / `.LIC.KEY` file references. Add the IronOCR license key at application startup:

```csharp
// Single line replaces the entire file-based license setup
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

// Production pattern: pull from environment variable or secrets manager
IronOcr.License.LicenseKey = Environment.GetEnvironmentVariable("IRONOCR_LICENSE");
```

The license key can be stored in any standard .NET secrets management pattern — `appsettings.json`, Azure Key Vault, AWS Secrets Manager, or a Kubernetes secret. No files need to be deployed alongside the application binary.

## Code Migration Examples

### Engine Startup and Shutdown Lifecycle Removal

LEADTOOLS mandates a strict service-class pattern because the engine lifecycle must be managed explicitly. The constructor starts the engine, `Dispose()` shuts it down in the correct order, and any code path that skips `Shutdown()` before `Dispose()` produces a runtime error.

**LEADTOOLS OCR Approach:**

```csharp
using Leadtools;
using Leadtools.Ocr;
using Leadtools.Codecs;

// Service class required purely to manage engine lifecycle
public class LeadtoolsOcrService : IDisposable
{
    private IOcrEngine _engine;
    private RasterCodecs _codecs;
    private readonly string _runtimePath;

    public LeadtoolsOcrService(string licPath, string keyPath, string runtimePath)
    {
        _runtimePath = runtimePath;

        // License setup — two files, both must be present
        RasterSupport.SetLicense(licPath, File.ReadAllText(keyPath));

        // Codec layer — required before engine creation
        _codecs = new RasterCodecs();

        // Engine factory — engine type determines capability and cost
        _engine = OcrEngineManager.CreateEngine(OcrEngineType.LEAD);

        // Blocking startup — loads runtime into memory (500–2000ms)
        _engine.Startup(_codecs, null, null, _runtimePath);
    }

    public bool IsReady => _engine?.IsStarted ?? false;

    public string Process(string imagePath)
    {
        if (!IsReady)
            throw new InvalidOperationException("Engine not started");

        using var image = _codecs.Load(imagePath);
        using var doc = _engine.DocumentManager.CreateDocument();
        var page = doc.Pages.AddPage(image, null);
        page.Recognize(null);
        return page.GetText(-1);
    }

    public void Dispose()
    {
        // Order is mandatory: Shutdown before Dispose
        if (_engine?.IsStarted == true)
            _engine.Shutdown();

        _engine?.Dispose();
        _codecs?.Dispose();
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

// No lifecycle management needed — the service class becomes trivial
public class OcrService
{
    private readonly IronTesseract _ocr = new IronTesseract();

    // Always ready — no IsStarted check needed
    public string Process(string imagePath) => _ocr.Read(imagePath).Text;

    // No Dispose() needed for the engine
    // No Startup(), no Shutdown(), no codec layer
}
```

The `IronTesseract` instance initializes on first use. No constructor arguments, no runtime path, no `Startup()` call. The service class above does not need to implement `IDisposable` at all — the engine is stateless and the `OcrInput` objects used inside each `Read()` call handle their own cleanup via the standard `using` pattern. The [IronTesseract setup guide](https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/) covers configuration options including language selection and performance tuning for production scenarios.

### Multi-Frame TIFF Batch Processing

LEADTOOLS processes multi-frame TIFF files by querying the codec for total frame count, then iterating with explicit `firstPage`/`lastPage` parameters on each `_codecs.Load()` call. Every frame image must be disposed manually or memory accumulates.

**LEADTOOLS OCR Approach:**

```csharp
using Leadtools;
using Leadtools.Ocr;
using Leadtools.Codecs;

public class LeadtoolsTiffBatchService
{
    private readonly IOcrEngine _engine;
    private readonly RasterCodecs _codecs;

    public List<string> ProcessMultiFrameTiff(string tiffPath)
    {
        var pageTexts = new List<string>();

        // Must query page count before iterating
        var info = _codecs.GetInformation(tiffPath, true);
        int frameCount = info.TotalPages;

        using var document = _engine.DocumentManager.CreateDocument();

        for (int frameNum = 1; frameNum <= frameCount; frameNum++)
        {
            // Load one frame at a time — must specify firstPage/lastPage
            using var frameImage = _codecs.Load(
                tiffPath,
                0,                            // bitsPerPixel
                CodecsLoadByteOrder.BgrOrGray,
                frameNum,                     // firstPage
                frameNum);                    // lastPage

            var page = document.Pages.AddPage(frameImage, null);
            page.Recognize(null);
            pageTexts.Add(page.GetText(-1));

            // GC pressure accumulates if disposal is missed on any frame
        }

        return pageTexts;
    }

    public Dictionary<string, List<string>> ProcessTiffDirectory(string directoryPath)
    {
        var results = new Dictionary<string, List<string>>();

        foreach (var tiffFile in Directory.GetFiles(directoryPath, "*.tiff"))
        {
            results[tiffFile] = ProcessMultiFrameTiff(tiffFile);

            // Manual GC between files to prevent memory growth
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        return results;
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public class TiffBatchService
{
    private readonly IronTesseract _ocr = new IronTesseract();

    public List<string> ProcessMultiFrameTiff(string tiffPath)
    {
        using var input = new OcrInput();
        input.LoadImageFrames(tiffPath);  // All frames loaded automatically

        var result = _ocr.Read(input);

        // Per-frame text available through result.Pages
        return result.Pages.Select(p => p.Text).ToList();
    }

    public Dictionary<string, List<string>> ProcessTiffDirectory(string directoryPath)
    {
        var results = new Dictionary<string, List<string>>();

        foreach (var tiffFile in Directory.GetFiles(directoryPath, "*.tiff"))
        {
            results[tiffFile] = ProcessMultiFrameTiff(tiffFile);
        }

        return results;
    }

    // Parallel processing across files — thread-safe out of the box
    public Dictionary<string, List<string>> ProcessTiffDirectoryParallel(string directoryPath)
    {
        var concurrentResults = new System.Collections.Concurrent.ConcurrentDictionary<string, List<string>>();
        var tiffFiles = Directory.GetFiles(directoryPath, "*.tiff");

        Parallel.ForEach(tiffFiles, tiffFile =>
        {
            using var input = new OcrInput();
            input.LoadImageFrames(tiffFile);
            var result = new IronTesseract().Read(input);
            concurrentResults[tiffFile] = result.Pages.Select(p => p.Text).ToList();
        });

        return new Dictionary<string, List<string>>(concurrentResults);
    }
}
```

`LoadImageFrames()` reads all frames from the TIFF in one call. No frame count query, no loop, no explicit per-frame disposal. The parallel version creates one `IronTesseract` instance per thread, which is the correct pattern — see the [multithreading example](https://ironsoftware.com/csharp/ocr/examples/csharp-tesseract-multithreading-for-speed/) for the complete threading model. For TIFF-specific input options, the [TIFF and GIF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-tiff-gif/) covers frame selection and multi-format handling.

### Document Writer Pipeline Simplification

LEADTOOLS searchable PDF creation requires configuring a `DocumentWriter` instance on the engine, constructing a `PdfDocumentOptions` object with output type and overlay settings, applying the options via `SetOptions()`, and then calling `document.Save()` with the format enum. Each of those steps is a separate object and a separate API call.

**LEADTOOLS OCR Approach:**

```csharp
using Leadtools;
using Leadtools.Ocr;
using Leadtools.Codecs;
using Leadtools.Forms.DocumentWriters;

public class LeadtoolsDocumentWriterService
{
    private readonly IOcrEngine _engine;
    private readonly RasterCodecs _codecs;

    public void CreateSearchablePdfFromImages(string[] imagePaths, string outputPath)
    {
        using var document = _engine.DocumentManager.CreateDocument();

        foreach (var imagePath in imagePaths)
        {
            using var image = _codecs.Load(imagePath);
            var page = document.Pages.AddPage(image, null);
            page.Recognize(null);
        }

        // DocumentWriter configuration — four properties to set before save
        var pdfOptions = new PdfDocumentOptions
        {
            DocumentType = PdfDocumentType.Pdf,
            ImageOverText = true,     // Image layer visible, text layer searchable
            Linearized = false,
            Title = "Searchable Output"
        };

        // Apply options to the engine's writer instance
        _engine.DocumentWriterInstance.SetOptions(DocumentFormat.Pdf, pdfOptions);

        // Save with format enum — the format must match the options set above
        document.Save(outputPath, DocumentFormat.Pdf, null);
    }

    public void CreateSearchablePdfFromPdf(string inputPdfPath, string outputPath)
    {
        var pdfInfo = _codecs.GetInformation(inputPdfPath, true);
        using var document = _engine.DocumentManager.CreateDocument();

        for (int i = 1; i <= pdfInfo.TotalPages; i++)
        {
            using var pageImage = _codecs.Load(inputPdfPath, 0,
                CodecsLoadByteOrder.BgrOrGray, i, i);

            var page = document.Pages.AddPage(pageImage, null);
            page.Recognize(null);
        }

        var pdfOptions = new PdfDocumentOptions
        {
            DocumentType = PdfDocumentType.Pdf,
            ImageOverText = true,
            Title = Path.GetFileNameWithoutExtension(inputPdfPath)
        };

        _engine.DocumentWriterInstance.SetOptions(DocumentFormat.Pdf, pdfOptions);
        document.Save(outputPath, DocumentFormat.Pdf, null);
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public class SearchablePdfService
{
    private readonly IronTesseract _ocr = new IronTesseract();

    public void CreateSearchablePdfFromImages(string[] imagePaths, string outputPath)
    {
        using var input = new OcrInput();
        foreach (var imagePath in imagePaths)
            input.LoadImage(imagePath);

        var result = _ocr.Read(input);
        result.SaveAsSearchablePdf(outputPath);  // DocumentWriter pipeline: gone
    }

    public void CreateSearchablePdfFromPdf(string inputPdfPath, string outputPath)
    {
        using var input = new OcrInput();
        input.LoadPdf(inputPdfPath);

        var result = _ocr.Read(input);
        result.SaveAsSearchablePdf(outputPath);
    }

    // Get bytes directly — useful for streaming responses in ASP.NET
    public byte[] CreateSearchablePdfBytes(string inputPdfPath)
    {
        using var input = new OcrInput();
        input.LoadPdf(inputPdfPath);
        return _ocr.Read(input).SaveAsSearchablePdfBytes();
    }
}
```

`SaveAsSearchablePdf()` replaces the entire `PdfDocumentOptions` + `SetOptions()` + `document.Save()` chain. The image-over-text layer behavior is automatic. For complete searchable PDF output documentation, the [searchable PDF how-to guide](https://ironsoftware.com/csharp/ocr/how-to/searchable-pdf/) covers output options and the [searchable PDF example](https://ironsoftware.com/csharp/ocr/examples/make-pdf-searchable/) shows integration with ASP.NET response streams.

### Multi-Zone Field Extraction Migration

LEADTOOLS zone-based OCR uses `OcrZone` objects with `LeadRect` bounds, `OcrZoneType`, and `OcrZoneCharacterFilters` properties. Multiple zones are added to a single page and recognized in one `page.Recognize()` call, then extracted by zone index. The zone index matches the insertion order, which means the extraction loop must maintain that ordering.

**LEADTOOLS OCR Approach:**

```csharp
using Leadtools;
using Leadtools.Ocr;
using Leadtools.Codecs;

public class LeadtoolsFormFieldExtractor
{
    private readonly IOcrEngine _engine;
    private readonly RasterCodecs _codecs;

    // Invoice field extraction using named zones
    public InvoiceFields ExtractInvoiceFields(string invoicePath)
    {
        using var image = _codecs.Load(invoicePath);
        using var document = _engine.DocumentManager.CreateDocument();
        var page = document.Pages.AddPage(image, null);

        // Must clear auto-detected zones before adding custom ones
        page.Zones.Clear();

        // Zone definitions — index order matters for extraction
        var zoneDefinitions = new[]
        {
            new { Name = "InvoiceNumber", X = 450, Y = 80,  W = 200, H = 30 },
            new { Name = "InvoiceDate",   X = 450, Y = 115, W = 200, H = 30 },
            new { Name = "VendorName",    X = 50,  Y = 150, W = 300, H = 40 },
            new { Name = "TotalAmount",   X = 450, Y = 600, W = 200, H = 30 }
        };

        foreach (var def in zoneDefinitions)
        {
            var zone = new OcrZone
            {
                Bounds = new LeadRect(def.X, def.Y, def.W, def.H),
                ZoneType = OcrZoneType.Text,
                CharacterFilters = OcrZoneCharacterFilters.None,
                RecognitionModule = OcrZoneRecognitionModule.Auto
            };
            page.Zones.Add(zone);
        }

        page.Recognize(null);

        // Extract by index — must match insertion order exactly
        return new InvoiceFields
        {
            InvoiceNumber = page.Zones[0].Text?.Trim(),
            InvoiceDate   = page.Zones[1].Text?.Trim(),
            VendorName    = page.Zones[2].Text?.Trim(),
            TotalAmount   = page.Zones[3].Text?.Trim()
        };
    }
}

public class InvoiceFields
{
    public string InvoiceNumber { get; set; }
    public string InvoiceDate   { get; set; }
    public string VendorName    { get; set; }
    public string TotalAmount   { get; set; }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public class FormFieldExtractor
{
    private readonly IronTesseract _ocr = new IronTesseract();

    // Each field gets its own CropRectangle-scoped Read() call
    // No zone index management, no zone ordering dependency
    public InvoiceFields ExtractInvoiceFields(string invoicePath)
    {
        return new InvoiceFields
        {
            InvoiceNumber = ReadRegion(invoicePath, 450, 80,  200, 30),
            InvoiceDate   = ReadRegion(invoicePath, 450, 115, 200, 30),
            VendorName    = ReadRegion(invoicePath, 50,  150, 300, 40),
            TotalAmount   = ReadRegion(invoicePath, 450, 600, 200, 30)
        };
    }

    private string ReadRegion(string imagePath, int x, int y, int width, int height)
    {
        using var input = new OcrInput();
        input.LoadImage(imagePath, new CropRectangle(x, y, width, height));
        return _ocr.Read(input).Text.Trim();
    }

    // Batch: extract the same field from many invoices in parallel
    public Dictionary<string, string> ExtractInvoiceNumbersBatch(string[] invoicePaths)
    {
        var results = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

        Parallel.ForEach(invoicePaths, invoicePath =>
        {
            using var input = new OcrInput();
            input.LoadImage(invoicePath, new CropRectangle(450, 80, 200, 30));
            results[invoicePath] = new IronTesseract().Read(input).Text.Trim();
        });

        return new Dictionary<string, string>(results);
    }
}
```

`CropRectangle` passed directly to `LoadImage()` replaces the entire `OcrZone` setup. There is no zone index to track, no `page.Zones.Clear()` call required, and no recognition status check needed. The [region-based OCR guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-region-of-an-image/) covers single and multi-region extraction patterns. For a complete invoice field extraction tutorial, see the [invoice OCR tutorial](https://ironsoftware.com/csharp/ocr/blog/using-ironocr/invoice-ocr-csharp-tutorial/).

### Structured Data Extraction with Word Coordinates

LEADTOOLS structured output operates at the page and zone level. To get word-level data with bounding box coordinates, the developer accesses `OcrWord` objects from within a recognized zone. The API requires working through the zone collection after recognition and iterating the word list per zone.

**LEADTOOLS OCR Approach:**

```csharp
using Leadtools;
using Leadtools.Ocr;
using Leadtools.Codecs;

public class LeadtoolsStructuredExtractor
{
    private readonly IOcrEngine _engine;
    private readonly RasterCodecs _codecs;

    public List<WordLocation> ExtractWordsWithLocations(string imagePath)
    {
        var wordLocations = new List<WordLocation>();

        using var image = _codecs.Load(imagePath);
        using var document = _engine.DocumentManager.CreateDocument();
        var page = document.Pages.AddPage(image, null);
        page.Recognize(null);

        // Access words through the zone collection
        foreach (OcrZone zone in page.Zones)
        {
            foreach (OcrWord word in zone.Words)
            {
                wordLocations.Add(new WordLocation
                {
                    Text       = word.Value,
                    X          = word.Bounds.X,
                    Y          = word.Bounds.Y,
                    Width      = word.Bounds.Width,
                    Height     = word.Bounds.Height,
                    Confidence = word.Confidence
                });
            }
        }

        return wordLocations;
    }
}

public class WordLocation
{
    public string Text       { get; set; }
    public int    X          { get; set; }
    public int    Y          { get; set; }
    public int    Width      { get; set; }
    public int    Height     { get; set; }
    public int    Confidence { get; set; }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public class StructuredExtractor
{
    private readonly IronTesseract _ocr = new IronTesseract();

    public List<WordLocation> ExtractWordsWithLocations(string imagePath)
    {
        var result = _ocr.Read(imagePath);

        // Five-level hierarchy: Pages > Paragraphs > Lines > Words > Characters
        return result.Pages
            .SelectMany(page => page.Paragraphs)
            .SelectMany(para => para.Lines)
            .SelectMany(line => line.Words)
            .Select(word => new WordLocation
            {
                Text       = word.Text,
                X          = word.X,
                Y          = word.Y,
                Width      = word.Width,
                Height     = word.Height,
                Confidence = (int)word.Confidence
            })
            .ToList();
    }

    // Paragraph-level extraction with position data
    public void PrintDocumentStructure(string imagePath)
    {
        var result = _ocr.Read(imagePath);
        Console.WriteLine($"Document confidence: {result.Confidence}%");

        foreach (var page in result.Pages)
        {
            Console.WriteLine($"Page {page.PageNumber}:");
            foreach (var paragraph in page.Paragraphs)
            {
                Console.WriteLine($"  Paragraph at ({paragraph.X}, {paragraph.Y}):");
                Console.WriteLine($"  {paragraph.Text}");
            }
        }
    }
}
```

IronOCR's result hierarchy descends from `Pages` through `Paragraphs`, `Lines`, `Words`, and `Characters`. Every level exposes `X`, `Y`, `Width`, `Height`, `Text`, and `Confidence`. The LEADTOOLS zone-based access pattern disappears — no zone iteration required to reach word data. The [read results how-to guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) covers the complete output model with coordinate access patterns. The [OcrResult API reference](https://ironsoftware.com/csharp/ocr/object-reference/api/IronOcr.OcrResult.html) documents every property on the result hierarchy.

## LEADTOOLS OCR API to IronOCR Mapping Reference

| LEADTOOLS OCR | IronOCR |
|---|---|
| `RasterSupport.SetLicense(licPath, keyContent)` | `IronOcr.License.LicenseKey = "key"` |
| `new RasterCodecs()` | Not required |
| `OcrEngineManager.CreateEngine(OcrEngineType.LEAD)` | `new IronTesseract()` |
| `engine.Startup(codecs, null, null, runtimePath)` | Not required |
| `engine.IsStarted` | Not required (always ready) |
| `engine.Shutdown()` | Not required |
| `engine.Dispose()` | Not required |
| `_codecs.Load(imagePath)` | `input.LoadImage(imagePath)` |
| `_codecs.Load(path, 0, BgrOrGray, page, page)` | `input.LoadPdf(path)` or `input.LoadImageFrames(path)` |
| `_codecs.GetInformation(path, true).TotalPages` | Not required — automatic |
| `engine.DocumentManager.CreateDocument()` | Not required |
| `document.Pages.AddPage(image, null)` | `input.LoadImage(imagePath)` |
| `page.Recognize(null)` | `ocr.Read(input)` (recognition is part of `Read()`) |
| `page.GetText(-1)` | `result.Text` |
| `page.RecognizeStatus` | `result.Confidence` (percentage) |
| `OcrZone { Bounds = new LeadRect(x, y, w, h) }` | `new CropRectangle(x, y, w, h)` |
| `page.Zones.Clear()` | Not required |
| `page.Zones.Add(zone)` | `input.LoadImage(path, cropRect)` |
| `zone.Words` / `word.Bounds` | `result.Pages[n].Words` / `word.X`, `word.Y` |
| `DeskewCommand().Run(image)` | `input.Deskew()` |
| `DespeckleCommand().Run(image)` | `input.DeNoise()` |
| `AutoBinarizeCommand().Run(image)` | `input.Binarize()` |
| `ContrastBrightnessCommand().Run(image)` | `input.Contrast()` |
| `new PdfDocumentOptions { ImageOverText = true }` | Handled automatically by `SaveAsSearchablePdf()` |
| `engine.DocumentWriterInstance.SetOptions(format, opts)` | Not required |
| `document.Save(path, DocumentFormat.Pdf, null)` | `result.SaveAsSearchablePdf(path)` |
| `_codecs.Options.Pdf.Load.Password = password` | `input.LoadPdf(path, Password: password)` |
| `RasterSupport.IsLocked(RasterSupportType.Document)` | `IronOcr.License.IsLicensed` |

## Common Migration Issues and Solutions

### Issue 1: License File Path Resolution Failures

**LEADTOOLS:** `RasterSupport.SetLicense()` resolves the `.LIC` and `.LIC.KEY` file paths relative to the working directory, which differs between `bin/Debug`, `bin/Release`, Docker containers, and IIS application pools. A common failure mode is a path that works in development but throws `"License file not found"` in production because the working directory changed.

**Solution:** Delete both license files and the `SetLicense()` call entirely. Replace with a single string assignment that reads from an environment variable:

```csharp
// Remove this:
// RasterSupport.SetLicense(licPath, File.ReadAllText(keyPath));

// Replace with this:
IronOcr.License.LicenseKey = Environment.GetEnvironmentVariable("IRONOCR_LICENSE")
    ?? throw new InvalidOperationException("IRONOCR_LICENSE environment variable not set");
```

The string key behaves identically in every environment. Store it in the same secrets manager already used for database connection strings.

### Issue 2: Engine Not Started Exception

**LEADTOOLS:** Calling any recognition method before `engine.Startup()` completes, or after `engine.Shutdown()` has been called (for example, in a dispose-before-use race condition during shutdown), throws an `InvalidOperationException` with "Engine not started." Long-lived service classes must guard against this with `IsStarted` checks.

**Solution:** `IronTesseract` requires no startup call and has no started/stopped state. The `IsStarted` guard and the entire lifecycle service class can be deleted:

```csharp
// Remove the guard:
// if (!_engine.IsStarted)
//     throw new InvalidOperationException("Engine not started");

// IronTesseract is always ready — just call Read()
var result = _ocr.Read(imagePath);
```

### Issue 3: RasterImage Memory Accumulation in Batch Processing

**LEADTOOLS:** Batch processing that iterates over PDF pages or image files creates `RasterImage` instances in a loop. If any code path fails to dispose a `RasterImage` — due to an exception thrown before the `using` block exits, or a manual disposal pattern with a missing call — the unreleased images accumulate in memory. LEADTOOLS production code commonly includes `GC.Collect()` / `GC.WaitForPendingFinalizers()` calls between batches as a compensating mechanism.

**Solution:** Remove all `GC.Collect()` calls. `OcrInput` is the only `IDisposable` in the IronOCR pipeline, and it is scoped to each batch operation with a standard `using` block:

```csharp
// Remove this pattern:
// GC.Collect();
// GC.WaitForPendingFinalizers();

// Replace with standard using scope:
foreach (var filePath in filePaths)
{
    using var input = new OcrInput();
    input.LoadImage(filePath);
    var text = _ocr.Read(input).Text;
    // input disposed here — no accumulation
}
```

For additional memory optimization guidance, see the [memory allocation reduction blog post](https://ironsoftware.com/csharp/ocr/blog/using-ironocr/ocr-memory-allocation-reduction/).

### Issue 4: DocumentWriterInstance Options Persist Across Calls

**LEADTOOLS:** `engine.DocumentWriterInstance.SetOptions()` modifies state on the shared engine writer instance. If one code path sets `PdfDocumentOptions` with `DocumentType = PdfDocumentType.PdfA` and a subsequent call does not reset those options before calling `document.Save()`, the output format from the previous call persists. This is a stateful side effect on the shared engine instance.

**Solution:** IronOCR has no shared writer state. Each `SaveAsSearchablePdf()` call is independent:

```csharp
// Remove the options setup:
// var pdfOptions = new PdfDocumentOptions { ... };
// _engine.DocumentWriterInstance.SetOptions(DocumentFormat.Pdf, pdfOptions);
// document.Save(outputPath, DocumentFormat.Pdf, null);

// Replace with:
result.SaveAsSearchablePdf(outputPath);
```

Each call produces standard PDF output with an image overlay and a searchable text layer. There is no shared options state to reset between calls.

### Issue 5: Wrong Bundle — Missing Module at Runtime

**LEADTOOLS:** Teams that purchased the Document Imaging SDK may find that `Leadtools.Pdf` types are unavailable, or that encrypted PDF pages throw `RasterException` with `RasterExceptionCode.FeatureNotSupported`. This happens when the purchased bundle does not include the PDF module, and the error surfaces only at runtime in production.

**Solution:** IronOCR includes all features at every license tier. There is no separate PDF module, no add-on for encrypted documents, and no secondary vendor for a higher-accuracy engine. After installing the single `IronOcr` package, the full feature set is available without any additional purchase:

```bash
dotnet add package IronOcr
# Password-protected PDFs, searchable PDF output, preprocessing — all included
```

### Issue 6: Zone Index Mismatch After Reordering

**LEADTOOLS:** Zone extraction uses positional indexing — `page.Zones[0].Text`, `page.Zones[1].Text` — which ties the extraction logic to the insertion order in `page.Zones.Add()`. Reordering zone definitions to match a changed form layout silently breaks extraction by shifting all subsequent indices.

**Solution:** IronOCR uses named variables with `CropRectangle` per field. Reordering field definitions has no effect on extraction because each field is independently scoped:

```csharp
// Each field is independent — reorder freely without breaking extraction
var invoiceNumber = ReadRegion(imagePath, 450, 80,  200, 30);
var invoiceDate   = ReadRegion(imagePath, 450, 115, 200, 30);
var vendorName    = ReadRegion(imagePath, 50,  150, 300, 40);
var totalAmount   = ReadRegion(imagePath, 450, 600, 200, 30);
```

## LEADTOOLS OCR Migration Checklist

### Pre-Migration

Audit the codebase to identify all LEADTOOLS usage before making any changes:

```bash
# Find all LEADTOOLS namespace imports
grep -rn "using Leadtools" --include="*.cs" .

# Find engine lifecycle calls
grep -rn "OcrEngineManager\|\.Startup(\|\.Shutdown()" --include="*.cs" .

# Find license file references
grep -rn "SetLicense\|LEADTOOLS\.LIC\|\.LIC\.KEY" --include="*.cs" .

# Find RasterCodecs usage
grep -rn "RasterCodecs\|_codecs\.Load\|GetInformation" --include="*.cs" .

# Find DocumentWriter usage
grep -rn "DocumentWriterInstance\|PdfDocumentOptions\|DocumentFormat\." --include="*.cs" .

# Find zone-based OCR
grep -rn "OcrZone\|page\.Zones\|LeadRect\|ZoneType" --include="*.cs" .

# Find GC workarounds to remove
grep -rn "GC\.Collect\|WaitForPendingFinalizers" --include="*.cs" .
```

Note which LEADTOOLS bundle was purchased — OCR module, PDF module, and engine type (LEAD vs Tesseract vs OmniPage) — to ensure equivalent IronOCR features are tested during post-migration validation.

### Code Migration

1. Remove all LEADTOOLS NuGet packages: `Leadtools`, `Leadtools.Ocr`, `Leadtools.Codecs`, `Leadtools.Forms.DocumentWriters`, `Leadtools.Pdf`
2. Install `IronOcr` NuGet package
3. Replace all LEADTOOLS `using` directives with `using IronOcr;`
4. Remove the `.LIC` and `.LIC.KEY` files from the project and deployment artifacts
5. Replace `RasterSupport.SetLicense(licPath, keyContent)` with `IronOcr.License.LicenseKey = "key"` at application startup
6. Delete all `IDisposable` service classes that exist only to manage `IOcrEngine` and `RasterCodecs` lifecycle
7. Replace `OcrEngineManager.CreateEngine()` + `engine.Startup()` with `new IronTesseract()`
8. Replace `_codecs.Load(imagePath)` with `input.LoadImage(imagePath)` inside a `using var input = new OcrInput()` block
9. Replace multi-frame TIFF page loops with `input.LoadImageFrames(tiffPath)`
10. Replace the PDF page iteration loop with `input.LoadPdf(pdfPath)`
11. Replace `document.Pages.AddPage()` + `page.Recognize(null)` + `page.GetText(-1)` with `ocr.Read(input).Text`
12. Replace `OcrZone` + `page.Zones.Add()` patterns with `input.LoadImage(path, new CropRectangle(x, y, w, h))`
13. Replace `PdfDocumentOptions` + `DocumentWriterInstance.SetOptions()` + `document.Save()` with `result.SaveAsSearchablePdf(path)`
14. Replace preprocessing command classes (`DeskewCommand`, `DespeckleCommand`, `AutoBinarizeCommand`) with `input.Deskew()`, `input.DeNoise()`, `input.Binarize()` on the `OcrInput` instance
15. Remove all `GC.Collect()` / `GC.WaitForPendingFinalizers()` calls added to compensate for LEADTOOLS memory management

### Post-Migration

- Verify recognized text output matches LEADTOOLS output on a representative sample of images and PDFs
- Confirm confidence scores are within expected ranges using `result.Confidence`
- Test multi-frame TIFF processing produces the same number of pages as the LEADTOOLS frame iteration loop
- Validate searchable PDF output is text-searchable in a PDF reader (Adobe Acrobat or equivalent)
- Test zone-based field extraction against known-good field values from production invoices or forms
- Verify password-protected PDF decryption works without the separate `Leadtools.Pdf` module
- Run batch processing under load and confirm no memory growth (remove all `GC.Collect()` first)
- Test Docker and CI/CD deployment without license files — confirm the string license key resolves correctly from the environment variable
- Test parallel processing with `Parallel.ForEach` to verify thread safety
- Confirm structured data extraction (`result.Pages`, `page.Paragraphs`, `page.Words`) returns correct coordinates

## Key Benefits of Migrating to IronOCR

**Deployment Becomes Stateless.** The LEADTOOLS `.LIC` and `.LIC.KEY` files are artifacts that every deployment environment must carry. Containers that bake them in expose license data in image history. Containers that mount them need volume coordination. After migration to IronOCR, the license is a string in an environment variable. The deployment artifact is a standard NuGet reference. No files, no paths, no mounting strategy. The [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/) and [Azure deployment guide](https://ironsoftware.com/csharp/ocr/get-started/azure/) show the complete setup for containerized environments.

**Four Packages Become One.** The migration reduces `Leadtools`, `Leadtools.Ocr`, `Leadtools.Codecs`, `Leadtools.Forms.DocumentWriters`, and optionally `Leadtools.Pdf` to a single `IronOcr` reference. All capabilities — preprocessing, native PDF input, searchable PDF output, barcode reading, structured data extraction, 125+ language support — are included in that one package. The feature set does not depend on which bundle was purchased.

**Batch Processing Eliminates Manual Memory Management.** LEADTOOLS batch code carries defensive `GC.Collect()` calls and explicit `RasterImage` disposal to prevent memory accumulation during multi-document runs. IronOCR's `OcrInput` scoped with a `using` block handles cleanup automatically. Thread-safe parallel processing with `Parallel.ForEach` — one `IronTesseract` instance per thread — delivers multi-core throughput with no synchronization code. See the [speed optimization guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-fast-configuration/) for production throughput tuning.

**Predictable Total Cost.** LEADTOOLS' estimated cost for a five-developer team runs $15,000–$40,000 in the first year, with annual maintenance of roughly 20–25% of license cost required to receive updates. IronOCR Professional at $2,999 covers ten developers and ten deployment locations as a one-time perpetual purchase. One year of updates is included. Continued use after the update period requires no additional payment. The [IronOCR licensing page](https://ironsoftware.com/csharp/ocr/licensing/) publishes all tier prices directly, without a sales consultation.

**Structured Data Without Zone Setup.** After migration, word-level, line-level, and paragraph-level data with bounding box coordinates are available directly on `OcrResult` — no zone definitions required. The five-level hierarchy (`Pages`, `Paragraphs`, `Lines`, `Words`, `Characters`) each exposes position coordinates and per-element confidence scores. Applications that needed LEADTOOLS zone configuration to achieve structured extraction get richer data from a simpler API. The [OCR results feature page](https://ironsoftware.com/csharp/ocr/features/ocr-results/) summarizes the complete output model.
