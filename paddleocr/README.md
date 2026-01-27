# PaddleOCR for .NET: Complete Developer Guide (2026)

*By [Jacob Mellor](https://ironsoftware.com/about-us/authors/jacobmellor/), CTO of Iron Software*

PaddleOCR represents a different approach to OCR in the .NET ecosystem. Built by Baidu as a Python-first deep learning OCR system, it offers state-of-the-art accuracy for certain use cases, particularly Chinese text recognition. The .NET wrapper, Sdcb.PaddleOCR, brings these deep learning models to C# developers, but the journey from Python to .NET comes with significant complexity that teams must evaluate carefully.

In this guide, I'll break down everything you need to know about PaddleOCR for .NET development: the deep learning paradigm shift, installation complexities, GPU configuration requirements, data sovereignty considerations, and when this library makes sense versus simpler alternatives like [IronOCR](https://ironsoftware.com/csharp/ocr/).

## Table of Contents

1. [What Is PaddleOCR?](#what-is-paddleocr)
2. [Deep Learning vs Traditional OCR](#deep-learning-vs-traditional-ocr)
3. [Technical Details](#technical-details)
4. [Installation and Setup](#installation-and-setup)
5. [GPU Configuration](#gpu-configuration)
6. [Basic Usage Examples](#basic-usage-examples)
7. [Key Limitations and Weaknesses](#key-limitations-and-weaknesses)
8. [Data Sovereignty Concerns](#data-sovereignty-concerns)
9. [Pricing Analysis](#pricing-analysis)
10. [PaddleOCR vs IronOCR Comparison](#paddleocr-vs-ironocr-comparison)
11. [Migration Guide: PaddleOCR to IronOCR](#migration-guide-paddleocr-to-ironocr)
12. [When to Use PaddleOCR vs IronOCR](#when-to-use-paddleocr-vs-ironocr)
13. [Code Examples](#code-examples)
14. [References](#references)

---

## What Is PaddleOCR?

PaddleOCR is an open-source OCR system developed by Baidu, China's largest search engine company. The project is built on PaddlePaddle, Baidu's deep learning framework (similar to TensorFlow or PyTorch). While PaddleOCR was designed primarily for Python developers, the .NET community wrapper Sdcb.PaddleOCR makes these deep learning models accessible to C# applications.

The architecture consists of three neural network models working in sequence:

1. **Detection Model (DB)** - Identifies text regions in the image using a differentiable binarization approach
2. **Direction Classifier** - Determines text orientation (0, 90, 180, 270 degrees)
3. **Recognition Model (CRNN)** - Extracts text from detected regions using a recurrent neural network

This three-stage pipeline differs fundamentally from traditional OCR engines like Tesseract, which use a single-pass approach combining image processing, character segmentation, and recognition.

### The Sdcb.PaddleOCR Wrapper

The .NET wrapper is maintained by Zhou Jie (sdcb) on GitHub. It provides:

- .NET bindings for PaddleOCR inference
- ONNX model support (portable across platforms)
- Optional CUDA/GPU acceleration
- Thread-safe API (added in v2.7.0.2)
- Model download helpers

The wrapper translates PaddleOCR's Python API into idiomatic C#, but developers should understand they're working with deep learning models that have different characteristics than traditional OCR engines.

---

## Deep Learning vs Traditional OCR

Understanding the fundamental difference between deep learning OCR (PaddleOCR) and traditional OCR (Tesseract, IronOCR) is essential for making the right technology choice.

### Traditional OCR (Tesseract-Based)

Traditional OCR engines like Tesseract use a deterministic pipeline:

1. **Image preprocessing** - Binarization, noise removal, deskew
2. **Layout analysis** - Identify text blocks, lines, words
3. **Character segmentation** - Split text into individual characters
4. **Feature extraction** - Geometric features, pixel patterns
5. **Classification** - Match features against trained character models
6. **Post-processing** - Dictionary correction, language modeling

This approach is:
- **Predictable** - Same input always produces same output
- **Debuggable** - Each step can be examined individually
- **Efficient** - Runs well on CPU, low memory footprint
- **Mature** - 35+ years of refinement

### Deep Learning OCR (PaddleOCR)

Deep learning OCR uses neural networks trained on millions of images:

1. **Detection network** - Convolutional neural network identifies text regions
2. **Direction network** - Classifies text orientation
3. **Recognition network** - Sequence-to-sequence model outputs text

This approach is:
- **Adaptive** - Can learn complex patterns without explicit programming
- **Better for some scripts** - Excels at Chinese, Japanese, Korean (complex characters)
- **Resource intensive** - Benefits significantly from GPU acceleration
- **Black box** - Difficult to debug why certain recognition fails

### Practical Implications

| Aspect | Traditional OCR | Deep Learning OCR |
|--------|----------------|-------------------|
| Latin text (English, etc.) | Excellent | Good to Excellent |
| Chinese/Japanese/Korean | Good | Excellent |
| Handwriting | Limited | Better |
| CPU performance | Fast | Slower |
| GPU acceleration | Limited benefit | Major benefit |
| Memory usage | 100-200MB | 500MB-2GB+ |
| Model size | 10-50MB | 100-500MB |
| Cold start time | Fast | Slow (model loading) |
| Preprocessing needed | Critical | Less critical |

For most .NET applications processing English documents, traditional OCR with automatic preprocessing (like IronOCR) provides better results with less complexity. Deep learning OCR shines in specialized scenarios: Chinese document processing, complex layouts, or when you've exhausted traditional approaches.

---

## Technical Details

### Package Information

| Property | Value |
|----------|-------|
| **NuGet Package** | Sdcb.PaddleOCR |
| **Current Version** | 3.0.1 |
| **License** | Apache 2.0 (Free) |
| **Maintainer** | Zhou Jie (sdcb) |
| **GitHub** | github.com/sdcb/PaddleSharp |

### Platform Support

| Platform | Support Level |
|----------|--------------|
| Windows x64 | Full support |
| Linux Ubuntu 22.04 x64 | Full support |
| Linux other distros | Requires manual native library setup |
| macOS | Community reported, not officially supported |
| Docker | Requires careful image configuration |
| Azure Functions | Complex due to model loading time |

### Language Support

PaddleOCR supports 14 languages with pre-trained models:

| Language | Model Quality |
|----------|--------------|
| Chinese (Simplified) | Excellent (primary focus) |
| Chinese (Traditional) | Very Good |
| English | Good |
| French | Good |
| German | Good |
| Korean | Very Good |
| Japanese | Very Good |
| Italian | Good |
| Spanish | Good |
| Portuguese | Good |
| Russian | Good |
| Arabic | Good |
| Hindi | Good |
| Tamil | Good |

**Note:** IronOCR supports 125+ languages with optimized Tesseract 5 LSTM models. If you need languages beyond PaddleOCR's 14, you'll need to look elsewhere.

### Required NuGet Packages

A complete PaddleOCR installation requires multiple packages:

```xml
<!-- Core OCR package -->
<PackageReference Include="Sdcb.PaddleOCR" Version="3.0.1" />

<!-- Runtime (choose ONE based on platform) -->
<!-- CPU-only (recommended for most users) -->
<PackageReference Include="Sdcb.PaddleInference.runtime.win64.mkl" Version="3.0.0" />

<!-- OR GPU with CUDA 11.8 -->
<PackageReference Include="Sdcb.PaddleInference.runtime.win64.cuda118" Version="3.0.0" />

<!-- Models (download separately or use model downloader) -->
<PackageReference Include="Sdcb.PaddleOCR.Models.Online" Version="3.0.1" />
```

---

## Installation and Setup

Setting up PaddleOCR for .NET is significantly more complex than traditional OCR libraries. Plan for 30-60 minutes for a first-time setup, longer if configuring GPU support.

### Step 1: Create Project and Add Core Package

```bash
dotnet new console -n PaddleOcrDemo
cd PaddleOcrDemo
dotnet add package Sdcb.PaddleOCR
```

### Step 2: Choose and Install Runtime

**Option A: CPU Runtime (Recommended for Most Users)**

```bash
# Windows x64 with Intel MKL acceleration
dotnet add package Sdcb.PaddleInference.runtime.win64.mkl

# Linux Ubuntu 22.04
dotnet add package Sdcb.PaddleInference.runtime.linux64
```

**Option B: GPU Runtime (Advanced Users Only)**

```bash
# Requires CUDA 11.8 and cuDNN 8.6+ installed separately
dotnet add package Sdcb.PaddleInference.runtime.win64.cuda118
```

See the [GPU Configuration](#gpu-configuration) section for CUDA/cuDNN setup details.

### Step 3: Model Download Strategy

PaddleOCR models are not bundled with the NuGet package. You have three options:

**Option A: Online Model Download (Simplest)**

```bash
dotnet add package Sdcb.PaddleOCR.Models.Online
```

```csharp
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.Online;

// Downloads models on first use (~100MB total)
var models = await OnlineFullModels.ChineseV4.DownloadAsync();
```

**Option B: Pre-download Models (Production Recommended)**

Download models manually and bundle with your application:

```csharp
// Models should be in: models/ch_PP-OCRv4_det_infer/
//                      models/ch_PP-OCRv4_rec_infer/
//                      models/ch_ppocr_mobile_v2.0_cls_infer/

var models = new FullOcrModel(
    LocalDetectionModel.FromDirectory("models/ch_PP-OCRv4_det_infer"),
    LocalClassificationModel.FromDirectory("models/ch_ppocr_mobile_v2.0_cls_infer"),
    LocalRecognitionModel.FromDirectory("models/ch_PP-OCRv4_rec_infer")
);
```

**Option C: Embedded Model Package**

```bash
dotnet add package Sdcb.PaddleOCR.Models.LocalV4
```

Note: This adds ~100MB to your deployment size.

### Step 4: Verify Installation

```csharp
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.Online;
using OpenCvSharp;

class Program
{
    static async Task Main()
    {
        // Download models (first run only)
        var models = await OnlineFullModels.ChineseV4.DownloadAsync();

        // Create OCR instance
        using var ocr = new PaddleOcrAll(models)
        {
            AllowRotateDetection = true,
            Enable180Classification = true
        };

        // Test with an image
        using var mat = Cv2.ImRead("test-image.png");
        var result = ocr.Run(mat);

        Console.WriteLine($"Detected {result.Regions.Length} text regions");
        foreach (var region in result.Regions)
        {
            Console.WriteLine($"  {region.Text} (confidence: {region.Score:P0})");
        }
    }
}
```

### Common Installation Errors

**Error: "Unable to load native library 'paddle_inference'"**

Solution: Ensure you've installed a runtime package matching your platform:
```bash
dotnet add package Sdcb.PaddleInference.runtime.win64.mkl
```

**Error: "Model file not found"**

Solution: Models must be downloaded separately. Use `OnlineFullModels.ChineseV4.DownloadAsync()` or download manually.

**Error: "CUDA driver version is insufficient"**

Solution: Either update NVIDIA drivers or switch to CPU runtime.

---

## GPU Configuration

GPU acceleration can provide 5-10x performance improvement for PaddleOCR, but the setup is complex and failure-prone. Most development teams should start with CPU and only invest in GPU setup if processing volume justifies the complexity.

### CUDA Requirements

| Component | Required Version | Download |
|-----------|-----------------|----------|
| NVIDIA Driver | 450.80.02+ (Linux) / 452.39+ (Windows) | nvidia.com/drivers |
| CUDA Toolkit | 11.8 | developer.nvidia.com/cuda-toolkit |
| cuDNN | 8.6.0+ (for CUDA 11.x) | developer.nvidia.com/cudnn |
| TensorRT (optional) | 8.5+ | developer.nvidia.com/tensorrt |

### GPU Setup Steps (Windows)

1. **Verify NVIDIA GPU and driver:**
```bash
nvidia-smi
```
Expected output shows GPU model and driver version.

2. **Install CUDA Toolkit 11.8:**
   - Download from NVIDIA developer site
   - Run installer with default options
   - Verify: `nvcc --version` shows 11.8

3. **Install cuDNN:**
   - Download cuDNN 8.6 for CUDA 11.x
   - Extract to CUDA installation directory
   - Copy `bin`, `include`, `lib` contents to CUDA folder
   - Verify files exist: `C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v11.8\bin\cudnn64_8.dll`

4. **Update PATH:**
```
C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v11.8\bin
C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v11.8\libnvvp
```

5. **Install GPU runtime package:**
```bash
dotnet add package Sdcb.PaddleInference.runtime.win64.cuda118
```

6. **Verify GPU inference:**
```csharp
using Sdcb.PaddleInference;

// Should show GPU device
var device = PaddleDevice.Gpu();
Console.WriteLine($"Using device: {device}");
```

### Why Most Users Should Stick with CPU

| Factor | CPU | GPU |
|--------|-----|-----|
| Setup time | 5 minutes | 2-4 hours |
| Maintenance | None | Driver updates, CUDA compatibility |
| Deployment | Simple NuGet | CUDA/cuDNN in container/server |
| Cost | Included | GPU hardware/cloud instance |
| Speed (single image) | 200-500ms | 50-100ms |
| Speed (batch 100 images) | 50s | 10s |

For most .NET applications processing <1000 images/day, the CPU runtime is sufficient. The complexity and cost of GPU setup only pays off at high volume.

### GPU in Docker

Docker GPU support requires additional configuration:

```dockerfile
FROM nvidia/cuda:11.8.0-cudnn8-runtime-ubuntu22.04

# Install .NET runtime
RUN apt-get update && apt-get install -y dotnet-runtime-8.0

# Copy application
WORKDIR /app
COPY . .

ENTRYPOINT ["dotnet", "YourApp.dll"]
```

Run with NVIDIA runtime:
```bash
docker run --gpus all your-paddle-ocr-app
```

---

## Basic Usage Examples

### Simple Text Extraction

```csharp
// Install: dotnet add package Sdcb.PaddleOCR
// Install: dotnet add package Sdcb.PaddleOCR.Models.Online
// Install: dotnet add package Sdcb.PaddleInference.runtime.win64.mkl

using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.Online;
using OpenCvSharp;

// Download models (cached after first run)
var models = await OnlineFullModels.ChineseV4.DownloadAsync();

// Create OCR instance
using var ocr = new PaddleOcrAll(models);

// Load and process image
using var mat = Cv2.ImRead("document.png");
var result = ocr.Run(mat);

// Output text
Console.WriteLine(result.Text);
```

### Processing with Confidence Filtering

```csharp
using var ocr = new PaddleOcrAll(models)
{
    AllowRotateDetection = true,
    Enable180Classification = true
};

using var mat = Cv2.ImRead("document.png");
var result = ocr.Run(mat);

// Filter by confidence
var highConfidenceText = result.Regions
    .Where(r => r.Score > 0.8)
    .Select(r => r.Text);

Console.WriteLine(string.Join("\n", highConfidenceText));
```

### Batch Processing

```csharp
var imageFiles = Directory.GetFiles("documents", "*.png");
var results = new List<(string File, string Text)>();

using var ocr = new PaddleOcrAll(models);

foreach (var file in imageFiles)
{
    using var mat = Cv2.ImRead(file);
    var result = ocr.Run(mat);
    results.Add((file, result.Text));
}
```

---

## Key Limitations and Weaknesses

Having worked with PaddleOCR in several production scenarios, these are the significant limitations teams should evaluate before adoption.

### 1. Python-First Architecture

PaddleOCR was designed for Python developers. The .NET wrapper is a community project, not an official Baidu product.

**Implications:**
- Documentation is primarily in Python (and often Chinese)
- New features appear in Python first, months before .NET
- Bug reports may be answered in Chinese
- The wrapper author (sdcb) maintains this as a side project

**Example issue:** When PaddleOCR v4 released with improved models, .NET developers waited 3 months for the wrapper update.

### 2. Chinese Company Data Concerns

Baidu is a Chinese company subject to Chinese data laws. While PaddleOCR itself runs locally, the ecosystem creates potential concerns:

**Model source:** Pre-trained models are hosted on Baidu's servers. When you download models, you're connecting to Baidu infrastructure.

**Telemetry uncertainty:** The Python package has been observed making network calls. While the .NET wrapper attempts to isolate this, enterprise security teams may require verification.

**Regulatory considerations:** Some organizations, particularly in government and defense, have policies restricting Chinese-origin software components.

See the [Data Sovereignty Concerns](#data-sovereignty-concerns) section for detailed analysis.

### 3. Model Download Complexity

Unlike IronOCR where language packs are simple NuGet dependencies, PaddleOCR requires downloading separate model files:

```
ch_PP-OCRv4_det_infer/           # Detection model (~5MB)
ch_PP-OCRv4_rec_infer/           # Recognition model (~15MB)
ch_ppocr_mobile_v2.0_cls_infer/  # Classification model (~3MB)
```

**Deployment challenges:**
- Models must be downloaded before first use (or bundled)
- Air-gapped environments require manual model transfer
- Model versioning must match wrapper version
- ~100MB additional deployment size

### 4. GPU Configuration Burden

To achieve advertised performance, GPU acceleration requires:

- NVIDIA GPU (AMD/Intel not supported)
- CUDA 11.8 installation
- cuDNN 8.6+ installation
- TensorRT for optimization (optional)
- Matching driver versions

Many teams spend 4-8 hours on initial GPU setup, plus ongoing maintenance when CUDA versions update.

### 5. Limited Language Support

PaddleOCR supports 14 languages. Compare:

| Library | Languages |
|---------|-----------|
| PaddleOCR | 14 |
| IronOCR | 125+ |
| Tesseract | 100+ |
| ABBYY | 200+ |

If your application needs to process documents in Polish, Dutch, Vietnamese, or dozens of other languages, PaddleOCR cannot help.

### 6. Newer Library, Smaller Community

PaddleOCR's .NET wrapper has ~200K NuGet downloads compared to:
- Tesseract (charlesw): ~8M downloads
- IronOCR: ~5.3M downloads

This translates to:
- Fewer Stack Overflow answers
- Less battle-tested in production
- Smaller pool of developers with experience
- Fewer blog posts and tutorials

### 7. Deployment Complexity

A minimal PaddleOCR deployment includes:

```
YourApp.dll
YourApp.deps.json
paddle_inference.dll (~200MB)
opencv_world*.dll (~30MB)
models/
  ch_PP-OCRv4_det_infer/
  ch_PP-OCRv4_rec_infer/
  ch_ppocr_mobile_v2.0_cls_infer/
```

Total deployment: 300-500MB minimum

Compare to IronOCR: single NuGet package, ~80MB.

---

## Data Sovereignty Concerns

For enterprise deployments, especially in regulated industries, the Chinese origin of PaddleOCR warrants careful consideration.

### What Runs Locally

The Sdcb.PaddleOCR wrapper and inference engine run entirely locally. Your document images are processed on your servers and are not transmitted to Baidu.

### What Connects Externally

**Model downloads:** When using `OnlineFullModels.*.DownloadAsync()`, your application connects to Baidu-hosted servers to download model files.

**Model sources:**
- Chinese models: `bj.bcebos.com` (Baidu Cloud Storage)
- Some models: GitHub releases

### Mitigation Strategies

**1. Pre-download and bundle models:**
```csharp
// Download once manually, bundle with application
var models = new FullOcrModel(
    LocalDetectionModel.FromDirectory("bundled-models/det"),
    LocalClassificationModel.FromDirectory("bundled-models/cls"),
    LocalRecognitionModel.FromDirectory("bundled-models/rec")
);
```

**2. Firewall/network isolation:**
- Block outbound connections to `*.baidu.com`, `*.bcebos.com`
- Verify no unexpected network calls in production

**3. Code review:**
- Review Sdcb.PaddleOCR source code
- Audit PaddleInference native library behavior

### Compliance Considerations

| Regulation | PaddleOCR Impact |
|------------|-----------------|
| GDPR | Generally OK (local processing) |
| HIPAA | Review model download paths |
| FedRAMP | May not be acceptable |
| ITAR | Consult compliance officer |
| Financial regulations | Varies by jurisdiction |

### Alternative: IronOCR for Sensitive Documents

IronOCR processes everything locally with no external connections:
- No model downloads (bundled in NuGet)
- No telemetry or analytics
- US-based company (Iron Software)
- Commercial support for compliance questions

---

## Pricing Analysis

PaddleOCR is free under the Apache 2.0 license. However, "free" comes with costs.

### Direct Costs: $0

- Software license: Free
- Model downloads: Free
- Commercial use: Permitted

### Indirect Costs

| Cost Factor | Estimate |
|-------------|----------|
| Setup time (first time) | 4-16 hours @ developer rate |
| GPU hardware (if needed) | $500-5000+ |
| GPU cloud instances | $0.50-3.00/hour |
| Troubleshooting | 2-8 hours per issue |
| Model storage | 100-500MB per deployment |
| Docker image bloat | Larger images = higher registry/transfer costs |

### Total Cost of Ownership Comparison

**Scenario: Processing 10,000 documents/month, 1 developer**

| Factor | PaddleOCR | IronOCR |
|--------|-----------|---------|
| License | $0 | $749 (perpetual) |
| Setup time | 8 hrs @ $100/hr = $800 | 0.5 hrs @ $100/hr = $50 |
| Maintenance (annual) | 10 hrs @ $100/hr = $1000 | 2 hrs @ $100/hr = $200 |
| Year 1 Total | $1,800 | $999 |
| Year 2+ Total | $1,000/year | $200/year |

For most teams, the development time saved with IronOCR exceeds the license cost within the first few months.

### When PaddleOCR is Cost-Effective

- High-volume Chinese document processing
- GPU infrastructure already in place
- Deep learning expertise on team
- Long-term project where setup cost amortizes

---

## PaddleOCR vs IronOCR Comparison

| Feature | PaddleOCR (Sdcb) | IronOCR |
|---------|-----------------|---------|
| **Paradigm** | Deep learning | Optimized Tesseract 5 |
| **Languages** | 14 | 125+ |
| **Chinese recognition** | Excellent | Good |
| **Latin text (English)** | Good | Excellent |
| **NuGet packages** | 3-5 required | 1 (IronOcr) |
| **Setup time** | 30-60 minutes | 5 minutes |
| **Model downloads** | Required | Bundled |
| **GPU support** | Yes (complex setup) | CPU optimized |
| **PDF support** | Manual conversion | Native |
| **Preprocessing** | Manual | Automatic |
| **License** | Apache 2.0 (Free) | Commercial ($749+) |
| **Support** | Community/GitHub | Commercial SLA |
| **Company origin** | China (Baidu) | USA (Iron Software) |
| **NuGet downloads** | ~200K | ~5.3M |

### Accuracy Comparison

| Document Type | PaddleOCR | IronOCR |
|--------------|-----------|---------|
| Chinese documents | 95%+ | 85-90% |
| English typed text | 92-95% | 97-99% |
| English handwriting | 80-85% | 75-80% |
| Mixed Chinese/English | 90-93% | 85-90% |
| Low quality scans | 85-90% | 90-95% (with preprocessing) |

### Performance Comparison

| Metric | PaddleOCR (CPU) | PaddleOCR (GPU) | IronOCR |
|--------|-----------------|-----------------|---------|
| Cold start | 3-5 seconds | 5-10 seconds | <1 second |
| Single page | 300-500ms | 50-100ms | 150-300ms |
| Memory usage | 500MB-1GB | 1-2GB | 100-200MB |

---

## Migration Guide: PaddleOCR to IronOCR

### Why Migrate?

Teams typically migrate from PaddleOCR to IronOCR when:

1. **Language requirements expand** - Need more than 14 languages
2. **Deployment complexity** - Model management becomes burdensome
3. **Support needs** - Require commercial support SLA
4. **Performance** - CPU-only deployment needs optimization
5. **Compliance** - Data sovereignty requirements

### Migration Complexity

**Estimated time:** 4-8 hours

The migration involves a paradigm shift from deep learning to traditional OCR. The API surface is different, but the output (extracted text) is equivalent.

### Package Changes

**Remove:**
```xml
<PackageReference Include="Sdcb.PaddleOCR" Version="3.0.1" />
<PackageReference Include="Sdcb.PaddleOCR.Models.Online" Version="3.0.1" />
<PackageReference Include="Sdcb.PaddleInference.runtime.win64.mkl" Version="3.0.0" />
<PackageReference Include="OpenCvSharp4" Version="*" />
<PackageReference Include="OpenCvSharp4.runtime.win" Version="*" />
```

**Add:**
```xml
<PackageReference Include="IronOcr" Version="2024.*" />
```

### API Mapping

| PaddleOCR | IronOCR | Notes |
|-----------|---------|-------|
| `PaddleOcrAll` | `IronTesseract` | Main OCR class |
| `OnlineFullModels.*.DownloadAsync()` | N/A | No model download needed |
| `ocr.Run(mat)` | `ocr.Read(input)` | Process image |
| `result.Text` | `result.Text` | Get full text |
| `result.Regions` | `result.Words` / `result.Lines` | Get text regions |
| `region.Score` | `word.Confidence` | Confidence value |
| `region.Rect` | `word.X, Y, Width, Height` | Bounding box |
| OpenCvSharp `Mat` | `OcrInput` | Image input |

### Migration Examples

#### Example 1: Basic Text Extraction

**Before (PaddleOCR):**
```csharp
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.Online;
using OpenCvSharp;

var models = await OnlineFullModels.ChineseV4.DownloadAsync();
using var ocr = new PaddleOcrAll(models);
using var mat = Cv2.ImRead("document.png");
var result = ocr.Run(mat);
Console.WriteLine(result.Text);
```

**After (IronOCR):**
```csharp
using IronOcr;

var ocr = new IronTesseract();
using var input = new OcrInput("document.png");
var result = ocr.Read(input);
Console.WriteLine(result.Text);
```

**Changes:**
- Removed 4 NuGet packages, added 1
- Removed model download step
- Removed OpenCvSharp dependency
- Simplified from 6 lines to 4 lines

#### Example 2: Multi-Language Processing

**Before (PaddleOCR):**
```csharp
// Limited to supported languages
var models = await OnlineFullModels.English.DownloadAsync();
using var ocr = new PaddleOcrAll(models);
using var mat = Cv2.ImRead("document.png");
var result = ocr.Run(mat);
```

**After (IronOCR):**
```csharp
var ocr = new IronTesseract();
// 125+ languages available
ocr.Language = OcrLanguage.English + OcrLanguage.French + OcrLanguage.German;
using var input = new OcrInput("document.png");
var result = ocr.Read(input);
```

#### Example 3: Processing with Confidence

**Before (PaddleOCR):**
```csharp
using var ocr = new PaddleOcrAll(models);
using var mat = Cv2.ImRead("document.png");
var result = ocr.Run(mat);

foreach (var region in result.Regions.Where(r => r.Score > 0.8))
{
    Console.WriteLine($"{region.Text} ({region.Score:P0})");
}
```

**After (IronOCR):**
```csharp
var ocr = new IronTesseract();
using var input = new OcrInput("document.png");
var result = ocr.Read(input);

foreach (var word in result.Words.Where(w => w.Confidence > 0.8))
{
    Console.WriteLine($"{word.Text} ({word.Confidence:P0})");
}
```

#### Example 4: PDF Processing

**Before (PaddleOCR):**
```csharp
// PaddleOCR doesn't support PDF directly - need manual conversion
using var pdfDoc = PdfiumViewer.PdfDocument.Load("document.pdf");
var results = new List<string>();

for (int i = 0; i < pdfDoc.PageCount; i++)
{
    using var pageImage = pdfDoc.Render(i, 200, 200, PdfRenderFlags.CorrectFromDpi);
    var tempPath = Path.GetTempFileName() + ".png";
    pageImage.Save(tempPath, ImageFormat.Png);

    using var mat = Cv2.ImRead(tempPath);
    var result = ocr.Run(mat);
    results.Add(result.Text);

    File.Delete(tempPath);
}
```

**After (IronOCR):**
```csharp
var ocr = new IronTesseract();
using var input = new OcrInput();
input.LoadPdf("document.pdf");
var result = ocr.Read(input);
Console.WriteLine(result.Text);
```

### Deployment Cleanup

After migration, remove from your deployment:

```
# Delete these files/folders
paddle_inference.dll
paddle2onnx.dll
opencv_world*.dll
models/
  ch_PP-OCRv4_det_infer/
  ch_PP-OCRv4_rec_infer/
  ch_ppocr_mobile_v2.0_cls_infer/
```

### Docker Image Simplification

**Before (PaddleOCR):**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Required for OpenCvSharp
RUN apt-get update && apt-get install -y \
    libgdiplus \
    libopencv-dev \
    && rm -rf /var/lib/apt/lists/*

# Copy models
COPY models/ /app/models/

WORKDIR /app
COPY . .
ENTRYPOINT ["dotnet", "YourApp.dll"]
```

**After (IronOCR):**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app
COPY . .
ENTRYPOINT ["dotnet", "YourApp.dll"]
```

### Migration Checklist

- [ ] Remove Sdcb.PaddleOCR packages from .csproj
- [ ] Remove OpenCvSharp packages from .csproj
- [ ] Add IronOcr package
- [ ] Update using statements
- [ ] Replace PaddleOcrAll with IronTesseract
- [ ] Replace Mat loading with OcrInput
- [ ] Update result access (Regions -> Words/Lines)
- [ ] Remove model download code
- [ ] Delete model files from deployment
- [ ] Update Dockerfile (remove apt-get, model copies)
- [ ] Test with representative documents
- [ ] Verify accuracy meets requirements

---

## When to Use PaddleOCR vs IronOCR

### Choose PaddleOCR When:

1. **Primary focus is Chinese documents** - PaddleOCR's strength is Chinese text recognition
2. **You have deep learning expertise** - Team comfortable with ML model management
3. **GPU infrastructure exists** - Already have CUDA-capable servers/containers
4. **Zero software budget** - Truly cannot afford any commercial licenses
5. **Research/experimentation** - Exploring deep learning OCR approaches

### Choose IronOCR When:

1. **Multi-language requirements** - Need more than 14 languages
2. **Production reliability** - Need commercial support and SLA
3. **Simple deployment** - Want single NuGet package without model management
4. **PDF-heavy workflow** - Documents arrive as PDFs, not images
5. **CPU-only deployment** - Standard server/cloud without GPU
6. **Time-to-market** - Need to ship quickly without setup complexity
7. **Compliance requirements** - Data sovereignty concerns about Chinese software
8. **English/Latin text focus** - Traditional OCR excels here

### Decision Matrix

| Scenario | Recommendation |
|----------|---------------|
| Chinese invoice processing startup | PaddleOCR |
| US government document digitization | IronOCR |
| Multi-national document management | IronOCR |
| Chinese language learning app | PaddleOCR |
| Healthcare records processing | IronOCR |
| General business document OCR | IronOCR |
| Academic research on OCR | PaddleOCR |
| Enterprise with existing GPU cluster | Either (evaluate) |

---

## Code Examples

For complete, runnable code examples demonstrating PaddleOCR patterns:

- [Basic OCR Operations](./paddleocr-basic-ocr.cs) - Model loading, text extraction, result parsing
- [GPU Setup and Configuration](./paddleocr-gpu-setup.cs) - CUDA configuration, GPU vs CPU comparison
- [Migration Comparison](./paddleocr-migration-comparison.cs) - Side-by-side PaddleOCR vs IronOCR examples

---

## References

- <a href="https://github.com/sdcb/PaddleSharp" rel="nofollow">Sdcb.PaddleOCR GitHub Repository</a>
- <a href="https://www.nuget.org/packages/Sdcb.PaddleOCR" rel="nofollow">Sdcb.PaddleOCR on NuGet</a>
- <a href="https://github.com/PaddlePaddle/PaddleOCR" rel="nofollow">PaddleOCR Official Repository (Python)</a>
- <a href="https://github.com/sdcb/PaddleSharp/blob/master/docs/ocr.md" rel="nofollow">PaddleSharp OCR Documentation</a>
- [IronOCR Documentation](https://ironsoftware.com/csharp/ocr/docs/)
- [IronOCR Free Trial](https://ironsoftware.com/csharp/ocr/docs/license/trial/)

---

*Last verified: January 2026*
