# Migrating from GdPicture.NET to IronOCR

This guide walks .NET developers through a complete migration from GdPicture.NET OCR to [IronOCR](https://ironsoftware.com/csharp/ocr/). It covers the package swap, namespace changes, and practical before/after code patterns for every major OCR workflow, with particular focus on eliminating the integer image ID lifecycle that defines GdPicture's resource management model. No prior reading of the comparison article is required.

## Why Migrate from GdPicture.NET

GdPicture.NET is a document imaging platform built for teams that need scanner integration, DICOM support, PDF editing, annotation, and OCR from a single vendor. When OCR is the only requirement, the platform's pricing and API architecture create friction that accumulates over time.

**Plugin Cost for a Basic OCR Workflow.** Extracting text from scanned PDFs and producing searchable output requires three separate license components: the Core SDK at roughly $4,000, the OCR Plugin at roughly $2,000, and the PDF Plugin at roughly $2,000. That is an $8,000 entry cost, plus 20% annual maintenance. Teams that need only OCR absorb the full pricing structure of a document imaging platform. IronOCR covers the same workflow — image OCR, PDF OCR, preprocessing, searchable PDF output — from a single package at $749 to $2,999 perpetual, with no per-feature licensing decisions. See the [IronOCR licensing page](https://ironsoftware.com/csharp/ocr/licensing/) for a full tier breakdown.

**The Integer Image ID Lifecycle.** Every image loaded through GdPicture returns an `int`. You pass that integer to OCR operations, then call `ReleaseGdPictureImage` with it when done. This pattern predates `IDisposable`. It works when followed correctly; it leaks memory when missed. In production services processing hundreds of documents daily, one missed release call on an error branch produces a memory growth that is genuinely difficult to diagnose. IronOCR's `OcrInput` implements `IDisposable` — a `using` statement eliminates the entire cleanup burden.

**Version-Specific Namespace.** GdPicture embeds the major version number in its namespace: `using GdPicture14`. Upgrading to the next major release requires updating that `using` directive in every source file that references GdPicture classes. A large application with OCR spread across dozens of services makes that a multi-hour find-and-replace task that delivers no functional improvement. IronOCR's namespace has been `IronOcr` across all major versions.

**External Resource Folder Requirement.** GdPicture OCR requires a `ResourceFolder` property pointing to a directory of `.traineddata` language files at runtime. That path works on a development machine, then breaks on Linux servers, Docker containers, and Azure App Service deployments where the directory structure does not exist. IronOCR bundles English language support inside the NuGet package; additional languages install as NuGet packages that travel with the build output.

**Three-Component Initialization.** A PDF OCR workflow in GdPicture requires instantiating `GdPictureImaging`, `GdPictureOCR`, and `GdPicturePDF` — three separate components with individual disposal requirements — plus the license manager registration and the resource folder assignment. IronOCR requires one line at startup and one class at call time.

**No Native Thread Safety.** GdPicture OCR instances are not thread-safe. Parallel document processing requires careful instance management and synchronization to avoid corrupted state. IronOCR is designed for concurrent use: create one `IronTesseract` per thread, or share a single instance under load — either pattern works without additional synchronization code.

### The Fundamental Problem

GdPicture allocates memory for every image as a runtime-managed integer handle. Every `RenderPageToGdPictureImage` call in a TIFF processing loop creates a new allocation that must be released manually:

```csharp
// GdPicture: every frame = new integer ID = manual release required
using var pdf = new GdPicturePDF();
pdf.LoadFromFile("multi-page.tiff", false);

var frameIds = new List<int>();
try
{
    for (int i = 1; i <= pdf.GetPageCount(); i++)
    {
        pdf.SelectPage(i);
        int frameId = pdf.RenderPageToGdPictureImage(200, false); // new allocation
        if (frameId != 0) frameIds.Add(frameId);
        // ... OCR call here ...
    }
}
finally
{
    foreach (var id in frameIds) _imaging.ReleaseGdPictureImage(id); // manual per-frame release
}
```

IronOCR eliminates the lifecycle entirely. `OcrInput` handles frame enumeration and cleanup:

```csharp
// IronOCR: the using statement handles everything
using var input = new OcrInput();
input.LoadImageFrames("multi-page.tiff");
var result = new IronTesseract().Read(input);
```

## IronOCR vs GdPicture.NET: Feature Comparison

The table below maps feature coverage between the two libraries for OCR-focused workflows.

| Feature | GdPicture.NET | IronOCR |
|---|---|---|
| **Installation** | Multiple NuGet packages | `dotnet add package IronOcr` |
| **License activation** | `LicenseManager.RegisterKEY()` | `IronOcr.License.LicenseKey = "..."` |
| **Namespace on major upgrade** | Requires find-and-replace (`GdPicture14` → next) | Unchanged (`IronOcr`) |
| **Component initialization** | `GdPictureImaging` + `GdPictureOCR` + `GdPicturePDF` | `new IronTesseract()` |
| **Resource memory model** | Manual integer ID tracking and release | `IDisposable` / `using` statement |
| **Memory leak risk** | High (missing `ReleaseGdPictureImage`) | None (compiler-enforced via `using`) |
| **External resource folder** | Required (`_ocr.ResourceFolder = path`) | Not required — bundled in package |
| **Image OCR** | Yes | Yes |
| **PDF OCR** | Yes (PDF Plugin required) | Built-in, no additional license |
| **Multi-page TIFF** | Manual frame loop + per-frame ID cleanup | `input.LoadImageFrames()` |
| **Stream input** | Via `GdPictureImaging` stream overload | `input.LoadImage(stream)` |
| **Searchable PDF output** | `pdf.OcrPage()` + `pdf.SaveToFile()` | `result.SaveAsSearchablePdf()` |
| **Deskew preprocessing** | Document Imaging Plugin required | `input.Deskew()` — built-in |
| **Noise removal** | Document Imaging Plugin required | `input.DeNoise()` — built-in |
| **Languages** | Tesseract-based traineddata files in folder | 125+ via NuGet packages |
| **Multi-language OCR** | Yes | `OcrLanguage.French + OcrLanguage.German` |
| **Thread safety** | Manual instance management | Thread-safe by design |
| **Async OCR** | Not built-in | `ReadAsync()` |
| **Barcode reading** | Separate barcode plugin | `ocr.Configuration.ReadBarCodes = true` |
| **Structured output** | Index-based block/line/word access | Typed `.Pages`, `.Words`, `.Characters` |
| **Confidence scoring** | `GetOCRResultConfidence(resultId)` | `result.Confidence` |
| **Cross-platform** | Windows, Linux, macOS | Windows, Linux, macOS, Docker, AWS, Azure |
| **Entry cost (OCR from PDFs)** | ~$8,000 (Core + OCR + PDF plugins) | $749–$2,999 |
| **Pricing model** | Plugin-based perpetual + 20%/yr maintenance | Flat perpetual, optional annual updates |
| **Commercial support** | Yes | Yes |

## Quick Start: GdPicture.NET to IronOCR Migration

### Step 1: Replace NuGet Package

Remove the GdPicture packages:

```bash
dotnet remove package GdPicture.NET
dotnet remove package GdPicture.NET.OCR
dotnet remove package GdPicture.NET.PDF
```

Install IronOCR from the [NuGet package page](https://www.nuget.org/packages/IronOcr):

```bash
dotnet add package IronOcr
```

For languages beyond English, install the corresponding language pack:

```bash
dotnet add package IronOcr.Languages.French
dotnet add package IronOcr.Languages.German
```

### Step 2: Update Namespaces

Replace the GdPicture namespace with the IronOCR namespace:

```csharp
// Before (GdPicture)
using GdPicture14;

// After (IronOCR)
using IronOcr;
```

### Step 3: Initialize License

Place this line in `Program.cs` or `Startup.cs`, before any OCR calls:

```csharp
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
```

Remove the GdPicture license registration block entirely:

```csharp
// Remove this
LicenseManager lm = new LicenseManager();
lm.RegisterKEY("GDPICTURE-LICENSE-KEY");
```

A free trial key is available from the [IronOCR product page](https://ironsoftware.com/csharp/ocr/) to evaluate before purchasing.

## Code Migration Examples

### Multi-Page TIFF Frame Processing

Processing multi-page TIFF files in GdPicture requires selecting each frame through the PDF/imaging API, rendering it to a new image ID, running OCR, and releasing the ID. A single frame that is not released in the `finally` block leaks 10–50 MB depending on DPI.

**GdPicture.NET Approach:**

```csharp
using GdPicture14;

public class GdPictureTiffProcessor
{
    private readonly GdPictureImaging _imaging;
    private readonly GdPictureOCR _ocr;

    public string ExtractTextFromTiff(string tiffPath)
    {
        var text = new StringBuilder();

        // Load TIFF through the imaging component
        int tiffId = _imaging.CreateGdPictureImageFromFile(tiffPath);

        if (tiffId == 0)
            throw new Exception($"TIFF load failed: {_imaging.GetStat()}");

        // Outer try: release the original TIFF handle
        try
        {
            int frameCount = _imaging.GetPageCount(tiffId);

            for (int i = 1; i <= frameCount; i++)
            {
                // Switch to frame — modifies the existing ID in place
                _imaging.SelectPage(tiffId, i);

                // Clone frame to a new image ID for OCR
                int frameId = _imaging.CloneImage(tiffId);

                if (frameId == 0) continue;

                // Inner try: release each cloned frame ID
                try
                {
                    _ocr.SetImage(frameId);
                    _ocr.Language = "eng";

                    string resultId = _ocr.RunOCR();

                    if (!string.IsNullOrEmpty(resultId))
                    {
                        text.AppendLine($"[Frame {i}] {_ocr.GetOCRResultText(resultId)}");
                    }
                }
                finally
                {
                    // Release cloned frame — critical for each iteration
                    _imaging.ReleaseGdPictureImage(frameId);
                }
            }
        }
        finally
        {
            // Release original TIFF handle
            _imaging.ReleaseGdPictureImage(tiffId);
        }

        return text.ToString();
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public class IronOcrTiffProcessor
{
    public string ExtractTextFromTiff(string tiffPath)
    {
        using var input = new OcrInput();
        input.LoadImageFrames(tiffPath);  // all frames loaded, all cleanup automatic

        var result = new IronTesseract().Read(input);

        // Per-frame text is available on result.Pages
        foreach (var page in result.Pages)
            Console.WriteLine($"[Frame {page.PageNumber}] {page.Text}");

        return result.Text;
    }
}
```

`LoadImageFrames` loads every frame from a multi-page TIFF into the `OcrInput` pipeline. The `using` block handles all memory associated with every frame at the end of the scope. No `List<int>` to maintain, no nested `try/finally` blocks required. The [TIFF and GIF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-tiff-gif/) covers frame selection and multi-format handling in detail.

### Stream-Based Input Replacing Plugin Initialization

Server applications frequently receive documents as streams rather than file paths — from HTTP uploads, message queues, or database blobs. GdPicture requires loading the stream through `GdPictureImaging`, which itself requires the component initialization sequence and resource folder configuration that precede every operation.

**GdPicture.NET Approach:**

```csharp
using GdPicture14;

public class GdPictureStreamOcr : IDisposable
{
    private readonly GdPictureImaging _imaging;
    private readonly GdPictureOCR _ocr;

    public GdPictureStreamOcr()
    {
        // Plugin initialization required before stream loading is possible
        var lm = new LicenseManager();
        lm.RegisterKEY("GDPICTURE-LICENSE-KEY");

        _imaging = new GdPictureImaging();
        _ocr = new GdPictureOCR();
        _ocr.ResourceFolder = @"C:\GdPicture\Resources\OCR"; // path must exist at runtime
    }

    public string ExtractTextFromStream(Stream documentStream)
    {
        // Load stream into imaging component to get an image ID
        int imageId = _imaging.CreateGdPictureImageFromStream(documentStream);

        if (imageId == 0)
            throw new Exception($"Stream load failed: {_imaging.GetStat()}");

        try
        {
            _ocr.SetImage(imageId);
            _ocr.Language = "eng";

            string resultId = _ocr.RunOCR();

            if (string.IsNullOrEmpty(resultId))
                throw new Exception($"OCR failed: {_ocr.GetStat()}");

            return _ocr.GetOCRResultText(resultId);
        }
        finally
        {
            _imaging.ReleaseGdPictureImage(imageId);
        }
    }

    public void Dispose()
    {
        _ocr?.Dispose();
        _imaging?.Dispose();
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public class IronOcrStreamOcr
{
    public string ExtractTextFromStream(Stream documentStream)
    {
        using var input = new OcrInput();
        input.LoadImage(documentStream);  // stream accepted directly — no imaging component

        return new IronTesseract().Read(input).Text;
    }
}
```

The GdPicture approach requires constructing three objects and configuring a filesystem path before the first stream can be consumed. IronOCR accepts the stream directly on `OcrInput` with no prior setup. The [stream input guide](https://ironsoftware.com/csharp/ocr/how-to/input-streams/) covers byte array, `MemoryStream`, and `FileStream` patterns for pipeline architectures where temporary file writes are undesirable.

### Async OCR Replacing Status-Code Polling

GdPicture OCR is synchronous. Applications that process documents in ASP.NET Core endpoints or background services must wrap `RunOCR` in `Task.Run` to avoid blocking request threads — and then manage the image ID lifecycle across the thread boundary. IronOCR provides first-class async support through `ReadAsync`.

**GdPicture.NET Approach:**

```csharp
using GdPicture14;
using System.Threading.Tasks;

public class GdPictureAsyncWrapper
{
    private readonly GdPictureImaging _imaging;
    private readonly GdPictureOCR _ocr;
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

    public async Task<string> ExtractTextAsync(string imagePath)
    {
        // Must acquire lock: GdPictureOCR is not thread-safe
        await _lock.WaitAsync();

        try
        {
            return await Task.Run(() =>
            {
                int imageId = _imaging.CreateGdPictureImageFromFile(imagePath);

                if (imageId == 0)
                    throw new Exception($"Load failed: {_imaging.GetStat()}");

                try
                {
                    _ocr.SetImage(imageId);
                    _ocr.Language = "eng";

                    string resultId = _ocr.RunOCR();

                    if (string.IsNullOrEmpty(resultId))
                        throw new Exception($"OCR failed: {_ocr.GetStat()}");

                    return _ocr.GetOCRResultText(resultId);
                }
                finally
                {
                    _imaging.ReleaseGdPictureImage(imageId);
                }
            });
        }
        finally
        {
            _lock.Release();
        }
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public class IronOcrAsyncService
{
    private readonly IronTesseract _ocr = new IronTesseract();

    public async Task<string> ExtractTextAsync(string imagePath)
    {
        // ReadAsync is natively async — no Task.Run wrapper, no lock required
        var result = await _ocr.ReadAsync(imagePath);
        return result.Text;
    }

    public async Task<string> ExtractFromStreamAsync(Stream stream)
    {
        using var input = new OcrInput();
        input.LoadImage(stream);

        var result = await _ocr.ReadAsync(input);
        return result.Text;
    }
}
```

The GdPicture approach requires a `SemaphoreSlim` to serialize access to the shared `GdPictureOCR` instance, plus a `Task.Run` to move synchronous blocking work off the calling thread, plus the full image ID lifecycle inside that lambda. IronOCR's `ReadAsync` is genuinely non-blocking and thread-safe. The [async OCR guide](https://ironsoftware.com/csharp/ocr/how-to/async/) covers integration with ASP.NET Core middleware and hosted background services.

### Parallel Batch Processing with Thread Safety

Processing a folder of scanned documents as fast as possible requires parallel execution. GdPicture requires one `GdPictureOCR` instance per thread — sharing a single instance causes non-deterministic failures. Each per-thread instance also requires its own `GdPictureImaging` component and resource folder configuration, making thread pool approaches impractical without a factory pattern.

**GdPicture.NET Approach:**

```csharp
using GdPicture14;
using System.Collections.Concurrent;
using System.Threading.Tasks;

public class GdPictureParallelBatch
{
    private readonly string _resourceFolder = @"C:\GdPicture\Resources\OCR";

    public ConcurrentDictionary<string, string> ProcessBatch(string[] imagePaths)
    {
        var results = new ConcurrentDictionary<string, string>();

        // Each thread must have its own component instances
        Parallel.ForEach(imagePaths, imagePath =>
        {
            // Create per-thread instances — shared instances cause failures
            using var threadImaging = new GdPictureImaging();
            using var threadOcr = new GdPictureOCR();
            threadOcr.ResourceFolder = _resourceFolder;

            // Re-register license per thread (may be required depending on SDK version)
            var lm = new LicenseManager();
            lm.RegisterKEY("GDPICTURE-LICENSE-KEY");

            int imageId = threadImaging.CreateGdPictureImageFromFile(imagePath);

            if (imageId == 0)
            {
                results[imagePath] = $"ERROR: {threadImaging.GetStat()}";
                return;
            }

            try
            {
                threadOcr.SetImage(imageId);
                threadOcr.Language = "eng";

                string resultId = threadOcr.RunOCR();
                results[imagePath] = string.IsNullOrEmpty(resultId)
                    ? $"ERROR: {threadOcr.GetStat()}"
                    : threadOcr.GetOCRResultText(resultId);
            }
            finally
            {
                threadImaging.ReleaseGdPictureImage(imageId);
            }
        });

        return results;
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;
using System.Collections.Concurrent;
using System.Threading.Tasks;

public class IronOcrParallelBatch
{
    public ConcurrentDictionary<string, string> ProcessBatch(string[] imagePaths)
    {
        var results = new ConcurrentDictionary<string, string>();

        // IronTesseract is thread-safe — one instance handles all threads
        var ocr = new IronTesseract();

        Parallel.ForEach(imagePaths, imagePath =>
        {
            try
            {
                results[imagePath] = ocr.Read(imagePath).Text;
            }
            catch (Exception ex)
            {
                results[imagePath] = $"ERROR: {ex.Message}";
            }
        });

        return results;
    }
}
```

The GdPicture parallel implementation creates three objects per thread and requires per-thread license registration. IronOCR handles concurrent reads from a single `IronTesseract` instance with no synchronization overhead. The [multithreading example](https://ironsoftware.com/csharp/ocr/examples/csharp-tesseract-multithreading-for-speed/) demonstrates throughput benchmarks and `Parallel.ForEach` patterns for high-volume document processing workloads.

### Byte Array Input and Structured Paragraph Extraction

Applications that retrieve documents from databases or object storage often work with byte arrays rather than file paths. GdPicture requires converting the byte array to a stream and loading it through `GdPictureImaging`. Extracting structured paragraph-level data from the result requires navigating the block/line index hierarchy.

**GdPicture.NET Approach:**

```csharp
using GdPicture14;

public class GdPictureByteArrayOcr
{
    private readonly GdPictureImaging _imaging;
    private readonly GdPictureOCR _ocr;

    public List<string> ExtractParagraphsFromBytes(byte[] imageBytes)
    {
        var paragraphs = new List<string>();

        // Byte array must go through MemoryStream to reach CreateGdPictureImageFromStream
        using var ms = new MemoryStream(imageBytes);
        int imageId = _imaging.CreateGdPictureImageFromStream(ms);

        if (imageId == 0)
            throw new Exception($"Byte array load failed: {_imaging.GetStat()}");

        try
        {
            _ocr.SetImage(imageId);
            _ocr.Language = "eng";

            string resultId = _ocr.RunOCR();

            if (string.IsNullOrEmpty(resultId))
                throw new Exception($"OCR failed: {_ocr.GetStat()}");

            // Paragraph-level data requires iterating block structure
            int blockCount = _ocr.GetOCRResultBlockCount(resultId);

            for (int b = 0; b < blockCount; b++)
            {
                var blockText = new StringBuilder();
                int lineCount = _ocr.GetOCRResultBlockLineCount(resultId, b);

                for (int l = 0; l < lineCount; l++)
                {
                    int wordCount = _ocr.GetOCRResultBlockLineWordCount(resultId, b, l);

                    for (int w = 0; w < wordCount; w++)
                    {
                        blockText.Append(_ocr.GetOCRResultBlockLineWordText(resultId, b, l, w));
                        blockText.Append(" ");
                    }
                }

                string text = blockText.ToString().Trim();
                if (!string.IsNullOrEmpty(text))
                    paragraphs.Add(text);
            }
        }
        finally
        {
            _imaging.ReleaseGdPictureImage(imageId);
        }

        return paragraphs;
    }
}
```

**IronOCR Approach:**

```csharp
using IronOcr;

public class IronOcrByteArrayOcr
{
    public List<string> ExtractParagraphsFromBytes(byte[] imageBytes)
    {
        using var input = new OcrInput();
        input.LoadImage(imageBytes);  // byte array accepted directly

        var result = new IronTesseract().Read(input);

        // Paragraphs are a first-class typed collection
        return result.Paragraphs
            .Select(p => p.Text)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
    }
}
```

IronOCR accepts `byte[]` directly on `LoadImage` without the intermediate `MemoryStream`. The result exposes `.Paragraphs` as a typed LINQ-queryable collection — no block/line/word triple-loop required. The [read results guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) covers coordinate access, confidence filtering, and structured output patterns for invoice and form processing workflows. For scanning specific document areas, see [region-based OCR](https://ironsoftware.com/csharp/ocr/how-to/ocr-region-of-an-image/).

## GdPicture.NET API to IronOCR Mapping Reference

| GdPicture.NET | IronOCR Equivalent |
|---|---|
| `using GdPicture14;` | `using IronOcr;` |
| `LicenseManager.RegisterKEY("key")` | `IronOcr.License.LicenseKey = "key"` |
| `new GdPictureImaging()` | Not required — internal to `OcrInput` |
| `new GdPictureOCR()` | `new IronTesseract()` |
| `new GdPicturePDF()` | Not required — `OcrInput.LoadPdf()` handles this |
| `_ocr.ResourceFolder = path` | Not required — resources bundled in NuGet |
| `imaging.CreateGdPictureImageFromFile(path)` | `input.LoadImage(path)` |
| `imaging.CreateGdPictureImageFromStream(stream)` | `input.LoadImage(stream)` |
| `imaging.CreateGdPictureImageFromBytes(bytes)` | `input.LoadImage(bytes)` |
| `imaging.CloneImage(tiffId)` per frame | `input.LoadImageFrames(tiffPath)` |
| `imaging.ReleaseGdPictureImage(imageId)` | `using var input = new OcrInput()` — automatic |
| `ocr.SetImage(imageId)` | Not required — `OcrInput` holds the image |
| `ocr.Language = "eng"` | `ocr.Language = OcrLanguage.English` |
| `ocr.RunOCR()` → `resultId` string | `ocr.Read(input)` → typed `OcrResult` |
| `ocr.GetOCRResultText(resultId)` | `result.Text` |
| `ocr.GetOCRResultConfidence(resultId)` | `result.Confidence` |
| `ocr.GetOCRResultBlockCount(resultId)` | `result.Pages[i].Paragraphs.Count` |
| `ocr.GetOCRResultBlockLineWordText(resultId, b, l, w)` | `result.Words[i].Text` |
| `imaging.GetStat()` / `ocr.GetStat()` | Standard .NET exceptions |
| `GdPictureStatus.OK` check after each call | Not required — exceptions propagate |
| `pdf.OcrPage("eng", resourcePath, "", 200)` | `result.SaveAsSearchablePdf(outputPath)` |
| `pdf.RenderPageToGdPictureImage(200, false)` | Not required — IronOCR renders internally |
| `pdf.SelectPage(i)` | Not required — all pages processed by default |
| `pdf.GetPageCount()` | `result.Pages.Count` |
| `Task.Run(() => ocr.RunOCR())` + `SemaphoreSlim` | `await ocr.ReadAsync(input)` |

## Common Migration Issues and Solutions

### Issue 1: Image ID Variables Left in Code After Refactor

**GdPicture.NET:** Existing code declares `int imageId` at the top of methods, tracks it through try/finally, and passes it to multiple GdPicture calls. After replacing GdPicture calls, these variables and their `ReleaseGdPictureImage` calls become orphaned dead code.

**Solution:** Delete the entire image ID pattern. Replace the declaration, the `try`, the `finally`, and the release call with a `using var input = new OcrInput()` block. Grep for `ReleaseGdPictureImage` to find every cleanup call that must be removed:

```bash
grep -rn "ReleaseGdPictureImage\|imageId\|resultId" --include="*.cs" .
```

```csharp
// Remove all of this
int imageId = _imaging.CreateGdPictureImageFromFile(path);
try
{
    _ocr.SetImage(imageId);
    string resultId = _ocr.RunOCR();
    return _ocr.GetOCRResultText(resultId);
}
finally
{
    _imaging.ReleaseGdPictureImage(imageId);
}

// Replace with
using var input = new OcrInput();
input.LoadImage(path);
return new IronTesseract().Read(input).Text;
```

### Issue 2: ResourceFolder Path Not Found in Deployed Environment

**GdPicture.NET:** The path set in `_ocr.ResourceFolder` resolves on the development machine but fails in production. Common symptoms are silent OCR failures returning empty results, or generic `GdPictureStatus` errors that do not name the missing file.

**Solution:** Remove the `ResourceFolder` assignment entirely. English support is embedded in the IronOCR NuGet package. Additional languages install as NuGet packages. There is no filesystem path to configure or deploy:

```bash
# Remove the filesystem folder from deployment artifacts
# Add language NuGet packages to the project instead
dotnet add package IronOcr.Languages.French
dotnet add package IronOcr.Languages.German
```

### Issue 3: GdPictureStatus Error Handling Replaced by Exceptions

**GdPicture.NET:** Every operation returns or sets a `GdPictureStatus` enum value that must be checked immediately. Code is dense with `if (status != GdPictureStatus.OK)` guards. Some status codes are generic (e.g., `Error`, `InvalidParameter`) and require consulting documentation to determine root cause.

**Solution:** IronOCR throws typed .NET exceptions. Replace status checks with try/catch blocks. Standard `IOException` and `FileNotFoundException` cover input failures; `IronOcr.Exceptions.OcrException` covers OCR-specific errors. See the [IronTesseract setup guide](https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/) for recommended error handling patterns:

```csharp
try
{
    var result = new IronTesseract().Read(imagePath);
    return result.Text;
}
catch (FileNotFoundException ex)
{
    _logger.LogError("Input file missing: {Path}", ex.FileName);
    throw;
}
catch (IronOcr.Exceptions.OcrException ex)
{
    _logger.LogError("OCR processing failed: {Message}", ex.Message);
    throw;
}
```

### Issue 4: `GdPicture14` Namespace in Multiple Files

**GdPicture.NET:** The version number in the namespace means a project-wide `using GdPicture14;` directive exists in dozens of files. After migration, these must all be replaced with `using IronOcr;`.

**Solution:** Use a global find-and-replace across all `.cs` files, then verify no GdPicture references remain:

```bash
# Find all files with GdPicture namespace
grep -rln "using GdPicture" --include="*.cs" .

# After replacing, verify nothing remains
grep -rn "GdPicture14\|GdPictureOCR\|GdPictureImaging\|GdPicturePDF" --include="*.cs" .
```

### Issue 5: TIFF Frame Count Logic

**GdPicture.NET:** Code that iterates TIFF frames often mixes calls across `GdPictureImaging.GetPageCount(imageId)` and `GdPicturePDF.GetPageCount()` depending on how the file was loaded. The frame index is 1-based.

**Solution:** `input.LoadImageFrames(path)` handles all frames automatically. If your existing code only processes specific frames, use `input.LoadImageFrames(path, frameNumbers)` with a zero-based index array. Access per-frame results through `result.Pages`, which is also zero-indexed. The [TIFF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-tiff-gif/) documents the index behavior explicitly.

### Issue 6: Preprocessing Requires Document Imaging Plugin

**GdPicture.NET:** Deskew and despeckle operations belong to the `GdPictureDocumentImaging` plugin, which requires a separate license purchase. Teams that skipped the plugin often have OCR accuracy problems on scanned documents with skewed or noisy pages.

**Solution:** IronOCR preprocessing methods are part of the base package. Add `Deskew()` and `DeNoise()` directly on `OcrInput`. The [image quality correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/) covers all available filters including `Contrast()`, `Sharpen()`, `Binarize()`, and `DeepCleanBackgroundNoise()` for severely degraded scans:

```csharp
using var input = new OcrInput();
input.LoadImage("scanned-document.tiff");
input.Deskew();        // no separate plugin license
input.DeNoise();
input.Contrast();

var result = new IronTesseract().Read(input);
```

## GdPicture.NET Migration Checklist

### Pre-Migration

Audit the codebase to locate every GdPicture dependency before making changes:

```bash
# Find all GdPicture namespace imports
grep -rn "using GdPicture" --include="*.cs" .

# Find all image ID creation points
grep -rn "CreateGdPictureImageFromFile\|CreateGdPictureImageFromStream\|RenderPageToGdPictureImage\|CloneImage" --include="*.cs" .

# Find all release calls — these map to using block boundaries
grep -rn "ReleaseGdPictureImage" --include="*.cs" .

# Find all resource folder assignments
grep -rn "ResourceFolder" --include="*.cs" .

# Find all OCR result ID accesses
grep -rn "RunOCR\|GetOCRResult\|resultId" --include="*.cs" .

# Find all GdPictureStatus checks
grep -rn "GdPictureStatus\|GetStat()" --include="*.cs" .

# Count GdPicture-dependent files
grep -rln "GdPicture14" --include="*.cs" . | wc -l
```

Document the following before modifying code:

- Which files contain `GdPictureImaging`, `GdPictureOCR`, and `GdPicturePDF` references
- How many distinct image ID creation/release pairs exist
- Whether the Document Imaging Plugin (deskew, despeckle) is in use
- Which language `traineddata` files are in the resource folder
- Which deployment scripts or Docker files reference the resource folder path

### Code Migration

1. Remove all GdPicture NuGet packages from the project file
2. Install `IronOcr` via NuGet
3. Install `IronOcr.Languages.*` packages for each language previously in the resource folder
4. Add `IronOcr.License.LicenseKey = "..."` to `Program.cs` or `Startup.cs`
5. Remove the `LicenseManager.RegisterKEY()` call and the license manager object
6. Remove all `GdPictureImaging` field declarations and constructor initialization
7. Remove all `GdPictureOCR` field declarations and the `ResourceFolder` assignment
8. Remove all `GdPicturePDF` field declarations used for OCR workflows
9. Replace each `int imageId = _imaging.CreateGdPictureImage*(...)` block with `using var input = new OcrInput(); input.Load*(...)`
10. Replace each `_ocr.SetImage(imageId); _ocr.Language = "..."; string resultId = _ocr.RunOCR();` with `var result = new IronTesseract().Read(input);`
11. Replace `_ocr.GetOCRResultText(resultId)` with `result.Text`
12. Remove all `_imaging.ReleaseGdPictureImage(imageId)` calls
13. Replace `GdPictureStatus` checks with try/catch blocks
14. Replace TIFF frame loops with `input.LoadImageFrames(path)`
15. Replace `pdf.OcrPage(...)` + `pdf.SaveToFile(...)` with `result.SaveAsSearchablePdf(outputPath)`
16. Remove the resource folder path from deployment scripts and Docker images
17. Update `using GdPicture14;` to `using IronOcr;` across all affected files

### Post-Migration

After completing all code changes, verify the following before deploying to production:

- Single image OCR returns the expected text with no `NullReferenceException` from removed components
- Multi-page TIFF processing covers all frames and produces per-page text through `result.Pages`
- PDF OCR processes all pages without memory growth over a batch of 50+ documents
- Stream input accepts `MemoryStream` and `FileStream` without intermediate file writes
- Async OCR integrates with ASP.NET Core request handlers without blocking the thread pool
- Parallel batch processing with `Parallel.ForEach` produces accurate results across all threads
- All language packs (French, German, etc.) activate correctly through NuGet packages
- Preprocessing filters (deskew, denoise) improve accuracy on scanned documents
- Searchable PDF output is readable by PDF viewers and text-search applications
- No GdPicture namespace references remain in any `.cs` file after migration
- Memory profile shows stable usage under sustained load (no leaked image allocations)
- Deployment succeeds on Linux and Docker targets without filesystem path errors

## Key Benefits of Migrating to IronOCR

**Compiler-Enforced Memory Safety.** The image ID lifecycle that GdPicture requires developers to maintain manually disappears entirely. `OcrInput` implements `IDisposable`, and `using` blocks enforce cleanup at the end of every scope — including exception paths. No `List<int>` to maintain, no `finally` blocks to remember, no production memory incidents from missed release calls.

**Single Package for Every OCR Scenario.** Image OCR, PDF OCR, multi-page TIFF processing, searchable PDF generation, preprocessing filters, barcode reading, and 125+ language packs are all available from the `IronOcr` NuGet package and its language companions. There is no feature gate that requires a second or third license purchase before a common workflow becomes possible. See the [IronOCR feature overview](https://ironsoftware.com/csharp/ocr/features/document/) for the complete capability list.

**Native Async and Thread Safety.** `ReadAsync` is a genuine async method, not a `Task.Run` wrapper. `IronTesseract` is safe to use across threads without synchronization. Document processing services that previously required per-thread component initialization and semaphore serialization reduce to a single shared instance with `Parallel.ForEach`. The [async OCR guide](https://ironsoftware.com/csharp/ocr/how-to/async/) covers both ASP.NET Core and hosted service patterns.

**Zero Deployment Configuration.** IronOCR requires no filesystem paths, no external language files, no native binary placements, and no deployment scripts. The NuGet restore step provides everything the application needs. Docker images, Azure App Service deployments, and Linux servers all work identically to the development machine. The [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/) and [Azure deployment guide](https://ironsoftware.com/csharp/ocr/get-started/azure/) demonstrate production-ready configurations.

**Version-Stable Namespace.** `using IronOcr` has not changed across major version releases. NuGet version numbers handle versioning through the standard package management model. Future major upgrades do not require a codebase-wide find-and-replace of the namespace import.

**Predictable Perpetual Licensing.** IronOCR is priced at $749 (Lite), $1,499 (Plus), $2,999 (Professional), and $5,999 (Unlimited) — one-time perpetual purchases that include one year of updates, with optional renewal annually. All features are available at every tier. There are no per-feature plugins, no per-page costs, and no maintenance obligation after the first year. Teams that previously absorbed $8,000+ in GdPicture plugin licenses for an OCR-only workflow recover that gap within the first license cycle. Full pricing details are available on the [IronOCR licensing page](https://ironsoftware.com/csharp/ocr/licensing/).
