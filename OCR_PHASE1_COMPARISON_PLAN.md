# OCR Phase 1: Comparison Articles Execution Plan

## Overview

This plan covers creating **27 comparison articles**, one per competitor, comparing each OCR library to IronOCR. Each article follows the gold standard structure from `_gold-standard/reference-compare-xfinium-pdf-vs-ironpdf.md` adapted for OCR.

**Product:** IronOCR (NuGet: `IronOcr`, namespace: `IronOcr`)
**Output per competitor:** `[competitor-dir]/compare-[competitor]-vs-ironocr.md`
**Agents:** 27 parallel, one per competitor

## IronOCR Reference

| Property | Value |
|----------|-------|
| NuGet | `IronOcr` |
| Namespace | `IronOcr` |
| Product URL | https://ironsoftware.com/csharp/ocr/ |
| Docs | https://ironsoftware.com/csharp/ocr/docs/ |
| Tutorials | https://ironsoftware.com/csharp/ocr/tutorials/ |
| Pricing | $749 Lite / $1,499 Professional / $2,999 Enterprise (perpetual); SaaS subscription also available |
| Key classes | `IronTesseract`, `OcrInput`, `OcrResult`, `CropRectangle` |
| Key features | Automatic preprocessing (Deskew, DeNoise, Contrast, Binarize, EnhanceResolution), native PDF input, searchable PDF output, 125+ languages via NuGet packs, thread-safe, cross-platform, barcode reading during OCR, structured data extraction, region-based OCR |

## Gold Standard Article Structure

Every article must follow this 13-section structure exactly. No sections may be added, removed, or reordered.

```
1.  Opening paragraph (no heading, no title, no byline)
2.  ## Understanding [Competitor]
        - Neutral factual description
        - Bullet list of key architectural characteristics
        ### [Key Technical Aspect]  <-- subsection with code example
3.  ## Understanding IronOCR
        - Neutral factual description
        - Bullet list of key characteristics
4.  ## Feature Comparison
        - Brief overview table (6-8 rows)
        ### Detailed Feature Comparison
        - Expanded table (15-20 rows, grouped by category)
5.  ## [Technical Comparison Area 1]
        ### [Competitor] Approach       <-- code block + explanation
        ### IronOCR Approach            <-- code block + contextual link
6.  ## [Technical Comparison Area 2]  (same pattern)
7.  ## [Technical Comparison Area 3]  (2-4 total comparison areas)
8.  ## API Mapping Reference
        - Table: Competitor API --> IronOCR equivalent
9.  ## When Teams Consider Moving from [Competitor] to IronOCR
        ### [Scenario 1]   <-- prose paragraph, no bullet list
        ### [Scenario 2]
        ### [Scenario 3]   (3-5 scenarios, each a named subsection)
10. ## Common Migration Considerations
        ### [Technical Note 1]  <-- brief prose + code snippet
        ### [Technical Note 2]  (3-4 notes, NOT a checklist)
11. ## Additional IronOCR Capabilities
        - Bullet list with contextual links
12. ## .NET Compatibility and Future Readiness
        - Single paragraph
13. ## Conclusion
        - 3-4 balanced paragraphs, no code, no new claims
```

## Voice and Tone

Write like a senior .NET developer explaining trade-offs to a peer over coffee. Not a marketing team. Not a textbook. Not a blog mill.

### What "senior dev tone" means concretely

**DO write like this:**
> Tesseract gives you raw engine access for free. The catch: you budget 20-40 hours writing preprocessing code, managing tessdata folders across environments, and debugging platform-specific native binary issues. IronOCR wraps an optimized Tesseract 5 engine with automatic preprocessing — one NuGet package, no tessdata management, no native binary headaches.

**DO NOT write like this:**
> Tesseract is an open-source OCR engine that provides text recognition capabilities. However, it requires additional configuration for optimal results. IronOCR offers a more streamlined approach that many development teams find beneficial for their OCR needs.

The first version has an opinion, a concrete time estimate, and reads like someone who has actually hit the problem. The second version says nothing. It is the AI-generated voice we must avoid.

### Specific anti-robotic rules

1. **Lead with the pain, not the product.** Every paragraph about a limitation should describe what goes wrong in practice before suggesting what fixes it.
2. **Use concrete numbers, not vague claims.** "100+ lines of preprocessing code" beats "significant development effort." "$749 one-time" beats "cost-effective pricing."
3. **Short paragraphs.** 2-4 sentences max for explanation paragraphs after code blocks. If you cannot say it in 4 sentences, the point is too vague.
4. **Code comments are terse.** `// Manual tessdata path — breaks on deployment` not `// Here we configure the tessdata path which needs to be set correctly for the OCR engine to function properly`
5. **No hedge words.** Delete "may", "might", "could potentially", "in certain scenarios", "some developers find that." State the fact or do not state it.
6. **No transition filler.** Delete "Moving on to", "Next, let us examine", "It is worth noting that", "As mentioned earlier", "Having covered X, let us now turn to Y."
7. **Every sentence must earn its place.** If a sentence could be deleted and the paragraph still makes sense, delete it.
8. **Vary sentence length.** Mix short punchy sentences (5-8 words) with longer technical ones. Monotone sentence length is the #1 tell of AI prose.
9. **No "comprehensive" or "robust."** These words are content-free. Say what it actually does.
10. **Opening paragraphs set the hook in the first sentence.** Do not warm up. No "When .NET developers evaluate OCR libraries, they often consider..." — go straight to the specific tension.

