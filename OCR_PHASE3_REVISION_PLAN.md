# OCR Phase 3 Revision Plan

Final quality pass for all 54 OCR competitor articles (27 comparison + 27 migration). Phase 1 (comparison articles) and Phase 2 (migration articles) are complete and committed. Phase 3 revises every article for structural alignment, anti-repetition, formatting, and factual consistency before publishing.

---

## 1. Purpose

Final quality pass ensuring all 54 articles meet gold standard structure, tone, and formatting before publishing.

- **Product:** IronOCR
- **Competitors:** 27
- **Total articles:** 54 (27 compare + 27 migrate)
- **Gold standards:** `_gold-standard/reference-compare-xfinium-pdf-vs-ironpdf.md` and `_gold-standard/reference-migrate-from-zetpdf-to-ironpdf.md`

Every comparison article and every migration article must match the section order, heading style, tone constraints, and formatting rules defined below. No article ships until it passes every check in this plan.

---

## 2. Structural Verification -- Comparison Articles

For each comparison article (`compare-[competitor]-vs-ironocr.md`), verify these sections exist **in this exact order**:

| #  | Section | Key Constraints |
|----|---------|-----------------|
| 1  | Opening paragraph (no heading, no byline) | Sets context. No `#` heading above it. No `*By ...` attribution. |
| 2  | `## Understanding [Competitor]` | Neutral tone. **Zero IronOCR mentions.** Must contain a bullet list summarizing the competitor plus a code subsection showing the competitor's API in isolation. |
| 3  | `## Understanding IronOCR` | Neutral tone. No competitor-weakness mentions. Presents IronOCR on its own merits. |
| 4  | `## Feature Comparison` | Overview table first, then `### Detailed Feature Comparison` with an expanded table. |
| 5  | 2--4 Technical Comparison sections | Each section has `### [Competitor] Approach` followed by `### IronOCR Approach`. Real code examples in both. |
| 6  | `## API Mapping Reference` | Table only. **Zero code examples** in this section. Maps competitor API calls to IronOCR equivalents. |
| 7  | `## When Teams Consider Moving` | Prose `###` subsections only. **Zero code blocks. Zero API names.** Discusses organizational and workflow reasons, not technical specifics. |
| 8  | `## Common Migration Considerations` | Brief `###` notes. **No checklist format** (no `- [ ]` items). |
| 9  | `## Additional IronOCR Capabilities` | Bullet list. Lists only features **not mentioned elsewhere** in the article. |
| 10 | `## .NET Compatibility and Future Readiness` | Single paragraph. Covers framework support and forward-looking readiness. |
| 11 | `## Conclusion` | 3--4 paragraphs. **No code blocks. No new claims. No links.** Summarizes the comparison without introducing information not already present in the article. |

### Verification steps

