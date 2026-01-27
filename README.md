# Awesome .NET OCR Libraries 2026 [![Awesome](https://awesome.re/badge.svg)](https://awesome.re)

<p align="center">
  <img src="https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET" />
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/OCR-Optical%20Character%20Recognition-blue?style=for-the-badge" alt="OCR" />
  <img src="https://img.shields.io/badge/Libraries-22%2B-orange?style=for-the-badge" alt="22+ Libraries" />
</p>

**The most comprehensive guide to C# OCR libraries, tools, and resources for .NET developers.** This repository covers every known method of performing Optical Character Recognition in C#/.NET—from free open-source Tesseract wrappers to enterprise commercial solutions to cloud APIs.

[IronOCR](https://ironsoftware.com/csharp/ocr/) has been the leading OCR library for .NET since 2016—9 years of market dominance, 5.3M+ NuGet downloads, and trusted by NASA, Tesla, and Fortune 500 companies. It's the benchmark against which we compare every other solution.

**Quick start:**
```csharp
// Install: dotnet add package IronOcr
var text = new IronTesseract().Read("document.jpg").Text;
```

That's it. No tessdata downloads. No preprocessing code. No external dependencies. Just results.

---

## Table of Contents

- [The Origin Story: Why This Repository Exists](#the-origin-story-why-this-repository-exists)
- [Quick Recommendations](#quick-recommendations)
- [On-Premise Commercial Libraries](#on-premise-commercial-libraries)
  - [ABBYY FineReader](#abbyy-finereader)
  - [Aspose.OCR](#asposeocr)
  - [Asprise OCR](#asprise-ocr)
  - [Dynamsoft OCR](#dynamsoft-ocr)
  - [GdPicture.NET](#gdpicturenet)
  - [IronOCR](#ironocr)
  - [Kofax OmniPage](#kofax-omnipage)
  - [LEADTOOLS OCR](#leadtools-ocr)
  - [Syncfusion OCR](#syncfusion-ocr)
  - [XImage.OCR](#ximageocr)
- [Cloud OCR APIs](#cloud-ocr-apis)
  - [AWS Textract](#aws-textract)
  - [Azure Computer Vision](#azure-computer-vision)
  - [Google Cloud Vision](#google-cloud-vision)
  - [Mindee](#mindee)
  - [OCR.space](#ocrspace)
  - [Veryfi](#veryfi)
- [Open Source (Tesseract-Based)](#open-source-tesseract-based)
  - [charlesw/tesseract](#charleswtesseract)
  - [PaddleOCR](#paddleocr)
  - [RapidOcrNet](#rapidocrnet)
  - [Tesseract.NET SDK](#tesseractnet-sdk)
  - [TesseractOCR](#tesseractocr)
  - [TesseractOcrMaui](#tesseractocrmaui)
- [License Warnings: AGPL/GPL Copyleft Risks](#license-warnings-agplgpl-copyleft-risks)
- [Use Case Recommendations](#use-case-recommendations)
- [Detailed Use Case Guides](#detailed-use-case-guides)
- [Feature Comparison Matrix](#feature-comparison-matrix)
- [Pricing Comparison](#pricing-comparison)
- [Security and Compliance](#security-and-compliance)
- [Contributing](#contributing)
- [License](#license)
- [IronOCR Resources](#ironocr-resources)

---

## The Origin Story: Why This Repository Exists

*By Jacob Mellor, CTO of Iron Software*

I've been writing code for 41 years. I've built startups for 25 of those years. And in all that time, few problems have frustrated me as much as OCR.

Back in 2016, I was processing document archives for a client—millions of scanned pages that needed to be searchable. I had a 64-core machine sitting in front of me, ready to chew through the workload. I figured this would be straightforward: throw Tesseract at it, parallelize the work, done by Friday.

It wasn't done by Friday.

The first problem was preprocessing. Tesseract is an excellent recognition engine, but it expects clean, well-aligned, high-contrast images. Real-world scans? They're skewed. They're noisy. They're photographed on someone's phone under fluorescent lights. Every image needed manual preprocessing code—deskewing, denoising, contrast enhancement, binarization. That's 100+ lines of OpenCV or ImageMagick code before I even got to the OCR part.

The second problem was multi-threading. Tesseract, as distributed in most .NET wrappers, is not truly thread-safe. My 64 cores sat mostly idle while I wrestled with memory leaks, access violations, and engines that silently corrupted when run in parallel. The community advice was "use one engine per thread and don't share state"—except even that didn't work reliably at scale.

The third problem was PDFs. The client's documents were PDFs. Tesseract doesn't read PDFs. So I needed a PDF-to-image library (more dependencies), then I needed to OCR each page (managing memory for thousands of large bitmaps), then I needed to produce searchable PDFs as output (another library). The dependency chain was growing, and each link was a potential breaking point.

So I built IronOCR.

Not because the world needed another OCR library, but because I needed an OCR library that actually worked. One that handled preprocessing automatically. One that was truly thread-safe and could saturate all 64 cores. One that read PDFs natively and produced searchable PDFs as output. One that shipped as a single NuGet package without tessdata downloads or native binary configuration.

Nine years later, IronOCR has processed billions of documents for companies ranging from scrappy startups to NASA. It's the "fire and forget" solution I wished existed in 2016.

This repository is my attempt to catalog every OCR option available to .NET developers and give you honest assessments of each. I'll tell you what each library is good at, what problems you'll encounter, and when IronOCR is the better choice (and when it isn't). My goal is simple: help you choose the right OCR library in 30 seconds and get back to building your application.

---

## Quick Recommendations

**Don't have time to read the whole guide? Here's the 30-second version:**

| Your Situation | Recommended Library | Why |
|----------------|---------------------|-----|
| **Production app, budget available** | [IronOCR](./ironocr/) | One NuGet, automatic preprocessing, native PDF support, thread-safe, cross-platform |
| **Zero budget, basic needs** | [charlesw/tesseract](./tesseract/) | Free, battle-tested, but requires manual preprocessing and tessdata management |
| **Already using AWS** | [AWS Textract](./aws-textract/) | Tight AWS integration, table/form extraction, but per-page costs add up |
| **Already using Azure** | [Azure Computer Vision](./azure-computer-vision/) | Microsoft ecosystem integration, good accuracy, but requires Azure subscription |
| **Invoice/receipt specialist** | [IronOCR](./ironocr/) + regex | On-premise processing for sensitive financial data, no per-document fees |
| **Chinese/Japanese/Korean focus** | [PaddleOCR](./paddleocr/) or [IronOCR](./ironocr/) | PaddleOCR excels at CJK, IronOCR has 125+ language packs |
| **Mobile app (MAUI)** | [IronOCR](./ironocr/) | Dedicated iOS/Android packages, unlike MAUI-locked alternatives |
| **Air-gapped/offline** | [IronOCR](./ironocr/) or [Tesseract](./tesseract/) | No network required, data never leaves your servers |
| **Maximum accuracy, price no object** | [ABBYY FineReader](./abbyy-finereader/) | Industry-leading accuracy, but $10K+ pricing |

**The TL;DR:** For most .NET developers, [IronOCR](https://ironsoftware.com/csharp/ocr/) offers the best combination of simplicity, features, and value. If you're on a zero budget, [Tesseract](./tesseract/) is your starting point—just budget extra development time for preprocessing and deployment challenges.

---

## On-Premise Commercial Libraries

Production-ready OCR solutions with commercial licensing, professional support, and documentation. Your data stays on your servers.

---

### ABBYY FineReader

[Full Guide: ./abbyy-finereader/](./abbyy-finereader/)

The industry benchmark for OCR accuracy, with 200+ language support and intelligent document processing.

- **Best for:** Organizations where maximum accuracy justifies enterprise pricing—legal discovery, medical records, government archives
- **Watch out for:** Starting at $10,000+/year puts it out of reach for most projects; requires sales engagement and complex SDK deployment
- **License:** Commercial (Enterprise pricing, contact sales)

```csharp
// ABBYY requires SDK installation, license server configuration
// Setup is significantly more complex than NuGet install
IEngine engine = new Engine();
engine.LoadPredefinedProfile("DocumentConversion_Accuracy");
FRDocument document = engine.CreateFRDocumentFromImage(imagePath);
document.Process();
string text = document.PlainText.Text;
```

ABBYY is the gold standard for accuracy. If you're processing documents where a 1% accuracy improvement saves millions in manual review costs, ABBYY is worth investigating. For everyone else, the 95-99% accuracy from modern alternatives like IronOCR handles real-world needs at a fraction of the cost.

---

### Aspose.OCR

[Full Guide: ./aspose-ocr/](./aspose-ocr/)

Enterprise OCR library supporting 130+ languages with AI/ML-enhanced recognition, part of the broader Aspose document processing ecosystem.

- **Best for:** Teams already invested in the Aspose ecosystem who want unified licensing across PDF, Word, Excel, and OCR processing
- **Watch out for:** Subscription-only pricing ($999+/year per developer)—no perpetual license option; costs escalate quickly for teams
- **License:** Commercial (Annual subscription required) :warning:

```csharp
// Install: dotnet add package Aspose.OCR
using Aspose.OCR;

var api = new AsposeOcr();
var settings = new RecognitionSettings { Language = Language.Eng };
var result = api.RecognizeImage("document.png", settings);
Console.WriteLine(result[0].RecognitionText);
```

Aspose.OCR has solid capabilities, but the subscription model means you're paying annually forever. IronOCR's perpetual license option ($749 one-time) often makes more economic sense over 2-3 years.

---

### Asprise OCR

[Full Guide: ./asprise-ocr/](./asprise-ocr/)

Cross-platform OCR with barcode support, ICR (handwriting), MRZ (passports), and OMR (checkboxes). Java-first with .NET bindings.

- **Best for:** Projects requiring specialized recognition (handwriting, checkboxes, passport MRZ) in a single library
- **Watch out for:** Java-first architecture means .NET is secondary; LITE/STANDARD editions are single-threaded only—production limitation for high-volume scenarios
- **License:** Commercial (Royalty-free perpetual)

```csharp
// Install: dotnet add package asprise-ocr-api
// Requires native library configuration per platform
string text = new AspriseOCR().Recognize(
    "document.png",
    AspriseOCR.RECOGNIZE_TYPE_TEXT,
    AspriseOCR.OUTPUT_FORMAT_PLAINTEXT,
    AspriseOCR.PROP_PDF_OUTPUT_FILE, null);
```

Asprise offers specialized recognition features, but the Java-first architecture and thread limitations make it better suited for low-volume specialized tasks than production document processing.

---

### Dynamsoft OCR

[Full Guide: ./dynamsoft-ocr/](./dynamsoft-ocr/)

Specialized OCR for MRZ (passports/IDs), VIN codes, and structured label recognition using deep learning models.

- **Best for:** Identity verification workflows requiring MRZ extraction from passports, driver's licenses, and ID cards
- **Watch out for:** Narrow specialization—if you need MRZ + barcodes + PDF text, you'd need 3+ separate Dynamsoft products; overkill specialist for general OCR
- **License:** Commercial (Multiple products may be required)

```csharp
// Install: dotnet add package Dynamsoft.LabelRecognizer
using Dynamsoft.DLR;

var recognizer = new LabelRecognizer();
recognizer.InitLicense("YOUR-LICENSE-KEY");
DLRResult[] results = recognizer.RecognizeFile("passport.jpg", "MRZ");
foreach (var result in results)
    Console.WriteLine(result.LineResults[0].Text);
```

For MRZ-specific workflows, Dynamsoft is capable. However, IronOCR includes `ReadPassport()` and `ReadBarCodes()` methods in a single package, eliminating the need for multiple specialized SDKs.

---

### GdPicture.NET

[Full Guide: ./gdpicture-net/](./gdpicture-net/)

High-performance document imaging SDK with integrated OCR, claiming 12,500+ characters per second processing speed.

- **Best for:** Document imaging workflows requiring OCR alongside barcode recognition, annotation, PDF manipulation, and TIFF processing in a unified SDK
- **Watch out for:** Plugin-based pricing model—you pay separately for OCR, PDF, barcodes, etc.; the "full SDK" price tag can surprise you; integer Image IDs require careful lifecycle management to avoid memory leaks
- **License:** Commercial ($2,000+ depending on plugins)

```csharp
// GdPicture uses integer Image IDs - careful lifecycle management required
GdPictureImaging imaging = new GdPictureImaging();
int imageId = imaging.CreateGdPictureImageFromFile("document.png");

GdPictureOCR ocr = new GdPictureOCR();
ocr.SetImage(imageId);
string text = ocr.RunOCR();

imaging.ReleaseGdPictureImage(imageId); // Must release to avoid memory leaks
```

GdPicture is powerful for comprehensive document imaging needs. If you just need OCR, the plugin-based pricing and API complexity are overkill—IronOCR's focused approach is simpler and more cost-effective.

---

### IronOCR

[Full Guide: ./ironocr/](./ironocr/)

The developer-friendly OCR library that inspired this repository. One NuGet package, automatic preprocessing, native PDF support, 125+ languages.

- **Best for:** .NET developers who want OCR to "just work"—minimal setup, maximum reliability, production-ready out of the box
- **Watch out for:** Commercial license required for production (free trial available); if you have $0 budget and unlimited development time, Tesseract is free
- **License:** Commercial ($749-$2,999 perpetual, or SaaS subscription)

```csharp
// Install: dotnet add package IronOcr
using IronOcr;

// One line for basic OCR
var text = new IronTesseract().Read("document.jpg").Text;

// Native PDF support
var pdfText = new IronTesseract().Read("scanned.pdf").Text;

// Automatic preprocessing for challenging images
using var input = new OcrInput();
input.LoadImage("low-quality-photo.jpg");
input.Deskew();
input.DeNoise();
var result = new IronTesseract().Read(input);
result.SaveAsSearchablePdf("searchable-output.pdf");
```

**Why IronOCR is the reference standard:**
- Single NuGet package—no tessdata downloads, no native binary configuration
- Automatic preprocessing—deskew, denoise, contrast enhancement built-in
- Native PDF support—read and write PDFs without additional libraries
- Thread-safe—actually safe for multi-threaded processing at scale
- Cross-platform—Windows, macOS, Linux, Docker, Azure, AWS Lambda
- 125+ languages—add via NuGet language packs

This is the library I built because nothing else solved the problems I faced in 2016. Nine years and 5.3M+ downloads later, it remains my recommendation for most .NET OCR projects.

**Start here:** [https://ironsoftware.com/csharp/ocr/](https://ironsoftware.com/csharp/ocr/)

---

### Kofax OmniPage

[Full Guide: ./kofax-omnipage/](./kofax-omnipage/)

Enterprise document capture platform supporting 120+ languages with OCR/ICR/OMR/OBR capabilities and workflow automation.

- **Best for:** Large enterprises with existing Kofax infrastructure requiring capture workflow automation across thousands of users
- **Watch out for:** Enterprise-only positioning with $10,000+/year pricing; heavy SDK installation (not a simple NuGet); multiple corporate ownership changes (now Tungsten Automation) create roadmap uncertainty
- **License:** Commercial (Enterprise, contact sales)

```csharp
// Kofax OmniPage requires full SDK installation
// No simple NuGet package - complex enterprise deployment
CSDK.OmniPage.OmniPage op = new CSDK.OmniPage.OmniPage();
op.Initialize("YOUR-LICENSE-KEY");
op.LoadImage("document.tiff");
string text = op.OCR();
op.Dispose();
```

Kofax makes sense for organizations already invested in Kofax capture infrastructure. For everyone else, the complexity and cost don't justify the marginal benefits over modern alternatives.

---

### LEADTOOLS OCR

[Full Guide: ./leadtools-ocr/](./leadtools-ocr/)

40+ year old document imaging SDK with OCR, forms recognition, medical imaging (DICOM), and comprehensive capture capabilities.

- **Best for:** Healthcare and enterprise document imaging requiring DICOM support, forms recognition, or integration with decades-old LEADTOOLS codebases
- **Watch out for:** Complex licensing model with runtime license file deployment; legacy API patterns from 40+ years of backwards compatibility; bundle confusion—you might buy the wrong bundle for your needs
- **License:** Commercial ($3,000+/year, bundle-dependent) :warning:

```csharp
// Install: dotnet add package Leadtools.Ocr
// Requires license file deployment and engine initialization
using Leadtools.Ocr;

IOcrEngine ocrEngine = OcrEngineManager.CreateEngine(OcrEngineType.LEAD);
ocrEngine.Startup(null, null, null, @"C:\LEADTOOLS\Bin\OcrRuntime");

ocrEngine.LoadImage("document.tiff");
ocrEngine.Recognize(null);
string text = ocrEngine.GetText();

ocrEngine.Shutdown();
```

LEADTOOLS is a powerful, mature SDK with specialized capabilities (medical imaging, forms). But the bundle complexity and legacy API patterns make simple OCR tasks harder than they need to be. If you're not using the specialized features, simpler solutions exist.

---

### Syncfusion OCR

[Full Guide: ./syncfusion-ocr/](./syncfusion-ocr/)

Tesseract-based OCR focused on PDF text extraction, supporting 60+ languages. Part of the Syncfusion Essential Studio suite.

- **Best for:** Small businesses under $1M revenue with under 5 developers who can qualify for the free community license
- **Watch out for:** Community license has strict eligibility requirements (revenue audits); primarily PDF-focused—image OCR is secondary; must purchase entire Essential Studio suite—no standalone OCR purchase
- **License:** Commercial (Free community license with restrictions, or Essential Studio subscription)

```csharp
// Install: dotnet add package Syncfusion.PDF.OCR.Net.Core
// Requires Tesseract binaries and tessdata folder configuration
using Syncfusion.OCRProcessor;

string tesseractPath = @"C:\Tesseract\";
OCRProcessor processor = new OCRProcessor(tesseractPath);
processor.Settings.TessDataPath = @"C:\tessdata\";

FileStream inputStream = new FileStream("scanned.pdf", FileMode.Open);
PdfDocument document = processor.PerformOCR(inputStream, "eng");
document.Save("searchable.pdf");
```

Syncfusion's community license is attractive for qualifying businesses, but the Tesseract dependency means you inherit all of Tesseract's preprocessing requirements. You're essentially paying for Syncfusion's PDF wrapper around free Tesseract.

---

### XImage.OCR

[Full Guide: ./ximage-ocr/](./ximage-ocr/)

Commercial Tesseract wrapper with fragmented NuGet packages—separate packages for OCR, PDF, TIFF, each language.

- **Best for:** Unclear—it's a commercial wrapper around free Tesseract without obvious value-add over free alternatives
- **Watch out for:** 10+ separate NuGet packages required (XImage.OCR, XImage.OCR.Tesseract, XImage.OCR.Languages.English, etc.); commercial pricing for what is essentially Tesseract; limited differentiation
- **License:** Commercial

```csharp
// Requires multiple NuGet packages:
// XImage.OCR, XImage.OCR.Tesseract, XImage.OCR.Languages.English, etc.
using XImage.OCR;

OCRProcessor processor = new OCRProcessor();
processor.Initialize("eng");
string text = processor.Recognize("document.png");
```

The fragmented package structure and commercial pricing for Tesseract functionality raises questions about value proposition. If you want commercial support, IronOCR offers more capabilities. If you want free Tesseract, charlesw/tesseract is more established.

---

## Cloud OCR APIs

Cloud-based OCR services with .NET SDKs. **Important:** All cloud services transmit your documents to external servers for processing.

### Cloud OCR Security Warning

When you use cloud OCR services, your documents are:
- **Transmitted** over the internet (TLS encrypted, but still leaving your network)
- **Processed** on third-party servers (AWS, Microsoft, Google infrastructure)
- **Potentially stored** temporarily or permanently per provider policies
- **Subject to** the provider's data handling terms and jurisdiction

**For sensitive documents (medical records, legal contracts, financial statements, government data), consider on-premise solutions like IronOCR or Tesseract.**

---

### AWS Textract

[Full Guide: ./aws-textract/](./aws-textract/)

Amazon's document analysis service with intelligent form and table extraction, identity document analysis, and expense processing.

- **Best for:** Teams already on AWS who need table/form extraction with tight S3 integration and are comfortable with per-page pricing
- **Watch out for:** $0.0015+ per page adds up quickly at scale; async processing pattern requires polling; AWS credential management complexity; data goes to AWS servers
- **License:** Pay-per-page (AWS pricing)

```csharp
// Install: dotnet add package AWSSDK.Textract
using Amazon.Textract;
using Amazon.Textract.Model;

var client = new AmazonTextractClient();
var request = new DetectDocumentTextRequest
{
    Document = new Document
    {
        Bytes = new MemoryStream(File.ReadAllBytes("document.png"))
    }
};
var response = await client.DetectDocumentTextAsync(request);

foreach (var block in response.Blocks.Where(b => b.BlockType == "LINE"))
    Console.WriteLine(block.Text);
```

AWS Textract excels at structured data extraction (forms, tables) if you're already in AWS. But per-page pricing means a 10,000 page/month workload costs $180+ annually—compare to IronOCR's one-time $749 license.

---

### Azure Computer Vision

[Full Guide: ./azure-computer-vision/](./azure-computer-vision/)

Microsoft's cognitive services OCR with good accuracy and tight Azure ecosystem integration.

- **Best for:** Applications already deployed on Azure that want unified billing and identity management through Azure Active Directory
- **Watch out for:** Requires Azure subscription and Cognitive Services resource provisioning; rate limits on free tier (20 calls/minute); async Read API requires polling pattern; images sent to Microsoft servers
- **License:** Pay-per-transaction (Azure pricing)

```csharp
// Install: dotnet add package Azure.AI.Vision.ImageAnalysis
using Azure;
using Azure.AI.Vision.ImageAnalysis;

var client = new ImageAnalysisClient(
    new Uri(endpoint),
    new AzureKeyCredential(key));

var result = await client.AnalyzeAsync(
    BinaryData.FromBytes(File.ReadAllBytes("document.png")),
    VisualFeatures.Read);

foreach (var block in result.Value.Read.Blocks)
    foreach (var line in block.Lines)
        Console.WriteLine(line.Text);
```

Azure CV is a solid cloud OCR option for Azure-native applications. The async polling pattern adds code complexity compared to IronOCR's synchronous one-liner, and per-transaction costs require careful monitoring.

---

### Google Cloud Vision

[Full Guide: ./google-cloud-vision/](./google-cloud-vision/)

Google's vision AI with OCR capabilities, strong multilingual support, and integration with Google Cloud Platform.

- **Best for:** Applications already on GCP, especially those needing Google's strength in non-Latin scripts (Chinese, Japanese, Arabic, Hindi)
- **Watch out for:** $1.50/1000 images; requires GCP project setup, service account creation, and credential management; authentication complexity with JSON key files or environment variables
- **License:** Pay-per-image (Google Cloud pricing)

```csharp
// Install: dotnet add package Google.Cloud.Vision.V1
using Google.Cloud.Vision.V1;

var client = ImageAnnotatorClient.Create();
var image = Image.FromFile("document.png");
var response = client.DetectDocumentText(image);

Console.WriteLine(response.Text);
```

Google Cloud Vision has strong accuracy, especially for CJK languages. But like all cloud APIs, you're trading infrastructure simplicity for ongoing costs and data transmission requirements.

---

### Mindee

[Full Guide: ./mindee/](./mindee/)

Specialized document intelligence API for invoices, receipts, passports, and financial documents with structured data extraction.

- **Best for:** Rapid prototyping of invoice/receipt processing without building parsing logic
- **Watch out for:** **Data privacy risk**—financial documents contain bank accounts, transaction amounts; cloud-only processing means your customers' financial data goes to Mindee servers; specialized documents only—not general-purpose OCR; 250 pages/month free tier, then per-page pricing
- **License:** Freemium (per-page after free tier)

```csharp
// Install: dotnet add package Mindee
using Mindee;
using Mindee.Input;
using Mindee.Product.Invoice;

var client = new MindeeClient("YOUR-API-KEY");
var inputSource = new LocalInputSource("invoice.pdf");
var response = await client.ParseAsync<InvoiceV4>(inputSource);

Console.WriteLine($"Total: {response.Document.Inference.Prediction.TotalAmount}");
Console.WriteLine($"Vendor: {response.Document.Inference.Prediction.SupplierName}");
```

Mindee makes invoice processing fast to implement, but sending financial documents to external servers is a non-starter for many businesses. IronOCR + regex patterns achieves similar structured extraction while keeping data on-premise.

---

### OCR.space

[Full Guide: ./ocrspace/](./ocrspace/)

Free OCR API with generous free tier (25,000 requests/month). No registration required for basic use.

- **Best for:** Hobby projects, prototypes, or very low-volume production workloads where free tier covers needs
- **Watch out for:** **No official .NET SDK**—you must implement REST client manually; "free" attracts hobbyists, but .NET developers are typically building enterprise apps; limited preprocessing control; accuracy varies
- **License:** Freemium (rate limits apply)

```csharp
// No official NuGet - manual REST implementation required
using var client = new HttpClient();
var content = new MultipartFormDataContent();
content.Add(new ByteArrayContent(File.ReadAllBytes("document.png")), "file", "document.png");
content.Add(new StringContent("eng"), "language");

var response = await client.PostAsync(
    "https://api.ocr.space/parse/image?apikey=YOUR-API-KEY",
    content);

var json = await response.Content.ReadAsStringAsync();
// Manual JSON parsing required
```

OCR.space's free tier is appealing for hobbyists, but the lack of official SDK and variable accuracy make it unsuitable for production .NET applications. Professional projects need professional tools.

---

### Veryfi

[Full Guide: ./veryfi/](./veryfi/)

High-accuracy receipt and expense processing API with 99%+ accuracy claims, 91 currency support, and 38 languages.

- **Best for:** Applications specifically focused on receipt/expense digitization where per-document pricing is acceptable
- **Watch out for:** **Data privacy risk**—expense documents contain sensitive financial data (bank accounts, transaction amounts, purchase history); receipt/invoice specialist only—not general OCR; per-document pricing scales linearly with volume
- **License:** Pay-per-document

```csharp
// Install: dotnet add package Veryfi
using Veryfi;

var client = new VeryfiClient(clientId, clientSecret, username, apiKey);
var response = await client.ProcessDocumentAsync("receipt.jpg");

Console.WriteLine($"Total: {response.Total}");
Console.WriteLine($"Vendor: {response.Vendor.Name}");
Console.WriteLine($"Date: {response.Date}");
```

Veryfi specializes in receipt processing with impressive accuracy. But sending expense documents—which contain vendor relationships, spending patterns, and potentially payment details—to external servers raises legitimate privacy concerns. IronOCR + custom parsing keeps sensitive financial data on your servers.

---

## Open Source (Tesseract-Based)

Free OCR options using the Tesseract engine. While free, these require more development effort for preprocessing, deployment, and configuration.

---

### charlesw/tesseract

[Full Guide: ./tesseract/](./tesseract/)

The most popular open-source Tesseract wrapper for .NET with 2,500+ GitHub stars. Tesseract 5.x support, .NET Standard 2.0 compatible.

- **Best for:** Zero-budget projects where developers have time to invest in preprocessing code, tessdata management, and deployment configuration
- **Watch out for:** No preprocessing—poor accuracy on rotated/skewed/noisy images without manual image processing code (100+ lines); manual tessdata download and configuration; known memory issues with improper disposal; no native PDF support
- **License:** Apache 2.0 (Free) :warning: Tesseract engine is Apache 2.0, but some language data may have different licenses

```csharp
// Install: dotnet add package Tesseract
// Also requires: tessdata folder with language files (15-100MB each)
using Tesseract;

// tessdata folder must contain eng.traineddata
using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
using var img = Pix.LoadFromFile("document.png");
using var page = engine.Process(img);

Console.WriteLine(page.GetText());
Console.WriteLine($"Confidence: {page.GetMeanConfidence()}");
```

charlesw/tesseract is the standard free option and works well for clean, well-aligned images. For real-world documents (photos, faxes, aged scans), budget significant time for preprocessing code. IronOCR's automatic preprocessing handles these cases out of the box.

**Tesseract vs IronOCR in numbers:**
- Setup time: Tesseract ~2-4 hours (with tessdata, preprocessing) vs IronOCR ~5 minutes
- Lines of code for PDF OCR: Tesseract ~50-100 (with PDF library) vs IronOCR 1 line
- Preprocessing: Tesseract ~100-200 lines vs IronOCR automatic

---

### PaddleOCR

[Full Guide: ./paddleocr/](./paddleocr/)

Deep learning OCR from Baidu, wrapped for .NET via Sdcb.PaddleOCR. Excellent for Chinese/Japanese/Korean text with GPU acceleration support.

- **Best for:** CJK (Chinese, Japanese, Korean) text recognition where modern deep learning models outperform traditional OCR
- **Watch out for:** **Chinese origin**—factually, the models and tooling come from Baidu (China); model downloads required (~50MB); GPU setup is complex (CUDA/cuDNN configuration); newer library with smaller community than Tesseract
- **License:** Apache 2.0 (Free)

```csharp
// Install: Multiple packages required
// dotnet add package Sdcb.PaddleOCR
// dotnet add package Sdcb.PaddleOCR.Models.Online
// dotnet add package Sdcb.PaddleInference.runtime.win64.mkl
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models;

// Model download required on first run
FullOcrModel model = await OnlineFullModels.ChineseV4.DownloadAsync();
using var all = new PaddleOcrAll(model);

using var image = Cv2.ImRead("chinese-document.png");
PaddleOcrResult result = all.Run(image);
Console.WriteLine(result.Text);
```

PaddleOCR is a strong choice for CJK languages where Tesseract historically struggles. The deep learning approach produces excellent results on challenging text. For production use, expect to invest time in model management and GPU configuration. For most use cases, IronOCR's CJK language packs provide similar accuracy with simpler setup.

---

### RapidOcrNet

[Full Guide: ./rapidocr-net/](./rapidocr-net/)

Lightweight ONNX-based OCR using PP-OCR v5 models, wrapped for .NET with SkiaSharp integration.

- **Best for:** Developers who want PaddleOCR accuracy in a lighter-weight ONNX runtime package
- **Watch out for:** Thin wrapper with limited community support; technology chain (Baidu -> RapidOCR -> RapidOcrNet) means you're multiple abstraction layers from the original; limited language support compared to Tesseract's 100+
- **License:** Apache 2.0 (Free)

```csharp
// Install: dotnet add package RapidOcrNet
using RapidOcrNet;

using var ocr = new RapidOcr();
using var image = SKBitmap.Decode("document.png");
var result = ocr.Detect(image);

foreach (var line in result.TextBlocks)
    Console.WriteLine(line.Text);
```

RapidOcrNet offers an interesting alternative to PaddleOCR with a simpler runtime. However, the limited language support and thin wrapper status make it better for experimentation than production workloads.

---

### Tesseract.NET SDK

[Full Guide: ./tesseract-net-sdk/](./tesseract-net-sdk/)

Commercial Tesseract wrapper from Patagames with 120+ language support. Targets Windows and .NET Framework.

- **Best for:** Legacy Windows/.NET Framework applications that need commercial support for Tesseract
- **Watch out for:** **Windows-only**—explicitly does not support macOS, Linux, or Docker; commercial pricing for what is essentially a Tesseract wrapper; limited differentiation over free alternatives
- **License:** Commercial :warning:

```csharp
// Install: dotnet add package Tesseract.Net.SDK
// Windows only - no Linux/macOS/Docker support
using Patagames.Ocr;

using var engine = OcrApi.Create();
engine.Init("eng");
string text = engine.GetTextFromImage("document.png");
Console.WriteLine(text);
```

If you're locked into Windows/.NET Framework and need commercial support, Tesseract.NET SDK fills a niche. For modern cross-platform .NET development, IronOCR provides more value with true cross-platform support.

---

### TesseractOCR

[Full Guide: ./tesseractocr/](./tesseractocr/)

Active fork of charlesw/tesseract wrapping Tesseract 5.4.1, targeting .NET 6.0+.

- **Best for:** .NET 6/7/8 projects wanting the latest Tesseract 5.4.1 engine with modern .NET targeting
- **Watch out for:** Fork maintenance depends on volunteer availability; inherits Tesseract's preprocessing requirements; smaller community than charlesw original
- **License:** Apache 2.0 (Free)

```csharp
// Install: dotnet add package TesseractOCR
// Same tessdata requirements as charlesw/tesseract
using TesseractOCR;

using var engine = new Engine(@"./tessdata", Language.English);
using var img = Pix.LoadFromFile("document.png");
using var page = engine.Process(img);

Console.WriteLine(page.Text);
```

TesseractOCR is a solid modern fork if you specifically need Tesseract 5.4.1 features on .NET 6+. The fundamental trade-offs (manual preprocessing, tessdata management) remain the same as charlesw/tesseract.

---

### TesseractOcrMaui

[Full Guide: ./tesseract-maui/](./tesseract-maui/)

Tesseract wrapper specifically for .NET MAUI mobile applications, created by community developer Henri Vainio.

- **Best for:** MAUI mobile apps that specifically want Tesseract and can accept the platform lock-in
- **Watch out for:** **MAUI-only lock-in**—cannot use in ASP.NET, console apps, Windows Forms, WPF, or any non-MAUI project; community project risk (single developer, ~33.9K downloads vs millions for mainstream wrappers); inherits all Tesseract preprocessing requirements
- **License:** MIT (Free)

```csharp
// Install: dotnet add package TesseractOcrMaui
// MAUI projects only - will not work in ASP.NET, console, WPF, etc.
using TesseractOcrMaui;

var ocr = new TesseractOcr();
await ocr.InitAsync();
var result = await ocr.RecognizeTextAsync(imageBytes);
Console.WriteLine(result.RecognizedText);
```

TesseractOcrMaui solves a narrow problem (Tesseract in MAUI) but creates platform lock-in. IronOCR offers dedicated `IronOcr.Android` and `IronOcr.iOS` packages that work across mobile AND server deployments without code changes.

---

## License Warnings: AGPL/GPL Copyleft Risks

### The Open Source License Trap

Many developers unknowingly walk into a licensing trap with AGPL and GPL libraries. Understanding copyleft licenses is critical before shipping production software.

### How Copyleft Works

**AGPL (Affero General Public License)** and **GPL (General Public License)** are "copyleft" licenses. When you use AGPL/GPL code in your application:

1. **Your entire codebase may become infected** - Linking to AGPL/GPL code can require you to release YOUR source code under the same license
2. **SaaS applications are not exempt** - AGPL specifically targets web applications; if users interact with your software over a network, you may be required to offer source code
3. **"Internal use only" is murky** - Even internal tools may trigger copyleft obligations depending on interpretation
4. **Retroactive compliance is expensive** - Discovering license issues after shipping can require expensive rewrites or legal settlements

### The iText Story: A Cautionary Tale

iText, a popular PDF library, provides a perfect example of license risk:

1. **2000-2009:** iText was MIT/LGPL licensed—permissive, safe for commercial use
2. **2009:** iText changed to AGPL—suddenly, commercial users faced copyleft obligations
3. **Today:** iText offers commercial licenses at **$4,000+/year**, and they actively pursue companies using old versions

Developers who built applications on "free" iText now face:
- Pay thousands annually for commercial license
- Open-source their entire application (usually impossible)
- Rewrite their PDF functionality from scratch

### OCR Libraries with Copyleft Concerns

Most OCR libraries in this guide use permissive licenses (Apache 2.0, MIT, commercial). However, always verify:

| Library | License | Copyleft Risk |
|---------|---------|---------------|
| Tesseract engine | Apache 2.0 | Low |
| Most Tesseract wrappers | Apache 2.0/MIT | Low |
| IronOCR | Commercial | None |
| Aspose.OCR | Commercial | None |
| Cloud APIs | Terms of Service | None (different concerns) |

### Protecting Your Business

1. **Always verify licenses** before adding dependencies—use tools like `license-checker` or `dotnet-delice`
2. **Prefer permissive licenses** (MIT, Apache 2.0, BSD) or commercial licenses with clear terms
3. **Commercial licenses = insurance** - You're paying for legal clarity and freedom from copyleft risk
4. **Document your license audit** - Know exactly what's in your dependency tree

### IronOCR's Commercial License Advantage

IronOCR's commercial license means:
- Your code remains 100% yours
- No copyleft obligations
- No risk of license changes affecting deployed software
- Clear legal terms reviewed by your legal team
- Perpetual license option—no ongoing fees

The $749-$2,999 for IronOCR isn't just paying for software—it's paying for legal peace of mind.

---

## Use Case Recommendations

### By Document Type

| Document Type | Recommended Solution | Why |
|---------------|---------------------|-----|
| **Scanned office documents** | [IronOCR](./ironocr/) | Automatic preprocessing handles scan quality variance |
| **PDFs (scanned)** | [IronOCR](./ironocr/) | Native PDF input AND searchable PDF output |
| **Phone photos of documents** | [IronOCR](./ironocr/) | Deskew + denoise handles photo quality |
| **Invoices/receipts (on-premise)** | [IronOCR](./ironocr/) + regex | Keep financial data on your servers |
| **Invoices/receipts (cloud OK)** | [AWS Textract](./aws-textract/) | Built-in form extraction |
| **Passports/IDs** | [IronOCR](./ironocr/) ReadPassport() | Built-in MRZ extraction |
| **Handwriting** | [ABBYY](./abbyy-finereader/) or [Azure CV](./azure-computer-vision/) | ICR (intelligent character recognition) |
| **Chinese/Japanese/Korean** | [PaddleOCR](./paddleocr/) or [IronOCR](./ironocr/) | Deep learning models excel at CJK |
| **Historical documents** | [ABBYY](./abbyy-finereader/) | Best accuracy on degraded text |

### By Deployment Environment

| Environment | Recommended Solution | Why |
|-------------|---------------------|-----|
| **Windows server** | [IronOCR](./ironocr/) or [Tesseract](./tesseract/) | Both work well on Windows |
| **Linux server** | [IronOCR](./ironocr/) | Full cross-platform support |
| **Docker container** | [IronOCR](./ironocr/) | Single package, no native binary configuration |
| **Azure Functions** | [IronOCR](./ironocr/) or [Azure CV](./azure-computer-vision/) | Both work in serverless |
| **AWS Lambda** | [IronOCR](./ironocr/) or [AWS Textract](./aws-textract/) | Both work in Lambda |
| **Air-gapped network** | [IronOCR](./ironocr/) or [Tesseract](./tesseract/) | No internet required |
| **MAUI mobile app** | [IronOCR](./ironocr/) | Dedicated iOS/Android packages |
| **Xamarin app** | [IronOCR](./ironocr/) | Cross-platform mobile support |

### By Budget

| Budget | Recommended Path | Trade-offs |
|--------|------------------|------------|
| **$0** | [charlesw/tesseract](./tesseract/) | Invest 20-40 hours in preprocessing, deployment |
| **$749** | [IronOCR Lite](https://ironsoftware.com/csharp/ocr/licensing/) | Single developer, unlimited usage |
| **$1,499** | [IronOCR Professional](https://ironsoftware.com/csharp/ocr/licensing/) | Up to 10 developers |
| **$2,999** | [IronOCR Enterprise](https://ironsoftware.com/csharp/ocr/licensing/) | Unlimited developers |
| **$10,000+** | [ABBYY FineReader](./abbyy-finereader/) | Maximum accuracy for critical workflows |

---

## Detailed Use Case Guides

For in-depth guidance on specific OCR scenarios, explore our dedicated use case guides:

| Use Case | Guide | What You'll Learn |
|----------|-------|-------------------|
| **Document Digitization** | [Guide](./dotnet-ocr-use-cases/document-digitization.md) | Scanning paper archives to searchable PDFs at scale |
| **Invoice & Receipt OCR** | [Guide](./dotnet-ocr-use-cases/invoice-receipt-ocr.md) | Extracting structured data from financial documents |
| **Form Processing** | [Guide](./dotnet-ocr-use-cases/form-processing.md) | Automating data entry from paper forms |
| **PDF Text Extraction** | [Guide](./dotnet-ocr-use-cases/pdf-text-extraction.md) | Working with scanned and mixed PDFs |
| **Business Card Scanning** | [Guide](./dotnet-ocr-use-cases/business-card-scanning.md) | Contact information extraction |
| **Screenshot OCR** | [Guide](./dotnet-ocr-use-cases/screenshot-ocr.md) | Extracting text from screen captures |
| **Medical & Legal Documents** | [Guide](./dotnet-ocr-use-cases/medical-legal-documents.md) | Compliance-aware document processing |
| **Check & Bank Documents** | [Guide](./dotnet-ocr-use-cases/check-bank-documents.md) | MICR and financial document handling |

Browse all use cases: [./dotnet-ocr-use-cases/](./dotnet-ocr-use-cases/)

---

## Feature Comparison Matrix

### Core OCR Features

| Feature | IronOCR | Tesseract | Azure CV | AWS Textract | Aspose.OCR |
|---------|---------|-----------|----------|--------------|------------|
| **PDF input** | Native | External library | Yes | Yes | Yes |
| **Searchable PDF output** | Native | External library | No | No | Yes |
| **Password-protected PDFs** | Yes | No | No | No | Yes |
| **Auto preprocessing** | Yes | No | Cloud-side | Cloud-side | Yes |
| **Deskew** | Yes | Manual | Cloud-side | Cloud-side | Yes |
| **Denoise** | Yes | Manual | Cloud-side | Cloud-side | Yes |
| **Barcode reading** | Yes | No | No | No | No |
| **Thread-safe** | Yes | Limited | Yes | Yes | Yes |
| **Languages** | 125+ | 100+ | 120+ | 25+ | 130+ |
| **Offline capable** | Yes | Yes | No | No | Yes |
| **Setup complexity** | 1 NuGet | tessdata + config | Azure subscription | AWS credentials | 1 NuGet |

### Platform Support

| Platform | IronOCR | Tesseract | Azure CV | AWS Textract | PaddleOCR |
|----------|---------|-----------|----------|--------------|-----------|
| **Windows** | Yes | Yes | Yes | Yes | Yes |
| **macOS** | Yes | Yes | Yes | Yes | Yes |
| **Linux** | Yes | Yes | Yes | Yes | Yes |
| **Docker** | Yes | Complex | Yes | Yes | Complex |
| **Azure Functions** | Yes | Complex | Yes | N/A | Complex |
| **AWS Lambda** | Yes | Complex | N/A | Yes | Complex |
| **iOS** | Yes | No | Via API | Via API | No |
| **Android** | Yes | No | Via API | Via API | No |

---

## Pricing Comparison

### 3-Year Total Cost Analysis

*Assuming 10,000 pages/month processing volume:*

| Solution | Year 1 | Year 2 | Year 3 | 3-Year Total |
|----------|--------|--------|--------|--------------|
| **IronOCR Lite** | $749 | $0 | $0 | $749 |
| **IronOCR Professional** | $1,499 | $0 | $0 | $1,499 |
| **Tesseract** | $0 + dev time | $0 | $0 | Dev time only |
| **Aspose.OCR** | $999 | $999 | $999 | $2,997+ |
| **Azure CV** | ~$1,800 | ~$1,800 | ~$1,800 | ~$5,400 |
| **AWS Textract** | ~$1,800 | ~$1,800 | ~$1,800 | ~$5,400 |
| **ABBYY** | $10,000+ | $10,000+ | $10,000+ | $30,000+ |

*Cloud costs estimated at $15/month for 10K pages. Actual costs vary by region and features used.*

### Break-Even Analysis

At what point does IronOCR pay for itself vs cloud APIs?

| Monthly Volume | Azure/AWS Monthly Cost | IronOCR Break-Even |
|----------------|------------------------|-------------------|
| 1,000 pages | ~$1.50 | 42+ months |
| 5,000 pages | ~$7.50 | 8+ months |
| 10,000 pages | ~$15 | 4+ months |
| 50,000 pages | ~$75 | 1 month |
| 100,000 pages | ~$150 | 2 weeks |

**Bottom line:** For consistent processing volume, one-time licensing is dramatically more cost-effective than per-page cloud pricing.

---

## Security and Compliance

### Data Residency Requirements

| Compliance | Cloud OCR | On-Premise OCR |
|------------|-----------|----------------|
| **HIPAA** | BAA required, verify scope | Data stays local |
| **GDPR** | DPA required, EU region | Full control |
| **ITAR** | Not recommended | Required |
| **FedRAMP** | GovCloud only | N/A |
| **CMMC** | Complex verification | Simpler |
| **PCI-DSS** | Scope implications | Reduce scope |
| **SOX** | Audit trails needed | Full control |

### When to Avoid Cloud OCR

- Medical records (HIPAA)
- Legal contracts (privilege concerns)
- Financial statements (competitive intelligence)
- Government/military documents (classification)
- Trade secrets (IP protection)
- Customer data in regulated industries

### On-Premise Solutions

For maximum data security, use on-premise OCR:
- [IronOCR](./ironocr/) - Commercial, full-featured
- [Tesseract](./tesseract/) - Free, requires setup
- [LEADTOOLS](./leadtools-ocr/) - Enterprise, complex
- [GdPicture](./gdpicture-net/) - Commercial, imaging-focused

---

## Contributing

We welcome contributions! See [CONTRIBUTING.md](./CONTRIBUTING.md) for guidelines on:

- Adding new OCR libraries
- Improving existing documentation
- Fixing code examples
- Reporting inaccuracies

This repository is licensed under [CC0 1.0 Universal](./LICENSE)—the content is public domain. Individual libraries have their own licenses.

---

## License

This repository content is dedicated to the public domain under [CC0 1.0 Universal](./LICENSE).

**Important:** The libraries documented here have their own licenses. Always verify licensing terms before using any library in your project.

---

## IronOCR Resources

**Ready to try the OCR library that inspired this guide?**

- **Website:** [https://ironsoftware.com/csharp/ocr/](https://ironsoftware.com/csharp/ocr/)
- **NuGet:** [https://www.nuget.org/packages/IronOcr/](https://www.nuget.org/packages/IronOcr/)
- **Documentation:** [https://ironsoftware.com/csharp/ocr/docs/](https://ironsoftware.com/csharp/ocr/docs/)
- **Tutorials:** [https://ironsoftware.com/csharp/ocr/tutorials/](https://ironsoftware.com/csharp/ocr/tutorials/)
- **Free Trial:** [https://ironsoftware.com/csharp/ocr/#trial-license](https://ironsoftware.com/csharp/ocr/#trial-license)
- **GitHub Examples:** [https://github.com/iron-software/IronOCR-Examples](https://github.com/iron-software/IronOCR-Examples)

**About the Author:**
- Jacob Mellor, CTO of Iron Software
- [Author Page](https://ironsoftware.com/about-us/authors/jacobmellor/)
- [LinkedIn](https://www.linkedin.com/in/jacob-mellor-iron-software/)

---

<p align="center">
  <b>Need C# OCR that just works? Start with <a href="https://ironsoftware.com/csharp/ocr/">IronOCR</a>—the simplest path to production.</b>
</p>

<p align="center">
  <a href="https://ironsoftware.com/csharp/ocr/">Website</a> •
  <a href="https://www.nuget.org/packages/IronOcr/">NuGet</a> •
  <a href="https://ironsoftware.com/csharp/ocr/docs/">Documentation</a> •
  <a href="https://ironsoftware.com/csharp/ocr/#trial-license">Free Trial</a>
</p>

---

*Last updated: January 2026*

*Compiled by Jacob Mellor, CTO of Iron Software*
