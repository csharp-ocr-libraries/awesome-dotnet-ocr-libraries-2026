# Veryfi OCR for .NET: Complete Developer Guide (2026)

*By [Jacob Mellor](https://ironsoftware.com/about-us/authors/jacobmellor/), CTO of Iron Software*

Veryfi is a cloud-based document intelligence API specializing in expense documents—receipts, invoices, checks, and bank statements. It claims 99%+ accuracy on supported document types through AI/ML models trained specifically for financial document extraction. Having integrated OCR into enterprise systems handling sensitive financial data for over a decade, I've seen both the appeal and the significant concerns that come with sending expense documents to third-party cloud services.

## Table of Contents

1. [Data Privacy Warning](#data-privacy-warning)
2. [What Is Veryfi?](#what-is-veryfi)
3. [Supported Document Types](#supported-document-types)
4. [Technical Architecture](#technical-architecture)
5. [Pricing Analysis](#pricing-analysis)
6. [Installation and Setup](#installation-and-setup)
7. [Basic Usage Examples](#basic-usage-examples)
8. [Specialist vs Generalist OCR](#specialist-vs-generalist-ocr)
9. [Key Limitations](#key-limitations)
10. [Compliance Certifications](#compliance-certifications)
11. [Veryfi vs IronOCR Comparison](#veryfi-vs-ironocr-comparison)
12. [Building Receipt Extraction with IronOCR](#building-receipt-extraction-with-ironocr)
13. [Migration Guide: Veryfi to IronOCR](#migration-guide-veryfi-to-ironocr)
14. [Code Examples](#code-examples)
15. [References](#references)

---

## Data Privacy Warning

**Every document you process with Veryfi is uploaded to their cloud servers.**

This is not a minor technical detail. Expense documents contain some of the most sensitive financial data in any organization:

### Sensitive Data Transmitted to Veryfi's Servers

| Data Type | What It Reveals | Risk Level |
|-----------|-----------------|------------|
| **Bank account numbers** | Direct access identifiers for financial accounts | Critical |
| **Transaction amounts** | Spending patterns, budget information | High |
| **Vendor relationships** | Supplier network, business partners | Medium |
| **Employee expense patterns** | Individual spending, locations visited | High |
| **Business spending patterns** | Cash flow, priorities, financial health | High |
| **Credit card numbers** | Payment card data on receipts | Critical |
| **Tax identification numbers** | Business and personal tax IDs | Critical |
| **Invoice payment terms** | Cash flow and credit arrangements | Medium |
| **Check routing numbers** | Bank routing and account details | Critical |

### What "Cloud Processing" Actually Means

When you call Veryfi's API:

1. **Your document bytes leave your infrastructure** - The receipt image or invoice PDF travels to Veryfi's servers
2. **Third-party processes your data** - Veryfi's systems extract, parse, and potentially store your data
3. **Data residency is Veryfi's choice** - Your expense data may be processed in regions you don't control
4. **Retention policies are Veryfi's policies** - You rely on their word for data deletion
5. **Security is Veryfi's security** - Any breach of their systems exposes your expense data

### Industries That Should Carefully Evaluate Cloud Expense Processing

**Financial Services:**
- Banks and credit unions handling customer transactions
- Investment firms with proprietary trading expense data
- Insurance companies processing claims documentation
- Payment processors managing merchant receipts

**Healthcare:**
- Medical practices with patient billing data
- Hospitals processing vendor invoices containing PHI
- Pharmaceutical companies with R&D expense documentation
- Healthcare networks managing multi-facility expenses

**Government and Defense:**
- Federal agencies with CUI (Controlled Unclassified Information)
- Defense contractors under DFARS/CMMC requirements
- State and local government procurement
- Intelligence community support organizations

**Legal and Professional Services:**
- Law firms with client matter expense documentation
- Accounting firms processing client financial data
- Consulting firms with confidential project expenses
- Any firm subject to attorney-client privilege concerns

### The Fundamental Question

Before adopting Veryfi, ask: **"Do I want a third-party cloud service to receive, process, and retain images of every expense document in my organization?"**

If the answer is "no" or "maybe not," consider on-premise alternatives that keep your financial data under your direct control.

---

## What Is Veryfi?

### Platform Overview

| Attribute | Details |
|-----------|---------|
| **Type** | Cloud API |
| **SDK** | Veryfi C# SDK (GitHub) |
| **Pricing** | Pay-per-document |
| **Specialty** | Expense document intelligence |
| **Accuracy Claim** | 99%+ on supported documents |
| **Processing** | AI/ML models trained on expense documents |

### Target Use Cases

Veryfi is designed specifically for:

- **Expense management applications** - Employee expense report automation
- **Accounting automation** - Invoice data entry into accounting systems
- **Receipt processing** - Point-of-sale receipt digitization
- **Accounts payable** - Invoice processing and matching
- **Tax preparation** - Receipt and expense documentation

### Core Value Proposition

Veryfi's appeal is structured output. Instead of raw OCR text that requires parsing, Veryfi returns pre-extracted fields:

```json
{
  "vendor": {
    "name": "Office Depot",
    "address": "123 Commerce St, Austin, TX 78701",
    "phone_number": "(512) 555-0123"
  },
  "date": "2026-01-15",
  "time": "14:32",
  "total": 156.78,
  "subtotal": 145.99,
  "tax": 10.79,
  "payment": {
    "type": "credit_card",
    "last4": "4242"
  },
  "line_items": [
    {
      "description": "Printer Paper (Case)",
      "quantity": 2,
      "unit_price": 22.99,
      "total": 45.98
    },
    {
      "description": "HP Ink Cartridge Black",
      "quantity": 1,
      "unit_price": 54.01,
      "total": 54.01
    },
    {
      "description": "USB Flash Drive 64GB",
      "quantity": 2,
      "unit_price": 22.99,
      "total": 45.98
    }
  ],
  "confidence_score": 0.97
}
```

This pre-parsed output eliminates the need to write regex patterns or field extraction logic—at the cost of sending your expense documents to a third-party cloud service.

---

## Supported Document Types

Veryfi focuses exclusively on expense and financial documents:

| Document Type | Extracted Fields | Use Case |
|--------------|------------------|----------|
| **Receipts** | Vendor, date, total, items, tax, payment | Expense reports |
| **Invoices** | Invoice #, vendor, amount, due date, line items | Accounts payable |
| **Checks** | Account #, routing #, amount, payee, date | Check processing |
| **Bank Statements** | Transactions, balances, account info | Reconciliation |
| **W-2 Forms** | Employer info, wages, tax withholdings | Tax processing |
| **Contracts** | Parties, dates, amounts, terms | Document management |
| **Business Cards** | Name, title, company, contact info | Contact management |

### Document Type Limitations

Veryfi is not a general-purpose OCR solution. It cannot effectively process:

- General business documents (memos, reports, manuals)
- Legal documents (contracts require specific training)
- Medical records
- Technical drawings or schematics
- Multi-language documents (limited language support)
- Custom form types without paid training
- Historical or degraded documents

If your OCR needs extend beyond expense documents, you'll need a supplementary solution.

---

## Technical Architecture

### How Veryfi Works

```
Your .NET Application
    |
    v
Veryfi C# SDK
    |
    v (HTTPS - Document bytes transmitted)
Veryfi Cloud API
    |
    +-- Document Upload & Storage
    +-- AI/ML Model Processing
    +-- Field Extraction
    +-- Response Generation
    |
    v
Structured JSON Response
```

### API Authentication

Veryfi uses a four-credential authentication model:

| Credential | Purpose |
|------------|---------|
| **Client ID** | Application identifier |
| **Client Secret** | Application secret key |
| **Username** | API user identifier |
| **API Key** | User-level API authentication |

This complexity requires secure storage for four separate credentials, each requiring rotation policies.

### SDK Architecture

The Veryfi C# SDK is available on GitHub and NuGet, providing a wrapper around the REST API:

```csharp
// Four credentials required for authentication
var client = new VeryfiClient(
    clientId: "your_client_id",
    clientSecret: "your_client_secret",
    username: "your_username",
    apiKey: "your_api_key"
);
```

### Processing Flow

1. **Document bytes read** - Your application reads the receipt/invoice
2. **Base64 encoding** - Document converted for transmission
3. **HTTPS upload** - Document sent to Veryfi servers
4. **AI processing** - Veryfi's models extract fields
5. **Response returned** - Structured JSON with extracted data

Every step after #1 involves your document data leaving your infrastructure.

---

## Pricing Analysis

### Per-Document Pricing Model

Veryfi charges for each document processed:

| Document Type | Approximate Price Per Document |
|--------------|--------------------------------|
| **Receipt** | $0.05 - $0.15 |
| **Invoice** | $0.10 - $0.25 |
| **Bank Statement** | $0.15 - $0.30 |
| **Check** | $0.10 - $0.20 |
| **W-2** | $0.15 - $0.25 |

*Exact pricing varies by plan, volume, and contract terms. Visit [Veryfi pricing page](https://www.veryfi.com/pricing/) for current rates.*

### Volume Cost Projections

**Scenario A: Small Business (5,000 receipts/month)**

```
Veryfi Annual Cost:
  5,000 receipts × $0.10 × 12 months = $6,000/year

IronOCR Alternative:
  Professional License: $2,999 one-time
  + Development time for extraction logic: ~8-16 hours

  Year 1: $2,999
  Year 2+: $0

  3-Year Savings: $15,001
```

**Scenario B: Medium Business (50,000 receipts/month)**

```
Veryfi Annual Cost:
  50,000 receipts × $0.10 × 12 months = $60,000/year

IronOCR Alternative:
  Enterprise License: $5,999 one-time
  + Development time: ~16-24 hours

  Year 1: $5,999
  Year 2+: $0

  3-Year Savings: $174,001
  5-Year Savings: $294,001
```

**Scenario C: Enterprise (200,000 receipts/month)**

```
Veryfi Annual Cost:
  200,000 receipts × $0.08 (volume discount) × 12 months = $192,000/year

IronOCR Alternative:
  Enterprise License with OEM: Custom pricing, typically $10,000-25,000
  + Development time: ~40 hours

  3-Year Savings: $500,000+
```

### Hidden and Indirect Costs

Beyond per-document pricing, consider:

| Hidden Cost | Description |
|-------------|-------------|
| **Overage charges** | Unexpected volume spikes increase bills |
| **Premium features** | Advanced extraction may require higher tiers |
| **API rate limiting** | Throttling forces batch processing redesign |
| **Integration maintenance** | SDK updates, API version changes |
| **Compliance overhead** | Legal review of data processing agreements |
| **Vendor lock-in** | Veryfi's JSON schema is proprietary |

### Cost Unpredictability

Per-document pricing creates budget uncertainty:

- Seasonal business fluctuations affect costs
- Growth increases expenses proportionally
- No cost ceiling without volume caps
- Year-over-year price increases possible

---

## Installation and Setup

### SDK Installation

```bash
# NuGet Package Manager
Install-Package Veryfi

# .NET CLI
dotnet add package Veryfi
```

### Project Configuration

```xml
<PackageReference Include="Veryfi" Version="*" />
```

### Credential Setup

Veryfi requires four credentials stored securely:

```csharp
// Configuration in appsettings.json (not recommended for production)
{
  "Veryfi": {
    "ClientId": "your_client_id",
    "ClientSecret": "your_client_secret",
    "Username": "your_username",
    "ApiKey": "your_api_key"
  }
}
```

For production, use secure secret management:

```csharp
// Using Azure Key Vault, AWS Secrets Manager, or similar
public class VeryfiService
{
    private readonly VeryfiClient _client;

    public VeryfiService(ISecretManager secrets)
    {
        _client = new VeryfiClient(
            secrets.GetSecret("Veryfi:ClientId"),
            secrets.GetSecret("Veryfi:ClientSecret"),
            secrets.GetSecret("Veryfi:Username"),
            secrets.GetSecret("Veryfi:ApiKey")
        );
    }
}
```

### Network Requirements

Veryfi requires:
- Outbound HTTPS access to api.veryfi.com
- Stable internet connectivity for every document
- Network bandwidth for document uploads

---

## Basic Usage Examples

### Receipt Processing

```csharp
using Veryfi;

public class ReceiptProcessor
{
    private readonly VeryfiClient _client;

    public ReceiptProcessor(string clientId, string clientSecret,
                            string username, string apiKey)
    {
        _client = new VeryfiClient(clientId, clientSecret, username, apiKey);
    }

    public async Task<ExpenseData> ProcessReceiptAsync(string imagePath)
    {
        // WARNING: Receipt image uploaded to Veryfi cloud
        // Sensitive data (amounts, vendors, payment info) transmitted
        var bytes = File.ReadAllBytes(imagePath);
        var base64 = Convert.ToBase64String(bytes);

        // Per-document charge applies here (~$0.05-0.15)
        var response = await _client.ProcessDocumentAsync(base64);

        return new ExpenseData
        {
            Vendor = response.Vendor?.Name,
            Date = response.Date,
            Total = response.Total,
            Subtotal = response.Subtotal,
            Tax = response.Tax,
            LineItems = response.LineItems?.Select(li => new LineItem
            {
                Description = li.Description,
                Quantity = li.Quantity,
                Price = li.Price
            }).ToList()
        };
    }
}
```

### Invoice Processing

```csharp
public async Task<InvoiceData> ProcessInvoiceAsync(string pdfPath)
{
    // Invoice data transmitted to Veryfi servers
    // Includes: vendor info, amounts, bank details, payment terms
    var bytes = File.ReadAllBytes(pdfPath);

    var response = await _client.ProcessDocumentAsync(
        fileData: bytes,
        categories: new[] { "invoices" }
    );

    return new InvoiceData
    {
        InvoiceNumber = response.InvoiceNumber,
        VendorName = response.Vendor?.Name,
        VendorAddress = response.Vendor?.Address,
        Total = response.Total,
        DueDate = response.DueDate,
        PaymentTerms = response.PaymentTerms,
        LineItems = response.LineItems
    };
}
```

---

## Specialist vs Generalist OCR

### The Specialist Advantage (Veryfi)

Veryfi excels at its specialty:

| Strength | Description |
|----------|-------------|
| **Pre-trained models** | No ML expertise required |
| **Structured output** | JSON with extracted fields |
| **High accuracy** | 99%+ on supported document types |
| **Fast integration** | API call returns parsed data |
| **Field confidence** | Per-field confidence scores |

For pure expense document processing where cloud is acceptable, Veryfi delivers results quickly.

### The Generalist Advantage (IronOCR)

IronOCR provides versatility:

| Strength | Description |
|----------|-------------|
| **Any document type** | Not limited to expense documents |
| **On-premise processing** | Data never leaves your infrastructure |
| **Custom extraction** | Build exactly what you need |
| **No per-document cost** | Unlimited processing after license |
| **Offline capability** | Works without internet |
| **Preprocessing built-in** | Deskew, denoise, contrast automatically |

### The Real Trade-off

**Veryfi:** "Great at one thing, but if you want a well-rounded solution..."

| Scenario | Veryfi | IronOCR |
|----------|--------|---------|
| Process 10K receipts/month | Works well (but $1,000+/month) | Works well ($3K one-time) |
| Process receipts + contracts | Need second solution | Single solution |
| Process receipts offline | Not possible | Works fully |
| Keep data on-premise | Not possible | Default behavior |
| Custom document types | Paid training required | Build your own extraction |

### When Specialist Falls Short

Real organizations rarely process only expense documents:

- **HR departments** need employee documents (not just expenses)
- **Legal teams** need contract OCR (not supported well)
- **Operations** need shipping/logistics documents
- **Customer service** needs varied document types

Adopting Veryfi for expenses often means maintaining multiple OCR solutions—complexity that a generalist tool avoids.

---

## Key Limitations

### 1. Cloud Processing Required

**Every document must be transmitted to Veryfi's servers.**

- No offline processing capability
- No air-gapped deployment option
- Internet connectivity required for each document
- Latency includes network round-trip
- Processing speed depends on Veryfi's infrastructure load

### 2. Per-Document Costs at Scale

**Costs grow linearly with volume.**

| Volume | Monthly Cost (@ $0.10/doc) | Annual Cost |
|--------|---------------------------|-------------|
| 10,000 | $1,000 | $12,000 |
| 50,000 | $5,000 | $60,000 |
| 100,000 | $10,000 | $120,000 |
| 500,000 | $50,000 | $600,000 |

No cost ceiling exists without contractual caps.

### 3. Document Type Limitations

**Optimized for expense documents only.**

- General documents get poor results
- Custom documents require paid training
- Multi-language support is limited
- Historical/degraded documents may fail
- Complex layouts may not parse correctly

### 4. No Offline Capability

**Requires internet for every operation.**

- Field technicians can't process offline
- Batch processing requires connectivity
- Network outages halt processing
- Remote locations may have issues
- Mobile apps need constant connectivity

### 5. Limited Customization

**Take the pre-trained models or leave them.**

- Can't adjust extraction logic
- Can't handle edge cases locally
- Custom fields require paid training
- Extraction errors require support tickets
- No ability to tune for your document variants

### 6. Vendor Lock-in

**Veryfi's JSON schema is proprietary.**

- Switching vendors requires code changes
- No standard expense document format
- Integration is Veryfi-specific
- Data export may not be straightforward
- Multi-year contracts may apply

---

## Compliance Certifications

### What Veryfi Claims

| Certification | Description |
|---------------|-------------|
| **SOC 2 Type II** | Security controls audited |
| **GDPR** | EU data protection compliance |
| **HIPAA** | Healthcare data handling (with BAA) |
| **CCPA** | California consumer privacy |
| **ITAR** | International traffic in arms (limited) |

### What Certifications Actually Mean

**Certifications prove Veryfi follows their stated security practices.** They do NOT mean:

- Your data stays in your control
- Your data never leaves your infrastructure
- You control where data is processed
- You control how long data is retained
- You have full audit visibility

### Certifications vs Control

| Aspect | With Veryfi (Certified) | With IronOCR (On-Premise) |
|--------|-------------------------|---------------------------|
| **Data location** | Veryfi's choice | Your infrastructure |
| **Data retention** | Veryfi's policy | Your policy |
| **Access controls** | Veryfi manages | You manage |
| **Audit trail** | Veryfi provides | You control |
| **Breach response** | Veryfi's process | Your process |
| **Subprocessors** | Veryfi's vendors | None |

### The Compliance Reality

For highly regulated industries:

- **HIPAA:** Requires BAA with Veryfi, adds them as Business Associate
- **CMMC Level 2+:** Cloud processing of CUI may be problematic
- **ITAR:** Processing controlled data offshore requires authorization
- **SOX:** Financial data processing adds audit scope
- **GLBA:** Customer financial data protection requirements apply

**On-premise processing eliminates third-party compliance dependencies entirely.**

---

## Veryfi vs IronOCR Comparison

### Feature Comparison

| Feature | Veryfi | IronOCR |
|---------|--------|---------|
| **Deployment** | Cloud API only | On-premise |
| **Data location** | Veryfi servers | Your infrastructure |
| **Output type** | Structured JSON | Text + positioning |
| **Document types** | Expense-focused | Any document |
| **Offline support** | No | Yes |
| **Per-document cost** | Yes ($0.05-0.30) | No (licensed) |
| **Accuracy** | 99%+ on supported | Depends on preprocessing |
| **Custom extraction** | Paid training | Code your own |
| **Air-gapped** | Not possible | Fully supported |
| **Preprocessing** | Handled by Veryfi | Built-in filters |
| **Multi-page PDF** | Supported | Supported |

### Development Experience Comparison

| Aspect | Veryfi | IronOCR |
|--------|--------|---------|
| **Setup time** | 30 minutes | 15 minutes |
| **First result** | Fast (API call) | Fast (method call) |
| **Credential management** | 4 credentials | License key |
| **Error handling** | API errors | Local exceptions |
| **Debugging** | Limited visibility | Full local control |
| **Unit testing** | Requires mocking API | Direct testing |

### Cost Comparison (3-Year TCO)

**Scenario: 25,000 documents/month**

| Cost Category | Veryfi | IronOCR |
|---------------|--------|---------|
| **Year 1 processing** | $30,000 | $0 |
| **Year 2 processing** | $30,000 | $0 |
| **Year 3 processing** | $30,000 | $0 |
| **License** | N/A | $3,999 |
| **Development** | 8 hours | 16 hours |
| **3-Year Total** | $90,000+ | $3,999 |

---

## Building Receipt Extraction with IronOCR

For on-premise expense processing, IronOCR combined with pattern matching can achieve similar results to Veryfi while keeping data local.

### Basic Receipt Extraction

```csharp
using IronOcr;
using System.Text.RegularExpressions;

public class LocalReceiptExtractor
{
    private readonly IronTesseract _ocr;

    public LocalReceiptExtractor()
    {
        _ocr = new IronTesseract();
        // Enable automatic preprocessing for receipt images
        _ocr.Configuration.ReadBarCodes = false; // Optimize for text
    }

    public ReceiptData ExtractReceipt(string imagePath)
    {
        // ALL PROCESSING HAPPENS LOCALLY
        // No data transmitted to any external service
        var result = _ocr.Read(imagePath);
        var text = result.Text;
        var lines = result.Lines.OrderBy(l => l.Y).ToList();

        return new ReceiptData
        {
            Vendor = ExtractVendor(lines),
            Date = ExtractDate(text),
            Time = ExtractTime(text),
            Total = ExtractTotal(text),
            Subtotal = ExtractSubtotal(text),
            Tax = ExtractTax(text),
            LineItems = ExtractLineItems(lines),
            PaymentMethod = ExtractPaymentMethod(text),
            RawText = text
        };
    }

    private string ExtractVendor(List<OcrLine> lines)
    {
        // Vendor typically appears in the first few lines
        return lines.Take(3)
                    .Select(l => l.Text.Trim())
                    .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t)
                                         && t.Length > 3);
    }

    private DateTime? ExtractDate(string text)
    {
        var patterns = new[]
        {
            @"\d{1,2}/\d{1,2}/\d{4}",
            @"\d{1,2}-\d{1,2}-\d{4}",
            @"\d{4}-\d{2}-\d{2}",
            @"(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]* \d{1,2},? \d{4}"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && DateTime.TryParse(match.Value, out var date))
                return date;
        }
        return null;
    }

    private decimal? ExtractTotal(string text)
    {
        var patterns = new[]
        {
            @"Total:?\s*\$?\s*([\d,]+\.?\d*)",
            @"Grand Total:?\s*\$?\s*([\d,]+\.?\d*)",
            @"Amount:?\s*\$?\s*([\d,]+\.?\d*)",
            @"Balance Due:?\s*\$?\s*([\d,]+\.?\d*)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && decimal.TryParse(
                match.Groups[1].Value.Replace(",", ""), out var total))
            {
                return total;
            }
        }
        return null;
    }

    private decimal? ExtractTax(string text)
    {
        var pattern = @"(?:Tax|Sales Tax|VAT):?\s*\$?\s*([\d,]+\.?\d*)";
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        if (match.Success && decimal.TryParse(
            match.Groups[1].Value.Replace(",", ""), out var tax))
        {
            return tax;
        }
        return null;
    }

    private List<LineItem> ExtractLineItems(List<OcrLine> lines)
    {
        var items = new List<LineItem>();

        // Line items typically have: description, quantity, price
        var pricePattern = @"\$?([\d,]+\.?\d{2})";

        foreach (var line in lines)
        {
            var matches = Regex.Matches(line.Text, pricePattern);
            if (matches.Count > 0)
            {
                var lastMatch = matches[matches.Count - 1];
                var description = line.Text.Substring(0, lastMatch.Index).Trim();

                if (!string.IsNullOrWhiteSpace(description)
                    && !IsMetadataLine(description))
                {
                    items.Add(new LineItem
                    {
                        Description = description,
                        Total = decimal.Parse(lastMatch.Groups[1].Value.Replace(",", ""))
                    });
                }
            }
        }

        return items;
    }

    private bool IsMetadataLine(string text)
    {
        var skipPatterns = new[] { "subtotal", "total", "tax", "change", "cash", "credit" };
        return skipPatterns.Any(p => text.ToLower().Contains(p));
    }
}
```

### Comparison: Veryfi vs IronOCR Implementation

| Aspect | Veryfi | IronOCR + Patterns |
|--------|--------|-------------------|
| **Lines of code** | ~20 | ~80 |
| **Development time** | 30 min | 2-4 hours |
| **Data location** | Veryfi cloud | Your server |
| **Per-doc cost** | $0.05-0.15 | $0 |
| **Customization** | Limited | Full control |
| **Edge cases** | Submit support ticket | Fix your code |

---

## Migration Guide: Veryfi to IronOCR

This section provides a complete migration path from Veryfi to IronOCR for .NET applications seeking data sovereignty and cost control.

### Why Migrate from Veryfi?

| Pain Point | Veryfi Reality | IronOCR Solution |
|------------|----------------|------------------|
| **Data leaves infrastructure** | Every document to cloud | All processing local |
| **Mounting per-doc costs** | $60K+/year at scale | One-time license |
| **Document type limits** | Expenses only | Any document |
| **Offline impossible** | Internet required | Fully offline |
| **Vendor lock-in** | Proprietary schema | Your code, your data |

### Migration Complexity Assessment

| Current Usage | Estimated Migration Time | Complexity |
|---------------|--------------------------|------------|
| **Receipt-only processing** | 2-4 hours | Low |
| **Receipts + invoices** | 4-8 hours | Medium |
| **Full expense ecosystem** | 1-2 days | Medium |
| **Custom trained models** | 2-5 days | High |

### Step 1: Package Changes

**Remove Veryfi SDK:**

```bash
# NuGet Package Manager
Uninstall-Package Veryfi

# .NET CLI
dotnet remove package Veryfi
```

**Add IronOCR:**

```bash
# NuGet Package Manager
Install-Package IronOcr

# .NET CLI
dotnet add package IronOcr
```

**Project file changes:**

```xml
<!-- Remove -->
<PackageReference Include="Veryfi" Version="*" />

<!-- Add -->
<PackageReference Include="IronOcr" Version="2024.*" />
```

### Step 2: License Configuration

**Remove Veryfi credentials:**

```csharp
// Delete from configuration
// - ClientId
// - ClientSecret
// - Username
// - ApiKey
```

**Add IronOCR license:**

```csharp
// Single license key, one-time setup
IronOcr.License.LicenseKey = "YOUR-IRONOCR-LICENSE-KEY";
```

### Step 3: API Migration

**Veryfi Client Initialization:**

```csharp
// Before (Veryfi)
var client = new VeryfiClient(clientId, clientSecret, username, apiKey);
```

**IronOCR Initialization:**

```csharp
// After (IronOCR)
var ocr = new IronTesseract();
```

### Step 4: Receipt Processing Migration

**Before (Veryfi):**

```csharp
public async Task<ReceiptResult> ProcessReceiptAsync(string path)
{
    // Document uploaded to Veryfi cloud
    var bytes = File.ReadAllBytes(path);
    var response = await _client.ProcessDocumentAsync(bytes);

    return new ReceiptResult
    {
        Vendor = response.Vendor?.Name,
        Total = response.Total,
        Date = response.Date
    };
}
```

**After (IronOCR):**

```csharp
public ReceiptResult ProcessReceipt(string path)
{
    // All processing stays local
    var result = _ocr.Read(path);

    return new ReceiptResult
    {
        Vendor = GetVendor(result),
        Total = ExtractTotal(result.Text),
        Date = ExtractDate(result.Text)
    };
}

private string GetVendor(OcrResult result)
{
    return result.Lines.FirstOrDefault()?.Text;
}

private decimal? ExtractTotal(string text)
{
    var match = Regex.Match(text, @"Total:?\s*\$?([\d,]+\.?\d*)",
                            RegexOptions.IgnoreCase);
    if (match.Success)
        return decimal.Parse(match.Groups[1].Value.Replace(",", ""));
    return null;
}

private DateTime? ExtractDate(string text)
{
    var match = Regex.Match(text, @"\d{1,2}/\d{1,2}/\d{4}");
    if (match.Success)
        return DateTime.Parse(match.Value);
    return null;
}
```

### Step 5: Invoice Processing Migration

**Before (Veryfi):**

```csharp
public async Task<InvoiceResult> ProcessInvoiceAsync(string path)
{
    var bytes = File.ReadAllBytes(path);
    var response = await _client.ProcessDocumentAsync(bytes,
                                   categories: new[] { "invoices" });

    return new InvoiceResult
    {
        InvoiceNumber = response.InvoiceNumber,
        VendorName = response.Vendor?.Name,
        Total = response.Total,
        DueDate = response.DueDate
    };
}
```

**After (IronOCR):**

```csharp
public InvoiceResult ProcessInvoice(string path)
{
    var result = _ocr.Read(path);
    var text = result.Text;

    return new InvoiceResult
    {
        InvoiceNumber = ExtractPattern(text, @"Invoice\s*#?\s*:?\s*(\w+)"),
        VendorName = result.Lines.FirstOrDefault()?.Text,
        Total = ExtractCurrency(text, @"Total:?\s*\$?([\d,]+\.?\d*)"),
        DueDate = ExtractDate(text, @"Due\s*Date:?\s*(\d{1,2}/\d{1,2}/\d{4})")
    };
}
```

### Step 6: Handle Async to Sync Transition

Veryfi uses async APIs; IronOCR is synchronous (faster for local processing).

**For UI thread compatibility:**

```csharp
// Wrap in Task.Run if needed for UI responsiveness
public async Task<ReceiptResult> ProcessReceiptAsync(string path)
{
    return await Task.Run(() => ProcessReceipt(path));
}
```

### Common Migration Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| **Missing structured fields** | Veryfi pre-extracts, IronOCR returns text | Build extraction patterns |
| **Async/await errors** | IronOCR is sync | Remove async or wrap in Task.Run |
| **Credential errors** | Looking for Veryfi config | Remove credential configuration |
| **Different accuracy** | Different OCR engines | Use preprocessing filters |

### Migration Checklist

**Pre-Migration:**
- [ ] Inventory all Veryfi API calls in codebase
- [ ] Document expected output fields
- [ ] Create test document corpus
- [ ] Obtain IronOCR license

**Migration:**
- [ ] Remove Veryfi NuGet package
- [ ] Add IronOCR NuGet package
- [ ] Configure IronOCR license
- [ ] Replace Veryfi client initialization
- [ ] Migrate receipt processing calls
- [ ] Migrate invoice processing calls
- [ ] Build field extraction patterns
- [ ] Update error handling

**Post-Migration:**
- [ ] Run validation tests
- [ ] Compare extraction accuracy
- [ ] Remove Veryfi credentials from all environments
- [ ] Update deployment pipelines
- [ ] Document cost savings

---

## Code Examples

Complete working examples demonstrating Veryfi patterns and IronOCR alternatives:

- [Veryfi vs IronOCR Examples](./veryfi-vs-ironocr-examples.cs) - Side-by-side comparison code
- [Receipt Extraction Patterns](./veryfi-receipt-extraction.cs) - Veryfi API patterns with data privacy annotations
- [Migration Comparison](./veryfi-migration-comparison.cs) - Before/after migration examples

---

## References

- <a href="https://www.veryfi.com/" rel="nofollow">Veryfi Official Website</a>
- <a href="https://github.com/veryfi/veryfi-csharp" rel="nofollow">Veryfi C# SDK on GitHub</a>
- <a href="https://www.veryfi.com/api/" rel="nofollow">Veryfi API Documentation</a>
- [IronOCR Documentation](https://ironsoftware.com/csharp/ocr/docs/)
- [IronOCR Tutorials](https://ironsoftware.com/csharp/ocr/tutorials/)
- [IronOCR NuGet Package](https://www.nuget.org/packages/IronOcr/)

---

*Last verified: January 2026*