1. Open the file and confirm headings appear in the order listed above.
2. Search "Understanding [Competitor]" for any occurrence of "IronOCR" -- must return zero results.
3. Search "When Teams Consider Moving" for fenced code blocks (` ``` `) and API names (e.g., method signatures, class names) -- must return zero results.
4. Confirm "Feature Comparison" contains both an overview table and a `### Detailed Feature Comparison` sub-heading with a second table.
5. Confirm "Conclusion" contains no fenced code blocks and no hyperlinks.
6. Confirm "Additional IronOCR Capabilities" bullet items are not duplicated from earlier sections.

---

## 3. Structural Verification -- Migration Articles

For each migration article (`migrate-from-[competitor].md`), verify these sections exist **in this exact order**:

| #  | Section | Key Constraints |
|----|---------|-----------------|
| 1  | `# Migrating from [Competitor] to IronOCR` | Exact title format. |
| 2  | Opening paragraph | Sets migration context. No byline. |
| 3  | `## Why Migrate from [Competitor]` | Lead-in sentence, then bold-headed reasons (e.g., `**Reason Name:** explanation`). Must end with `### The Fundamental Problem` subsection. |
| 4  | `## IronOCR vs [Competitor]: Feature Comparison` | Single feature comparison table. |
| 5  | `## Quick Start: [Competitor] to IronOCR Migration` | Full title format including competitor name. Exactly 3 numbered steps. |
| 6  | `## Code Migration Examples` | Each example uses `**[X] Approach:**` and `**IronOCR Approach:**` labels with fenced code blocks. |
| 7  | `## [Competitor] API to IronOCR Mapping Reference` | Table mapping old API to new API. |
| 8  | `## Common Migration Issues and Solutions` | Uses `### Issue N: [Name]` heading format for each issue. |
| 9  | `## [Competitor] Migration Checklist` | Exactly 3 subsections: `### Pre-Migration`, `### Code Migration`, `### Post-Migration`. |
| 10 | `## Key Benefits of Migrating to IronOCR` | Bold-headed paragraphs (e.g., `**Benefit Name:** explanation`). No bullet lists. |

### Verification steps

1. Confirm `# Migrating from [Competitor] to IronOCR` is the first heading.
2. Confirm "Why Migrate" contains `### The Fundamental Problem`.
3. Confirm "Quick Start" heading includes the competitor name and contains exactly 3 steps.
4. Confirm "Code Migration Examples" uses the `**[X] Approach:**` / `**IronOCR Approach:**` pattern.
5. Confirm "Common Migration Issues" uses `### Issue N:` numbering.
6. Confirm "Migration Checklist" has exactly 3 subsections: Pre-Migration, Code Migration, Post-Migration.
7. Confirm "Key Benefits" uses bold-headed paragraph format, not bullet lists.

---

## 4. Formatting Rules

Apply to **all 54 articles** without exception.

### 4.1 Zero `---` horizontal separators

Remove every `---` that acts as a horizontal rule. Do not remove `---` inside YAML front matter if present.

### 4.2 Zero contractions outside code blocks

Scan for and expand every contraction that appears outside of fenced code blocks. The full list to check:

| Contraction | Expansion |
|-------------|-----------|
| don't | do not |
| doesn't | does not |
| isn't | is not |
| aren't | are not |
| can't | cannot |
| couldn't | could not |
| wouldn't | would not |
| shouldn't | should not |
| won't | will not |
| haven't | have not |
| hasn't | has not |
| there's | there is |
| that's | that is |
| it's | it is |
| I'm | I am |
| I've | I have |
| you're | you are |
| you'll | you will |
| we're | we are |
| they're | they are |
| he's | he is |
| she's | she is |
| let's | let us |

**Important:** Do not modify contractions inside fenced code blocks (between ` ``` ` markers). Code samples may legitimately contain contractions in comments or string literals.

### 4.3 Zero bylines

Remove any line matching `*By ...` or `**By ...` patterns at the top of the article.

### 4.4 Zero "Related Comparisons" sections

Delete any `## Related Comparisons` or `## Related Articles` section and all its content.

### 4.5 Zero footer lines

Remove any of the following patterns wherever they appear:

- "Last verified ..."
- "See also ..."
- "For a deeper look ..."
- "Last updated ..."

### 4.6 Zero standalone Pricing Reference tables

Remove any pricing table that shows only IronOCR pricing without competitor pricing context. A pricing table in a feature comparison that includes both products is acceptable.

### 4.7 Zero TCO rows in feature comparison tables

Remove any table row containing speculative total-cost-of-ownership estimates based on assumed team sizes or renewal cycles. Examples of rows to remove:

- `| 5-year TCO (10 devs) | ~$X | ~$Y |`
- `| Total Cost of Ownership | ... | ... |`
- `| Estimated 3-year cost | ... | ... |`

At most one prose sentence about cost considerations may appear in the Conclusion if it is essential and does not include dollar estimates.

---

## 5. Tone and Voice Audit (Senior Dev Standard)

This is the most important quality gate. Formatting errors are easy to fix. Robotic prose is what makes content feel cheap.

### 5.0 The test

Read each paragraph aloud. If it sounds like it came from a content mill — vague, hedging, filler-heavy, says nothing specific — rewrite it.

### 5.1 Kill these patterns on sight

| Pattern | Example (BAD) | Fix |
|---------|--------------|-----|
| Vague opening | "When .NET developers evaluate OCR libraries, they often consider various factors..." | Start with the specific tension: "Tesseract is free but ships with no preprocessing — every team writes the same 100 lines of deskew/denoise code." |
| Hedge words | "may", "might", "could potentially", "in certain scenarios" | State the fact directly or remove the sentence |
| Transition filler | "Moving on to", "Next, let us examine", "Having covered X, let us now turn to Y" | Delete entirely. The heading is the transition. |
| Content-free adjectives | "comprehensive", "robust", "streamlined", "powerful" | Replace with what it actually does: "supports 125+ languages" not "comprehensive language support" |
| Redundant restatement | "IronOCR provides automatic preprocessing. This means that image quality is automatically improved." | Keep one sentence. Delete the restatement. |
| Promotional tone | "IronOCR offers an excellent solution that many teams find beneficial" | "IronOCR handles preprocessing in one method call — no external libraries needed." |
| Monotone sentence length | Five consecutive 15-20 word sentences | Mix 5-word punches with longer technical explanations |
| Warm-up paragraphs | Any paragraph that says "In this article, we will..." or "This comparison examines..." | Delete or rewrite as a direct hook |

### 5.2 Per-article tone check

For each article, verify:

1. **Opening paragraph** hooks in the first sentence with a specific claim, not a generic preamble.
2. **Code explanation paragraphs** are 2-4 sentences max. No paragraph after a code block exceeds 4 sentences.
3. **Zero instances** of: "comprehensive", "robust", "streamlined", "seamless", "powerful", "best-in-class", "cutting-edge".
4. **No back-to-back sentences** that start the same way (e.g., two sentences starting with "IronOCR" or two starting with "The library").
5. **Every claim has a concrete detail.** "Faster" needs a comparison. "Simpler" needs a line count or step count. "Cheaper" needs a dollar figure.

### 5.3 Tesseract cluster special check

For the 6 Tesseract wrapper articles (tesseract, charlesw-tesseract, tesseractocr, patagames-tesseract, tesseract-net-sdk, tesseract-maui, tesseract-ocr-wrapper), read all articles back-to-back. If any two articles feel interchangeable — same pain points, same IronOCR selling points, same structure of arguments — rewrite the weaker one to lean harder into its mandatory differentiation angle.

## 6. Anti-Repetition Checks

### 6.1 Cross-article checks (comparison vs. migration for same competitor)

For each competitor, open both articles side by side and verify:

1. **"Why Migrate" vs. comparison opening/technical sections:** The migration article's "Why Migrate from [Competitor]" section must not copy sentences verbatim from the comparison article's opening paragraph or technical comparison sections. Paraphrasing with different sentence structure is acceptable; identical sentences are not.

2. **Code Migration Examples vs. comparison technical sections:** The migration article's "Code Migration Examples" must use **different code scenarios** from the comparison article's technical comparison sections. The same underlying concept (e.g., "extract text from PDF") is acceptable if the code is written differently (different variable names, different file paths, different configuration, or a different sub-feature). Identical code blocks are not acceptable.

3. **"Key Benefits" vs. comparison "Conclusion":** The migration article's "Key Benefits of Migrating to IronOCR" must not restate the comparison article's "Conclusion" word-for-word. The same themes are fine; identical phrasing is not.

### 6.2 Within-article checks

For each article individually:

1. **No section restates content from a previous section.** If two sections cover the same point, consolidate into the section where it structurally belongs and remove the duplicate.

2. **"Understanding [Competitor]" has zero IronOCR mentions.** This section describes the competitor in isolation. Any mention of IronOCR -- even in passing -- must be removed.

3. **"When Teams Consider Moving" has zero code blocks and zero API names.** This section discusses organizational motivations, not technical details. Remove any fenced code blocks and any references to specific method names, class names, or API signatures.

4. **"Conclusion" introduces no new claims.** Every statement in the Conclusion must be supported by content already present in the article. If the Conclusion contains a claim not covered earlier, either add supporting content to the appropriate section or remove the claim from the Conclusion.

5. **"Additional IronOCR Capabilities" lists only features not mentioned elsewhere.** Cross-reference every bullet point against earlier sections. If a feature is already discussed (e.g., in the feature comparison table or a technical section), remove it from "Additional IronOCR Capabilities."

---

## 7. Duplicate Code Example Consolidation

Within each migration article, review all Code Migration Examples. If two examples produce **identical IronOCR API calls** (same static method invocation, same return type pattern, same essential structure), merge them:

1. Keep the first example with its full code block.
2. Replace the second example's IronOCR code block with a prose note: *"The IronOCR approach is identical to the example above -- [method name] handles this scenario with the same API call."*
3. Keep the second example's competitor code block intact so readers can see the competitor-side difference.

This prevents the impression that IronOCR examples were copy-pasted without thought.

---

## 8. IronOCR Link Verification and Density

**Minimum: 10 distinct IronOCR links per article. Target: 8-12.** Links must be distributed evenly with clusters of 2-3 links roughly every 4th paragraph.

### 7.1 Required Link Categories

Each article must contain links from at least 4 of these categories:

| Category | Example URLs | Where to Place |
|----------|-------------|----------------|
| Product page | `https://ironsoftware.com/csharp/ocr/` | First IronOCR mention, Understanding IronOCR |
| Docs/Tutorials | `https://ironsoftware.com/csharp/ocr/docs/`, `/tutorials/` | After code examples, Conclusion |
| How-To guides | `/how-to/searchable-pdf/`, `/how-to/input-pdfs/`, `/how-to/image-quality-correction/`, etc. | Code example explanations (bulk zone) |
| Examples | `/examples/simple-csharp-ocr-tesseract/`, `/examples/csharp-pdf-ocr/`, etc. | After code examples as "see full example" |
| Feature pages | `/features/preprocessing/`, `/features/languages/`, `/features/document/`, etc. | Additional Capabilities bullets |
| Deployment guides | `/get-started/docker/`, `/get-started/aws/`, `/get-started/linux/`, etc. | Cross-platform discussions |
| Specialized docs | `/how-to/read-passport/`, `/how-to/read-table-in-document/`, etc. | Relevant specialized document discussions |
| Blog posts | `/blog/using-ironocr/invoice-ocr-csharp-tutorial/`, etc. | Deeper-dive references sparingly |
| Licensing | `https://ironsoftware.com/csharp/ocr/licensing/` | Pricing/licensing discussions |
| NuGet | `https://www.nuget.org/packages/IronOcr` | Installation sections |

### 7.2 Verification Steps

1. Count total `ironsoftware.com` links per article. Flag any article with fewer than 10.
2. Verify links are not broken (no typos, no trailing characters).
3. Verify link distribution: no section should contain more than 40% of an article's total links.
4. Confirm no links appear in "Conclusion" sections of comparison articles (structural rule).
5. Preserve all existing correct links. Add links where density is too low.

### 7.3 Full Link Catalog Reference

Agents should consult `OCR_PHASE1_COMPARISON_PLAN.md` and `OCR_PHASE2_MIGRATION_PLAN.md` for the complete categorized link catalog with 80+ URLs organized by topic.

---

## 9. Format Audit Script

Create or update `audit.js` in the repository root to programmatically scan all 54 articles. The script must:

### 9.1 Directory scanning

List all 27 competitor directories and locate:
- `compare-[competitor]-vs-ironocr.md` (comparison article)
- `migrate-from-[competitor].md` (migration article)

### 9.2 Checks to perform

For each article, scan for and report:

| Check | Pattern | Severity |
|-------|---------|----------|
| Contractions outside code blocks | All contractions from Section 4.2 list, excluding content between ` ``` ` fences | Error |
| Horizontal separators | `---` on its own line (outside YAML front matter) | Error |
| Bylines | Lines matching `*By ` or `**By ` at start | Error |
| "Related Comparisons" | `## Related Comparisons` or `## Related Articles` | Error |
| "Last verified" | `Last verified`, `Last updated`, `See also`, `For a deeper look` | Warning |
| TCO rows | Table rows containing `TCO`, `Total Cost of Ownership`, or dollar estimates like `~$` in table cells | Error |
| Standalone pricing tables | Pricing tables without competitor column | Warning |
| Insufficient links | Fewer than 8 `ironsoftware.com` URLs in the article | Error |

### 9.3 Output format

The script should output:
- A summary line per article: `PASS` or `FAIL (N issues)`
- For each failure, the line number, check name, and the offending text
- A final summary: total articles scanned, total passed, total failed, total issues by category

### 9.4 Usage

```bash
node audit.js
```

No external dependencies. The script should use only Node.js built-in modules (`fs`, `path`).

---

## 10. Agent Execution

### 10.1 Parallel processing

Launch 27 parallel agents, one per competitor. Each agent handles both articles for its assigned competitor.

### 10.2 Per-agent workflow

Each agent performs the following steps in order:

1. **Read** both the comparison article and the migration article for its competitor.
2. **Read** this revision plan (`OCR_PHASE3_REVISION_PLAN.md`).
3. **Read** the gold standard articles (`_gold-standard/reference-compare-xfinium-pdf-vs-ironpdf.md` and `_gold-standard/reference-migrate-from-zetpdf-to-ironpdf.md`) for structural reference.
4. **Apply all fixes** from Sections 2--8 of this plan.
5. **Overwrite** both files with the corrected versions.
6. **Report** a summary of changes made, structured as:
   - Competitor name
   - Comparison article changes (list of fixes applied)
   - Migration article changes (list of fixes applied)
   - Any issues that could not be auto-fixed and require human review

### 10.3 Post-agent steps

After all 27 agents complete:

1. Run the format audit script: `node audit.js`
2. Review the audit output. If any articles still fail, apply targeted fixes.
3. Run the audit script a second time to confirm all articles pass.
4. Stage changes locally. The user commits and pushes at their discretion.

---

## 11. Quality Checklist Summary

### Per comparison article

- [ ] All 11 sections present in correct order (Opening through Conclusion)
- [ ] Zero IronOCR mentions in "Understanding [Competitor]"
- [ ] Zero code blocks in "When Teams Consider Moving"
- [ ] Zero code blocks in "Conclusion"
- [ ] Zero API names in "When Teams Consider Moving"
- [ ] Zero new claims in "Conclusion"
- [ ] "Additional IronOCR Capabilities" contains only features not covered elsewhere
- [ ] Feature Comparison has both overview table and Detailed Feature Comparison sub-table
- [ ] API Mapping Reference contains a table and zero code examples
- [ ] .NET Compatibility section is a single paragraph
- [ ] Zero contractions outside code blocks
- [ ] Zero `---` horizontal separators
- [ ] Zero bylines
- [ ] Zero "Related Comparisons" sections
- [ ] Zero footer lines (Last verified, See also, For a deeper look)
- [ ] Zero standalone pricing tables
- [ ] Zero TCO rows in tables
- [ ] Minimum 8 distinct IronOCR contextual links, distributed evenly with clusters every ~4th paragraph

### Per article — Tone checks (both comparison and migration)

- [ ] Opening paragraph hooks in the first sentence (no warm-up preamble)
- [ ] Zero instances of: "comprehensive", "robust", "streamlined", "seamless", "powerful", "best-in-class", "cutting-edge"
- [ ] Zero hedge words outside code blocks: "may", "might", "could potentially", "in certain scenarios"
- [ ] Zero transition filler: "Moving on to", "Next, let us examine", "It is worth noting that", "Having covered X"
- [ ] No paragraph after a code block exceeds 4 sentences
- [ ] No back-to-back sentences starting the same way
- [ ] Every claim backed by concrete detail (number, comparison, or specific feature name)
- [ ] Sentence length varies (mix of short punchy + longer technical)

### Per migration article

- [ ] All 10 sections present in correct order
- [ ] Title matches `# Migrating from [Competitor] to IronOCR` format
- [ ] "Why Migrate" includes `### The Fundamental Problem` subsection
- [ ] Quick Start heading includes competitor name and has exactly 3 steps
- [ ] Code Migration Examples use `**[X] Approach:**` / `**IronOCR Approach:**` labels
- [ ] Common Migration Issues use `### Issue N: [Name]` format
- [ ] Migration Checklist has exactly 3 subsections (Pre-Migration, Code Migration, Post-Migration)
- [ ] Key Benefits uses bold-headed paragraphs, not bullet lists
- [ ] Code examples differ from comparison article code examples
- [ ] Zero contractions outside code blocks
- [ ] Zero `---` horizontal separators
- [ ] Zero bylines
- [ ] Zero footer lines
- [ ] Zero TCO rows in tables
- [ ] Minimum 8 distinct IronOCR contextual links, distributed evenly with clusters every ~4th paragraph

### Cross-article checks (per competitor pair)

- [ ] No verbatim sentence copying between "Why Migrate" and comparison opening/technical sections
- [ ] No identical code blocks shared between migration Code Migration Examples and comparison technical sections
- [ ] No word-for-word restatement between migration "Key Benefits" and comparison "Conclusion"
- [ ] No duplicate IronOCR code examples within the migration article (consolidated per Section 7)

---

## Appendix: Competitor List

The 27 competitors and their directory names for reference:

| # | Competitor | Directory |
|---|-----------|-----------|
| 1 | ABBYY FineReader | abbyy-finereader |
| 2 | Amazon Textract | amazon-textract |
| 3 | Asprise OCR | asprise-ocr |
| 4 | Azure AI Vision (Computer Vision OCR) | azure-ai-vision |
| 5 | Dynamsoft Label Recognizer | dynamsoft-label-recognizer |
| 6 | EasyOCR | easyocr |
| 7 | Google Cloud Vision | google-cloud-vision |
| 8 | GdPicture.NET OCR | gdpicture-ocr |
| 9 | Hyperion OCR | hyperion-ocr |
| 10 | Inlite ClearImage | inlite-clearimage |
| 11 | Kofax OmniPage | kofax-omnipage |
| 12 | Leadtools OCR | leadtools-ocr |
| 13 | Microsoft OCR Library | microsoft-ocr-library |
| 14 | Nanonets | nanonets |
| 15 | Nicomsoft OCR | nicomsoft-ocr |
| 16 | OnlineOCR.net | onlineocr-net |
| 17 | OpenCV (with Tesseract) | opencv-tesseract |
| 18 | Paddle OCR | paddleocr |
| 19 | Rossum | rossum |
| 20 | Sakura OCR | sakura-ocr |
| 21 | SimpleOCR | simpleocr |
| 22 | SmartZone | smartzone |
| 23 | Syncfusion OCR | syncfusion-ocr |
| 24 | Tesseract.NET | tesseract-net |
| 25 | Textract Alternatives | textract-alternatives |
| 26 | Veryfi | veryfi |
| 27 | Windows.Media.Ocr | windows-media-ocr |

> **Note:** Directory names above are indicative. Agents must verify actual directory names in the repository before processing.