## Style Rules

- Every code block uses correct, current IronOCR API.
- Each article has a distinct hook. No two articles may feel like the same template with names swapped.
- No contractions anywhere (do not, cannot, it is -- never don't, can't, it's).
- No `---` horizontal separators.
- No bylines or author attribution.
- No tables of contents.
- 8-12 contextual IronOCR hyperlinks per article, distributed evenly with clusters every ~4th paragraph. See Master Link Catalog below.

## Anti-Repetition Rules (Section Ownership)

Each section owns specific content and must NOT contain content that belongs to other sections.

| Section | Owns | Must NOT Contain |
|---------|------|------------------|
| Opening paragraph | Single most important pain point as hook | Comparison detail, code, IronOCR features |
| Understanding [Competitor] | Architecture, design, positioning | Any IronOCR mention |
| Understanding IronOCR | IronOCR design and approach | Competitor weaknesses |
| Feature Comparison tables | Capability matrix (yes/no/value) | Explanations of why capabilities matter |
| Technical Comparison sections | How and why with code side-by-side | Restating table rows as prose |
| API Mapping Reference | Name-to-name translation only | Code examples |
| When Teams Consider Moving | Real-world scenarios (business context) | Technical implementation already covered |
| Common Migration Considerations | Brief forward-looking technical notes | Full before/after code |
| Additional IronOCR Capabilities | Features not discussed elsewhere | Anything already mentioned |
| .NET Compatibility | Version support, future readiness | Anything from other sections |
| Conclusion | Synthesis and verdict | Code, new claims, repetition of earlier content |

## IronOCR API Quick Reference

Agents must use these exact API patterns. Do not invent API methods that do not exist.

```csharp
// Install
dotnet add package IronOcr

// License
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

// Basic OCR
var text = new IronTesseract().Read("document.jpg").Text;

// PDF OCR
var pdfText = new IronTesseract().Read("scanned.pdf").Text;

// Password-protected PDF
using var input = new OcrInput();
input.LoadPdf("encrypted.pdf", Password: "secret");
var result = new IronTesseract().Read(input);

// Searchable PDF output
var result = new IronTesseract().Read("scanned.pdf");
result.SaveAsSearchablePdf("searchable-output.pdf");

// Preprocessing
using var input = new OcrInput();
input.LoadImage("low-quality.jpg");
input.Deskew();
input.DeNoise();
input.Contrast();
input.Binarize();
input.EnhanceResolution(300);
var result = new IronTesseract().Read(input);

// Multi-language
var ocr = new IronTesseract();
ocr.Language = OcrLanguage.French;
ocr.AddSecondaryLanguage(OcrLanguage.German);
var result = ocr.Read("document.jpg");

// Structured data extraction
var result = new IronTesseract().Read("document.jpg");
foreach (var page in result.Pages) { ... }
foreach (var line in result.Lines) { ... }
foreach (var word in result.Words) { ... }

// Region-based OCR
var region = new CropRectangle(0, 0, 600, 100);
using var input = new OcrInput();
input.LoadImage("invoice.jpg", region);
var text = new IronTesseract().Read(input).Text;

// Barcode reading during OCR
var ocr = new IronTesseract();
ocr.Configuration.ReadBarCodes = true;
var result = ocr.Read("document.jpg");
foreach (var barcode in result.Barcodes) { ... }

// Confidence score
Console.WriteLine($"Confidence: {result.Confidence}%");
```

## Agent Inputs

Each agent receives:

1. **Competitor README:** `[competitor-dir]/README.md` (detailed competitor info, pain points, pricing)
2. **Competitor .cs files:** All `.cs` files in `[competitor-dir]/` (code patterns and API examples)
3. **This plan:** `OCR_PHASE1_COMPARISON_PLAN.md`
4. **Gold standard:** `_gold-standard/reference-compare-xfinium-pdf-vs-ironpdf.md`
5. **IronOCR README:** `ironocr/README.md`
6. **IronOCR .cs files:** All `.cs` files in `ironocr/` (canonical IronOCR code patterns)

## Agent Execution Steps

1. Read the competitor README to understand pain points, API patterns, pricing, and positioning.
2. Read all competitor .cs files for concrete code examples and API names.
3. Read the IronOCR README and .cs files for correct IronOCR API usage.
4. Read the gold standard reference to understand exact structure, tone, and depth.
5. Create the comparison article following the 13-section structure exactly.
6. Use competitor-specific code from their .cs files (do not fabricate API calls).
7. Use correct IronOCR API code from the quick reference and ironocr .cs files.
8. Verify: zero contractions, zero `---` separators, zero bylines, zero tables of contents.

## Thin-Source Competitors (HIGH HALLUCINATION RISK)

The following 6 competitors have critically thin .cs source material (single file, under 350 lines). Agents working on these MUST:

1. **Use ONLY API names, classes, and methods that appear in the .cs file and README.** Do not guess or invent competitor API calls.
2. **If you lack enough competitor code for a code example section, show the conceptual pattern** with a comment like `// Simplified — see [competitor] documentation for full API` rather than fabricating specific method signatures.
3. **Lean harder on the README** for these competitors — it is the primary source of competitor facts.
4. **Keep competitor code blocks shorter** rather than padding with invented details.

| Competitor | .cs Files | Total Lines | Risk |
|-----------|:---------:|:-----------:|------|
| paddlesharp-ocr | 1 | 167 | CRITICAL |
| klippa-ocr | 1 | 241 | CRITICAL |
| windows-media-ocr | 1 | 298 | CRITICAL |
| patagames-tesseract | 1 | 315 | CRITICAL |
| tesseract-ocr-wrapper | 1 | 328 | CRITICAL |
| charlesw-tesseract | 1 | 336 | CRITICAL |

## Tesseract Cluster Differentiation Mandate

6 of the 27 competitors are Tesseract wrappers. A reader may encounter multiple articles. Each MUST have a genuinely different angle — not just different headings over the same substance.

| Competitor | Mandatory Angle | Must NOT Overlap With |
|-----------|----------------|----------------------|
| tesseract | Preprocessing gap + PDF gap (the "big two" Tesseract limitations) | Any other Tesseract article |
| charlesw-tesseract | Archived/unmaintained status + native binary deployment headaches | tesseract article |
| tesseractocr | Modern fork (Tesseract 5.4.1 / .NET 6+) but same fundamental limits | charlesw-tesseract |
| patagames-tesseract | Windows-only + commercial price for free engine | tesseractocr |
| tesseract-net-sdk | Legacy .NET Framework targeting + same Patagames but different era | patagames-tesseract |
| tesseract-maui | MAUI-only lock-in (no server, no desktop non-MAUI) | All others |
| tesseract-ocr-wrapper | API completeness gaps + error handling inconsistency | tesseractocr |

**Enforcement:** Each Tesseract article's opening paragraph must name the specific angle in the first two sentences. The "Understanding [Competitor]" section must focus 80% on that angle, not generic Tesseract limitations.

## Competitor List (27 Total)

### On-Premise Commercial (9)

#### 1. abbyy-finereader

- **Directory:** `abbyy-finereader/`
- **Output:** `abbyy-finereader/compare-abbyy-finereader-vs-ironocr.md`
- **Source files:** `README.md`, `abbyy-migration-comparison.cs`, `abbyy-sdk-integration.cs`, `abbyy-vs-ironocr-examples.cs`
- **Hook:** Enterprise pricing ($10K+/year), mandatory sales engagement, complex SDK installation with no simple NuGet path
- **Compare areas:** Accuracy vs simplicity, pricing model, setup complexity, deployment model
- **Pain:** For 99% of OCR use cases, IronOCR delivers 95-99% accuracy at $749 perpetual vs ABBYY at $10K+/year for marginal accuracy gains
- **Tone:** Respectful. ABBYY is the accuracy benchmark. Comparison centers on cost/complexity vs marginal accuracy advantage.

#### 2. aspose-ocr

- **Directory:** `aspose-ocr/`
- **Output:** `aspose-ocr/compare-aspose-ocr-vs-ironocr.md`
- **Source files:** `README.md`, `aspose-ocr-examples.cs`, `aspose-ocr-pdf-processing.cs`, `aspose-ocr-preprocessing-comparison.cs`
- **Hook:** Subscription-only pricing ($999+/year per developer, no perpetual option), costs escalate for teams
- **Compare areas:** Pricing model (subscription vs perpetual), preprocessing capability, PDF support, API verbosity
- **Pain:** 3-year TCO for a 5-developer team: Aspose approximately $15K+ vs IronOCR $2,999 one-time

#### 3. asprise-ocr

- **Directory:** `asprise-ocr/`
- **Output:** `asprise-ocr/compare-asprise-ocr-vs-ironocr.md`
- **Source files:** `README.md`, `asprise-basic-ocr.cs`, `asprise-migration-comparison.cs`, `asprise-vs-ironocr-examples.cs`
- **Hook:** Java-first architecture with .NET as a secondary citizen; LITE/STANDARD editions are single-threaded
- **Compare areas:** Native vs managed code, threading model, platform support, API design
- **Pain:** Java bridging adds complexity and deployment friction for .NET developers

#### 4. dynamsoft-ocr

- **Directory:** `dynamsoft-ocr/`
- **Output:** `dynamsoft-ocr/compare-dynamsoft-ocr-vs-ironocr.md`
- **Source files:** `README.md`, `dynamsoft-migration-comparison.cs`, `dynamsoft-mrz-recognition.cs`, `dynamsoft-vs-ironocr-examples.cs`
- **Hook:** MRZ/VIN/label specialist, not general-purpose OCR; full coverage requires multiple Dynamsoft products
- **Compare areas:** Specialist vs generalist scope, multi-product cost, deployment model
- **Pain:** MRZ + general OCR + PDF requires 3+ separate Dynamsoft products

#### 5. gdpicture-net

- **Directory:** `gdpicture-net/`
- **Output:** `gdpicture-net/compare-gdpicture-net-vs-ironocr.md`
- **Source files:** `README.md`, `gdpicture-migration-examples.cs`, `gdpicture-pdf-ocr.cs`, `gdpicture-vs-ironocr-examples.cs`
- **Hook:** Plugin-based pricing ($2K+ depending on plugins), integer Image ID lifecycle management, overkill for OCR-only use cases
- **Compare areas:** Plugin bundling cost, API complexity (Image ID pattern), memory management, OCR-only simplicity
- **Pain:** GdPicture is a document imaging SDK; if the requirement is only OCR, the complexity tax is unjustified

#### 6. kofax-omnipage

- **Directory:** `kofax-omnipage/`
- **Output:** `kofax-omnipage/compare-kofax-omnipage-vs-ironocr.md`
- **Source files:** `README.md`, `kofax-enterprise-ocr.cs`, `kofax-migration-comparison.cs`, `kofax-vs-ironocr-examples.cs`
- **Hook:** Enterprise-only ($10K+/year), heavy SDK installation (not NuGet), multiple ownership changes (now Tungsten Automation)
- **Compare areas:** Enterprise vs developer accessibility, deployment complexity, roadmap stability
- **Pain:** Enterprise procurement process vs NuGet install in 5 minutes

#### 7. leadtools-ocr

- **Directory:** `leadtools-ocr/`
- **Output:** `leadtools-ocr/compare-leadtools-ocr-vs-ironocr.md`
- **Source files:** `README.md`, `leadtools-migration-examples.cs`, `leadtools-pdf-processing.cs`, `leadtools-vs-ironocr-examples.cs`
- **Hook:** 40+ year SDK with runtime .LIC file deployment, bundle confusion, legacy API patterns
- **Compare areas:** License architecture (file-based vs string key), bundle complexity, API verbosity, setup overhead
- **Pain:** Runtime license file must be deployed with every installation; wrong bundle purchase is common

#### 8. syncfusion-ocr

- **Directory:** `syncfusion-ocr/`
- **Output:** `syncfusion-ocr/compare-syncfusion-ocr-vs-ironocr.md`
- **Source files:** `README.md`, `syncfusion-ocr-migration-comparison.cs`, `syncfusion-ocr-pdf-extraction.cs`, `syncfusion-vs-ironocr-examples.cs`
- **Hook:** Tesseract wrapper inside a PDF toolkit that still requires a tessdata folder; community license has strict eligibility ($1M revenue cap)
- **Compare areas:** Tesseract dependency (inherits all its limits), community license restrictions, preprocessing, suite purchase requirement
- **Pain:** Paying for a commercial wrapper around free Tesseract with no additional OCR intelligence

#### 9. ximage-ocr

- **Directory:** `ximage-ocr/`
- **Output:** `ximage-ocr/compare-ximage-ocr-vs-ironocr.md`
- **Source files:** `README.md`, `ximage-basic-ocr.cs`, `ximage-migration-comparison.cs`
- **Hook:** 10+ separate NuGet packages required, commercial pricing for essentially Tesseract underneath
- **Compare areas:** Package fragmentation, value proposition over free Tesseract, feature differentiation
- **Pain:** Limited differentiation from free Tesseract wrappers despite commercial licensing

### Cloud OCR APIs (7)

#### 10. aws-textract

- **Directory:** `aws-textract/`
- **Output:** `aws-textract/compare-aws-textract-vs-ironocr.md`
- **Source files:** `README.md`, `aws-textract-async-processing.cs`, `aws-textract-migration-examples.cs`, `aws-textract-vs-ironocr-examples.cs`
- **Hook:** Per-page pricing adds up at scale ($0.0015+/page), data transmitted to AWS servers, async polling pattern required
- **Compare areas:** Cost at scale, data sovereignty/compliance, latency, offline capability, API simplicity
- **Pain:** 10K pages/month = $180+/year ongoing vs $749 one-time perpetual

#### 11. azure-computer-vision

- **Directory:** `azure-computer-vision/`
- **Output:** `azure-computer-vision/compare-azure-computer-vision-vs-ironocr.md`
- **Source files:** `README.md`, `azure-cost-calculator-examples.cs`, `azure-vs-ironocr-examples.cs`
- **Hook:** Azure subscription required, per-transaction pricing, async Read API requires polling pattern
- **Compare areas:** Cost model, data sovereignty, polling vs synchronous, offline capability
- **Pain:** Cloud dependency + per-transaction cost + async complexity vs one-line local OCR

#### 12. google-cloud-vision

- **Directory:** `google-cloud-vision/`
- **Output:** `google-cloud-vision/compare-google-cloud-vision-vs-ironocr.md`
- **Source files:** `README.md`, `google-cloud-vision-migration-examples.cs`, `google-security-considerations.cs`, `google-vision-vs-ironocr-examples.cs`
- **Hook:** GCP dependency, per-image pricing ($1.50/1000), service account credential complexity
- **Compare areas:** CJK strength, cost model, authentication complexity, data sovereignty
- **Pain:** Strong accuracy but cloud-only with ongoing costs and mandatory data transmission

#### 13. klippa-ocr

- **Directory:** `klippa-ocr/`
- **Output:** `klippa-ocr/compare-klippa-ocr-vs-ironocr.md`
- **Source files:** `README.md`, `klippa-vs-ironocr-examples.cs`
- **Hook:** Cloud-only expense/identity document specialist, not general-purpose OCR
- **Compare areas:** Specialist vs generalist, data privacy, per-page pricing, deployment model
- **Pain:** Financial documents sent to external servers; narrow focus cannot handle general OCR needs

#### 14. mindee

- **Directory:** `mindee/`
- **Output:** `mindee/compare-mindee-vs-ironocr.md`
- **Source files:** `README.md`, `mindee-invoice-extraction.cs`, `mindee-migration-comparison.cs`, `mindee-vs-ironocr-examples.cs`
- **Hook:** Cloud-based invoice/receipt specialist with financial documents sent to external servers
- **Compare areas:** Data privacy for financial docs, specialist vs generalist, per-page pricing
- **Pain:** Bank accounts and transaction amounts transmitted to third-party servers

#### 15. ocrspace

- **Directory:** `ocrspace/`
- **Output:** `ocrspace/compare-ocrspace-vs-ironocr.md`
- **Source files:** `README.md`, `ocrspace-api-client.cs`, `ocrspace-migration-comparison.cs`, `ocrspace-vs-ironocr-examples.cs`
- **Hook:** No official .NET SDK (manual REST client required), variable accuracy, hobby-tier positioning
- **Compare areas:** SDK quality, accuracy consistency, production readiness, free tier limits
- **Pain:** No NuGet package, manual HTTP calls, variable quality: not production-grade

#### 16. veryfi

- **Directory:** `veryfi/`
- **Output:** `veryfi/compare-veryfi-vs-ironocr.md`
- **Source files:** `README.md`, `veryfi-migration-comparison.cs`, `veryfi-receipt-extraction.cs`, `veryfi-vs-ironocr-examples.cs`
- **Hook:** Cloud receipt/expense specialist with sensitive financial data transmitted to external servers
- **Compare areas:** Data privacy, specialist scope, per-document cost, deployment model
- **Pain:** Expense documents containing bank accounts and spending patterns routed through cloud servers

### Open Source / Tesseract-Based (11)

#### 17. tesseract (charlesw/Tesseract wrapper)

- **Directory:** `tesseract/`
- **Output:** `tesseract/compare-tesseract-vs-ironocr.md`
- **Source files:** `README.md`, `basic-text-extraction-tesseract.cs`, `image-preprocessing-tesseract.cs`, `multi-language-tesseract.cs`, `pdf-conversion-tesseract.cs`, `pdf-ocr-processing-tesseract.cs`
- **Hook:** Most popular free wrapper but requires manual preprocessing (100+ lines), tessdata management, no PDF support
- **Compare areas:** Setup time (2-4 hours vs 5 min), preprocessing (manual vs automatic), PDF support, thread safety
- **Pain:** Free but budget 20-40 hours for preprocessing code, deployment configuration, and tessdata management
- **Tone:** Most balanced. Tesseract is genuinely good and free. Comparison focuses on effort/capability, not quality.

#### 18. charlesw-tesseract

- **Directory:** `charlesw-tesseract/`
- **Output:** `charlesw-tesseract/compare-charlesw-tesseract-vs-ironocr.md`
- **Source files:** `README.md`, `tesseract-vs-ironocr-examples.cs`
- **Hook:** Original charlesw NuGet wrapper (archived), no longer maintained, still widely referenced in tutorials
- **Compare areas:** Maintenance status, API age, preprocessing gap, migration path from archived project
- **Pain:** Archived project with no updates; new projects should not depend on unmaintained wrappers

#### 19. paddleocr

- **Directory:** `paddleocr/`
- **Output:** `paddleocr/compare-paddleocr-vs-ironocr.md`
- **Source files:** `README.md`, `paddleocr-basic-ocr.cs`, `paddleocr-gpu-setup.cs`, `paddleocr-migration-comparison.cs`
- **Hook:** Baidu deep learning OCR with excellent CJK accuracy, but model downloads (~50MB), complex GPU setup (CUDA/cuDNN)
- **Compare areas:** CJK accuracy, setup complexity, model management, GPU dependency
- **Pain:** Strong for CJK but Python-first, complex CUDA setup, model management overhead

#### 20. paddlesharp-ocr

- **Directory:** `paddlesharp-ocr/`
- **Output:** `paddlesharp-ocr/compare-paddlesharp-ocr-vs-ironocr.md`
- **Source files:** `README.md`, `paddlesharp-vs-ironocr-examples.cs`
- **Hook:** Wrapper for a wrapper of PaddleOCR with multiple abstraction layers from the original, limited language support
- **Compare areas:** Abstraction depth (Baidu to RapidOCR to RapidOcrNet), community size, language coverage
- **Pain:** Technology chain creates maintenance risk; limited community support

#### 21. patagames-tesseract

- **Directory:** `patagames-tesseract/`
- **Output:** `patagames-tesseract/compare-patagames-tesseract-vs-ironocr.md`
- **Source files:** `README.md`, `patagames-vs-ironocr-examples.cs`
- **Hook:** Commercial Tesseract wrapper, Windows-only, charges commercial prices for free software
- **Compare areas:** Value over free wrappers, platform limitation, Tesseract inheritance
- **Pain:** Commercial license on free Tesseract with no obvious differentiation

#### 22. rapidocr-net

- **Directory:** `rapidocr-net/`
- **Output:** `rapidocr-net/compare-rapidocr-net-vs-ironocr.md`
- **Source files:** `README.md`, `rapidocr-basic-ocr.cs`, `rapidocr-migration-comparison.cs`, `rapidocr-vs-ironocr-examples.cs`
- **Hook:** Lightweight ONNX-based but thin wrapper, limited community, needs 4 separate ONNX model downloads
- **Compare areas:** Model management, community maturity, language support, production readiness
- **Pain:** Interesting experiment but not production-ready for enterprise use

#### 23. tesseract-net-sdk

- **Directory:** `tesseract-net-sdk/`
- **Output:** `tesseract-net-sdk/compare-tesseract-net-sdk-vs-ironocr.md`
- **Source files:** `README.md`, `tesseract-net-sdk-basic-ocr.cs`, `tesseract-net-sdk-migration-comparison.cs`, `tesseract-net-sdk-pdf-processing.cs`
- **Hook:** Commercial Tesseract wrapper from Patagames, Windows-only, targets legacy .NET Framework 2.0-4.5
- **Compare areas:** Platform coverage (Windows-only), .NET version support (legacy only), value proposition
- **Pain:** Paying commercial price for free Tesseract, locked to Windows and legacy .NET

#### 24. tesseractocr

- **Directory:** `tesseractocr/`
- **Output:** `tesseractocr/compare-tesseractocr-vs-ironocr.md`
- **Source files:** `README.md`, `tesseractocr-basic-ocr.cs`, `tesseractocr-migration-comparison.cs`, `tesseractocr-pdf-processing.cs`
- **Hook:** Active fork of charlesw wrapping Tesseract 5.4.1 for .NET 6+, but inherits all Tesseract limitations
- **Compare areas:** Modern .NET targeting, preprocessing gap, tessdata management, community vs charlesw
- **Pain:** Modern fork does not solve Tesseract fundamentals: no preprocessing, no PDF, manual tessdata

#### 25. tesseract-maui

- **Directory:** `tesseract-maui/`
- **Output:** `tesseract-maui/compare-tesseract-maui-vs-ironocr.md`
- **Source files:** `README.md`, `tesseract-maui-basic-ocr.cs`, `tesseract-maui-migration-comparison.cs`
- **Hook:** MAUI-only lock-in (cannot use in ASP.NET, console, WPF), single community developer, approximately 33.9K downloads
- **Compare areas:** Platform lock-in, maintenance risk (single developer), capabilities, Tesseract inheritance
- **Pain:** MAUI-only means no server-side, no desktop (non-MAUI), no cross-project reuse

#### 26. tesseract-ocr-wrapper

- **Directory:** `tesseract-ocr-wrapper/`
- **Output:** `tesseract-ocr-wrapper/compare-tesseract-ocr-wrapper-vs-ironocr.md`
- **Source files:** `README.md`, `tesseract-wrapper-vs-ironocr-examples.cs`
- **Hook:** Community Tesseract wrapper with simplified interface but inherits all Tesseract limitations
- **Compare areas:** Simplification value, community size, preprocessing gap, production readiness
- **Pain:** Simpler API surface but same fundamental Tesseract constraints underneath

#### 27. windows-media-ocr

- **Directory:** `windows-media-ocr/`
- **Output:** `windows-media-ocr/compare-windows-media-ocr-vs-ironocr.md`
- **Source files:** `README.md`, `windows-ocr-vs-ironocr-examples.cs`
- **Hook:** Free Windows built-in OCR but Windows 10/11 only with no Linux, no macOS, no Docker, no server
- **Compare areas:** Platform coverage, feature set, preprocessing, cross-platform deployment
- **Pain:** Platform lock-in makes it unsuitable for modern cross-platform .NET development

## Quality Checklist (Per Article)

Every article must pass all of these checks before being considered complete.

- [ ] Opens with a specific, non-generic hook about that competitor
- [ ] All code uses correct IronOCR API (verified against quick reference above)
- [ ] Feature comparison table has 15+ rows in the detailed section
- [ ] API mapping table uses confirmed competitor API names from the competitor .cs files
- [ ] Conclusion circles back to the opening hook
- [ ] No two articles share the same opening sentence structure
- [ ] Zero contractions anywhere in the article
- [ ] Zero `---` horizontal separators
- [ ] Zero bylines or author attribution
- [ ] Zero tables of contents
- [ ] Minimum 10 distinct contextual IronOCR hyperlinks included, distributed evenly with clusters every ~4th paragraph
- [ ] "Understanding [Competitor]" section has zero IronOCR mentions
- [ ] "Understanding IronOCR" section has zero competitor weakness mentions
- [ ] "When Teams Consider Moving" section has zero code blocks
- [ ] "Conclusion" section has zero code blocks, zero new claims
- [ ] "API Mapping Reference" has zero code examples (table only)
- [ ] "Additional IronOCR Capabilities" discusses only features NOT covered in earlier sections

## IronOCR Contextual Links

Agents must weave these links naturally throughout each article. **Target: 8-12 links per article.** Do NOT dump them all in one section. Distribution rule: spread links evenly across the article body, with a higher concentration (cluster of 2-3 links) roughly every 4th paragraph of prose.

### Link Placement Rules

1. **Opening + Understanding IronOCR sections:** 1-2 links (product page, docs).
2. **Feature Comparison tables:** 0 links (tables stay clean).
3. **Technical Comparison sections (code examples):** 4-6 links — this is the bulk zone. After each IronOCR code block explanation paragraph, include 1-2 relevant how-to or tutorial links.
4. **API Mapping Reference:** 0-1 links (table stays clean, optional docs link).
5. **When Teams Consider Moving:** 1-2 links (product page, licensing).
6. **Common Migration Considerations:** 2-3 links (relevant how-to guides).
7. **Additional IronOCR Capabilities:** 3-5 links (one per bullet, each linking to its feature page).
8. **Conclusion:** 1 link max (docs or tutorials hub).

### Master Link Catalog

Pick from these based on which features each article discusses. Every article must use at least 8 distinct links.

**Core Pages**

| Context | URL |
|---------|-----|
| IronOCR product page (first mention of IronOCR) | https://ironsoftware.com/csharp/ocr/ |
| Documentation hub | https://ironsoftware.com/csharp/ocr/docs/ |
| Tutorials hub | https://ironsoftware.com/csharp/ocr/tutorials/ |
| NuGet package | https://www.nuget.org/packages/IronOcr |
| Licensing page | https://ironsoftware.com/csharp/ocr/licensing/ |
| API reference (IronTesseract) | https://ironsoftware.com/csharp/ocr/object-reference/api/IronOcr.IronTesseract.html |
| API reference (OcrResult) | https://ironsoftware.com/csharp/ocr/object-reference/api/IronOcr.OcrResult.html |

**Tutorials (use when discussing the corresponding feature)**

| Context | URL |
|---------|-----|
| Reading text from images | https://ironsoftware.com/csharp/ocr/tutorials/how-to-read-text-from-an-image-in-csharp-net/ |
| Tesseract in C# | https://ironsoftware.com/csharp/ocr/tutorials/c-sharp-tesseract-ocr/ |
| Image filters tutorial | https://ironsoftware.com/csharp/ocr/tutorials/c-sharp-ocr-image-filters/ |
| Reading specific documents | https://ironsoftware.com/csharp/ocr/tutorials/read-specific-document/ |

**How-To Guides (use when discussing the corresponding feature)**

| Context | URL |
|---------|-----|
| IronTesseract setup | https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/ |
| Image input | https://ironsoftware.com/csharp/ocr/how-to/input-images/ |
| PDF input (native PDF OCR) | https://ironsoftware.com/csharp/ocr/how-to/input-pdfs/ |
| Stream input | https://ironsoftware.com/csharp/ocr/how-to/input-streams/ |
| TIFF/GIF input | https://ironsoftware.com/csharp/ocr/how-to/input-tiff-gif/ |
| Searchable PDF output | https://ironsoftware.com/csharp/ocr/how-to/searchable-pdf/ |
| Read results (structured data) | https://ironsoftware.com/csharp/ocr/how-to/read-results/ |
| Image quality correction (preprocessing) | https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/ |
| Image color correction | https://ironsoftware.com/csharp/ocr/how-to/image-color-correction/ |
| Image orientation correction | https://ironsoftware.com/csharp/ocr/how-to/image-orientation-correction/ |
| DPI settings | https://ironsoftware.com/csharp/ocr/how-to/dpi-setting/ |
| Multiple languages | https://ironsoftware.com/csharp/ocr/how-to/ocr-multiple-languages/ |
| Custom language packs | https://ironsoftware.com/csharp/ocr/how-to/ocr-custom-language/ |
| Region-based OCR | https://ironsoftware.com/csharp/ocr/how-to/ocr-region-of-an-image/ |
| Speed optimization | https://ironsoftware.com/csharp/ocr/how-to/ocr-fast-configuration/ |
| Computer vision | https://ironsoftware.com/csharp/ocr/how-to/computer-vision/ |
| Barcode reading during OCR | https://ironsoftware.com/csharp/ocr/how-to/barcodes/ |
| Async OCR | https://ironsoftware.com/csharp/ocr/how-to/async/ |
| Confidence scores | https://ironsoftware.com/csharp/ocr/how-to/tesseract-result-confidence/ |
| Page rotation detection | https://ironsoftware.com/csharp/ocr/how-to/detect-page-rotation/ |
| hOCR export | https://ironsoftware.com/csharp/ocr/how-to/html-hocr-export/ |
| Progress tracking | https://ironsoftware.com/csharp/ocr/how-to/progress-tracking/ |

**Specialized Document Guides (use when discussing document-type capabilities)**

| Context | URL |
|---------|-----|
| Handwriting recognition | https://ironsoftware.com/csharp/ocr/how-to/read-handwritten-image/ |
| License plate reading | https://ironsoftware.com/csharp/ocr/how-to/read-license-plate/ |
| MICR/cheque reading | https://ironsoftware.com/csharp/ocr/how-to/read-micr-cheque/ |
| Passport reading | https://ironsoftware.com/csharp/ocr/how-to/read-passport/ |
| Photo OCR | https://ironsoftware.com/csharp/ocr/how-to/read-photo/ |
| Scanned document processing | https://ironsoftware.com/csharp/ocr/how-to/read-scanned-document/ |
| Screenshot reading | https://ironsoftware.com/csharp/ocr/how-to/read-screenshot/ |
| Table extraction | https://ironsoftware.com/csharp/ocr/how-to/read-table-in-document/ |

**Examples (use as "see full example" references)**

| Context | URL |
|---------|-----|
| Basic OCR example | https://ironsoftware.com/csharp/ocr/examples/simple-csharp-ocr-tesseract/ |
| PDF OCR example | https://ironsoftware.com/csharp/ocr/examples/csharp-pdf-ocr/ |
| Searchable PDF example | https://ironsoftware.com/csharp/ocr/examples/make-pdf-searchable/ |
| Image filters example | https://ironsoftware.com/csharp/ocr/examples/ocr-image-filters-for-net-tesseract/ |
| Region crop example | https://ironsoftware.com/csharp/ocr/examples/net-tesseract-content-area-rectangle-crop/ |
| Multithreading example | https://ironsoftware.com/csharp/ocr/examples/csharp-tesseract-multithreading-for-speed/ |
| International languages example | https://ironsoftware.com/csharp/ocr/examples/intl-languages/ |
| Low quality scan example | https://ironsoftware.com/csharp/ocr/examples/ocr-low-quality-scans-tesseract/ |
| Barcode OCR example | https://ironsoftware.com/csharp/ocr/examples/csharp-ocr-barcodes/ |
| Speed tuning example | https://ironsoftware.com/csharp/ocr/examples/tune-tesseract-for-speed-in-dotnet/ |
| Table reading example | https://ironsoftware.com/csharp/ocr/examples/read-table-in-document/ |

**Feature Pages (use in "Additional IronOCR Capabilities" bullets)**

| Context | URL |
|---------|-----|
| Document features | https://ironsoftware.com/csharp/ocr/features/document/ |
| Language features | https://ironsoftware.com/csharp/ocr/features/languages/ |
| OCR results features | https://ironsoftware.com/csharp/ocr/features/ocr-results/ |
| Preprocessing features | https://ironsoftware.com/csharp/ocr/features/preprocessing/ |
| Specialized features | https://ironsoftware.com/csharp/ocr/features/specialized/ |
| Languages index (125+) | https://ironsoftware.com/csharp/ocr/languages/ |

**Deployment Guides (use when discussing cross-platform or cloud deployment)**

| Context | URL |
|---------|-----|
| AWS deployment | https://ironsoftware.com/csharp/ocr/get-started/aws/ |
| Azure deployment | https://ironsoftware.com/csharp/ocr/get-started/azure/ |
| Docker deployment | https://ironsoftware.com/csharp/ocr/get-started/docker/ |
| Linux deployment | https://ironsoftware.com/csharp/ocr/get-started/linux/ |
| macOS deployment | https://ironsoftware.com/csharp/ocr/get-started/mac/ |
| Windows setup | https://ironsoftware.com/csharp/ocr/get-started/windows/ |
| MAUI tutorial | https://ironsoftware.com/csharp/ocr/get-started/net-maui-ocr-tutorial/ |

**Use Case Pages (use when discussing specific application contexts)**

| Context | URL |
|---------|-----|
| ASP.NET OCR | https://ironsoftware.com/csharp/ocr/use-case/asp-net-ocr/ |
| PDF OCR in C# | https://ironsoftware.com/csharp/ocr/use-case/pdf-ocr-csharp/ |
| .NET OCR library | https://ironsoftware.com/csharp/ocr/use-case/net-ocr-library/ |
| OCR SDK for .NET | https://ironsoftware.com/csharp/ocr/use-case/ocr-sdk-net/ |

**Blog Posts (use sparingly for deeper-dive references)**

| Context | URL |
|---------|-----|
| Searchable PDFs with IronOCR | https://ironsoftware.com/csharp/ocr/blog/using-ironocr/searchable-pdfs-with-ironocr/ |
| Memory optimization | https://ironsoftware.com/csharp/ocr/blog/using-ironocr/ocr-memory-allocation-reduction/ |
| PDF data extraction | https://ironsoftware.com/csharp/ocr/blog/using-ironocr/pdf-data-extraction-dotnet/ |
| Table data extraction | https://ironsoftware.com/csharp/ocr/blog/using-ironocr/ironocr-extract-table-data/ |
| Multi-language OCR guide | https://ironsoftware.com/csharp/ocr/blog/using-ironocr/tesseract-ocr-for-multiple-languages/ |
| Invoice OCR tutorial | https://ironsoftware.com/csharp/ocr/blog/using-ironocr/invoice-ocr-csharp-tutorial/ |
| Receipt scanning | https://ironsoftware.com/csharp/ocr/blog/using-ironocr/receipt-scanning-api-tutorial/ |
| Identity document OCR | https://ironsoftware.com/csharp/ocr/blog/using-ironocr/identity-documents-ocr/ |
| License plate OCR | https://ironsoftware.com/csharp/ocr/blog/using-ironocr/license-plate-ocr-csharp-tutorial/ |
| Passport OCR | https://ironsoftware.com/csharp/ocr/blog/using-ironocr/passport-ocr-sdk/ |
| Why IronOCR over Tesseract | https://ironsoftware.com/csharp/ocr/troubleshooting/why-ironocr-and-not-tesseract/ |

## Banned Content

The following must never appear in any article:

- Contractions (don't, can't, it's, won't, isn't, doesn't, wouldn't, couldn't, shouldn't, they're, we're, you're, there's, that's, who's, what's, here's, let's)
- Horizontal separators (`---`)
- Bylines or author attribution (no "By ...", no "Author: ...")
- Tables of contents
- "Let's dive in", "let's explore", "let's get started"
- "In this article, we will..."
- "Without further ado..."
- "It's worth noting that..."
- "Needless to say..."
- "Simply put..."
- "Seamless" (overused)
- "Game-changing", "revolutionary", "cutting-edge"
- "Best-in-class", "world-class", "industry-leading" (unless citing specific ranking)

## Post-Execution Steps

After all 27 agents complete:

1. **Format audit:** Verify every article passes the quality checklist above.
2. **Contraction scan:** Automated search for all banned contractions across all 27 files.
3. **Separator scan:** Automated search for `---` in all 27 files.
4. **API correctness scan:** Verify all IronOCR code references match the quick reference.
5. **Commit** all 27 comparison articles to the `develop` branch. Do NOT push.
6. **Notify user** for review.
7. **Push** only when the user explicitly says "push".
