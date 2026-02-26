# Migrating from Asprise OCR to IronOCR

This guide walks .NET developers through every step of replacing Asprise OCR with [IronOCR](https://ironsoftware.com/csharp/ocr/). It covers the mechanical package swap, namespace changes, and the four code migration patterns that account for the majority of Asprise usage in production .NET applications. Developers who have already decided to migrate and need a concrete action plan are the intended audience.

## Why Migrate from Asprise OCR

Asprise OCR was designed as a Java product first. The .NET surface is a wrapper around a Java-originated native engine, and that origin shapes every aspect of the library's behavior in .NET — from deployment to API design to licensing constraints.

**JRE and Native Binary Dependency.** Asprise OCR for .NET requires platform-specific native binaries (`aocr.dll`, `aocr_x64.dll`, `libaocr.so`, `libaocr.dylib`) to be present on every machine where the application runs. Each binary must match the target platform and process architecture exactly. A 64-bit Docker container built with the 32-bit DLL throws `BadImageFormatException` at runtime. A Linux deployment missing `libaocr.so` from `LD_LIBRARY_PATH` throws `DllNotFoundException`. Neither error surfaces at build time. Every new deployment target — a new server, a new container image, a CI agent — becomes a manual binary sourcing exercise.

**String-Constant API from Java Heritage.** Asprise exposes integer constants for recognition type and output format: `Ocr.RECOGNIZE_TYPE_TEXT`, `Ocr.OUTPUT_FORMAT_PLAINTEXT`, `Ocr.OUTPUT_FORMAT_XML`. These constants map directly to the Java SDK's integer-based API. .NET developers receive no IntelliSense guidance on valid constant values, no compile-time safety on argument combinations, and no strongly-typed result objects. Extracting structured output requires parsing XML strings manually.

**No Async Support Without Workarounds.** Asprise provides no native async API. Wrapping synchronous Asprise calls in `Task.Run` to avoid blocking ASP.NET threads creates thread pool pressure and does not resolve the license restriction that prohibits concurrent execution on LITE and STANDARD tiers. Async patterns in modern .NET applications — background services, minimal API endpoints, Azure Functions — have no clean Asprise equivalent.

**Multi-Frame TIFF Processing Requires Manual Splitting.** Asprise operates on single image files. Processing a multi-page TIFF requires external code to split frames into individual files, then process each file in a loop. No frame metadata or page numbering carries over to the output.

**Thread Restriction Blocks Production Deployment.** LITE (~$299) and STANDARD (~$699) licenses contractually restrict execution to a single thread and single process. ASP.NET Core processes all HTTP requests on a thread pool. Every web API endpoint that calls Asprise on those tiers is a license violation from the first concurrent request. Upgrading to ENTERPRISE removes the restriction but requires contacting sales with no published price — estimates range from $2,000 to $5,000+ depending on deployment scope.

**Output Format Handling Requires String Parsing.** When `OUTPUT_FORMAT_XML` is specified, Asprise returns a raw XML string. The application is responsible for deserializing that string, validating its structure, and extracting words and their coordinates. Per-word confidence scores are embedded in XML attributes. There is no object model — only string manipulation.

### The Fundamental Problem

Asprise requires JRE-adjacent native binary configuration before the first OCR call can execute. IronOCR requires nothing beyond a NuGet package:

```csharp
// Asprise: native binary must exist in PATH or application directory
// aocr_x64.dll missing → DllNotFoundException at runtime, not at build
Ocr.SetUp();                              // Static init — touches native binary
Ocr ocr = new Ocr();
ocr.StartEngine("eng", Ocr.SPEED_FAST);  // Allocates native engine memory
string text = ocr.Recognize(imagePath, Ocr.RECOGNIZE_TYPE_TEXT, Ocr.OUTPUT_FORMAT_PLAINTEXT);
ocr.StopEngine();                         // Must call or native memory leaks
```

```csharp
// IronOCR: dotnet add package IronOcr — no binary sourcing, no lifecycle calls
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
string text = new IronTesseract().Read(imagePath).Text;
```

## IronOCR vs Asprise OCR: Feature Comparison

The table below covers the capabilities most relevant to developers evaluating this migration.

| Feature | Asprise OCR | IronOCR |
|---|---|---|
| Primary platform | Java (legacy) | .NET native |
| NuGet installation | Wrapper + platform native DLLs | Single package (`IronOcr`) |
| Native binary required at runtime | Yes (per-platform DLL) | No |
| .NET API style | Integer constants, string returns | Strongly-typed classes and enums |
| `IDisposable` / `using` pattern | Not implemented | Yes (`OcrInput`) |
| Async OCR | No native support | Yes (`ReadAsync`) |
| Multi-threading — LITE/STANDARD tier | Prohibited by license | Permitted |
| Multi-threading — all tiers | ENTERPRISE only | All tiers |
| ASP.NET Core Web API support | ENTERPRISE required | Any tier |
| Azure Functions / AWS Lambda | ENTERPRISE required | Any tier |
| Native PDF input | No | Yes |
| Multi-frame TIFF input | No (manual frame splitting) | Yes (`LoadImageFrames`) |
| Byte array and stream input | Limited | Yes |
| Built-in image preprocessing | No | Yes (9+ filters) |
| Searchable PDF output | No | Yes (`SaveAsSearchablePdf`) |
| Structured result object model | No (XML string only) | Yes (pages, paragraphs, words, characters) |
| Per-word confidence scores | No (XML attribute parsing) | Yes (`result.Confidence`) |
| Word pixel coordinates | XML attribute parsing | Strongly-typed properties |
| Language count | 20+ | 125+ |
| Strongly-typed language selection | No (string codes) | Yes (`OcrLanguage` enum) |
| Barcode reading | Yes (separate recognize type) | Yes (configuration flag) |
| hOCR export | No | Yes |
| Cross-platform deployment | Manual binary per platform | NuGet handles all platforms |
| Docker / Linux / macOS | Manual `LD_LIBRARY_PATH` config | Works out of the box |
| .NET compatibility | Limited (Java bridge) | .NET Framework 4.6.2+, .NET 5–9 |
| Entry price for server use | ENTERPRISE (~$2,000+) | $749 (Lite, all features) |
| License type | Per-tier, contact sales for Enterprise | Perpetual (one-time purchase) |

## Quick Start: Asprise OCR to IronOCR Migration

### Step 1: Replace NuGet Package

Remove Asprise OCR:

```bash
dotnet remove package asprise-ocr-api
```

Install IronOCR from the [NuGet package page](https://www.nuget.org/packages/IronOcr):

```bash
dotnet add package IronOcr
```

### Step 2: Update Namespaces

Replace Asprise namespaces with the IronOcr namespace:

```csharp
// Before (Asprise)
using asprise.ocr;

// After (IronOCR)
using IronOcr;
```

### Step 3: Initialize License

Add the license key assignment at application startup — in `Program.cs` before any OCR calls, in `Startup.Configure`, or in a static constructor:

```csharp
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
```

A free trial key is available from the [IronOCR licensing page](https://ironsoftware.com/csharp/ocr/licensing/). During development and evaluation, IronOCR runs without a key and stamps output with a trial watermark.

## Code Migration Examples

### JRE Path Configuration and Engine Initialization Removal

Asprise applications that run on Linux or macOS typically include startup code that sets the JRE path or validates native binary presence before any OCR work begins. This infrastructure has no equivalent in IronOCR.

**Asprise OCR Approach:**

```csharp
// AppStartup.cs — native binary validation before accepting any requests
public static void InitializeOcr()
{
    // Validate native library is reachable before first use
    string nativePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? Path.Combine(AppContext.BaseDirectory, "aocr_x64.dll")
        : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? "/usr/lib/libaocr.so"
            : "/usr/local/lib/libaocr.dylib";

    if (!File.Exists(nativePath))
        throw new FileNotFoundException(
            $"Asprise native binary not found: {nativePath}. " +
            "Deploy the correct platform binary before starting.");

    // Static global init — must run before any Ocr instance is created
    Ocr.SetUp();
}
```

**IronOCR Approach:**

```csharp
// Program.cs — license key assignment is the entire initialization
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

// That is it. No binary validation, no path configuration, no SetUp() call.
// NuGet resolved the correct native runtime during package restore.
```

The Asprise pattern is typically 15-30 lines across multiple files — a startup validator, a platform switch, an exception with a deployment message, and the `SetUp()` call. IronOCR replaces all of it with a single assignment. The [IronTesseract setup guide](https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/) covers deployment configuration options for environments that require custom tessdata paths or offline operation.

### XML Output Format Replacement with Structured Result Objects

Asprise produces structured output as a raw XML string when `OUTPUT_FORMAT_XML` is specified. Extracting word text, coordinates, and confidence from that string requires XML parsing code. IronOCR returns a typed object graph.

**Asprise OCR Approach:**

```csharp
// Asprise: structured output is an XML string — must parse manually
Ocr.SetUp();
Ocr ocr = new Ocr();
try
{
    ocr.StartEngine("eng", Ocr.SPEED_FAST);
    string xmlOutput = ocr.Recognize(
        imagePath,
        Ocr.RECOGNIZE_TYPE_TEXT,
        Ocr.OUTPUT_FORMAT_XML);   // Returns raw XML, not an object

    // Parse the XML manually to extract words and coordinates
    var doc = System.Xml.Linq.XDocument.Parse(xmlOutput);
    var words = doc.Descendants("word")
        .Select(w => new
        {
            Text       = (string)w.Attribute("text"),
            Confidence = (float)w.Attribute("confidence"),
            X          = (int)w.Attribute("x"),
            Y          = (int)w.Attribute("y"),
        })
        .ToList();

    foreach (var word in words)
        Console.WriteLine($"{word.Text} @ ({word.X},{word.Y}) conf={word.Confidence}");
}
finally
{
    ocr.StopEngine();
}
```

**IronOCR Approach:**

```csharp
// IronOCR: structured result is a typed object — no XML parsing
var result = new IronTesseract().Read(imagePath);

foreach (var page in result.Pages)
{
    foreach (var paragraph in page.Paragraphs)
    {
        foreach (var word in paragraph.Words)
        {
            Console.WriteLine(
                $"{word.Text} @ ({word.X},{word.Y}) conf={word.Confidence:F1}%");
        }
    }
}
```

No XML deserialization, no attribute casting, no schema assumptions. The `OcrResult` object model exposes pages, paragraphs, lines, words, and characters with typed properties. The [read results guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) covers the full hierarchy and coordinate system, including how to filter by confidence threshold for automated workflows.

### Multi-Frame TIFF Processing

Asprise accepts single image files. A multi-frame TIFF — common in document scanning workflows — must be split into individual frame files before Asprise can process it. IronOCR accepts multi-frame TIFFs directly through `LoadImageFrames`.

**Asprise OCR Approach:**

```csharp
// Asprise: no multi-frame TIFF support — split frames externally first
// Using an external imaging library (e.g., System.Drawing or Magick.NET)
var frameFiles = new List<string>();
using (var tiff = System.Drawing.Image.FromFile("scanned-batch.tiff"))
{
    int frameCount = tiff.GetFrameCount(System.Drawing.Imaging.FrameDimension.Page);
    for (int i = 0; i < frameCount; i++)
    {
        tiff.SelectActiveFrame(System.Drawing.Imaging.FrameDimension.Page, i);
        string framePath = $"frame_{i}.png";
        tiff.Save(framePath);
        frameFiles.Add(framePath);
    }
}

// Now process each frame individually — sequential on LITE/STANDARD
var allText = new System.Text.StringBuilder();
Ocr.SetUp();
Ocr ocr = new Ocr();
try
{
    ocr.StartEngine("eng", Ocr.SPEED_FAST);
    foreach (var framePath in frameFiles)
    {
        string pageText = ocr.Recognize(
            framePath,
            Ocr.RECOGNIZE_TYPE_TEXT,
            Ocr.OUTPUT_FORMAT_PLAINTEXT);
        allText.AppendLine(pageText);
    }
}
finally
{
    ocr.StopEngine();
    // Clean up temporary frame files
    foreach (var f in frameFiles)
        File.Delete(f);
}
Console.WriteLine(allText.ToString());
```

**IronOCR Approach:**

```csharp
// IronOCR: multi-frame TIFF loads directly — no frame splitting, no temp files
using var input = new OcrInput();
input.LoadImageFrames("scanned-batch.tiff");  // All frames, one call

var result = new IronTesseract().Read(input);

// Access each page independently with its page number
foreach (var page in result.Pages)
    Console.WriteLine($"Page {page.PageNumber}: {page.Text}");
```

The Asprise approach requires an external imaging dependency, temporary file management, manual cleanup, and sequential per-frame processing. IronOCR processes all frames in a single pass. The [TIFF and GIF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-tiff-gif/) covers frame range selection for large TIFFs where only specific pages are needed.

### Searchable PDF Generation

Asprise has no searchable PDF output capability at any license tier. Creating a PDF with embedded OCR text from a scanned document requires an external PDF library, a separate OCR pass to get text positions, and manual overlay construction. IronOCR produces searchable PDFs directly from the recognition result.

**Asprise OCR Approach:**

```csharp
// Asprise: no searchable PDF output — external PDF library required
// Step 1: OCR the document to get text
Ocr.SetUp();
Ocr ocr = new Ocr();
string recognizedText;
try
{
    ocr.StartEngine("eng", Ocr.SPEED_FAST);
    recognizedText = ocr.Recognize(
        "scanned-contract.jpg",
        Ocr.RECOGNIZE_TYPE_TEXT,
        Ocr.OUTPUT_FORMAT_PLAINTEXT);   // Only plain text — no position data
}
finally
{
    ocr.StopEngine();
}

// Step 2: Use an external PDF library to embed text over the image
// (iTextSharp, PdfSharp, or similar — adds another dependency and license)
// Text positioning requires coordinate data Asprise cannot provide in plain text mode
// ... 40-80 lines of PDF construction code
Console.WriteLine("Searchable PDF: not achievable with Asprise alone");
```

**IronOCR Approach:**

```csharp
// IronOCR: searchable PDF in two lines — no external PDF library
var result = new IronTesseract().Read("scanned-contract.jpg");
result.SaveAsSearchablePdf("searchable-contract.pdf");

// Batch: convert a folder of scanned images to searchable PDFs
foreach (var imagePath in Directory.GetFiles("scans", "*.jpg"))
{
    var batchResult = new IronTesseract().Read(imagePath);
    string outputPath = Path.ChangeExtension(imagePath, ".searchable.pdf");
    batchResult.SaveAsSearchablePdf(outputPath);
    Console.WriteLine($"Converted: {outputPath}");
}
```

The searchable PDF contains the original image as the visual layer with invisible OCR text overlaid at the correct coordinates — the standard format for archival and compliance workflows. See the [searchable PDF how-to guide](https://ironsoftware.com/csharp/ocr/how-to/searchable-pdf/) and the [searchable PDF example](https://ironsoftware.com/csharp/ocr/examples/make-pdf-searchable/) for options including PDF/A output for long-term archival.

### Async OCR in Web Applications

Asprise has no async API. Developers integrate it into async .NET applications by wrapping synchronous calls in `Task.Run`, which consumes thread pool threads and does not eliminate blocking. IronOCR provides a native async path.

**Asprise OCR Approach:**

```csharp
// Asprise: no async API — must offload to Task.Run
// This blocks a thread pool thread during the entire OCR operation
// On LITE/STANDARD, concurrent Task.Run calls = license violation
public async Task<string> ProcessUploadAsync(Stream fileStream, string fileName)
{
    string tempPath = Path.GetTempFileName();
    await using (var fs = new FileStream(tempPath, FileMode.Create))
        await fileStream.CopyToAsync(fs);

    // Task.Run wraps synchronous Asprise — occupies a thread pool thread
    // Two concurrent requests still violate LITE/STANDARD license
    return await Task.Run(() =>
    {
        Ocr ocr = new Ocr();
        try
        {
            ocr.StartEngine("eng", Ocr.SPEED_FAST);
            return ocr.Recognize(
                tempPath,
                Ocr.RECOGNIZE_TYPE_TEXT,
                Ocr.OUTPUT_FORMAT_PLAINTEXT);
        }
        finally
        {
            ocr.StopEngine();
            File.Delete(tempPath);
        }
    });
}
```

**IronOCR Approach:**

```csharp
// IronOCR: native async, concurrent requests permitted on all tiers
public async Task<string> ProcessUploadAsync(Stream fileStream, string fileName)
{
    using var input = new OcrInput();
    input.LoadImage(fileStream);       // Stream input directly — no temp file

    var ocr = new IronTesseract();
    var result = await ocr.ReadAsync(input);
    return result.Text;
}
```

The IronOCR version eliminates the temp file write, the `Task.Run` wrapper, and the thread-blocking behavior. Multiple concurrent requests each create their own `IronTesseract` instance — the class is stateless and each instance is independent. The [async OCR guide](https://ironsoftware.com/csharp/ocr/how-to/async/) covers `ReadAsync` patterns and cancellation token support for long-running batch operations in hosted services.

## Asprise OCR API to IronOCR Mapping Reference

| Asprise OCR | IronOCR Equivalent |
|---|---|
| `asprise.ocr` namespace | `IronOcr` namespace |
| `Ocr.SetUp()` | Not required |
| `new Ocr()` | `new IronTesseract()` |
| `ocr.StartEngine("eng", Ocr.SPEED_FAST)` | Not required |
| `ocr.StartEngine("eng+fra", speed)` | `ocr.Language = OcrLanguage.English + OcrLanguage.French` |
| `ocr.Recognize(path, type, format)` | `ocr.Read(path)` or `ocr.Read(input)` |
| `Ocr.RECOGNIZE_TYPE_TEXT` | Default behavior |
| `Ocr.RECOGNIZE_TYPE_BARCODE` | `ocr.Configuration.ReadBarCodes = true` |
| `Ocr.RECOGNIZE_TYPE_ALL` | `ocr.Configuration.ReadBarCodes = true` |
| `Ocr.OUTPUT_FORMAT_PLAINTEXT` | `result.Text` |
| `Ocr.OUTPUT_FORMAT_XML` | `result.Pages` / `result.Pages[n].Words` |
| `Ocr.OUTPUT_FORMAT_PDF` | `result.SaveAsSearchablePdf(path)` |
| `Ocr.SPEED_FASTEST` | `ocr.Configuration.TesseractEngineMode` tuning |
| `Ocr.SPEED_FAST` | Default configuration |
| `Ocr.SPEED_SLOW` | Higher-accuracy configuration settings |
| `ocr.StopEngine()` | Not required — `OcrInput` is `IDisposable` |
| `result.StartsWith("ERROR:")` check | Standard .NET exception handling (`try/catch`) |
| Platform native DLL (aocr_x64.dll) | NuGet runtime package (automatic) |
| Manual temp file for stream input | `input.LoadImage(stream)` directly |
| External library for multi-frame TIFF | `input.LoadImageFrames(path)` |
| External library for searchable PDF | `result.SaveAsSearchablePdf(path)` |

## Common Migration Issues and Solutions

### Issue 1: DllNotFoundException After Removing Native Binaries

**Asprise OCR:** Removing the Asprise NuGet package but leaving native binary references (in project file copy rules, Docker COPY instructions, or deployment scripts) can cause `DllNotFoundException` to resurface from stale configuration pointing at a non-existent binary.

**Solution:** Search deployment artifacts for any reference to `aocr`, `libaocr`, or `LD_LIBRARY_PATH` settings and remove them. IronOCR has no corresponding configuration requirement. In Dockerfile:

```dockerfile
# Remove: COPY aocr_x64.dll /app/
# Remove: ENV LD_LIBRARY_PATH=/app
# IronOCR: nothing to add — NuGet handles native runtime packaging
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish
```

For cross-platform deployment, the [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/) covers the base image requirements for IronOCR on Linux containers.

### Issue 2: Missing `Ocr.SetUp()` Removal Breaks Startup

**Asprise OCR:** `Ocr.SetUp()` performs global native initialization. Some codebases call it in a static constructor or `Startup.Configure`. After migration, removing the Asprise namespace removes the compile error, but if `SetUp()` is wrapped in a try/catch that suppresses the exception, the code may compile and run silently without initializing anything.

**Solution:** Grep for all `SetUp()` calls and remove the entire initialization block. Replace the equivalent startup hook with the IronOCR license key assignment:

```bash
grep -rn "Ocr.SetUp\|StartEngine\|StopEngine" --include="*.cs" .
```

```csharp
// Remove all occurrences of the engine lifecycle pattern:
// Ocr.SetUp();
// ocr.StartEngine(...);
// ocr.StopEngine();

// Replace application startup initialization with:
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
```

### Issue 3: XML Output Parsing Code Has No Direct Replacement

**Asprise OCR:** Code that parses `OUTPUT_FORMAT_XML` strings using `XDocument`, `XmlReader`, or regex patterns has no equivalent XML structure in IronOCR. The XML schema Asprise produces does not map directly to IronOCR's object model.

**Solution:** Replace XML parsing code with direct property access on `OcrResult`. The mapping is:

```csharp
// Asprise XML parsing (remove)
var words = XDocument.Parse(xmlOutput)
    .Descendants("word")
    .Select(w => new { Text = (string)w.Attribute("text"), X = (int)w.Attribute("x") });

// IronOCR object model (replace with)
var result = new IronTesseract().Read(imagePath);
var words = result.Pages
    .SelectMany(p => p.Paragraphs)
    .SelectMany(para => para.Words)
    .Select(w => new { w.Text, w.X });
```

The [read results guide](https://ironsoftware.com/csharp/ocr/how-to/read-results/) covers the complete object hierarchy including character-level data with bounding boxes.

### Issue 4: Task.Run Wrappers Causing Thread Pool Exhaustion

**Asprise OCR:** High-concurrency web applications wrapping Asprise in `Task.Run` can exhaust the thread pool when OCR volume spikes. Each queued `Task.Run` holds a thread pool thread for the full duration of the OCR operation.

**Solution:** Replace `Task.Run(() => { asprise... })` patterns with native IronOCR async calls. Each `IronTesseract` instance is independent — create one per request:

```csharp
// Remove: await Task.Run(() => { ocr.Recognize(...) });

// Replace with:
using var input = new OcrInput();
input.LoadImage(stream);
var result = await new IronTesseract().ReadAsync(input);
return result.Text;
```

### Issue 5: String-Based Language Code Validation

**Asprise OCR:** Language codes are passed as strings (`"eng"`, `"fra"`, `"eng+fra"`). Applications that validate these strings at runtime — checking against a hard-coded list, reading from configuration — need updating when the string format changes to the `OcrLanguage` enum.

**Solution:** Replace string language parameters with `OcrLanguage` enum values. Configuration-driven language selection maps cleanly to `Enum.Parse`:

```csharp
// Asprise string-based (remove)
string language = config["OcrLanguage"];  // e.g. "eng+fra"
ocr.StartEngine(language, Ocr.SPEED_FAST);

// IronOCR enum-based (replace with)
// For single language from config:
var ocr = new IronTesseract();
ocr.Language = Enum.Parse<OcrLanguage>(config["OcrLanguage"]);  // e.g. "English"
var result = ocr.Read(input);
```

The [multiple languages guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-multiple-languages/) lists all valid `OcrLanguage` enum values and their corresponding NuGet language pack packages.

### Issue 6: License Tier Check Logic No Longer Needed

**Asprise OCR:** Some production codebases include runtime checks that detect the Asprise license tier and serialize OCR work when running below ENTERPRISE. These guards prevent license violations but add complexity and reduce throughput.

**Solution:** Remove all tier-detection and serialization guards. IronOCR carries no threading restriction on any tier. The `ConcurrentQueue`, `SemaphoreSlim`, or single-threaded dispatcher patterns used to serialize Asprise calls serve no purpose after migration:

```csharp
// Remove: SemaphoreSlim _ocrLock = new SemaphoreSlim(1, 1);
// Remove: await _ocrLock.WaitAsync(); ... _ocrLock.Release();

// IronOCR: direct concurrent access, no guards needed
Parallel.ForEach(documentPaths, path =>
{
    var text = new IronTesseract().Read(path).Text;
    results[path] = text;
});
```

## Asprise OCR Migration Checklist

### Pre-Migration Tasks

Audit the codebase for all Asprise usage before writing any replacement code:

```bash
# Find all files importing asprise namespace
grep -rn "using asprise" --include="*.cs" .

# Find all engine lifecycle calls
grep -rn "SetUp\|StartEngine\|StopEngine" --include="*.cs" .

# Find all integer constant references
grep -rn "RECOGNIZE_TYPE\|OUTPUT_FORMAT\|SPEED_FAST\|SPEED_SLOW" --include="*.cs" .

# Find native binary references in project files and deployment scripts
grep -rn "aocr\|libaocr" --include="*.csproj" --include="Dockerfile" --include="*.yml" .

# Find XML output parsing code
grep -rn "OUTPUT_FORMAT_XML\|XDocument.Parse\|Descendants.*word" --include="*.cs" .

# Find Task.Run wrappers around OCR calls
grep -rn "Task.Run.*ocr\|Task.Run.*Recognize" --include="*.cs" .
```

Inventory the results:
- Count files importing `asprise.ocr` — these all need namespace updates
- List every `StartEngine` call site — each becomes a `Read` call
- Identify XML output parsing code — each block needs object model replacement
- Note any license tier guards or serialization wrappers — these are removable
- Locate native binary deployment scripts and container configuration

### Code Update Tasks

1. Remove the `asprise-ocr-api` NuGet package from all projects
2. Install `IronOcr` NuGet package in each project that performs OCR
3. Replace `using asprise.ocr` with `using IronOcr` in all files
4. Add `IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY"` at application startup
5. Remove `Ocr.SetUp()` calls from all startup and initialization code
6. Replace every `Ocr.StartEngine` / `Recognize` / `StopEngine` block with `new IronTesseract().Read(path).Text`
7. Replace `OUTPUT_FORMAT_XML` parsing blocks with `result.Pages` object traversal
8. Replace `OUTPUT_FORMAT_PDF` workarounds with `result.SaveAsSearchablePdf(path)`
9. Replace multi-frame TIFF splitting code with `input.LoadImageFrames(tiffPath)`
10. Replace stream-based `Task.Run` wrappers with `await ocr.ReadAsync(input)`
11. Remove `SemaphoreSlim` or serialization guards that protected Asprise from concurrent use
12. Remove native binary copy instructions from `.csproj` files and Dockerfiles
13. Remove `LD_LIBRARY_PATH` settings from environment configuration and CI scripts
14. Replace string language codes (`"eng"`, `"eng+fra"`) with `OcrLanguage` enum values
15. Replace `result.StartsWith("ERROR:")` checks with `try/catch` blocks

### Post-Migration Testing

- Verify `dotnet build` completes with zero warnings about missing native libraries
- Confirm no `DllNotFoundException` or `BadImageFormatException` occurs on startup in all target environments (Windows, Linux, Docker)
- Run OCR on a representative image and confirm text output matches pre-migration baseline
- Test multi-frame TIFF processing and verify all pages are returned with correct page numbers
- Generate a searchable PDF and verify text is selectable and searchable in a PDF viewer
- Send concurrent HTTP requests to any API endpoint that calls OCR and confirm all requests complete without errors
- Verify async endpoints return results without deadlocking under concurrent load
- Confirm structured data extraction (word coordinates and confidence) produces correct output on a known document
- Check application memory usage over time to confirm no native memory leaks (previously caused by missed `StopEngine()` calls)
- Run the application under Linux or in a Docker container to confirm cross-platform deployment works without binary configuration

## Key Benefits of Migrating to IronOCR

**Deployment Reduces to a Single NuGet Reference.** After migration, every deployment target — development workstations, staging servers, Linux containers, CI agents — installs the same package with the same command. There is no platform detection logic, no architecture-specific binary sourcing, and no runtime path configuration. A Docker image that previously required manual native binary COPY instructions now requires nothing beyond `dotnet restore`. The [Linux deployment guide](https://ironsoftware.com/csharp/ocr/get-started/linux/) and [Azure deployment guide](https://ironsoftware.com/csharp/ocr/get-started/azure/) cover environment-specific notes where applicable.

**All License Tiers Unlock Server-Grade Deployment.** The $749 Lite license supports ASP.NET Core Web APIs, Windows Services, Azure Functions, AWS Lambda, and any other multi-threaded .NET workload. The ENTERPRISE-level threading capability that Asprise charges $2,000–$5,000+ for is included at every IronOCR tier. Teams migrating from Asprise ENTERPRISE to IronOCR Lite reduce their OCR licensing cost while gaining capabilities that ENTERPRISE did not provide — native PDF, structured output, searchable PDF generation, and 125 languages.

**Structured OCR Results Replace XML String Parsing.** The `OcrResult` object model exposes a complete document hierarchy: pages, paragraphs, lines, words, and characters, each with pixel-accurate bounding box coordinates and confidence scores. Code that previously parsed Asprise XML strings with XDocument or regex becomes direct property access. The [OCR results features page](https://ironsoftware.com/csharp/ocr/features/ocr-results/) covers coordinate systems and how to filter results by confidence for automated quality gates.

**Built-In Preprocessing Removes External Imaging Dependencies.** The preprocessing pipeline available through `OcrInput` — `Deskew`, `DeNoise`, `Contrast`, `Binarize`, `Sharpen`, `Dilate`, `Erode`, `Scale`, `Invert`, and `DeepCleanBackgroundNoise` — eliminates the external imaging library that Asprise integrations require. Removing that dependency removes a licensing concern, reduces the build footprint, and puts preprocessing configuration directly adjacent to OCR configuration in the same code file. The [preprocessing features page](https://ironsoftware.com/csharp/ocr/features/preprocessing/) and [image quality correction guide](https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/) cover when to apply each filter and the measurable accuracy gains each provides on low-quality scans.

**Native Async and True Parallelism Improve Throughput.** `ReadAsync` integrates into the standard `async/await` pattern without thread pool blocking. Parallel batch processing with `Parallel.ForEach` or PLINQ scales linearly with available cores. A document batch that Asprise LITE/STANDARD forced to run sequentially — 100 documents at 2 seconds each takes over 3 minutes — runs in approximately 25 seconds on an 8-core machine with IronOCR. The [multithreading example](https://ironsoftware.com/csharp/ocr/examples/csharp-tesseract-multithreading-for-speed/) demonstrates parallel throughput patterns and shows how to use `ConcurrentBag` for thread-safe result collection.

**125+ Languages with No Binary Distribution.** Language packs install as NuGet packages — `dotnet add package IronOcr.Languages.Arabic`, `dotnet add package IronOcr.Languages.Japanese` — and deploy with the application like any other dependency. There is no manual tessdata folder to populate, no language binary to locate, and no path configuration required on the target machine. The [languages index](https://ironsoftware.com/csharp/ocr/languages/) lists all 125+ available language packs.
