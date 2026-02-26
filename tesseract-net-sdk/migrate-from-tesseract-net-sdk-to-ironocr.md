# Migrating from Tesseract.NET SDK to IronOCR

This guide walks .NET developers through a concrete migration from Tesseract.NET SDK (`Tesseract.Net.SDK`, namespace `Patagames.Ocr`) to [IronOCR](https://ironsoftware.com/csharp/ocr/). It focuses specifically on teams carrying .NET Framework-era initialization patterns, legacy disposal idioms, and synchronous-only pipelines into a world that now runs on .NET 8, Linux containers, and async-first web frameworks. If your OCR service compiles against `net472` and fails the moment someone adds `<TargetFramework>net8.0</TargetFramework>` to the `.csproj`, this guide is written for you.

## Why Migrate from Tesseract.NET SDK

The Patagames SDK delivered real value when .NET Framework 4.5 was the deployment baseline and Windows Server was the only target. That context has shifted. Most organizations now containerize services, run CI on Linux runners, and standardize on .NET 6, 8, or 9. Tesseract.NET SDK cannot follow them.

**Hard ceiling at .NET Framework 4.5.** The package targets `net20` through `net45`. It produces no `netstandard` or `net6.0` assembly. A project file that includes `Tesseract.Net.SDK` cannot set `<TargetFramework>net8.0</TargetFramework>`. The .NET upgrade that the rest of the codebase completes in a sprint stalls at the OCR layer indefinitely.

**No container path.** The SDK ships Windows-only P/Invoke calls into Windows native binaries. On any Linux base image — `mcr.microsoft.com/dotnet/aspnet:8.0`, `ubuntu:22.04`, `alpine:3.19` — the application throws `DllNotFoundException` before processing a single document. Windows containers exist as a workaround, but they carry larger image sizes, a separate licensing cost, and incompatibility with most managed Kubernetes services that default to Linux node pools.

**Synchronous-only API blocks ASP.NET Core pipelines.** The `OcrApi.GetTextFromImage()` method is synchronous. In ASP.NET Core, calling blocking synchronous operations on request threads degrades throughput under load and risks thread-pool starvation. IronOCR provides `ReadAsync()` for non-blocking integration. See the [async OCR guide](https://ironsoftware.com/csharp/ocr/how-to/async/) for the pattern.

**Per-request engine creation burns memory.** .NET Framework code commonly creates one `OcrApi` instance per method call or per request, disposing it on exit. This is idiomatic .NET Framework lifecycle management. It is also expensive: each `Init()` loads 40–100 MB of language data. Ten concurrent requests load the same language model ten times. IronOCR's `IronTesseract` is thread-safe — one instance lives for the application lifetime and serves all concurrent callers from a single language model load.

**Legacy disposal patterns accumulate risk.** The SDK's correct usage requires a `using (var api = OcrApi.Create()) { ... }` block — the C# 1.0 `using` statement that predates `using var` declarations. Codebases that were written before C# 8.0 often include `try/finally` disposal patterns or, in bug cases, no disposal at all. Those patterns compile and run on .NET Framework but carry technical debt that blocks modern refactoring.

**No async, no DI, no modern startup.** The SDK has no concept of dependency injection integration, hosted service lifetime, or `IOptions<T>` configuration. Wiring it into an ASP.NET Core application requires manual service registration and careful avoidance of per-request instantiation. IronOCR integrates cleanly as a singleton service in the standard DI container.

### The Fundamental Problem

```csharp
// Tesseract.NET SDK: .NET Framework 4.5 ceiling — will not compile on net8.0
// Every project referencing this package is locked below the upgrade line
using Patagames.Ocr;  // Patagames.Ocr targets net45; no netstandard or net8 assembly

public class OcrService
{
    public string ProcessDocument(string imagePath)
    {
        // Synchronous-only — blocks ASP.NET Core request threads
        // No DI support — must be instantiated manually each time
        using (var api = OcrApi.Create())    // C# 1.0 using statement, 40-100MB load per call
        {
            api.Init(Languages.English);
            return api.GetTextFromImage(imagePath);
        }
        // Project cannot target net6.0, net8.0, or any Linux container base image
    }
}
```

```csharp
// IronOCR: same logic, any runtime from net462 to net9.0, any platform
using IronOcr;  // Single NuGet, supports .NET Framework 4.6.2+, .NET 5/6/7/8/9

// Register once as singleton — load language model once, share across all requests
// Call ReadAsync() in ASP.NET Core for non-blocking operation
var ocr = new IronTesseract();
var result = await ocr.ReadAsync("document.jpg");  // Async-first, no thread blocking
Console.WriteLine(result.Text);
```

## IronOCR vs Tesseract.NET SDK: Feature Comparison

The table below maps the capabilities directly relevant to a .NET modernization migration.

| Feature | Tesseract.NET SDK | IronOCR |
|---|---|---|
| .NET Framework 2.0–4.5 | Yes | No |
| .NET Framework 4.6.2–4.8 | No | Yes |
| .NET Core 2.x / 3.x | No | Yes |
| .NET 5 | No | Yes |
| .NET 6 | No | Yes |
| .NET 7 | No | Yes |
| .NET 8 | No | Yes |
| .NET 9 | No | Yes |
| Windows deployment | Yes | Yes |
| Linux deployment | No | Yes |
| macOS deployment | No | Yes |
| Docker Linux containers | No | Yes |
| Azure App Service (Linux) | No | Yes |
| AWS Lambda | No | Yes |
| Async API (`ReadAsync`) | No | Yes |
| Thread-safe single instance | No | Yes |
| ASP.NET Core DI integration | Manual | Singleton service |
| Native PDF input | No | Yes |
| Built-in preprocessing | No | Yes |
| Searchable PDF output | No | Yes |
| Structured data (words, lines, paragraphs) | No | Yes |
| Commercial support / SLA | No (individual developer) | Yes |
| Perpetual license price | ~$20–50 (single developer) | From $749 |

## Quick Start: Tesseract.NET SDK to IronOCR Migration

### Step 1: Replace NuGet Package

Remove Tesseract.NET SDK:

```bash
dotnet remove package Tesseract.Net.SDK
```

If PdfiumViewer or a similar PDF rendering library was installed solely to feed PDF pages to the SDK, remove it as well — IronOCR reads PDFs natively:

```bash
dotnet remove package PdfiumViewer
```

Install IronOCR from [NuGet](https://www.nuget.org/packages/IronOcr):

```bash
dotnet add package IronOcr
```

### Step 2: Update Namespaces

```csharp
// Before (Tesseract.NET SDK)
using Patagames.Ocr;
using Patagames.Ocr.Enums;

// After (IronOCR)
using IronOcr;
```

### Step 3: Initialize License

Add the license key call once at application startup — in `Program.cs`, `Startup.cs`, or the application host builder:

```csharp
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
```

A [free trial license](https://ironsoftware.com/csharp/ocr/licensing/) is available for evaluation without watermarks.

## Code Migration Examples

### .NET Framework Startup Pattern to Modern Host Builder

.NET Framework applications typically initialize the OCR engine in a static constructor, an `Application_Start` event, or a `Global.asax` handler. None of these exist in .NET 6+ applications built on the generic host model.

**Tesseract.NET SDK Approach:**

```csharp
// Global.asax.cs — .NET Framework MVC application
// OcrApi lifecycle managed manually; no DI container involved
public class MvcApplication : System.Web.HttpApplication
{
    // Static field — one engine for the app lifetime
    // But: NOT thread-safe; concurrent requests share a single OcrApi instance
    private static OcrApi _globalApi;

    protected void Application_Start()
    {
        // Initialize OCR engine on app startup
        // Path to tessdata hardcoded for deployment environment
        _globalApi = OcrApi.Create();
        _globalApi.Init(Languages.English);

        AreaRegistration.RegisterAllAreas();
        RouteConfig.RegisterRoutes(RouteTable.Routes);
    }

    protected void Application_End()
    {
        // Must manually dispose on shutdown
        _globalApi?.Dispose();
    }
}
```

**IronOCR Approach:**

```csharp
// Program.cs — .NET 8 ASP.NET Core application
// IronTesseract is thread-safe; register as singleton, inject where needed
var builder = WebApplication.CreateBuilder(args);

IronOcr.License.LicenseKey = builder.Configuration["IronOcr:LicenseKey"];

// Register as singleton — one instance, thread-safe, shared across all requests
builder.Services.AddSingleton<IronTesseract>();

builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

The `Global.asax` pattern disappears entirely. `IronTesseract` registers as a standard singleton service, injected into controllers and services through the constructor. The language model loads once at first use and remains in memory for the application lifetime. The [IronTesseract setup guide](https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/) covers configuration options including language selection and engine mode at registration time.

### Legacy Disposal Pattern Modernization

.NET Framework 2.0 code uses the `using (var x = ...) { }` block statement. C# 8.0 introduced `using var` declarations that scope disposal to the enclosing block. Older codebases also carry `try/finally` disposal guards written when `using` statements were not trusted in all scenarios. All of these patterns indicate code written for .NET Framework and should be modernized during migration.

**Tesseract.NET SDK Approach:**

```csharp
// .NET Framework 4.x disposal patterns — three variants encountered in production
public class LegacyOcrProcessor
{
    // Pattern 1: try/finally guard (pre-C# 2.0 style, still common in legacy code)
    public string ProcessWithTryFinally(string imagePath)
    {
        OcrApi api = null;
        try
        {
            api = OcrApi.Create();
            api.Init(Languages.English);
            return api.GetTextFromImage(imagePath);
        }
        finally
        {
            if (api != null)
                api.Dispose();  // Manual null check required
        }
    }

    // Pattern 2: nested using blocks — one for engine, one for image object
    public string ProcessWithNestedUsing(string imagePath)
    {
        using (var api = OcrApi.Create())
        {
            api.Init(Languages.English);
            using (var img = OcrImage.FromFile(imagePath))
            {
                api.SetImage(img);
                return api.GetText();
            }   // img disposed here
        }       // api disposed here — nested indentation grows with each resource
    }

    // Pattern 3: missing disposal — memory leak, common in older service code
    public string ProcessUnsafe(string imagePath)
    {
        var api = OcrApi.Create();   // WARNING: never disposed
        api.Init(Languages.English);
        return api.GetTextFromImage(imagePath);
    }
}
```

**IronOCR Approach:**

```csharp
// Modern C# 8.0+ disposal — flat, readable, no nesting
public class ModernOcrProcessor
{
    private readonly IronTesseract _ocr;  // Injected singleton, never disposed per-request

    public ModernOcrProcessor(IronTesseract ocr) => _ocr = ocr;

    // Pattern 1: using var declaration — scoped to method, no nesting
    public string ProcessDocument(string imagePath)
    {
        using var input = new OcrInput();  // OcrInput is the disposable resource, not the engine
        input.LoadImage(imagePath);
        return _ocr.Read(input).Text;
    }   // input disposed here automatically — no nesting, no try/finally

    // Pattern 2: multiple inputs in one scope — still flat
    public string ProcessMultipleInputs(string imagePath, string pdfPath)
    {
        using var imageInput = new OcrInput();
        imageInput.LoadImage(imagePath);

        using var pdfInput = new OcrInput();
        pdfInput.LoadPdf(pdfPath);

        var imageText = _ocr.Read(imageInput).Text;
        var pdfText = _ocr.Read(pdfInput).Text;

        return $"{imageText}\n{pdfText}";
    }   // both inputs disposed here — zero nesting
}
```

`OcrInput` is the only disposable resource in IronOCR. The engine itself (`IronTesseract`) is not disposed per-request — it is a singleton. This eliminates the per-request 40–100 MB language model reload that `OcrApi.Create()` + `api.Init()` imposed. The [image input guide](https://ironsoftware.com/csharp/ocr/how-to/input-images/) covers all `OcrInput` loading methods including streams, byte arrays, and URLs.

### Async Integration for ASP.NET Core Controllers

Tesseract.NET SDK has no async API. Every call is synchronous. In ASP.NET Core, calling synchronous blocking operations from async controller actions is a thread-pool starvation risk under load. The common workaround — wrapping synchronous calls in `Task.Run()` — offloads the blocking work to a thread-pool thread but does not eliminate the thread consumption. IronOCR's `ReadAsync()` provides genuine async I/O integration.

**Tesseract.NET SDK Approach:**

```csharp
// ASP.NET Core controller — forced workaround for synchronous OCR API
[ApiController]
[Route("api/ocr")]
public class OcrController : ControllerBase
{
    [HttpPost("extract")]
    public async Task<IActionResult> ExtractText(IFormFile file)
    {
        // Must copy upload to temp file — OcrApi does not accept streams directly
        var tempPath = Path.GetTempFileName();
        await using (var stream = System.IO.File.OpenWrite(tempPath))
            await file.CopyToAsync(stream);

        string text;
        try
        {
            // Task.Run wraps synchronous call — still consumes a thread-pool thread
            // Does NOT free the calling thread during OCR processing
            text = await Task.Run(() =>
            {
                using (var api = OcrApi.Create())   // 40-100MB load per request
                {
                    api.Init(Languages.English);
                    return api.GetTextFromImage(tempPath);  // synchronous, blocking
                }
            });
        }
        finally
        {
            System.IO.File.Delete(tempPath);  // Manual temp file cleanup
        }

        return Ok(new { text });
    }
}
```

**IronOCR Approach:**

```csharp
// ASP.NET Core controller — genuine async OCR, no temp files, no thread blocking
[ApiController]
[Route("api/ocr")]
public class OcrController : ControllerBase
{
    private readonly IronTesseract _ocr;  // Singleton injected via DI

    public OcrController(IronTesseract ocr) => _ocr = ocr;

    [HttpPost("extract")]
    public async Task<IActionResult> ExtractText(IFormFile file)
    {
        // Load stream directly — no temp file needed
        using var input = new OcrInput();
        input.LoadImage(file.OpenReadStream());  // Stream input, no disk write

        // ReadAsync — genuinely non-blocking, integrates with ASP.NET Core pipeline
        var result = await _ocr.ReadAsync(input);

        return Ok(new
        {
            text = result.Text,
            confidence = result.Confidence
        });
    }
}
```

The temp file round-trip disappears. The `Task.Run` wrapper disappears. The per-request `OcrApi.Create()` and the 40–100 MB load that followed it disappear. The [async OCR how-to](https://ironsoftware.com/csharp/ocr/how-to/async/) and the [stream input guide](https://ironsoftware.com/csharp/ocr/how-to/input-streams/) document the complete async pipeline including cancellation token support.

### Multi-Frame TIFF Processing

The Phase 1 comparison article covered basic image and PDF processing. Multi-frame TIFF is a distinct scenario common in document archiving, fax systems, and medical imaging pipelines. Tesseract.NET SDK requires manually iterating TIFF frames using `System.Drawing.Bitmap`, extracting each frame to a temporary PNG file, running OCR on the temp file, and cleaning up. The pattern forces explicit GC calls on large documents to avoid out-of-memory errors.

**Tesseract.NET SDK Approach:**

```csharp
// Multi-frame TIFF: manual frame extraction to temp files + forced GC
using System.Drawing;
using System.Drawing.Imaging;
using Patagames.Ocr;

public List<string> ProcessMultiFrameTiff(string tiffPath)
{
    var pageTexts = new List<string>();

    using (var api = OcrApi.Create())
    {
        api.Init(Languages.English);

        using (var bitmap = new Bitmap(tiffPath))
        {
            var dimension = new FrameDimension(bitmap.FrameDimensionsList[0]);
            int frameCount = bitmap.GetFrameCount(dimension);

            for (int i = 0; i < frameCount; i++)
            {
                bitmap.SelectActiveFrame(dimension, i);

                // Must write each frame to a temp file — no in-memory path
                var tempPath = Path.GetTempFileName() + ".png";
                bitmap.Save(tempPath, ImageFormat.Png);

                try
                {
                    pageTexts.Add(api.GetTextFromImage(tempPath));
                }
                finally
                {
                    File.Delete(tempPath);  // Manual cleanup on every frame
                }

                // Force GC every 10 frames — workaround for memory pressure
                // Slows processing; indicates memory management is manual
                if (i % 10 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
        }
    }

    return pageTexts;
}
```

**IronOCR Approach:**

```csharp
// Multi-frame TIFF: one method call, no temp files, no manual GC
using IronOcr;

public List<string> ProcessMultiFrameTiff(string tiffPath)
{
    var ocr = new IronTesseract();

    using var input = new OcrInput();
    input.LoadImageFrames(tiffPath);  // Loads all frames natively — no temp files

    var result = ocr.Read(input);

    // Pages map directly to TIFF frames
    return result.Pages.Select(page => page.Text).ToList();
}
```

Thirty lines collapse to eight. No temp files, no `Bitmap` frame iteration, no `GC.Collect()` calls. `LoadImageFrames` handles arbitrarily large multi-frame TIFFs without writing intermediate files. The [TIFF and GIF input guide](https://ironsoftware.com/csharp/ocr/how-to/input-tiff-gif/) covers selective frame loading (by index range) and progress callbacks for large documents.

### Docker Container Deployment Preparation

Tesseract.NET SDK code that runs on a developer's Windows machine fails at the Docker build or run step when the base image is Linux. The fix is not a Dockerfile tweak — the native binaries are Windows-only and cannot be loaded on Linux at all. IronOCR's Linux support requires a small `apt-get` addition to the Dockerfile and nothing else in the application code.

**Tesseract.NET SDK Approach:**

```dockerfile
# Dockerfile attempt — fails at runtime on Linux base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
# This base image is Linux (Debian) by default
# Tesseract.Net.SDK's Windows native DLLs cannot load here
# Application throws DllNotFoundException on first OCR call

WORKDIR /app
COPY --from=build /app/publish .

# Even copying the Windows tessdata folder has no effect —
# the P/Invoke DLL cannot be loaded regardless of file placement
COPY tessdata/ ./tessdata/

ENTRYPOINT ["dotnet", "MyApp.dll"]
# Runtime error: DllNotFoundException: Unable to load DLL 'libtesseract'
# No fix available within Tesseract.Net.SDK — requires replacing the library
```

**IronOCR Approach:**

```dockerfile
# Dockerfile for IronOCR on Linux — add one apt-get line, nothing else changes
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Required system dependency for IronOCR on Debian/Ubuntu base images
RUN apt-get update && apt-get install -y libgdiplus \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

# No tessdata folder — language data is bundled with the IronOcr NuGet packages
# No platform check code — IronOCR runs identically on Windows and Linux

ENTRYPOINT ["dotnet", "MyApp.dll"]
```

One `apt-get` line. No tessdata folder. No platform-conditional code in the application. The same application binary that runs on a developer's Windows machine runs in this Linux container unchanged. The [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/) covers Alpine-based images (which use `apk` instead of `apt-get`), multi-stage build optimization, and environment variable configuration for the license key. The [Linux deployment guide](https://ironsoftware.com/csharp/ocr/get-started/linux/) covers bare-metal Linux and WSL2 scenarios.

## Tesseract.NET SDK API to IronOCR Mapping Reference

| Tesseract.NET SDK | IronOCR Equivalent | Notes |
|---|---|---|
| `Install-Package Tesseract.Net.SDK` | `dotnet add package IronOcr` | IronOCR targets .NET Framework 4.6.2+ and .NET 5–9 |
| `using Patagames.Ocr;` | `using IronOcr;` | Single namespace |
| `using Patagames.Ocr.Enums;` | (not needed) | Enums are in the `IronOcr` namespace |
| `OcrApi.Create()` | `new IronTesseract()` | IronTesseract is thread-safe; use as singleton |
| `api.Init(Languages.English)` | `ocr.Language = OcrLanguage.English` | Property assignment, not method call |
| `api.Init(Languages.English \| Languages.German)` | `ocr.Language = OcrLanguage.English + OcrLanguage.German` | Operator `+`, not bitwise OR |
| `api.GetTextFromImage(path)` | `ocr.Read("path.jpg").Text` | Direct or via `OcrInput` |
| `api.GetTextFromImage(path)` (async) | `await ocr.ReadAsync(input)` | Genuine async — no `Task.Run` wrapper needed |
| `OcrImage.FromFile(path)` | `input.LoadImage(path)` | `OcrInput` replaces `OcrImage` |
| `OcrImage.FromBitmap(bitmap)` | `input.LoadImage(bitmap)` | |
| `new MemoryStream(bytes)` → `OcrImage.FromBitmap` | `input.LoadImage(bytes)` | Direct byte array support |
| `api.SetImage(img); api.GetText()` | `ocr.Read(input).Text` | `OcrInput` passed to `Read` |
| `api.GetMeanConfidence()` | `result.Confidence` | Returns percentage; also available per-word |
| `api.SetRectangle(x, y, w, h)` | `input.LoadImage(path, new CropRectangle(x, y, w, h))` | Region-based OCR via `CropRectangle` |
| `api.SetVariable("tessedit_char_whitelist", x)` | `ocr.Configuration.WhiteListCharacters = x` | |
| `api.SetVariable("tessedit_char_blacklist", x)` | `ocr.Configuration.BlackListCharacters = x` | |
| Bitmap frame iteration + temp file | `input.LoadImageFrames(tiffPath)` | Native multi-frame TIFF support |
| (synchronous only) | `result.SaveAsSearchablePdf("out.pdf")` | No equivalent in Tesseract.NET SDK |
| (no structured output) | `result.Pages`, `result.Words`, `result.Lines` | Word-level coordinates and confidence |
| `GC.Collect()` workarounds | (not needed) | IronOCR manages memory internally |
| Platform check: `IsOSPlatform(Windows)` | (remove entirely) | IronOCR is cross-platform |
| Tessdata folder management | (remove entirely) | Languages bundled with NuGet packages |

## Common Migration Issues and Solutions

### Issue 1: Project Target Framework Conflict

**Tesseract.NET SDK:** After removing `Tesseract.Net.SDK` and adding `IronOcr`, the project still targets `net45` or `net472` from the old requirement. IronOCR supports `net462` and later, so `net45` projects need the target framework updated before the package will restore cleanly.

**Solution:** Update the `<TargetFramework>` in the `.csproj` file before adding IronOCR. If the project must support both old and new runtimes during a phased migration, use multi-targeting:

```xml
<!-- Single modern target (preferred) -->
<TargetFramework>net8.0</TargetFramework>

<!-- Multi-targeting during phased migration — supports both simultaneously -->
<TargetFrameworks>net462;net8.0</TargetFrameworks>
```

IronOCR resolves the correct assembly for each target automatically. The same `dotnet add package IronOcr` command works for both. The [.NET OCR library page](https://ironsoftware.com/csharp/ocr/use-case/net-ocr-library/) lists all supported target frameworks.

### Issue 2: Static OcrApi Field Replaced by DI Singleton

**Tesseract.NET SDK:** Legacy code registers a single `OcrApi` instance as a static field (in `Global.asax`, a static service locator, or a singleton wrapper class). This pattern was necessary because `OcrApi` is not thread-safe — sharing one instance across threads causes race conditions, so the static field was protected by a lock or was actually re-created per request despite the field name.

**Solution:** Register `IronTesseract` as a genuine thread-safe singleton through the DI container. Remove the lock, remove the static field, remove any per-request re-creation:

```csharp
// Remove: private static OcrApi _instance; / private static readonly object _lock = new();

// Replace with DI registration in Program.cs
builder.Services.AddSingleton<IronTesseract>();

// In consuming classes — constructor injection
public class DocumentProcessor
{
    private readonly IronTesseract _ocr;
    public DocumentProcessor(IronTesseract ocr) => _ocr = ocr;

    public async Task<string> ProcessAsync(string path)
    {
        using var input = new OcrInput();
        input.LoadImage(path);
        var result = await _ocr.ReadAsync(input);
        return result.Text;
    }
}
```

### Issue 3: Tessdata Folder Missing After Deployment

**Tesseract.NET SDK:** After switching to IronOCR, teams sometimes leave tessdata deployment steps in CI/CD pipelines. The `tessdata/` folder referenced in build scripts and deployment manifests no longer exists — it was part of the old SDK's language model management. The scripts fail when they try to copy or verify a folder that is no longer present.

**Solution:** Remove all tessdata references from deployment scripts, `.csproj` copy targets, Docker COPY commands, and CI/CD pipeline steps. IronOCR language data travels with the NuGet packages. Run `dotnet restore` and the language data is available. Nothing else is needed:

```bash
# Remove from CI/CD pipeline
# BEFORE (delete these lines):
# - cp -r tessdata/ $DEPLOY_PATH/tessdata/
# - test -f $DEPLOY_PATH/tessdata/eng.traineddata

# AFTER: nothing — language data is in the NuGet package restore output
dotnet restore   # Downloads IronOcr and any IronOcr.Languages.* packages
dotnet publish   # Includes language data automatically
```

The [multiple languages guide](https://ironsoftware.com/csharp/ocr/how-to/ocr-multiple-languages/) covers installing specific language packs as NuGet packages for offline/airgapped deployments.

### Issue 4: `BadImageFormatException` on 32/64-bit Mismatch

**Tesseract.NET SDK:** The SDK ships separate x86 and x64 Windows native binaries. Projects targeting `AnyCPU` sometimes resolve to the wrong binary depending on the process architecture. The error surfaces as `BadImageFormatException` or `DllNotFoundException` at runtime on machines where the process architecture does not match the native DLL in the output folder.

**Solution:** IronOCR bundles the correct native binary for each platform within the NuGet package and resolves the right binary automatically via the `runtimes/` folder in the package layout. No `Platform` target setting, no architecture-conditional copy commands, no `x86`/`x64` subfolders to manage:

```xml
<!-- Remove architecture-specific build configurations from .csproj -->
<!-- BEFORE: Conditional native DLL copy based on Platform target -->
<!--
<ItemGroup Condition="'$(Platform)' == 'x64'">
  <Content Include="$(SolutionDir)libs\x64\*.dll">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </Content>
</ItemGroup>
-->

<!-- AFTER: Nothing. IronOCR resolves the correct binary automatically. -->
```

### Issue 5: Configuration String Migration

**Tesseract.NET SDK:** Tesseract engine variables are set via `api.SetVariable(string name, string value)` using raw string keys from the Tesseract API reference (e.g., `"tessedit_char_whitelist"`, `"tessedit_pageseg_mode"`). These are untyped strings with no IDE completion. Typos cause silent failures — the variable is ignored, not an exception.

**Solution:** IronOCR exposes engine configuration as typed properties on `ocr.Configuration`. Typos become compile-time errors:

```csharp
// Before: untyped string variables, silent failures on typos
api.SetVariable("tessedit_char_whitelist", "0123456789");
api.SetVariable("tessedit_pageseg_mode", "7");

// After: typed properties, compile-time validation, IDE completion
ocr.Configuration.WhiteListCharacters = "0123456789";
ocr.Configuration.PageSegmentationMode = TesseractPageSegmentationMode.SingleLine;
```

The [IronTesseract API reference](https://ironsoftware.com/csharp/ocr/object-reference/api/IronOcr.IronTesseract.html) documents all configuration properties with their types and accepted values.

### Issue 6: Progress Reporting for Long Batch Jobs

**Tesseract.NET SDK:** Batch processing code that reports progress using `IProgress<T>` works at the job level (increment a counter after each file) but cannot report within a single document — there is no callback mechanism inside `GetTextFromImage()`. For a 500-page document, the progress bar is stuck until the entire document finishes.

**Solution:** IronOCR provides built-in progress tracking via the `OcrProgress` event on `OcrInput`. Progress fires per page, enabling accurate progress bars for long multi-page documents:

```csharp
// IronOCR: page-level progress tracking for multi-page documents
using IronOcr;

var ocr = new IronTesseract();

using var input = new OcrInput();
input.LoadPdf("large-archive.pdf");

// Subscribe to page-level progress events
input.OcrProgress += (sender, e) =>
{
    Console.WriteLine($"Processing page {e.CurrentPage} of {e.TotalPages} " +
                      $"({e.ProgressPercent:F0}%)");
};

var result = ocr.Read(input);
Console.WriteLine($"Complete: {result.Pages.Count} pages extracted");
```

The [progress tracking guide](https://ironsoftware.com/csharp/ocr/how-to/progress-tracking/) covers integration with ASP.NET Core SignalR for real-time progress push to browser clients.

## Tesseract.NET SDK Migration Checklist

### Pre-Migration Tasks

Audit the codebase for all Tesseract.NET SDK usage before touching any code:

```bash
# Find all files referencing Patagames namespace
grep -rl "Patagames" --include="*.cs" .

# Find all OcrApi instantiation points
grep -rn "OcrApi.Create" --include="*.cs" .

# Find tessdata references in project and build files
grep -rn "tessdata" --include="*.cs" --include="*.csproj" --include="*.yaml" --include="*.yml" .

# Find platform guard checks that can be removed after migration
grep -rn "IsOSPlatform.*Windows" --include="*.cs" .

# Find Task.Run wrappers around synchronous OCR calls
grep -rn "Task.Run" --include="*.cs" . | grep -i "ocr\|image\|text"

# Count distinct OcrApi.Create() call sites to estimate migration scope
grep -c "OcrApi.Create" $(find . -name "*.cs")
```

Document the count of `OcrApi.Create()` call sites — each is a candidate for singleton injection replacement. Note any `try/finally` disposal patterns for modernization. Identify any `Global.asax`, `Application_Start`, or static constructor initialization that will move to `Program.cs`.

### Code Update Tasks

1. Update `<TargetFramework>` to `net8.0` (or the target modern runtime) in all `.csproj` files
2. Run `dotnet remove package Tesseract.Net.SDK` in each project
3. Run `dotnet remove package PdfiumViewer` (or equivalent PDF rendering package) if present
4. Run `dotnet add package IronOcr` in each project
5. Add `IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";` to `Program.cs` or the host builder
6. Register `IronTesseract` as a singleton in the DI container: `services.AddSingleton<IronTesseract>()`
7. Replace all `using Patagames.Ocr;` and `using Patagames.Ocr.Enums;` with `using IronOcr;`
8. Replace `OcrApi.Create()` + `api.Init(Languages.X)` with constructor-injected `IronTesseract`
9. Replace `using (var api = OcrApi.Create()) { ... }` blocks with `using var input = new OcrInput()` declarations
10. Replace `api.GetTextFromImage(path)` with `ocr.Read(input).Text` or `await ocr.ReadAsync(input)`
11. Replace `Task.Run(() => { /* synchronous OCR */ })` with direct `await ocr.ReadAsync(input)`
12. Replace `api.GetMeanConfidence()` with `result.Confidence`
13. Replace Bitmap frame-iteration TIFF loops with `input.LoadImageFrames(tiffPath)`
14. Replace `api.SetVariable("tessedit_char_whitelist", x)` with `ocr.Configuration.WhiteListCharacters = x`
15. Delete tessdata folder from project, remove all deployment script references to tessdata

### Post-Migration Testing

- Compile the project targeting `net8.0` and confirm no `Patagames` references remain in build output
- Run the application on a Linux host or Linux Docker container and confirm no `DllNotFoundException`
- Verify OCR text output matches pre-migration output on a representative sample of production documents (10–20 documents)
- Test multi-page TIFF processing and confirm page count matches the original frame count
- Run load tests on the ASP.NET Core endpoints using `ReadAsync()` and verify thread-pool metrics show no blocking
- Confirm the DI container resolves `IronTesseract` as a singleton (same instance across requests)
- Verify CI/CD pipeline completes without errors now that tessdata copy steps are removed
- Test Docker image build and container run on a Linux base image
- Confirm progress events fire correctly on a multi-page document (PDF or TIFF)
- Verify confidence scores are in the expected range for known-good documents

## Key Benefits of Migrating to IronOCR

**The .NET upgrade blocker is gone.** Before migration, any plan to move the service from .NET Framework 4.x to .NET 8 stopped at the OCR layer. After migration, the OCR service compiles and runs on .NET Framework 4.6.2, .NET 6, .NET 8, and .NET 9 from the same package reference. The upgrade path is unblocked. Teams that were maintaining a separate legacy-runtime deployment for OCR alone can consolidate onto a single modern runtime target.

**Container deployment works without compromise.** The `DllNotFoundException` on Linux base images is eliminated. The same application binary that runs on a developer's Windows workstation runs inside a Debian or Alpine container with one `apt-get` line in the Dockerfile. Kubernetes deployments, Azure Container Apps, and AWS ECS tasks on Linux node pools all work without Windows container licensing, larger image sizes, or architecture-conditional code paths. The [Docker deployment guide](https://ironsoftware.com/csharp/ocr/get-started/docker/) and [Azure guide](https://ironsoftware.com/csharp/ocr/get-started/azure/) document the exact configuration for each target environment.

**Async-first pipelines eliminate thread-pool pressure.** The `Task.Run` workaround that wrapped synchronous OCR in an async method is replaced by `ReadAsync()`. ASP.NET Core request threads are freed during OCR processing rather than blocked. Under high concurrency, this translates directly to higher request throughput and lower latency for the entire application, not just the OCR endpoints.

**Memory consumption drops proportionally with concurrency.** A service that previously created one `OcrApi` instance per concurrent request — each loading 40–100 MB of language data — now loads that data once into a singleton `IronTesseract` instance. At ten concurrent requests, the difference is 400–1000 MB versus a single fixed load. This reduction is immediately visible in container resource metrics and enables smaller pod memory limits, higher pod density, and lower cloud infrastructure cost.

**Modern C# patterns replace .NET Framework ceremony.** The `try/finally` disposal guards, the nested `using` blocks, the `GC.Collect()` calls between TIFF frames — all of these disappear. `using var input = new OcrInput()` is the entire resource management pattern. Code reviews are shorter. Onboarding new developers to the OCR service takes less time. The [OcrResult API reference](https://ironsoftware.com/csharp/ocr/object-reference/api/IronOcr.OcrResult.html) documents the full result object model including structured data, confidence scores, and searchable PDF output that replace manual result handling patterns from the legacy SDK.

**Commercial support replaces single-developer dependency.** Tesseract.NET SDK is operated by an individual developer with no SLA and no organizational continuity guarantee. IronOCR is developed by Iron Software, a commercial entity with dedicated support channels, documented security disclosure processes, and licensing terms that satisfy enterprise procurement requirements. The [IronOCR licensing page](https://ironsoftware.com/csharp/ocr/licensing/) covers support tiers and the perpetual license model (from $749) that replaces both the Patagames SDK fee and the hidden cost of maintaining Windows-only infrastructure on a modernizing .NET stack.
