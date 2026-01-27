# Passport and MRZ Scanning in .NET: Implementation Guide

*By [Jacob Mellor](https://ironsoftware.com/about-us/authors/jacobmellor/), CTO of Iron Software*

Your KYC workflow needs passport verification. Do you buy Dynamsoft's MRZ SDK, their Barcode SDK, AND their PDF SDK separately? Or do you use [IronOCR](../ironocr/) which includes all three in a single package?

This isn't a rhetorical question. We've talked to developers who spent $5,000+ on specialized SDKs before realizing a general-purpose OCR library with passport support would have done the job. MRZ reading isn't magic. It's OCR with specific fonts and validation rules. Any good OCR engine can handle it if you know what you're doing.

This guide covers the Machine Readable Zone (MRZ) format, compares the available .NET solutions, and provides working code that extracts passport data with proper check digit validation.

---

## Table of Contents

1. [The MRZ Challenge](#the-mrz-challenge)
2. [How MRZ OCR Works](#how-mrz-ocr-works)
3. [Library Comparison for MRZ](#library-comparison-for-mrz)
4. [Implementation Guide with IronOCR](#implementation-guide-with-ironocr)
5. [Beyond Passports](#beyond-passports)
6. [Common Pitfalls](#common-pitfalls)
7. [Related Use Cases](#related-use-cases)

---

## The MRZ Challenge

### What is MRZ (Machine Readable Zone)?

The Machine Readable Zone is the two or three lines of text at the bottom of a passport or ID card. Those angular characters that look vaguely futuristic aren't random. They're a precisely specified format defined by the International Civil Aviation Organization (ICAO) in Document 9303.

Every MRZ encodes:
- Document type
- Country code
- Surname and given names
- Passport number with check digit
- Nationality
- Date of birth with check digit
- Sex
- Date of expiry with check digit
- Optional data (varies by country)
- Overall check digit

The format looks like this:

```
P<UTOERIKSSON<<ANNA<MARIA<<<<<<<<<<<<<<<<<<<
L898902C36UTO7408122F1204159ZE184226B<<<<<10
```

That's a TD3 format passport for "ANNA MARIA ERIKSSON" from Utopia (UTO), born August 12, 1974, passport number L898902C3, expiring April 15, 2012.

### Why MRZ Matters

MRZ extraction serves several industries:

**Immigration and Border Control**
Automated passport readers at airport e-gates scan MRZ zones in under a second. Human inspection of 200+ characters would take significantly longer and introduce errors.

**KYC (Know Your Customer)**
Financial institutions verify identity during onboarding. Extracting passport data automatically reduces friction and errors compared to manual entry.

**Hotel and Travel Check-in**
Hotels in many jurisdictions must record guest passport details. MRZ scanning automates this regulatory requirement.

**Age Verification**
Any business requiring age verification can use passport MRZ to extract date of birth reliably.

### Compliance Requirements (ICAO Standards)

Document 9303 specifies everything about MRZ:

| Element | Specification |
|---------|---------------|
| Font | OCR-B as defined in ISO 1073-2 |
| Character height | 2.5-3.0mm for TD3 |
| Character width | 1.5-2.0mm for TD3 |
| Line spacing | Precisely defined per format |
| Check digits | Modulo 10 with specific weights |
| Character set | A-Z, 0-9, and filler character (<) |

The OCR-B font was specifically designed for machine reading in the 1960s. Its regular, non-connected letterforms minimize OCR errors. This is why MRZ zones are actually easier to read than general text, if your OCR engine knows what to look for.

---

## How MRZ OCR Works

### OCR-B Font Recognition

The OCR-B font used in MRZ has characteristics that make it ideal for machine reading:

- **No serifs** - Clean letter edges
- **Distinct characters** - 0/O, 1/I, 5/S designed to be different
- **Monospaced** - Every character same width
- **Limited character set** - Only 37 possible characters

A general OCR engine can read OCR-B, but engines with specific OCR-B training perform better on damaged or low-quality scans.

### MRZ Formats: TD1, TD2, TD3

Three main formats exist:

**TD3 (Passport)**
- 2 lines, 44 characters each
- Used in standard passports
- Most common format

```
P<UTOERIKSSON<<ANNA<MARIA<<<<<<<<<<<<<<<<<<<
L898902C36UTO7408122F1204159ZE184226B<<<<<10
```

**TD2 (Older ID cards)**
- 2 lines, 36 characters each
- Used in some ID cards and older passports
- Less common today

**TD1 (ID cards)**
- 3 lines, 30 characters each
- Used in credit card-sized ID documents
- Common in European national ID cards

```
I<UTOD231458907<<<<<<<<<<<<<<<
7408122F1204159UTO<<<<<<<<<<<6
ERIKSSON<<ANNA<MARIA<<<<<<<<<<
```

### Check Digit Validation

MRZ uses modulo 10 check digits with weighted character positions. Each check digit validates a specific field:

| Position | Validates |
|----------|-----------|
| Character 10 (line 2) | Passport number |
| Character 20 (line 2) | Date of birth |
| Character 28 (line 2) | Date of expiry |
| Character 43 (line 2) | Composite (all above) |

The algorithm:
1. Convert each character to a number (A=10, B=11, ..., Z=35, <=0)
2. Multiply alternating positions by weights 7, 3, 1, 7, 3, 1...
3. Sum results
4. Take modulo 10

If the calculated check digit doesn't match the scanned digit, something is wrong. Either the scan quality is poor or the document may be fraudulent.

### Field Parsing

Once you have the raw MRZ text, parsing extracts individual fields:

```csharp
// TD3 Line 1 (44 characters)
// Position 1-2: Document type (P<)
// Position 3-5: Issuing country
// Position 6-44: Name (SURNAME<<GIVEN<NAMES)

// TD3 Line 2 (44 characters)
// Position 1-9: Passport number
// Position 10: Check digit (passport number)
// Position 11-13: Nationality
// Position 14-19: Date of birth (YYMMDD)
// Position 20: Check digit (DOB)
// Position 21: Sex (M/F/<)
// Position 22-27: Date of expiry (YYMMDD)
// Position 28: Check digit (expiry)
// Position 29-42: Optional data
// Position 43: Check digit (optional data)
// Position 44: Composite check digit
```

---

## Library Comparison for MRZ

### IronOCR - Built-in Passport Support

[IronOCR](../ironocr/) includes dedicated passport reading methods. No separate MRZ SDK, no additional license.

```csharp
var result = new IronTesseract().ReadPassport("passport-scan.jpg");
// Returns structured passport data with validation
```

**Why it works:** IronOCR's `ReadPassport()` method:
- Automatically locates the MRZ zone
- Applies OCR-B-optimized recognition
- Parses all fields
- Validates check digits
- Returns structured data

**What you get:**
- Passport reading as one feature among many
- Same license covers general OCR, PDF, barcodes
- No additional cost for MRZ capability
- On-premise processing (no data transmission)

**Pricing:** $749-$2,999 one-time, includes all features.

### Dynamsoft - Specialist Overkill

[Dynamsoft](../dynamsoft-ocr/) offers specialized SDKs for each document type. Their product lineup includes:

- **Dynamsoft Label Recognizer** - MRZ reading
- **Dynamsoft Barcode Reader** - Barcodes and QR codes
- **Dynamsoft Document Normalizer** - Document boundary detection
- **Dynamsoft Camera Enhancer** - Image preprocessing

Each product has separate licensing. A typical identity verification workflow might need three or four products.

**The value question:** If you need MRZ, barcodes, and PDF processing, Dynamsoft's total cost can exceed $5,000 per developer per year. IronOCR includes all three capabilities in a single product for $1,499-$2,999 one-time.

**When Dynamsoft makes sense:** If you need the absolute highest accuracy on damaged passports and don't need any other OCR functionality, Dynamsoft's specialized training may provide marginally better results. But for most implementations, the accuracy difference doesn't justify the cost difference.

### LEADTOOLS - Enterprise Pricing

[LEADTOOLS](../leadtools-ocr/) includes MRZ recognition in their Document SDK.

**Strengths:**
- Comprehensive feature set
- Good accuracy
- Long-established vendor

**Considerations:**
- Enterprise pricing (typically $2,000-$10,000+)
- Complex bundle structure
- May include features you don't need

### Tesseract - Possible but Custom

[Tesseract](../tesseract/) can read MRZ, but requires:
- Custom OCR-B training data
- Manual MRZ zone detection
- Your own parsing and validation code
- Check digit implementation

**Time investment:** 40-80 hours to build a robust MRZ reader with Tesseract.

**When it makes sense:** If you're processing millions of documents and license costs at scale are prohibitive, building with Tesseract may be economical.

### Comparison Summary

| Capability | IronOCR | Dynamsoft | LEADTOOLS | Tesseract |
|------------|---------|-----------|-----------|-----------|
| MRZ Reading | Built-in | Separate SDK | SDK bundle | Manual |
| Barcode Reading | Built-in | Separate SDK | SDK bundle | Not included |
| PDF Support | Built-in | Separate SDK | SDK bundle | Manual |
| Pricing Model | One-time | Annual subscription | Perpetual | Free + dev time |
| Typical Cost | $1,499 | $3,000+/year | $5,000+ | 60+ hours |

---

## Implementation Guide with IronOCR

### Basic Passport Reading

The simplest case using [IronOCR](../ironocr/):

```csharp
using IronOcr;

var ocr = new IronTesseract();
var result = ocr.ReadPassport("passport-scan.jpg");

Console.WriteLine($"Name: {result.GivenNames} {result.Surname}");
Console.WriteLine($"Passport: {result.DocumentNumber}");
Console.WriteLine($"Nationality: {result.Nationality}");
Console.WriteLine($"DOB: {result.DateOfBirth}");
Console.WriteLine($"Expiry: {result.ExpirationDate}");
Console.WriteLine($"Valid: {result.IsValid}");
```

### Manual MRZ Parsing (For Understanding)

If you want to understand the parsing or need custom handling:

```csharp
using IronOcr;
using System;
using System.Text.RegularExpressions;

public class MrzParser
{
    public PassportData ParseTd3(string mrzText)
    {
        // Clean input
        var lines = mrzText.Split(new[] { '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
            throw new ArgumentException("Invalid MRZ: Expected 2 lines for TD3");

        var line1 = lines[0].PadRight(44).Substring(0, 44);
        var line2 = lines[1].PadRight(44).Substring(0, 44);

        var data = new PassportData();

        // Line 1: Document type and name
        data.DocumentType = line1.Substring(0, 2).Trim('<');
        data.IssuingCountry = line1.Substring(2, 3);

        // Name: SURNAME<<GIVEN<NAMES
        var namePart = line1.Substring(5).TrimEnd('<');
        var nameSplit = namePart.Split(new[] { "<<" }, StringSplitOptions.None);
        data.Surname = nameSplit[0].Replace("<", " ").Trim();
        data.GivenNames = nameSplit.Length > 1
            ? nameSplit[1].Replace("<", " ").Trim()
            : "";

        // Line 2: Document number, dates, etc.
        data.DocumentNumber = line2.Substring(0, 9).TrimEnd('<');
        data.DocumentNumberCheck = line2[9];

        data.Nationality = line2.Substring(10, 3);

        // Date of birth (YYMMDD)
        var dobStr = line2.Substring(13, 6);
        data.DateOfBirth = ParseMrzDate(dobStr);
        data.DobCheck = line2[19];

        data.Sex = line2[20].ToString();

        // Expiration date (YYMMDD)
        var expiryStr = line2.Substring(21, 6);
        data.ExpirationDate = ParseMrzDate(expiryStr);
        data.ExpiryCheck = line2[27];

        // Optional data
        data.OptionalData = line2.Substring(28, 14).TrimEnd('<');
        data.OptionalCheck = line2[42];
        data.CompositeCheck = line2[43];

        // Validate check digits
        data.IsValid = ValidateAllCheckDigits(line2, data);

        return data;
    }

    private DateTime? ParseMrzDate(string yymmdd)
    {
        if (yymmdd.Length != 6) return null;

        if (!int.TryParse(yymmdd.Substring(0, 2), out var yy)) return null;
        if (!int.TryParse(yymmdd.Substring(2, 2), out var mm)) return null;
        if (!int.TryParse(yymmdd.Substring(4, 2), out var dd)) return null;

        // Century handling: 00-30 = 2000s, 31-99 = 1900s
        var year = yy <= 30 ? 2000 + yy : 1900 + yy;

        try
        {
            return new DateTime(year, mm, dd);
        }
        catch
        {
            return null;
        }
    }

    private bool ValidateAllCheckDigits(string line2, PassportData data)
    {
        // Document number check
        var docNumValid = CalculateCheckDigit(line2.Substring(0, 9))
            == CharToValue(data.DocumentNumberCheck);

        // DOB check
        var dobValid = CalculateCheckDigit(line2.Substring(13, 6))
            == CharToValue(data.DobCheck);

        // Expiry check
        var expiryValid = CalculateCheckDigit(line2.Substring(21, 6))
            == CharToValue(data.ExpiryCheck);

        return docNumValid && dobValid && expiryValid;
    }

    private int CalculateCheckDigit(string input)
    {
        int[] weights = { 7, 3, 1 };
        int sum = 0;

        for (int i = 0; i < input.Length; i++)
        {
            sum += CharToValue(input[i]) * weights[i % 3];
        }

        return sum % 10;
    }

    private int CharToValue(char c)
    {
        if (c == '<') return 0;
        if (c >= '0' && c <= '9') return c - '0';
        if (c >= 'A' && c <= 'Z') return c - 'A' + 10;
        return 0;
    }
}

public class PassportData
{
    public string DocumentType { get; set; } = "";
    public string IssuingCountry { get; set; } = "";
    public string Surname { get; set; } = "";
    public string GivenNames { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public char DocumentNumberCheck { get; set; }
    public string Nationality { get; set; } = "";
    public DateTime? DateOfBirth { get; set; }
    public char DobCheck { get; set; }
    public string Sex { get; set; } = "";
    public DateTime? ExpirationDate { get; set; }
    public char ExpiryCheck { get; set; }
    public string OptionalData { get; set; } = "";
    public char OptionalCheck { get; set; }
    public char CompositeCheck { get; set; }
    public bool IsValid { get; set; }
}
```

### Handling TD1/TD2/TD3 Formats

```csharp
public PassportData ParseMrz(string mrzText)
{
    var lines = mrzText.Split(new[] { '\n', '\r' },
        StringSplitOptions.RemoveEmptyEntries);

    return lines.Length switch
    {
        2 when lines[0].Length >= 44 => ParseTd3(mrzText), // Passport
        2 when lines[0].Length >= 36 => ParseTd2(mrzText), // Older ID
        3 => ParseTd1(mrzText), // ID card
        _ => throw new ArgumentException($"Unknown MRZ format: {lines.Length} lines")
    };
}
```

---

## Beyond Passports

### ID Cards and Driver's Licenses

Many ID cards use TD1 format MRZ zones. The same code that reads passports can read these with format detection:

```csharp
// IronOCR handles multiple MRZ formats
var result = new IronTesseract().ReadPassport("id-card.jpg");
// Works for TD1, TD2, and TD3 formats
```

### Visa Stickers

Visa stickers often include TD3-format MRZ zones. The same parsing applies, though the document type field will differ (V< instead of P<).

### Travel Documents

Refugee travel documents, emergency passports, and other travel documents use standard MRZ formats. If it has an MRZ zone, IronOCR can read it.

### Why IronOCR vs Specialized SDKs

The question isn't whether specialized SDKs are more accurate. For pristine passport scans, accuracy differences between IronOCR and specialized tools are negligible.

The question is: what else do you need?

If your KYC workflow also needs:
- PDF processing (contracts, statements)
- Barcode scanning (document IDs, tracking)
- General OCR (utility bills for address verification)

Then one [IronOCR](../ironocr/) license at $1,499 replaces potentially $5,000+ in specialized tools, with simpler deployment and unified API.

---

## Common Pitfalls

### Reflective Passport Surfaces

**The problem:** Passport lamination creates reflections that obscure the MRZ zone.

**Symptoms:** Check digits fail validation, missing characters, wildly wrong field values.

**Solutions:**
- Diffuse lighting (not direct flash)
- Anti-glare scanning surfaces
- Multiple captures from different angles
- IronOCR's preprocessing can help with mild glare

### Passport Holder Covering MRZ

**The problem:** The passport holder or user's fingers obscure part of the MRZ.

**Symptoms:** Truncated lines, missing check digits.

**Solutions:**
- UI prompts showing exactly what needs to be visible
- Capture guides overlaid on camera preview
- Validation before accepting scan (reject incomplete MRZ)

### Non-Standard Document Sizes

**The problem:** Some travel documents aren't standard passport size.

**Symptoms:** MRZ zone not found in expected location.

**Solutions:**
- Use MRZ detection rather than fixed position extraction
- IronOCR automatically searches for MRZ zones

### Preprocessing for Damaged Documents

**The problem:** Coffee stains, folds, wear, and tear degrade MRZ readability.

```csharp
using var input = new OcrInput();
input.LoadImage("worn-passport.jpg");

// Preprocessing for damaged documents
input.DeNoise();          // Remove speckles
input.Sharpen();          // Enhance edges
input.Contrast();         // Improve text visibility
input.Deskew();           // Correct rotation

var result = new IronTesseract().ReadPassport(input);
```

---

## Related Use Cases

- [Business Card Scanning](./business-card-scanning.md) - Similar structured extraction patterns
- [Invoice and Receipt OCR](./invoice-receipt-ocr.md) - Field extraction with validation
- [Form Processing](./form-processing.md) - Structured document data extraction

For library-specific documentation:

- [IronOCR](../ironocr/) - Recommended for passport scanning
- [Dynamsoft](../dynamsoft-ocr/) - Specialized MRZ SDK (higher cost)
- [LEADTOOLS](../leadtools-ocr/) - Enterprise document processing
- [Tesseract](../tesseract/) - Open source option (requires custom development)

---

## Quick Navigation

[Back to Use Case Guides](./README.md) | [Back to Main README](../README.md) | [IronOCR Documentation](../ironocr/)

---

*Last verified: January 2026*
