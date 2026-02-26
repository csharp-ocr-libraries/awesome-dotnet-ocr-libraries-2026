# OCR Phase 2: Migration Article Execution Plan

**Project:** awesome-dotnet-ocr-libraries-2026
**Phase:** 2 of 2 (Migration Articles)
**Predecessor:** Phase 1 (Comparison README articles -- assumed complete)
**Scope:** 27 migration articles, one per competitor directory
**Product:** IronOCR (NuGet: `IronOcr`, namespace: `IronOcr`)
**Product URL:** https://ironsoftware.com/csharp/ocr/
**Date:** 2026-02-26

## Table of Contents

1. [Overview and Goals](#overview-and-goals)
2. [Output Specification](#output-specification)
3. [Gold Standard Migration Structure](#gold-standard-migration-structure)
4. [Section Ownership and Anti-Repetition Rules](#section-ownership-and-anti-repetition-rules)
5. [IronOCR API Quick Reference](#ironocr-api-quick-reference)
6. [Style Rules](#style-rules)
7. [Cross-Article Differentiation Rule](#cross-article-differentiation-rule)
8. [Competitor Directory Map and Migration Briefs](#competitor-directory-map-and-migration-briefs)
9. [Agent Execution Protocol](#agent-execution-protocol)
10. [Quality Checklist](#quality-checklist)
11. [Post-Execution Audit](#post-execution-audit)
12. [Appendix A: IronOCR Pricing](#appendix-a-ironocr-pricing)
13. [Appendix B: IronOCR Feature Summary for Reference](#appendix-b-ironocr-feature-summary-for-reference)

## Overview and Goals

Phase 2 produces 27 migration guide articles. Each article lives inside its respective competitor directory and is named `migrate-from-[competitor]-to-ironocr.md`. These guides serve developers who have already decided to evaluate IronOCR as a replacement and need a practical, step-by-step path from their current library.

**Phase 2 assumes Phase 1 is complete.** Every competitor directory already contains:

- `README.md` -- the Phase 1 comparison article
- One or more `.cs` example files demonstrating the competitor library

Each migration article must be self-contained (a developer should not need to read the comparison article first), yet must not duplicate sentences or code scenarios from the Phase 1 comparison article in the same directory.

## Output Specification

| Property | Value |
|---|---|
| **File name** | `migrate-from-[competitor-dir-name]-to-ironocr.md` |
| **Location** | Inside the competitor directory (e.g., `tesseract/migrate-from-tesseract-to-ironocr.md`) |
| **Format** | Markdown |
| **Encoding** | UTF-8, LF line endings |
| **Target length** | 1,800 -- 3,000 words (measured excluding code blocks) |
| **Heading hierarchy** | Single `#` H1, then `##` and `###` only |
| **No byline** | Do not include author name or date |
| **No TOC** | Do not include a table of contents |
| **No horizontal rules** | Do not use `---` separators anywhere |

## Gold Standard Migration Structure

Every migration article must follow this exact section order. Agents must not reorder, skip, or rename sections.

```
1.  # Migrating from [Competitor Display Name] to IronOCR
2.  Opening paragraph -- what this guide covers (2-4 sentences, no technical detail)
3.  ## Why Migrate from [Competitor Display Name]
        - One lead-in sentence, then 4-6 bold-headed paragraph reasons (NOT a bullet list)
        ### The Fundamental Problem
            - One before/after code block illustrating the single biggest pain point
4.  ## IronOCR vs [Competitor Display Name]: Feature Comparison
        - Feature table (minimum 15 rows, maximum 20 rows)
        - No explanatory prose in this section beyond a single intro sentence
5.  ## Quick Start: [Competitor Display Name] to IronOCR Migration
        ### Step 1: Replace NuGet Package
            - Remove command (or manual uninstall instruction if no NuGet)
            - Install command: dotnet add package IronOcr
        ### Step 2: Update Namespaces
            - Before/after using statements
        ### Step 3: Initialize License
            - IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";
6.  ## Code Migration Examples
        ### [Descriptive Example Title 1]
            **[Competitor] Approach:**  <-- code block
            **IronOCR Approach:**       <-- code block
            Explanation paragraph (2-4 sentences) + contextual IronOCR link
        ### [Descriptive Example Title 2]
            (same pattern)
        ### [Descriptive Example Title 3]
            (same pattern)
        ### [Descriptive Example Title 4]
            (same pattern)
        (4 to 6 examples total, each with a distinct scenario)
7.  ## [Competitor Display Name] API to IronOCR Mapping Reference
        - Table with two columns: [Competitor] and IronOCR
        - 10-20 rows mapping classes, methods, and concepts
8.  ## Common Migration Issues and Solutions
        ### Issue 1: [Descriptive Name]
            **[Competitor]:** description of the problem
            **Solution:** code snippet or explanation showing the IronOCR fix
        ### Issue 2: [Descriptive Name]
        ### Issue 3: [Descriptive Name]
        ### Issue 4: [Descriptive Name]
        (4 to 6 issues)
9.  ## [Competitor Display Name] Migration Checklist
        ### Pre-Migration Tasks
            - Grep/search commands to audit the codebase
            - Inventory notes
        ### Code Update Tasks
            - Numbered step list (8-15 items)
        ### Post-Migration Testing
            - Bulleted verification items (6-10 items)
10. ## Key Benefits of Migrating to IronOCR
        - 4-6 bold-headed paragraphs describing outcomes AFTER migration
        - Do NOT restate old-library pain points here (that belongs in Why Migrate)
```

**Gold standard reference file:** `_gold-standard/reference-migrate-from-zetpdf-to-ironpdf.md`

Agents should read this file before writing to internalize the exact tone, depth, and formatting conventions. Adapt the pattern for OCR (IronOCR, not IronPDF) while preserving the structural fidelity.

## Section Ownership and Anti-Repetition Rules

Each section has a defined scope. Content must not leak across boundaries.

| Section | Owns | Must NOT Contain |
|---|---|---|
| **Opening paragraph** | Guide scope and audience statement | Technical detail (belongs in Why Migrate), implementation steps (belongs in Quick Start) |
| **Why Migrate** | Pain points and frustrations that trigger migration | Implementation steps (belongs in Quick Start), code migration examples (belongs in Code Examples) |
| **The Fundamental Problem** | One before/after code block showing the single biggest pain point | Anything beyond that single pain point; no multi-scenario coverage |
| **Feature Comparison** | Capability matrix (table only) | Explanatory prose (belongs in Why Migrate or Code Examples) |
| **Quick Start** | Three mechanical steps: uninstall old package, install IronOcr, initialize license | Code migration examples (belongs in Code Examples), reasons to migrate (belongs in Why Migrate) |
| **Code Migration Examples** | Full before/after code per scenario with explanation | Reasons to migrate (Why Migrate owns), benefits after migration (Key Benefits owns) |
| **API Mapping Reference** | Name-to-name translation table | Code examples or explanatory paragraphs |
| **Common Migration Issues** | Problems encountered during migration and their fixes | Reasons to migrate (Why Migrate owns), benefits after migration (Key Benefits owns) |
| **Migration Checklist** | Grep audit commands, numbered task list, test verification list | Prose explanations or justifications |
| **Key Benefits** | Outcomes and advantages realized after migration is complete | Old library pain points (Why Migrate owns), implementation steps (Quick Start owns) |

## IronOCR API Quick Reference

Agents must use these exact API patterns. Do not invent methods or classes that do not exist.

```csharp
// NuGet installation
dotnet add package IronOcr

// License initialization (call once at app startup)
IronOcr.License.LicenseKey = "YOUR-LICENSE-KEY";

// Basic OCR from image file
var ocr = new IronTesseract();
var result = ocr.Read("document.jpg");
Console.WriteLine(result.Text);

// One-liner shorthand
var text = new IronTesseract().Read("document.jpg").Text;

// OCR from PDF (native support, no conversion needed)
var pdfText = new IronTesseract().Read("scanned.pdf").Text;

// Save as searchable PDF
var ocr = new IronTesseract();
var result = ocr.Read("scanned.pdf");
result.SaveAsSearchablePdf("searchable-output.pdf");

// Image preprocessing pipeline
using var input = new OcrInput();
input.LoadImage("low-quality.jpg");
input.Deskew();
input.DeNoise();
input.Contrast();
var ocr = new IronTesseract();
var result = ocr.Read(input);
Console.WriteLine(result.Text);

// Multi-language OCR
var ocr = new IronTesseract();
ocr.Language = OcrLanguage.French;
var result = ocr.Read("french-document.jpg");

// Multiple languages simultaneously
var ocr = new IronTesseract();
ocr.Language = OcrLanguage.French + OcrLanguage.German;
var result = ocr.Read("multilingual-document.jpg");

// Region-based OCR (crop rectangle)
var region = new CropRectangle(0, 0, 600, 100);
using var input = new OcrInput();
input.LoadImage("invoice.jpg", region);
var ocr = new IronTesseract();
var result = ocr.Read(input);

// Confidence score
var result = new IronTesseract().Read("document.jpg");
Console.WriteLine($"Confidence: {result.Confidence}%");

// Barcode reading from scanned documents
var ocr = new IronTesseract();
ocr.Configuration.ReadBarCodes = true;
var result = ocr.Read("document-with-barcode.jpg");
foreach (var barcode in result.Barcodes)
    Console.WriteLine(barcode.Value);

// Structured data: pages, paragraphs, lines, words, characters
var result = new IronTesseract().Read("document.jpg");
foreach (var page in result.Pages)
{
    foreach (var paragraph in page.Paragraphs)
    {
        Console.WriteLine(paragraph.Text);
        Console.WriteLine($"Location: {paragraph.X}, {paragraph.Y}");
    }
}

// Thread-safe parallel processing
Parallel.ForEach(imageFiles, imagePath =>
{
    var ocr = new IronTesseract();
    var result = ocr.Read(imagePath);
    Console.WriteLine(result.Text);
});

// Loading from various sources
using var input = new OcrInput();
input.LoadImage("image.png");               // Single image
input.LoadImageFrames("multipage.tiff");    // Multi-frame TIFF
input.LoadPdf("scanned.pdf");               // PDF document
input.LoadImage(imageBytes);                // Byte array
input.LoadImage(stream);                    // Stream

// Additional preprocessing methods
input.Binarize();       // Convert to black and white
input.Invert();         // Invert colors
input.Dilate();         // Thicken text strokes
input.Erode();          // Thin text strokes
input.Scale(200);       // Scale to percentage
input.Sharpen();        // Sharpen image
input.DeepCleanBackgroundNoise(); // Heavy noise removal
```

**Key classes and their roles:**

| Class | Role |
|---|---|
| `IronTesseract` | Main OCR engine. Create one instance, call `.Read()` |
| `OcrInput` | Image/PDF loader with preprocessing pipeline. Implements `IDisposable` |
| `OcrResult` | Result container with `.Text`, `.Confidence`, `.Pages`, `.Barcodes`, `.SaveAsSearchablePdf()` |
| `OcrResult.Page` | Single page with `.Paragraphs`, `.Lines`, `.Words`, `.Characters` |
| `CropRectangle` | Defines a region for targeted OCR (x, y, width, height) |
| `OcrLanguage` | Enum for 125+ language packs |
| `IronOcr.License` | Static class for license key initialization |

**IronOCR Contextual Links — Master Catalog**

Agents must weave links naturally throughout each article. **Target: 8-12 links per article.** Do NOT dump them all in one section. Distribution rule: spread links evenly across the article body, with a higher concentration (cluster of 2-3 links) roughly every 4th paragraph of prose.

**Link Placement Rules for Migration Articles:**

1. **Opening paragraph + Why Migrate:** 1-2 links (product page, licensing).
2. **The Fundamental Problem:** 0-1 links (keep focus on code comparison).
3. **Feature Comparison table:** 0 links (table stays clean).
4. **Quick Start:** 1 link (NuGet page in Step 1).
5. **Code Migration Examples:** 4-8 links — this is the bulk zone. After each IronOCR code block explanation paragraph, include 1-2 relevant how-to, tutorial, or example links.
6. **API Mapping Reference:** 0-1 links.
7. **Common Migration Issues:** 2-3 links (relevant how-to guides as solutions).
8. **Migration Checklist:** 0-1 links.
9. **Key Benefits:** 2-4 links (feature pages, deployment guides, docs).

**Core Pages**

| Context | URL |
|---------|-----|
| IronOCR product page | https://ironsoftware.com/csharp/ocr/ |
| Documentation hub | https://ironsoftware.com/csharp/ocr/docs/ |
| Tutorials hub | https://ironsoftware.com/csharp/ocr/tutorials/ |
| NuGet package | https://www.nuget.org/packages/IronOcr |
| Licensing page | https://ironsoftware.com/csharp/ocr/licensing/ |
| API reference (IronTesseract) | https://ironsoftware.com/csharp/ocr/object-reference/api/IronOcr.IronTesseract.html |
| API reference (OcrResult) | https://ironsoftware.com/csharp/ocr/object-reference/api/IronOcr.OcrResult.html |

**Tutorials**

| Context | URL |
|---------|-----|
| Reading text from images | https://ironsoftware.com/csharp/ocr/tutorials/how-to-read-text-from-an-image-in-csharp-net/ |
| Tesseract in C# | https://ironsoftware.com/csharp/ocr/tutorials/c-sharp-tesseract-ocr/ |
| Image filters tutorial | https://ironsoftware.com/csharp/ocr/tutorials/c-sharp-ocr-image-filters/ |
| Reading specific documents | https://ironsoftware.com/csharp/ocr/tutorials/read-specific-document/ |

**How-To Guides (Core)**

| Context | URL |
|---------|-----|
| IronTesseract setup | https://ironsoftware.com/csharp/ocr/how-to/iron-tesseract/ |
| Image input | https://ironsoftware.com/csharp/ocr/how-to/input-images/ |
| PDF input (native PDF OCR) | https://ironsoftware.com/csharp/ocr/how-to/input-pdfs/ |
| Stream input | https://ironsoftware.com/csharp/ocr/how-to/input-streams/ |
| TIFF/GIF input | https://ironsoftware.com/csharp/ocr/how-to/input-tiff-gif/ |
| Searchable PDF output | https://ironsoftware.com/csharp/ocr/how-to/searchable-pdf/ |
| Read results (structured data) | https://ironsoftware.com/csharp/ocr/how-to/read-results/ |

**How-To Guides (Preprocessing)**

| Context | URL |
|---------|-----|
| Image quality correction | https://ironsoftware.com/csharp/ocr/how-to/image-quality-correction/ |
| Image color correction | https://ironsoftware.com/csharp/ocr/how-to/image-color-correction/ |
| Image orientation correction | https://ironsoftware.com/csharp/ocr/how-to/image-orientation-correction/ |
| DPI settings | https://ironsoftware.com/csharp/ocr/how-to/dpi-setting/ |
| Filter wizard | https://ironsoftware.com/csharp/ocr/how-to/filter-wizard/ |

**How-To Guides (Language)**

| Context | URL |
|---------|-----|
| Multiple languages | https://ironsoftware.com/csharp/ocr/how-to/ocr-multiple-languages/ |
| Custom language packs | https://ironsoftware.com/csharp/ocr/how-to/ocr-custom-language/ |
| Custom font training | https://ironsoftware.com/csharp/ocr/how-to/ocr-custom-font-training/ |

**How-To Guides (Advanced)**

| Context | URL |
|---------|-----|
| Region-based OCR | https://ironsoftware.com/csharp/ocr/how-to/ocr-region-of-an-image/ |
| Speed optimization | https://ironsoftware.com/csharp/ocr/how-to/ocr-fast-configuration/ |
| Computer vision | https://ironsoftware.com/csharp/ocr/how-to/computer-vision/ |
| Barcode reading during OCR | https://ironsoftware.com/csharp/ocr/how-to/barcodes/ |
| Async OCR | https://ironsoftware.com/csharp/ocr/how-to/async/ |
| Confidence scores | https://ironsoftware.com/csharp/ocr/how-to/tesseract-result-confidence/ |
| Page rotation detection | https://ironsoftware.com/csharp/ocr/how-to/detect-page-rotation/ |
| hOCR export | https://ironsoftware.com/csharp/ocr/how-to/html-hocr-export/ |
| Progress tracking | https://ironsoftware.com/csharp/ocr/how-to/progress-tracking/ |

**Specialized Document Guides**

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

**Feature Pages**

| Context | URL |
|---------|-----|
| Document features | https://ironsoftware.com/csharp/ocr/features/document/ |
| Language features | https://ironsoftware.com/csharp/ocr/features/languages/ |
| OCR results features | https://ironsoftware.com/csharp/ocr/features/ocr-results/ |
| Preprocessing features | https://ironsoftware.com/csharp/ocr/features/preprocessing/ |
| Specialized features | https://ironsoftware.com/csharp/ocr/features/specialized/ |
| Languages index (125+) | https://ironsoftware.com/csharp/ocr/languages/ |

**Deployment Guides**

| Context | URL |
|---------|-----|
| AWS deployment | https://ironsoftware.com/csharp/ocr/get-started/aws/ |
| Azure deployment | https://ironsoftware.com/csharp/ocr/get-started/azure/ |
| Docker deployment | https://ironsoftware.com/csharp/ocr/get-started/docker/ |
| Linux deployment | https://ironsoftware.com/csharp/ocr/get-started/linux/ |
| macOS deployment | https://ironsoftware.com/csharp/ocr/get-started/mac/ |
| Windows setup | https://ironsoftware.com/csharp/ocr/get-started/windows/ |
| MAUI tutorial | https://ironsoftware.com/csharp/ocr/get-started/net-maui-ocr-tutorial/ |

**Use Case Pages**

| Context | URL |
|---------|-----|
| ASP.NET OCR | https://ironsoftware.com/csharp/ocr/use-case/asp-net-ocr/ |
| PDF OCR in C# | https://ironsoftware.com/csharp/ocr/use-case/pdf-ocr-csharp/ |
| .NET OCR library | https://ironsoftware.com/csharp/ocr/use-case/net-ocr-library/ |

**Blog Posts (sparingly for deeper-dive references)**

| Context | URL |
|---------|-----|
| Searchable PDFs with IronOCR | https://ironsoftware.com/csharp/ocr/blog/using-ironocr/searchable-pdfs-with-ironocr/ |
| Memory optimization | https://ironsoftware.com/csharp/ocr/blog/using-ironocr/ocr-memory-allocation-reduction/ |
| PDF data extraction | https://ironsoftware.com/csharp/ocr/blog/using-ironocr/pdf-data-extraction-dotnet/ |
| Table data extraction | https://ironsoftware.com/csharp/ocr/blog/using-ironocr/ironocr-extract-table-data/ |
| Multi-language OCR | https://ironsoftware.com/csharp/ocr/blog/using-ironocr/tesseract-ocr-for-multiple-languages/ |
| Invoice OCR tutorial | https://ironsoftware.com/csharp/ocr/blog/using-ironocr/invoice-ocr-csharp-tutorial/ |
| Receipt scanning | https://ironsoftware.com/csharp/ocr/blog/using-ironocr/receipt-scanning-api-tutorial/ |
| Identity document OCR | https://ironsoftware.com/csharp/ocr/blog/using-ironocr/identity-documents-ocr/ |
| License plate OCR | https://ironsoftware.com/csharp/ocr/blog/using-ironocr/license-plate-ocr-csharp-tutorial/ |
| Passport OCR | https://ironsoftware.com/csharp/ocr/blog/using-ironocr/passport-ocr-sdk/ |
| Why IronOCR over Tesseract | https://ironsoftware.com/csharp/ocr/troubleshooting/why-ironocr-and-not-tesseract/ |

## Voice and Tone

Write like a senior .NET developer writing a migration guide for a colleague. Not a marketing team. Not a textbook. Not a blog mill.

### What "senior dev tone" means concretely

**DO write like this:**
> The charlesw Tesseract wrapper requires you to manage tessdata folders, set engine paths per platform, and write your own preprocessing pipeline before you get usable output. That is 20-40 hours of infrastructure work before you extract a single line of text. IronOCR ships as one NuGet package with preprocessing built in — `new IronTesseract().Read("scan.jpg").Text` and you are done.

**DO NOT write like this:**
> The migration from Tesseract to IronOCR offers several advantages. IronOCR provides a more streamlined approach to OCR processing. Developers will find that the API is easier to use and requires less configuration. The migration process involves updating package references and modifying code to use the new API.

The first version has specific pain, a concrete time cost, and a one-liner payoff. The second version is four sentences of nothing.

### Anti-robotic rules

1. **Lead with the pain, not the product.** Describe what goes wrong before describing what fixes it.
2. **Concrete numbers always.** "100+ lines of preprocessing code" not "significant effort." "$749 one-time" not "cost-effective."
3. **Short paragraphs after code blocks.** 2-4 sentences max. If you need more, the point is too vague.
4. **Terse code comments.** `// Manual tessdata path — breaks on deployment` not `// Here we configure the tessdata path which needs to be set correctly`
5. **No hedge words.** Kill "may", "might", "could potentially", "in certain scenarios", "some developers find that."
6. **No transition filler.** Kill "Moving on to", "Next, let us examine", "It is worth noting", "Having covered X, let us now turn to Y."
7. **Every sentence earns its place.** If you can delete it and the paragraph still works, delete it.
8. **Vary sentence length.** Mix 5-word punches with longer technical explanations. Monotone length = AI giveaway.
9. **No "comprehensive" or "robust."** Say what it actually does.
10. **The Fundamental Problem code block needs a punchy comment.** One-line comment showing the pain. Not a paragraph in a comment.

## Style Rules

### Mandatory

1. **No contractions.** Write "do not" instead of "don't", "cannot" instead of "can't", "it is" instead of "it's", etc.
2. **No horizontal separators.** Do not use `---` anywhere in the article.
3. **No bylines.** Do not include author name, date, or attribution line.
4. **No table of contents.** Do not include a TOC section.
5. **No filler phrases.** Avoid "Let's dive in", "Let's explore", "Without further ado", "It's worth noting", "As we all know", "Simply put".
6. **No promotional superlatives.** Avoid "amazing", "incredible", "revolutionary", "game-changing", "seamless", "best-in-class", "comprehensive", "robust".
7. **Fair competitor treatment.** State factual limitations. Do not disparage or use strawman code examples. Show realistic competitor code patterns.
8. **Correct IronOCR API.** Every code block must use the API exactly as documented in the Quick Reference above. Do not invent methods.
9. **Contextual IronOCR hyperlinks.** 8-12 distinct IronOCR links per article, distributed evenly with clusters every ~4th paragraph. See Master Link Catalog above.

### Formatting

- Code blocks use triple backticks with language identifier (`csharp`, `bash`)
- Feature comparison tables use standard Markdown pipe syntax
- Bold text uses `**text**` (not `__text__`)
- The competitor approach label format is exactly: `**[Competitor Name] Approach:**`
- The IronOCR approach label format is exactly: `**IronOCR Approach:**`

## Thin-Source Competitors (HIGH HALLUCINATION RISK)

These 6 competitors have critically thin .cs source material. Migration article agents MUST:

1. **Use ONLY API names from the .cs file and README.** Do not invent competitor methods.
2. **Show conceptual patterns** with comments like `// Simplified — see documentation` rather than fabricating signatures.
3. **Keep competitor code blocks shorter** — better to show less real code than more fake code.
4. **Lean on README for competitor facts** — it is the primary source for these libraries.

| Competitor | .cs Lines | Risk |
|-----------|:---------:|------|
| paddlesharp-ocr | 167 | CRITICAL |
| klippa-ocr | 241 | CRITICAL |
| windows-media-ocr | 298 | CRITICAL |
| patagames-tesseract | 315 | CRITICAL |
| tesseract-ocr-wrapper | 328 | CRITICAL |
| charlesw-tesseract | 336 | CRITICAL |

## Tesseract Cluster — Migration Article Differentiation

6 Tesseract wrapper migration articles must each tell a different story. The migration destination (IronOCR) is the same, but the migration SOURCE pain must differ.

| Competitor | Migration Story Focus | Must NOT Overlap With |
|-----------|----------------------|----------------------|
| tesseract | Tessdata management elimination + preprocessing pipeline addition | Any other |
| charlesw-tesseract | Native binary deployment cleanup + platform-conditional code removal | tesseract |
| tesseractocr | External preprocessing tool removal (ImageMagick/System.Drawing) + PDF enablement | charlesw-tesseract |
| patagames-tesseract | Windows-only escape + cross-platform deployment unlock | tesseractocr |
| tesseract-net-sdk | .NET Framework modernization + container deployment enablement | patagames-tesseract |
| tesseract-maui | MAUI lock-in escape + server-side OCR enablement | All others |
| tesseract-ocr-wrapper | API completeness upgrade + error handling standardization | tesseractocr |

**Enforcement:** Each article's "Why Migrate" section must name the specific pain in the first bold-headed paragraph. "The Fundamental Problem" code block must demonstrate that specific pain, not generic Tesseract limitations.

## Cross-Article Differentiation Rule

Each competitor has both a Phase 1 comparison article (`README.md`) and a Phase 2 migration article (`migrate-from-[x]-to-ironocr.md`). The following rules prevent duplication:

1. **Same concepts may appear in both articles; same sentences must not.** If the comparison article mentions that the competitor lacks preprocessing, the migration article can discuss preprocessing in the context of migration steps, but must use entirely different wording.

2. **Code Migration Examples must use DIFFERENT code scenarios** from those in the Phase 1 comparison article. Before writing code examples, the agent must read the competitor's `README.md` and all `.cs` files to identify which scenarios are already covered, then choose different ones.

3. **Scenario differentiation guidance for Code Migration Examples:**

   | If Phase 1 covers... | Phase 2 should use instead... |
   |---|---|
   | Basic text extraction from image | Multi-page document batch processing |
   | PDF OCR | Region-based OCR on invoices |
   | Language configuration | Multi-language simultaneous OCR |
   | Simple preprocessing | Advanced preprocessing pipeline (deskew + denoise + contrast) |
   | Confidence scoring | Structured data extraction (paragraphs, words, locations) |
   | Barcode reading | Searchable PDF output |
   | Single image OCR | TIFF multi-frame processing |
   | Stream-based input | Byte array input |

   If a scenario must overlap, the code implementation must be substantially different (different input types, different output handling, different error patterns).

## Competitor Directory Map and Migration Briefs

Each entry below defines the competitor directory name, display name, NuGet package (if any), key namespaces to remove, and the specific migration narrative the agent must tell.

### On-Premise Commercial (9 competitors)

#### 1. abbyy-finereader

| Property | Value |
|---|---|
| **Directory** | `abbyy-finereader` |
| **Display Name** | ABBYY FineReader |
| **NuGet** | No simple NuGet package (complex SDK installer) |
| **Namespaces to Remove** | `FREngine`, `ABBYY.FineReader` (varies by SDK version) |
| **Migration Narrative** | Cost and complexity reduction. ABBYY requires enterprise SDK installation, COM interop on Windows, and per-page or per-volume licensing. Migration to IronOCR eliminates COM dependencies, simplifies deployment to a single NuGet package, and replaces volume-based pricing with perpetual licensing. |
| **Fundamental Problem Focus** | COM interop complexity and Windows-only SDK installation vs. single NuGet install |
| **Unique Code Examples** | COM object initialization vs. IronTesseract instantiation, engine lifecycle management vs. stateless calls, recognition language setup, document export pipeline |
| **Existing .cs Files** | `abbyy-migration-comparison.cs`, `abbyy-sdk-integration.cs`, `abbyy-vs-ironocr-examples.cs` |

#### 2. aspose-ocr

| Property | Value |
|---|---|
| **Directory** | `aspose-ocr` |
| **Display Name** | Aspose.OCR |
| **NuGet** | `Aspose.OCR` |
| **Namespaces to Remove** | `Aspose.OCR`, `Aspose.OCR.Models` |
| **Migration Narrative** | Subscription elimination and API simplification. Aspose.OCR requires an active subscription for updates and uses a multi-step recognition pipeline (load, settings, recognize, result extraction). IronOCR provides a simpler one-call API with perpetual licensing and built-in preprocessing. |
| **Fundamental Problem Focus** | Multi-step recognition pipeline vs. single `.Read()` call |
| **Unique Code Examples** | Recognition settings configuration, area detection modes, batch recognition with result filtering, JSON/XML output handling |

#### 3. asprise-ocr

| Property | Value |
|---|---|
| **Directory** | `asprise-ocr` |
| **Display Name** | Asprise OCR |
| **NuGet** | `asprise-ocr-api` |
| **Namespaces to Remove** | `asprise.ocr` (Java-bridge namespace) |
| **Migration Narrative** | Java bridge removal and threading unlock. Asprise OCR for .NET is a wrapper around a Java OCR engine, requiring JRE installation and introducing cross-language complexity. IronOCR is a native .NET library with no Java dependency, full thread safety, and no bridge overhead. |
| **Fundamental Problem Focus** | JRE dependency and Java bridge initialization vs. native .NET instantiation |
| **Unique Code Examples** | JRE path configuration removal, string-based API replacement, output format handling, thread-safe parallel processing |

#### 4. dynamsoft-ocr

| Property | Value |
|---|---|
| **Directory** | `dynamsoft-ocr` |
| **Display Name** | Dynamsoft OCR (Label Recognizer) |
| **NuGet** | `Dynamsoft.LabelRecognizer` |
| **Namespaces to Remove** | `Dynamsoft.LabelRecognizer`, `Dynamsoft.Core` |
| **Migration Narrative** | Specialist-to-generalist migration and multi-product consolidation. Dynamsoft Label Recognizer is optimized for structured text (labels, IDs, MRZ zones) but requires separate Dynamsoft products for general OCR, barcode reading, and document processing. IronOCR handles general OCR, structured data, barcodes, and PDF processing in a single package. |
| **Fundamental Problem Focus** | Multiple Dynamsoft SDKs for different tasks vs. single IronOCR package |
| **Unique Code Examples** | Template-based recognition replacement, runtime settings JSON elimination, region definition migration, result parsing simplification |

#### 5. gdpicture-net

| Property | Value |
|---|---|
| **Directory** | `gdpicture-net` |
| **Display Name** | GdPicture.NET |
| **NuGet** | GdPicture packages (multiple) |
| **Namespaces to Remove** | `GdPicture14` (or current version namespace) |
| **Migration Narrative** | Image ID lifecycle removal and plugin consolidation. GdPicture requires creating image IDs, loading resources through an imaging toolkit, adding OCR plugins, and manually releasing image IDs. IronOCR eliminates the resource lifecycle pattern and consolidates OCR, preprocessing, and PDF handling into one API. |
| **Fundamental Problem Focus** | Image ID creation/release lifecycle vs. disposable OcrInput pattern |
| **Unique Code Examples** | Image ID lifecycle replacement, OCR plugin initialization removal, resource table management elimination, multi-page TIFF processing |

#### 6. kofax-omnipage

| Property | Value |
|---|---|
| **Directory** | `kofax-omnipage` |
| **Display Name** | Kofax OmniPage (Tungsten) |
| **NuGet** | No NuGet (SDK installer required) |
| **Namespaces to Remove** | `Kofax.OmniPageCSDK`, `CSDK` (varies by version) |
| **Migration Narrative** | Enterprise SDK to NuGet simplification. Kofax OmniPage requires a dedicated SDK installer, license server configuration, and C-style API interop. Migration to IronOCR replaces the entire SDK footprint with a single NuGet package, removes license server dependency, and provides a modern .NET API. |
| **Fundamental Problem Focus** | C-style SDK initialization ceremony vs. single NuGet + one-liner |
| **Unique Code Examples** | Engine initialization/shutdown ceremony replacement, zone-based recognition migration, output format conversion, error handling modernization |

#### 7. leadtools-ocr

| Property | Value |
|---|---|
| **Directory** | `leadtools-ocr` |
| **Display Name** | LEADTOOLS OCR |
| **NuGet** | `Leadtools.Ocr` |
| **Namespaces to Remove** | `Leadtools`, `Leadtools.Ocr`, `Leadtools.Codecs`, `Leadtools.Forms.DocumentWriters` |
| **Migration Narrative** | .LIC file removal and engine initialization simplification. LEADTOOLS requires .LIC license files, runtime engine path configuration, document writer setup, and multi-step page-by-page processing. IronOCR replaces the entire initialization ceremony with a license key string and a single `.Read()` call. |
| **Fundamental Problem Focus** | Multi-namespace, multi-step engine setup with .LIC files vs. single namespace + string license |
| **Unique Code Examples** | Engine startup/shutdown lifecycle replacement, page-by-page processing to batch processing, document writer pipeline simplification, zone-based recognition migration |

#### 8. syncfusion-ocr

| Property | Value |
|---|---|
| **Directory** | `syncfusion-ocr` |
| **Display Name** | Syncfusion OCR |
| **NuGet** | `Syncfusion.PDF.OCR.Net.Core` |
| **Namespaces to Remove** | `Syncfusion.OCRProcessor`, `Syncfusion.Pdf`, `Syncfusion.Pdf.Parsing` |
| **Migration Narrative** | Tessdata elimination and preprocessing unlock. Syncfusion OCR requires manually downloading and configuring tessdata language files and a Tesseract binary path. It lacks built-in preprocessing. IronOCR bundles language data internally and provides automatic preprocessing filters. |
| **Fundamental Problem Focus** | Manual tessdata path configuration + Tesseract binary path vs. zero-config language support |
| **Unique Code Examples** | Tessdata path elimination, Tesseract binary path removal, PDF-specific OCR pipeline migration, searchable PDF generation comparison |

#### 9. ximage-ocr

| Property | Value |
|---|---|
| **Directory** | `ximage-ocr` |
| **Display Name** | XImage.OCR |
| **NuGet** | Multiple `XImage.OCR.*` packages |
| **Namespaces to Remove** | `Yiigo.Image.Ocr`, `XImage.OCR` (varies) |
| **Migration Narrative** | Package consolidation. XImage.OCR splits functionality across multiple NuGet packages for different image formats, OCR engines, and output types. IronOCR consolidates all OCR functionality into a single package with unified API. |
| **Fundamental Problem Focus** | Multiple packages with separate initialization vs. single `IronOcr` package |
| **Unique Code Examples** | Multi-package initialization consolidation, image format handling unification, output format streamlining, batch document processing |

### Cloud APIs (7 competitors)

**Common migration theme:** All cloud API migrations share the narrative of moving from cloud-dependent, per-request pricing to local processing with perpetual licensing. Each article must emphasize: no internet required, no per-page costs, data stays on-premise, no async polling, no credential management.

#### 10. aws-textract

| Property | Value |
|---|---|
| **Directory** | `aws-textract` |
| **Display Name** | AWS Textract |
| **NuGet** | `AWSSDK.Textract` |
| **Namespaces to Remove** | `Amazon.Textract`, `Amazon.Textract.Model`, `Amazon.Runtime` |
| **Migration Narrative** | Cloud-to-local migration, cost elimination, and async removal. AWS Textract requires AWS credentials, IAM roles, S3 bucket configuration for large documents, and async polling for results. Per-page pricing adds up at scale. IronOCR processes locally with zero cloud dependency. |
| **Fundamental Problem Focus** | AWS credential setup + async polling + per-page cost vs. local synchronous processing |
| **Unique Code Examples** | Credential/client setup elimination, async StartDocumentTextDetection replacement, S3 upload removal, structured block parsing to OcrResult mapping |

#### 11. azure-computer-vision

| Property | Value |
|---|---|
| **Directory** | `azure-computer-vision` |
| **Display Name** | Azure Computer Vision OCR |
| **NuGet** | `Azure.AI.Vision.ImageAnalysis` |
| **Namespaces to Remove** | `Azure.AI.Vision.ImageAnalysis`, `Azure` |
| **Migration Narrative** | Cloud-to-local migration and polling removal. Azure Computer Vision requires an Azure subscription, endpoint URL, API key management, and HTTP-based polling for async results. IronOCR processes locally with synchronous results. |
| **Fundamental Problem Focus** | Azure endpoint + API key + async polling loop vs. local synchronous `.Read()` |
| **Unique Code Examples** | Azure client initialization replacement, polling loop elimination, image analysis result mapping, multi-page document handling without cloud upload |

#### 12. google-cloud-vision

| Property | Value |
|---|---|
| **Directory** | `google-cloud-vision` |
| **Display Name** | Google Cloud Vision OCR |
| **NuGet** | `Google.Cloud.Vision.V1` |
| **Namespaces to Remove** | `Google.Cloud.Vision.V1`, `Google.Protobuf` |
| **Migration Narrative** | Cloud-to-local migration and credential removal. Google Cloud Vision requires a GCP project, service account JSON key file, and per-request billing. Protobuf response parsing adds complexity. IronOCR eliminates all cloud infrastructure requirements. |
| **Fundamental Problem Focus** | Service account JSON + GCP project setup + protobuf parsing vs. direct local OCR |
| **Unique Code Examples** | Service account credential elimination, protobuf annotation parsing replacement, batch annotation request simplification, document text detection migration |

#### 13. klippa-ocr

| Property | Value |
|---|---|
| **Directory** | `klippa-ocr` |
| **Display Name** | Klippa OCR |
| **NuGet** | No official NuGet (REST API) |
| **Namespaces to Remove** | `System.Net.Http` (HTTP client code), `Newtonsoft.Json` or `System.Text.Json` (response parsing) |
| **Migration Narrative** | Cloud-to-local migration and data privacy improvement. Klippa is a REST-only OCR API requiring HTTP client code, API key headers, multipart form uploads, and JSON response parsing. Sensitive documents must leave the network. IronOCR processes everything locally with a native .NET API. |
| **Fundamental Problem Focus** | Manual HTTP request construction + file upload + JSON parsing vs. single `.Read()` call |
| **Unique Code Examples** | HttpClient/REST replacement, multipart form upload elimination, JSON response deserialization replacement, error handling and retry logic simplification |

#### 14. mindee

| Property | Value |
|---|---|
| **Directory** | `mindee` |
| **Display Name** | Mindee |
| **NuGet** | `Mindee` |
| **Namespaces to Remove** | `Mindee`, `Mindee.Input`, `Mindee.Product` |
| **Migration Narrative** | Cloud-to-local migration and financial data privacy. Mindee specializes in invoice and receipt parsing via cloud API. Financial documents contain sensitive data that must leave the network for processing. IronOCR processes invoices locally with region-based OCR for structured field extraction. |
| **Fundamental Problem Focus** | Cloud upload of financial documents + per-page pricing vs. local processing with data sovereignty |
| **Unique Code Examples** | Invoice parsing migration (cloud fields to region-based OCR), receipt processing, async prediction replacement, custom endpoint elimination |

#### 15. ocrspace

| Property | Value |
|---|---|
| **Directory** | `ocrspace` |
| **Display Name** | OCR.space |
| **NuGet** | No official NuGet (manual REST) |
| **Namespaces to Remove** | `System.Net.Http`, JSON parsing namespaces |
| **Migration Narrative** | REST-to-local migration and SDK upgrade. OCR.space provides a free-tier REST API with rate limits and no official .NET SDK. Developers must write raw HTTP code, handle rate limiting, and parse JSON responses manually. IronOCR provides a proper .NET SDK with no rate limits and no network dependency. |
| **Fundamental Problem Focus** | Manual REST calls with rate limits + no SDK vs. native .NET library |
| **Unique Code Examples** | Raw HttpClient replacement, base64 image encoding elimination, rate limit handling removal, OCR engine selection (Engine1/Engine2) replacement |

#### 16. veryfi

| Property | Value |
|---|---|
| **Directory** | `veryfi` |
| **Display Name** | Veryfi |
| **NuGet** | `Veryfi` |
| **Namespaces to Remove** | `Veryfi`, `Veryfi.Models` |
| **Migration Narrative** | Cloud-to-local migration and financial data privacy. Veryfi is a cloud API specializing in receipt, invoice, and expense document processing. All financial documents must be uploaded to Veryfi servers. IronOCR processes the same documents locally with region-based OCR for field extraction, keeping sensitive financial data on-premise. |
| **Fundamental Problem Focus** | Cloud upload of financial receipts/invoices vs. local region-based OCR with data sovereignty |
| **Unique Code Examples** | Document processing client replacement, receipt field extraction migration, expense categorization replacement with structured OCR, webhook elimination |

### Open Source / Tesseract Wrappers (11 competitors)

**Common migration theme:** All Tesseract wrapper migrations share the narrative of escaping Tesseract limitations (no preprocessing, no native PDF, tessdata management, platform-specific native binaries). Each article must emphasize: built-in preprocessing, native PDF support, searchable PDF output, no tessdata management, cross-platform NuGet.

#### 17. tesseract (charlesw)

| Property | Value |
|---|---|
| **Directory** | `tesseract` |
| **Display Name** | Tesseract (charlesw wrapper) |
| **NuGet** | `Tesseract` |
| **Namespaces to Remove** | `Tesseract` |
| **Migration Narrative** | Preprocessing automation and PDF unlock. The charlesw Tesseract wrapper is the most popular .NET Tesseract binding but is stuck on Tesseract 4.1.1, requires manual tessdata management, provides no preprocessing, and cannot read or produce PDFs. IronOCR uses an optimized Tesseract 5 engine with automatic preprocessing and native PDF support. |
| **Fundamental Problem Focus** | Manual tessdata folder management + no preprocessing vs. zero-config language data + built-in filters |
| **Unique Code Examples** | Tessdata path elimination, Pix object replacement, engine initialization simplification, multi-page processing with preprocessing pipeline |

#### 18. charlesw-tesseract

| Property | Value |
|---|---|
| **Directory** | `charlesw-tesseract` |
| **Display Name** | Charlesw Tesseract |
| **NuGet** | `Tesseract` (same package as #17) |
| **Namespaces to Remove** | `Tesseract` |
| **Migration Narrative** | Same core library as #17, but this article focuses on different pain points: native binary management across platforms and the stale Tesseract 4.x engine. Emphasize cross-platform deployment simplification and Tesseract 5 accuracy improvements. |
| **Fundamental Problem Focus** | Platform-specific native binary deployment (leptonica, tesseract DLLs) vs. self-contained NuGet |
| **Unique Code Examples** | Native binary path configuration removal, platform-conditional code elimination, Leptonica image conversion replacement, confidence threshold handling |
| **Differentiation from #17** | This article must focus on deployment and platform issues. Article #17 focuses on preprocessing and PDF. No overlapping code examples between the two. |

#### 19. paddleocr

| Property | Value |
|---|---|
| **Directory** | `paddleocr` |
| **Display Name** | PaddleOCR (.NET) |
| **NuGet** | Multiple `Sdcb.PaddleOCR.*` packages |
| **Namespaces to Remove** | `Sdcb.PaddleOCR`, `Sdcb.PaddleOCR.Models`, `Sdcb.PaddleInference` |
| **Migration Narrative** | Model management elimination and GPU dependency removal. PaddleOCR requires downloading ONNX or PaddlePaddle model files, configuring model paths, and optionally setting up GPU inference. The multi-package architecture (detection model, recognition model, inference runtime) adds complexity. IronOCR bundles everything in one package. |
| **Fundamental Problem Focus** | Model file downloads + GPU configuration + multi-package setup vs. single NuGet with zero model management |
| **Unique Code Examples** | Model path configuration elimination, PaddleOCR engine pipeline replacement, GPU/CPU device selection removal, detection + recognition two-stage pipeline consolidation |

#### 20. paddlesharp-ocr

| Property | Value |
|---|---|
| **Directory** | `paddlesharp-ocr` |
| **Display Name** | PaddleSharp OCR |
| **NuGet** | `Sdcb.PaddleOCR` and related packages |
| **Namespaces to Remove** | `Sdcb.PaddleOCR`, `Sdcb.PaddleInference` |
| **Migration Narrative** | Abstraction layer reduction. Similar to PaddleOCR but this article focuses on the PaddleSharp-specific patterns: inference session management, OpenCV interop for preprocessing, and the complexity of choosing between CPU/GPU/OpenVINO backends. IronOCR handles all of this internally. |
| **Fundamental Problem Focus** | Inference backend selection (CPU/GPU/OpenVINO) + OpenCV preprocessing vs. automatic backend + built-in preprocessing |
| **Unique Code Examples** | Inference session lifecycle replacement, OpenCV preprocessing pipeline migration, backend selection elimination, table recognition migration |
| **Differentiation from #19** | This article focuses on inference backends and OpenCV interop. Article #19 focuses on model management and GPU setup. No overlapping code examples. |

#### 21. patagames-tesseract

| Property | Value |
|---|---|
| **Directory** | `patagames-tesseract` |
| **Display Name** | Patagames Tesseract.NET SDK |
| **NuGet** | `Tesseract.Net.SDK` |
| **Namespaces to Remove** | `Patagames.Ocr`, `Patagames.Ocr.Enums` |
| **Migration Narrative** | Windows-only escape and platform unlock. Patagames Tesseract.NET SDK is a commercial Tesseract wrapper that historically targets Windows. Migration to IronOCR unlocks cross-platform deployment (Windows, Linux, macOS, Docker, Azure, AWS) while maintaining commercial support and adding preprocessing. |
| **Fundamental Problem Focus** | Windows-only deployment constraint vs. cross-platform NuGet |
| **Unique Code Examples** | Patagames API initialization replacement, page segmentation mode migration, result iterator pattern replacement, image-to-PDF conversion |

#### 22. rapidocr-net

| Property | Value |
|---|---|
| **Directory** | `rapidocr-net` |
| **Display Name** | RapidOCR.NET |
| **NuGet** | `RapidOcrNet` |
| **Namespaces to Remove** | `RapidOcrNet` |
| **Migration Narrative** | ONNX model management elimination. RapidOCR.NET uses ONNX Runtime for inference, requiring separate model files for detection, classification, and recognition. Model updates require manual file replacement. IronOCR bundles its engine internally with no external model dependencies. |
| **Fundamental Problem Focus** | ONNX model file management (3 separate models) vs. zero model configuration |
| **Unique Code Examples** | ONNX model path configuration removal, detection/classification/recognition pipeline consolidation, custom model loading replacement, batch processing migration |

#### 23. tesseract-net-sdk

| Property | Value |
|---|---|
| **Directory** | `tesseract-net-sdk` |
| **Display Name** | Tesseract.NET SDK |
| **NuGet** | `Tesseract.Net.SDK` |
| **Namespaces to Remove** | `Patagames.Ocr` |
| **Migration Narrative** | Legacy .NET Framework escape and platform unlock. This is the same Patagames SDK but this article focuses on developers migrating from .NET Framework-era code. Emphasize .NET 6/7/8/9 compatibility, modern async patterns, and container deployment. |
| **Fundamental Problem Focus** | .NET Framework-era API patterns and deployment model vs. modern .NET with container support |
| **Unique Code Examples** | .NET Framework initialization modernization, legacy disposal patterns update, modern async integration, Docker deployment preparation |
| **Differentiation from #21** | This article focuses on .NET Framework modernization. Article #21 focuses on Windows-only platform escape. No overlapping code examples. |

#### 24. tesseractocr

| Property | Value |
|---|---|
| **Directory** | `tesseractocr` |
| **Display Name** | TesseractOCR |
| **NuGet** | `TesseractOCR` |
| **Namespaces to Remove** | `TesseractOCR`, `TesseractOCR.Enums` |
| **Migration Narrative** | Tesseract limitation escape focused on preprocessing and PDF. TesseractOCR is a community wrapper that exposes raw Tesseract functionality without preprocessing filters or PDF I/O. IronOCR adds automatic preprocessing, native PDF input, and searchable PDF output on top of the same core engine. |
| **Fundamental Problem Focus** | No preprocessing + no PDF support vs. built-in preprocessing pipeline + native PDF |
| **Unique Code Examples** | Manual image preprocessing replacement (external ImageMagick or System.Drawing to built-in filters), PDF input addition, searchable PDF output, confidence-based filtering |

#### 25. tesseract-maui

| Property | Value |
|---|---|
| **Directory** | `tesseract-maui` |
| **Display Name** | TesseractOcrMaui |
| **NuGet** | `TesseractOcrMaui` |
| **Namespaces to Remove** | `TesseractOcrMaui` |
| **Migration Narrative** | MAUI lock-in escape and server-side unlock. TesseractOcrMaui is designed exclusively for .NET MAUI mobile/desktop applications. It cannot run in server-side scenarios (ASP.NET, Azure Functions, Docker). IronOCR runs everywhere: mobile, desktop, server, cloud, and containers. |
| **Fundamental Problem Focus** | MAUI-only platform restriction vs. universal .NET deployment |
| **Unique Code Examples** | MAUI dependency injection replacement, platform-specific handler elimination, server-side OCR enablement, cross-platform project restructuring |

#### 26. tesseract-ocr-wrapper

| Property | Value |
|---|---|
| **Directory** | `tesseract-ocr-wrapper` |
| **Display Name** | Tesseract OCR Wrapper |
| **NuGet** | `TesseractOCR` |
| **Namespaces to Remove** | `TesseractOCR` |
| **Migration Narrative** | Tesseract limitation escape. This is another community Tesseract wrapper with similar limitations to #24 but this article focuses on the wrapper abstraction layer and its gaps: incomplete API coverage, inconsistent error handling, and limited output formats. IronOCR provides a complete, professionally maintained API. |
| **Fundamental Problem Focus** | Incomplete wrapper coverage and inconsistent error handling vs. complete professional API |
| **Unique Code Examples** | Error handling improvement, output format expansion (text, searchable PDF, structured data), engine configuration simplification, multi-format input handling |
| **Differentiation from #24** | This article focuses on API completeness and error handling. Article #24 focuses on preprocessing and PDF. No overlapping code examples. |

#### 27. windows-media-ocr

| Property | Value |
|---|---|
| **Directory** | `windows-media-ocr` |
| **Display Name** | Windows.Media.Ocr (UWP/WinRT OCR) |
| **NuGet** | No NuGet (Windows built-in API) |
| **Namespaces to Remove** | `Windows.Media.Ocr`, `Windows.Graphics.Imaging`, `Windows.Storage` |
| **Migration Narrative** | Windows-only escape and cross-platform unlock. Windows.Media.Ocr is built into Windows 10/11 but is exclusively a Windows API with limited language support (only installed Windows language packs), no preprocessing, no PDF support, and no server-side deployment capability. IronOCR works on all platforms with 125+ languages. |
| **Fundamental Problem Focus** | Windows-only WinRT API with installed language pack dependency vs. cross-platform NuGet with 125+ bundled languages |
| **Unique Code Examples** | WinRT async/await pattern replacement, SoftwareBitmap conversion elimination, Windows language pack dependency removal, server-side deployment enablement |

## Agent Execution Protocol

### Pre-Execution Reading (each agent)

Before writing, each agent must read these files from its assigned competitor directory:

1. `README.md` -- the Phase 1 comparison article (to avoid duplicating code scenarios)
2. All `.cs` files -- to understand existing code examples
3. This plan file (`OCR_PHASE2_MIGRATION_PLAN.md`) -- for structure and API reference
4. `_gold-standard/reference-migrate-from-zetpdf-to-ironpdf.md` -- for tone and format reference

### Writing Protocol

1. Follow the Gold Standard Migration Structure exactly (all 10 sections in order).
2. Use the Migration Brief for the assigned competitor to determine the narrative, fundamental problem, and code example topics.
3. Verify every IronOCR code block against the API Quick Reference. Do not invent methods.
4. Include 3-6 contextual IronOCR hyperlinks in code example explanation paragraphs.
5. Ensure zero contractions and zero `---` separators.
6. Ensure code examples are DIFFERENT scenarios from the Phase 1 comparison article.
7. Write the file to: `[competitor-dir]/migrate-from-[competitor-dir-name]-to-ironocr.md`

### Parallelism

All 27 agents may execute in parallel. There are no cross-article dependencies. Each agent reads only from its own competitor directory and shared reference files.

## Quality Checklist

Every article must pass all of the following checks before it is considered complete:

| # | Check | How to Verify |
|---|---|---|
| 1 | Title is exactly `# Migrating from [Display Name] to IronOCR` | First line of file |
| 2 | Opening paragraph exists (2-4 sentences, no technical detail) | Lines 3-6 |
| 3 | `## Why Migrate from [Display Name]` section exists | Grep for heading |
| 4 | Why Migrate has bold-headed paragraphs (not bullet list) | Visual inspection |
| 5 | `### The Fundamental Problem` subsection exists with before/after code | Grep for heading |
| 6 | `## IronOCR vs [Display Name]: Feature Comparison` exists | Grep for heading |
| 7 | Feature table has 15+ rows | Count table rows |
| 8 | `## Quick Start: [Display Name] to IronOCR Migration` exists | Grep for heading |
| 9 | Quick Start has exactly 3 steps (### Step 1, ### Step 2, ### Step 3) | Grep for step headings |
| 10 | `## Code Migration Examples` exists with 4-6 subsections | Grep for heading and count ### |
| 11 | Each code example uses `**[Competitor] Approach:**` / `**IronOCR Approach:**` | Grep for bold labels |
| 12 | Code examples are different scenarios from README.md | Manual cross-reference |
| 13 | `## [Display Name] API to IronOCR Mapping Reference` exists | Grep for heading |
| 14 | API mapping is a table (not prose) | Visual inspection |
| 15 | `## Common Migration Issues and Solutions` exists | Grep for heading |
| 16 | Issues use `### Issue N: [Name]` format (4-6 issues) | Grep for pattern |
| 17 | Each issue has `**[Competitor]:**` and `**Solution:**` labels | Grep for bold labels |
| 18 | `## [Display Name] Migration Checklist` exists | Grep for heading |
| 19 | Checklist has exactly 3 subsections: Pre-Migration, Code Update, Post-Migration | Grep for subsection headings |
| 20 | `## Key Benefits of Migrating to IronOCR` exists | Grep for heading |
| 21 | Key Benefits uses bold-headed paragraphs (not bullet list) | Visual inspection |
| 22 | Zero contractions in entire file | Regex: `\b\w+n't\b`, `\bit's\b`, `\blet's\b`, `\bwe're\b`, `\bthey're\b`, `\byou're\b`, `\bwe've\b`, `\bdon't\b`, `\bcan't\b`, `\bwon't\b`, `\bdoesn't\b`, `\bisn't\b`, `\baren't\b`, `\bwouldn't\b`, `\bcouldn't\b`, `\bshouldn't\b`, `\bhasn't\b`, `\bhaven't\b` |
| 23 | Zero `---` horizontal separators | Grep for `^---$` |
| 24 | No byline or author attribution | Grep for "By " or "Author" at top |
| 25 | No table of contents section | Grep for "Table of Contents" |
| 26 | 8-12 distinct IronOCR hyperlinks present, distributed evenly with clusters every ~4th paragraph | Count `ironsoftware.com` links |
| 27 | All IronOCR code uses correct API (IronTesseract, OcrInput, OcrResult) | Manual review |
| 28 | File name matches pattern `migrate-from-[dir]-to-ironocr.md` | File system check |

## Post-Execution Audit

After all 27 articles are written, run the following automated audit:

### Step 1: File Existence Check

Verify all 27 files exist:

```bash
DIRS=(
  abbyy-finereader aspose-ocr asprise-ocr dynamsoft-ocr gdpicture-net
  kofax-omnipage leadtools-ocr syncfusion-ocr ximage-ocr
  aws-textract azure-computer-vision google-cloud-vision klippa-ocr
  mindee ocrspace veryfi
  tesseract charlesw-tesseract paddleocr paddlesharp-ocr
  patagames-tesseract rapidocr-net tesseract-net-sdk tesseractocr
  tesseract-maui tesseract-ocr-wrapper windows-media-ocr
)

for dir in "${DIRS[@]}"; do
  FILE="$dir/migrate-from-$dir-to-ironocr.md"
  if [ -f "$FILE" ]; then
    echo "OK: $FILE"
  else
    echo "MISSING: $FILE"
  fi
done
```

### Step 2: Structure Validation

For each file, verify required headings exist:

```bash
for dir in "${DIRS[@]}"; do
  FILE="$dir/migrate-from-$dir-to-ironocr.md"
  echo "=== $FILE ==="
  grep -c "^# Migrating from" "$FILE"
  grep -c "^## Why Migrate" "$FILE"
  grep -c "^### The Fundamental Problem" "$FILE"
  grep -c "^## IronOCR vs" "$FILE"
  grep -c "^## Quick Start" "$FILE"
  grep -c "^### Step 1" "$FILE"
  grep -c "^### Step 2" "$FILE"
  grep -c "^### Step 3" "$FILE"
  grep -c "^## Code Migration Examples" "$FILE"
  grep -c "^## .* API to IronOCR Mapping" "$FILE"
  grep -c "^## Common Migration Issues" "$FILE"
  grep -c "^## .* Migration Checklist" "$FILE"
  grep -c "^### Pre-Migration" "$FILE"
  grep -c "^### Code Update" "$FILE"
  grep -c "^### Post-Migration" "$FILE"
  grep -c "^## Key Benefits" "$FILE"
done
```

### Step 3: Contraction Check

```bash
for dir in "${DIRS[@]}"; do
  FILE="$dir/migrate-from-$dir-to-ironocr.md"
  CONTRACTIONS=$(grep -Poin "\b(don't|can't|won't|isn't|aren't|doesn't|didn't|couldn't|wouldn't|shouldn't|hasn't|haven't|it's|let's|we're|they're|you're|we've|there's|that's|who's|what's|here's)\b" "$FILE" | wc -l)
  if [ "$CONTRACTIONS" -gt 0 ]; then
    echo "FAIL: $FILE has $CONTRACTIONS contractions"
    grep -Poin "\b(don't|can't|won't|isn't|aren't|doesn't|didn't|couldn't|wouldn't|shouldn't|hasn't|haven't|it's|let's|we're|they're|you're|we've|there's|that's|who's|here's|what's)\b" "$FILE"
  else
    echo "PASS: $FILE"
  fi
done
```

### Step 4: Separator Check

```bash
for dir in "${DIRS[@]}"; do
  FILE="$dir/migrate-from-$dir-to-ironocr.md"
  SEPS=$(grep -c "^---$" "$FILE")
  if [ "$SEPS" -gt 0 ]; then
    echo "FAIL: $FILE has $SEPS separators"
  else
    echo "PASS: $FILE"
  fi
done
```

### Step 5: Link Count

```bash
for dir in "${DIRS[@]}"; do
  FILE="$dir/migrate-from-$dir-to-ironocr.md"
  LINKS=$(grep -o "ironsoftware.com" "$FILE" | wc -l)
  echo "$FILE: $LINKS IronOCR links"
  if [ "$LINKS" -lt 10 ]; then
    echo "  WARNING: fewer than 8 links (target: 8-12)"
  fi
done
```

### Step 6: Feature Table Row Count

```bash
for dir in "${DIRS[@]}"; do
  FILE="$dir/migrate-from-$dir-to-ironocr.md"
  # Count rows in the feature comparison table (lines starting with | that are not headers or separators)
  TABLE_ROWS=$(awk '/^## IronOCR vs/,/^## /' "$FILE" | grep "^|" | grep -v "^|---" | grep -v "^| Feature" | grep -v "^| ---" | wc -l)
  echo "$FILE: $TABLE_ROWS feature rows"
  if [ "$TABLE_ROWS" -lt 15 ]; then
    echo "  WARNING: fewer than 15 feature rows"
  fi
done
```

### Step 7: Cross-Article Code Duplication Check

For each competitor, compare code blocks between README.md and the migration article to flag potential duplication:

```bash
for dir in "${DIRS[@]}"; do
  README="$dir/README.md"
  MIGRATE="$dir/migrate-from-$dir-to-ironocr.md"
  if [ -f "$README" ] && [ -f "$MIGRATE" ]; then
    # Extract IronOCR code from both files and check for identical lines
    README_CODE=$(awk '/```csharp/,/```/' "$README" | grep -v '```')
    MIGRATE_CODE=$(awk '/```csharp/,/```/' "$MIGRATE" | grep -v '```')
    DUPES=$(comm -12 <(echo "$README_CODE" | sort -u) <(echo "$MIGRATE_CODE" | sort -u) | grep -v "^$" | grep -v "^using " | grep -v "^//" | wc -l)
    if [ "$DUPES" -gt 5 ]; then
      echo "WARNING: $dir has $DUPES potentially duplicated code lines"
    else
      echo "PASS: $dir ($DUPES shared lines, likely boilerplate)"
    fi
  fi
done
```

### Step 8: Commit (Local Only)

After all checks pass:

```bash
git add */migrate-from-*-to-ironocr.md
git commit -m "Phase 2: Add 27 OCR competitor migration articles

Each article follows the gold standard migration structure with:
- Why Migrate section with bold-headed paragraphs
- Feature comparison table (15+ rows)
- Quick Start 3-step guide
- 4-6 code migration examples (distinct from comparison articles)
- API mapping reference table
- Common migration issues and solutions
- Pre/Code/Post migration checklist
- Key benefits section

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

**DO NOT push to remote.** Notify the user for review after local commit.

## Appendix A: IronOCR Pricing

Use these prices when pricing appears in feature comparison tables or migration benefit sections:

| Tier | Price | Description |
|---|---|---|
| Lite | $749 | 1 developer, 1 location |
| Professional | $1,499 | 10 developers, 10 locations |
| Enterprise | $2,999 | Unlimited developers, unlimited locations |
| **License Type** | **Perpetual** | One-time purchase, includes 1 year updates |

**Key pricing differentiators by competitor type:**

- **vs. On-Premise Commercial:** Highlight perpetual vs. subscription model where applicable (Aspose, Syncfusion community license limitations)
- **vs. Cloud APIs:** Highlight zero per-page/per-request cost, predictable annual spend, no metered billing surprises
- **vs. Open Source:** Highlight commercial support, SLA, and the true cost of maintaining Tesseract infrastructure (tessdata, native binaries, preprocessing pipelines) vs. a supported commercial package

## Appendix B: IronOCR Feature Summary for Reference

Use this list when building the 15+ row feature comparison tables. Select rows relevant to each competitor comparison.

| Feature | IronOCR Capability |
|---|---|
| OCR Engine | Optimized Tesseract 5 (bundled) |
| Installation | Single NuGet package (`IronOcr`) |
| Image Input | JPG, PNG, BMP, TIFF, GIF, and more |
| PDF Input | Native (no conversion needed) |
| Multi-Page TIFF | Yes |
| Searchable PDF Output | Yes (`result.SaveAsSearchablePdf()`) |
| Automatic Preprocessing | Deskew, DeNoise, Contrast, Binarize, Sharpen, Scale, Dilate, Erode, Invert |
| Deep Background Noise Removal | Yes (`DeepCleanBackgroundNoise()`) |
| Languages Supported | 125+ (bundled, no external tessdata) |
| Multi-Language Simultaneous | Yes (e.g., `OcrLanguage.French + OcrLanguage.German`) |
| Region-Based OCR | Yes (`CropRectangle`) |
| Barcode Reading | Yes (embedded in OCR pipeline) |
| Structured Data Output | Pages, Paragraphs, Lines, Words, Characters with coordinates |
| Confidence Scoring | Yes (`result.Confidence`) |
| Thread Safety | Full (create `IronTesseract` per thread) |
| Cross-Platform | Windows, Linux, macOS, Docker, Azure, AWS |
| .NET Compatibility | .NET Framework 4.6.2+, .NET Core, .NET 5/6/7/8/9 |
| Licensing | Perpetual (Lite $749, Pro $1,499, Enterprise $2,999) |
| Commercial Support | Yes (email, priority support on higher tiers) |
| NuGet Downloads | 5.3M+ |
