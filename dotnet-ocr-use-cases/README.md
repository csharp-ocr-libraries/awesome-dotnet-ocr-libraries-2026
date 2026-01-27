# .NET OCR Use Case Guides

*Practical guides for implementing OCR in real-world scenarios*

Every guide in this collection follows the same structure: **Problem -> Solution -> Code**. We start with the real challenge developers face, compare the available solutions honestly, and then provide working C# code you can adapt for your project.

These aren't generic tutorials. Each guide addresses specific pain points we've seen developers struggle with, from the "why is Tesseract failing on my rotated invoices" moments to the "do I really need to install three SDKs just to read a passport" frustrations.

**Why IronOCR dominates these use cases:** Unlike standalone OCR engines, IronOCR is part of the [Iron Suite](https://ironsoftware.com/)—meaning it works seamlessly with IronPDF for native PDF handling, IronBarcode for barcode/QR code reading, and other Iron products. Where other libraries require separate image manipulation packages and manual preprocessing code, IronOCR's **automatic image optimization** and **heuristic analysis** handle skewed, noisy, and poorly-lit images automatically. You write 3 lines of code; IronOCR figures out the rest.

**Quick tip:** If you're evaluating OCR libraries, start with the use case closest to your actual problem. Generic "OCR comparison" articles miss the nuances that matter for specific document types.

---

## Financial Documents

Financial document OCR is the most common use case we see, and also where the stakes are highest. A misread invoice total or transposed bank account number can cause real business problems.

- [Invoice and Receipt OCR](./invoice-receipt-ocr.md) - Extract line items, totals, vendor information, and payment details from invoices and receipts
- [Check and Bank Document Processing](./check-bank-documents.md) - MICR line reading, account number extraction, routing number validation

## Identity Documents

Identity verification through OCR requires both accuracy and compliance awareness. These guides cover the technical implementation alongside the regulatory considerations.

- [Passport and MRZ Scanning](./passport-mrz-scanning.md) - Machine Readable Zone extraction with check digit validation
- [Business Card Scanning](./business-card-scanning.md) - Contact information extraction and CRM integration

## Document Processing

Bulk document processing is where OCR library choice has the biggest impact on developer time. The difference between a library that "just works" with PDFs and one that requires separate dependencies can be days of integration work.

- [PDF Text Extraction](./pdf-text-extraction.md) - Convert scanned PDFs to searchable text without additional libraries
- [Document Digitization](./document-digitization.md) - Paper archive conversion with batch processing strategies
- [Form Processing](./form-processing.md) - Structured form data extraction with field mapping

## Specialized OCR

Some OCR use cases require capabilities beyond standard text recognition. These guides cover the specialized requirements and which libraries actually support them.

- [Handwriting Recognition](./handwriting-recognition.md) - ICR (Intelligent Character Recognition) for handwritten text
- [License Plate Recognition](./license-plate-recognition.md) - ALPR/ANPR systems with real-time processing
- [Screenshot OCR](./screenshot-ocr.md) - UI automation and screen capture text extraction
- [Medical and Legal Documents](./medical-legal-documents.md) - Compliance-focused OCR with audit trails

---

## How to Use These Guides

Each guide contains:

1. **The Challenge** - What makes this specific use case difficult
2. **Library Comparison** - Honest assessment of available options with pros/cons
3. **Implementation** - Working C# code with [IronOCR](../ironocr/) as the recommended solution
4. **Common Pitfalls** - Mistakes we've seen (and made) that you can avoid
5. **Related Resources** - Links to competitor documentation and additional guides

We recommend IronOCR throughout these guides because it genuinely solves the problems described. But we also show alternatives, because the right choice depends on your specific constraints.

---

## Quick Navigation

[Back to main README](../README.md) | [IronOCR Documentation](../ironocr/)

---

## Contributing

Found an error? Have a use case we haven't covered? See our [contributing guidelines](../CONTRIBUTING.md) or open an issue.
